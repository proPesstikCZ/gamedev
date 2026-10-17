using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    public enum BotStrategy
    {
        /// <summary>Reads the hidden data: best combo, best audience, split by genre weights.</summary>
        FollowWeights = 0,
        /// <summary>Random choices and random splits.</summary>
        Random = 1,
    }

    /// <summary>
    /// A simple automatic player for simulation tests, balancing and the debug command.
    /// Uses its own RNG so it never changes the simulation's random sequence.
    /// </summary>
    public sealed class SimpleBot
    {
        private static readonly Audience[] Audiences = { Audience.Kids, Audience.Everyone, Audience.Adults };

        private readonly SeededRandom _random;
        private int _gamesStarted;

        public BotStrategy Strategy { get; }
        public int QaWeeksForWeightBot { get; set; } = 3;

        public SimpleBot(BotStrategy strategy, ulong seed)
        {
            Strategy = strategy;
            _random = new SeededRandom(seed);
        }

        /// <summary>Starts a game when none is running, then advances one week.</summary>
        public void PlayWeek(GameSimulation sim)
        {
            if (sim.IsGameOver) return;
            if (sim.ActiveProject == null) TryStartGame(sim);
            sim.AdvanceWeek();
        }

        public void Play(GameSimulation sim, int weeks)
        {
            for (int i = 0; i < weeks && !sim.IsGameOver; i++) PlayWeek(sim);
        }

        /// <summary>Returns true when a game was started.</summary>
        public bool TryStartGame(GameSimulation sim)
        {
            var concept = Strategy == BotStrategy.FollowWeights ? ChooseByWeights(sim) : ChooseRandomly(sim);
            if (concept == null) return false;

            var dev = sim.Database.Balance.Development;
            int qa = Strategy == BotStrategy.FollowWeights ? Math.Min(QaWeeksForWeightBot, dev.MaxQaWeeks) : _random.Next(0, dev.MaxQaWeeks + 1);
            if (sim.StartProject(concept, qa).Count > 0) return false;
            _gamesStarted++;

            var genre = sim.Database.GetGenre(concept.GenreId);
            foreach (var phase in DevPhases.WorkPhases)
                sim.ActiveProject.SetAllocation(phase, Strategy == BotStrategy.FollowWeights ? WeightSplit(genre, phase) : RandomSplit(phase));
            return true;
        }

        /// <summary>Split proportional to weight squared; with area score = points^0.5 this is the best split.</summary>
        public static int[] WeightSplit(GenreData genre, DevPhase phase)
        {
            var areas = DevPhases.AreasOf(phase);
            var shares = new float[areas.Count];
            for (int i = 0; i < areas.Count; i++)
            {
                float w = genre.Weights[areas[i]];
                shares[i] = w * w;
            }
            return DevPhases.ToPercents(shares);
        }

        private int[] RandomSplit(DevPhase phase)
        {
            var shares = new float[DevPhases.AreasOf(phase).Count];
            for (int i = 0; i < shares.Length; i++) shares[i] = _random.Range(0f, 1f);
            return DevPhases.ToPercents(shares);
        }

        private GameConcept ChooseByWeights(GameSimulation sim)
        {
            var db = sim.Database;
            int year = sim.Date.Year;
            var review = db.Balance.Review;
            GameConcept best = null;
            float bestScore = float.MinValue;

            foreach (var platform in Affordable(sim))
            {
                float installBase = platform.InstallBaseMillions.Evaluate(year);
                foreach (var genre in db.AvailableGenres(year))
                {
                    foreach (var theme in db.AvailableThemes(year))
                    {
                        var combo = theme.TryGetCombo(genre.Id, out var c) ? c : ComboRating.Bad;
                        var audience = Audiences.OrderByDescending(a => genre.AudienceFit[a]).First();
                        float score = ReviewCalculator.ComboFactor(review, combo) * genre.AudienceFit[audience]
                                      * sim.Trends.GenreTrend(genre.Id, year) * sim.Trends.ThemeTrend(theme.Id, year)
                                      * platform.GetGenrePopularity(genre.Id) * installBase;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = new GameConcept(NextName(), genre.Id, theme.Id, platform.Id, audience);
                        }
                    }
                }
            }
            return best;
        }

        private GameConcept ChooseRandomly(GameSimulation sim)
        {
            var db = sim.Database;
            int year = sim.Date.Year;
            var platforms = Affordable(sim).ToList();
            var genres = db.AvailableGenres(year).ToList();
            var themes = db.AvailableThemes(year).ToList();
            if (platforms.Count == 0 || genres.Count == 0 || themes.Count == 0) return null;
            return new GameConcept(NextName(),
                genres[_random.Next(0, genres.Count)].Id,
                themes[_random.Next(0, themes.Count)].Id,
                platforms[_random.Next(0, platforms.Count)].Id,
                Audiences[_random.Next(0, Audiences.Length)]);
        }

        private static IEnumerable<PlatformData> Affordable(GameSimulation sim)
        {
            foreach (var p in sim.Database.AvailablePlatforms(sim.Date.Year))
            {
                long kit = sim.Studio.OwnsDevKit(p.Id) ? 0 : p.DevKitCost;
                if (kit <= sim.Studio.Money) yield return p;
            }
        }

        // Player-entered title in the real game; bots just number their games.
        private string NextName() => $"Bot game {_gamesStarted + 1}";
    }
}
