using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class ValidatorTests
    {
        [Test]
        public void AllAuthoredLevels_Pass()
        {
            Assert.AreEqual(8, LevelLibrary.Playlist.Count);
            foreach (var level in LevelLibrary.Playlist)
            {
                var errors = LevelValidator.Validate(level);
                Assert.AreEqual(0, errors.Count, level.id + ": " + string.Join("; ", errors.ToArray()));
            }
        }

        [Test]
        public void Fails_WhenAnObjectMatchesNoBin()
        {
            var level = new LevelDef("t_none", 1, 20f,
                new[] { BinDef.Color(SortColor.Red), BinDef.Color(SortColor.Blue) },
                new[] { "apple", "notebook", "banana" }); // banana is yellow
            var errors = LevelValidator.Validate(level);
            Assert.IsTrue(errors.Exists(e => e.Contains("banana") && e.Contains("no bin")));
        }

        [Test]
        public void Fails_WhenAnObjectMatchesTwoBins()
        {
            var level = new LevelDef("t_two", 1, 20f,
                new[] { BinDef.Color(SortColor.Blue), BinDef.Category("TOYS", "FF6FAE") },
                new[] { "notebook", "teddy_bear", "toy_train" }); // toy_train is a blue toy
            var errors = LevelValidator.Validate(level);
            Assert.IsTrue(errors.Exists(e => e.Contains("toy_train") && e.Contains("more than one")));
        }

        [Test]
        public void Fails_ForEmptyBin_UnknownObject_AndUnknownCategory()
        {
            var level = new LevelDef("t_bad", 1, 20f,
                new[] { BinDef.Color(SortColor.Red), BinDef.Category("NOT_A_CATEGORY", "F28C28"), BinDef.Color(SortColor.Green) },
                new[] { "apple", "no_such_object" });
            var errors = LevelValidator.Validate(level);
            Assert.IsTrue(errors.Exists(e => e.Contains("unknown object no_such_object")));
            Assert.IsTrue(errors.Exists(e => e.Contains("unknown category NOT_A_CATEGORY")));
            Assert.IsTrue(errors.Exists(e => e.Contains("GREEN") && e.Contains("receives no objects")));
        }

        [Test]
        public void SortingMode_IsDerivedFromBins()
        {
            Assert.AreEqual(SortingMode.Color, LevelLibrary.Playlist[0].Mode);
            Assert.AreEqual(SortingMode.Category, LevelLibrary.Playlist[2].Mode);
            Assert.AreEqual(SortingMode.ColorAndCategory, LevelLibrary.Playlist[4].Mode);
            Assert.AreEqual(SortingMode.Mixed, LevelLibrary.Playlist[6].Mode);
            Assert.AreEqual("SORT BY:", LevelLibrary.Playlist[6].BannerTitle);
            Assert.AreEqual("SORT BY CATEGORY + COLOR", LevelLibrary.Playlist[4].BannerTitle);
        }

        [Test]
        public void ObjectAndBinCounts_ComeFromTheLevel()
        {
            int[] objects = { 6, 8, 8, 9, 9, 10, 10, 12 };
            int[] bins = { 3, 3, 3, 3, 3, 3, 3, 4 };
            for (int i = 0; i < objects.Length; i++)
            {
                Assert.AreEqual(objects[i], LevelLibrary.Playlist[i].objectIds.Length, LevelLibrary.Playlist[i].id);
                Assert.AreEqual(bins[i], LevelLibrary.Playlist[i].bins.Length, LevelLibrary.Playlist[i].id);
            }
        }

        [Test]
        public void EveryObject_HasKnownCategories_AndEveryCategoryRowHasAnObject()
        {
            foreach (var d in ObjectLibrary.All)
            {
                Assert.IsNotNull(d.PrimaryCategory, d.id + " has no primary category");
                foreach (var c in d.Categories) Assert.IsTrue(CategoryLibrary.Exists(c), d.id + ": unknown category " + c);
            }
            Assert.AreEqual(0, ObjectLibrary.CategoryRowsWithoutObject().Count);
        }

        [Test]
        public void CategoryTiers_FollowTheSpec()
        {
            Assert.AreEqual(1, CategoryLibrary.Get("FRUIT").tier);
            Assert.AreEqual(1, CategoryLibrary.Get("VEHICLES").tier);
            Assert.AreEqual(2, CategoryLibrary.Get("KITCHENWARE").tier);
            Assert.AreEqual(2, CategoryLibrary.Get("SNACKS").tier);      // unlisted -> tier 2
            Assert.AreEqual(3, CategoryLibrary.Get("DINOSAURS").tier);
            Assert.AreEqual(3, CategoryLibrary.Get("SUPERHEROES").tier); // thematic group -> tier 3
            Assert.AreEqual("FAST FOOD", CategoryLibrary.Get("FAST_FOOD").label);
        }
    }
}
