using System;
using System.Collections.Generic;

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
        /// <summary>Player skill; multiplies work points (GDD 6.2). Grows with released games.</summary>
        public float Skill { get; private set; }
        /// <summary>Platforms whose dev kit the studio already bought.</summary>
        public IReadOnlyCollection<string> OwnedDevKits => _devKits;

        private readonly SortedSet<string> _devKits = new SortedSet<string>(StringComparer.Ordinal);

        public event Action Bankrupted;

        public StudioState(long money, int fans = 0, int monthsInDebt = 0, bool isBankrupt = false,
            float skill = 1f, IEnumerable<string> ownedDevKits = null)
        {
            if (float.IsNaN(skill) || float.IsInfinity(skill) || skill <= 0f)
                throw new ArgumentOutOfRangeException(nameof(skill), skill, "Must be a positive number.");
            Skill = skill;
            if (ownedDevKits != null)
                foreach (var id in ownedDevKits) if (!string.IsNullOrEmpty(id)) _devKits.Add(id);
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
            // Paying off the debt ends the debt period, even if the money is spent again before month end (GDD 14).
            if (Money >= 0) MonthsInDebt = 0;
        }

        /// <summary>Spending may push the balance below zero; bankruptcy is checked at month end.</summary>
        public void Spend(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must not be negative.");
            Money -= amount;
        }

        public bool OwnsDevKit(string platformId) => platformId != null && _devKits.Contains(platformId);

        public void AddDevKit(string platformId)
        {
            if (string.IsNullOrEmpty(platformId)) throw new ArgumentException("Platform id is empty.", nameof(platformId));
            _devKits.Add(platformId);
        }

        public void ImproveSkill(float amount, float maxSkill)
        {
            if (amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must not be negative.");
            // Grows up to maxSkill, never drops.
            Skill = Math.Max(Skill, Math.Min(maxSkill, Skill + amount));
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
