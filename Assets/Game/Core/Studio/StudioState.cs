using System;

namespace Game.Core
{
    /// <summary>Money, fans and bankruptcy state of the player's studio (GDD 14).</summary>
    public sealed class StudioState
    {
        public long Money { get; private set; }
        public int Fans { get; private set; }
        /// <summary>Consecutive month ends with a negative balance.</summary>
        public int MonthsInDebt { get; private set; }
        public bool IsBankrupt { get; private set; }

        public event Action Bankrupted;

        public StudioState(long money, int fans = 0, int monthsInDebt = 0, bool isBankrupt = false)
        {
            if (fans < 0) throw new ArgumentOutOfRangeException(nameof(fans), fans, "Must not be negative.");
            if (monthsInDebt < 0) throw new ArgumentOutOfRangeException(nameof(monthsInDebt), monthsInDebt, "Must not be negative.");
            Money = money;
            Fans = fans;
            MonthsInDebt = monthsInDebt;
            IsBankrupt = isBankrupt;
        }

        public void AddMoney(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Use Spend for costs.");
            Money += amount;
        }

        /// <summary>Spending may push the balance below zero; bankruptcy is checked at month end.</summary>
        public void Spend(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must not be negative.");
            Money -= amount;
        }

        public void AddFans(int amount)
        {
            Fans = Math.Max(0, Fans + amount);
        }

        /// <summary>
        /// Pays monthly costs and updates the debt counter. The studio goes bankrupt when the balance
        /// stays negative for more than <paramref name="monthsInDebtBeforeBankruptcy"/> month ends in a row.
        /// </summary>
        public void ApplyMonthEnd(long monthlyCosts, int monthsInDebtBeforeBankruptcy)
        {
            if (IsBankrupt) return;
            Spend(monthlyCosts);

            if (Money < 0) MonthsInDebt++;
            else MonthsInDebt = 0;

            if (MonthsInDebt > monthsInDebtBeforeBankruptcy)
            {
                IsBankrupt = true;
                Bankrupted?.Invoke();
            }
        }
    }
}
