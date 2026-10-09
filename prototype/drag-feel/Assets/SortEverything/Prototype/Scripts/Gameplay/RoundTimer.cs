using System;

namespace SortEverything.Prototype
{
    public enum TimerState { Idle, Running, Completed, Failed }

    public enum TimerUrgency { Normal, Low, Critical }

    /// <summary>Configurable timer defaults (not hard-coded constants).</summary>
    public sealed class TimerSettings
    {
        public float lowFraction = 0.3f;       // Low urgency at or below this fraction of the duration
        public float criticalSeconds = 5f;     // Critical urgency at or below this many seconds
        public float penaltyFloor = 0.5f;      // a wrong drop never takes the clock below this (it never fails alone)

        public static readonly TimerSettings Default = new TimerSettings();
    }

    /// <summary>
    /// Countdown for one round. Plain C#: the caller feeds it delta time, so it is testable without Play mode.
    /// Idle until Start (first drag). Expiry is two-phase so that a completion in the same frame wins:
    /// Advance marks expiry pending, Complete can still succeed, and ResolveExpiry turns it into a failure once.
    /// No allocations after construction.
    /// </summary>
    public sealed class RoundTimer
    {
        public readonly float Duration;
        readonly TimerSettings settings;

        public TimerState State { get; private set; }
        public float Remaining { get; private set; }
        /// <summary>Running time since Start, excluding paused time.</summary>
        public float Elapsed { get; private set; }
        /// <summary>Seconds removed by wrong-drop penalties.</summary>
        public float PenaltySeconds { get; private set; }
        /// <summary>Elapsed + penalties at completion: what the results screen and best time use.</summary>
        public float CompletionSeconds { get; private set; }
        public bool Paused { get; set; }
        public bool ExpiryPending { get; private set; }

        public RoundTimer(float duration, TimerSettings settings = null)
        {
            Duration = Math.Max(0.1f, duration);
            this.settings = settings ?? TimerSettings.Default;
            Reset();
        }

        public void Reset()
        {
            State = TimerState.Idle;
            Remaining = Duration;
            Elapsed = 0f;
            PenaltySeconds = 0f;
            CompletionSeconds = 0f;
            ExpiryPending = false;
        }

        public bool IsRunning { get { return State == TimerState.Running; } }

        /// <summary>Idle -> Running. Ignored in any other state.</summary>
        public void Start()
        {
            if (State == TimerState.Idle) State = TimerState.Running;
        }

        /// <summary>Counts down while running and not paused. Marks expiry pending when the clock reaches zero.</summary>
        public void Advance(float dt)
        {
            if (State != TimerState.Running || Paused || ExpiryPending || dt <= 0f) return;
            float step = Math.Min(dt, Remaining);
            Remaining -= step;
            Elapsed += step;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                ExpiryPending = true;
            }
        }

        /// <summary>Removes time for a wrong drop. Returns the seconds actually removed (0 if disabled or not running).</summary>
        public float ApplyPenalty(float seconds)
        {
            if (State != TimerState.Running || ExpiryPending || seconds <= 0f) return 0f;
            float floor = Math.Min(Remaining, settings.penaltyFloor);
            float next = Math.Max(Remaining - seconds, floor);
            float removed = Remaining - next;
            Remaining = next;
            PenaltySeconds += removed;
            return removed;
        }

        /// <summary>Stops on success. Wins over an expiry that is still pending in the same frame.</summary>
        public bool Complete()
        {
            if (State != TimerState.Running) return false;
            State = TimerState.Completed;
            ExpiryPending = false;
            CompletionSeconds = Elapsed + PenaltySeconds;
            return true;
        }

        /// <summary>Turns a pending expiry into failure. True exactly once.</summary>
        public bool ResolveExpiry()
        {
            if (State != TimerState.Running || !ExpiryPending) return false;
            ExpiryPending = false;
            State = TimerState.Failed;
            return true;
        }

        public TimerUrgency Urgency
        {
            get
            {
                if (State == TimerState.Idle) return TimerUrgency.Normal;
                if (Remaining <= settings.criticalSeconds) return TimerUrgency.Critical;
                if (Remaining <= Duration * settings.lowFraction) return TimerUrgency.Low;
                return TimerUrgency.Normal;
            }
        }

        /// <summary>Remaining time in tenths, rounded up so "0.0" only shows at expiry.</summary>
        public int DisplayTenths { get { return (int)Math.Ceiling(Remaining * 10f - 1e-3f); } }
    }

    /// <summary>
    /// Pre-built "12.4" strings for every tenth up to the level duration, so the HUD never allocates a string while
    /// the clock runs. Build once per level.
    /// </summary>
    public sealed class TimerText
    {
        readonly string[] tenths;

        public TimerText(float maxSeconds)
        {
            int n = (int)Math.Ceiling(maxSeconds * 10f) + 1;
            tenths = new string[n];
            for (int i = 0; i < n; i++)
                tenths[i] = (i / 10).ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + (i % 10);
        }

        public string For(int displayTenths)
        {
            if (displayTenths < 0) displayTenths = 0;
            if (displayTenths >= tenths.Length) displayTenths = tenths.Length - 1;
            return tenths[displayTenths];
        }
    }
}
