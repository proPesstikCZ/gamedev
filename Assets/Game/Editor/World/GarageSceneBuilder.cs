using Game.Runtime;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace Game.Editor.World
{
    /// <summary>
    /// Builds the placeholder garage in the open scene (PLAN 4.2, 4.3): isometric grid 2:1 with
    /// 128 x 64 px cells, floor and wall tilemaps, desk with a computer, chair, the developer and the
    /// camera. Running it again replaces the previous garage.
    ///
    /// Sorting: floor and walls are behind everything (walls stand only on the back edges). Objects
    /// share one order and are sorted along the Y axis (Renderer2D custom axis 0,1,0), so objects
    /// lower on screen cover those higher up.
    /// </summary>
    public static class GarageSceneBuilder
    {
        public const string RootName = "Garage";
        public const int Width = 6;   // cells along gx
        public const int Depth = 5;   // cells along gy
        public const int FloorOrder = -20;
        public const int WallOrder = -10;
        public const int ObjectOrder = 0;

        private const string TileFolder = PlaceholderArtGenerator.Root + "/Tiles";

        /// <summary>World position of a grid point on the floor (cell size 1 x 0.5 units).</summary>
        public static Vector3 GridToWorld(float gx, float gy) => new Vector3((gx - gy) * 0.5f, (gx + gy) * 0.25f, 0f);

        [MenuItem("Game/World/Build Garage In Open Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.FloorTile) == null)
                PlaceholderArtGenerator.GenerateAll();

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(RootName);
            var grid = root.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            BuildFloor(root.transform);
            BuildWalls(root.transform);
            var objects = new GameObject("Objects").transform;
            objects.SetParent(root.transform, false);
            var deskRenderer = BuildDesk(objects);
            BuildChair(objects);
            BuildDeveloper(objects, deskRenderer);
            SetUpCamera();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[Garage] Placeholder garage built. Save the scene to keep it.");
        }

        private static void BuildFloor(Transform parent)
        {
            var tile = TileAsset("tile_floor_wood_70s", PlaceholderArtGenerator.FloorTile, Vector3.zero);
            var map = NewTilemap(parent, "Floor", FloorOrder);
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Depth; y++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        private static void BuildWalls(Transform parent)
        {
            // Wall sprites are 192 px tall with the cell at the bottom; lift them by 64 px = 0.5 units.
            var lift = new Vector3(0f, (PlaceholderArtGenerator.WallHeightPx / 2f - 32f) / 128f, 0f);
            var left = TileAsset("tile_wall_left_70s", PlaceholderArtGenerator.WallLeftTile, lift);
            var right = TileAsset("tile_wall_right_70s", PlaceholderArtGenerator.WallRightTile, lift);
            var corner = TileAsset("tile_wall_corner_70s", PlaceholderArtGenerator.WallCornerTile, lift);
            var map = NewTilemap(parent, "Walls", WallOrder);
            for (int x = 0; x < Width - 1; x++) map.SetTile(new Vector3Int(x, Depth - 1, 0), left);
            for (int y = 0; y < Depth - 1; y++) map.SetTile(new Vector3Int(Width - 1, y, 0), right);
            map.SetTile(new Vector3Int(Width - 1, Depth - 1, 0), corner);
        }

        /// <summary>
        /// Desk on cells (1..2, 4) against the back-left wall. Its sprite pivot is the front corner,
        /// which would sort it in front of the developer, so a Sorting Group at the centre of the
        /// footprint decides its order instead.
        /// </summary>
        private static SpriteRenderer BuildDesk(Transform parent)
        {
            var desk = new GameObject("furn_desk_computer_70s");
            desk.transform.SetParent(parent, false);
            desk.transform.position = GridToWorld(2f, 4.5f);
            var group = desk.AddComponent<SortingGroup>();
            group.sortingOrder = ObjectOrder;

            var sprite = new GameObject("Sprite");
            sprite.transform.SetParent(desk.transform, false);
            sprite.transform.position = GridToWorld(1f, 4f);
            var renderer = sprite.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.Desk);
            renderer.sortingOrder = ObjectOrder;
            return renderer;
        }

        private static void BuildChair(Transform parent)
        {
            // The chair is half a cell; its pivot (front corner) is a quarter cell before its centre.
            var chair = NewSprite(parent, "furn_chair_70s", PlaceholderArtGenerator.Chair);
            chair.transform.position = GridToWorld(1.75f, 3.35f);
        }

        private static void BuildDeveloper(Transform parent, SpriteRenderer desk)
        {
            var dev = NewSprite(parent, "char_dev_male_70s", PlaceholderArtGenerator.DevWork0);
            // Sits on the chair: behind the chair's front corner, in front of the desk's centre.
            dev.transform.position = GridToWorld(2f, 3.6f);
            var view = dev.gameObject.AddComponent<DeveloperView>();
            view.Configure(
                Object.FindAnyObjectByType<GameRunner>(),
                dev,
                new[]
                {
                    AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.DevWork0),
                    AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.DevWork1),
                },
                AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.DevRest),
                desk,
                AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.Desk),
                AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.DeskScreenOn));
            dev.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArtGenerator.DevRest);
            EditorUtility.SetDirty(view);
        }

        private static void SetUpCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[Garage] No main camera in the scene.");
                return;
            }
            // Centre of the floor, a little higher because of the walls.
            Vector3 centre = GridToWorld(Width / 2f, Depth / 2f) + new Vector3(0f, 0.35f, 0f);
            camera.transform.position = new Vector3(centre.x, centre.y, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 2.6f;
            camera.backgroundColor = new Color(0.12f, 0.11f, 0.10f);

            var controller = camera.GetComponent<IsoCameraController>();
            if (controller == null) controller = camera.gameObject.AddComponent<IsoCameraController>();
            controller.Configure(new Rect(centre.x - 2.5f, centre.y - 1.5f, 5f, 3f), 1.2f, 4f);
            EditorUtility.SetDirty(controller);

            var ui = Object.FindAnyObjectByType<GameUI>();
            if (ui != null)
            {
                var so = new SerializedObject(ui);
                so.FindProperty("cameraController").objectReferenceValue = controller;
                so.ApplyModifiedProperties();
            }
        }

        private static Tilemap NewTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var map = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return map;
        }

        private static SpriteRenderer NewSprite(Transform parent, string name, string spritePath)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.sortingOrder = ObjectOrder;
            return renderer;
        }

        /// <summary>Creates or updates a Tile asset next to the placeholder sprites.</summary>
        private static Tile TileAsset(string name, string spritePath, Vector3 offset)
        {
            string path = $"{TileFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            tile.colliderType = Tile.ColliderType.None;
            tile.transform = Matrix4x4.Translate(offset);
            tile.flags = TileFlags.LockTransform | TileFlags.LockColor;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssetIfDirty(tile);
            return tile;
        }
    }
}
