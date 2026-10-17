using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// The whole game state and the weekly simulation step. No Unity scene code here,
    /// so it can be driven by tests and by the runner in the same way.
    /// </summary>
    public sealed class GameSimulation
    {
        private readonly List<ReleasedGame> _releasedGames;
        private List<TrendNews> _news = new List<TrendNews>();
        // Costs paid at the start of the active project; reported with the released game.
        private long _projectCost;
        // Set when a month starts; rent and bankruptcy are settled after the week's sales.
        private bool _monthEndPending;

        public GameDatabaseData Database { get; }
        public GameClock Clock { get; }
        public StudioState Studio { get; }
        public SeededRandom Random { get; }
        public MarketTrends Trends { get; }
        public MarketKnowledge Knowledge { get; }
        /// <summary>The game in development, or null.</summary>
        public DevelopmentProject ActiveProject { get; private set; }
        public IReadOnlyList<ReleasedGame> ReleasedGames => _releasedGames;
        /// <summary>Market news announced at the start of the current year (GDD 9).</summary>
        public IReadOnlyList<TrendNews> CurrentNews => _news;

        public GameDate Date => Clock.Date;
        public bool IsGameOver => Studio.IsBankrupt;

        /// <summary>Raised when a game is released, after its review and post-mortem are ready.</summary>
        public event Action<ReleasedGame> GameReleased;
        /// <summary>Raised when a released game stops selling.</summary>
        public event Action<ReleasedGame> SalesEnded;
        /// <summary>Raised at the start of every year with the trend news for the years ahead.</summary>
        public event Action<IReadOnlyList<TrendNews>> TrendNewsAnnounced;

        private GameSimulation(GameDatabaseData database, GameClock clock, StudioState studio, SeededRandom random,
            ulong trendSeed, MarketKnowledge knowledge, DevelopmentProject activeProject, long projectCost, List<ReleasedGame> releasedGames)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            if (database.Balance == null) throw new ArgumentException("Database has no balance config.", nameof(database));
            Clock = clock;
            Studio = studio;
            Random = random;
            Trends = new MarketTrends(database, trendSeed);
            Knowledge = knowledge ?? new MarketKnowledge();
            ActiveProject = activeProject;
            _projectCost = activeProject != null ? projectCost : 0;
            _releasedGames = releasedGames ?? new List<ReleasedGame>();
            _news = Trends.NewsForYear(Clock.Date.Year);
            Clock.MonthStarted += OnMonthStarted;
            Clock.YearStarted += OnYearStarted;
        }

        public static GameSimulation NewGame(GameDatabaseData database, ulong seed)
        {
            if (database?.Balance == null) throw new ArgumentException("Database has no balance config.", nameof(database));
            var balance = database.Balance;
            // Trends get their own seed so they never consume the simulation RNG.
            ulong trendSeed = new SeededRandom(seed ^ 0x7472656E64UL).NextULong();
            var studio = new StudioState(balance.StartMoney, skill: Math.Max(0.01f, balance.Development.StartSkill));
            return new GameSimulation(database, new GameClock(balance.StartYear), studio, new SeededRandom(seed),
                trendSeed, null, null, 0, null);
        }

        public static GameSimulation FromSave(GameDatabaseData database, SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            return new GameSimulation(database,
                new GameClock(save.StartYear, save.TotalWeeks),
                new StudioState(save.Money, save.Fans, save.MonthsInDebt, save.IsBankrupt, save.Skill, save.OwnedDevKits),
                new SeededRandom(SaveSerializer.ParseRandomState(save.RandomState)),
                SaveSerializer.ParseRandomState(save.TrendSeed),
                save.Knowledge,
                save.HasActiveProject ? save.ActiveProject : null,
                save.ActiveProjectCost,
                save.ReleasedGames != null ? new List<ReleasedGame>(save.ReleasedGames) : null);
        }

        /// <summary>Errors that would stop this concept from starting now. Empty = can start.</summary>
        public List<ConceptError> ValidateConcept(GameConcept concept, int qaWeeks = 0)
        {
            var errors = ConceptValidator.Validate(concept, Database, Date.Year, Studio);
            if (ActiveProject != null) errors.Add(ConceptError.ProjectAlreadyRunning);
            if (!errors.Contains(ConceptError.UnknownPlatform) && !errors.Contains(ConceptError.PlatformNotAvailable)
                && qaWeeks > MaxQaWeeksFor(concept))
                errors.Add(ConceptError.PlatformEndsBeforeRelease);
            return errors;
        }

        /// <summary>
        /// Most QA weeks a game started now can have and still be released while the platform
        /// takes new games (until the end of its EndYear). -1 = cannot be released in time even without QA.
        /// </summary>
        public int MaxQaWeeksFor(GameConcept concept)
        {
            var dev = Database.Balance.Development;
            var platform = Database.GetPlatform(concept?.PlatformId);
            if (platform == null) return -1;
            int workWeeks = dev.PreProductionWeeks + dev.ProductionWeeks + dev.FinishingWeeks;
            int lastWeek = (platform.EndYear - Clock.StartYear + 1) * GameDate.WeeksPerYear - 1;
            int weeksLeft = lastWeek - Clock.TotalWeeks;
            return Math.Min(dev.MaxQaWeeks, weeksLeft - workWeeks);
        }

        /// <summary>
        /// Starts development: buys the dev kit (once per platform) and the licensed engine.
        /// Returns the errors; when there are any, nothing changes.
        /// </summary>
        public List<ConceptError> StartProject(GameConcept concept, int qaWeeks = 0)
        {
            var dev = Database.Balance.Development;
            if (qaWeeks < 0 || qaWeeks > dev.MaxQaWeeks) throw new ArgumentOutOfRangeException(nameof(qaWeeks), qaWeeks, $"Must be 0..{dev.MaxQaWeeks}.");
            var errors = ValidateConcept(concept, qaWeeks);
            if (errors.Count > 0) return errors;

            var platform = Database.GetPlatform(concept.PlatformId);
            long cost = ConceptValidator.UpfrontCost(concept, Database, Studio);
            Studio.Spend(cost);
            Studio.AddDevKit(platform.Id);

            float tech = DevelopmentProject.TechnologyFor(concept.Engine, platform, dev);
            // QA cannot be stretched later past the platform's last year.
            ActiveProject = new DevelopmentProject(concept, dev, Studio.Skill, tech, qaWeeks, MaxQaWeeksFor(concept));
            _projectCost = cost;
            return errors;
        }

        /// <summary>
        /// Moves the game one week forward: sales, development and release first, then rent and
        /// the bankruptcy check, so the week's income counts. Does nothing after bankruptcy.
        /// </summary>
        public void AdvanceWeek()
        {
            if (IsGameOver) return;
            Clock.AdvanceWeek();

            for (int i = 0; i < _releasedGames.Count; i++)
            {
                var game = _releasedGames[i];
                if (!game.OnSale) continue;
                game.SellWeek(Database, Trends, Date, Studio, out bool ended);
                if (ended) SalesEnded?.Invoke(game);
            }

            if (ActiveProject != null && ActiveProject.AdvanceWeek()) Release();

            if (_monthEndPending)
            {
                _monthEndPending = false;
                var balance = Database.Balance;
                Studio.ApplyMonthEnd(balance.MonthlyGarageCost, balance.MonthsInDebtBeforeBankruptcy);
            }
        }

        private void Release()
        {
            var project = ActiveProject;
            var concept = project.Concept;
            int year = Date.Year;
            var balance = Database.Balance;
            var platform = Database.GetPlatform(concept.PlatformId);

            var review = ReviewCalculator.Calculate(Database, concept, project.CopyAreaPoints(), project.Bugs, year, Random);
            float genreTrend = Trends.GenreTrend(concept.GenreId, year);
            var report = PostMortemBuilder.Build(Database, concept, review, genreTrend, Knowledge);

            long license = platform != null ? platform.LicenseFee : 0;
            Studio.Spend(license);
            if (review.Review < balance.Sales.FlopReviewThreshold)
                Studio.AddFans(-(int)Math.Round(Studio.Fans * balance.Sales.FanLossOnFlop));
            Studio.ImproveSkill(balance.Development.SkillGainPerGame, balance.Development.MaxSkill);

            var released = new ReleasedGame(concept, review, report, Clock.TotalWeeks, project.WeeksWorked, _projectCost + license);
            _releasedGames.Add(released);
            ActiveProject = null;
            _projectCost = 0;
            GameReleased?.Invoke(released);
        }

        /// <summary>
        /// Snapshot for saving. It shares objects with the running game, so serialize it
        /// (SaveSerializer.ToJson) right away instead of keeping it.
        /// </summary>
        public SaveData ToSave()
        {
            return new SaveData
            {
                StartYear = Clock.StartYear,
                TotalWeeks = Clock.TotalWeeks,
                Money = Studio.Money,
                Fans = Studio.Fans,
                MonthsInDebt = Studio.MonthsInDebt,
                IsBankrupt = Studio.IsBankrupt,
                Skill = Studio.Skill,
                OwnedDevKits = new List<string>(Studio.OwnedDevKits),
                RandomState = SaveSerializer.FormatRandomState(Random.State),
                TrendSeed = SaveSerializer.FormatRandomState(Trends.Seed),
                HasActiveProject = ActiveProject != null,
                ActiveProject = ActiveProject,
                ActiveProjectCost = _projectCost,
                ReleasedGames = new List<ReleasedGame>(_releasedGames),
                Knowledge = Knowledge,
            };
        }

        private void OnMonthStarted(GameDate date) => _monthEndPending = true;

        private void OnYearStarted(GameDate date)
        {
            _news = Trends.NewsForYear(date.Year);
            if (_news.Count > 0) TrendNewsAnnounced?.Invoke(_news);
        }
    }
}
