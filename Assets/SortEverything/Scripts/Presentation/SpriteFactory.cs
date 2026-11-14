using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Generates readable placeholder sprites at runtime (rounded, outlined, softly shaded silhouettes).
    /// Registry is keyed by asset ID so final art can replace any entry without changing IDs:
    /// a sprite placed at Resources/SortEverything/Art/&lt;assetId&gt; takes priority.
    /// </summary>
    public static class SpriteFactory
    {
        const int N = 128;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Runtime-created textures are destroyed when Play mode stops. With domain reload disabled the static
        /// cache would keep those dead references (rendering as flat rectangles), so clear it on every Play.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => Cache.Clear();

        public static Sprite Object(string assetId)
        {
            if (Cache.TryGetValue(assetId, out var s) && s != null && s.texture != null) return s;
            var art = Resources.Load<Sprite>("SortEverything/Art/" + assetId);
            if (art == null)
            {
                var c = new Raster(N);
                Draw(c, assetId);
                art = c.ToSprite(assetId);
            }
            Cache[assetId] = art;
            return art;
        }

        public static Sprite CategoryIcon(string categoryId)
        {
            switch (categoryId)
            {
                case "fruit": return Object("apple");
                case "vegetables": return Object("carrot");
                case "drinks": return Object("water_bottle");
                default: return Object("fork");
            }
        }

        public static Sprite RoundedRect(int radius = 18)
        {
            string key = "rr" + radius;
            if (Cache.TryGetValue(key, out var s) && s != null && s.texture != null) return s;
            int size = radius * 2 + 8;
            var r = new Raster(size) { Outline = 0, Shade = false };
            r.RoundRect(size / 2f, size / 2f, size / 2f - 1, size / 2f - 1, radius, 0, Color.white);
            s = r.ToSprite(key, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
            Cache[key] = s;
            return s;
        }

        public static Sprite Circle()
        {
            if (Cache.TryGetValue("circle", out var s) && s != null && s.texture != null) return s;
            var r = new Raster(64) { Outline = 0, Shade = false };
            r.Circle(32, 32, 31, Color.white);
            s = r.ToSprite("circle");
            Cache["circle"] = s;
            return s;
        }

        public static Sprite Ring()
        {
            if (Cache.TryGetValue("ring", out var s) && s != null && s.texture != null) return s;
            var r = new Raster(64) { Outline = 0, Shade = false };
            r.Ring(32, 32, 28, 3.5f, Color.white);
            s = r.ToSprite("ring");
            Cache["ring"] = s;
            return s;
        }

        public static Sprite Shadow()
        {
            if (Cache.TryGetValue("shadow", out var s) && s != null && s.texture != null) return s;
            var tex = new Texture2D(64, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f, dy = (y - 15.5f) / 15.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(0, 0, 0, a * a * 0.55f));
                }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f), 100);
            Cache["shadow"] = s;
            return s;
        }

        /// <summary>Simple pointing hand used for the noninteractive drag illustration.</summary>
        public static Sprite Hand()
        {
            if (Cache.TryGetValue("hand", out var s) && s != null && s.texture != null) return s;
            var r = new Raster(N);
            var skin = Hex("#F4C9A8");
            r.Capsule(64, 30, 64, 70, 11, skin);           // index finger
            r.RoundRect(70, 88, 26, 26, 14, 0, skin);       // palm
            r.Capsule(80, 72, 92, 92, 9, skin);             // other fingers
            r.Capsule(46, 82, 54, 100, 9, skin);            // thumb
            s = r.ToSprite("hand");
            Cache["hand"] = s;
            return s;
        }

        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static void Draw(Raster c, string id)
        {
            switch (id)
            {
                case "apple":
                    c.Circle(50, 72, 34, Hex("#E0433B")); c.Circle(78, 72, 34, Hex("#E0433B"));
                    c.Ellipse(64, 98, 30, 14, 0, Hex("#D23A33"));
                    c.Capsule(64, 40, 66, 22, 4, Hex("#7A4B2A"));
                    c.Ellipse(82, 28, 15, 7, -25, Hex("#5DB34A"));
                    c.Ellipse(48, 58, 7, 12, 25, new Color(1, 1, 1, 0.35f), outline: false);
                    break;
                case "banana":
                {
                    // One crescent band (a single outlined shape, not a chain of circles).
                    var pts = new System.Collections.Generic.List<Vector2>();
                    const int steps = 24;
                    for (int i = 0; i <= steps; i++)
                    {
                        float t = i / (float)steps, ang = Mathf.Lerp(200, 340, t) * Mathf.Deg2Rad;
                        pts.Add(new Vector2(64 + Mathf.Cos(ang) * 50, 34 - Mathf.Sin(ang) * 50));
                    }
                    for (int i = steps; i >= 0; i--)
                    {
                        float t = i / (float)steps, ang = Mathf.Lerp(200, 340, t) * Mathf.Deg2Rad;
                        float r = 50 - (6 + 20 * Mathf.Sin(t * Mathf.PI));
                        pts.Add(new Vector2(64 + Mathf.Cos(ang) * r, 34 - Mathf.Sin(ang) * r));
                    }
                    c.Polygon(pts.ToArray(), Hex("#F6D33C"));
                    for (int i = 4; i <= 20; i++)
                    {
                        float t = i / 24f, ang = Mathf.Lerp(200, 340, t) * Mathf.Deg2Rad;
                        c.Circle(64 + Mathf.Cos(ang) * 40, 34 - Mathf.Sin(ang) * 40, 2.2f, new Color(1f, 0.96f, 0.7f, 0.8f), outline: false);
                    }
                    c.Capsule(17, 54, 11, 46, 4, Hex("#6B4A26"));
                    c.Circle(110, 52, 4, Hex("#6B4A26"));
                    break;
                }
                case "orange":
                    c.Circle(64, 70, 42, Hex("#F58A22"));
                    for (int i = 0; i < 9; i++) c.Circle(40 + (i * 23) % 52, 52 + (i * 17) % 40, 2.2f, Hex("#E57510"), outline: false);
                    c.Ellipse(74, 28, 14, 7, -20, Hex("#5DB34A"));
                    c.Ellipse(50, 54, 8, 13, 25, new Color(1, 1, 1, 0.3f), outline: false);
                    break;
                case "strawberry":
                    c.Polygon(new[] { new Vector2(22, 44), new Vector2(106, 44), new Vector2(64, 116) }, Hex("#E2353F"));
                    c.Circle(42, 52, 22, Hex("#E2353F")); c.Circle(86, 52, 22, Hex("#E2353F")); c.Circle(64, 84, 26, Hex("#E2353F"));
                    for (int i = 0; i < 10; i++) c.Ellipse(40 + (i * 29) % 48, 54 + (i * 19) % 40, 2, 3, 0, Hex("#FFE27A"), outline: false);
                    for (int i = 0; i < 5; i++) c.Ellipse(44 + i * 10, 34, 6, 12, -40 + i * 20, Hex("#4FA944"));
                    break;
                case "carrot":
                    c.Polygon(new[] { new Vector2(40, 40), new Vector2(88, 40), new Vector2(60, 120) }, Hex("#F37B22"));
                    c.Ellipse(64, 42, 25, 9, 0, Hex("#F37B22"));
                    for (int i = 0; i < 3; i++) c.Capsule(52 + i * 4, 60 + i * 18, 70 - i * 2, 62 + i * 18, 1.2f, Hex("#D9611A"), outline: false);
                    c.Ellipse(52, 22, 7, 18, -25, Hex("#4FA944")); c.Ellipse(64, 18, 7, 20, 0, Hex("#5DB34A")); c.Ellipse(76, 22, 7, 18, 25, Hex("#4FA944"));
                    break;
                case "pepper":
                    c.Ellipse(44, 74, 22, 38, 8, Hex("#D9352C")); c.Ellipse(84, 74, 22, 38, -8, Hex("#D9352C"));
                    c.Ellipse(64, 78, 22, 40, 0, Hex("#E8463A"));
                    c.Capsule(64, 36, 70, 16, 5, Hex("#3F8F37"));
                    c.Ellipse(64, 38, 22, 7, 0, Hex("#4FA944"));
                    break;
                case "broccoli":
                    c.Polygon(new[] { new Vector2(52, 70), new Vector2(76, 70), new Vector2(72, 118), new Vector2(56, 118) }, Hex("#9BCB6B"));
                    c.Circle(40, 56, 20, Hex("#3E9A3C")); c.Circle(88, 56, 20, Hex("#3E9A3C"));
                    c.Circle(64, 40, 24, Hex("#46A845")); c.Circle(52, 66, 16, Hex("#3E9A3C")); c.Circle(76, 66, 16, Hex("#3E9A3C"));
                    break;
                case "corn":
                    c.Ellipse(64, 66, 22, 50, 0, Hex("#F7CF3B"));
                    for (int row = 0; row < 8; row++)
                        for (int col = 0; col < 3; col++)
                            c.Circle(54 + col * 10, 28 + row * 10, 3, Hex("#E3B528"), outline: false);
                    c.Ellipse(40, 82, 12, 36, 20, Hex("#6DBE52")); c.Ellipse(88, 82, 12, 36, -20, Hex("#6DBE52"));
                    break;
                case "water_bottle":
                    c.RoundRect(64, 76, 24, 42, 12, 0, Hex("#BFE6FA"));
                    c.RoundRect(64, 30, 12, 10, 4, 0, Hex("#BFE6FA"));
                    c.RoundRect(64, 16, 13, 7, 3, 0, Hex("#2C6FD1"));
                    // Water drop symbol
                    c.Circle(64, 84, 11, Hex("#2F86E0"), outline: false);
                    c.Polygon(new[] { new Vector2(54, 80), new Vector2(74, 80), new Vector2(64, 62) }, Hex("#2F86E0"), outline: false);
                    break;
                case "milk_carton":
                    c.RoundRect(64, 80, 30, 38, 4, 0, Hex("#F7F7F2"));
                    c.Polygon(new[] { new Vector2(34, 44), new Vector2(94, 44), new Vector2(80, 22), new Vector2(48, 22) }, Hex("#E8E8E0"));
                    c.RoundRect(64, 18, 16, 5, 2, 0, Hex("#E8E8E0"));
                    c.RoundRect(64, 84, 26, 14, 4, 0, Hex("#3D7FD9"), outline: false);
                    c.Circle(64, 84, 7, Color.white, outline: false); // milk drop
                    c.Polygon(new[] { new Vector2(58, 82), new Vector2(70, 82), new Vector2(64, 72) }, Color.white, outline: false);
                    break;
                case "juice_carton":
                    c.RoundRect(64, 78, 28, 40, 5, 0, Hex("#FFA62B"));
                    c.Polygon(new[] { new Vector2(36, 40), new Vector2(92, 40), new Vector2(76, 22), new Vector2(52, 22) }, Hex("#F28F16"));
                    c.Capsule(78, 26, 84, 8, 3, Hex("#E24A6A"));
                    c.Circle(64, 82, 16, Hex("#FF7A00"), outline: false);
                    c.Circle(64, 82, 11, Hex("#FFC060"), outline: false);
                    c.Capsule(64, 71, 64, 93, 1.2f, Hex("#FF7A00"), outline: false);
                    c.Capsule(53, 82, 75, 82, 1.2f, Hex("#FF7A00"), outline: false);
                    break;
                case "soda_can":
                    c.RoundRect(64, 76, 30, 34, 10, 0, Hex("#E2453C"));
                    c.RoundRect(64, 40, 27, 6, 4, 0, Hex("#C9CED6"));
                    c.RoundRect(64, 112, 27, 5, 4, 0, Hex("#C9CED6"));
                    c.Circle(56, 84, 7, Color.white, outline: false); c.Circle(72, 72, 5, Color.white, outline: false); c.Circle(68, 92, 4, Color.white, outline: false);
                    break;
                case "spoon":
                    c.Capsule(64, 60, 64, 118, 6, Hex("#B8C0CC"));
                    c.Ellipse(64, 34, 20, 26, 0, Hex("#C7CED8"));
                    c.Ellipse(60, 30, 7, 12, 0, new Color(1, 1, 1, 0.45f), outline: false);
                    break;
                case "fork":
                    c.Capsule(64, 62, 64, 118, 6, Hex("#B8C0CC"));
                    c.RoundRect(64, 50, 20, 10, 6, 0, Hex("#C7CED8"));
                    for (int i = 0; i < 4; i++) c.Capsule(50 + i * 9.3f, 14, 50 + i * 9.3f, 46, 3.2f, Hex("#C7CED8"));
                    break;
                case "plate":
                    c.Circle(64, 66, 52, Hex("#F4F1EA"));
                    c.Ring(64, 66, 36, 2.5f, Hex("#D8D2C4"));
                    c.Ring(64, 66, 48, 2, Hex("#6BA6D9"));
                    break;
                case "mug":
                    c.Ring(96, 70, 17, 6, Hex("#4E89C9"));
                    c.RoundRect(58, 72, 34, 38, 9, 0, Hex("#5B97D6"));
                    c.Ellipse(58, 36, 32, 8, 0, Hex("#2E4E73")); // empty interior
                    break;
                case "spatula":
                    c.Capsule(64, 70, 64, 120, 5.5f, Hex("#8A5A3A"));
                    c.RoundRect(64, 34, 24, 26, 6, 0, Hex("#3C3F46"));
                    for (int i = 0; i < 3; i++) c.RoundRect(52 + i * 12, 30, 2.5f, 15, 1.5f, 0, Hex("#22252B"), outline: false);
                    break;
                case "pan":
                    c.Capsule(92, 82, 124, 108, 7, Hex("#3A2E27"));
                    c.Circle(54, 66, 46, Hex("#3E4249"));
                    c.Circle(54, 66, 36, Hex("#565B64"), outline: false);
                    c.Ellipse(42, 54, 10, 6, -30, new Color(1, 1, 1, 0.25f), outline: false);
                    break;
                default:
                    c.RoundRect(64, 64, 40, 40, 12, 0, Hex("#999999"));
                    break;
            }
        }

        /// <summary>Tiny SDF rasterizer with outlines and soft top-light shading.</summary>
        internal sealed class Raster
        {
            readonly int _n;
            readonly Color[] _px;
            public float Outline = 3f;
            public bool Shade = true;
            static readonly Color Ink = new Color(0.22f, 0.16f, 0.13f, 1f);

            public Raster(int n) { _n = n; _px = new Color[n * n]; }

            void Fill(Func<float, float, float> sdf, Color col, bool outline, float x0, float y0, float x1, float y1)
            {
                float o = outline ? Outline : 0f;
                int ix0 = Mathf.Max(0, (int)(x0 - o - 2)), iy0 = Mathf.Max(0, (int)(y0 - o - 2));
                int ix1 = Mathf.Min(_n - 1, (int)(x1 + o + 2)), iy1 = Mathf.Min(_n - 1, (int)(y1 + o + 2));
                for (int y = iy0; y <= iy1; y++)
                    for (int x = ix0; x <= ix1; x++)
                    {
                        float d = sdf(x + 0.5f, y + 0.5f);
                        int idx = (_n - 1 - y) * _n + x; // art space is y-down
                        if (o > 0)
                        {
                            float ao = Mathf.Clamp01(0.5f - (d - o));
                            if (ao > 0) Blend(idx, Ink, ao * col.a);
                        }
                        float a = Mathf.Clamp01(0.5f - d);
                        if (a > 0)
                        {
                            var c = col;
                            if (Shade && col.a >= 0.99f)
                            {
                                float k = 1.12f - 0.28f * (y / (float)_n);
                                c = new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
                            }
                            Blend(idx, c, a * col.a);
                        }
                    }
            }

            void Blend(int i, Color c, float a)
            {
                var d = _px[i];
                float outA = a + d.a * (1 - a);
                if (outA <= 0) return;
                _px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / outA, (c.g * a + d.g * d.a * (1 - a)) / outA,
                    (c.b * a + d.b * d.a * (1 - a)) / outA, outA);
            }

            public void Circle(float cx, float cy, float r, Color col, bool outline = true) =>
                Fill((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, col, outline, cx - r, cy - r, cx + r, cy + r);

            public void Ring(float cx, float cy, float r, float th, Color col, bool outline = true) =>
                Fill((x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - th, col, outline, cx - r - th, cy - r - th, cx + r + th, cy + r + th);

            public void Ellipse(float cx, float cy, float rx, float ry, float rotDeg, Color col, bool outline = true)
            {
                float cs = Mathf.Cos(rotDeg * Mathf.Deg2Rad), sn = Mathf.Sin(rotDeg * Mathf.Deg2Rad);
                float m = Mathf.Max(rx, ry);
                Fill((x, y) =>
                {
                    float dx = x - cx, dy = y - cy;
                    float lx = dx * cs + dy * sn, ly = -dx * sn + dy * cs;
                    float k = Mathf.Sqrt(lx * lx / (rx * rx) + ly * ly / (ry * ry));
                    return (k - 1f) * Mathf.Min(rx, ry);
                }, col, outline, cx - m, cy - m, cx + m, cy + m);
            }

            public void RoundRect(float cx, float cy, float hx, float hy, float r, float rotDeg, Color col, bool outline = true)
            {
                float cs = Mathf.Cos(rotDeg * Mathf.Deg2Rad), sn = Mathf.Sin(rotDeg * Mathf.Deg2Rad);
                float m = Mathf.Sqrt(hx * hx + hy * hy);
                Fill((x, y) =>
                {
                    float dx = x - cx, dy = y - cy;
                    float lx = Mathf.Abs(dx * cs + dy * sn) - (hx - r), ly = Mathf.Abs(-dx * sn + dy * cs) - (hy - r);
                    float ox = Mathf.Max(lx, 0), oy = Mathf.Max(ly, 0);
                    return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(lx, ly), 0) - r;
                }, col, outline, cx - m, cy - m, cx + m, cy + m);
            }

            public void Capsule(float ax, float ay, float bx, float by, float r, Color col, bool outline = true)
            {
                Fill((x, y) =>
                {
                    float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
                    float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
                    float dx = pax - bax * h, dy = pay - bay * h;
                    return Mathf.Sqrt(dx * dx + dy * dy) - r;
                }, col, outline, Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r);
            }

            public void Polygon(Vector2[] v, Color col, bool outline = true)
            {
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var p in v) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
                Fill((x, y) =>
                {
                    var p = new Vector2(x, y);
                    float d = Vector2.SqrMagnitude(p - v[0]);
                    float s = 1f;
                    for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
                    {
                        var e = v[j] - v[i];
                        var w = p - v[i];
                        var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                        d = Mathf.Min(d, Vector2.Dot(b, b));
                        bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                        if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                    }
                    return s * Mathf.Sqrt(d);
                }, col, outline, x0, y0, x1, y1);
            }

            public Sprite ToSprite(string name, Vector4 border = default)
            {
                var tex = new Texture2D(_n, _n, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                tex.SetPixels(_px);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, _n, _n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
            }
        }
    }
}
