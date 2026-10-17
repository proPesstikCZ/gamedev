using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class SalesTests
    {
        private static SalesFactors Week(GameDatabaseData db, float review, string platform = "small", int fans = 0, int weeksOnSale = 0)
        {
            var trends = new MarketTrends(db, 1);
            return SalesCalculator.WeeklyFactors(db, trends, M2TestData.Concept(platform: platform), review, new GameDate(1975, 10), fans, weeksOnSale);
        }

        [Test]
        public void BetterReview_SellsMore()
        {
            var db = M2TestData.Database();
            Assert.That(Week(db, 8f).ExpectedUnits, Is.GreaterThan(Week(db, 5f).ExpectedUnits));
        }

        [Test]
        public void BiggerInstalledBase_SellsMore()
        {
            var db = M2TestData.Database();
            Assert.That(Week(db, 7f, "big").ExpectedUnits, Is.GreaterThan(Week(db, 7f, "small").ExpectedUnits));
        }

        [Test]
        public void SalesFall_WeekByWeek()
        {
            var db = M2TestData.Database();
            Assert.That(Week(db, 7f, weeksOnSale: 5).ExpectedUnits, Is.LessThan(Week(db, 7f, weeksOnSale: 1).ExpectedUnits));
        }

        [Test]
        public void FanFactor_GrowsButSaturates()
        {
            var sales = new SalesBalance();
            Assert.That(SalesCalculator.FanFactor(sales, 0), Is.EqualTo(1f));
            Assert.That(SalesCalculator.FanFactor(sales, 10000), Is.GreaterThan(SalesCalculator.FanFactor(sales, 1000)));
            Assert.That(SalesCalculator.FanFactor(sales, int.MaxValue), Is.LessThanOrEqualTo(1f + sales.MaxFanBonus));
        }

        [Test]
        public void ReleasedGame_SalesDropToZeroAndEnd_AndMoneyComesIn()
        {
            var balance = TestData.Balance(startMoney: 20000);
            balance.Trends.NoiseAmplitude = 0f; // a trend jump at new year must not hide the decay
            var sim = GameSimulation.NewGame(M2TestData.Database(balance), 3);
            sim.StartProject(M2TestData.Concept(platform: "big"), 2);
            var game = M2TestData.RunUntilReleased(sim);
            Assert.That(game, Is.Not.Null);

            bool ended = false;
            sim.SalesEnded += g => ended = true;
            long moneyAfterRelease = sim.Studio.Money;
            for (int i = 0; i < 200 && game.OnSale; i++) sim.AdvanceWeek();

            Assert.That(game.OnSale, Is.False);
            Assert.That(ended, Is.True);
            Assert.That(game.WeeklyUnits.Count, Is.GreaterThan(3));
            for (int i = 1; i < game.WeeklyUnits.Count; i++)
                Assert.That(game.WeeklyUnits[i], Is.LessThanOrEqualTo(game.WeeklyUnits[i - 1]), $"week {i}");
            Assert.That(game.Revenue, Is.EqualTo(SalesCalculator.Revenue(sim.Database.Balance.Sales, game.UnitsSold)).Within(game.WeeklyUnits.Count));
            Assert.That(sim.Studio.Money, Is.GreaterThan(moneyAfterRelease - 300 * 12), "Income must have arrived.");

            long unitsAtEnd = game.UnitsSold;
            for (int i = 0; i < 10; i++) sim.AdvanceWeek();
            Assert.That(game.UnitsSold, Is.EqualTo(unitsAtEnd), "No sales after the end.");
        }

        [Test]
        public void GoodGame_BringsFans_FlopLosesThem()
        {
            var sales = new SalesBalance();
            Assert.That(SalesCalculator.FansGained(sales, 9f, 1000), Is.GreaterThan(0));
            Assert.That(SalesCalculator.FansGained(sales, sales.FanReviewThreshold - 0.1f, 1000), Is.EqualTo(0));
        }

        [Test]
        public void Release_PaysPlatformLicense()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(TestData.Balance(startMoney: 20000, monthlyCost: 0)), 3);
            sim.StartProject(M2TestData.Concept(), 0);
            long before = sim.Studio.Money;
            var game = M2TestData.RunUntilReleased(sim);
            Assert.That(sim.Studio.Money, Is.EqualTo(before - 1000), "Only the license fee is paid in the release week.");
            Assert.That(game.DevelopmentCost, Is.EqualTo(500 + 1000));
        }
    }
}
