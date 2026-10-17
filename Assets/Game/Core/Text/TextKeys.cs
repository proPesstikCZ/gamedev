using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Localization keys the game logic and content need, and the string table each key lives in.
    /// UI and tests share this, so a missing translation is caught by a test, not by a player.
    /// </summary>
    public static class TextKeys
    {
        public const string UiTable = "UI";
        public const string ContentTable = "Content";
        public const string PostMortemTable = "PostMortem";

        public static string Area(DevArea area) => "area." + PostMortemKeys.AreaId(area) + ".name";
        public static string Audience(Audience audience) => "audience." + PostMortemKeys.AudienceId(audience) + ".name";
        public static string Engine(EngineChoice engine) => "engine." + (engine == EngineChoice.Licensed ? "licensed" : "custom") + ".name";
        public static string Phase(DevPhase phase) => "phase." + PhaseId(phase) + ".name";
        public static string Combo(ComboRating rating) => "combo." + rating.ToString().ToLowerInvariant() + ".name";
        public static string ConceptError(ConceptError error) => "concept.error." + ToSnakeCase(error.ToString());

        /// <summary>Content names come from data (genre, theme, platform); the rest are fixed by code.</summary>
        public static string TableFor(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key is empty.", nameof(key));
            if (key.StartsWith("postmortem.", StringComparison.Ordinal)) return PostMortemTable;
            if (key.StartsWith("genre.", StringComparison.Ordinal) || key.StartsWith("theme.", StringComparison.Ordinal)
                || key.StartsWith("platform.", StringComparison.Ordinal) || key.StartsWith("area.", StringComparison.Ordinal)
                || key.StartsWith("audience.", StringComparison.Ordinal) || key.StartsWith("engine.", StringComparison.Ordinal)
                || key.StartsWith("phase.", StringComparison.Ordinal) || key.StartsWith("combo.", StringComparison.Ordinal))
                return ContentTable;
            return UiTable;
        }

        /// <summary>Every key the logic can produce plus the names of all content in the database.</summary>
        public static List<string> AllRequired(GameDatabaseData db)
        {
            var keys = new List<string>();
            if (db != null)
            {
                foreach (var g in db.Genres) if (g != null) keys.Add(g.NameKey);
                foreach (var t in db.Themes) if (t != null) keys.Add(t.NameKey);
                foreach (var p in db.Platforms) if (p != null) keys.Add(p.NameKey);
            }
            for (int i = 0; i < DevAreas.Count; i++) keys.Add(Area((DevArea)i));
            foreach (Audience a in Enum.GetValues(typeof(Audience))) keys.Add(Audience(a));
            foreach (EngineChoice e in Enum.GetValues(typeof(EngineChoice))) keys.Add(Engine(e));
            foreach (DevPhase p in Enum.GetValues(typeof(DevPhase))) keys.Add(Phase(p));
            foreach (ComboRating c in Enum.GetValues(typeof(ComboRating))) keys.Add(Combo(c));
            foreach (ConceptError e in Enum.GetValues(typeof(ConceptError))) keys.Add(ConceptError(e));
            keys.Add(new TrendNews(TrendSubject.Genre, "", 0, TrendDirection.Rising, 0f).Key);
            keys.Add(new TrendNews(TrendSubject.Genre, "", 0, TrendDirection.Falling, 0f).Key);
            foreach (var field in typeof(PostMortemKeys).GetFields())
                if (field.IsLiteral && field.FieldType == typeof(string)) keys.Add((string)field.GetRawConstantValue());
            return keys;
        }

        /// <summary>
        /// Localization keys of the names behind a post-mortem entry's args, in order
        /// ({0}, {1} in the text). Unknown ids are returned as they are.
        /// </summary>
        public static string[] PostMortemArgKeys(PostMortemEntry entry, GameDatabaseData db)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            var args = entry.Args ?? new string[0];
            var kinds = PostMortemArgKinds(entry.Key);
            var result = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                result[i] = i < kinds.Length ? NameKeyOf(kinds[i], args[i], db) : args[i];
            return result;
        }

        private enum ArgKind { Genre, Theme, Area, Audience }

        private static ArgKind[] PostMortemArgKinds(string key)
        {
            switch (key)
            {
                case PostMortemKeys.ComboExcellent:
                case PostMortemKeys.ComboGood:
                case PostMortemKeys.ComboBad:
                    return new[] { ArgKind.Genre, ArgKind.Theme };
                case PostMortemKeys.AudiencePoor:
                    return new[] { ArgKind.Genre, ArgKind.Audience };
                case PostMortemKeys.TrendHot:
                case PostMortemKeys.TrendCold:
                    return new[] { ArgKind.Genre };
                case PostMortemKeys.HintWeightHigh:
                case PostMortemKeys.HintWeightMedium:
                case PostMortemKeys.HintWeightLow:
                    return new[] { ArgKind.Genre, ArgKind.Area };
                case PostMortemKeys.StrongestArea:
                case PostMortemKeys.WeakestArea:
                    return new[] { ArgKind.Area };
                default:
                    return new ArgKind[0];
            }
        }

        private static string NameKeyOf(ArgKind kind, string id, GameDatabaseData db)
        {
            switch (kind)
            {
                case ArgKind.Genre: return db?.GetGenre(id)?.NameKey ?? id;
                case ArgKind.Theme: return db?.GetTheme(id)?.NameKey ?? id;
                case ArgKind.Area: return "area." + id + ".name";
                case ArgKind.Audience: return "audience." + id + ".name";
                default: return id;
            }
        }

        private static string PhaseId(DevPhase phase)
        {
            switch (phase)
            {
                case DevPhase.PreProduction: return "pre_production";
                case DevPhase.Production: return "production";
                case DevPhase.Finishing: return "finishing";
                case DevPhase.QA: return "qa";
                case DevPhase.Done: return "done";
                default: throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        /// <summary>"PlatformEndsBeforeRelease" -> "platform_ends_before_release".</summary>
        internal static string ToSnakeCase(string name)
        {
            var sb = new System.Text.StringBuilder(name.Length + 8);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
