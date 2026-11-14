using System.Collections.Generic;

namespace SortEverything.Domain
{
    /// <summary>English localization values. Keys are stable and separate from IDs and geometry.</summary>
    public static class Strings
    {
        static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            ["category.fruit"] = "Fruit",
            ["category.vegetables"] = "Vegetables",
            ["category.drinks"] = "Drinks",
            ["category.kitchenware"] = "Kitchenware",
            ["target.shared"] = "Shared tray",
            ["action.next"] = "Next",
            ["action.home"] = "Home",
            ["action.replay"] = "Replay",
            ["action.inspect"] = "Inspect Scene",
            ["action.back"] = "Back",
            ["action.leave_for_later"] = "Leave for later",
            ["action.leave_puzzle_for_later"] = "Leave puzzle for later",
            ["action.finish_kitchen"] = "Finish Kitchen",
            ["action.begin_puzzle"] = "Begin Puzzle",
            ["action.resume"] = "Resume",
            ["action.continue"] = "Continue",
            ["action.restart_board"] = "Restart Board",
            ["action.save_explore"] = "Save and Explore",
            ["action.save_home"] = "Save and Home",
            ["action.settings"] = "Settings",
            ["action.hint"] = "Hint",
            ["action.watch_example"] = "Watch example",
            ["action.start_sorting"] = "Start sorting",
            ["action.inspect_kitchen"] = "Inspect Kitchen",
            ["action.explore_places"] = "Explore Places",
            ["action.replay_scenes"] = "Replay Scenes",
            ["action.later_puzzles"] = "Later Puzzles",
            ["action.replay_finish"] = "Replay Finish",
            ["action.resume_puzzle"] = "Resume Puzzle",
            ["status.full"] = "Full",
            ["status.wrong_category"] = "Doesn't go here",
            ["status.saved_for_later"] = "Puzzle saved for later",
            ["status.in_progress"] = "In progress",
            ["status.later"] = "Later",
            ["status.sorted"] = "Sorted",
            ["result.sorted"] = "Sorted",
            ["result.kitchen_ready"] = "Kitchen Ready",
            ["result.kitchen_master"] = "Kitchen Master",
            ["result.sample_complete"] = "Sample complete",
            ["error.save_retry"] = "Progress could not be saved. Try again.",
            ["error.save_corrupt"] = "Saved progress could not be read. Your files were kept.",
            ["lesson.title"] = "Learn this puzzle",
            ["lesson.shared_capacity_recovery"] = "Some trays are shared. Fill the Fruit-only basket first so the shared tray keeps room for everything else. You can always move things back.",
            ["lesson.weighted_units"] = "Objects with a 2 badge are big: they use two spaces and must fit in one container.",
            ["lesson.overlapping_baskets"] = "Keep a whole basket free for the large drink.",
            ["tutorial.drag"] = "Drag each item to where it belongs",
            ["anchor.breakfast_005"] = "Breakfast corner arranged",
            ["anchor.pantry_010"] = "Pantry corner restored",
            ["anchor.cooking_015"] = "Cooking area cleared",
            ["anchor.finished_020"] = "Finished kitchen revealed",
            ["variant.woven_basket"] = "Woven basket accent",
            ["variant.shelf_trim"] = "Shelf trim finish",
            ["variant.utensil_rail"] = "Utensil rail finish",
            ["variant.coordinated_accent"] = "Coordinated kitchen accent",
            ["area.house_kitchen"] = "Kitchen",
            ["destination.house"] = "Your House",
            ["asset.apple"] = "Apple", ["asset.banana"] = "Banana", ["asset.orange"] = "Orange", ["asset.strawberry"] = "Strawberry",
            ["asset.carrot"] = "Carrot", ["asset.pepper"] = "Pepper", ["asset.broccoli"] = "Broccoli", ["asset.corn"] = "Corn",
            ["asset.water_bottle"] = "Water Bottle", ["asset.milk_carton"] = "Milk Carton", ["asset.juice_carton"] = "Juice Carton",
            ["asset.soda_can"] = "Soda Can", ["asset.spoon"] = "Spoon", ["asset.fork"] = "Fork", ["asset.plate"] = "Plate",
            ["asset.mug"] = "Mug", ["asset.spatula"] = "Spatula", ["asset.pan"] = "Pan",
        };

        public static string Get(string key)
        {
            if (key == null) return "";
            if (En.TryGetValue(key, out var v)) return v;
            if (key.StartsWith("scene."))
            {
                var level = KitchenCatalogue.BuildLevelsCached().Find(l => l.PurposeKey == key);
                if (level != null) return level.PurposeEnglish;
            }
            return key; // fallback: visible key, never a crash
        }
    }
}
