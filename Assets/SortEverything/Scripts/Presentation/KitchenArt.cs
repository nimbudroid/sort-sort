using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Procedural kitchen environment: tileable surfaces (wall, wood, floor, basket weaves, crate slats, cloth),
    /// container frames and small furniture props. Decorative only: nothing here receives raycasts or affects rules.
    /// </summary>
    public static class KitchenArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => Cache.Clear();

        static Color H(string hex) => SpriteFactory.Hex(hex);

        static Sprite Get(string key, Func<Sprite> make)
        {
            if (Cache.TryGetValue(key, out var s) && s != null && s.texture != null) return s;
            s = make();
            Cache[key] = s;
            return s;
        }

        /// <summary>Builds a repeating tile; pixelsPerUnit sets its on-screen scale (100 = 1 px per canvas unit).</summary>
        static Sprite Tile(string name, int w, int h, float ppu, Func<int, int, Color> px)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var data = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    data[y * w + x] = px(x, h - 1 - y); // callers use y-down
            tex.SetPixels(data);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        }

        static float Hash(int x, int y) { unchecked { int n = x * 374761393 + y * 668265263; n = (n ^ (n >> 13)) * 1274126177; return ((n ^ (n >> 16)) & 0xffff) / 65535f; } }
        static Color Shade(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

        // ---------- Tileable surfaces ----------

        /// <summary>Cream subway tiles with soft grout.</summary>
        public static Sprite WallTile() => Get("wall", () => Tile("wall", 64, 32, 200, (x, y) =>
        {
            int row = y / 16, ly = y % 16;
            int lx = (x + (row % 2) * 16) % 32;
            if (ly == 0 || lx == 0) return H("#E2D4BF");
            float k = 1.04f - ly * 0.006f - (lx > 28 ? 0.03f : 0) + (Hash(x / 32 + row * 7, row) - 0.5f) * 0.03f;
            return Shade(H("#F6EEE2"), k);
        }));

        /// <summary>Warm butcher-block planks running horizontally.</summary>
        public static Sprite Wood() => Get("wood", () => Tile("wood", 128, 32, 200, (x, y) =>
        {
            int plank = y / 8;
            int seam = (int)(Hash(plank, 3) * 128);
            if (y % 8 == 0 || (x + 128 - seam) % 128 == 0) return H("#9C6B3E");
            float grain = Mathf.Sin((x + plank * 37) * 0.09f + Mathf.Sin(x * 0.021f + plank) * 2.2f) * 0.05f;
            float tone = 0.94f + Hash(plank, 9) * 0.12f;
            return Shade(H("#D3A06A"), tone + grain);
        }));

        /// <summary>Darker vertical planks for shelf backs.</summary>
        public static Sprite ShelfBack() => Get("shelfback", () => Tile("shelfback", 32, 64, 200, (x, y) =>
        {
            if (x % 16 == 0) return H("#7E5534");
            float grain = Mathf.Sin(y * 0.12f + (x / 16) * 3 + Mathf.Sin(y * 0.03f) * 2) * 0.04f;
            return Shade(H("#A9784B"), 0.96f + Hash(x / 16, 2) * 0.08f + grain);
        }));

        /// <summary>Two-tone terracotta checker floor.</summary>
        public static Sprite Floor() => Get("floor", () => Tile("floor", 64, 64, 200, (x, y) =>
        {
            if (x % 32 == 0 || y % 32 == 0) return H("#B8A083");
            bool dark = ((x / 32) + (y / 32)) % 2 == 0;
            return Shade(dark ? H("#D2B796") : H("#E8D8C0"), 0.98f + Hash(x / 32, y / 32) * 0.04f);
        }));

        /// <summary>Over-under basket weave (tinted per basket).</summary>
        public static Sprite Weave() => Get("weave", () => Tile("weave", 24, 24, 100, (x, y) =>
        {
            int cx = x / 6, cy = y / 6;
            bool horizontal = (cx + cy) % 2 == 0;
            float along = horizontal ? (x % 6) / 5f : (y % 6) / 5f;
            float across = horizontal ? (y % 6) / 5f : (x % 6) / 5f;
            float k = 0.86f + Mathf.Sin(across * Mathf.PI) * 0.22f - Mathf.Abs(along - 0.5f) * 0.08f;
            if ((horizontal ? y % 6 : x % 6) == 0) k = 0.62f;
            return new Color(k, k, k, 1);
        }));

        /// <summary>Horizontal crate slats with gaps.</summary>
        public static Sprite Slats() => Get("slats", () => Tile("slats", 32, 20, 100, (x, y) =>
        {
            if (y >= 16) return new Color(0.35f, 0.35f, 0.38f, 1);
            float k = 0.92f + Mathf.Sin(y / 15f * Mathf.PI) * 0.12f + Mathf.Sin(x * 0.4f + y) * 0.015f;
            return new Color(k, k, k, 1);
        }));

        /// <summary>Gingham check for serving cloths.</summary>
        public static Sprite Gingham() => Get("gingham", () => Tile("gingham", 16, 16, 100, (x, y) =>
        {
            bool a = (x / 8) % 2 == 0, b = (y / 8) % 2 == 0;
            float alpha = a && b ? 0.55f : a || b ? 0.28f : 0.0f;
            return new Color(0.82f, 0.25f, 0.24f, alpha);
        }));

        /// <summary>Silicone baking mat with a faint guide grid.</summary>
        public static Sprite MatGrid() => Get("mat", () => Tile("mat", 24, 24, 100, (x, y) =>
            x % 12 == 0 || y % 12 == 0 ? new Color(1, 1, 1, 0.55f) : new Color(1, 1, 1, 0f)));

        /// <summary>
        /// Surface/wall material by data ID (area data): a sprite at Resources/SortEverything/Art/Surfaces/&lt;id&gt;
        /// wins; otherwise one of the procedural tiles.
        /// </summary>
        public static Sprite Surface(string id, string baseHex = "#A87653")
        {
            if (!string.IsNullOrEmpty(id))
            {
                var art = Resources.Load<Sprite>("SortEverything/Art/Surfaces/" + id);
                if (art != null) return art;
            }
            switch (id)
            {
                case "subway_tile": return WallTile();
                case "light_stone": return Stone();
                case "pantry_shelf": return ShelfBack();
                case "floor_check": return Floor();
                case "honey_wood": return Wood();
                default: return ButcherBlock(baseHex);
            }
        }

        /// <summary>
        /// Countertop: broad butcher-block planks running one way, low-contrast seams and soft grain, so items read
        /// clearly on top. Mid-tone teak by default (best worst-case contrast with every kitchen item).
        /// </summary>
        public static Sprite ButcherBlock(string baseHex) => Get("butcher_" + baseHex, () => Tile("butcher_" + baseHex, 192, 96, 60, (x, y) =>
        {
            var baseC = H(baseHex);
            int plank = y / 24;
            int seamX = (int)(Hash(plank, 5) * 192);
            if (y % 24 == 0) return Shade(baseC, 0.86f);
            if ((x + 192 - seamX) % 192 == 0) return Shade(baseC, 0.9f);
            float grain = Mathf.Sin((x + plank * 53) * 0.05f + Mathf.Sin(x * 0.013f + plank * 1.7f) * 2.6f) * 0.03f;
            float tone = 0.96f + Hash(plank, 13) * 0.07f;
            float edge = (y % 24) < 2 ? 0.05f : 0f; // soft bevel highlight on each plank (light from above)
            return Shade(baseC, tone + grain + edge);
        }));

        /// <summary>Pale warm stone with soft speckles (alternative counter).</summary>
        public static Sprite Stone() => Get("stone", () => Tile("stone", 64, 64, 200, (x, y) =>
        {
            float n = Hash(x / 3, y / 3) * 0.5f + Hash(x / 9 + 11, y / 9 + 5) * 0.5f;
            float speck = Hash(x, y) > 0.985f ? -0.18f : 0f;
            return Shade(H("#EADFCF"), 0.95f + n * 0.08f + speck);
        }));

        // ---------- Frames ----------

        /// <summary>Rounded frame with a darker rim and lighter inner floor, 9-sliced.</summary>
        public static Sprite Frame(string key, Color rim, Color inner, float rimWidth = 5, int radius = 16) => Get("frame_" + key, () =>
        {
            int n = 64;
            var r = new SpriteFactory.Raster(n) { Outline = 1.5f, Shade = false };
            r.RoundRect(32, 32, 31, 31, radius, 0, rim);
            r.RoundRect(32, 33, 31 - rimWidth, 30 - rimWidth, Mathf.Max(4, radius - rimWidth), 0, inner, outline: false);
            return r.ToSprite("frame_" + key, new Vector4(radius + 4, radius + 4, radius + 4, radius + 4));
        });

        // ---------- Furniture props (128×128 outlined sprites) ----------

        public static Sprite Prop(string id) => Get("prop_" + id, () =>
        {
            var c = new SpriteFactory.Raster(128);
            switch (id)
            {
                case "window":
                    c.RoundRect(64, 60, 50, 50, 6, 0, H("#FFFFFF"));
                    c.RoundRect(64, 60, 42, 42, 3, 0, H("#BFE3F5"), outline: false);
                    c.Ellipse(44, 46, 16, 8, 0, new Color(1, 1, 1, 0.9f), outline: false);
                    c.Ellipse(80, 56, 18, 7, 0, new Color(1, 1, 1, 0.8f), outline: false);
                    c.RoundRect(64, 92, 42, 10, 2, 0, H("#9FD08A"), outline: false);
                    c.RoundRect(64, 60, 2.5f, 42, 1, 0, H("#FFFFFF"), outline: false);
                    c.RoundRect(64, 60, 42, 2.5f, 1, 0, H("#FFFFFF"), outline: false);
                    c.Polygon(new[] { new Vector2(14, 8), new Vector2(40, 8), new Vector2(28, 112), new Vector2(10, 112) }, H("#F0B7A6"));
                    c.Polygon(new[] { new Vector2(88, 8), new Vector2(114, 8), new Vector2(118, 112), new Vector2(100, 112) }, H("#F0B7A6"));
                    c.RoundRect(64, 116, 56, 5, 2, 0, H("#FFFFFF"));
                    break;
                case "stove":
                    c.RoundRect(64, 78, 54, 44, 6, 0, H("#E9E4DC"));
                    c.RoundRect(64, 92, 40, 22, 4, 0, H("#3D3A38"));
                    c.RoundRect(64, 92, 34, 16, 3, 0, H("#5C5753"), outline: false);
                    c.RoundRect(64, 40, 54, 6, 3, 0, H("#BEB7AD"));
                    for (int i = 0; i < 4; i++) c.Circle(34 + i * 20, 54, 4, H("#7E7871"));
                    c.Ellipse(44, 33, 14, 4, 0, H("#3E4249")); c.Ellipse(84, 33, 14, 4, 0, H("#3E4249"));
                    c.Polygon(new[] { new Vector2(30, 4), new Vector2(98, 4), new Vector2(110, 22), new Vector2(18, 22) }, H("#C9C2B8"));
                    break;
                case "pantry_shelf":
                    c.RoundRect(64, 64, 56, 56, 4, 0, H("#B07C4D"));
                    c.RoundRect(64, 64, 48, 50, 2, 0, H("#8B5E38"), outline: false);
                    for (int s = 0; s < 3; s++)
                    {
                        float y = 44 + s * 34;
                        c.RoundRect(64, y, 50, 3, 1, 0, H("#C99360"));
                        for (int j = 0; j < 4; j++)
                        {
                            var col = j % 2 == 0 ? H("#F3E6C8") : H("#E9C27A");
                            c.RoundRect(28 + j * 24, y - 12, 8, 10, 3, 0, col);
                            c.RoundRect(28 + j * 24, y - 23, 7, 2.5f, 1, 0, H("#C0453A"));
                        }
                    }
                    break;
                case "plate_shelf":
                    c.RoundRect(64, 64, 56, 50, 4, 0, H("#F1E9DC"));
                    c.RoundRect(64, 50, 52, 3, 1, 0, H("#C99360"));
                    c.RoundRect(64, 96, 52, 3, 1, 0, H("#C99360"));
                    for (int j = 0; j < 4; j++) { c.Circle(30 + j * 23, 34, 11, H("#FFFFFF")); c.Ring(30 + j * 23, 34, 7, 1, H("#6BA6D9")); }
                    for (int j = 0; j < 3; j++) { c.RoundRect(38 + j * 26, 80, 9, 11, 3, 0, H("#5B97D6")); c.Ring(50 + j * 26, 80, 5, 2, H("#4E89C9")); }
                    break;
                case "table":
                    c.RoundRect(64, 60, 58, 8, 4, 0, H("#C8955E"));
                    c.RoundRect(22, 92, 4, 30, 2, 0, H("#9C6B3E")); c.RoundRect(106, 92, 4, 30, 2, 0, H("#9C6B3E"));
                    c.Ellipse(64, 50, 24, 5, 0, H("#F3D7D0"));
                    c.Circle(64, 44, 9, H("#FFFFFF"));
                    break;
                case "plant":
                    c.Ellipse(50, 50, 12, 26, -25, H("#5DB34A")); c.Ellipse(78, 50, 12, 26, 25, H("#4FA944")); c.Ellipse(64, 40, 11, 28, 0, H("#6DBE52"));
                    c.Polygon(new[] { new Vector2(40, 74), new Vector2(88, 74), new Vector2(80, 118), new Vector2(48, 118) }, H("#D27A5B"));
                    c.RoundRect(64, 76, 26, 5, 2, 0, H("#B8644A"));
                    break;
                case "rail":
                    c.Capsule(8, 20, 120, 20, 3, H("#9AA3AE"));
                    c.Capsule(28, 22, 28, 80, 3, H("#B8C0CC")); c.Ellipse(28, 92, 9, 13, 0, H("#C7CED8"));
                    c.Capsule(64, 22, 64, 70, 3, H("#8A5A3A")); c.RoundRect(64, 88, 12, 14, 4, 0, H("#3C3F46"));
                    c.Capsule(100, 22, 100, 76, 3, H("#B8C0CC")); c.RoundRect(100, 90, 10, 6, 3, 0, H("#C7CED8"));
                    break;
                case "basket":
                    c.Ring(64, 54, 34, 4, H("#A8743F"));
                    c.Polygon(new[] { new Vector2(22, 60), new Vector2(106, 60), new Vector2(94, 112), new Vector2(34, 112) }, H("#C8925A"));
                    for (int i = 0; i < 4; i++) c.RoundRect(64, 70 + i * 11, 38 - i * 3, 1.5f, 1, 0, H("#A8743F"), outline: false);
                    break;
                default:
                    c.RoundRect(64, 64, 40, 40, 8, 0, H("#CCCCCC"));
                    break;
            }
            return c.ToSprite("prop_" + id);
        });

        // ---------- Helpers ----------

        public static Image Tiled(Transform parent, string name, Sprite tile, Color tint)
        {
            var rt = UIKit.Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = tile;
            img.type = Image.Type.Tiled;
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        public static Image Sliced(Transform parent, string name, Sprite frame, Color tint)
        {
            var rt = UIKit.Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = frame;
            img.type = Image.Type.Sliced;
            img.color = tint;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Container look per GDD target style: woven fruit basket, green wicker, drinks crate, kitchenware drawer, shared tray.</summary>
        public static (Color rim, Color inner, Sprite pattern, Color patternTint, string key) ContainerStyle(string categoryOrShared)
        {
            switch (categoryOrShared)
            {
                case "fruit": return (H("#B9834B"), H("#E8C495"), Weave(), new Color(0.86f, 0.66f, 0.42f, 0.55f), "fruit");
                case "vegetables": return (H("#7E9A4E"), H("#CFE0A8"), Weave(), new Color(0.55f, 0.72f, 0.36f, 0.5f), "veg");
                case "drinks": return (H("#5F7F99"), H("#C9DAE7"), Slats(), new Color(0.62f, 0.75f, 0.86f, 0.55f), "drinks");
                case "kitchenware": return (H("#8B5E38"), H("#D9B88E"), ShelfBack(), new Color(1f, 1f, 1f, 0.25f), "kitchen");
                default: return (H("#C9B38E"), H("#FBF4E6"), null, Color.clear, "shared");
            }
        }

        public static (Sprite pattern, Color tint) PropPattern(string prop)
        {
            switch (prop)
            {
                case "serving_cloth_tray": return (Gingham(), Color.white);
                case "pantry_shelf": return (ShelfBack(), new Color(1, 1, 1, 0.5f));
                case "utensil_drawer": return (Wood(), new Color(1, 1, 1, 0.45f));
                case "bottle_crate": return (Slats(), new Color(0.7f, 0.8f, 0.88f, 0.55f));
                case "baking_mat": return (MatGrid(), Color.white);
                default: return (Weave(), new Color(0.9f, 0.75f, 0.55f, 0.35f));
            }
        }
    }
}
