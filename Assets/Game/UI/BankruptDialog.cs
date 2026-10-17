using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>Game over screen (GDD 14). Cannot be closed: the player starts over or loads.</summary>
    public sealed class BankruptDialog : IUiPanel
    {
        private readonly GameUI _ui;
        private readonly Label _text;
        private readonly Button _load;

        public VisualElement Element { get; }

        public BankruptDialog(GameUI ui, VisualElement element)
        {
            _ui = ui;
            Element = element;
            _text = element.Q<Label>("bk-text");
            _load = element.Q<Button>("bk-load");
            _load.clicked += () => _ui.Hud.LoadFromSlot();
            element.Q<Button>("bk-restart").clicked += () => _ui.Runner.NewGame();
        }

        public void Open() => _ui.OpenModal(this);

        public void Refresh()
        {
            var sim = _ui.Simulation;
            if (sim == null || !_ui.IsOpen(this)) return;
            _text.text = Loc.Get("ui.bankrupt.text", sim.ReleasedGames.Count, sim.Studio.Fans);
            _load.SetEnabled(_ui.Runner.HasSave);
        }
    }
}
