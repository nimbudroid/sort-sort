using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    public enum ToyTone { Primary, Secondary, Purple, Reward, Warning, Danger, Inactive, Paper }

    /// <summary>Button depth/size presets. Each fits its existing rect height (Small 34 dp, Medium 36–44 dp, Large 70 dp).</summary>
    public enum ToySize { Small, Medium, Large }

    /// <summary>
    /// IMGUI toy kit for the visual style prototype. Everything is built as layered molded plastic:
    /// bright face → contrasting rim → dark outline → coloured extrusion → dark depth band → soft shadow.
    /// Text uses the same idea: light face → thick dark outline → warm depth → dark shadow.
    /// Purely presentational: ToyGui.Button returns true exactly when GUI.Button would (released inside the rect).
    /// </summary>
    public static class ToyGui
    {
        static float u = -1f; // pixels per dp the textures were built for

        struct Geo
        {
            public float radius, outline, rim, extrude, depth, shadow, pressed, textOutline, textDepth;
        }

        static Geo GeoFor(ToySize size)
        {
            switch (size)
            {
                case ToySize.Large:
                    return new Geo { radius = 14f, outline = 4.2f, rim = 4.6f, extrude = 9.5f, depth = 4f, shadow = 4.5f, pressed = 8f, textOutline = 4.4f, textDepth = 4.6f };
                case ToySize.Medium:
                    return new Geo { radius = 11f, outline = 3.4f, rim = 3.2f, extrude = 4.4f, depth = 2f, shadow = 2.4f, pressed = 3.6f, textOutline = 3f, textDepth = 2.8f };
                default:
                    return new Geo { radius = 8.5f, outline = 2.9f, rim = 2.4f, extrude = 3.4f, depth = 1.6f, shadow = 2f, pressed = 2.8f, textOutline = 2.4f, textDepth = 2.1f };
            }
        }

        struct ToneColors
        {
            public Color face, rim, extrusion;
        }

        /// <summary>Face / contrasting rim / coloured extrusion for each tone (e.g. green face, yellow rim, blue base).</summary>
        static ToneColors Tone(ToyTone tone)
        {
            switch (tone)
            {
                case ToyTone.Primary: return T("3FD436", "FFC414", "2654E8");
                case ToyTone.Secondary: return T("1E96FF", "9FF0FF", "1C3FC4");
                case ToyTone.Purple: return T("9447FF", "FF9CF0", "4A1FB8");
                case ToyTone.Reward: return T("FFC800", "FFF09A", "EB6A00");
                case ToyTone.Warning: return T("FF8A00", "FFE07A", "C23A08");
                case ToyTone.Danger: return T("FF3D3D", "FFC0A8", "A01434");
                case ToyTone.Inactive: return T("ABA2C8", "E8E3F4", "625A82");
                default: return T("FFFFFF", "FFD84A", "8447FF");
            }
        }

        static ToneColors T(string face, string rim, string ext)
        {
            return new ToneColors { face = ToyStyle.Hex(face), rim = ToyStyle.Hex(rim), extrusion = ToyStyle.Hex(ext) };
        }

        /// <summary>Warm depth colour for headline text (orange/gold extrusion).</summary>
        public static readonly Color TextDepthWarm = ToyStyle.Hex("FF8A00");
        /// <summary>Cream face for headline text.</summary>
        public static readonly Color TextCream = ToyStyle.Hex("FFF9E8");

        public static Color ToneColor(ToyTone tone) { return Tone(tone).face; }

        static readonly Dictionary<string, GUIStyle> skins = new Dictionary<string, GUIStyle>();
        static readonly List<Texture2D> textures = new List<Texture2D>();

        class Press
        {
            public float scaleY = 1f, velocity;
            public bool down;
            public int lastFrame = -1;
        }

        static readonly Dictionary<int, Press> presses = new Dictionary<int, Press>();

        /// <summary>Call at the start of OnGUI with the current pixels-per-dp.</summary>
        public static void Begin(float pxPerDp)
        {
            if (Mathf.Approximately(pxPerDp, u)) return;
            u = pxPerDp;
            skins.Clear();
            foreach (var t in textures) if (t != null) Object.Destroy(t);
            textures.Clear();
        }

        static Rgba R(Color c) { return new Rgba(c.r, c.g, c.b, c.a); }

        static GUIStyle MakeSkin(string key, ToyPanelSpec spec, RectOffset border)
        {
            GUIStyle s;
            if (skins.TryGetValue(key, out s)) return s;
            var tex = ToyStyle.ToTexture(ToySkinArt.Panel(spec), spec.width, spec.height, key);
            textures.Add(tex);
            s = new GUIStyle { border = border };
            s.normal.background = tex;
            skins[key] = s;
            return s;
        }

        static GUIStyle LayeredSkin(string key, ToneColors c, Geo g, bool down)
        {
            if (skins.ContainsKey(key)) return skins[key];
            int top = Mathf.CeilToInt((g.radius + g.outline + 1f) * u);
            int bottom = Mathf.CeilToInt((g.radius + g.outline + g.extrude + g.depth + g.shadow + 1f) * u);
            int size = top + bottom + Mathf.CeilToInt(10f * u);
            var spec = new ToyPanelSpec
            {
                width = size, height = size, radius = g.radius * u, outline = g.outline * u, rim = g.rim * u,
                extrude = g.extrude * u, depth = g.depth * u, shadow = g.shadow * u, shadowSoftness = g.shadow * u,
                pressed = down ? g.pressed * u : 0f, highlight = 0.32f, face = R(c.face), rimColor = R(c.rim),
                extrusionColor = R(c.extrusion), depthColor = R(ToyStyle.Ink), ink = R(ToyStyle.Ink),
                shadowColor = new Rgba(0.1f, 0.06f, 0.22f, 0.32f),
            };
            return MakeSkin(key, spec, new RectOffset(top, top, top, bottom));
        }

        static GUIStyle ButtonSkin(ToyTone tone, ToySize size, bool down)
        {
            return LayeredSkin("btn_" + tone + "_" + size + (down ? "_down" : ""), Tone(tone), GeoFor(size), down);
        }

        /// <summary>Vertical pixels of a size's depth (extrusion + depth + shadow); text sits above it.</summary>
        public static float Depth(ToySize size)
        {
            var g = GeoFor(size);
            return (g.extrude + g.depth + g.shadow) * u;
        }

        static Texture2D burst;

        /// <summary>Full-screen translucent dim (completion state backdrop).</summary>
        public static void Dim(float alpha)
        {
            if (Event.current.type != EventType.Repaint || alpha <= 0f) return;
            var old = GUI.color;
            GUI.color = new Color(ToyStyle.Ink.r, ToyStyle.Ink.g, ToyStyle.Ink.b, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>Soft rotating light burst behind a headline (decoration only).</summary>
        public static void Burst(Vector2 centre, float radius, float angle, Color tint)
        {
            if (Event.current.type != EventType.Repaint || radius <= 0f) return;
            if (burst == null)
            {
                const int res = 256;
                burst = ToyStyle.ToTexture(ToySkinArt.Sunburst(res, 14), res, res, "burst");
            }
            var old = GUI.color;
            Matrix4x4 m = GUI.matrix;
            GUI.color = tint;
            GUIUtility.RotateAroundPivot(angle, centre);
            GUI.DrawTexture(new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f), burst);
            GUI.matrix = m;
            GUI.color = old;
        }

        /// <summary>Kept for existing call sites: depth of a Small element.</summary>
        public static float ButtonDepth { get { return Depth(ToySize.Small); } }

        /// <summary>Big layered card (the tuning panel): purple face, lighter rim, dark extrusion.</summary>
        public static void Card(Rect r)
        {
            if (Event.current.type != EventType.Repaint) return;
            var g = new Geo { radius = 22f, outline = 4.2f, rim = 4f, extrude = 6f, depth = 3f, shadow = 6f };
            var c = new ToneColors { face = ToyStyle.Card, rim = ToyStyle.Hex("7B5CC9"), extrusion = ToyStyle.Hex("2B1F54") };
            LayeredSkin("card", c, g, false).Draw(r, false, false, false, false);
        }

        /// <summary>Layered pill/panel in a tone, no interaction (HUD round label, toast).</summary>
        public static void Pill(Rect r, ToyTone tone, ToySize size = ToySize.Small)
        {
            if (Event.current.type != EventType.Repaint) return;
            ButtonSkin(tone, size, false).Draw(r, false, false, false, false);
        }

        /// <summary>
        /// Chunky cartoon text: dark outline ring, a dark extrusion below it, then the fill (single-depth variant).
        /// </summary>
        public static void Text(Rect r, string text, GUIStyle style, Color fill, float outlineDp = 2.2f, float extrudeDp = 2f)
        {
            Logo(r, text, style, fill, ToyStyle.Ink, outlineDp, extrudeDp, false);
        }

        /// <summary>
        /// Headline "sticker" text: light face → thick dark outline → coloured depth → dark soft shadow.
        /// </summary>
        public static void Logo(Rect r, string text, GUIStyle style, Color face, Color depth, float outlineDp, float depthDp,
            bool shadow = true)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(text)) return;
            Color old = style.normal.textColor;
            float alpha = face.a;
            float o = outlineDp * u, e = depthDp * u;
            Color ink = ToyStyle.Ink;
            ink.a = alpha;

            if (shadow)
            {
                style.normal.textColor = new Color(ink.r, ink.g, ink.b, 0.35f * alpha);
                DrawRing(style, r, text, o, o * 0.35f, e + o * 0.9f);
            }

            // Dark silhouette of the whole extruded block.
            style.normal.textColor = ink;
            float step = Mathf.Max(1f, 1.6f * u);
            for (float k = e; k > 0f; k -= step) DrawRing(style, r, text, o, 0f, k);
            DrawRing(style, r, text, o, 0f, 0f);

            // Coloured depth inside the silhouette.
            if (e > 0f)
            {
                style.normal.textColor = new Color(depth.r, depth.g, depth.b, alpha);
                for (float k = e; k > 0.5f * step; k -= step)
                    style.Draw(new Rect(r.x, r.y + k, r.width, r.height), text, false, false, false, false);
                // Thin dark line separating the face from its depth.
                style.normal.textColor = ink;
                DrawRing(style, r, text, Mathf.Max(1f, o * 0.4f), 0f, 0f, 8);
            }

            style.normal.textColor = face;
            style.Draw(r, text, false, false, false, false);
            style.normal.textColor = old;
        }

        static void DrawRing(GUIStyle style, Rect r, string text, float radius, float dx, float dy, int ring = 16)
        {
            for (int i = 0; i < ring; i++)
            {
                float a = i * Mathf.PI * 2f / ring;
                style.Draw(new Rect(r.x + Mathf.Cos(a) * radius + dx, r.y + Mathf.Sin(a) * radius + dy, r.width, r.height),
                    text, false, false, false, false);
            }
        }

        /// <summary>
        /// Molded plastic toy button. Squashes while held, bounces on release; returns true on release inside (like GUI.Button).
        /// </summary>
        public static bool Button(Rect r, string text, ToyTone tone, GUIStyle textStyle, Color? textColor = null,
            ToySize size = ToySize.Small)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive, r);
            Press p;
            if (!presses.TryGetValue(id, out p)) { p = new Press(); presses[id] = p; }
            Event e = Event.current;
            bool clicked = false;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (r.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        p.down = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) e.Use();
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        p.down = false;
                        p.velocity += 7f; // release bounce
                        clicked = r.Contains(e.mousePosition);
                        e.Use();
                    }
                    break;
                case EventType.Repaint:
                    StepSpring(p);
                    var g = GeoFor(size);
                    float sy = p.scaleY;
                    float sx = 1f + (1f - sy) * 0.6f;
                    Matrix4x4 m = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(sx, sy), new Vector2(r.center.x, r.yMax));
                    ButtonSkin(tone, size, p.down).Draw(r, false, false, false, false);
                    float push = p.down ? g.pressed * u : 0f;
                    var textRect = new Rect(r.x, r.y - Depth(size) * 0.5f + push, r.width, r.height);
                    Logo(textRect, text, textStyle, textColor ?? TextCream, Tone(tone).extrusion, g.textOutline, g.textDepth, false);
                    GUI.matrix = m;
                    break;
            }
            return clicked;
        }

        static void StepSpring(Press p)
        {
            if (p.lastFrame == Time.frameCount) return;
            p.lastFrame = Time.frameCount;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float target = p.down ? 0.9f : 1f;
            const int steps = 4;
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float force = (target - p.scaleY) * 700f - p.velocity * 18f;
                p.velocity += force * h;
                p.scaleY += p.velocity * h;
            }
        }

        /// <summary>Toy slider: chunky inset track and a layered knob (white face, gold rim, purple base) that presses in.</summary>
        public static void SkinSliders(GUISkin skin)
        {
            const string trackKey = "slider_track";
            if (!skins.ContainsKey(trackKey))
            {
                int w = Mathf.CeilToInt(32f * u), h = Mathf.CeilToInt(16f * u);
                var spec = new ToyPanelSpec
                {
                    width = w, height = h, radius = 7f * u, outline = 2.4f * u, extrude = 0f, shadow = 0f,
                    shadowSoftness = 1f, highlight = 0f, face = R(ToyStyle.Ink).Light(0.15f), ink = R(ToyStyle.Ink),
                };
                int b = Mathf.CeilToInt(8f * u);
                MakeSkin(trackKey, spec, new RectOffset(b, b, Mathf.Max(1, h / 2 - 1), Mathf.Max(1, h / 2 - 1)));

                int k = Mathf.CeilToInt(28f * u);
                foreach (bool down in new[] { false, true })
                {
                    string key = down ? "slider_knob_down" : "slider_knob";
                    var knob = new ToyPanelSpec
                    {
                        width = k, height = k, outline = 3f * u, rim = 2.8f * u, extrude = (down ? 1.2f : 3.2f) * u,
                        depth = 1.4f * u, shadow = 0f, shadowSoftness = 1f, highlight = 0.3f,
                        face = R(Color.white), rimColor = R(ToyStyle.Reward), extrusionColor = R(ToyStyle.Purple),
                        depthColor = R(ToyStyle.Ink), ink = R(ToyStyle.Ink),
                    };
                    knob.radius = (k - (3f * u + 2f) - knob.extrude - knob.depth) * 0.5f;
                    var tex = ToyStyle.ToTexture(ToySkinArt.Panel(knob), k, k, key);
                    textures.Add(tex);
                    var st = new GUIStyle();
                    st.normal.background = tex;
                    skins[key] = st;
                }
            }

            var track = skins[trackKey];
            skin.horizontalSlider.normal.background = track.normal.background;
            skin.horizontalSlider.border = track.border;
            skin.horizontalSlider.fixedHeight = 16f * u;
            skin.horizontalSlider.padding = new RectOffset(0, 0, 0, 0);
            skin.horizontalSlider.margin = new RectOffset(0, 0, 0, 0);
            skin.horizontalSlider.overflow = new RectOffset(0, 0, 0, 0);

            var thumb = skin.horizontalSliderThumb;
            thumb.normal.background = skins["slider_knob"].normal.background;
            thumb.hover.background = skins["slider_knob"].normal.background;
            thumb.active.background = skins["slider_knob_down"].normal.background;
            thumb.focused.background = skins["slider_knob"].normal.background;
            thumb.border = new RectOffset(0, 0, 0, 0);
            thumb.fixedWidth = 28f * u;
            thumb.fixedHeight = 28f * u;
            int ov = Mathf.RoundToInt(6f * u);
            thumb.overflow = new RectOffset(0, 0, ov, ov);
        }
    }
}
