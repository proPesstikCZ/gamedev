using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Every factor of a review (GDD 6.4), saved so the post-mortem can explain the result.
    /// </summary>
    [Serializable]
    public sealed class ReviewBreakdown
    {
        public int Year;
        /// <summary>Points earned per area (time x skill x technology).</summary>
        public float[] AreaPoints = new float[DevAreas.Count];
        /// <summary>Area score = points ^ AreaExponent.</summary>
        public float[] AreaScores = new float[DevAreas.Count];
        /// <summary>Hidden genre weights at the time of release.</summary>
        public float[] Weights = new float[DevAreas.Count];
        /// <summary>Σ(area score x weight) / Σ weight.</summary>
        public float AreaQuality;
        public ComboRating Combo;
        public float ComboFactor;
        public float AudienceFit;
        public float AudienceFactor;
        public float Bugs;
        public float BugFactor;
        /// <summary>AreaQuality x ComboFactor x AudienceFactor x BugFactor.</summary>
        public float Quality;
        /// <summary>Quality the market expects in the release year.</summary>
        public float Expectation;
        /// <summary>Quality / Expectation.</summary>
        public float QualityRatio;
        /// <summary>Review before noise.</summary>
        public float BaseReview;
        /// <summary>Random part, never more than ±MaxNoise (at most ±0.5).</summary>
        public float Noise;
        /// <summary>Final review 1..10.</summary>
        public float Review;

        /// <summary>How much an area added to AreaQuality.</summary>
        public float Contribution(DevArea area)
        {
            float weightSum = 0f;
            foreach (float w in Weights) weightSum += w;
            return weightSum > 0f ? AreaScores[(int)area] * Weights[(int)area] / weightSum : 0f;
        }
    }

    public static class ReviewCalculator
    {
        public const float MinReview = 1f;
        public const float MaxReview = 10f;
        /// <summary>Hard limit from GDD 6.4, whatever the balance says.</summary>
        public const float NoiseLimit = 0.5f;

        public static float Expectation(ReviewBalance balance, int startYear, int year)
        {
            return balance.ExpectationAtStart * Mathf.Pow(1f + balance.ExpectationGrowthPerYear, year - startYear);
        }

        public static float ComboFactor(ReviewBalance balance, ComboRating rating)
        {
            switch (rating)
            {
                case ComboRating.Excellent: return balance.ExcellentComboFactor;
                case ComboRating.Good: return balance.GoodComboFactor;
                default: return balance.BadComboFactor;
            }
        }

        /// <summary>Review without noise for a quality / expectation ratio.</summary>
        public static float ReviewForRatio(ReviewBalance balance, float ratio)
        {
            float r = MinReview + (MaxReview - MinReview) * (1f - Mathf.Exp(-balance.CurveSteepness * Math.Max(0f, ratio)));
            return Mathf.Clamp(r, MinReview, MaxReview);
        }

        /// <summary>
        /// Quality and review of a finished game. Uses the RNG exactly once (for the noise).
        /// </summary>
        public static ReviewBreakdown Calculate(GameDatabaseData db, GameConcept concept, float[] areaPoints, float bugs,
            int year, IRandom random)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (concept == null) throw new ArgumentNullException(nameof(concept));
            if (areaPoints == null || areaPoints.Length != DevAreas.Count) throw new ArgumentException("Need one value per area.", nameof(areaPoints));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var genre = db.GetGenre(concept.GenreId) ?? throw new ArgumentException($"Unknown genre '{concept.GenreId}'.", nameof(concept));
            var theme = db.GetTheme(concept.ThemeId) ?? throw new ArgumentException($"Unknown theme '{concept.ThemeId}'.", nameof(concept));
            var balance = db.Balance.Review;

            var b = new ReviewBreakdown { Year = year, Bugs = Math.Max(0f, bugs) };
            float weighted = 0f, weightSum = 0f;
            for (int i = 0; i < DevAreas.Count; i++)
            {
                float w = genre.Weights[(DevArea)i];
                float points = Math.Max(0f, areaPoints[i]);
                float score = Mathf.Pow(points, balance.AreaExponent);
                b.AreaPoints[i] = points;
                b.AreaScores[i] = score;
                b.Weights[i] = w;
                weighted += score * w;
                weightSum += w;
            }
            b.AreaQuality = weightSum > 0f ? weighted / weightSum : 0f;

            b.Combo = theme.TryGetCombo(genre.Id, out var combo) ? combo : ComboRating.Bad;
            b.ComboFactor = ComboFactor(balance, b.Combo);
            b.AudienceFit = Mathf.Clamp01(genre.AudienceFit[concept.Audience]);
            b.AudienceFactor = Mathf.Lerp(balance.MinAudienceFactor, 1f, b.AudienceFit);
            b.BugFactor = 1f / (1f + Math.Max(0f, balance.BugPenaltyPerBug) * b.Bugs);

            b.Quality = b.AreaQuality * b.ComboFactor * b.AudienceFactor * b.BugFactor;
            b.Expectation = Expectation(balance, db.Balance.StartYear, year);
            b.QualityRatio = b.Expectation > 0f ? b.Quality / b.Expectation : 0f;
            b.BaseReview = ReviewForRatio(balance, b.QualityRatio);

            float maxNoise = Mathf.Clamp(balance.MaxNoise, 0f, NoiseLimit);
            float noise = random.Range(-maxNoise, maxNoise);
            b.Review = Mathf.Clamp(b.BaseReview + noise, MinReview, MaxReview);
            b.Noise = b.Review - b.BaseReview;
            return b;
        }
    }
}
