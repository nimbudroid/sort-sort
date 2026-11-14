using System;
using System.Collections.Generic;
using System.Linq;

namespace SortEverything.Domain
{
    /// <summary>
    /// Deterministic expansion of the exact compact Kitchen tables in GDD Section 25 into level definitions.
    /// No randomness: the same input always yields the same IDs, units, coordinates and targets.
    /// </summary>
    public static class KitchenCatalogue
    {
        public const int ContentVersion = 5;
        public const int KitchenBoardContentVersion = 4;
        public const string DestinationId = "house";
        public const string AreaId = "house_kitchen";

        public static readonly AssetDef[] Assets =
        {
            new AssetDef("AP", "apple", CategoryIds.Fruit),
            new AssetDef("BA", "banana", CategoryIds.Fruit),
            new AssetDef("OR", "orange", CategoryIds.Fruit),
            new AssetDef("ST", "strawberry", CategoryIds.Fruit),
            new AssetDef("CA", "carrot", CategoryIds.Vegetables),
            new AssetDef("PE", "pepper", CategoryIds.Vegetables),
            new AssetDef("BR", "broccoli", CategoryIds.Vegetables),
            new AssetDef("CO", "corn", CategoryIds.Vegetables),
            new AssetDef("WA", "water_bottle", CategoryIds.Drinks),
            new AssetDef("MI", "milk_carton", CategoryIds.Drinks),
            new AssetDef("JU", "juice_carton", CategoryIds.Drinks),
            new AssetDef("SO", "soda_can", CategoryIds.Drinks),
            new AssetDef("SP", "spoon", CategoryIds.Kitchenware),
            new AssetDef("FO", "fork", CategoryIds.Kitchenware),
            new AssetDef("PL", "plate", CategoryIds.Kitchenware),
            new AssetDef("MU", "mug", CategoryIds.Kitchenware),
            new AssetDef("SX", "spatula", CategoryIds.Kitchenware),
            new AssetDef("PA", "pan", CategoryIds.Kitchenware),
        };

        public static readonly AnchorDef[] Anchors =
        {
            new AnchorDef("breakfast_005", "breakfast", "kitchen_005", "woven_basket", 75),
            new AnchorDef("pantry_010", "pantry", "kitchen_010", "shelf_trim", 100),
            new AnchorDef("cooking_015", "cooking", "kitchen_015", "utensil_rail", 125),
            new AnchorDef("finished_020", "finishing", "kitchen_020", "coordinated_accent", 150),
        };

        public static readonly string[] Regions = { "breakfast", "pantry", "cooking", "finishing" };

        struct Row
        {
            public string Scene, Template, F, V, D, K;
            public bool Required;
            public Row(string scene, string template, string f, string v, string d, string k, bool required = true)
            { Scene = scene; Template = template; F = f; V = v; D = d; K = k; Required = required; }
        }

        // Section 25 "Exact board recipes". A dash means the category is absent.
        static readonly Row[] Rows =
        {
            new Row("01", "U", "AP BA OR", "-", "WA MI JU", "-"),
            new Row("02", "U", "AP BA OR ST", "CA PE BR CO", "-", "-"),
            new Row("03", "U", "ST AP BA", "CO CA PE", "SO WA JU", "-"),
            new Row("04", "U", "AP BA OR ST", "CA PE BR CO", "WA MI JU SO", "-"),
            new Row("05", "U", "AP OR ST", "-", "MI WA JU", "-"),
            new Row("06", "P1", "AP BA OR ST", "CA PE BR CO", "-", "-"),
            new Row("07", "U", "BA AP ST", "-", "JU MI WA", "-"),
            new Row("08", "P1", "ST OR BA AP", "CO BR PE CA", "-", "-"),
            new Row("09", "U", "AP BA OR", "CA PE BR", "WA MI JU", "SP FO PL"),
            new Row("10", "U", "OR ST AP", "CO PE CA", "SO WA MI", "-"),
            new Row("11", "P2", "AP BA OR", "CA PE BR", "WA MI JU", "-"),
            new Row("12", "P2", "ST OR BA", "CO BR PE", "SO JU MI", "-"),
            new Row("13", "P3", "AP OR ST", "CA BR CO", "MI WA SO", "MU SX SP"),
            new Row("14", "P4", "AP BA", "CA PE", "WA", "-"),
            new Row("15", "U", "BA OR ST", "PE BR CO", "JU MI SO", "SP FO PL"),
            new Row("16", "P4", "OR ST", "BR CO", "JU", "-"),
            new Row("17", "U", "AP ST BA", "CA CO PE", "WA SO JU", "-"),
            new Row("18", "U", "AP BA ST", "-", "MI WA JU", "PA SP PL"),
            new Row("19", "P3", "ST BA OR", "CO PE BR", "SO MI WA", "MU SX PA"),
            new Row("20a", "P4", "AP OR", "CA CO", "MI", "-", required: false),
            new Row("20b", "U", "AP", "-", "WA", "MU"),
        };

