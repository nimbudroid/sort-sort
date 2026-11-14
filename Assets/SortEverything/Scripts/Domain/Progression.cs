using System;
using System.Collections.Generic;
using System.Linq;

namespace SortEverything.Domain
{
    public enum LevelStatus { Locked, Available, InProgress, Later, Completed }

    public sealed class CompletionFacts
    {
        public string LevelId;
        public int CoinsEarned;
        public bool NewSeal;
        public List<string> NewDiscoveries = new List<string>();
        public bool TraceAdded;
        public string AnchorUnlocked = "";
        public List<string> NewRecognitions = new List<string>();
        public List<string> NewBadges = new List<string>();
        public bool NewBestTime;
        public long ActiveMs;
        public bool OptionalBoardOnly;
    }

    /// <summary>
    /// Pure progression rules. Every method mutates a caller-owned candidate save and never writes to disk;
    /// the outer command commits the candidate once (Section 26).
    /// </summary>
    public static class Progression
    {
        public const string KitchenReady = "kitchen_ready";
        public const string KitchenMaster = "kitchen_master";
        public const string KitchenCollectionComplete = "kitchen_collection_complete";

        // ---------- Queries ----------

        public static LevelStatus Status(SaveData s, Catalogue cat, LevelDef level)
        {
            if (s.HasSeal(level.LevelId))
            {
                // Level 20 with saved optional board still shows its seal; Later Puzzles lists it separately.
                return LevelStatus.Completed;
            }
            if (s.IsDeferred(level.LevelId)) return LevelStatus.Later;
            if (!IsAvailable(s, cat, level)) return LevelStatus.Locked;
            var a = s.AttemptFor(level.LevelId);
            if (a != null && a.boards.Any(b => b.assignments.Count > 0 || b.status == BoardStatus.Solved || b.status == BoardStatus.Deferred))
                return LevelStatus.InProgress;
            return LevelStatus.Available;
        }

        public static bool IsAvailable(SaveData s, Catalogue cat, LevelDef level)
        {
            if (!cat.IsInstalled(level.LevelId)) return false;
            if (!s.unlockedAreaIds.Contains(level.AreaId)) return false;
            if (s.HasSeal(level.LevelId) || s.IsDeferred(level.LevelId)) return true;
            return level.OrdinalInArea <= s.Cursor(level.AreaId);
        }

        /// <summary>True when an optional board of a completed scene is still saved for later.</summary>
        public static bool HasLaterOptionalBoard(SaveData s, LevelDef level)
        {
            if (!level.HasOptionalBoard || !s.HasSeal(level.LevelId)) return false;
            return !s.boardCompleteMarkers.Contains(BoardMarker(level.LevelId, level.OptionalBoard.BoardId));
        }

        public static IEnumerable<LevelDef> LaterPuzzles(SaveData s, Catalogue cat, string areaId) =>
            cat.InstalledInArea(areaId).Where(l => s.IsDeferred(l.LevelId) || HasLaterOptionalBoard(s, l));

        /// <summary>The area cursor scene, or null when the installed route is traversed.</summary>
        public static LevelDef CursorLevel(SaveData s, Catalogue cat, string areaId)
        {
            var installed = cat.InstalledInArea(areaId);
            int c = s.Cursor(areaId);
            return installed.FirstOrDefault(l => l.OrdinalInArea == c);
        }

        public static bool AreaRouteTraversed(SaveData s, Catalogue cat, string areaId) => CursorLevel(s, cat, areaId) == null;

        public static string BoardMarker(string levelId, string boardId) => $"board_complete:{levelId}:{boardId}";
        public static string TraceId(string levelId) => "trace:" + levelId;
        public static string BadgeId(string rule, string levelId, string boardId) => $"{rule}:{levelId}:{boardId}";

        // ---------- Attempts ----------

        public static AttemptData CreateAttempt(SaveData s, LevelDef level, string entryKind, Func<string> newId, string configSignature)
        {
            var a = new AttemptData
            {
                attemptId = newId(),
                levelId = level.LevelId,
                contentVersion = level.ContentVersion,
                boardContentVersion = level.BoardContentVersion,
                entryKind = entryKind,
                currentBoardId = level.FirstBoard.BoardId,
                configSignature = configSignature,
            };
            foreach (var b in level.Boards)
                a.boards.Add(new BoardRunData { boardId = b.BoardId, boardRunId = newId() });
            s.attempts.RemoveAll(x => x.levelId == level.LevelId);
            s.attempts.Add(a);
            return a;
        }

