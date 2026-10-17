using UnityEditor;
using UnityEngine;

namespace Game.Editor.World
{
    /// <summary>
    /// Draws placeholder art for the garage (PLAN 4.2, 4.3) into Assets/Placeholders/.
    /// Simple shapes in the sizes of STYLE 3, names of STYLE 9 and the 1972–1982 palette of STYLE 4.
    /// Codex replaces them with real art in Assets/Art/ (PLAN M4b).
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        public const string Root = "Assets/Placeholders";
        public const string FloorTile = Root + "/Tiles/tile_floor_wood_70s.png";
        public const string WallLeftTile = Root + "/Tiles/tile_wall_left_70s.png";
        public const string WallRightTile = Root + "/Tiles/tile_wall_right_70s.png";
        public const string WallCornerTile = Root + "/Tiles/tile_wall_corner_70s.png";
        public const string Desk = Root + "/Furniture/furn_desk_computer_70s.png";
        public const string DeskScreenOn = Root + "/Furniture/furn_desk_computer_70s_on.png";
        public const string Chair = Root + "/Furniture/furn_chair_70s.png";
        public const string DevWork0 = Root + "/Characters/char_dev_male_70s_nw_work_0.png";
        public const string DevWork1 = Root + "/Characters/char_dev_male_70s_nw_work_1.png";
        public const string DevRest = Root + "/Characters/char_dev_male_70s_nw_rest.png";

        /// <summary>Wall tiles are taller than a cell: 128 x 192 px with the cell diamond at the bottom.</summary>
        public const int WallHeightPx = 192;

        // 1972–1982 palette (STYLE 4): brown, orange, mustard, olive.
        private static readonly Color32 Wood = new Color32(150, 98, 58, 255);
        private static readonly Color32 DarkWood = new Color32(112, 72, 42, 255);
        private static readonly Color32 Wall = new Color32(200, 168, 104, 255);
        private static readonly Color32 Skirting = new Color32(110, 84, 40, 255);
        private static readonly Color32 Beige = new Color32(214, 200, 170, 255);
        private static readonly Color32 ScreenOff = new Color32(44, 56, 46, 255);
        private static readonly Color32 ScreenOn = new Color32(130, 225, 140, 255);
        private static readonly Color32 Orange = new Color32(190, 98, 40, 255);
        private static readonly Color32 Olive = new Color32(112, 116, 52, 255);
        private static readonly Color32 Metal = new Color32(90, 86, 80, 255);
        private static readonly Color32 Sweater = new Color32(168, 92, 44, 255);
        private static readonly Color32 Mustard = new Color32(206, 160, 52, 255);
        private static readonly Color32 Hair = new Color32(74, 46, 30, 255);
        private static readonly Color32 Skin = new Color32(226, 180, 140, 255);

        [MenuItem("Game/World/Generate Placeholder Art")]
        public static void GenerateAll()
        {
            DrawFloor().SavePng(FloorTile);
            DrawWall(left: true, right: false).SavePng(WallLeftTile);
            DrawWall(left: false, right: true).SavePng(WallRightTile);
            DrawWall(left: true, right: true).SavePng(WallCornerTile);
            DrawDesk(screenOn: false).SavePng(Desk);
            DrawDesk(screenOn: true).SavePng(DeskScreenOn);
            DrawChair().SavePng(Chair);
            DrawDeveloper(DevPose.Work0).SavePng(DevWork0);
            DrawDeveloper(DevPose.Work1).SavePng(DevWork1);
            DrawDeveloper(DevPose.Rest).SavePng(DevRest);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[Placeholders] Placeholder art written to {Root}.");
        }

        /// <summary>Floor tile 128 x 64 px: wooden planks along the gx axis.</summary>
        private static IsoCanvas DrawFloor()
        {
            var c = new IsoCanvas(128, 64, new Vector2(64, 0));
            // Half a pixel larger than the cell, so bilinear filtering leaves no seams between tiles.
            c.Poly(Wood, IsoCanvas.Shade(Wood, 0.85f),
                c.P(0, 0) + new Vector2(0, -0.5f), c.P(1, 0) + new Vector2(0.5f, 0),
                c.P(1, 1) + new Vector2(0, 0.5f), c.P(0, 1) + new Vector2(-0.5f, 0));
            var seam = IsoCanvas.Shade(Wood, 0.8f);
            for (int i = 1; i < 4; i++) c.Line(c.P(0, i / 4f), c.P(1, i / 4f), seam);
            return c;
        }

        /// <summary>
        /// Wall tile 128 x 192 px. Left = wall on the cell's back-left edge, right = back-right edge.
        /// The face looking right-down (left wall) is darker, light comes from the left.
        /// </summary>
        private static IsoCanvas DrawWall(bool left, bool right)
        {
            var c = new IsoCanvas(128, WallHeightPx, new Vector2(64, 0));
            const float height = 128f, skirting = 10f;
            var up = new Vector2(0, height);
            var skirtUp = new Vector2(0, skirting);
            if (left)
            {
                Vector2 a = c.P(0, 1), b = c.P(1, 1);
                WallFace(c, IsoCanvas.Shade(Wall, 0.8f), IsoCanvas.Shade(Skirting, 0.8f), a, b, up, skirtUp);
            }
            if (right)
            {
                Vector2 a = c.P(1, 1), b = c.P(1, 0);
                WallFace(c, Wall, Skirting, a, b, up, skirtUp);
            }
            return c;
        }

        /// <summary>One wall face without side outlines, so neighbouring tiles join without seams.</summary>
        private static void WallFace(IsoCanvas c, Color32 wall, Color32 skirting, Vector2 a, Vector2 b, Vector2 up, Vector2 skirtUp)
        {
            c.Poly(wall, null, a, b, b + up, a + up);
            c.Poly(skirting, null, a, b, b + skirtUp, a + skirtUp);
            c.Line(a + up, b + up, IsoCanvas.Shade(wall, 0.6f));
        }

