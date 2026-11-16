using System;
using System.Collections.Generic;
using System.Linq;

namespace SortEverything.Domain
{
    /// <summary>Stable category IDs (Section 4). Localized labels live in Strings.</summary>
    public static class CategoryIds
    {
        public const string Fruit = "fruit";
        public const string Vegetables = "vegetables";
        public const string Drinks = "drinks";
        public const string Kitchenware = "kitchenware";
        public static readonly string[] Ordered = { Fruit, Vegetables, Drinks, Kitchenware };
        public static readonly char[] Letters = { 'f', 'v', 'd', 'k' };
    }

    public static class TemplateIds
    {
        public const string Unlimited = "U";
        public const string P1 = "P1";
        public const string P2 = "P2";
        public const string P3 = "P3";
        public const string P4 = "P4";
    }

    public static class RecordRules
    {
        public const string CleanSort = "clean_sort";
        public const string UnassistedPlanner = "unassisted_planner";
    }

    public static class TeachingIds
    {
        public const string FirstDrag = "first_drag";
        public const string SharedCapacityRecovery = "shared_capacity_recovery";
        public const string WeightedUnits = "weighted_units";
        public const string OverlappingBaskets = "overlapping_baskets";

        /// <summary>Lessons a template requires, in offer order (Section 11).</summary>
        public static IReadOnlyList<string> RequiredFor(string templateId)
        {
            switch (templateId)
            {
                case TemplateIds.P1: return new[] { SharedCapacityRecovery };
                case TemplateIds.P2:
                case TemplateIds.P3: return new[] { SharedCapacityRecovery, WeightedUnits };
                case TemplateIds.P4: return new[] { WeightedUnits, OverlappingBaskets };
                default: return Array.Empty<string>();
            }
        }
    }

    public sealed class AssetDef
    {
        public readonly string AssetId;
        public readonly string CategoryId;
        public readonly string Code;
        public AssetDef(string code, string assetId, string categoryId) { Code = code; AssetId = assetId; CategoryId = categoryId; }
    }

    public sealed class ObjectDef
    {
        public readonly string InstanceId;
        public readonly string AssetId;
        public readonly string CategoryId;
        public readonly int Units;
        public readonly float SourceX;
        public readonly float SourceY;
        public readonly bool Large;
        /// <summary>Index in the authored source order; used for before-profile rotation cycling.</summary>
        public readonly int SourceIndex;

        public ObjectDef(string instanceId, string assetId, string categoryId, int units, float sx, float sy, bool large, int sourceIndex)
        {
            InstanceId = instanceId; AssetId = assetId; CategoryId = categoryId; Units = units;
            SourceX = sx; SourceY = sy; Large = large; SourceIndex = sourceIndex;
        }
    }

    public sealed class TargetDef
    {
        public readonly string TargetId;
        public readonly IReadOnlyList<string> Accepts;
        /// <summary>-1 means unlimited; zero is invalid.</summary>
        public readonly int Capacity;
        public readonly float X;
        public readonly float Y;
        public readonly string RectProfile;
        public readonly string SlotProfile;
        public readonly int SlotCount;
        public readonly string MotionProfileId;

        public TargetDef(string id, IReadOnlyList<string> accepts, int capacity, float x, float y, string rectProfile, string slotProfile, int slotCount)
        {
            TargetId = id; Accepts = accepts; Capacity = capacity; X = x; Y = y;
            RectProfile = rectProfile; SlotProfile = slotProfile; SlotCount = slotCount; MotionProfileId = "";
        }

        public bool IsUnlimited => Capacity < 0;
        public bool AcceptsCategory(string categoryId) => Accepts.Contains(categoryId);
    }

    public sealed class BoardDef
    {
        public readonly string BoardId;
        public readonly bool Required;
        public readonly string TemplateId;
        public readonly IReadOnlyList<ObjectDef> Objects;
        public readonly IReadOnlyList<TargetDef> Targets;
        public readonly string SourceLayoutId;
        public readonly string TargetLayoutId;
        public readonly string RecordRule;
        readonly Dictionary<string, ObjectDef> _objects;
        readonly Dictionary<string, TargetDef> _targets;

        public BoardDef(string boardId, bool required, string templateId, IReadOnlyList<ObjectDef> objects,
            IReadOnlyList<TargetDef> targets, string sourceLayoutId, string targetLayoutId)
        {
            BoardId = boardId; Required = required; TemplateId = templateId; Objects = objects; Targets = targets;
            SourceLayoutId = sourceLayoutId; TargetLayoutId = targetLayoutId;
            RecordRule = templateId == TemplateIds.Unlimited ? RecordRules.CleanSort : RecordRules.UnassistedPlanner;
            _objects = objects.ToDictionary(o => o.InstanceId);
            _targets = targets.ToDictionary(t => t.TargetId);
        }

        /// <summary>Capacity boards leave assigned objects selectable and allow returns.</summary>
        public bool IsPlanning => TemplateId != TemplateIds.Unlimited;
        public ObjectDef Object(string id) => _objects[id];
        public TargetDef Target(string id) => _targets[id];
        public bool HasTarget(string id) => id != null && _targets.ContainsKey(id);
        public bool HasObject(string id) => id != null && _objects.ContainsKey(id);
        public int TotalUnits => Objects.Sum(o => o.Units);
    }

    public sealed class LevelDef
    {
        public string LevelId;
        public int ContentVersion;
        public int BoardContentVersion;
        public string DestinationId;
        public string AreaId;
        public int OrdinalInArea;
        public string ThemeId;
        public string PurposeKey;
        public string PurposeEnglish;
        public IReadOnlyList<BoardDef> Boards;
        public string PreviewLevelId;
        public bool CanDeferLevel;
        public string TeachingId;
        public string ProjectRegion;
        public int TraceIndex;
        public string ProjectAnchorId;
        public string JourneyMilestoneId;
        public int FirstCompletionCoins;
        public string CoinRewardId;
        public string FinishMicroAction;
        public string PurposeProp;
        public string BeforeProfile;
        public string AfterProfile;
        /// <summary>Optional one-line instruction from data; empty = derived from the board's rule shape.</summary>
        public string InstructionOverride = "";

        public BoardDef Board(string boardId) => Boards.First(b => b.BoardId == boardId);
        public BoardDef RequiredBoard => Boards.First(b => b.Required);
        public BoardDef OptionalBoard => Boards.FirstOrDefault(b => !b.Required);
        public bool HasOptionalBoard => Boards.Any(b => !b.Required);
        public BoardDef FirstBoard => Boards[0];
    }

    public sealed class AnchorDef
    {
        public readonly string AnchorId;
        public readonly string Region;
        public readonly string TriggerLevelId;
        public readonly string VariantId;
        public readonly int VariantPrice;
        public AnchorDef(string anchorId, string region, string trigger, string variantId, int price)
        {
            AnchorId = anchorId; Region = region; TriggerLevelId = trigger; VariantId = variantId; VariantPrice = price;
        }
    }

    public sealed class AreaDef
    {
        public string AreaId;
        public string DestinationId;
        public IReadOnlyList<string> LevelIds;
        public string ReadyRecognitionId;
    }

    public sealed class BuildProfile
    {
        public readonly string ProfileId;
        public readonly IReadOnlyCollection<string> LevelIds;
        public BuildProfile(string id, IEnumerable<string> levels) { ProfileId = id; LevelIds = new HashSet<string>(levels); }
        public bool Contains(string levelId) => LevelIds.Contains(levelId);
    }
}
