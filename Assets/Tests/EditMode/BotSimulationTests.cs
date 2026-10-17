using Game.Core;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>PLAN 2.6: ten years played by simple bots on the real content.</summary>
    public class BotSimulationTests
    {
        private const int TenYears = 52 * 10;
        private GameDatabaseData _db;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            _db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ContentAssetTests.DatabasePath).ToCore();
        }

        private GameSimulation Play(BotStrategy strategy, ulong seed)
        {
            var sim = GameSimulation.NewGame(_db, seed);
            new SimpleBot(strategy, seed + 1000).Play(sim, TenYears);
            return sim;
        }

        [Test]
        public void TenYears_RunWithoutExceptions_AndReleaseGames()
        {
            foreach (var strategy in new[] { BotStrategy.FollowWeights, BotStrategy.Random })
            {
                GameSimulation sim = null;
                Assert.DoesNotThrow(() => sim = Play(strategy, 1));
                Assert.That(sim.ReleasedGames.Count, Is.GreaterThan(5), strategy.ToString());
                if (!sim.IsGameOver) Assert.That(sim.Date.Year, Is.EqualTo(1982));
            }
        }

        [Test]
        public void SameSeed_GivesTheSameTenYears()
        {
            var a = Play(BotStrategy.FollowWeights, 21);
            var b = Play(BotStrategy.FollowWeights, 21);
            Assert.That(SaveSerializer.ToJson(a.ToSave()), Is.EqualTo(SaveSerializer.ToJson(b.ToSave())));

            var c = Play(BotStrategy.Random, 21);
            var d = Play(BotStrategy.Random, 21);
            Assert.That(SaveSerializer.ToJson(c.ToSave()), Is.EqualTo(SaveSerializer.ToJson(d.ToSave())));
        }

        [Test]
        public void BotFollowingWeights_EarnsMoreThanRandomBot()
        {
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var smart = Play(BotStrategy.FollowWeights, seed);
                var random = Play(BotStrategy.Random, seed);
                Assert.That(smart.IsGameOver, Is.False, $"seed {seed}: weight bot went bankrupt");
                Assert.That(smart.Studio.Money, Is.GreaterThan(random.Studio.Money), $"seed {seed}");
                Assert.That(AverageReview(smart), Is.GreaterThan(AverageReview(random)), $"seed {seed}");
            }
        }

        [Test]
        public void DebugCommand_SimulatesOneGameAndReportsIt()
        {
            string report = SimulateOneGameCommand.Simulate(_db, 1972, BotStrategy.FollowWeights);
            StringAssert.Contains("REVIEW", report);
            StringAssert.Contains("postmortem.hint.weight_high", report);
            StringAssert.Contains("Weekly units", report);
            Assert.That(report, Is.EqualTo(SimulateOneGameCommand.Simulate(_db, 1972, BotStrategy.FollowWeights)), "Same seed, same report.");
        }

        private static float AverageReview(GameSimulation sim)
        {
            float sum = 0f;
            foreach (var g in sim.ReleasedGames) sum += g.Review.Review;
            return sim.ReleasedGames.Count > 0 ? sum / sim.ReleasedGames.Count : 0f;
        }
    }
}
