using System;
using System.Globalization;
using System.Text;
using Game.Core;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Debug command: simulates the development of one game from the start of a new game
    /// until its sales end and prints every step to the console. Texts are for developers only.
    /// </summary>
    public static class SimulateOneGameCommand
    {
        public const string DatabasePath = "Assets/Game/Content/GameDatabase.asset";
        private const int MaxWeeks = 52 * 4;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [MenuItem("Game/Debug/Simulate One Game (Weight Bot)")]
        private static void RunWeightBot() => RunAndLog(1972, BotStrategy.FollowWeights);

        [MenuItem("Game/Debug/Simulate One Game (Random Bot)")]
        private static void RunRandomBot() => RunAndLog(1972, BotStrategy.Random);

        [MenuItem("Game/Debug/Simulate One Game (Weight Bot, Random Seed)")]
        private static void RunRandomSeed() => RunAndLog((ulong)DateTime.Now.Ticks, BotStrategy.FollowWeights);

        private static void RunAndLog(ulong seed, BotStrategy strategy)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (asset == null)
            {
                Debug.LogError($"[SimulateOneGame] Missing {DatabasePath}.");
                return;
            }
            var db = asset.ToCore();
            var errors = DatabaseValidator.Validate(db);
            if (errors.Count > 0)
            {
                Debug.LogError("[SimulateOneGame] Content errors:\n" + string.Join("\n", errors));
                return;
            }
            Debug.Log(Simulate(db, seed, strategy));
        }

        /// <summary>Runs one game and returns the report. Public so tests can call it.</summary>
        public static string Simulate(GameDatabaseData db, ulong seed, BotStrategy strategy)
        {
            var sim = GameSimulation.NewGame(db, seed);
            var bot = new SimpleBot(strategy, seed);
            var sb = new StringBuilder();
            long startMoney = sim.Studio.Money;
            sb.AppendLine($"[SimulateOneGame] seed {seed}, bot {strategy}, start {sim.Date}, money {startMoney}");

            if (!bot.TryStartGame(sim))
            {
                sb.AppendLine("Bot could not start a game (no affordable concept).");
                return sb.ToString();
            }

            var project = sim.ActiveProject;
            var c = project.Concept;
            sb.AppendLine($"Concept: '{c.Name}' genre {c.GenreId}, theme {c.ThemeId}, platform {c.PlatformId}, audience {c.Audience}, engine {c.Engine}, size {c.Size}");
            sb.AppendLine($"Paid upfront: {startMoney - sim.Studio.Money} | skill {F(project.Skill)} x technology {F(project.Technology)} = {F(project.PointsPerWorkWeek)} points/week");
            foreach (var phase in DevPhases.WorkPhases)
            {
                var areas = DevPhases.AreasOf(phase);
                var split = project.GetAllocation(phase);
                sb.Append($"  {phase} ({project.WeeksOf(phase)} wk):");
                for (int i = 0; i < areas.Count; i++) sb.Append($" {areas[i]} {split[i]}%");
                sb.AppendLine();
            }
            sb.AppendLine($"  QA: {project.QaWeeks} wk | total {project.TotalWeeks} wk");

            ReleasedGame released = null;
            sim.GameReleased += g => released = g;
            int weeks = 0;
            while (released == null && !sim.IsGameOver && weeks++ < MaxWeeks) sim.AdvanceWeek();
            if (released == null)
            {
                sb.AppendLine(sim.IsGameOver ? $"Bankrupt on {sim.Date} before release." : "Game was not released in time.");
                return sb.ToString();
            }

            AppendReview(sb, released.Review);
            AppendPostMortem(sb, released.PostMortem);

            while (released.OnSale && !sim.IsGameOver && weeks++ < MaxWeeks) sim.AdvanceWeek();
            AppendSales(sb, released);

            sb.AppendLine($"End {sim.Date}: money {sim.Studio.Money} (start {startMoney}, change {sim.Studio.Money - startMoney}), fans {sim.Studio.Fans}, skill {F(sim.Studio.Skill)}, bankrupt {sim.IsGameOver}");
            sb.AppendLine($"Knowledge: {sim.Knowledge.RevealedWeightCount} weights, {sim.Knowledge.RevealedComboCount} combos revealed");
            if (sim.CurrentNews.Count > 0)
            {
                sb.Append("Market news this year:");
                foreach (var n in sim.CurrentNews) sb.Append($" [{n.Subject} {n.Id} {n.Direction} {n.Change:+0%;-0%} in {n.Year}]");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static void AppendReview(StringBuilder sb, ReviewBreakdown r)
        {
            sb.AppendLine($"Released in {r.Year}. Areas (points -> score x weight):");
            for (int i = 0; i < DevAreas.Count; i++)
                sb.AppendLine($"  {(DevArea)i,-16} {F(r.AreaPoints[i]),7} -> {F(r.AreaScores[i]),5} x {F(r.Weights[i])}");
            sb.AppendLine($"Quality = area {F(r.AreaQuality)} x combo {r.Combo} {F(r.ComboFactor)} x audience {F(r.AudienceFactor)} (fit {F(r.AudienceFit)}) x bugs {F(r.BugFactor)} ({F(r.Bugs)} bugs) = {F(r.Quality)}");
            sb.AppendLine($"Expectation {F(r.Expectation)} -> ratio {F(r.QualityRatio)} -> base review {F(r.BaseReview)} + noise {r.Noise.ToString("+0.00;-0.00", Inv)} = REVIEW {F(r.Review)}");
        }

        private static void AppendPostMortem(StringBuilder sb, PostMortemReport report)
        {
            sb.AppendLine("Post-mortem:");
            foreach (var e in report.Entries)
                sb.AppendLine($"  {e.Key} ({string.Join(", ", e.Args)}) = {F(e.Value)}");
        }

        private static void AppendSales(StringBuilder sb, ReleasedGame g)
        {
            var f = g.LaunchSales;
            if (f != null)
                sb.AppendLine($"Launch week: base {f.InstallBase:0} x review {F(f.ReviewFactor)} x genre trend {F(f.GenreTrend)} x theme trend {F(f.ThemeTrend)} x platform {F(f.PlatformPopularity)} x hype {F(f.Hype)} x marketing {F(f.Marketing)} x fans {F(f.FanFactor)} x price {F(f.PriceFactor)} -> {F(f.ExpectedUnits)} units");
            sb.Append($"Weekly units ({g.WeeksOnSale} wk):");
            foreach (long u in g.WeeklyUnits) sb.Append(' ').Append(u);
            sb.AppendLine();
            sb.AppendLine($"Total: {g.UnitsSold} units, revenue {g.Revenue}, costs {g.DevelopmentCost}, fans gained {g.FansGained}, on sale {g.OnSale}");
        }

        private static string F(float v) => v.ToString("0.00", Inv);
    }
}
