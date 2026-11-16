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
    /// The mess on the counter: crumbs, stains and dust that disappear as items get sorted, plus a dusty
    /// haze over the room that lifts with progress. Purely presentation; derived from the sorted fraction.
    /// </summary>
    public sealed class MessLayer : MonoBehaviour
    {
        sealed class Decal { public Image Img; public float Threshold; public float BaseAlpha; public bool Gone; public Vector2 Pos; }
        readonly List<Decal> _decals = new List<Decal>();
        Image _haze;
        float _hazeAlpha;
        MonoBehaviour _host;
        RectTransform _fxLayer;

        public static MessLayer Create(RectTransform parent, RectTransform hazeParent, MonoBehaviour host, RectTransform fxLayer,
            string seedKey, Rect area, int count)
        {
            var rt = UIKit.Rect(parent, "Mess").Stretch();
            var m = rt.gameObject.AddComponent<MessLayer>();
            m._host = host; m._fxLayer = fxLayer;
            var rng = new System.Random(seedKey.GetHashCode() & 0x7fffffff);
            var stain = SpriteFactory.Hex("#6E4224");
            var dust = SpriteFactory.Hex("#8C8177");
            var spill = SpriteFactory.Hex("#E39A3B");
            for (int i = 0; i < count; i++)
            {
                int kind = i % 4;
                var pos = new Vector2(area.x + (float)rng.NextDouble() * area.width, area.y + (float)rng.NextDouble() * area.height);
                float size; Sprite sprite; Color color;
                switch (kind)
                {
                    case 0: sprite = Fx.Crumbs(rng.Next(6)); size = 34 + (float)rng.NextDouble() * 14; color = SpriteFactory.Hex("#8E5E30"); color.a = 0.95f; break;
                    case 1: sprite = Fx.Blob(rng.Next(8)); size = 40 + (float)rng.NextDouble() * 28; color = stain; color.a = 0.4f; break;
                    case 2: sprite = Fx.Blob(10 + rng.Next(8)); size = 34 + (float)rng.NextDouble() * 22; color = dust; color.a = 0.42f; break;
                    default: sprite = Fx.Blob(20 + rng.Next(8)); size = 32 + (float)rng.NextDouble() * 24; color = spill; color.a = 0.38f; break;
                }
                var img = UIKit.Icon(rt, "Decal" + i, sprite, size, color);
                img.preserveAspect = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0, 1);
                img.rectTransform.anchoredPosition = new Vector2(pos.x, -pos.y);
                img.rectTransform.sizeDelta = new Vector2(size, size * (0.65f + (float)rng.NextDouble() * 0.35f));
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, (float)rng.NextDouble() * 360f);
                // Spread thresholds so something clears on most placements; the last bits go with the final item.
                float threshold = (i + 1f) / (count + 1f) + ((float)rng.NextDouble() - 0.5f) * 0.08f;
                m._decals.Add(new Decal { Img = img, Threshold = Mathf.Clamp(threshold, 0.05f, 1f), BaseAlpha = color.a, Pos = pos });
            }
            if (hazeParent != null)
            {
                m._haze = UIKit.Panel(hazeParent, "DustHaze", new Color(0.55f, 0.47f, 0.4f, 0f), 0);
                m._haze.rectTransform.Stretch(-400, -200, -400, -200);
            }
            return m;
        }

        /// <summary>Applies progress (0..1). With animate, cleared decals fade out with a little sparkle.</summary>
        public void SetProgress(float fraction, bool animate, bool reduced)
        {
            foreach (var d in _decals)
            {
                bool gone = fraction >= d.Threshold - 0.0001f;
                if (gone == d.Gone) continue;
                d.Gone = gone;
                if (!animate || reduced || !isActiveAndEnabled)
                {
                    d.Img.color = WithAlpha(d.Img.color, gone ? 0 : d.BaseAlpha);
                    continue;
                }
                if (gone)
                {
                    var img = d.Img; float a0 = img.color.a;
                    StartCoroutine(UIKit.Tween(0.4f, t => { if (img != null) img.color = WithAlpha(img.color, a0 * (1 - t)); }));
                    Fx.Burst(_host, _fxLayer, d.Pos, new Color(1f, 0.96f, 0.8f), 4, 0.55f);
                }
                else d.Img.color = WithAlpha(d.Img.color, d.BaseAlpha);
            }
            if (_haze != null)
            {
                float target = 0.16f * (1 - fraction);
                if (!animate || reduced || !isActiveAndEnabled) _haze.color = WithAlpha(_haze.color, target);
                else
                {
                    float a0 = _haze.color.a;
                    StartCoroutine(UIKit.Tween(0.5f, t => { if (_haze != null) _haze.color = WithAlpha(_haze.color, Mathf.Lerp(a0, target, t)); }));
                }
            }
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
