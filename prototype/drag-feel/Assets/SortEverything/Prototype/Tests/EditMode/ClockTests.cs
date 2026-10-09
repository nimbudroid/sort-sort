using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class ClockTests
    {
        [Test]
        public void CountsUpOnlyWhileRunningAndNotPaused()
        {
            var c = new ActiveClock();
            c.Advance(1f);
            Assert.AreEqual(0f, c.Elapsed);
            c.Start();
            c.Advance(1f);
            c.Paused = true;
            c.Advance(10f);
            c.Paused = false;
            c.Advance(0.5f);
            Assert.AreEqual(1.5f, c.Elapsed, 1e-4f);
            c.Stop();
            c.Advance(3f);
            Assert.AreEqual(1.5f, c.Elapsed, 1e-4f);
        }

        [Test]
        public void IgnoresNegativeOrHugeFrames()
        {
            var c = new ActiveClock();
            c.Start();
            c.Advance(-1f);
            Assert.AreEqual(0f, c.Elapsed);
        }

        [Test]
        public void DisplayFloorsToTenths()
        {
            var c = new ActiveClock();
            c.Reset(12.49f);
            Assert.AreEqual(124, c.DisplayTenths);
            c.Reset(12.5f);
            Assert.AreEqual(125, c.DisplayTenths);
            c.Reset(-3f);
            Assert.AreEqual(0, c.DisplayTenths);
        }

        [Test]
        public void TimerText_IsCachedAndFormatted()
        {
            var t = new TimerText(120f);
            Assert.AreEqual("12.4", t.For(124));
            Assert.AreSame(t.For(124), t.For(124));
            Assert.AreEqual("0.0", t.For(0));
        }
    }
}
