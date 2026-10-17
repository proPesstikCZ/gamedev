namespace Game.Core
{
    public sealed class GenreData
    {
        public string Id { get; }
        public string NameKey { get; }
        /// <summary>First year the genre can be developed (real history).</summary>
        public int FirstYear { get; }
        /// <summary>Hidden weights of development areas (GDD 6.3).</summary>
        public AreaWeights Weights { get; }
        public AudienceFit AudienceFit { get; }
        /// <summary>Popularity multiplier over years (GDD 9). Empty = always 1.</summary>
        public YearCurve Trend { get; }

        public GenreData(string id, string nameKey, int firstYear, AreaWeights weights, AudienceFit audienceFit,
            YearCurve trend = null)
        {
            Trend = trend ?? new YearCurve(null);
            Id = id;
            NameKey = nameKey;
            FirstYear = firstYear;
            Weights = weights;
            AudienceFit = audienceFit;
        }

        public bool IsAvailable(int year) => year >= FirstYear;
    }
}
