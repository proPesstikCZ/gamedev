using System.Collections.Generic;

namespace Game.Core
{
    public readonly struct GenreCombo
    {
        public readonly string GenreId;
        public readonly ComboRating Rating;

        public GenreCombo(string genreId, ComboRating rating)
        {
            GenreId = genreId;
            Rating = rating;
        }
    }

    public sealed class ThemeData
    {
        private readonly Dictionary<string, ComboRating> _comboByGenre = new Dictionary<string, ComboRating>();

        public string Id { get; }
        public string NameKey { get; }
        public int FirstYear { get; }
        public IReadOnlyList<GenreCombo> Combos { get; }

        public ThemeData(string id, string nameKey, int firstYear, IReadOnlyList<GenreCombo> combos)
        {
            Id = id;
            NameKey = nameKey;
            FirstYear = firstYear;
            Combos = combos ?? new GenreCombo[0];
            foreach (var combo in Combos)
            {
                if (combo.GenreId != null && !_comboByGenre.ContainsKey(combo.GenreId))
                    _comboByGenre.Add(combo.GenreId, combo.Rating);
            }
        }

        public bool IsAvailable(int year) => year >= FirstYear;

        public bool TryGetCombo(string genreId, out ComboRating rating)
        {
            if (genreId == null)
            {
                rating = ComboRating.Bad;
                return false;
            }
            return _comboByGenre.TryGetValue(genreId, out rating);
        }
    }
}
