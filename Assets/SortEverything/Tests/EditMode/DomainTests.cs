using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SortEverything.Domain;
using SortEverything.Runtime;

namespace SortEverything.Tests
{
    /// <summary>Domain tests for the Section 27 acceptance scenarios, using the real catalogue.</summary>
    public class DomainTests
    {
        Catalogue _cat;
        SaveData _s;
        int _ids;

        string NewId() => "id" + (_ids++);

        [SetUp]
        public void SetUp()
        {
            _cat = Catalogue.CreateKitchen("kitchen_20");
            _s = SaveFactory.Create("test");
            _ids = 0;
        }

        LevelDef L(int n) => _cat.Level(KitchenCatalogue.LevelId(n));

        AttemptData Begin(int n)
        {
            var level = L(n);
            return _s.AttemptFor(level.LevelId) ?? Progression.CreateAttempt(_s, level, _s.HasSeal(level.LevelId) ? "replay" : "first", NewId, "sig");
        }

        BoardState State(LevelDef level, AttemptData a) => Progression.ReadBoard(level.Board(a.currentBoardId), a.Current);

        /// <summary>Proposes a drop exactly as the runtime does; returns the result and any completion facts.</summary>
        DropResult Drop(int n, string obj, string target, out CompletionFacts facts, bool overSource = false)
        {
            facts = null;
            var level = L(n);
            var a = _s.AttemptFor(level.LevelId);
            var def = level.Board(a.currentBoardId);
            var st = State(level, a);
            var p = new DropProposal(obj, target, overSource);
            var r = RuleEvaluator.Evaluate(st, p);
            if (r.Kind == DropKind.Accept)
            {
                var next = BoardReducer.Apply(st, p, r, out var f);
                Progression.RecordAccepted(a.Current, def, next, f);
                if (f.Solved) facts = Progression.CompleteBoard(_s, _cat, level, a);
            }
            else Progression.RecordRejection(a.Current, r.Kind);
            return r;
        }

        DropResult Drop(int n, string obj, string target) => Drop(n, obj, target, out _);

        CompletionFacts SolveCurrent(int n)
        {
            var level = L(n);
            var a = Begin(n);
            var def = level.Board(a.currentBoardId);
            var sol = Solver.Solve(def);
            Assert.IsTrue(sol.Solved, level.LevelId + " must be solvable");
            CompletionFacts facts = null;
            if (def.IsPlanning)
                foreach (var placed in State(level, a).Assignments().ToList())
                    Assert.AreEqual(DropKind.Accept, Drop(n, placed.instanceId, null, out _, overSource: true).Kind);
            foreach (var kv in sol.Allocation.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (State(level, a).TargetOf(kv.Key) == kv.Value) continue;
                var r = Drop(n, kv.Key, kv.Value, out var f);
                Assert.AreEqual(DropKind.Accept, r.Kind, $"{level.LevelId} {kv.Key}->{kv.Value}");
                if (f != null) facts = f;
            }
            return facts;
        }

        void Defer(int n)
        {
            var a = Begin(n);
            Assert.IsTrue(Progression.DeferLevel(_s, _cat, L(n), a));
        }

        // ---------- DATA ----------

        [Test] public void DATA01_AllScenesExpandAndSolve()
        {
            var diags = CatalogueValidator.Validate(_cat);
            Assert.IsEmpty(diags, string.Join("\n", diags));
            Assert.AreEqual(20, _cat.Levels.Count);
            Assert.AreEqual(21, _cat.Levels.Sum(l => l.Boards.Count));
            CollectionAssert.AreEquivalent(new[] { 6, 8, 11, 12, 13, 14, 16, 19 }, _cat.Levels.Where(l => l.CanDeferLevel).Select(l => l.OrdinalInArea));
            Assert.IsFalse(L(20).Board("a").Required);
            Assert.IsTrue(L(20).Board("b").Required);
            Assert.AreEqual(18, _cat.Levels.SelectMany(l => l.Boards).SelectMany(b => b.Objects).Select(o => o.AssetId).Distinct().Count());
            Assert.AreEqual(9, L(20).Boards.Sum(b => b.TotalUnits));
        }

