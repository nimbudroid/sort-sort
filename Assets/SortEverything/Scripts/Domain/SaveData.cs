using System;
using System.Collections.Generic;
using System.Linq;

namespace SortEverything.Domain
{
    // JsonUtility-friendly DTOs: public fields, serializable lists and scalars, strings for IDs/enums.
    // Nullable IDs are represented by an empty string.

    [Serializable]
    public sealed class SettingsData
    {
        public string preference = "calm"; // calm | mastery
        public bool music = true;
        public bool effects = true;
        public bool haptics = true;
        public bool reducedMotion = false;
    }

    [Serializable]
    public sealed class AreaCursorData { public string areaId; public int cursor; }

    [Serializable]
    public sealed class EquipData { public string anchorId; public string variantId; } // variantId "" = free default

    [Serializable]
    public sealed class BoardRecordData
    {
        public string levelId;
        public string boardId;
        public int boardContentVersion;
        public string configSignature;
        public long bestMs = -1;
        public long bestUnassistedMs = -1;
    }

    [Serializable]
    public sealed class AssignmentData { public string instanceId; public string targetId; public int slot; }

    [Serializable]
    public sealed class BoardRunData
    {
        public string boardId;
        public string status = BoardStatus.Unstarted; // unstarted | playing | solved | deferred
        public string boardRunId;
        public List<AssignmentData> assignments = new List<AssignmentData>();
        public List<string> pendingDiscoveries = new List<string>();
        public long activeMs;
        public int categoryRejections;
        public int fullRejections;
        public int legalRearrangements;
        public bool assisted;
        public List<string> assistanceKinds = new List<string>();
        public List<string> firstCredits = new List<string>();
        public int streak;
    }

    public static class BoardStatus
    {
        public const string Unstarted = "unstarted";
        public const string Playing = "playing";
        public const string Solved = "solved";
        public const string Deferred = "deferred";
    }

    [Serializable]
    public sealed class AttemptData
    {
        public string attemptId;
        public string levelId;
        public int contentVersion;
        public int boardContentVersion;
        public string entryKind; // first | replay | replay_finish
        public string currentBoardId;
        public string configSignature;
        public List<BoardRunData> boards = new List<BoardRunData>();

        public BoardRunData Board(string id) => boards.First(b => b.boardId == id);
        public BoardRunData Current => Board(currentBoardId);
    }

    [Serializable]
    public sealed class SaveData
    {
        public int schemaVersion = 4;
        public long revision;
        public int contentVersion = KitchenCatalogue.ContentVersion;
        public int wallet;
        public List<string> grantIds = new List<string>();
        public List<string> seals = new List<string>();
        public List<string> deferredLevelIds = new List<string>();
        public List<string> traceIds = new List<string>();
        public List<string> anchorIds = new List<string>();
        public List<string> ownedCosmetics = new List<string>();
        public List<EquipData> equipped = new List<EquipData>();
        public List<string> legacyInventory = new List<string>();
        public List<string> discoveries = new List<string>();
        public List<string> badges = new List<string>();
        public List<string> recognitions = new List<string>();
        public List<string> boardCompleteMarkers = new List<string>();
        public List<AreaCursorData> areaCursors = new List<AreaCursorData>();
        public List<BoardRecordData> records = new List<BoardRecordData>();
        public SettingsData settings = new SettingsData();
        public List<string> tutorialOfferedIds = new List<string>();
        public List<string> tutorialExposedIds = new List<string>();
        public List<AttemptData> attempts = new List<AttemptData>();
        public string selectedAttemptId = "";
        // World fields (Section 29). Only house/kitchen are populated by the kitchen profiles.
        public List<string> unlockedDestinationIds = new List<string>();
        public List<string> unlockedAreaIds = new List<string>();
        public List<string> journeyMilestoneIds = new List<string>();
        public List<string> areaCompletionIds = new List<string>();
        public List<string> ownedKeepsakeIds = new List<string>();
        public string lastVisitedAreaId = "";
        public string participantId = "";

        public int Cursor(string areaId) => areaCursors.FirstOrDefault(c => c.areaId == areaId)?.cursor ?? 1;
        public AttemptData Attempt(string attemptId) => attempts.FirstOrDefault(a => a.attemptId == attemptId);
        public AttemptData AttemptFor(string levelId) => attempts.FirstOrDefault(a => a.levelId == levelId);
        public bool HasSeal(string levelId) => seals.Contains(levelId);
        public bool IsDeferred(string levelId) => deferredLevelIds.Contains(levelId);
        public SaveData ShallowCopy() => (SaveData)MemberwiseClone();
        public string Equipped(string anchorId) => equipped.FirstOrDefault(e => e.anchorId == anchorId)?.variantId ?? "";
    }

    /// <summary>The single source of new-save defaults (Section 14).</summary>
    public static class SaveFactory
    {
        public static SaveData Create(string participantId = "")
        {
            var s = new SaveData
            {
                revision = 0,
                wallet = 0,
                participantId = participantId,
            };
            s.areaCursors.Add(new AreaCursorData { areaId = KitchenCatalogue.AreaId, cursor = 1 });
            s.unlockedDestinationIds.Add(KitchenCatalogue.DestinationId);
            s.unlockedAreaIds.Add(KitchenCatalogue.AreaId);
            return s;
        }
    }

    public static class SaveValidator
    {
        /// <summary>Returns null when valid, otherwise a short reason.</summary>
        public static string Validate(SaveData s, Catalogue cat)
        {
            if (s == null) return "null save";
            if (s.schemaVersion != 4) return "unsupported schema " + s.schemaVersion;
            if (s.wallet < 0) return "negative wallet";
            if (s.grantIds.Count != s.grantIds.Distinct().Count()) return "duplicate grant";
            foreach (var e in s.equipped)
                if (!string.IsNullOrEmpty(e.variantId) && !s.ownedCosmetics.Contains(e.variantId)) return "equipped unowned " + e.variantId;
            foreach (var a in s.attempts)
            {
                var level = cat.Level(a.levelId);
                if (level == null) continue; // preserved out-of-catalogue data
                foreach (var b in a.boards)
                {
                    if (!level.Boards.Any(d => d.BoardId == b.boardId)) return "unknown board " + b.boardId;
                    var def = level.Board(b.boardId);
                    var st = BoardState.FromAssignments(def, b.assignments.Select(x => (x.instanceId, x.targetId, x.slot)), out var err);
                    if (st == null) return "attempt " + a.attemptId + ": " + err;
                }
                if (a.boards.All(b => b.boardId != a.currentBoardId)) return "bad current board";
            }
            return null;
        }
    }
}
