using System;

namespace Game.Core
{
    /// <summary>
    /// Advances in-game time week by week. Speed and pause are handled by the Unity runner;
    /// the clock only knows discrete weeks.
    /// </summary>
    public sealed class GameClock
    {
        public int StartYear { get; }
        /// <summary>Weeks elapsed since week 1 of StartYear.</summary>
        public int TotalWeeks { get; private set; }
        public GameDate Date => GameDate.FromTotalWeeks(StartYear, TotalWeeks);

        /// <summary>Raised after every week with the new date.</summary>
        public event Action<GameDate> WeekStarted;
        /// <summary>Raised when the month changes; argument is the date of the first week of the new month.</summary>
        public event Action<GameDate> MonthStarted;
        /// <summary>Raised when the year changes; argument is the date of week 1 of the new year.</summary>
        public event Action<GameDate> YearStarted;

        public GameClock(int startYear, int totalWeeks = 0)
        {
            if (totalWeeks < 0) throw new ArgumentOutOfRangeException(nameof(totalWeeks), totalWeeks, "Must not be negative.");
            StartYear = startYear;
            TotalWeeks = totalWeeks;
        }

        public void AdvanceWeek()
        {
            var before = Date;
            TotalWeeks++;
            var after = Date;

            WeekStarted?.Invoke(after);
            if (after.Year != before.Year) YearStarted?.Invoke(after);
            if (after.Month != before.Month || after.Year != before.Year) MonthStarted?.Invoke(after);
        }
    }
}