        /// <summary>
        /// Desk with a 70s home computer, 256 x 192 px. Footprint 2 x 1 cells along gx, pivot (bottom
        /// centre) is the front corner of the footprint. The screen looks to the chair (towards -gy).
        /// </summary>
        private static IsoCanvas DrawDesk(bool screenOn)
        {
            var c = new IsoCanvas(256, 192, new Vector2(128, 0));
            const float x0 = 0.05f, x1 = 1.95f, y0 = 0.05f, y1 = 0.95f, leg = 0.08f, top = 36f;
            // Back legs first, front legs last (painter's order).
            c.Box(x1 - leg, y1 - leg, leg, leg, 0, top, DarkWood);
            c.Box(x0, y1 - leg, leg, leg, 0, top, DarkWood);
            c.Box(x1 - leg, y0, leg, leg, 0, top, DarkWood);
            c.Box(x0, y0, leg, leg, 0, top, DarkWood);
            c.Box(x0, y0, x1 - x0, y1 - y0, top, 8, Wood);

            const float deskTop = top + 8;
            // CRT monitor at the back, keyboard unit in front of it.
            c.Box(0.65f, 0.5f, 0.7f, 0.4f, deskTop, 50, Beige);
            Color32 screen = screenOn ? ScreenOn : ScreenOff;
            c.Poly(screen, Metal, c.P(0.72f, 0.5f, deskTop + 10), c.P(1.28f, 0.5f, deskTop + 10),
                c.P(1.28f, 0.5f, deskTop + 42), c.P(0.72f, 0.5f, deskTop + 42));
            if (screenOn)
                for (int i = 0; i < 4; i++)
                {
                    float z = deskTop + 36 - i * 7;
                    c.Line(c.P(0.76f, 0.5f, z), c.P(0.9f + 0.08f * ((i * 3) % 4), 0.5f, z), IsoCanvas.Shade(ScreenOn, 0.55f));
                }
            c.Box(0.6f, 0.12f, 0.8f, 0.3f, deskTop, 10, Beige);
            c.Box(0.68f, 0.16f, 0.64f, 0.18f, deskTop + 10, 2, IsoCanvas.Shade(Beige, 0.8f));
            return c;
        }

        /// <summary>Chair 128 x 128 px, footprint half a cell. The backrest is on the -gy side.</summary>
        private static IsoCanvas DrawChair()
        {
            var c = new IsoCanvas(128, 128, new Vector2(64, 0));
            const float s = 0.5f, leg = 0.06f, seat = 24f;
            c.Box(s - leg, s - leg, leg, leg, 0, seat, Metal);
            c.Box(0, s - leg, leg, leg, 0, seat, Metal);
            c.Box(s - leg, 0, leg, leg, 0, seat, Metal);
            c.Box(0, 0, leg, leg, 0, seat, Metal);
            c.Box(0, 0, s, s, seat, 6, Orange);
            c.Box(0, 0, s, 0.08f, seat + 6, 30, Olive);
            return c;
        }

        private enum DevPose { Work0, Work1, Rest }

        /// <summary>
        /// Developer seen from behind, sitting and facing north-west (to the desk), 96 x 128 px.
        /// Pivot (bottom centre) is the floor under the seat. Work frames move the elbows (typing),
        /// the rest pose has the hands behind the head.
        /// </summary>
        private static IsoCanvas DrawDeveloper(DevPose pose)
        {
            var c = new IsoCanvas(96, 128, new Vector2(48, 0));
            const float seat = 30f;
            float bob = pose == DevPose.Work1 ? 1f : 0f;
            var shoulderL = new Vector2(36, 74 + bob);
            var shoulderR = new Vector2(60, 74 + bob);
            var head = new Vector2(48, 92 + bob);

            if (pose == DevPose.Rest)
            {
                // Arms behind the head, drawn first so the head covers the hands.
                c.Capsule(shoulderL, new Vector2(22, 96), 5, Sweater);
                c.Capsule(new Vector2(22, 96), new Vector2(40, 100), 5, Sweater);
                c.Capsule(shoulderR, new Vector2(74, 96), 5, Sweater);
                c.Capsule(new Vector2(74, 96), new Vector2(56, 100), 5, Sweater);
            }

            // Torso: sweater with a mustard stripe.
            c.Ellipse(new Vector2(48, seat + 22 + bob), 18, 24, Sweater);
            c.Poly(Mustard, null, new Vector2(31, seat + 26 + bob), new Vector2(65, seat + 26 + bob),
                new Vector2(65, seat + 31 + bob), new Vector2(31, seat + 31 + bob));

            if (pose != DevPose.Rest)
            {
                // Elbows out to the sides, hands forward on the keyboard (hidden by the body).
                float spread = pose == DevPose.Work0 ? 0f : 2f;
                c.Capsule(shoulderL, new Vector2(26 + spread, 58), 5, Sweater);
                c.Capsule(new Vector2(26 + spread, 58), new Vector2(34, 54 + spread), 4, Skin);
                c.Capsule(shoulderR, new Vector2(70 - spread, 58), 5, Sweater);
                c.Capsule(new Vector2(70 - spread, 58), new Vector2(62, 56 - spread), 4, Skin);
            }

            c.Ellipse(new Vector2(48, 79 + bob), 6, 4, Skin);
            c.Ellipse(new Vector2(34, 92 + bob), 3, 4, Skin);
            c.Ellipse(new Vector2(62, 92 + bob), 3, 4, Skin);
            c.Ellipse(head, 13, 14, Hair);
            return c;
        }
    }
}
