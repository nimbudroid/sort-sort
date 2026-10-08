using System;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Sculpted renderer (ToyShading.Mode = Sculpted): each drawn part is modelled as a small toy piece, built so
    /// that its form survives at phone size, where only large tonal zones are visible.
    ///
    /// Hard parts (Plastic, Paper families):
    ///   - a flat top face, shifted up inside the part;
    ///   - a wide rounded bevel ring that starts where the contour ends, lit on the key side and dark opposite;
    ///   - a side plane in the band the face shift leaves along the bottom, clearly darker, with a seam above it.
    ///   Parts too thin for a face and bevel (pencil barrel, key shaft, stems) become tubes.
    /// Soft parts (Soft family): an inflated dome over the whole face plus a small underside plane; Matte food and
    /// fabric get a gentler dome.
    /// Shared with Molded: part classification (raised / printed / bump), wall clearance for child parts,
    /// stacking occlusion, specular from the normals only (Shine shapes dropped), soft baked contact shadow.
    /// The outer contour is full weight except on parts too thin to carry it. Coverage and silhouette are unchanged.
    /// </summary>
    public sealed partial class VectorPainter
    {
        byte[] RasterizeSculpted(int res)
        {
            int n = shapes.Count, count = res * res;
            float px = 2f / res;

            // Distance fields, the silhouette, and which body part defines the silhouette at each pixel.
            var D = new float[n][];
            for (int i = 0; i < n; i++) D[i] = new float[count];
            var U = new float[count];
            var owner = new int[count];
            for (int yi = 0; yi < res; yi++)
            {
                float y = (yi + 0.5f) / res * 2f - 1f;
                for (int xi = 0; xi < res; xi++)
                {
                    float x = (xi + 0.5f) / res * 2f - 1f;
                    int k = yi * res + xi;
                    float u = 1e9f;
                    int own = -1;
                    for (int i = 0; i < n; i++)
                    {
                        float d = shapes[i].sdf(x, y);
                        D[i][k] = d;
                        if (shapes[i].silhouette && d < u) { u = d; own = i; }
                    }
                    U[k] = u;
                    owner[k] = own;
                }
            }

            float lx, ly, lz;
            ToyShading.Light(out lx, out ly, out lz);
            float lxy = (float)Math.Sqrt(lx * lx + ly * ly);
            float dirX = lx / lxy, dirY = ly / lxy;
            float wdl = (float)Math.Sqrt(ToyShading.WallDirX * ToyShading.WallDirX + ToyShading.WallDirY * ToyShading.WallDirY);
            float wallX = ToyShading.WallDirX / wdl, wallY = ToyShading.WallDirY / wdl;
            int blur = Math.Max(1, (int)(res * ToyShading.NormalSmoothing + 0.5f));
            var tmp = new float[count];

            var bx0 = new int[n]; var by0 = new int[n]; var bx1 = new int[n]; var by1 = new int[n];
            var depth = new float[n];
            for (int i = 0; i < n; i++)
            {
                bx0[i] = res; by0[i] = res; bx1[i] = -1; by1[i] = -1;
                float[] di = D[i];
                float dep = 0f;
                for (int yi = 0; yi < res; yi++)
                    for (int xi = 0; xi < res; xi++)
                    {
                        float v = di[yi * res + xi];
                        if (v >= 0f) continue;
                        if (-v > dep) dep = -v;
                        if (xi < bx0[i]) bx0[i] = xi;
                        if (xi > bx1[i]) bx1[i] = xi;
                        if (yi < by0[i]) by0[i] = yi;
                        if (yi > by1[i]) by1[i] = yi;
                    }
                depth[i] = dep;
            }

            // Per-part model.
            var kind = new PartKind[n];
            var look = new ToyShading.FamilyLook[n];
            var soft = new bool[n];
            var inset = new float[n];       // contour weight on this part = where its visible face begins
            var bevelW = new float[n];      // width of the rounded ring (or the whole half-width for tubes/domes)
            var tilt = new float[n];        // tilt at the outside of the ring (radians)
            var wall = new float[n];
            var offX = new float[n];
            var offY = new float[n];
            var N = new float[n][];
            for (int i = 0; i < n; i++)
            {
                var s = shapes[i];
                float dep = depth[i];
                float[] di = D[i];
                ToyFamily fam = Family, partFam;
                if (PartFamilies != null && PartFamilies.TryGetValue(i, out partFam)) fam = partFam;
                look[i] = ToyShading.Look(fam);
                soft[i] = fam == ToyFamily.Soft || fam == ToyFamily.Matte; // domes; Plastic and Paper are molded
                inset[i] = Math.Min(ToyShading.Sculpt.Outline, ToyShading.Sculpt.ThinOutline * dep);
                N[i] = di;

                if (s.shine) { kind[i] = PartKind.Skip; continue; }
                if (dep <= px || !s.shade) { kind[i] = PartKind.Printed; continue; }
                if (!s.silhouette) { kind[i] = PartKind.Bump; bevelW[i] = Math.Min(dep, ToyShading.DetailRadius); continue; }

                int edge = 0, covered = 0;
                for (int yi = Math.Max(0, by0[i] - 1); yi <= Math.Min(res - 1, by1[i] + 1); yi++)
                    for (int xi = Math.Max(0, bx0[i] - 1); xi <= Math.Min(res - 1, bx1[i] + 1); xi++)
                    {
                        int k = yi * res + xi;
                        if (Math.Abs(di[k]) >= px) continue;
                        edge++;
                        for (int j = 0; j < i; j++)
                            if (shapes[j].silhouette && D[j][k] < -0.02f) { covered++; break; }
                    }
                if (edge > 0 && covered >= edge * 0.85f) { kind[i] = PartKind.Printed; continue; }

                kind[i] = PartKind.Raised;
                bool matte = fam == ToyFamily.Matte || fam == ToyFamily.Paper;
                float t = soft[i]
                    ? Math.Min(ToyShading.Sculpt.SoftWall * dep, ToyShading.Sculpt.SoftWallMax)
                    : Math.Min(ToyShading.Sculpt.Wall * dep, ToyShading.Sculpt.WallMax);
                if (t >= px) t = WallClearance(D, i, t, wallX, wallY, px, res, bx0, by0, bx1, by1, inset[i] + ToyShading.Sculpt.EdgeRegion);
                if (t < px) t = 0f;
                wall[i] = t;
                offX[i] = wallX * t / px;
                offY[i] = wallY * t / px;

                float usable = Math.Max(px, dep - t * 0.5f - inset[i]); // visible half-thickness of the face
                if (soft[i])
                {
                    bevelW[i] = usable;
                    tilt[i] = (matte ? ToyShading.Sculpt.MatteDomeTilt : ToyShading.Sculpt.DomeTilt) * (float)Math.PI / 180f;
                }
                else
                {
                    float bw = matte ? ToyShading.Sculpt.MatteBevel : ToyShading.Sculpt.Bevel;
                    // Parts sitting on this one (keys, labels) stay on the flat face: the ring stops short of them.
                    float clear = ChildFaceClearance(D, i, offX[i], offY[i], res, bx0, by0, bx1, by1) - inset[i] - 0.015f;
                    if (clear < bw) bw = Math.Max(2f * px, clear);
                    bevelW[i] = usable < bw * 1.3f ? usable : bw; // thin: the whole part rounds over (a tube)
                    tilt[i] = (matte ? ToyShading.Sculpt.MatteBevelTilt : ToyShading.Sculpt.BevelTilt) * (float)Math.PI / 180f;
                }
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

            var sc = ToyShading.Sculpt.CastOffset;
            int castX = (int)Math.Round(dirX * sc / px), castY = (int)Math.Round(dirY * sc / px);
            var aboveCast = new float[n];
            var aboveNear = new float[n];
            var outBytes = new byte[count * 4];

            for (int yi = 0; yi < res; yi++)
            {
                for (int xi = 0; xi < res; xi++)
                {
                    int k = yi * res + xi;
                    float union = U[k];
                    if (union > px)
                    {
                        outBytes[k * 4] = outBytes[k * 4 + 1] = outBytes[k * 4 + 2] = 255; outBytes[k * 4 + 3] = 0;
                        if (!AnyDetailCovers(D, k, px)) continue;
                    }

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

                    float ground = 0f;
                    if (union < 0.2f)
                    {
                        float ugx, ugy;
                        Grad(NU, xi, yi, res, out ugx, out ugy);
                        if (ugy < 0f) ground = ToyShading.Sculpt.GroundAO * (1f - Smooth(0f, 0.16f, -union)) * -ugy;
                    }

                    float bnx = 0f, bny = 0f, bnz = 1f;
                    ToyShading.FamilyLook below = n > 0 ? look[0] : ToyShading.Plastic;
                    Rgba belowFill = new Rgba(0f, 0f, 0f, 0f);

                    float ar = 0f, ag = 0f, ab = 0f, aa = 0f;
                    for (int i = 0; i < n; i++)
                    {
                        if (kind[i] == PartKind.Skip) continue;
                        var s = shapes[i];
                        float d = D[i][k];
                        float cov = Clamp01(0.5f - d / px) * s.fill.a;
                        if (cov <= 0f) continue;
                        // A decorative stroke that is only a lighter tint of the surface beneath is a painted
                        // highlight; gloss comes from the normals instead.
                        if (!s.silhouette && kind[i] == PartKind.Printed && belowFill.a > 0f && IsTintHighlight(s.fill, belowFill)) continue;

                        Rgba f = s.fill;
                        float sr, sg, sb;
                        SculptShadowTone(f, out sr, out sg, out sb);
                        float cr, cg, cb;
                        var lk = look[i];

                        if (kind[i] == PartKind.Raised)
                        {
                            // Top face field (part ∩ part shifted up), smoothed for normals.
                            float fc = FaceField(N[i], xi, yi, res, offX[i], offY[i]);
                            float gx = FaceField(N[i], xi + 1, yi, res, offX[i], offY[i]) - FaceField(N[i], xi - 1, yi, res, offX[i], offY[i]);
                            float gy = FaceField(N[i], xi, yi + 1, res, offX[i], offY[i]) - FaceField(N[i], xi, yi - 1, res, offX[i], offY[i]);
                            float gl = (float)Math.Sqrt(gx * gx + gy * gy);
                            if (gl > 1e-6f) { gx /= gl; gy /= gl; } else { gx = 0f; gy = 0f; }

                            // Profile across the visible face: 0 at the inner edge of the contour, 1 at the ring's end.
                            float e = Clamp01((-fc - inset[i]) / bevelW[i]);
                            float slope;
                            if (soft[i])
                            {
                                // Dome: circular profile, steepest at the edge.
                                float q = 1f - e;
                                slope = Math.Min((float)Math.Tan(tilt[i]), q / (float)Math.Sqrt(Math.Max(1f - q * q, 1e-4f)));
                            }
                            else
                            {
                                // Rounded bevel: from `tilt` at the outside of the ring to a flat molded face.
                                float q = 1f - e, ease = q * q * (3f - 2f * q);
                                float hold = ToyShading.Sculpt.BevelHold;
                                slope = (float)Math.Tan(tilt[i] * (ease + (q - ease) * hold));
                            }
                            float nx = slope * gx, ny = slope * gy, il = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 1f);
                            nx *= il; ny *= il;
                            float nz = il;
                            SculptLight(ref rig, f, sr, sg, sb, nx, ny, nz, lk, out cr, out cg, out cb);

                            if (wall[i] > 0f)
                            {
                                float fdExact = Math.Max(d, SampleField(D[i], xi - offX[i], yi - offY[i], res));
                                float faceW = Clamp01(0.5f - fdExact / px);
                                if (faceW < 1f)
                                {
                                    // Side plane: faces down and out, darkening toward its bottom edge.
                                    float wx, wy;
                                    Grad(N[i], xi, yi, res, out wx, out wy);
                                    float sz = ToyShading.Sculpt.SideNormalZ, wl = 1f / (float)Math.Sqrt(wx * wx + wy * wy + sz * sz);
                                    float wr, wg, wb;
                                    SculptLight(ref rig, f, sr, sg, sb, wx * wl, wy * wl, sz * wl, lk, out wr, out wg, out wb);
                                    float dk = 1f - ToyShading.Sculpt.SideDarken * Clamp01(1f + d / wall[i]);
                                    wr *= dk; wg *= dk; wb *= dk;
                                    cr = wr + (cr - wr) * faceW; cg = wg + (cg - wg) * faceW; cb = wb + (cb - wb) * faceW;
                                    if (faceW < 0.5f) { nx = wx * wl; ny = wy * wl; nz = sz * wl; }
                                }
                                if (d < -inset[i])
                                {
                                    float bl = Clamp01(1f - Math.Abs(fdExact) / ToyShading.Sculpt.BevelLineWidth) * ToyShading.Sculpt.BevelLine;
                                    if (bl > 0f)
                                    {
                                        cr += (sr + (Ink.r - sr) * 0.5f - cr) * bl; cg += (sg + (Ink.g - sg) * 0.5f - cg) * bl; cb += (sb + (Ink.b - sb) * 0.5f - cb) * bl;
                                    }
                                }
                            }
                            if (cov > 0.5f) { bnx = nx; bny = ny; bnz = nz; below = lk; belowFill = f; }
                        }
                        else
                        {
                            float nx = bnx / bnz, ny = bny / bnz;
                            if (kind[i] == PartKind.Bump || (s.silhouette && depth[i] > px * 2f))
                            {
                                float gx, gy;
                                Grad(N[i], xi, yi, res, out gx, out gy);
                                float r = kind[i] == PartKind.Bump ? bevelW[i] : Math.Min(depth[i], ToyShading.PrintedBevel);
                                float sl = Slope(-d / Math.Max(px, r)) * (kind[i] == PartKind.Bump ? 0.8f : 0.5f);
                                nx += sl * gx; ny += sl * gy;
                            }
                            float il = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 1f);
                            SculptLight(ref rig, f, sr, sg, sb, nx * il, ny * il, il, below, out cr, out cg, out cb);
                        }

                        if (s.silhouette || kind[i] == PartKind.Printed)
                        {
                            float occ = 1f - ground;
                            float c = aboveCast[i];
                            if (c < 1e8f) occ -= ToyShading.Sculpt.CastStrength * (1f - Smooth(-0.02f, ToyShading.Sculpt.CastSoftness, c));
                            float p2 = aboveNear[i];
                            if (p2 > 0f && p2 < ToyShading.Sculpt.ContactWidth)
                            {
                                float w = 1f - p2 / ToyShading.Sculpt.ContactWidth;
                                occ -= ToyShading.Sculpt.ContactStrength * w * w;
                            }
                            occ = Math.Max(0.4f, occ);
                            cr *= occ; cg *= occ; cb *= occ;
                        }

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
                        // Full-weight contour, thinner only where the part defining the edge is too thin to carry it.
                        float w = owner[k] >= 0 ? inset[owner[k]] : ToyShading.Sculpt.Outline;
                        float t = Clamp01((union + w) / px + 0.5f);
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
        /// Smallest distance from part i's top-face edge to any child part sitting on it (later body parts at least
        /// 85% inside i), or a large value when there are none. Flat decals (pepperoni) and regions that reach i's
        /// edge (a toe cap, a folded corner) are part of i's surface and do not count.
        /// </summary>
        float ChildFaceClearance(float[][] D, int i, float ox, float oy, int res, int[] bx0, int[] by0, int[] bx1, int[] by1)
        {
            float[] di = D[i];
            float best = 1e9f;
            for (int j = i + 1; j < shapes.Count; j++)
            {
                var sj = shapes[j];
                if (!sj.silhouette || sj.shine || !sj.shade || bx1[j] < 0) continue; // flat decals follow the bevel
                if (bx1[j] < bx0[i] || bx0[j] > bx1[i] || by1[j] < by0[i] || by0[j] > by1[i]) continue;
                float[] dj = D[j];
                int all = 0, inside = 0;
                float m = 1e9f;
                for (int yi = by0[j]; yi <= by1[j]; yi++)
                    for (int xi = bx0[j]; xi <= bx1[j]; xi++)
                    {
                        int k = yi * res + xi;
                        if (dj[k] >= 0f) continue;
                        all++;
                        if (di[k] >= 0f) continue;
                        inside++;
                        float face = -FaceField(di, xi, yi, res, ox, oy);
                        if (face < m) m = face;
                    }
                if (all == 0 || inside < all * 0.85f) continue;
                if (ReachesEdge(di, dj, res, bx0[j], by0[j], bx1[j], by1[j], Math.Min(ToyShading.Sculpt.Outline, ToyShading.Sculpt.ThinOutline * Depth(di)) + ToyShading.Sculpt.EdgeRegion)) continue;
                if (m < best) best = m;
            }
            return best;
        }

        static float Depth(float[] d)
        {
            float m = 0f;
            for (int k = 0; k < d.Length; k++) if (-d[k] > m) m = -d[k];
            return m;
        }

        /// <summary>True when `c` is `under` lightened toward white by 10-80% (Rgba.Light), i.e. a painted highlight.</summary>
        static bool IsTintHighlight(Rgba c, Rgba under)
        {
            float kr = under.r < 0.98f ? (c.r - under.r) / (1f - under.r) : -1f;
            float kg = under.g < 0.98f ? (c.g - under.g) / (1f - under.g) : -1f;
            float kb = under.b < 0.98f ? (c.b - under.b) / (1f - under.b) : -1f;
            float lo = Math.Min(kr, Math.Min(kg, kb)), hi = Math.Max(kr, Math.Max(kg, kb));
            return lo > 0.1f && hi < 0.8f && hi - lo < 0.1f;
        }

        static void SculptShadowTone(Rgba f, out float sr, out float sg, out float sb)
        {
            sr = f.r * ToyShading.Sculpt.ShadowR + ToyShading.Sculpt.ShadowLift;
            sg = f.g * ToyShading.Sculpt.ShadowG + ToyShading.Sculpt.ShadowLift;
            sb = f.b * ToyShading.Sculpt.ShadowB + ToyShading.Sculpt.ShadowLift * 1.6f;
            float sl = (sr + sg + sb) / 3f, sat = ToyShading.Sculpt.ShadowSaturation;
            sr = Clamp01(sl + (sr - sl) * sat); sg = Clamp01(sl + (sg - sl) * sat); sb = Clamp01(sl + (sb - sl) * sat);
        }

        /// <summary>Two-band cartoon lighting with a warm lit side; gloss from the part's family, on the same normal.</summary>
        static void SculptLight(ref Rig rig, Rgba f, float sr, float sg, float sb, float nx, float ny, float nz,
            ToyShading.FamilyLook lk, out float cr, out float cg, out float cb)
        {
            float lum = ToyShading.Ambient
                        + ToyShading.KeyStrength * Math.Max(0f, nx * rig.lx + ny * rig.ly + nz * rig.lz)
                        + ToyShading.FillStrength * Math.Max(0f, nx * rig.fx + ny * rig.fy + nz * rig.fz);
            cr = f.r; cg = f.g; cb = f.b;
            if (lum >= rig.front)
            {
                float t = Clamp01((lum - rig.front) / Math.Max(1e-4f, rig.hi - rig.front)) * ToyShading.Sculpt.LightAmount;
                cr += (1f - cr) * t; cg += (0.98f - cg) * t; cb += (0.9f - cb) * t;
            }
            else
            {
                float v = Clamp01((lum - rig.lo) / Math.Max(1e-4f, rig.front - rig.lo));
                int b = Math.Max(1, ToyShading.Sculpt.Bands);
                float x = v * b, i = (float)Math.Floor(x), fr = x - i, soft = ToyShading.Sculpt.BandSoftness;
                v = Math.Min(1f, (i + Smooth(0.5f - soft, 0.5f + soft, fr)) / b);
                cr = sr + (cr - sr) * v; cg = sg + (cg - sg) * v; cb = sb + (cb - sb) * v;
            }
            float nh = Math.Max(0f, nx * rig.hx + ny * rig.hy + nz * rig.hz);
            float spec = Clamp01(rig.gloss * (lk.hot * Smooth(lk.hotSize - ToyShading.Sculpt.HotWidth, lk.hotSize, nh) + lk.sheen * Smooth(lk.sheenFrom, 1f, nh)));
            cr += (1f - cr) * spec; cg += (1f - cg) * spec; cb += (1f - cb) * spec;
        }
    }
}