        [Test] public void DATA02_P1SourceOrderAndGrids()
        {
            var b = L(6).Board("a");
            CollectionAssert.AreEqual(new[] { "a_f1", "a_v1", "a_f2", "a_v2", "a_f3", "a_v3", "a_f4", "a_v4" }, b.Objects.Select(o => o.InstanceId));
            var f2 = b.Object("a_f2");
            Assert.AreEqual(0.62f, f2.SourceX, 1e-4); Assert.AreEqual(0.28f, f2.SourceY, 1e-4);
            Assert.AreEqual("grid8", b.SourceLayoutId);
            Assert.AreEqual(2, b.Target("fruit_only").Capacity);
            Assert.AreEqual(6, b.Target("shared").Capacity);
            // P2/P3 large first item, P4 large drink
            Assert.AreEqual(2, L(11).Board("a").Object("a_f1").Units);
            Assert.AreEqual(1, L(11).Board("a").Object("a_f2").Units);
            Assert.AreEqual(2, L(14).Board("a").Object("a_d1").Units);
            Assert.AreEqual(9, L(13).Board("a").Target("shared").SlotCount);
            Assert.AreEqual(10, L(13).Board("a").Target("shared").Capacity);
            foreach (var l in _cat.Levels) foreach (var bd in l.Boards)
                Assert.AreEqual(bd.Objects.Count, bd.Objects.Select(o => (o.SourceX, o.SourceY)).Distinct().Count(), l.LevelId);
        }

        // ---------- INPUT / ALLOC ----------

        [Test] public void INPUT01_WrongCategoryLeavesSourceUnchanged()
        {
            var a = Begin(1);
            var r = Drop(1, "a_f1", "drinks_only");
            Assert.AreEqual(DropKind.WrongCategory, r.Kind);
            Assert.AreEqual(0, State(L(1), a).SortedCount);
            Assert.AreEqual(0, _s.wallet);
            Assert.AreEqual(1, a.Current.categoryRejections);
        }

        [Test] public void Unlimited_AssignedObjectsAreLocked()
        {
            var a = Begin(1);
            Drop(1, "a_f1", "fruit_only");
            var st = State(L(1), a);
            Assert.IsFalse(RuleEvaluator.CanPickUp(st, "a_f1"));
            Assert.AreEqual(DropKind.Locked, RuleEvaluator.Evaluate(st, new DropProposal("a_f1", "drinks_only", false)).Kind);
        }

        [Test] public void ALLOC01_P1TrapAndRecovery()
        {
            Begin(6);
            foreach (var f in new[] { "a_f1", "a_f2", "a_f3", "a_f4" }) Assert.AreEqual(DropKind.Accept, Drop(6, f, "shared").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(6, "a_v1", "shared").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(6, "a_v2", "shared").Kind);
            Assert.AreEqual(DropKind.Full, Drop(6, "a_v3", "shared").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(6, "a_f1", "fruit_only").Kind); // transfer
            Assert.AreEqual(DropKind.Accept, Drop(6, "a_f2", "fruit_only").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(6, "a_v3", "shared").Kind);
            Drop(6, "a_v4", "shared", out var facts);
            Assert.IsNotNull(facts);
            Assert.IsTrue(_s.HasSeal("kitchen_006"));
        }

        [Test] public void ALLOC02_P2LargeOrSmallBothComplete()
        {
            // Large in dedicated
            Begin(11);
            var b = L(11).Board("a");
            Assert.AreEqual(DropKind.Accept, Drop(11, "a_f1", "fruit_only").Kind);
            Assert.AreEqual(DropKind.Full, Drop(11, "a_f2", "fruit_only").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(11, "a_v2", "vegetables_only").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(11, "a_v3", "vegetables_only").Kind);
            Assert.AreEqual(DropKind.Full, Drop(11, "a_v1", "vegetables_only").Kind); // large never split
            foreach (var (o, t) in new[] { ("a_d1", "drinks_only"), ("a_f2", "shared"), ("a_f3", "shared"), ("a_v1", "shared"), ("a_d2", "shared") })
                Assert.AreEqual(DropKind.Accept, Drop(11, o, t).Kind, o);
            Drop(11, "a_d3", "shared", out var facts);
            Assert.IsNotNull(facts);
        }

