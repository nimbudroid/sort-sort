using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Sorting categories of every library object (CategoryLibrary ids): primary first, then secondary. Only
    /// categories an object honestly belongs to are listed; there is no implied hierarchy (FRUIT does not imply
    /// FOOD unless FOOD is listed). Secondary colours are descriptive only.
    /// </summary>
    public static partial class ObjectLibrary
    {
        // id, primary, secondaries (space separated ids), optional secondary colours.
        static readonly string[][] CategoryTable =
        {
            // Core food
            new[] { "apple", "FRUIT", "FOOD SNACKS PICNIC" },
            new[] { "banana", "FRUIT", "FOOD SNACKS BREAKFAST" },
            new[] { "strawberry", "FRUIT", "FOOD SNACKS" },
            new[] { "watermelon_slice", "FRUIT", "FOOD SUMMER PICNIC", "Green" },
            new[] { "orange", "FRUIT", "FOOD SNACKS BREAKFAST" },
            new[] { "cupcake", "SWEETS", "FOOD DESSERTS BAKERY PARTY" },
            new[] { "pizza_slice", "FOOD", "FAST_FOOD LUNCH DINNER", "Red" },
            new[] { "donut", "SWEETS", "FOOD DESSERTS BAKERY BREAKFAST" },
            new[] { "popsicle", "SWEETS", "FOOD DESSERTS SUMMER" },
            new[] { "ketchup", "FOOD", "GROCERIES FAST_FOOD" },
            // Core office
            new[] { "stapler", "OFFICE", "STATIONERY DESK_ITEMS SCHOOL" },
            new[] { "pencil", "SCHOOL", "OFFICE WRITING STATIONERY" },
            new[] { "pen", "OFFICE", "WRITING STATIONERY SCHOOL" },
            new[] { "notebook", "SCHOOL", "PAPER STATIONERY STUDY OFFICE" },
            new[] { "paper_clip", "OFFICE", "STATIONERY DESK_ITEMS" },
            new[] { "coffee_mug", "KITCHENWARE", "KITCHEN CUPS TABLEWARE DRINKS" },
            new[] { "scissors", "OFFICE", "STATIONERY SCHOOL CRAFT ART_SUPPLIES" },
            new[] { "sticky_note", "OFFICE", "PAPER STATIONERY DESK_ITEMS" },
            new[] { "calculator", "OFFICE", "SCHOOL ELECTRONICS DESK_ITEMS STUDY" },
            new[] { "eraser", "SCHOOL", "STATIONERY OFFICE DRAWING" },
            // Core everyday
            new[] { "key", "HOUSEHOLD", "" },
            new[] { "sunglasses", "ACCESSORIES", "BEACH SUMMER TRAVEL" },
            new[] { "toothbrush", "BATHROOM", "HOUSEHOLD" },
            new[] { "sock", "CLOTHING", "" },
            new[] { "shoe", "SHOES", "CLOTHING" },
            new[] { "umbrella", "ACCESSORIES", "WEATHER TRAVEL_ACCESSORIES" },
            new[] { "toy_ball", "TOYS", "BALLS PLAYGROUND OUTDOOR_TOYS" },
            new[] { "alarm_clock", "BEDROOM", "HOUSEHOLD" },
            new[] { "small_plant", "PLANTS", "NATURE DECORATION LIVING_ROOM" },
            new[] { "gift_box", "PARTY", "" },
            // Extended food
            new[] { "burger", "FAST_FOOD", "FOOD LUNCH DINNER" },
            new[] { "fries", "FAST_FOOD", "FOOD SNACKS" },
            new[] { "cookie", "SWEETS", "FOOD BAKERY SNACKS DESSERTS" },
            new[] { "ice_cream", "DESSERTS", "FOOD SWEETS SUMMER" },
            new[] { "egg", "FOOD", "BREAKFAST GROCERIES BAKING" },
            new[] { "cheese", "FOOD", "GROCERIES SNACKS PICNIC" },
            new[] { "milk_carton", "DRINKS", "FOOD GROCERIES BREAKFAST" },
            new[] { "lemon", "FRUIT", "FOOD GROCERIES" },
            new[] { "carrot", "VEGETABLES", "FOOD GROCERIES" },
            new[] { "broccoli", "VEGETABLES", "FOOD GROCERIES" },
            new[] { "avocado", "FRUIT", "FOOD GROCERIES" },
            new[] { "watermelon", "FRUIT", "FOOD SUMMER PICNIC" },
            new[] { "bread", "BAKERY", "FOOD BREAKFAST GROCERIES" },
            new[] { "cereal_box", "BREAKFAST", "FOOD GROCERIES PACKAGES" },
            new[] { "jam_jar", "FOOD", "BREAKFAST GROCERIES JARS" },
            // Extended office
            new[] { "ruler", "SCHOOL", "STATIONERY OFFICE" },
            new[] { "folder", "OFFICE", "PAPER STATIONERY SCHOOL DOCUMENTS" },
            new[] { "tape_dispenser", "OFFICE", "DESK_ITEMS STATIONERY" },
            new[] { "hole_punch", "OFFICE", "DESK_ITEMS STATIONERY" },
            new[] { "keyboard", "COMPUTERS", "ELECTRONICS OFFICE HOME_OFFICE" },
            new[] { "computer_mouse", "COMPUTERS", "ELECTRONICS OFFICE HOME_OFFICE" },
            new[] { "headphones", "ELECTRONICS", "MUSIC GADGETS" },
            new[] { "desk_lamp", "DESK_ITEMS", "OFFICE HOME_OFFICE STUDY" },
            new[] { "paper_sheet", "PAPER", "OFFICE SCHOOL STATIONERY" },
            new[] { "clipboard", "OFFICE", "PAPER STATIONERY" },
            new[] { "marker", "ART_SUPPLIES", "WRITING SCHOOL OFFICE DRAWING STATIONERY" },
            new[] { "highlighter", "STATIONERY", "WRITING SCHOOL OFFICE STUDY" },
            // Extended home
            new[] { "pillow", "BEDROOM", "HOUSEHOLD" },
            new[] { "book", "BOOKS", "SCHOOL STUDY PAPER" },
            new[] { "remote_control", "HOME_ELECTRONICS", "ELECTRONICS LIVING_ROOM MOVIES" },
            new[] { "wallet", "ACCESSORIES", "TRAVEL" },
            new[] { "phone", "PHONES", "ELECTRONICS HOME_ELECTRONICS LIVING_ROOM" },
            new[] { "hairbrush", "BATHROOM", "HOUSEHOLD" },
            new[] { "shampoo", "BATHROOM", "HOUSEHOLD" },
            new[] { "soap", "BATHROOM", "CLEANING HOUSEHOLD" },
            new[] { "towel", "BATHROOM", "HOUSEHOLD" },
            new[] { "hanger", "HOUSEHOLD", "STORAGE BEDROOM" },
            new[] { "laundry_basket", "CLEANING", "HOUSEHOLD STORAGE BATHROOM" },
            new[] { "tissue_box", "BATHROOM", "HOUSEHOLD LIVING_ROOM" },
            // Extended toys
            new[] { "teddy_bear", "TOYS", "PLUSH_TOYS ANIMALS" },
            new[] { "toy_car", "TOYS", "VEHICLES CARS" },
            new[] { "building_block", "TOYS", "BUILDING_TOYS" },
            new[] { "toy_train", "TOYS", "VEHICLES TRAINS" },
            new[] { "rubber_duck", "TOYS", "BATHROOM" },
            new[] { "yoyo", "TOYS", "" },
            new[] { "toy_robot", "TOYS", "" },
            // Extended electronics
            new[] { "smartphone", "PHONES", "ELECTRONICS GADGETS" },
            new[] { "tablet", "COMPUTERS", "ELECTRONICS GADGETS" },
            new[] { "camera", "CAMERAS", "ELECTRONICS GADGETS TRAVEL VACATION" },
            new[] { "game_controller", "GAMING", "ELECTRONICS GAMES ENTERTAINMENT" },
            new[] { "smartwatch", "GADGETS", "ELECTRONICS ACCESSORIES FITNESS" },
            new[] { "speaker", "HOME_ELECTRONICS", "ELECTRONICS MUSIC PARTY" },
            // Extended nature
            new[] { "flower", "FLOWERS", "PLANTS NATURE GARDEN" },
            new[] { "leaf", "NATURE", "PLANTS TREES SEASONS FOREST" },
            new[] { "mushroom", "NATURE", "FOREST" },
            new[] { "rock", "NATURE", "MOUNTAINS OUTDOORS" },
            new[] { "pine_cone", "NATURE", "TREES FOREST SEASONS" },
            // Later: animals
            new[] { "fish", "SEA_ANIMALS", "ANIMALS OCEAN UNDERWATER PETS" },
            new[] { "butterfly", "INSECTS", "ANIMALS GARDEN NATURE" },
            new[] { "frog", "ANIMALS", "WILD_ANIMALS NATURE" },
            new[] { "bird", "BIRDS", "ANIMALS" },
            new[] { "turtle", "ANIMALS", "PETS" },
            new[] { "cat", "PETS", "ANIMALS" },
            new[] { "dog", "PETS", "ANIMALS" },
            // Later: fantasy
            new[] { "monster", "MONSTERS", "FANTASY" },
            new[] { "alien", "SPACE", "FANTASY" },
            new[] { "ufo", "SPACE", "FANTASY" },
            new[] { "treasure_chest", "TREASURE", "PIRATES ADVENTURE" },
            new[] { "magic_wand", "MAGICAL", "FANTASY" },
            new[] { "dragon_egg", "FANTASY", "MAGICAL" },
            new[] { "wizard_hat", "MAGICAL", "FANTASY HATS" },
            new[] { "dinosaur_bone", "DINOSAURS", "SCIENCE" },
            new[] { "rocket", "SPACE", "VEHICLES SCIENCE" },
            // Kitchenware, beach and sports
            new[] { "spoon", "KITCHENWARE", "KITCHEN TABLEWARE CUTLERY COOKING" },
            new[] { "plate", "KITCHENWARE", "KITCHEN TABLEWARE PLATES" },
            new[] { "bowl", "KITCHENWARE", "KITCHEN TABLEWARE COOKING" },
            new[] { "teapot", "KITCHENWARE", "KITCHEN TABLEWARE" },
            new[] { "spatula", "COOKING_TOOLS", "KITCHENWARE KITCHEN COOKING BAKING" },
            new[] { "flip_flops", "SHOES", "BEACH SUMMER SUMMER_CLOTHING SWIMMING CLOTHING" },
            new[] { "seashell", "NATURE", "BEACH OCEAN SEASIDE" },
            new[] { "beach_ball", "TOYS", "BEACH BALLS SUMMER POOL", "Red Yellow" },
            new[] { "soccer_ball", "SPORTS", "BALL_SPORTS BALLS TEAM_SPORTS OUTDOOR_SPORTS SPORTS_EQUIPMENT" },
            new[] { "tennis_racket", "SPORTS", "BALL_SPORTS SPORTS_EQUIPMENT" },
            new[] { "dumbbell", "FITNESS", "SPORTS GYM SPORTS_EQUIPMENT" },
            // Kitchen chapter: the GDD's four kitchen categories (each object belongs to exactly one of them).
            new[] { "pepper", "VEGETABLES", "FOOD GROCERIES" },
            new[] { "corn", "VEGETABLES", "FOOD GROCERIES" },
            new[] { "water_bottle", "DRINKS", "" },
            new[] { "juice_carton", "DRINKS", "BREAKFAST GROCERIES" },
            new[] { "soda_can", "DRINKS", "CANS PARTY" },
            new[] { "fork", "KITCHENWARE", "KITCHEN TABLEWARE CUTLERY" },
            new[] { "mug", "KITCHENWARE", "KITCHEN TABLEWARE CUPS" },
            new[] { "pan", "KITCHENWARE", "KITCHEN COOKING COOKING_TOOLS" },
            // Pantry: container type (JARS / CANS / PACKAGES) and purpose (BREAKFAST / BAKING / SNACKS).
            new[] { "tin_can", "CANS", "FOOD GROCERIES DINNER" },
            new[] { "soup_can", "CANS", "FOOD GROCERIES LUNCH" },
            new[] { "pasta_box", "PACKAGES", "FOOD GROCERIES DINNER" },
            new[] { "cracker_box", "PACKAGES", "SNACKS FOOD" },
            new[] { "flour_bag", "PACKAGES", "BAKING GROCERIES" },
            new[] { "honey_jar", "JARS", "BREAKFAST FOOD" },
            new[] { "pickle_jar", "JARS", "FOOD GROCERIES" },
            new[] { "spice_jar", "JARS", "BAKING COOKING" },
        };

        static void ApplyCategories()
        {
            foreach (var row in CategoryTable)
            {
                var def = Get(row[0]);
                if (def == null) continue; // reported by the tests
                string[] secondary = row[2].Length == 0 ? new string[0] : row[2].Split(' ');
                SortColor[] colors = null;
                if (row.Length > 3)
                {
                    var names = row[3].Split(' ');
                    colors = new SortColor[names.Length];
                    for (int i = 0; i < names.Length; i++) colors[i] = (SortColor)System.Enum.Parse(typeof(SortColor), names[i]);
                }
                def.SetCategories(row[1], secondary, colors);
            }
        }

        /// <summary>Category table rows whose object id does not exist (for tests).</summary>
        public static List<string> CategoryRowsWithoutObject()
        {
            var missing = new List<string>();
            foreach (var row in CategoryTable) if (Get(row[0]) == null) missing.Add(row[0]);
            return missing;
        }
    }
}