        // Section 9 scene purposes (English values; keys are scene.<levelId>).
        static readonly string[] Purposes =
        {
            "Prepare breakfast", "Stock fresh produce", "Organize picnic supplies", "Clear the preparation counter",
            "Restore breakfast corner", "Share the produce tray", "Tidy snack time", "Pack fresh produce",
            "Put utensils away", "Restore pantry corner", "Store big and small items", "Pack tomorrow supplies",
            "Make room for cookware", "Reserve a basket", "Restore cooking area", "Plan the next picnic",
            "Set the drinks station", "Prepare for baking", "Organize the last shelf", "Finish the kitchen",
        };

        static readonly HashSet<int> Deferrable = new HashSet<int> { 6, 8, 11, 12, 13, 14, 16, 19 };
        static readonly HashSet<int> Basket = new HashSet<int> { 1, 2, 5, 6, 8, 10, 11, 12, 14, 16 };
        static readonly HashSet<int> Drawer = new HashSet<int> { 9, 13, 15, 19 };

        public static readonly int[] OrdinaryScenes = { 1, 2, 3, 4, 5, 7, 9, 10, 15, 17, 18, 20 };

        public static string LevelId(int n) => $"kitchen_{n:000}";

        public static AssetDef Asset(string code) => Assets.First(a => a.Code == code);
        public static AssetDef AssetById(string id) => Assets.FirstOrDefault(a => a.AssetId == id);

        static List<LevelDef> _cached;
        public static List<LevelDef> BuildLevelsCached() => _cached ?? (_cached = BuildLevels());

        public static List<LevelDef> BuildLevels()
        {
            var levels = new List<LevelDef>();
            for (int n = 1; n <= 20; n++)
            {
                string scene = n.ToString("00");
                var rows = Rows.Where(r => r.Scene.StartsWith(scene, StringComparison.Ordinal)).ToList();
                var boards = new List<BoardDef>();
                foreach (var row in rows)
                {
                    string boardId = row.Scene.Length == 3 ? row.Scene.Substring(2) : "a";
                    boards.Add(BuildBoard(boardId, row));
                }

                string teaching = n == 1 ? TeachingIds.FirstDrag
                    : n == 6 ? TeachingIds.SharedCapacityRecovery
                    : n == 11 ? TeachingIds.WeightedUnits
                    : n == 14 ? TeachingIds.OverlappingBaskets : "";
                var anchor = Anchors.FirstOrDefault(a => a.TriggerLevelId == LevelId(n));

                levels.Add(new LevelDef
                {
                    LevelId = LevelId(n),
                    ContentVersion = ContentVersion,
                    BoardContentVersion = KitchenBoardContentVersion,
                    DestinationId = DestinationId,
                    AreaId = AreaId,
                    OrdinalInArea = n,
                    ThemeId = "kitchen",
                    PurposeKey = "scene." + LevelId(n),
                    PurposeEnglish = Purposes[n - 1],
                    Boards = boards,
                    PreviewLevelId = n < 20 ? LevelId(n + 1) : "",
                    CanDeferLevel = Deferrable.Contains(n),
                    TeachingId = teaching,
                    ProjectRegion = Regions[(n - 1) / 5],
                    TraceIndex = (n - 1) % 5,
                    ProjectAnchorId = anchor != null ? anchor.AnchorId : "",
                    JourneyMilestoneId = anchor != null ? (n == 20 ? "kitchen_ready" : anchor.AnchorId) : "",
                    FirstCompletionCoins = 50,
                    CoinRewardId = "campaign_complete:" + LevelId(n),
                    FinishMicroAction = Basket.Contains(n) ? "basket_alignment" : Drawer.Contains(n) ? "drawer_close" : "tray_settle",
                    PurposeProp = PropFor(n),
                    BeforeProfile = LevelId(n) + "_before",
                    AfterProfile = LevelId(n) + "_after",
                });
            }
            return levels;
        }

        static string PropFor(int n)
        {
            switch (n)
            {
                case 1: case 5: case 7: return "serving_cloth_tray";
                case 2: case 6: case 8: case 10: case 11: case 12: return "pantry_shelf";
                case 3: case 14: case 16: return "carry_handle_tray";
                case 4: return "prep_surface";
                case 9: case 13: case 15: case 19: return "utensil_drawer";
                case 17: return "bottle_crate";
                case 18: return "baking_mat";
                default: return "breakfast_tray";
            }
        }

