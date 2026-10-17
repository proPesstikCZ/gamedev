using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class M2SaveAndBankruptcyTests
    {
        private static string Json(GameSimulation sim) => SaveSerializer.ToJson(sim.ToSave());

        [Test]
        public void GameInDevelopment_AndReleasedGames_SurviveSaveAndLoad()
        {
            var db = M2TestData.Database();
            var sim = GameSimulation.NewGame(db, 8);
            sim.StartProject(M2TestData.Concept(), 1);
            M2TestData.RunUntilReleased(sim);
            sim.StartProject(M2TestData.Concept(platform: "big", engine: EngineChoice.Licensed), 2);
            sim.ActiveProject.SetAllocation(DevPhase.Production, new[] { 70, 10, 10, 10 });
            for (int i = 0; i < 5; i++) sim.AdvanceWeek();

            var json = Json(sim);
            var loaded = GameSimulation.FromSave(db, SaveSerializer.FromJson(json));

            Assert.That(loaded.ActiveProject, Is.Not.Null);
            Assert.That(loaded.ActiveProject.Phase, Is.EqualTo(sim.ActiveProject.Phase));
            Assert.That(loaded.ActiveProject.WeekInPhase, Is.EqualTo(sim.ActiveProject.WeekInPhase));
            Assert.That(loaded.ActiveProject.Bugs, Is.EqualTo(sim.ActiveProject.Bugs));
            CollectionAssert.AreEqual(sim.ActiveProject.CopyAreaPoints(), loaded.ActiveProject.CopyAreaPoints());
            CollectionAssert.AreEqual(new[] { 70, 10, 10, 10 }, loaded.ActiveProject.GetAllocation(DevPhase.Production));
            Assert.That(loaded.ActiveProject.Concept.Engine, Is.EqualTo(EngineChoice.Licensed));
            Assert.That(loaded.ReleasedGames.Count, Is.EqualTo(1));
            Assert.That(loaded.ReleasedGames[0].Review.Review, Is.EqualTo(sim.ReleasedGames[0].Review.Review));
            Assert.That(loaded.ReleasedGames[0].UnitsSold, Is.EqualTo(sim.ReleasedGames[0].UnitsSold));
            Assert.That(loaded.ReleasedGames[0].PostMortem.Entries.Count, Is.EqualTo(sim.ReleasedGames[0].PostMortem.Entries.Count));
            Assert.That(loaded.Studio.Skill, Is.EqualTo(sim.Studio.Skill));
            Assert.That(loaded.Studio.OwnsDevKit("small") && loaded.Studio.OwnsDevKit("big"), Is.True);
            Assert.That(Json(loaded), Is.EqualTo(json));
        }

        [Test]
        public void SaveWithoutProject_LoadsWithoutProject()
        {
            var db = M2TestData.Database();
            var loaded = GameSimulation.FromSave(db, SaveSerializer.FromJson(Json(GameSimulation.NewGame(db, 1))));
            Assert.That(loaded.ActiveProject, Is.Null);
            Assert.That(loaded.ReleasedGames, Is.Empty);
        }

        [Test]
        public void LoadedGame_ContinuesExactlyLikeAnUninterruptedGame_WithDevelopmentAndSales()
        {
            var db = M2TestData.Database();
            var uninterrupted = GameSimulation.NewGame(db, 77);
            new SimpleBot(BotStrategy.Random, 3).Play(uninterrupted, 120);

            var firstHalf = GameSimulation.NewGame(db, 77);
            var bot = new SimpleBot(BotStrategy.Random, 3);
            bot.Play(firstHalf, 50);
            var resumed = GameSimulation.FromSave(db, SaveSerializer.FromJson(Json(firstHalf)));
            bot.Play(resumed, 70);

            Assert.That(Json(resumed), Is.EqualTo(Json(uninterrupted)));
        }

        [Test]
        public void OldSaveVersion_IsRejected()
        {
            var json = Json(GameSimulation.NewGame(M2TestData.Database(), 1)).Replace("\"Version\": 2", "\"Version\": 1");
            Assert.Throws<FormatException>(() => SaveSerializer.FromJson(json));
        }

        [Test]
        public void StudioWithoutIncome_GoesBankrupt_AndDevelopmentStops()
        {
            var db = M2TestData.Database(TestData.Balance(startMoney: 600, monthlyCost: 300, monthsInDebt: 0));
            var sim = GameSimulation.NewGame(db, 1);
            bool bankrupted = false;
            sim.Studio.Bankrupted += () => bankrupted = true;
            Assert.That(sim.StartProject(M2TestData.Concept(), 0), Is.Empty);

            for (int i = 0; i < 52 && !sim.IsGameOver; i++) sim.AdvanceWeek();
            Assert.That(bankrupted, Is.True);
            Assert.That(sim.IsGameOver, Is.True);

            var date = sim.Date;
            var weeksWorked = sim.ActiveProject?.WeeksWorked;
            sim.AdvanceWeek();
            Assert.That(sim.Date, Is.EqualTo(date));
            Assert.That(sim.ActiveProject?.WeeksWorked, Is.EqualTo(weeksWorked));
            Assert.That(sim.ValidateConcept(M2TestData.Concept()), Does.Contain(ConceptError.Bankrupt));
        }

        [Test]
        public void GoodSales_SaveTheStudioFromBankruptcy()
        {
            // Little money, but a good game on a big platform pays the rent.
            var db = M2TestData.Database(TestData.Balance(startMoney: 1200, monthlyCost: 300, monthsInDebt: 2));
            var sim = GameSimulation.NewGame(db, 1);
            Assert.That(sim.StartProject(M2TestData.Concept(platform: "big"), 0), Is.Empty);
            var genre = db.GetGenre("arcade");
            foreach (var phase in DevPhases.WorkPhases) sim.ActiveProject.SetAllocation(phase, SimpleBot.WeightSplit(genre, phase));

            for (int i = 0; i < 52 && !sim.IsGameOver; i++) sim.AdvanceWeek();
            Assert.That(sim.IsGameOver, Is.False);
            Assert.That(sim.Studio.Money, Is.GreaterThan(0));
        }
    }
}
