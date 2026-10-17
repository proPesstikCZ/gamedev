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

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedLanguage = PlayerPrefs.GetString(Loc.LanguagePrefKey, "");
            yield return LoadGameScene();
        }

        [TearDown]
        public void CleanUp()
        {
            if (_runner != null && File.Exists(_runner.SavePath)) File.Delete(_runner.SavePath);
            if (string.IsNullOrEmpty(_savedLanguage)) PlayerPrefs.DeleteKey(Loc.LanguagePrefKey);
            else PlayerPrefs.SetString(Loc.LanguagePrefKey, _savedLanguage);
        }

        private IEnumerator LoadGameScene()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;
            _ui = Object.FindAnyObjectByType<GameUI>();
            _runner = Object.FindAnyObjectByType<GameRunner>();
            Assert.That(_ui, Is.Not.Null, "No GameUI in the scene.");
            Assert.That(_runner, Is.Not.Null, "No GameRunner in the scene.");
            for (int i = 0; i < 300 && !_ui.IsReady; i++) yield return null;
            Assert.That(_ui.IsReady, Is.True, "UI did not finish loading.");
            // Never touch the player's save, and let the test control time.
            _runner.SaveFileName = TestSaveFile;
            _runner.SetSpeed(GameRunner.Speed.Paused);
        }

        private IEnumerator SetLanguage(string code)
        {
            Loc.SetLanguage(code);
            yield return null;
            Assert.That(Loc.CurrentLanguage, Is.EqualTo(code));
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

            // "Restart": a fresh scene starts a new game, then the slot is loaded.
            yield return LoadGameScene();
            Assert.That(_runner.HasSave, Is.True);
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
    }
}
