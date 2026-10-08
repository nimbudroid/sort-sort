using System;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Pixel recipe for one chunky "toy" panel: soft offset shadow, a darker extrusion underneath, and a bright face
    /// with a thick ink outline and a glossy top highlight. Used for buttons, cards, pills and bin/table panels.
    /// All sizes are in pixels.
    /// </summary>
    public sealed class ToyPanelSpec
    {
        public int width = 64, height = 64;
        public float radius = 16f;
        public float outline = 4f;
        public float extrude = 6f;       // depth of the darker base under the face
        public float pressed = 0f;       // how far (px) the face is pushed down into the extrusion
        public float shadow = 4f;        // shadow offset below the extrusion
        public float shadowSoftness = 4f;
        public float highlight = 0.28f;  // glossy band strength
        public Rgba face = Rgba.Hex("6BD36A");
        public Rgba? extrusionColor;     // default: face darkened
        // Layered "molded plastic" construction (all optional; 0 / null = off):
        public Rgba? rimColor;           // contrasting outer border between the face and the dark outline
        public float rim = 0f;           // rim width
        public float depth = 0f;         // dark depth band under the coloured extrusion
        public Rgba? depthColor;         // default: ink
        public Rgba ink = Rgba.Hex("2E2433");
        public Rgba shadowColor = new Rgba(0.12f, 0.08f, 0.22f, 0.28f);
    }

    /// <summary>Plain C# rasteriser for toy UI art (no UnityEngine), so it can be previewed outside Unity.</summary>
    public static class ToySkinArt
    {
        /// <summary>Straight-alpha RGBA bytes, row 0 = bottom (Unity texture order).</summary>
        public static byte[] Panel(ToyPanelSpec s)
        {
            int w = s.width, h = s.height;
            var bytes = new byte[w * h * 4];
            float m = s.outline * 0.5f + 1f;
            // Layout in "top-down" pixel space.
            float faceH = h - 2 * m - s.extrude - s.depth - s.shadow;
            float faceTop = m + s.pressed;
            float extTop = m + s.extrude;
            float depthTop = extTop + s.depth;
            Rgba ext = s.extrusionColor ?? s.face.Dark(0.32f);
            Rgba depthColor = s.depthColor ?? s.ink;
            Rgba faceTopColor = s.face.Light(0.14f);

            for (int row = 0; row < h; row++)
            {
                float y = h - row - 0.5f; // top-down coordinate of this texture row
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float ar = 0, ag = 0, ab = 0, aa = 0;

                    // 1. soft shadow under everything
                    float ds = RoundRect(px, y, m, depthTop + s.shadow, w - m, depthTop + s.shadow + faceH, s.radius);
                    float sa = s.shadowColor.a * (1f - Smooth(-s.shadowSoftness, s.shadowSoftness, ds));
                    Over(ref ar, ref ag, ref ab, ref aa, s.shadowColor, sa);

                    // 2a. dark depth layer
                    if (s.depth > 0f)
                    {
                        float dd = RoundRect(px, y, m, depthTop, w - m, depthTop + faceH, s.radius);
                        float cd = Cov(dd);
                        if (cd > 0f) Over(ref ar, ref ag, ref ab, ref aa, Rgba.Lerp(depthColor, s.ink, Cov(-(dd + s.outline))), cd);
                    }

                    // 2b. coloured extrusion with outline
                    float de = RoundRect(px, y, m, extTop, w - m, extTop + faceH, s.radius);
                    float ce = Cov(de);
                    if (ce > 0f) Over(ref ar, ref ag, ref ab, ref aa, Rgba.Lerp(ext, s.ink, Cov(-(de + s.outline))), ce);

                    // 3. face with vertical gradient, glossy highlight and outline
                    float df = RoundRect(px, y, m, faceTop, w - m, faceTop + faceH, s.radius);
                    float cf = Cov(df);
                    if (cf > 0f)
                    {
                        float t = Clamp01((y - faceTop) / Math.Max(1f, faceH));
                        Rgba c = Rgba.Lerp(faceTopColor, s.face, t);
                        float inset = s.outline + s.rim + Math.Max(2f, s.radius * 0.25f);
                        float hl = RoundRect(px, y, m + inset, faceTop + inset * 0.7f, w - m - inset, faceTop + faceH * 0.42f, s.radius * 0.6f);
                        c = Rgba.Lerp(c, new Rgba(1f, 1f, 1f, 1f), s.highlight * Cov(hl));
                        if (s.rim > 0f && s.rimColor.HasValue)
                        {
                            // Rim band just inside the outline, with a thin ink line separating it from the face.
                            Rgba rimC = s.rimColor.Value;
                            float inner = -(df + s.outline + s.rim);
                            c = Rgba.Lerp(c, rimC, Cov(inner));
                            c = Rgba.Lerp(c, s.ink, Cov(inner) * Cov(-inner - Math.Max(1f, s.outline * 0.35f)));
                        }
                        c = Rgba.Lerp(c, s.ink, Cov(-(df + s.outline)));
                        Over(ref ar, ref ag, ref ab, ref aa, c, cf);
                    }

                    int o = (row * w + x) * 4;
                    if (aa <= 0.001f) { bytes[o] = bytes[o + 1] = bytes[o + 2] = 255; bytes[o + 3] = 0; continue; }
                    bytes[o] = B(ar / aa); bytes[o + 1] = B(ag / aa); bytes[o + 2] = B(ab / aa); bytes[o + 3] = B(aa);
                }
            }
            return bytes;
        }

        /// <summary>Round toy knob (slider thumb): shadow, extrusion, face, outline, highlight.</summary>
        public static byte[] Knob(int size, float outline, float extrude, Rgba face, Rgba ink)
        {
            var spec = new ToyPanelSpec
            {
                width = size, height = size, outline = outline, extrude = extrude, shadow = 0f, shadowSoftness = 1f,
                face = face, ink = ink, highlight = 0.3f,
            };
            spec.radius = (size - (outline + 2f) - extrude) * 0.5f;
            return Panel(spec);
        }

        /// <summary>White light burst: alternating soft rays fading out from the centre.</summary>
        public static byte[] Sunburst(int size, int rays)
        {
            var bytes = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    double a = Math.Atan2(dy, dx);
                    float ray = (float)(0.5 + 0.5 * Math.Cos(a * rays));
                    ray = Smooth(0.35f, 0.65f, ray);
                    float fade = 1f - Smooth(0.15f, 1f, r);
                    float glow = 1f - Smooth(0f, 0.45f, r);
                    float alpha = Clamp01(fade * (0.55f * ray + 0.25f) + 0.5f * glow);
                    int o = (y * size + x) * 4;
                    bytes[o] = bytes[o + 1] = bytes[o + 2] = 255;
                    bytes[o + 3] = B(alpha);
                }
            return bytes;
        }

        /// <summary>Vertical gradient (row 0 = bottom colour).</summary>
        public static byte[] Gradient(int height, Rgba bottom, Rgba top)
        {
            var bytes = new byte[height * 4];
            for (int i = 0; i < height; i++)
            {
                Rgba c = Rgba.Lerp(bottom, top, i / (float)(height - 1));
                bytes[i * 4] = B(c.r); bytes[i * 4 + 1] = B(c.g); bytes[i * 4 + 2] = B(c.b); bytes[i * 4 + 3] = 255;
            }
            return bytes;
        }

        /// <summary>Soft ellipse blob (for ground shadows), white with falloff alpha.</summary>
        public static byte[] SoftBlob(int size)
        {
            var bytes = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = (float)Math.Sqrt(dx * dx + dy * dy);
                    float a = 1f - Smooth(0.45f, 1f, r);
                    int o = (y * size + x) * 4;
                    bytes[o] = bytes[o + 1] = bytes[o + 2] = 255;
                    bytes[o + 3] = B(a);
                }
            return bytes;
        }

        // Signed distance to a rounded rectangle given by its edges (top-down pixel space).
        static float RoundRect(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            float hw = (x1 - x0) * 0.5f, hh = (y1 - y0) * 0.5f;
            r = Math.Max(0f, Math.Min(r, Math.Min(hw, hh)));
            float qx = Math.Abs(x - cx) - (hw - r), qy = Math.Abs(y - cy) - (hh - r);
            float ox = Math.Max(qx, 0f), oy = Math.Max(qy, 0f);
            return (float)Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0f) - r;
        }

        static void Over(ref float ar, ref float ag, ref float ab, ref float aa, Rgba c, float a)
        {
            if (a <= 0f) return;
            ar = c.r * a + ar * (1f - a);
            ag = c.g * a + ag * (1f - a);
            ab = c.b * a + ab * (1f - a);
            aa = a + aa * (1f - a);
        }

        static float Cov(float d) { return Clamp01(0.5f - d); }
        static float Smooth(float e0, float e1, float x) { float t = Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }
        static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
        static byte B(float v) { return (byte)(Clamp01(v) * 255f + 0.5f); }
    }
}
