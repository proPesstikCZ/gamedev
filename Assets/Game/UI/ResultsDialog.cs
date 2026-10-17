using Game.Core;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// Results of a released game (GDD 6.4, 6.6, PLAN 3.5): review with every factor behind it,
    /// sales so far with a weekly chart and the post-mortem hints.
    /// </summary>
    public sealed class ResultsDialog : IUiPanel
    {
        private const int MaxChartBars = 52;

        private readonly GameUI _ui;
        private readonly Label _name;
        private readonly Label _subtitle;
        private readonly Label _date;
        private readonly Label _review;
        private readonly VisualElement _postMortem;
        private readonly VisualElement _why;
        private readonly VisualElement _sales;
        private readonly VisualElement _chart;
        private readonly Label _chartEmpty;
        private readonly VisualElement _launch;
        private ReleasedGame _game;

        public VisualElement Element { get; }
        public ReleasedGame Game => _game;

        public ResultsDialog(GameUI ui, VisualElement element)
        {
            _ui = ui;
            Element = element;
            _name = element.Q<Label>("rs-name");
            _subtitle = element.Q<Label>("rs-subtitle");
            _date = element.Q<Label>("rs-date");
            _review = element.Q<Label>("rs-review");
            _postMortem = element.Q("rs-postmortem");
            _why = element.Q("rs-why");
            _sales = element.Q("rs-sales");
            _chart = element.Q("rs-chart");
            _chartEmpty = element.Q<Label>("rs-chart-empty");
            _launch = element.Q("rs-launch");
            element.Q<Button>("rs-close").clicked += _ui.CloseModal;
        }

        public void Open(ReleasedGame game)
        {
            _game = game;
            _ui.OpenModal(this);
        }

        public void Refresh()
        {
            var sim = _ui.Simulation;
            // Only redraw while open; a game from a replaced simulation is dropped.
            if (_game == null || sim == null || !_ui.IsOpen(this)) return;
            if (!Contains(sim, _game))
            {
                _game = null;
                return;
            }
            var db = _ui.Data;
            var concept = _game.Concept;
            var review = _game.Review;

            _name.text = concept.Name;
            _subtitle.text = Loc.Get("ui.dev.subtitle", Loc.Genre(db, concept.GenreId), Loc.Theme(db, concept.ThemeId), Loc.Platform(db, concept.PlatformId));
            _date.text = Loc.Get("ui.results.released_on", Loc.Date(GameDate.FromTotalWeeks(sim.Clock.StartYear, _game.ReleaseWeek)));
            _review.text = Loc.Get("ui.results.review", review.Review);

            _postMortem.Clear();
            foreach (var entry in _game.PostMortem.Entries)
            {
                if (entry.Key == PostMortemKeys.Review) continue;
                _postMortem.Add(new Label(Loc.PostMortem(entry, db)));
            }

            _why.Clear();
            Line(_why, Loc.Get("ui.results.quality", review.Quality, review.Expectation));
            Line(_why, Loc.Get("ui.results.combo", Loc.Get(TextKeys.Combo(review.Combo)), review.ComboFactor));
            Line(_why, Loc.Get("ui.results.audience", review.AudienceFactor));
            Line(_why, Loc.Get("ui.results.bugs", review.Bugs, review.BugFactor));
            Line(_why, Loc.Get("ui.results.luck", review.Noise));

            _sales.Clear();
            Line(_sales, Loc.Get("ui.results.units", _game.UnitsSold));
            Line(_sales, Loc.Get("ui.results.revenue", Loc.Money(_game.Revenue)));
            Line(_sales, Loc.Get("ui.results.cost", Loc.Money(_game.DevelopmentCost)));
            long profit = _game.Revenue - _game.DevelopmentCost;
            Line(_sales, Loc.Get("ui.results.profit", Loc.Money(profit)), profit >= 0 ? "good" : "bad");
            Line(_sales, Loc.Get("ui.results.fans", _game.FansGained));
            Line(_sales, Loc.Get(_game.OnSale ? "ui.results.on_sale" : "ui.results.sales_ended", _game.WeeksOnSale), "dim");

            DrawChart();

            _launch.Clear();
            var launch = _game.LaunchSales;
            _launch.EnableInClassList("hidden", launch == null);
            if (launch != null)
            {
                Line(_launch, Loc.Get("ui.results.launch_base", launch.InstallBase / 1_000_000f));
                Line(_launch, Loc.Get("ui.results.launch_trend", launch.GenreTrend, launch.ThemeTrend));
                Line(_launch, Loc.Get("ui.results.launch_platform", launch.PlatformPopularity, launch.FanFactor));
            }
        }

        private void DrawChart()
        {
            _chart.Clear();
            var weeks = _game.WeeklyUnits;
            _chartEmpty.EnableInClassList("hidden", weeks.Count > 0);
            _chart.EnableInClassList("hidden", weeks.Count == 0);
            long max = 1;
            foreach (long u in weeks) if (u > max) max = u;
            int start = System.Math.Max(0, weeks.Count - MaxChartBars);
            for (int i = start; i < weeks.Count; i++)
            {
                var bar = new VisualElement();
                bar.AddToClassList("chart-bar");
                bar.style.height = Length.Percent(100f * weeks[i] / max);
                _chart.Add(bar);
            }
        }

        private static void Line(VisualElement parent, string text, string cssClass = null)
        {
            var label = new Label(text);
            if (cssClass != null) label.AddToClassList(cssClass);
            parent.Add(label);
        }

        private static bool Contains(GameSimulation sim, ReleasedGame game)
        {
            foreach (var g in sim.ReleasedGames) if (g == game) return true;
            return false;
        }
    }
}
