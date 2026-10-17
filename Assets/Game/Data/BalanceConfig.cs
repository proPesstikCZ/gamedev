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

        [Header("Development (GDD 6.2)")]
        [SerializeField] private DevelopmentBalance development = new DevelopmentBalance();
        [Header("Quality and review (GDD 6.4)")]
        [SerializeField] private ReviewBalance review = new ReviewBalance();
        [Header("Sales (GDD 6.4)")]
        [SerializeField] private SalesBalance sales = new SalesBalance();
        [Header("Trends (GDD 9)")]
        [SerializeField] private TrendBalance trends = new TrendBalance();
        [Header("Post-mortem and knowledge (GDD 6.6, 8)")]
        [SerializeField] private PostMortemBalance postMortem = new PostMortemBalance();

        public BalanceData ToCore() => new BalanceData(startYear, startMoney, monthlyGarageCost, monthsInDebtBeforeBankruptcy,
            development, review, sales, trends, postMortem);
    }
}
