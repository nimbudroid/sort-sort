using System.Linq;
using NUnit.Framework;
using SortEverything.Domain;
using SortEverything.Editor;
using SortEverything.Presentation;
using SortEverything.Runtime;
using UnityEngine;

namespace SortEverything.Tests
{
    /// <summary>Board & art pass guarantees (layout) and the content validator, for every authored board.</summary>
    public class BoardPassTests
    {
        static readonly (float w, float h)[] Sizes = { (360, 800), (393, 852), (411, 914), (1024, 768) };

        static System.Collections.Generic.IEnumerable<BoardDef> AllBoards() =>
            Catalogue.CreateKitchen("kitchen_20", ContentDb.DataLevels).Levels.SelectMany(l => l.Boards);

        [Test]
        public void B2_ContentValidator_RunsCleanOnAllAuthoredContent()
        {
            var issues = ContentValidator.Validate();
            Assert.IsEmpty(issues, string.Join("\n", issues));
        }

        [Test]
        public void A2_EveryItemHitArea_AtLeast48_AndVisibleItemsReadable()
        {
            var cfg = BoardLayoutConfig.Current;
            foreach (var (w, h) in Sizes)
                foreach (var b in AllBoards())
                {
                    var L = BoardLayout.Compute(b, w, h);
                    foreach (var p in L.Pile.Values)
                        Assert.GreaterOrEqual(p.Hit, cfg.itemHitMin - 0.01f, $"{b.BoardId} at {w}x{h}: hit area below {cfg.itemHitMin}");
                    // On phones the smallest pile item should stay at the readable size (tablet landscape may go to the floor).
                    if (w < h)
                        Assert.GreaterOrEqual(L.Pile.Values.Min(p => p.Visible), cfg.itemMinVisible - 0.5f, $"{b.BoardId} at {w}x{h}: item drawn below {cfg.itemMinVisible}");
                }
        }

        [Test]
        public void A2_PileOverlap_AtMost20Percent_AndInsideTheCounter()
        {
            foreach (var (w, h) in Sizes)
                foreach (var b in AllBoards())
                {
                    var L = BoardLayout.Compute(b, w, h);
                    Assert.LessOrEqual(L.MaxPileCover(), BoardLayoutConfig.Current.maxOverlap + 0.02f, $"{b.BoardId} at {w}x{h}");
                    foreach (var p in L.Pile.Values)
                    {
                        Assert.IsTrue(L.PlayRect.Contains(p.Center), $"{b.BoardId} at {w}x{h}: item outside the counter");
                        Assert.LessOrEqual(Mathf.Abs(p.Rotation), BoardLayoutConfig.Current.rotationRange + 0.01f);
                    }
                }
        }

        [Test]
        public void A2_BinsSpan85To92PercentOfBoardWidth_InsideTheContainerZone()
        {
            foreach (var (w, h) in Sizes)
                foreach (var b in AllBoards())
                {
                    var L = BoardLayout.Compute(b, w, h);
                    foreach (var row in L.Bins.Values.GroupBy(x => Mathf.Round(x.Rect.y)))
                    {
                        float span = row.Max(x => x.Rect.xMax) - row.Min(x => x.Rect.xMin);
                        float share = span / L.Width;
                        Assert.That(share, Is.InRange(0.85f, 0.92f), $"{b.BoardId} at {w}x{h}: bins span {share:P0}");
                    }
                    foreach (var bin in L.Bins.Values)
                    {
                        Assert.GreaterOrEqual(bin.Rect.yMin, L.ContainersTop - 0.01f);
                        Assert.LessOrEqual(bin.Rect.yMax, L.ContainersTop + L.ContainersH + 0.01f);
                        Assert.GreaterOrEqual(bin.ItemSize, BoardLayoutConfig.Current.itemInBinMin - 0.01f);
                    }
                }
        }

        [Test]
        public void A2_SortedItemsSitInsideTheirContainer()
        {
            foreach (var b in AllBoards())
            {
                var L = BoardLayout.Compute(b, 360, 800);
                foreach (var t in b.Targets)
                    for (int s = 0; s < Mathf.Max(1, t.SlotCount); s++)
                    {
                        var p = L.SlotPosition(t.TargetId, s, t.SlotCount);
                        Assert.IsTrue(L.Bins[t.TargetId].Rect.Contains(p), $"{b.BoardId}/{t.TargetId} slot {s} outside its container");
                    }
            }
        }

