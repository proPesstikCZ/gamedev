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
        public ResultsDialog Results { get; private set; }
        public GamesDialog Games { get; private set; }
        public EncyclopediaDialog Encyclopedia { get; private set; }
        public BankruptDialog Bankrupt { get; private set; }
        public bool IsModalOpen => _openModal != null;
        public bool IsOpen(IUiPanel panel) => panel != null && _openModal == panel;

        /// <summary>True once localization is loaded and the panels are built.</summary>
        public bool IsReady { get; private set; }

        private void OnEnable()
        {
            if (runner == null)
            {
                Debug.LogError("[GameUI] No GameRunner assigned.", this);
                enabled = false;
                return;
            }
            // Wait for the tables without blocking, then build the UI once.
            var init = UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation;
            if (init.IsDone) OnLocalizationReady();
            else init.Completed += _ => OnLocalizationReady();
        }

        private void OnLocalizationReady()
        {
            if (!isActiveAndEnabled || IsReady) return;
            Loc.Initialize();
            if (Hud == null) Build();
            Bind(true);
            IsReady = true;
            RefreshAll();
            if (Simulation != null && Simulation.IsGameOver) Bankrupt.Open();
            ReportStartupLoad();
        }

        /// <summary>Tells the player when the save could not be continued at startup.</summary>
        private void ReportStartupLoad()
        {
            if (!(runner.StartupLoadResult is SaveLoadError result) || result == SaveLoadError.None) return;
            Toast(() => Loc.Get(HudController.LoadMessageKey(result)));
            if (!runner.AutosaveAllowed) Toast(() => Loc.Get("ui.msg.autosave_held"));
        }

        private void OnDisable()
        {
            if (!IsReady) return;
            Bind(false);
            IsReady = false;
        }

        private void Build()
        {
            Root = GetComponent<UIDocument>().rootVisualElement;
            _modalLayer = Root.Q("modal-layer");
            _toasts = Root.Q("toasts");

            Hud = new HudController(this, Root.Q("hud"));
            NewGame = new NewGameDialog(this, Root.Q("new-game-dialog"));
            Dev = new DevPanel(this, Root.Q("dev-panel"));
            Results = new ResultsDialog(this, Root.Q("results-dialog"));
            Games = new GamesDialog(this, Root.Q("games-dialog"));
            Encyclopedia = new EncyclopediaDialog(this, Root.Q("encyclopedia-dialog"));
            Bankrupt = new BankruptDialog(this, Root.Q("bankrupt-dialog"));
            _panels.AddRange(new IUiPanel[] { Hud, NewGame, Dev, Results, Games, Encyclopedia, Bankrupt });
            Hud.NewGameClicked += NewGame.Open;
            Hud.GamesClicked += Games.Open;
            Hud.EncyclopediaClicked += Encyclopedia.Open;
            // Dialogs are still in the tree here, so one pass localizes them too.
            _localizer.Register(Root);
        }

        private void Bind(bool on)
        {
            if (on)
            {
                Loc.LanguageChanged += OnLanguageChanged;
                runner.SimulationStarted += OnSimulationStarted;
                runner.WeekAdvanced += OnWeekAdvanced;
                runner.SpeedChanged += OnSpeedChanged;
                runner.Autosaved += OnAutosaved;
                runner.SalesEnded += OnSalesEnded;
                runner.TrendNewsAnnounced += OnTrendNews;
                runner.GameReleased += OnGameReleased;
                runner.Bankrupted += OnBankrupted;
            }
            else
            {
                Loc.LanguageChanged -= OnLanguageChanged;
                runner.SimulationStarted -= OnSimulationStarted;
                runner.WeekAdvanced -= OnWeekAdvanced;
                runner.SpeedChanged -= OnSpeedChanged;
                runner.Autosaved -= OnAutosaved;
                runner.SalesEnded -= OnSalesEnded;
                runner.TrendNewsAnnounced -= OnTrendNews;
                runner.GameReleased -= OnGameReleased;
                runner.Bankrupted -= OnBankrupted;
            }
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
        /// <param name="text">Builds the localized text; called again when the language changes.</param>
        public void Toast(System.Func<string> text)
        {
            var label = new Label(text());
            label.userData = text;
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
            foreach (var child in _toasts.Children())
                if (child is Label label && label.userData is System.Func<string> text) label.text = text();
            RefreshAll();
        }

        private void OnSimulationStarted(GameSimulation simulation)
        {
            CloseModal();
            RefreshAll();
            if (simulation.IsGameOver) Bankrupt.Open();
        }

        private void OnGameReleased(ReleasedGame game) => Results.Open(game);
        private void OnBankrupted() => Bankrupt.Open();

        private void OnWeekAdvanced() => RefreshAll();
        private void OnSpeedChanged(GameRunner.Speed speed) => Hud.Refresh();
        // Autosave runs every month, so only a failure is worth a message.
        private void OnAutosaved(bool ok)
        {
            if (!ok) Toast(() => Loc.Get("ui.msg.save_failed"));
        }

        private void OnSalesEnded(ReleasedGame game) =>
            Toast(() => Loc.Get("ui.msg.sales_ended", game.Concept.Name, game.UnitsSold));

        private void OnTrendNews(IReadOnlyList<TrendNews> news)
        {
            foreach (var item in news) Toast(() => Loc.News(item, Data));
        }
    }

    /// <summary>A part of the UI that redraws itself from the simulation state.</summary>
    public interface IUiPanel
    {
        VisualElement Element { get; }
        void Refresh();
    }
}
