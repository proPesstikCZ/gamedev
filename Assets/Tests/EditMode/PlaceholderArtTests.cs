using System.Linq;
using System.Text.RegularExpressions;
using Game.Editor.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>M4.2: placeholder art keeps the sizes of STYLE 3 and the names of STYLE 9.</summary>
    public class PlaceholderArtTests
    {
        private static readonly Regex Name = new Regex("^(tile|furn|char)_[a-z0-9_]+$");

        private static string[] PlaceholderPngs() => AssetDatabase.FindAssets("t:Texture2D", new[] { PlaceholderArtGenerator.Root })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png"))
            .ToArray();

        [Test]
        public void AllPlaceholdersExist()
        {
            string[] expected =
            {
                PlaceholderArtGenerator.FloorTile, PlaceholderArtGenerator.WallLeftTile, PlaceholderArtGenerator.WallRightTile,
                PlaceholderArtGenerator.WallCornerTile, PlaceholderArtGenerator.Desk, PlaceholderArtGenerator.DeskScreenOn,
                PlaceholderArtGenerator.Chair, PlaceholderArtGenerator.DevWork0, PlaceholderArtGenerator.DevWork1,
                PlaceholderArtGenerator.DevRest,
            };
            foreach (string path in expected)
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null, path);
        }

        [Test]
        public void Placeholders_FollowStyleNamesAndSizes()
        {
            var pngs = PlaceholderPngs();
            Assert.That(pngs, Is.Not.Empty);
            foreach (string path in pngs)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                Assert.That(Name.IsMatch(name), Is.True, $"{name}: lower case, underscores, English (STYLE 9).");

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                int w = (int)sprite.rect.width, h = (int)sprite.rect.height;
                if (name.StartsWith("tile_floor")) Assert.That((w, h), Is.EqualTo((128, 64)), path);
                else if (name.StartsWith("tile_wall")) Assert.That(w, Is.EqualTo(128), path);
                else if (name.StartsWith("furn_chair")) Assert.That(w <= 128 && h <= 128, Is.True, $"{path} is {w}x{h}");
                else if (name.StartsWith("furn_")) Assert.That(w <= 256 && h <= 192, Is.True, $"{path} is {w}x{h}");
                else if (name.StartsWith("char_")) Assert.That(h, Is.InRange(96, 128), path);

                // Objects stand on their pivot at the bottom centre; tiles sit on the cell centre (STYLE 3).
                Vector2 pivot = new Vector2(sprite.pivot.x / w, sprite.pivot.y / h);
                Vector2 expected = name.StartsWith("tile_") ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
                Assert.That(Vector2.Distance(pivot, expected), Is.LessThan(0.01f), $"{path} pivot {pivot}");
            }
        }

        [Test]
        public void Character_IsDrawnBetween96And128PixelsTall()
        {
            foreach (string path in new[] { PlaceholderArtGenerator.DevWork0, PlaceholderArtGenerator.DevRest })
            {
                var texture = new Texture2D(2, 2);
                Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(path)), Is.True);
                var pixels = texture.GetPixels32();
                int top = -1, bottom = -1;
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a == 0) continue;
                    if (bottom < 0) bottom = y;
                    top = y;
                }
                Object.DestroyImmediate(texture);
                // Seated figure: from the floor (pivot) to the top of the head.
                Assert.That(top + 1, Is.InRange(96, 128), $"{path} drawn {bottom}..{top}");
            }
        }
    }
}