        [Test]
        public void A2_ZoneSharesFillTheSafeArea()
        {
            var L = BoardLayout.Compute(Catalogue.CreateKitchen("kitchen_20").Levels[0].FirstBoard, 360, 800);
            Assert.AreEqual(800f, L.HeaderH + L.InstructionH + L.PlayH + L.ContainersH, 0.5f);
        }

        [Test]
        public void B1_DataLevel_BuildsIntoTheSameLevelModel_AndIsPlayable()
        {
            var file = new LevelsFile
            {
                levels = new[]
                {
                    new DataLevel
                    {
                        levelId = "kitchen_extra_test", purpose = "Set out the tea things — test", instruction = "Put each item with its group",
                        boards = new[]
                        {
                            new DataBoard
                            {
                                id = "a", required = true, template = TemplateIds.Unlimited,
                                objects = new[] { "mug", "plate", "apple", "banana", "orange" },
                                targets = new[]
                                {
                                    new DataTarget { id = "kitchenware_only", accepts = new[] { CategoryIds.Kitchenware }, capacity = -1, x = 0.25f, y = 0.5f, rectProfile = "wide", slotProfile = "two", slots = 2 },
                                    new DataTarget { id = "fruit_only", accepts = new[] { CategoryIds.Fruit }, capacity = -1, x = 0.75f, y = 0.5f, rectProfile = "wide", slotProfile = "four_square", slots = 3 },
                                },
                            },
                        },
                    },
                },
            };
            var built = DataLevelBuilder.Build(file, ContentDb.Objects, 21);
            Assert.AreEqual(1, built.Count);
            var cat = Catalogue.CreateKitchen("kitchen_20", built);
            var level = cat.Level("kitchen_extra_test");
            Assert.IsNotNull(level);
            Assert.IsTrue(cat.IsInstalled(level.LevelId));
            Assert.AreEqual(21, level.OrdinalInArea);
            Assert.AreEqual(0, level.FirstCompletionCoins, "data levels never touch the GDD economy");
            Assert.IsNotNull(Solver.Solve(level.RequiredBoard).Allocation);
            // The one rule evaluator decides correctness from metadata.
            var s = new BoardState(level.RequiredBoard);
            var mug = level.RequiredBoard.Objects.First(o => o.AssetId == "mug");
            Assert.AreEqual(DropKind.WrongCategory, RuleEvaluator.Evaluate(s, new DropProposal(mug.InstanceId, "fruit_only", false)).Kind);
            Assert.AreEqual(DropKind.Accept, RuleEvaluator.Evaluate(s, new DropProposal(mug.InstanceId, "kitchenware_only", false)).Kind);
            var L = BoardLayout.Compute(level.RequiredBoard, 360, 800);
            Assert.AreEqual(5, L.Pile.Count);
            Assert.AreEqual(2, L.Bins.Count);
            // The gate profile never installs data levels.
            Assert.IsFalse(Catalogue.CreateKitchen("kitchen_gate_08", built).IsInstalled("kitchen_extra_test"));
        }

        [Test]
        public void Slice_Level1SceneProfile_KeepsTheCounterComfortable()
        {
            var scene = ContentDb.Scene("kitchen_001");
            Assert.IsNotNull(scene, "Level 1 has a scene profile");
            Assert.IsTrue(scene.HasComposition, "Level 1 ends in a composed breakfast");
            var board = Catalogue.CreateKitchen("kitchen_20").Level("kitchen_001").RequiredBoard;
            foreach (var (w, h) in Sizes)
            {
                var L = BoardLayout.Compute(board, w, h, null, scene.backsplashShare);
                Assert.Greater(L.BacksplashH, 0);
                Assert.GreaterOrEqual(L.PlayRect.yMin, L.BacksplashH, "the pile stays on the counter, below the wall band");
                foreach (var p in L.Pile.Values)
                {
                    Assert.GreaterOrEqual(p.Hit, BoardLayoutConfig.Current.itemHitMin - 0.01f);
                    if (w < h) Assert.GreaterOrEqual(p.Visible, BoardLayoutConfig.Current.itemMinVisible - 0.5f);
                }
                Assert.LessOrEqual(L.MaxPileCover(), BoardLayoutConfig.Current.maxOverlap + 0.02f);
            }
        }

        [Test]
        public void A4_BoardFontCoversEveryLevelString()
        {
            var cover = ContentValidator.FontCoverage("Assets/SortEverything/Resources/SortEverything/Fonts/Lato-Regular.ttf", out var err);
            Assert.IsNull(err, err);
            foreach (var ch in "—·'é’‹…–") Assert.IsTrue(cover.Contains(ch), $"font lacks U+{(int)ch:X4}");
        }
    }
}
