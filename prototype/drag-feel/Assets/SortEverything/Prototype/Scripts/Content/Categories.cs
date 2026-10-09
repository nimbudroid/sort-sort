using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// One sorting category ("FRUIT", "KITCHENWARE"). Plain data. Categories are flat: there is no hierarchy,
    /// so an object belongs to a category only when that category is listed on the object.
    /// </summary>
    public sealed class CategoryDef
    {
        public readonly string id;      // stable id, e.g. "FAST_FOOD"
        public readonly string label;   // what a bin shows, e.g. "FAST FOOD"
        public readonly int tier;       // 1 = obvious (teaches category sorting) .. 3 = advanced / thematic
        public readonly string group;   // e.g. "Food & drink"
        public readonly string icon;    // optional icon key; null = none yet (labels never rely on icons)

        public CategoryDef(string id, string label, int tier, string group, string icon = null)
        {
            this.id = id;
            this.label = label;
            this.tier = tier;
            this.group = group;
            this.icon = icon;
        }

        public override string ToString() { return id; }
    }

    /// <summary>
    /// The category library. Registering a category does not mean objects or levels exist for it yet; see
    /// <see cref="ObjectLibrary"/> for which objects carry which categories.
    /// </summary>
    public static class CategoryLibrary
    {
        public static readonly List<CategoryDef> All = new List<CategoryDef>();
        static readonly Dictionary<string, CategoryDef> byId = new Dictionary<string, CategoryDef>();

        public const string ThematicGroup = "Thematic";

        // Tier 1: extremely obvious, used to teach category sorting.
        static readonly string[] Tier1 =
            { "FRUIT", "TOYS", "KITCHEN", "CLOTHING", "SPORTS", "BEACH", "ANIMALS", "OFFICE", "VEHICLES", "FOOD" };

        // Tier 3: advanced or thematic. Anything not tier 1 or 3 is tier 2, except the Thematic group (tier 3).
        static readonly string[] Tier3 =
            { "PICNIC", "VACATION", "ADVENTURE", "SPACE", "SCIENCE", "FANTASY", "PIRATES", "MONSTERS", "TREASURE",
              "UNDERWATER", "DINOSAURS" };

        static CategoryLibrary()
        {
            Group("Food & drink", "FOOD", "FRUIT", "VEGETABLES", "SWEETS", "DESSERTS", "SNACKS", "FAST FOOD", "BAKERY",
                "BREAKFAST", "LUNCH", "DINNER", "DRINKS", "GROCERIES", "PICNIC");
            Group("Kitchen & cooking", "KITCHEN", "KITCHENWARE", "COOKING", "TABLEWARE", "CUTLERY", "PLATES", "CUPS",
                "COOKING TOOLS", "BAKING");
            Group("Pantry & storage", "JARS", "CANS", "PACKAGES");
            Group("Toys & play", "TOYS", "BALLS", "BOARD GAMES", "PUZZLES", "DOLLS", "PLUSH TOYS", "ACTION FIGURES",
                "BUILDING TOYS", "OUTDOOR TOYS", "PLAYGROUND");
            Group("School & office", "SCHOOL", "OFFICE", "STATIONERY", "WRITING", "PAPER", "BOOKS", "DESK ITEMS",
                "ART SUPPLIES", "STUDY");
            Group("Clothing & personal", "CLOTHING", "SHOES", "ACCESSORIES", "JEWELRY", "BAGS", "HATS", "SUMMER CLOTHING",
                "WINTER CLOTHING", "TRAVEL ACCESSORIES");
            Group("Sports", "SPORTS", "BALL SPORTS", "FITNESS", "GYM", "WATER SPORTS", "OUTDOOR SPORTS", "TEAM SPORTS",
                "SPORTS EQUIPMENT");
            Group("Beach & summer", "BEACH", "SUMMER", "SWIMMING", "POOL", "SEASIDE", "SUNNY DAY", "WATER ACTIVITIES");
            Group("Home", "HOUSEHOLD", "LIVING ROOM", "BEDROOM", "BATHROOM", "CLEANING", "STORAGE", "DECORATION",
                "FURNITURE", "HOME OFFICE");
            Group("Garden & outdoors", "GARDEN", "GARDEN TOOLS", "OUTDOORS", "CAMPING");
            Group("Nature", "NATURE", "PLANTS", "FLOWERS", "TREES", "FOREST", "MOUNTAINS", "OCEAN", "WEATHER", "SEASONS");
            Group("Animals", "ANIMALS", "PETS", "FARM ANIMALS", "WILD ANIMALS", "SEA ANIMALS", "BIRDS", "INSECTS",
                "DINOSAURS");
            Group("Transport", "TRANSPORT", "VEHICLES", "CARS", "TRAINS", "AIRPLANES", "BOATS", "BICYCLES", "MOTORCYCLES");
            Group("Travel", "TRAVEL", "LUGGAGE", "DOCUMENTS", "VACATION", "AIRPORT", "ROAD TRIP");
            Group("Electronics", "ELECTRONICS", "GADGETS", "COMPUTERS", "PHONES", "CAMERAS", "GAMING", "HOME ELECTRONICS");
            Group("Tools & workshop", "TOOLS", "HARDWARE", "WORKSHOP", "BUILDING", "REPAIR");
            Group("Music & entertainment", "MUSIC", "MUSICAL INSTRUMENTS", "PARTY", "ENTERTAINMENT", "GAMES", "MOVIES");
            Group("Art & creativity", "ART", "PAINTING", "DRAWING", "CRAFT", "SEWING", "DIY");
            Group("Space & science", "SPACE", "ASTRONOMY", "SCIENCE", "LABORATORY", "PLANETS");
            Group(ThematicGroup, "MAGICAL", "FANTASY", "MONSTERS", "PIRATES", "SUPERHEROES", "ADVENTURE", "TREASURE",
                "UNDERWATER");
        }

        static void Group(string group, params string[] labels)
        {
            foreach (var label in labels)
            {
                string id = IdFor(label);
                if (byId.ContainsKey(id)) continue; // a name listed under two groups is still one category
                int tier = System.Array.IndexOf(Tier1, id) >= 0 ? 1
                    : System.Array.IndexOf(Tier3, id) >= 0 || group == ThematicGroup ? 3
                    : 2;
                var def = new CategoryDef(id, label, tier, group);
                All.Add(def);
                byId[id] = def;
            }
        }

        /// <summary>"FAST FOOD" -> "FAST_FOOD".</summary>
        public static string IdFor(string label) { return label.Replace(' ', '_'); }

        public static CategoryDef Get(string id)
        {
            CategoryDef c;
            return id != null && byId.TryGetValue(id, out c) ? c : null;
        }

        public static bool Exists(string id) { return id != null && byId.ContainsKey(id); }
    }
}
