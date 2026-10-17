using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Core;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Game.Editor.Localization
{
    /// <summary>
    /// Builds the Unity Localization setup from the text sources: locales cs and en,
    /// String Tables UI, Content and PostMortem. The .tsv files are the source of truth;
    /// keys missing from them are removed from the tables.
    /// </summary>
    public static class LocalizationTableImporter
    {
        public const string Root = "Assets/Game/Localization";
        public const string SourceDir = Root + "/Source";
        public const string SettingsPath = Root + "/LocalizationSettings.asset";
        public const string LocalesDir = Root + "/Locales";
        public const string TablesDir = Root + "/Tables";
        /// <summary>PlayerPrefs key of the chosen language; the UI writes it on every switch.</summary>
        public const string LanguagePrefKey = "selected-locale";

        public static readonly string[] Tables = { TextKeys.UiTable, TextKeys.ContentTable, TextKeys.PostMortemTable };
        public static readonly string[] LocaleCodes = { "en", "cs" };

        [MenuItem("Game/Localization/Import Tables")]
        public static void ImportMenu()
        {
            var errors = Import();
            if (errors.Count == 0) Debug.Log("[Localization] Tables imported.");
            else foreach (var e in errors) Debug.LogError("[Localization] " + e);
        }

        /// <summary>Reads all sources. Errors are returned; nothing is written when there are any.</summary>
        public static Dictionary<string, List<TextEntry>> ReadSources(List<string> errors)
        {
            var result = new Dictionary<string, List<TextEntry>>();
            foreach (var table in Tables)
            {
                string path = $"{SourceDir}/{table}.tsv";
                if (!File.Exists(path))
                {
                    errors.Add($"Missing {path}.");
                    continue;
                }
                var entries = TextSource.Parse(File.ReadAllText(path), Path.GetFileName(path), errors);
                foreach (var e in entries)
                    if (TextKeys.TableFor(e.Key) != table)
                        errors.Add($"{table}.tsv:{e.Line}: '{e.Key}' belongs to table {TextKeys.TableFor(e.Key)}.");
                result[table] = entries;
            }
            return result;
        }

        public static List<string> Import()
        {
            var errors = new List<string>();
            var sources = ReadSources(errors);
            if (errors.Count > 0) return errors;

            var settings = EnsureSettings();
            var locales = EnsureLocales();
            EnsureStartupSelectors(settings);

            foreach (var table in Tables)
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(table)
                                 ?? LocalizationEditorSettings.CreateStringTableCollection(table, TablesDir, locales);
                Fill(collection, locales, sources[table]);
            }
            AssetDatabase.SaveAssets();
            return errors;
        }

        private static LocalizationSettings EnsureSettings()
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
                if (settings == null)
                {
                    Directory.CreateDirectory(Root);
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                }
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }
            return settings;
        }

        private static List<Locale> EnsureLocales()
        {
            Directory.CreateDirectory(LocalesDir);
            var result = new List<Locale>();
            foreach (var code in LocaleCodes)
            {
                var locale = LocalizationEditorSettings.GetLocales().FirstOrDefault(l => l.Identifier.Code == code);
                if (locale == null)
                {
                    string path = $"{LocalesDir}/{code}.asset";
                    locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
                    if (locale == null)
                    {
                        locale = Locale.CreateLocale(code);
                        AssetDatabase.CreateAsset(locale, path);
                    }
                    LocalizationEditorSettings.AddLocale(locale);
                }
                result.Add(locale);
            }
            return result;
        }

        /// <summary>Saved choice first, then the system language, English as the last resort.</summary>
        private static void EnsureStartupSelectors(LocalizationSettings settings)
        {
            var selectors = settings.GetStartupLocaleSelectors();
            selectors.Clear();
            selectors.Add(new CommandLineLocaleSelector());
            selectors.Add(new PlayerPrefLocaleSelector { PlayerPreferenceKey = LanguagePrefKey });
            selectors.Add(new SystemLocaleSelector());
            selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier("en") });
            EditorUtility.SetDirty(settings);
        }

        private static void Fill(StringTableCollection collection, List<Locale> locales, List<TextEntry> entries)
        {
            var shared = collection.SharedData;
            var wanted = new HashSet<string>(entries.Select(e => e.Key));
            foreach (var key in shared.Entries.Select(e => e.Key).Where(k => !wanted.Contains(k)).ToList())
                collection.RemoveEntry(key);

            foreach (var locale in locales)
            {
                var table = (StringTable)collection.GetTable(locale.Identifier) ?? (StringTable)collection.AddNewTable(locale.Identifier);
                foreach (var e in entries)
                {
                    string value = locale.Identifier.Code == "cs" ? e.Cs : e.En;
                    var entry = table.AddEntry(e.Key, value);
                    // Smart strings allow {0:0.0} formats and Czech plurals.
                    entry.IsSmart = value.IndexOf('{') >= 0;
                }
                LocalizationEditorSettings.SetPreloadTableFlag(table, true);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(shared);
            EditorUtility.SetDirty(collection);
        }
    }
}