        public static void WriteBoard(BoardRunData run, BoardState state)
        {
            run.assignments = state.Assignments()
                .Select(x => new AssignmentData { instanceId = x.instanceId, targetId = x.targetId, slot = x.slot }).ToList();
            if (run.status == BoardStatus.Unstarted && state.SortedCount > 0) run.status = BoardStatus.Playing;
        }

        public static BoardState ReadBoard(BoardDef def, BoardRunData run) =>
            BoardState.FromAssignments(def, run.assignments.Select(x => (x.instanceId, x.targetId, x.slot)), out _) ?? new BoardState(def);

        /// <summary>Applies the bookkeeping of an accepted drop to the board run.</summary>
        public static void RecordAccepted(BoardRunData run, BoardDef def, BoardState next, BoardFacts f)
        {
            WriteBoard(run, next);
            if (run.status == BoardStatus.Unstarted) run.status = BoardStatus.Playing;
            if (f.FirstAssignment && !run.firstCredits.Contains(f.ObjectId))
            {
                run.firstCredits.Add(f.ObjectId);
                if (!def.IsPlanning) run.streak++;
            }
            if (f.Transfer || f.Return) run.legalRearrangements++;
            if (f.ToTarget != null)
            {
                var asset = def.Object(f.ObjectId).AssetId;
                if (!run.pendingDiscoveries.Contains(asset)) run.pendingDiscoveries.Add(asset);
            }
        }

        public static void RecordRejection(BoardRunData run, DropKind kind)
        {
            if (kind == DropKind.WrongCategory) run.categoryRejections++;
            else if (kind == DropKind.Full) run.fullRejections++;
            else return;
            run.streak = 0;
            if (run.status == BoardStatus.Unstarted) run.status = BoardStatus.Playing;
        }

        public static void MarkAssistance(BoardRunData run, string kind)
        {
            run.assisted = true;
            if (!run.assistanceKinds.Contains(kind)) run.assistanceKinds.Add(kind);
        }

        /// <summary>Restart Board: resets only the current unsolved board; keeps assistance exposure.</summary>
        public static void RestartBoard(BoardRunData run, Func<string> newId)
        {
            bool assisted = run.assisted;
            var kinds = run.assistanceKinds.ToList();
            run.assignments.Clear();
            run.pendingDiscoveries.Clear();
            run.activeMs = 0;
            run.categoryRejections = run.fullRejections = run.legalRearrangements = 0;
            run.firstCredits.Clear();
            run.streak = 0;
            run.status = BoardStatus.Unstarted;
            run.boardRunId = newId();
            run.assisted = assisted;
            run.assistanceKinds = kinds;
        }

        // ---------- Completion ----------

        static void ApplyBoardRecord(SaveData s, LevelDef level, AttemptData a, BoardRunData run, BoardDef def, CompletionFacts facts)
        {
            bool unassisted = !run.assisted;
            bool clean = unassisted && run.categoryRejections == 0 && run.fullRejections == 0;
            if (clean)
            {
                var badge = BadgeId(def.RecordRule, level.LevelId, def.BoardId);
                if (!s.badges.Contains(badge)) { s.badges.Add(badge); facts.NewBadges.Add(badge); }
            }
            var rec = s.records.FirstOrDefault(r => r.levelId == level.LevelId && r.boardId == def.BoardId
                && r.boardContentVersion == level.BoardContentVersion && r.configSignature == a.configSignature);
            if (rec == null)
            {
                rec = new BoardRecordData { levelId = level.LevelId, boardId = def.BoardId, boardContentVersion = level.BoardContentVersion, configSignature = a.configSignature };
                s.records.Add(rec);
            }
            if (rec.bestMs < 0 || run.activeMs < rec.bestMs) { if (rec.bestMs >= 0) facts.NewBestTime = true; rec.bestMs = run.activeMs; }
            if (unassisted && (rec.bestUnassistedMs < 0 || run.activeMs < rec.bestUnassistedMs)) rec.bestUnassistedMs = run.activeMs;
        }

