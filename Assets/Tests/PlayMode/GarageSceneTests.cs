using System.Collections;
using System.IO;
using System.Linq;
using Game.Core;
using Game.Runtime;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace Game.Tests
{
    /// <summary>M4: the placeholder garage, its sorting, the developer and the camera.</summary>
    public class GarageSceneTests
    {
        private const string SceneName = "SampleScene";
        private const string TestSaveFile = "playmode_garage_test_save.json";

        private GameRunner _runner;
        private GameUI _ui;
        private DeveloperView _developer;

        private static string TestSavePath => Path.Combine(Application.persistentDataPath, TestSaveFile);

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            GameRunner.SaveFileNameOverride = TestSaveFile;
            if (File.Exists(TestSavePath)) File.Delete(TestSavePath);
            SceneManager.LoadScene(SceneName);
            yield return null;
            _runner = Object.FindAnyObjectByType<GameRunner>();
            _ui = Object.FindAnyObjectByType<GameUI>();
            _developer = Object.FindAnyObjectByType<DeveloperView>();
            Assert.That(_runner, Is.Not.Null);
            Assert.That(_ui, Is.Not.Null);
            Assert.That(_developer, Is.Not.Null, "No developer in the garage.");
            for (int i = 0; i < 300 && !_ui.IsReady; i++) yield return null;
            Assert.That(_ui.IsReady, Is.True);
            _runner.SetSpeed(GameRunner.Speed.Paused);
        }

        [TearDown]
        public void CleanUp()
        {
            if (File.Exists(TestSavePath)) File.Delete(TestSavePath);
            GameRunner.SaveFileNameOverride = null;
        }

        [Test]
        public void Garage_IsIsometricGridWithFloorAndWalls()
        {
            var garage = GameObject.Find("Garage");
            Assert.That(garage, Is.Not.Null);
            var grid = garage.GetComponent<Grid>();
            Assert.That(grid.cellLayout, Is.EqualTo(GridLayout.CellLayout.Isometric));
            Assert.That(grid.cellSize, Is.EqualTo(new Vector3(1f, 0.5f, 1f)), "Cell 128 x 64 px at 128 PPU.");

            var floor = garage.transform.Find("Floor").GetComponent<Tilemap>();
            var walls = garage.transform.Find("Walls").GetComponent<Tilemap>();
            Assert.That(floor.GetUsedTilesCount(), Is.GreaterThan(0));
            Assert.That(CountTiles(floor), Is.GreaterThanOrEqualTo(20), "Garage floor is too small.");
            Assert.That(CountTiles(walls), Is.GreaterThan(0));

            int objectOrder = 0;
            Assert.That(floor.GetComponent<TilemapRenderer>().sortingOrder, Is.LessThan(objectOrder));
            Assert.That(walls.GetComponent<TilemapRenderer>().sortingOrder, Is.LessThan(objectOrder));
            Assert.That(walls.GetComponent<TilemapRenderer>().sortingOrder,
                Is.GreaterThan(floor.GetComponent<TilemapRenderer>().sortingOrder), "Walls stand on the floor.");
        }

        [Test]
        public void Objects_LowerOnScreenCoverHigherOnes()
        {
            var objects = GameObject.Find("Garage/Objects").transform;
            var chair = objects.Find("furn_chair_70s");
            var developer = _developer.transform;
            var desk = objects.Find("furn_desk_computer_70s");
            Assert.That(chair, Is.Not.Null);
            Assert.That(desk, Is.Not.Null);
            Assert.That(desk.GetComponent<SortingGroup>(), Is.Not.Null, "The desk sorts as one group.");

            // Same layer and order for all objects, so only the Y axis decides (custom axis 0,1,0).
            var renderers = objects.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(renderers.Select(r => r.sortingLayerID).Distinct().Count(), Is.EqualTo(1));
            Assert.That(renderers.Select(r => r.sortingOrder).Distinct().Count(), Is.EqualTo(1));
            Assert.That(desk.GetComponent<SortingGroup>().sortingOrder, Is.EqualTo(renderers[0].sortingOrder));

            Assert.That(chair.position.y, Is.LessThan(developer.position.y), "The chair is in front of the developer.");
            Assert.That(developer.position.y, Is.LessThan(desk.position.y), "The developer is in front of the desk.");
        }

        [UnityTest]
        public IEnumerator Developer_WorksOnlyDuringDevelopment()
        {
            _runner.NewGame(42);
            yield return null;
            Assert.That(_developer.Activity, Is.EqualTo(DeveloperActivity.Resting), "Rests without a game in development.");
            var restSprite = _developer.CurrentSprite;

            var sim = _runner.Simulation;
            var concept = new GameConcept("Garage Test", sim.Database.Genres.First(g => g.IsAvailable(sim.Date.Year)).Id,
                sim.Database.Themes.First(t => t.IsAvailable(sim.Date.Year)).Id,
                sim.Database.Platforms.First(p => p.IsAvailable(sim.Date.Year)).Id,
                Audience.Everyone, EngineChoice.Custom, GameSize.Small);
            Assert.That(sim.StartProject(concept), Is.Empty);
            yield return null;
            Assert.That(_developer.Activity, Is.EqualTo(DeveloperActivity.Working), "Works after the start.");
            Assert.That(_developer.CurrentSprite, Is.Not.EqualTo(restSprite), "Shows the working pose.");

            for (int i = 0; i < 40 && sim.ActiveProject != null; i++) _runner.Step();
            Assert.That(sim.ActiveProject, Is.Null, "The game was not released.");
            yield return null;
            Assert.That(_developer.Activity, Is.EqualTo(DeveloperActivity.Resting), "Rests after the release.");
            Assert.That(_developer.CurrentSprite, Is.EqualTo(restSprite));
        }

        [Test]
        public void Camera_LooksAtGarageAndStaysInBounds()
        {
            var controller = Camera.main.GetComponent<IsoCameraController>();
            Assert.That(controller, Is.Not.Null, "Main camera has no pan/zoom controller.");
            Assert.That(_ui.CameraController, Is.EqualTo(controller), "UI does not block the camera.");
            Assert.That(controller.Bounds.Contains((Vector2)_developer.transform.position), Is.True, "Camera bounds miss the desk.");

            controller.Pan(new Vector2(1000f, 1000f));
            Assert.That(controller.transform.position.x, Is.EqualTo(controller.Bounds.xMax).Within(1e-4));
            Assert.That(controller.transform.position.y, Is.EqualTo(controller.Bounds.yMax).Within(1e-4));

            var centre = new Vector2(Screen.width / 2f, Screen.height / 2f);
            for (int i = 0; i < 100; i++) controller.ZoomAt(centre, 1f);
            Assert.That(controller.Camera.orthographicSize, Is.EqualTo(controller.MinZoom).Within(1e-4));
            for (int i = 0; i < 100; i++) controller.ZoomAt(centre, -1f);
            Assert.That(controller.Camera.orthographicSize, Is.EqualTo(controller.MaxZoom).Within(1e-4));
        }

        [Test]
        public void Camera_IgnoresPointerOverUi()
        {
            var hud = _ui.Root.Q("hud");
            var rect = hud.worldBound;
            Assert.That(rect.width, Is.GreaterThan(0f));
            // Panel coordinates have the origin at the top left, screen coordinates at the bottom left.
            var scale = Screen.height / _ui.Root.panel.visualTree.layout.height;
            var overHud = new Vector2(rect.center.x * scale, Screen.height - rect.center.y * scale);
            Assert.That(_ui.IsPointerOverUi(overHud), Is.True, "Pointer over the HUD must not move the camera.");
        }

        private static int CountTiles(Tilemap map)
        {
            int count = 0;
            foreach (var position in map.cellBounds.allPositionsWithin)
                if (map.HasTile(position)) count++;
            return count;
        }
    }
}
