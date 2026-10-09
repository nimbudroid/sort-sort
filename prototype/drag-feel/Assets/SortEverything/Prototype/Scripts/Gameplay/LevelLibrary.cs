using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// The authored levels, in playlist order. Every entry is checked by LevelValidator in the EditMode tests:
    /// each object must match exactly one bin. Timer values are starting points for tuning.
    /// </summary>
    public static class LevelLibrary
    {
        const SortColor Red = SortColor.Red, Blue = SortColor.Blue, Yellow = SortColor.Yellow, Green = SortColor.Green;

        // Category bins use colours that are not rule colours, so they never read as "sort by colour".
        const string Orange = "F28C28", Teal = "22B8B0", Pink = "FF6FAE", Wood = "B9875A";

        public static readonly List<LevelDef> Playlist = new List<LevelDef>
        {
            // 1 - design level 1: colour sorting, 2 per bin.
            new LevelDef("L01_colors_rbg", 1, 25f,
                new[] { BinDef.Color(Red), BinDef.Color(Blue), BinDef.Color(Green) },
                new[] { "apple", "alarm_clock", "notebook", "shoe", "calculator", "small_plant" }),

            // 2 - design level 5: colour sorting, more objects.
            new LevelDef("L02_colors_ybr", 5, 22f,
                new[] { BinDef.Color(Yellow), BinDef.Color(Blue), BinDef.Color(Red) },
                new[] { "banana", "pencil", "toy_ball", "stapler", "popsicle", "coffee_mug", "strawberry", "sock" }),

            // 3 - design level 8: category sorting introduced with tier 1 categories.
            new LevelDef("L03_fruit_toys_kitchen", 8, 22f,
                new[] { BinDef.Category("FRUIT", Orange), BinDef.Category("TOYS", Pink), BinDef.Category("KITCHEN", Teal) },
                new[] { "apple", "banana", "orange", "toy_ball", "teddy_bear", "toy_car", "plate", "spoon" }),

            // 4 - design level 12: a wider mix of categories.
            new LevelDef("L04_beach_office_sports", 12, 20f,
                new[] { BinDef.Category("BEACH", Teal), BinDef.Category("OFFICE", Wood), BinDef.Category("SPORTS", Orange) },
                new[] { "sunglasses", "flip_flops", "beach_ball", "stapler", "paper_clip", "sticky_note",
                        "soccer_ball", "tennis_racket", "dumbbell" }),

            // 5 - design level 16: colour + category. Every object fits exactly one bin (no unsortable distractors).
            new LevelDef("L05_red_fruit_blue_toys_yellow_kitchen", 16, 20f,
                new[] { BinDef.Both(Red, "FRUIT"), BinDef.Both(Blue, "TOYS"), BinDef.Both(Yellow, "KITCHEN") },
                new[] { "apple", "strawberry", "watermelon_slice", "toy_train", "toy_robot", "beach_ball",
                        "bowl", "teapot", "spatula" }),

            // 6 - design level 20: overlapping categories; no beach ball (it is TOYS and BEACH).
            new LevelDef("L06_kitchenware_toys_beach", 20, 22f,
                new[] { BinDef.Category("KITCHENWARE", Teal), BinDef.Category("TOYS", Pink), BinDef.Category("BEACH", Orange) },
                new[] { "plate", "coffee_mug", "spoon", "bowl", "teddy_bear", "toy_car", "toy_robot",
                        "sunglasses", "flip_flops", "seashell" }),

            // 7 - design level 25: bins of different rule shapes; no blue toys or blue fruit.
            new LevelDef("L07_fruit_blue_toys", 25, 18f,
                new[] { BinDef.Category("FRUIT", Orange), BinDef.Color(Blue), BinDef.Category("TOYS", Pink) },
                new[] { "apple", "banana", "strawberry", "notebook", "stapler", "coffee_mug", "shoe",
                        "toy_ball", "teddy_bear", "toy_car" }),

            // 8 - design level 30: four bins; no sports balls tagged TOYS.
            new LevelDef("L08_food_toys_sports_office", 30, 22f,
                new[] { BinDef.Category("FOOD", Orange), BinDef.Category("TOYS", Pink), BinDef.Category("SPORTS", Teal),
                        BinDef.Category("OFFICE", Wood) },
                new[] { "pizza_slice", "cupcake", "apple", "teddy_bear", "toy_car", "building_block",
                        "soccer_ball", "tennis_racket", "dumbbell", "stapler", "paper_clip", "sticky_note" }),
        };

        public static LevelDef Get(string id)
        {
            for (int i = 0; i < Playlist.Count; i++) if (Playlist[i].id == id) return Playlist[i];
            return null;
        }
    }
}
