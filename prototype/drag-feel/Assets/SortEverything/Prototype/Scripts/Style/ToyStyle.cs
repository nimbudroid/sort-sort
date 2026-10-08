using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Visual style prototype: "colourful digital toy box". Palette, fonts and the world-space toy sprites
    /// (outlined chunky panels, outlined eyes, soft shadows, gradient background). Visual only.
    /// </summary>
    public static class ToyStyle
    {
        // ---- UI palette -------------------------------------------------------------------------
        public static readonly Color Ink = Hex("2A2347");        // dark navy outline / depth
        public static readonly Color Primary = Hex("3FD436");    // vivid green: primary actions
        public static readonly Color Secondary = Hex("1E96FF");  // saturated blue: secondary actions
        public static readonly Color Purple = Hex("9447FF");     // purple: secondary actions
        public static readonly Color Reward = Hex("FFC800");     // yellow/gold: rewards
        public static readonly Color Warning = Hex("FF8A00");    // orange: warnings
        public static readonly Color Danger = Hex("FF3D3D");     // red: failure/danger
        public static readonly Color Inactive = Hex("ABA2C8");   // muted grey-purple: inactive
        public static readonly Color Card = Hex("4A3880");       // panel card
        public static readonly Color Paper = Hex("FFFFFF");

        // ---- world palette ----------------------------------------------------------------------
        public static readonly Color SkyTop = Hex("BFE0FF");
        public static readonly Color SkyBottom = Hex("E8F3FF");
        public static readonly Color FloorColor = Hex("A7E3C2");
        public static readonly Color Wood = Hex("E6A468");

        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }

        static Rgba ToRgba(Color c) { return new Rgba(c.r, c.g, c.b, c.a); }

        // ---- fonts ------------------------------------------------------------------------------
        static Font display, body;

        /// <summary>Titan One: chunky rounded display font for game text and buttons.</summary>
        public static Font Display
        {
            get
            {
                if (display == null) display = Resources.Load<Font>("Fonts/TitanOne-Regular");
                return display != null ? display : Hud.BuiltinFont;
            }
        }

        /// <summary>Nunito Black: rounded, very readable font for small text.</summary>
        public static Font Body
        {
            get
            {
                if (body == null) body = Resources.Load<Font>("Fonts/Nunito-Black");
                return body != null ? body : Hud.BuiltinFont;
            }
        }

        // ---- textures ---------------------------------------------------------------------------
        public static Texture2D ToTexture(byte[] rgba, int w, int h, string name, FilterMode filter = FilterMode.Bilinear)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = filter;
            tex.LoadRawTextureData(rgba);
            tex.Apply(false, true);
            return tex;
        }

        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        // World pixels: 4 px of outline ≈ 2.5 dp on screen.
        static float WorldPerPx { get { return Units.DpToWorld(2.5f) / 4f; } }

        /// <summary>9-sliced chunky toy panel for world sprites (bins, table, floor). Use SpriteDrawMode.Sliced.</summary>
        public static Sprite Panel(Color face, bool extrude)
        {
            string key = "panel_" + ColorUtility.ToHtmlStringRGB(face) + (extrude ? "_e" : "");
            Sprite s;
            if (sprites.TryGetValue(key, out s)) return s;
            const int size = 64;
            var spec = new ToyPanelSpec
            {
                width = size, height = size, radius = 20f, outline = 5f, extrude = extrude ? 6f : 0f, shadow = 0f,
                shadowSoftness = 1f, highlight = 0.22f, face = ToRgba(face), ink = ToRgba(Ink),
            };
            if (extrude)
            {
                // Layered molded construction: lighter rim, darker coloured extrusion, dark depth band.
                spec.rim = 4f;
                spec.rimColor = ToRgba(Color.Lerp(face, Color.white, 0.55f));
                spec.extrusionColor = ToRgba(Color.Lerp(face, Ink, 0.45f));
                spec.depth = 3f;
            }
            var tex = ToTexture(ToySkinArt.Panel(spec), size, size, key);
            float bottom = extrude ? 35f : 26f;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f / WorldPerPx, 0,
                SpriteMeshType.FullRect, new Vector4(26f, bottom, 26f, 26f));
            sprites[key] = s;
            return s;
        }

        /// <summary>Narrow outlined strip (bin walls, lid, table lips). Use SpriteDrawMode.Sliced.</summary>
        public static Sprite Strip(Color face)
        {
            string key = "strip_" + ColorUtility.ToHtmlStringRGB(face);
            Sprite s;
            if (sprites.TryGetValue(key, out s)) return s;
            const int size = 24;
            var spec = new ToyPanelSpec
            {
                width = size, height = size, radius = 7f, outline = 4f, extrude = 0f, shadow = 0f, shadowSoftness = 1f,
                highlight = 0.15f, face = ToRgba(face), ink = ToRgba(Ink),
            };
            var tex = ToTexture(ToySkinArt.Panel(spec), size, size, key);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f / WorldPerPx, 0,
                SpriteMeshType.FullRect, new Vector4(11f, 11f, 11f, 11f));
            sprites[key] = s;
            return s;
        }

        /// <summary>
        /// Small layered game-piece badge (bin counters): saturated face, light rim, thick outline, coloured base.
        /// 9-sliced, minimum height ≈ 0.4 world units.
        /// </summary>
        public static Sprite Badge(Color face)
        {
            string key = "badge_" + ColorUtility.ToHtmlStringRGB(face);
            Sprite s;
            if (sprites.TryGetValue(key, out s)) return s;
            const int size = 48; // borders 25+18 must fit inside the texture height
            var spec = new ToyPanelSpec
            {
                width = size, height = size, radius = 13f, outline = 4.5f, rim = 3f, extrude = 5f, depth = 2f, shadow = 0f,
                shadowSoftness = 1f, highlight = 0.25f, face = ToRgba(face),
                rimColor = ToRgba(Color.Lerp(face, Color.white, 0.5f)), extrusionColor = ToRgba(Color.Lerp(face, Ink, 0.5f)),
                depthColor = ToRgba(Ink), ink = ToRgba(Ink),
            };
            var tex = ToTexture(ToySkinArt.Panel(spec), size, size, key);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f / WorldPerPx, 0,
                SpriteMeshType.FullRect, new Vector4(18f, 25f, 18f, 18f));
            sprites[key] = s;
            return s;
        }

        /// <summary>White eye with a thick ink ring.</summary>
        public static Sprite Eye()
        {
            Sprite s;
            if (sprites.TryGetValue("eye", out s)) return s;
            var p = new VectorPainter { OutlineWidth = 0.16f, Ink = ToRgba(Ink) };
            p.Circle(0f, 0f, 0.9f, new Rgba(1f, 1f, 1f)).Flat();
            const int res = 64;
            var tex = ToTexture(p.Rasterize(res), res, res, "eye");
            s = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res, 0, SpriteMeshType.FullRect);
            sprites["eye"] = s;
            return s;
        }

        public static Sprite SoftBlob()
        {
            Sprite s;
            if (sprites.TryGetValue("blob", out s)) return s;
            const int res = 64;
            var tex = ToTexture(ToySkinArt.SoftBlob(res), res, res, "blob");
            s = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res, 0, SpriteMeshType.FullRect);
            sprites["blob"] = s;
            return s;
        }

        // ---- world builders ---------------------------------------------------------------------

        // ---- environment tunables (visual only; adjust freely) -----------------------------------

        /// <summary>Master switch for the decorative layer (clouds, accents, vignette) and all ambient motion.</summary>
        public static bool Ambient = true;

        public const int AccentCount = 14;            // dots + rings + stars + sparkles
        public const int AccentSeed = 20261008;       // fixed seed: identical layout every run
        public const float AccentMinSpacing = 0.85f;  // world units between accents
        public const float BobAmplitudeMin = 0.03f;   // world units (≈ 4 px on a 1080-wide phone)
        public const float BobAmplitudeMax = 0.07f;
        public const float BobPeriodMin = 3f;         // seconds
        public const float BobPeriodMax = 6f;
        public const float CloudDrift = 0.12f;        // horizontal drift amplitude for clouds (world units)
        public static float TwinkleInterval = 2.6f;   // at most one sparkle twinkle per interval (seconds)
        public const float CloudLightAlpha = 0.34f;   // white over sky ≈ 10% lighter
        public const float CloudDarkAlpha = 0.45f;    // sky tone ≈ 12% darker
        public const float VignetteAlpha = 0.16f;
        public const float PlayZoneAlpha = 0.3f;      // light backplate behind the object area
        public const float ContactShadowAlpha = 0.32f;
        public const float DropShadowAlpha = 0.16f;
        public static readonly Color PlatformFace = Hex("F2A65A");   // toy-wood top face
        public static readonly Color PlatformRim = Hex("FFD9A0");    // light top edge
        public static readonly Color PlatformBase = Hex("B8642E");   // darker lower extrusion

        /// <summary>Table edges, mirroring RoundDirector.ComputeLayout (screen edge ± 16 dp).</summary>
        public static void TableExtents(out float left, out float right)
        {
            float halfW = Proto.Cam.orthographicSize * Proto.Cam.aspect;
            float margin = Units.DpToWorld(16f);
            left = -halfW + margin;
            right = halfW - margin;
        }

        static Sprite CachedSprite(string key, System.Func<Sprite> make)
        {
            Sprite s;
            if (!sprites.TryGetValue(key, out s)) { s = make(); sprites[key] = s; }
            return s;
        }

        static Sprite WhiteSprite(string key, byte[] rgba, int w, int h)
        {
            return CachedSprite(key, () =>
            {
                var tex = ToTexture(rgba, w, h, key);
                return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w, 0, SpriteMeshType.FullRect);
            });
        }

        static SpriteRenderer Deco(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 scale, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Builds the whole environment behind the gameplay, once per round (called from RoundDirector.BuildStatics,
        /// after the layout is computed): sky gradient, clouds, accents and vignette (decor, behind ToyStyle.Ambient),
        /// the play-zone backplate, and the toy platform slab drawn over the existing table strip.
        /// Positions are derived from the camera, the safe area and Director.TableTop / BinsTop.
        /// </summary>
        public static void BuildBackground(Transform parent, float left, float right, float bottom, float top)
        {
            var grad = CachedSprite("sky", () =>
            {
                var tex = ToTexture(ToySkinArt.Gradient(256, ToRgba(SkyBottom), ToRgba(SkyTop)), 1, 256, "sky");
                return Sprite.Create(tex, new Rect(0, 0, 1, 256), new Vector2(0.5f, 0.5f), 256f, 0, SpriteMeshType.FullRect);
            });
            var env = new GameObject("ToyEnvironment").transform;
            env.SetParent(parent, false);
            float W = right - left, H = top - bottom;
            Deco(env, "Sky", grad, new Vector2((left + right) / 2f, (top + bottom) / 2f), new Vector2((W + 2f) * 256f, H + 4f),
                Color.white, -100);

            // Layout anchors.
            float wpp = Units.WorldPerPx;
            Rect safe = Screen.safeArea;
            float safeTop = top - (Screen.height - safe.yMax) * wpp;
            float hudBottom = safeTop - Units.DpToWorld(64f);
            float tableTop = Proto.Director != null ? Proto.Director.TableTop : bottom + H * 0.35f;
            float binsTop = Proto.Director != null ? Proto.Director.BinsTop : bottom + H * 0.2f;
            float objMax = Units.DpToWorld(56f) * 1.3f;
            float pileTop = tableTop + objMax * 2f;
            float tableL, tableR;
            TableExtents(out tableL, out tableR);

            // Play zone: a faint rounded backplate behind the object area (always on).
            var zone = WhiteSprite("playzone", ToySkinArt.SoftPanel(128, 64, 22f, 12f), 128, 64);
            float zoneBottom = tableTop - 0.35f, zoneTop = pileTop + 0.6f;
            Deco(env, "PlayZone", zone, new Vector2((tableL + tableR) / 2f, (zoneBottom + zoneTop) / 2f),
                new Vector2((tableR - tableL) + 0.5f, (zoneTop - zoneBottom) * 2f), new Color(1f, 1f, 1f, PlayZoneAlpha), -60);

            BuildPlatform(env, tableL, tableR, tableTop);

            if (!Ambient) return;

            var ambient = env.gameObject.AddComponent<ToyAmbient>();
            ambient.Init(AccentCount + 3, AccentCount);

            // Clouds: two lighter, one darker, close in tone to the sky. Large and soft; no outlines.
            var cloud = WhiteSprite("cloud", ToySkinArt.Cloud(256, 128), 256, 128);
            float bandLow = pileTop + 0.4f, bandHigh = hudBottom;
            var c1 = Deco(env, "CloudA", cloud, new Vector2(left + W * 0.3f, Mathf.Lerp(bandLow, bandHigh, 0.78f)),
                new Vector2(W * 0.66f, W * 0.33f), new Color(1f, 1f, 1f, CloudLightAlpha), -99);
            var c2 = Deco(env, "CloudB", cloud, new Vector2(right - W * 0.22f, Mathf.Lerp(bandLow, bandHigh, 0.36f)),
                new Vector2(-W * 0.52f, W * 0.26f), new Color(1f, 1f, 1f, CloudLightAlpha * 0.8f), -99);
            Color dark = Color.Lerp(SkyTop, Ink, 0.12f);
            var c3 = Deco(env, "CloudC", cloud, new Vector2(right - W * 0.08f, Mathf.Lerp(bandLow, bandHigh, 0.98f)),
                new Vector2(W * 0.58f, W * 0.29f), new Color(dark.r, dark.g, dark.b, CloudDarkAlpha), -99);
            ambient.Add(c1.transform, 0.05f, 6f, 0.3f, CloudDrift);
            ambient.Add(c2.transform, 0.04f, 5.5f, 2.1f, CloudDrift);
            ambient.Add(c3.transform, 0.05f, 6f, 4.0f, CloudDrift * 0.8f);

            // Accents: seeded, unevenly spread (rejection sampling with a minimum spacing).
            var rng = new System.Random(AccentSeed);
            var dot = ProcSprites.Circle();
            var ring = WhiteSprite("ring", ToySkinArt.SoftRing(64, 0.16f), 64, 64);
            var star = WhiteSprite("sparkle", ToySkinArt.Sparkle(64), 64, 64);
            Color[] tints = { new Color(1f, 1f, 1f, 0.6f), Hex("FFF4C2"), Hex("E5DEFF"), Hex("DDEEFF") };
            var placed = new Vector2[AccentCount];
            int n = 0;
            for (int attempt = 0; attempt < 400 && n < AccentCount; attempt++)
            {
                Vector2 p;
                if (n < AccentCount - 3)
                    p = new Vector2(Lerp(rng, left + 0.3f, right - 0.3f), Lerp(rng, bandLow, bandHigh));
                else // a few near the screen edges between the platform and the bins, clear of the centre
                    p = new Vector2(rng.NextDouble() < 0.5 ? Lerp(rng, left + 0.25f, left + W * 0.2f) : Lerp(rng, right - W * 0.2f, right - 0.25f),
                        Lerp(rng, binsTop + 0.5f, tableTop - 0.9f));
                bool ok = true;
                for (int k = 0; k < n; k++) if ((placed[k] - p).sqrMagnitude < AccentMinSpacing * AccentMinSpacing) { ok = false; break; }
                if (!ok) continue;
                placed[n] = p;

                int kind = n % 7; // 0-1 dots, 2-3 stars, 4 ring, 5 sparkle, 6 dot
                Color tint = tints[rng.Next(tints.Length)];
                tint.a = Lerp(rng, 0.45f, 0.75f);
                SpriteRenderer sr;
                if (kind == 2 || kind == 3 || kind == 5)
                {
                    float size = kind == 5 ? Lerp(rng, 0.34f, 0.46f) : Lerp(rng, 0.22f, 0.34f);
                    sr = Deco(env, "Star", star, p, new Vector2(size, size), tint, -98);
                    ambient.AddTwinkler(sr);
                }
                else if (kind == 4)
                {
                    float size = Lerp(rng, 0.22f, 0.34f);
                    sr = Deco(env, "Ring", ring, p, new Vector2(size, size), tint, -98);
                }
                else
                {
                    float size = Lerp(rng, 0.08f, 0.16f);
                    sr = Deco(env, "Dot", dot, p, new Vector2(size, size), tint, -98);
                }
                ambient.Add(sr.transform, Lerp(rng, BobAmplitudeMin, BobAmplitudeMax), Lerp(rng, BobPeriodMin, BobPeriodMax),
                    Lerp(rng, 0f, Mathf.PI * 2f), 0f);
                n++;
            }

            // Vignette: soft darkening towards the screen edges, above the decor, below gameplay.
            var vig = WhiteSprite("vignette", ToySkinArt.Vignette(128), 128, 128);
            Deco(env, "Vignette", vig, new Vector2((left + right) / 2f, (top + bottom) / 2f), new Vector2(W + 0.1f, H + 0.1f),
                new Color(Ink.r, Ink.g, Ink.b, VignetteAlpha), -97);
        }

        static float Lerp(System.Random rng, float a, float b) { return a + (b - a) * (float)rng.NextDouble(); }

        /// <summary>
        /// Chunky molded-plastic platform slab drawn over the existing table strip: rounded ends, thick outline,
        /// light top edge, lighter top face, darker extrusion, soft shadow. Top face sits at the collider top (TableTop).
        /// </summary>
        static void BuildPlatform(Transform parent, float tableL, float tableR, float tableTop)
        {
            const int texW = 160, texH = 48;
            const float outline = 5f, rim = 3f, extrude = 10f, depth = 3f;
            var slab = CachedSprite("slab", () =>
            {
                var spec = new ToyPanelSpec
                {
                    width = texW, height = texH, radius = 20f, outline = outline, rim = rim, extrude = extrude, depth = depth,
                    shadow = 0f, shadowSoftness = 1f, highlight = 0.4f, face = ToRgba(PlatformFace), rimColor = ToRgba(PlatformRim),
                    extrusionColor = ToRgba(PlatformBase), depthColor = ToRgba(Ink), ink = ToRgba(Ink),
                };
                var tex = ToTexture(ToySkinArt.Panel(spec), texW, texH, "slab");
                // ppu so the texture is exactly the slab's height: only the rounded ends are 9-sliced horizontally.
                float slabH = Units.DpToWorld(30f);
                return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), texH / slabH, 0,
                    SpriteMeshType.FullRect, new Vector4(30f, 0f, 30f, 0f));
            });
            float h = Units.DpToWorld(30f);
            float topMargin = (outline * 0.5f + 1f) / texH * h; // the texture's face starts this far below its top
            var go = new GameObject("PlatformSlab");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3((tableL + tableR) / 2f, tableTop + topMargin - h / 2f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = slab;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2((tableR - tableL) + Units.DpToWorld(4f), h);
            sr.sortingOrder = -8;

            GroundShadow(parent, new Vector2((tableL + tableR) / 2f, tableTop - h - 0.02f), (tableR - tableL) * 1.02f, h * 0.9f, -9);
        }

        /// <summary>Soft contact shadow on the ground (under bins).</summary>
        public static void GroundShadow(Transform parent, Vector2 centre, float width, float height, int order)
        {
            var go = new GameObject("ToyGroundShadow");
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.transform.localScale = new Vector3(width, height, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SoftBlob();
            sr.color = new Color(Ink.r, Ink.g, Ink.b, 0.28f);
            sr.sortingOrder = order;
        }

        /// <summary>Stylised offset drop shadow that follows an object (visual only, no physics).</summary>
        public static void AddObjectShadow(SortObject o)
        {
            if (o == null || o.sr == null) return;
            var go = new GameObject("ToyShadow");
            go.transform.SetParent(o.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = o.sr.sprite;
            sr.color = new Color(0.16f, 0.1f, 0.3f, DropShadowAlpha);
            sr.sortingOrder = 6;

            // Soft contact shadow on the platform surface (sibling, so it doesn't inherit rotation/scale).
            var contact = new GameObject("ToyContactShadow");
            contact.transform.SetParent(o.transform.parent, false);
            var csr = contact.AddComponent<SpriteRenderer>();
            csr.sprite = SoftBlob();
            csr.sortingOrder = -7;
            csr.enabled = false;

            var shadow = go.AddComponent<ToyShadow>();
            shadow.Init(o, sr, csr);
        }
    }

    /// <summary>Keeps a drop shadow offset down-right of its object in world space and mirrors its squash.</summary>
    public class ToyShadow : MonoBehaviour
    {
        SortObject owner;
        SpriteRenderer sr, contact;
        float tableL, tableR;

        public void Init(SortObject o, SpriteRenderer renderer, SpriteRenderer contactShadow)
        {
            owner = o;
            sr = renderer;
            contact = contactShadow;
            ToyStyle.TableExtents(out tableL, out tableR);
            LateUpdate();
        }

        void LateUpdate()
        {
            if (owner == null || owner.sr == null) return;
            // Under the finger the existing landing shadow takes over.
            bool held = owner.state == ObjState.Held;
            sr.enabled = !held;
            Transform vis = owner.sr.transform;
            transform.position = vis.position + new Vector3(owner.size * 0.06f, -owner.size * 0.08f, 0f);
            transform.rotation = vis.rotation;
            transform.localScale = vis.localScale;

            // Contact shadow: only over the platform, fading as the object rises above it.
            if (contact == null || Proto.Director == null) return;
            Vector2 p = vis.position;
            float tableTop = Proto.Director.TableTop;
            float lift = (p.y - owner.HalfExtent * 0.8f) - tableTop;
            bool over = !held && owner.gameObject.activeInHierarchy && p.x > tableL && p.x < tableR && lift > -0.15f && lift < 1.6f;
            contact.enabled = over;
            if (!over) return;
            float k = Mathf.Clamp01(1f - lift / 1.6f);
            float w = owner.size * Mathf.Lerp(0.55f, 0.95f, k);
            contact.transform.position = new Vector3(p.x, tableTop + 0.02f, 0f);
            contact.transform.localScale = new Vector3(w, w * 0.22f, 1f);
            var ink = ToyStyle.Ink;
            contact.color = new Color(ink.r, ink.g, ink.b, ToyStyle.ContactShadowAlpha * k);
        }
    }
}
