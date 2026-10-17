using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Displayed size of a revealed genre weight: ●○○ / ●●○ / ●●● (GDD 8).</summary>
    public enum WeightLevel
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    }

    /// <summary>
    /// What the player has learned about the market (GDD 8): revealed genre weights and
    /// genre + theme combo ratings. Serializable, so it is part of the save.
    /// </summary>
    [Serializable]
    public sealed class MarketKnowledge
    {
        [SerializeField] private List<string> revealedWeights = new List<string>();
        [SerializeField] private List<string> revealedCombos = new List<string>();

        private HashSet<string> _weights;
        private HashSet<string> _combos;

        public int RevealedWeightCount => Weights.Count;
        public int RevealedComboCount => Combos.Count;

        private HashSet<string> Weights => _weights ?? (_weights = new HashSet<string>(revealedWeights ?? new List<string>(), StringComparer.Ordinal));
        private HashSet<string> Combos => _combos ?? (_combos = new HashSet<string>(revealedCombos ?? new List<string>(), StringComparer.Ordinal));

        private static string WeightKey(string genreId, DevArea area) => genreId + "/" + (int)area;
        private static string ComboKey(string genreId, string themeId) => genreId + "/" + themeId;

        public bool IsWeightRevealed(string genreId, DevArea area) => genreId != null && Weights.Contains(WeightKey(genreId, area));
        public bool IsComboRevealed(string genreId, string themeId) => genreId != null && themeId != null && Combos.Contains(ComboKey(genreId, themeId));

        /// <summary>Returns true when the weight was not known before.</summary>
        public bool RevealWeight(string genreId, DevArea area)
        {
            if (string.IsNullOrEmpty(genreId)) throw new ArgumentException("Genre id is empty.", nameof(genreId));
            string key = WeightKey(genreId, area);
            if (!Weights.Add(key)) return false;
            revealedWeights.Add(key);
            return true;
        }

        /// <summary>Returns true when the combo was not known before.</summary>
        public bool RevealCombo(string genreId, string themeId)
        {
            if (string.IsNullOrEmpty(genreId) || string.IsNullOrEmpty(themeId)) throw new ArgumentException("Genre or theme id is empty.");
            string key = ComboKey(genreId, themeId);
            if (!Combos.Add(key)) return false;
            revealedCombos.Add(key);
            return true;
        }

        public static WeightLevel LevelOf(float weight, PostMortemBalance balance)
        {
            if (weight < balance.LowWeightBelow) return WeightLevel.Low;
            if (weight >= balance.HighWeightFrom) return WeightLevel.High;
            return WeightLevel.Medium;
        }

        /// <summary>The level shown in the encyclopedia; Unknown until revealed.</summary>
        public WeightLevel GetWeightLevel(GameDatabaseData db, string genreId, DevArea area)
        {
            var genre = db.GetGenre(genreId);
            if (genre == null || !IsWeightRevealed(genreId, area)) return WeightLevel.Unknown;
            return LevelOf(genre.Weights[area], db.Balance.PostMortem);
        }

        /// <summary>The combo rating if the player knows it.</summary>
        public bool TryGetCombo(GameDatabaseData db, string genreId, string themeId, out ComboRating rating)
        {
            rating = ComboRating.Bad;
            var theme = db.GetTheme(themeId);
            return theme != null && IsComboRevealed(genreId, themeId) && theme.TryGetCombo(genreId, out rating);
        }

        /// <summary>
        /// Picks the hidden weights to reveal after a release: the most important unknown areas
        /// and the least important unknown areas of the genre. Ties go to the lower area index.
        /// </summary>
        public List<DevArea> PickWeightsToReveal(GenreData genre, PostMortemBalance balance)
        {
            var unknown = new List<DevArea>();
            for (int i = 0; i < DevAreas.Count; i++)
                if (!IsWeightRevealed(genre.Id, (DevArea)i)) unknown.Add((DevArea)i);

            var picked = new List<DevArea>();
            var byHigh = new List<DevArea>(unknown);
            byHigh.Sort((a, b) => Compare(genre.Weights[b], genre.Weights[a], a, b));
            for (int i = 0; i < byHigh.Count && i < balance.HighWeightRevealsPerGame; i++) picked.Add(byHigh[i]);

            var byLow = new List<DevArea>(unknown);
            byLow.Sort((a, b) => Compare(genre.Weights[a], genre.Weights[b], a, b));
            int lows = 0;
            foreach (var area in byLow)
            {
                if (lows >= balance.LowWeightRevealsPerGame) break;
                if (picked.Contains(area)) continue;
                picked.Add(area);
                lows++;
            }
            return picked;
        }

        private static int Compare(float x, float y, DevArea a, DevArea b)
        {
            int c = x.CompareTo(y);
            return c != 0 ? c : ((int)a).CompareTo((int)b);
        }

        internal bool IsStructurallyValid() => revealedWeights != null && revealedCombos != null;
    }
}
