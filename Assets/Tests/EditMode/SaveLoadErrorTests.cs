using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>M3.6: loading a bad save reports why instead of throwing.</summary>
    public class SaveLoadErrorTests
    {
        [Test]
        public void ValidSave_LoadsWithoutError()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 5);
            for (int i = 0; i < 10; i++) sim.AdvanceWeek();
            var json = SaveSerializer.ToJson(sim.ToSave());

            var save = SaveSerializer.TryFromJson(json, out var error);

            Assert.That(error, Is.EqualTo(SaveLoadError.None));
            Assert.That(save.TotalWeeks, Is.EqualTo(10));
        }

        [Test]
        public void OlderVersion_IsReportedAsUnsupported()
        {
            var save = SaveSerializer.TryFromJson("{\"Version\":1,\"StartYear\":1972,\"TotalWeeks\":3,\"Money\":100}", out var error);
            Assert.That(save, Is.Null);
            Assert.That(error, Is.EqualTo(SaveLoadError.UnsupportedVersion));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"StartYear\":1972}")]
        [TestCase("{\"Version\":2,\"TotalWeeks\":-5}")]
        public void BrokenSave_IsReportedAsCorrupt(string json)
        {
            var save = SaveSerializer.TryFromJson(json, out var error);
            Assert.That(save, Is.Null);
            Assert.That(error, Is.EqualTo(SaveLoadError.Corrupt));
        }

        /// <summary>A save with a project in development, as JSON.</summary>
        private static string SaveWithProject()
        {
            var sim = GameSimulation.NewGame(M2TestData.Database(), 5);
            Assert.That(sim.StartProject(M2TestData.Concept()), Is.Empty);
            sim.AdvanceWeek();
            return SaveSerializer.ToJson(sim.ToSave());
        }

        [TestCase("\"genreId\": \"arcade\"", "\"genreId\": \"no_such_genre\"")]
        [TestCase("\"themeId\": \"space\"", "\"themeId\": \"no_such_theme\"")]
        [TestCase("\"platformId\": \"small\"", "\"platformId\": \"no_such_platform\"")]
        [TestCase("\"audience\": 1", "\"audience\": 99")]
        public void SaveWithUnknownContent_IsRejectedBeforeItReplacesTheGame(string from, string to)
        {
            string json = SaveWithProject();
            Assert.That(json, Does.Contain(from), "Test setup: JSON format changed.");
            var save = SaveSerializer.TryFromJson(json.Replace(from, to), out var error);
            // Structurally fine, so only the content check can catch it.
            Assert.That(error, Is.EqualTo(SaveLoadError.None));

            Assert.That(GameSimulation.FindContentErrors(M2TestData.Database(), save), Is.Not.Empty);
            Assert.Throws<System.FormatException>(() => GameSimulation.FromSave(M2TestData.Database(), save));
        }

        [Test]
        public void SaveWithUnknownDevKit_IsRejected()
        {
            var save = SaveSerializer.TryFromJson(SaveWithProject(), out _);
            save.OwnedDevKits.Add("no_such_platform");
            Assert.Throws<System.FormatException>(() => GameSimulation.FromSave(M2TestData.Database(), save));
        }

        [Test]
        public void ValidSaveWithProject_FitsTheContent()
        {
            var save = SaveSerializer.TryFromJson(SaveWithProject(), out _);
            Assert.That(GameSimulation.FindContentErrors(M2TestData.Database(), save), Is.Empty);
            Assert.That(GameSimulation.FromSave(M2TestData.Database(), save).ActiveProject, Is.Not.Null);
        }
    }
}