        static void CommitDiscoveries(SaveData s, BoardRunData run, CompletionFacts facts)
        {
            foreach (var d in run.pendingDiscoveries)
                if (!s.discoveries.Contains(d)) { s.discoveries.Add(d); facts.NewDiscoveries.Add(d); }
            run.pendingDiscoveries.Clear();
            if (!s.recognitions.Contains(KitchenCollectionComplete) &&
                KitchenCatalogue.Assets.All(x => s.discoveries.Contains(x.AssetId)))
            {
                s.recognitions.Add(KitchenCollectionComplete);
                facts.NewRecognitions.Add(KitchenCollectionComplete);
            }
        }

        /// <summary>
        /// A board in the attempt became solved. For a required board this completes the level in the same candidate.
        /// Returns facts; when the board was an optional board before the finale, returns facts with OptionalBoardOnly
        /// and moves the attempt to the required board.
        /// </summary>
        public static CompletionFacts CompleteBoard(SaveData s, Catalogue cat, LevelDef level, AttemptData a)
        {
            var run = a.Current;
            var def = level.Board(run.boardId);
            run.status = BoardStatus.Solved;
            var facts = new CompletionFacts { LevelId = level.LevelId, ActiveMs = run.activeMs };

            if (!def.Required)
            {
                ApplyBoardRecord(s, level, a, run, def, facts);
                facts.OptionalBoardOnly = true;
                if (s.HasSeal(level.LevelId))
                {
                    // Optional board finished after the finale: discoveries + record only, wallet unchanged.
                    CommitDiscoveries(s, run, facts);
                    var marker = BoardMarker(level.LevelId, def.BoardId);
                    if (!s.boardCompleteMarkers.Contains(marker)) s.boardCompleteMarkers.Add(marker);
                    var req = a.Board(level.RequiredBoard.BoardId);
                    if (req.status == BoardStatus.Solved || a.entryKind != "replay_finish")
                    {
                        s.attempts.Remove(a);
                        if (s.selectedAttemptId == a.attemptId) s.selectedAttemptId = "";
                    }
                    else a.currentBoardId = req.boardId;
                    CheckMaster(s, facts);
                }
                else
                {
                    // Checkpoint; keep pending discoveries on the board until scene completion commits them.
                    a.currentBoardId = level.RequiredBoard.BoardId;
                }
                return facts;
            }
            return CompleteLevel(s, cat, level, a, facts);
        }

        static CompletionFacts CompleteLevel(SaveData s, Catalogue cat, LevelDef level, AttemptData a, CompletionFacts facts)
        {
            var reqRun = a.Board(level.RequiredBoard.BoardId);
            facts.ActiveMs = reqRun.activeMs;

            // Idempotent coin grant.
            if (!s.grantIds.Contains(level.CoinRewardId))
            {
                s.grantIds.Add(level.CoinRewardId);
                s.wallet += level.FirstCompletionCoins;
                facts.CoinsEarned = level.FirstCompletionCoins;
            }
            if (!s.seals.Contains(level.LevelId)) { s.seals.Add(level.LevelId); facts.NewSeal = true; }

            foreach (var run in a.boards.Where(b => b.status == BoardStatus.Solved))
            {
                var def = level.Board(run.boardId);
                ApplyBoardRecord(s, level, a, run, def, facts);
                CommitDiscoveries(s, run, facts);
                var marker = BoardMarker(level.LevelId, run.boardId);
                if (!s.boardCompleteMarkers.Contains(marker)) s.boardCompleteMarkers.Add(marker);
            }
            // Required board record was already applied above when solved; avoid double "new best" from optional.

            var trace = TraceId(level.LevelId);
            if (!s.traceIds.Contains(trace)) { s.traceIds.Add(trace); facts.TraceAdded = true; }

            if (!string.IsNullOrEmpty(level.ProjectAnchorId) && !s.anchorIds.Contains(level.ProjectAnchorId))
            {
                s.anchorIds.Add(level.ProjectAnchorId);
                facts.AnchorUnlocked = level.ProjectAnchorId;
                if (s.equipped.All(e => e.anchorId != level.ProjectAnchorId))
                    s.equipped.Add(new EquipData { anchorId = level.ProjectAnchorId, variantId = "" }); // free default
                if (!string.IsNullOrEmpty(level.JourneyMilestoneId) && !s.journeyMilestoneIds.Contains(level.JourneyMilestoneId))
                    s.journeyMilestoneIds.Add(level.JourneyMilestoneId);
            }

            s.deferredLevelIds.Remove(level.LevelId);
            AdvanceCursor(s, cat, level.AreaId);

            if (level.ProjectAnchorId == "finished_020" && !s.recognitions.Contains(KitchenReady)
                && KitchenCatalogue.OrdinaryScenes.All(n => s.HasSeal(KitchenCatalogue.LevelId(n))))
            {
                s.recognitions.Add(KitchenReady);
                facts.NewRecognitions.Add(KitchenReady);
                if (!s.areaCompletionIds.Contains(KitchenCatalogue.AreaId)) s.areaCompletionIds.Add(KitchenCatalogue.AreaId);
            }
            CheckMaster(s, facts);

            // Discard the attempt unless an optional board remains saved for later.
            var opt = level.OptionalBoard;
            bool keep = opt != null && a.Board(opt.BoardId).status != BoardStatus.Solved;
            if (keep)
            {
                a.currentBoardId = opt.BoardId;
                a.entryKind = "first";
                if (s.selectedAttemptId == a.attemptId) s.selectedAttemptId = ""; // finale done; puzzle stays in Later Puzzles
            }
            else
            {
                s.attempts.Remove(a);
                if (s.selectedAttemptId == a.attemptId) s.selectedAttemptId = "";
            }
            return facts;
        }

