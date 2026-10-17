using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// New game dialog (GDD 6.1, PLAN 3.3). Offers only what exists in the current year and shows
    /// what the player already knows, the costs and why the concept cannot start yet.
    /// </summary>
    public sealed class NewGameDialog : IUiPanel
    {
        private readonly GameUI _ui;
        private readonly TextField _name;
        private readonly DropdownField _genre;
        private readonly DropdownField _theme;
        private readonly DropdownField _platform;
        private readonly DropdownField _audience;
        private readonly DropdownField _engine;
        private readonly Label _combo;
        private readonly Label _platformInfo;
        private readonly Label _duration;
        private readonly Label _devKit;
        private readonly Label _engineCost;
        private readonly Label _paidNow;
        private readonly Label _license;
        private readonly VisualElement _errors;
        private readonly Button _start;

        // Ids behind the dropdown choices, in the same order.
        private List<string> _genreIds = new List<string>();
        private List<string> _themeIds = new List<string>();
        private List<string> _platformIds = new List<string>();
        private static readonly Audience[] Audiences = (Audience[])Enum.GetValues(typeof(Audience));
        private static readonly EngineChoice[] Engines = (EngineChoice[])Enum.GetValues(typeof(EngineChoice));

        public VisualElement Element { get; }

        /// <summary>Raised after development started from this dialog.</summary>
        public event Action<GameConcept> ProjectStarted;

        public NewGameDialog(GameUI ui, VisualElement element)
        {
            _ui = ui;
            Element = element;
            _name = element.Q<TextField>("ng-name");
            _genre = element.Q<DropdownField>("ng-genre");
            _theme = element.Q<DropdownField>("ng-theme");
            _platform = element.Q<DropdownField>("ng-platform");
            _audience = element.Q<DropdownField>("ng-audience");
            _engine = element.Q<DropdownField>("ng-engine");
            _combo = element.Q<Label>("ng-combo");
            _platformInfo = element.Q<Label>("ng-platform-info");
            _duration = element.Q<Label>("ng-duration");
            _devKit = element.Q<Label>("ng-devkit");
            _engineCost = element.Q<Label>("ng-engine-cost");
            _paidNow = element.Q<Label>("ng-paid-now");
            _license = element.Q<Label>("ng-license");
            _errors = element.Q("ng-errors");
            _start = element.Q<Button>("ng-start");

            _name.RegisterValueChangedCallback(_ => UpdateSummary());
            foreach (var dropdown in new[] { _genre, _theme, _platform, _audience, _engine })
                dropdown.RegisterValueChangedCallback(_ => UpdateSummary());
            element.Q<Button>("ng-cancel").clicked += _ui.CloseModal;
            _start.clicked += Start;
        }

        /// <summary>Opens the dialog with a clean form.</summary>
        public void Open()
        {
            _name.SetValueWithoutNotify("");
            foreach (var dropdown in new[] { _genre, _theme, _platform, _audience, _engine })
                dropdown.SetValueWithoutNotify(null);
            _ui.OpenModal(this);
        }

        /// <summary>Rebuilds the choices: content available this year, names in the current language.</summary>
        public void Refresh()
        {
            var sim = _ui.Simulation;
            var db = _ui.Data;
            if (sim == null || db == null) return;
            int year = sim.Date.Year;

            _genreIds = SetChoices(_genre, _genreIds, db.AvailableGenres(year).Select(g => g.Id), id => Loc.Genre(db, id));
            _themeIds = SetChoices(_theme, _themeIds, db.Themes.Where(t => t != null && t.IsAvailable(year)).Select(t => t.Id), id => Loc.Theme(db, id));
            _platformIds = SetChoices(_platform, _platformIds, db.AvailablePlatforms(year).Select(p => p.Id), id => Loc.Platform(db, id));
            SetChoices(_audience, Audiences, Audiences, a => Loc.Get(TextKeys.Audience(a)));
            SetChoices(_engine, Engines, Engines, e => Loc.Get(TextKeys.Engine(e)));
            if (_audience.index < 0) _audience.SetValueWithoutNotify(_audience.choices[Array.IndexOf(Audiences, Audience.Everyone)]);
            if (_engine.index < 0) _engine.SetValueWithoutNotify(_engine.choices[Array.IndexOf(Engines, EngineChoice.Custom)]);
            UpdateSummary();
        }

        /// <summary>The concept from the current form; ids are null where nothing is picked.</summary>
        public GameConcept BuildConcept()
        {
            return new GameConcept(_name.value?.Trim(), Pick(_genreIds, _genre), Pick(_themeIds, _theme), Pick(_platformIds, _platform),
                _audience.index >= 0 ? Audiences[_audience.index] : (Audience)(-1),
                _engine.index >= 0 ? Engines[_engine.index] : EngineChoice.Custom);
        }

        // Used by PlayMode tests to fill the form like a player would.
        public void SetName(string name) => _name.value = name;
        public void SelectGenre(string id) => _genre.index = _genreIds.IndexOf(id);
        public void SelectTheme(string id) => _theme.index = _themeIds.IndexOf(id);
        public void SelectPlatform(string id) => _platform.index = _platformIds.IndexOf(id);
        public IReadOnlyList<string> GenreChoices => _genreIds;
        public IReadOnlyList<string> ThemeChoices => _themeIds;
        public IReadOnlyList<string> PlatformChoices => _platformIds;
        public bool CanStart => _start.enabledSelf;

        public void Start()
        {
            var sim = _ui.Simulation;
            if (sim == null) return;
            var concept = BuildConcept();
            var errors = sim.StartProject(concept);
            if (errors.Count > 0)
            {
                ShowErrors(errors);
                return;
            }
            _ui.CloseModal();
            ProjectStarted?.Invoke(concept);
            _ui.RefreshAll();
        }

        private void UpdateSummary()
        {
            var sim = _ui.Simulation;
            var db = _ui.Data;
            if (sim == null || db == null) return;
            var concept = BuildConcept();

            string genreId = concept.GenreId, themeId = concept.ThemeId;
            if (genreId != null && themeId != null && sim.Knowledge.TryGetCombo(db, genreId, themeId, out var rating))
                _combo.text = Loc.Get("ui.newgame.combo_known", Loc.Get(TextKeys.Combo(rating)));
            else
                _combo.text = Loc.Get("ui.newgame.combo_unknown");

            var platform = db.GetPlatform(concept.PlatformId);
            var dev = db.Balance.Development;
            _duration.text = Loc.Get("ui.newgame.duration", dev.PreProductionWeeks + dev.ProductionWeeks + dev.FinishingWeeks);
            if (platform != null)
            {
                _platformInfo.text = Loc.Get("ui.newgame.platform_info", platform.InstallBaseMillions.Evaluate(sim.Date.Year));
                _devKit.text = sim.Studio.OwnsDevKit(platform.Id)
                    ? Loc.Get("ui.newgame.devkit_owned")
                    : Loc.Get("ui.newgame.devkit", Loc.Money(platform.DevKitCost));
                _license.text = Loc.Get("ui.newgame.license", Loc.Money(platform.LicenseFee));
            }
            _platformInfo.EnableInClassList("hidden", platform == null);
            _devKit.EnableInClassList("hidden", platform == null);
            _license.EnableInClassList("hidden", platform == null);

            bool licensed = concept.Engine == EngineChoice.Licensed;
            _engineCost.text = Loc.Get("ui.newgame.engine_cost", Loc.Money(dev.LicensedEngineCost));
            _engineCost.EnableInClassList("hidden", !licensed);
            _paidNow.text = Loc.Get("ui.newgame.paid_now", Loc.Money(ConceptValidator.UpfrontCost(concept, db, sim.Studio)));

            ShowErrors(sim.ValidateConcept(concept));
        }

        private void ShowErrors(List<ConceptError> errors)
        {
            _errors.Clear();
            foreach (var error in errors.Distinct()) _errors.Add(new Label(Loc.Get(TextKeys.ConceptError(error))));
            _start.SetEnabled(errors.Count == 0);
        }

        /// <summary>
        /// Fills a dropdown with names of the given ids and keeps the picked id selected, even when
        /// the language changed or the list changed with the year. Returns the new id list.
        /// </summary>
        private static List<T> SetChoices<T>(DropdownField field, IList<T> oldIds, IEnumerable<T> newIds, Func<T, string> name)
        {
            int oldIndex = field.index;
            bool hadPick = oldIndex >= 0 && oldIndex < oldIds.Count;
            T picked = hadPick ? oldIds[oldIndex] : default;
            var ids = newIds.ToList();
            field.choices = ids.Select(name).ToList();
            int index = hadPick ? ids.IndexOf(picked) : -1;
            field.SetValueWithoutNotify(index >= 0 ? field.choices[index] : null);
            return ids;
        }

        private static string Pick(List<string> ids, DropdownField field) =>
            field.index >= 0 && field.index < ids.Count ? ids[field.index] : null;
    }
}
