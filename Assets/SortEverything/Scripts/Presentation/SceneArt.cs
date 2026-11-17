using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Supporting scene props in the shared sculpted style (one upper-left light, warm-brown outline). Decorative only:
    /// never raycast targets, never sortable. A sprite at Resources/SortEverything/Art/Props/&lt;id&gt; replaces any of them.
    /// </summary>
    public static class SceneArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => Cache.Clear();

        /// <summary>Prop IDs scene data may use (checked by the content validator).</summary>
        public static readonly string[] Known =
        {
            "window_morning", "wall_shelf", "toast_plate", "jam_jar", "flower_vase", "napkin",
            "sofa", "floor_lamp", "wall_art", "armchair", "popcorn_bowl", "tea_cup",
            "dining_table", "sideboard", "pendant_lamp",
            "window", "stove", "pantry_shelf", "plate_shelf", "table", "plant", "rail", "basket",
        };

        static Color H(string hex) => SpriteFactory.Hex(hex);

        public static Sprite Prop(string id)
        {
            if (Cache.TryGetValue(id, out var s) && s != null && s.texture != null) return s;
            var art = Resources.Load<Sprite>("SortEverything/Art/Props/" + id);
            s = art != null ? art : Draw(id);
            Cache[id] = s;
            return s;
        }

        static Sprite Draw(string id)
        {
            var c = new SpriteFactory.Raster(256, 128f) { Gloss = 0.2f, Bevel = 10f };
            switch (id)
            {
                case "window_morning":
                    // Frame, warm morning sky with a low sun, curtains tied back, a sill.
                    c.RoundRect(64, 58, 50, 50, 6, 0, H("#FFF8EE"));
                    c.Shade = false;
                    c.Custom((x, y) => Mathf.Max(Mathf.Abs(x - 64) - 41, Mathf.Abs(y - 58) - 41), H("#FBD9A6"), false, 23, 17, 105, 99);
                    c.Custom((x, y) => Mathf.Max(Mathf.Abs(x - 64) - 41, Mathf.Abs(y - 44) - 27), H("#BFE1F2"), false, 23, 17, 105, 71);
                    c.Circle(80, 70, 13, H("#FFE8A3"), outline: false);
                    c.Circle(80, 70, 8, H("#FFF6D8"), outline: false);
                    c.Ellipse(44, 36, 14, 5, 0, new Color(1, 1, 1, 0.9f), outline: false);
                    c.RoundRect(64, 92, 41, 7, 2, 0, H("#A9D39A"), outline: false);
                    c.Shade = true;
                    c.RoundRect(64, 58, 2.6f, 41, 1, 0, H("#FFF8EE"));
                    c.RoundRect(64, 58, 41, 2.6f, 1, 0, H("#FFF8EE"));
                    c.Polygon(new[] { new Vector2(12, 6), new Vector2(38, 6), new Vector2(30, 60), new Vector2(26, 112), new Vector2(8, 112) }, H("#F2B9A4"));
                    c.Polygon(new[] { new Vector2(90, 6), new Vector2(116, 6), new Vector2(120, 112), new Vector2(102, 112), new Vector2(98, 60) }, H("#F2B9A4"));
                    c.Capsule(18, 64, 34, 64, 3, H("#D98E78"));
                    c.Capsule(94, 64, 110, 64, 3, H("#D98E78"));
                    c.RoundRect(64, 116, 58, 5, 2, 0, H("#FFF8EE"));
                    break;
                case "wall_shelf":
                    // Small wooden wall shelf with three jars and a cup.
                    c.RoundRect(64, 90, 58, 6, 2, 0, H("#B07C4D"));
                    c.Polygon(new[] { new Vector2(16, 96), new Vector2(26, 96), new Vector2(18, 112) }, H("#8E5F33"));
                    c.Polygon(new[] { new Vector2(102, 96), new Vector2(112, 96), new Vector2(110, 112) }, H("#8E5F33"));
                    for (int i = 0; i < 3; i++)
                    {
                        float x = 26 + i * 26;
                        c.Gloss = 0.5f;
                        c.RoundRect(x, 66, 10, 18, 5, 0, new Color(0.86f, 0.93f, 0.95f, 1f));
                        c.Gloss = 0.2f;
                        c.RoundRect(x, 72, 8, 10, 4, 0, i == 0 ? H("#E9B35B") : i == 1 ? H("#C9653E") : H("#E9D7B4"), outline: false);
                        c.RoundRect(x, 46, 9, 4, 2, 0, i == 1 ? H("#D24C3E") : H("#B98950"));
                    }
                    c.RoundRect(104, 74, 9, 10, 3, 0, H("#7FAFD9"));
                    break;
                case "toast_plate":
                    c.Gloss = 0.45f;
                    c.Ellipse(64, 70, 54, 34, 0, H("#F7F1E6"));
                    c.Ellipse(64, 70, 42, 25, 0, H("#EFE5D3"), outline: false);
                    c.Gloss = 0.1f; c.Bevel = 8;
                    c.RoundRect(52, 62, 20, 18, 7, -10, H("#E2A25A"));
                    c.RoundRect(52, 62, 15, 13, 5, -10, H("#F6D9A0"), outline: false);
                    c.RoundRect(76, 70, 20, 18, 7, 12, H("#E2A25A"));
                    c.RoundRect(76, 70, 15, 13, 5, 12, H("#F6D9A0"), outline: false);
                    c.RoundRect(78, 64, 6, 4, 2, 12, H("#FFE7A0"), outline: false); // butter
                    break;
                case "jam_jar":
                    c.Gloss = 0.6f;
                    c.RoundRect(64, 74, 30, 36, 12, 0, H("#C23A45"));
                    c.Shade = false;
                    c.RoundRect(64, 78, 22, 14, 4, 0, H("#FFF3DC"), outline: false); // label band, no text
                    c.Circle(64, 78, 6, H("#E15B63"), outline: false);
                    c.Shade = true; c.Gloss = 0.1f;
                    c.RoundRect(64, 36, 32, 8, 4, 0, H("#F3E6CF"));
                    c.Polygon(new[] { new Vector2(34, 38), new Vector2(94, 38), new Vector2(88, 46), new Vector2(40, 46) }, H("#D6503A"));
                    break;
                case "flower_vase":
                    c.Gloss = 0.1f;
                    c.Capsule(56, 70, 50, 26, 2.5f, H("#6FA35A"));
                    c.Capsule(66, 70, 74, 30, 2.5f, H("#6FA35A"));
                    c.Ellipse(44, 52, 9, 5, -30, H("#7FB26A"));
                    for (int i = 0; i < 5; i++) c.Circle(50 + Mathf.Cos(i * 1.256f) * 7, 22 + Mathf.Sin(i * 1.256f) * 7, 6, H("#F6C6D4"));
                    c.Circle(50, 22, 4, H("#F6D36B"));
                    for (int i = 0; i < 5; i++) c.Circle(76 + Mathf.Cos(i * 1.256f) * 6, 28 + Mathf.Sin(i * 1.256f) * 6, 5, H("#FFF3E0"));
                    c.Circle(76, 28, 3.5f, H("#F6D36B"));
                    c.Gloss = 0.55f;
                    c.Custom((x, y) =>
                    {
                        float dy = (y - 92) / 26f, half = 12 + 8 * Mathf.Sin(Mathf.Clamp01((y - 68) / 48f) * Mathf.PI);
                        return Mathf.Max(Mathf.Abs(x - 64) - half, Mathf.Abs(dy) * 26 - 24);
                    }, H("#79B6D6"), true, 36, 66, 92, 118);
                    break;
                case "napkin":
                    c.Gloss = 0.05f; c.Bevel = 6;
                    c.Polygon(new[] { new Vector2(20, 40), new Vector2(104, 34), new Vector2(110, 96), new Vector2(26, 102) }, H("#FFFFFF"));
                    c.Shade = false;
                    c.Polygon(new[] { new Vector2(24, 44), new Vector2(100, 39), new Vector2(102, 46), new Vector2(26, 51) }, H("#E8828A"), outline: false);
                    c.Shade = true; c.Gloss = 0.8f;
                    c.Capsule(40, 74, 92, 66, 3.2f, H("#C9D1DA"));
                    c.Ellipse(96, 65, 9, 6, -8, H("#C9D1DA"));
                    break;
                case "sofa":
                    c.RoundRect(64, 74, 58, 26, 12, 0, H("#7C93A8"));                 // body
                    c.RoundRect(64, 52, 48, 18, 10, 0, H("#8BA2B6"));                 // back cushions
                    c.RoundRect(14, 70, 10, 22, 8, 0, H("#6D8498")); c.RoundRect(114, 70, 10, 22, 8, 0, H("#6D8498")); // arms
                    c.RoundRect(44, 74, 18, 10, 6, 0, H("#9AB0C3")); c.RoundRect(84, 74, 18, 10, 6, 0, H("#9AB0C3")); // seats
                    c.RoundRect(40, 56, 11, 10, 5, -12, H("#E4B23E"));                 // a cushion
                    c.RoundRect(22, 102, 3, 6, 1, 0, H("#5E4128")); c.RoundRect(106, 102, 3, 6, 1, 0, H("#5E4128"));
                    break;
                case "armchair":
                    c.RoundRect(64, 76, 40, 26, 12, 0, H("#C98F6E"));
                    c.RoundRect(64, 52, 32, 22, 12, 0, H("#D59E7E"));
                    c.RoundRect(28, 72, 9, 20, 7, 0, H("#B87F60")); c.RoundRect(100, 72, 9, 20, 7, 0, H("#B87F60"));
                    c.RoundRect(40, 104, 3, 6, 1, 0, H("#5E4128")); c.RoundRect(88, 104, 3, 6, 1, 0, H("#5E4128"));
                    break;
                case "floor_lamp":
                    c.Gloss = 0.5f;
                    c.Capsule(64, 40, 64, 112, 2.5f, H("#3E3A36"));
                    c.Ellipse(64, 114, 16, 4, 0, H("#3E3A36"));
                    c.Gloss = 0.1f;
                    c.Polygon(new[] { new Vector2(46, 14), new Vector2(82, 14), new Vector2(92, 44), new Vector2(36, 44) }, H("#F4DFA8"));
                    c.Shade = false; c.Ellipse(64, 44, 26, 5, 0, new Color(1f, 0.95f, 0.75f, 0.9f), outline: false);
                    break;
                case "wall_art":
                    c.RoundRect(64, 60, 40, 32, 3, 0, H("#4A3527"));
                    c.Shade = false;
                    c.RoundRect(64, 60, 34, 26, 2, 0, H("#F6E7CF"), outline: false);
                    c.Circle(80, 50, 7, H("#F2B05E"), outline: false);
                    c.Polygon(new[] { new Vector2(32, 84), new Vector2(54, 58), new Vector2(72, 84) }, H("#7FA88A"), outline: false);
                    c.Polygon(new[] { new Vector2(56, 84), new Vector2(76, 64), new Vector2(96, 84) }, H("#5E8B74"), outline: false);
                    break;
                case "popcorn_bowl":
                    c.Gloss = 0.3f; c.Bevel = 8;
                    for (int i = 0; i < 9; i++) c.Circle(40 + (i % 5) * 12 + (i / 5) * 6, 56 - (i / 5) * 9 + (i % 2) * 3, 9, H("#FFF3D6"));
                    c.Gloss = 0.5f;
                    c.Custom((x, y) => Mathf.Max(Mathf.Sqrt((x - 64) * (x - 64) / 1600f + (y - 62) * (y - 62) / 900f) * 30 - 30, 62 - y), H("#D9473E"), true, 22, 60, 106, 96);
                    c.Shade = false;
                    for (int i = 0; i < 4; i++) c.RoundRect(44 + i * 13, 78, 3, 12, 1, 0, new Color(1, 1, 1, 0.85f), outline: false);
                    break;
                case "tea_cup":
                    c.Gloss = 0.5f;
                    c.Ellipse(64, 84, 34, 10, 0, H("#F3EBDD"));
                    c.Custom((x, y) => Mathf.Max(Mathf.Abs(x - 64) - (20 - (y - 52) * 0.15f), Mathf.Abs(y - 66) - 16), H("#F3EBDD"), true, 40, 48, 88, 84);
                    c.Ellipse(64, 52, 20, 5, 0, H("#B57A43"));
                    c.Ring(88, 66, 8, 2.5f, H("#F3EBDD"));
                    c.Shade = false; c.RoundRect(64, 68, 18, 2, 1, 0, H("#7FA0C9"), outline: false);
                    break;
                case "dining_table":
                    c.RoundRect(64, 62, 58, 9, 4, 0, H("#6B4A36"));
                    c.RoundRect(18, 90, 5, 26, 2, 0, H("#5E4030")); c.RoundRect(110, 90, 5, 26, 2, 0, H("#5E4030"));
                    c.RoundRect(64, 56, 54, 3, 2, 0, H("#F6EFE3"), outline: false);
                    c.RoundRect(30, 40, 9, 16, 4, 0, H("#9DB5C8")); c.RoundRect(98, 40, 9, 16, 4, 0, H("#9DB5C8")); // chair backs
                    break;
                case "sideboard":
                    c.RoundRect(64, 78, 56, 30, 5, 0, H("#7C5A44"));
                    c.RoundRect(42, 80, 20, 22, 3, 0, H("#8E6A52")); c.RoundRect(86, 80, 20, 22, 3, 0, H("#8E6A52"));
                    c.Circle(56, 80, 2.5f, H("#D2AE62")); c.Circle(72, 80, 2.5f, H("#D2AE62"));
                    c.RoundRect(20, 112, 3, 6, 1, 0, H("#4A3426")); c.RoundRect(108, 112, 3, 6, 1, 0, H("#4A3426"));
                    c.Capsule(40, 44, 40, 34, 6, H("#BFDDEA")); c.Ellipse(86, 44, 12, 4, 0, H("#F3EBDD"));
                    break;
                case "pendant_lamp":
                    c.Capsule(64, 0, 64, 52, 1.5f, H("#3E3A36"));
                    c.Gloss = 0.55f;
                    c.Custom((x, y) => Mathf.Max(Mathf.Sqrt((x - 64) * (x - 64) / 1100f + (y - 84) * (y - 84) / 900f) * 30 - 30, 84 - y - 30 > 0 ? 84 - y - 30 : -1f), H("#E9C46A"), true, 30, 54, 98, 86);
                    c.Shade = false; c.Ellipse(64, 86, 30, 5, 0, new Color(1f, 0.96f, 0.8f, 0.95f), outline: false);
                    break;
                default:
                    return KitchenArt.Prop(id);
            }
            return c.ToSprite("scene_" + id);
        }
    }
}
