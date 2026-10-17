using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>All factors of one week of sales (GDD 6.4), kept so they can be explained.</summary>
    [Serializable]
    public sealed class SalesFactors
    {
        public float InstallBase;
        public float ReviewFactor;
        public float GenreTrend;
        public float ThemeTrend;
        public float PlatformPopularity;
        public float Hype;
        public float Marketing;
        public float FanFactor;
        public float PriceFactor;
        /// <summary>WeeklyDecay ^ weeks on sale.</summary>
        public float Decay;
        /// <summary>Product of all factors with the base purchase rate, before rounding.</summary>
        public float ExpectedUnits;
    }

    public static class SalesCalculator
    {
        /// <summary>
        /// Units sold in one week: base rate x installed base x review x genre trend x theme trend
        /// x platform popularity x hype x marketing x fans x price x decay,
        /// capped at MaxWeeklyShareOfBase of the installed base.
        /// </summary>
        public static SalesFactors WeeklyFactors(GameDatabaseData db, MarketTrends trends, GameConcept concept,
            float review, GameDate date, int fans, int weeksOnSale)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (trends == null) throw new ArgumentNullException(nameof(trends));
            if (concept == null) throw new ArgumentNullException(nameof(concept));
            var balance = db.Balance.Sales;
            var platform = db.GetPlatform(concept.PlatformId);

            float year = date.Year + (date.Week - 1) / (float)GameDate.WeeksPerYear;
            var f = new SalesFactors
            {
                InstallBase = platform != null ? Math.Max(0f, platform.InstallBaseMillions.Evaluate(year)) * 1_000_000f : 0f,
                ReviewFactor = Mathf.Pow(Mathf.Clamp(review, ReviewCalculator.MinReview, ReviewCalculator.MaxReview) / ReviewCalculator.MaxReview, balance.ReviewExponent),
                GenreTrend = trends.GenreTrend(concept.GenreId, date.Year),
                ThemeTrend = trends.ThemeTrend(concept.ThemeId, date.Year),
                PlatformPopularity = platform != null ? platform.GetGenrePopularity(concept.GenreId) : 1f,
                Hype = balance.Hype,
                Marketing = balance.Marketing,
                FanFactor = FanFactor(balance, fans),
                PriceFactor = balance.Price > 0f ? Mathf.Pow(balance.ReferencePrice / balance.Price, balance.PriceElasticity) : 0f,
                Decay = Mathf.Pow(Mathf.Clamp01(balance.WeeklyDecay), Math.Max(0, weeksOnSale)),
            };
            f.ExpectedUnits = balance.BaseWeeklyPurchaseRate * f.InstallBase * f.ReviewFactor * f.GenreTrend * f.ThemeTrend
                              * f.PlatformPopularity * f.Hype * f.Marketing * f.FanFactor * f.PriceFactor * f.Decay;
            f.ExpectedUnits = Math.Min(f.ExpectedUnits, f.InstallBase * Mathf.Clamp01(balance.MaxWeeklyShareOfBase));
            return f;
        }

        public static float FanFactor(SalesBalance balance, int fans)
        {
            float f = Math.Max(0, fans);
            return 1f + balance.MaxFanBonus * f / (f + Math.Max(1f, balance.FansForHalfBonus));
        }

        public static long Revenue(SalesBalance balance, long units)
        {
            return (long)Math.Round(units * (double)balance.Price * balance.RevenueShare);
        }

        /// <summary>New fans from one week of sales. Only games reviewed above the threshold gain fans.</summary>
        public static int FansGained(SalesBalance balance, float review, long units)
        {
            float span = ReviewCalculator.MaxReview - balance.FanReviewThreshold;
            if (units <= 0 || review < balance.FanReviewThreshold || span <= 0f) return 0;
            float strength = (review - balance.FanReviewThreshold) / span;
            return (int)Math.Round(units * balance.FansPerUnit * strength);
        }
    }
}