        [Test] public void ALLOC03_P3KitchenwareOnlyShared()
        {
            Begin(13);
            foreach (var t in new[] { "fruit_only", "vegetables_only", "drinks_only" })
                Assert.AreEqual(DropKind.WrongCategory, Drop(13, "a_k2", t).Kind);
            var sol = Solver.Solve(L(13).Board("a"));
            Assert.IsTrue(sol.Allocation.Where(kv => kv.Key.Contains("_k")).All(kv => kv.Value == "shared"));
        }

        [Test] public void ALLOC04_P4TrapRecoverableBothSolutions()
        {
            Begin(14);
            // Spread small objects across vd and fd before the large drink.
            Assert.AreEqual(DropKind.Accept, Drop(14, "a_v1", "vd").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(14, "a_f1", "fd").Kind);
            Assert.AreEqual(DropKind.Full, Drop(14, "a_d1", "vd").Kind);
            Assert.AreEqual(DropKind.Full, Drop(14, "a_d1", "fd").Kind);
            // Recover with a transfer and a return.
            Assert.AreEqual(DropKind.Accept, Drop(14, "a_f1", "fv").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(14, "a_v1", null, out _, overSource: true).Kind);
            Assert.AreEqual(DropKind.Accept, Drop(14, "a_d1", "fd").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(14, "a_f2", "fv").Kind);
            Assert.AreEqual(DropKind.Accept, Drop(14, "a_v1", "vd").Kind);
            Drop(14, "a_v2", "vd", out var f);
            Assert.IsNotNull(f);
            var run = _s.records.First(r => r.levelId == "kitchen_014");
            Assert.IsNotNull(run);

            // Second solution: Vegetables in fv, Fruit in fd, Drink in vd.
            var b = L(14).Board("a");
            var st = new BoardState(b);
            foreach (var (o, t) in new[] { ("a_v1", "fv"), ("a_v2", "fv"), ("a_f1", "fd"), ("a_f2", "fd"), ("a_d1", "vd") })
            {
                var p = new DropProposal(o, t, false);
                var r = RuleEvaluator.Evaluate(st, p);
                Assert.AreEqual(DropKind.Accept, r.Kind, o);
                st = BoardReducer.Apply(st, p, r, out _);
            }
            Assert.IsTrue(st.IsSolved);
        }

        [Test] public void ALLOC05_SameFullTargetIsNoOp()
        {
            var a = Begin(6);
            Drop(6, "a_f1", "fruit_only");
            Drop(6, "a_f2", "fruit_only");
            var st = State(L(6), a);
            var r = RuleEvaluator.Evaluate(st, new DropProposal("a_f1", "fruit_only", false));
            Assert.AreEqual(DropKind.SameTargetNoOp, r.Kind);
            Assert.AreEqual(2, st.UnitsIn("fruit_only"));
            Assert.AreEqual(0, a.Current.fullRejections);
        }

        [Test] public void Rearrangement_PreservesAccountingAndSortedCount()
        {
            var a = Begin(6);
            Drop(6, "a_f1", "shared");
            Assert.AreEqual(1, State(L(6), a).SortedCount);
            Drop(6, "a_f1", "fruit_only");
            var st = State(L(6), a);
            Assert.AreEqual(1, st.SortedCount);
            Assert.AreEqual(1, st.UnitsIn("fruit_only"));
            Assert.AreEqual(0, st.UnitsIn("shared"));
            Drop(6, "a_f1", null, out _, overSource: true);
            Assert.AreEqual(0, State(L(6), a).SortedCount);
            Assert.AreEqual(1, a.Current.firstCredits.Count, "no second first-assignment credit");
        }

        // ---------- DEFER / FINAL ----------

        [Test] public void DEFER01_DeferLevel6ThenComplete7()
        {
            for (int n = 1; n <= 5; n++) SolveCurrent(n);
            Begin(6);
            Drop(6, "a_f1", "shared");
            Defer(6);
            Assert.AreEqual(7, _s.Cursor(KitchenCatalogue.AreaId));
            SolveCurrent(7);
            Assert.IsTrue(Progression.IsAvailable(_s, _cat, L(8)));
            Assert.IsFalse(_s.HasSeal("kitchen_006"));
            Assert.IsFalse(_s.grantIds.Contains("campaign_complete:kitchen_006"));
            Assert.IsFalse(_s.traceIds.Contains("trace:kitchen_006"));
            Assert.AreEqual(1, _s.AttemptFor("kitchen_006").Current.assignments.Count);
            Assert.AreEqual(LevelStatus.Later, Progression.Status(_s, _cat, L(6)));
        }

