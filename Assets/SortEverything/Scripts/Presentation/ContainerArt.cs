using System;
using System.Collections.Generic;
using SortEverything.Domain;
using UnityEngine;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Procedural looks for container archetypes (basket, fridge door shelf, crisper drawer, cutlery drawer, dish
    /// rack, serving tray) in the same sculpted language as the objects: one upper-left light, the same warm
    /// brown outline, soft volume. Each container is a back (body + interior) and a front lip drawn over the
    /// items so they read as sitting inside. Archetype data may replace either layer with a sprite.
    /// </summary>
    public static class ContainerArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => Cache.Clear();

        static Sprite Get(string key, Func<Sprite> make)
        {
            if (Cache.TryGetValue(key, out var s) && s != null && s.texture != null) return s;
            s = make();
            Cache[key] = s;
            return s;
        }

        static Color H(string hex) => SpriteFactory.Hex(hex);
        static Color Mul(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

        static Sprite Override(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return Resources.Load<Sprite>(path);
        }

        /// <summary>Body + interior, 9-sliced (128 px, 30 px borders).</summary>
        public static Sprite Back(ContainerArchetypeData a) => Override(a.backSprite) ?? Get("back_" + a.id, () =>
        {
            var r = new SpriteFactory.Raster(128) { Outline = 2.2f, Gloss = 0.12f, Bevel = 18f };
            var body = H(a.body);
            var inner = H(a.inner);
            switch (a.style)
            {
                case "fridge":
                    r.RoundRect(64, 64, 62, 62, 18, 0, body);
                    r.Bevel = 6; r.Shade = false;
                    r.RoundRect(64, 67, 54, 54, 12, 0, inner, outline: false);
                    r.RoundRect(64, 18, 50, 5, 5, 0, new Color(1, 1, 1, 0.75f), outline: false);  // interior light strip
                    break;
                case "crisper":
                    r.RoundRect(64, 64, 62, 62, 20, 0, body);
                    r.Shade = false;
                    r.RoundRect(64, 67, 55, 55, 14, 0, inner, outline: false);
                    for (int i = 0; i < 5; i++) r.RoundRect(18 + i * 23, 70, 2.5f, 48, 2, 0, new Color(1, 1, 1, 0.22f), outline: false); // ridges
                    break;
                case "drawer":
                    r.RoundRect(64, 64, 62, 62, 10, 0, body);
                    r.Shade = false;
                    r.RoundRect(64, 66, 54, 54, 6, 0, inner, outline: false);
                    r.RoundRect(64, 16, 54, 5, 3, 0, Mul(inner, 0.86f), outline: false); // back wall shadow
                    break;
                case "rack":
                    r.RoundRect(64, 64, 62, 62, 14, 0, body);
                    r.Shade = false;
                    r.RoundRect(64, 66, 55, 55, 10, 0, inner, outline: false);
                    break;
                case "tray":
                    r.RoundRect(64, 64, 62, 62, 16, 0, body);
                    r.Shade = false;
                    r.RoundRect(64, 66, 54, 54, 10, 0, inner, outline: false);
                    break;
                default: // basket
                    r.RoundRect(64, 64, 62, 62, 24, 0, body);
                    r.Shade = false;
                    r.RoundRect(64, 67, 54, 54, 18, 0, inner, outline: false);
                    break;
            }
            // Soft inner shadow under the rim (one light, from the upper left).
            r.RoundRect(64, 14, 54, 4, 4, 0, new Color(0.3f, 0.18f, 0.1f, 0.18f), outline: false);
            return r.ToSprite("back_" + a.id, new Vector4(30, 30, 30, 30));
        });

        /// <summary>The front lip that overlaps the bottom of the items (9-sliced horizontally).</summary>
        public static Sprite Front(ContainerArchetypeData a) => Override(a.frontSprite) ?? Get("front_" + a.id, () =>
        {
            var r = new SpriteFactory.Raster(128) { Outline = 2.2f, Gloss = 0.2f, Bevel = 10f };
            var body = H(a.body);
            var accent = H(a.accent);
            // The band fills the whole sprite; BoardView sizes it to the lip height of each bin.
            switch (a.style)
            {
                case "fridge":
                    // Clear plastic door rail with a bright top edge.
                    r.Shade = false;
                    r.RoundRect(64, 64, 62, 60, 18, 0, new Color(accent.r, accent.g, accent.b, 0.45f));
                    r.RoundRect(64, 14, 56, 5, 5, 0, new Color(1, 1, 1, 0.9f), outline: false);
                    break;
                case "crisper":
                    r.Shade = false;
                    r.RoundRect(64, 64, 62, 60, 18, 0, new Color(0.93f, 0.97f, 0.9f, 0.82f));
                    r.RoundRect(64, 30, 20, 8, 8, 0, Mul(accent, 0.9f)); // handle slot
                    break;
                case "drawer":
                    r.RoundRect(64, 64, 62, 60, 10, 0, accent);
                    r.RoundRect(64, 60, 18, 7, 7, 0, H("#7D8792"));      // metal pull
                    break;
                case "rack":
                    r.Shade = false;
                    for (int i = 0; i < 9; i++) r.RoundRect(8 + i * 14, 64, 2.2f, 56, 2, 0, accent);
                    r.Shade = true;
                    r.Capsule(6, 14, 122, 14, 7, accent);
                    r.Capsule(6, 112, 122, 112, 7, accent);
                    break;
                case "tray":
                    r.RoundRect(64, 64, 62, 60, 14, 0, accent);
                    break;
                default: // basket: woven front band with a rolled rim
                    r.RoundRect(64, 70, 62, 54, 18, 0, Mul(body, 0.95f));
                    r.Shade = false;
                    for (int i = 0; i < 9; i++) r.RoundRect(10 + i * 13.5f, 76, 4.5f, 40, 3, 0, new Color(1, 1, 1, 0.16f), outline: false);
                    r.Shade = true;
                    r.Capsule(8, 18, 120, 18, 12, Mul(body, 1.08f));
                    break;
            }
            return r.ToSprite("front_" + a.id, new Vector4(30, 0, 30, 0));
        });

        /// <summary>Optional tiled interior pattern.</summary>
        public static (Sprite tile, Color tint) Pattern(ContainerArchetypeData a)
        {
            switch (a.pattern)
            {
                case "weave": return (KitchenArt.Weave(), new Color(Mul(H(a.body), 1.1f).r, Mul(H(a.body), 1.1f).g, Mul(H(a.body), 1.1f).b, 0.55f));
                case "slats": return (KitchenArt.Slats(), new Color(1, 1, 1, 0.4f));
                default: return (null, Color.clear);
            }
        }

        /// <summary>A soft rounded shadow that grounds a container on its shelf.</summary>
        public static Sprite GroundShadow() => Get("ground", () =>
        {
            var r = new SpriteFactory.Raster(64) { Outline = 0, Shade = false };
            for (int i = 0; i < 8; i++) r.RoundRect(32, 32, 30 - i * 1.5f, 30 - i * 1.5f, 14, 0, new Color(0.25f, 0.15f, 0.08f, 0.06f), outline: false);
            return r.ToSprite("ground", new Vector4(22, 22, 22, 22));
        });

        /// <summary>A rounded check mark used for "done" stamps (the UI font has no ✓ glyph on device).</summary>
        public static Sprite Check() => Get("check", () =>
        {
            var r = new SpriteFactory.Raster(64) { Outline = 0, Shade = false };
            r.Capsule(17, 33, 27, 44, 4.5f, Color.white, outline: false);
            r.Capsule(27, 44, 47, 21, 4.5f, Color.white, outline: false);
            return r.ToSprite("check");
        });
    }
}
