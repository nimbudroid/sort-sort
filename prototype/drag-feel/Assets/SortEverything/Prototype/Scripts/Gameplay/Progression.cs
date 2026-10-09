using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>What one completion changed. Built once at commit; replaying the result screen re-reads it.</summary>
    public sealed class CompletionResult
    {
        public LevelDef level;
        public bool campaign;                 // false for lab levels (no rewards, no progress)
        public bool firstCompletion;
        public int coinsEarned;
        public AreaDef areaRestored;          // area whose last section this was (first time only)
        public RoomDef roomCompleted;         // room finished (first time only)
        public bool houseCompleted;           // every room in the house finished (first time only)
        public readonly List<string> newDiscoveries = new List<string>();
        public float time;                    // active seconds
        public bool recordEligible;           // Mastery, records enabled, unassisted
        public bool newBest;
        public float previousBest;            // 0 when there was none
        public LevelDef next;                 // the next authored level, or null at the end of what is built
    }

    /// <summary>
    /// The campaign's rules for unlocks, rewards and restoration, applied to SaveData. One-time grants are keyed by
    /// immutable ids (campaign_complete:&lt;level&gt;, area_restored:&lt;area&gt;, room_complete:&lt;room&gt;,
    /// the house milestone), so repeating a commit never grants twice. Time never gates anything.
    /// </summary>
    public sealed class Progression
    {
        public readonly Campaign Campaign;
        public readonly SaveData Save;

        public Progression(Campaign campaign, SaveData save)
        {
            Campaign = campaign;
            Save = save ?? new SaveData();
        }

        public bool IsCampaign(LevelDef l) { return l != null && Campaign.IndexOf(l) >= 0; }
        public bool IsCompleted(LevelDef l) { return l != null && Save.completed.Contains(l.id); }

        /// <summary>The first level, any completed level, and the level after a completed one.</summary>
        public bool IsUnlocked(LevelDef l)
        {
            int i = Campaign.IndexOf(l);
            if (i < 0) return false;
            if (i == 0 || Save.completed.Contains(l.id)) return true;
            return Save.completed.Contains(Campaign.Levels[i - 1].id);
        }

        /// <summary>The next level to play: the first unfinished one in campaign order, or null when all are done.</summary>
        public LevelDef NextUnfinished()
        {
            foreach (var l in Campaign.Levels) if (!Save.completed.Contains(l.id)) return l;
            return null;
        }

        public bool IsRoomUnlocked(RoomDef r)
        {
            if (r == null || !r.available) return false;
            var levels = Campaign.LevelsInRoom(r.id);
            return levels.Count > 0 && IsUnlocked(levels[0]);
        }

        public int CompletedInRoom(RoomDef r)
        {
            int n = 0;
            foreach (var l in Campaign.LevelsInRoom(r.id)) if (Save.completed.Contains(l.id)) n++;
            return n;
        }

        public bool IsAreaRestored(AreaDef a)
        {
            if (a == null || a.sections.Length == 0) return false;
            foreach (var id in a.sections) if (!Save.completed.Contains(id)) return false;
            return true;
        }

        public int RestoredAreas(RoomDef r)
        {
            int n = 0;
            foreach (var a in r.areas) if (IsAreaRestored(a)) n++;
            return n;
        }

        public bool IsRoomComplete(RoomDef r)
        {
            if (r == null || !r.available || r.areas.Length == 0) return false;
            foreach (var a in r.areas) if (!IsAreaRestored(a)) return false;
            return true;
        }

        /// <summary>Whole-house restoration: restored areas over all areas the house plans (locked rooms included).</summary>
        public void HouseProgress(out int restored, out int planned)
        {
            restored = planned = 0;
            foreach (var r in Campaign.House.rooms)
            {
                planned += r.available ? r.areas.Length : r.plannedAreas;
                if (r.available) restored += RestoredAreas(r);
            }
        }

        public bool IsHouseComplete()
        {
            foreach (var r in Campaign.House.rooms) if (!IsRoomComplete(r)) return false;
            return Campaign.House.rooms.Length > 0;
        }

        /// <summary>Commits a finished attempt atomically into SaveData. Safe to call more than once.</summary>
        public CompletionResult Commit(LevelAttempt attempt, bool mastery)
        {
            var level = attempt.Level;
            var r = new CompletionResult { level = level, campaign = IsCampaign(level), time = attempt.Clock.Elapsed };
            r.next = r.campaign ? Campaign.Next(level) : null;
            if (Save.resume != null && Save.resume.levelId == level.id) Save.resume = null;
            if (!r.campaign) return r;

            foreach (var d in attempt.PendingDiscoveries) if (Save.discoveries.Add(d)) r.newDiscoveries.Add(d);

            r.firstCompletion = Save.completed.Add(level.id);
            if (Save.rewards.Add(level.RewardId))
            {
                r.coinsEarned = level.coins;
                Save.coins += level.coins;
            }

            var area = Campaign.Area(level.areaId);
            if (area != null && IsAreaRestored(area) && Save.rewards.Add("area_restored:" + area.id)) r.areaRestored = area;
            var room = Campaign.Room(level.roomId);
            if (room != null && IsRoomComplete(room) && Save.rewards.Add("room_complete:" + room.id)) r.roomCompleted = room;
            if (IsHouseComplete() && Save.rewards.Add(Campaign.House.finalMilestoneId)) r.houseCompleted = true;

            // Optional Mastery record: a positive addition only; never affects rewards or unlocks.
            r.recordEligible = mastery && level.recordsEnabled && !attempt.Assisted && r.time > 0f;
            float best;
            bool had = Save.bestTimes.TryGetValue(level.id, out best);
            r.previousBest = had ? best : 0f;
            if (r.recordEligible && (!had || r.time < best - 1e-4f))
            {
                Save.bestTimes[level.id] = r.time;
                r.newBest = true;
            }
            Save.lastLevel = r.next != null ? r.next.id : level.id;
            return r;
        }

        /// <summary>Remembers the last committed state of an unfinished campaign level (lab levels are not saved).</summary>
        public void Remember(LevelAttempt attempt)
        {
            if (attempt == null || !IsCampaign(attempt.Level)) return;
            Save.lastLevel = attempt.Level.id;
            Save.resume = attempt.State == AttemptState.Complete ? null : attempt.Snapshot();
        }

        /// <summary>The saved unfinished attempt, if it still matches the authored level.</summary>
        public LevelAttempt Resume()
        {
            var s = Save.resume;
            if (s == null) return null;
            var level = Campaign.Get(s.levelId);
            if (level == null || !IsUnlocked(level)) { Save.resume = null; return null; }
            var a = LevelAttempt.FromSnapshot(level, s);
            if (a == null) Save.resume = null;
            return a;
        }

        /// <summary>Optional appearance variant for a restored area, bought once with campaign coins.</summary>
        public bool TryBuyVariant(AreaDef area)
        {
            if (area == null || area.variantPrice <= 0 || !IsAreaRestored(area) || Save.ownedVariants.Contains(area.id)) return false;
            if (Save.coins < area.variantPrice) return false;
            Save.coins -= area.variantPrice;
            Save.ownedVariants.Add(area.id);
            Save.variantEquipped[area.id] = true;
            return true;
        }
    }
}
