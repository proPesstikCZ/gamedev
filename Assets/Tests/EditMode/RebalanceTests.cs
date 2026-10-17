using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>M3.4: the sliders of a phase always sum to 100 %.</summary>
    public class RebalanceTests
    {
        [Test]
        public void RaisingOneSlider_LowersTheOthersInTheirRatio()
        {
            var result = DevPhases.Rebalance(new[] { 50, 30, 20 }, 0, 80);
            Assert.That(result, Is.EqualTo(new[] { 80, 12, 8 }));
        }

        [Test]
        public void OthersAtZero_ShareTheRestEvenly()
        {
            var result = DevPhases.Rebalance(new[] { 100, 0, 0, 0 }, 0, 40);
            Assert.That(result, Is.EqualTo(new[] { 40, 20, 20, 20 }));
        }

        [Test]
        public void ValueIsClampedTo0To100()
        {
            Assert.That(DevPhases.Rebalance(new[] { 34, 33, 33 }, 1, 150), Is.EqualTo(new[] { 0, 100, 0 }));
            Assert.That(DevPhases.Rebalance(new[] { 34, 33, 33 }, 1, -10)[1], Is.EqualTo(0));
        }

        [Test]
        public void AnySequenceOfMoves_KeepsSum100AndValidAllocation()
        {
            var rng = new SeededRandom(42);
            var values = DevPhases.EvenSplit(DevPhase.Production);
            for (int i = 0; i < 500; i++)
            {
                values = DevPhases.Rebalance(values, rng.Next(0, values.Length), rng.Next(0, 101));
                Assert.That(values.Sum(), Is.EqualTo(100));
                Assert.That(DevPhases.IsValidAllocation(DevPhase.Production, values), Is.True);
            }
        }
    }
}
