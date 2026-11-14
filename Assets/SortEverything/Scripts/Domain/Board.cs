using System;
using System.Collections.Generic;
using System.Linq;

namespace SortEverything.Domain
{
    /// <summary>
    /// Authoritative committed board allocation. Immutable from the caller's perspective:
    /// <see cref="BoardReducer"/> returns a new instance for every accepted command.
    /// </summary>
    public sealed class BoardState
    {
        public readonly BoardDef Def;
        readonly Dictionary<string, string> _target = new Dictionary<string, string>();
        readonly Dictionary<string, int> _slot = new Dictionary<string, int>();
        public int Revision { get; private set; }

        public BoardState(BoardDef def) { Def = def; }

        public BoardState Clone()
        {
            var c = new BoardState(Def) { Revision = Revision };
            foreach (var kv in _target) c._target[kv.Key] = kv.Value;
            foreach (var kv in _slot) c._slot[kv.Key] = kv.Value;
            return c;
        }

        /// <summary>Target of an object, or null when it sits in its source slot.</summary>
        public string TargetOf(string instanceId) => _target.TryGetValue(instanceId, out var t) ? t : null;
        public int SlotOf(string instanceId) => _slot.TryGetValue(instanceId, out var s) ? s : -1;
        public bool IsAssigned(string instanceId) => _target.ContainsKey(instanceId);

        public IEnumerable<string> ObjectsIn(string targetId) =>
            _target.Where(kv => kv.Value == targetId).Select(kv => kv.Key).OrderBy(id => _slot[id]);

        public int UnitsIn(string targetId) =>
            _target.Where(kv => kv.Value == targetId).Sum(kv => Def.Object(kv.Key).Units);

        public int SortedCount => _target.Count;
        public int ObjectCount => Def.Objects.Count;

        /// <summary>Solved: every object assigned to a compatible target without overflow.</summary>
        public bool IsSolved
        {
            get
            {
                if (_target.Count != Def.Objects.Count) return false;
                foreach (var kv in _target)
                    if (!Def.Target(kv.Value).AcceptsCategory(Def.Object(kv.Key).CategoryId)) return false;
                foreach (var t in Def.Targets)
                    if (!t.IsUnlimited && UnitsIn(t.TargetId) > t.Capacity) return false;
                return true;
            }
        }

        public int LowestFreeSlot(string targetId)
        {
            var used = new HashSet<int>(_target.Where(kv => kv.Value == targetId).Select(kv => _slot[kv.Key]));
            for (int i = 0; ; i++) if (!used.Contains(i)) return i;
        }

        internal void SetAssignment(string instanceId, string targetId, int slot)
        {
            if (targetId == null) { _target.Remove(instanceId); _slot.Remove(instanceId); }
            else { _target[instanceId] = targetId; _slot[instanceId] = slot; }
        }

        internal void BumpRevision() => Revision++;

        public IEnumerable<(string instanceId, string targetId, int slot)> Assignments() =>
            _target.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => (kv.Key, kv.Value, _slot[kv.Key]));

