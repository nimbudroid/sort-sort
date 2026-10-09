using NUnit.Framework;
using SortEverything.Prototype;
#if UNITY_5_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using UnityIs = UnityEngine.TestTools.Constraints.Is;
#endif

namespace SortEverything.Tests
{
    public class TimerTests
    {
        const float Eps = 1e-4f;

        static RoundSession Session(int playlistIndex = 0) { return new RoundSession(LevelLibrary.Playlist[playlistIndex]); }

        static void SortAll(RoundSession s)
        {
            for (int i = 0; i < s.Objects.Length; i++) s.Drop(i, s.CorrectBin(i));
        }

        [Test]
        public void IdleUntilFirstDrag_ThenRuns()
        {
            var s = Session();
            s.Tick(5f);
            Assert.AreEqual(TimerState.Idle, s.Timer.State);
            Assert.AreEqual(s.Level.timerSeconds, s.Timer.Remaining, Eps);
            s.BeginDrag();
            Assert.AreEqual(TimerState.Running, s.Timer.State);
            s.Tick(2f);
            Assert.AreEqual(s.Level.timerSeconds - 2f, s.Timer.Remaining, Eps);
        }

        [Test]
        public void StopsOnCompletion()
        {
            var s = Session();
            s.BeginDrag();
            s.Tick(3f);
            SortAll(s);
            Assert.AreEqual(SessionState.Complete, s.State);
            Assert.AreEqual(TimerState.Completed, s.Timer.State);
            float left = s.Timer.Remaining;
            s.Tick(5f);
            Assert.AreEqual(left, s.Timer.Remaining, Eps);
            Assert.IsFalse(s.EndFrame());
        }

        [Test]
        public void StopsOnFailure_AndFailureFiresOnce()
        {
            var s = Session();
            s.BeginDrag();
            s.Tick(s.Level.timerSeconds - 0.05f);
            Assert.IsFalse(s.EndFrame());
            s.Tick(0.1f);
            Assert.AreEqual(0f, s.Timer.Remaining, Eps);
            Assert.IsTrue(s.EndFrame());
            Assert.AreEqual(SessionState.TimeUp, s.State);
            Assert.AreEqual(TimerState.Failed, s.Timer.State);
            s.Tick(1f);
            Assert.IsFalse(s.EndFrame());
            Assert.AreEqual(DropResult.Ignored, s.Drop(0, s.CorrectBin(0)));
        }

        [Test]
        public void CompletionOnTheSameTickAsExpiry_CountsAsCompletion()
        {
            var s = Session();
            s.BeginDrag();
            for (int i = 0; i < s.Objects.Length - 1; i++) s.Drop(i, s.CorrectBin(i));
            s.Tick(s.Level.timerSeconds + 1f);           // clock hits zero this frame...
            int last = s.Objects.Length - 1;
            Assert.AreEqual(DropResult.Correct, s.Drop(last, s.CorrectBin(last))); // ...and the final drop lands too
            Assert.IsFalse(s.EndFrame());
            Assert.AreEqual(SessionState.Complete, s.State);
        }

        [Test]
        public void CompletionTime_IncludesPenalties()
        {
            var s = Session(2);
            s.BeginDrag();
            s.Tick(4f);
            int apple = System.Array.IndexOf(s.Level.objectIds, "apple");
            Assert.AreEqual(DropResult.Wrong, s.Drop(apple, (s.CorrectBin(apple) + 1) % s.Level.bins.Length));
            Assert.AreEqual(1f, s.LastPenalty, Eps);
            s.Tick(2f);
            SortAll(s);
            Assert.AreEqual(7f, s.CompletionSeconds, Eps); // 6 s running + 1 s penalty
        }

        [Test]
        public void PenaltyOfZero_ChangesNothing()
        {
            var level = LevelLibrary.Playlist[2];
            var noPenalty = new LevelDef("t_zero", level.designLevel, level.timerSeconds, level.bins, level.objectIds, 0f);
            var s = new RoundSession(noPenalty);
            s.BeginDrag();
            s.Tick(1f);
            float before = s.Timer.Remaining;
            int apple = System.Array.IndexOf(level.objectIds, "apple");
            Assert.AreEqual(DropResult.Wrong, s.Drop(apple, (s.CorrectBin(apple) + 1) % level.bins.Length));
            Assert.AreEqual(before, s.Timer.Remaining, Eps);
            Assert.AreEqual(0f, s.Timer.PenaltySeconds, Eps);
        }

