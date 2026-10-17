using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>A released game with its review, post-mortem and weekly sales history.</summary>
    [Serializable]
    public sealed class ReleasedGame
    {
        [SerializeField] private GameConcept concept;
        [SerializeField] private ReviewBreakdown review;
        [SerializeField] private PostMortemReport postMortem;
        [SerializeField] private SalesFactors launchSales;
        // JsonUtility cannot store null objects, so a flag tells whether launchSales is set.
        [SerializeField] private bool hasLaunchSales;
        [SerializeField] private int releaseWeek;
        [SerializeField] private int developmentWeeks;
        [SerializeField] private long developmentCost;
        [SerializeField] private bool onSale = true;
        [SerializeField] private long unitsSold;
        [SerializeField] private long revenue;
        [SerializeField] private int fansGained;
        [SerializeField] private List<long> weeklyUnits = new List<long>();

        public GameConcept Concept => concept;
        public ReviewBreakdown Review => review;
        public PostMortemReport PostMortem => postMortem;
        /// <summary>Sales factors of the first week on sale; null until then.</summary>
        public SalesFactors LaunchSales => hasLaunchSales ? launchSales : null;
        /// <summary>Total weeks since the start of the game when the game was released.</summary>
        public int ReleaseWeek => releaseWeek;
        public int DevelopmentWeeks => developmentWeeks;
        /// <summary>Money spent on dev kit, engine and platform license for this game.</summary>
        public long DevelopmentCost => developmentCost;
        public bool OnSale => onSale;
        public long UnitsSold => unitsSold;
        public long Revenue => revenue;
        public int FansGained => fansGained;
        public IReadOnlyList<long> WeeklyUnits => weeklyUnits;
        public int WeeksOnSale => weeklyUnits.Count;

        private ReleasedGame() { }

        public ReleasedGame(GameConcept concept, ReviewBreakdown review, PostMortemReport postMortem,
            int releaseWeek, int developmentWeeks, long developmentCost)
        {
            this.concept = concept ?? throw new ArgumentNullException(nameof(concept));
            this.review = review ?? throw new ArgumentNullException(nameof(review));
            this.postMortem = postMortem ?? throw new ArgumentNullException(nameof(postMortem));
            this.releaseWeek = releaseWeek;
            this.developmentWeeks = developmentWeeks;
            this.developmentCost = developmentCost;
        }

        /// <summary>
        /// Sells one week. Returns units sold; ends sales when a week falls under the minimum
        /// or the game reaches the longest sales period.
        /// </summary>
        internal long SellWeek(GameDatabaseData db, MarketTrends trends, GameDate date, StudioState studio, out bool ended)
        {
            ended = false;
            if (!onSale) return 0;
            var balance = db.Balance.Sales;

            var factors = SalesCalculator.WeeklyFactors(db, trends, concept, review.Review, date, studio.Fans, weeklyUnits.Count);
            if (!hasLaunchSales)
            {
                launchSales = factors;
                hasLaunchSales = true;
            }

            long units = (long)Math.Round(factors.ExpectedUnits);
            if (units < balance.MinWeeklyUnits) units = 0;

            if (units > 0)
            {
                long income = SalesCalculator.Revenue(balance, units);
                int fans = SalesCalculator.FansGained(balance, review.Review, units);
                studio.AddMoney(income);
                studio.AddFans(fans);
                unitsSold += units;
                revenue += income;
                fansGained += fans;
                weeklyUnits.Add(units);
            }

            if (units == 0 || weeklyUnits.Count >= balance.MaxSalesWeeks)
            {
                onSale = false;
                ended = true;
            }
            return units;
        }

        internal bool IsStructurallyValid()
        {
            return concept != null && review != null && review.AreaPoints != null && review.AreaPoints.Length == DevAreas.Count
                   && postMortem != null && weeklyUnits != null && unitsSold >= 0;
        }
    }
}
