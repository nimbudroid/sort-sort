using System;
using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Builds the Campaign from authored JSON (campaign.json plus one file per room and an optional lab file).
    /// Adding rooms, areas, sections or levels is a data change. `read(name)` returns a file's text, e.g. from
    /// Resources in Unity or from disk in tests.
    ///
    /// Level boards are compact:
    ///   "objects": "orange* apple banana"  - library ids; '*' = large variant (2 units)
    ///   "targets": "FRUIT VEGETABLES"      - one unlimited target per category, coloured from categoryColors
    ///   "targets": "P1"                    - a capacity template from campaign.json
    ///   "targets": [ {...}, ... ]          - explicit targets (accepts, color, capacity, label, style, colorHex)
    /// </summary>
    public static class CatalogLoader
    {
        public const int SchemaVersion = 1;

        public static Campaign Load(Func<string, string> read)
        {
            var root = MiniJson.Parse(read("campaign")) as IDictionary<string, object>;
            if (root == null) throw new FormatException("campaign.json is not an object");
            int schema = MiniJson.Int(root, "schemaVersion");
            if (schema != SchemaVersion) throw new FormatException("campaign.json schemaVersion " + schema + " is not supported");

            var colors = new Dictionary<string, string>();
            var cc = MiniJson.Obj(root, "categoryColors");
            if (cc != null) foreach (var kv in cc) colors[kv.Key] = kv.Value as string;

            var templates = new Dictionary<string, List<object>>();
            var tp = MiniJson.Obj(root, "templates");
            if (tp != null) foreach (var kv in tp) templates[kv.Key] = kv.Value as List<object>;

            var house = new HouseDef();
            var h = MiniJson.Obj(root, "house");
            house.id = MiniJson.Str(h, "id", "house");
            house.finalMilestoneId = MiniJson.Str(h, "finalMilestoneId", "house_complete");
            var rooms = new List<RoomDef>();
            foreach (var r in MiniJson.List(root, "rooms") ?? new List<object>()) rooms.Add(ParseRoom(r as IDictionary<string, object>));
            house.rooms = rooms.ToArray();

            var levels = new List<LevelDef>();
            foreach (var file in MiniJson.Strings(root, "levelFiles")) levels.AddRange(ParseLevels(read(file), colors, templates));
            var lab = new List<LevelDef>();
            string labFile = MiniJson.Str(root, "labFile");
            if (labFile != null)
            {
                string text = read(labFile);
                if (text != null) lab.AddRange(ParseLevels(text, colors, templates));
            }
            var campaign = new Campaign(house, levels, lab);
            campaign.AllLevels.AddRange(levels);
            return campaign;
        }

        static RoomDef ParseRoom(IDictionary<string, object> d)
        {
            var room = new RoomDef
            {
                id = MiniJson.Str(d, "id"),
                name = MiniJson.Str(d, "name"),
                available = MiniJson.Bool(d, "available"),
                masterName = MiniJson.Str(d, "masterName"),
                icons = MiniJson.Strings(d, "icons"),
                plannedAreas = MiniJson.Int(d, "plannedAreas"),
            };
            var t = MiniJson.Obj(d, "theme");
            if (t != null)
            {
                room.theme.wallHex = MiniJson.Str(t, "wall", room.theme.wallHex);
                room.theme.wallAccentHex = MiniJson.Str(t, "wallAccent", room.theme.wallAccentHex);
                room.theme.surfaceHex = MiniJson.Str(t, "surface", room.theme.surfaceHex);
                room.theme.pattern = MiniJson.Str(t, "pattern", room.theme.pattern);
                room.theme.accentHex = MiniJson.Str(t, "accent", room.theme.accentHex);
            }
            var areas = new List<AreaDef>();
            foreach (var a in MiniJson.List(d, "areas") ?? new List<object>())
            {
                var ad = a as IDictionary<string, object>;
                areas.Add(new AreaDef
                {
                    id = MiniJson.Str(ad, "id"),
                    name = MiniJson.Str(ad, "name"),
                    restoredText = MiniJson.Str(ad, "restored"),
                    variantName = MiniJson.Str(ad, "variant"),
                    variantPrice = MiniJson.Int(ad, "price"),
                    sections = MiniJson.Strings(ad, "sections"),
                });
            }
            room.areas = areas.ToArray();
            if (room.plannedAreas == 0) room.plannedAreas = room.areas.Length;
            return room;
        }

        static List<LevelDef> ParseLevels(string json, Dictionary<string, string> colors, Dictionary<string, List<object>> templates)
        {
            var list = new List<LevelDef>();
            var root = MiniJson.Parse(json) as IDictionary<string, object>;
            foreach (var o in MiniJson.List(root, "levels") ?? new List<object>())
            {
                var d = o as IDictionary<string, object>;
                var level = new LevelDef
                {
                    id = MiniJson.Str(d, "id"),
                    contentVersion = MiniJson.Int(d, "version", 1),
                    title = MiniJson.Str(d, "title"),
                    cue = MiniJson.Str(d, "cue"),
                    teaches = MiniJson.Str(d, "teaches"),
                    role = MiniJson.Str(d, "role", "breather"),
                    surface = MiniJson.Str(d, "surface"),
                    finish = MiniJson.Str(d, "finish"),
                    tutorial = MiniJson.Bool(d, "tutorial"),
                    recordsEnabled = MiniJson.Bool(d, "records", true),
                    coins = MiniJson.Int(d, "coins", 50),
                };
                var boards = new List<BoardDef>();
                foreach (var b in MiniJson.List(d, "boards") ?? new List<object>())
                    boards.Add(ParseBoard(b as IDictionary<string, object>, colors, templates));
                level.boards = boards.ToArray();
                list.Add(level);
            }
            return list;
        }

        static BoardDef ParseBoard(IDictionary<string, object> d, Dictionary<string, string> colors, Dictionary<string, List<object>> templates)
        {
            var board = new BoardDef();
            var objects = new List<SpawnDef>();
            var counts = new Dictionary<string, int>();
            foreach (var tok in (MiniJson.Str(d, "objects") ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                bool large = tok.EndsWith("*");
                string asset = large ? tok.Substring(0, tok.Length - 1) : tok;
                int n;
                counts.TryGetValue(asset, out n);
                counts[asset] = n + 1;
                objects.Add(new SpawnDef { id = asset + "_" + (n + 1), asset = asset, units = large ? 2 : 1 });
            }
            board.objects = objects.ToArray();

            object tv;
            d.TryGetValue("targets", out tv);
            var targets = new List<TargetDef>();
            string ts = tv as string;
            if (ts != null && templates.ContainsKey(ts))
            {
                board.template = ts;
                foreach (var t in templates[ts]) targets.Add(ParseTarget(t as IDictionary<string, object>, colors));
            }
            else if (ts != null)
            {
                board.template = "dedicated";
                foreach (var cat in ts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string hex;
                    colors.TryGetValue(cat, out hex);
                    targets.Add(new TargetDef { id = cat.ToLowerInvariant() + "_bin", accepts = new[] { cat }, colorHex = hex });
                }
            }
            else if (tv is List<object>)
            {
                board.template = "explicit";
                foreach (var t in (List<object>)tv) targets.Add(ParseTarget(t as IDictionary<string, object>, colors));
            }
            board.targets = targets.ToArray();
            return board;
        }

        static TargetDef ParseTarget(IDictionary<string, object> d, Dictionary<string, string> colors)
        {
            var t = new TargetDef
            {
                accepts = MiniJson.Strings(d, "accepts"),
                color = RuleEvaluator.ParseColor(MiniJson.Str(d, "color")),
                capacity = MiniJson.Int(d, "capacity", -1),
                label = MiniJson.Str(d, "label"),
                style = MiniJson.Str(d, "style", "bin"),
                colorHex = MiniJson.Str(d, "colorHex"),
            };
            t.id = MiniJson.Str(d, "id") ?? (t.accepts.Length > 0 ? string.Join("_", t.accepts).ToLowerInvariant() : RuleEvaluator.ColorName(t.color).ToLowerInvariant()) + "_bin";
            if (t.colorHex == null)
            {
                if (t.color != SortColor.None) t.colorHex = RuleEvaluator.ColorHex(t.color);
                else if (t.accepts.Length == 1) colors.TryGetValue(t.accepts[0], out t.colorHex);
            }
            if (t.colorHex == null) t.colorHex = "FF6FAE";
            return t;
        }
    }
}
