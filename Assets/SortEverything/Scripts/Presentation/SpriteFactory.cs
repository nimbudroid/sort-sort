using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Generates the game's sprites at runtime in a cozy 2.5D style: rounded volumes lit from the upper-left,
    /// per-material highlights, warm shadows and thin warm-brown outlines (see Docs/ArtDirection.md).
    /// Registry is keyed by asset ID so final art can replace any entry without changing IDs:
    /// a sprite at Resources/SortEverything/Art/&lt;assetId&gt; takes priority.
    /// </summary>
    public static class SpriteFactory
    {
        /// <summary>Object sprites render at 256×256 from a 128-unit design grid.</summary>
        const int ObjectPixels = 256;
        const float Design = 128f;
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
            if (art == null) art = Generated(assetId);
            Cache[assetId] = art;
            return art;
        }

        /// <summary>Always the code-drawn version (used by the gallery and export even when an override exists).</summary>
        public static Sprite Generated(string assetId)
        {
            var c = new Raster(ObjectPixels, Design);
            Draw(c, assetId);
            return c.ToSprite(assetId);
        }

        public static Sprite CategoryIcon(string categoryId)
        {
            switch (categoryId)
            {
                case "fruit": return Object("apple");
                case "vegetables": return Object("carrot");
                case "drinks": return Object("water_bottle");
                default: return Object("mug"); // reads best at icon size
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
                    tex.SetPixel(x, y, new Color(0.25f, 0.15f, 0.08f, a * a * 0.5f));
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
            var r = new Raster(ObjectPixels, Design) { Gloss = 0.15f, Bevel = 10 };
            var skin = Hex("#F4C9A8");
            r.Capsule(64, 30, 64, 70, 11, skin);
            r.RoundRect(70, 88, 26, 26, 14, 0, skin);
            r.Capsule(80, 72, 92, 92, 9, skin);
            r.Capsule(46, 82, 54, 100, 9, skin);
            s = r.ToSprite("hand");
            Cache["hand"] = s;
            return s;
        }

        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        // ----------------------------------------------------------------------------------------------
        // Object designs (128-unit design grid, y down). Each part sets its material: Gloss (0 matte → 1
        // glossy/metal) and Bevel (how rounded the volume is). Decals use outline:false and stay flat.
        // ----------------------------------------------------------------------------------------------

        static void Mat(Raster c, float gloss, float bevel) { c.Gloss = gloss; c.Bevel = bevel; }

        static void Draw(Raster c, string id)
        {
            switch (id)
            {
                case "apple":
                    Mat(c, 0.75f, 26);
                    // One smooth silhouette: two shoulders + a rounded base, merged (single outline, continuous light).
                    c.Custom((x, y) => Sd.SMin(Sd.SMin(Sd.Circle(x, y, 47, 72, 36), Sd.Circle(x, y, 81, 72, 36), 10),
                        Sd.Ellipse(x, y, 64, 96, 34, 20), 12), Hex("#E2463A"), true, 10, 34, 118, 118);
                    c.Ellipse(64, 42, 12, 5, 0, Hex("#B8322A"), outline: false); // stem dimple
                    c.Ellipse(64, 112, 9, 3, 0, Hex("#B8322A"), outline: false);
                    Mat(c, 0.2f, 4);
                    c.Capsule(63, 42, 67, 20, 4.5f, Hex("#7A4B2A"));
                    Mat(c, 0.35f, 7);
                    c.Ellipse(84, 26, 17, 8, -28, Hex("#62B54C"));
                    c.Capsule(72, 31, 96, 21, 0.9f, Hex("#4C9A3A"), outline: false);
                    break;

                case "banana":
                {
                    var pts = new List<Vector2>();
                    const int steps = 28;
                    for (int i = 0; i <= steps; i++)
                    {
                        float t = i / (float)steps, ang = Mathf.Lerp(198, 342, t) * Mathf.Deg2Rad;
                        pts.Add(new Vector2(64 + Mathf.Cos(ang) * 52, 30 - Mathf.Sin(ang) * 52));
                    }
                    for (int i = steps; i >= 0; i--)
                    {
                        float t = i / (float)steps, ang = Mathf.Lerp(198, 342, t) * Mathf.Deg2Rad;
                        float r = 52 - (10 + 26 * Mathf.Sin(t * Mathf.PI));
                        pts.Add(new Vector2(64 + Mathf.Cos(ang) * r, 30 - Mathf.Sin(ang) * r));
                    }
                    Mat(c, 0.45f, 12);
                    c.Polygon(pts.ToArray(), Hex("#F7D447"));
                    // Second, slightly offset banana behind gives a chunky little bunch.
                    Mat(c, 0.2f, 4);
                    c.Capsule(16, 52, 10, 44, 4.5f, Hex("#7C5A2E"));
                    c.Circle(111, 50, 4.5f, Hex("#5E4426"));
                    for (int i = 6; i <= 22; i += 8)
                    {
                        float t = i / 28f, ang = Mathf.Lerp(198, 342, t) * Mathf.Deg2Rad;
                        c.Circle(64 + Mathf.Cos(ang) * 46, 30 - Mathf.Sin(ang) * 46, 1.4f, Hex("#C9A53A"), outline: false);
                    }
                    break;
                }

                case "orange":
                    Mat(c, 0.4f, 30);
                    c.Circle(64, 72, 44, Hex("#F7922A"));
                    for (int i = 0; i < 14; i++) c.Circle(36 + (i * 23) % 58, 50 + (i * 17) % 48, 1.5f, Hex("#E57A18"), outline: false);
                    Mat(c, 0.2f, 3);
                    c.Circle(64, 30, 4, Hex("#8A6A2E"));
                    Mat(c, 0.35f, 7);
                    c.Ellipse(80, 25, 16, 7.5f, -18, Hex("#5DB34A"));
                    break;

                case "strawberry":
                    Mat(c, 0.7f, 22);
                    // Rounded heart-shaped berry as one merged shape.
                    c.Custom((x, y) => Sd.SMin(Sd.SMin(Sd.Circle(x, y, 44, 56, 25), Sd.Circle(x, y, 84, 56, 25), 14),
                        Sd.Poly(x, y, new[] { new Vector2(24, 56), new Vector2(104, 56), new Vector2(68, 116), new Vector2(60, 116) }), 16),
                        Hex("#E53A44"), true, 16, 28, 112, 118);
                    for (int i = 0; i < 16; i++)
                        c.Ellipse(38 + (i * 29) % 54, 52 + (i * 19) % 48, 1.6f, 2.6f, 10, Hex("#FFE38A"), outline: false);
                    Mat(c, 0.3f, 6);
                    for (int i = 0; i < 5; i++) c.Ellipse(42 + i * 11, 36, 7, 13, -44 + i * 22, Hex("#4FAA45"));
                    c.Capsule(64, 32, 66, 18, 3, Hex("#3F8F37"));
                    break;

                case "carrot":
                    Mat(c, 0.1f, 18);
                    c.Polygon(new[] { new Vector2(38, 42), new Vector2(90, 42), new Vector2(66, 120), new Vector2(60, 120) }, Hex("#F47F25"));
                    c.Ellipse(64, 43, 26, 10, 0, Hex("#F47F25"));
                    for (int i = 0; i < 4; i++) c.Capsule(50 + i * 3, 58 + i * 15, 66 - i * 1.5f, 60 + i * 15, 1.1f, Hex("#D9631A"), outline: false);
                    Mat(c, 0.25f, 6);
                    c.Ellipse(50, 22, 8, 19, -26, Hex("#4FAA45")); c.Ellipse(78, 22, 8, 19, 26, Hex("#4FAA45"));
                    c.Ellipse(64, 17, 8, 21, 0, Hex("#62B54C"));
                    break;

                case "pepper":
                    Mat(c, 0.85f, 22);
                    c.Custom((x, y) => Sd.SMin(Sd.SMin(Sd.Ellipse(x, y, 44, 78, 24, 39), Sd.Ellipse(x, y, 84, 78, 24, 39), 10),
                        Sd.Ellipse(x, y, 64, 82, 23, 40), 8), Hex("#E2402F"), true, 18, 36, 110, 124);
                    c.Capsule(52, 54, 49, 104, 1.6f, Hex("#B92E22"), outline: false);  // lobe grooves
                    c.Capsule(76, 54, 79, 104, 1.6f, Hex("#B92E22"), outline: false);
                    Mat(c, 0.35f, 6);
                    c.Ellipse(64, 40, 24, 8, 0, Hex("#4FAA45"));
                    c.Capsule(64, 38, 70, 15, 5.5f, Hex("#3F8F37"));
                    break;

                case "broccoli":
                    Mat(c, 0.08f, 16);
                    c.Polygon(new[] { new Vector2(50, 70), new Vector2(78, 70), new Vector2(74, 120), new Vector2(54, 120) }, Hex("#A5D27A"));
                    Mat(c, 0.05f, 13);
                    c.Circle(38, 58, 21, Hex("#3F9C3D")); c.Circle(90, 58, 21, Hex("#3F9C3D"));
                    c.Circle(64, 40, 25, Hex("#4AAA47")); c.Circle(51, 68, 17, Hex("#3F9C3D")); c.Circle(77, 68, 17, Hex("#3F9C3D"));
                    for (int i = 0; i < 12; i++) c.Circle(34 + (i * 31) % 62, 34 + (i * 13) % 38, 2.2f, Hex("#5DBA55"), outline: false);
                    break;

                case "corn":
                    Mat(c, 0.3f, 18);
                    c.Ellipse(64, 62, 25, 52, 0, Hex("#F8D040"));
                    for (int row = 0; row < 9; row++)
                        for (int col = 0; col < 4; col++)
                            c.Circle(52 + col * 8 + (row % 2) * 4 - 2, 22 + row * 9.5f, 2.6f, Hex("#FBE27A"), outline: false);
                    Mat(c, 0.2f, 10);
                    // Husk wraps only the lower part so the cob stays readable.
                    c.Polygon(new[] { new Vector2(34, 74), new Vector2(54, 84), new Vector2(62, 120), new Vector2(44, 118) }, Hex("#72C056"));
                    c.Polygon(new[] { new Vector2(94, 74), new Vector2(74, 84), new Vector2(66, 120), new Vector2(84, 118) }, Hex("#64B24C"));
                    break;

                case "water_bottle":
                    Mat(c, 0.85f, 14);
                    c.RoundRect(64, 78, 27, 42, 13, 0, Hex("#C4E8FA"));
                    c.RoundRect(64, 31, 14, 9, 5, 0, Hex("#C4E8FA"));
                    c.Ellipse(64, 58, 25, 3, 0, Hex("#A8D7EF"), outline: false); // ribbing
                    c.Ellipse(64, 102, 25, 3, 0, Hex("#A8D7EF"), outline: false);
                    Mat(c, 0.5f, 6);
                    c.RoundRect(64, 17, 15, 8, 4, 0, Hex("#2F74D4"));
                    // Water drop symbol
                    c.Circle(64, 85, 11, Hex("#2F86E0"), outline: false);
                    c.Polygon(new[] { new Vector2(54, 81), new Vector2(74, 81), new Vector2(64, 63) }, Hex("#2F86E0"), outline: false);
                    break;

                case "milk_carton":
                    Mat(c, 0.15f, 6);
                    // Three-quarter carton: front face, shaded side face, gable roof.
                    c.Polygon(new[] { new Vector2(30, 46), new Vector2(82, 46), new Vector2(82, 118), new Vector2(30, 118) }, Hex("#F8F7F1"));
                    c.Polygon(new[] { new Vector2(82, 46), new Vector2(100, 38), new Vector2(100, 110), new Vector2(82, 118) }, Hex("#DCDCD2"));
                    c.Polygon(new[] { new Vector2(30, 46), new Vector2(82, 46), new Vector2(70, 22), new Vector2(42, 22) }, Hex("#ECECE4"));
                    c.Polygon(new[] { new Vector2(82, 46), new Vector2(100, 38), new Vector2(86, 16), new Vector2(70, 22) }, Hex("#D0D0C6"));
                    c.RoundRect(56, 18, 15, 5, 2, 0, Hex("#ECECE4"));
                    c.RoundRect(56, 84, 25, 15, 4, 0, Hex("#3D7FD9"), outline: false);
                    c.Circle(56, 87, 7, Color.white, outline: false);
                    c.Polygon(new[] { new Vector2(50, 85), new Vector2(62, 85), new Vector2(56, 74) }, Color.white, outline: false);
                    break;

                case "juice_carton":
                    Mat(c, 0.2f, 6);
                    c.Polygon(new[] { new Vector2(30, 42), new Vector2(82, 42), new Vector2(82, 118), new Vector2(30, 118) }, Hex("#FFA930"));
                    c.Polygon(new[] { new Vector2(82, 42), new Vector2(100, 34), new Vector2(100, 110), new Vector2(82, 118) }, Hex("#E78A18"));
                    c.Polygon(new[] { new Vector2(30, 42), new Vector2(82, 42), new Vector2(100, 34), new Vector2(48, 34) }, Hex("#FFC261"));
                    Mat(c, 0.4f, 3);
                    c.Capsule(76, 36, 86, 10, 3, Hex("#E24A6A"));
                    c.Circle(56, 82, 17, Hex("#FF7A00"), outline: false);
                    c.Circle(56, 82, 12, Hex("#FFC060"), outline: false);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * Mathf.PI / 4;
                        c.Capsule(56 - Mathf.Cos(a) * 11, 82 - Mathf.Sin(a) * 11, 56 + Mathf.Cos(a) * 11, 82 + Mathf.Sin(a) * 11, 1.1f, Hex("#FF7A00"), outline: false);
                    }
                    break;

                case "soda_can":
                    Mat(c, 0.9f, 12);
                    c.RoundRect(64, 78, 32, 38, 10, 0, Hex("#E2463C"));
                    c.Ellipse(64, 41, 30, 9, 0, Hex("#D3D8E0"));   // top lid seen from above
                    c.Ellipse(64, 41, 22, 6, 0, Hex("#B8BEC8"), outline: false);
                    c.RoundRect(70, 39, 6, 3, 1.5f, 0, Hex("#8E959F"), outline: false); // tab
                    c.RoundRect(64, 114, 30, 4, 3, 0, Hex("#C6CCD5"));
                    c.Circle(54, 86, 8, Color.white, outline: false); c.Circle(72, 72, 5.5f, Color.white, outline: false); c.Circle(70, 96, 4.5f, Color.white, outline: false);
                    break;

                case "spoon":
                    Mat(c, 0.95f, 8);
                    c.Capsule(64, 58, 66, 120, 7.5f, Hex("#B9C2CE"));
                    Mat(c, 0.95f, 14);
                    c.Ellipse(63, 34, 23, 29, -4, Hex("#C9D0DA"));
                    c.Ellipse(62, 36, 15, 21, -4, Hex("#AEB7C3"), outline: false); // bowl hollow
                    break;

                case "fork":
                    Mat(c, 0.95f, 8);
                    c.Capsule(64, 64, 66, 120, 7.5f, Hex("#B9C2CE"));
                    c.RoundRect(64, 52, 23, 12, 8, 0, Hex("#C9D0DA"));
                    for (int i = 0; i < 4; i++) c.Capsule(48 + i * 10.7f, 14, 48 + i * 10.7f, 48, 4.2f, Hex("#C9D0DA"));
                    break;

                case "plate":
                    Mat(c, 0.6f, 10);
                    c.Ellipse(64, 68, 56, 44, 0, Hex("#F6F2EA"));     // three-quarter view
                    c.Ellipse(64, 66, 38, 29, 0, Hex("#ECE6DA"), outline: false);
                    DrawEllipseRing(c, 64, 68, 50, 39, 2.2f, Hex("#6BA6D9"));
                    break;

                case "mug":
                    Mat(c, 0.55f, 10);
                    DrawEllipseRing(c, 98, 76, 15, 19, 6.5f, Hex("#4E89C9"), outline: true);
                    Mat(c, 0.55f, 16);
                    c.RoundRect(58, 78, 35, 38, 10, 0, Hex("#5B97D6"));
                    c.Ellipse(58, 42, 35, 10, 0, Hex("#6FA6E0"));                      // rim
                    c.Ellipse(58, 43, 29, 7, 0, Hex("#2E4E73"), outline: false);       // empty interior
                    c.Ellipse(58, 46, 26, 4, 0, Hex("#24405F"), outline: false);
                    break;

                case "spatula":
                    Mat(c, 0.3f, 6);
                    c.Capsule(64, 70, 66, 122, 6.5f, Hex("#A06A42"));
                    c.RoundRect(64, 66, 9, 6, 3, 0, Hex("#9AA3AE"));
                    Mat(c, 0.25f, 9);
                    c.RoundRect(64, 34, 26, 28, 8, 0, Hex("#3E4249"));
                    for (int i = 0; i < 3; i++) c.RoundRect(52 + i * 12, 30, 2.6f, 15, 1.5f, 0, Hex("#24272C"), outline: false);
                    break;

                case "pan":
                    Mat(c, 0.35f, 7);
                    c.Capsule(88, 82, 116, 104, 7, Hex("#4A3A30"));
                    Mat(c, 0.6f, 12);
                    c.Ellipse(52, 68, 45, 36, 0, Hex("#43474F"));          // three-quarter pan
                    c.Ellipse(52, 64, 36, 27, 0, Hex("#5E636C"), outline: false);
                    c.Ellipse(52, 66, 30, 21, 0, Hex("#4E535B"), outline: false);
                    break;

                default:
                    Mat(c, 0.3f, 14);
                    c.RoundRect(64, 64, 40, 40, 12, 0, Hex("#999999"));
                    break;
            }
        }

        /// <summary>Signed-distance helpers for merged silhouettes (design units).</summary>
        static class Sd
        {
            public static float Circle(float x, float y, float cx, float cy, float r) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
            public static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
            }
            public static float Poly(float x, float y, Vector2[] v)
            {
                var p = new Vector2(x, y);
                float d = Vector2.SqrMagnitude(p - v[0]), s = 1f;
                for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
                {
                    var e = v[j] - v[i]; var w = p - v[i];
                    var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                    d = Mathf.Min(d, Vector2.Dot(b, b));
                    bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * Mathf.Sqrt(d);
            }
            /// <summary>Smooth union: blends two shapes into one soft silhouette.</summary>
            public static float SMin(float a, float b, float k)
            {
                float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
                return Mathf.Lerp(b, a, h) - k * h * (1 - h);
            }
        }

        /// <summary>Elliptical band (for plate rims and mug handles).</summary>
        static void DrawEllipseRing(Raster c, float cx, float cy, float rx, float ry, float th, Color col, bool outline = false) =>
            c.Custom((x, y) =>
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                float k = Mathf.Sqrt(dx * dx + dy * dy);
                return Mathf.Abs(k - 1f) * Mathf.Min(rx, ry) - th;
            }, col, outline, cx - rx - th, cy - ry - th, cx + rx + th, cy + ry + th);

        /// <summary>
        /// SDF rasterizer. Shapes are given on a design grid (Design units) and rendered at N pixels.
        /// Outlined shapes get 2.5D lighting: a rounded height field from the distance to the edge, lit from the
        /// upper-left, with warm shadows and a material-dependent specular highlight. Decals stay flat.
        /// </summary>
        internal sealed class Raster
        {
            readonly int _n;
            readonly float _s;
            readonly Color[] _px;
            public float Outline = 1.7f;
            public bool Shade = true;
            public float Gloss = 0.3f;
            public float Bevel = 14f;
            public Color OutlineColor = new Color(0.29f, 0.2f, 0.14f, 1f);
            static readonly Vector3 Light = new Vector3(-0.48f, -0.62f, 0.62f).normalized;
            static readonly Vector3 Half = (new Vector3(-0.48f, -0.62f, 0.62f).normalized + Vector3.forward).normalized;

            public Raster(int n) : this(n, n) { }
            public Raster(int n, float design) { _n = n; _s = n / design; _px = new Color[n * n]; }

            public void Custom(Func<float, float, float> sdf, Color col, bool outline, float x0, float y0, float x1, float y1) =>
                Fill(sdf, col, outline, x0, y0, x1, y1);

            void Fill(Func<float, float, float> sdf, Color col, bool outline, float x0, float y0, float x1, float y1)
            {
                float o = outline ? Outline : 0f;           // design units
                int ix0 = Mathf.Max(0, (int)((x0 - o - 2) * _s)), iy0 = Mathf.Max(0, (int)((y0 - o - 2) * _s));
                int ix1 = Mathf.Min(_n - 1, (int)((x1 + o + 2) * _s)), iy1 = Mathf.Min(_n - 1, (int)((y1 + o + 2) * _s));
                bool lit = Shade && outline && col.a >= 0.99f;
                float bevel = Mathf.Max(1f, Bevel);
                float e = 0.6f / _s;
                float shininess = 6f + Gloss * 42f;
                for (int y = iy0; y <= iy1; y++)
                    for (int x = ix0; x <= ix1; x++)
                    {
                        float u = (x + 0.5f) / _s, v = (y + 0.5f) / _s;
                        float dd = sdf(u, v);                    // design units
                        float dPix = dd * _s;
                        int idx = (_n - 1 - y) * _n + x;          // art space is y-down
                        if (o > 0)
                        {
                            float ao = Mathf.Clamp01(0.5f - (dPix - o * _s));
                            if (ao > 0) Blend(idx, OutlineColor, ao * col.a);
                        }
                        float a = Mathf.Clamp01(0.5f - dPix);
                        if (a <= 0) continue;
                        var c = col;
                        if (lit)
                        {
                            float t = Mathf.Clamp01(-dd / bevel);
                            float gx = (sdf(u + e, v) - sdf(u - e, v)) / (2 * e);
                            float gy = (sdf(u, v + e) - sdf(u, v - e)) / (2 * e);
                            float slope = Mathf.Pow(1f - t, 1.6f) * 1.9f;
                            var n = new Vector3(gx * slope, gy * slope, 1f).normalized;
                            float diff = Vector3.Dot(n, Light);
                            float k = Mathf.Clamp(0.93f + 0.62f * (diff - Light.z), 0.58f, 1.18f);
                            k -= 0.06f * (y / (float)_n - 0.5f);                              // soft top-to-bottom falloff
                            k += (Noise(x / 5, y / 5) - 0.5f) * 0.025f;                         // handcrafted variation
                            float warm = 1f - k;                                               // warm, not grey, shadows
                            c = new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * (k - warm * 0.07f)), Mathf.Clamp01(c.b * (k - warm * 0.16f)), c.a);
                            float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, Half)), shininess) * Gloss * 0.7f;
                            spec += Mathf.Pow(Mathf.Clamp01(1f - t), 6f) * Mathf.Max(0, Vector3.Dot(n, new Vector3(0.6f, 0.7f, 0.3f).normalized)) * 0.12f; // soft rim bounce
                            c = new Color(Mathf.Clamp01(c.r + spec), Mathf.Clamp01(c.g + spec * 0.97f), Mathf.Clamp01(c.b + spec * 0.88f), c.a);
                        }
                        Blend(idx, c, a * col.a);
                    }
            }

            static float Noise(int x, int y) { unchecked { int n = x * 374761393 + y * 668265263; n = (n ^ (n >> 13)) * 1274126177; return ((n ^ (n >> 16)) & 0xffff) / 65535f; } }

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
