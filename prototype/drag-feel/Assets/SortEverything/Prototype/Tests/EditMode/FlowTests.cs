using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class FlowTests
    {
        [Test]
        public void CompletingALevel_ThenNext_LoadsTheNextPlaylistEntry()
        {
            var playlist = new Playlist(LevelLibrary.Playlist);
            var s = new RoundSession(playlist.Current);
            s.BeginDrag();
            for (int i = 0; i < s.Objects.Length; i++) Assert.AreEqual(DropResult.Correct, s.Drop(i, s.CorrectBin(i)));
            Assert.AreEqual(SessionState.Complete, s.State);

            var next = playlist.Advance();
            Assert.AreSame(LevelLibrary.Playlist[1], next);
            Assert.AreEqual(2, playlist.Number);
            var s2 = new RoundSession(next);
            Assert.AreEqual(SessionState.Ready, s2.State);
            Assert.AreEqual(TimerState.Idle, s2.Timer.State);
        }

        [Test]
        public void Playlist_WalksInOrder_AndWraps()
        {
            var playlist = new Playlist(LevelLibrary.Playlist);
            for (int i = 1; i < LevelLibrary.Playlist.Count; i++) Assert.AreSame(LevelLibrary.Playlist[i], playlist.Advance());
            Assert.AreSame(LevelLibrary.Playlist[0], playlist.Advance());
        }

        [Test]
        public void RetryAfterTimeUp_ResetsTimerToIdle_AndRestoresAllObjects()
        {
            var s = new RoundSession(LevelLibrary.Playlist[3]);
            s.BeginDrag();
            s.Drop(0, s.CorrectBin(0));
            s.Drop(1, s.CorrectBin(1));
            s.Tick(s.Level.timerSeconds + 1f);
            Assert.IsTrue(s.EndFrame());
            Assert.AreEqual(SessionState.TimeUp, s.State);

            s.Restart();
            Assert.AreEqual(SessionState.Ready, s.State);
            Assert.AreEqual(TimerState.Idle, s.Timer.State);
            Assert.AreEqual(s.Level.timerSeconds, s.Timer.Remaining, 1e-4f);
            Assert.AreEqual(0, s.SortedCount);
            for (int i = 0; i < s.Objects.Length; i++) Assert.IsFalse(s.IsSorted(i));
            for (int b = 0; b < s.BinCount.Length; b++) Assert.AreEqual(0, s.BinCount[b]);
        }

        [Test]
        public void BinCapacities_MatchTheObjects()
        {
            foreach (var level in LevelLibrary.Playlist)
            {
                var s = new RoundSession(level);
                int total = 0;
                foreach (int c in s.BinCapacity) { Assert.IsTrue(c > 0, level.id); total += c; }
                Assert.AreEqual(level.objectIds.Length, total, level.id);
            }
        }

        [Test]
        public void WrongRollIn_BeforeFirstDrag_HasNoPenalty_AndKeepsTheClockIdle()
        {
            var s = new RoundSession(LevelLibrary.Playlist[0]);
            int wrongBin = (s.CorrectBin(0) + 1) % s.Level.bins.Length;
            Assert.AreEqual(DropResult.Wrong, s.Drop(0, wrongBin));
            Assert.AreEqual(SessionState.Ready, s.State);
            Assert.AreEqual(TimerState.Idle, s.Timer.State);
            Assert.AreEqual(0f, s.Timer.PenaltySeconds, 1e-4f);
        }

        [Test]
        public void Combo_IsOffByDefault_AndCosmeticWhenOn()
        {
            var level = LevelLibrary.Playlist[7];
            var off = new RoundSession(level);
            off.BeginDrag();
            for (int i = 0; i < 5; i++) off.Drop(i, off.CorrectBin(i));
            Assert.AreEqual(1, off.Multiplier);

            var withCombo = new LevelDef("t_combo", level.designLevel, level.timerSeconds, level.bins, level.objectIds,
                level.wrongDropPenalty, true);
            var on = new RoundSession(withCombo);
            on.BeginDrag();
            for (int i = 0; i < 3; i++) on.Drop(i, on.CorrectBin(i));
            Assert.AreEqual(2, on.Multiplier);
            Assert.IsTrue(on.MultiplierRaised);
            for (int i = 3; i < 5; i++) on.Drop(i, on.CorrectBin(i));
            Assert.AreEqual(3, on.Multiplier);
            float remaining = on.Timer.Remaining;
            on.Drop(5, (on.CorrectBin(5) + 1) % level.bins.Length); // wrong drop resets the combo
            Assert.AreEqual(1, on.Multiplier);
            Assert.AreEqual(0, on.Streak);
            Assert.AreEqual(remaining - level.wrongDropPenalty, on.Timer.Remaining, 1e-4f); // combo never changes time
        }
    }
}
