using System;

namespace SortEverything.Prototype
{
    public enum SessionState { Ready, Playing, Complete, TimeUp }

    public enum DropResult { Ignored, Correct, Wrong }

    /// <summary>Configurable combo defaults (the combo is cosmetic and off unless the level enables it).</summary>
    public sealed class ComboSettings
    {
        public float window = 2.5f;     // max running seconds between correct sorts to keep a streak
        public int x2Streak = 3;        // streak length for x2
        public int x3Streak = 5;        // streak length for x3

        public static readonly ComboSettings Default = new ComboSettings();
    }

    /// <summary>
    /// The rules of one attempt at one level, with no scene dependency. Objects are addressed by their index in the
    /// level's object list and bins by their index in the level's bin list. Every acceptance decision goes through
    /// here (and so through RuleEvaluator); bins only play the feedback.
    /// </summary>
    public sealed class RoundSession
    {
        public readonly LevelDef Level;
        public readonly ObjectDef[] Objects;
        public readonly RoundTimer Timer;
        public readonly int[] BinCapacity;
        public readonly int[] BinCount;

        readonly int[] correctBin;
        readonly bool[] sorted;
        readonly ComboSettings combo;
        float lastCorrectAt = -100f;

        public SessionState State { get; private set; }
        public int SortedCount { get; private set; }
        public int WrongDrops { get; private set; }
        /// <summary>Seconds removed by the most recent wrong drop (0 when the penalty is disabled or floored).</summary>
        public float LastPenalty { get; private set; }

        /// <summary>Consecutive correct sorts inside the combo window.</summary>
        public int Streak { get; private set; }
        /// <summary>Cosmetic multiplier: 1, 2 or 3 (always 1 when the level's combo flag is off).</summary>
        public int Multiplier { get; private set; }
        /// <summary>True when the latest correct sort raised the multiplier (show the pop-up).</summary>
        public bool MultiplierRaised { get; private set; }

        public RoundSession(LevelDef level, TimerSettings timerSettings = null, ComboSettings comboSettings = null)
        {
            if (level == null) throw new ArgumentNullException("level");
            Level = level;
            combo = comboSettings ?? ComboSettings.Default;
            Timer = new RoundTimer(level.timerSeconds, timerSettings);
            int n = level.objectIds.Length;
            Objects = new ObjectDef[n];
            correctBin = new int[n];
            sorted = new bool[n];
            BinCapacity = new int[level.bins.Length];
            BinCount = new int[level.bins.Length];
            for (int i = 0; i < n; i++)
            {
                Objects[i] = ObjectLibrary.Get(level.objectIds[i]);
                correctBin[i] = LevelValidator.MatchingBin(level, Objects[i]);
                if (correctBin[i] >= 0) BinCapacity[correctBin[i]]++;
            }
            Restart();
        }

        /// <summary>Back to the state before the first drag: timer idle, nothing sorted.</summary>
        public void Restart()
        {
            Timer.Reset();
            State = SessionState.Ready;
            SortedCount = 0;
            WrongDrops = 0;
            LastPenalty = 0f;
            Streak = 0;
            Multiplier = 1;
            MultiplierRaised = false;
            lastCorrectAt = -100f;
            for (int i = 0; i < sorted.Length; i++) sorted[i] = false;
            for (int b = 0; b < BinCount.Length; b++) BinCount[b] = 0;
        }

        public bool Paused
        {
            get { return Timer.Paused; }
            set { Timer.Paused = value; }
        }

        public bool IsFinished { get { return State == SessionState.Complete || State == SessionState.TimeUp; } }

        /// <summary>The first drag of a sortable object starts the clock.</summary>
        public void BeginDrag()
        {
            if (State != SessionState.Ready) return;
            State = SessionState.Playing;
            Timer.Start();
        }

        public bool IsSorted(int objectIndex) { return sorted[objectIndex]; }

        /// <summary>Index of the one bin that accepts this object (-1 none, -2 several; the validator forbids both).</summary>
        public int CorrectBin(int objectIndex) { return correctBin[objectIndex]; }

        public bool Accepts(int binIndex, int objectIndex)
        {
            return RuleEvaluator.Matches(Level.bins[binIndex].rule, Objects[objectIndex]);
        }

