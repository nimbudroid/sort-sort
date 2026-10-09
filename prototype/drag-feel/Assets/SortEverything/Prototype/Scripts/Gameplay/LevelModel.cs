using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>One destination on a board.</summary>
    public sealed class TargetDef
    {
        public string id;
        public string label;                 // shown on the target; default from the rule
        public string[] accepts = new string[0]; // CategoryLibrary ids; empty = colour-only target
        public SortColor color = SortColor.None;
        public int capacity = -1;            // units; -1 = unlimited
        public string style = "bin";         // presentation: bin, basket, tray, drawer, shelf
        public string colorHex;              // body colour

        public bool Unlimited { get { return capacity < 0; } }

        public string DisplayLabel { get { return !string.IsNullOrEmpty(label) ? label : RuleEvaluator.Label(accepts, color); } }
    }

    /// <summary>One object instance on a board. Units: visible space cost (1 normal, 2 large).</summary>
    public sealed class SpawnDef
    {
        public string id;
        public string asset;                 // ObjectLibrary id (also the Sculpted drawing id)
        public int units = 1;
    }

    public sealed class BoardDef
    {
        public string template;              // e.g. "dedicated", "P1".."P4" (informational)
        public SpawnDef[] objects = new SpawnDef[0];
        public TargetDef[] targets = new TargetDef[0];

        /// <summary>A capacity (planning) board: assigned objects stay movable and full targets refuse drops.</summary>
        public bool IsCapacity
        {
            get
            {
                for (int i = 0; i < targets.Length; i++) if (!targets[i].Unlimited) return true;
                return false;
            }
        }
    }

    /// <summary>One playable section. Counts and rules come only from here.</summary>
    public sealed class LevelDef
    {
        public string id;                    // immutable level id, e.g. "kitchen_006"
        public int contentVersion = 1;
        public string roomId, areaId;
        public int section;                  // 1-based position within its room
        public string title;                 // scene purpose, e.g. "Share the produce tray"
        public string cue;                   // short teaching phrase shown with the rule, optional
        public string teaches;               // concept introduced here, or null
        public string role = "breather";     // teach / challenge / breather
        public string surface;               // presentation profile
        public string finish;                // completion micro-action profile
        public bool tutorial;                // first-drag hand demonstration
        public bool recordsEnabled = true;
        public int coins = 50;               // first-completion coins (granted once via RewardId)
        public BoardDef[] boards = new BoardDef[0];

        public string RewardId { get { return "campaign_complete:" + id; } }

        public int ObjectCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < boards.Length; i++) n += boards[i].objects.Length;
                return n;
            }
        }

        /// <summary>Rule text for the banner when no cue is authored.</summary>
        public string RuleText(int board)
        {
            var b = boards[board];
            if (b.IsCapacity) return "Watch the spaces: each target shows what it takes and how much fits.";
            return "Drag each item into the bin with its label.";
        }

        public override string ToString() { return id; }
    }

    /// <summary>A part of a room that is visibly restored when all its sections are done.</summary>
    public sealed class AreaDef
    {
        public string id;
        public string name;
        public string restoredText;          // e.g. "Breakfast corner arranged"
        public string variantName;           // optional appearance variant
        public int variantPrice;
        public string[] sections = new string[0]; // level ids in order
    }

    public sealed class RoomTheme
    {
        public string wallHex = "CFE9F7";
        public string wallAccentHex = "B9DCF0";
        public string surfaceHex = "C98B4E";
        public string pattern = "none";      // tiles, planks, stripes, dots
        public string accentHex = "F28C28";  // overview card colour
    }

    public sealed class RoomDef
    {
        public string id;
        public string name;
        public bool available;               // authored and playable; false = shown locked ("coming soon")
        public string masterName;            // completion title, e.g. "Kitchen Master"
        public string[] icons = new string[0]; // object ids shown on the overview card
        public RoomTheme theme = new RoomTheme();
        public AreaDef[] areas = new AreaDef[0];
        public int plannedAreas;             // for rooms not yet authored: how many areas the overview shows
    }

    public sealed class HouseDef
    {
        public string id = "house";
        public string finalMilestoneId = "house_complete";
        public RoomDef[] rooms = new RoomDef[0];
    }

    /// <summary>The whole authored campaign: house, rooms, areas and levels in play order.</summary>
    public sealed class Campaign
    {
        public readonly HouseDef House;
        public readonly List<LevelDef> Levels = new List<LevelDef>();      // global play order (available rooms only)
        public readonly List<LevelDef> Lab = new List<LevelDef>();         // debug-only levels
        public readonly List<LevelDef> AllLevels = new List<LevelDef>();   // every authored campaign level (for validation)
        readonly Dictionary<string, LevelDef> byId = new Dictionary<string, LevelDef>();

        public Campaign(HouseDef house, IEnumerable<LevelDef> levels, IEnumerable<LevelDef> lab = null)
        {
            House = house;
            var all = new Dictionary<string, LevelDef>();
            foreach (var l in levels) all[l.id] = l;
            foreach (var room in house.rooms)
            {
                if (!room.available) continue;
                foreach (var area in room.areas)
                    foreach (var id in area.sections)
                    {
                        LevelDef l;
                        if (!all.TryGetValue(id, out l)) continue; // reported by the validator
                        l.roomId = room.id;
                        l.areaId = area.id;
                        l.section = CountInRoom(room.id) + 1;
                        Levels.Add(l);
                        byId[l.id] = l;
                    }
            }
            if (lab != null)
                foreach (var l in lab) { Lab.Add(l); byId[l.id] = l; }
        }

        int CountInRoom(string roomId)
        {
            int n = 0;
            foreach (var l in Levels) if (l.roomId == roomId) n++;
            return n;
        }

        public LevelDef Get(string id)
        {
            LevelDef l;
            return id != null && byId.TryGetValue(id, out l) ? l : null;
        }

        public int IndexOf(LevelDef l) { return Levels.IndexOf(l); }

        /// <summary>The next level in play order, or null after the last authored level (or for lab levels).</summary>
        public LevelDef Next(LevelDef l)
        {
            int i = Levels.IndexOf(l);
            return i >= 0 && i + 1 < Levels.Count ? Levels[i + 1] : null;
        }

        public RoomDef Room(string id)
        {
            foreach (var r in House.rooms) if (r.id == id) return r;
            return null;
        }

        public AreaDef Area(string id)
        {
            foreach (var r in House.rooms) foreach (var a in r.areas) if (a.id == id) return a;
            return null;
        }

        public List<LevelDef> LevelsInRoom(string roomId)
        {
            var list = new List<LevelDef>();
            foreach (var l in Levels) if (l.roomId == roomId) list.Add(l);
            return list;
        }
    }
}
