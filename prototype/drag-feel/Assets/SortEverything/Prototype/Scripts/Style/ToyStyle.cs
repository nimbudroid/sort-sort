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
        public static readonly Color Ink = Hex("2E2433");
        public static readonly Color Primary = Hex("58CC6A");    // friendly green: primary actions
        public static readonly Color Secondary = Hex("4C8DF6");  // blue: secondary actions
        public static readonly Color Purple = Hex("8E63F0");     // purple: secondary actions
        public static readonly Color Reward = Hex("FFCD3C");     // yellow/gold: rewards
        public static readonly Color Warning = Hex("FF9A2E");    // orange: warnings
        public static readonly Color Danger = Hex("F2564A");     // red: failure/danger
        public static readonly Color Inactive = Hex("A79CC0");   // muted grey-purple: inactive
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
                width = size, height = size, radius = 14f, outline = 4f, extrude = extrude ? 6f : 0f, shadow = 0f,
                shadowSoftness = 1f, highlight = 0.22f, face = ToRgba(face), ink = ToRgba(Ink),
            };
            var tex = ToTexture(ToySkinArt.Panel(spec), size, size, key);
            float bottom = extrude ? 26f : 20f;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f / WorldPerPx, 0,
                SpriteMeshType.FullRect, new Vector4(20f, bottom, 20f, 20f));
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
                width = size, height = size, radius = 7f, outline = 3f, extrude = 0f, shadow = 0f, shadowSoftness = 1f,
                highlight = 0.15f, face = ToRgba(face), ink = ToRgba(Ink),
            };
            var tex = ToTexture(ToySkinArt.Panel(spec), size, size, key);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f / WorldPerPx, 0,
                SpriteMeshType.FullRect, new Vector4(9f, 9f, 9f, 9f));
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

        /// <summary>Soft sky gradient with a few very faint bokeh blobs, behind everything.</summary>
        public static void BuildBackground(Transform parent, float left, float right, float bottom, float top)
        {
            Sprite grad;
            if (!sprites.TryGetValue("sky", out grad))
            {
                var tex = ToTexture(ToySkinArt.Gradient(256, ToRgba(SkyBottom), ToRgba(SkyTop)), 1, 256, "sky");
                grad = Sprite.Create(tex, new Rect(0, 0, 1, 256), new Vector2(0.5f, 0.5f), 256f, 0, SpriteMeshType.FullRect);
                sprites["sky"] = grad;
            }
            var go = new GameObject("ToyBackground");
            go.transform.SetParent(parent, false);
            float w = (right - left) + 2f, h = (top - bottom) + 4f;
            go.transform.position = new Vector3((left + right) / 2f, (top + bottom) / 2f, 0f);
            go.transform.localScale = new Vector3(w * 256f, h, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = grad;
            sr.sortingOrder = -100;

            float[] blobs = { 0.2f, 0.82f, 0.9f, 0.85f, 0.62f, 0.6f, 0.15f, 0.5f, 0.7f };
            for (int i = 0; i < blobs.Length; i += 3)
            {
                var b = new GameObject("Bokeh");
                b.transform.SetParent(parent, false);
                b.transform.position = new Vector3(Mathf.Lerp(left, right, blobs[i]), Mathf.Lerp(bottom, top, blobs[i + 1]), 0f);
                b.transform.localScale = Vector3.one * (right - left) * blobs[i + 2];
                var bs = b.AddComponent<SpriteRenderer>();
                bs.sprite = SoftBlob();
                bs.color = new Color(1f, 1f, 1f, 0.35f);
                bs.sortingOrder = -99;
            }
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
            sr.color = new Color(0.16f, 0.1f, 0.3f, 0.22f);
            sr.sortingOrder = 6;
            var shadow = go.AddComponent<ToyShadow>();
            shadow.Init(o, sr);
        }
    }

    /// <summary>Keeps a drop shadow offset down-right of its object in world space and mirrors its squash.</summary>
    public class ToyShadow : MonoBehaviour
    {
        SortObject owner;
        SpriteRenderer sr;

        public void Init(SortObject o, SpriteRenderer renderer)
        {
            owner = o;
            sr = renderer;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (owner == null || owner.sr == null) return;
            // Under the finger the existing landing shadow takes over.
            sr.enabled = owner.state != ObjState.Held;
            Transform vis = owner.sr.transform;
            transform.position = vis.position + new Vector3(owner.size * 0.06f, -owner.size * 0.08f, 0f);
            transform.rotation = vis.rotation;
            transform.localScale = vis.localScale;
        }
    }
}
