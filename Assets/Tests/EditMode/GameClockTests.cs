using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class GameClockTests
    {
        [Test]
        public void NewClock_StartsInWeekOneOfStartYear()
        {
            var clock = new GameClock(1972);
            Assert.That(clock.Date, Is.EqualTo(new GameDate(1972, 1)));
            Assert.That(clock.Date.Month, Is.EqualTo(1));
        }

        [Test]
        public void After52Weeks_ItIs1973()
        {
            var clock = new GameClock(1972);
            for (int i = 0; i < 51; i++) clock.AdvanceWeek();
            Assert.That(clock.Date, Is.EqualTo(new GameDate(1972, 52)));
            Assert.That(clock.Date.Month, Is.EqualTo(12));

            clock.AdvanceWeek();
            Assert.That(clock.Date, Is.EqualTo(new GameDate(1973, 1)));
        }

        [Test]
        public void EachYear_HasExactlyTwelveMonthChangesAndOneYearChange()
        {
            var clock = new GameClock(1972);
            var months = new List<int>();
            int years = 0;
            clock.MonthStarted += d => months.Add(d.Month);
            clock.YearStarted += _ => years++;

            for (int i = 0; i < GameDate.WeeksPerYear; i++) clock.AdvanceWeek();

            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 1 }, months);
            Assert.That(years, Is.EqualTo(1));
        }

        [Test]
        public void WeekStarted_FiresEveryWeekWithNewDate()
        {
            var clock = new GameClock(1972);
            var dates = new List<GameDate>();
            clock.WeekStarted += dates.Add;
            clock.AdvanceWeek();
            clock.AdvanceWeek();
            CollectionAssert.AreEqual(new[] { new GameDate(1972, 2), new GameDate(1972, 3) }, dates);
        }

        [Test]
        public void YearStarted_FiresBeforeMonthStarted()
        {
            var clock = new GameClock(1972, GameDate.WeeksPerYear - 1);
            var order = new List<string>();
            clock.YearStarted += _ => order.Add("year");
            clock.MonthStarted += _ => order.Add("month");
            clock.AdvanceWeek();
            CollectionAssert.AreEqual(new[] { "year", "month" }, order);
        }

        [Test]
        public void MonthMapping_CoversAllMonthsInOrder()
        {
            int previous = 1;
            for (int week = 1; week <= GameDate.WeeksPerYear; week++)
            {
                int month = new GameDate(1972, week).Month;
                Assert.That(month, Is.InRange(previous, previous + 1), $"week {week}");
                previous = month;
            }
            Assert.That(previous, Is.EqualTo(12));
        }
    }
}
