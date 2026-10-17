using System.Collections.Generic;
using Game.Core;
using UnityEngine.UIElements;

namespace Game.UI
{
    /// <summary>
    /// Side panel of the game in development (GDD 6.2, PLAN 3.4): phase, progress, bugs,
    /// sliders that split the work of each phase (always 100 %) and the QA length.
    /// </summary>
    public sealed class DevPanel : IUiPanel
    {
        private sealed class AreaRow
        {
            public DevArea Area;
            public Label Name;
            public Label Weight;
            public SliderInt Slider;
            public Label Value;
        }

        private sealed class PhaseSection
        {
            public DevPhase Phase;
            public VisualElement Root;
            public Label Title;
            public readonly List<AreaRow> Rows = new List<AreaRow>();
        }

        private readonly GameUI _ui;
        private readonly Label _name;
        private readonly Label _subtitle;
        private readonly Label _phase;
        private readonly ProgressBar _progress;
        private readonly Label _bugs;
        private readonly SliderInt _qa;
        private readonly Label _qaLabel;
        private readonly Label _qaHint;
        private readonly List<PhaseSection> _sections = new List<PhaseSection>();

        public VisualElement Element { get; }

        public DevPanel(GameUI ui, VisualElement element)
        {
            _ui = ui;
            Element = element;
            _name = element.Q<Label>("dev-name");
            _subtitle = element.Q<Label>("dev-subtitle");
            _phase = element.Q<Label>("dev-phase");
            _progress = element.Q<ProgressBar>("dev-progress");
            _bugs = element.Q<Label>("dev-bugs");
            _qa = element.Q<SliderInt>("dev-qa");
            _qaLabel = element.Q<Label>("dev-qa-label");
            _qaHint = element.Q<Label>("dev-qa-hint");
            _qa.RegisterValueChangedCallback(e => OnQaChanged(e.newValue));

            var phases = element.Q("dev-phases");
            foreach (var phase in DevPhases.WorkPhases)
            {
                var section = new PhaseSection { Phase = phase, Root = new VisualElement() };
                section.Root.AddToClassList("dev-phase");
                section.Title = new Label();
                section.Title.AddToClassList("strong");
                section.Root.Add(section.Title);
                var areas = DevPhases.AreasOf(phase);
                for (int i = 0; i < areas.Count; i++)
                {
                    int index = i;
                    var row = new AreaRow { Area = areas[i], Name = new Label(), Weight = new Label(), Slider = new SliderInt(0, 100), Value = new Label() };
                    var line = new VisualElement();
                    line.AddToClassList("dev-area");
                    row.Name.AddToClassList("dev-area-name");
                    row.Weight.AddToClassList("dev-area-weight");
                    row.Slider.AddToClassList("dev-area-slider");
                    row.Value.AddToClassList("dev-area-value");
                    row.Slider.RegisterValueChangedCallback(e => OnSliderChanged(section, index, e.newValue));
                    line.Add(row.Name);
                    line.Add(row.Weight);
                    line.Add(row.Slider);
                    line.Add(row.Value);
                    section.Root.Add(line);
                    section.Rows.Add(row);
                }
                phases.Add(section.Root);
                _sections.Add(section);
            }
        }

        public void Refresh()
        {
            var sim = _ui.Simulation;
            var project = sim?.ActiveProject;
            Element.EnableInClassList("hidden", project == null);
            if (project == null) return;
            var db = _ui.Data;
            var concept = project.Concept;

            _name.text = concept.Name;
            _subtitle.text = Loc.Get("ui.dev.subtitle", Loc.Genre(db, concept.GenreId), Loc.Theme(db, concept.ThemeId), Loc.Platform(db, concept.PlatformId));
            _phase.text = Loc.Get("ui.dev.phase", Loc.Get(TextKeys.Phase(project.Phase)));
            _progress.value = project.TotalWeeks > 0 ? (float)project.WeeksWorked / project.TotalWeeks : 0f;
            _progress.title = Loc.Get("ui.dev.progress", project.WeeksWorked, project.TotalWeeks);
            _bugs.text = Loc.Get("ui.dev.bugs", project.Bugs);

            foreach (var section in _sections)
            {
                bool done = section.Phase < project.Phase;
                string phaseName = Loc.Get(TextKeys.Phase(section.Phase));
                section.Title.text = done ? Loc.Get("ui.dev.phase_done", phaseName)
                    : section.Phase == project.Phase ? Loc.Get("ui.dev.phase_now", phaseName) : phaseName;
                section.Root.SetEnabled(!done);
                section.Root.EnableInClassList("current", section.Phase == project.Phase);
                foreach (var row in section.Rows)
                {
                    int percent = project.GetAllocation(row.Area);
                    row.Name.text = Loc.Get(TextKeys.Area(row.Area));
                    row.Weight.text = Loc.WeightDots(sim.Knowledge.GetWeightLevel(db, concept.GenreId, row.Area));
                    row.Slider.SetValueWithoutNotify(percent);
                    row.Value.text = Loc.Get("ui.dev.percent", percent);
                }
            }

            bool qaOpen = project.Phase < DevPhase.QA;
            _qa.highValue = project.MaxQaWeeks;
            _qa.SetValueWithoutNotify(project.QaWeeks);
            _qa.SetEnabled(qaOpen && project.MaxQaWeeks > 0);
            _qaLabel.text = Loc.Get("ui.dev.qa", project.QaWeeks);
            _qaHint.text = Loc.Get(qaOpen ? "ui.dev.qa_hint" : "ui.dev.qa_locked");
        }

        private void OnSliderChanged(PhaseSection section, int index, int value)
        {
            var project = _ui.Simulation?.ActiveProject;
            if (project == null || section.Phase < project.Phase) return;
            var split = DevPhases.Rebalance(project.GetAllocation(section.Phase), index, value);
            project.SetAllocation(section.Phase, split);
            Refresh();
        }

        private void OnQaChanged(int weeks)
        {
            var project = _ui.Simulation?.ActiveProject;
            if (project == null || project.Phase >= DevPhase.QA) return;
            project.SetQaWeeks(System.Math.Max(0, System.Math.Min(weeks, project.MaxQaWeeks)));
            Refresh();
        }
    }
}