        [Test]
        public void WrongDropAlone_NeverFailsTheRound()
        {
            var s = Session(2);
            s.BeginDrag();
            s.Tick(s.Level.timerSeconds - 0.3f);
            int apple = System.Array.IndexOf(s.Level.objectIds, "apple");
            s.Drop(apple, (s.CorrectBin(apple) + 1) % s.Level.bins.Length);
            Assert.IsTrue(s.Timer.Remaining > 0f);
            Assert.IsFalse(s.EndFrame());
            Assert.AreEqual(SessionState.Playing, s.State);
        }

        [Test]
        public void DoesNotAdvanceWhilePaused()
        {
            var s = Session();
            s.BeginDrag();
            s.Paused = true;
            s.Tick(10f);
            Assert.AreEqual(s.Level.timerSeconds, s.Timer.Remaining, Eps);
            s.Paused = false;
            s.Tick(1f);
            Assert.AreEqual(s.Level.timerSeconds - 1f, s.Timer.Remaining, Eps);
        }

        [Test]
        public void Urgency_NormalLowCritical()
        {
            var t = new RoundTimer(20f);
            t.Start();
            Assert.AreEqual(TimerUrgency.Normal, t.Urgency);
            t.Advance(14.1f); // 5.9 s left <= 30% of 20
            Assert.AreEqual(TimerUrgency.Low, t.Urgency);
            t.Advance(1f);    // 4.9 s left
            Assert.AreEqual(TimerUrgency.Critical, t.Urgency);
        }

        [Test]
        public void DisplayText_ShowsTenths_RoundedUp()
        {
            var t = new RoundTimer(12.4f);
            var text = new TimerText(12.4f);
            Assert.AreEqual("12.4", text.For(t.DisplayTenths));
            t.Start();
            t.Advance(0.05f);
            Assert.AreEqual("12.4", text.For(t.DisplayTenths));
            t.Advance(12.3f);
            Assert.AreEqual("0.1", text.For(t.DisplayTenths));
            t.Advance(1f);
            Assert.AreEqual("0.0", text.For(t.DisplayTenths));
        }

        [Test]
        public void BestTime_UpdatesOnlyWhenBeaten()
        {
            var store = new MemoryBestTimeStore();
            float best;
            bool had;
            Assert.IsTrue(BestTimeBook.Submit(store, "L", 9f, out best, out had));
            Assert.IsFalse(had);
            Assert.AreEqual(9f, best, Eps);
            Assert.IsFalse(BestTimeBook.Submit(store, "L", 9.5f, out best, out had));
            Assert.IsTrue(had);
            Assert.AreEqual(9f, best, Eps);
            Assert.IsFalse(BestTimeBook.Submit(store, "L", 9f, out best, out had)); // a tie is not beaten
            Assert.IsTrue(BestTimeBook.Submit(store, "L", 8.42f, out best, out had));
            Assert.AreEqual(8.42f, best, Eps);
            float stored;
            Assert.IsTrue(store.TryGet("L", out stored));
            Assert.AreEqual(8.42f, stored, Eps);
        }

#if UNITY_5_3_OR_NEWER
        [Test]
        public void RunningTimerAndDrops_DoNotAllocate()
        {
            var s = Session(2);
            var text = new TimerText(s.Level.timerSeconds);
            s.BeginDrag();
            s.Drop(0, s.CorrectBin(0)); // warm up
            Assert.That(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    s.Tick(0.016f);
                    text.For(s.Timer.DisplayTenths);
                    if (s.Timer.Urgency == TimerUrgency.Critical) s.Paused = false;
                }
                s.Drop(1, s.CorrectBin(1));
                s.EndFrame();
            }, UnityIs.Not.AllocatingGCMemory());
        }
#endif
    }
}
