using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class StudioStateTests
    {
        [Test]
        public void MonthEnd_PaysMonthlyCosts()
        {
            var studio = new StudioState(1000);
            studio.ApplyMonthEnd(300, 3);
            Assert.That(studio.Money, Is.EqualTo(700));
            Assert.That(studio.MonthsInDebt, Is.EqualTo(0));
        }

        [Test]
        public void NegativeBalance_CountsMonthsInDebt()
        {
            var studio = new StudioState(100);
            studio.ApplyMonthEnd(300, 3);
            Assert.That(studio.Money, Is.EqualTo(-200));
            Assert.That(studio.MonthsInDebt, Is.EqualTo(1));
            Assert.That(studio.IsBankrupt, Is.False);
        }

        [Test]
        public void BankruptAfterMoreThanAllowedMonthsInDebt()
        {
            var studio = new StudioState(0);
            int bankruptEvents = 0;
            studio.Bankrupted += () => bankruptEvents++;

            for (int i = 0; i < 3; i++) studio.ApplyMonthEnd(100, 3);
            Assert.That(studio.IsBankrupt, Is.False, "3 months in debt are still allowed");

            studio.ApplyMonthEnd(100, 3);
            Assert.That(studio.IsBankrupt, Is.True);
            Assert.That(bankruptEvents, Is.EqualTo(1));

            studio.ApplyMonthEnd(100, 3);
            Assert.That(bankruptEvents, Is.EqualTo(1), "Bankruptcy is reported only once");
            Assert.That(studio.Money, Is.EqualTo(-400), "No more costs after bankruptcy");
        }

        [Test]
        public void IncomeJustBeforeBankruptcy_SavesTheStudio()
        {
            var studio = new StudioState(0);
            for (int i = 0; i < 3; i++) studio.ApplyMonthEnd(100, 3);
            Assert.That(studio.MonthsInDebt, Is.EqualTo(3));

            studio.AddMoney(1000);
            studio.ApplyMonthEnd(100, 3);

            Assert.That(studio.IsBankrupt, Is.False);
            Assert.That(studio.MonthsInDebt, Is.EqualTo(0));
            Assert.That(studio.Money, Is.EqualTo(600));
        }

        [Test]
        public void RepayingDebtDuringMonth_ResetsDebtPeriodEvenIfSpentAgain()
        {
            var studio = new StudioState(0);
            for (int i = 0; i < 3; i++) studio.ApplyMonthEnd(100, 3);
            Assert.That(studio.MonthsInDebt, Is.EqualTo(3));

            studio.AddMoney(300);   // balance back to 0 -> debt repaid
            Assert.That(studio.MonthsInDebt, Is.EqualTo(0));
            studio.Spend(500);      // in debt again during the same month

            studio.ApplyMonthEnd(100, 3);
            Assert.That(studio.IsBankrupt, Is.False, "A new debt period must start after repayment");
            Assert.That(studio.MonthsInDebt, Is.EqualTo(1));
        }

        [Test]
        public void PartialRepayment_KeepsDebtPeriod()
        {
            var studio = new StudioState(0);
            for (int i = 0; i < 2; i++) studio.ApplyMonthEnd(100, 3);
            studio.AddMoney(50);    // still -150
            Assert.That(studio.MonthsInDebt, Is.EqualTo(2));
        }

        [Test]
        public void Fans_NeverGoBelowZero()
        {
            var studio = new StudioState(0, fans: 10);
            studio.AddFans(-50);
            Assert.That(studio.Fans, Is.EqualTo(0));
        }

        [Test]
        public void NegativeAmounts_AreRejected()
        {
            var studio = new StudioState(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => studio.AddMoney(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => studio.Spend(-1));
        }
    }
}
