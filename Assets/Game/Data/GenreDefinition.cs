using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Genre", fileName = "genre_")]
    public sealed class GenreDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [Tooltip("Localization key, e.g. genre.arcade.name")]
        [SerializeField] private string nameKey;
        [Tooltip("First year the genre exists (real history).")]
        [SerializeField] private int firstYear = 1972;
        [Tooltip("Hidden importance of each development area, 0..1 (GDD 6.3).")]
        [SerializeField] private AreaWeights weights;
        [Tooltip("How well the genre fits each audience, 0..1.")]
        [SerializeField] private AudienceFit audienceFit = new AudienceFit(1f, 1f, 1f);
        [Tooltip("Popularity multiplier over years, 1 = neutral (GDD 9). Empty = always 1.")]
        [SerializeField] private List<YearPoint> trend = new List<YearPoint>();

        public string Id => id;

        public GenreData ToCore() => new GenreData(id, nameKey, firstYear, weights, audienceFit, new YearCurve(trend));
    }
}
