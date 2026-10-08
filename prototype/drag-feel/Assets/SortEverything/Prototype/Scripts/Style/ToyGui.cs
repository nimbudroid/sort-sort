using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    public enum ToyTone { Primary, Secondary, Purple, Reward, Warning, Danger, Inactive, Paper }

    /// <summary>
    /// IMGUI toy kit for the visual style prototype: chunky outlined text, physical-feeling buttons that squash when
    /// pressed and bounce when released, rounded cards and a toy slider skin. Purely presentational: a ToyGui.Button
    /// returns true exactly when GUI.Button would (released inside the rect).
    /// </summary>
    public static class ToyGui
    {
        static float u = -1f;   // pixels per dp the textures were built for

        // Button geometry in dp (kept small enough that 34 dp tall panel buttons still 9-slice cleanly).
        const float BtnRadius = 10f, BtnOutline = 2.6f, BtnExtrude = 4.5f, BtnShadow = 2.5f, BtnPressed = 3.5f;

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

        public static Color ToneColor(ToyTone tone)
        {
            switch (tone)
            {
                case ToyTone.Primary: return ToyStyle.Primary;
                case ToyTone.Secondary: return ToyStyle.Secondary;
                case ToyTone.Purple: return ToyStyle.Purple;
                case ToyTone.Reward: return ToyStyle.Reward;
                case ToyTone.Warning: return ToyStyle.Warning;
                case ToyTone.Danger: return ToyStyle.Danger;
                case ToyTone.Inactive: return ToyStyle.Inactive;
                default: return ToyStyle.Paper;
            }
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

        static GUIStyle ButtonSkin(ToyTone tone, bool down)
        {
            string key = "btn_" + tone + (down ? "_down" : "");
            if (skins.ContainsKey(key)) return skins[key];
            int size = Mathf.CeilToInt(48f * u);
            var spec = new ToyPanelSpec
            {
                width = size, height = size, radius = BtnRadius * u, outline = BtnOutline * u, extrude = BtnExtrude * u,
                pressed = down ? BtnPressed * u : 0f, shadow = BtnShadow * u, shadowSoftness = 2.5f * u,
                highlight = 0.3f, face = R(ToneColor(tone)), ink = R(ToyStyle.Ink),
            };
            int side = Mathf.CeilToInt((BtnRadius + BtnOutline + 1f) * u);
            int bottom = Mathf.CeilToInt((BtnRadius + BtnOutline + BtnExtrude + BtnShadow + 1f) * u);
            return MakeSkin(key, spec, new RectOffset(side, side, side, bottom));
        }

        static GUIStyle CardSkin(Color face, string key)
        {
            if (skins.ContainsKey(key)) return skins[key];
            int size = Mathf.CeilToInt(72f * u);
            const float radius = 20f, outline = 3f, extrude = 5f, shadow = 5f;
            var spec = new ToyPanelSpec
            {
                width = size, height = size, radius = radius * u, outline = outline * u, extrude = extrude * u,
                shadow = shadow * u, shadowSoftness = 5f * u, highlight = 0.08f, face = R(face), ink = R(ToyStyle.Ink),
            };
            int side = Mathf.CeilToInt((radius + outline + 1f) * u);
            int bottom = Mathf.CeilToInt((radius + outline + extrude + shadow + 1f) * u);
            return MakeSkin(key, spec, new RectOffset(side, side, side, bottom));
        }

        /// <summary>Dark rounded card (the tuning panel).</summary>
        public static void Card(Rect r)
        {
            if (Event.current.type != EventType.Repaint) return;
            CardSkin(ToyStyle.Card, "card").Draw(r, false, false, false, false);
        }

        /// <summary>Rounded pill/panel in a tone, no interaction (HUD round label, toast).</summary>
        public static void Pill(Rect r, ToyTone tone)
        {
            if (Event.current.type != EventType.Repaint) return;
            ButtonSkin(tone, false).Draw(r, false, false, false, false);
        }

        /// <summary>Vertical pixels of a button's depth (extrusion + shadow); text sits above it.</summary>
        public static float ButtonDepth { get { return (BtnExtrude + BtnShadow) * u; } }

        /// <summary>
        /// Chunky cartoon text: dark outline ring, a dark extrusion below it, then the fill.
        /// </summary>
        public static void Text(Rect r, string text, GUIStyle style, Color fill, float outlineDp = 2.2f, float extrudeDp = 2f)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(text)) return;
            Color old = style.normal.textColor;
            Color ink = ToyStyle.Ink;
            ink.a = fill.a;
            float o = outlineDp * u, e = extrudeDp * u;
            style.normal.textColor = ink;
            const int ring = 12;
            if (e > 0f)
                for (int i = 0; i < ring; i++)
                {
                    float a = i * Mathf.PI * 2f / ring;
                    style.Draw(new Rect(r.x + Mathf.Cos(a) * o, r.y + Mathf.Sin(a) * o + e, r.width, r.height), text, false, false, false, false);
                }
            if (o > 0f)
                for (int i = 0; i < ring; i++)
                {
                    float a = i * Mathf.PI * 2f / ring;
                    style.Draw(new Rect(r.x + Mathf.Cos(a) * o, r.y + Mathf.Sin(a) * o, r.width, r.height), text, false, false, false, false);
                }
            style.normal.textColor = fill;
            style.Draw(r, text, false, false, false, false);
            style.normal.textColor = old;
        }

        /// <summary>
        /// Physical toy button. Squashes while held, bounces on release; returns true on release inside (like GUI.Button).
        /// </summary>
        public static bool Button(Rect r, string text, ToyTone tone, GUIStyle textStyle, Color? textColor = null)
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
                    float sy = p.scaleY;
                    float sx = 1f + (1f - sy) * 0.6f;
                    Matrix4x4 m = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(sx, sy), new Vector2(r.center.x, r.yMax));
                    ButtonSkin(tone, p.down).Draw(r, false, false, false, false);
                    float push = p.down ? BtnPressed * u : 0f;
                    var textRect = new Rect(r.x, r.y - ButtonDepth * 0.5f + push, r.width, r.height);
                    Text(textRect, text, textStyle, textColor ?? Color.white, 1.8f, 1.2f);
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

        /// <summary>Toy slider: chunky inset track and a round white knob that presses in.</summary>
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
                    var tex = ToyStyle.ToTexture(ToySkinArt.Knob(k, 2.6f * u, down ? 1.2f * u : 3.5f * u,
                        R(down ? ToyStyle.Reward : ToyStyle.Paper), R(ToyStyle.Ink)), k, k, key);
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
