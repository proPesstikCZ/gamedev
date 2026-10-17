using System.Linq;
using Game.Core;
using Game.Data;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>Checks the real content assets used by the game.</summary>
    public class ContentAssetTests
    {
        public const string DatabasePath = "Assets/Game/Content/GameDatabase.asset";
        private const int SliceStartYear = 1972;
        private const int SliceEndYear = 1985;

        private GameDatabaseData _db;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            Assert.That(asset, Is.Not.Null, $"Missing {DatabasePath}");
            _db = asset.ToCore();
        }

        [Test]
        public void RealDatabase_PassesValidation()
        {
            var errors = DatabaseValidator.Validate(_db);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void RealDatabase_HasFullGenreCatalogAndSliceContent()
        {
            Assert.That(_db.Genres.Count, Is.EqualTo(20));
            Assert.That(_db.Themes.Count, Is.EqualTo(6));
            Assert.That(_db.Platforms.Count, Is.EqualTo(4));
        }

        [Test]
        public void In1972_OnlyContentReleasedBy1972IsAvailable()
        {
            var genres = _db.AvailableGenres(1972).ToList();
            Assert.That(genres.All(g => g.FirstYear <= 1972), Is.True);
            CollectionAssert.AreEquivalent(new[] { "arcade", "action", "puzzle", "sport" }, genres.Select(g => g.Id));

            var platforms = _db.AvailablePlatforms(1972).ToList();
            Assert.That(platforms.All(p => p.ReleaseYear <= 1972), Is.True);
            CollectionAssert.AreEquivalent(new[] { "magnafox_odyssey" }, platforms.Select(p => p.Id));
        }

        [Test]
        public void In1985_SliceGenresAndPlatformsAreAvailable()
        {
            var genres = _db.AvailableGenres(1985).Select(g => g.Id).ToList();
            Assert.That(genres.Count, Is.EqualTo(13));
            CollectionAssert.Contains(genres, "rpg");
            CollectionAssert.Contains(genres, "fighting");
            CollectionAssert.DoesNotContain(genres, "fps");

            var platforms = _db.AvailablePlatforms(1985).Select(p => p.Id);
            CollectionAssert.AreEquivalent(new[] { "astari_vcs", "pear_ii", "komodor_64" }, platforms);
        }

        [Test]
        public void EverySliceYear_HasAtLeastOnePlatformAndGenre()
        {
            for (int year = SliceStartYear; year <= SliceEndYear; year++)
            {
                Assert.That(_db.AvailablePlatforms(year).Any(), Is.True, $"No platform available in {year}");
                Assert.That(_db.AvailableGenres(year).Any(), Is.True, $"No genre available in {year}");
            }
        }

        [Test]
        public void InstallBase_IsPositiveWhileThePlatformIsSold()
        {
            foreach (var p in _db.Platforms)
            {
                for (int year = p.ReleaseYear; year <= p.EndYear; year++)
                    Assert.That(p.InstallBaseMillions.Evaluate(year), Is.GreaterThan(0f), $"{p.Id} in {year}");
            }
        }
    }
}
