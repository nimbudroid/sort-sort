using System.Collections.Generic;
using NUnit.Framework;
using SortEverything.Prototype;

namespace SortEverything.Tests
{
    public class CatalogTests
    {
        static Campaign Authored() { return CatalogLoader.Load(TestObjects.ReadCampaignFile); }

        [Test]
        public void AuthoredCampaign_LoadsAndValidates()
        {
            var c = Authored();
            var all = new List<LevelDef>(c.AllLevels);
            all.AddRange(c.Lab);
            var errors = LevelValidator.ValidateCampaign(c, all);
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors.ToArray()));
            Assert.Greater(c.Lab.Count, 0);
        }

        [Test]
        public void House_RoomsFollowTheApprovedOrder()
        {
            var rooms = Authored().House.rooms;
            var ids = new string[rooms.Length];
            for (int i = 0; i < rooms.Length; i++) ids[i] = rooms[i].id;
            CollectionAssert.AreEqual(new[] { "kitchen", "pantry", "kids_room", "bathroom", "entryway", "office" }, ids);
        }

        [Test]
        public void EveryCampaignLevel_IsSolvable()
        {
            var c = Authored();
            var all = new List<LevelDef>(c.AllLevels);
            all.AddRange(c.Lab);
            foreach (var l in all)
                foreach (var b in l.boards)
                    Assert.IsNotNull(BoardSolver.Solve(b), l.id);
        }

        const string MiniCampaign = @"{
          // comments are allowed
          ""schemaVersion"": 1,
          ""categoryColors"": { ""FRUIT"": ""F28C28"" },
          ""templates"": { ""T"": [ { ""id"": ""basket"", ""accepts"": [""FRUIT""], ""capacity"": 2 },
                                  { ""id"": ""tray"", ""accepts"": [""FRUIT"", ""VEGETABLES""], ""capacity"": 6 } ] },
          ""house"": { ""id"": ""h"" },
          ""rooms"": [
            { ""id"": ""r1"", ""name"": ""Room One"", ""available"": true,
              ""areas"": [ { ""id"": ""a1"", ""name"": ""Area"", ""sections"": [""l1"", ""l2""] } ] },
            { ""id"": ""r2"", ""name"": ""Room Two"", ""available"": true,
              ""areas"": [ { ""id"": ""a2"", ""name"": ""Area 2"", ""sections"": [""l3""] } ] },
            { ""id"": ""r3"", ""name"": ""Locked"", ""available"": false, ""plannedAreas"": 3 }
          ],
          ""levelFiles"": [""levels""]
        }";

        const string MiniLevels = @"{ ""levels"": [
          { ""id"": ""l2"", ""title"": ""Two"", ""boards"": [ { ""objects"": ""orange* apple carrot"", ""targets"": ""T"" } ] },
          { ""id"": ""l1"", ""title"": ""One"", ""version"": 3, ""coins"": 75,
            ""boards"": [ { ""objects"": ""apple apple carrot"", ""targets"": ""FRUIT VEGETABLES"" } ] },
          { ""id"": ""l3"", ""title"": ""Three"", ""boards"": [ { ""objects"": ""apple spoon"",
            ""targets"": [ { ""accepts"": [""FRUIT""], ""color"": ""RED"" }, { ""accepts"": [""KITCHENWARE""], ""label"": ""SPOONS"" } ] } ] }
        ] }";

        static Campaign Mini()
        {
            return CatalogLoader.Load(n => n == "campaign" ? MiniCampaign : n == "levels" ? MiniLevels : null);
        }

        [Test]
        public void Order_ComesFromRoomsAndAreas_NotFileOrder()
        {
            var c = Mini();
            Assert.AreEqual(3, c.Levels.Count);
            Assert.AreEqual("l1", c.Levels[0].id);
            Assert.AreEqual("l2", c.Levels[1].id);
            Assert.AreEqual("l3", c.Levels[2].id);
            Assert.AreEqual("r1", c.Levels[1].roomId);
            Assert.AreEqual(2, c.Levels[1].section);
            Assert.AreEqual(1, c.Levels[2].section); // sections restart per room
            Assert.AreEqual("a2", c.Levels[2].areaId);
            Assert.AreSame(c.Levels[1], c.Next(c.Levels[0]));
            Assert.IsNull(c.Next(c.Levels[2]));
            Assert.AreEqual(2, c.LevelsInRoom("r1").Count);
        }

        [Test]
        public void Tokens_TemplatesAndExplicitTargets_Parse()
        {
            var c = Mini();
            var l1 = c.Get("l1");
            Assert.AreEqual(3, l1.contentVersion);
            Assert.AreEqual(75, l1.coins);
            Assert.AreEqual("apple_1", l1.boards[0].objects[0].id);
            Assert.AreEqual("apple_2", l1.boards[0].objects[1].id);
            Assert.AreEqual("F28C28", l1.boards[0].targets[0].colorHex);
            Assert.IsTrue(l1.boards[0].targets[0].Unlimited);

            var l2 = c.Get("l2");
            Assert.AreEqual(2, l2.boards[0].objects[0].units);
            Assert.AreEqual("orange", l2.boards[0].objects[0].asset);
            Assert.IsTrue(l2.boards[0].IsCapacity);
            Assert.AreEqual("T", l2.boards[0].template);

            var l3 = c.Get("l3");
            Assert.AreEqual(SortColor.Red, l3.boards[0].targets[0].color);
            Assert.AreEqual("RED FRUIT", l3.boards[0].targets[0].DisplayLabel);
            Assert.AreEqual("SPOONS", l3.boards[0].targets[1].DisplayLabel);
            CollectionAssert.IsEmpty(LevelValidator.ValidateCampaign(c, c.AllLevels));
        }

        [Test]
        public void LockedRooms_ContributeNoLevels_ButStayInTheHouse()
        {
            var c = Mini();
            Assert.AreEqual(3, c.House.rooms.Length);
            Assert.IsFalse(c.Room("r3").available);
            Assert.AreEqual(3, c.Room("r3").plannedAreas);
            Assert.AreEqual(0, c.LevelsInRoom("r3").Count);
        }

        [Test]
        public void MissingSection_IsReported()
        {
            var c = CatalogLoader.Load(n => n == "campaign" ? MiniCampaign.Replace("\"l3\"]", "\"l9\"]") : n == "levels" ? MiniLevels : null);
            CollectionAssert.IsNotEmpty(LevelValidator.ValidateCampaign(c, c.AllLevels));
        }

        [Test]
        public void UnsupportedSchema_Throws()
        {
            Assert.Throws<System.FormatException>(() =>
                CatalogLoader.Load(n => n == "campaign" ? MiniCampaign.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99") : null));
        }

        [Test]
        public void Json_RoundTrips()
        {
            var obj = MiniJson.Parse("{\"b\": [1, 2.5, \"x\\\"y\", true, null], \"a\": {\"k\": -3}}");
            string text = MiniJson.Write(obj);
            Assert.AreEqual(text, MiniJson.Write(MiniJson.Parse(text)));
            Assert.IsTrue(text.IndexOf("\"a\"") < text.IndexOf("\"b\"")); // sorted keys
        }
    }
}