        static BoardDef BuildBoard(string boardId, Row row)
        {
            string[][] arrays = { Split(row.F), Split(row.V), Split(row.D), Split(row.K) };
            string template = row.Template;

            // Create stable instances per category array (array order defines IDs and large variants).
            var perCategory = new List<List<(string id, AssetDef asset, int units, bool large)>>();
            for (int c = 0; c < 4; c++)
            {
                var list = new List<(string, AssetDef, int, bool)>();
                for (int i = 0; i < arrays[c].Length; i++)
                {
                    var asset = Asset(arrays[c][i]);
                    int units = 1;
                    if ((template == TemplateIds.P2 || template == TemplateIds.P3) && i == 0) units = 2;
                    if (template == TemplateIds.P4 && asset.CategoryId == CategoryIds.Drinks) units = 2;
                    list.Add(($"{boardId}_{CategoryIds.Letters[c]}{i + 1}", asset, units, units == 2));
                }
                perCategory.Add(list);
            }

            // Round-robin interleave F/V/D/K for source order.
            var order = new List<(string id, AssetDef asset, int units, bool large)>();
            int maxLen = perCategory.Max(l => l.Count);
            for (int i = 0; i < maxLen; i++)
                for (int c = 0; c < 4; c++)
                    if (i < perCategory[c].Count) order.Add(perCategory[c][i]);

            string grid = "grid" + order.Count;
            var slots = SourceGrid(grid);
            var objects = new List<ObjectDef>();
            for (int i = 0; i < order.Count; i++)
            {
                var o = order[i];
                objects.Add(new ObjectDef(o.id, o.asset.AssetId, o.asset.CategoryId, o.units, slots[i].x, slots[i].y, o.large, i));
            }

            var present = Enumerable.Range(0, 4).Where(c => arrays[c].Length > 0).ToList();
            var targets = new List<TargetDef>();
            string targetLayout;
            switch (template)
            {
                case TemplateIds.Unlimited:
                {
                    targetLayout = "u" + present.Count;
                    var centers = TargetCenters(targetLayout);
                    string rect = present.Count == 2 ? "wide" : present.Count == 3 ? "narrow" : "quad";
                    for (int i = 0; i < present.Count; i++)
                    {
                        int c = present[i];
                        string cat = CategoryIds.Ordered[c];
                        int count = arrays[c].Length;
                        string slotProfile = count == 1 ? "one" : count == 2 ? "two"
                            : rect == "narrow" ? (count == 3 ? "three_vertical" : "four_vertical") : "four_square";
                        targets.Add(new TargetDef(cat + "_only", new[] { cat }, -1, centers[i].x, centers[i].y, rect, slotProfile, count));
                    }
                    break;
                }
                case TemplateIds.P1:
                {
                    targetLayout = "p1";
                    var c = TargetCenters("u2");
                    targets.Add(new TargetDef("fruit_only", new[] { CategoryIds.Fruit }, 2, c[0].x, c[0].y, "wide", "two", 2));
                    targets.Add(new TargetDef("shared", new[] { CategoryIds.Fruit, CategoryIds.Vegetables }, 6, c[1].x, c[1].y, "wide", "six", 6));
                    break;
                }
                case TemplateIds.P2:
                case TemplateIds.P3:
                {
                    bool p3 = template == TemplateIds.P3;
                    targetLayout = p3 ? "p3" : "p2";
                    float[] xs = { 0.1667f, 0.5f, 0.8333f };
                    targets.Add(new TargetDef("fruit_only", new[] { CategoryIds.Fruit }, 2, xs[0], 0.16f, "narrow", "two", 2));
                    targets.Add(new TargetDef("vegetables_only", new[] { CategoryIds.Vegetables }, 2, xs[1], 0.16f, "narrow", "two", 2));
                    targets.Add(new TargetDef("drinks_only", new[] { CategoryIds.Drinks }, 2, xs[2], 0.16f, "narrow", "two", 2));
                    // Shared card top sits 8 units below the dedicated cards (bottom = 0.16*322 + 50).
                    const float regionH = 322f;
                    float top = 0.16f * regionH + 50f + 8f;
                    float h = p3 ? 204f : 152f;
                    float cy = (top + h / 2f) / regionH;
                    var accepts = p3
                        ? new[] { CategoryIds.Fruit, CategoryIds.Vegetables, CategoryIds.Drinks, CategoryIds.Kitchenware }
                        : new[] { CategoryIds.Fruit, CategoryIds.Vegetables, CategoryIds.Drinks };
                    targets.Add(new TargetDef("shared", accepts, p3 ? 10 : 6, 0.5f, cy, "full_width", p3 ? "nine" : "six", p3 ? 9 : 6));
                    break;
                }
                case TemplateIds.P4:
                {
                    targetLayout = "p4";
                    var c = TargetCenters("u3");
                    targets.Add(new TargetDef("fv", new[] { CategoryIds.Fruit, CategoryIds.Vegetables }, 2, c[0].x, c[0].y, "narrow", "two", 2));
                    targets.Add(new TargetDef("vd", new[] { CategoryIds.Vegetables, CategoryIds.Drinks }, 2, c[1].x, c[1].y, "narrow", "two", 2));
                    targets.Add(new TargetDef("fd", new[] { CategoryIds.Fruit, CategoryIds.Drinks }, 2, c[2].x, c[2].y, "narrow", "two", 2));
                    break;
                }
                default: throw new InvalidOperationException("Unknown template " + template);
            }

            return new BoardDef(boardId, row.Required, template, objects, targets, grid, targetLayout);
        }

