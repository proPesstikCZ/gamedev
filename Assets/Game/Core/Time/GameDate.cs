using System;

namespace Game.Core
{
    /// <summary>
    /// In-game date. The game counts 52 weeks per year; months are derived from the week
    /// so that every year has exactly 12 month changes.
    /// </summary>
    public readonly struct GameDate : IEquatable<GameDate>
    {
        public const int WeeksPerYear = 52;
        public const int MonthsPerYear = 12;

        /// <summary>Calendar year, e.g. 1972.</summary>
        public int Year { get; }
        /// <summary>Week of the year, 1..52.</summary>
        public int Week { get; }
        /// <summary>Month of the year, 1..12.</summary>
        public int Month => (Week - 1) * MonthsPerYear / WeeksPerYear + 1;

        public GameDate(int year, int week)
        {
            if (week < 1 || week > WeeksPerYear) throw new ArgumentOutOfRangeException(nameof(week), week, "Week must be 1..52.");
            Year = year;
            Week = week;
        }

        public static GameDate FromTotalWeeks(int startYear, int totalWeeks)
        {
            if (totalWeeks < 0) throw new ArgumentOutOfRangeException(nameof(totalWeeks), totalWeeks, "Must not be negative.");
            return new GameDate(startYear + totalWeeks / WeeksPerYear, totalWeeks % WeeksPerYear + 1);
        }

        public bool Equals(GameDate other) => Year == other.Year && Week == other.Week;
        public override bool Equals(object obj) => obj is GameDate other && Equals(other);
        public override int GetHashCode() => Year * 100 + Week;
        public static bool operator ==(GameDate a, GameDate b) => a.Equals(b);
        public static bool operator !=(GameDate a, GameDate b) => !a.Equals(b);

        /// <summary>Debug format, e.g. 1972-W01 (M1). Not for players.</summary>
        public override string ToString() => $"{Year}-W{Week:00} (M{Month})";
    }
}
