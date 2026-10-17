using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Import settings for game art (STYLE 8): Sprite, Bilinear, 128 pixels per unit, no mip maps,
    /// normal quality compression. Applies to PNGs in Assets/Art/ and in Assets/Placeholders/,
    /// so placeholders behave exactly like the final art that replaces them.
    /// Pivot follows STYLE 3: bottom centre for objects, centre for floor and wall tiles.
    /// </summary>
    public sealed class ArtImportPostprocessor : AssetPostprocessor
    {
        public const int PixelsPerUnit = 128;
        public static readonly string[] Roots = { "Assets/Art/", "Assets/Placeholders/" };

        // Raise when the rules change, so Unity reimports the art with the new settings.
        private const uint Version = 1;

        public override uint GetVersion() => Version;

        /// <summary>True when the asset lies in a folder with game art.</summary>
        public static bool AppliesTo(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            string path = assetPath.Replace('\\', '/');
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return false;
            foreach (string root in Roots)
                if (path.StartsWith(root, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Tiles sit on a tilemap cell, which places them by their centre.</summary>
        public static bool IsTile(string assetPath) =>
            assetPath.Replace('\\', '/').Contains("/Tiles/");

        private void OnPreprocessTexture()
        {
            if (!AppliesTo(assetPath)) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        /// <summary>Sets the STYLE 8 values on an importer. Sprite sheets keep Sprite Mode Multiple.</summary>
        public static void Apply(TextureImporter importer, string assetPath)
        {
            importer.textureType = TextureImporterType.Sprite;
            // Codex may cut a sheet into several sprites; only new or odd modes become Single.
            if (importer.importSettingsMissing || importer.spriteImportMode != SpriteImportMode.Multiple)
                importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            if (importer.spriteImportMode == SpriteImportMode.Single)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)(IsTile(assetPath) ? SpriteAlignment.Center : SpriteAlignment.BottomCenter);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
        }
    }
}
