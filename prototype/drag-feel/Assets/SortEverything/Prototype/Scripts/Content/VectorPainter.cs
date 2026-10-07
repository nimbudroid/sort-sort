using System;
using System.Collections.Generic;

namespace SortEverything.Prototype
{
    // A tiny signed-distance vector painter for prototype object art. Plain C# (no UnityEngine) so drawings can be
    // rendered and checked outside Unity. Coordinates are normalised: the canvas spans -1..1 on both axes, y up.

    public struct Rgba
    {
        public float r, g, b, a;

        public Rgba(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static Rgba Hex(string hex, float a = 1f)
        {
            int v = Convert.ToInt32(hex.TrimStart('#'), 16);
            return new Rgba(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, a);
        }

        public Rgba Dark(float k) { return new Rgba(r * (1f - k), g * (1f - k), b * (1f - k), a); }
        public Rgba Light(float k) { return new Rgba(r + (1f - r) * k, g + (1f - g) * k, b + (1f - b) * k, a); }
        public Rgba WithAlpha(float na) { return new Rgba(r, g, b, na); }

        public static Rgba Lerp(Rgba x, Rgba y, float t)
        {
            return new Rgba(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t);
        }
    }

    public delegate float Sdf(float x, float y);

    public sealed class VShape
    {
        public Sdf sdf;
        public Rgba fill;
        public bool outline = true;     // thin ink line just inside this shape's edge
        public bool silhouette = true;  // contributes to the thick outer outline and the collider hull
        public bool shade = true;       // toy-style top-left lighting

        /// <summary>Carve another shape out of this one.</summary>
        public VShape Minus(Sdf cut)
        {
            Sdf a = sdf;
            sdf = (x, y) => Math.Max(a(x, y), -cut(x, y));
            return this;
        }

        /// <summary>Keep only the part of this shape inside another.</summary>
        public VShape Clip(Sdf keep)
        {
            Sdf a = sdf;
            sdf = (x, y) => Math.Max(a(x, y), keep(x, y));
            return this;
        }

        public VShape NoLine() { outline = false; return this; }

        /// <summary>Decoration: no outline, not part of the silhouette or collider.</summary>
        public VShape Detail() { outline = false; silhouette = false; return this; }

        public VShape Flat() { shade = false; return this; }
    }

    public static class Sd
    {
        public static float Len(float x, float y) { return (float)Math.Sqrt(x * x + y * y); }

        public static Sdf Circle(float cx, float cy, float r) { return (x, y) => Len(x - cx, y - cy) - r; }

        public static Sdf Ellipse(float cx, float cy, float rx, float ry, float rotDeg = 0f)
        {
            float c = (float)Math.Cos(rotDeg * Math.PI / 180.0), s = (float)Math.Sin(rotDeg * Math.PI / 180.0);
            float m = Math.Min(rx, ry);
            return (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float lx = dx * c + dy * s, ly = -dx * s + dy * c;
                return (Len(lx / rx, ly / ry) - 1f) * m;
            };
        }

