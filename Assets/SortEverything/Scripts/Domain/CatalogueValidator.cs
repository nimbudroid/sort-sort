using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SortEverything.Domain
{
    public sealed class Diagnostic
    {
        public string LevelId, BoardId, Field, Reason;
        public override string ToString() => $"{LevelId}/{BoardId} {Field}: {Reason}";
    }

    /// <summary>Structural and allocation validation (Sections 16 and 27). Produces a report and diagnostics.</summary>
    public static class CatalogueValidator
    {
        public static List<Diagnostic> Validate(Catalogue cat, StringBuilder report = null)
        {
            var d = new List<Diagnostic>();
            void Fail(string level, string board, string field, string reason) =>
                d.Add(new Diagnostic { LevelId = level, BoardId = board, Field = field, Reason = reason });

            var levelIds = new HashSet<string>();
            var rewardIds = new HashSet<string>();
            var assetIds = new HashSet<string>(KitchenCatalogue.Assets.Select(a => a.AssetId));
            report?.AppendLine($"profile={cat.Profile.ProfileId} contentVersion={KitchenCatalogue.ContentVersion} saveSchema=4");

            int boardCount = 0;
            foreach (var l in cat.Levels)
            {
                if (!levelIds.Add(l.LevelId)) Fail(l.LevelId, "", "levelId", "duplicate");
                if (!rewardIds.Add(l.CoinRewardId)) Fail(l.LevelId, "", "coinRewardId", "duplicate");
                if (l.CoinRewardId != "campaign_complete:" + l.LevelId) Fail(l.LevelId, "", "coinRewardId", "unstable");
                if (l.FirstCompletionCoins != 50) Fail(l.LevelId, "", "firstCompletionCoins", "must be 50");
                if (!string.IsNullOrEmpty(l.PreviewLevelId) && cat.Level(l.PreviewLevelId) == null) Fail(l.LevelId, "", "previewLevelId", "missing");
                if (l.PreviewLevelId != "" && cat.Level(l.PreviewLevelId)?.AreaId != l.AreaId) Fail(l.LevelId, "", "previewLevelId", "crosses area");
                if (l.Boards.Count(b => b.Required) != 1) Fail(l.LevelId, "", "boards", "exactly one required board");
                bool planningOnly = l.Boards.All(b => b.IsPlanning);
                if (l.CanDeferLevel != planningOnly) Fail(l.LevelId, "", "canDeferLevel", "must equal planning-only");

                foreach (var b in l.Boards)
                {
                    boardCount++;
                    var ids = new HashSet<string>();
                    foreach (var o in b.Objects)
                    {
                        if (!ids.Add(o.InstanceId)) Fail(l.LevelId, b.BoardId, "instanceId", "duplicate " + o.InstanceId);
                        if (!assetIds.Contains(o.AssetId)) Fail(l.LevelId, b.BoardId, "assetId", "unknown " + o.AssetId);
                        if (o.Units < 1 || o.Units > 2) Fail(l.LevelId, b.BoardId, "units", o.InstanceId);
                        if (o.Large != (o.Units == 2)) Fail(l.LevelId, b.BoardId, "visualVariant", o.InstanceId);
                        if (o.SourceX <= 0 || o.SourceX >= 1 || o.SourceY <= 0 || o.SourceY >= 1) Fail(l.LevelId, b.BoardId, "sourceXY", o.InstanceId);
                        if (!b.Targets.Any(t => t.AcceptsCategory(o.CategoryId))) Fail(l.LevelId, b.BoardId, "accepts", "no target for " + o.InstanceId);
                    }
                    if (b.Objects.Count > 12) Fail(l.LevelId, b.BoardId, "objects", "more than twelve");
                    var srcPositions = b.Objects.Select(o => (o.SourceX, o.SourceY)).ToList();
                    if (srcPositions.Distinct().Count() != srcPositions.Count) Fail(l.LevelId, b.BoardId, "sourceXY", "overlapping source slots");
                    foreach (var t in b.Targets)
                    {
                        if (t.Capacity == 0 || t.Capacity < -1) Fail(l.LevelId, b.BoardId, "capacity", t.TargetId);
                        if (t.SlotCount < 1) Fail(l.LevelId, b.BoardId, "slots", t.TargetId);
                        int maxInstances = b.Objects.Count(o => t.AcceptsCategory(o.CategoryId));
                        if (t.IsUnlimited && t.SlotCount < maxInstances) Fail(l.LevelId, b.BoardId, "slots", "too few for " + t.TargetId);
                    }
                    if (b.Targets.Select(t => t.TargetId).Distinct().Count() != b.Targets.Count) Fail(l.LevelId, b.BoardId, "targetId", "duplicate");

                    var sol = Solver.Solve(b);
                    if (!sol.Solved) Fail(l.LevelId, b.BoardId, "allocation", "no complete legal assignment");
                    if (report != null)
                    {
                        report.Append($"{l.LevelId}/{b.BoardId} {b.TemplateId} req={b.Required} objects={b.Objects.Count} units={b.TotalUnits} ");
                        report.Append("targets=[" + string.Join(", ", b.Targets.Select(t => $"{t.TargetId}:{(t.IsUnlimited ? "∞" : t.Capacity.ToString())}/{t.SlotCount}slots")) + "] ");
                        report.Append($"defer={l.CanDeferLevel} anchor={l.ProjectAnchorId} preview={cat.PreviewFor(l)} nodes={sol.Nodes} ");
                        if (sol.Solved)
                            report.Append("solution={" + string.Join(" ", sol.Allocation.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => k.Key + "→" + k.Value)) + "}");
                        report.AppendLine();
                    }
                }
            }

            // Economy audit (Sections 8 and 12).
            int allComplete = cat.Levels.Count * 50;
            int ordinary = KitchenCatalogue.OrdinaryScenes.Length * 50;
            int purchases = KitchenCatalogue.Anchors.Sum(a => a.VariantPrice);
            if (cat.Levels.Count == 20)
            {
                if (allComplete != 1000) Fail("", "", "economy", "all-complete must be 1000");
                if (ordinary - purchases != 150) Fail("", "", "economy", "ordinary route after purchases must be 150");
            }
            report?.AppendLine($"boards={boardCount} economy: all={allComplete} ordinary={ordinary} purchases={purchases} ordinaryAfter={ordinary - purchases}");
            report?.AppendLine(d.Count == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({d.Count} diagnostics)");
            foreach (var x in d) report?.AppendLine("  " + x);
            return d;
        }
    }
}
