using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>List of released games, newest first. A row opens the game's results.</summary>
    public sealed class GamesDialog : IUiPanel
    {
        private readonly GameUI _ui;
        private readonly ScrollView _list;
        private readonly Label _empty;

        public VisualElement Element { get; }

        public GamesDialog(GameUI ui, VisualElement element)
        {
            _ui = ui;
            Element = element;
            _list = element.Q<ScrollView>("gm-list");
            _empty = element.Q<Label>("gm-empty");
            element.Q<Button>("gm-close").clicked += _ui.CloseModal;
        }

        public void Open() => _ui.OpenModal(this);

        public void Refresh()
        {
            var sim = _ui.Simulation;
            if (sim == null || !_ui.IsOpen(this)) return;
            var db = _ui.Data;
            _list.Clear();
            var games = sim.ReleasedGames;
            _empty.EnableInClassList("hidden", games.Count > 0);
            for (int i = games.Count - 1; i >= 0; i--)
            {
                var game = games[i];
                string text = Loc.Get("ui.games.row", game.Concept.Name, Loc.Genre(db, game.Concept.GenreId), game.Review.Review, game.UnitsSold);
                if (game.OnSale) text += " · " + Loc.Get("ui.games.on_sale");
                var row = new Button(() => _ui.Results.Open(game)) { text = text };
                row.AddToClassList("game-row");
                _list.Add(row);
            }
        }
    }
}
