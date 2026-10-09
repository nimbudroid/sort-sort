using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Builds the portrait diorama (pile on a table, bins along the bottom thumb zone) for the current playlist
    /// level and connects the scene to the plain-C# RoundSession: bins ask the session whether a drop is correct,
    /// the session owns the timer, and this class plays the existing feedback, the completion celebration and the
    /// TIME'S UP state.
    /// </summary>
    public class RoundDirector : MonoBehaviour
    {
        public readonly List<SortObject> Objects = new List<SortObject>();
        public readonly List<Bin> Bins = new List<Bin>();

        public int Round { get; private set; }
        public bool InputLive { get; private set; }
        public bool RoundComplete { get; private set; }
        public float CompleteTime { get; private set; }
        public float StampTime { get; private set; }
        public bool ShowNext { get; private set; }
        public int Combo { get; private set; }
        public float ComboTime { get; private set; }
        public Vector2 ComboWorldPos { get; private set; }

        // Levels, session and timer.
        public Playlist Playlist { get; private set; }
        public RoundSession Session { get; private set; }
        public LevelDef Level { get { return Session != null ? Session.Level : null; } }
        public TimerText TimerText { get; private set; }
        public float TimerStartedAt { get; private set; }
        public float PenaltyAt { get; private set; }
        public string PenaltyText { get; private set; }
        public bool TimeUp { get; private set; }
        public float TimeUpAt { get; private set; }
        public bool ShowRetry { get; private set; }
        // Results (formatted once at completion).
        public string ResultTimeText { get; private set; }
        public string ResultBestText { get; private set; }
        public bool ResultNewBest { get; private set; }

        IBestTimeStore bestTimes;
        bool appFocused = true;

        // Layout (world units), recomputed every round so rotation / resolution changes are picked up.
        public float TableTop { get; private set; }
        public float ContainerZoneTop { get { return TableTop - 0.05f; } }
        public float BinsTop { get; private set; }
        float left, right, top, floorY, tableLeft, tableRight;

        Transform worldRoot;
        PhysicsMaterial2D objectMaterial, staticMaterial;
        int pileOrder;
        float roundStart;
        int[] melody;
        int noteIndex;
        int audioStreak;
        float lastCorrect = -10f;

        /// <summary>Content the round was built with (telemetry). Levels always use library objects.</summary>
        public ContentMode RoundMode { get; private set; }

        void Awake()
        {
            objectMaterial = new PhysicsMaterial2D("Toy") { friction = 0.7f, bounciness = 0.08f };
            staticMaterial = new PhysicsMaterial2D("Static") { friction = 0.6f, bounciness = 0f };
            RoundMode = ContentMode.RealObjects;
            Playlist = new Playlist(LevelLibrary.Playlist);
            bestTimes = new PlayerPrefsBestTimeStore();
        }

        public int NextPileOrder() { return ++pileOrder; }

        /// <summary>(Re)builds the current playlist level from scratch: timer idle, every object back on the pile.</summary>
        public void StartRound()
        {
            StopAllCoroutines();
            if (Proto.Drag != null) Proto.Drag.ReleaseAll();
            Time.timeScale = 1f;
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            Objects.Clear();
            Bins.Clear();
            worldRoot = new GameObject("World").transform;

            Round++;
            RoundComplete = false;
            ShowNext = false;
            InputLive = false;
            TimeUp = false;
            ShowRetry = false;
            Combo = 0;
            audioStreak = 0;
            noteIndex = 0;
            pileOrder = 100;
            ResultTimeText = ResultBestText = null;
            ResultNewBest = false;

            var level = Playlist.Current;
            Session = new RoundSession(level);
            TimerText = new TimerText(level.timerSeconds);
            TimerStartedAt = -10f;
            PenaltyAt = -10f;

            ComputeLayout();
            BuildStatics();
            BuildBinsAndObjects(level);
            StartCoroutine(Intro());
            StartCoroutine(PrewarmArt(Playlist.Peek(1)));
        }

        /// <summary>Plays the given playlist entry (observer panel).</summary>
        public void SelectLevel(int index)
        {
            Playlist.Select(index);
            StartRound();
        }

        void Update()
        {
            if (Session == null) return;
            Session.Paused = !appFocused || (Proto.Hud != null && Proto.Hud.PanelOpen);
            // The clock starts on the first drag of a sortable object; looking at the level costs nothing.
            if (Session.State == SessionState.Ready && InputLive && Proto.Drag != null && Proto.Drag.IsHolding)
            {
                Session.BeginDrag();
                TimerStartedAt = Time.unscaledTime;
            }
            // Unscaled so completion slow-motion never changes the clock; clamped so a hitch can't eat seconds.
            Session.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        }

        void LateUpdate()
        {
            // After this frame's drops: a completion in the same frame has already stopped the clock and wins.
            if (Session != null && Session.EndFrame()) StartCoroutine(OnTimeUp());
        }

        void OnApplicationFocus(bool focus) { appFocused = focus; }
        void OnApplicationPause(bool paused) { appFocused = !paused; }

        void ComputeLayout()
        {
            var cam = Proto.Cam;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float wpp = Units.WorldPerPx;
            Rect safe = Screen.safeArea;
            left = -halfW;
            right = halfW;
            top = halfH - (Screen.height - safe.yMax) * wpp;
            floorY = -halfH + safe.yMin * wpp + Units.DpToWorld(16f);

            float objMax = Units.DpToWorld(56f) * 1.3f;
            float binH = objMax * 2.15f;
            BinsTop = floorY + binH;
            TableTop = BinsTop + Mathf.Max(Units.DpToWorld(160f), objMax * 2f);
            float margin = Units.DpToWorld(16f);
            tableLeft = left + margin;
            tableRight = right - margin;
        }

        void BuildStatics()
        {
            var rr = ProcSprites.RoundedRect();

            // Screen-edge walls and ceiling: nothing is ever lost (GDD ch. 02 §2.5 rule 1).
            StaticBox("WallL", new Vector2(left - 0.5f, 0f), new Vector2(1f, 60f));
            StaticBox("WallR", new Vector2(right + 0.5f, 0f), new Vector2(1f, 60f));
            StaticBox("Ceiling", new Vector2(0f, top + 0.5f), new Vector2(60f, 1f));
            StaticBox("Floor", new Vector2(0f, floorY - 0.5f), new Vector2(60f, 1f));

            // Visual style: soft sky gradient behind everything.
            float halfH = Proto.Cam.orthographicSize;
            ToyStyle.BuildBackground(worldRoot, left, right, -halfH, halfH);

            // Floor band (the spill area in front of the containers).
            Visual("FloorBand", ToyStyle.Panel(ToyStyle.FloorColor, false), new Vector2(0f, floorY - 5f + 0.02f),
                new Vector2((right - left) + 1f, 10f), Color.white, -50);

            // The table the pile sits on, with small lips so objects stay put.
            float thick = Units.DpToWorld(14f);
            float lip = Units.DpToWorld(20f);
            float width = tableRight - tableLeft;
            float cx = (tableLeft + tableRight) / 2f;
            StaticBox("Table", new Vector2(cx, TableTop - thick / 2f), new Vector2(width, thick));
            StaticBox("LipL", new Vector2(tableLeft + thick / 2f, TableTop + lip / 2f), new Vector2(thick, lip));
            StaticBox("LipR", new Vector2(tableRight - thick / 2f, TableTop + lip / 2f), new Vector2(thick, lip));
            var wood = ToyStyle.Strip(ToyStyle.Wood);
            Visual("TableTop", wood, new Vector2(cx, TableTop - thick / 2f), new Vector2(width, thick), Color.white, -10);
            Visual("TableLipL", wood, new Vector2(tableLeft + thick / 2f, TableTop + lip / 2f - thick * 0.25f),
                new Vector2(thick, lip + thick * 0.5f), Color.white, -9);
            Visual("TableLipR", wood, new Vector2(tableRight - thick / 2f, TableTop + lip / 2f - thick * 0.25f),
                new Vector2(thick, lip + thick * 0.5f), Color.white, -9);
            Visual("TableShadow", rr, new Vector2(cx, TableTop - thick - thick * 0.35f), new Vector2(width * 0.96f, thick * 0.7f),
                new Color(ToyStyle.Ink.r, ToyStyle.Ink.g, ToyStyle.Ink.b, 0.14f), -11);
        }

        void BuildBinsAndObjects(LevelDef level)
        {
            // Bins: count, rules, labels and colours come only from the level definition.
            int binCount = level.bins.Length;
            float gap = Units.DpToWorld(binCount > 3 ? 8f : 12f);
            float margin = Units.DpToWorld(binCount > 3 ? 10f : 16f);
            float binW = ((right - left) - 2f * margin - (binCount - 1) * gap) / binCount;
            float binH = BinsTop - floorY;
            for (int i = 0; i < binCount; i++)
            {
                float x = left + margin + binW / 2f + i * (binW + gap);
                var bin = Bin.Create(i, level.bins[i], Session.BinCapacity[i], new Vector2(x, floorY), binW, binH,
                    staticMaterial, worldRoot);
                Bins.Add(bin);
            }

            // Objects: the level's explicit list, each the shared library definition and its cached sprite.
            float baseSize = Units.DpToWorld(56f);
            for (int i = 0; i < Session.Objects.Length; i++)
            {
                var def = Session.Objects[i];
                int correct = Session.CorrectBin(i);
                Color binColor = correct >= 0 ? Bins[correct].color : Color.white;
                var o = SortObject.CreateFromDef(i, def, binColor, baseSize * def.SizeScale,
                    new Vector2(-1000f, -1000f), objectMaterial, NextPileOrder(), worldRoot);
                if (o == null)
                {
                    Debug.LogError("No art for " + def.id + " in level " + level.id);
                    continue;
                }
                ToyStyle.AddObjectShadow(o); // visual style: stylised drop shadow
                o.gameObject.SetActive(false);
                Objects.Add(o);
            }

            // Pour order is shuffled so bins are not filled in authoring order.
            for (int i = Objects.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = Objects[i];
                Objects[i] = Objects[j];
                Objects[j] = tmp;
            }

            melody = MakeMelody(Mathf.Max(2, Objects.Count));
        }

        IEnumerator Intro()
        {
            for (int i = 0; i < Bins.Count; i++) Bins[i].PopIn(0.1f + 0.06f * i);
            yield return new WaitForSeconds(0.5f);
            InputLive = true;
            roundStart = Time.time;

            // Pour the objects in from the top like tipping out a bag.
            float spread = (tableRight - tableLeft) / 2f - Units.DpToWorld(56f);
            float cx = (tableLeft + tableRight) / 2f;
            for (int i = 0; i < Objects.Count; i++)
            {
                var o = Objects[i];
                float y = Mathf.Min(top - o.HalfExtent - 0.1f, TableTop + 4f + (i % 3) * 0.9f);
                o.transform.position = new Vector2(cx + Random.Range(-spread, spread), y);
                o.gameObject.SetActive(true);
                o.rb.SetVelocity(new Vector2(Random.Range(-1.5f, 1.5f), -2f));
                o.rb.angularVelocity = Random.Range(-200f, 200f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// Rasterise the rest of the current pool's art in the background (one object per frame), so later rounds
        /// don't pay for it during their build.
        /// </summary>
        /// <summary>
        /// Rasterise the next level's art in the background (one object per frame, never during a drag), so NEXT
        /// doesn't pay for it during its build.
        /// </summary>
        IEnumerator PrewarmArt(LevelDef next)
        {
            yield return new WaitForSeconds(1.5f);
            if (next == null) yield break;
            for (int i = 0; i < next.objectIds.Length; i++)
            {
                var d = ObjectLibrary.Get(next.objectIds[i]);
                if (d == null || ObjectArt.IsCached(d.id)) continue;
                while (Proto.Drag != null && Proto.Drag.IsHolding) yield return null; // never hitch a drag
                Sprite sprite;
                Vector2[] hull;
                ObjectArt.TryGet(d, out sprite, out hull);
                yield return null;
            }
        }

        public Vector2 ClampToPlayfield(Vector2 p, float halfExtent)
        {
            return new Vector2(
                Mathf.Clamp(p.x, left + halfExtent, right - halfExtent),
                Mathf.Clamp(p.y, floorY + halfExtent, top - halfExtent));
        }

        public Vector2 RandomPileLandingPoint()
        {
            float spread = (tableRight - tableLeft) / 2f - Units.DpToWorld(56f);
            float cx = (tableLeft + tableRight) / 2f;
            return new Vector2(cx + Random.Range(-spread, spread), TableTop + Units.DpToWorld(56f) * 1.5f);
        }

        // ---- feedback hooks from bins --------------------------------------------------------

        /// <summary>Asked by a bin when an object's centre enters its mouth. The session decides.</summary>
        public DropResult Judge(Bin bin, SortObject o)
        {
            if (Session == null || TimeUp) return DropResult.Ignored;
            return Session.Drop(o.id, bin.index);
        }

        public void OnCorrect(Bin bin, SortObject o)
        {
            float now = Time.time;
            audioStreak = now - lastCorrect <= 1.2f ? audioStreak + 1 : 1;
            lastCorrect = now;

            // Cosmetic combo pop-up only when the level enables it.
            if (Session.MultiplierRaised)
            {
                Combo = Session.Multiplier;
                ComboTime = Time.unscaledTime;
                ComboWorldPos = bin.MouthCenter;
            }

            int degree = melody[Mathf.Min(noteIndex, melody.Length - 1)];
            noteIndex++;
            if (Proto.Config.juice)
            {
                Proto.Audio.Note(degree);
                if (audioStreak >= 3) Proto.Audio.Note(degree + 2, 0.35f); // harmony a third above
            }
            else
            {
                Proto.Audio.Pop(1f);
            }

            if (Session.State == SessionState.Complete) StartCoroutine(Complete());
        }

        public void OnWrong(Bin bin, SortObject o)
        {
            audioStreak = 0;
            Combo = 0;
            if (Session.LastPenalty > 0f)
            {
                PenaltyAt = Time.unscaledTime;
                PenaltyText = "-" + Session.LastPenalty.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        IEnumerator Complete()
        {
            RoundComplete = true;
            InputLive = false;
            CompleteTime = Time.unscaledTime;
            Proto.Telemetry.OnRoundComplete(Round, Time.time - roundStart);

            // Results, formatted once.
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            float seconds = Session.CompletionSeconds;
            float best;
            bool hadPrevious;
            ResultNewBest = BestTimeBook.Submit(bestTimes, Session.Level.id, seconds, out best, out hadPrevious);
            ResultTimeText = seconds.ToString("0.00", inv) + "s";
            if (hadPrevious) ResultBestText = ResultNewBest ? "NEW BEST!" : "BEST " + best.ToString("0.00", inv) + "s";

            if (Proto.Config.juice)
            {
                Time.timeScale = 0.35f;
                yield return new WaitForSecondsRealtime(0.25f);
                Time.timeScale = 1f;
            }

            // Cadence: bins hop left to right on an ascending phrase.
            int[] cadence = { 5, 7, 9, 12 };
            for (int i = 0; i < Bins.Count; i++)
            {
                Bins[i].Hop();
                Proto.Audio.Note(cadence[i % cadence.Length], 0.7f);
                yield return new WaitForSeconds(0.08f);
            }

            yield return new WaitForSeconds(0.3f);
            StampTime = Time.unscaledTime;
            Proto.Audio.Stamp();
            Proto.Juice.Shake(4f, 0.12f);
            Haptics.Play(Haptics.Kind.Heavy);
            yield return new WaitForSeconds(0.9f);
            ShowNext = true;
        }

        /// <summary>Time ran out: stop input, put any held object back on the pile, show TIME'S UP with retry / skip.</summary>
        IEnumerator OnTimeUp()
        {
            TimeUp = true;
            InputLive = false;
            TimeUpAt = Time.unscaledTime;
            if (Proto.Drag != null)
            {
                var held = new List<SortObject>();
                for (int i = 0; i < Objects.Count; i++) if (Objects[i].state == ObjState.Held) held.Add(Objects[i]);
                Proto.Drag.ReleaseAll();
                foreach (var o in held)
                {
                    if (o.state == ObjState.Guided) continue; // already tweening into a bin; the session ignores it
                    o.transform.position = RandomPileLandingPoint();
                    o.rb.SetVelocity(Vector2.zero);
                    o.state = ObjState.Pile;
                    o.SetSimulated(true);
                }
            }
            Proto.Audio.Bwomp();
            Haptics.Play(Haptics.Kind.Medium);
            yield return new WaitForSecondsRealtime(0.6f);
            ShowRetry = true;
        }

        public void Next()
        {
            if (!ShowNext) return;
            Proto.Telemetry.OnNextTapped(Time.unscaledTime - CompleteTime);
            Playlist.Advance();
            StartRound();
        }

        /// <summary>TIME'S UP: play the same level again from idle with every object restored.</summary>
        public void Retry()
        {
            if (!ShowRetry) return;
            StartRound();
        }

        /// <summary>TIME'S UP: continue to the next playlist entry.</summary>
        public void Skip()
        {
            if (!ShowRetry) return;
            Playlist.Advance();
            StartRound();
        }

        // ---- helpers -------------------------------------------------------------------------

        static int[] MakeMelody(int length)
        {
            var notes = new int[length];
            int d = Random.Range(2, 5);
            for (int i = 0; i < length; i++)
            {
                notes[i] = d;
                int step = Random.Range(1, 3) * (Random.value < 0.5f ? -1 : 1);
                d = Mathf.Clamp(d + step, 0, AudioKit.ScaleLength - 1);
            }
            notes[length - 1] = 5; // resolve on the tonic
            return notes;
        }

        void StaticBox(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.sharedMaterial = staticMaterial;
        }

        void Visual(string name, Sprite sprite, Vector2 center, Vector2 size, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = c;
            sr.sortingOrder = order;
        }
    }
}
