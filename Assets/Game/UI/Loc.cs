using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Game.UI
{
    /// <summary>
    /// Player texts from the String Tables (GDD 16). The table is picked from the key by
    /// <see cref="TextKeys.TableFor"/>, so callers only pass keys and values.
    /// </summary>
    public static class Loc
    {
        public static readonly string[] LanguageCodes = { "cs", "en" };
        /// <summary>Same key as the PlayerPrefLocaleSelector in the localization settings.</summary>
        public const string LanguagePrefKey = "selected-locale";

        /// <summary>Raised when the language changed and its tables are ready.</summary>
        public static event Action LanguageChanged;

        private static bool _hooked;

        /// <summary>Blocks until localization is loaded. Call once before the first text is read.</summary>
        public static void Initialize()
        {
            LocalizationSettings.InitializationOperation.WaitForCompletion();
            if (_hooked) return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public static string Get(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            IList<object> list = args != null && args.Length > 0 ? args : null;
            return LocalizationSettings.StringDatabase.GetLocalizedString(TextKeys.TableFor(key), key, list);
        }

        public static string CurrentLanguage => LocalizationSettings.SelectedLocale?.Identifier.Code;

        /// <summary>Switches the language at runtime and remembers it for the next start (PLAN 3.6).</summary>
        public static void SetLanguage(string code)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale == null)
            {
                Debug.LogWarning($"[Loc] Unknown language '{code}'.");
                return;
            }
            PlayerPrefs.SetString(LanguagePrefKey, code);
            PlayerPrefs.Save();
            LocalizationSettings.SelectedLocale = locale;
        }

        private static void OnLocaleChanged(Locale locale)
        {
            var op = LocalizationSettings.InitializationOperation;
            if (op.IsDone) LanguageChanged?.Invoke();
            else op.Completed += _ => LanguageChanged?.Invoke();
        }

        // Shortcuts for common names.
        public static string Genre(GameDatabaseData db, string id) => Get(db.GetGenre(id)?.NameKey ?? id);
        public static string Theme(GameDatabaseData db, string id) => Get(db.GetTheme(id)?.NameKey ?? id);
        public static string Platform(GameDatabaseData db, string id) => Get(db.GetPlatform(id)?.NameKey ?? id);
        public static string Money(long amount) => Get("ui.hud.money", amount);

        public static string Date(GameDate date) => Get("ui.hud.date", date.Year, Get("ui.month." + date.Month), date.Week);

        /// <summary>A post-mortem line with its args turned into localized names.</summary>
        public static string PostMortem(PostMortemEntry entry, GameDatabaseData db)
        {
            var argKeys = TextKeys.PostMortemArgKeys(entry, db);
            var args = new object[3];
            args[0] = argKeys.Length > 0 ? Get(argKeys[0]) : "";
            args[1] = argKeys.Length > 1 ? Get(argKeys[1]) : "";
            args[2] = entry.Value;
            return Get(entry.Key, args);
        }

        public static string News(TrendNews news, GameDatabaseData db)
        {
            string name = news.Subject == TrendSubject.Genre ? Genre(db, news.Id) : Theme(db, news.Id);
            return Get(news.Key, name, news.Year, (int)Math.Round(news.Change * 100f));
        }

        /// <summary>●○○ / ●●○ / ●●● for a revealed weight, ? when unknown (GDD 8).</summary>
        public static string WeightDots(WeightLevel level)
        {
            switch (level)
            {
                case WeightLevel.Low: return "●○○";
                case WeightLevel.Medium: return "●●○";
                case WeightLevel.High: return "●●●";
                default: return "?";
            }
        }
    }
}
