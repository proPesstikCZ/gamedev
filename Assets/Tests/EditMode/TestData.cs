using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.Tests
{
    /// <summary>Builds small, valid in-memory databases for tests.</summary>
    internal static class TestData
    {
        public static AreaWeights Weights(float value = 0.5f)
        {
            var w = new AreaWeights();
            for (int i = 0; i < DevAreas.Count; i++) w[(DevArea)i] = value;
            return w;
        }

        public static GenreData Genre(string id, int firstYear = 1972, AreaWeights? weights = null)
        {
            return new GenreData(id, $"genre.{id}.name", firstYear, weights ?? Weights(), new AudienceFit(1f, 1f, 1f));
        }

        public static ThemeData Theme(string id, IEnumerable<GenreData> genres, int firstYear = 1972)
        {
            var combos = genres.Select(g => new GenreCombo(g.Id, ComboRating.Good)).ToList();
            return new ThemeData(id, $"theme.{id}.name", firstYear, combos);
        }

        public static PlatformData Platform(string id, int releaseYear = 1972, int endYear = 1980,
            IReadOnlyList<GenrePopularity> popularity = null)
        {
            var curve = new YearCurve(new[] { new YearPoint(releaseYear, 0.1f), new YearPoint(endYear, 1f) });
            return new PlatformData(id, $"platform.{id}.name", releaseYear, endYear, 1000, 500, 0.2f, curve, popularity);
        }

        public static BalanceData Balance(long startMoney = 10000, long monthlyCost = 300, int monthsInDebt = 3)
        {
            return new BalanceData(1972, startMoney, monthlyCost, monthsInDebt);
        }

        public static GameDatabaseData Database(BalanceData balance = null)
        {
            var genres = new List<GenreData> { Genre("arcade"), Genre("rpg", 1979) };
            var themes = new List<ThemeData> { Theme("space", genres) };
            var platforms = new List<PlatformData> { Platform("console"), Platform("computer", 1977, 1993) };
            return new GameDatabaseData(genres, themes, platforms, balance ?? Balance());
        }
    }
}
