using System;
using System.Collections.Generic;
using System.Linq;

namespace SortEverything.Domain
{
    // ------------------------------------------------------------------------------------------------
    // Authored content as plain data (Resources/SortEverything/Content/*.json). These DTOs are engine-free
    // so the Domain validator and level builder can use them; Runtime/Presentation parse the JSON.
    // See Docs/ContentAuthoring.md.
    // ------------------------------------------------------------------------------------------------

    [Serializable]
    public sealed class ObjectData
    {
        public string id;
        public string code;          // two-letter code used by board recipes (GDD Section 25 style)
        public string category;      // a CategoryIds value: sorting correctness comes only from this
        public string color = "#CCCCCC";
        public float relativeSize;   // 1.0 = a typical fruit; 0 means missing (validator error)
        public string material;      // fruit | plastic | carton | metal | ceramic | glass | wood | paper
        public string[] tags = Array.Empty<string>();
        public string art = "";      // optional Resources path of a sprite; empty = Art/<id>.png or the built-in sculpted drawing
    }

    [Serializable] public sealed class ObjectsFile { public ObjectData[] objects = Array.Empty<ObjectData>(); }

    [Serializable]
    public sealed class ContainerArchetypeData
    {
        public string id;
        public string label;
        public string style;         // basket | fridge | crisper | drawer | rack | tray (procedural looks)
        public string body = "#C9A27A", inner = "#F1E2C8", accent = "#9C6B3E", light = "#FFF6E6";
        public string pattern = "";  // weave | slats | ""
        public string fillLayout = "grid";
        public string sound = "wood";
        public float lipShare = 0.18f;
        public string backSprite = "";  // optional Resources sprite paths that replace the procedural look
        public string frontSprite = "";
    }

    [Serializable] public sealed class CategoryArchetype { public string category; public string archetype; }

    [Serializable]
    public sealed class ContainersFile
    {
        public ContainerArchetypeData[] archetypes = Array.Empty<ContainerArchetypeData>();
        public CategoryArchetype[] categoryArchetypes = Array.Empty<CategoryArchetype>();
        public string sharedArchetype = "";

        /// <summary>Single-category bins use that category's archetype; bins accepting several kinds use the shared one.</summary>
        public ContainerArchetypeData ForAccepts(IReadOnlyList<string> accepts)
        {
            string id = accepts.Count == 1
                ? categoryArchetypes.FirstOrDefault(c => c.category == accepts[0])?.archetype
                : sharedArchetype;
            return archetypes.FirstOrDefault(a => a.id == id);
        }
    }

    [Serializable]
    public sealed class MessData
    {
        public bool enabled = true;
        public float density = 1f;   // decals per item
        public string[] decals = { "crumbs", "drip", "ring" };
    }

    [Serializable] public sealed class LevelMessData { public string levelId; public float density = -1f; public string[] decals = Array.Empty<string>(); }

    [Serializable]
    public sealed class AreaData
    {
        public string id;
        public string name;
        public string surface = "butcher_block";
        public string surfaceColor = "#A87653"; // base color of the counter (procedural surfaces + contrast check)
        public string surfaceTint = "#FFFFFF";
        public string wall = "subway_tile";
        public string cabinet = "#9FB9AA";
        public string cabinetShade = "#7F9C8C";
        public string lip = "#A9784A";
        public MessData mess = new MessData();
        public LevelMessData[] levelMess = Array.Empty<LevelMessData>();

        public MessData MessFor(string levelId)
        {
            var o = levelMess.FirstOrDefault(m => m.levelId == levelId);
            if (o == null) return mess;
            return new MessData
            {
                enabled = mess.enabled,
                density = o.density >= 0 ? o.density : mess.density,
                decals = o.decals != null && o.decals.Length > 0 ? o.decals : mess.decals,
            };
        }
    }

    [Serializable] public sealed class AreasFile { public AreaData[] areas = Array.Empty<AreaData>(); }

    // ---------- Data levels (added after the GDD's code-generated Kitchen campaign) ----------

