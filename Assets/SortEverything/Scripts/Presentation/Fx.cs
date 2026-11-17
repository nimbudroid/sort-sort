using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Lightweight UI particle effects (sparkle bursts, rings, confetti, floating words) and the procedural
    /// mess decals (crumbs, stains, dust) that clear as a board gets sorted. All decorative: no raycasts,
    /// unscaled time, and every effect is skipped when Reduced Motion is on.
    /// Layers use the board convention: anchored at top-left, y downward in board units.
    /// </summary>
    public static class Fx
    {
        // ---------- Sprites ----------

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => Cache.Clear(); // Enter Play Mode without domain reload keeps statics.

        static Sprite Get(string key, System.Func<Texture2D> make)
        {
            if (Cache.TryGetValue(key, out var s) && s != null && s.texture != null) return s;
            var tex = make();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            s.name = "fx_" + key;
            Cache[key] = s;
            return s;
        }

        static Texture2D Paint(int n, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2 - 1, v = (y + 0.5f) / n * 2 - 1;
                    byte a = (byte)(Mathf.Clamp01(alpha(u, v)) * 255);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Four-point twinkle star.</summary>
        public static Sprite Sparkle() => Get("sparkle", () => Paint(64, (u, v) =>
        {
            float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
            float star = 1f - Mathf.Pow(Mathf.Pow(ax, 0.5f) + Mathf.Pow(ay, 0.5f), 2f);
            float core = 1f - Mathf.Sqrt(u * u + v * v) * 2.2f;
            return Mathf.Max(star * 1.6f, core);
        }));

        /// <summary>Soft round glow.</summary>
        public static Sprite Glow() => Get("glow", () => Paint(64, (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            return Mathf.Pow(Mathf.Clamp01(1 - d), 2f);
        }));

        /// <summary>Small rounded confetti chip.</summary>
        public static Sprite Chip() => Get("chip", () => Paint(32, (u, v) =>
        {
            float dx = Mathf.Max(Mathf.Abs(u) - 0.55f, 0), dy = Mathf.Max(Mathf.Abs(v) - 0.25f, 0);
            return (0.4f - Mathf.Sqrt(dx * dx + dy * dy)) * 12f;
        }));

        /// <summary>Irregular soft blob (stains, spills, dust), seeded so each one looks different.</summary>
        public static Sprite Blob(int seed) => Get("blob" + seed, () =>
        {
            var rng = new System.Random(seed * 7919 + 3);
            float[] k = new float[6];
            for (int i = 0; i < k.Length; i++) k[i] = (float)rng.NextDouble();
            return Paint(64, (u, v) =>
            {
                float a = Mathf.Atan2(v, u);
                float r = 0.62f + 0.12f * Mathf.Sin(a * 2 + k[0] * 6) + 0.08f * Mathf.Sin(a * 3 + k[1] * 6) + 0.05f * Mathf.Sin(a * 5 + k[2] * 6);
                float d = Mathf.Sqrt(u * u + v * v);
                float edge = Mathf.Clamp01((r - d) * 9f);
                float ring = 1f - 0.35f * Mathf.Clamp01(1 - Mathf.Abs(d - r * 0.85f) * 10f); // drier rim
                return edge * (0.55f + 0.45f * (1 - d / r)) * ring;
            });
        });

        /// <summary>Scattered crumbs: a cluster of small dots.</summary>
        public static Sprite Crumbs(int seed) => Get("crumbs" + seed, () =>
        {
            var rng = new System.Random(seed * 104729 + 11);
            var dots = new List<Vector3>();
            int count = 6 + rng.Next(5);
            for (int i = 0; i < count; i++)
                dots.Add(new Vector3((float)rng.NextDouble() * 1.4f - 0.7f, (float)rng.NextDouble() * 1.4f - 0.7f, 0.06f + (float)rng.NextDouble() * 0.09f));
            return Paint(64, (u, v) =>
            {
                float best = 0;
                foreach (var d in dots)
                {
                    float dist = Mathf.Sqrt((u - d.x) * (u - d.x) + (v - d.y) * (v - d.y));
                    best = Mathf.Max(best, Mathf.Clamp01((d.z - dist) * 30f));
                }
                return best;
            });
        });

        /// <summary>Crisp toy-style mess decals with the shared warm outline: crumbs, a drip, a coffee ring.</summary>
        public static Sprite Decal(string kind, int variant) => Get("decal_" + kind + variant, () =>
        {
            var r = new SpriteFactory.Raster(96) { Outline = 1.6f, Gloss = 0.35f, Bevel = 6f };
            var rng = new System.Random(variant * 7919 + kind.Length);
            switch (kind)
            {
                case "drip":
                {
                    // A small glossy juice puddle with a couple of droplets.
                    var c = new Color(0.93f, 0.62f, 0.22f, 0.9f);
                    float a0 = variant * 0.7f;
                    r.Custom((x, y) =>
                    {
                        float dx = x - 46, dy = y - 50;
                        float ang = Mathf.Atan2(dy, dx);
                        float rr = 22f + 4f * Mathf.Sin(ang * 3 + a0) + 2.5f * Mathf.Sin(ang * 5 + a0 * 2);
                        return Mathf.Sqrt(dx * dx + dy * dy) - rr;
                    }, c, true, 16, 20, 76, 80);
                    r.Ellipse(38, 42, 7, 4, -30, new Color(1, 1, 1, 0.6f), outline: false);
                    r.Circle(76, 74, 5.5f, c);
                    r.Circle(20 + variant * 4, 80, 3.5f, c);
                    break;
                }
                case "ring":
                {
                    r.Shade = false;
                    var c = new Color(0.42f, 0.24f, 0.12f, 0.55f); // coffee ring
                    r.Ring(48, 48, 34, 3.2f, c, outline: false);
                    r.Ring(48, 48, 32, 1.4f, new Color(0.35f, 0.2f, 0.1f, 0.45f), outline: false);
                    r.Circle(48 + 30, 48 + 24, 4, c, outline: false);
                    break;
                }
                default: // crumbs: chunky rounded bread/biscuit crumbs, toy style
                {
                    var c = new Color(0.97f, 0.82f, 0.55f, 1f);
                    var dark = new Color(0.86f, 0.62f, 0.33f, 1f);
                    for (int i = 0; i < 6; i++)
                    {
                        float x = 22 + (float)rng.NextDouble() * 52, y = 22 + (float)rng.NextDouble() * 52;
                        float s = i == 0 ? 8.5f : 3.5f + (float)rng.NextDouble() * 4f;
                        r.RoundRect(x, y, s, s * (0.7f + (float)rng.NextDouble() * 0.3f), s * 0.55f, (float)rng.NextDouble() * 90f, i % 3 == 0 ? dark : c);
                    }
                    break;
                }
            }
            return r.ToSprite("decal_" + kind + variant).texture;
        });

        /// <summary>A soft vertical band of light (for the clean-shine sweep).</summary>
        public static Sprite ShineBand() => Get("shine", () => Paint(64, (u, v) =>
        {
            float across = 1f - Mathf.Abs(u);
            return Mathf.Pow(Mathf.Clamp01(across), 1.6f) * (1f - Mathf.Pow(Mathf.Abs(v), 6f));
        }));

        // ---------- Effects ----------

        static readonly Color[] Palette =
        {
            SpriteFactory.Hex("#F6C453"), SpriteFactory.Hex("#F28B6B"), SpriteFactory.Hex("#7CC6A4"),
            SpriteFactory.Hex("#6FA8DC"), SpriteFactory.Hex("#F4A6C6"), SpriteFactory.Hex("#FFF4D6"),
        };

        static Image Spawn(RectTransform layer, string name, Sprite sprite, Vector2 boardPos, float size, Color color)
        {
            var img = UIKit.Icon(layer, name, sprite, size, color);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(boardPos.x, -boardPos.y);
            return img;
        }

        /// <summary>Sparkles and chips bursting outward from a point, with a little gravity.</summary>
        public static void Burst(MonoBehaviour host, RectTransform layer, Vector2 boardPos, Color tint, int count = 10, float power = 1f)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * Mathf.PI * 2 + Random.Range(-0.3f, 0.3f);
                float speed = Random.Range(90f, 170f) * power;
                bool sparkle = i % 2 == 0;
                var color = sparkle ? Color.Lerp(tint, Color.white, 0.55f) : (Random.value < 0.5f ? tint : Palette[Random.Range(0, Palette.Length)]);
                var img = Spawn(layer, "Particle", sparkle ? Sparkle() : Chip(), boardPos, sparkle ? Random.Range(12f, 20f) * power : Random.Range(7f, 10f), color);
                host.StartCoroutine(Fly(img, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed, Random.Range(0.45f, 0.7f), Random.Range(-360f, 360f), 260f));
            }
        }

        static IEnumerator Fly(Image img, Vector2 velocity, float life, float spin, float gravity)
        {
            var rt = img.rectTransform;
            var c = img.color;
            float t = 0, rot = Random.Range(0f, 360f);
            Vector3 baseScale = rt.localScale;
            while (t < life && img != null)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                velocity.y -= gravity * dt; // anchoredPosition is y-up
                velocity *= 1f - 1.8f * dt;
                rt.anchoredPosition += velocity * dt;
                rot += spin * dt;
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                float k = t / life;
                rt.localScale = baseScale * (k < 0.15f ? Mathf.Lerp(0.3f, 1f, k / 0.15f) : 1f - 0.4f * k);
                img.color = new Color(c.r, c.g, c.b, c.a * (1 - k * k));
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }

        /// <summary>An expanding, fading ring.</summary>
        public static void Ring(MonoBehaviour host, RectTransform layer, Vector2 boardPos, Color color, float from = 30f, float to = 120f, float life = 0.45f)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            var img = Spawn(layer, "Ring", SpriteFactory.Ring(), boardPos, from, color);
            host.StartCoroutine(UIKit.Tween(life, t =>
            {
                if (img == null) return;
                float e = UIKit.EaseOut(t);
                img.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(from, to, e);
                img.color = new Color(color.r, color.g, color.b, color.a * (1 - t));
            }, () => { if (img != null) Object.Destroy(img.gameObject); }));
        }

        /// <summary>A soft glow flash behind a point.</summary>
        public static void Flash(MonoBehaviour host, RectTransform layer, Vector2 boardPos, Color color, float size = 90f, float life = 0.35f)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            var img = Spawn(layer, "Flash", Glow(), boardPos, size, color);
            host.StartCoroutine(UIKit.Tween(life, t =>
            {
                if (img == null) return;
                img.rectTransform.sizeDelta = Vector2.one * size * (0.6f + 0.6f * t);
                img.color = new Color(color.r, color.g, color.b, color.a * Mathf.Sin(Mathf.PI * Mathf.Min(1, t * 1.4f)));
            }, () => { if (img != null) Object.Destroy(img.gameObject); }));
        }

        /// <summary>A short word that pops, rises and fades ("Nice!", "Fruit done!").</summary>
        public static void FloatText(MonoBehaviour host, RectTransform layer, Vector2 boardPos, string text, Color color, int size = 18, float rise = 34f)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            var label = UIKit.Label(layer, "FloatText", text, size, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.15f, 0.08f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(220, 30);
            var start = new Vector2(boardPos.x, -boardPos.y);
            host.StartCoroutine(UIKit.Tween(0.9f, t =>
            {
                if (label == null) return;
                float pop = t < 0.18f ? UIKit.EaseOutBack(t / 0.18f) : 1f;
                rt.localScale = Vector3.one * Mathf.Max(0.01f, pop);
                rt.anchoredPosition = start + new Vector2(0, rise * UIKit.EaseOut(t));
                float a = t < 0.65f ? 1 : 1 - (t - 0.65f) / 0.35f;
                label.color = new Color(color.r, color.g, color.b, a);
                outline.effectColor = new Color(0.25f, 0.15f, 0.08f, 0.85f * a);
            }, () => { if (label != null) Object.Destroy(label.gameObject); }));
        }

        /// <summary>Confetti falling across an area (board units), for scene completion.</summary>
        public static void Confetti(MonoBehaviour host, RectTransform layer, float width, float top, int count = 70)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(Random.Range(0f, width), top - Random.Range(0f, 80f));
                var img = Spawn(layer, "Confetti", i % 4 == 0 ? Sparkle() : Chip(), pos, i % 4 == 0 ? 16f : Random.Range(8f, 12f), Palette[i % Palette.Length]);
                var vel = new Vector2(Random.Range(-60f, 60f), Random.Range(-40f, 120f));
                host.StartCoroutine(Fall(img, vel, Random.Range(1.4f, 2.2f), Random.Range(0f, 0.35f)));
            }
        }

        static IEnumerator Fall(Image img, Vector2 velocity, float life, float delay)
        {
            var rt = img.rectTransform;
            var c = img.color;
            img.color = new Color(c.r, c.g, c.b, 0);
            float wait = 0;
            while (wait < delay) { wait += Time.unscaledDeltaTime; yield return null; }
            float t = 0, phase = Random.Range(0f, 6f), spin = Random.Range(-540f, 540f), rot = 0;
            while (t < life && img != null)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                velocity.y -= 320f * dt;
                velocity.y = Mathf.Max(velocity.y, -260f);
                rt.anchoredPosition += new Vector2(velocity.x + Mathf.Sin(t * 6 + phase) * 40f, velocity.y) * dt;
                rot += spin * dt;
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                rt.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * 8 + phase)) * 0.8f + 0.2f, 1, 1); // flutter
                float k = t / life;
                img.color = new Color(c.r, c.g, c.b, k > 0.75f ? (1 - k) / 0.25f : 1f);
                yield return null;
            }
            if (img != null) Object.Destroy(img.gameObject);
        }
    }

    /// <summary>
    /// The mess on the counter: crisp toy-style crumbs, drips and coffee rings. Each decal belongs to one item and
    /// sits next to it; when that item is sorted its mess fades out with a small sparkle. Density and decal set
    /// come from area/level data. Purely presentation.
    /// </summary>
    public sealed class MessLayer : MonoBehaviour
    {
        sealed class Decal { public Image Img; public string ObjectId; public float BaseAlpha; public bool Gone; public Vector2 Pos; }
        readonly List<Decal> _decals = new List<Decal>();
        MonoBehaviour _host;
        RectTransform _fxLayer;

        public static MessLayer Create(RectTransform parent, MonoBehaviour host, RectTransform fxLayer, SortEverything.Domain.BoardDef def, BoardLayout layout, SortEverything.Domain.MessData mess)
        {
            var rt = UIKit.Rect(parent, "Mess").Stretch();
            var m = rt.gameObject.AddComponent<MessLayer>();
            m._host = host; m._fxLayer = fxLayer;
            if (mess == null || !mess.enabled || mess.density <= 0 || mess.decals == null || mess.decals.Length == 0) return m;
            uint seed = 2166136261u;
            foreach (char c in def.BoardId + def.Objects.Count) { seed ^= c; seed *= 16777619u; }
            var rng = new System.Random((int)(seed & 0x7fffffff));
            var area = layout.PlayRect;
            int k = 0;
            foreach (var o in def.Objects)
            {
                var p = layout.Pile[o.InstanceId];
                // density 1.0 = one decal per item; fractions are rounded by a seeded coin flip.
                int count = (int)mess.density + (rng.NextDouble() < mess.density - (int)mess.density ? 1 : 0);
                for (int j = 0; j < count; j++, k++)
                {
                    string kind = mess.decals[(k + rng.Next(mess.decals.Length)) % mess.decals.Length];
                    float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float dist = p.Visible * (0.42f + (float)rng.NextDouble() * 0.25f);
                    var pos = p.Center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                    float size = kind == "ring" ? p.Visible * 0.62f : kind == "drip" ? p.Visible * 0.42f : p.Visible * 0.5f;
                    pos.x = Mathf.Clamp(pos.x, area.xMin + size * 0.4f, area.xMax - size * 0.4f);
                    pos.y = Mathf.Clamp(pos.y, area.yMin + size * 0.4f, area.yMax - size * 0.4f);
                    var img = UIKit.Icon(rt, "Decal_" + o.InstanceId + "_" + j, Fx.Decal(kind, rng.Next(4)), size, Color.white);
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0, 1);
                    img.rectTransform.anchoredPosition = new Vector2(pos.x, -pos.y);
                    img.rectTransform.localRotation = Quaternion.Euler(0, 0, (float)rng.NextDouble() * 360f);
                    m._decals.Add(new Decal { Img = img, ObjectId = o.InstanceId, BaseAlpha = 1f, Pos = pos });
                }
            }
            return m;
        }

        /// <summary>A sorted item's mess is gone for good this visit (moving it back does not re-dirty the counter).</summary>
        public void Refresh(SortEverything.Domain.BoardState s, bool animate)
        {
            foreach (var d in _decals)
            {
                if (d.Gone || !s.IsAssigned(d.ObjectId)) continue;
                d.Gone = true;
                if (!animate || !isActiveAndEnabled) { d.Img.color = new Color(1, 1, 1, 0); continue; }
                var img = d.Img;
                StartCoroutine(UIKit.Tween(0.4f, t => { if (img != null) { img.color = new Color(1, 1, 1, 1 - t); img.rectTransform.localScale = Vector3.one * (1f - 0.25f * t); } }));
                Fx.Burst(_host, _fxLayer, d.Pos, new Color(1f, 0.96f, 0.8f), 5, 0.5f);
            }
        }

        public int Count => _decals.Count;
    }
}
