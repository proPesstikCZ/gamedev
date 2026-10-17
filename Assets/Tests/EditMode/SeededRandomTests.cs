using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_GivesSameSequence()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 100; i++) Assert.That(a.NextULong(), Is.EqualTo(b.NextULong()));
        }

        [Test]
        public void DifferentSeeds_GiveDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            Assert.That(a.NextULong(), Is.Not.EqualTo(b.NextULong()));
        }

        [Test]
        public void RestoredState_ContinuesTheSequence()
        {
            var a = new SeededRandom(7);
            for (int i = 0; i < 10; i++) a.NextDouble();
            var b = new SeededRandom(a.State);
            for (int i = 0; i < 10; i++) Assert.That(b.NextDouble(), Is.EqualTo(a.NextDouble()));
        }

        [Test]
        public void Values_StayInRange()
        {
            var r = new SeededRandom(123);
            for (int i = 0; i < 10000; i++)
            {
                Assert.That(r.NextDouble(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
                Assert.That(r.Next(-3, 4), Is.InRange(-3, 3));
                Assert.That(r.Range(-0.5f, 0.5f), Is.GreaterThanOrEqualTo(-0.5f).And.LessThan(0.5f));
            }
        }

        [Test]
        public void Next_RejectsEmptyRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SeededRandom(1).Next(5, 5));
        }
    }
}
