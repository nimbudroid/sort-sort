using System.IO;
using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class ProgressionTests
    {
        // Mini campaign: r1 = [l1 (FRUIT/VEG unlimited), l2 (template T)], r2 = [l3], r3 locked (3 planned areas).

        static LevelAttempt Solve(LevelDef level)
        {
            var a = new LevelAttempt(level);
            a.BeginPickup();
            a.Tick(4f);
            for (int b = 0; b < level.boards.Length; b++)
            {
                var plan = BoardSolver.Solve(level.boards[b]);
                for (int i = 0; i < plan.Length; i++) Assert.AreEqual(PlaceResult.Accepted, a.Drop(i, plan[i]));
                if (b < level.boards.Length - 1) Assert.IsTrue(a.AdvanceBoard());
            }
            Assert.IsTrue(a.TryComplete());
            return a;
        }

        [Test]
        public void Unlocks_FollowCampaignOrder()
        {
            var c = CatalogTests.Mini();
            var p = new Progression(c, new SaveData());
            Assert.IsTrue(p.IsUnlocked(c.Levels[0]));
            Assert.IsFalse(p.IsUnlocked(c.Levels[1]));
            Assert.IsFalse(p.IsRoomUnlocked(c.Room("r2")));
            Assert.IsFalse(p.IsRoomUnlocked(c.Room("r3")));
            p.Commit(Solve(c.Levels[0]), false);
            Assert.IsTrue(p.IsUnlocked(c.Levels[1]));
            Assert.AreSame(c.Levels[1], p.NextUnfinished());
        }

        [Test]
        public void Rewards_AreIdempotent()
        {
            var c = CatalogTests.Mini();
            var p = new Progression(c, new SaveData());
            var a = Solve(c.Levels[0]);
            var r1 = p.Commit(a, false);
            Assert.IsTrue(r1.firstCompletion);
            Assert.AreEqual(75, r1.coinsEarned);
            var r2 = p.Commit(a, false);                       // double tap / re-opened result
            var r3 = p.Commit(Solve(c.Levels[0]), false);      // replay
            Assert.IsFalse(r2.firstCompletion);
            Assert.AreEqual(0, r2.coinsEarned);
            Assert.AreEqual(0, r3.coinsEarned);
            Assert.AreEqual(75, p.Save.coins);
            Assert.IsTrue(p.Save.rewards.Contains("campaign_complete:l1"));
        }

        [Test]
        public void AreaRoomAndHouseMilestones_GrantOnce()
        {
            var c = CatalogTests.Mini();
            var p = new Progression(c, new SaveData());
            Assert.IsNull(p.Commit(Solve(c.Levels[0]), false).areaRestored);
            var r = p.Commit(Solve(c.Levels[1]), false);
            Assert.AreEqual("a1", r.areaRestored.id);
            Assert.AreEqual("r1", r.roomCompleted.id);
            Assert.IsNull(p.Commit(Solve(c.Levels[1]), false).areaRestored);
            var last = p.Commit(Solve(c.Levels[2]), false);
            Assert.AreEqual("r2", last.roomCompleted.id);
            Assert.IsNull(last.next);
            Assert.IsFalse(last.houseCompleted);               // r3 is still a locked placeholder
            int restored, planned;
            p.HouseProgress(out restored, out planned);
            Assert.AreEqual(2, restored);
            Assert.AreEqual(5, planned);
        }

        [Test]
        public void Records_OnlyInMastery_NeverAffectRewards()
        {
            var c = CatalogTests.Mini();
            var p = new Progression(c, new SaveData());
            var calm = p.Commit(Solve(c.Levels[0]), false);
            Assert.IsFalse(calm.recordEligible);
            Assert.AreEqual(0, p.Save.bestTimes.Count);
            var m = p.Commit(Solve(c.Levels[0]), true);
            Assert.IsTrue(m.newBest);
            Assert.AreEqual(4f, p.Save.bestTimes["l1"], 1e-4);
            var assisted = Solve(c.Levels[0]);
            assisted.Assisted = true;
            Assert.IsFalse(p.Commit(assisted, true).recordEligible);
            Assert.AreEqual(75, p.Save.coins);
        }

        [Test]
        public void LabLevels_GrantNothing()
        {
            var c = CatalogTests.Mini();
            var lab = TestObjects.Level("lab_x", TestObjects.Board("apple carrot", TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES")));
            var p = new Progression(c, new SaveData());
            var r = p.Commit(Solve(lab), true);
            Assert.IsFalse(r.campaign);
            Assert.AreEqual(0, p.Save.coins);
            Assert.AreEqual(0, p.Save.completed.Count);
        }

        [Test]
        public void Resume_RestoresTheLastCommittedState()
        {
            var c = CatalogTests.Mini();
            var save = new SaveData();
            var p = new Progression(c, save);
            var a = new LevelAttempt(c.Levels[0]);
            a.Drop(0, 0);
            a.Tick(2f);
            p.Remember(a);
            var p2 = new Progression(c, SaveData.FromJson(save.ToJson()));
            var b = p2.Resume();
            Assert.IsNotNull(b);
            Assert.AreEqual(0, b.Board.Location(0));
            Assert.AreEqual(a.Clock.Elapsed, b.Clock.Elapsed, 1e-4);
            // Completing clears it.
            b.Drop(1, 0); b.Drop(2, 1);
            Assert.IsTrue(b.TryComplete());
            p2.Commit(b, false);
            Assert.IsNull(p2.Save.resume);
        }

        [Test]
        public void Resume_DroppedWhenContentChanged()
        {
            var c = CatalogTests.Mini();
            var p = new Progression(c, new SaveData());
            var a = new LevelAttempt(c.Levels[0]);
            a.Drop(0, 0);
            p.Remember(a);
            p.Save.resume.contentVersion = 99;
            Assert.IsNull(p.Resume());
            Assert.IsNull(p.Save.resume);
        }

        [Test]
        public void Variant_BoughtOnceWithCoins_AfterRestoration()
        {
            var c = CatalogTests.Mini();
            var area = c.Area("a1");
            area.variantPrice = 100;
            var p = new Progression(c, new SaveData());
            p.Save.coins = 500;
            Assert.IsFalse(p.TryBuyVariant(area));             // not restored yet
            p.Commit(Solve(c.Levels[0]), false);
            p.Commit(Solve(c.Levels[1]), false);
            int coins = p.Save.coins;
            Assert.IsTrue(p.TryBuyVariant(area));
            Assert.IsFalse(p.TryBuyVariant(area));
            Assert.AreEqual(coins - 100, p.Save.coins);
        }

        [Test]
        public void SaveData_RoundTripsEverything()
        {
            var s = new SaveData { coins = 125, mastery = true, showItemNames = true, lastLevel = "l2" };
            s.completed.Add("l1"); s.rewards.Add("campaign_complete:l1"); s.discoveries.Add("apple");
            s.bestTimes["l1"] = 12.5f; s.ownedVariants.Add("a1"); s.variantEquipped["a1"] = false;
            s.resume = new AttemptSnapshot { levelId = "l2", contentVersion = 1, attemptId = "x", board = 0,
                locations = new[] { -1, 1, 0 }, activeSeconds = 3.5f, started = true, credited = new[] { "0:apple_1" } };
            var t = SaveData.FromJson(s.ToJson());
            Assert.AreEqual(s.ToJson(), t.ToJson());
            Assert.AreEqual(125, t.coins);
            Assert.IsTrue(t.mastery && t.showItemNames && !t.reducedMotion);
            CollectionAssert.AreEqual(new[] { -1, 1, 0 }, t.resume.locations);
            Assert.IsFalse(t.variantEquipped["a1"]);
        }

        [Test]
        public void SaveData_MigratesUnversionedPrototypeSaves()
        {
            var t = SaveData.FromJson("{ \"mastery\": true, \"best\": { \"level_03\": 9.5 } }");
            Assert.AreEqual(SaveData.CurrentVersion, t.version);
            Assert.IsTrue(t.mastery);
            Assert.AreEqual(9.5f, t.legacyBestTimes["level_03"], 1e-4);
            Assert.AreEqual(0, t.bestTimes.Count);
        }

        [Test]
        public void SaveData_RefusesNewerVersions()
        {
            Assert.Throws<System.FormatException>(() => SaveData.FromJson("{ \"version\": 99 }"));
        }

        [Test]
        public void SaveFile_KeepsABackup_AndFallsBackToIt()
        {
            string dir = Path.Combine(Path.GetTempPath(), "se_save_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var f = new SaveFile(Path.Combine(dir, "save.json"));
                Assert.IsNull(f.Load());
                Assert.IsTrue(f.Save(new SaveData { coins = 50 }));
                Assert.IsTrue(f.Save(new SaveData { coins = 100 }));
                Assert.AreEqual(100, f.Load().coins);
                File.WriteAllText(f.Path, "{ broken");
                Assert.AreEqual(50, f.Load().coins);           // previous good file
                File.WriteAllText(f.Path, "{ \"version\": 99 }");
                File.Delete(f.Path + ".bak");
                Assert.IsNull(f.Load());
                Assert.IsTrue(f.ReadOnly);
                Assert.IsFalse(f.Save(new SaveData()));        // never overwrites a newer save
                Assert.AreEqual("{ \"version\": 99 }", File.ReadAllText(f.Path));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
