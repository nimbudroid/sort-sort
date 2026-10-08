using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>Material family for the Molded renderer. See ToyShading.Soft / Plastic / Matte for the looks.</summary>
    public enum ToyFamily { Soft, Plastic, Matte }

    /// <summary>
    /// Which material family each library object (and, where it matters, each part) is made of.
    /// Rendering data only, keyed by object id; ObjectLibrary / ObjectDefs are untouched.
    /// Resolution order: per-part override, then per-object override, then the category default.
    /// Part indices are the drawing's draw order in ObjectDrawings (0 = first shape added).
    /// </summary>
    public static class ObjectMaterials
    {
        static readonly Dictionary<ObjectCategory, ToyFamily> byCategory = new Dictionary<ObjectCategory, ToyFamily>
        {
            { ObjectCategory.Food, ToyFamily.Soft },
            { ObjectCategory.Office, ToyFamily.Plastic },
            { ObjectCategory.Home, ToyFamily.Plastic },
            { ObjectCategory.Toys, ToyFamily.Plastic },
            { ObjectCategory.Electronics, ToyFamily.Plastic },
            { ObjectCategory.Nature, ToyFamily.Soft },
            { ObjectCategory.Animals, ToyFamily.Soft },
            { ObjectCategory.Fantasy, ToyFamily.Plastic },
        };

        static readonly Dictionary<string, ToyFamily> byObject = new Dictionary<string, ToyFamily>
        {
            // Food that is a container or packaging, not an inflated food mass.
            { "ketchup", ToyFamily.Plastic }, { "milk_carton", ToyFamily.Plastic }, { "jam_jar", ToyFamily.Plastic },
            { "cereal_box", ToyFamily.Matte },
            // Baked / dry food.
            { "pizza_slice", ToyFamily.Matte }, { "bread", ToyFamily.Matte }, { "cookie", ToyFamily.Matte },
            { "burger", ToyFamily.Matte }, { "fries", ToyFamily.Matte },
            // Paper and fabric.
            { "paper_sheet", ToyFamily.Matte }, { "sticky_note", ToyFamily.Matte }, { "clipboard", ToyFamily.Matte },
            { "sock", ToyFamily.Matte }, { "towel", ToyFamily.Matte }, { "teddy_bear", ToyFamily.Matte },
            { "wallet", ToyFamily.Matte }, { "wizard_hat", ToyFamily.Matte },
            // Inflated / squishy.
            { "pillow", ToyFamily.Soft }, { "toy_ball", ToyFamily.Soft }, { "rubber_duck", ToyFamily.Soft },
            { "soap", ToyFamily.Soft }, { "dragon_egg", ToyFamily.Soft }, { "monster", ToyFamily.Soft },
            { "alien", ToyFamily.Soft },
            // Nature that is hard and dry.
            { "rock", ToyFamily.Matte }, { "pine_cone", ToyFamily.Matte }, { "dinosaur_bone", ToyFamily.Matte },
        };

        static readonly Dictionary<string, Dictionary<int, ToyFamily>> byPart = new Dictionary<string, Dictionary<int, ToyFamily>>
        {
            // Cupcake: paper liner (part 0) is matte under the inflated frosting.
            { "cupcake", new Dictionary<int, ToyFamily> { { 0, ToyFamily.Matte } } },
            // Donut: baked dough ring (part 0) is matte, the frosting on top stays soft and glossy.
            { "donut", new Dictionary<int, ToyFamily> { { 0, ToyFamily.Matte } } },
            // Popsicle stick (part 0) is wood.
            { "popsicle", new Dictionary<int, ToyFamily> { { 0, ToyFamily.Matte } } },
            // Pencil: sharpened wood cone (part 0) is matte; barrel, ferrule and eraser are painted plastic.
            { "pencil", new Dictionary<int, ToyFamily> { { 0, ToyFamily.Matte } } },
            // Ice cream: wafer cone (part 0) is matte under the soft scoop.
            { "ice_cream", new Dictionary<int, ToyFamily> { { 0, ToyFamily.Matte } } },
        };

        /// <summary>Applies every per-object rendering parameter to a painter before it is baked.</summary>
        public static void Configure(VectorPainter painter, ObjectDef def)
        {
            if (painter == null || def == null) return;
            var m = ToyShading.For(def.id);        // Painted-pass material (gloss, volume, dome)
            painter.Gloss = m.gloss;
            painter.Volume = m.volume;
            painter.Dome = m.dome;
            painter.Family = For(def);
            painter.PartFamilies = Parts(def.id);
        }

        public static ToyFamily For(ObjectDef def)
        {
            if (def == null) return ToyFamily.Plastic;
            ToyFamily f;
            if (byObject.TryGetValue(def.id, out f)) return f;
            return byCategory.TryGetValue(def.category, out f) ? f : ToyFamily.Plastic;
        }

        /// <summary>Per-part overrides for an object, or null.</summary>
        public static Dictionary<int, ToyFamily> Parts(string id)
        {
            Dictionary<int, ToyFamily> parts;
            return id != null && byPart.TryGetValue(id, out parts) ? parts : null;
        }
    }
}
