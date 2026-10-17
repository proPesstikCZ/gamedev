using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>M4.1: art import settings (STYLE 8) and isometric sorting in the 2D renderer.</summary>
    public class ArtImportTests
    {
        // Temporary folder for test PNGs. Tests never write into Assets/Art/ (AGENTS.md).
        private const string TempFolder = "Assets/Placeholders/_ImportTest";

        [TearDown]
        public void CleanUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
        }

        [TestCase("Assets/Art/Furniture/furn_desk_computer_70s.png", true)]
        [TestCase("Assets/Art/Tiles/tile_floor_wood_70s.PNG", true)]
        [TestCase("Assets/Placeholders/Tiles/tile_floor_wood_70s.png", true)]
        [TestCase("Assets/Art/Furniture/notes.txt", false)]
        [TestCase("Assets/Game/UI/icon.png", false)]
        [TestCase("Assets/Artwork/furn_chair.png", false)]
        [TestCase("", false)]
        public void Rules_ApplyOnlyToArtFolders(string path, bool expected)
        {
            Assert.That(ArtImportPostprocessor.AppliesTo(path), Is.EqualTo(expected));
        }

        [TestCase("Furniture/furn_test_70s.png", SpriteAlignment.BottomCenter)]
        [TestCase("Tiles/tile_test_70s.png", SpriteAlignment.Center)]
        public void ImportedPng_GetsStyleSettings(string relativePath, SpriteAlignment pivot)
        {
            string path = $"{TempFolder}/{relativePath}";
            WritePng(path, 128, 128);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(128f));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Compressed));

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That((SpriteAlignment)settings.spriteAlignment, Is.EqualTo(pivot));

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(sprite, Is.Not.Null, "The PNG was not imported as a sprite.");
            Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(1e-4), "128 px must be 1 unit (one tile wide).");
        }

        [Test]
        public void ImportedSpriteSheet_KeepsMultipleMode()
        {
            string path = $"{TempFolder}/Characters/char_test_70s_sheet.png";
            WritePng(path, 256, 128);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();

            importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(128f), "Other rules still apply to sheets.");
        }

        [Test]
        public void ExistingArt_HasStyleSettings()
        {
            // Read only: checks whatever art Codex has delivered so far.
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!ArtImportPostprocessor.AppliesTo(path)) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(128f), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
            }
        }

        [Test]
        public void Renderer2D_SortsAlongVerticalAxis()
        {
            var renderer = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Renderer2D.asset");
            Assert.That(renderer, Is.Not.Null);
            var so = new SerializedObject(renderer);
            Assert.That(so.FindProperty("m_TransparencySortMode").intValue, Is.EqualTo((int)TransparencySortMode.CustomAxis));
            Assert.That(so.FindProperty("m_TransparencySortAxis").vector3Value, Is.EqualTo(new Vector3(0, 1, 0)));
        }

        private static void WritePng(string assetPath, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(200, 120, 40, 255);
            texture.SetPixels32(pixels);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, png);
            // Refresh also imports the new folders above the file.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
