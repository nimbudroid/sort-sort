using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class BoardModelTests
    {
        static BoardDef Dedicated()
        {
            return TestObjects.Board("apple banana carrot spoon",
                TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES"), TestObjects.Target("KITCHENWARE"));
        }

        // P1: fruit basket (2) + shared produce tray (6).
        static BoardDef P1()
        {
            return TestObjects.Board("apple banana orange strawberry carrot broccoli",
                TestObjects.Target("FRUIT", 2), TestObjects.Target("FRUIT VEGETABLES", 6));
        }

        [Test]
        public void WrongTarget_IsRefused_AndChangesNothing()
        {
            var b = new BoardModel(Dedicated());
            Assert.AreEqual(PlaceResult.WrongTarget, b.Place(0, 1));
            Assert.AreEqual(-1, b.Location(0));
            Assert.AreEqual(0, b.AssignedCount);
            Assert.AreEqual(0, b.Count(1));
        }

        [Test]
        public void Accepted_CommitsAndCounts()
        {
            var b = new BoardModel(Dedicated());
            Assert.AreEqual(2, b.Expected(0));
            Assert.AreEqual(PlaceResult.Accepted, b.Place(0, 0));
            Assert.AreEqual(0, b.Location(0));
            Assert.AreEqual(1, b.Count(0));
            Assert.AreEqual(1, b.AssignedCount);
            Assert.AreEqual(PlaceResult.Ignored, b.Place(0, 0)); // same target again is a no-op
        }

        [Test]
        public void OrdinaryBoard_PlacedObjectsAreFinal()
        {
            var b = new BoardModel(Dedicated());
            b.Place(0, 0);
            Assert.IsFalse(b.CanPickUp(0));
            Assert.IsFalse(b.Unassign(0));
            Assert.IsTrue(b.CanPickUp(1));
        }

        [Test]
        public void Solved_WhenEveryObjectIsAssigned()
        {
            var b = new BoardModel(Dedicated());
            b.Place(0, 0); b.Place(1, 0); b.Place(2, 1);
            Assert.IsFalse(b.Solved);
            b.Place(3, 2);
            Assert.IsTrue(b.Solved);
        }

        [Test]
        public void CapacityTarget_RefusesWhenFull()
        {
            var b = new BoardModel(P1());
            Assert.AreEqual(PlaceResult.Accepted, b.Place(0, 0));
            Assert.AreEqual(PlaceResult.Accepted, b.Place(1, 0));
            Assert.AreEqual(PlaceResult.Full, b.Check(2, 0));
            Assert.AreEqual(PlaceResult.Full, b.Place(2, 0));
            Assert.AreEqual(-1, b.Location(2));
            Assert.AreEqual(2, b.Used(0));
        }

        [Test]
        public void LargeObject_CostsTwoUnits()
        {
            var b = new BoardModel(TestObjects.Board("orange* apple", TestObjects.Target("FRUIT", 2), TestObjects.Target("FRUIT VEGETABLES", 6)));
            Assert.AreEqual(PlaceResult.Accepted, b.Place(0, 0));
            Assert.AreEqual(2, b.Used(0));
            Assert.AreEqual(PlaceResult.Full, b.Place(1, 0));
        }

        [Test]
        public void CapacityBoard_TransferIsTransactional()
        {
            var b = new BoardModel(P1());
            b.Place(0, 0); b.Place(1, 0);
            Assert.AreEqual(PlaceResult.Accepted, b.Place(0, 1)); // move apple to the tray
            Assert.AreEqual(1, b.Used(0));
            Assert.AreEqual(1, b.Used(1));
            Assert.AreEqual(2, b.AssignedCount);
            Assert.AreEqual(PlaceResult.WrongTarget, b.Place(4, 0)); // carrot never fits the fruit basket
            Assert.AreEqual(-1, b.Location(4));
        }

        [Test]
        public void CapacityBoard_UnassignIsLegal()
        {
            var b = new BoardModel(P1());
            b.Place(0, 0);
            Assert.IsTrue(b.CanPickUp(0));
            Assert.IsTrue(b.Unassign(0));
            Assert.AreEqual(-1, b.Location(0));
            Assert.AreEqual(0, b.Used(0));
            Assert.AreEqual(0, b.AssignedCount);
        }

        [Test]
        public void Restore_RevalidatesEveryPlacement()
        {
            var b = new BoardModel(P1());
            b.Place(0, 0); b.Place(4, 1);
            var snap = b.Snapshot();
            var c = new BoardModel(P1());
            Assert.IsTrue(c.Restore(snap));
            Assert.AreEqual(2, c.AssignedCount);
            Assert.AreEqual(1, c.Used(0));

            Assert.IsFalse(c.Restore(new[] { 0, 0, 0, -1, -1, -1 })); // three fruits in a 2-unit basket
            Assert.AreEqual(0, c.AssignedCount);
            Assert.IsFalse(c.Restore(new[] { -1, -1, -1, -1, 0, -1 })); // carrot in the fruit basket
            Assert.IsFalse(c.Restore(new[] { 0 }));                     // wrong length
        }
    }
}
