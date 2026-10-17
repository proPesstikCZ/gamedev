using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Regression tests for the M2 review findings.</summary>
    public class ReviewFixTests
    {
        private static string Json(GameSimulation sim) => SaveSerializer.ToJson(sim.ToSave());

        [Test]
        public void WeeklySales_AreCountedBeforeTheBankruptcyCheck()
        {
            // A game on sale, then the studio is put one coin into debt on its last allowed month.
            var db = M2TestData.Database(TestData.Balance(startMoney: 20000, monthlyCost: 300, monthsInDebt: 3));
            var sim = GameSimulation.NewGame(db, 1);
            sim.StartProject(M2TestData.Concept(platform: "big"), 0);
            var game = M2TestData.RunUntilReleased(sim);
            while (sim.Date.Month == GameDate.FromTotalWeeks(1972, sim.Clock.TotalWeeks + 1).Month) sim.AdvanceWeek();
            Assert.That(game.OnSale, Is.True);

            var save = sim.ToSave();
            save.Money = -1;
            save.MonthsInDebt = 3;
            var loaded = GameSimulation.FromSave(db, SaveSerializer.FromJson(SaveSerializer.ToJson(save)));
            int monthBefore = loaded.Date.Month;
            long unitsBefore = loaded.ReleasedGames[0].UnitsSold;

            loaded.AdvanceWeek(); // month changes in this week: rent is due, and this week's sales arrive
            Assert.That(loaded.Date.Month, Is.Not.EqualTo(monthBefore));
            Assert.That(loaded.ReleasedGames[0].UnitsSold, Is.GreaterThan(unitsBefore), "The week's sales must be counted.");
            Assert.That(loaded.IsGameOver, Is.False);
            Assert.That(loaded.Studio.Money, Is.GreaterThan(0));
            Assert.That(loaded.Studio.MonthsInDebt, Is.EqualTo(0));
        }

        [Test]
        public void StudioWithoutSales_StillGoesBankruptAtMonthEnd()
        {
            var db = M2TestData.Database(TestData.Balance(startMoney: 100, monthlyCost: 300, monthsInDebt: 0));
            var sim = GameSimulation.NewGame(db, 1);
            for (int i = 0; i < 10 && !sim.IsGameOver; i++) sim.AdvanceWeek();
            Assert.That(sim.IsGameOver, Is.True);
            Assert.That(sim.Date, Is.EqualTo(GameDate.FromTotalWeeks(1972, 5)), "Bankrupt in the first week of month 2.");
        }

        private static GameDatabaseData DatabaseWithPlatformEnding1977()
        {
            var genres = new List<GenreData> { TestData.Genre("arcade") };
            var themes = new List<ThemeData> { TestData.Theme("space", genres) };
            var old = new PlatformData("old", "platform.old.name", 1972, 1977, 0, 100, 0.1f,
                new YearCurve(new[] { new YearPoint(1972, 1f) }), null);
            var balance = TestData.Balance(startMoney: 1_000_000, monthlyCost: 0);
            return new GameDatabaseData(genres, themes, new List<PlatformData> { old }, balance);
        }

        private static GameSimulation SimAt(GameDatabaseData db, int year, int week)
        {
            var sim = GameSimulation.NewGame(db, 1);
            int target = (year - 1972) * GameDate.WeeksPerYear + week - 1;
            while (sim.Clock.TotalWeeks < target) sim.AdvanceWeek();
            Assert.That(sim.Date, Is.EqualTo(new GameDate(year, week)));
            return sim;
        }

        [Test]
        public void GameThatWouldBeReleasedAfterPlatformEnd_CannotStart()
        {
            var db = DatabaseWithPlatformEnding1977();
            var sim = SimAt(db, 1977, 52);
            var concept = new GameConcept("Late", "arcade", "space", "old", Audience.Everyone);
            Assert.That(sim.MaxQaWeeksFor(concept), Is.LessThan(0));
            Assert.That(sim.StartProject(concept, 0), Does.Contain(ConceptError.PlatformEndsBeforeRelease));
            Assert.That(sim.ActiveProject, Is.Null);
        }

        [Test]
        public void QaLength_IsLimitedSoTheGameIsReleasedBeforePlatformEnd()
        {
            var db = DatabaseWithPlatformEnding1977();
            var dev = db.Balance.Development;
            int workWeeks = dev.PreProductionWeeks + dev.ProductionWeeks + dev.FinishingWeeks;
            // Start so that exactly two QA weeks still fit into 1977.
            var sim = SimAt(db, 1977, 52 - workWeeks - 2);
            var concept = new GameConcept("Tight", "arcade", "space", "old", Audience.Everyone);

            Assert.That(sim.MaxQaWeeksFor(concept), Is.EqualTo(2));
            Assert.That(sim.StartProject(concept, 3), Does.Contain(ConceptError.PlatformEndsBeforeRelease));
            Assert.That(sim.StartProject(concept, 2), Is.Empty);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sim.ActiveProject.SetQaWeeks(3));

            var game = M2TestData.RunUntilReleased(sim);
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Review.Year, Is.EqualTo(1977));
        }

        [Test]
        public void FreeStart_IsAllowedInDebt_PaidStartIsNot()
        {
            var db = M2TestData.Database(TestData.Balance(startMoney: 20000, monthlyCost: 300, monthsInDebt: 3));
            var sim = GameSimulation.NewGame(db, 1);
            sim.StartProject(M2TestData.Concept(), 0);
            M2TestData.RunUntilReleased(sim);

            var save = sim.ToSave();
            save.Money = -500;
            save.MonthsInDebt = 1;
            var inDebt = GameSimulation.FromSave(db, SaveSerializer.FromJson(SaveSerializer.ToJson(save)));

            Assert.That(inDebt.ValidateConcept(M2TestData.Concept()), Is.Empty, "Own dev kit + own engine costs nothing.");
            Assert.That(inDebt.ValidateConcept(M2TestData.Concept(engine: EngineChoice.Licensed)), Does.Contain(ConceptError.NotEnoughMoney));
            Assert.That(inDebt.ValidateConcept(M2TestData.Concept(platform: "big")), Does.Contain(ConceptError.NotEnoughMoney));
            Assert.That(inDebt.StartProject(M2TestData.Concept()), Is.Empty);
            Assert.That(inDebt.Studio.Money, Is.EqualTo(-500));
        }
    }
}
