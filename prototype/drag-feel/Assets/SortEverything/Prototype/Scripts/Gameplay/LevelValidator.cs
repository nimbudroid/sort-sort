using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Checks an authored level: every bin has a usable rule, every object exists and has art, every object matches
    /// exactly one bin, and every bin receives at least one object. A level that fails must never ship.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>Index of the single bin that accepts the object, -1 if none, -2 if more than one.</summary>
        public static int MatchingBin(LevelDef level, ObjectDef def)
        {
            int found = -1;
            for (int b = 0; b < level.bins.Length; b++)
            {
                if (!RuleEvaluator.Matches(level.bins[b].rule, def)) continue;
                if (found >= 0) return -2;
                found = b;
            }
            return found;
        }

        public static List<string> Validate(LevelDef level)
        {
            var errors = new List<string>();
            if (level == null) { errors.Add("level is null"); return errors; }
            string at = "level " + level.id + ": ";
            if (string.IsNullOrEmpty(level.id)) errors.Add("level has no id");
            if (level.timerSeconds <= 0f) errors.Add(at + "timer must be > 0");
            if (level.wrongDropPenalty < 0f) errors.Add(at + "wrong-drop penalty must be >= 0");
            if (level.bins.Length < 2) errors.Add(at + "needs at least 2 bins");
            if (level.objectIds.Length == 0) errors.Add(at + "has no objects");

            for (int b = 0; b < level.bins.Length; b++)
            {
                var rule = level.bins[b].rule;
                if (rule.Shape == RuleShape.Invalid) errors.Add(at + "bin " + b + " has neither a colour nor a category");
                if (rule.HasColor && !RuleEvaluator.IsBinColor(rule.color)) errors.Add(at + "bin " + b + " uses a non-bin colour");
                if (rule.HasCategory && !CategoryLibrary.Exists(rule.category))
                    errors.Add(at + "bin " + b + " uses unknown category " + rule.category);
                if (string.IsNullOrEmpty(level.bins[b].label)) errors.Add(at + "bin " + b + " has no label");
            }

            var perBin = new int[level.bins.Length];
            var seen = new HashSet<string>();
            foreach (var id in level.objectIds)
            {
                if (!seen.Add(id)) errors.Add(at + "object " + id + " is listed twice");
                var def = ObjectLibrary.Get(id);
                if (def == null) { errors.Add(at + "unknown object " + id); continue; }
                if (!ObjectDrawings.Has(id)) errors.Add(at + "object " + id + " has no drawing");
                int bin = MatchingBin(level, def);
                if (bin == -1) errors.Add(at + "object " + id + " matches no bin");
                else if (bin == -2) errors.Add(at + "object " + id + " matches more than one bin");
                else perBin[bin]++;
            }
            for (int b = 0; b < perBin.Length; b++)
                if (perBin[b] == 0 && level.bins[b].rule.Shape != RuleShape.Invalid)
                    errors.Add(at + "bin " + level.bins[b].label + " receives no objects");
            return errors;
        }
    }
}
