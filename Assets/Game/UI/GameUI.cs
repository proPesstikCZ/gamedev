using System.Collections.Generic;
using Game.Core;
using Game.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// Root of the game UI (GDD 15). Owns the HUD, the side panel and the dialogs, pauses the game
    /// while a dialog is open and shows short messages. Logic stays in GameRunner and Game.Core.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private GameRunner runner;
        [SerializeField, Min(0.5f)] private float toastSeconds = 4f;
        [SerializeField, Min(1)] private int maxToasts = 4;

        private readonly UiLocalizer _localizer = new UiLocalizer();
        private readonly List<IUiPanel> _panels = new List<IUiPanel>();
        private VisualElement _modalLayer;
        private VisualElement _toasts;
        private IUiPanel _openModal;
        private GameRunner.Speed _speedBeforeModal;

        public GameRunner Runner => runner;
        public GameSimulation Simulation => runner != null ? runner.Simulation : null;
        public GameDatabaseData Data => runner != null ? runner.Data : null;
        public VisualElement Root { get; private set; }
        public HudController Hud { get; private set; }
        public NewGameDialog NewGame { get; private set; }
        public DevPanel Dev { get; private set; }
        public bool IsModalOpen => _openModal != null;

        private void OnEnable()
        {
            if (runner == null)
            {
                Debug.LogError("[GameUI] No GameRunner assigned.", this);
                enabled = false;
                return;
            }
            Loc.Initialize();

            Root = GetComponent<UIDocument>().rootVisualElement;
            _modalLayer = Root.Q("modal-layer");
            _toasts = Root.Q("toasts");

            Hud = new HudController(this, Root.Q("hud"));
            _panels.Add(Hud);
            NewGame = new NewGameDialog(this, Root.Q("new-game-dialog"));
            _panels.Add(NewGame);
            Hud.NewGameClicked += NewGame.Open;
            Dev = new DevPanel(this, Root.Q("dev-panel"));
            _panels.Add(Dev);
            // Dialogs are still in the tree here, so one pass localizes them too.
            _localizer.Register(Root);

            Loc.LanguageChanged += OnLanguageChanged;
            runner.SimulationStarted += OnSimulationStarted;
            runner.WeekAdvanced += OnWeekAdvanced;
            runner.SpeedChanged += OnSpeedChanged;
            runner.Autosaved += OnAutosaved;
            runner.SalesEnded += OnSalesEnded;
            runner.TrendNewsAnnounced += OnTrendNews;
            RefreshAll();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= OnLanguageChanged;
            if (runner == null) return;
            runner.SimulationStarted -= OnSimulationStarted;
            runner.WeekAdvanced -= OnWeekAdvanced;
            runner.SpeedChanged -= OnSpeedChanged;
            runner.Autosaved -= OnAutosaved;
            runner.SalesEnded -= OnSalesEnded;
            runner.TrendNewsAnnounced -= OnTrendNews;
        }

        /// <summary>Adds a panel so it gets refreshed on weekly ticks and language changes.</summary>
        public void AddPanel(IUiPanel panel)
        {
            _panels.Add(panel);
            _localizer.Register(panel.Element);
            panel.Refresh();
        }

        /// <summary>Shows a dialog over the game and pauses time until it is closed.</summary>
        public void OpenModal(IUiPanel panel)
        {
            if (_openModal == panel) return;
            if (_openModal != null) CloseModal();
            _openModal = panel;
            _speedBeforeModal = runner.CurrentSpeed;
            runner.SetSpeed(GameRunner.Speed.Paused);
            _modalLayer.Add(panel.Element);
            _modalLayer.RemoveFromClassList("hidden");
            panel.Element.RemoveFromClassList("hidden");
            panel.Refresh();
        }

        public void CloseModal()
        {
            if (_openModal == null) return;
            _openModal.Element.RemoveFromHierarchy();
            _openModal = null;
            _modalLayer.AddToClassList("hidden");
            if (Simulation != null && !Simulation.IsGameOver) runner.SetSpeed(_speedBeforeModal);
            Hud.Refresh();
        }

        /// <summary>Shows a short message in the corner for a few seconds.</summary>
        public void Toast(string text)
        {
            var label = new Label(text);
            label.AddToClassList("toast");
            _toasts.Add(label);
            while (_toasts.childCount > maxToasts) _toasts.RemoveAt(0);
            label.schedule.Execute(() => label.RemoveFromHierarchy()).StartingIn((long)(toastSeconds * 1000));
        }

        public void RefreshAll()
        {
            foreach (var panel in _panels) panel.Refresh();
        }

        private void OnLanguageChanged()
        {
            _localizer.Apply();
            RefreshAll();
        }

        private void OnSimulationStarted(GameSimulation simulation)
        {
            CloseModal();
            RefreshAll();
        }

        private void OnWeekAdvanced() => RefreshAll();
        private void OnSpeedChanged(GameRunner.Speed speed) => Hud.Refresh();
        private void OnAutosaved(bool ok) => Toast(Loc.Get(ok ? "ui.msg.autosaved" : "ui.msg.save_failed"));

        private void OnSalesEnded(ReleasedGame game) =>
            Toast(Loc.Get("ui.msg.sales_ended", game.Concept.Name, game.UnitsSold));

        private void OnTrendNews(IReadOnlyList<TrendNews> news)
        {
            foreach (var item in news) Toast(Loc.News(item, Data));
        }
    }

    /// <summary>A part of the UI that redraws itself from the simulation state.</summary>
    public interface IUiPanel
    {
        VisualElement Element { get; }
        void Refresh();
    }
}
