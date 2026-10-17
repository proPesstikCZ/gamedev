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

        public GenreData(string id, string nameKey, int firstYear, AreaWeights weights, AudienceFit audienceFit)
        {
            Id = id;
            NameKey = nameKey;
            FirstYear = firstYear;
            Weights = weights;
            AudienceFit = audienceFit;
        }

        public bool IsAvailable(int year) => year >= FirstYear;
    }
}
