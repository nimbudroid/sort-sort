using System.Collections.Generic;

namespace SortEverything.Prototype
{
    public enum AttemptState { Ready, Playing, Complete }

    /// <summary>
    /// One attempt at one level, across its boards. Owns the board model and the active clock; records mistakes,
    /// rearrangements, assistance, the Mastery clean streak and pending discoveries. No scene dependency.
    /// There is no failure state: the attempt ends only when every board is solved.
    /// </summary>
    public sealed class LevelAttempt
    {
        public readonly LevelDef Level;
        public readonly ActiveClock Clock = new ActiveClock();
        public string AttemptId { get; private set; }

        public AttemptState State { get; private set; }
        public int BoardIndex { get; private set; }
        public BoardModel Board { get; private set; }

        public int WrongCategory { get; private set; }
        public int WrongFull { get; private set; }
        public int Mistakes { get { return WrongCategory + WrongFull; } }
        public int Rearrangements { get; private set; }
        public bool Assisted { get; set; }
        /// <summary>Mastery clean streak on ordinary boards: consecutive correct first assignments.</summary>
        public int Streak { get; private set; }
        public bool StreakMilestone { get; private set; } // true when the latest drop reached 3 or 5

        readonly HashSet<string> credited = new HashSet<string>();      // board:instance ids with first-assignment credit
        public readonly HashSet<string> PendingDiscoveries = new HashSet<string>();

        public LevelAttempt(LevelDef level, string attemptId = null)
        {
            Level = level;
            AttemptId = attemptId ?? System.Guid.NewGuid().ToString("N");
            BoardIndex = 0;
            Board = new BoardModel(level.boards[0]);
            State = AttemptState.Ready;
        }

        public bool IsLastBoard { get { return BoardIndex >= Level.boards.Length - 1; } }

        /// <summary>The first pickup starts active time.</summary>
        public void BeginPickup()
        {
            if (State != AttemptState.Ready) return;
            State = AttemptState.Playing;
            Clock.Start();
        }

        public void Tick(float dt)
        {
            if (State == AttemptState.Playing) Clock.Advance(dt);
        }

        /// <summary>An object was released over a target. Accepted placements and transfers commit here.</summary>
        public PlaceResult Drop(int obj, int target)
        {
            if (State == AttemptState.Complete) return PlaceResult.Ignored;
            if (State == AttemptState.Ready) BeginPickup();
            bool wasAssigned = Board.Location(obj) >= 0;
            var r = Board.Place(obj, target);
            StreakMilestone = false;
            switch (r)
            {
                case PlaceResult.WrongTarget:
                    WrongCategory++;
                    ResetStreak();
                    break;
                case PlaceResult.Full:
                    WrongFull++;
                    ResetStreak();
                    break;
                case PlaceResult.Accepted:
                    if (wasAssigned) Rearrangements++;
                    PendingDiscoveries.Add(Level.boards[BoardIndex].objects[obj].asset);
                    string key = BoardIndex + ":" + Level.boards[BoardIndex].objects[obj].id;
                    if (credited.Add(key) && !Board.Def.IsCapacity)
                    {
                        Streak++;
                        StreakMilestone = Streak == 3 || Streak == 5;
                    }
                    break;
            }
            return r;
        }

        /// <summary>Planning boards: an assigned object returned to the board (legal, never a mistake).</summary>
        public bool ReturnToBoard(int obj)
        {
            if (State == AttemptState.Complete || !Board.Unassign(obj)) return false;
            Rearrangements++;
            return true;
        }

        void ResetStreak() { Streak = 0; }

        public bool BoardSolved { get { return Board.Solved; } }

        /// <summary>Moves to the next board once the current one is solved. False on the last board.</summary>
        public bool AdvanceBoard()
        {
            if (!Board.Solved || IsLastBoard) return false;
            BoardIndex++;
            Board = new BoardModel(Level.boards[BoardIndex]);
            return true;
        }

        /// <summary>Completes the attempt when the last board is solved. True exactly once.</summary>
        public bool TryComplete()
        {
            if (State == AttemptState.Complete || !Board.Solved || !IsLastBoard) return false;
            State = AttemptState.Complete;
            Clock.Stop();
            return true;
        }

        // ---- save / resume ------------------------------------------------------------------

        public AttemptSnapshot Snapshot()
        {
            return new AttemptSnapshot
            {
                levelId = Level.id,
                contentVersion = Level.contentVersion,
                attemptId = AttemptId,
                board = BoardIndex,
                locations = Board.Snapshot(),
                activeSeconds = Clock.Elapsed,
                wrongCategory = WrongCategory,
                wrongFull = WrongFull,
                rearrangements = Rearrangements,
                assisted = Assisted,
                started = State != AttemptState.Ready,
                discoveries = new List<string>(PendingDiscoveries).ToArray(),
                credited = new List<string>(credited).ToArray(),
            };
        }

        /// <summary>Rebuilds an attempt from a snapshot, re-validating every placement. Null if it no longer fits.</summary>
        public static LevelAttempt FromSnapshot(LevelDef level, AttemptSnapshot s)
        {
            if (level == null || s == null || s.levelId != level.id || s.contentVersion != level.contentVersion) return null;
            if (s.board < 0 || s.board >= level.boards.Length) return null;
            var a = new LevelAttempt(level, s.attemptId);
            a.BoardIndex = s.board;
            a.Board = new BoardModel(level.boards[s.board]);
            if (!a.Board.Restore(s.locations)) return null;
            a.Clock.Reset(s.activeSeconds);
            a.WrongCategory = s.wrongCategory;
            a.WrongFull = s.wrongFull;
            a.Rearrangements = s.rearrangements;
            a.Assisted = s.assisted;
            if (s.discoveries != null) foreach (var d in s.discoveries) a.PendingDiscoveries.Add(d);
            if (s.credited != null) foreach (var c in s.credited) a.credited.Add(c);
            if (s.started) { a.State = AttemptState.Playing; a.Clock.Start(); }
            return a;
        }
    }

    /// <summary>The last committed state of an unfinished attempt (no pointer or animation state).</summary>
    public sealed class AttemptSnapshot
    {
        public string levelId;
        public int contentVersion;
        public string attemptId;
        public int board;
        public int[] locations;
        public float activeSeconds;
        public int wrongCategory, wrongFull, rearrangements;
        public bool assisted, started;
        public string[] discoveries;
        public string[] credited;
    }
}
