namespace Game.Core
{
    /// <summary>Global tuning values for the simulation.</summary>
    public sealed class BalanceData
    {
        public int StartYear { get; }
        public long StartMoney { get; }
        /// <summary>Garage rent and living costs, paid at the end of each month.</summary>
        public long MonthlyGarageCost { get; }
        /// <summary>The studio goes bankrupt when its balance stays negative for more months than this.</summary>
        public int MonthsInDebtBeforeBankruptcy { get; }

        public BalanceData(int startYear, long startMoney, long monthlyGarageCost, int monthsInDebtBeforeBankruptcy)
        {
            StartYear = startYear;
            StartMoney = startMoney;
            MonthlyGarageCost = monthlyGarageCost;
            MonthsInDebtBeforeBankruptcy = monthsInDebtBeforeBankruptcy;
        }
    }
}
