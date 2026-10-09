using System.Collections.Generic;
using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    /// <summary>The authored Kitchen matches the GDD's twenty-scene plan; the Pantry follows it in the house order.</summary>
    public class ContentTests
    {
        static Campaign C() { return CatalogLoader.Load(TestObjects.ReadCampaignFile); }

        static readonly string[] Kitchen18 =
        {
            "apple", "banana", "orange", "strawberry", "carrot", "pepper", "broccoli", "corn", "water_bottle",
            "milk_carton", "juice_carton", "soda_can", "spoon", "fork", "plate", "mug", "spatula", "pan",
        };

        // GDD section 9: object count, F/V/D/K mix and template per level ("" = unlimited targets).
        static readonly object[][] Plan =
        {
            new object[] { 6, "3300", "" }, new object[] { 8, "4040", "" }, new object[] { 9, "3330", "" },
            new object[] { 12, "4440", "" }, new object[] { 9, "3330", "" }, new object[] { 8, "4400", "P1" },
            new object[] { 6, "3030", "" }, new object[] { 8, "4400", "P1" }, new object[] { 12, "3333", "" },
            new object[] { 9, "3330", "" }, new object[] { 9, "3330", "P2" }, new object[] { 9, "3330", "P2" },
            new object[] { 12, "3333", "P3" }, new object[] { 5, "2210", "P4" }, new object[] { 12, "3333", "" },
            new object[] { 5, "2210", "P4" }, new object[] { 9, "3330", "" }, new object[] { 12, "3333", "" },
            new object[] { 12, "3333", "P3" }, new object[] { 24, "6666", "P3" },
        };

        static int Cat(ObjectDef d)
        {
            if (d.HasCategory("FRUIT")) return 0;
            if (d.HasCategory("VEGETABLES")) return 1;
            if (d.HasCategory("DRINKS")) return 2;
            return d.HasCategory("KITCHENWARE") ? 3 : -1;
        }

        [Test]
        public void Kitchen_IsTheGddTwentyInOrder_ThenPantry()
        {
            var c = C();
            var k = c.LevelsInRoom("kitchen");
            Assert.AreEqual(20, k.Count);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual("kitchen_" + (i + 1).ToString("00"), k[i].id);
                Assert.AreSame(k[i], c.Levels[i]);
                Assert.AreEqual(i + 1, k[i].section);
            }
            Assert.AreEqual("pantry", c.Levels[20].roomId);
            Assert.AreEqual(4, c.Room("kitchen").areas.Length);
        }

        [Test]
        public void Kitchen_MatchesTheGddCompositionAndTemplates()
        {
            var k = C().LevelsInRoom("kitchen");
            for (int i = 0; i < 20; i++)
            {
                var l = k[i];
                int n = 0;
                var mix = new int[4];
                bool capacity = false;
                foreach (var b in l.boards)
                {
                    capacity |= b.IsCapacity;
                    foreach (var o in b.objects)
                    {
                        n++;
                        int cat = Cat(ObjectLibrary.Get(o.asset));
                        Assert.IsTrue(cat >= 0, l.id + " " + o.asset);
                        mix[cat]++;
                    }
                }
                Assert.AreEqual((int)Plan[i][0], n, l.id + " count");
                Assert.AreEqual((string)Plan[i][1], "" + mix[0] + mix[1] + mix[2] + mix[3], l.id + " mix");
                Assert.AreEqual(((string)Plan[i][2]).Length > 0, capacity, l.id + " template");
            }
        }

        [Test]
        public void LargeObjects_OnlyWhereTheTemplateCallsForThem()
        {
            foreach (var l in C().LevelsInRoom("kitchen"))
                foreach (var b in l.boards)
                {
                    int large = 0;
                    foreach (var o in b.objects) if (o.units == 2) large++;
                    int expected = b.objects.Length == 12 && b.IsCapacity ? 4 : b.objects.Length == 9 && b.IsCapacity ? 3
                        : b.objects.Length == 5 ? 1 : 0;
                    Assert.AreEqual(expected, large, l.id);
                }
        }

        [Test]
        public void AllEighteenKitchenAssets_IntroducedByLevel18_AndNothingElse()
        {
            var k = C().LevelsInRoom("kitchen");
            var seen = new HashSet<string>();
            for (int i = 0; i < 18; i++) foreach (var b in k[i].boards) foreach (var o in b.objects) seen.Add(o.asset);
            foreach (var a in Kitchen18) Assert.IsTrue(seen.Contains(a), a);
            foreach (var l in k) foreach (var b in l.boards) foreach (var o in b.objects)
                Assert.IsTrue(System.Array.IndexOf(Kitchen18, o.asset) >= 0, l.id + " uses " + o.asset);
        }

        [Test]
        public void FirstCompletionCoins_KitchenTotalIs1000()
        {
            int total = 0;
            foreach (var l in C().LevelsInRoom("kitchen")) total += l.coins;
            Assert.AreEqual(1000, total);
        }

        [Test]
        public void HouseOrder_UnavailableRoomsAreLockedPlaceholders()
        {
            var c = C();
            foreach (var r in c.House.rooms)
            {
                bool built = r.id == "kitchen" || r.id == "pantry";
                Assert.AreEqual(built, r.available, r.id);
                if (!built) Assert.AreEqual(0, c.LevelsInRoom(r.id).Count);
                Assert.Greater(r.plannedAreas, 0);
            }
        }
    }
}
