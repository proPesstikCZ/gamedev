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
    }
}
