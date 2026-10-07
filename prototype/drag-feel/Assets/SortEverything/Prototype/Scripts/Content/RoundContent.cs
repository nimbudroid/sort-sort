using System;
using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>One object in a round: which bin colour it belongs to, and which library object (null = shape).</summary>
    public struct RoundSlot
    {
        public SortColor color;
        public ObjectDef def;
    }

    /// <summary>
    /// Chooses library objects for a colour round. Plain C# so it can be tested outside Unity.
    /// Rules: no duplicate ids in a round, avoid ids from the last rounds, spread categories,
    /// and keep at least one heavy and two light objects when the pool allows (so weight lag stays testable).
    /// </summary>
    public static class RoundContent
    {
        public const int HeavyMass = 4;
        public const int LightMass = 2;
        const int MinHeavy = 1;
        const int MinLight = 2;

        /// <summary>Bin colours that have at least minCount pickable objects in the pool.</summary>
        public static List<SortColor> ColorsWithAtLeast(ObjectPool pool, int minCount)
        {
            var list = new List<SortColor>();
            for (int c = 0; c < 5; c++)
                if (ObjectLibrary.ColorSortable(pool, (SortColor)c).Count >= minCount) list.Add((SortColor)c);
            return list;
        }

        /// <summary>
        /// Fill the given colour slots. realSlot[i] false leaves slot i as a shape (def = null).
        /// rand(n) must return an int in [0, n).
        /// </summary>
        public static RoundSlot[] Fill(SortColor[] slotColors, bool[] realSlot, ObjectPool pool,
            ICollection<string> recentIds, Func<int, int> rand)
        {
            int n = slotColors.Length;
            var slots = new RoundSlot[n];
            var used = new HashSet<string>();
            var categoryCount = new Dictionary<ObjectCategory, int>();

            // Visit slots in random order so category spreading doesn't favour the first bin.
            var order = new List<int>();
            for (int i = 0; i < n; i++) order.Add(i);
            Shuffle(order, rand);

            for (int k = 0; k < order.Count; k++)
            {
                int i = order[k];
                slots[i].color = slotColors[i];
                if (!realSlot[i]) continue;
                var best = Best(ObjectLibrary.ColorSortable(pool, slotColors[i]), used, categoryCount, recentIds, rand, null);
                if (best == null) continue; // pool exhausted for this colour; caller falls back to a shape
                slots[i].def = best;
                used.Add(best.id);
                int cc;
                categoryCount.TryGetValue(best.category, out cc);
                categoryCount[best.category] = cc + 1;
            }

            EnsureMassSpread(slots, pool, used, recentIds, rand, true);
            EnsureMassSpread(slots, pool, used, recentIds, rand, false);
            return slots;
        }

        static ObjectDef Best(List<ObjectDef> candidates, HashSet<string> used, Dictionary<ObjectCategory, int> categoryCount,
            ICollection<string> recentIds, Func<int, int> rand, Func<ObjectDef, bool> filter)
        {
            ObjectDef best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var d = candidates[i];
                if (used.Contains(d.id)) continue;
                if (filter != null && !filter(d)) continue;
                int cc;
                categoryCount.TryGetValue(d.category, out cc);
                float score = rand(10000) / 10000f + 0.25f * cc;
                if (recentIds != null && recentIds.Contains(d.id)) score += 2f;
                if (score < bestScore) { bestScore = score; best = d; }
            }
            return best;
        }

        // Swap objects (same colour) until the round has enough heavy (or light) objects, if the pool allows.
        static void EnsureMassSpread(RoundSlot[] slots, ObjectPool pool, HashSet<string> used, ICollection<string> recentIds,
            Func<int, int> rand, bool heavy)
        {
            Func<int, bool> isTarget = m => heavy ? m >= HeavyMass : m <= LightMass;
            int need = heavy ? MinHeavy : MinLight;
            for (int guard = 0; guard < 10 && Count(slots, isTarget) < need; guard++)
            {
                bool swapped = false;
                var idx = new List<int>();
                for (int i = 0; i < slots.Length; i++) idx.Add(i);
                Shuffle(idx, rand);
                foreach (int i in idx)
                {
                    var cur = slots[i].def;
                    if (cur == null || isTarget(cur.mass)) continue;
                    // Don't break the other constraint while fixing this one.
                    if (!heavy && cur.mass >= HeavyMass && Count(slots, m => m >= HeavyMass) <= MinHeavy) continue;
                    if (heavy && cur.mass <= LightMass && Count(slots, m => m <= LightMass) <= MinLight) continue;
                    var replacement = Best(ObjectLibrary.ColorSortable(pool, slots[i].color), used,
                        new Dictionary<ObjectCategory, int>(), recentIds, rand, d => isTarget(d.mass));
                    if (replacement == null) continue;
                    used.Remove(cur.id);
                    used.Add(replacement.id);
                    slots[i].def = replacement;
                    swapped = true;
                    break;
                }
                if (!swapped) break;
            }
        }

        static int Count(RoundSlot[] slots, Func<int, bool> massTest)
        {
            int n = 0;
            for (int i = 0; i < slots.Length; i++) if (slots[i].def != null && massTest(slots[i].def.mass)) n++;
            return n;
        }

        public static void Shuffle<T>(List<T> list, Func<int, int> rand)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rand(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