        static void CheckMaster(SaveData s, CompletionFacts facts)
        {
            if (!s.recognitions.Contains(KitchenMaster) &&
                Enumerable.Range(1, 20).All(n => s.HasSeal(KitchenCatalogue.LevelId(n))))
            {
                s.recognitions.Add(KitchenMaster);
                facts.NewRecognitions.Add(KitchenMaster);
            }
        }

        /// <summary>Area cursor = earliest installed scene neither completed nor deferred. Never rewinds.</summary>
        public static void AdvanceCursor(SaveData s, Catalogue cat, string areaId)
        {
            var installed = cat.InstalledInArea(areaId);
            int old = s.Cursor(areaId);
            int c = installed.Count + 1;
            foreach (var l in installed)
                if (!s.HasSeal(l.LevelId) && !s.IsDeferred(l.LevelId)) { c = l.OrdinalInArea; break; }
            c = Math.Max(c, old);
            var entry = s.areaCursors.FirstOrDefault(x => x.areaId == areaId);
            if (entry == null) s.areaCursors.Add(new AreaCursorData { areaId = areaId, cursor = c });
            else entry.cursor = c;
        }

        /// <summary>Leave for later: legal only on planning-only scenes before completion.</summary>
        public static bool DeferLevel(SaveData s, Catalogue cat, LevelDef level, AttemptData a)
        {
            if (!level.CanDeferLevel || s.HasSeal(level.LevelId)) return false;
            if (a != null && a.Current.status != BoardStatus.Solved) a.Current.status = BoardStatus.Deferred;
            if (!s.deferredLevelIds.Contains(level.LevelId)) s.deferredLevelIds.Add(level.LevelId);
            AdvanceCursor(s, cat, level.AreaId);
            return true;
        }

        /// <summary>Finish Kitchen / Leave puzzle for later on Level 20's optional board.</summary>
        public static bool DeferOptionalBoard(SaveData s, LevelDef level, AttemptData a)
        {
            var opt = level.OptionalBoard;
            if (opt == null || a.currentBoardId != opt.BoardId) return false;
            a.Current.status = BoardStatus.Deferred;
            a.currentBoardId = level.RequiredBoard.BoardId;
            return true;
        }

        // ---------- Cosmetics ----------

        public static AnchorDef AnchorForVariant(string variantId) => KitchenCatalogue.Anchors.FirstOrDefault(x => x.VariantId == variantId);

        public static string TransactionId(string variantId) => "cosmetic_purchase:" + variantId;

        public static bool CanPurchase(SaveData s, string variantId)
        {
            var anchor = AnchorForVariant(variantId);
            return anchor != null && s.anchorIds.Contains(anchor.AnchorId) && !s.ownedCosmetics.Contains(variantId)
                && s.wallet >= anchor.VariantPrice;
        }

