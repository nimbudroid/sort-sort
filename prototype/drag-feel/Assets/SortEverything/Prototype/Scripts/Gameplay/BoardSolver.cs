using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Bounded backtracking over unit costs and capacities (GDD §16): finds a complete legal assignment for a board,
    /// or proves there is none. Two-unit objects are indivisible. Used by validation now and by hints later.
    /// </summary>
    public static class BoardSolver
    {
        /// <summary>One complete assignment (object -> target), or null when the board cannot be solved.</summary>
        public static int[] Solve(BoardDef board)
        {
            int[] result = null;
            Count(board, 1, a => result = (int[])a.Clone());
            return result;
        }

        /// <summary>Number of complete assignments, counting at most `limit`.</summary>
        public static int CountSolutions(BoardDef board, int limit = 50)
        {
            return Count(board, limit, null);
        }

        static int Count(BoardDef board, int limit, System.Action<int[]> onSolution)
        {
            int n = board.objects.Length, t = board.targets.Length;
            var defs = new ObjectDef[n];
            for (int i = 0; i < n; i++) defs[i] = ObjectLibrary.Get(board.objects[i].asset);
            // Larger objects first: they are the hardest to fit.
            var order = new List<int>();
            for (int i = 0; i < n; i++) order.Add(i);
            order.Sort((a, b) => board.objects[b].units != board.objects[a].units
                ? board.objects[b].units.CompareTo(board.objects[a].units) : a.CompareTo(b));
            var free = new int[t];
            for (int k = 0; k < t; k++) free[k] = board.targets[k].capacity < 0 ? int.MaxValue / 2 : board.targets[k].capacity;
            var assign = new int[n];
            int found = 0;
            Recurse(0, order, board, defs, free, assign, ref found, limit, onSolution);
            return found;
        }

        static void Recurse(int depth, List<int> order, BoardDef board, ObjectDef[] defs, int[] free, int[] assign,
            ref int found, int limit, System.Action<int[]> onSolution)
        {
            if (found >= limit) return;
            if (depth == order.Count)
            {
                found++;
                if (onSolution != null) onSolution(assign);
                return;
            }
            int obj = order[depth];
            int units = board.objects[obj].units;
            for (int k = 0; k < board.targets.Length; k++)
            {
                if (free[k] < units || !RuleEvaluator.Accepts(board.targets[k], defs[obj])) continue;
                free[k] -= units;
                assign[obj] = k;
                Recurse(depth + 1, order, board, defs, free, assign, ref found, limit, onSolution);
                free[k] += units;
                if (found >= limit) return;
            }
        }
    }
}
