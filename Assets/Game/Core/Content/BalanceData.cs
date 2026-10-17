using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Global tuning values for the simulation. Treat as read-only during play.</summary>
    public sealed class BalanceData
    {
        public int StartYear { get; }
        public long StartMoney { get; }
        /// <summary>Garage rent and living costs, paid at the end of each month.</summary>
        public long MonthlyGarageCost { get; }
        /// <summary>The studio goes bankrupt when its balance stays negative for more months than this.</summary>
        public int MonthsInDebtBeforeBankruptcy { get; }

        public DevelopmentBalance Development { get; }
        public ReviewBalance Review { get; }
        public SalesBalance Sales { get; }
        public TrendBalance Trends { get; }
        public PostMortemBalance PostMortem { get; }

        public BalanceData(int startYear, long startMoney, long monthlyGarageCost, int monthsInDebtBeforeBankruptcy,
            DevelopmentBalance development = null, ReviewBalance review = null, SalesBalance sales = null,
            TrendBalance trends = null, PostMortemBalance postMortem = null)
        {
            StartYear = startYear;
            StartMoney = startMoney;
            MonthlyGarageCost = monthlyGarageCost;
            MonthsInDebtBeforeBankruptcy = monthsInDebtBeforeBankruptcy;
            Development = development ?? new DevelopmentBalance();
            Review = review ?? new ReviewBalance();
            Sales = sales ?? new SalesBalance();
            Trends = trends ?? new TrendBalance();
            PostMortem = postMortem ?? new PostMortemBalance();
        }
    }

    /// <summary>Development phases, points and bugs (GDD 6.2). Values are for a small game.</summary>
    [Serializable]
    public sealed class DevelopmentBalance
    {
        [Tooltip("Weeks of pre-production.")]
        public int PreProductionWeeks = 3;
        [Tooltip("Weeks of production.")]
        public int ProductionWeeks = 5;
        [Tooltip("Weeks of finishing.")]
        public int FinishingWeeks = 3;
        [Tooltip("Most QA weeks the player can choose.")]
        public int MaxQaWeeks = 5;
        [Tooltip("Work points per week before skill and technology.")]
        public float PointsPerWeek = 10f;
        [Tooltip("Player skill at the start of the game.")]
        public float StartSkill = 1f;
        [Tooltip("Skill gained with every released game.")]
        public float SkillGainPerGame = 0.1f;
        public float MaxSkill = 3f;
        [Tooltip("Technology multiplier of the player's own engine.")]
        public float CustomEngineTech = 1f;
        [Tooltip("Technology multiplier of a licensed engine.")]
        public float LicensedEngineTech = 1.2f;
        [Tooltip("One-off price of a licensed engine per game.")]
        public long LicensedEngineCost = 1500;
        [Tooltip("Technology = engine x (1 + this x platform tech ceiling).")]
        public float PlatformTechWeight = 2f;
        [Tooltip("Bugs added per development week, divided by skill.")]
        public float BugsPerWeek = 2f;
        [Tooltip("Share of the remaining bugs removed by each QA week.")]
        [Range(0f, 1f)]
        public float QaBugReductionPerWeek = 0.35f;
    }

    /// <summary>Quality and review formula (GDD 6.4).</summary>
    [Serializable]
    public sealed class ReviewBalance
    {
        [Tooltip("Area score = points ^ this. Below 1 gives diminishing returns, so spreading by genre weights pays off.")]
        [Range(0.1f, 1f)]
        public float AreaExponent = 0.5f;
        public float ExcellentComboFactor = 1.2f;
        public float GoodComboFactor = 1f;
        public float BadComboFactor = 0.7f;
        [Tooltip("Audience factor at genre fit 0; fit 1 gives factor 1.")]
        [Range(0f, 1f)]
        public float MinAudienceFactor = 0.6f;
        [Tooltip("Bug factor = 1 / (1 + this x bugs).")]
        public float BugPenaltyPerBug = 0.03f;
        [Tooltip("Quality the market expects in the start year.")]
        public float ExpectationAtStart = 3.5f;
        [Tooltip("Yearly growth of market expectations (0.04 = 4 %).")]
        public float ExpectationGrowthPerYear = 0.04f;
        [Tooltip("Review = 1 + 9 x (1 - e^(-this x quality/expectation)).")]
        public float CurveSteepness = 1f;
        [Tooltip("Largest random change of a review. GDD 6.4 allows at most 0.5.")]
        [Range(0f, 0.5f)]
        public float MaxNoise = 0.5f;
    }

    /// <summary>Weekly sales (GDD 6.4).</summary>
    [Serializable]
    public sealed class SalesBalance
    {
        [Tooltip("Share of the installed base buying in the first week with review 10 and all factors 1.")]
        public float BaseWeeklyPurchaseRate = 0.004f;
        [Tooltip("Review factor = (review / 10) ^ this.")]
        public float ReviewExponent = 2f;
        [Tooltip("Sales of each week = previous week x this.")]
        [Range(0f, 1f)]
        public float WeeklyDecay = 0.85f;
        [Tooltip("Sales end when a week sells fewer units than this.")]
        public long MinWeeklyUnits = 5;
        public int MaxSalesWeeks = 104;
        [Tooltip("Shop price of a game.")]
        public float Price = 20f;
        [Tooltip("Price at which the price factor is 1.")]
        public float ReferencePrice = 20f;
        [Tooltip("Price factor = (reference / price) ^ this.")]
        public float PriceElasticity = 1f;
        [Tooltip("Share of the price that goes to the studio.")]
        [Range(0f, 1f)]
        public float RevenueShare = 0.35f;
        [Tooltip("Constant in the slice (GDD 18).")]
        public float Hype = 1f;
        [Tooltip("Constant in the slice (GDD 18).")]
        public float Marketing = 1f;
        [Tooltip("Fan factor = 1 + fans / this.")]
        public float FansForDoubleSales = 5000f;
        [Tooltip("New fans per sold unit for a review of 10.")]
        public float FansPerUnit = 0.05f;
        [Tooltip("Games below this review gain no fans.")]
        public float FanReviewThreshold = 6f;
        [Tooltip("Games below this review lose a share of fans.")]
        public float FlopReviewThreshold = 4f;
        [Range(0f, 1f)]
        public float FanLossOnFlop = 0.1f;
    }

    /// <summary>Genre and theme popularity in time (GDD 9).</summary>
    [Serializable]
    public sealed class TrendBalance
    {
        [Tooltip("Largest generated change of a trend per year (0.1 = +-10 %).")]
        [Range(0f, 0.5f)]
        public float NoiseAmplitude = 0.1f;
        [Tooltip("How many years ahead market news announce a trend.")]
        public int AnnounceYearsAhead = 1;
        [Tooltip("Relative change needed to announce a trend (0.15 = 15 %).")]
        public float AnnounceThreshold = 0.15f;
        [Tooltip("Lowest possible trend multiplier.")]
        public float MinTrend = 0.2f;
    }

    /// <summary>Post-mortem hints and revealing of market knowledge (GDD 6.6, 8).</summary>
    [Serializable]
    public sealed class PostMortemBalance
    {
        [Tooltip("Most important hidden weights revealed per released game.")]
        public int HighWeightRevealsPerGame = 1;
        [Tooltip("Least important hidden weights revealed per released game.")]
        public int LowWeightRevealsPerGame = 1;
        [Tooltip("Weights below this show as low (one dot).")]
        public float LowWeightBelow = 0.34f;
        [Tooltip("Weights from this show as high (three dots).")]
        public float HighWeightFrom = 0.67f;
        [Tooltip("Quality / expectation above this counts as above expectations.")]
        public float AboveExpectationRatio = 1.2f;
        [Tooltip("Quality / expectation below this counts as below expectations.")]
        public float BelowExpectationRatio = 0.8f;
        [Tooltip("Audience factor below this is reported as a poor fit.")]
        public float PoorAudienceFactor = 0.9f;
        [Tooltip("Bug factor below this is reported as too many bugs.")]
        public float ManyBugsFactor = 0.85f;
        [Tooltip("Bug factor above this is reported as well tested.")]
        public float FewBugsFactor = 0.97f;
        [Tooltip("Genre trend from this is reported as hot.")]
        public float HotTrend = 1.2f;
        [Tooltip("Genre trend below this is reported as cold.")]
        public float ColdTrend = 0.85f;
    }
}
