using System.Collections.Generic;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class TrendTests
    {
        private static GameDatabaseData DatabaseWithTrends(float noise = 0.1f)
        {
            var balance = TestData.Balance(monthlyCost: 0); // no income here, so no rent either
            balance.Trends.NoiseAmplitude = noise;
            var rising = new YearCurve(new[] { new YearPoint(1972, 1f), new YearPoint(1975, 1f), new YearPoint(1976, 2f) });
            var genres = new List<GenreData>
            {
                new GenreData("arcade", "genre.arcade.name", 1972, TestData.Weights(), new AudienceFit(1f, 1f, 1f), rising),
                TestData.Genre("puzzle"),
            };
            var themes = new List<ThemeData>
            {
                new ThemeData("space", "theme.space.name", 1972, genres.Select(g => new GenreCombo(g.Id, ComboRating.Good)).ToList(),
                    new YearCurve(new[] { new YearPoint(1972, 1.5f), new YearPoint(1980, 0.5f) })),
            };
            return new GameDatabaseData(genres, themes, new List<PlatformData> { TestData.Platform("console") }, balance);
        }

        [Test]
        public void Trend_FollowsTheCurve_WithinTheNoise()
        {
            var trends = new MarketTrends(DatabaseWithTrends(0.1f), 99);
            for (int year = 1972; year <= 1990; year++)
            {
                float expected = year <= 1975 ? 1f : 2f;
                Assert.That(trends.GenreTrend("arcade", year), Is.InRange(expected * 0.9f - 1e-4f, expected * 1.1f + 1e-4f), year.ToString());
            }
            Assert.That(trends.ThemeTrend("space", 1976), Is.InRange(1.0f * 0.9f, 1.0f * 1.1f));
        }

        [Test]
        public void GenreWithoutCurve_StaysNeutral()
        {
            var trends = new MarketTrends(DatabaseWithTrends(0f), 1);
            Assert.That(trends.GenreTrend("puzzle", 1980), Is.EqualTo(1f));
        }

        [Test]
        public void SameSeed_GivesTheSameTrends_DifferentSeedDiffers()
        {
            var db = DatabaseWithTrends();
            var a = new MarketTrends(db, 5);
            var b = new MarketTrends(db, 5);
            var c = new MarketTrends(db, 6);
            bool anyDifferent = false;
            for (int year = 1972; year < 1990; year++)
            {
                Assert.That(a.GenreTrend("puzzle", year), Is.EqualTo(b.GenreTrend("puzzle", year)));
                anyDifferent |= a.GenreTrend("puzzle", year) != c.GenreTrend("puzzle", year);
            }
            Assert.That(anyDifferent, Is.True);
        }

        [Test]
        public void RisingTrend_IsAnnouncedAYearAhead()
        {
            var trends = new MarketTrends(DatabaseWithTrends(0f), 1);
            var news = trends.NewsForYear(1975);
            Assert.That(news.Any(n => n.Subject == TrendSubject.Genre && n.Id == "arcade" && n.Direction == TrendDirection.Rising && n.Year == 1976), Is.True);
            Assert.That(trends.NewsForYear(1972).Any(n => n.Id == "arcade"), Is.False);
            Assert.That(news.First(n => n.Id == "arcade").Key, Is.EqualTo("news.trend.rising"));
        }

        [Test]
        public void Simulation_AnnouncesNewsAtNewYear_AndTrendChangesSales()
        {
            var sim = GameSimulation.NewGame(DatabaseWithTrends(0f), 1);
            var announced = new List<TrendNews>();
            sim.TrendNewsAnnounced += n => announced.AddRange(n);
            for (int i = 0; i < 52 * 4; i++) sim.AdvanceWeek();
            Assert.That(sim.Date.Year, Is.EqualTo(1976));
            Assert.That(announced.Any(n => n.Id == "arcade" && n.Year == 1976), Is.True);

            var concept = new GameConcept("x", "arcade", "space", "console", Audience.Everyone);
            var before = SalesCalculator.WeeklyFactors(sim.Database, sim.Trends, concept, 7f, new GameDate(1975, 30), 0, 0);
            var after = SalesCalculator.WeeklyFactors(sim.Database, sim.Trends, concept, 7f, new GameDate(1976, 30), 0, 0);
            Assert.That(after.GenreTrend, Is.EqualTo(2f * before.GenreTrend).Within(1e-4));
        }
    }
}
