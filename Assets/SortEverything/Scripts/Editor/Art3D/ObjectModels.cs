using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SortEverything.Editor.Art3D.MeshKit;

namespace SortEverything.Editor.Art3D
{
    /// <summary>
    /// The 18 kitchen objects as modeled 3D props (Premium Cozy Stylized 3D). Object IDs and categories are the
    /// GDD's; only the art changes. Units: roughly one unit tall; the baker frames each object automatically.
    /// Each model returns its parts and a three-quarter pose for the shared camera.
    /// </summary>
    public static class ObjectModels
    {
        public sealed class Model { public List<Part> Parts = new List<Part>(); public Vector3 Pose; }

        static Color H(string hex) => Mat.H(hex);

        // ---------- Material families (shared so the whole library reads as one set) ----------

        static Mat Produce(string baseHex, string shadeHex, float spec = 0.18f, float gloss = 0.35f) => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = spec, Gloss = gloss, Rim = 0.16f, Wrap = 0.45f, Noise = 0.045f, NoiseScale = 7f };
        static Mat Leafy(string baseHex = "#5FA444", string shadeHex = "#2C5A22") => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = 0.12f, Gloss = 0.3f, Rim = 0.18f, Wrap = 0.5f, Noise = 0.05f, NoiseScale = 12f };
        static Mat Stem(string hex = "#6E4A2C") => new Mat { Base = H(hex), Shade = H("#3A2416"), Spec = 0.08f, Gloss = 0.2f, Wrap = 0.3f, Noise = 0.06f, NoiseScale = 20f };
        static Mat Ceramic(string baseHex, string shadeHex) => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = 0.55f, Gloss = 0.82f, Rim = 0.2f, Wrap = 0.25f, Noise = 0.012f };
        static Mat Paper(string baseHex, string shadeHex) => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = 0.06f, Gloss = 0.15f, Rim = 0.1f, Wrap = 0.3f, Noise = 0.025f, NoiseScale = 30f };
        static Mat Plastic(string baseHex, string shadeHex, float spec = 0.32f) => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = spec, Gloss = 0.55f, Rim = 0.15f, Wrap = 0.3f, Noise = 0.015f };
        static Mat Metal(string baseHex = "#C7CDD4", float metal = 0.85f) => new Mat
        { Base = H(baseHex), Shade = H("#5E6066"), Spec = 0.8f, Gloss = 0.88f, Rim = 0.12f, Wrap = 0.15f, Metal = metal, Noise = 0.01f };
        static Mat PaintedMetal(string baseHex, string shadeHex) => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = 0.6f, Gloss = 0.78f, Rim = 0.18f, Wrap = 0.2f, Metal = 0.18f, Noise = 0.01f };
        static Mat Wood(string baseHex = "#B9824E", string shadeHex = "#6B4122") => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = 0.14f, Gloss = 0.3f, Rim = 0.1f, Wrap = 0.25f, Noise = 0.07f, NoiseScale = 16f };

        static Mat Fabric(string baseHex, string shadeHex) => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = 0.05f, Gloss = 0.12f, Rim = 0.28f, Wrap = 0.6f, Noise = 0.07f, NoiseScale = 46f };
        static Mat Wax(string baseHex, string shadeHex) => new Mat
        { Base = H(baseHex), Shade = H(shadeHex), Spec = 0.2f, Gloss = 0.35f, Rim = 0.3f, Wrap = 0.75f, Noise = 0.02f };
        static Mat Glow(string hex) => new Mat { Base = H(hex), Shade = H(hex), Spec = 0f, Gloss = 0f, Rim = 0.2f, Wrap = 1f, Noise = 0f };

        static Mat Grad(Mat m, string tintHex, float strength, Vector3 axis, float from, float to)
        {
            m.Tint2 = H(tintHex); m.Tint2.a = strength; m.GradY = new Vector2(from, to); m.GradAxis = axis;
            return m;
        }

        static Mesh Warp(Mesh m, Func<Vector3, Vector3> f)
        {
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = f(v[i]);
            m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static float SStep(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }

        public static readonly string[] Ids =
        {
            "apple", "banana", "orange", "strawberry", "carrot", "pepper", "broccoli", "corn",
            "water_bottle", "milk_carton", "juice_carton", "soda_can", "spoon", "fork", "plate", "mug", "spatula", "pan",
            // Living Room
            "book", "magazine", "newspaper", "cushion", "folded_blanket", "slipper",
            "remote", "game_controller", "headphones", "candle", "photo_frame", "succulent",
        };

        public static Model Build(string id)
        {
            var m = new Model();
            void P(string name, Mesh mesh, Mat mat) => m.Parts.Add(new Part(name, mesh, mat));
            switch (id)
            {
                // ===================== FRUIT =====================
                case "apple":
                {
                    var body = Sculpt(d =>
                    {
                        float th = Mathf.Acos(Mathf.Clamp(d.y, -1, 1));
                        float ph = Mathf.Atan2(d.z, d.x);
                        float r = 0.5f;
                        r *= 1f + 0.07f * d.y;                                   // broader shoulders
                        r *= 1f + 0.035f * Mathf.Cos(3 * ph + 0.6f) * (1 - Mathf.Abs(d.y)); // soft natural lobes
                        r -= 0.17f * Mathf.Exp(-th * th / 0.055f);               // stem well
                        r -= 0.07f * Mathf.Exp(-(Mathf.PI - th) * (Mathf.PI - th) / 0.07f); // blossom end
                        return r;
                    }, 64, 96);
                    Warp(body, v => new Vector3(v.x, v.y * 0.9f, v.z));
                    P("body", body, Grad(Produce("#D9352F", "#7A1420", 0.38f, 0.66f), "#F0A13A", 0.35f, Vector3.up, -0.5f, -0.05f));
                    P("stem", Tube(Bezier(new Vector3(0, 0.25f, 0), new Vector3(0.01f, 0.36f, 0), new Vector3(0.03f, 0.47f, 0.01f), new Vector3(0.07f, 0.55f, 0.02f), 12),
                        t => Mathf.Lerp(0.036f, 0.026f, t), 14), Stem());
                    P("leaf", Transform(Leaf(0.36f, 0.17f, 0.22f, 0.015f), new Vector3(0.06f, 0.5f, 0.0f), new Vector3(-15f, -30f, 28f)), Leafy());
                    m.Pose = new Vector3(0, -20, 0);
                    break;
                }
                case "banana":
                {
                    var path = Bezier(new Vector3(-0.95f, 0.18f, 0), new Vector3(-0.45f, -0.36f, 0), new Vector3(0.45f, -0.36f, 0), new Vector3(0.95f, 0.2f, 0), 48);
                    var body = Tube(path, t => 0.2f * Mathf.Pow(Mathf.Sin(Mathf.Lerp(0.05f, 0.95f, t) * Mathf.PI), 0.55f) + 0.012f, 40,
                        (a, t) => 1f + 0.075f * Mathf.Cos(5 * a * Mathf.PI * 2f), 0.92f, Vector3.forward);
                    P("body", body, Grad(Produce("#F6CD3C", "#B0741A", 0.22f, 0.45f), "#B8C24A", 0.75f, Vector3.right, -0.85f, -0.45f));
                    P("stem", Tube(Bezier(new Vector3(-0.92f, 0.16f, 0), new Vector3(-1.0f, 0.22f, 0), new Vector3(-1.07f, 0.28f, 0), new Vector3(-1.12f, 0.29f, 0), 10),
                        t => Mathf.Lerp(0.055f, 0.045f, t), 16, (a, t) => 1f + 0.08f * Mathf.Cos(5 * a * Mathf.PI * 2f)), Grad(Stem("#8C7A3A"), "#5C4A22", 0.8f, Vector3.right, -1.12f, -1.0f));
                    P("tip", Transform(Ellipsoid(new Vector3(0.05f, 0.045f, 0.045f)), new Vector3(0.95f, 0.2f, 0), Vector3.zero), Stem("#4A3020"));
                    m.Pose = new Vector3(8, 0, 6);
                    break;
                }
                case "orange":
                {
                    var body = Sculpt(d =>
                    {
                        float th = Mathf.Acos(Mathf.Clamp(d.y, -1, 1));
                        float r = 0.5f;
                        r -= 0.004f * (Noise3(d * 34f) - 0.5f) * 2f + 0.004f * (Noise3(d * 61f + Vector3.one * 7f) - 0.5f) * 2f; // dimpled peel
                        r -= 0.025f * Mathf.Exp(-th * th / 0.012f);           // stem end
                        r -= 0.02f * Mathf.Exp(-(Mathf.PI - th) * (Mathf.PI - th) / 0.01f);
                        return r;
                    }, 72, 112);
                    Warp(body, v => new Vector3(v.x, v.y * 0.94f, v.z));
                    P("body", body, Grad(Produce("#F38B1F", "#A8410C", 0.24f, 0.4f), "#FFB347", 0.3f, Vector3.up, -0.1f, 0.5f));
                    P("nub", Transform(Ellipsoid(new Vector3(0.05f, 0.03f, 0.05f)), new Vector3(0, 0.455f, 0), Vector3.zero), Leafy("#7DA046", "#3F5A22"));
                    P("leaf", Transform(Leaf(0.34f, 0.16f, 0.2f, 0.015f), new Vector3(0.02f, 0.47f, 0.0f), new Vector3(-20f, 25f, 22f)), Leafy());
                    m.Pose = new Vector3(0, 30, 0);
                    break;
                }
                case "strawberry":
                {
                    Vector3 Berry(Vector3 p)
                    {
                        float y = p.y;
                        float w = Mathf.Lerp(0.34f, 1f, SStep(-1f, 0.4f, y)) * (1f - 0.14f * SStep(0.6f, 1f, y));
                        float ph = Mathf.Atan2(p.z, p.x);
                        w *= 1f + 0.04f * Mathf.Cos(3 * ph);
                        float yy = y * 0.56f - 0.03f * Mathf.Max(0, -y) * Mathf.Max(0, -y);
                        if (y > 0.8f) yy -= (y - 0.8f) * 0.25f;
                        return new Vector3(p.x * 0.5f * w, yy, p.z * 0.5f * w);
                    }
                    P("body", Warp(Sphere(1f, 64, 96), Berry), Grad(Produce("#E2303E", "#7C1121", 0.45f, 0.72f), "#F5714F", 0.25f, Vector3.up, -0.5f, 0.2f));
                    var seeds = new List<Mesh>();
                    int count = 70;
                    for (int i = 0; i < count; i++)
                    {
                        float y = 1 - 2 * (i + 0.5f) / count;
                        if (y > 0.82f) continue;
                        float rad = Mathf.Sqrt(1 - y * y), ph = i * 2.39996f;
                        var d = new Vector3(Mathf.Cos(ph) * rad, y, Mathf.Sin(ph) * rad);
                        var pos = Berry(d) * 0.972f;
                        var outward = new Vector3(pos.x, pos.y * 0.3f, pos.z).normalized;
                        seeds.Add(Transform(Ellipsoid(new Vector3(0.013f, 0.02f, 0.012f), 8, 10), pos, Quaternion.LookRotation(outward) * Quaternion.Euler(90, 0, 0), Vector3.one));
                    }
                    P("seeds", Combine(seeds), new Mat { Base = H("#F5D56A"), Shade = H("#A7781C"), Spec = 0.3f, Gloss = 0.6f, Wrap = 0.3f });
                    var calyx = new List<Mesh>();
                    for (int i = 0; i < 6; i++)
                        calyx.Add(Transform(Leaf(0.24f, 0.1f, 0.12f, 0.012f), new Vector3(0, 0.56f, 0), new Vector3(0, i * 60f + 10f, -18f)));
                    P("calyx", Combine(calyx), Leafy("#4E9C3A", "#25551C"));
                    P("stem", Tube(Bezier(new Vector3(0, 0.55f, 0), new Vector3(0, 0.62f, 0), new Vector3(0.02f, 0.68f, 0), new Vector3(0.05f, 0.72f, 0), 8), t => 0.024f, 12), Leafy("#5E9A3E", "#2E5A1E"));
                    m.Pose = new Vector3(-12, 20, -6);
                    break;
                }

                // ===================== VEGETABLES =====================
                case "carrot":
                {
                    var path = Bezier(new Vector3(0, 0.45f, 0), new Vector3(0.01f, 0.05f, 0), new Vector3(0.05f, -0.4f, 0), new Vector3(0.12f, -0.82f, 0), 40);
                    var body = Tube(path, t =>
                    {
                        float r = 0.2f * Mathf.Pow(1 - t, 0.8f) + 0.012f;
                        r *= t < 0.06f ? Mathf.Sqrt(t / 0.06f) * 0.35f + 0.65f : 1f;          // rounded shoulder
                        r *= 1f - 0.05f * Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 30f)), 8f);     // growth rings
                        return r;
                    }, 36, (a, t) => 1f + 0.03f * Mathf.Sin(3 * a * Mathf.PI * 2f + t * 5f));
                    P("body", body, Grad(Produce("#F27A22", "#9E3E0E", 0.14f, 0.3f), "#F9A04C", 0.3f, Vector3.up, -0.6f, 0.4f));
                    var fronds = new List<Mesh>();
                    fronds.Add(Transform(Leaf(0.42f, 0.13f, 0.18f), new Vector3(0, 0.44f, 0), new Vector3(0, 0, 80f)));
                    fronds.Add(Transform(Leaf(0.38f, 0.12f, 0.18f), new Vector3(0, 0.44f, 0), new Vector3(0, 0, 105f)));
                    fronds.Add(Transform(Leaf(0.36f, 0.12f, 0.18f), new Vector3(0, 0.44f, 0), new Vector3(0, 70, 92f)));
                    fronds.Add(Transform(Leaf(0.34f, 0.11f, 0.18f), new Vector3(0, 0.44f, 0), new Vector3(0, -80, 95f)));
                    P("fronds", Combine(fronds), Leafy("#5BA53F", "#2A5A1E"));
                    m.Pose = new Vector3(0, 10, -28);
                    break;
                }
                case "pepper":
                {
                    // Bell pepper: taller than wide, four lobes with real creases, flat shoulders, bumpy blossom end.
                    var body = Sculpt(d =>
                    {
                        float th = Mathf.Acos(Mathf.Clamp(d.y, -1, 1));
                        float ph = Mathf.Atan2(d.z, d.x) + 0.35f;
                        float lobe = Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * ph)), 0.55f);      // 1 on the lobes, 0 in the creases
                        float r = 0.5f * (0.86f + 0.16f * lobe);
                        r *= 1f + 0.08f * SStep(-0.2f, 0.7f, d.y);                         // broad shoulders
                        r -= 0.2f * Mathf.Exp(-th * th / 0.07f);                            // deep well around the stem
                        float bottom = Mathf.Exp(-(Mathf.PI - th) * (Mathf.PI - th) / 0.3f);
                        r -= 0.12f * bottom * (1f - lobe);                                  // four feet at the blossom end
                        return r;
                    }, 80, 128);
                    Warp(body, v => new Vector3(v.x, v.y * 1.08f, v.z));
                    P("body", body, Grad(Produce("#DD2E21", "#6E0E07", 0.62f, 0.86f), "#A9140B", 0.35f, Vector3.up, -0.5f, 0.15f));
                    var calyx = Sculpt(d => 0.15f * (1f + 0.3f * Mathf.Cos(5 * Mathf.Atan2(d.z, d.x))), 16, 40);
                    P("calyx", Transform(Warp(calyx, v => new Vector3(v.x, v.y * 0.32f, v.z)), new Vector3(0, 0.37f, 0), Vector3.zero), Leafy("#5E9A3A", "#2E521C"));
                    P("stem", Tube(Bezier(new Vector3(0, 0.36f, 0), new Vector3(0, 0.52f, 0), new Vector3(0.05f, 0.62f, 0), new Vector3(0.16f, 0.66f, 0.02f), 14),
                        t => Mathf.Lerp(0.07f, 0.05f, t) * (t > 0.92f ? 0.85f : 1f), 18, (a, t) => 1f + 0.07f * Mathf.Cos(6 * a * Mathf.PI * 2f)), Leafy("#6EA243", "#33561F"));
                    m.Pose = new Vector3(-6, 25, 0);
                    break;
                }
                case "broccoli":
                {
                    var florets = new List<Mesh>();
                    var centers = new List<(Vector3 c, float r)>
                    {
                        (new Vector3(0, 0.3f, 0), 0.22f), (new Vector3(-0.25f, 0.2f, 0.05f), 0.19f), (new Vector3(0.25f, 0.21f, 0.03f), 0.19f),
                        (new Vector3(-0.12f, 0.24f, -0.18f), 0.18f), (new Vector3(0.14f, 0.25f, -0.17f), 0.18f), (new Vector3(-0.1f, 0.22f, 0.2f), 0.17f),
                        (new Vector3(0.13f, 0.22f, 0.19f), 0.17f), (new Vector3(-0.36f, 0.07f, -0.06f), 0.13f), (new Vector3(0.37f, 0.08f, -0.05f), 0.13f),
                        (new Vector3(0.0f, 0.1f, -0.3f), 0.14f),
                    };
                    int k = 0;
                    foreach (var (c, r) in centers)
                    {
                        int seed = k++;
                        var f = Sculpt(d => r * (1f + 0.12f * (Noise3(d * 5f + Vector3.one * seed * 3.1f) - 0.5f) + 0.05f * Mathf.Abs(Mathf.Sin(d.x * 9f) * Mathf.Sin(d.z * 9f + seed))), 28, 40);
                        florets.Add(Transform(f, c, Vector3.zero));
                    }
                    P("florets", Combine(florets), Grad(Produce("#4F903B", "#1F4A1A", 0.1f, 0.25f), "#2E6A2A", 0.5f, Vector3.up, -0.05f, 0.45f));
                    var stalk = new List<Mesh>
                    {
                        Tube(Line(new Vector3(0.02f, -0.62f, 0), new Vector3(0, 0.05f, 0), 16), t => Mathf.Lerp(0.12f, 0.16f, t), 28),
                        Tube(Bezier(new Vector3(0, -0.05f, 0), new Vector3(-0.08f, 0.02f, 0), new Vector3(-0.2f, 0.08f, 0.03f), new Vector3(-0.26f, 0.14f, 0.05f), 10), t => Mathf.Lerp(0.07f, 0.05f, t), 16),
                        Tube(Bezier(new Vector3(0, -0.05f, 0), new Vector3(0.08f, 0.02f, 0), new Vector3(0.2f, 0.08f, 0.02f), new Vector3(0.26f, 0.14f, 0.03f), 10), t => Mathf.Lerp(0.07f, 0.05f, t), 16),
                    };
                    P("stalk", Combine(stalk), Grad(Produce("#A9CB7C", "#5E7E3A", 0.15f, 0.35f), "#D3E3A8", 0.4f, Vector3.up, -0.6f, -0.2f));
                    m.Pose = new Vector3(0, 15, 0);
                    break;
                }
                case "corn":
                {
                    Vector3 Cob(Vector3 d) => new Vector3(d.x * 0.25f, d.y * 0.62f, d.z * 0.25f) * (1f - 0.12f * Mathf.Max(0, d.y) * Mathf.Max(0, d.y));
                    P("cob", Warp(Sphere(1f, 32, 48), Cob), new Mat { Base = H("#E8B83A"), Shade = H("#93621A") });
                    var kernels = new List<Mesh>();
                    for (int row = 0; row < 20; row++)
                    {
                        float y = Mathf.Lerp(-0.92f, 0.95f, row / 19f);
                        float rad = Mathf.Sqrt(Mathf.Max(0.02f, 1 - y * y));
                        for (int c = 0; c < 14; c++)
                        {
                            float ph = (c + (row % 2) * 0.5f) / 14f * Mathf.PI * 2f;
                            var d = new Vector3(Mathf.Cos(ph) * rad, y, Mathf.Sin(ph) * rad);
                            var pos = Cob(d);
                            var outward = new Vector3(pos.x, 0.15f * pos.y, pos.z).normalized;
                            float s = Mathf.Lerp(1f, 0.6f, Mathf.Abs(y) * Mathf.Abs(y));
                            kernels.Add(Transform(RoundedBox(new Vector3(0.085f, 0.07f, 0.06f) * s, 0.026f * s, 3), pos, Quaternion.LookRotation(outward, Vector3.up), Vector3.one));
                        }
                    }
                    P("kernels", Combine(kernels), new Mat { Base = H("#F6D04A"), Shade = H("#B07D1A"), Spec = 0.4f, Gloss = 0.66f, Rim = 0.15f, Wrap = 0.35f, Noise = 0.05f, NoiseScale = 14f });
                    var husk = new List<Mesh>();
                    husk.Add(Transform(Leaf(0.95f, 0.42f, 0.55f, 0.02f), new Vector3(0, -0.66f, 0), new Vector3(0, -95f, 70f)));
                    husk.Add(Transform(Leaf(0.85f, 0.38f, 0.55f, 0.02f), new Vector3(0, -0.66f, 0), new Vector3(0, 25f, 72f)));
                    husk.Add(Transform(Leaf(0.8f, 0.36f, 0.55f, 0.02f), new Vector3(0, -0.66f, 0), new Vector3(0, 145f, 74f)));
                    P("husk", Combine(husk), Grad(Leafy("#8FC25A", "#3E6A24"), "#C9DE8A", 0.45f, Vector3.up, -0.6f, 0.0f));
                    P("stub", Tube(Line(new Vector3(0, -0.6f, 0), new Vector3(0, -0.78f, 0), 4), t => 0.07f, 16), Leafy("#A5C77A", "#5E7E3A"));
                    m.Pose = new Vector3(0, 60, -22);
                    break;
                }

                // ===================== DRINKS =====================
                case "water_bottle":
                {
                    var prof = Smooth(new List<Vector2>
                    {
                        new Vector2(0, -0.62f), new Vector2(0.17f, -0.62f), new Vector2(0.235f, -0.59f), new Vector2(0.25f, -0.52f), new Vector2(0.25f, 0.12f),
                        new Vector2(0.235f, 0.24f), new Vector2(0.17f, 0.38f), new Vector2(0.105f, 0.46f), new Vector2(0.095f, 0.5f), new Vector2(0.095f, 0.555f),
                    }, 6);
                    // Grip ribs between the label and the shoulder; a molded base.
                    Func<float, float, float> ribs = (a, t) =>
                    {
                        float y = Mathf.Lerp(-0.62f, 0.555f, t);
                        float rib = y > 0.0f && y < 0.18f ? 1f - 0.035f * Mathf.Pow(Mathf.Abs(Mathf.Sin(y * 70f)), 6f) : 1f;
                        return rib;
                    };
                    var glass = Plastic("#D8EEF8", "#7FAFC8", 0.7f); glass.Gloss = 0.92f; glass.Rim = 0.55f; glass.Alpha = 0.42f; glass.Wrap = 0.2f;
                    P("shell", Lathe(prof, 72, ribs), glass);
                    var waterProf = new List<Vector2> { new Vector2(0, -0.6f), new Vector2(0.225f, -0.6f), new Vector2(0.235f, -0.55f), new Vector2(0.235f, 0.16f), new Vector2(0, 0.16f) };
                    var water = new Mat { Base = H("#7CC3EC"), Shade = H("#2D74A8"), Spec = 0.3f, Gloss = 0.7f, Rim = 0.3f, Wrap = 0.2f, Alpha = 0.38f };
                    P("water", Lathe(waterProf, 64), water);
                    P("label", Lathe(new List<Vector2> { new Vector2(0.256f, -0.36f), new Vector2(0.256f, -0.02f) }, 72), Paper("#F7FBFF", "#9DB6C8"));
                    P("labelEdge", Lathe(new List<Vector2> { new Vector2(0.258f, -0.05f), new Vector2(0.258f, -0.02f) }, 72), Plastic("#3E8FE0", "#1E4F8A", 0.2f));
                    var drop = Extrude(Drop2(0.085f), 0.03f, 0.4f, 3);
                    P("drop", Transform(drop, new Vector3(0, -0.2f, -0.262f), new Vector3(0, 0, 0)), Plastic("#3B8EE0", "#1A4E8C", 0.4f));
                    P("neckRing", Lathe(Smooth(new List<Vector2> { new Vector2(0.095f, 0.555f), new Vector2(0.125f, 0.56f), new Vector2(0.125f, 0.58f), new Vector2(0.095f, 0.585f) }, 3), 48), Plastic("#2F6FC6", "#173E78"));
                    var cap = Lathe(Smooth(new List<Vector2> { new Vector2(0.11f, 0.59f), new Vector2(0.115f, 0.6f), new Vector2(0.115f, 0.72f), new Vector2(0.1f, 0.745f), new Vector2(0f, 0.75f) }, 4), 64,
                        (a, t) => t < 0.8f ? 1f + 0.025f * Mathf.Cos(28 * a * Mathf.PI * 2f) : 1f);
                    P("cap", cap, Plastic("#2F6FC6", "#173E78", 0.4f));
                    m.Pose = new Vector3(0, 18, 0);
                    break;
                }
                case "milk_carton":
                {
                    var white = Paper("#F8F5EF", "#9E9488");
                    P("body", Transform(RoundedBox(new Vector3(0.62f, 0.92f, 0.62f), 0.05f, 10), new Vector3(0, -0.12f, 0), Vector3.zero), white);
                    var roofOutline = new List<Vector2> { new Vector2(-0.31f, 0f), new Vector2(0.31f, 0f), new Vector2(0.0f, 0.3f) };
                    var roof = Extrude(Densify(roofOutline, 12), 0.6f, 0.06f, 2);
                    P("roof", Transform(roof, new Vector3(0, 0.335f, 0), new Vector3(0, 90, 0)), white);
                    P("fin", Transform(RoundedBox(new Vector3(0.66f, 0.09f, 0.05f), 0.02f, 4), new Vector3(0, 0.66f, 0), Vector3.zero), white);
                    P("fold", Transform(RoundedBox(new Vector3(0.64f, 0.02f, 0.64f), 0.01f, 3), new Vector3(0, 0.335f, 0), Vector3.zero), Paper("#E6E0D6", "#8E8578"));
                    P("band", Transform(RoundedBox(new Vector3(0.63f, 0.3f, 0.63f), 0.05f, 10), new Vector3(0, -0.1f, 0), Vector3.zero), Paper("#3B7FD2", "#1D4180"));
                    P("drop", Transform(Extrude(Drop2(0.085f), 0.03f, 0.4f, 3), new Vector3(0, -0.11f, -0.318f), Vector3.zero), Paper("#FFFFFF", "#B7C6D8"));
                    P("spout", Transform(Lathe(Smooth(new List<Vector2> { new Vector2(0.06f, 0), new Vector2(0.06f, 0.05f), new Vector2(0.05f, 0.065f), new Vector2(0, 0.07f) }, 3), 32, null, true),
                        new Vector3(0.0f, 0.46f, -0.17f), new Vector3(-45f, 0, 0)), Plastic("#3B7FD2", "#1D4180"));
                    m.Pose = new Vector3(0, 32, 0);
                    break;
                }
                case "juice_carton":
                {
                    var card = Paper("#F39A2C", "#9A4D10");
                    P("box", RoundedBox(new Vector3(0.52f, 0.78f, 0.34f), 0.035f, 10), card);
                    P("seam", Transform(RoundedBox(new Vector3(0.53f, 0.035f, 0.35f), 0.015f, 3), new Vector3(0, 0.38f, 0), Vector3.zero), Paper("#E1801C", "#8A400C"));
                    P("panel", Transform(RoundedBox(new Vector3(0.42f, 0.48f, 0.02f), 0.02f, 4), new Vector3(0, -0.06f, -0.17f), Vector3.zero), Paper("#FFE2A6", "#C08A3A"));
                    // Orange-slice emblem: a rind, a pale pith and eight raised segments.
                    P("rind", Transform(Extrude(Circle2(0.13f, 48), 0.02f, 0.3f), new Vector3(0, -0.06f, -0.185f), Vector3.zero), Paper("#F28A1E", "#A0430C"));
                    P("pith", Transform(Extrude(Circle2(0.112f, 48), 0.022f, 0.3f), new Vector3(0, -0.06f, -0.19f), Vector3.zero), Paper("#FFF1D2", "#C9A26A"));
                    var segs = new List<Mesh>();
                    for (int i = 0; i < 8; i++)
                    {
                        float a0 = i / 8f * Mathf.PI * 2f + 0.06f, a1 = (i + 1) / 8f * Mathf.PI * 2f - 0.06f;
                        var o = new List<Vector2> { new Vector2(Mathf.Cos((a0 + a1) / 2) * 0.012f, Mathf.Sin((a0 + a1) / 2) * 0.012f) };
                        for (int k = 0; k <= 6; k++) { float a = Mathf.Lerp(a0, a1, k / 6f); o.Add(new Vector2(Mathf.Cos(a) * 0.095f, Mathf.Sin(a) * 0.095f)); }
                        segs.Add(Extrude(o, 0.024f, 0.35f));
                    }
                    P("segments", Transform(Combine(segs), new Vector3(0, -0.06f, -0.195f), Vector3.zero), Paper("#FFAE3C", "#C26A12"));
                    P("straw", Tube(Bezier(new Vector3(0.13f, 0.3f, 0.04f), new Vector3(0.13f, 0.55f, 0.04f), new Vector3(0.13f, 0.62f, 0.04f), new Vector3(0.03f, 0.7f, 0.04f), 20),
                        t => 0.026f, 18), Plastic("#F06C8E", "#9C2C4A", 0.45f));
                    P("strawHole", Transform(Extrude(Circle2(0.04f, 24), 0.01f, 0.4f), new Vector3(0.13f, 0.392f, 0.04f), new Vector3(90, 0, 0)), Paper("#D9D4CC", "#8E877C"));
                    m.Pose = new Vector3(0, -24, 0);
                    break;
                }
                case "soda_can":
                {
                    var body = Lathe(Smooth(new List<Vector2>
                    {
                        new Vector2(0.2f, -0.47f), new Vector2(0.255f, -0.43f), new Vector2(0.27f, -0.37f), new Vector2(0.27f, 0.33f), new Vector2(0.245f, 0.42f), new Vector2(0.205f, 0.47f),
                    }, 6), 72);
                    P("body", body, Grad(PaintedMetal("#D9312F", "#701210"), "#F0584A", 0.25f, Vector3.up, 0.1f, 0.35f));
                    P("top", Lathe(Smooth(new List<Vector2> { new Vector2(0.205f, 0.468f), new Vector2(0.215f, 0.5f), new Vector2(0.2f, 0.505f), new Vector2(0.185f, 0.48f), new Vector2(0f, 0.47f) }, 4), 64), Metal());
                    P("bottom", Lathe(Smooth(new List<Vector2> { new Vector2(0f, -0.49f), new Vector2(0.17f, -0.5f), new Vector2(0.2f, -0.47f) }, 4), 64), Metal("#B6BCC3"));
                    P("tab", Transform(RoundedBox(new Vector3(0.1f, 0.012f, 0.16f), 0.006f, 3), new Vector3(0, 0.485f, 0.03f), new Vector3(-4, 0, 0)), Metal("#D2D7DC"));
                    P("rivet", Transform(Ellipsoid(new Vector3(0.022f, 0.01f, 0.022f)), new Vector3(0, 0.483f, -0.01f), Vector3.zero), Metal());
                    var bubbles = new List<Mesh>
                    {
                        Transform(Extrude(Circle2(0.062f, 32), 0.02f, 0.4f), new Vector3(-0.03f, -0.08f, -0.271f), Vector3.zero),
                        Transform(Extrude(Circle2(0.04f, 32), 0.02f, 0.4f), new Vector3(0.07f, 0.04f, -0.262f), new Vector3(0, -15, 0)),
                        Transform(Extrude(Circle2(0.028f, 32), 0.02f, 0.4f), new Vector3(-0.07f, 0.1f, -0.261f), new Vector3(0, 15, 0)),
                    };
                    P("bubbles", Combine(bubbles), Plastic("#FFFFFF", "#C9CED6", 0.3f));
                    m.Pose = new Vector3(0, -12, 0);
                    break;
                }

                // ===================== KITCHENWARE =====================
                case "spoon":
                {
                    var handle = Tube(Bezier(new Vector3(0.7f, -0.02f, 0), new Vector3(0.4f, 0.03f, 0), new Vector3(0.05f, 0.04f, 0), new Vector3(-0.1f, 0.0f, 0), 30),
                        t => Mathf.Lerp(0.075f, 0.04f, SStep(0f, 1f, t)) * (t < 0.04f ? Mathf.Sqrt(t / 0.04f) * 0.4f + 0.6f : 1f), 24, null, 0.5f, Vector3.up);
                    var bowl = Warp(Sphere(1f, 32, 48), v =>
                    {
                        var p = new Vector3(v.x * 0.31f, v.y, v.z * 0.22f);
                        p.y = v.y > 0 ? -v.y * 0.075f + 0.03f : v.y * 0.1f; // a dish: the top is pushed down into a bowl with a rounded rim
                        return p;
                    });
                    P("handle", handle, Metal());
                    P("bowl", Transform(bowl, new Vector3(-0.36f, 0.0f, 0), new Vector3(0, 0, -4)), Metal());
                    m.Pose = new Vector3(-62, 22, 34);
                    break;
                }
                case "fork":
                {
                    var handle = Tube(Bezier(new Vector3(0.7f, -0.02f, 0), new Vector3(0.4f, 0.03f, 0), new Vector3(0.05f, 0.04f, 0), new Vector3(-0.12f, 0.01f, 0), 30),
                        t => Mathf.Lerp(0.075f, 0.04f, SStep(0f, 1f, t)) * (t < 0.04f ? Mathf.Sqrt(t / 0.04f) * 0.4f + 0.6f : 1f), 24, null, 0.5f, Vector3.up);
                    P("handle", handle, Metal());
                    var head = Warp(RoundedBox(new Vector3(0.22f, 0.045f, 0.25f), 0.02f, 6), v => new Vector3(v.x, v.y, v.z * (1f - 0.35f * Mathf.Clamp01((v.x + 0.1f) / 0.2f))));
                    P("head", Transform(head, new Vector3(-0.2f, 0.0f, 0), Vector3.zero), Metal());
                    var tines = new List<Mesh>();
                    for (int i = 0; i < 4; i++)
                    {
                        float z = Mathf.Lerp(-0.09f, 0.09f, i / 3f);
                        tines.Add(Tube(Bezier(new Vector3(-0.27f, 0.0f, z), new Vector3(-0.38f, 0.01f, z), new Vector3(-0.47f, 0.02f, z), new Vector3(-0.56f, 0.035f, z * 1.05f), 12),
                            t => Mathf.Lerp(0.026f, 0.019f, t), 14, null, 0.65f, Vector3.up));
                    }
                    P("tines", Combine(tines), Metal());
                    m.Pose = new Vector3(-62, 22, 34);
                    break;
                }
                case "plate":
                {
                    var prof = Smooth(new List<Vector2>
                    {
                        new Vector2(0f, -0.02f), new Vector2(0.28f, -0.02f), new Vector2(0.3f, -0.045f), new Vector2(0.34f, -0.04f), new Vector2(0.56f, 0.05f),
                        new Vector2(0.63f, 0.095f), new Vector2(0.635f, 0.115f), new Vector2(0.6f, 0.115f), new Vector2(0.45f, 0.055f), new Vector2(0.33f, 0.025f), new Vector2(0f, 0.02f),
                    }, 6);
                    P("plate", Lathe(prof, 96), Ceramic("#F7F0E3", "#A8987E"));
                    P("rimLine", Lathe(Smooth(new List<Vector2> { new Vector2(0.62f, 0.112f), new Vector2(0.6f, 0.117f), new Vector2(0.575f, 0.106f) }, 3), 96), Ceramic("#3E7DC4", "#1B3F78"));
                    m.Pose = new Vector3(-40, 0, 0);
                    break;
                }
                case "mug":
                {
                    var prof = Smooth(new List<Vector2>
                    {
                        new Vector2(0f, -0.42f), new Vector2(0.25f, -0.42f), new Vector2(0.29f, -0.39f), new Vector2(0.3f, -0.32f), new Vector2(0.3f, 0.34f),
                        new Vector2(0.295f, 0.39f), new Vector2(0.275f, 0.405f), new Vector2(0.255f, 0.39f), new Vector2(0.255f, -0.3f), new Vector2(0.2f, -0.34f), new Vector2(0f, -0.34f),
                    }, 6);
                    P("mug", Lathe(prof, 96), Ceramic("#3F74C3", "#173B78"));
                    P("handle", Tube(Bezier(new Vector3(0.28f, 0.24f, 0), new Vector3(0.56f, 0.28f, 0), new Vector3(0.56f, -0.24f, 0), new Vector3(0.28f, -0.2f, 0), 30),
                        t => 0.058f, 24, null, 0.78f, Vector3.forward), Ceramic("#3F74C3", "#173B78"));
                    m.Pose = new Vector3(-14, 28, 0);
                    break;
                }
                case "spatula":
                {
                    var head = new List<Mesh>();
                    float w = 0.38f, h = 0.44f, slotW = 0.045f, bar = (w - 3 * slotW) / 4f;
                    for (int i = 0; i < 4; i++)
                        head.Add(Transform(RoundedBox(new Vector3(bar + 0.004f, h, 0.045f), 0.02f, 5), new Vector3(-w / 2 + bar / 2 + i * (bar + slotW), 0.39f, 0), Vector3.zero));
                    head.Add(Transform(RoundedBox(new Vector3(w, 0.07f, 0.047f), 0.022f, 5), new Vector3(0, 0.39f + h / 2 - 0.035f, 0), Vector3.zero));
                    head.Add(Transform(RoundedBox(new Vector3(w * 0.92f, 0.07f, 0.047f), 0.022f, 5), new Vector3(0, 0.39f - h / 2 + 0.035f, 0), Vector3.zero));
                    P("head", Combine(head), Plastic("#3A3638", "#141214", 0.28f));
                    P("neck", Tube(Line(new Vector3(0, 0.17f, 0), new Vector3(0, 0.02f, 0), 8), t => Mathf.Lerp(0.05f, 0.04f, t), 20, null, 0.55f, Vector3.forward), Plastic("#3A3638", "#141214", 0.28f));
                    P("collar", Tube(Line(new Vector3(0, 0.03f, 0), new Vector3(0, -0.08f, 0), 6), t => 0.058f, 32), Metal());
                    P("handle", Tube(Bezier(new Vector3(0, -0.08f, 0), new Vector3(0, -0.3f, 0), new Vector3(0, -0.55f, 0), new Vector3(0, -0.8f, 0), 24),
                        t => 0.06f * (1f + 0.12f * Mathf.Sin(t * Mathf.PI)) * (t > 0.96f ? Mathf.Sqrt((1 - t) / 0.04f) * 0.5f + 0.5f : 1f), 28), Grad(Wood(), "#9C6838", 0.4f, Vector3.up, -0.8f, -0.1f));
                    m.Pose = new Vector3(-22, 18, -22);
                    break;
                }
                case "pan":
                {
                    var outer = Smooth(new List<Vector2> { new Vector2(0f, -0.07f), new Vector2(0.36f, -0.07f), new Vector2(0.44f, -0.045f), new Vector2(0.5f, 0.06f), new Vector2(0.525f, 0.1f), new Vector2(0.515f, 0.115f) }, 6);
                    var inner = Smooth(new List<Vector2> { new Vector2(0.515f, 0.115f), new Vector2(0.495f, 0.1f), new Vector2(0.45f, 0.0f), new Vector2(0.38f, -0.04f), new Vector2(0f, -0.045f) }, 6);
                    P("outer", Lathe(outer, 96), new Mat { Base = H("#3D3B40"), Shade = H("#121113"), Spec = 0.45f, Gloss = 0.65f, Rim = 0.25f, Wrap = 0.2f, Metal = 0.15f });
                    P("inner", Lathe(inner, 96), new Mat { Base = H("#2D2C31"), Shade = H("#0E0D10"), Spec = 0.6f, Gloss = 0.75f, Rim = 0.1f, Wrap = 0.2f, Metal = 0.1f, Noise = 0.02f, NoiseScale = 40f });
                    P("rim", Lathe(Smooth(new List<Vector2> { new Vector2(0.527f, 0.1f), new Vector2(0.522f, 0.118f), new Vector2(0.51f, 0.118f) }, 3), 96), Metal("#9AA2AB"));
                    P("handle", Tube(Bezier(new Vector3(0.48f, 0.06f, 0), new Vector3(0.75f, 0.1f, 0), new Vector3(1.0f, 0.13f, 0), new Vector3(1.3f, 0.15f, 0), 24),
                        t => Mathf.Lerp(0.055f, 0.07f, t) * (t > 0.95f ? Mathf.Sqrt((1 - t) / 0.05f) * 0.5f + 0.5f : 1f), 24, null, 0.62f, Vector3.up),
                        Plastic("#2E2B2E", "#0D0C0D", 0.3f));
                    var rivets = new List<Mesh>
                    {
                        Transform(Ellipsoid(new Vector3(0.022f, 0.022f, 0.012f)), new Vector3(0.49f, 0.07f, -0.035f), new Vector3(0, -80, 0)),
                        Transform(Ellipsoid(new Vector3(0.022f, 0.022f, 0.012f)), new Vector3(0.49f, 0.07f, 0.035f), new Vector3(0, -100, 0)),
                    };
                    P("rivets", Combine(rivets), Metal());
                    m.Pose = new Vector3(-38, 30, 0);
                    break;
                }
                // ===================== LIVING ROOM =====================
                case "book":
                {
                    var cover = Fabric("#C2453B", "#6A1A14"); cover.Spec = 0.12f; cover.Gloss = 0.3f; cover.Noise = 0.04f;
                    P("frontCover", Transform(RoundedBox(new Vector3(0.6f, 0.86f, 0.045f), 0.02f, 5), new Vector3(0.02f, 0, -0.135f), Vector3.zero), cover);
                    P("backCover", Transform(RoundedBox(new Vector3(0.6f, 0.86f, 0.045f), 0.02f, 5), new Vector3(0.02f, 0, 0.135f), Vector3.zero), cover);
                    P("spine", Tube(Line(new Vector3(-0.27f, -0.43f, 0), new Vector3(-0.27f, 0.43f, 0), 8), t => 0.14f, 32, (a, t) => 1f, 1f, Vector3.right), cover);
                    P("pages", Transform(RoundedBox(new Vector3(0.56f, 0.82f, 0.23f), 0.015f, 5), new Vector3(0.04f, 0, 0), Vector3.zero), Paper("#F5EDD9", "#B9A584"));
                    var bands = new List<Mesh>
                    {
                        Tube(Line(new Vector3(-0.27f, 0.27f, 0), new Vector3(-0.27f, 0.31f, 0), 2), t => 0.143f, 32, null, 1f, Vector3.right),
                        Tube(Line(new Vector3(-0.27f, -0.31f, 0), new Vector3(-0.27f, -0.27f, 0), 2), t => 0.143f, 32, null, 1f, Vector3.right),
                        Transform(RoundedBox(new Vector3(0.3f, 0.12f, 0.012f), 0.01f, 3), new Vector3(0.06f, 0.18f, -0.16f), Vector3.zero),
                    };
                    P("gold", Combine(bands), Metal("#D9B260", 0.55f));
                    m.Pose = new Vector3(4, -38, 0);
                    break;
                }
                case "magazine":
                {
                    Mesh Curl(Mesh mm) => Warp(mm, v => new Vector3(v.x, v.y + 0.06f * v.x * v.x * 4f, v.z));
                    P("pages", Curl(Transform(RoundedBox(new Vector3(0.64f, 0.035f, 0.86f), 0.012f, 6), new Vector3(0.01f, -0.025f, 0.01f), Vector3.zero)), Paper("#F7F2E8", "#BDB2A0"));
                    var coverMat = Plastic("#5FA8C8", "#24607E", 0.35f); coverMat.Gloss = 0.7f;
                    P("cover", Curl(RoundedBox(new Vector3(0.66f, 0.02f, 0.88f), 0.01f, 6)), coverMat);
                    var deco = new List<Mesh>
                    {
                        Curl(Transform(RoundedBox(new Vector3(0.5f, 0.012f, 0.13f), 0.006f, 3), new Vector3(0, 0.015f, -0.32f), Vector3.zero)),
                        Curl(Transform(RoundedBox(new Vector3(0.22f, 0.012f, 0.04f), 0.006f, 3), new Vector3(-0.14f, 0.015f, 0.3f), Vector3.zero)),
                        Curl(Transform(RoundedBox(new Vector3(0.22f, 0.012f, 0.04f), 0.006f, 3), new Vector3(-0.14f, 0.015f, 0.37f), Vector3.zero)),
                    };
                    P("title", Combine(deco), Paper("#FFFFFF", "#C9CFD6"));
                    P("photo", Curl(Transform(Extrude(Circle2(0.15f, 40), 0.014f, 0.3f), new Vector3(0.08f, 0.016f, 0.04f), new Vector3(90, 0, 0))), Paper("#F2A34F", "#A85A1A"));
                    m.Pose = new Vector3(-52, 18, 0);
                    break;
                }
                case "newspaper":
                {
                    var paper = Paper("#EEE8DA", "#A39A86");
                    P("lower", Transform(RoundedBox(new Vector3(0.72f, 0.04f, 0.52f), 0.012f, 6), new Vector3(0, -0.035f, 0), Vector3.zero), paper);
                    P("upper", Transform(RoundedBox(new Vector3(0.7f, 0.04f, 0.5f), 0.012f, 6), new Vector3(0.01f, 0.01f, 0), new Vector3(0, 0, 1.5f)), paper);
                    P("fold", Tube(Line(new Vector3(-0.355f, -0.012f, -0.25f), new Vector3(-0.355f, -0.012f, 0.25f), 6), t => 0.035f, 20, null, 1f, Vector3.up), paper);
                    var ink = new List<Mesh> { Transform(RoundedBox(new Vector3(0.56f, 0.01f, 0.08f), 0.004f, 2), new Vector3(0.02f, 0.034f, -0.17f), Vector3.zero) };
                    for (int c = 0; c < 3; c++)
                        for (int r = 0; r < 4; r++)
                            ink.Add(Transform(RoundedBox(new Vector3(0.16f, 0.008f, 0.025f), 0.003f, 2), new Vector3(-0.17f + c * 0.19f, 0.033f, -0.05f + r * 0.065f), Vector3.zero));
                    P("ink", Combine(ink), Paper("#6E6A64", "#3A3733"));
                    P("photo", Transform(RoundedBox(new Vector3(0.16f, 0.009f, 0.12f), 0.004f, 2), new Vector3(0.21f, 0.034f, 0.02f), Vector3.zero), Paper("#8FB2C9", "#4D6C80"));
                    m.Pose = new Vector3(-55, -18, 0);
                    break;
                }
                case "cushion":
                {
                    var pillow = Warp(RoundedBox(new Vector3(0.82f, 0.82f, 0.34f), 0.15f, 16), v =>
                    {
                        float nx = Mathf.Clamp01(Mathf.Abs(v.x) / 0.41f), ny = Mathf.Clamp01(Mathf.Abs(v.y) / 0.41f);
                        float puff = Mathf.Sqrt(Mathf.Max(0.05f, (1 - Mathf.Pow(nx, 3)) * (1 - Mathf.Pow(ny, 3))));
                        float pinch = 1f - 0.08f * nx * ny;                      // corners pull in to soft points
                        return new Vector3(v.x * pinch, v.y * pinch, v.z * (0.25f + 0.75f * puff));
                    });
                    P("pillow", pillow, Fabric("#E4B23E", "#8A5A12"));
                    var edge = new List<Vector3>();
                    for (int i = 0; i <= 64; i++)
                    {
                        float a = i / 64f * Mathf.PI * 2f;
                        float x = Mathf.Cos(a), y = Mathf.Sin(a);
                        float k = 1f / Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 4) + Mathf.Pow(Mathf.Abs(y), 4), 0.25f);   // rounded square
                        edge.Add(new Vector3(x * k * 0.385f, y * k * 0.385f, 0));
                    }
                    P("piping", Tube(edge, t => 0.025f, 12, null, 1f, Vector3.forward, false), Fabric("#C98F22", "#6E4410"));
                    P("button", Transform(Ellipsoid(new Vector3(0.045f, 0.045f, 0.025f)), new Vector3(0, 0, -0.165f), Vector3.zero), Fabric("#B9821E", "#6E4410"));
                    m.Pose = new Vector3(-8, -24, 6);
                    break;
                }
                case "folded_blanket":
                {
                    var wool = Fabric("#E7A3A0", "#8E4A48");
                    var layers = new List<Mesh>();
                    for (int i = 0; i < 3; i++)
                        layers.Add(Transform(RoundedBox(new Vector3(0.84f - i * 0.03f, 0.11f, 0.56f), 0.05f, 8), new Vector3(i * 0.015f, -0.13f + i * 0.115f, 0), new Vector3(0, i * 1.5f, 0)));
                    P("layers", Combine(layers), wool);
                    var stripes = new List<Mesh>();
                    for (int i = 0; i < 3; i++)
                        stripes.Add(Transform(RoundedBox(new Vector3(0.05f, 0.112f, 0.565f), 0.03f, 4), new Vector3(0.27f + i * 0.015f, -0.13f + i * 0.115f, 0), new Vector3(0, i * 1.5f, 0)));
                    P("stripe", Combine(stripes), Fabric("#FFF4E6", "#B9A08C"));
                    var fringe = new List<Mesh>();
                    for (int k = 0; k < 9; k++)
                    {
                        float z = Mathf.Lerp(-0.24f, 0.24f, k / 8f);
                        fringe.Add(Tube(Bezier(new Vector3(0.43f, 0.11f, z), new Vector3(0.47f, 0.1f, z), new Vector3(0.49f, 0.06f, z), new Vector3(0.5f, 0.03f, z + 0.01f), 6), t => 0.012f, 8));
                    }
                    P("fringe", Combine(fringe), Fabric("#F2C1BE", "#9A5856"));
                    m.Pose = new Vector3(-22, -28, 0);
                    break;
                }
                case "slipper":
                {
                    // A cosy slipper seen from the side: long cream sole, a plush upper over the toes with an open back.
                    var foot = new List<Vector2>();
                    for (int i = 0; i < 48; i++)
                    {
                        float a = i / 48f * Mathf.PI * 2f;
                        float x = Mathf.Cos(a) * 0.19f * (1f + 0.15f * Mathf.Sin(a)), y = Mathf.Sin(a) * 0.48f;
                        x *= y < 0 ? 0.85f : 1f;
                        foot.Add(new Vector2(x, y));
                    }
                    P("sole", Transform(Extrude(foot, 0.08f, 0.35f, 3), Vector3.zero, new Vector3(90, 0, 0)), Plastic("#EFE3CF", "#A8977A", 0.12f));
                    // Upper: a dome over the front 60% only.
                    var upper = Warp(Sphere(1f, 32, 48), v => new Vector3(v.x * 0.205f, Mathf.Max(0, v.y) * 0.19f + 0.04f, v.z * 0.3f));
                    P("upper", Transform(upper, new Vector3(0, 0, 0.17f), Vector3.zero), Fabric("#8C7BC0", "#4A3A7A"));
                    // Fluffy cuff around the opening.
                    var cuff = new List<Vector3>();
                    for (int i = 0; i <= 32; i++) { float a = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / 32f); cuff.Add(new Vector3(Mathf.Sin(a) * 0.205f, 0.04f + Mathf.Cos(a) * 0.17f, -0.13f)); }
                    P("cuff", Tube(cuff, t => 0.045f, 16, null, 1f, Vector3.forward), Fabric("#F4F0FA", "#B9AEC9"));
                    var pom = Sculpt(d => 0.07f * (1f + 0.18f * (Noise3(d * 9f) - 0.5f)), 24, 32);
                    P("pom", Transform(pom, new Vector3(0, 0.22f, 0.24f), Vector3.zero), Fabric("#FFFFFF", "#C9C2D6"));
                    m.Pose = new Vector3(-28, -62, 0);
                    break;
                }
                case "remote":
                {
                    var body = Warp(RoundedBox(new Vector3(0.24f, 0.07f, 0.86f), 0.035f, 8), v => new Vector3(v.x * (1f - 0.12f * Mathf.Clamp01(v.z / 0.43f)), v.y, v.z));
                    P("body", body, Plastic("#3A3A42", "#121216", 0.35f));
                    P("power", Transform(Extrude(Circle2(0.035f, 24), 0.02f, 0.4f), new Vector3(0, 0.04f, -0.33f), new Vector3(90, 0, 0)), Plastic("#E2483C", "#7A1A12", 0.4f));
                    P("dpad", Transform(Extrude(Circle2(0.075f, 32), 0.018f, 0.4f), new Vector3(0, 0.04f, -0.15f), new Vector3(90, 0, 0)), Plastic("#6D6E78", "#2A2A30"));
                    P("ok", Transform(Extrude(Circle2(0.032f, 24), 0.024f, 0.4f), new Vector3(0, 0.046f, -0.15f), new Vector3(90, 0, 0)), Plastic("#9DA0AC", "#4A4C56"));
                    var keys = new List<Mesh>();
                    for (int r = 0; r < 4; r++)
                        for (int c = 0; c < 3; c++)
                            keys.Add(Transform(RoundedBox(new Vector3(0.045f, 0.022f, 0.035f), 0.01f, 3), new Vector3(-0.06f + c * 0.06f, 0.042f, 0.03f + r * 0.065f), Vector3.zero));
                    P("keys", Combine(keys), Plastic("#B6B9C4", "#5A5D68", 0.25f));
                    m.Pose = new Vector3(-62, 28, 0);
                    break;
                }
                case "game_controller":
                {
                    var shell = Plastic("#EFEBE3", "#9E968A", 0.35f);
                    var parts = new List<Mesh>
                    {
                        RoundedBox(new Vector3(0.62f, 0.15f, 0.34f), 0.07f, 10),
                        Transform(Ellipsoid(new Vector3(0.13f, 0.1f, 0.21f), 24, 32), new Vector3(-0.27f, -0.01f, 0.12f), new Vector3(0, -18, 0)),
                        Transform(Ellipsoid(new Vector3(0.13f, 0.1f, 0.21f), 24, 32), new Vector3(0.27f, -0.01f, 0.12f), new Vector3(0, 18, 0)),
                    };
                    P("shell", Combine(parts), shell);
                    var dpad = new List<Mesh>
                    {
                        Transform(RoundedBox(new Vector3(0.12f, 0.03f, 0.04f), 0.012f, 3), new Vector3(-0.2f, 0.08f, -0.03f), Vector3.zero),
                        Transform(RoundedBox(new Vector3(0.04f, 0.03f, 0.12f), 0.012f, 3), new Vector3(-0.2f, 0.08f, -0.03f), Vector3.zero),
                    };
                    P("dpad", Combine(dpad), Plastic("#4A4D58", "#1E2026"));
                    string[] cols = { "#E2483C", "#3B8EE0", "#5FB55A", "#F2C53D" };
                    Vector2[] at = { new Vector2(0.2f, -0.08f), new Vector2(0.27f, -0.03f), new Vector2(0.2f, 0.02f), new Vector2(0.13f, -0.03f) };
                    for (int i = 0; i < 4; i++)
                        P("btn" + i, Transform(Ellipsoid(new Vector3(0.026f, 0.018f, 0.026f)), new Vector3(at[i].x, 0.08f, at[i].y), Vector3.zero), Plastic(cols[i], "#3A3A3A", 0.45f));
                    var sticks = new List<Mesh>
                    {
                        Transform(Lathe(Smooth(new List<Vector2> { new Vector2(0.05f, 0), new Vector2(0.055f, 0.03f), new Vector2(0.045f, 0.045f), new Vector2(0, 0.04f) }, 3), 32, null, true), new Vector3(-0.09f, 0.07f, 0.09f), Vector3.zero),
                        Transform(Lathe(Smooth(new List<Vector2> { new Vector2(0.05f, 0), new Vector2(0.055f, 0.03f), new Vector2(0.045f, 0.045f), new Vector2(0, 0.04f) }, 3), 32, null, true), new Vector3(0.09f, 0.07f, 0.09f), Vector3.zero),
                    };
                    P("sticks", Combine(sticks), Plastic("#4A4D58", "#1E2026"));
                    m.Pose = new Vector3(-48, 12, 0);
                    break;
                }
                case "headphones":
                {
                    var arc = new List<Vector3>();
                    for (int i = 0; i <= 40; i++) { float a = Mathf.Lerp(Mathf.PI, 0f, i / 40f); arc.Add(new Vector3(Mathf.Cos(a) * 0.4f, Mathf.Sin(a) * 0.46f, 0)); }
                    P("band", Tube(arc, t => 0.05f, 20, null, 0.6f, Vector3.forward), Plastic("#E88464", "#8E3A22", 0.35f));
                    var pad = new List<Vector3>();
                    for (int i = 0; i <= 30; i++) { float a = Mathf.Lerp(Mathf.PI * 0.72f, Mathf.PI * 0.28f, i / 30f); pad.Add(new Vector3(Mathf.Cos(a) * 0.37f, Mathf.Sin(a) * 0.43f, 0)); }
                    P("pad", Tube(pad, t => 0.045f, 16, null, 0.7f, Vector3.forward), Fabric("#5A4A48", "#2A2220"));
                    var cup = Lathe(Smooth(new List<Vector2> { new Vector2(0f, -0.06f), new Vector2(0.14f, -0.06f), new Vector2(0.17f, -0.03f), new Vector2(0.17f, 0.03f), new Vector2(0.15f, 0.06f), new Vector2(0f, 0.06f) }, 4), 48);
                    P("cupL", Transform(cup, new Vector3(-0.42f, -0.06f, 0), new Vector3(0, 0, 90)), Plastic("#E88464", "#8E3A22", 0.35f));
                    P("cupR", Transform(cup, new Vector3(0.42f, -0.06f, 0), new Vector3(0, 0, 90)), Plastic("#E88464", "#8E3A22", 0.35f));
                    var ring = new List<Vector3>();
                    for (int i = 0; i <= 40; i++) { float a = i / 40f * Mathf.PI * 2f; ring.Add(new Vector3(0, Mathf.Cos(a) * 0.12f, Mathf.Sin(a) * 0.12f)); }
                    P("cushionL", Transform(Tube(ring, t => 0.045f, 14, null, 1f, Vector3.right, false), new Vector3(-0.36f, -0.06f, 0), Vector3.zero), Fabric("#4A3E3C", "#1E1918"));
                    P("cushionR", Transform(Tube(ring, t => 0.045f, 14, null, 1f, Vector3.right, false), new Vector3(0.36f, -0.06f, 0), Vector3.zero), Fabric("#4A3E3C", "#1E1918"));
                    m.Pose = new Vector3(-8, 28, 0);
                    break;
                }
                case "candle":
                {
                    P("saucer", Lathe(Smooth(new List<Vector2> { new Vector2(0f, -0.42f), new Vector2(0.3f, -0.42f), new Vector2(0.36f, -0.38f), new Vector2(0.37f, -0.35f), new Vector2(0.3f, -0.37f), new Vector2(0f, -0.375f) }, 4), 64), Metal("#D2AE62", 0.6f));
                    var body = Lathe(Smooth(new List<Vector2> { new Vector2(0f, -0.38f), new Vector2(0.19f, -0.38f), new Vector2(0.2f, -0.3f), new Vector2(0.2f, 0.18f), new Vector2(0.185f, 0.22f), new Vector2(0.12f, 0.2f), new Vector2(0f, 0.17f) }, 5), 64,
                        (a, t) => 1f + 0.015f * Mathf.Sin(a * Mathf.PI * 6f) * t);
                    P("wax", body, Wax("#F6EDD8", "#B9A27A"));
                    var drips = new List<Mesh>();
                    float[] ang = { 0.3f, 2.1f, 4.0f };
                    float[] len = { 0.16f, 0.24f, 0.12f };
                    for (int i = 0; i < 3; i++)
                    {
                        var dir = new Vector3(Mathf.Cos(ang[i]), 0, Mathf.Sin(ang[i]));
                        drips.Add(Tube(Line(dir * 0.2f + Vector3.up * 0.2f, dir * 0.205f + Vector3.up * (0.2f - len[i]), 8), t => Mathf.Lerp(0.022f, 0.03f, t), 12));
                    }
                    P("drips", Combine(drips), Wax("#F9F1DE", "#C2AA80"));
                    P("wick", Tube(Bezier(new Vector3(0, 0.17f, 0), new Vector3(0, 0.22f, 0), new Vector3(0.01f, 0.25f, 0), new Vector3(0.02f, 0.27f, 0), 6), t => 0.01f, 8), Stem("#2A1E16"));
                    var flame = Warp(Sphere(1f, 24, 32), v => new Vector3(v.x * 0.055f * (1f - Mathf.Max(0, v.y) * 0.75f), v.y * 0.11f, v.z * 0.055f * (1f - Mathf.Max(0, v.y) * 0.75f)));
                    P("flame", Transform(flame, new Vector3(0.02f, 0.37f, 0), Vector3.zero), Glow("#FFC24A"));
                    P("core", Transform(Ellipsoid(new Vector3(0.025f, 0.05f, 0.025f)), new Vector3(0.02f, 0.345f, -0.03f), Vector3.zero), Glow("#FFF6D6"));
                    m.Pose = new Vector3(0, 15, 0);
                    break;
                }
                case "photo_frame":
                {
                    var wood = Wood("#4A3527", "#1E120A"); wood.Spec = 0.25f; wood.Gloss = 0.5f;
                    float W = 0.64f, Hh = 0.8f, b = 0.09f, d = 0.07f;
                    var bars = new List<Mesh>
                    {
                        Transform(RoundedBox(new Vector3(W, b, d), 0.025f, 4), new Vector3(0, Hh / 2 - b / 2, 0), Vector3.zero),
                        Transform(RoundedBox(new Vector3(W, b, d), 0.025f, 4), new Vector3(0, -Hh / 2 + b / 2, 0), Vector3.zero),
                        Transform(RoundedBox(new Vector3(b, Hh, d), 0.025f, 4), new Vector3(-W / 2 + b / 2, 0, 0), Vector3.zero),
                        Transform(RoundedBox(new Vector3(b, Hh, d), 0.025f, 4), new Vector3(W / 2 - b / 2, 0, 0), Vector3.zero),
                    };
                    P("frame", Combine(bars), wood);
                    P("mat", Transform(RoundedBox(new Vector3(W - 2 * b + 0.01f, Hh - 2 * b + 0.01f, 0.02f), 0.005f, 3), new Vector3(0, 0, 0.012f), Vector3.zero), Paper("#BFE0F0", "#6E9AB2"));
                    P("sun", Transform(Extrude(Circle2(0.06f, 32), 0.012f, 0.3f), new Vector3(0.1f, 0.14f, -0.004f), Vector3.zero), Paper("#F7C760", "#B07E1A"));
                    P("hills", Transform(Extrude(new List<Vector2> { new Vector2(-0.23f, -0.31f), new Vector2(0.23f, -0.31f), new Vector2(0.23f, -0.12f), new Vector2(0.12f, -0.02f), new Vector2(0.0f, -0.1f), new Vector2(-0.1f, 0.02f), new Vector2(-0.23f, -0.1f) }, 0.014f, 0.25f),
                        new Vector3(0, 0, -0.006f), Vector3.zero), Paper("#7FB27A", "#3E6A3A"));
                    P("stand", Transform(RoundedBox(new Vector3(0.12f, 0.5f, 0.03f), 0.012f, 3), new Vector3(0, -0.12f, 0.16f), new Vector3(-22, 0, 0)), wood);
                    m.Pose = new Vector3(6, -26, 0);
                    break;
                }
                case "succulent":
                {
                    P("pot", Lathe(Smooth(new List<Vector2> { new Vector2(0f, -0.36f), new Vector2(0.16f, -0.36f), new Vector2(0.21f, -0.05f), new Vector2(0.245f, -0.04f), new Vector2(0.245f, 0.02f), new Vector2(0.21f, 0.02f), new Vector2(0.2f, -0.02f), new Vector2(0f, -0.03f) }, 4), 64),
                        Ceramic("#C9734E", "#6E2E16"));
                    P("soil", Transform(Ellipsoid(new Vector3(0.2f, 0.02f, 0.2f)), new Vector3(0, -0.015f, 0), Vector3.zero), Stem("#4A3324"));
                    var leaves = new List<Mesh>();
                    int[] count = { 7, 6, 5, 3 };
                    float[] tilt = { 20f, 36f, 54f, 72f }, len = { 0.32f, 0.27f, 0.2f, 0.13f };
                    for (int ring = 0; ring < 4; ring++)
                        for (int i = 0; i < count[ring]; i++)
                        {
                            var leaf = Warp(Sphere(1f, 16, 20), v =>
                            {
                                float x = (v.x + 1) * 0.5f; // 0 at base → 1 at tip
                                float w = Mathf.Sin(Mathf.Clamp01(x * 0.95f + 0.05f) * Mathf.PI) * (1f - x * 0.35f);
                                return new Vector3(x * len[ring], v.y * 0.05f * w + 0.012f * x * x, v.z * 0.09f * w);
                            });
                            float yaw = i * 360f / count[ring] + ring * 23f;
                            leaves.Add(Transform(leaf, new Vector3(0, 0.0f + ring * 0.03f, 0), new Vector3(0, yaw, tilt[ring])));
                        }
                    P("rosette", Combine(leaves), Grad(Produce("#7DB58A", "#2E5A3E", 0.2f, 0.4f), "#E9A3A0", 0.45f, Vector3.up, 0.18f, 0.06f));
                    m.Pose = new Vector3(-12, 20, 0);
                    break;
                }

                default: throw new ArgumentException("No 3D model for " + id);
            }
            return m;
        }

        /// <summary>Adds evenly spaced points along a polygon so extrusion bevels stay smooth.</summary>
        static List<Vector2> Densify(IList<Vector2> poly, int per)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % poly.Count];
                for (int k = 0; k < per; k++) o.Add(Vector2.Lerp(a, b, k / (float)per));
            }
            return o;
        }
    }
}
