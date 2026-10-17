using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Serializable snapshot of the whole simulation state.</summary>
    [Serializable]
    public sealed class SaveData
    {
        // 2: development, releases, knowledge and trends (M2).
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public int StartYear;
        public int TotalWeeks;
        public long Money;
        public int Fans;
        public int MonthsInDebt;
        public bool IsBankrupt;
        /// <summary>RNG state as hex; JSON numbers cannot hold a full 64-bit value safely.</summary>
        public string RandomState;
        /// <summary>Seed of generated trend changes, hex.</summary>
        public string TrendSeed;
        public float Skill = 1f;
        public List<string> OwnedDevKits = new List<string>();
        /// <summary>JsonUtility cannot store null objects, so this flag tells whether ActiveProject is set.</summary>
        public bool HasActiveProject;
        public DevelopmentProject ActiveProject;
        public long ActiveProjectCost;
        public List<ReleasedGame> ReleasedGames = new List<ReleasedGame>();
        public MarketKnowledge Knowledge = new MarketKnowledge();
    }

    /// <summary>Why a save could not be loaded. UI maps it to a localized message.</summary>
    public enum SaveLoadError
    {
        None = 0,
        /// <summary>The save was made by an older (or newer) version of the game.</summary>
        UnsupportedVersion = 1,
        /// <summary>The file is empty, broken or has invalid values.</summary>
        Corrupt = 2,
    }

    public static class SaveSerializer
    {
        [Serializable]
        private sealed class VersionOnly
        {
            public int Version = -1;
        }

        /// <summary>Like <see cref="FromJson"/>, but never throws: returns the reason instead.</summary>
        public static SaveData TryFromJson(string json, out SaveLoadError error)
        {
            error = SaveLoadError.None;
            int version;
            try
            {
                var header = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<VersionOnly>(json);
                version = header?.Version ?? -1;
            }
            catch (ArgumentException)
            {
                error = SaveLoadError.Corrupt;
                return null;
            }
            if (version < 0)
            {
                error = SaveLoadError.Corrupt;
                return null;
            }
            if (version != SaveData.CurrentVersion)
            {
                error = SaveLoadError.UnsupportedVersion;
                return null;
            }
            try
            {
                return FromJson(json);
            }
            catch (Exception e) when (e is FormatException || e is ArgumentException)
            {
                error = SaveLoadError.Corrupt;
                return null;
            }
        }

        public static string ToJson(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return JsonUtility.ToJson(data, true);
        }

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("Save is empty.");
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) throw new FormatException("Save could not be parsed.");
            if (data.Version != SaveData.CurrentVersion)
                throw new FormatException($"Unsupported save version {data.Version} (expected {SaveData.CurrentVersion}).");
            if (data.TotalWeeks < 0 || data.Fans < 0 || data.MonthsInDebt < 0)
                throw new FormatException("Save contains negative counters.");
            ParseRandomState(data.RandomState);
            ParseRandomState(data.TrendSeed);
            if (float.IsNaN(data.Skill) || float.IsInfinity(data.Skill) || data.Skill <= 0f)
                throw new FormatException("Save has an invalid skill.");
            if (data.HasActiveProject && (data.ActiveProject == null || !data.ActiveProject.IsStructurallyValid()))
                throw new FormatException("Save has a broken project in development.");
            if (data.ReleasedGames == null || data.ReleasedGames.Exists(g => g == null || !g.IsStructurallyValid()))
                throw new FormatException("Save has a broken released game.");
            if (data.Knowledge == null || !data.Knowledge.IsStructurallyValid())
                throw new FormatException("Save has broken market knowledge.");
            return data;
        }

        internal static string FormatRandomState(ulong state) => state.ToString("X16", CultureInfo.InvariantCulture);

        internal static ulong ParseRandomState(string value)
        {
            if (!ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var state))
                throw new FormatException("Save has an invalid random state.");
            return state;
        }
    }
}