        /// <summary>
        /// An object entered a bin. Correct: counted. Wrong: rejected with the level's time penalty; it never ends the
        /// round by itself. Ignored once the round is finished or for an object already sorted.
        /// </summary>
        public DropResult Drop(int objectIndex, int binIndex)
        {
            if (IsFinished || objectIndex < 0 || objectIndex >= sorted.Length || sorted[objectIndex]) return DropResult.Ignored;
            if (binIndex < 0 || binIndex >= BinCount.Length) return DropResult.Ignored;
            if (State == SessionState.Ready) BeginDrag(); // something reached a bin before any drag (a roll-in)
            MultiplierRaised = false;

            if (!Accepts(binIndex, objectIndex))
            {
                WrongDrops++;
                LastPenalty = Timer.ApplyPenalty(Level.wrongDropPenalty);
                Streak = 0;
                Multiplier = 1;
                return DropResult.Wrong;
            }

            sorted[objectIndex] = true;
            BinCount[binIndex]++;
            SortedCount++;

            float now = Timer.Elapsed;
            Streak = now - lastCorrectAt <= combo.window ? Streak + 1 : 1;
            lastCorrectAt = now;
            if (Level.comboEnabled)
            {
                int m = Streak >= combo.x3Streak ? 3 : Streak >= combo.x2Streak ? 2 : 1;
                MultiplierRaised = m > Multiplier;
                Multiplier = m;
            }

            if (SortedCount == sorted.Length && Timer.Complete()) State = SessionState.Complete;
            return DropResult.Correct;
        }

        /// <summary>Feed frame time while playing.</summary>
        public void Tick(float dt)
        {
            if (State == SessionState.Playing) Timer.Advance(dt);
        }

        /// <summary>Call once per frame after drops were processed. True on the frame the round is lost to time.</summary>
        public bool EndFrame()
        {
            if (State != SessionState.Playing || !Timer.ResolveExpiry()) return false;
            State = SessionState.TimeUp;
            return true;
        }

        public float CompletionSeconds { get { return Timer.CompletionSeconds; } }
    }

    /// <summary>The ordered list of levels; NEXT walks it in order and wraps after the last level.</summary>
    public sealed class Playlist
    {
        readonly System.Collections.Generic.IList<LevelDef> levels;

        public Playlist(System.Collections.Generic.IList<LevelDef> levels)
        {
            if (levels == null || levels.Count == 0) throw new ArgumentException("playlist needs at least one level");
            this.levels = levels;
        }

        public int Index { get; private set; }
        public int Count { get { return levels.Count; } }
        public LevelDef Current { get { return levels[Index]; } }
        /// <summary>1-based position shown to the player.</summary>
        public int Number { get { return Index + 1; } }

        public LevelDef Advance()
        {
            Index = (Index + 1) % levels.Count;
            return Current;
        }

        public void Select(int index)
        {
            Index = Math.Max(0, Math.Min(levels.Count - 1, index));
        }

        public LevelDef Peek(int offset) { return levels[((Index + offset) % levels.Count + levels.Count) % levels.Count]; }
    }

    /// <summary>Where best times live; PlayerPrefs in the game, memory in tests.</summary>
    public interface IBestTimeStore
    {
        bool TryGet(string levelId, out float seconds);
        void Set(string levelId, float seconds);
    }

    public sealed class MemoryBestTimeStore : IBestTimeStore
    {
        readonly System.Collections.Generic.Dictionary<string, float> times = new System.Collections.Generic.Dictionary<string, float>();
        public bool TryGet(string levelId, out float seconds) { return times.TryGetValue(levelId, out seconds); }
        public void Set(string levelId, float seconds) { times[levelId] = seconds; }
    }

    public static class BestTimeBook
    {
        /// <summary>
        /// Records a completion time. Returns true when it is a new best (stored); `best` is the best after the call,
        /// `hadPrevious` whether a best existed before.
        /// </summary>
        public static bool Submit(IBestTimeStore store, string levelId, float seconds, out float best, out bool hadPrevious)
        {
            float previous;
            hadPrevious = store.TryGet(levelId, out previous);
            if (!hadPrevious || seconds < previous)
            {
                store.Set(levelId, seconds);
                best = seconds;
                return true;
            }
            best = previous;
            return false;
        }
    }
}
