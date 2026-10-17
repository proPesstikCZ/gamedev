using System;
using System.Collections.Generic;

namespace Game.Editor.Localization
{
    /// <summary>One text in the source files: key with its English and Czech text.</summary>
    public readonly struct TextEntry
    {
        public readonly string Key;
        public readonly string En;
        public readonly string Cs;
        public readonly int Line;

        public TextEntry(string key, string en, string cs, int line)
        {
            Key = key;
            En = en;
            Cs = cs;
            Line = line;
        }
    }

    /// <summary>
    /// Parses the text sources in Assets/Game/Localization/Source: one entry per line,
    /// "key TAB en TAB cs". Lines starting with # and empty lines are skipped, "\n" means a line break.
    /// </summary>
    public static class TextSource
    {
        public static List<TextEntry> Parse(string text, string fileName, List<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var entries = new List<TextEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNo = i + 1;
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;

                var cols = line.Split('\t');
                if (cols.Length != 3)
                {
                    errors.Add($"{fileName}:{lineNo}: expected 3 tab-separated columns, found {cols.Length}.");
                    continue;
                }
                string key = cols[0].Trim();
                string en = Unescape(cols[1].Trim());
                string cs = Unescape(cols[2].Trim());
                if (key.Length == 0 || key.IndexOf(' ') >= 0)
                {
                    errors.Add($"{fileName}:{lineNo}: invalid key '{key}'.");
                    continue;
                }
                if (en.Length == 0 || cs.Length == 0)
                {
                    errors.Add($"{fileName}:{lineNo}: '{key}' is missing a translation.");
                    continue;
                }
                if (!seen.Add(key))
                {
                    errors.Add($"{fileName}:{lineNo}: duplicate key '{key}'.");
                    continue;
                }
                entries.Add(new TextEntry(key, en, cs, lineNo));
            }
            return entries;
        }

        private static string Unescape(string value) => value.Replace("\\n", "\n");
    }
}
