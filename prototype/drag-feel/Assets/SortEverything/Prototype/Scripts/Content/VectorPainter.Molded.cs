using System;
using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Molded renderer (ToyShading.Mode = Molded): shading computed from a per-part shape model.
    ///
    /// Each drawing is an ordered list of parts (body, leaf, cap, label, frosting...). Per raised part:
    ///   1. Face and side wall: the part's face is the part shifted up (toward the viewer's eye line, ToyShading.WallDir)
    ///      and clipped to the part; the band left along the bottom is the side wall, so every piece has thickness.
    ///   2. Height map: inner distance from the face edge through the family's profile curve (dome / bevel + flat).
    ///   3. Normals from that height map; the side wall faces outward and away from the viewer.
    ///   4. One light rig for the library: key upper-left front, soft fill lower-right, low ambient, banded tones.
    ///   5. Stacking: later parts sit higher and cast soft, light-offset occlusion onto earlier parts.
    ///   6. Specular from the same normals (sheen + hot accent); hand-placed Shine() shapes are dropped.
    ///   7. Contour hierarchy: outer ink contour > bevel line between face and side > light internal seams.
    /// Printed parts (labels, flat icons, strokes, inlays) take the normal of the surface beneath them, so they
    /// follow its curve. Coverage and the silhouette are unchanged: all shading stays inside the drawn shapes.
    /// The contact shadow is a separate baked sprite (ToyShading.SoftShadow), not part of this raster.
    /// </summary>
    public sealed partial class VectorPainter
    {
        /// <summary>Material family of the object (set by ObjectArt from ObjectMaterials before baking).</summary>
        public ToyFamily Family = ToyFamily.Plastic;

        /// <summary>Optional per-part family overrides, keyed by draw-order index.</summary>
        public Dictionary<int, ToyFamily> PartFamilies;

        enum PartKind { Skip, Printed, Bump, Raised }

        byte[] RasterizeMolded(int res)
        {
            int n = shapes.Count, count = res * res;
            float px = 2f / res;

            // Distance field of every part, and of the whole silhouette.
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

            float lx, ly, lz;
            ToyShading.Light(out lx, out ly, out lz);
            float lxy = (float)Math.Sqrt(lx * lx + ly * ly);
            float dirX = lx / lxy, dirY = ly / lxy; // toward the key light, in the picture plane (cast shadows)
            float wdl = (float)Math.Sqrt(ToyShading.WallDirX * ToyShading.WallDirX + ToyShading.WallDirY * ToyShading.WallDirY);
            float wallX = ToyShading.WallDirX / wdl, wallY = ToyShading.WallDirY / wdl; // face shift (wall shows opposite)
            int blur = Math.Max(1, (int)(res * ToyShading.NormalSmoothing + 0.5f));
            var tmp = new float[count];

            // Bounding box of each part's interior (pixels), to keep part-vs-part tests cheap.
            var bx0 = new int[n]; var by0 = new int[n]; var bx1 = new int[n]; var by1 = new int[n];
            for (int i = 0; i < n; i++)
            {
                bx0[i] = res; by0[i] = res; bx1[i] = -1; by1[i] = -1;
                float[] di = D[i];
                for (int yi = 0; yi < res; yi++)
                    for (int xi = 0; xi < res; xi++)
                        if (di[yi * res + xi] < 0f)
                        {
                            if (xi < bx0[i]) bx0[i] = xi;
                            if (xi > bx1[i]) bx1[i] = xi;
                            if (yi < by0[i]) by0[i] = yi;
                            if (yi > by1[i]) by1[i] = yi;
                        }
            }

            // Classify parts and build their shape model.
            var kind = new PartKind[n];
            var look = new ToyShading.FamilyLook[n];
            var depth = new float[n];
            var radius = new float[n];
            var wall = new float[n];        // side wall thickness (canvas units)
            var offX = new float[n];        // face shift toward the light (pixels)
            var offY = new float[n];
            var N = new float[n][];         // smoothed field used for normals
            for (int i = 0; i < n; i++)
            {
                var s = shapes[i];
                float dep = 0f;
                float[] di = D[i];
                for (int k = 0; k < count; k++) dep = Math.Max(dep, -di[k]);
                depth[i] = dep;
                ToyFamily fam = Family, partFam;
                if (PartFamilies != null && PartFamilies.TryGetValue(i, out partFam)) fam = partFam;
                look[i] = ToyShading.Look(fam);
                N[i] = di;

                if (s.shine) { kind[i] = PartKind.Skip; continue; }
                if (dep <= px || !s.shade) { kind[i] = PartKind.Printed; continue; }
                if (!s.silhouette) { kind[i] = PartKind.Bump; radius[i] = Math.Min(dep, ToyShading.DetailRadius); continue; }

                // Printed onto the parts beneath it (label, window): its whole edge lies inside them.
                int edge = 0, covered = 0;
                for (int k = 0; k < count; k++)
                {
                    if (Math.Abs(di[k]) >= px) continue;
                    edge++;
                    for (int j = 0; j < i; j++)
                        if (shapes[j].silhouette && D[j][k] < -0.02f) { covered++; break; }
                }
                if (edge > 0 && covered >= edge * 0.85f) { kind[i] = PartKind.Printed; continue; }

                kind[i] = PartKind.Raised;
                var lk = look[i];
                float t = Math.Min(lk.side * dep, lk.sideMax);
                if (t >= px) t = WallClearance(D, i, t, wallX, wallY, px, res, bx0, by0, bx1, by1);
                if (t < px) t = 0f;
                wall[i] = t;
                offX[i] = wallX * t / px;
                offY[i] = wallY * t / px;
                float faceDepth = Math.Max(px, dep - t * 0.5f);
                if (lk.bevel >= 0f)
                    radius[i] = faceDepth < ToyShading.ThinPart ? faceDepth : Math.Min(faceDepth, lk.bevel);
                else
                {
                    float corner = 0f;
                    if (!Dome)
                    {
                        float ratio = CornerRatio(di, res, dep);
                        if (ratio > 0f) corner = Smooth(ToyShading.RoundRatio, ToyShading.BoxRatio, ratio);
                    }
                    float bevel = Math.Min(faceDepth, ToyShading.CornerBevel);
                    radius[i] = faceDepth + (bevel - faceDepth) * corner;
                }
                radius[i] = Math.Max(px, radius[i]);
                if (dep >= blur * px * 2f)
                {
                    N[i] = (float[])di.Clone();
                    BoxBlur(N[i], tmp, res, blur);
                    BoxBlur(N[i], tmp, res, blur);
                }
            }

            var NU = (float[])U.Clone();
            BoxBlur(NU, tmp, res, blur);
            BoxBlur(NU, tmp, res, blur);

            // Light rig constants.
            float fx = ToyShading.FillX, fy = ToyShading.FillY, fz = ToyShading.FillZ;
            float fl = (float)Math.Sqrt(fx * fx + fy * fy + fz * fz);
            fx /= fl; fy /= fl; fz /= fl;
            float hx = lx, hy = ly, hz = lz + 1f, hl = (float)Math.Sqrt(hx * hx + hy * hy + hz * hz);
            hx /= hl; hy /= hl; hz /= hl;
            var rig = new Rig
            {
                lx = lx, ly = ly, lz = lz, fx = fx, fy = fy, fz = fz, hx = hx, hy = hy, hz = hz,
                lo = ToyShading.Ambient,
                front = ToyShading.Ambient + ToyShading.KeyStrength * lz + ToyShading.FillStrength * Math.Max(0f, fz),
                hi = ToyShading.Ambient + ToyShading.KeyStrength + ToyShading.FillStrength * Math.Max(0f, lx * fx + ly * fy + lz * fz),
                gloss = Gloss,
            };

            int castX = (int)Math.Round(dirX * ToyShading.CastOffset / px), castY = (int)Math.Round(dirY * ToyShading.CastOffset / px);
            var aboveCast = new float[n];
            var aboveNear = new float[n];
            var outBytes = new byte[count * 4];

            for (int yi = 0; yi < res; yi++)
            {
                for (int xi = 0; xi < res; xi++)
                {
                    int k = yi * res + xi;
                    float union = U[k];
                    if (union > px) { outBytes[k * 4] = outBytes[k * 4 + 1] = outBytes[k * 4 + 2] = 255; outBytes[k * 4 + 3] = 0; if (!AnyDetailCovers(D, k, px)) continue; }

                    // Occluders above each part: raised parts drawn later, sampled toward the light (cast) and in place.
                    int kc = Math.Min(res - 1, Math.Max(0, yi + castY)) * res + Math.Min(res - 1, Math.Max(0, xi + castX));
                    float nearC = 1e9f, nearP = 1e9f;
                    for (int i = n - 1; i >= 0; i--)
                    {
                        aboveCast[i] = nearC;
                        aboveNear[i] = nearP;
                        if (kind[i] != PartKind.Raised) continue;
                        if (D[i][kc] < nearC) nearC = D[i][kc];
                        if (D[i][k] < nearP) nearP = D[i][k];
                    }

                    // Underside of the whole object.
                    float ground = 0f;
                    if (union < 0.2f)
                    {
                        float ugx, ugy;
                        Grad(NU, xi, yi, res, out ugx, out ugy);
                        if (ugy < 0f) ground = ToyShading.GroundAO * (1f - Smooth(0f, 0.16f, -union)) * -ugy;
                    }

                    // Surface beneath the current part (printed parts follow it).
                    float bnx = 0f, bny = 0f, bnz = 1f;
                    ToyShading.FamilyLook below = look.Length > 0 ? look[0] : ToyShading.Plastic;

                    float ar = 0f, ag = 0f, ab = 0f, aa = 0f; // premultiplied accumulator
                    for (int i = 0; i < n; i++)
                    {
                        if (kind[i] == PartKind.Skip) continue;
                        var s = shapes[i];
                        float d = D[i][k];
                        float cov = Clamp01(0.5f - d / px) * s.fill.a;
                        if (cov <= 0f) continue;

                        Rgba f = s.fill;
                        float sr, sg, sb;
                        ShadowTone(f, out sr, out sg, out sb);
                        float cr, cg, cb;
                        var lk = look[i];

                        if (kind[i] == PartKind.Raised)
                        {
                            // Face: field of (part ∩ part shifted toward the light), through the family profile.
                            float fc = FaceField(N[i], xi, yi, res, offX[i], offY[i]);
                            float gx = FaceField(N[i], xi + 1, yi, res, offX[i], offY[i]) - FaceField(N[i], xi - 1, yi, res, offX[i], offY[i]);
                            float gy = FaceField(N[i], xi, yi + 1, res, offX[i], offY[i]) - FaceField(N[i], xi, yi - 1, res, offX[i], offY[i]);
                            float gl = (float)Math.Sqrt(gx * gx + gy * gy);
                            if (gl > 1e-6f) { gx /= gl; gy /= gl; } else { gx = 0f; gy = 0f; }
                            float slope = Slope(-fc / radius[i]) * lk.slope;
                            float nx = slope * gx, ny = slope * gy, il = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 1f);
                            nx *= il; ny *= il;
                            float nz = il;
                            Light(ref rig, f, sr, sg, sb, nx, ny, nz, lk, out cr, out cg, out cb);

                            float faceW = 1f;
                            if (wall[i] > 0f)
                            {
                                float fdExact = Math.Max(d, SampleField(D[i], xi - offX[i], yi - offY[i], res));
                                faceW = Clamp01(0.5f - fdExact / px);
                                if (faceW < 1f)
                                {
                                    // Side wall: faces outward and away from the viewer, darkening toward its outer edge.
                                    float wx, wy;
                                    Grad(N[i], xi, yi, res, out wx, out wy);
                                    float sz = ToyShading.SideNormalZ, wl = 1f / (float)Math.Sqrt(wx * wx + wy * wy + sz * sz);
                                    float wr, wg, wb;
                                    Light(ref rig, f, sr, sg, sb, wx * wl, wy * wl, sz * wl, lk, out wr, out wg, out wb);
                                    float dk = 1f - ToyShading.SideDarken * Clamp01(1f + d / wall[i]);
                                    wr *= dk; wg *= dk; wb *= dk;
                                    cr = wr + (cr - wr) * faceW; cg = wg + (cg - wg) * faceW; cb = wb + (cb - wb) * faceW;
                                    if (faceW < 0.5f) { nx = wx * wl; ny = wy * wl; nz = sz * wl; }
                                }
                                // Bevel line between face and side (medium weight), kept off the outer contour.
                                if (d < -OutlineWidth)
                                {
                                    float bl = Clamp01(1f - Math.Abs(fdExact) / ToyShading.BevelLineWidth);
                                    if (bl > 0f)
                                    {
                                        float m = ToyShading.BevelLineInk * bl;
                                        cr += (sr + (Ink.r - sr) * 0.5f - cr) * m; cg += (sg + (Ink.g - sg) * 0.5f - cg) * m; cb += (sb + (Ink.b - sb) * 0.5f - cb) * m;
                                    }
                                }
                            }
                            if (cov > 0.5f) { bnx = nx; bny = ny; bnz = nz; below = lk; }
                        }
                        else
                        {
                            // Printed or bump: follow the surface beneath, plus a small bevel / dome of its own.
                            float nx = bnx / bnz, ny = bny / bnz;
                            if (kind[i] == PartKind.Bump || (s.silhouette && depth[i] > px * 2f))
                            {
                                float gx, gy;
                                Grad(N[i], xi, yi, res, out gx, out gy);
                                float r = kind[i] == PartKind.Bump ? radius[i] : Math.Min(depth[i], ToyShading.PrintedBevel);
                                float sl = Slope(-d / Math.Max(px, r)) * (kind[i] == PartKind.Bump ? 0.8f : 0.5f);
                                nx += sl * gx; ny += sl * gy;
                            }
                            float il = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 1f);
                            Light(ref rig, f, sr, sg, sb, nx * il, ny * il, il, below, out cr, out cg, out cb);
                        }

                        // Stacking occlusion from raised parts above this one, and the object's underside.
                        if (s.silhouette || kind[i] == PartKind.Printed)
                        {
                            float occ = 1f - ground;
                            float c = aboveCast[i];
                            if (c < 1e8f) occ -= ToyShading.CastStrength * (1f - Smooth(-0.02f, ToyShading.CastSoftness, c));
                            float p2 = aboveNear[i];
                            if (p2 > 0f && p2 < ToyShading.ContactWidth)
                            {
                                float w = 1f - p2 / ToyShading.ContactWidth;
                                occ -= ToyShading.ContactStrength * w * w;
                            }
                            occ = Math.Max(0.45f, occ);
                            cr *= occ; cg *= occ; cb *= occ;
                        }

                        // Light internal seams; tiny parts get none.
                        if (s.outline && depth[i] >= ToyShading.SmallPart)
                        {
                            float t = Clamp01((d + ToyShading.SeamWidth) / px + 0.5f);
                            float m = ToyShading.SeamInk;
                            cr += (sr + (Ink.r - sr) * m - cr) * t; cg += (sg + (Ink.g - sg) * m - cg) * t; cb += (sb + (Ink.b - sb) * m - cb) * t;
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
        /// Largest wall thickness (up to t) that keeps every part sitting on part i (keys on a calculator, a label
        /// on a bottle) on i's face: a child pixel p is on the face while p moved down the wall direction by the
        /// thickness is still inside i. Children are later body parts lying at least 85% inside i.
        /// </summary>
        float WallClearance(float[][] D, int i, float t, float wallX, float wallY, float px, int res,
            int[] bx0, int[] by0, int[] bx1, int[] by1)
        {
            float[] di = D[i];
            for (int j = i + 1; j < shapes.Count && t >= px; j++)
            {
                var sj = shapes[j];
                if (!sj.silhouette || sj.shine || bx1[j] < 0) continue;
                if (bx1[j] < bx0[i] || bx0[j] > bx1[i] || by1[j] < by0[i] || by0[j] > by1[i]) continue;
                float[] dj = D[j];
                int all = 0, inside = 0;
                for (int yi = by0[j]; yi <= by1[j]; yi++)
                    for (int xi = bx0[j]; xi <= bx1[j]; xi++)
                    {
                        int k = yi * res + xi;
                        if (dj[k] >= 0f) continue;
                        all++;
                        if (di[k] < 0f) inside++;
                    }
                if (all == 0 || inside < all * 0.85f) continue;
                // Shrink the wall one pixel at a time until the whole child stays on the face.
                while (t >= px)
                {
                    float ox = wallX * t / px, oy = wallY * t / px;
                    bool ok = true;
                    for (int yi = by0[j]; yi <= by1[j] && ok; yi++)
                        for (int xi = bx0[j]; xi <= bx1[j]; xi++)
                        {
                            int k = yi * res + xi;
                            if (dj[k] >= 0f || di[k] >= 0f) continue;
                            if (SampleField(di, xi - ox, yi - oy, res) >= -0.5f * px) { ok = false; break; }
                        }
                    if (ok) break;
                    t -= px;
                }
            }
            return t;
        }

        // Pixels outside the silhouette can still hold decorations (steam, sparkles) that must be drawn.
        bool AnyDetailCovers(float[][] D, int k, float px)
        {
            for (int i = 0; i < shapes.Count; i++)
                if (!shapes[i].silhouette && !shapes[i].shine && D[i][k] < 0.5f * px) return true;
            return false;
        }

        struct Rig
        {
            public float lx, ly, lz, fx, fy, fz, hx, hy, hz, lo, front, hi, gloss;
        }

        static void ShadowTone(Rgba f, out float sr, out float sg, out float sb)
        {
            sr = f.r * ToyShading.ShadowR + ToyShading.ShadowLift;
            sg = f.g * ToyShading.ShadowG + ToyShading.ShadowLift;
            sb = f.b * ToyShading.ShadowB + ToyShading.ShadowLift * 1.6f;
            float sl = (sr + sg + sb) / 3f, sat = ToyShading.ShadowSaturation;
            sr = Clamp01(sl + (sr - sl) * sat); sg = Clamp01(sl + (sg - sl) * sat); sb = Clamp01(sl + (sb - sl) * sat);
        }

        /// <summary>Cartoon lighting of one surface point: banded shadow ramp, warm lit side, sheen + hot accent.</summary>
        static void Light(ref Rig rig, Rgba f, float sr, float sg, float sb, float nx, float ny, float nz,
            ToyShading.FamilyLook lk, out float cr, out float cg, out float cb)
        {
            float lum = ToyShading.Ambient
                        + ToyShading.KeyStrength * Math.Max(0f, nx * rig.lx + ny * rig.ly + nz * rig.lz)
                        + ToyShading.FillStrength * Math.Max(0f, nx * rig.fx + ny * rig.fy + nz * rig.fz);
            cr = f.r; cg = f.g; cb = f.b;
            if (lum >= rig.front)
            {
                float t = Clamp01((lum - rig.front) / Math.Max(1e-4f, rig.hi - rig.front)) * ToyShading.LightAmount;
                cr += (1f - cr) * t; cg += (0.98f - cg) * t; cb += (0.92f - cb) * t;
            }
            else
            {
                float v = Bands(Clamp01((lum - rig.lo) / Math.Max(1e-4f, rig.front - rig.lo)));
                cr = sr + (cr - sr) * v; cg = sg + (cg - sg) * v; cb = sb + (cb - sb) * v;
            }
            float nh = Math.Max(0f, nx * rig.hx + ny * rig.hy + nz * rig.hz);
            float spec = rig.gloss * (lk.hot * Smooth(lk.hotSize - 0.02f, lk.hotSize, nh) + lk.sheen * Smooth(lk.sheenFrom, 1f, nh));
            spec = Clamp01(spec);
            cr += (1f - cr) * spec; cg += (1f - cg) * spec; cb += (1f - cb) * spec;
        }

        static float Bands(float v)
        {
            int b = Math.Max(1, ToyShading.ToonBands);
            float x = v * b;
            float i = (float)Math.Floor(x), fr = x - i;
            float soft = ToyShading.ToonSoftness;
            fr = Smooth(0.5f - soft, 0.5f + soft, fr);
            return Math.Min(1f, (i + fr) / b);
        }

        // max(field, field shifted by (ox, oy) pixels): the part's face after moving it toward the light.
        static float FaceField(float[] f, int xi, int yi, int res, float ox, float oy)
        {
            xi = Math.Min(res - 1, Math.Max(0, xi));
            yi = Math.Min(res - 1, Math.Max(0, yi));
            float v = f[yi * res + xi];
            if (ox == 0f && oy == 0f) return v;
            return Math.Max(v, SampleField(f, xi - ox, yi - oy, res));
        }

        static float SampleField(float[] f, float x, float y, int res)
        {
            x = Math.Min(res - 1f, Math.Max(0f, x));
            y = Math.Min(res - 1f, Math.Max(0f, y));
            int x0 = (int)x, y0 = (int)y, x1 = Math.Min(res - 1, x0 + 1), y1 = Math.Min(res - 1, y0 + 1);
            float tx = x - x0, ty = y - y0;
            float a = f[y0 * res + x0] + (f[y0 * res + x1] - f[y0 * res + x0]) * tx;
            float b = f[y1 * res + x0] + (f[y1 * res + x1] - f[y1 * res + x0]) * tx;
            return a + (b - a) * ty;
        }
    }
}
