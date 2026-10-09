using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class RuleMatchingTests
    {
        [Test]
        public void ColorBin_AcceptsMatchingColor_AcrossCategories()
        {
            var red = BinRule.ByColor(SortColor.Red);
            Assert.IsTrue(red.Matches(TestObjects.Lib("apple")));        // red FRUIT
            Assert.IsTrue(red.Matches(TestObjects.Lib("alarm_clock")));  // red BEDROOM
            Assert.IsTrue(red.Matches(TestObjects.Lib("plate")));        // red KITCHENWARE
            Assert.IsFalse(red.Matches(TestObjects.Lib("banana")));      // yellow
        }

        [Test]
        public void CategoryBin_AcceptsMatchingCategory_AcrossColors()
        {
            var kitchenware = BinRule.ByCategory("KITCHENWARE");
            Assert.IsTrue(kitchenware.Matches(TestObjects.Lib("plate")));      // red
            Assert.IsTrue(kitchenware.Matches(TestObjects.Lib("coffee_mug"))); // blue
            Assert.IsTrue(kitchenware.Matches(TestObjects.Lib("spoon")));      // green
            Assert.IsTrue(kitchenware.Matches(TestObjects.Lib("bowl")));       // yellow
            Assert.IsFalse(kitchenware.Matches(TestObjects.Lib("toy_car")));
        }

        [Test]
        public void ColorAndCategoryBin_RequiresBoth()
        {
            var redFruit = new BinRule(SortColor.Red, "FRUIT");
            Assert.IsTrue(redFruit.Matches(TestObjects.Lib("apple")));     // red fruit
            Assert.IsFalse(redFruit.Matches(TestObjects.Lib("banana")));   // right category, wrong colour
            Assert.IsFalse(redFruit.Matches(TestObjects.Lib("toy_car")));  // right colour, wrong category
            Assert.AreEqual(RuleShape.ColorAndCategory, redFruit.Shape);
        }

        [Test]
        public void PrimaryAndSecondaryCategories_BothMatch()
        {
            var apple = TestObjects.Lib("apple");
            Assert.AreEqual("FRUIT", apple.PrimaryCategory);
            Assert.IsTrue(BinRule.ByCategory("FRUIT").Matches(apple)); // primary
            Assert.IsTrue(BinRule.ByCategory("FOOD").Matches(apple));  // secondary
        }

        [Test]
        public void ObjectWithSeveralCategories_MatchesEachIndividually()
        {
            var pencil = TestObjects.Lib("pencil");
            foreach (var c in new[] { "SCHOOL", "OFFICE", "WRITING", "STATIONERY" })
                Assert.IsTrue(BinRule.ByCategory(c).Matches(pencil), c);
            Assert.IsFalse(BinRule.ByCategory("TOYS").Matches(pencil));
        }

        [Test]
        public void NoImplicitHierarchy()
        {
            // FRUIT does not imply FOOD: only listed categories count.
            var fruitOnly = TestObjects.Make("fruit_only", SortColor.Red, "FRUIT");
            Assert.IsTrue(BinRule.ByCategory("FRUIT").Matches(fruitOnly));
            Assert.IsFalse(BinRule.ByCategory("FOOD").Matches(fruitOnly));
        }

        [Test]
        public void ColorUsesPrimaryColorOnly()
        {
            // The beach ball is blue with red and yellow as descriptive secondary colours.
            var ball = TestObjects.Lib("beach_ball");
            Assert.IsTrue(BinRule.ByColor(SortColor.Blue).Matches(ball));
            Assert.IsFalse(BinRule.ByColor(SortColor.Red).Matches(ball));
            Assert.IsFalse(BinRule.ByColor(SortColor.Yellow).Matches(ball));
        }

        [Test]
        public void EmptyRule_MatchesNothing()
        {
            var none = new BinRule(SortColor.None, null);
            Assert.AreEqual(RuleShape.Invalid, none.Shape);
            Assert.IsFalse(none.Matches(TestObjects.Lib("apple")));
        }

        [Test]
        public void WrongBin_IsRejected_AndDoesNotFailTheLevel()
        {
            var level = LevelLibrary.Playlist[2]; // FRUIT, TOYS, KITCHEN
            var session = new RoundSession(level);
            session.BeginDrag();
            int apple = System.Array.IndexOf(level.objectIds, "apple");
            int toysBin = 1;
            Assert.AreEqual(DropResult.Wrong, session.Drop(apple, toysBin));
            Assert.AreEqual(SessionState.Playing, session.State);
            Assert.IsFalse(session.IsSorted(apple));
            Assert.AreEqual(1, session.WrongDrops);
            Assert.AreEqual(DropResult.Correct, session.Drop(apple, 0));
        }

        [Test]
        public void Labels_ReadAsTheRule()
        {
            Assert.AreEqual("RED", RuleEvaluator.Label(BinRule.ByColor(SortColor.Red)));
            Assert.AreEqual("FRUIT", RuleEvaluator.Label(BinRule.ByCategory("FRUIT")));
            Assert.AreEqual("RED FRUIT", RuleEvaluator.Label(new BinRule(SortColor.Red, "FRUIT")));
            Assert.AreEqual("FAST FOOD", RuleEvaluator.Label(BinRule.ByCategory("FAST_FOOD")));
        }
    }
}