        static string[] Split(string s) => s == "-" ? Array.Empty<string>() : s.Split(' ');

        public static List<(float x, float y)> SourceGrid(string grid)
        {
            float[] x4 = { 0.14f, 0.38f, 0.62f, 0.86f };
            float[] x3 = { 0.18f, 0.50f, 0.82f };
            float[] y3 = { 0.17f, 0.50f, 0.83f };
            float[] y2 = { 0.28f, 0.72f };
            var r = new List<(float, float)>();
            switch (grid)
            {
                case "grid12": foreach (var y in y3) foreach (var x in x4) r.Add((x, y)); break;
                case "grid8": foreach (var y in y2) foreach (var x in x4) r.Add((x, y)); break;
                case "grid9": foreach (var y in y3) foreach (var x in x3) r.Add((x, y)); break;
                case "grid6": foreach (var y in y2) foreach (var x in x3) r.Add((x, y)); break;
                case "grid5":
                    foreach (var x in x3) r.Add((x, 0.28f));
                    r.Add((0.34f, 0.72f)); r.Add((0.66f, 0.72f));
                    break;
                case "grid3": foreach (var x in x3) r.Add((x, 0.50f)); break;
                default: throw new InvalidOperationException("Unknown source grid " + grid);
            }
            return r;
        }

        public static List<(float x, float y)> TargetCenters(string layout)
        {
            switch (layout)
            {
                case "u2": return new List<(float, float)> { (0.25f, 0.5f), (0.75f, 0.5f) };
                case "u3": return new List<(float, float)> { (0.1667f, 0.5f), (0.5f, 0.5f), (0.8333f, 0.5f) };
                case "u4": return new List<(float, float)> { (0.25f, 0.25f), (0.75f, 0.25f), (0.25f, 0.75f), (0.75f, 0.75f) };
                default: throw new InvalidOperationException("Unknown target layout " + layout);
            }
        }
    }

    /// <summary>Immutable registry of installed content for one build profile.</summary>
    public sealed class Catalogue
    {
        public readonly IReadOnlyList<LevelDef> Levels;
        public readonly BuildProfile Profile;
        readonly Dictionary<string, LevelDef> _byId;

        public static readonly string[] ProfileIds = { "kitchen_gate_08", "kitchen_20" };

        public Catalogue(IReadOnlyList<LevelDef> levels, BuildProfile profile)
        {
            Levels = levels; Profile = profile;
            _byId = levels.ToDictionary(l => l.LevelId);
        }

        public static Catalogue CreateKitchen(string profileId)
        {
            var levels = KitchenCatalogue.BuildLevels();
            int count = profileId == "kitchen_gate_08" ? 8 : 20;
            var profile = new BuildProfile(profileId, levels.Take(count).Select(l => l.LevelId));
            return new Catalogue(levels, profile);
        }

        public LevelDef Level(string id) => _byId.TryGetValue(id, out var l) ? l : null;
        public bool IsInstalled(string levelId) => Profile.Contains(levelId);

        public IReadOnlyList<LevelDef> InstalledInArea(string areaId) =>
            Levels.Where(l => l.AreaId == areaId && IsInstalled(l.LevelId)).OrderBy(l => l.OrdinalInArea).ToList();

        /// <summary>Next scene in the same area, filtered to empty when outside the active manifest.</summary>
        public string PreviewFor(LevelDef level) =>
            !string.IsNullOrEmpty(level.PreviewLevelId) && IsInstalled(level.PreviewLevelId) ? level.PreviewLevelId : "";
    }
}
