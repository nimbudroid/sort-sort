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
        public float feather;           // > 0: soft-edged (gloss shines); canvas units
        public bool shine;              // hand-placed highlight; the Molded renderer drops these (gloss is computed)

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
    public sealed partial class VectorPainter
    {
        public Rgba Ink = Rgba.Hex("2E2433");
        public float OutlineWidth = 0.09f;   // outer silhouette outline (normalised units); heavier for the toy style
        public float LineWidth = 0.035f;     // inner part outlines
        public static float SpecularStrength = 0.42f; // edge highlight on body shapes (visual style)
        public float Gloss = 1f;    // per-object material (ToyShading.PerObject), set by ObjectArt before Rasterize
        public float Volume = 1f;
        public bool Dome;           // per-object: skip the corner test and always dome parts

        readonly List<VShape> shapes = new List<VShape>();

        // Optional transform applied to shapes added after Push (rotation in degrees about a pivot, then offset).
        float rot, pivotX, pivotY, offX, offY;
        bool transformed;

        public int ShapeCount { get { return shapes.Count; } }
        public IReadOnlyList<VShape> Parts { get { return shapes; } }

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
            var s = Ellipse(x, y, rx, ry, new Rgba(1f, 1f, 1f, alpha), rotDeg).Detail().Flat();
            s.shine = true;
            if (ToyShading.Enabled)
            {
                // Toy look: the lighting already places a gloss hotspot, so hand-placed shines become soft sheen.
                s.fill.a = alpha * ToyShading.ShineAlphaScale;
                s.feather = Math.Min(rx, ry) * ToyShading.ShineFeather;
            }
            return s;
        }

        /// <summary>Rasterise to straight-alpha RGBA bytes, row 0 = bottom (Unity texture order).</summary>
        public byte[] Rasterize(int res)
        {
            switch (ToyShading.Mode)
            {
                case ToyRenderMode.Molded: return RasterizeMolded(res);
                case ToyRenderMode.Painted: return RasterizeToy(res);
                default: return RasterizeFlat(res);
            }
        }

        /// <summary>The previous flat "sticker" shading, kept as the ToyShading.Enabled = false fallback.</summary>
        byte[] RasterizeFlat(int res)
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
                            // Toy shading: top-left key light plus a soft bevel (edges darker, middle "puffed up").
                            float h = Clamp01(0.5f + 0.5f * (-x * 0.4f + y * 0.7f));
                            float bevel = 1f;
                            if (s.silhouette) // body shapes only; small decorations stay flat and clean
                            {
                                bevel = Clamp01(-d / 0.22f);
                                bevel = 0.79f + 0.24f * bevel * bevel * (3f - 2f * bevel);
                            }
                            float k = (0.84f + 0.2f * h) * bevel;
                            cr = Math.Min(1f, cr * k); cg = Math.Min(1f, cg * k); cb = Math.Min(1f, cb * k);
                            // Specular: a soft crescent just inside the upper-left edge of body shapes (molded plastic).
                            if (s.silhouette && d > -0.2f && d < -0.03f)
                            {
                                const float e = 0.01f;
                                float gx = s.sdf(x + e, y) - s.sdf(x - e, y), gy = s.sdf(x, y + e) - s.sdf(x, y - e);
                                float gl = (float)Math.Sqrt(gx * gx + gy * gy);
                                if (gl > 1e-5f)
                                {
                                    float facing = (gx * -0.55f + gy * 0.83f) / gl;
                                    float band = Clamp01(1f - Math.Abs(d + 0.1f) / 0.065f);
                                    float spec = SpecularStrength * band * Clamp01((facing - 0.35f) / 0.5f);
                                    cr += (1f - cr) * spec; cg += (1f - cg) * spec; cb += (1f - cb) * spec;
                                }
                            }
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
        /// Toy rendering (ToyShading): every part is treated as an inflated, molded piece. The signed distance to a
        /// part's edge becomes a rounded height profile, giving a surface normal per pixel; the whole object also
        /// domes as one piece, and parts printed onto a body (labels, windows) follow that body's curve with only a
        /// small bevel. The normal is lit by one rig for the whole library: hue-shifted shadow side, warm lit side,
        /// underside and contact occlusion, a cool bounce rim and a cartoon gloss hotspot. Coverage and the
        /// silhouette are computed exactly as before, so sprite alpha and colliders do not change.
        /// </summary>
        byte[] RasterizeToy(int res)
        {
            int n = shapes.Count, count = res * res;
            float px = 2f / res;

            // Pass 1: every shape's distance field on the pixel grid (normals come from grid differences).
            var D = new float[n][];
            for (int i = 0; i < n; i++) D[i] = new float[count];
            var U = new float[count];
            for (int yi = 0; yi < res; yi++)
            {
                float y = (yi + 0.5f) / res * 2f - 1f;
                for (int xi = 0; xi < res; xi++)
                {
                    float x = (xi + 0.5f) / res * 2f - 1f;
                    int k = yi * res + xi;
                    float u = 1e9f;
                    for (int i = 0; i < n; i++)
                    {
                        float d = shapes[i].sdf(x, y);
                        D[i][k] = d;
                        if (shapes[i].silhouette && d < u) u = d;
                    }
                    U[k] = u;
                }
            }

            // Smoothed copies of the fields drive the normals (never coverage), so boxy parts dome as soft pillows
            // instead of showing hard diagonal creases along their medial axis.
            int blur = Math.Max(1, (int)(res * ToyShading.NormalSmoothing + 0.5f));
            var N = new float[n][];
            var tmp = new float[count];
            for (int i = 0; i < n; i++)
            {
                if (!shapes[i].shade || shapes[i].feather > 0f || !HasDepth(D[i], px)) continue;
                // Parts thinner than the blur would only be distorted by it (and it is the costly step on
                // many-part objects such as keyboards), so they use their exact field.
                if (!HasDepth(D[i], blur * px * 2f)) { N[i] = D[i]; continue; }
                N[i] = (float[])D[i].Clone();
                BoxBlur(N[i], tmp, res, blur);
                BoxBlur(N[i], tmp, res, blur);
            }
            var NU = (float[])U.Clone();
            BoxBlur(NU, tmp, res, blur);
            BoxBlur(NU, tmp, res, blur);

            // Pass 2: each part's role and inflation radius.
            float depthU = 0f;
            for (int k = 0; k < count; k++) depthU = Math.Max(depthU, -U[k]);
            float radiusU = Math.Max(px, PartRadius(U, res, depthU, ToyShading.MaxPartRadius * 1.4f, Dome) * Volume);
            var volume = new bool[n];
            var inlay = new bool[n];
            var radius = new float[n];
            var partW = new float[n];
            var bodyW = new float[n];
            for (int i = 0; i < n; i++)
            {
                var s = shapes[i];
                if (N[i] == null) continue;
                float[] di = D[i];
                float depth = 0f;
                for (int k = 0; k < count; k++) depth = Math.Max(depth, -di[k]);
                if (depth <= px) continue; // too small to read as volume
                volume[i] = true;
                if (!s.silhouette)
                {
                    radius[i] = Math.Min(depth, ToyShading.DetailRadius);
                    partW[i] = 1f;
                    bodyW[i] = ToyShading.WholeBodyWeight;
                    continue;
                }
                // Printed-on part: its whole edge lies inside the parts drawn beneath it.
                int edge = 0, covered = 0;
                for (int k = 0; k < count; k++)
                {
                    if (Math.Abs(di[k]) >= px) continue;
                    edge++;
                    for (int j = 0; j < i; j++)
                        if (shapes[j].silhouette && D[j][k] < -0.02f) { covered++; break; }
                }
                inlay[i] = edge > 0 && covered >= edge * 0.85f;
                if (inlay[i])
                {
                    radius[i] = Math.Min(depth, ToyShading.InlayBevel);
                    partW[i] = 0.6f;
                    bodyW[i] = 1f;
                }
                else
                {
                    radius[i] = PartRadius(di, res, depth, ToyShading.MaxPartRadius, Dome) * Volume;
                    partW[i] = 1f;
                    bodyW[i] = ToyShading.WholeBodyWeight * Math.Min(1f, depth / Math.Max(depthU, 1e-4f));
                }
            }

            float lx, ly, lz;
            ToyShading.Light(out lx, out ly, out lz);
            float hx = lx, hy = ly, hz = lz + 1f, hl = (float)Math.Sqrt(hx * hx + hy * hy + hz * hz);
            hx /= hl; hy /= hl; hz /= hl;
            float front = ToyShading.FrontLevel;

            var outBytes = new byte[count * 4];
            var above = new float[n];
            for (int yi = 0; yi < res; yi++)
            {
                for (int xi = 0; xi < res; xi++)
                {
                    int k = yi * res + xi;
                    float ar = 0f, ag = 0f, ab = 0f, aa = 0f; // premultiplied accumulator
                    float union = U[k];
                    float ugx = 0f, ugy = 0f, us = 0f;
                    if (union < 0.5f * px + ToyShading.MaxPartRadius)
                    {
                        Grad(NU, xi, yi, res, out ugx, out ugy);
                        us = Slope(-NU[k] / radiusU);
                    }
                    // above[i] = distance to the nearest raised part drawn on top of part i (contact shadow source).
                    float nearest = 1e9f;
                    for (int i = n - 1; i >= 0; i--)
                    {
                        above[i] = nearest;
                        if (shapes[i].silhouette && !inlay[i] && D[i][k] < nearest) nearest = D[i][k];
                    }
                    for (int i = 0; i < n; i++)
                    {
                        var s = shapes[i];
                        float d = D[i][k];
                        float cov = (s.feather > 0f ? Clamp01(-d / s.feather) : Clamp01(0.5f - d / px)) * s.fill.a;
                        if (cov <= 0f) continue;
                        Rgba f = s.fill;
                        float cr = f.r, cg = f.g, cb = f.b;
                        // Hue-shifted shadow tone of this part (also used for its internal seam lines).
                        float sr = f.r * ToyShading.ShadowR + ToyShading.ShadowLift;
                        float sg = f.g * ToyShading.ShadowG + ToyShading.ShadowLift;
                        float sb = f.b * ToyShading.ShadowB + ToyShading.ShadowLift * 1.6f;
                        float sl = (sr + sg + sb) / 3f, sat = ToyShading.ShadowSaturation;
                        sr = Clamp01(sl + (sr - sl) * sat); sg = Clamp01(sl + (sg - sl) * sat); sb = Clamp01(sl + (sb - sl) * sat);
                        if (volume[i])
                        {
                            float gx, gy;
                            Grad(N[i], xi, yi, res, out gx, out gy);
                            float ps = Slope(-N[i][k] / radius[i]) * partW[i], bs = us * bodyW[i];
                            float nx = ps * gx + bs * ugx, ny = ps * gy + bs * ugy;
                            float il = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 1f);
                            nx *= il; ny *= il;
                            float nz = il;

                            // Soft three-tone ramp: shadow -> base (front-facing) -> warm light.
                            float dif = 0.5f + 0.5f * (nx * lx + ny * ly + nz * lz);
                            if (dif >= front)
                            {
                                float t = Math.Min(1f, (dif - front) / (1f - front)) * ToyShading.LightAmount;
                                cr += (1f - cr) * t; cg += (0.98f - cg) * t; cb += (0.92f - cb) * t;
                            }
                            else
                            {
                                float t = Smooth(ToyShading.ShadowStart, front, dif);
                                cr = sr + (cr - sr) * t; cg = sg + (cg - sg) * t; cb = sb + (cb - sb) * t;
                            }

                            if (s.silhouette)
                            {
                                float occ = 1f;
                                // Underside of the whole object: grounded, sitting on something.
                                if (ugy < 0f) occ -= ToyShading.UndersideAO * (1f - Smooth(0f, 0.16f, -union)) * -ugy;
                                // Parts resting on top of this one cast a soft contact shadow onto it.
                                float dj = above[i];
                                if (dj > 0f && dj < ToyShading.ContactAOWidth)
                                {
                                    float w = 1f - dj / ToyShading.ContactAOWidth;
                                    occ -= ToyShading.ContactAO * w * w;
                                }
                                occ = Math.Max(0.45f, occ);
                                cr *= occ; cg *= occ; cb *= occ;
                            }

                            // Cool bounce light along the lower-right rim.
                            float rl = (float)Math.Sqrt(nx * nx + ny * ny);
                            if (rl > 1e-4f)
                            {
                                // Bounce lifts the part's own colour (slightly cooled) so yellows and greens never go muddy.
                                float b = ToyShading.BounceLight * (1f - nz) * Clamp01((nx * 0.55f - ny * 0.83f) / rl);
                                float br = f.r + (1f - f.r) * 0.35f, bg = f.g + (1f - f.g) * 0.35f, bb = f.b + (1f - f.b) * 0.45f;
                                cr += (br - cr) * b; cg += (bg - cg) * b; cb += (bb - cb) * b;
                            }

                            // Gloss: crisp cartoon hotspot + broad sheen, both following the surface.
                            float nh = Math.Max(0f, nx * hx + ny * hy + nz * hz);
                            float nh2 = nh * nh, nh4 = nh2 * nh2, nh8 = nh4 * nh4;
                            float spec = Gloss * (ToyShading.SpecularHotspot * Smooth(ToyShading.SpecularSize - 0.02f, ToyShading.SpecularSize, nh)
                                                  + ToyShading.SpecularSheen * nh8 * nh4);
                            spec = Clamp01(spec);
                            cr += (1f - cr) * spec; cg += (1f - cg) * spec; cb += (1f - cb) * spec;
                        }
                        if (s.outline)
                        {
                            // Internal seams: medium weight, tinted by the part's own shadow colour.
                            float t = Clamp01((d + LineWidth) / px + 0.5f);
                            float m = ToyShading.InnerLineInk;
                            float lr = sr + (Ink.r - sr) * m, lg = sg + (Ink.g - sg) * m, lb = sb + (Ink.b - sb) * m;
                            cr += (lr - cr) * t; cg += (lg - cg) * t; cb += (lb - cb) * t;
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
                    int o = k * 4;
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
        /// Inflation radius for a part. Smooth parts (circles, ellipses, blobs) dome fully; parts with corners (boxes,
        /// slices, stars) get flat faces and a rounded bevel, like molded plastic, instead of a creased pillow.
        /// Corner detection: area relative to an ellipse with the same depth and proportions (1.0 for an ellipse,
        /// 1.27 for any rectangle, higher for triangles and stars), proportions from the part's second moments.
        /// </summary>
        static float PartRadius(float[] d, int res, float depth, float maxRadius, bool forceDome)
        {
            float dome = Math.Min(depth, maxRadius);
            if (depth <= 0f || forceDome) return dome;
            float ratio = CornerRatio(d, res, depth);
            if (ratio <= 0f) return dome;
            float bevel = Math.Min(depth, ToyShading.BoxBevelRadius);
            float t = Smooth(ToyShading.RoundRatio, ToyShading.BoxRatio, ratio);
            return dome + (bevel - dome) * t;
        }

        /// <summary>Part area relative to an ellipse of the same depth and proportions; 0 if too small to measure.</summary>
        static float CornerRatio(float[] d, int res, float depth)
        {
            double m = 0, sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0;
            for (int yi = 0; yi < res; yi++)
                for (int xi = 0; xi < res; xi++)
                {
                    if (d[yi * res + xi] >= 0f) continue;
                    double x = xi, y = yi;
                    m++; sx += x; sy += y; sxx += x * x; syy += y * y; sxy += x * y;
                }
            if (m < 4) return 0f;
            double cx = sx / m, cy = sy / m;
            double vxx = sxx / m - cx * cx, vyy = syy / m - cy * cy, vxy = sxy / m - cx * cy;
            double tr = (vxx + vyy) * 0.5, disc = Math.Sqrt(Math.Max(0.0, (vxx - vyy) * (vxx - vyy) * 0.25 + vxy * vxy));
            double aspect = Math.Sqrt((tr + disc) / Math.Max(tr - disc, 1e-6));
            double area = m * (2.0 / res) * (2.0 / res);
            return (float)(area / (Math.PI * depth * depth * aspect));
        }

        static bool HasDepth(float[] d, float px)
        {
            for (int k = 0; k < d.Length; k++) if (d[k] < -px) return true;
            return false;
        }

        // Separable box blur in place (sliding window, clamped edges).
        static void BoxBlur(float[] f, float[] tmp, int res, int r)
        {
            float inv = 1f / (2 * r + 1);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int line = 0; line < res; line++)
                {
                    float sum = 0f;
                    for (int t = -r; t <= r; t++) sum += At(f, pass, line, Math.Min(res - 1, Math.Max(0, t)), res);
                    for (int t = 0; t < res; t++)
                    {
                        tmp[pass == 0 ? line * res + t : t * res + line] = sum * inv;
                        sum += At(f, pass, line, Math.Min(res - 1, t + r + 1), res) - At(f, pass, line, Math.Max(0, t - r), res);
                    }
                }
                Array.Copy(tmp, f, f.Length);
            }
        }

        static float At(float[] f, int pass, int line, int t, int res) { return pass == 0 ? f[line * res + t] : f[t * res + line]; }

        // Unit direction of a distance field's gradient on the pixel grid (points out of the shape).
        static void Grad(float[] f, int xi, int yi, int res, out float gx, out float gy)
        {
            int k = yi * res + xi;
            gx = f[xi < res - 1 ? k + 1 : k] - f[xi > 0 ? k - 1 : k];
            gy = f[yi < res - 1 ? k + res : k] - f[yi > 0 ? k - res : k];
            float l = (float)Math.Sqrt(gx * gx + gy * gy);
            if (l > 1e-6f) { gx /= l; gy /= l; } else { gx = 0f; gy = 0f; }
        }

        // Edge tilt of a rounded (quarter-circle) profile at normalised depth t (0 = edge, 1 = top).
        static float Slope(float t)
        {
            t = Clamp01(t);
            float q = 1f - t;
            return Math.Min(ToyShading.MaxSlope, q / (float)Math.Sqrt(Math.Max(1f - q * q, 1e-4f)));
        }

        static float Smooth(float a, float b, float v)
        {
            float t = Clamp01((v - a) / (b - a));
            return t * t * (3f - 2f * t);
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
