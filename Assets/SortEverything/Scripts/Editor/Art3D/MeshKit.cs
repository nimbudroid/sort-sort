using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Editor.Art3D
{
    /// <summary>
    /// Small procedural modeling kit for the baked object art: surfaces of revolution (lathe), swept tubes with shaped
    /// cross-sections, sculpted (radius-function) spheres, rounded boxes and leaves. Everything is smooth-shaded and
    /// generated deterministically, so the same code always bakes the same props.
    /// </summary>
    public sealed class Part
    {
        public string Name;
        public Mesh Mesh;
        public Mat Mat;
        public Part(string name, Mesh mesh, Mat mat) { Name = name; Mesh = mesh; Mat = mat; }
    }

    /// <summary>Material description for the stylized bake shader.</summary>
    public sealed class Mat
    {
        public Color Base = Color.white;
        public Color Shade = new Color(0.5f, 0.35f, 0.3f);   // color in the core shadow (warm, never grey)
        public Color Tint2 = Color.clear;                     // optional second color for a vertical gradient (a = strength)
        public Vector2 GradY = new Vector2(-0.5f, 0.5f);      // object-space height range of the gradient (Tint2 at bottom)
        public Vector3 GradAxis = Vector3.up;                 // object-space direction of the gradient
        public float Spec = 0.25f;     // specular strength
        public float Gloss = 0.4f;     // 0 broad soft highlight → 1 small tight highlight
        public float Rim = 0.15f;      // soft fresnel rim light
        public float Wrap = 0.35f;     // light wrap (soft, subsurface-like falloff for fruit, fabric)
        public float Metal = 0f;       // reflects the warm room gradient (polished metal)
        public float Alpha = 1f;       // < 1 for clear plastic / glass
        public float Noise = 0.03f;    // subtle surface tone variation (organic)
        public float NoiseScale = 9f;

        public Mat Clone() => (Mat)MemberwiseClone();
        public static Mat Of(string hex, Action<Mat> set = null)
        {
            var m = new Mat { Base = H(hex) };
            m.Shade = new Color(m.Base.r * 0.55f, m.Base.g * 0.42f, m.Base.b * 0.42f);
            set?.Invoke(m);
            return m;
        }
        public static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
    }

    public sealed class MeshBuilder
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<int> _t = new List<int>();
        public int Count => _v.Count;

        public int Add(Vector3 p, Vector3 n) { _v.Add(p); _n.Add(n); return _v.Count - 1; }
        public void Tri(int a, int b, int c) { _t.Add(a); _t.Add(b); _t.Add(c); }
        public void Quad(int a, int b, int c, int d) { Tri(a, b, c); Tri(a, c, d); }

        /// <summary>Grid of rows×cols vertices (row-major) connected as quads; wrap closes the columns.</summary>
        public void Grid(int start, int rows, int cols, bool wrap, bool flip = false)
        {
            int cc = wrap ? cols : cols - 1;
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < cc; c++)
                {
                    int c1 = (c + 1) % cols;
                    int a = start + r * cols + c, b = start + r * cols + c1, d = start + (r + 1) * cols + c, e = start + (r + 1) * cols + c1;
                    if (flip) Quad(a, d, e, b); else Quad(a, b, e, d);
                }
        }

        public Mesh ToMesh(bool recalcNormals = true)
        {
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(_v);
            m.SetTriangles(_t, 0);
            if (recalcNormals) m.RecalculateNormals(); else m.SetNormals(_n);
            m.RecalculateBounds();
            return m;
        }
    }

    public static class MeshKit
    {
        // ---------- Surfaces of revolution ----------

        /// <summary>
        /// Lathe around the Y axis. profile: (radius, y) from bottom to top. radiusMod(angle01, t01) multiplies the radius
        /// (ribs, lobes). Ends with radius 0 close naturally; set capBottom/capTop to close open ends with a fan.
        /// </summary>
        public static Mesh Lathe(IList<Vector2> profile, int segments = 64, Func<float, float, float> radiusMod = null, bool capBottom = false, bool capTop = false)
        {
            var b = new MeshBuilder();
            int rows = profile.Count;
            for (int r = 0; r < rows; r++)
            {
                float t = rows > 1 ? r / (float)(rows - 1) : 0;
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments;
                    float ang = a * Mathf.PI * 2f;
                    float rad = profile[r].x * (radiusMod?.Invoke(a, t) ?? 1f);
                    b.Add(new Vector3(Mathf.Cos(ang) * rad, profile[r].y, Mathf.Sin(ang) * rad), Vector3.up);
                }
            }
            b.Grid(0, rows, segments, true, flip: true);
            if (capBottom) Cap(b, 0, segments, profile[0].y, false);
            if (capTop) Cap(b, (rows - 1) * segments, segments, profile[rows - 1].y, true);
            return b.ToMesh();
        }

        static void Cap(MeshBuilder b, int ringStart, int segments, float y, bool top)
        {
            int c = b.Add(new Vector3(0, y, 0), top ? Vector3.up : Vector3.down);
            for (int s = 0; s < segments; s++)
            {
                int s1 = (s + 1) % segments;
                if (top) b.Tri(c, ringStart + s1, ringStart + s); else b.Tri(c, ringStart + s, ringStart + s1);
            }
        }

        /// <summary>Smooth profile helper: samples a rounded polyline (Catmull-Rom) through control points.</summary>
        public static List<Vector2> Smooth(IList<Vector2> pts, int perSeg = 6)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p0 = pts[Mathf.Max(0, i - 1)]; var p1 = pts[i]; var p2 = pts[i + 1]; var p3 = pts[Mathf.Min(pts.Count - 1, i + 2)];
                for (int k = 0; k < perSeg; k++)
                {
                    float t = k / (float)perSeg;
                    o.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t));
                }
            }
            o.Add(pts[pts.Count - 1]);
            return o;
        }

        // ---------- Tubes ----------

        /// <summary>
        /// Sweeps a cross-section along a path. radius(t) sets size, shape(angle, t) multiplies it (flattening, ridges),
        /// squash scales the section's second axis (flat handles). Ends are closed with rounded caps.
        /// </summary>
        public static Mesh Tube(IList<Vector3> path, Func<float, float> radius, int sides = 24, Func<float, float, float> shape = null, float squash = 1f, Vector3? upHint = null, bool caps = true)
        {
            var b = new MeshBuilder();
            int n = path.Count;
            Vector3 up = upHint ?? Vector3.forward;
            var frames = new List<(Vector3 p, Vector3 x, Vector3 y)>();
            for (int i = 0; i < n; i++)
            {
                var tan = (path[Mathf.Min(n - 1, i + 1)] - path[Mathf.Max(0, i - 1)]).normalized;
                var x = Vector3.Cross(tan, up);
                if (x.sqrMagnitude < 1e-6f) x = Vector3.Cross(tan, Vector3.right);
                x.Normalize();
                var y = Vector3.Cross(x, tan).normalized;
                frames.Add((path[i], x, y));
            }
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? i / (float)(n - 1) : 0;
                float r = radius(t);
                for (int s = 0; s < sides; s++)
                {
                    float a = s / (float)sides, ang = a * Mathf.PI * 2f;
                    float k = shape?.Invoke(a, t) ?? 1f;
                    var f = frames[i];
                    b.Add(f.p + (f.x * Mathf.Cos(ang) + f.y * Mathf.Sin(ang) * squash) * r * k, Vector3.up);
                }
            }
            b.Grid(0, n, sides, true, flip: true);
            if (caps)
            {
                int c0 = b.Add(path[0], Vector3.zero), c1 = b.Add(path[n - 1], Vector3.zero);
                for (int s = 0; s < sides; s++)
                {
                    int s1 = (s + 1) % sides;
                    b.Tri(c0, s, s1);
                    b.Tri(c1, (n - 1) * sides + s1, (n - 1) * sides + s);
                }
            }
            return b.ToMesh();
        }

        /// <summary>Points along a cubic Bézier.</summary>
        public static List<Vector3> Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int steps = 32)
        {
            var o = new List<Vector3>();
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1 - t;
                o.Add(u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d);
            }
            return o;
        }

        public static List<Vector3> Line(Vector3 a, Vector3 b, int steps = 12)
        {
            var o = new List<Vector3>();
            for (int i = 0; i <= steps; i++) o.Add(Vector3.Lerp(a, b, i / (float)steps));
            return o;
        }

        // ---------- Sculpted spheres ----------

        /// <summary>
        /// A sphere whose radius is a function of direction (organic forms: fruit, florets). r(dir) receives a unit
        /// vector (y up) and returns the distance from the center.
        /// </summary>
        public static Mesh Sculpt(Func<Vector3, float> r, int lat = 48, int lon = 64)
        {
            var b = new MeshBuilder();
            for (int i = 0; i <= lat; i++)
            {
                float th = i / (float)lat * Mathf.PI; // 0 = top
                for (int j = 0; j < lon; j++)
                {
                    float ph = j / (float)lon * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    b.Add(d * r(d), d);
                }
            }
            b.Grid(0, lat + 1, lon, true);
            return b.ToMesh();
        }

        public static Mesh Sphere(float radius, int lat = 24, int lon = 32) => Sculpt(_ => radius, lat, lon);

        public static Mesh Ellipsoid(Vector3 radii, int lat = 24, int lon = 32)
        {
            var m = Sphere(1f, lat, lon);
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = Vector3.Scale(v[i], radii);
            m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // ---------- Rounded boxes ----------

        /// <summary>A box with rounded edges and corners (bevel radius r), smooth-shaded.</summary>
        public static Mesh RoundedBox(Vector3 size, float r, int res = 10)
        {
            var b = new MeshBuilder();
            var half = size * 0.5f;
            var inner = new Vector3(Mathf.Max(0, half.x - r), Mathf.Max(0, half.y - r), Mathf.Max(0, half.z - r));
            // Six subdivided faces of a unit cube, projected onto the rounded box.
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in normals)
            {
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int start = b.Count;
                for (int i = 0; i <= res; i++)
                    for (int j = 0; j <= res; j++)
                    {
                        // Cluster samples near the edges so the bevels stay smooth.
                        float a = Ease(i / (float)res) * 2 - 1, c = Ease(j / (float)res) * 2 - 1;
                        var p = n + u * a + v * c; // on the unit cube
                        var q = Vector3.Scale(p, half);
                        var clamped = new Vector3(Mathf.Clamp(q.x, -inner.x, inner.x), Mathf.Clamp(q.y, -inner.y, inner.y), Mathf.Clamp(q.z, -inner.z, inner.z));
                        var dir = q - clamped;
                        var pos = dir.sqrMagnitude > 1e-8f ? clamped + dir.normalized * r : q;
                        b.Add(pos, Vector3.up);
                    }
                for (int i = 0; i < res; i++)
                    for (int j = 0; j < res; j++)
                    {
                        int a0 = start + i * (res + 1) + j;
                        b.Quad(a0, a0 + res + 1, a0 + res + 2, a0 + 1);
                    }
            }
            var m = b.ToMesh();
            Weld(m);
            return m;
        }

        static float Ease(float t) => 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI) * 0.6f - (0.5f - t) * 0.4f;

        /// <summary>Merges coincident vertices so smooth normals run across face seams.</summary>
        public static void Weld(Mesh m, float eps = 1e-4f)
        {
            var v = m.vertices; var t = m.triangles;
            var map = new Dictionary<Vector3Int, int>();
            var nv = new List<Vector3>(); var remap = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var key = new Vector3Int(Mathf.RoundToInt(v[i].x / eps), Mathf.RoundToInt(v[i].y / eps), Mathf.RoundToInt(v[i].z / eps));
                if (!map.TryGetValue(key, out int idx)) { idx = nv.Count; nv.Add(v[i]); map[key] = idx; }
                remap[i] = idx;
            }
            for (int i = 0; i < t.Length; i++) t[i] = remap[t[i]];
            m.Clear();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(nv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
        }

        // ---------- Leaves, petals, flat shapes ----------

        /// <summary>
        /// A leaf: an almond outline with thickness, a raised midrib and a gentle curl. Lies along +X from the origin,
        /// facing +Y; transform it into place.
        /// </summary>
        public static Mesh Leaf(float length, float width, float curl = 0.25f, float thickness = 0.02f, int segs = 16, int across = 8)
        {
            var b = new MeshBuilder();
            for (int side = 0; side < 2; side++)
            {
                int start = b.Count;
                for (int i = 0; i <= segs; i++)
                {
                    float t = i / (float)segs;
                    float w = Mathf.Sin(Mathf.Pow(t, 0.8f) * Mathf.PI) * width * 0.5f;
                    for (int j = 0; j <= across; j++)
                    {
                        float s = j / (float)across * 2 - 1;
                        float x = t * length;
                        float z = s * w;
                        float y = curl * (s * s) * width + Mathf.Sin(t * Mathf.PI) * curl * 0.3f * length - t * t * curl * 0.2f * length;
                        y += (side == 0 ? 1 : -1) * thickness * (1 - s * s) * (1 - Mathf.Abs(2 * t - 1));
                        y += 0.012f * length * (1 - Mathf.Abs(s) * 4f > 0 ? 1 - Mathf.Abs(s) * 4f : 0) * (side == 0 ? 1 : 0); // midrib
                        b.Add(new Vector3(x, y, z), Vector3.up);
                    }
                }
                b.Grid(start, segs + 1, across + 1, false, flip: side == 1);
            }
            return b.ToMesh();
        }

        /// <summary>A flat extruded outline (badges, symbols, slices) with softly rounded rims. Outline in the XY plane.</summary>
        public static Mesh Extrude(IList<Vector2> outline, float depth, float bevel = 0.2f, int bevelSteps = 3)
        {
            // Build as stacked rings shrinking at front/back to fake a rounded bevel, then cap both faces with a fan.
            var b = new MeshBuilder();
            var center = Vector2.zero; foreach (var p in outline) center += p; center /= outline.Count;
            // Rings run from the front face (slightly inset) round the bevel, along the side, to the back face.
            var rings = new List<(float z, float k)>();
            float bd = depth * Mathf.Clamp(bevel, 0f, 0.5f), bk = 0.08f * Mathf.Clamp01(bevel * 2f);
            for (int i = 0; i <= bevelSteps; i++) { float a = i / (float)bevelSteps * Mathf.PI * 0.5f; rings.Add((-depth * 0.5f + bd * (1 - Mathf.Cos(a)), 1 - bk * (1 - Mathf.Sin(a)))); }
            for (int i = bevelSteps; i >= 0; i--) { float a = i / (float)bevelSteps * Mathf.PI * 0.5f; rings.Add((depth * 0.5f - bd * (1 - Mathf.Cos(a)), 1 - bk * (1 - Mathf.Sin(a)))); }
            int n = outline.Count;
            foreach (var (z, k) in rings)
                foreach (var p in outline)
                {
                    var q = center + (p - center) * k;
                    b.Add(new Vector3(q.x, q.y, z), Vector3.up);
                }
            b.Grid(0, rings.Count, n, true);
            int c0 = b.Add(new Vector3(center.x, center.y, rings[0].z), Vector3.back);
            int c1 = b.Add(new Vector3(center.x, center.y, rings[rings.Count - 1].z), Vector3.forward);
            int last = (rings.Count - 1) * n;
            for (int i = 0; i < n; i++)
            {
                int i1 = (i + 1) % n;
                b.Tri(c0, i1, i);
                b.Tri(c1, last + i, last + i1);
            }
            return b.ToMesh();
        }

        public static List<Vector2> Circle2(float r, int n = 32, float cx = 0, float cy = 0)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < n; i++) { float a = i / (float)n * Mathf.PI * 2; o.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r)); }
            return o;
        }

        /// <summary>Water/milk drop outline (point at the top).</summary>
        public static List<Vector2> Drop2(float r, int n = 40)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2;
                var p = new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * r;
                float up = Mathf.Clamp01(p.y / r);
                p.x *= 1 - Mathf.Pow(up, 1.4f) * 0.98f;
                p.y += Mathf.Pow(up, 2f) * r * 0.9f;
                o.Add(p);
            }
            return o;
        }

        // ---------- Mesh utilities ----------

        public static Mesh Transform(Mesh m, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var mat = Matrix4x4.TRS(pos, rot, scale);
            var c = UnityEngine.Object.Instantiate(m);
            var v = c.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = mat.MultiplyPoint3x4(v[i]);
            c.vertices = v; c.RecalculateNormals(); c.RecalculateBounds();
            return c;
        }

        public static Mesh Transform(Mesh m, Vector3 pos, Vector3 euler, float scale = 1f) => Transform(m, pos, Quaternion.Euler(euler), Vector3.one * scale);

        /// <summary>Displaces every vertex along its normal by f(position).</summary>
        public static Mesh Displace(Mesh m, Func<Vector3, float> f)
        {
            var v = m.vertices; var n = m.normals;
            for (int i = 0; i < v.Length; i++) v[i] += n[i] * f(v[i]);
            m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        public static Mesh Combine(IEnumerable<Mesh> meshes)
        {
            var list = new List<CombineInstance>();
            foreach (var m in meshes) list.Add(new CombineInstance { mesh = m, transform = Matrix4x4.identity });
            var o = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            o.CombineMeshes(list.ToArray(), true, true);
            o.RecalculateBounds();
            return o;
        }

        // Smooth 3D value noise for organic surface variation.
        public static float Noise3(Vector3 p)
        {
            Vector3Int i = new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
            Vector3 f = p - i; f = new Vector3(f.x * f.x * (3 - 2 * f.x), f.y * f.y * (3 - 2 * f.y), f.z * f.z * (3 - 2 * f.z));
            float H(int x, int y, int z) { unchecked { int n = x * 374761393 + y * 668265263 + z * 2147483647; n = (n ^ (n >> 13)) * 1274126177; return ((n ^ (n >> 16)) & 0xffff) / 65535f; } }
            float L(float a, float b, float t) => a + (b - a) * t;
            return L(L(L(H(i.x, i.y, i.z), H(i.x + 1, i.y, i.z), f.x), L(H(i.x, i.y + 1, i.z), H(i.x + 1, i.y + 1, i.z), f.x), f.y),
                     L(L(H(i.x, i.y, i.z + 1), H(i.x + 1, i.y, i.z + 1), f.x), L(H(i.x, i.y + 1, i.z + 1), H(i.x + 1, i.y + 1, i.z + 1), f.x), f.y), f.z);
        }
    }
}
