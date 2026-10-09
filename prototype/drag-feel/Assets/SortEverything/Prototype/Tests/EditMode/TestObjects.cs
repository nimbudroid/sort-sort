using System;
using System.Collections.Generic;
using System.IO;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    /// <summary>Builds throwaway objects, targets, boards and levels for the logic tests.</summary>
    static class TestObjects
    {
        public static ObjectDef Make(string id, SortColor color, string primary, params string[] secondary)
        {
            var d = new ObjectDef("test_" + id, id, ObjectCategory.Home, color, 1, SizeClass.Small, MaterialKind.Plastic,
                Room.LivingRoom, ContentTier.Extended, null);
            d.SetCategories(primary, secondary, null);
            return d;
        }

        public static ObjectDef Lib(string id)
        {
            var d = ObjectLibrary.Get(id);
            if (d == null) throw new ArgumentException("no library object " + id);
            return d;
        }

        /// <summary>A target: space-separated categories, optional colour, capacity in units (-1 = unlimited).</summary>
        public static TargetDef Target(string categories, int capacity = -1, SortColor color = SortColor.None)
        {
            var cats = string.IsNullOrEmpty(categories) ? new string[0] : categories.Split(' ');
            return new TargetDef { id = (categories ?? "c") + "_" + capacity + "_" + color, accepts = cats, color = color, capacity = capacity };
        }

        /// <summary>A board from compact object tokens ("apple carrot orange*") and targets.</summary>
        public static BoardDef Board(string objects, params TargetDef[] targets)
        {
            var list = new List<SpawnDef>();
            var n = new Dictionary<string, int>();
            foreach (var tok in objects.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                bool large = tok.EndsWith("*");
                string asset = large ? tok.Substring(0, tok.Length - 1) : tok;
                int k; n.TryGetValue(asset, out k); n[asset] = k + 1;
                list.Add(new SpawnDef { id = asset + "_" + (k + 1), asset = asset, units = large ? 2 : 1 });
            }
            return new BoardDef { objects = list.ToArray(), targets = targets };
        }

        public static LevelDef Level(string id, params BoardDef[] boards)
        {
            return new LevelDef { id = id, title = "Test " + id, boards = boards };
        }

        /// <summary>
        /// Reads an authored campaign file. Unity runs tests from the project root; the mono runner sets
        /// SE_CAMPAIGN_DIR.
        /// </summary>
        public static string ReadCampaignFile(string name)
        {
            string dir = Environment.GetEnvironmentVariable("SE_CAMPAIGN_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine("Assets", Path.Combine("SortEverything", Path.Combine("Prototype", Path.Combine("Resources", "Campaign"))));
            string path = Path.Combine(dir, name + ".json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}
