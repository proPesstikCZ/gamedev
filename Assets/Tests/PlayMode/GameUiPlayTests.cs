using System.Collections;
using System.IO;
using System.Linq;
using Game.Core;
using Game.Runtime;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Game.Tests
{
    /// <summary>M3: the game scene with UI, driven like a player would drive it.</summary>
    public class GameUiPlayTests
    {
        private const string SceneName = "SampleScene";
        private const string TestSaveFile = "playmode_test_save.json";

        private string _savedLanguage;
        private GameUI _ui;
        private GameRunner _runner;

        private static string TestSavePath => Path.Combine(Application.persistentDataPath, TestSaveFile);

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedLanguage = PlayerPrefs.GetString(Loc.LanguagePrefKey, "");
            // Set before the scene loads: the runner reads the slot in Awake. Never touch the player's save.
            GameRunner.SaveFileNameOverride = TestSaveFile;
            if (File.Exists(TestSavePath)) File.Delete(TestSavePath);
            yield return LoadGameScene();
        }

        [TearDown]
        public void CleanUp()
        {
            if (File.Exists(TestSavePath)) File.Delete(TestSavePath);
            GameRunner.SaveFileNameOverride = null;
            if (string.IsNullOrEmpty(_savedLanguage)) PlayerPrefs.DeleteKey(Loc.LanguagePrefKey);
            else PlayerPrefs.SetString(Loc.LanguagePrefKey, _savedLanguage);
        }

        /// <summary>Loads the scene like a game start. Paused by default so the test controls time.</summary>
        private IEnumerator LoadGameScene(bool pause = true)
        {
            SceneManager.LoadScene(SceneName);
            yield return null;
            _ui = Object.FindAnyObjectByType<GameUI>();
            _runner = Object.FindAnyObjectByType<GameRunner>();
            Assert.That(_ui, Is.Not.Null, "No GameUI in the scene.");
            Assert.That(_runner, Is.Not.Null, "No GameRunner in the scene.");
            for (int i = 0; i < 300 && !_ui.IsReady; i++) yield return null;
            Assert.That(_ui.IsReady, Is.True, "UI did not finish loading.");
            Assert.That(_runner.SavePath, Is.EqualTo(TestSavePath));
            if (pause) _runner.SetSpeed(GameRunner.Speed.Paused);
        }

        /// <summary>Lets real time run at the fastest speed until a new month starts.</summary>
        private IEnumerator RunUntilNextMonth()
        {
            int month = _runner.Simulation.Date.Month;
            _runner.SetSpeed(GameRunner.Speed.Fastest);
            float end = Time.realtimeSinceStartup + 15f;
            while (_runner.Simulation.Date.Month == month && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(_runner.Simulation.Date.Month, Is.Not.EqualTo(month), "Time did not run.");
            _runner.SetSpeed(GameRunner.Speed.Paused);
        }

        private string[] ToastTexts() => _ui.Root.Q("toasts").Query<Label>().ToList().Select(l => l.text).ToArray();

        private IEnumerator SetLanguage(string code)
        {
            bool switching = Loc.CurrentLanguage != code;
            bool changed = false;
            void OnChanged() => changed = true;
            Loc.LanguageChanged += OnChanged;
            Loc.SetLanguage(code);
            // The UI updates when the new tables are ready, which can take a few frames.
            for (int i = 0; i < 300 && switching && !changed; i++) yield return null;
            Loc.LanguageChanged -= OnChanged;
            yield return null;
            Assert.That(Loc.CurrentLanguage, Is.EqualTo(code));
            Assert.That(changed || !switching, Is.True, "LanguageChanged was not raised.");
        }

        /// <summary>Fills the new game dialog with the first offered choices and starts development.</summary>
        private GameConcept StartGameFromDialog(string name)
        {
            _ui.NewGame.Open();
            Assert.That(_ui.IsOpen(_ui.NewGame), Is.True);
            _ui.NewGame.SetName(name);
            _ui.NewGame.SelectGenre(_ui.NewGame.GenreChoices[0]);
            _ui.NewGame.SelectTheme(_ui.NewGame.ThemeChoices[0]);
            _ui.NewGame.SelectPlatform(_ui.NewGame.PlatformChoices[0]);
            var concept = _ui.NewGame.BuildConcept();
            Assert.That(_ui.NewGame.CanStart, Is.True, "Start is disabled for a complete concept.");
            _ui.NewGame.Start();
            return concept;
        }

        [UnityTest]
        public IEnumerator NewGameDialog_CreatesValidConcept()
        {
            _runner.NewGame(11);
            yield return null;

            var concept = StartGameFromDialog("Test Game");

            Assert.That(_runner.Simulation.ActiveProject, Is.Not.Null);
            var started = _runner.Simulation.ActiveProject.Concept;
            Assert.That(started.Name, Is.EqualTo("Test Game"));
            Assert.That(started.GenreId, Is.EqualTo(concept.GenreId));
            Assert.That(started.ThemeId, Is.EqualTo(concept.ThemeId));
            Assert.That(started.PlatformId, Is.EqualTo(concept.PlatformId));
            int year = _runner.Simulation.Date.Year;
            Assert.That(_runner.Data.GetGenre(started.GenreId).IsAvailable(year), Is.True);
            Assert.That(_runner.Data.GetPlatform(started.PlatformId).IsAvailable(year), Is.True);
            Assert.That(_ui.IsModalOpen, Is.False, "Dialog stays open after start.");
        }

        [UnityTest]
        public IEnumerator NewGameDialog_OffersOnlyContentOfTheCurrentYear()
        {
            _runner.NewGame(3);
            yield return null;
            _ui.NewGame.Open();
            int year = _runner.Simulation.Date.Year;
            Assert.That(_ui.NewGame.GenreChoices.All(id => _runner.Data.GetGenre(id).IsAvailable(year)), Is.True);
            Assert.That(_ui.NewGame.PlatformChoices.All(id => _runner.Data.GetPlatform(id).IsAvailable(year)), Is.True);
            Assert.That(_ui.NewGame.GenreChoices, Does.Not.Contain("fps"));
            Assert.That(_ui.NewGame.CanStart, Is.False, "Empty form must not start.");
        }

        [UnityTest]
        public IEnumerator DevSliders_AlwaysSumTo100()
        {
            _runner.NewGame(5);
            yield return null;
            StartGameFromDialog("Slider Test");
            var sliders = _ui.Dev.Element.Query<SliderInt>(className: "dev-area-slider").ToList();
            Assert.That(sliders.Count, Is.EqualTo(DevAreas.Count));

            var project = _runner.Simulation.ActiveProject;
            int[] values = { 90, 0, 45, 100, 7 };
            foreach (int v in values)
            {
                sliders[0].value = v;                 // pre-production
                sliders[4].value = 100 - v;           // production
                foreach (var phase in DevPhases.WorkPhases)
                    Assert.That(project.GetAllocation(phase).Sum(), Is.EqualTo(100), $"{phase} after {v}");
            }
        }

        [UnityTest]
        public IEnumerator FullCycle_WorksInBothLanguages([Values("cs", "en")] string language)
        {
            yield return SetLanguage(language);
            _runner.NewGame(21);
            yield return null;
            StartGameFromDialog(language == "cs" ? "Příliš žluťoučký kůň" : "Test Game");

            for (int i = 0; i < 40 && _runner.Simulation.ActiveProject != null; i++) _runner.Step();
            Assert.That(_runner.Simulation.ActiveProject, Is.Null, "The game was not released.");
            Assert.That(_ui.IsOpen(_ui.Results), Is.True, "Results did not open after release.");

            for (int i = 0; i < 3; i++) _runner.Step();
            yield return null;

            var texts = _ui.Results.Element.Query<Label>().ToList().Select(l => l.text).Where(t => !string.IsNullOrEmpty(t)).ToList();
            Assert.That(_ui.Results.Element.Q("rs-postmortem").childCount, Is.GreaterThan(0), "Post-mortem is empty.");
            foreach (var text in texts)
            {
                Assert.That(text, Does.Not.StartWith("#"), "Untranslated UXML key.");
                Assert.That(text, Does.Not.Contain("No translation found"), "Missing translation.");
            }

            _ui.CloseModal();
            _ui.Encyclopedia.Open();
            yield return null;
            Assert.That(_ui.Encyclopedia.Element.Q("en-weights").childCount, Is.EqualTo(DevAreas.Count));
        }

        [UnityTest]
        public IEnumerator SaveRestartLoad_RestoresDateMoneyFansAndProject()
        {
            _runner.NewGame(42);
            yield return null;
            StartGameFromDialog("Saved Game");
            for (int i = 0; i < 5; i++) _runner.Step();

            var sim = _runner.Simulation;
            var date = sim.Date;
            long money = sim.Studio.Money;
            int fans = sim.Studio.Fans;
            var project = sim.ActiveProject;
            string name = project.Concept.Name;
            var phase = project.Phase;
            int weeks = project.WeeksWorked;
            float bugs = project.Bugs;
            Assert.That(_runner.Save(), Is.True);

            // "Restart": a fresh scene continues the saved game on its own.
            yield return LoadGameScene();
            Assert.That(_runner.StartupLoadResult, Is.EqualTo(SaveLoadError.None));
            Assert.That(_runner.Simulation.ActiveProject?.Concept.Name, Is.EqualTo(name), "Startup did not continue the save.");
            // Loading by hand gives the same state.
            Assert.That(_runner.Load(), Is.EqualTo(SaveLoadError.None));
            yield return null;

            sim = _runner.Simulation;
            Assert.That(sim.Date, Is.EqualTo(date));
            Assert.That(sim.Studio.Money, Is.EqualTo(money));
            Assert.That(sim.Studio.Fans, Is.EqualTo(fans));
            Assert.That(sim.ActiveProject, Is.Not.Null);
            Assert.That(sim.ActiveProject.Concept.Name, Is.EqualTo(name));
            Assert.That(sim.ActiveProject.Phase, Is.EqualTo(phase));
            Assert.That(sim.ActiveProject.WeeksWorked, Is.EqualTo(weeks));
            Assert.That(sim.ActiveProject.Bugs, Is.EqualTo(bugs));
            Assert.That(_ui.Dev.Element.ClassListContains("hidden"), Is.False, "Dev panel is hidden after load.");
        }

        [UnityTest]
        public IEnumerator LoadingOldSave_ShowsMessageAndKeepsTheGame()
        {
            _runner.NewGame(8);
            yield return null;
            for (int i = 0; i < 3; i++) _runner.Step();
            var before = _runner.Simulation;
            File.WriteAllText(_runner.SavePath, "{\"Version\":1,\"StartYear\":1972,\"TotalWeeks\":3,\"Money\":5}");

            _ui.Hud.LoadFromSlot();
            yield return null;

            Assert.That(_runner.Simulation, Is.SameAs(before), "The running game was replaced.");
            var toasts = _ui.Root.Q("toasts").Query<Label>().ToList().Select(l => l.text).ToList();
            Assert.That(toasts, Does.Contain(Loc.Get("ui.msg.load_old_version")));
        }

        [UnityTest]
        public IEnumerator Autosave_WritesTheSlotAtMonthStart()
        {
            _runner.NewGame(9);
            yield return null;
            if (File.Exists(_runner.SavePath)) File.Delete(_runner.SavePath);
            int month = _runner.Simulation.Date.Month;
            while (_runner.Simulation.Date.Month == month) _runner.Step();
            Assert.That(_runner.HasSave, Is.True, "No autosave at month start.");
            var save = SaveSerializer.TryFromJson(File.ReadAllText(_runner.SavePath), out var error);
            Assert.That(error, Is.EqualTo(SaveLoadError.None));
            Assert.That(save.TotalWeeks, Is.EqualTo(_runner.Simulation.Clock.TotalWeeks));
        }

        [UnityTest]
        public IEnumerator Restart_WithSaveAndRunningTime_ContinuesTheSaveInsteadOfOverwritingIt()
        {
            _runner.NewGame(42);
            yield return null;
            StartGameFromDialog("Saved Game");
            for (int i = 0; i < 5; i++) _runner.Step();
            int savedWeeks = _runner.Simulation.Clock.TotalWeeks;
            Assert.That(_runner.Save(), Is.True);

            // Restart without pausing: time and autosave run from the first frame, as for a player.
            yield return LoadGameScene(pause: false);
            Assert.That(_runner.CurrentSpeed, Is.Not.EqualTo(GameRunner.Speed.Paused));
            yield return RunUntilNextMonth();

            var save = SaveSerializer.TryFromJson(File.ReadAllText(_runner.SavePath), out var error);
            Assert.That(error, Is.EqualTo(SaveLoadError.None));
            Assert.That(save.HasActiveProject, Is.True, "Autosave replaced the save with a new game.");
            Assert.That(save.ActiveProject.Concept.Name, Is.EqualTo("Saved Game"));
            Assert.That(save.TotalWeeks, Is.GreaterThan(savedWeeks), "Autosave did not continue the loaded game.");
        }

        [UnityTest]
        public IEnumerator Restart_WithUnloadableSave_KeepsTheFileUntilThePlayerDecides()
        {
            _runner.NewGame(42);
            yield return null;
            var concept = StartGameFromDialog("Broken Game");
            Assert.That(_runner.Save(), Is.True);
            // Structurally fine, but the genre does not exist in the content.
            string good = File.ReadAllText(_runner.SavePath);
            string from = $"\"genreId\": \"{concept.GenreId}\"";
            Assert.That(good, Does.Contain(from), "Test setup: JSON format changed.");
            string broken = good.Replace(from, "\"genreId\": \"no_such_genre\"");
            File.WriteAllText(_runner.SavePath, broken);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save does not fit the content"));
            yield return LoadGameScene(pause: false);
            Assert.That(_runner.StartupLoadResult, Is.EqualTo(SaveLoadError.Corrupt));
            Assert.That(_runner.Simulation.ActiveProject, Is.Null, "The broken save replaced the game.");
            Assert.That(_runner.AutosaveAllowed, Is.False);
            Assert.That(ToastTexts(), Does.Contain(Loc.Get("ui.msg.load_corrupt")));
            Assert.That(ToastTexts(), Does.Contain(Loc.Get("ui.msg.autosave_held")));

            yield return RunUntilNextMonth();
            Assert.That(File.ReadAllText(_runner.SavePath), Is.EqualTo(broken), "Autosave overwrote the save.");

            // Saving by hand is the player's choice and turns autosave back on.
            Assert.That(_runner.Save(), Is.True);
            Assert.That(_runner.AutosaveAllowed, Is.True);
        }

        [UnityTest]
        public IEnumerator SwitchingLanguage_UpdatesOpenFormAndKeepsTheChoices()
        {
            yield return SetLanguage("en");
            _runner.NewGame(13);
            yield return null;
            _ui.NewGame.Open();
            _ui.NewGame.SetName("Kept Name");
            _ui.NewGame.SelectGenre(_ui.NewGame.GenreChoices[0]);
            _ui.NewGame.SelectTheme(_ui.NewGame.ThemeChoices[0]);
            _ui.NewGame.SelectPlatform(_ui.NewGame.PlatformChoices[0]);
            var before = _ui.NewGame.BuildConcept();
            _ui.Hud.LoadFromSlot(); // no save yet: shows a message
            yield return null;

            var form = _ui.NewGame.Element;
            var title = form.Q<Label>(className: "modal-title");
            var genre = form.Q<DropdownField>("ng-genre");
            string enTitle = title.text, enGenreLabel = genre.label, enGenre = genre.value;
            string enToast = Loc.Get("ui.msg.no_save");
            Assert.That(ToastTexts(), Does.Contain(enToast));

            yield return SetLanguage("cs");

            Assert.That(_ui.IsOpen(_ui.NewGame), Is.True, "Switching language closed the form.");
            Assert.That(title.text, Is.EqualTo(Loc.Get("ui.newgame.title")).And.Not.EqualTo(enTitle));
            Assert.That(genre.label, Is.EqualTo(Loc.Get("ui.newgame.genre")).And.Not.EqualTo(enGenreLabel));
            Assert.That(genre.value, Is.EqualTo(Loc.Genre(_runner.Data, before.GenreId)).And.Not.EqualTo(enGenre));
            var after = _ui.NewGame.BuildConcept();
            Assert.That(after.Name, Is.EqualTo(before.Name));
            Assert.That(after.GenreId, Is.EqualTo(before.GenreId));
            Assert.That(after.ThemeId, Is.EqualTo(before.ThemeId));
            Assert.That(after.PlatformId, Is.EqualTo(before.PlatformId));
            Assert.That(after.Audience, Is.EqualTo(before.Audience));
            Assert.That(after.Engine, Is.EqualTo(before.Engine));
            Assert.That(ToastTexts(), Does.Contain(Loc.Get("ui.msg.no_save")).And.Not.Contain(enToast), "Message stayed in English.");
            foreach (var text in _ui.Root.Query<TextElement>().ToList().Select(t => t.text))
                Assert.That(text ?? "", Does.Not.StartWith("#"), "Untranslated UXML key after switch.");
        }

        [UnityTest]
        public IEnumerator CzechTexts_FitTheLayout()
        {
            yield return SetLanguage("cs");
            _runner.NewGame(17);
            yield return null;
            _ui.NewGame.Open();
            _ui.NewGame.SetName("Příliš žluťoučký kůň úpěl ďábelské ódy");
            _ui.NewGame.SelectGenre(_ui.NewGame.GenreChoices[0]);
            _ui.NewGame.SelectTheme(_ui.NewGame.ThemeChoices[0]);
            _ui.NewGame.SelectPlatform(_ui.NewGame.PlatformChoices[0]);
            yield return WaitForLayout();
            AssertTextsFit(_ui.Root, "HUD + new game dialog");

            _ui.NewGame.Start();
            yield return WaitForLayout();
            AssertTextsFit(_ui.Root, "HUD + dev panel");

            _ui.Encyclopedia.Open();
            yield return WaitForLayout();
            AssertTextsFit(_ui.Root, "encyclopedia");
            _ui.CloseModal();

            _ui.Games.Open();
            yield return WaitForLayout();
            AssertTextsFit(_ui.Root, "games list");
        }

        private static IEnumerator WaitForLayout()
        {
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Every visible text fits its box: one-line texts are not wider than the box, wrapped texts
        /// not taller, and no text sticks out of the screen.
        /// </summary>
        private static void AssertTextsFit(VisualElement root, string where)
        {
            const float tolerance = 1f;
            var screen = root.worldBound;
            var problems = new System.Collections.Generic.List<string>();
            foreach (var element in root.Query<TextElement>().ToList())
            {
                if (string.IsNullOrEmpty(element.text) || !IsShown(element)) continue;
                var box = element.contentRect;
                if (box.width <= 0f) continue;
                string id = $"{element.GetType().Name} '{element.name}' \"{element.text}\"";
                bool wraps = element.resolvedStyle.whiteSpace == WhiteSpace.Normal;
                var size = wraps
                    // Boxes sized to their text measure a hair narrower; the tolerance avoids a false line break.
                    ? element.MeasureTextSize(element.text, box.width + tolerance, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined)
                    : element.MeasureTextSize(element.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                if (!wraps && size.x > box.width + tolerance) problems.Add($"{id}: {size.x:F0} px wide in {box.width:F0} px");
                // Text may use the padding (e.g. the progress bar title), but not go past it.
                float height = box.height + element.resolvedStyle.paddingTop + element.resolvedStyle.paddingBottom;
                if (wraps && size.y > height + tolerance) problems.Add($"{id}: {size.y:F0} px tall in {height:F0} px");
                var bound = element.worldBound;
                if (bound.xMin < screen.xMin - tolerance || bound.xMax > screen.xMax + tolerance) problems.Add($"{id}: outside the screen");
            }
            Assert.That(problems, Is.Empty, $"{where}:\n" + string.Join("\n", problems));
        }

        private static bool IsShown(VisualElement element)
        {
            if (element.panel == null) return false;
            for (var e = element; e != null; e = e.parent)
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden) return false;
            return true;
        }
    }
}
