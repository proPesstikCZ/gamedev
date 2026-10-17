using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Core;
using Game.Data;
using Game.Editor.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Game.Tests
{
    /// <summary>M3.1: every text the game needs exists in Czech and English.</summary>
    public class LocalizationTests
    {
        private static readonly Regex Placeholder = new Regex(@"\{(\d+)[^}]*\}");

        private static StringTable GetTable(string tableName, string code)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            Assert.That(collection, Is.Not.Null, $"Missing string table collection {tableName}. Run Game > Localization > Import Tables.");
            var table = collection.GetTable(code) as StringTable;
            Assert.That(table, Is.Not.Null, $"Table {tableName} has no {code} locale.");
            return table;
        }

        [Test]
        public void EveryRequiredKey_ExistsInBothLanguages()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ContentAssetTests.DatabasePath).ToCore();
            var missing = new List<string>();
            foreach (var key in TextKeys.AllRequired(db).Distinct())
            {
                foreach (var code in LocalizationTableImporter.LocaleCodes)
                {
                    var entry = GetTable(TextKeys.TableFor(key), code).GetEntry(key);
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Value)) missing.Add($"{code}: {TextKeys.TableFor(key)}/{key}");
                }
            }
            Assert.That(missing, Is.Empty, string.Join("\n", missing));
        }

        [Test]
        public void PostMortemArgs_MapToExistingNames()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ContentAssetTests.DatabasePath).ToCore();
            var required = new HashSet<string>(TextKeys.AllRequired(db));
            var entries = new[]
            {
                new PostMortemEntry(PostMortemKeys.ComboGood, 1f, "arcade", "space"),
                new PostMortemEntry(PostMortemKeys.AudiencePoor, 1f, "rpg", "kids"),
                new PostMortemEntry(PostMortemKeys.HintWeightHigh, 3f, "puzzle", "level_design"),
                new PostMortemEntry(PostMortemKeys.WeakestArea, 1f, "ai_and_systems"),
            };
            foreach (var e in entries)
                foreach (var key in TextKeys.PostMortemArgKeys(e, db))
                    Assert.That(required, Does.Contain(key), $"{e.Key} arg {key}");
        }

        [Test]
        public void Sources_ParseWithoutErrors_AndTablesAreImported()
        {
            var errors = new List<string>();
            var sources = LocalizationTableImporter.ReadSources(errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));

            var stale = new List<string>();
            foreach (var pair in sources)
            {
                var en = GetTable(pair.Key, "en");
                var cs = GetTable(pair.Key, "cs");
                foreach (var e in pair.Value)
                {
                    if (en.GetEntry(e.Key)?.Value != e.En || cs.GetEntry(e.Key)?.Value != e.Cs) stale.Add($"{pair.Key}/{e.Key}");
                }
                Assert.That(en.SharedData.Entries.Count, Is.EqualTo(pair.Value.Count), $"{pair.Key} has keys that are not in the source.");
            }
            Assert.That(stale, Is.Empty, "Tables differ from the sources. Run Game > Localization > Import Tables.\n" + string.Join("\n", stale));
        }

        [Test]
        public void Translations_UseTheSamePlaceholders()
        {
            var errors = new List<string>();
            foreach (var pair in LocalizationTableImporter.ReadSources(errors))
            {
                foreach (var e in pair.Value)
                {
                    var en = Placeholder.Matches(e.En).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x);
                    var cs = Placeholder.Matches(e.Cs).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x);
                    Assert.That(cs, Is.EqualTo(en), $"{e.Key}: placeholders differ between en and cs.");
                }
            }
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void EveryKeyInUxml_ExistsInBothLanguages()
        {
            // Fields (TextField, DropdownField, sliders) carry their key in label="#…".
            var keyInUxml = new Regex("\\b(?:text|label)=\"#([^\"]+)\"");
            var missing = new List<string>();
            int found = 0;
            foreach (var path in System.IO.Directory.GetFiles("Assets/Game/UI", "*.uxml", System.IO.SearchOption.AllDirectories))
            {
                foreach (Match m in keyInUxml.Matches(System.IO.File.ReadAllText(path)))
                {
                    found++;
                    string key = m.Groups[1].Value;
                    foreach (var code in LocalizationTableImporter.LocaleCodes)
                        if (string.IsNullOrWhiteSpace(GetTable(TextKeys.TableFor(key), code).GetEntry(key)?.Value))
                            missing.Add($"{System.IO.Path.GetFileName(path)}: {code}/{key}");
                }
            }
            Assert.That(found, Is.GreaterThan(0), "No #keys found in UXML.");
            Assert.That(missing, Is.Empty, string.Join("\n", missing));
        }

        [Test]
        public void Parser_ReportsBrokenLines()
        {
            var errors = new List<string>();
            var entries = TextSource.Parse("# comment\n\na.key\tA\tÁ\nb.key\tonly en\na.key\tA\tÁ\nc key\tC\tČ\nd.key\t\tD\n", "t.tsv", errors);
            Assert.That(entries.Select(e => e.Key), Is.EqualTo(new[] { "a.key" }));
            Assert.That(errors.Count, Is.EqualTo(4), string.Join("\n", errors));
        }

        [Test]
        public void Parser_TurnsEscapedNewlinesIntoLineBreaks()
        {
            var errors = new List<string>();
            var entries = TextSource.Parse("k\tone\\ntwo\tjedna\\ndva", "t.tsv", errors);
            Assert.That(errors, Is.Empty);
            Assert.That(entries[0].En, Is.EqualTo("one\ntwo"));
        }
    }
}
