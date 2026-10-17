using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Balance Config", fileName = "balance")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [SerializeField] private int startYear = 1972;
        [SerializeField] private long startMoney = 10000;
        [Tooltip("Garage rent and living costs per month.")]
        [SerializeField] private long monthlyGarageCost = 300;
        [Tooltip("Bankruptcy when the balance stays negative for more months than this.")]
        [SerializeField] private int monthsInDebtBeforeBankruptcy = 3;

        public BalanceData ToCore() => new BalanceData(startYear, startMoney, monthlyGarageCost, monthsInDebtBeforeBankruptcy);
    }
}
