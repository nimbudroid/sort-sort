using System;
using System.Collections.Generic;
using System.Linq;
using SortEverything.Domain;
using SortEverything.Runtime;
using UnityEngine;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Tunable board proportions, loaded from Resources/SortEverything/BoardLayout.json (no code change needed).
    /// Shares are fractions of the safe-area height; sizes are canvas units (≈ dp at the 360-wide reference).
    /// </summary>
    [Serializable]
    public sealed class BoardLayoutConfig
    {
        public float headerShare = 0.08f, instructionShare = 0.05f, playShare = 0.55f, containerShare = 0.32f;
        public float maxBoardWidth = 520f, landscapeMaxWidthToHeight = 0.78f;
        public float playPadding = 12f;
        public float itemMinVisible = 56f, itemMaxVisible = 96f, itemHitMin = 48f;
        public float pileFillShare = 0.36f, rotationRange = 12f, maxOverlap = 0.2f;
        public float binSpanShare = 0.9f, binGutter = 10f, binRowGap = 8f, binLabelHeight = 24f, binLipShare = 0.17f, binItemFill = 0.9f;
        public int maxBinsPerRow = 3;
        public float itemInBinMin = 30f, itemInBinMax = 66f;
        public float dragLiftScale = 1.08f;
        public bool messEnabled = true;

        static BoardLayoutConfig _current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => _current = null;

        public static BoardLayoutConfig Current
        {
            get
            {
                if (_current != null) return _current;
                var ta = Resources.Load<TextAsset>("SortEverything/BoardLayout");
                try { _current = ta != null ? JsonUtility.FromJson<BoardLayoutConfig>(ta.text) : new BoardLayoutConfig(); }
                catch (Exception e) { Debug.LogError("Sort Everything: BoardLayout.json invalid, using defaults: " + e.Message); _current = new BoardLayoutConfig(); }
                return _current;
            }
        }
    }

    /// <summary>Where one item rests on the counter.</summary>
    public struct PilePlacement
    {
        public Vector2 Center;   // board units, y down
        public float Visible;    // drawn size of the item square
        public float Hit;        // touch square (≥ itemHitMin)
        public float Rotation;   // degrees
        public int Layer;        // draw order (0 = bottom)
    }

    /// <summary>One container's rectangle and the cells its items settle into.</summary>
    public sealed class BinPlacement
    {
        public string TargetId;
        public Rect Rect;            // board units, y down
        public Rect Interior;        // where items sit (below the label tag)
        public int Cols, Rows;
        public float ItemSize;       // drawn size of a sorted item in this bin
        public float CellSide;       // square cell the items settle into (compact, never bigger than needed)
        public float LipHeight;      // front lip that overlaps the bottom row a little

        /// <summary>
        /// Items settle like real things in a container: the bottom row fills first, rows are centered, and the grid
        /// sits on the container floor (a deep basket keeps its space above the items).
        /// </summary>
        public Vector2 Cell(int slot, int slotCount)
        {
            int count = Mathf.Max(1, slotCount);
            int col = slot % Cols, rowFromBottom = slot / Cols;
            int inRow = Mathf.Min(Cols, count - rowFromBottom * Cols);
            float rowW = inRow * CellSide;
            float x0 = Interior.center.x - rowW / 2f;
            float y = Interior.yMax - (rowFromBottom + 0.5f) * CellSide;
            return new Vector2(x0 + (col + 0.5f) * CellSide, y);
        }
    }

    /// <summary>
    /// The one board layout system. From the safe-area size and a board definition it derives the zones
    /// (header, instruction, play surface, containers), the board width, a seeded natural item pile and the
    /// container rows. Pure geometry: it never decides what is correct; that stays in the RuleEvaluator.
    /// </summary>
    public sealed class BoardLayout
    {
        public float Width;            // board width (centered in the screen; the room bleeds past it)
        public float HeaderH, InstructionH, PlayTop, PlayH, ContainersTop, ContainersH, TotalH;
        public Rect PlayRect;          // counter surface, board units
        public readonly Dictionary<string, PilePlacement> Pile = new Dictionary<string, PilePlacement>();
        public readonly Dictionary<string, BinPlacement> Bins = new Dictionary<string, BinPlacement>();
        public string BinRule = "";     // which overflow rule applied (for the report/debug)
        public float BaseItem;          // pile base size before relativeSize

        public static BoardLayout Compute(BoardDef def, float safeW, float safeH, BoardLayoutConfig cfg = null)
        {
            cfg = cfg ?? BoardLayoutConfig.Current;
            var L = new BoardLayout();
            float total = cfg.headerShare + cfg.instructionShare + cfg.playShare + cfg.containerShare;
            float k = total > 0 ? 1f / total : 1f; // shares are normalized so they always fill the safe area
            L.TotalH = safeH;
            L.HeaderH = safeH * cfg.headerShare * k;
            L.InstructionH = safeH * cfg.instructionShare * k;
            L.PlayH = safeH * cfg.playShare * k;
            L.ContainersH = safeH * cfg.containerShare * k;
            float w = Mathf.Min(safeW, cfg.maxBoardWidth);
            if (safeW > safeH) w = Mathf.Min(w, safeH * cfg.landscapeMaxWidthToHeight); // portrait game on a landscape screen
            L.Width = Mathf.Floor(w);
            // Board-local coordinates start below the header + instruction line.
            L.PlayTop = 0;
            L.ContainersTop = L.PlayH;
            float pad = cfg.playPadding;
            L.PlayRect = new Rect(pad, L.PlayTop + pad, L.Width - 2 * pad, L.PlayH - 2 * pad - 8); // 8 = counter lip
            L.LayoutBins(def, cfg);
            L.LayoutPile(def, cfg);
            return L;
        }

        // ---------- Containers ----------

        static float AuthoredWidth(TargetDef t)
        {
            switch (t.RectProfile)
            {
                case "narrow": return 0.31f;
                case "full_width": return 0.96f;
                default: return 0.46f;
            }
        }

        void LayoutBins(BoardDef def, BoardLayoutConfig cfg)
        {
            // Rows follow the authored layout (targets sharing a Y form a row, left to right by X); a row longer
            // than maxBinsPerRow wraps. Every row spans binSpanShare of the board width with even gutters.
            var rows = new List<List<TargetDef>>();
            foreach (var g in def.Targets.GroupBy(t => Mathf.Round(t.Y * 20f)).OrderBy(g => g.Key))
            {
                var ordered = g.OrderBy(t => t.X).ToList();
                for (int i = 0; i < ordered.Count; i += Mathf.Max(1, cfg.maxBinsPerRow))
                    rows.Add(ordered.Skip(i).Take(Mathf.Max(1, cfg.maxBinsPerRow)).ToList());
            }
            BinRule = rows.Count > 1 ? $"{rows.Count} rows" : "one row";
            float span = Width * cfg.binSpanShare;
            float left = (Width - span) / 2f;
            float top = ContainersTop + 6f, avail = ContainersH - 12f;
            // Rows share the height in proportion to how many slot rows they need (at least one each).
            var need = rows.Select(r => Mathf.Max(1f, r.Max(t => SlotRowsFor(t, span * AuthoredWidth(t) / Mathf.Max(0.01f, r.Sum(AuthoredWidth)))))).ToList();
            float needSum = need.Sum();
            float y = top;
            for (int ri = 0; ri < rows.Count; ri++)
            {
                var row = rows[ri];
                float rowH = (avail - cfg.binRowGap * (rows.Count - 1)) * need[ri] / needSum;
                float weights = row.Sum(AuthoredWidth);
                float usable = span - cfg.binGutter * (row.Count - 1);
                float x = left;
                foreach (var t in row)
                {
                    float bw = usable * AuthoredWidth(t) / weights;
                    var rect = new Rect(x, y, bw, rowH);
                    Bins[t.TargetId] = MakeBin(t, rect, cfg);
                    x += bw + cfg.binGutter;
                }
                y += rowH + cfg.binRowGap;
            }
            float smallest = Bins.Values.Count > 0 ? Bins.Values.Min(b => b.ItemSize) : 0;
            if (smallest <= cfg.itemInBinMin + 0.01f) BinRule += $", items scaled to the {cfg.itemInBinMin:0}-unit minimum";
        }

        static int SlotRowsFor(TargetDef t, float width)
        {
            int n = Mathf.Max(1, t.SlotCount);
            int cols = Mathf.Clamp(Mathf.FloorToInt(width / 52f), 1, n);
            return Mathf.CeilToInt(n / (float)cols);
        }

        static BinPlacement MakeBin(TargetDef t, Rect rect, BoardLayoutConfig cfg)
        {
            var b = new BinPlacement { TargetId = t.TargetId, Rect = rect };
            float lip = Mathf.Min(rect.height * cfg.binLipShare, 34f) * 0.5f; // the lip may cover the bottom of the lowest row a little
            b.Interior = new Rect(rect.x + 8, rect.y + cfg.binLabelHeight + 4, rect.width - 16, rect.height - cfg.binLabelHeight - 8 - lip);
            int n = Mathf.Max(1, t.SlotCount);
            // Choose the grid that gives the biggest square cell for n slots in this interior.
            float best = -1; int bc = 1, br = n;
            for (int cols = 1; cols <= n; cols++)
            {
                int rows = Mathf.CeilToInt(n / (float)cols);
                float cell = Mathf.Min(b.Interior.width / cols, b.Interior.height / rows);
                if (cell > best + 0.01f) { best = cell; bc = cols; br = rows; }
            }
            b.Cols = bc; b.Rows = br;
            b.CellSide = Mathf.Min(best, cfg.itemInBinMax / Mathf.Max(0.1f, cfg.binItemFill) + 6f);
            b.ItemSize = Mathf.Clamp(b.CellSide * cfg.binItemFill, cfg.itemInBinMin, cfg.itemInBinMax);
            b.LipHeight = Mathf.Clamp(rect.height * cfg.binLipShare, 12f, b.CellSide * 0.42f + 6f);
            return b;
        }

        public Vector2 SlotPosition(string targetId, int slot, int slotCount) => Bins[targetId].Cell(slot, slotCount);

        /// <summary>Drawn size of an item resting in a bin (relativeSize applied, never larger than its cell).</summary>
        public float BinItemSize(string targetId, string assetId)
        {
            var b = Bins[targetId];
            float rel = Mathf.Clamp(ContentDb.Object(assetId).relativeSize, 0.6f, 1.6f);
            return Mathf.Min(b.ItemSize * Mathf.Lerp(1f, rel, 0.5f), b.CellSide * 1.02f);
        }

        // ---------- Item pile ----------

        static float Rand(ref uint s) { unchecked { s ^= s << 13; s ^= s >> 17; s ^= s << 5; } return (s & 0xFFFFFF) / (float)0x1000000; }

        static uint Seed(string a, string b)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in a) { h ^= c; h *= 16777619u; }
                foreach (char c in b) { h ^= c; h *= 16777619u; }
                return h == 0 ? 1u : h;
            }
        }

        void LayoutPile(BoardDef def, BoardLayoutConfig cfg)
        {
            var objs = def.Objects.ToList();
            if (objs.Count == 0) return;
            var rel = objs.ToDictionary(o => o.InstanceId, o => Mathf.Clamp(ContentDb.Object(o.AssetId).relativeSize > 0 ? ContentDb.Object(o.AssetId).relativeSize : 1f, 0.6f, 1.6f) * (o.Large ? 1.1f : 1f));
            float minRel = rel.Values.Min();
            // Base size: fill a share of the counter (few items are scaled up), clamped so the smallest item stays
            // ≥ itemMinVisible and nothing exceeds itemMaxVisible. If the counter is too small even for that, fall
            // back to the touch-target floor (itemHitMin) instead of overflowing.
            float area = PlayRect.width * PlayRect.height;
            float sumRel2 = rel.Values.Sum(r => r * r);
            float fit = Mathf.Sqrt(area * cfg.pileFillShare / sumRel2);
            float floor = cfg.itemMinVisible / minRel;
            float tight = Mathf.Sqrt(area * 0.62f / sumRel2); // densest acceptable packing
            BaseItem = Mathf.Min(Mathf.Max(fit, floor), cfg.itemMaxVisible / rel.Values.Max());
            if (BaseItem > tight) BaseItem = Mathf.Max(tight, cfg.itemHitMin / minRel);

            uint s = Seed(def.BoardId, string.Join(",", objs.Select(o => o.InstanceId)));
            var placed = new List<(Vector2 c, float r)>();
            // Large items first so they sit underneath and small ones stay on top (never fully hidden).
            var order = objs.OrderByDescending(o => rel[o.InstanceId]).ThenBy(o => o.SourceIndex).ToList();
            for (int i = 0; i < order.Count; i++)
            {
                var o = order[i];
                float vis = BaseItem * rel[o.InstanceId];
                float r = vis * 0.4f; // the sprite's body roughly fills 80% of its square
                float margin = r + 2f;
                Vector2 best = PlayRect.center; float bestScore = float.MaxValue;
                for (int c = 0; c < 80; c++)
                {
                    // Bias toward the authored grid spot so the layout stays stable and readable, plus a random spread.
                    var authored = new Vector2(PlayRect.x + o.SourceX * PlayRect.width, PlayRect.y + o.SourceY * PlayRect.height);
                    var rnd = new Vector2(PlayRect.x + margin + Rand(ref s) * (PlayRect.width - 2 * margin), PlayRect.y + margin + Rand(ref s) * (PlayRect.height - 2 * margin));
                    var p = Vector2.Lerp(authored, rnd, c < 40 ? 0.35f : 0.8f);
                    p.x = Mathf.Clamp(p.x, PlayRect.xMin + margin, PlayRect.xMax - margin);
                    p.y = Mathf.Clamp(p.y, PlayRect.yMin + margin, PlayRect.yMax - margin);
                    float violation = 0, crowd = 0;
                    foreach (var q in placed)
                    {
                        float cover = Covered(p, r, q.c, q.r);
                        violation += Mathf.Max(0, cover - cfg.maxOverlap) * 10f;
                        crowd += cover;
                    }
                    float score = violation + crowd * 0.6f + (p - authored).magnitude / Mathf.Max(PlayRect.width, 1f) * 0.4f + Rand(ref s) * 0.05f;
                    if (score < bestScore) { bestScore = score; best = p; }
                }
                placed.Add((best, r));
                Pile[o.InstanceId] = new PilePlacement
                {
                    Center = best,
                    Visible = vis,
                    Hit = Mathf.Max(cfg.itemHitMin, vis * 0.9f),
                    Rotation = (Rand(ref s) * 2f - 1f) * cfg.rotationRange,
                    Layer = i,
                };
            }
        }

        /// <summary>Approximate fraction of the smaller circle covered by the other (0..1).</summary>
        public static float Covered(Vector2 a, float ra, Vector2 b, float rb)
        {
            float d = (a - b).magnitude;
            float overlap = ra + rb - d;
            if (overlap <= 0) return 0;
            return Mathf.Clamp01(overlap / (2f * Mathf.Min(ra, rb)));
        }

        /// <summary>Largest fraction of any pile item covered by items drawn above it (for tests and the report).</summary>
        public float MaxPileCover()
        {
            float worst = 0;
            var list = Pile.Values.OrderBy(p => p.Layer).ToList();
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                    worst = Mathf.Max(worst, Covered(list[i].Center, list[i].Visible * 0.4f, list[j].Center, list[j].Visible * 0.4f));
            return worst;
        }
    }
}
