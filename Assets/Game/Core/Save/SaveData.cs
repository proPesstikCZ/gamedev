using System;
using System.Globalization;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Serializable snapshot of the whole simulation state.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int StartYear;
        public int TotalWeeks;
        public long Money;
        public int Fans;
        public int MonthsInDebt;
        public bool IsBankrupt;
        /// <summary>RNG state as hex; JSON numbers cannot hold a full 64-bit value safely.</summary>
        public string RandomState;
    }

    public static class SaveSerializer
    {
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
