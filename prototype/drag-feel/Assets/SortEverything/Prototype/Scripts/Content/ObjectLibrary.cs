using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// The object database. To add an object: add one D(...) line here and one drawing in ObjectDrawings.
    /// Nothing else (drag, bins, rounds) needs to change.
    /// </summary>
    public static class ObjectLibrary
    {
        public static readonly List<ObjectDef> All = new List<ObjectDef>();
        static readonly Dictionary<string, ObjectDef> byId = new Dictionary<string, ObjectDef>();

        // Shorthands to keep each definition on one line.
        const ObjectCategory Food = ObjectCategory.Food, Office = ObjectCategory.Office, Home = ObjectCategory.Home,
            Toys = ObjectCategory.Toys, Elec = ObjectCategory.Electronics, Nature = ObjectCategory.Nature,
            Animals = ObjectCategory.Animals, Fantasy = ObjectCategory.Fantasy;
        const SortColor Red = SortColor.Red, Blue = SortColor.Blue, Yellow = SortColor.Yellow, Green = SortColor.Green,
            Purple = SortColor.Purple, None = SortColor.None;
        const SizeClass S = SizeClass.Small, M = SizeClass.Medium, L = SizeClass.Large;
        const MaterialKind Organic = MaterialKind.Organic, Plastic = MaterialKind.Plastic, Metal = MaterialKind.Metal,
            Paper = MaterialKind.Paper, Fabric = MaterialKind.Fabric, Glass = MaterialKind.Glass, Wood = MaterialKind.Wood,
            Rubber = MaterialKind.Rubber, Ceramic = MaterialKind.Ceramic, Stone = MaterialKind.Stone,
            Electronic = MaterialKind.Electronic, Magic = MaterialKind.Magic;
        const Room Kitchen = Room.Kitchen, OfficeRoom = Room.Office, Bathroom = Room.Bathroom, Bedroom = Room.Bedroom,
            Living = Room.LivingRoom, Playroom = Room.Playroom, Garden = Room.Garden, Outdoors = Room.Outdoors,
            FantasyRoom = Room.Fantasy;
        const ContentTier Core = ContentTier.Core, Ext = ContentTier.Extended, Later = ContentTier.Later;

        static ObjectLibrary()
        {
            // id, name, category, colour, mass 1-5, size, material, room, tier, tags...

            // ---- Core 30: food -------------------------------------------------------------
            D("apple", "Apple", Food, Red, 1, S, Organic, Kitchen, Core, "fruit", "food", "kitchen");
            D("banana", "Banana", Food, Yellow, 1, M, Organic, Kitchen, Core, "fruit", "food", "kitchen");
            D("strawberry", "Strawberry", Food, Red, 1, S, Organic, Kitchen, Core, "fruit", "food", "berry");
            D("watermelon_slice", "Watermelon Slice", Food, Red, 4, M, Organic, Kitchen, Core, "fruit", "food", "summer");
            // Orange is not one of the five bin colours, so it never appears in colour rounds (kept for category rules).
            D("orange", "Orange", Food, None, 2, S, Organic, Kitchen, Core, "fruit", "food", "citrus");
            D("cupcake", "Cupcake", Food, Red, 1, S, Organic, Kitchen, Core, "dessert", "food", "sweet");
            D("pizza_slice", "Pizza Slice", Food, Yellow, 2, M, Organic, Kitchen, Core, "fastfood", "food");
            D("donut", "Donut", Food, Purple, 1, S, Organic, Kitchen, Core, "dessert", "food", "sweet");
            D("popsicle", "Popsicle", Food, Blue, 1, S, Organic, Kitchen, Core, "dessert", "food", "frozen", "summer");
            D("ketchup", "Ketchup Bottle", Food, Red, 3, M, Plastic, Kitchen, Core, "condiment", "food", "bottle");

            // ---- Core 30: office / desk ---------------------------------------------------
            D("stapler", "Stapler", Office, Blue, 4, M, Metal, OfficeRoom, Core, "office", "desk", "tool");
            D("pencil", "Pencil", Office, Yellow, 1, M, Wood, OfficeRoom, Core, "office", "desk", "writing", "school");
            D("pen", "Pen", Office, Purple, 1, M, Plastic, OfficeRoom, Core, "office", "desk", "writing");
            D("notebook", "Notebook", Office, Blue, 2, M, Paper, OfficeRoom, Core, "office", "desk", "school", "paper");
            D("paper_clip", "Paper Clip", Office, Purple, 1, S, Metal, OfficeRoom, Core, "office", "desk");
            D("coffee_mug", "Coffee Mug", Office, Blue, 3, S, Ceramic, OfficeRoom, Core, "office", "kitchen", "drink");
            D("scissors", "Scissors", Office, Green, 2, M, Metal, OfficeRoom, Core, "office", "desk", "tool", "sharp");
            D("sticky_note", "Sticky Note", Office, Green, 1, S, Paper, OfficeRoom, Core, "office", "desk", "paper");
            D("calculator", "Calculator", Office, Green, 3, M, Electronic, OfficeRoom, Core, "office", "desk", "electronic");
            D("eraser", "Eraser", Office, Blue, 1, S, Rubber, OfficeRoom, Core, "office", "desk", "school");

            // ---- Core 30: everyday ----------------------------------------------------------
            D("key", "Key", Home, Yellow, 2, S, Metal, Living, Core, "everyday", "metal");
            D("sunglasses", "Sunglasses", Home, Purple, 1, M, Plastic, Outdoors, Core, "everyday", "wearable", "summer");
            D("toothbrush", "Toothbrush", Home, Green, 1, M, Plastic, Bathroom, Core, "everyday", "bathroom", "hygiene");
            D("sock", "Sock", Home, Red, 1, S, Fabric, Bedroom, Core, "everyday", "clothing", "laundry");
            D("shoe", "Shoe", Home, Blue, 4, L, Fabric, Bedroom, Core, "everyday", "clothing", "wearable");
            D("umbrella", "Umbrella", Home, Purple, 3, L, Fabric, Outdoors, Core, "everyday", "weather");
            D("toy_ball", "Toy Ball", Toys, Yellow, 2, M, Rubber, Playroom, Core, "toy", "round", "bouncy");
            D("alarm_clock", "Alarm Clock", Home, Red, 4, M, Metal, Bedroom, Core, "everyday", "bedroom", "time");
            D("small_plant", "Small Plant", Nature, Green, 5, M, Ceramic, Living, Core, "plant", "decor");
            D("gift_box", "Gift Box", Home, Purple, 4, M, Paper, Living, Core, "everyday", "party", "box");

            // ---- Extended: food --------------------------------------------------------------
            D("burger", "Burger", Food, None, 3, M, Organic, Kitchen, Ext, "fastfood", "food");
            D("fries", "Fries", Food, Yellow, 2, M, Organic, Kitchen, Ext, "fastfood", "food");
            D("cookie", "Cookie", Food, None, 1, S, Organic, Kitchen, Ext, "dessert", "food", "sweet");
            D("ice_cream", "Ice Cream", Food, Purple, 1, M, Organic, Kitchen, Ext, "dessert", "food", "frozen");
            D("egg", "Egg", Food, None, 1, S, Organic, Kitchen, Ext, "food", "breakfast", "breakable");
            D("cheese", "Cheese", Food, Yellow, 2, S, Organic, Kitchen, Ext, "food", "dairy");
            D("milk_carton", "Milk Carton", Food, Blue, 4, M, Paper, Kitchen, Ext, "food", "dairy", "drink");
            D("lemon", "Lemon", Food, Yellow, 1, S, Organic, Kitchen, Ext, "fruit", "food", "citrus");
            D("carrot", "Carrot", Food, None, 1, M, Organic, Kitchen, Ext, "vegetable", "food");
            D("broccoli", "Broccoli", Food, Green, 1, M, Organic, Kitchen, Ext, "vegetable", "food");
            D("avocado", "Avocado", Food, Green, 2, S, Organic, Kitchen, Ext, "fruit", "food");
            D("watermelon", "Watermelon", Food, Green, 5, L, Organic, Kitchen, Ext, "fruit", "food", "summer");
            D("bread", "Bread", Food, None, 2, M, Organic, Kitchen, Ext, "food", "bakery");
            D("cereal_box", "Cereal Box", Food, Red, 2, L, Paper, Kitchen, Ext, "food", "breakfast", "box");
            D("jam_jar", "Jam Jar", Food, Purple, 3, S, Glass, Kitchen, Ext, "food", "breakfast", "breakable");

            // ---- Extended: office -------------------------------------------------------------
            D("ruler", "Ruler", Office, Yellow, 1, L, Plastic, OfficeRoom, Ext, "office", "school", "measuring");
            D("folder", "Folder", Office, Blue, 2, L, Paper, OfficeRoom, Ext, "office", "paper");
            D("tape_dispenser", "Tape Dispenser", Office, Red, 4, M, Plastic, OfficeRoom, Ext, "office", "desk");
            D("hole_punch", "Hole Punch", Office, Purple, 4, M, Metal, OfficeRoom, Ext, "office", "desk", "tool");
            D("keyboard", "Keyboard", Office, None, 3, L, Electronic, OfficeRoom, Ext, "office", "computer", "electronic");
            D("computer_mouse", "Computer Mouse", Office, Blue, 1, S, Electronic, OfficeRoom, Ext, "office", "computer", "electronic");
            D("headphones", "Headphones", Office, Red, 2, M, Electronic, OfficeRoom, Ext, "electronic", "music", "wearable");
            D("desk_lamp", "Desk Lamp", Office, Green, 4, L, Metal, OfficeRoom, Ext, "office", "desk", "light");
            D("paper_sheet", "Paper Sheet", Office, None, 1, M, Paper, OfficeRoom, Ext, "office", "paper");
            D("clipboard", "Clipboard", Office, None, 2, L, Wood, OfficeRoom, Ext, "office", "paper");
            D("marker", "Marker", Office, Purple, 1, M, Plastic, OfficeRoom, Ext, "office", "writing", "school");
            D("highlighter", "Highlighter", Office, Yellow, 1, M, Plastic, OfficeRoom, Ext, "office", "writing", "school");

            // ---- Extended: home -----------------------------------------------------------------
            D("pillow", "Pillow", Home, Purple, 1, L, Fabric, Bedroom, Ext, "bedroom", "soft");
            D("book", "Book", Home, Red, 3, M, Paper, Living, Ext, "reading", "paper", "school");
            D("remote_control", "Remote Control", Home, None, 2, M, Electronic, Living, Ext, "electronic", "tv");
            D("wallet", "Wallet", Home, None, 1, S, Fabric, Bedroom, Ext, "everyday", "money");
            D("phone", "Telephone", Home, Red, 4, M, Plastic, Living, Ext, "electronic", "retro");
            D("hairbrush", "Hairbrush", Home, Purple, 1, M, Plastic, Bathroom, Ext, "bathroom", "hygiene");
            D("shampoo", "Shampoo Bottle", Home, Green, 3, M, Plastic, Bathroom, Ext, "bathroom", "hygiene", "bottle");
            D("soap", "Soap", Home, Blue, 1, S, Organic, Bathroom, Ext, "bathroom", "hygiene", "slippery");
            D("towel", "Towel", Home, Yellow, 2, M, Fabric, Bathroom, Ext, "bathroom", "laundry", "soft");
            D("hanger", "Hanger", Home, None, 1, M, Wood, Bedroom, Ext, "laundry", "clothing");
            D("laundry_basket", "Laundry Basket", Home, Blue, 3, L, Plastic, Bathroom, Ext, "laundry", "container");
            D("tissue_box", "Tissue Box", Home, Green, 1, M, Paper, Living, Ext, "bathroom", "paper", "box");

            // ---- Extended: toys -----------------------------------------------------------------
            D("teddy_bear", "Teddy Bear", Toys, None, 2, M, Fabric, Playroom, Ext, "toy", "soft");
            D("toy_car", "Toy Car", Toys, Red, 3, M, Metal, Playroom, Ext, "toy", "vehicle");
            D("building_block", "Building Block", Toys, Yellow, 1, S, Plastic, Playroom, Ext, "toy", "construction");
            D("toy_train", "Toy Train", Toys, Blue, 4, M, Wood, Playroom, Ext, "toy", "vehicle");
            D("rubber_duck", "Rubber Duck", Toys, Yellow, 1, S, Rubber, Bathroom, Ext, "toy", "bath", "floats");
            D("yoyo", "Yo-Yo", Toys, Red, 1, S, Plastic, Playroom, Ext, "toy", "round");
            D("toy_robot", "Toy Robot", Toys, Blue, 3, M, Metal, Playroom, Ext, "toy", "robot");

            // ---- Extended: electronics ------------------------------------------------------------
            D("smartphone", "Smartphone", Elec, None, 2, S, Electronic, Living, Ext, "electronic", "screen");
            D("tablet", "Tablet", Elec, None, 3, L, Electronic, Living, Ext, "electronic", "screen");
            D("camera", "Camera", Elec, None, 3, M, Electronic, Living, Ext, "electronic", "photo");
            D("game_controller", "Game Controller", Elec, Purple, 2, M, Electronic, Living, Ext, "electronic", "game");
            D("smartwatch", "Smartwatch", Elec, None, 1, S, Electronic, Bedroom, Ext, "electronic", "wearable", "time");
            D("speaker", "Speaker", Elec, None, 4, M, Electronic, Living, Ext, "electronic", "music");

            // ---- Extended: nature -------------------------------------------------------------------
            D("flower", "Flower", Nature, Purple, 1, M, Organic, Garden, Ext, "plant", "garden");
            D("leaf", "Leaf", Nature, Green, 1, S, Organic, Garden, Ext, "plant", "autumn");
            D("mushroom", "Mushroom", Nature, Red, 1, S, Organic, Outdoors, Ext, "plant", "forest");
            D("rock", "Rock", Nature, None, 5, M, Stone, Outdoors, Ext, "stone", "heavy");
            D("pine_cone", "Pine Cone", Nature, None, 1, S, Wood, Outdoors, Ext, "plant", "forest");

            // ---- Later: animals ---------------------------------------------------------------------
            D("fish", "Fish", Animals, Blue, 2, M, Organic, Outdoors, Later, "animal", "water", "slippery");
            D("butterfly", "Butterfly", Animals, Purple, 1, M, Organic, Garden, Later, "animal", "insect", "flying");
            D("frog", "Frog", Animals, Green, 2, S, Organic, Garden, Later, "animal", "pond", "bouncy");
            D("bird", "Bird", Animals, Red, 1, S, Organic, Outdoors, Later, "animal", "flying");
            D("turtle", "Turtle", Animals, Green, 4, M, Organic, Garden, Later, "animal", "pond", "slow");
            D("cat", "Cat", Animals, None, 3, M, Organic, Living, Later, "animal", "pet");
            D("dog", "Dog", Animals, None, 4, L, Organic, Living, Later, "animal", "pet");

            // ---- Later: fantasy / absurd --------------------------------------------------------------
            D("monster", "Monster", Fantasy, Purple, 3, M, Organic, FantasyRoom, Later, "fantasy", "living");
            D("alien", "Alien", Fantasy, Green, 2, M, Organic, FantasyRoom, Later, "fantasy", "space", "living");
            D("ufo", "UFO", Fantasy, None, 3, M, Metal, FantasyRoom, Later, "fantasy", "space", "vehicle");
            D("treasure_chest", "Treasure Chest", Fantasy, None, 5, L, Wood, FantasyRoom, Later, "fantasy", "pirate", "container");
            D("magic_wand", "Magic Wand", Fantasy, Yellow, 1, M, Magic, FantasyRoom, Later, "fantasy", "magic");
            D("dragon_egg", "Dragon Egg", Fantasy, Red, 4, M, Stone, FantasyRoom, Later, "fantasy", "dragon", "breakable");
            D("wizard_hat", "Wizard Hat", Fantasy, Blue, 1, L, Fabric, FantasyRoom, Later, "fantasy", "magic", "wearable");
            D("dinosaur_bone", "Dinosaur Bone", Fantasy, None, 3, L, Stone, FantasyRoom, Later, "fantasy", "fossil");
            D("rocket", "Rocket", Fantasy, Red, 3, L, Metal, FantasyRoom, Later, "fantasy", "space", "vehicle");
        }

        static void D(string id, string name, ObjectCategory category, SortColor color, int mass, SizeClass size,
            MaterialKind material, Room room, ContentTier tier, params string[] tags)
        {
            var def = new ObjectDef(id, name, category, color, mass, size, material, room, tier, tags);
            All.Add(def);
            byId[id] = def;
        }

        public static ObjectDef Get(string id)
        {
            ObjectDef d;
            return byId.TryGetValue(id, out d) ? d : null;
        }

        /// <summary>Objects that can appear in a colour round: in the pool, bin-coloured and with a drawing.</summary>
        public static List<ObjectDef> ColorSortable(ObjectPool pool, SortColor color)
        {
            var list = new List<ObjectDef>();
            for (int i = 0; i < All.Count; i++)
            {
                var d = All[i];
                if (d.sortColor == color && d.InPool(pool) && ObjectDrawings.Has(d.id)) list.Add(d);
            }
            return list;
        }
    }
}
