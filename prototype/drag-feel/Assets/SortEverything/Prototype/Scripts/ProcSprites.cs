using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    public enum ShapeKind { Circle, Square, Triangle, Star, Capsule }

    /// <summary>
    /// Generates every sprite at runtime so the prototype has no art dependencies.
    /// Shapes are drawn white/grey and tinted by SpriteRenderer.color, so the outline becomes a darker
    /// shade of the object's colour ("sticker outline", GDD ch. 08) and patterns stay colour-blind safe.
    /// All sprites are 1 world unit wide; callers scale the transform.
    /// </summary>
    public static class ProcSprites
    {
        const int ShapeRes = 128;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static readonly Vector2[] TrianglePoly =
        {
            new Vector2(0f, 0.80f), new Vector2(-0.80f, -0.62f), new Vector2(0.80f, -0.62f),
        };

        static Vector2[] starPoly;
        static Vector2[] StarPoly
        {
            get
            {
                if (starPoly != null) return starPoly;
                starPoly = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float r = (i % 2 == 0) ? 0.92f : 0.42f;
                    float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                    starPoly[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.04f);
                }
                return starPoly;
            }
        }

        /// <summary>Collider outline in sprite-local units (sprite spans -0.5..0.5).</summary>
        public static Vector2[] ColliderPolygon(ShapeKind shape)
        {
            Vector2[] src = shape == ShapeKind.Star ? StarPoly : TrianglePoly;
            var pts = new Vector2[src.Length];
            // The drawn shape is the polygon rounded by ~0.08; grow the collider slightly to match it.
            for (int i = 0; i < src.Length; i++) pts[i] = src[i] * 0.5f * 1.07f;
            return pts;
        }

        public static Sprite Shape(ShapeKind shape, int pattern)
        {
            string key = "shape_" + shape + "_" + pattern;
            Sprite s;
            if (cache.TryGetValue(key, out s)) return s;

            var tex = NewTexture(ShapeRes, ShapeRes, key);
            var px = new Color32[ShapeRes * ShapeRes];
            float pixel = 2f / ShapeRes;
            const float outline = 0.13f; // visual style: heavier sticker outline

            for (int y = 0; y < ShapeRes; y++)
            {
                for (int x = 0; x < ShapeRes; x++)
                {
                    var p = new Vector2((x + 0.5f) / ShapeRes * 2f - 1f, (y + 0.5f) / ShapeRes * 2f - 1f);
                    float d = ShapeSdf(shape, p);
                    float coverage = Mathf.Clamp01(0.5f - d / pixel);
                    if (coverage <= 0f) { px[y * ShapeRes + x] = new Color32(255, 255, 255, 0); continue; }

                    // Fill: toy shading, lighter towards the top-left, a soft bevel so the shape looks
                    // puffed up like a vinyl toy, plus a specular spot.
                    float bevel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-d / 0.3f));
                    float shade = (0.8f + 0.18f * Mathf.Clamp01(0.5f + 0.5f * (-p.x * 0.4f + p.y * 0.7f))) * (0.8f + 0.2f * bevel);
                    float spec = Mathf.Clamp01(1f - (p - new Vector2(-0.32f, 0.38f)).magnitude / 0.2f);
                    float v = Mathf.Lerp(shade, 1f, spec * spec);
                    if (PatternMask(pattern, p)) v *= 0.74f;

                    // Outline band just inside the edge, anti-aliased.
                    float inOutline = Mathf.Clamp01((d + outline) / pixel + 0.5f);
                    v = Mathf.Lerp(v, 0.40f, inOutline);

                    byte b = (byte)(Mathf.Clamp01(v) * 255f);
                    px[y * ShapeRes + x] = new Color32(b, b, b, (byte)(coverage * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, ShapeRes, ShapeRes), new Vector2(0.5f, 0.5f), ShapeRes);
            s.name = key;
            cache[key] = s;
            return s;
        }

        /// <summary>Anti-aliased white disc, 1 unit wide. Used for eyes, particles and shadows.</summary>
        public static Sprite Circle()
        {
            Sprite s;
            if (cache.TryGetValue("circle", out s)) return s;
            const int res = 64;
            var tex = NewTexture(res, res, "circle");
            var px = new Color32[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = (x + 0.5f) / res * 2f - 1f, dy = (y + 0.5f) / res * 2f - 1f;
                    float a = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * res * 0.5f);
                    px[y * res + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
            cache["circle"] = s;
            return s;
        }

        /// <summary>Soft radial glow (alpha falls off to the edge).</summary>
        public static Sprite Glow()
        {
            Sprite s;
            if (cache.TryGetValue("glow", out s)) return s;
            const int res = 64;
            var tex = NewTexture(res, res, "glow");
            var px = new Color32[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = (x + 0.5f) / res * 2f - 1f, dy = (y + 0.5f) / res * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * res + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
            cache["glow"] = s;
            return s;
        }

        /// <summary>9-sliced rounded rectangle. Use with SpriteDrawMode.Sliced and set .size.</summary>
        public static Sprite RoundedRect()
        {
            Sprite s;
            if (cache.TryGetValue("rrect", out s)) return s;
            const int res = 64;
            const float radius = 18f;
            var tex = NewTexture(res, res, "rrect");
            var px = new Color32[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - res / 2f) - (res / 2f - radius);
                    float qy = Mathf.Abs(y + 0.5f - res / 2f) - (res / 2f - radius);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float d = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    px[y * res + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res, 0,
                SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
            cache["rrect"] = s;
            return s;
        }

        static Texture2D NewTexture(int w, int h, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        // Signed distance in normalised sprite space (-1..1); negative inside.
        static float ShapeSdf(ShapeKind shape, Vector2 p)
        {
            switch (shape)
            {
                case ShapeKind.Circle:
                    return p.magnitude - 0.86f;
                case ShapeKind.Square:
                {
                    const float r = 0.22f;
                    float qx = Mathf.Abs(p.x) - (0.84f - r), qy = Mathf.Abs(p.y) - (0.84f - r);
                    return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                           + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                }
                case ShapeKind.Capsule:
                {
                    float hx = Mathf.Clamp(p.x, -0.38f, 0.38f);
                    return new Vector2(p.x - hx, p.y).magnitude - 0.5f;
                }
                case ShapeKind.Triangle:
                    return SdPolygon(p, TrianglePoly) - 0.08f;
                case ShapeKind.Star:
                    return SdPolygon(p, StarPoly) - 0.06f;
            }
            return 1f;
        }

        // Exact signed distance to a polygon (Inigo Quilez).
        static float SdPolygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        // Colour-blind patterns (GDD ch. 08): each colour category always carries the same pattern.
        static bool PatternMask(int pattern, Vector2 p)
        {
            switch (pattern)
            {
                case 1: // dots
                {
                    float gx = Mathf.Repeat(p.x + 0.2f, 0.4f) - 0.2f, gy = Mathf.Repeat(p.y + 0.2f, 0.4f) - 0.2f;
                    return gx * gx + gy * gy < 0.11f * 0.11f;
                }
                case 2: // diagonal stripes
                    return Mathf.Repeat((p.x + p.y) * 2.2f, 1f) < 0.38f;
                case 3: // waves
                    return Mathf.Repeat(p.y * 2.4f + 0.12f * Mathf.Sin(p.x * 9f), 1f) < 0.35f;
                case 4: // checker
                    return ((Mathf.FloorToInt(p.x * 3f + 9f) + Mathf.FloorToInt(p.y * 3f + 9f)) & 1) == 0;
            }
            return false;
        }
    }
}
