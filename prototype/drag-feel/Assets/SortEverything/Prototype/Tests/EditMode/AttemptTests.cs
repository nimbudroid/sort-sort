using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class AttemptTests
    {
        static LevelDef Simple()
        {
            return TestObjects.Level("t_simple", TestObjects.Board("apple banana orange carrot broccoli spoon",
                TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES"), TestObjects.Target("KITCHENWARE")));
        }

        static LevelDef TwoBoards()
        {
            return TestObjects.Level("t_two",
                TestObjects.Board("apple carrot", TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES")),
                TestObjects.Board("spoon banana", TestObjects.Target("FRUIT"), TestObjects.Target("KITCHENWARE")));
        }

        [Test]
        public void StartsReady_FirstPickupStartsActiveTime()
        {
            var a = new LevelAttempt(Simple());
            Assert.AreEqual(AttemptState.Ready, a.State);
            a.Tick(5f);
            Assert.AreEqual(0f, a.Clock.Elapsed);
            a.BeginPickup();
            Assert.AreEqual(AttemptState.Playing, a.State);
            a.Tick(1.5f);
            Assert.AreEqual(1.5f, a.Clock.Elapsed, 1e-4f);
        }

        [Test]
        public void Mistakes_AreCounted_AndNeverFail()
        {
            var a = new LevelAttempt(Simple());
            for (int i = 0; i < 50; i++) Assert.AreEqual(PlaceResult.WrongTarget, a.Drop(0, 1));
            Assert.AreEqual(50, a.WrongCategory);
            Assert.AreEqual(AttemptState.Playing, a.State);
            Assert.AreEqual(PlaceResult.Accepted, a.Drop(0, 0));
        }

        [Test]
        public void Complete_ExactlyOnce_AndStopsTheClock()
        {
            var a = new LevelAttempt(Simple());
            a.Drop(0, 0); a.Drop(1, 0); a.Drop(2, 0); a.Drop(3, 1); a.Drop(4, 1);
            Assert.IsFalse(a.TryComplete());
            a.Drop(5, 2);
            Assert.IsTrue(a.TryComplete());
            Assert.IsFalse(a.TryComplete());
            Assert.AreEqual(AttemptState.Complete, a.State);
            float t = a.Clock.Elapsed;
            a.Tick(3f);
            Assert.AreEqual(t, a.Clock.Elapsed);
            Assert.AreEqual(PlaceResult.Ignored, a.Drop(0, 1));
        }

        [Test]
        public void MultiBoard_AdvancesThenCompletes()
        {
            var a = new LevelAttempt(TwoBoards());
            a.Drop(0, 0); a.Drop(1, 1);
            Assert.IsTrue(a.BoardSolved);
            Assert.IsFalse(a.TryComplete()); // not the last board
            Assert.IsTrue(a.AdvanceBoard());
            Assert.AreEqual(1, a.BoardIndex);
            Assert.AreEqual(0, a.Board.AssignedCount);
            a.Drop(0, 1); a.Drop(1, 0);
            Assert.IsFalse(a.AdvanceBoard());
            Assert.IsTrue(a.TryComplete());
        }

        [Test]
        public void Streak_MilestonesAtThreeAndFive_ResetOnMistake()
        {
            var a = new LevelAttempt(Simple());
            a.Drop(0, 0); a.Drop(1, 0);
            Assert.IsFalse(a.StreakMilestone);
            a.Drop(2, 0);
            Assert.IsTrue(a.StreakMilestone);
            Assert.AreEqual(3, a.Streak);
            a.Drop(3, 0); // wrong
            Assert.AreEqual(0, a.Streak);
            Assert.IsFalse(a.StreakMilestone);
        }

        [Test]
        public void CapacityBoard_RearrangementIsNotAMistake()
        {
            var level = TestObjects.Level("t_cap", TestObjects.Board("apple banana carrot",
                TestObjects.Target("FRUIT", 2), TestObjects.Target("FRUIT VEGETABLES", 6)));
            var a = new LevelAttempt(level);
            a.Drop(0, 0);
            Assert.AreEqual(PlaceResult.Accepted, a.Drop(0, 1));
            Assert.IsTrue(a.ReturnToBoard(0));
            Assert.AreEqual(2, a.Rearrangements);
            Assert.AreEqual(0, a.Mistakes);
        }

        [Test]
        public void Snapshot_RoundTrips()
        {
            var level = TwoBoards();
            var a = new LevelAttempt(level);
            a.Drop(0, 0); a.Drop(1, 1); a.AdvanceBoard();
            a.Drop(0, 1);
            a.Drop(1, 1); // wrong
            a.Tick(4.2f);
            var s = a.Snapshot();
            var b = LevelAttempt.FromSnapshot(level, s);
            Assert.IsNotNull(b);
            Assert.AreEqual(1, b.BoardIndex);
            Assert.AreEqual(1, b.Board.AssignedCount);
            Assert.AreEqual(1, b.Board.Location(0));
            Assert.AreEqual(1, b.WrongCategory);
            Assert.AreEqual(a.Clock.Elapsed, b.Clock.Elapsed, 1e-4f);
            Assert.AreEqual(AttemptState.Playing, b.State);
            Assert.AreEqual(a.AttemptId, b.AttemptId);
            Assert.IsTrue(b.PendingDiscoveries.Contains("spoon"));
        }

        [Test]
        public void Snapshot_RejectedWhenContentChanged()
        {
            var level = TwoBoards();
            var a = new LevelAttempt(level);
            a.Drop(0, 0);
            var s = a.Snapshot();
            level.contentVersion = 2;
            Assert.IsNull(LevelAttempt.FromSnapshot(level, s));
            level.contentVersion = 1;
            s.locations = new[] { 1, -1 }; // apple in VEGETABLES: illegal
            Assert.IsNull(LevelAttempt.FromSnapshot(level, s));
            s.locations = new[] { 0, -1 };
            s.levelId = "other";
            Assert.IsNull(LevelAttempt.FromSnapshot(level, s));
        }

        [Test]
        public void RewardId_IsStablePerLevel()
        {
            Assert.AreEqual("campaign_complete:kitchen_001", new LevelDef { id = "kitchen_001" }.RewardId);
        }
    }
}