    [Serializable]
    public sealed class DataTarget
    {
        public string id;
        public string[] accepts = Array.Empty<string>();
        public int capacity = -1;    // -1 = unlimited
        public float x = 0.5f, y = 0.5f;
        public string rectProfile = "wide";
        public string slotProfile = "four_square";
        public int slots = 4;
    }

    [Serializable]
    public sealed class DataBoard
    {
        public string id = "a";
        public bool required = true;
        public string template = TemplateIds.Unlimited;  // a rule shape the existing RuleEvaluator understands
        public string[] objects = Array.Empty<string>(); // object ids; append ":2" for a two-unit (large) instance
        public DataTarget[] targets = Array.Empty<DataTarget>();
    }

    [Serializable]
    public sealed class DataLevel
    {
        public string levelId;
        public string areaId = KitchenCatalogue.AreaId;
        public string purpose;
        public string instruction = "";
        public string region = "finishing";
        public string finishMicroAction = "tray_settle";
        public string purposeProp = "breakfast_tray";
        public bool canDefer;
        public DataBoard[] boards = Array.Empty<DataBoard>();
    }

    [Serializable] public sealed class LevelsFile { public DataLevel[] levels = Array.Empty<DataLevel>(); }

    /// <summary>
    /// Turns data levels into the same LevelDef model the code-built Kitchen uses, so the single rule evaluator,
    /// level flow and board layout handle them unchanged. Ordinals continue after the existing levels of the area.
    /// Data levels grant no coins and set no anchors or journey milestones (the GDD economy stays untouched).
    /// </summary>
    public static class DataLevelBuilder
    {
        public static List<LevelDef> Build(LevelsFile file, IReadOnlyDictionary<string, ObjectData> objects, int firstOrdinal)
        {
            var result = new List<LevelDef>();
            if (file?.levels == null) return result;
            int ordinal = firstOrdinal;
            foreach (var dl in file.levels)
            {
                var boards = new List<BoardDef>();
                foreach (var db in dl.boards)
                {
                    var objs = new List<ObjectDef>();
                    var grid = KitchenCatalogue.SourceGridFor(db.objects.Length);
                    for (int i = 0; i < db.objects.Length; i++)
                    {
                        var parts = db.objects[i].Split(':');
                        if (!objects.TryGetValue(parts[0], out var od)) throw new InvalidOperationException($"{dl.levelId}: unknown object {parts[0]}");
                        int units = parts.Length > 1 && int.TryParse(parts[1], out var u) ? u : 1;
                        objs.Add(new ObjectDef($"{db.id}_o{i + 1}", od.id, od.category, units, grid[i].x, grid[i].y, units > 1, i));
                    }
                    var targets = db.targets.Select(t => new TargetDef(t.id, t.accepts, t.capacity, t.x, t.y, t.rectProfile, t.slotProfile, t.slots)).ToList();
                    boards.Add(new BoardDef(db.id, db.required, db.template, objs, targets, "grid" + objs.Count, "data"));
                }
                result.Add(new LevelDef
                {
                    LevelId = dl.levelId,
                    ContentVersion = KitchenCatalogue.ContentVersion,
                    BoardContentVersion = KitchenCatalogue.KitchenBoardContentVersion,
                    DestinationId = KitchenCatalogue.DestinationId,
                    AreaId = dl.areaId,
                    OrdinalInArea = ordinal++,
                    ThemeId = "kitchen",
                    PurposeKey = "scene." + dl.levelId,
                    PurposeEnglish = dl.purpose,
                    Boards = boards,
                    PreviewLevelId = "",
                    CanDeferLevel = dl.canDefer,
                    TeachingId = "",
                    ProjectRegion = dl.region,
                    TraceIndex = 0,
                    ProjectAnchorId = "",
                    JourneyMilestoneId = "",
                    FirstCompletionCoins = 0,
                    CoinRewardId = "campaign_complete:" + dl.levelId,
                    FinishMicroAction = dl.finishMicroAction,
                    PurposeProp = dl.purposeProp,
                    BeforeProfile = dl.levelId + "_before",
                    AfterProfile = dl.levelId + "_after",
                    InstructionOverride = dl.instruction ?? "",
                });
            }
            return result;
        }
    }
}
