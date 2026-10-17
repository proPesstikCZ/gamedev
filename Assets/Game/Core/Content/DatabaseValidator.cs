using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Checks game content for authoring mistakes. Messages are for developers, not players.</summary>
    public static class DatabaseValidator
    {
        public const float MaxPopularityMultiplier = 3f;

        private static readonly Audience[] AllAudiences = { Audience.Kids, Audience.Everyone, Audience.Adults };

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>True only for finite values within [min, max]; NaN never passes.</summary>
        private static bool InRange(float value, float min, float max) => IsFinite(value) && value >= min && value <= max;

        public static IReadOnlyList<string> Validate(GameDatabaseData db)
        {
            var errors = new List<string>();
            if (db == null)
            {
                errors.Add("Database is null.");
                return errors;
            }

            var genreIds = ValidateGenres(db.Genres, errors);
            ValidateThemes(db.Themes, genreIds, errors);
            ValidatePlatforms(db.Platforms, genreIds, errors);
            ValidateBalance(db.Balance, errors);
            return errors;
        }

        private static HashSet<string> ValidateGenres(IReadOnlyList<GenreData> genres, List<string> errors)
        {
            var ids = new HashSet<string>();
            if (genres.Count == 0) errors.Add("No genres defined.");
            foreach (var g in genres)
            {
                if (!CheckEntry("Genre", g?.Id, g?.NameKey, ids, errors, g == null)) continue;
                if (g.FirstYear <= 0) errors.Add($"Genre '{g.Id}': FirstYear must be positive.");
                for (int i = 0; i < DevAreas.Count; i++)
                {
                    float w = g.Weights[(DevArea)i];
                    if (!InRange(w, 0f, 1f)) errors.Add($"Genre '{g.Id}': weight {(DevArea)i} = {w} is outside 0..1.");
                }
                float sum = g.Weights.Sum();
                if (IsFinite(sum) && sum <= 0f) errors.Add($"Genre '{g.Id}': all weights are zero.");
                foreach (var a in AllAudiences)
                {
                    float fit = g.AudienceFit[a];
                    if (!InRange(fit, 0f, 1f)) errors.Add($"Genre '{g.Id}': audience fit {a} = {fit} is outside 0..1.");
                }
                ValidateTrend($"Genre '{g.Id}'", g.Trend, errors);
            }
            return ids;
        }

        private static void ValidateThemes(IReadOnlyList<ThemeData> themes, HashSet<string> genreIds, List<string> errors)
        {
            var ids = new HashSet<string>();
            if (themes.Count == 0) errors.Add("No themes defined.");
            foreach (var t in themes)
            {
                if (!CheckEntry("Theme", t?.Id, t?.NameKey, ids, errors, t == null)) continue;
                if (t.FirstYear <= 0) errors.Add($"Theme '{t.Id}': FirstYear must be positive.");
                ValidateTrend($"Theme '{t.Id}'", t.Trend, errors);

                var seen = new HashSet<string>();
                foreach (var combo in t.Combos)
                {
                    if (string.IsNullOrEmpty(combo.GenreId)) errors.Add($"Theme '{t.Id}': combo without genre.");
                    else if (!genreIds.Contains(combo.GenreId)) errors.Add($"Theme '{t.Id}': combo references unknown genre '{combo.GenreId}'.");
                    else if (!seen.Add(combo.GenreId)) errors.Add($"Theme '{t.Id}': duplicate combo for genre '{combo.GenreId}'.");
                }
                foreach (var genreId in genreIds)
                {
                    if (!seen.Contains(genreId)) errors.Add($"Theme '{t.Id}': missing combo rating for genre '{genreId}'.");
                }
            }
        }

        private static void ValidatePlatforms(IReadOnlyList<PlatformData> platforms, HashSet<string> genreIds, List<string> errors)
        {
            var ids = new HashSet<string>();
            if (platforms.Count == 0) errors.Add("No platforms defined.");
            foreach (var p in platforms)
            {
                if (!CheckEntry("Platform", p?.Id, p?.NameKey, ids, errors, p == null)) continue;
                if (p.ReleaseYear <= 0) errors.Add($"Platform '{p.Id}': ReleaseYear must be positive.");
                if (p.EndYear < p.ReleaseYear) errors.Add($"Platform '{p.Id}': EndYear is before ReleaseYear.");
                if (p.LicenseFee < 0) errors.Add($"Platform '{p.Id}': LicenseFee is negative.");
                if (p.DevKitCost < 0) errors.Add($"Platform '{p.Id}': DevKitCost is negative.");
                if (!InRange(p.TechCeiling, 0f, 1f) || p.TechCeiling <= 0f) errors.Add($"Platform '{p.Id}': TechCeiling must be in (0, 1].");

                var points = p.InstallBaseMillions.Points;
                if (points.Count == 0) errors.Add($"Platform '{p.Id}': installed base curve is empty.");
                for (int i = 0; i < points.Count; i++)
                {
                    if (!IsFinite(points[i].Value) || points[i].Value < 0f)
                        errors.Add($"Platform '{p.Id}': installed base in {points[i].Year} = {points[i].Value} must be a finite, non-negative number.");
                    if (i > 0 && points[i].Year == points[i - 1].Year) errors.Add($"Platform '{p.Id}': installed base has two points for {points[i].Year}.");
                }
                if (points.Count > 0 && points[0].Year < p.ReleaseYear)
                    errors.Add($"Platform '{p.Id}': installed base starts before ReleaseYear.");

                var seen = new HashSet<string>();
                foreach (var pop in p.GenrePopularity)
                {
                    if (string.IsNullOrEmpty(pop.GenreId) || !genreIds.Contains(pop.GenreId))
                        errors.Add($"Platform '{p.Id}': popularity references unknown genre '{pop.GenreId}'.");
                    else if (!seen.Add(pop.GenreId))
                        errors.Add($"Platform '{p.Id}': duplicate popularity for genre '{pop.GenreId}'.");
                    if (!InRange(pop.Multiplier, 0f, MaxPopularityMultiplier))
                        errors.Add($"Platform '{p.Id}': popularity of '{pop.GenreId}' = {pop.Multiplier} is outside 0..{MaxPopularityMultiplier}.");
                }
            }
        }

        private static void ValidateBalance(BalanceData b, List<string> errors)
        {
            if (b == null)
            {
                errors.Add("Balance config is missing.");
                return;
            }
            if (b.StartYear <= 0) errors.Add("Balance: StartYear must be positive.");
            if (b.StartMoney < 0) errors.Add("Balance: StartMoney is negative.");
            if (b.MonthlyGarageCost < 0) errors.Add("Balance: MonthlyGarageCost is negative.");
            if (b.MonthsInDebtBeforeBankruptcy < 0) errors.Add("Balance: MonthsInDebtBeforeBankruptcy is negative.");

            var d = b.Development;
            if (d.PreProductionWeeks < 1 || d.ProductionWeeks < 1 || d.FinishingWeeks < 1) errors.Add("Balance: every work phase needs at least 1 week.");
            if (d.MaxQaWeeks < 0) errors.Add("Balance: MaxQaWeeks is negative.");
            int minWeeks = d.PreProductionWeeks + d.ProductionWeeks + d.FinishingWeeks;
            if (minWeeks < SmallGameMinWeeks || minWeeks + d.MaxQaWeeks > SmallGameMaxWeeks)
                errors.Add($"Balance: a small game must take {SmallGameMinWeeks}..{SmallGameMaxWeeks} weeks (GDD 3), got {minWeeks}..{minWeeks + d.MaxQaWeeks}.");
            Positive("Development.PointsPerWeek", d.PointsPerWeek, errors);
            Positive("Development.StartSkill", d.StartSkill, errors);
            Positive("Development.MaxSkill", d.MaxSkill, errors);
            Positive("Development.CustomEngineTech", d.CustomEngineTech, errors);
            Positive("Development.LicensedEngineTech", d.LicensedEngineTech, errors);
            NonNegative("Development.SkillGainPerGame", d.SkillGainPerGame, errors);
            NonNegative("Development.PlatformTechWeight", d.PlatformTechWeight, errors);
            NonNegative("Development.BugsPerWeek", d.BugsPerWeek, errors);
            if (!InRange(d.QaBugReductionPerWeek, 0f, 1f)) errors.Add("Balance: Development.QaBugReductionPerWeek must be 0..1.");
            if (d.LicensedEngineCost < 0) errors.Add("Balance: Development.LicensedEngineCost is negative.");

            var r = b.Review;
            if (!InRange(r.AreaExponent, 0.01f, 1f)) errors.Add("Balance: Review.AreaExponent must be in (0, 1].");
            NonNegative("Review.ExcellentComboFactor", r.ExcellentComboFactor, errors);
            NonNegative("Review.GoodComboFactor", r.GoodComboFactor, errors);
            NonNegative("Review.BadComboFactor", r.BadComboFactor, errors);
            if (!(r.BadComboFactor <= r.GoodComboFactor && r.GoodComboFactor <= r.ExcellentComboFactor))
                errors.Add("Balance: combo factors must grow from Bad to Excellent.");
            if (!InRange(r.MinAudienceFactor, 0f, 1f)) errors.Add("Balance: Review.MinAudienceFactor must be 0..1.");
            NonNegative("Review.BugPenaltyPerBug", r.BugPenaltyPerBug, errors);
            Positive("Review.ExpectationAtStart", r.ExpectationAtStart, errors);
            if (!IsFinite(r.ExpectationGrowthPerYear) || r.ExpectationGrowthPerYear <= -1f) errors.Add("Balance: Review.ExpectationGrowthPerYear must be above -1.");
            Positive("Review.CurveSteepness", r.CurveSteepness, errors);
            if (!InRange(r.MaxNoise, 0f, ReviewCalculator.NoiseLimit)) errors.Add($"Balance: Review.MaxNoise must be 0..{ReviewCalculator.NoiseLimit} (GDD 6.4).");

            var s = b.Sales;
            NonNegative("Sales.BaseWeeklyPurchaseRate", s.BaseWeeklyPurchaseRate, errors);
            NonNegative("Sales.ReviewExponent", s.ReviewExponent, errors);
            if (!InRange(s.WeeklyDecay, 0f, 0.999f)) errors.Add("Balance: Sales.WeeklyDecay must be 0..0.999 so sales end.");
            if (s.MinWeeklyUnits < 1) errors.Add("Balance: Sales.MinWeeklyUnits must be at least 1.");
            if (s.MaxSalesWeeks < 1) errors.Add("Balance: Sales.MaxSalesWeeks must be at least 1.");
            Positive("Sales.Price", s.Price, errors);
            Positive("Sales.ReferencePrice", s.ReferencePrice, errors);
            NonNegative("Sales.PriceElasticity", s.PriceElasticity, errors);
            if (!InRange(s.RevenueShare, 0f, 1f)) errors.Add("Balance: Sales.RevenueShare must be 0..1.");
            NonNegative("Sales.Hype", s.Hype, errors);
            NonNegative("Sales.Marketing", s.Marketing, errors);
            Positive("Sales.FansForDoubleSales", s.FansForDoubleSales, errors);
            NonNegative("Sales.FansPerUnit", s.FansPerUnit, errors);
            if (!InRange(s.FanReviewThreshold, 1f, 9.99f)) errors.Add("Balance: Sales.FanReviewThreshold must be 1..9.99.");
            if (!InRange(s.FlopReviewThreshold, 1f, 10f)) errors.Add("Balance: Sales.FlopReviewThreshold must be 1..10.");
            if (!InRange(s.FanLossOnFlop, 0f, 1f)) errors.Add("Balance: Sales.FanLossOnFlop must be 0..1.");

            var t = b.Trends;
            if (!InRange(t.NoiseAmplitude, 0f, 0.5f)) errors.Add("Balance: Trends.NoiseAmplitude must be 0..0.5.");
            if (t.AnnounceYearsAhead < 1) errors.Add("Balance: Trends.AnnounceYearsAhead must be at least 1.");
            Positive("Trends.AnnounceThreshold", t.AnnounceThreshold, errors);
            Positive("Trends.MinTrend", t.MinTrend, errors);

            var p = b.PostMortem;
            if (p.HighWeightRevealsPerGame < 0 || p.LowWeightRevealsPerGame < 0) errors.Add("Balance: weight reveals per game must not be negative.");
            if (p.HighWeightRevealsPerGame + p.LowWeightRevealsPerGame < 1) errors.Add("Balance: at least one weight must be revealed per game.");
            if (!(InRange(p.LowWeightBelow, 0f, 1f) && InRange(p.HighWeightFrom, 0f, 1f) && p.LowWeightBelow < p.HighWeightFrom))
                errors.Add("Balance: PostMortem weight thresholds must be 0..1 with LowWeightBelow < HighWeightFrom.");
            if (!(p.BelowExpectationRatio < p.AboveExpectationRatio)) errors.Add("Balance: BelowExpectationRatio must be below AboveExpectationRatio.");
        }

        public const int SmallGameMinWeeks = 8;
        public const int SmallGameMaxWeeks = 16;

        private static void Positive(string name, float value, List<string> errors)
        {
            if (!IsFinite(value) || value <= 0f) errors.Add($"Balance: {name} must be a positive number.");
        }

        private static void NonNegative(string name, float value, List<string> errors)
        {
            if (!IsFinite(value) || value < 0f) errors.Add($"Balance: {name} must not be negative.");
        }

        private static void ValidateTrend(string owner, YearCurve trend, List<string> errors)
        {
            var points = trend.Points;
            for (int i = 0; i < points.Count; i++)
            {
                if (!InRange(points[i].Value, 0f, MaxPopularityMultiplier))
                    errors.Add($"{owner}: trend in {points[i].Year} = {points[i].Value} is outside 0..{MaxPopularityMultiplier}.");
                if (i > 0 && points[i].Year == points[i - 1].Year) errors.Add($"{owner}: trend has two points for {points[i].Year}.");
            }
        }

        private static bool CheckEntry(string kind, string id, string nameKey, HashSet<string> ids, List<string> errors, bool isNull)
        {
            if (isNull)
            {
                errors.Add($"{kind} entry is null.");
                return false;
            }
            if (string.IsNullOrEmpty(id))
            {
                errors.Add($"{kind} has an empty id.");
                return false;
            }
            if (!ids.Add(id)) errors.Add($"{kind} id '{id}' is duplicated.");
            if (string.IsNullOrEmpty(nameKey)) errors.Add($"{kind} '{id}': NameKey is empty.");
            return true;
        }
    }
}
