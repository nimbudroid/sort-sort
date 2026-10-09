using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class RuleTests
    {
        static bool Accepts(TargetDef t, string asset) { return RuleEvaluator.Accepts(t, TestObjects.Lib(asset)); }

        [Test]
        public void ColorTarget_AcceptsMatchingPrimaryColor_AcrossCategories()
        {
            var red = TestObjects.Target(null, -1, SortColor.Red);
            Assert.IsTrue(Accepts(red, "apple"));
            Assert.IsTrue(Accepts(red, "alarm_clock"));
            Assert.IsTrue(Accepts(red, "plate"));
            Assert.IsFalse(Accepts(red, "banana"));
        }

        [Test]
        public void CategoryTarget_AcceptsMatchingCategory_AcrossColors()
        {
            var kitchenware = TestObjects.Target("KITCHENWARE");
            Assert.IsTrue(Accepts(kitchenware, "plate"));
            Assert.IsTrue(Accepts(kitchenware, "spoon"));
            Assert.IsTrue(Accepts(kitchenware, "bowl"));
            Assert.IsFalse(Accepts(kitchenware, "toy_car"));
        }

        [Test]
        public void ColorAndCategory_RequiresBoth()
        {
            var redFruit = TestObjects.Target("FRUIT", -1, SortColor.Red);
            Assert.IsTrue(Accepts(redFruit, "apple"));
            Assert.IsFalse(Accepts(redFruit, "banana"));
            Assert.IsFalse(Accepts(redFruit, "toy_car"));
        }

        [Test]
        public void SharedTarget_AcceptsAnyListedCategory()
        {
            var tray = TestObjects.Target("FRUIT VEGETABLES", 6);
            Assert.IsTrue(Accepts(tray, "apple"));
            Assert.IsTrue(Accepts(tray, "carrot"));
            Assert.IsFalse(Accepts(tray, "spoon"));
        }

        [Test]
        public void PrimaryAndSecondaryCategories_BothMatch()
        {
            var apple = TestObjects.Lib("apple");
            Assert.AreEqual("FRUIT", apple.PrimaryCategory);
            Assert.IsTrue(RuleEvaluator.Accepts(TestObjects.Target("FRUIT"), apple));
            Assert.IsTrue(RuleEvaluator.Accepts(TestObjects.Target("FOOD"), apple));
        }

        [Test]
        public void NoImplicitHierarchy()
        {
            var fruitOnly = TestObjects.Make("fruit_only", SortColor.Red, "FRUIT");
            Assert.IsTrue(RuleEvaluator.Accepts(TestObjects.Target("FRUIT"), fruitOnly));
            Assert.IsFalse(RuleEvaluator.Accepts(TestObjects.Target("FOOD"), fruitOnly));
        }

        [Test]
        public void ColorUsesPrimaryColorOnly()
        {
            var ball = TestObjects.Lib("beach_ball"); // blue, with red and yellow as descriptive secondary colours
            Assert.IsTrue(RuleEvaluator.Accepts(TestObjects.Target(null, -1, SortColor.Blue), ball));
            Assert.IsFalse(RuleEvaluator.Accepts(TestObjects.Target(null, -1, SortColor.Red), ball));
        }

        [Test]
        public void EmptyTarget_AcceptsNothing()
        {
            Assert.IsFalse(Accepts(TestObjects.Target(null), "apple"));
        }

        [Test]
        public void Labels_ReadAsTheRule()
        {
            Assert.AreEqual("RED", RuleEvaluator.Label(null, SortColor.Red));
            Assert.AreEqual("FRUIT", RuleEvaluator.Label(new[] { "FRUIT" }, SortColor.None));
            Assert.AreEqual("RED FRUIT", RuleEvaluator.Label(new[] { "FRUIT" }, SortColor.Red));
            Assert.AreEqual("FAST FOOD", RuleEvaluator.Label(new[] { "FAST_FOOD" }, SortColor.None));
            Assert.AreEqual("FRUIT + VEGETABLES", RuleEvaluator.Label(new[] { "FRUIT", "VEGETABLES" }, SortColor.None));
        }
    }
}