        [Test] public void DEFER02_ResumeDeferredWhileLaterAttemptExists()
        {
            for (int n = 1; n <= 5; n++) SolveCurrent(n);
            Defer(6); SolveCurrent(7); Defer(8);
            Begin(9); Drop(9, "a_f1", "fruit_only");
            Assert.AreEqual(9, _s.Cursor(KitchenCatalogue.AreaId));
            SolveCurrent(6);
            Assert.AreEqual(9, _s.Cursor(KitchenCatalogue.AreaId), "cursor never rewinds");
            Assert.IsNotNull(_s.AttemptFor("kitchen_009"));
            Assert.IsTrue(_s.HasSeal("kitchen_006"));
            Assert.IsFalse(_s.IsDeferred("kitchen_006"));
        }

        void PlayOrdinaryRouteDeferringPlanning()
        {
            for (int n = 1; n <= 19; n++)
            {
                if (L(n).CanDeferLevel) Defer(n);
                else SolveCurrent(n);
            }
            var a = Begin(20);
            Assert.AreEqual("a", a.currentBoardId);
            Assert.IsTrue(Progression.DeferOptionalBoard(_s, L(20), a));
            SolveCurrent(20);
        }

        [Test] public void FINAL01_DeferAllPlanningCompleteOrdinary()
        {
            PlayOrdinaryRouteDeferringPlanning();
            Assert.IsTrue(_s.recognitions.Contains(Progression.KitchenReady));
            Assert.IsFalse(_s.recognitions.Contains(Progression.KitchenMaster));
            Assert.AreEqual(12, _s.seals.Count);
            Assert.AreEqual(600, _s.wallet);
            Assert.AreEqual(4, _s.anchorIds.Count);
            Assert.IsTrue(Progression.HasLaterOptionalBoard(_s, L(20)));
            Assert.AreEqual(9, Progression.LaterPuzzles(_s, _cat, KitchenCatalogue.AreaId).Count());
        }

        [Test] public void FINAL02_SolveOptionalThenResumeRequired()
        {
            for (int n = 1; n <= 19; n++) { if (L(n).CanDeferLevel) Defer(n); else SolveCurrent(n); }
            int wallet = _s.wallet;
            var facts = SolveCurrent(20);
            Assert.IsTrue(facts.OptionalBoardOnly);
            Assert.AreEqual(wallet, _s.wallet, "no Level 20 coins before required completion");
            var json = FileSaveStore.Serialize(_s);
            var resumed = FileSaveStore.TryParse(json, out _);
            var a = resumed.AttemptFor("kitchen_020");
            Assert.AreEqual("b", a.currentBoardId);
            Assert.AreEqual(BoardStatus.Solved, a.Board("a").status);
            Assert.IsNull(SaveValidator.Validate(resumed, _cat));
        }

        [Test] public void FINAL03_RequiredWithOptionalDeferredThenLaterOptional()
        {
            PlayOrdinaryRouteDeferringPlanning();
            int wallet = _s.wallet;
            Assert.AreEqual(1, _s.grantIds.Count(g => g == "campaign_complete:kitchen_020"));
            var a = _s.AttemptFor("kitchen_020");
            Assert.AreEqual("a", a.currentBoardId);
            a.Current.status = BoardStatus.Playing;
            var facts = SolveCurrent(20);
            Assert.IsTrue(facts.OptionalBoardOnly);
            Assert.AreEqual(wallet, _s.wallet);
            Assert.AreEqual(0, facts.CoinsEarned);
            Assert.IsTrue(_s.boardCompleteMarkers.Contains("board_complete:kitchen_020:a"));
            Assert.IsNull(_s.AttemptFor("kitchen_020"));
            Assert.IsFalse(Progression.HasLaterOptionalBoard(_s, L(20)));
        }

