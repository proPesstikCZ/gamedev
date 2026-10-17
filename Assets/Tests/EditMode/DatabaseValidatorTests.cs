using System.Collections.Generic;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class DatabaseValidatorTests
    {
        private static IReadOnlyList<string> Validate(IReadOnlyList<GenreData> genres, IReadOnlyList<ThemeData> themes,
            IReadOnlyList<PlatformData> platforms, BalanceData balance = null)
        {
            return DatabaseValidator.Validate(new GameDatabaseData(genres, themes, platforms, balance ?? TestData.Balance()));
        }

        private static void AssertHasError(IReadOnlyList<string> errors, string fragment)
        {
            Assert.That(errors.Any(e => e.Contains(fragment)), Is.True,
                $"Expected an error containing '{fragment}'. Got:\n{string.Join("\n", errors)}");
        }

        [Test]
        public void ValidDatabase_HasNoErrors()
        {
            var errors = DatabaseValidator.Validate(TestData.Database());
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void DuplicateGenreId_IsReported()
        {
            var genres = new[] { TestData.Genre("arcade"), TestData.Genre("arcade") };
            var errors = Validate(genres, new[] { TestData.Theme("space", genres.Take(1)) }, new[] { TestData.Platform("p") });
            AssertHasError(errors, "id 'arcade' is duplicated");
        }

        [Test]
        public void EmptyId_IsReported()
        {
            var genres = new[] { TestData.Genre("") };
            var errors = Validate(genres, new[] { TestData.Theme("space", new GenreData[0]) }, new[] { TestData.Platform("p") });
            AssertHasError(errors, "Genre has an empty id");
        }

        [Test]
        public void WeightOutOfRange_IsReported()
        {
            var weights = TestData.Weights();
            weights.Graphics = 1.5f;
            var genres = new[] { TestData.Genre("arcade", weights: weights) };
            var errors = Validate(genres, new[] { TestData.Theme("space", genres) }, new[] { TestData.Platform("p") });
            AssertHasError(errors, "weight Graphics");
        }

        [Test]
        public void AllWeightsZero_IsReported()
        {
            var genres = new[] { TestData.Genre("arcade", weights: TestData.Weights(0f)) };
            var errors = Validate(genres, new[] { TestData.Theme("space", genres) }, new[] { TestData.Platform("p") });
            AssertHasError(errors, "all weights are zero");
        }

        [Test]
        public void ThemeMissingComboForGenre_IsReported()
        {
            var genres = new[] { TestData.Genre("arcade"), TestData.Genre("rpg") };
            var theme = TestData.Theme("space", genres.Take(1));
            var errors = Validate(genres, new[] { theme }, new[] { TestData.Platform("p") });
            AssertHasError(errors, "missing combo rating for genre 'rpg'");
        }

        [Test]
        public void ThemeComboWithUnknownGenre_IsReported()
        {
            var genres = new[] { TestData.Genre("arcade") };
            var theme = new ThemeData("space", "theme.space.name", 1972, new[]
            {
                new GenreCombo("arcade", ComboRating.Good),
                new GenreCombo("missing", ComboRating.Bad),
            });
            var errors = Validate(genres, new[] { theme }, new[] { TestData.Platform("p") });
            AssertHasError(errors, "unknown genre 'missing'");
        }

        [Test]
        public void PlatformEndBeforeRelease_IsReported()
        {
            var genres = new[] { TestData.Genre("arcade") };
            var platform = new PlatformData("p", "platform.p.name", 1980, 1975, 0, 0, 0.2f,
                new YearCurve(new[] { new YearPoint(1980, 1f) }), null);
            var errors = Validate(genres, new[] { TestData.Theme("space", genres) }, new[] { platform });
            AssertHasError(errors, "EndYear is before ReleaseYear");
        }

        [Test]
        public void PlatformWithEmptyInstallBase_IsReported()
        {
            var genres = new[] { TestData.Genre("arcade") };
            var platform = new PlatformData("p", "platform.p.name", 1972, 1980, 0, 0, 0.2f, new YearCurve(null), null);
            var errors = Validate(genres, new[] { TestData.Theme("space", genres) }, new[] { platform });
            AssertHasError(errors, "installed base curve is empty");
        }

        [Test]
        public void PlatformPopularityWithUnknownGenre_IsReported()
        {
            var genres = new[] { TestData.Genre("arcade") };
            var platform = TestData.Platform("p", popularity: new[] { new GenrePopularity("missing", 1.2f) });
            var errors = Validate(genres, new[] { TestData.Theme("space", genres) }, new[] { platform });
            AssertHasError(errors, "popularity references unknown genre 'missing'");
        }

        [Test]
        public void MissingBalance_IsReported()
        {
            var db = TestData.Database();
            var errors = DatabaseValidator.Validate(new GameDatabaseData(db.Genres, db.Themes, db.Platforms, null));
            AssertHasError(errors, "Balance config is missing");
        }
    }
}
