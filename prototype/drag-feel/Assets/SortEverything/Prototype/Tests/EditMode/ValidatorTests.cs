using System.Collections.Generic;
using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class ValidatorTests
    {
        static List<string> V(BoardDef b) { return LevelValidator.Validate(TestObjects.Level("t", b)); }

        [Test]
        public void ValidOrdinaryBoard_Passes()
        {
            var errors = V(TestObjects.Board("apple carrot spoon",
                TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES"), TestObjects.Target("KITCHENWARE")));
            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void ObjectWithNoTarget_Fails()
        {
            CollectionAssert.IsNotEmpty(V(TestObjects.Board("apple carrot toy_car",
                TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES"))));
        }

        [Test]
        public void ObjectWithTwoTargets_FailsOnOrdinaryBoards()
        {
            // apple is FRUIT and FOOD
            CollectionAssert.IsNotEmpty(V(TestObjects.Board("apple carrot", TestObjects.Target("FRUIT"), TestObjects.Target("FOOD"))));
        }

        [Test]
        public void TargetReceivingNothing_Fails()
        {
            CollectionAssert.IsNotEmpty(V(TestObjects.Board("apple banana", TestObjects.Target("FRUIT"), TestObjects.Target("TOYS"))));
        }

        [Test]
        public void LargeObjectOnOrdinaryBoard_Fails()
        {
            CollectionAssert.IsNotEmpty(V(TestObjects.Board("orange* carrot", TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES"))));
        }

        [Test]
        public void UnknownAssetOrCategory_Fails()
        {
            CollectionAssert.IsNotEmpty(V(TestObjects.Board("not_a_thing carrot", TestObjects.Target("FRUIT"), TestObjects.Target("VEGETABLES"))));
            CollectionAssert.IsNotEmpty(V(TestObjects.Board("apple carrot", TestObjects.Target("FRUIT"), TestObjects.Target("NOT_A_CATEGORY"))));
        }

        [Test]
        public void CapacityBoard_SolvableAndUnsolvable()
        {
            var ok = TestObjects.Board("apple banana orange carrot broccoli",
                TestObjects.Target("FRUIT", 2), TestObjects.Target("FRUIT VEGETABLES", 6));
            CollectionAssert.IsEmpty(V(ok));
            Assert.IsNotNull(BoardSolver.Solve(ok));

            var tooMuch = TestObjects.Board("apple banana orange* carrot broccoli carrot",
                TestObjects.Target("FRUIT", 2), TestObjects.Target("FRUIT VEGETABLES", 4));
            Assert.IsNull(BoardSolver.Solve(tooMuch));
            CollectionAssert.IsNotEmpty(V(tooMuch));
        }

        [Test]
        public void Solver_CountsSolutions()
        {
            // 2 fruits, basket holds 2, tray holds 2: either both in the basket, both in the tray, or one each (2 ways).
            var b = TestObjects.Board("apple banana", TestObjects.Target("FRUIT", 2), TestObjects.Target("FRUIT", 2, SortColor.None));
            b.targets[1].id = "second";
            Assert.AreEqual(4, BoardSolver.CountSolutions(b));
        }

        [Test]
        public void SolverResult_IsALegalAssignment()
        {
            var def = TestObjects.Board("orange* apple banana carrot broccoli",
                TestObjects.Target("FRUIT", 2), TestObjects.Target("FRUIT VEGETABLES", 5));
            var plan = BoardSolver.Solve(def);
            Assert.IsNotNull(plan);
            var board = new BoardModel(def);
            Assert.IsTrue(board.Restore(plan));
            Assert.IsTrue(board.Solved);
        }
    }
}
