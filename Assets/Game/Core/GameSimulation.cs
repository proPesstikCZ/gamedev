using System;

namespace Game.Core
{
    /// <summary>
    /// The whole game state and the weekly simulation step. No Unity scene code here,
    /// so it can be driven by tests and by the runner in the same way.
    /// </summary>
    public sealed class GameSimulation
    {
        public GameDatabaseData Database { get; }
        public GameClock Clock { get; }
        public StudioState Studio { get; }
        public SeededRandom Random { get; }

        public GameDate Date => Clock.Date;
        public bool IsGameOver => Studio.IsBankrupt;

        private GameSimulation(GameDatabaseData database, GameClock clock, StudioState studio, SeededRandom random)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            if (database.Balance == null) throw new ArgumentException("Database has no balance config.", nameof(database));
            Clock = clock;
            Studio = studio;
            Random = random;
            Clock.MonthStarted += OnMonthStarted;
        }

        public static GameSimulation NewGame(GameDatabaseData database, ulong seed)
        {
            if (database?.Balance == null) throw new ArgumentException("Database has no balance config.", nameof(database));
            var balance = database.Balance;
            return new GameSimulation(database, new GameClock(balance.StartYear), new StudioState(balance.StartMoney), new SeededRandom(seed));
        }

        public static GameSimulation FromSave(GameDatabaseData database, SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            return new GameSimulation(database,
                new GameClock(save.StartYear, save.TotalWeeks),
                new StudioState(save.Money, save.Fans, save.MonthsInDebt, save.IsBankrupt),
                new SeededRandom(SaveSerializer.ParseRandomState(save.RandomState)));
        }

        /// <summary>Moves the game one week forward. Does nothing after bankruptcy.</summary>
        public void AdvanceWeek()
        {
            if (IsGameOver) return;
            Clock.AdvanceWeek();
        }

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
                RandomState = SaveSerializer.FormatRandomState(Random.State),
            };
        }

        private void OnMonthStarted(GameDate date)
        {
            var balance = Database.Balance;
            Studio.ApplyMonthEnd(balance.MonthlyGarageCost, balance.MonthsInDebtBeforeBankruptcy);
        }
    }
}
