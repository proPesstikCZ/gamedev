using System.IO;
using UnityEngine;

namespace Game.Editor.World
{
    /// <summary>
    /// Tiny software canvas for placeholder art: filled polygons, isometric boxes and round shapes.
    /// Coordinates are pixels with the origin at the bottom left, like Texture2D.
    /// Isometry is 2:1 (STYLE 2): one grid cell is 128 x 64 px.
    /// </summary>
    internal sealed class IsoCanvas
    {
        public const float HalfWidth = 64f;
        public const float HalfHeight = 32f;

        private readonly Color32[] _pixels;
        private readonly Vector2 _anchor;

        public int Width { get; }
        public int Height { get; }

        /// <param name="anchor">Pixel where grid point (0, 0) on the floor lies.</param>
        public IsoCanvas(int width, int height, Vector2 anchor)
        {
            Width = width;
            Height = height;
            _anchor = anchor;
            _pixels = new Color32[width * height];
        }

        /// <summary>Pixel of a grid point: gx goes right-up, gy left-up, z straight up in pixels.</summary>
        public Vector2 P(float gx, float gy, float z = 0f) =>
            _anchor + new Vector2((gx - gy) * HalfWidth, (gx + gy) * HalfHeight + z);

        /// <summary>Fills a polygon and draws its outline in a darker shade.</summary>
        public void Poly(Color32 fill, params Vector2[] points) => Poly(fill, Shade(fill, 0.6f), points);

        public void Poly(Color32 fill, Color32? outline, params Vector2[] points)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (int y = Mathf.Max(0, Mathf.FloorToInt(minY)); y <= Mathf.Min(Height - 1, Mathf.CeilToInt(maxY)); y++)
            for (int x = Mathf.Max(0, Mathf.FloorToInt(minX)); x <= Mathf.Min(Width - 1, Mathf.CeilToInt(maxX)); x++)
                if (Inside(points, x + 0.5f, y + 0.5f)) Set(x, y, fill);

            if (outline is Color32 line)
                for (int i = 0; i < points.Length; i++)
                    Line(points[i], points[(i + 1) % points.Length], line);
        }

        /// <summary>
        /// Box standing on the grid. Light comes from the top left (STYLE 2): the top is lightest,
        /// the face looking left-down is mid and the face looking right-down is darkest.
        /// </summary>
        public void Box(float gx, float gy, float sizeX, float sizeY, float z, float height, Color32 color)
        {
            Vector2 a = P(gx, gy, z), bx = P(gx + sizeX, gy, z), by = P(gx, gy + sizeY, z), c = P(gx + sizeX, gy + sizeY, z);
            var up = new Vector2(0, height);
            Poly(color, a, by, by + up, a + up);
            Poly(Shade(color, 0.75f), a, bx, bx + up, a + up);
            Poly(Shade(color, 1.15f), a + up, bx + up, c + up, by + up);
        }

        public void Ellipse(Vector2 center, float rx, float ry, Color32 color)
        {
            for (int y = Mathf.Max(0, (int)(center.y - ry)); y <= Mathf.Min(Height - 1, (int)(center.y + ry) + 1); y++)
            for (int x = Mathf.Max(0, (int)(center.x - rx)); x <= Mathf.Min(Width - 1, (int)(center.x + rx) + 1); x++)
            {
                float dx = (x + 0.5f - center.x) / rx, dy = (y + 0.5f - center.y) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, color);
            }
        }

        /// <summary>Thick line with round ends, used for arms.</summary>
        public void Capsule(Vector2 from, Vector2 to, float radius, Color32 color)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to)));
            for (int i = 0; i <= steps; i++) Ellipse(Vector2.Lerp(from, to, i / (float)steps), radius, radius, color);
        }

        public void Line(Vector2 from, Vector2 to, Color32 color)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) * 2f));
            for (int i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)steps);
                Set(Mathf.Clamp((int)p.x, 0, Width - 1), Mathf.Clamp((int)p.y, 0, Height - 1), color);
            }
        }

        public void SavePng(string assetPath)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(_pixels);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, png);
        }

        public static Color32 Shade(Color32 c, float factor) => new Color32(
            (byte)Mathf.Clamp(c.r * factor, 0, 255), (byte)Mathf.Clamp(c.g * factor, 0, 255),
            (byte)Mathf.Clamp(c.b * factor, 0, 255), c.a);

        private void Set(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            _pixels[y * Width + x] = color;
        }

        private static bool Inside(Vector2[] poly, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
