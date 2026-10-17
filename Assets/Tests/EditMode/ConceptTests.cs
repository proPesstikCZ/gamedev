using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class ConceptTests
    {
        [Test]
        public void ValidConcept_HasNoErrors()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 1);
            Assert.That(sim.ValidateConcept(M2TestData.Concept()), Is.Empty);
        }

        [Test]
        public void GenreFromTheFuture_IsRejected()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 1);
            Assert.That(sim.ValidateConcept(M2TestData.Concept(genre: "rpg")), Does.Contain(ConceptError.GenreNotAvailable));
        }

        [Test]
        public void PlatformNotYetReleased_IsRejected()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 1);
            Assert.That(sim.ValidateConcept(M2TestData.Concept(platform: "computer")), Does.Contain(ConceptError.PlatformNotAvailable));
        }

        [Test]
        public void UnknownIdsEmptyNameAndBigSize_AreRejected()
        {
            var db = M2TestData.Database();
            var errors = ConceptValidator.Validate(new GameConcept(" ", "nope", "nope", "nope", Audience.Kids, EngineChoice.Custom, GameSize.Medium), db, 1972, null);
            CollectionAssert.IsSupersetOf(errors, new[]
            {
                ConceptError.EmptyName, ConceptError.UnknownGenre, ConceptError.UnknownTheme,
                ConceptError.UnknownPlatform, ConceptError.SizeNotAvailable,
            });
        }

        [Test]
        public void ConceptThatCannotBePaid_IsRejected()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(TestData.Balance(startMoney: 100)), 1);
            Assert.That(sim.ValidateConcept(M2TestData.Concept()), Does.Contain(ConceptError.NotEnoughMoney));
        }

        [Test]
        public void StartProject_PaysDevKitOnlyOncePerPlatform()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 1);
            long start = sim.Studio.Money;

            Assert.That(sim.StartProject(M2TestData.Concept()), Is.Empty);
            Assert.That(sim.Studio.Money, Is.EqualTo(start - 500));
            Assert.That(sim.Studio.OwnsDevKit("small"), Is.True);

            M2TestData.RunUntilReleased(sim);
            long beforeSecond = sim.Studio.Money;
            Assert.That(sim.StartProject(M2TestData.Concept()), Is.Empty);
            Assert.That(sim.Studio.Money, Is.EqualTo(beforeSecond), "Second game on the same platform must not buy the dev kit again.");
        }

        [Test]
        public void LicensedEngine_CostsMoneyAndGivesBetterTechnology()
        {
            var db = M2TestData.Database();
            var custom = GameSimulation.NewGame(db, 1);
            var licensed = GameSimulation.NewGame(db, 1);
            custom.StartProject(M2TestData.Concept());
            licensed.StartProject(M2TestData.Concept(engine: EngineChoice.Licensed));

            Assert.That(custom.Studio.Money - licensed.Studio.Money, Is.EqualTo(db.Balance.Development.LicensedEngineCost));
            Assert.That(licensed.ActiveProject.Technology, Is.GreaterThan(custom.ActiveProject.Technology));
        }

        [Test]
        public void InvalidConcept_ChangesNothing_AndOnlyOneProjectRuns()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 1);
            long money = sim.Studio.Money;
            Assert.That(sim.StartProject(M2TestData.Concept(genre: "rpg")), Is.Not.Empty);
            Assert.That(sim.ActiveProject, Is.Null);
            Assert.That(sim.Studio.Money, Is.EqualTo(money));

            sim.StartProject(M2TestData.Concept());
            Assert.That(sim.StartProject(M2TestData.Concept()), Does.Contain(ConceptError.ProjectAlreadyRunning));
        }
    }
}