        /// <summary>Rebuild from saved assignments, validating every entry. Returns null on invalid data.</summary>
        public static BoardState FromAssignments(BoardDef def, IEnumerable<(string instanceId, string targetId, int slot)> saved, out string error)
        {
            error = null;
            var s = new BoardState(def);
            var usedSlots = new HashSet<(string, int)>();
            foreach (var (id, target, slot) in saved)
            {
                if (!def.HasObject(id)) { error = "unknown object " + id; return null; }
                if (!def.HasTarget(target)) { error = "unknown target " + target; return null; }
                if (s.IsAssigned(id)) { error = "duplicate assignment " + id; return null; }
                var t = def.Target(target);
                if (!t.AcceptsCategory(def.Object(id).CategoryId)) { error = "incompatible " + id; return null; }
                if (slot < 0 || slot >= Math.Max(t.SlotCount, 1) || !usedSlots.Add((target, slot))) { error = "bad slot " + id; return null; }
                s.SetAssignment(id, target, slot);
            }
            foreach (var t in def.Targets)
                if (!t.IsUnlimited && s.UnitsIn(t.TargetId) > t.Capacity) { error = "overflow " + t.TargetId; return null; }
            return s;
        }
    }

    public enum DropKind { Accept, Cancel, SameTargetNoOp, WrongCategory, Full, Locked }

    public readonly struct DropProposal
    {
        public readonly string ObjectId;
        /// <summary>Target under the projected object center, or null.</summary>
        public readonly string TargetId;
        /// <summary>True when released over the board return area (SourceRegion).</summary>
        public readonly bool OverSource;
        public DropProposal(string objectId, string targetId, bool overSource) { ObjectId = objectId; TargetId = targetId; OverSource = overSource; }
    }

    public readonly struct DropResult
    {
        public readonly DropKind Kind;
        /// <summary>Accepted return of an assigned planning object to its source slot.</summary>
        public readonly bool IsReturn;
        public readonly string TargetId;
        public DropResult(DropKind kind, string targetId = null, bool isReturn = false) { Kind = kind; TargetId = targetId; IsReturn = isReturn; }
        public override string ToString() => IsReturn ? "Return" : Kind.ToString();
    }

    /// <summary>Pure rule evaluation (Sections 5 and 26). Never mutates state.</summary>
    public static class RuleEvaluator
    {
        public static bool CanPickUp(BoardState s, string objectId)
        {
            if (!s.Def.HasObject(objectId)) return false;
            return !(s.IsAssigned(objectId) && !s.Def.IsPlanning);
        }

        public static DropResult Evaluate(BoardState s, DropProposal p)
        {
            if (!s.Def.HasObject(p.ObjectId)) return new DropResult(DropKind.Cancel);
            if (!CanPickUp(s, p.ObjectId)) return new DropResult(DropKind.Locked);

            var current = s.TargetOf(p.ObjectId);
            if (p.TargetId == null)
            {
                if (current != null && s.Def.IsPlanning && p.OverSource)
                    return new DropResult(DropKind.Accept, null, isReturn: true);
                return new DropResult(DropKind.Cancel);
            }
            if (!s.Def.HasTarget(p.TargetId)) return new DropResult(DropKind.Cancel);
            if (p.TargetId == current) return new DropResult(DropKind.SameTargetNoOp, p.TargetId);

            var obj = s.Def.Object(p.ObjectId);
            var target = s.Def.Target(p.TargetId);
            if (!target.AcceptsCategory(obj.CategoryId)) return new DropResult(DropKind.WrongCategory, p.TargetId);
            if (!target.IsUnlimited)
            {
                // Proposed units: committed assignments in the candidate target (dragged object is not there) + object.
                int proposed = s.UnitsIn(p.TargetId) + obj.Units;
                if (proposed > target.Capacity) return new DropResult(DropKind.Full, p.TargetId);
            }
            return new DropResult(DropKind.Accept, p.TargetId);
        }
    }

    public readonly struct BoardFacts
    {
        public readonly string ObjectId;
        public readonly string FromTarget;
        public readonly string ToTarget;
        public readonly int Slot;
        public readonly bool FirstAssignment;
        public readonly bool Transfer;
        public readonly bool Return;
        public readonly bool Solved;
        public readonly bool CategoryComplete;
        public BoardFacts(string obj, string from, string to, int slot, bool first, bool transfer, bool ret, bool solved, bool catComplete)
        { ObjectId = obj; FromTarget = from; ToTarget = to; Slot = slot; FirstAssignment = first; Transfer = transfer; Return = ret; Solved = solved; CategoryComplete = catComplete; }
    }

    /// <summary>Applies an accepted drop to a deep copy and returns the new state and presentation facts.</summary>
    public static class BoardReducer
    {
        public static BoardState Apply(BoardState s, DropProposal p, DropResult accepted, out BoardFacts facts)
        {
            if (accepted.Kind != DropKind.Accept) throw new InvalidOperationException("Only accepted drops can be applied");
            var next = s.Clone();
            string from = s.TargetOf(p.ObjectId);
            string to = accepted.IsReturn ? null : accepted.TargetId;
            int slot = to == null ? -1 : next.LowestFreeSlot(to);
            next.SetAssignment(p.ObjectId, null, -1); // free old display slot first
            if (to != null) slot = next.LowestFreeSlot(to);
            next.SetAssignment(p.ObjectId, to, slot);
            next.BumpRevision();

            bool catComplete = false;
            if (to != null && !s.Def.IsPlanning)
            {
                var cat = s.Def.Object(p.ObjectId).CategoryId;
                catComplete = s.Def.Objects.Where(o => o.CategoryId == cat).All(o => next.IsAssigned(o.InstanceId));
            }
            facts = new BoardFacts(p.ObjectId, from, to, slot, from == null && to != null, from != null && to != null,
                accepted.IsReturn, next.IsSolved, catComplete);
            return next;
        }

        public static BoardState Reset(BoardDef def) => new BoardState(def);
    }

    /// <summary>
    /// Deterministic bounded backtracking allocation solver (Section 26).
    /// Orders objects by fewest compatible targets, descending units, then instance ID; targets by target ID.
    /// </summary>
    public static class Solver
    {
        public sealed class Result
        {
            public Dictionary<string, string> Allocation; // null when unsatisfiable or aborted
            public int Nodes;
            public bool Aborted;
            public bool Solved => Allocation != null;
        }

        public static Result Solve(BoardDef board, int nodeLimit = int.MaxValue)
        {
            var targets = board.Targets.OrderBy(t => t.TargetId, StringComparer.Ordinal).ToList();
            var objects = board.Objects
                .OrderBy(o => targets.Count(t => t.AcceptsCategory(o.CategoryId)))
                .ThenByDescending(o => o.Units)
                .ThenBy(o => o.InstanceId, StringComparer.Ordinal)
                .ToList();
            var residual = targets.Select(t => t.IsUnlimited ? int.MaxValue / 4 : t.Capacity).ToArray();
            var slotsLeft = targets.Select(t => Math.Max(t.SlotCount, 0)).ToArray();
            var chosen = new int[objects.Count];
            var failed = new HashSet<string>();
            var res = new Result();

            bool Rec(int i)
            {
                if (++res.Nodes > nodeLimit) { res.Aborted = true; return false; }
                if (i == objects.Count) return true;
                string key = i + ":" + string.Join(",", residual) + ":" + string.Join(",", slotsLeft);
                if (failed.Contains(key)) return false;

                // Prune: remaining finite demand vs remaining finite capacity when no unlimited target can absorb it.
                int remainingUnits = 0;
                for (int k = i; k < objects.Count; k++) remainingUnits += objects[k].Units;
                long totalResidual = 0;
                for (int t = 0; t < targets.Count; t++) totalResidual += residual[t];
                if (remainingUnits > totalResidual) { failed.Add(key); return false; }

                var o = objects[i];
                for (int t = 0; t < targets.Count; t++)
                {
                    if (!targets[t].AcceptsCategory(o.CategoryId)) continue;
                    if (residual[t] < o.Units || slotsLeft[t] <= 0) continue;
                    residual[t] -= o.Units; slotsLeft[t]--; chosen[i] = t;
                    if (Rec(i + 1)) return true;
                    residual[t] += o.Units; slotsLeft[t]++;
                    if (res.Aborted) return false;
                }
                failed.Add(key);
                return false;
            }

            if (Rec(0))
            {
                res.Allocation = new Dictionary<string, string>();
                for (int i = 0; i < objects.Count; i++) res.Allocation[objects[i].InstanceId] = targets[chosen[i]].TargetId;
            }
            return res;
        }
    }

    public enum HintKind { None, Place, Transfer, Return }

    public readonly struct Hint
    {
        public readonly HintKind Kind;
        public readonly string ObjectId;
        public readonly string TargetId; // null for Return
        public readonly int BoardRevision;
        public Hint(HintKind kind, string obj, string target, int rev) { Kind = kind; ObjectId = obj; TargetId = target; BoardRevision = rev; }
    }

    /// <summary>Executable next-step hint toward a fixed desired allocation (Section 26).</summary>
    public static class HintService
    {
        static readonly Dictionary<string, Dictionary<string, string>> Cache = new Dictionary<string, Dictionary<string, string>>();

        public static Dictionary<string, string> DesiredAllocation(string levelId, int boardContentVersion, BoardDef board)
        {
            string key = levelId + "|" + board.BoardId + "|" + boardContentVersion;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var r = Solver.Solve(board, 20000);
            if (r.Solved) Cache[key] = r.Allocation;
            return r.Allocation;
        }

        public static Hint Next(BoardState s, Dictionary<string, string> desired)
        {
            if (desired == null || s.IsSolved) return new Hint(HintKind.None, null, null, s.Revision);
            bool HasRoom(string targetId, string objectId)
            {
                var t = s.Def.Target(targetId);
                return t.IsUnlimited || s.UnitsIn(targetId) + s.Def.Object(objectId).Units <= t.Capacity;
            }

            foreach (var o in s.Def.Objects.OrderBy(o => o.InstanceId, StringComparer.Ordinal))
            {
                if (s.IsAssigned(o.InstanceId)) continue;
                var want = desired[o.InstanceId];
                if (HasRoom(want, o.InstanceId)) return new Hint(HintKind.Place, o.InstanceId, want, s.Revision);
            }
            foreach (var o in s.Def.Objects.OrderBy(o => o.InstanceId, StringComparer.Ordinal))
            {
                var cur = s.TargetOf(o.InstanceId);
                if (cur == null) continue;
                var want = desired[o.InstanceId];
                if (cur == want) continue;
                if (HasRoom(want, o.InstanceId)) return new Hint(HintKind.Transfer, o.InstanceId, want, s.Revision);
                return new Hint(HintKind.Return, o.InstanceId, null, s.Revision);
            }
            return new Hint(HintKind.None, null, null, s.Revision);
        }
    }
}
