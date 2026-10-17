using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Checks game content for authoring mistakes. Messages are for developers, not players.</summary>
    public static class DatabaseValidator
    {
        public const float MaxPopularityMultiplier = 3f;

        private static readonly Audience[] AllAudiences = { Audience.Kids, Audience.Everyone, Audience.Adults };

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
                    if (w < 0f || w > 1f) errors.Add($"Genre '{g.Id}': weight {(DevArea)i} = {w} is outside 0..1.");
                }
                if (g.Weights.Sum() <= 0f) errors.Add($"Genre '{g.Id}': all weights are zero.");
                foreach (var a in AllAudiences)
                {
                    float fit = g.AudienceFit[a];
                    if (fit < 0f || fit > 1f) errors.Add($"Genre '{g.Id}': audience fit {a} = {fit} is outside 0..1.");
                }
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
                if (p.TechCeiling <= 0f || p.TechCeiling > 1f) errors.Add($"Platform '{p.Id}': TechCeiling must be in (0, 1].");

                var points = p.InstallBaseMillions.Points;
                if (points.Count == 0) errors.Add($"Platform '{p.Id}': installed base curve is empty.");
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i].Value < 0f) errors.Add($"Platform '{p.Id}': installed base in {points[i].Year} is negative.");
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
                    if (pop.Multiplier < 0f || pop.Multiplier > MaxPopularityMultiplier)
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
