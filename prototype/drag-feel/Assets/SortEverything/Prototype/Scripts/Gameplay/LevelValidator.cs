using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Structural and solvability checks for authored levels and the campaign. A level that fails never ships.
    /// Ordinary (unlimited) boards: every object matches exactly one target and every target receives an object.
    /// Capacity boards: units and capacities are legal and the solver finds a complete assignment.
    /// </summary>
    public static class LevelValidator
    {
        public static List<string> Validate(LevelDef level)
        {
            var errors = new List<string>();
            if (level == null) { errors.Add("level is null"); return errors; }
            string at = "level " + level.id + ": ";
            if (string.IsNullOrEmpty(level.id)) errors.Add("level has no id");
            if (string.IsNullOrEmpty(level.title)) errors.Add(at + "has no title");
            if (level.boards.Length == 0) errors.Add(at + "has no boards");
            if (level.coins < 0) errors.Add(at + "negative coins");
            for (int b = 0; b < level.boards.Length; b++) ValidateBoard(level.boards[b], at + "board " + b + ": ", errors);
            return errors;
        }

        static void ValidateBoard(BoardDef board, string at, List<string> errors)
        {
            if (board.targets.Length < 2) errors.Add(at + "needs at least 2 targets");
            if (board.objects.Length == 0) errors.Add(at + "has no objects");
            var ids = new HashSet<string>();
            foreach (var t in board.targets)
            {
                if (!ids.Add("t:" + t.id)) errors.Add(at + "duplicate target id " + t.id);
                bool hasCats = t.accepts != null && t.accepts.Length > 0;
                if (!hasCats && t.color == SortColor.None) errors.Add(at + "target " + t.id + " accepts nothing");
                if (t.color != SortColor.None && !RuleEvaluator.IsBinColor(t.color)) errors.Add(at + "target " + t.id + " uses a non-bin colour");
                if (hasCats) foreach (var c in t.accepts) if (!CategoryLibrary.Exists(c)) errors.Add(at + "target " + t.id + " unknown category " + c);
                if (t.capacity == 0 || t.capacity < -1) errors.Add(at + "target " + t.id + " has an illegal capacity");
            }
            bool bad = false;
            foreach (var o in board.objects)
            {
                if (!ids.Add("o:" + o.id)) errors.Add(at + "duplicate object id " + o.id);
                if (o.units < 1 || o.units > 2) errors.Add(at + "object " + o.id + " has illegal units " + o.units);
                if (ObjectLibrary.Get(o.asset) == null) { errors.Add(at + "unknown asset " + o.asset); bad = true; }
                else if (!ObjectDrawings.Has(o.asset)) errors.Add(at + "asset " + o.asset + " has no drawing");
            }
            if (bad) return;

            if (!board.IsCapacity)
            {
                var perTarget = new int[board.targets.Length];
                foreach (var o in board.objects)
                {
                    if (o.units != 1) errors.Add(at + "object " + o.id + ": large objects belong on capacity boards");
                    var def = ObjectLibrary.Get(o.asset);
                    int match = -1, n = 0;
                    for (int t = 0; t < board.targets.Length; t++)
                        if (RuleEvaluator.Accepts(board.targets[t], def)) { n++; match = t; }
                    if (n == 0) errors.Add(at + "object " + o.id + " (" + o.asset + ") matches no target");
                    else if (n > 1) errors.Add(at + "object " + o.id + " (" + o.asset + ") matches more than one target");
                    else perTarget[match]++;
                }
                for (int t = 0; t < perTarget.Length; t++)
                    if (perTarget[t] == 0) errors.Add(at + "target " + board.targets[t].DisplayLabel + " receives no objects");
            }
            else
            {
                foreach (var o in board.objects)
                {
                    var def = ObjectLibrary.Get(o.asset);
                    bool any = false;
                    foreach (var t in board.targets) if (RuleEvaluator.Accepts(t, def) && (t.Unlimited || t.capacity >= o.units)) any = true;
                    if (!any) errors.Add(at + "object " + o.id + " (" + o.asset + ") fits no target");
                }
                if (BoardSolver.Solve(board) == null) errors.Add(at + "has no complete legal assignment");
            }
        }

        /// <summary>Campaign-level checks: unique ids, every area section exists, available rooms have levels.</summary>
        public static List<string> ValidateCampaign(Campaign campaign, IEnumerable<LevelDef> allLevels)
        {
            var errors = new List<string>();
            var known = new Dictionary<string, LevelDef>();
            foreach (var l in allLevels)
            {
                if (known.ContainsKey(l.id)) errors.Add("duplicate level id " + l.id);
                known[l.id] = l;
                errors.AddRange(Validate(l));
            }
            var roomIds = new HashSet<string>();
            foreach (var room in campaign.House.rooms)
            {
                if (!roomIds.Add(room.id)) errors.Add("duplicate room id " + room.id);
                if (!room.available) continue;
                if (room.areas.Length == 0) errors.Add("room " + room.id + " has no areas");
                foreach (var area in room.areas)
                {
                    if (area.sections.Length == 0) errors.Add("area " + area.id + " has no sections");
                    foreach (var id in area.sections) if (!known.ContainsKey(id)) errors.Add("area " + area.id + " references missing level " + id);
                }
            }
            return errors;
        }
    }
}