        [Test] public void FINAL04_ReplayFinishKeptWhenOptionalCompletes()
        {
            PlayOrdinaryRouteDeferringPlanning();
            var a = _s.AttemptFor("kitchen_020");
            // Start Replay Finish of b, place one object, then switch and complete a.
            Progression.RestartBoard(a.Board("b"), NewId);
            a.currentBoardId = "b"; a.entryKind = "replay_finish";
            Drop(20, "b_f1", "fruit_only");
            a.currentBoardId = "a"; a.Board("a").status = BoardStatus.Playing;
            int wallet = _s.wallet;
            SolveCurrent(20);
            var still = _s.AttemptFor("kitchen_020");
            Assert.IsNotNull(still, "unfinished b remains resumable");
            Assert.AreEqual("b", still.currentBoardId);
            Assert.AreEqual(1, still.Board("b").assignments.Count);
            Assert.AreEqual(wallet, _s.wallet);
        }

        [Test] public void FullRoute_AllTwentyGivesMasterAnd1000()
        {
            for (int n = 1; n <= 20; n++)
            {
                SolveCurrent(n);
                if (n == 20) SolveCurrent(20); // optional a first, then b
            }
            Assert.AreEqual(1000, _s.wallet);
            Assert.IsTrue(_s.recognitions.Contains(Progression.KitchenMaster));
            Assert.IsTrue(_s.recognitions.Contains(Progression.KitchenReady));
            Assert.IsTrue(_s.recognitions.Contains(Progression.KitchenCollectionComplete));
            Assert.AreEqual(21, _s.cursorAfterAll());
        }

        // ---------- REWARD / BADGE / ECON / TRACE ----------

        [Test] public void REWARD01_ReplayEarnsZeroAndGrantsAreIdempotent()
        {
            SolveCurrent(1);
            Assert.AreEqual(50, _s.wallet);
            Assert.AreEqual(1, _s.traceIds.Count);
            Progression.CreateAttempt(_s, L(1), "replay", NewId, "sig");
            var facts = SolveCurrent(1);
            Assert.AreEqual(0, facts.CoinsEarned);
            Assert.AreEqual(50, _s.wallet);
            Assert.AreEqual(1, _s.seals.Count(x => x == "kitchen_001"));
            Assert.AreEqual(1, _s.traceIds.Count);
            Assert.AreEqual(1, _s.grantIds.Count(g => g == "campaign_complete:kitchen_001"));
        }

        [Test] public void BADGE01_HintMeansFullRecognitionButNoBadge()
        {
            for (int n = 1; n <= 5; n++) SolveCurrent(n);
            var a = Begin(6);
            Progression.MarkAssistance(a.Current, "hint");
            Progression.RestartBoard(a.Current, NewId);
            Assert.IsTrue(a.Current.assisted, "restart cannot erase assistance");
            int before = _s.wallet;
            SolveCurrent(6);
            Assert.AreEqual(before + 50, _s.wallet);
            Assert.IsTrue(_s.HasSeal("kitchen_006"));
            Assert.IsFalse(_s.badges.Contains("unassisted_planner:kitchen_006:a"));
        }

        [Test] public void BADGE02_LegalRearrangementKeepsPlannerBadge()
        {
            Begin(6);
            Drop(6, "a_f1", "shared"); Drop(6, "a_f1", "fruit_only"); Drop(6, "a_f2", "shared"); Drop(6, "a_f2", null, out _, true);
            SolveCurrent(6);
            Assert.IsTrue(_s.badges.Contains("unassisted_planner:kitchen_006:a"));
        }

        [Test] public void ECON01_OrdinaryRouteBuysEveryVariantAt150()
        {
            for (int n = 1; n <= 20; n++)
            {
                if (L(n).CanDeferLevel) { Defer(n); continue; }
                if (n == 20) Progression.DeferOptionalBoard(_s, L(20), Begin(20));
                SolveCurrent(n);
                var anchor = KitchenCatalogue.Anchors.FirstOrDefault(x => x.TriggerLevelId == L(n).LevelId);
                if (anchor != null)
                {
                    Assert.AreEqual("", _s.Equipped(anchor.AnchorId), "free default equips on unlock");
                    Assert.IsTrue(Progression.Purchase(_s, anchor.VariantId), anchor.VariantId);
                }
            }
            Assert.AreEqual(150, _s.wallet);
        }

