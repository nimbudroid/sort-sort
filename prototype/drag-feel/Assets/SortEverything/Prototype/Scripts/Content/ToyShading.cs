using System;
using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Art direction for library objects: "small molded 3D toy", not "flat vector sticker".
    /// VectorPainter turns every drawing's 2D shapes into an implied height field (each part is inflated like a
    /// rubber toy, the whole object domes as one piece, and parts printed onto a body follow the body's curve),
    /// then lights it with one consistent toy-box rig. Every tunable for that look lives here so the whole library
    /// stays in one style; per-object material tweaks live in <see cref="PerObject"/>.
    /// Plain C# (no UnityEngine). Rendering only: silhouettes and therefore colliders are unaffected.
    /// </summary>
    /// <summary>Which object renderer VectorPainter uses.</summary>
    public enum ToyRenderMode
    {
        Flat,     // original flat sticker shading
        Painted,  // first ToyShading pass (dae71c2): lighting painted over inflated regions
        Molded,   // per-part height maps with face + side wall, stacking occlusion, families (9a39506)
        Sculpted, // modelled toy parts: inset face, wide lit/dark bevel ring, side plane, tubes (default)
    }

    public static class ToyShading
    {
        /// <summary>Renderer switch: Flat / Painted / Molded. Read once per object when its sprite is baked.</summary>
        public static ToyRenderMode Mode = ToyRenderMode.Sculpted;

        /// <summary>True for the renderers that bake a soft contact-shadow sprite (Molded, Sculpted).</summary>
        public static bool BakesShadow { get { return Mode == ToyRenderMode.Molded || Mode == ToyRenderMode.Sculpted; } }

        /// <summary>Legacy switch from the Painted pass: true for any toy renderer, false = Flat.</summary>
        public static bool Enabled
        {
            get { return Mode != ToyRenderMode.Flat; }
            set { Mode = value ? (Mode == ToyRenderMode.Flat ? ToyRenderMode.Painted : Mode) : ToyRenderMode.Flat; }
        }

        // Light rig (normalised in Normalize()): key light from the upper-left, in front of the object.
        public static float LightX = -0.45f, LightY = 0.7f, LightZ = 0.75f;

        // Volume.
        public static float MaxPartRadius = 0.6f;   // inflation radius cap for a part (canvas units, -1..1 space)
        public static float InlayBevel = 0.07f;      // printed-on parts (labels, windows) only get a small bevel
        public static float WholeBodyWeight = 0.4f;  // how much every part also follows the whole object's dome
        public static float DetailRadius = 0.1f;     // shaded decorations (seeds, sprinkles) get a tiny dome
        public static float MaxSlope = 2.4f;         // steepest edge tilt; lower = softer, rounder edges
        public static float BoxBevelRadius = 0.22f;  // bevel radius for parts with corners (boxes, slices)
        public static float RoundRatio = 1.07f, BoxRatio = 1.22f; // corner test thresholds (see PartRadius)
        public static float NormalSmoothing = 0.025f; // blur of the fields behind the normals (fraction of sprite)

        // Tone ramp: shadow -> base -> light. The front-facing middle stays at the base (bin) colour.
        public static float FrontLevel = 0.8f;       // diffuse level that maps exactly to the base colour
        public static float ShadowStart = 0.22f;     // diffuse level that maps to the full shadow tone
        public static float ShadowR = 0.6f, ShadowG = 0.5f, ShadowB = 0.7f;   // hue-shifted (cool, purple) shadow
        public static float ShadowLift = 0.035f;     // keeps shadows from going muddy on dark colours
        public static float ShadowSaturation = 1.3f; // shadows stay colourful (pale greens/yellows never go grey)
        public static float LightAmount = 0.24f;     // how far the lit side goes toward warm white

        // Occlusion and bounce.
        public static float UndersideAO = 0.22f;     // darkening along the bottom of the whole silhouette
        public static float ContactAO = 0.3f;        // darkening where a part sits on top of another
        public static float ContactAOWidth = 0.09f;
        public static float BounceLight = 0.16f;     // soft cool rim light on the lower-right edges

        // Gloss: a crisp cartoon hotspot plus a broad sheen. Scaled per object by Gloss in PerObject.
        public static float SpecularSize = 0.975f;   // closer to 1 = smaller hotspot
        public static float SpecularHotspot = 0.85f;
        public static float SpecularSheen = 0.14f;

        // Hand-placed Shine() ellipses become soft gloss instead of opaque stickers.
        public static float ShineAlphaScale = 0.6f;
        public static float ShineFeather = 0.65f;    // fraction of the shine's short radius that is feathered

        // Contour: the outer silhouette stays ink; internal part lines take the part's own shadow colour mixed
        // with ink so they read as a medium-weight seam rather than a full outline.
        public static float InnerLineInk = 0.7f;

        /// <summary>Per-object material: gloss (specular strength) and volume (inflation radius scale).</summary>
        public struct Material
        {
            public float gloss, volume;
            public bool dome; // organic shapes with points (heart-shaped strawberry, two-lobed apple): always dome
            public Material(float gloss, float volume, bool dome = false) { this.gloss = gloss; this.volume = volume; this.dome = dome; }
        }

        public static readonly Material Default = new Material(1f, 1f);
        static Material Dome(float gloss = 1f) { return new Material(gloss, 1f, true); }

        /// <summary>
        /// Exceptions to the default plastic. Kept short on purpose: consistency across the library matters more
        /// than material realism. Matte = soft rubber/baked look, glossy = candy/plastic shell.
        /// </summary>
        public static readonly Dictionary<string, Material> PerObject = new Dictionary<string, Material>
        {
            // Baked / soft foods: matte so they don't read as plastic shells.
            { "pizza_slice", new Material(0.45f, 1f) }, { "cookie", new Material(0.4f, 1f) },
            { "bread", new Material(0.4f, 1f) }, { "burger", new Material(0.6f, 1f) },
            { "fries", new Material(0.5f, 1f) }, { "sock", new Material(0.35f, 1f) },
            { "paper_sheet", new Material(0.3f, 0.6f) }, { "sticky_note", new Material(0.35f, 0.7f) },
            { "notebook", new Material(0.45f, 0.8f) }, { "folder", new Material(0.45f, 0.8f) },
            // Glossy candy / plastic.
            { "donut", new Material(1.2f, 1f) }, { "popsicle", new Material(1.15f, 1f) },
            { "ketchup", new Material(1.2f, 1f) }, { "toy_ball", new Material(1.25f, 1f) },
            { "apple", new Material(1.15f, 1.5f, true) }, { "sunglasses", new Material(1.3f, 1f) },
            // Organic shapes whose outline has points, so the corner test would bevel them.
            { "strawberry", Dome() }, { "lemon", Dome() }, { "avocado", Dome() }, { "dragon_egg", Dome() },
            { "pepper", Dome(1.15f) }, { "corn", Dome() },
            { "carrot", Dome() }, { "pine_cone", Dome() }, { "fish", Dome() }, { "rubber_duck", Dome(1.2f) },
        };

        public static Material For(string id)
        {
            Material m;
            return id != null && PerObject.TryGetValue(id, out m) ? m : Default;
        }

        // ======================================================================================
        // Molded renderer (Mode = Molded). Everything below is used only by VectorPainter.Molded.cs.
        // ======================================================================================

        /// <summary>Look of one material family: height profile, side wall, gloss.</summary>
        public sealed class FamilyLook
        {
            public float bevel;       // < 0: dome that keeps rising to the centre; else bevel radius, then flat face
            public float slope;       // profile steepness (1 = round, lower = gentle)
            public float side;        // side wall thickness as a fraction of the part's depth...
            public float sideMax;     // ...capped at this (canvas units). The outer contour (0.09) is drawn inside
                                      // the silhouette, so only the part of the wall beyond it is visible.
            public float hot;         // small hot specular accent strength
            public float hotSize;     // N.H threshold for the hot accent (lower = bigger)
            public float sheen;       // broad soft sheen strength
            public float sheenFrom;   // N.H where the sheen starts (lower = broader)
        }

        /// <summary>Fruit, balls, frosting, popsicle: rises to the centre, soft, big rounded highlight.</summary>
        public static readonly FamilyLook Soft = new FamilyLook
        {
            bevel = -1f, slope = 1f, side = 0.3f, sideMax = 0.16f,
            hot = 0.7f, hotSize = 0.978f, sheen = 0.2f, sheenFrom = 0.86f,
        };

        /// <summary>Calculator, stapler, bottle, toys: rounded bevel, nearly flat face, crisp bevel highlight.</summary>
        public static readonly FamilyLook Plastic = new FamilyLook
        {
            bevel = 0.13f, slope = 1f, side = 0.45f, sideMax = 0.24f,
            hot = 0.9f, hotSize = 0.988f, sheen = 0.12f, sheenFrom = 0.9f,
        };

        /// <summary>Pizza, bread, paper, fabric: gentle dome, almost no specular, broad soft shading.</summary>
        public static readonly FamilyLook Matte = new FamilyLook
        {
            bevel = -1f, slope = 0.5f, side = 0.36f, sideMax = 0.19f,
            hot = 0.1f, hotSize = 0.975f, sheen = 0.07f, sheenFrom = 0.85f,
        };

        public static FamilyLook Look(ToyFamily f)
        {
            return f == ToyFamily.Soft ? Soft : (f == ToyFamily.Matte || f == ToyFamily.Paper) ? Matte : Plastic;
        }

        // Lights: key from the upper-left front, soft fill from the lower-right, low ambient.
        public static float FillX = 0.6f, FillY = -0.55f, FillZ = 0.45f;
        public static float KeyStrength = 0.85f, FillStrength = 0.2f, Ambient = 0.14f;

        // Cartoon tone ramp: the shadow half is softly stepped into bands.
        public static int ToonBands = 3;
        public static float ToonSoftness = 0.3f;     // 0 = hard cel bands, 0.5 = smooth

        // Shape.
        public static float ThinPart = 0.2f;         // plastic parts thinner than this are fully round (barrels)
        public static float CornerBevel = 0.2f;      // soft/matte parts with corners use this bevel, not a pillow dome
        public static float SideNormalZ = 0.25f;     // side wall tilt (lower = side faces further away)
        public static float SideDarken = 0.18f;      // extra darkening across the side wall toward its outer edge
        // Where the side wall shows: toys are seen slightly from above and in front, so the wall is mostly the
        // bottom/front face with a little of the right side (the face is shifted the opposite way, up-left).
        public static float WallDirX = -0.3f, WallDirY = 1f;
        public static float PrintedBevel = 0.035f;   // printed-on parts (labels, pepperoni) get only this bevel

        // Stacking occlusion: parts drawn later sit higher and shade what is beneath them.
        public static float CastOffset = 0.06f;      // how far upper parts' shadows fall away from the key light
        public static float CastSoftness = 0.07f;
        public static float CastStrength = 0.32f;
        public static float ContactWidth = 0.06f;    // tight occlusion right along an upper part's edge
        public static float ContactStrength = 0.22f;
        public static float GroundAO = 0.2f;         // underside of the whole object

        // Contour hierarchy: outer contour (VectorPainter.OutlineWidth, ink) > bevel line > internal seams.
        public static float BevelLineWidth = 0.016f;
        public static float BevelLineInk = 0.5f;
        public static float SeamWidth = 0.028f;
        public static float SeamInk = 0.42f;
        public static float SmallPart = 0.06f;       // parts thinner than this get no internal seam line

        // Baked contact shadow sprite (separate layer under the object; built from the silhouette, never read by it).
        public static int ShadowRes = 64;
        public static float ShadowPadding = 0.15f;   // extra canvas around the 1-unit object, each side (world units)
        public static float ShadowBlur = 0.035f;     // blur radius as a fraction of the shadow texture
        public static float ShadowAlpha = 0.3f;

        /// <summary>
        /// Soft shadow alpha mask from a silhouette mask (RasterizeSilhouette layout, res x res, covering one unit).
        /// Output covers (1 + 2 * ShadowPadding) units at ShadowRes x ShadowRes, white RGB, straight alpha.
        /// </summary>
        public static byte[] SoftShadow(byte[] silhouette, int res)
        {
            int outRes = ShadowRes;
            float span = 1f + 2f * ShadowPadding;
            var a = new float[outRes * outRes];
            // Box-filter the silhouette into the padded output grid.
            for (int yo = 0; yo < outRes; yo++)
                for (int xo = 0; xo < outRes; xo++)
                {
                    float u0 = (xo / (float)outRes) * span - ShadowPadding, u1 = ((xo + 1) / (float)outRes) * span - ShadowPadding;
                    float v0 = (yo / (float)outRes) * span - ShadowPadding, v1 = ((yo + 1) / (float)outRes) * span - ShadowPadding;
                    int x0 = Math.Max(0, (int)(u0 * res)), x1 = Math.Min(res, (int)Math.Ceiling(u1 * res));
                    int y0 = Math.Max(0, (int)(v0 * res)), y1 = Math.Min(res, (int)Math.Ceiling(v1 * res));
                    int on = 0, all = 0;
                    for (int y = y0; y < y1; y++)
                        for (int x = x0; x < x1; x++) { all++; if (silhouette[(y * res + x) * 4 + 3] > 127) on++; }
                    a[yo * outRes + xo] = all > 0 ? on / (float)all : 0f;
                }
            int r = Math.Max(1, (int)(outRes * ShadowBlur + 0.5f));
            var tmp = new float[a.Length];
            for (int pass = 0; pass < 3; pass++) BoxBlur(a, tmp, outRes, r);
            var bytes = new byte[outRes * outRes * 4];
            for (int i = 0; i < a.Length; i++)
            {
                bytes[i * 4] = bytes[i * 4 + 1] = bytes[i * 4 + 2] = 255;
                bytes[i * 4 + 3] = (byte)(Math.Min(1f, a[i]) * 255f + 0.5f);
            }
            return bytes;
        }

        static void BoxBlur(float[] f, float[] tmp, int res, int r)
        {
            float inv = 1f / (2 * r + 1);
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float s = 0f;
                    for (int t = -r; t <= r; t++) s += f[y * res + Math.Min(res - 1, Math.Max(0, x + t))];
                    tmp[y * res + x] = s * inv;
                }
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float s = 0f;
                    for (int t = -r; t <= r; t++) s += tmp[Math.Min(res - 1, Math.Max(0, y + t)) * res + x];
                    f[y * res + x] = s * inv;
                }
        }

        // ======================================================================================
        // Sculpted renderer (Mode = Sculpted). Used only by VectorPainter.Sculpted.cs. Families pick the geometry:
        // Soft = inflated dome, Matte = gentle dome (baked food, fabric), Plastic and Paper = molded face + bevel +
        // side plane. Gloss per family comes from the Soft / Plastic / Matte FamilyLook above (Paper uses Matte).
        // Sizes are canvas units (the sprite spans 2.0; at phone size 0.1 is roughly 7-8 screen pixels).
        // ======================================================================================
        public static class Sculpt
        {
            // Contour. Full weight on normal parts; parts too thin to carry it get a proportionally thinner one,
            // so the key shaft, pencil and stems keep visible form instead of turning solid ink.
            public static float Outline = 0.08f;
            public static float ThinOutline = 0.34f;      // contour <= this fraction of the part's half-thickness

            // Molded (hard) parts: flat-ish face, rounded bevel ring, side plane below.
            public static float Bevel = 0.17f;            // visible width of the rounded edge ring (plastic)
            public static float MatteBevel = 0.18f;       // paper parts round over a wider, softer edge
            public static float BevelTilt = 76f;          // edge tilt at the outside of the ring (degrees)
            public static float MatteBevelTilt = 58f;
            public static float Wall = 0.6f;              // side plane height as a fraction of the part's depth...
            public static float WallMax = 0.28f;          // ...capped (canvas units)
            public static float EdgeRegion = 0.03f;       // a child within contour + this of the edge is a painted region
            public static float SoftWall = 0.26f, SoftWallMax = 0.12f; // inflated parts show a smaller underside

            // Soft (inflated) parts: dome over the whole face, steepest at the edge. Matte food domes more gently.
            public static float DomeTilt = 70f;
            public static float MatteDomeTilt = 56f;
            public static float BevelHold = 0.55f;        // 0 = ring eases to flat quickly, 1 = keeps its tilt longer
            public static float HotWidth = 0.045f;        // width of the gloss line along bevels (N.H band)

            // Side plane.
            public static float SideNormalZ = 0.2f;       // lower = faces further down, darker
            public static float SideDarken = 0.25f;       // extra darkening toward its bottom edge
            public static float BevelLine = 0.45f;        // dark seam where the face's bevel meets the side plane
            public static float BevelLineWidth = 0.014f;

            // Tones: lit side, base, and a deeper hue-shifted shadow, in two soft cartoon bands.
            public static float LightAmount = 0.42f;
            public static float ShadowR = 0.5f, ShadowG = 0.42f, ShadowB = 0.62f;
            public static float ShadowLift = 0.03f;
            public static float ShadowSaturation = 1.3f;
            public static int Bands = 2;
            public static float BandSoftness = 0.3f;

            // Occlusion: localized contact between attached parts matters more than gloss.
            public static float CastOffset = 0.05f, CastSoftness = 0.06f, CastStrength = 0.4f;
            public static float ContactWidth = 0.05f, ContactStrength = 0.32f;
            public static float GroundAO = 0.25f;
        }

        internal static void Light(out float lx, out float ly, out float lz)
        {
            float l = (float)Math.Sqrt(LightX * LightX + LightY * LightY + LightZ * LightZ);
            lx = LightX / l; ly = LightY / l; lz = LightZ / l;
        }
    }
}
