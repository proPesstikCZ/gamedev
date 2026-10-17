using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class KnowledgeTests
    {
        private static (GameSimulation sim, ReleasedGame game) ReleaseOne(ulong seed = 1, GameConcept concept = null)
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), seed);
            Assert.That(sim.StartProject(concept ?? M2TestData.Concept(), 2), Is.Empty);
            return (sim, M2TestData.RunUntilReleased(sim));
        }

        [Test]
        public void Release_RevealsTheComboOfTheGame()
        {
            var (sim, _) = ReleaseOne();
            Assert.That(sim.Knowledge.IsComboRevealed("arcade", "space"), Is.True);
            Assert.That(sim.Knowledge.IsComboRevealed("arcade", "pirates"), Is.False);
            Assert.That(sim.Knowledge.TryGetCombo(sim.Database, "arcade", "space", out var rating), Is.True);
            Assert.That(rating, Is.EqualTo(ComboRating.Excellent));
        }

        [Test]
        public void Release_RevealsTheMostAndTheLeastImportantWeight()
        {
            var (sim, game) = ReleaseOne();
            // UnevenWeights: GameplayDesign 1.0 is the first highest, StoryAndWorld 0.05 the first lowest.
            Assert.That(sim.Knowledge.IsWeightRevealed("arcade", DevArea.GameplayDesign), Is.True);
            Assert.That(sim.Knowledge.IsWeightRevealed("arcade", DevArea.StoryAndWorld), Is.True);
            Assert.That(sim.Knowledge.RevealedWeightCount, Is.EqualTo(2));
            Assert.That(sim.Knowledge.GetWeightLevel(sim.Database, "arcade", DevArea.GameplayDesign), Is.EqualTo(WeightLevel.High));
            Assert.That(sim.Knowledge.GetWeightLevel(sim.Database, "arcade", DevArea.StoryAndWorld), Is.EqualTo(WeightLevel.Low));
            Assert.That(sim.Knowledge.GetWeightLevel(sim.Database, "arcade", DevArea.Graphics), Is.EqualTo(WeightLevel.Unknown));

            var high = game.PostMortem.Find(PostMortemKeys.HintWeightHigh);
            var low = game.PostMortem.Find(PostMortemKeys.HintWeightLow);
            CollectionAssert.AreEqual(new[] { "arcade", "gameplay_design" }, high.Args);
            CollectionAssert.AreEqual(new[] { "arcade", "story_and_world" }, low.Args);
        }

        [Test]
        public void NextGameInTheSameGenre_RevealsTheNextWeights()
        {
            var (sim, _) = ReleaseOne();
            sim.StartProject(M2TestData.Concept(), 0);
            var second = M2TestData.RunUntilReleased(sim);

            // Next highest is Gameplay (1.0, tie broken by area order), next lowest DialogueAndText (0.05).
            Assert.That(sim.Knowledge.IsWeightRevealed("arcade", DevArea.Gameplay), Is.True);
            Assert.That(sim.Knowledge.IsWeightRevealed("arcade", DevArea.DialogueAndText), Is.True);
            Assert.That(sim.Knowledge.RevealedWeightCount, Is.EqualTo(4));
            Assert.That(second.PostMortem.FindAll(PostMortemKeys.HintWeightHigh).Single().Args[1], Is.EqualTo("gameplay"));
        }

        [Test]
        public void AllWeights_AreKnownAfterEnoughGames()
        {
            var (sim, _) = ReleaseOne();
            for (int i = 0; i < 6; i++)
            {
                sim.StartProject(M2TestData.Concept(), 0);
                M2TestData.RunUntilReleased(sim);
            }
            Assert.That(sim.Knowledge.RevealedWeightCount, Is.EqualTo(DevAreas.Count));
        }

        [Test]
        public void PostMortem_ContainsOnlyKeysAndExplainsTheFactors()
        {
            var (_, game) = ReleaseOne(concept: M2TestData.Concept(theme: "pirates", audience: Audience.Adults));
            var report = game.PostMortem;
            Assert.That(report.Entries.All(e => e.Key.StartsWith("postmortem.")), Is.True);
            Assert.That(report.Find(PostMortemKeys.Review).Value, Is.EqualTo(game.Review.Review));
            Assert.That(report.Has(PostMortemKeys.ComboBad), Is.True);
            CollectionAssert.AreEqual(new[] { "arcade", "pirates" }, report.Find(PostMortemKeys.ComboBad).Args);
            Assert.That(report.Has(PostMortemKeys.AudiencePoor), Is.True);
            Assert.That(report.Has(PostMortemKeys.StrongestArea), Is.True);
        }

        [Test]
        public void Knowledge_SurvivesSaveAndLoad()
        {
            var (sim, _) = ReleaseOne();
            var loaded = GameSimulation.FromSave(sim.Database, SaveSerializer.FromJson(SaveSerializer.ToJson(sim.ToSave())));

            Assert.That(loaded.Knowledge.IsComboRevealed("arcade", "space"), Is.True);
            Assert.That(loaded.Knowledge.IsWeightRevealed("arcade", DevArea.GameplayDesign), Is.True);
            Assert.That(loaded.Knowledge.IsWeightRevealed("arcade", DevArea.Graphics), Is.False);
            Assert.That(loaded.Knowledge.RevealedWeightCount, Is.EqualTo(2));

            loaded.StartProject(M2TestData.Concept(), 0);
            M2TestData.RunUntilReleased(loaded);
            Assert.That(loaded.Knowledge.RevealedWeightCount, Is.EqualTo(4), "Loaded knowledge must keep growing, not restart.");
        }
    }
}
