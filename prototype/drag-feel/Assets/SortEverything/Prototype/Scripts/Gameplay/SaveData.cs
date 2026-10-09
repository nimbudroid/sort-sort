using System;
using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Everything the player keeps: completion seals, granted one-time rewards (by immutable id), coins, discoveries,
    /// optional Mastery records, appearance choices, settings and the last committed state of an unfinished level.
    /// Plain C#; serialised as versioned JSON. Older versions are migrated forward step by step; a newer file is
    /// refused rather than overwritten.
    /// </summary>
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public readonly HashSet<string> completed = new HashSet<string>();     // level ids with a completion seal
        public readonly HashSet<string> rewards = new HashSet<string>();       // granted reward / milestone ids
        public int coins;
        public readonly HashSet<string> discoveries = new HashSet<string>();   // object ids placed at least once
        public readonly Dictionary<string, float> bestTimes = new Dictionary<string, float>(); // Mastery only
        public readonly HashSet<string> ownedVariants = new HashSet<string>(); // area ids whose variant was bought
        public readonly Dictionary<string, bool> variantEquipped = new Dictionary<string, bool>();
        public readonly Dictionary<string, float> legacyBestTimes = new Dictionary<string, float>(); // imported, unused

        // Settings.
        public bool mastery, reducedMotion, showItemNames;

        public string lastLevel;
        public AttemptSnapshot resume;     // last committed state of an unfinished campaign level

        // ---- JSON --------------------------------------------------------------------------------

        public string ToJson()
        {
            var d = new Dictionary<string, object>
            {
                { "version", version },
                { "completed", Sorted(completed) },
                { "rewards", Sorted(rewards) },
                { "coins", coins },
                { "discoveries", Sorted(discoveries) },
                { "bestTimes", Floats(bestTimes) },
                { "ownedVariants", Sorted(ownedVariants) },
                { "variantEquipped", Bools(variantEquipped) },
                { "legacyBestTimes", Floats(legacyBestTimes) },
                { "settings", new Dictionary<string, object>
                    { { "mastery", mastery }, { "reducedMotion", reducedMotion }, { "showItemNames", showItemNames } } },
            };
            if (lastLevel != null) d["lastLevel"] = lastLevel;
            if (resume != null) d["resume"] = SnapshotToJson(resume);
            return MiniJson.Write(d, true);
        }

        /// <summary>Parses and migrates a save. Throws FormatException for unreadable or newer-version files.</summary>
        public static SaveData FromJson(string json)
        {
            var root = MiniJson.Parse(json) as IDictionary<string, object>;
            if (root == null) throw new FormatException("save is not an object");
            int v = MiniJson.Int(root, "version", 0);
            if (v > CurrentVersion) throw new FormatException("save version " + v + " is newer than " + CurrentVersion);
            root = Migrate(root, v);

            var s = new SaveData();
            foreach (var x in MiniJson.Strings(root, "completed")) if (x != null) s.completed.Add(x);
            foreach (var x in MiniJson.Strings(root, "rewards")) if (x != null) s.rewards.Add(x);
            s.coins = Math.Max(0, MiniJson.Int(root, "coins"));
            foreach (var x in MiniJson.Strings(root, "discoveries")) if (x != null) s.discoveries.Add(x);
            ReadFloats(MiniJson.Obj(root, "bestTimes"), s.bestTimes);
            foreach (var x in MiniJson.Strings(root, "ownedVariants")) if (x != null) s.ownedVariants.Add(x);
            var eq = MiniJson.Obj(root, "variantEquipped");
            if (eq != null) foreach (var kv in eq) if (kv.Value is bool) s.variantEquipped[kv.Key] = (bool)kv.Value;
            ReadFloats(MiniJson.Obj(root, "legacyBestTimes"), s.legacyBestTimes);
            var st = MiniJson.Obj(root, "settings");
            s.mastery = MiniJson.Bool(st, "mastery");
            s.reducedMotion = MiniJson.Bool(st, "reducedMotion");
            s.showItemNames = MiniJson.Bool(st, "showItemNames");
            s.lastLevel = MiniJson.Str(root, "lastLevel");
            s.resume = SnapshotFromJson(MiniJson.Obj(root, "resume"));
            return s;
        }

        /// <summary>Steps an older save up to the current version. Version 0 = the unversioned prototype layout.</summary>
        static IDictionary<string, object> Migrate(IDictionary<string, object> root, int from)
        {
            if (from < 1)
            {
                // v0 (prototype): flat settings and "best" times keyed by experiment level ids. Keep them as legacy.
                var settings = MiniJson.Obj(root, "settings") ?? new Dictionary<string, object>();
                foreach (var k in new[] { "mastery", "reducedMotion", "showItemNames" })
                {
                    object val;
                    if (!settings.ContainsKey(k) && root.TryGetValue(k, out val)) settings[k] = val;
                }
                root["settings"] = settings;
                object best;
                if (!root.ContainsKey("legacyBestTimes") && root.TryGetValue("best", out best)) root["legacyBestTimes"] = best;
                root["version"] = 1.0;
            }
            return root;
        }

        static List<object> Sorted(HashSet<string> set)
        {
            var l = new List<string>(set);
            l.Sort(StringComparer.Ordinal);
            return l.ConvertAll(x => (object)x);
        }

        static Dictionary<string, object> Floats(Dictionary<string, float> d)
        {
            var o = new Dictionary<string, object>();
            foreach (var kv in d) o[kv.Key] = (double)kv.Value;
            return o;
        }

        static Dictionary<string, object> Bools(Dictionary<string, bool> d)
        {
            var o = new Dictionary<string, object>();
            foreach (var kv in d) o[kv.Key] = kv.Value;
            return o;
        }

        static void ReadFloats(IDictionary<string, object> src, Dictionary<string, float> dst)
        {
            if (src == null) return;
            foreach (var kv in src) if (kv.Value is double && (double)kv.Value > 0) dst[kv.Key] = (float)(double)kv.Value;
        }

        static Dictionary<string, object> SnapshotToJson(AttemptSnapshot a)
        {
            var locs = new List<object>();
            foreach (var x in a.locations ?? new int[0]) locs.Add(x);
            return new Dictionary<string, object>
            {
                { "levelId", a.levelId }, { "contentVersion", a.contentVersion }, { "attemptId", a.attemptId ?? "" },
                { "board", a.board }, { "locations", locs }, { "activeSeconds", (double)a.activeSeconds },
                { "wrongCategory", a.wrongCategory }, { "wrongFull", a.wrongFull }, { "rearrangements", a.rearrangements },
                { "assisted", a.assisted }, { "started", a.started },
                { "discoveries", new List<object>(a.discoveries ?? new string[0]) },
                { "credited", new List<object>(a.credited ?? new string[0]) },
            };
        }

        static AttemptSnapshot SnapshotFromJson(IDictionary<string, object> d)
        {
            if (d == null || MiniJson.Str(d, "levelId") == null) return null;
            var list = MiniJson.List(d, "locations") ?? new List<object>();
            var locs = new int[list.Count];
            for (int i = 0; i < locs.Length; i++) locs[i] = list[i] is double ? (int)(double)list[i] : -1;
            return new AttemptSnapshot
            {
                levelId = MiniJson.Str(d, "levelId"),
                contentVersion = MiniJson.Int(d, "contentVersion", 1),
                attemptId = MiniJson.Str(d, "attemptId"),
                board = MiniJson.Int(d, "board"),
                locations = locs,
                activeSeconds = MiniJson.Float(d, "activeSeconds"),
                wrongCategory = MiniJson.Int(d, "wrongCategory"),
                wrongFull = MiniJson.Int(d, "wrongFull"),
                rearrangements = MiniJson.Int(d, "rearrangements"),
                assisted = MiniJson.Bool(d, "assisted"),
                started = MiniJson.Bool(d, "started"),
                discoveries = MiniJson.Strings(d, "discoveries"),
                credited = MiniJson.Strings(d, "credited"),
            };
        }
    }
}
