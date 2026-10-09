using System;

namespace SortEverything.Prototype
{
    /// <summary>
    /// The authoritative state of one board: where every object is (-1 = on the board) and how many units each target
    /// holds. Visual objects only display this. Placement is one transaction: a transfer between targets moves the
    /// object's units in a single step, and a refused placement changes nothing. Allocation-free after construction.
    /// </summary>
    public sealed class BoardModel
    {
        public readonly BoardDef Def;
        public readonly ObjectDef[] Objects;
        readonly int[] loc;
        readonly int[] used;
        readonly int[] count;
        readonly int[] expected;

        public BoardModel(BoardDef def)
        {
            Def = def;
            Objects = new ObjectDef[def.objects.Length];
            for (int i = 0; i < Objects.Length; i++) Objects[i] = ObjectLibrary.Get(def.objects[i].asset);
            loc = new int[Objects.Length];
            used = new int[def.targets.Length];
            count = new int[def.targets.Length];
            expected = new int[def.targets.Length];
            // On unlimited boards every object has exactly one target (validated); count how many each will receive.
            for (int i = 0; i < Objects.Length; i++)
            {
                int only = -1, matches = 0;
                for (int t = 0; t < def.targets.Length; t++)
                    if (RuleEvaluator.Accepts(def.targets[t], Objects[i])) { matches++; only = t; }
                if (matches == 1) expected[only]++;
            }
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < loc.Length; i++) loc[i] = -1;
            for (int t = 0; t < used.Length; t++) { used[t] = 0; count[t] = 0; }
            AssignedCount = 0;
        }

        public int ObjectCount { get { return Objects.Length; } }
        public int TargetCount { get { return used.Length; } }
        public int AssignedCount { get; private set; }
        public bool Solved { get { return AssignedCount == Objects.Length; } }
        public int Units(int obj) { return Def.objects[obj].units; }
        public int Location(int obj) { return loc[obj]; }
        public int Used(int target) { return used[target]; }
        public int Count(int target) { return count[target]; }
        /// <summary>Objects this target will hold when the board is solved (unlimited boards only; 0 otherwise).</summary>
        public int Expected(int target) { return expected[target]; }
        public int Capacity(int target) { return Def.targets[target].capacity; }

        /// <summary>Assigned objects can be picked up again only on capacity (planning) boards.</summary>
        public bool CanPickUp(int obj) { return loc[obj] < 0 || Def.IsCapacity; }

        /// <summary>What would happen if obj were dropped on target, without changing anything.</summary>
        public PlaceResult Check(int obj, int target)
        {
            if (obj < 0 || obj >= loc.Length || target < 0 || target >= used.Length) return PlaceResult.Ignored;
            if (loc[obj] == target) return PlaceResult.Ignored;
            if (!RuleEvaluator.Accepts(Def.targets[target], Objects[obj])) return PlaceResult.WrongTarget;
            int cap = Def.targets[target].capacity;
            if (cap >= 0 && used[target] + Units(obj) > cap) return PlaceResult.Full;
            return PlaceResult.Accepted;
        }

        /// <summary>Commits a placement or transfer when it is legal. Returns the reason otherwise.</summary>
        public PlaceResult Place(int obj, int target)
        {
            var r = Check(obj, target);
            if (r != PlaceResult.Accepted) return r;
            int from = loc[obj];
            if (from >= 0) { used[from] -= Units(obj); count[from]--; }
            else AssignedCount++;
            loc[obj] = target;
            used[target] += Units(obj);
            count[target]++;
            return PlaceResult.Accepted;
        }

        /// <summary>Returns an assigned object to the board (legal rearrangement on capacity boards).</summary>
        public bool Unassign(int obj)
        {
            if (loc[obj] < 0 || !Def.IsCapacity) return false;
            int from = loc[obj];
            used[from] -= Units(obj);
            count[from]--;
            loc[obj] = -1;
            AssignedCount--;
            return true;
        }

        public int[] Snapshot() { return (int[])loc.Clone(); }

        /// <summary>
        /// Rebuilds the board from saved locations, re-checking every rule and capacity (saved totals are never
        /// trusted). Returns false and leaves the board reset when the snapshot is not valid for this board.
        /// </summary>
        public bool Restore(int[] locations)
        {
            Reset();
            if (locations == null || locations.Length != loc.Length) return false;
            for (int i = 0; i < locations.Length; i++)
            {
                if (locations[i] < 0) continue;
                if (Place(i, locations[i]) != PlaceResult.Accepted) { Reset(); return false; }
            }
            return true;
        }
    }

    /// <summary>
    /// Active play time: starts on the first pickup, excludes pauses, inspection and background time (the caller
    /// sets Paused and only feeds frame time while the level is actually playable). Counts up; there is no deadline.
    /// </summary>
    public sealed class ActiveClock
    {
        public bool Running { get; private set; }
        public bool Paused { get; set; }
        public float Elapsed { get; private set; }

        public void Start() { Running = true; }
        public void Stop() { Running = false; }
        public void Reset(float elapsed = 0f) { Running = false; Elapsed = Math.Max(0f, elapsed); }

        public void Advance(float dt)
        {
            if (!Running || Paused || dt <= 0f) return;
            Elapsed += dt;
        }

        /// <summary>Elapsed time in tenths (rounded down) for the HUD.</summary>
        public int DisplayTenths { get { return (int)Math.Floor(Elapsed * 10f + 1e-3f); } }
    }

    /// <summary>
    /// Pre-built "12.4" strings, so the Mastery HUD never allocates while the clock runs. Values above the table
    /// range show the last entry.
    /// </summary>
    public sealed class TimerText
    {
        readonly string[] tenths;

        public TimerText(float maxSeconds)
        {
            int n = (int)Math.Ceiling(maxSeconds * 10f) + 1;
            tenths = new string[n];
            for (int i = 0; i < n; i++)
                tenths[i] = (i / 10).ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + (i % 10);
        }

        public string For(int displayTenths)
        {
            if (displayTenths < 0) displayTenths = 0;
            if (displayTenths >= tenths.Length) displayTenths = tenths.Length - 1;
            return tenths[displayTenths];
        }
    }
}
