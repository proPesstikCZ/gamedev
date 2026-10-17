using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// Studio encyclopedia (GDD 8): what is known about each genre, its area weights
    /// (●○○ / ●●○ / ●●●) and its match with every theme.
    /// </summary>
    public sealed class EncyclopediaDialog : IUiPanel
    {
        private readonly GameUI _ui;
        private readonly DropdownField _genre;
        private readonly Label _weightsCount;
        private readonly VisualElement _weights;
        private readonly VisualElement _combos;
        private List<string> _genreIds = new List<string>();

        public VisualElement Element { get; }

        public EncyclopediaDialog(GameUI ui, VisualElement element)
        {
            _ui = ui;
            Element = element;
            _genre = element.Q<DropdownField>("en-genre");
            _weightsCount = element.Q<Label>("en-weights-count");
            _weights = element.Q("en-weights");
            _combos = element.Q("en-combos");
            _genre.RegisterValueChangedCallback(_ => ShowGenre());
            element.Q<Button>("en-close").clicked += _ui.CloseModal;
        }

        public void Open() => _ui.OpenModal(this);

        public void Refresh()
        {
            var sim = _ui.Simulation;
            if (sim == null || !_ui.IsOpen(this)) return;
            var db = _ui.Data;
            string picked = _genre.index >= 0 && _genre.index < _genreIds.Count ? _genreIds[_genre.index] : null;
            _genreIds = db.AvailableGenres(sim.Date.Year).Select(g => g.Id).ToList();
            _genre.choices = _genreIds.Select(id => Loc.Genre(db, id)).ToList();
            int index = picked != null ? _genreIds.IndexOf(picked) : -1;
            if (index < 0) index = MostKnownGenre(sim, db);
            _genre.SetValueWithoutNotify(index >= 0 ? _genre.choices[index] : null);
            ShowGenre();
        }

        private void ShowGenre()
        {
            var sim = _ui.Simulation;
            var db = _ui.Data;
            _weights.Clear();
            _combos.Clear();
            if (sim == null || _genre.index < 0 || _genre.index >= _genreIds.Count) return;
            string genreId = _genreIds[_genre.index];

            int known = 0;
            for (int i = 0; i < DevAreas.Count; i++)
            {
                var area = (DevArea)i;
                var level = sim.Knowledge.GetWeightLevel(db, genreId, area);
                if (level != WeightLevel.Unknown) known++;
                Row(_weights, Loc.Get(TextKeys.Area(area)), Loc.WeightDots(level));
            }
            _weightsCount.text = Loc.Get("ui.encyclopedia.weights_count", known, DevAreas.Count);

            foreach (var theme in db.Themes)
            {
                if (theme == null) continue;
                string rating = sim.Knowledge.TryGetCombo(db, genreId, theme.Id, out var combo)
                    ? Loc.Get(TextKeys.Combo(combo))
                    : Loc.Get("ui.encyclopedia.unknown");
                Row(_combos, Loc.Get(theme.NameKey), rating);
            }
        }

        /// <summary>The genre the player knows most about, so the first view is not empty.</summary>
        private int MostKnownGenre(GameSimulation sim, GameDatabaseData db)
        {
            int best = _genreIds.Count > 0 ? 0 : -1, bestKnown = 0;
            for (int g = 0; g < _genreIds.Count; g++)
            {
                int known = 0;
                for (int i = 0; i < DevAreas.Count; i++)
                    if (sim.Knowledge.IsWeightRevealed(_genreIds[g], (DevArea)i)) known++;
                if (known > bestKnown)
                {
                    best = g;
                    bestKnown = known;
                }
            }
            return best;
        }

        private static void Row(VisualElement parent, string name, string value)
        {
            var line = new VisualElement();
            line.AddToClassList("row-line");
            line.Add(new Label(name));
            var v = new Label(value);
            v.AddToClassList("accent-text");
            line.Add(v);
            parent.Add(line);
        }
    }
}
