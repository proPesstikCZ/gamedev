using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class GameSimulationTests
    {
        private static void AdvanceWeeks(GameSimulation sim, int weeks)
        {
            for (int i = 0; i < weeks; i++) sim.AdvanceWeek();
        }

        [Test]
        public void NewGame_UsesBalanceStartValues()
        {
            var sim = GameSimulation.NewGame(TestData.Database(TestData.Balance(startMoney: 5000)), 1);
            Assert.That(sim.Date, Is.EqualTo(new GameDate(1972, 1)));
            Assert.That(sim.Studio.Money, Is.EqualTo(5000));
            Assert.That(sim.Studio.Fans, Is.EqualTo(0));
        }

        [Test]
        public void OneYear_PaysTwelveMonthlyCosts()
        {
            var sim = GameSimulation.NewGame(TestData.Database(TestData.Balance(startMoney: 10000, monthlyCost: 300)), 1);
            AdvanceWeeks(sim, GameDate.WeeksPerYear);
            Assert.That(sim.Date, Is.EqualTo(new GameDate(1973, 1)));
            Assert.That(sim.Studio.Money, Is.EqualTo(10000 - 12 * 300));
        }

        [Test]
        public void WithoutIncome_StudioGoesBankruptAndTimeStops()
        {
            // 1000 lasts 3 months, then 3 allowed months in debt, bankrupt at the 7th month end.
            var sim = GameSimulation.NewGame(TestData.Database(TestData.Balance(startMoney: 1000, monthlyCost: 300, monthsInDebt: 3)), 1);
            int monthEnds = 0;
            sim.Clock.MonthStarted += _ => monthEnds++;

            AdvanceWeeks(sim, 3 * GameDate.WeeksPerYear);

            Assert.That(sim.IsGameOver, Is.True);
            Assert.That(monthEnds, Is.EqualTo(7));
            var dateAtBankruptcy = sim.Date;
            sim.AdvanceWeek();
            Assert.That(sim.Date, Is.EqualTo(dateAtBankruptcy), "Time must not move after bankruptcy");
        }

        [Test]
        public void NewGame_RequiresBalance()
        {
            var db = TestData.Database();
            var noBalance = new GameDatabaseData(db.Genres, db.Themes, db.Platforms, null);
            Assert.Throws<ArgumentException>(() => GameSimulation.NewGame(noBalance, 1));
        }
    }
}
