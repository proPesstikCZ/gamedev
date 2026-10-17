using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>M4.3: the developer works while a game is in development, otherwise rests.</summary>
    public class DeveloperActivityTests
    {
        [Test]
        public void NoSimulation_Rests()
        {
            Assert.That(DeveloperActivityRule.For(null), Is.EqualTo(DeveloperActivity.Resting));
        }

        [Test]
        public void FollowsTheDevelopmentCycle()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 7);
            Assert.That(DeveloperActivityRule.For(sim), Is.EqualTo(DeveloperActivity.Resting), "Rests before the first game.");

            Assert.That(sim.StartProject(M2TestData.Concept()), Is.Empty);
            Assert.That(DeveloperActivityRule.For(sim), Is.EqualTo(DeveloperActivity.Working), "Works right after the start.");

            sim.AdvanceWeek();
            Assert.That(DeveloperActivityRule.For(sim), Is.EqualTo(DeveloperActivity.Working), "Works during development.");

            Assert.That(M2TestData.RunUntilReleased(sim), Is.Not.Null);
            Assert.That(DeveloperActivityRule.For(sim), Is.EqualTo(DeveloperActivity.Resting), "Rests after the release.");
        }

        [Test]
        public void AfterBankruptcy_Rests()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(TestData.Balance(startMoney: 2000, monthlyCost: 5000, monthsInDebt: 1)), 3);
            Assert.That(sim.StartProject(M2TestData.Concept()), Is.Empty);
            for (int i = 0; i < 60 && !sim.IsGameOver; i++) sim.AdvanceWeek();

            Assert.That(sim.IsGameOver, Is.True);
            Assert.That(sim.ActiveProject, Is.Not.Null, "Bankrupt in the middle of development.");
            Assert.That(DeveloperActivityRule.For(sim), Is.EqualTo(DeveloperActivity.Resting));
        }
    }
}
