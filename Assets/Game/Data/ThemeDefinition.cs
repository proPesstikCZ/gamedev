using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Theme", fileName = "theme_")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        [Serializable]
        public struct ComboEntry
        {
            public GenreDefinition Genre;
            public ComboRating Rating;

            public ComboEntry(GenreDefinition genre, ComboRating rating)
            {
                Genre = genre;
                Rating = rating;
            }
        }

        [SerializeField] private string id;
        [Tooltip("Localization key, e.g. theme.space.name")]
        [SerializeField] private string nameKey;
        [SerializeField] private int firstYear = 1972;
        [Tooltip("How well this theme matches each genre. Every genre needs exactly one entry.")]
        [SerializeField] private List<ComboEntry> combos = new List<ComboEntry>();
        [Tooltip("Popularity multiplier over years, 1 = neutral (GDD 9). Empty = always 1.")]
        [SerializeField] private List<YearPoint> trend = new List<YearPoint>();

        public string Id => id;

        public ThemeData ToCore()
        {
            var list = new List<GenreCombo>(combos.Count);
            foreach (var c in combos) list.Add(new GenreCombo(c.Genre != null ? c.Genre.Id : null, c.Rating));
            return new ThemeData(id, nameKey, firstYear, list, new YearCurve(trend));
        }
    }
}
