using System.Collections.Generic;

namespace Game.Core
{
    public readonly struct GenrePopularity
    {
        public readonly string GenreId;
        /// <summary>Sales multiplier for the genre on this platform (1 = neutral).</summary>
        public readonly float Multiplier;

        public GenrePopularity(string genreId, float multiplier)
        {
            GenreId = genreId;
            Multiplier = multiplier;
        }
    }

    public sealed class PlatformData
    {
        private readonly Dictionary<string, float> _popularityByGenre = new Dictionary<string, float>();

        public string Id { get; }
        public string NameKey { get; }
        /// <summary>First release year on the western market.</summary>
        public int ReleaseYear { get; }
        /// <summary>Last year new games can be released for the platform.</summary>
        public int EndYear { get; }
        public long LicenseFee { get; }
        public long DevKitCost { get; }
        /// <summary>Maximum technical quality the platform allows (0..1).</summary>
        public float TechCeiling { get; }
        /// <summary>Installed base in millions of units over years.</summary>
        public YearCurve InstallBaseMillions { get; }
        public IReadOnlyList<GenrePopularity> GenrePopularity { get; }

        public PlatformData(string id, string nameKey, int releaseYear, int endYear, long licenseFee, long devKitCost,
            float techCeiling, YearCurve installBaseMillions, IReadOnlyList<GenrePopularity> genrePopularity)
        {
            Id = id;
            NameKey = nameKey;
            ReleaseYear = releaseYear;
            EndYear = endYear;
            LicenseFee = licenseFee;
            DevKitCost = devKitCost;
            TechCeiling = techCeiling;
            InstallBaseMillions = installBaseMillions ?? new YearCurve(null);
            GenrePopularity = genrePopularity ?? new GenrePopularity[0];
            foreach (var entry in GenrePopularity)
            {
                if (entry.GenreId != null && !_popularityByGenre.ContainsKey(entry.GenreId))
                    _popularityByGenre.Add(entry.GenreId, entry.Multiplier);
            }
        }

        public bool IsAvailable(int year) => year >= ReleaseYear && year <= EndYear;

        /// <summary>Genre popularity multiplier; genres without an entry are neutral (1).</summary>
        public float GetGenrePopularity(string genreId)
        {
            return genreId != null && _popularityByGenre.TryGetValue(genreId, out var value) ? value : 1f;
        }
    }
}