        /// <summary>One transaction: subtract price, add ownership, equip. Duplicate requests are no-ops.</summary>
        public static bool Purchase(SaveData s, string variantId)
        {
            if (!CanPurchase(s, variantId)) return false;
            var anchor = AnchorForVariant(variantId);
            var tx = TransactionId(variantId);
            if (s.grantIds.Contains(tx)) return false;
            s.grantIds.Add(tx);
            s.wallet -= anchor.VariantPrice;
            s.ownedCosmetics.Add(variantId);
            Equip(s, anchor.AnchorId, variantId);
            return true;
        }

        public static bool Equip(SaveData s, string anchorId, string variantId)
        {
            if (!s.anchorIds.Contains(anchorId)) return false;
            if (!string.IsNullOrEmpty(variantId))
            {
                var anchor = AnchorForVariant(variantId);
                if (anchor == null || anchor.AnchorId != anchorId || !s.ownedCosmetics.Contains(variantId)) return false;
            }
            var e = s.equipped.FirstOrDefault(x => x.anchorId == anchorId);
            if (e == null) s.equipped.Add(new EquipData { anchorId = anchorId, variantId = variantId ?? "" });
            else e.variantId = variantId ?? "";
            return true;
        }

        // ---------- Deep copy for candidate transactions ----------

        public static SaveData Clone(SaveData s)
        {
            var c = s.ShallowCopy();
            c.grantIds = s.grantIds.ToList(); c.seals = s.seals.ToList(); c.deferredLevelIds = s.deferredLevelIds.ToList();
            c.traceIds = s.traceIds.ToList(); c.anchorIds = s.anchorIds.ToList(); c.ownedCosmetics = s.ownedCosmetics.ToList();
            c.equipped = s.equipped.Select(e => new EquipData { anchorId = e.anchorId, variantId = e.variantId }).ToList();
            c.legacyInventory = s.legacyInventory.ToList(); c.discoveries = s.discoveries.ToList(); c.badges = s.badges.ToList();
            c.recognitions = s.recognitions.ToList(); c.boardCompleteMarkers = s.boardCompleteMarkers.ToList();
            c.areaCursors = s.areaCursors.Select(x => new AreaCursorData { areaId = x.areaId, cursor = x.cursor }).ToList();
            c.records = s.records.Select(r => new BoardRecordData
            {
                levelId = r.levelId, boardId = r.boardId, boardContentVersion = r.boardContentVersion,
                configSignature = r.configSignature, bestMs = r.bestMs, bestUnassistedMs = r.bestUnassistedMs
            }).ToList();
            c.settings = new SettingsData
            {
                preference = s.settings.preference, music = s.settings.music, effects = s.settings.effects,
                haptics = s.settings.haptics, reducedMotion = s.settings.reducedMotion
            };
            c.tutorialOfferedIds = s.tutorialOfferedIds.ToList(); c.tutorialExposedIds = s.tutorialExposedIds.ToList();
            c.attempts = s.attempts.Select(CloneAttempt).ToList();
            c.unlockedDestinationIds = s.unlockedDestinationIds.ToList(); c.unlockedAreaIds = s.unlockedAreaIds.ToList();
            c.journeyMilestoneIds = s.journeyMilestoneIds.ToList(); c.areaCompletionIds = s.areaCompletionIds.ToList();
            c.ownedKeepsakeIds = s.ownedKeepsakeIds.ToList();
            return c;
        }

        static AttemptData CloneAttempt(AttemptData a) => new AttemptData
        {
            attemptId = a.attemptId, levelId = a.levelId, contentVersion = a.contentVersion,
            boardContentVersion = a.boardContentVersion, entryKind = a.entryKind, currentBoardId = a.currentBoardId,
            configSignature = a.configSignature,
            boards = a.boards.Select(b => new BoardRunData
            {
                boardId = b.boardId, status = b.status, boardRunId = b.boardRunId,
                assignments = b.assignments.Select(x => new AssignmentData { instanceId = x.instanceId, targetId = x.targetId, slot = x.slot }).ToList(),
                pendingDiscoveries = b.pendingDiscoveries.ToList(), activeMs = b.activeMs,
                categoryRejections = b.categoryRejections, fullRejections = b.fullRejections,
                legalRearrangements = b.legalRearrangements, assisted = b.assisted,
                assistanceKinds = b.assistanceKinds.ToList(), firstCredits = b.firstCredits.ToList(), streak = b.streak,
            }).ToList(),
        };
    }

}
