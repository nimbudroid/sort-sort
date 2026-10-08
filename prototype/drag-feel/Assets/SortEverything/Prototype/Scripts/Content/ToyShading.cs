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
    public static class ToyShading
    {
        /// <summary>Master switch. False = the previous flat sticker shading (one-line revert).</summary>
        public static bool Enabled = true;

        // Light rig (normalised in Normalize()): key light from the upper-left, in front of the object.
        public static float LightX = -0.45f, LightY = 0.7f, LightZ = 0.75f;

        // Volume.
        public static float MaxPartRadius = 0.6f;   // inflation radius cap for a part (canvas units, -1..1 space)
        public static float InlayBevel = 0.07f;      // printed-on parts (labels, windows) only get a small bevel
        public static float WholeBodyWeight = 0.4f;  // how much every part also follows the whole object's dome
        public static float DetailRadius = 0.1f;     // shaded decorations (seeds, sprinkles) get a tiny dome
        public static float MaxSlope = 2.4f;
        public static float BoxBevelRadius = 0.22f;  // bevel radius for parts with corners (boxes, slices)
        public static float RoundRatio = 1.07f, BoxRatio = 1.22f; // corner test thresholds (see PartRadius)
        public static float NormalSmoothing = 0.025f; // blur of the fields behind the normals (fraction of sprite)         // steepest edge tilt; lower = softer, rounder edges

        // Tone ramp: shadow -> base -> light. The front-facing middle stays at the base (bin) colour.
        public static float FrontLevel = 0.8f;       // diffuse level that maps exactly to the base colour
        public static float ShadowStart = 0.22f;     // diffuse level that maps to the full shadow tone
        public static float ShadowR = 0.6f, ShadowG = 0.5f, ShadowB = 0.7f;   // hue-shifted (cool, purple) shadow
        public static float ShadowLift = 0.035f;
        public static float ShadowSaturation = 1.3f; // shadows stay colourful (pale greens/yellows never go grey)     // keeps shadows from going muddy on dark colours
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
            { "carrot", Dome() }, { "pine_cone", Dome() }, { "fish", Dome() }, { "rubber_duck", Dome(1.2f) },
        };

        public static Material For(string id)
        {
            Material m;
            return id != null && PerObject.TryGetValue(id, out m) ? m : Default;
        }

        internal static void Light(out float lx, out float ly, out float lz)
        {
            float l = (float)Math.Sqrt(LightX * LightX + LightY * LightY + LightZ * LightZ);
            lx = LightX / l; ly = LightY / l; lz = LightZ / l;
        }
    }
}
