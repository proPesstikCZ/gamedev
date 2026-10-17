using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class ReviewTests
    {
        /// <summary>Develops one game with the given splits and returns its release.</summary>
        private static ReleasedGame Develop(GameDatabaseData db, ulong seed, bool followWeights, GameConcept concept = null)
        {
            var sim = GameSimulation.NewGame(db, seed);
            Assert.That(sim.StartProject(concept ?? M2TestData.Concept(), 3), Is.Empty);
            var genre = db.GetGenre(sim.ActiveProject.Concept.GenreId);
            foreach (var phase in DevPhases.WorkPhases)
            {
                var split = SimpleBot.WeightSplit(genre, phase);
                if (!followWeights) System.Array.Reverse(split); // opposite: most points where the weight is lowest
                sim.ActiveProject.SetAllocation(phase, split);
            }
            return M2TestData.RunUntilReleased(sim);
        }

        [Test]
        public void SplitByGenreWeights_BeatsTheOppositeSplit()
        {
            var db = M2TestData.Database();
            var good = Develop(db, 7, true);
            var bad = Develop(db, 7, false);
            Assert.That(good.Review.Quality, Is.GreaterThan(bad.Review.Quality));
            Assert.That(good.Review.Review, Is.GreaterThan(bad.Review.Review));
        }

        [Test]
        public void SameSeed_GivesTheSameReview()
        {
            var db = M2TestData.Database();
            var a = Develop(db, 42, true);
            var b = Develop(db, 42, true);
            Assert.That(a.Review.Review, Is.EqualTo(b.Review.Review));
            Assert.That(a.Review.Noise, Is.EqualTo(b.Review.Noise));
        }

        [Test]
        public void Noise_NeverExceedsHalfAPoint()
        {
            var db = M2TestData.Database();
            var points = M2TestData.FlatPoints(20f);
            for (ulong seed = 0; seed < 2000; seed++)
            {
                var r = ReviewCalculator.Calculate(db, M2TestData.Concept(), points, 2f, 1975, new SeededRandom(seed));
                Assert.That(r.Noise, Is.InRange(-0.5f, 0.5f));
                Assert.That(r.Review - r.BaseReview, Is.InRange(-0.5f, 0.5f));
                Assert.That(r.Review, Is.InRange(1f, 10f));
            }
        }

        [Test]
        public void Noise_IsCappedEvenWhenBalanceAsksForMore()
        {
            var balance = TestData.Balance();
            balance.Review.MaxNoise = 3f;
            var db = M2TestData.Database(balance);
            var points = M2TestData.FlatPoints(20f);
            Assert.That(ReviewCalculator.Calculate(db, M2TestData.Concept(), points, 0f, 1975, new M2TestData.FixedRandom(0.0)).Noise, Is.EqualTo(-0.5f).Within(1e-5));
            Assert.That(ReviewCalculator.Calculate(db, M2TestData.Concept(), points, 0f, 1975, new M2TestData.FixedRandom(0.999999)).Noise, Is.EqualTo(0.5f).Within(1e-4));
        }

        [Test]
        public void SameGameFiveYearsLater_GetsAWorseReview()
        {
            var db = M2TestData.Database();
            var points = M2TestData.FlatPoints(20f);
            var early = ReviewCalculator.Calculate(db, M2TestData.Concept(), points, 2f, 1974, new SeededRandom(3));
            var late = ReviewCalculator.Calculate(db, M2TestData.Concept(), points, 2f, 1979, new SeededRandom(3));
            Assert.That(late.Expectation, Is.GreaterThan(early.Expectation));
            Assert.That(late.Quality, Is.EqualTo(early.Quality));
            Assert.That(late.Review, Is.LessThan(early.Review));
        }

        [Test]
        public void ComboAudienceAndBugs_ChangeQualityAsDescribed()
        {
            var db = M2TestData.Database();
            var points = M2TestData.FlatPoints(20f);
            var rng = new M2TestData.FixedRandom(0.5);
            float Quality(GameConcept c, float bugs) => ReviewCalculator.Calculate(db, c, points, bugs, 1975, rng).Quality;

            float excellent = Quality(M2TestData.Concept(theme: "space"), 0f);
            float good = Quality(M2TestData.Concept(theme: "sport"), 0f);
            float bad = Quality(M2TestData.Concept(theme: "pirates"), 0f);
            Assert.That(excellent, Is.GreaterThan(good));
            Assert.That(good, Is.GreaterThan(bad));

            Assert.That(Quality(M2TestData.Concept(audience: Audience.Adults), 0f), Is.LessThan(Quality(M2TestData.Concept(audience: Audience.Kids), 0f)));
            Assert.That(Quality(M2TestData.Concept(), 20f), Is.LessThan(Quality(M2TestData.Concept(), 0f)));
        }

        [Test]
        public void Breakdown_MultipliesToTheQuality()
        {
            var db = M2TestData.Database();
            var r = Develop(db, 11, true).Review;
            Assert.That(r.Quality, Is.EqualTo(r.AreaQuality * r.ComboFactor * r.AudienceFactor * r.BugFactor).Within(1e-4));
            Assert.That(r.QualityRatio, Is.EqualTo(r.Quality / r.Expectation).Within(1e-5));
            Assert.That(r.Review, Is.EqualTo(r.BaseReview + r.Noise).Within(1e-5));
        }
    }
}
