using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class SaveLoadTests
    {
        private static void Play(GameSimulation sim, int weeks, Action<GameSimulation> eachWeek = null)
        {
            for (int i = 0; i < weeks; i++)
            {
                sim.AdvanceWeek();
                eachWeek?.Invoke(sim);
            }
        }

        [Test]
        public void SaveAndLoad_RestoresTheSameState()
        {
            var db = TestData.Database();
            var sim = GameSimulation.NewGame(db, 99);
            Play(sim, 40);
            sim.Studio.AddFans(123);
            sim.Random.NextDouble();

            var json = SaveSerializer.ToJson(sim.ToSave());
            var loaded = GameSimulation.FromSave(db, SaveSerializer.FromJson(json));

            Assert.That(loaded.Date, Is.EqualTo(sim.Date));
            Assert.That(loaded.Studio.Money, Is.EqualTo(sim.Studio.Money));
            Assert.That(loaded.Studio.Fans, Is.EqualTo(123));
            Assert.That(loaded.Studio.MonthsInDebt, Is.EqualTo(sim.Studio.MonthsInDebt));
            Assert.That(loaded.Studio.IsBankrupt, Is.EqualTo(sim.Studio.IsBankrupt));
            Assert.That(loaded.Random.State, Is.EqualTo(sim.Random.State));
            Assert.That(SaveSerializer.ToJson(loaded.ToSave()), Is.EqualTo(json));
        }

        [Test]
        public void LoadedGame_ContinuesExactlyLikeAnUninterruptedGame()
        {
            var db = TestData.Database(TestData.Balance(startMoney: 3000, monthlyCost: 300));
            Action<GameSimulation> useRandom = s =>
            {
                if (s.Random.NextDouble() < 0.3) s.Studio.AddMoney(s.Random.Next(50, 500));
            };

            var uninterrupted = GameSimulation.NewGame(db, 2024);
            Play(uninterrupted, 60, useRandom);

            var firstHalf = GameSimulation.NewGame(db, 2024);
            Play(firstHalf, 30, useRandom);
            var resumed = GameSimulation.FromSave(db, SaveSerializer.FromJson(SaveSerializer.ToJson(firstHalf.ToSave())));
            Play(resumed, 30, useRandom);

            Assert.That(resumed.Date, Is.EqualTo(uninterrupted.Date));
            Assert.That(resumed.Studio.Money, Is.EqualTo(uninterrupted.Studio.Money));
            Assert.That(resumed.Studio.MonthsInDebt, Is.EqualTo(uninterrupted.Studio.MonthsInDebt));
            Assert.That(resumed.Random.State, Is.EqualTo(uninterrupted.Random.State));
        }

        [Test]
        public void RandomState_SurvivesFullUInt64Range()
        {
            var db = TestData.Database();
            var sim = GameSimulation.NewGame(db, ulong.MaxValue);
            var loaded = GameSimulation.FromSave(db, SaveSerializer.FromJson(SaveSerializer.ToJson(sim.ToSave())));
            Assert.That(loaded.Random.State, Is.EqualTo(ulong.MaxValue));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"Version\": 999, \"RandomState\": \"0\"}")]
        [TestCase("{\"Version\": 1, \"RandomState\": \"xyz\"}")]
        [TestCase("{\"Version\": 1, \"TotalWeeks\": -5, \"RandomState\": \"0\"}")]
        public void InvalidSave_IsRejected(string json)
        {
            Assert.That(() => SaveSerializer.FromJson(json), Throws.InstanceOf<Exception>());
        }
    }
}
