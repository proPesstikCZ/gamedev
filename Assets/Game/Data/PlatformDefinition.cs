using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Platform", fileName = "plat_")]
    public sealed class PlatformDefinition : ScriptableObject
    {
        [Serializable]
        public struct PopularityEntry
        {
            public GenreDefinition Genre;
            [Tooltip("Sales multiplier, 1 = neutral.")]
            public float Multiplier;

            public PopularityEntry(GenreDefinition genre, float multiplier)
            {
                Genre = genre;
                Multiplier = multiplier;
            }
        }

        [SerializeField] private string id;
        [Tooltip("Localization key, e.g. platform.astari_vcs.name")]
        [SerializeField] private string nameKey;
        [Tooltip("First release year on the western market.")]
        [SerializeField] private int releaseYear = 1972;
        [Tooltip("Last year new games can be released.")]
        [SerializeField] private int endYear = 1980;
        [SerializeField] private long licenseFee;
        [SerializeField] private long devKitCost;
        [Tooltip("Maximum technical quality the platform allows, 0..1.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float techCeiling = 0.1f;
        [Tooltip("Installed base in millions of units; linear between points.")]
        [SerializeField] private List<YearPoint> installBaseMillions = new List<YearPoint>();
        [Tooltip("Genres missing here are neutral (1).")]
        [SerializeField] private List<PopularityEntry> genrePopularity = new List<PopularityEntry>();

        public string Id => id;

        public PlatformData ToCore()
        {
            var popularity = new List<GenrePopularity>(genrePopularity.Count);
            foreach (var p in genrePopularity) popularity.Add(new GenrePopularity(p.Genre != null ? p.Genre.Id : null, p.Multiplier));
            return new PlatformData(id, nameKey, releaseYear, endYear, licenseFee, devKitCost, techCeiling,
                new YearCurve(installBaseMillions), popularity);
        }
    }
}