        [Test] public void ECON02_DuplicatePurchaseDebitsOnce()
        {
            for (int n = 1; n <= 5; n++) SolveCurrent(n);
            Assert.AreEqual(250, _s.wallet);
            Assert.IsTrue(Progression.Purchase(_s, "woven_basket"));
            Assert.IsFalse(Progression.Purchase(_s, "woven_basket"));
            Assert.AreEqual(175, _s.wallet);
            Assert.AreEqual("woven_basket", _s.Equipped("breakfast_005"));
            Assert.IsTrue(Progression.Equip(_s, "breakfast_005", ""));
            Assert.IsFalse(Progression.Equip(_s, "pantry_010", "shelf_trim"), "locked anchor");
            Assert.IsFalse(Progression.Purchase(_s, "shelf_trim"), "anchor not unlocked");
        }

        [Test] public void TRACE01_TraceOnceAnchorPermanent()
        {
            SolveCurrent(1);
            Progression.CreateAttempt(_s, L(1), "replay", NewId, "sig");
            SolveCurrent(1);
            Assert.AreEqual(1, _s.traceIds.Count(t => t == "trace:kitchen_001"));
            for (int n = 2; n <= 5; n++) SolveCurrent(n);
            Assert.IsTrue(_s.anchorIds.Contains("breakfast_005"));
            Assert.AreEqual(5, _s.traceIds.Count, "anchor keeps trace IDs");
        }

        // ---------- HINT ----------

        [Test] public void HINT01_HintsSolveFromTrapsWithinTwiceObjectCount()
        {
            foreach (var n in new[] { 6, 11, 13, 14, 16, 19 })
            {
                var level = L(n);
                var def = level.FirstBoard;
                var st = new BoardState(def);
                // Build a trap: greedily put everything compatible into the shared/first target.
                foreach (var o in def.Objects)
                    foreach (var t in def.Targets.OrderByDescending(t => t.Accepts.Count))
                    {
                        var p = new DropProposal(o.InstanceId, t.TargetId, false);
                        var r = RuleEvaluator.Evaluate(st, p);
                        if (r.Kind == DropKind.Accept) { st = BoardReducer.Apply(st, p, r, out _); break; }
                    }
                var desired = HintService.DesiredAllocation(level.LevelId, level.BoardContentVersion, def);
                int steps = 0;
                while (!st.IsSolved)
                {
                    var h = HintService.Next(st, desired);
                    Assert.AreNotEqual(HintKind.None, h.Kind, level.LevelId);
                    var p = h.Kind == HintKind.Return ? new DropProposal(h.ObjectId, null, true) : new DropProposal(h.ObjectId, h.TargetId, false);
                    var r = RuleEvaluator.Evaluate(st, p);
                    Assert.AreEqual(DropKind.Accept, r.Kind, $"{level.LevelId} hint {h.Kind} {h.ObjectId}->{h.TargetId} must be executable");
                    st = BoardReducer.Apply(st, p, r, out _);
                    Assert.LessOrEqual(++steps, def.Objects.Count * 2, level.LevelId);
                }
            }
        }

        // ---------- SAVE ----------

