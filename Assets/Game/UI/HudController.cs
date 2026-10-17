using Game.Core;
using Game.Runtime;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>Top bar (PLAN 3.2): date, money, fans, speed, game actions and language.</summary>
    public sealed class HudController : IUiPanel
    {
        private readonly GameUI _ui;
        private readonly Label _date;
        private readonly Label _money;
        private readonly Label _fans;
        private readonly Label _paused;
        private readonly Button[] _speedButtons = new Button[4];
        private readonly Button _newGame;
        private readonly Button _save;
        private readonly Button _load;
        private readonly Button _langCs;
        private readonly Button _langEn;

        public VisualElement Element { get; }

        /// <summary>Raised by the New game button; the dialog subscribes to it.</summary>
        public event System.Action NewGameClicked;
        public event System.Action GamesClicked;
        public event System.Action EncyclopediaClicked;

        public HudController(GameUI ui, VisualElement element)
        {
            _ui = ui;
            Element = element;
            _date = element.Q<Label>("hud-date");
            _money = element.Q<Label>("hud-money");
            _fans = element.Q<Label>("hud-fans");
            _paused = element.Q<Label>("hud-paused");
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                var speed = (GameRunner.Speed)i;
                _speedButtons[i] = element.Q<Button>("speed-" + i);
                _speedButtons[i].clicked += () => _ui.Runner.SetSpeed(speed);
            }

            _newGame = element.Q<Button>("btn-new-game");
            _newGame.clicked += () => NewGameClicked?.Invoke();
            element.Q<Button>("btn-games").clicked += () => GamesClicked?.Invoke();
            element.Q<Button>("btn-encyclopedia").clicked += () => EncyclopediaClicked?.Invoke();
            _save = element.Q<Button>("btn-save");
            _save.clicked += OnSave;
            _load = element.Q<Button>("btn-load");
            _load.clicked += OnLoad;
            _langCs = element.Q<Button>("lang-cs");
            _langCs.clicked += () => Loc.SetLanguage("cs");
            _langEn = element.Q<Button>("lang-en");
            _langEn.clicked += () => Loc.SetLanguage("en");
        }

        public void Refresh()
        {
            var sim = _ui.Simulation;
            if (sim == null) return;
            _date.text = Loc.Date(sim.Date);
            _money.text = Loc.Money(sim.Studio.Money);
            _money.EnableInClassList("bad", sim.Studio.Money < 0);
            _fans.text = Loc.Get("ui.hud.fans", sim.Studio.Fans);

            var speed = _ui.Runner.CurrentSpeed;
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                _speedButtons[i].EnableInClassList("selected", (int)speed == i);
                _speedButtons[i].SetEnabled(!sim.IsGameOver && !_ui.IsModalOpen);
            }
            _paused.EnableInClassList("hidden", speed != GameRunner.Speed.Paused);

            _newGame.SetEnabled(!sim.IsGameOver && sim.ActiveProject == null);
            _save.SetEnabled(!sim.IsGameOver);
            _load.SetEnabled(_ui.Runner.HasSave);

            string lang = Loc.CurrentLanguage;
            _langCs.EnableInClassList("selected", lang == "cs");
            _langEn.EnableInClassList("selected", lang == "en");
        }

        private void OnSave()
        {
            _ui.Toast(Loc.Get(_ui.Runner.Save() ? "ui.msg.saved" : "ui.msg.save_failed"));
            Refresh();
        }

        private void OnLoad()
        {
            if (!_ui.Runner.HasSave)
            {
                _ui.Toast(Loc.Get("ui.msg.no_save"));
                return;
            }
            switch (_ui.Runner.Load())
            {
                case SaveLoadError.None: _ui.Toast(Loc.Get("ui.msg.loaded")); break;
                case SaveLoadError.UnsupportedVersion: _ui.Toast(Loc.Get("ui.msg.load_old_version")); break;
                default: _ui.Toast(Loc.Get("ui.msg.load_corrupt")); break;
            }
        }
    }
}