        /// <summary>Rounded box: centre, half extents, corner radius, rotation.</summary>
        public static Sdf Box(float cx, float cy, float hw, float hh, float radius = 0f, float rotDeg = 0f)
        {
            float c = (float)Math.Cos(rotDeg * Math.PI / 180.0), s = (float)Math.Sin(rotDeg * Math.PI / 180.0);
            radius = Math.Min(radius, Math.Min(hw, hh));
            return (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float lx = dx * c + dy * s, ly = -dx * s + dy * c;
                float qx = Math.Abs(lx) - (hw - radius), qy = Math.Abs(ly) - (hh - radius);
                return Len(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - radius;
            };
        }

        public static float SegmentDistance(float x, float y, float x1, float y1, float x2, float y2)
        {
            float ex = x2 - x1, ey = y2 - y1, wx = x - x1, wy = y - y1;
            float ee = ex * ex + ey * ey;
            float t = ee > 0f ? Math.Max(0f, Math.Min(1f, (wx * ex + wy * ey) / ee)) : 0f;
            return Len(wx - ex * t, wy - ey * t);
        }

        public static Sdf Capsule(float x1, float y1, float x2, float y2, float r)
        {
            return (x, y) => SegmentDistance(x, y, x1, y1, x2, y2) - r;
        }

        /// <summary>Capsule tapering from r1 at the first point to r2 at the second (approximate).</summary>
        public static Sdf Taper(float x1, float y1, float x2, float y2, float r1, float r2)
        {
            return (x, y) =>
            {
                float ex = x2 - x1, ey = y2 - y1, wx = x - x1, wy = y - y1;
                float ee = ex * ex + ey * ey;
                float t = Math.Max(0f, Math.Min(1f, (wx * ex + wy * ey) / ee));
                return Len(wx - ex * t, wy - ey * t) - (r1 + (r2 - r1) * t);
            };
        }

        public static Sdf Ring(float cx, float cy, float r, float thickness)
        {
            return (x, y) => Math.Abs(Len(x - cx, y - cy) - r) - thickness;
        }

        /// <summary>Outline of a capsule (paper clips, hangers).</summary>
        public static Sdf CapsuleRing(float x1, float y1, float x2, float y2, float r, float thickness)
        {
            return (x, y) => Math.Abs(SegmentDistance(x, y, x1, y1, x2, y2) - r) - thickness;
        }

        /// <summary>Polygon from x,y pairs, optionally rounded.</summary>
        public static Sdf Poly(float round, params float[] xy)
        {
            int n = xy.Length / 2;
            var vx = new float[n];
            var vy = new float[n];
            for (int i = 0; i < n; i++) { vx[i] = xy[i * 2]; vy[i] = xy[i * 2 + 1]; }
            return (x, y) =>
            {
                float d = (x - vx[0]) * (x - vx[0]) + (y - vy[0]) * (y - vy[0]);
                float s = 1f;
                for (int i = 0, j = n - 1; i < n; j = i, i++)
                {
                    float ex = vx[j] - vx[i], ey = vy[j] - vy[i];
                    float wx = x - vx[i], wy = y - vy[i];
                    float t = Math.Max(0f, Math.Min(1f, (wx * ex + wy * ey) / (ex * ex + ey * ey)));
                    float bx = wx - ex * t, by = wy - ey * t;
                    d = Math.Min(d, bx * bx + by * by);
                    bool c1 = y >= vy[i], c2 = y < vy[j], c3 = ex * wy > ey * wx;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * (float)Math.Sqrt(d) - round;
            };
        }

        /// <summary>Regular star with n points.</summary>
        public static Sdf Star(float cx, float cy, float rOuter, float rInner, int points, float round = 0f, float rotDeg = 0f)
        {
            var xy = new float[points * 4];
            for (int i = 0; i < points * 2; i++)
            {
                float r = i % 2 == 0 ? rOuter : rInner;
                double a = Math.PI / 2 + rotDeg * Math.PI / 180.0 + i * Math.PI / points;
                xy[i * 2] = cx + (float)Math.Cos(a) * r;
                xy[i * 2 + 1] = cy + (float)Math.Sin(a) * r;
            }
            return Poly(round, xy);
        }

        /// <summary>Half-plane: inside where y &lt; y0 (below) or above when below=false.</summary>
        public static Sdf HalfPlaneY(float y0, bool below) { return (x, y) => below ? y - y0 : y0 - y; }

        public static Sdf HalfPlaneX(float x0, bool left) { return (x, y) => left ? x - x0 : x0 - x; }

        public static Sdf Union(Sdf a, Sdf b) { return (x, y) => Math.Min(a(x, y), b(x, y)); }
    }

    /// <summary>Collects shapes (back to front) and rasterises them with anti-aliased sticker outlines.</summary>
    public sealed class VectorPainter
    {
        public Rgba Ink = Rgba.Hex("2E2433");
        public float OutlineWidth = 0.075f;  // outer silhouette outline (normalised units)
        public float LineWidth = 0.035f;     // inner part outlines

        readonly List<VShape> shapes = new List<VShape>();

        // Optional transform applied to shapes added after Push (rotation in degrees about a pivot, then offset).
        float rot, pivotX, pivotY, offX, offY;
        bool transformed;

        public int ShapeCount { get { return shapes.Count; } }

        public void Push(float rotDeg, float pivotX = 0f, float pivotY = 0f, float offsetX = 0f, float offsetY = 0f)
        {
            rot = rotDeg; this.pivotX = pivotX; this.pivotY = pivotY; offX = offsetX; offY = offsetY;
            transformed = true;
        }

        public void Pop() { transformed = false; }

        public VShape Add(Sdf sdf, Rgba fill)
        {
            if (transformed)
            {
                float c = (float)Math.Cos(-rot * Math.PI / 180.0), s = (float)Math.Sin(-rot * Math.PI / 180.0);
                float px = pivotX, py = pivotY, ox = offX, oy = offY;
                Sdf inner = sdf;
                sdf = (x, y) =>
                {
                    float dx = x - ox - px, dy = y - oy - py;
                    return inner(dx * c - dy * s + px, dx * s + dy * c + py);
                };
            }
            var shape = new VShape { sdf = sdf, fill = fill };
            shapes.Add(shape);
            return shape;
        }

        public VShape Circle(float x, float y, float r, Rgba c) { return Add(Sd.Circle(x, y, r), c); }
        public VShape Ellipse(float x, float y, float rx, float ry, Rgba c, float rot = 0f) { return Add(Sd.Ellipse(x, y, rx, ry, rot), c); }
        public VShape Box(float x, float y, float hw, float hh, Rgba c, float radius = 0f, float rot = 0f) { return Add(Sd.Box(x, y, hw, hh, radius, rot), c); }
        public VShape Capsule(float x1, float y1, float x2, float y2, float r, Rgba c) { return Add(Sd.Capsule(x1, y1, x2, y2, r), c); }
        public VShape Taper(float x1, float y1, float x2, float y2, float r1, float r2, Rgba c) { return Add(Sd.Taper(x1, y1, x2, y2, r1, r2), c); }
        public VShape Ring(float x, float y, float r, float thickness, Rgba c) { return Add(Sd.Ring(x, y, r, thickness), c); }
        public VShape Poly(Rgba c, float round, params float[] xy) { return Add(Sd.Poly(round, xy), c); }
        public VShape Star(float x, float y, float ro, float ri, int n, Rgba c, float round = 0f, float rot = 0f) { return Add(Sd.Star(x, y, ro, ri, n, round, rot), c); }

        /// <summary>Ink stroke for details (seams, text lines, whiskers).</summary>
        public VShape Line(float x1, float y1, float x2, float y2, float width, Rgba? color = null)
        {
            return Capsule(x1, y1, x2, y2, width * 0.5f, color ?? Ink).Detail().Flat();
        }

        public VShape Dot(float x, float y, float r, Rgba c) { return Circle(x, y, r, c).Detail().Flat(); }

        /// <summary>Soft white highlight.</summary>
        public VShape Shine(float x, float y, float rx, float ry, float rotDeg = 0f, float alpha = 0.55f)
        {
            return Ellipse(x, y, rx, ry, new Rgba(1f, 1f, 1f, alpha), rotDeg).Detail().Flat();
        }

        /// <summary>Rasterise to straight-alpha RGBA bytes, row 0 = bottom (Unity texture order).</summary>
        public byte[] Rasterize(int res)
        {
            var outBytes = new byte[res * res * 4];
            float px = 2f / res;
            int n = shapes.Count;
            var dist = new float[n];
            for (int yi = 0; yi < res; yi++)
            {
                float y = (yi + 0.5f) / res * 2f - 1f;
                for (int xi = 0; xi < res; xi++)
                {
                    float x = (xi + 0.5f) / res * 2f - 1f;
                    float ar = 0f, ag = 0f, ab = 0f, aa = 0f; // premultiplied accumulator
                    float union = 1e9f;
                    for (int i = 0; i < n; i++)
                    {
                        var s = shapes[i];
                        float d = s.sdf(x, y);
                        dist[i] = d;
                        if (s.silhouette && d < union) union = d;
                        float cov = Clamp01(0.5f - d / px) * s.fill.a;
                        if (cov <= 0f) continue;
                        float cr = s.fill.r, cg = s.fill.g, cb = s.fill.b;
                        if (s.shade)
                        {
                            float h = Clamp01(0.5f + 0.5f * (-x * 0.4f + y * 0.7f));
                            float k = 0.86f + 0.16f * h;
                            cr *= k; cg *= k; cb *= k;
                        }
                        if (s.outline)
                        {
                            float t = Clamp01((d + LineWidth) / px + 0.5f);
                            cr += (Ink.r - cr) * t; cg += (Ink.g - cg) * t; cb += (Ink.b - cb) * t;
                        }
                        ar = cr * cov + ar * (1f - cov);
                        ag = cg * cov + ag * (1f - cov);
                        ab = cb * cov + ab * (1f - cov);
                        aa = cov + aa * (1f - cov);
                    }
                    if (aa > 0f && union < 1e8f)
                    {
                        float t = Clamp01((union + OutlineWidth) / px + 0.5f);
                        ar += (Ink.r * aa - ar) * t; ag += (Ink.g * aa - ag) * t; ab += (Ink.b * aa - ab) * t;
                    }
                    int o = (yi * res + xi) * 4;
                    if (aa <= 0.001f) { outBytes[o] = outBytes[o + 1] = outBytes[o + 2] = 255; outBytes[o + 3] = 0; continue; }
                    outBytes[o] = ToByte(ar / aa);
                    outBytes[o + 1] = ToByte(ag / aa);
                    outBytes[o + 2] = ToByte(ab / aa);
                    outBytes[o + 3] = ToByte(aa);
                }
            }
            return outBytes;
        }

        /// <summary>
        /// Coverage of the silhouette shapes only (decorations like steam or crumbs excluded), in the same
        /// RGBA layout as Rasterize so it can be passed to ConvexHull.
        /// </summary>
        public byte[] RasterizeSilhouette(int res)
        {
            var outBytes = new byte[res * res * 4];
            for (int yi = 0; yi < res; yi++)
            {
                float y = (yi + 0.5f) / res * 2f - 1f;
                for (int xi = 0; xi < res; xi++)
                {
                    float x = (xi + 0.5f) / res * 2f - 1f;
                    float union = 1e9f;
                    for (int i = 0; i < shapes.Count; i++)
                    {
                        var s = shapes[i];
                        if (!s.silhouette) continue;
                        float d = s.sdf(x, y);
                        if (d < union) union = d;
                    }
                    outBytes[(yi * res + xi) * 4 + 3] = union < 0f ? (byte)255 : (byte)0;
                }
            }
            return outBytes;
        }

        /// <summary>
        /// Convex hull of the opaque pixels, simplified to at most maxPoints, in sprite-local units (-0.5..0.5).
        /// Used as the physics collider: convex shapes stack stably and keep pickup forgiving.
        /// </summary>
        public static float[] ConvexHull(byte[] rgba, int res, int maxPoints = 12)
        {
            var pts = new List<float[]>();
            for (int y = 0; y < res; y++)
            {
                int minX = -1, maxX = -1;
                for (int x = 0; x < res; x++)
                {
                    if (rgba[(y * res + x) * 4 + 3] < 128) continue;
                    if (minX < 0) minX = x;
                    maxX = x;
                }
                if (minX < 0) continue;
                pts.Add(new float[] { minX, y }); pts.Add(new float[] { minX, y + 1 });
                pts.Add(new float[] { maxX + 1, y }); pts.Add(new float[] { maxX + 1, y + 1 });
            }
            if (pts.Count < 3) return new float[] { -0.4f, -0.4f, 0.4f, -0.4f, 0.4f, 0.4f, -0.4f, 0.4f };

            pts.Sort((a, b) => a[0] != b[0] ? a[0].CompareTo(b[0]) : a[1].CompareTo(b[1]));
            var hull = new List<float[]>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = hull.Count;
                for (int k = 0; k < pts.Count; k++)
                {
                    var p = pass == 0 ? pts[k] : pts[pts.Count - 1 - k];
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0f)
                        hull.RemoveAt(hull.Count - 1);
                    hull.Add(p);
                }
                hull.RemoveAt(hull.Count - 1);
            }

            // Drop the vertex that removes the least area until small enough.
            while (hull.Count > maxPoints)
            {
                int best = 0;
                float bestArea = float.MaxValue;
                for (int i = 0; i < hull.Count; i++)
                {
                    var a = hull[(i + hull.Count - 1) % hull.Count];
                    var c = hull[(i + 1) % hull.Count];
                    float area = Math.Abs(Cross(a, hull[i], c));
                    if (area < bestArea) { bestArea = area; best = i; }
                }
                hull.RemoveAt(best);
            }

            var result = new float[hull.Count * 2];
            for (int i = 0; i < hull.Count; i++)
            {
                result[i * 2] = hull[i][0] / res - 0.5f;
                result[i * 2 + 1] = hull[i][1] / res - 0.5f;
            }
            return result;
        }

        static float Cross(float[] o, float[] a, float[] b)
        {
            return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
        }

        static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
        static byte ToByte(float v) { return (byte)(Clamp01(v) * 255f + 0.5f); }
    }
}