        string TempDir()
        {
            var d = Path.Combine(Path.GetTempPath(), "se_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            return d;
        }

        [Test] public void SAVE01_FailedWriteStagesKeepPreviousState()
        {
            var dir = TempDir();
            var store = new FileSaveStore(dir);
            var session = new GameSession(_cat, new GameConfig(), store, null);
            session.Boot(out _);
            long rev = session.Save.revision;
            foreach (var stage in new[] { "pending", "promote" })
            {
                store.FailAtStage = stage;
                Assert.IsFalse(session.Commit(s => { s.wallet += 50; return true; }));
                Assert.AreEqual(0, session.Save.wallet, "candidate never published");
                Assert.IsTrue(session.SaveBlocked);
                store.FailAtStage = null;
                Assert.IsTrue(session.RetryFailedCommit());
                Assert.AreEqual(50, session.Save.wallet);
                session.Commit(s => { s.wallet = 0; return true; });
            }
            var reload = new GameSession(_cat, new GameConfig(), new FileSaveStore(dir), null);
            reload.Boot(out _);
            Assert.AreEqual(session.Save.revision, reload.Save.revision);
            Assert.Greater(reload.Save.revision, rev);
        }

        [Test] public void SAVE02_CorruptPrimaryRecoversFromBackup_AllCorruptPreserved()
        {
            var dir = TempDir();
            var store = new FileSaveStore(dir);
            var session = new GameSession(_cat, new GameConfig(), store, null);
            session.Boot(out _);
            session.Commit(s => { s.wallet = 75; return true; });
            session.Commit(s => { s.wallet = 125; return true; });
            File.WriteAllText(store.MainPath, "{ not json");
            var recovered = new GameSession(_cat, new GameConfig(), new FileSaveStore(dir), null);
            Assert.AreEqual(SaveLoadOutcome.Recovered, recovered.Boot(out _));
            Assert.AreEqual(75, recovered.Save.wallet);

            File.WriteAllText(store.MainPath, "bad");
            File.WriteAllText(store.BackupPath, "bad");
            var corrupt = new GameSession(_cat, new GameConfig(), new FileSaveStore(dir), null);
            Assert.AreEqual(SaveLoadOutcome.AllCorrupt, corrupt.Boot(out _));
            Assert.AreEqual("bad", File.ReadAllText(store.MainPath), "bytes preserved");
        }

        [Test] public void SAVE03_FinalPlacementAndRewardAreOneTransaction()
        {
            var store = new MemorySaveStore();
            var session = new GameSession(_cat, new GameConfig(), store, null);
            session.Boot(out _);
            var level = L(1);
            string attemptId = null;
            session.Commit(s => { attemptId = Progression.CreateAttempt(s, level, "first", NewId, "sig").attemptId; return true; });
            var sol = Solver.Solve(level.FirstBoard).Allocation.OrderBy(k => k.Key).ToList();
            for (int i = 0; i < sol.Count; i++)
            {
                var kv = sol[i];
                bool last = i == sol.Count - 1;
                if (last) store.FailNext = true; // interrupt the final transaction
                session.Commit(s =>
                {
                    var a = s.Attempt(attemptId);
                    var st = Progression.ReadBoard(level.FirstBoard, a.Current);
                    var p = new DropProposal(kv.Key, kv.Value, false);
                    var r = RuleEvaluator.Evaluate(st, p);
                    var next = BoardReducer.Apply(st, p, r, out var f);
                    Progression.RecordAccepted(a.Current, level.FirstBoard, next, f);
                    if (f.Solved) Progression.CompleteBoard(s, _cat, level, a);
                    return true;
                });
            }
            var disk = FileSaveStore.TryParse(store.Stored, out _);
            Assert.IsFalse(disk.HasSeal("kitchen_001"));
            Assert.AreEqual(0, disk.wallet);
            Assert.AreEqual(sol.Count - 1, disk.Attempt(attemptId).Current.assignments.Count, "no solved-but-unawarded snapshot");
            Assert.IsTrue(session.RetryFailedCommit());
            disk = FileSaveStore.TryParse(store.Stored, out _);
            Assert.IsTrue(disk.HasSeal("kitchen_001"));
            Assert.AreEqual(50, disk.wallet);
        }

        [Test] public void SaveFactory_Defaults()
        {
            Assert.AreEqual(4, _s.schemaVersion);
            Assert.AreEqual(0, _s.wallet);
            Assert.AreEqual(1, _s.Cursor(KitchenCatalogue.AreaId));
            Assert.AreEqual("calm", _s.settings.preference);
            Assert.IsTrue(_s.settings.music && _s.settings.effects && _s.settings.haptics);
            CollectionAssert.AreEqual(new[] { "house" }, _s.unlockedDestinationIds);
            CollectionAssert.AreEqual(new[] { "house_kitchen" }, _s.unlockedAreaIds);
            Assert.AreEqual("", _s.selectedAttemptId);
        }

        [Test] public void GateProfile_EndsAtKitchen8()
        {
            var gate = Catalogue.CreateKitchen("kitchen_gate_08");
            Assert.AreEqual("", gate.PreviewFor(gate.Level("kitchen_008")));
            Assert.IsFalse(gate.IsInstalled("kitchen_009"));
            Assert.AreEqual(8, gate.InstalledInArea(KitchenCatalogue.AreaId).Count);
        }
    }

    static class TestExtensions
    {
        public static int cursorAfterAll(this SaveData s) => s.Cursor(KitchenCatalogue.AreaId);
    }
}
