using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class ContentDataTests
    {
        [Test]
        public void YearCurve_InterpolatesAndHoldsEnds()
        {
            var curve = new YearCurve(new[] { new YearPoint(1980, 2f), new YearPoint(1977, 0f) });
            Assert.That(curve.Evaluate(1976), Is.EqualTo(0f));
            Assert.That(curve.Evaluate(1977), Is.EqualTo(0f));
            Assert.That(curve.Evaluate(1978.5f), Is.EqualTo(1f).Within(1e-5));
            Assert.That(curve.Evaluate(1980), Is.EqualTo(2f));
            Assert.That(curve.Evaluate(1990), Is.EqualTo(2f));
        }

        [Test]
        public void Platform_IsAvailableOnlyBetweenReleaseAndEnd()
        {
            var p = TestData.Platform("p", 1977, 1983);
            Assert.That(p.IsAvailable(1976), Is.False);
            Assert.That(p.IsAvailable(1977), Is.True);
            Assert.That(p.IsAvailable(1983), Is.True);
            Assert.That(p.IsAvailable(1984), Is.False);
        }

        [Test]
        public void Platform_MissingGenrePopularityIsNeutral()
        {
            var p = TestData.Platform("p", popularity: new[] { new GenrePopularity("arcade", 1.5f) });
            Assert.That(p.GetGenrePopularity("arcade"), Is.EqualTo(1.5f));
            Assert.That(p.GetGenrePopularity("rpg"), Is.EqualTo(1f));
        }

        [Test]
        public void Database_FiltersAvailableContentByYear()
        {
            var db = TestData.Database();
            CollectionAssert.AreEquivalent(new[] { "arcade" }, System.Linq.Enumerable.Select(db.AvailableGenres(1975), g => g.Id));
            CollectionAssert.AreEquivalent(new[] { "arcade", "rpg" }, System.Linq.Enumerable.Select(db.AvailableGenres(1979), g => g.Id));
            CollectionAssert.AreEquivalent(new[] { "computer" }, System.Linq.Enumerable.Select(db.AvailablePlatforms(1985), p => p.Id));
        }

        [Test]
        public void AreaWeights_IndexerMatchesFields()
        {
            var w = new AreaWeights();
            for (int i = 0; i < DevAreas.Count; i++) w[(DevArea)i] = i / 10f;
            Assert.That(w.GameplayDesign, Is.EqualTo(0f));
            Assert.That(w.Polish, Is.EqualTo(0.9f).Within(1e-6));
            Assert.That(w.Sum(), Is.EqualTo(4.5f).Within(1e-5));
        }
    }
}
