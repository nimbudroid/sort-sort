using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Presents one level: builds the portrait diorama (pile on a table, targets along the bottom thumb zone) for the
    /// current board of a LevelAttempt and connects the scene to it. The board model decides every placement; this
    /// class plays the existing feel and feedback, keeps placed objects in deterministic slots, returns refused or
    /// interrupted objects to their last committed spot, advances multi-board levels and runs the completion
    /// celebration. There is no countdown and no failure state.
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

        // The level being played.
        public LevelDef Level { get; private set; }
        public LevelAttempt Attempt { get; private set; }
        public TimerText TimerText { get; private set; }
        public float FirstPickupAt { get; private set; }
        /// <summary>Raised once when the last board is solved (the flow commits the result).</summary>
        public System.Action<LevelAttempt> LevelCompleted;
        /// <summary>Raised after every committed change (the flow saves a snapshot).</summary>
        public System.Action<LevelAttempt> BoardChanged;

        // Rejection reason bubble.
        public string ReasonText { get; private set; }
        public float ReasonAt { get; private set; }
        public Vector2 ReasonWorldPos { get; private set; }

        bool appFocused = true;
        public bool ExternallyPaused { get; set; }

        // Layout (world units), recomputed every build so rotation / resolution changes are picked up.
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

        // Where each held object came from: its committed slot (target index) or its pile position.
        struct Origin { public int target; public Vector2 pos; }
        readonly Dictionary<SortObject, Origin> origins = new Dictionary<SortObject, Origin>();
        readonly Dictionary<SortObject, Coroutine> tweens = new Dictionary<SortObject, Coroutine>();

        /// <summary>Content the round was built with (telemetry).</summary>
        public ContentMode RoundMode { get; private set; }

        void Awake()
        {
            objectMaterial = new PhysicsMaterial2D("Toy") { friction = 0.7f, bounciness = 0.08f };
            staticMaterial = new PhysicsMaterial2D("Static") { friction = 0.6f, bounciness = 0f };
            RoundMode = ContentMode.RealObjects;
        }

        void Start()
        {
            if (Proto.Drag != null)
            {
                Proto.Drag.Picked += OnPicked;
                Proto.Drag.Released += OnReleased;
            }
        }

        public int NextPileOrder() { return ++pileOrder; }

        /// <summary>Plays a level from its start, or resumes it from a validated snapshot.</summary>
        public void Play(LevelDef level, AttemptSnapshot resume = null)
        {
            Level = level;
            Attempt = resume != null ? LevelAttempt.FromSnapshot(level, resume) : null;
            if (Attempt == null) Attempt = new LevelAttempt(level);
            if (TimerText == null) TimerText = new TimerText(3600f);
            FirstPickupAt = Attempt.State == AttemptState.Ready ? -10f : Time.unscaledTime;
            Round++;
            BuildBoard(true);
        }

        /// <summary>Restart: same level, fresh attempt.</summary>
        public void Restart()
        {
            if (Level != null) Play(Level);
        }

        /// <summary>Removes the current board from the scene (e.g. on Home).</summary>
        public void Clear()
        {
            StopAllCoroutines();
            if (Proto.Drag != null) Proto.Drag.ReleaseAll();
            Time.timeScale = 1f;
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            worldRoot = null;
            Objects.Clear();
            Bins.Clear();
            origins.Clear();
            tweens.Clear();
            Level = null;
            Attempt = null;
            InputLive = false;
            RoundComplete = false;
            ShowNext = false;
        }

        void BuildBoard(bool pour)
        {
            StopAllCoroutines();
            if (Proto.Drag != null) Proto.Drag.ReleaseAll();
            Time.timeScale = 1f;
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            Objects.Clear();
            Bins.Clear();
            origins.Clear();
            tweens.Clear();
            worldRoot = new GameObject("World").transform;

            RoundComplete = false;
            ShowNext = false;
            InputLive = false;
            Combo = 0;
            audioStreak = 0;
            noteIndex = 0;
            pileOrder = 100;
            ReasonText = null;

            ComputeLayout();
            BuildStatics();
            BuildBinsAndObjects();
            StartCoroutine(Intro(pour));
            StartCoroutine(PrewarmArt());
        }

        void Update()
        {
            if (Attempt == null) return;
            Attempt.Clock.Paused = !appFocused || ExternallyPaused || (Proto.Hud != null && Proto.Hud.PanelOpen);
            // Unscaled so completion slow-motion never changes active time; clamped so a hitch can't add seconds.
            if (InputLive) Attempt.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        }

        void OnApplicationFocus(bool focus) { appFocused = focus; }
        void OnApplicationPause(bool paused) { appFocused = !paused; }

        // ---- layout and building --------------------------------------------------------------

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
            var theme = Theme();

            // Screen-edge walls and ceiling: nothing is ever lost (GDD ch. 02 §2.5 rule 1).
            StaticBox("WallL", new Vector2(left - 0.5f, 0f), new Vector2(1f, 60f));
            StaticBox("WallR", new Vector2(right + 0.5f, 0f), new Vector2(1f, 60f));
            StaticBox("Ceiling", new Vector2(0f, top + 0.5f), new Vector2(60f, 1f));
            StaticBox("Floor", new Vector2(0f, floorY - 0.5f), new Vector2(60f, 1f));

            float halfH = Proto.Cam.orthographicSize;
            ToyStyle.BuildBackground(worldRoot, left, right, -halfH, halfH);
            RoomBackdrop.Build(worldRoot, theme, left, right, TableTop, top);

            Visual("FloorBand", ToyStyle.Panel(ToyStyle.FloorColor, false), new Vector2(0f, floorY - 5f + 0.02f),
                new Vector2((right - left) + 1f, 10f), Color.white, -50);

            // The table the pile sits on, with small lips so objects stay put. Its surface follows the room.
            float thick = Units.DpToWorld(14f);
            float lip = Units.DpToWorld(20f);
            float width = tableRight - tableLeft;
            float cx = (tableLeft + tableRight) / 2f;
            StaticBox("Table", new Vector2(cx, TableTop - thick / 2f), new Vector2(width, thick));
            StaticBox("LipL", new Vector2(tableLeft + thick / 2f, TableTop + lip / 2f), new Vector2(thick, lip));
            StaticBox("LipR", new Vector2(tableRight - thick / 2f, TableTop + lip / 2f), new Vector2(thick, lip));
            var surface = ToyStyle.Strip(ToyStyle.Hex(theme.surfaceHex));
            Visual("TableTop", surface, new Vector2(cx, TableTop - thick / 2f), new Vector2(width, thick), Color.white, -10);
            Visual("TableLipL", surface, new Vector2(tableLeft + thick / 2f, TableTop + lip / 2f - thick * 0.25f),
                new Vector2(thick, lip + thick * 0.5f), Color.white, -9);
            Visual("TableLipR", surface, new Vector2(tableRight - thick / 2f, TableTop + lip / 2f - thick * 0.25f),
                new Vector2(thick, lip + thick * 0.5f), Color.white, -9);
            Visual("TableShadow", rr, new Vector2(cx, TableTop - thick - thick * 0.35f), new Vector2(width * 0.96f, thick * 0.7f),
                new Color(ToyStyle.Ink.r, ToyStyle.Ink.g, ToyStyle.Ink.b, 0.14f), -11);
        }

        RoomTheme Theme()
        {
            var room = Level != null && Proto.Flow != null && Proto.Flow.Campaign != null ? Proto.Flow.Campaign.Room(Level.roomId) : null;
            return room != null ? room.theme : new RoomTheme();
        }

        void BuildBinsAndObjects()
        {
            var board = Attempt.Board;
            var def = board.Def;
            int binCount = def.targets.Length;
            float gap = Units.DpToWorld(binCount > 3 ? 8f : 12f);
            float margin = Units.DpToWorld(binCount > 3 ? 10f : 16f);
            float binW = ((right - left) - 2f * margin - (binCount - 1) * gap) / binCount;
            float binH = BinsTop - floorY;
            for (int i = 0; i < binCount; i++)
            {
                float x = left + margin + binW / 2f + i * (binW + gap);
                Bins.Add(Bin.Create(i, def.targets[i], new Vector2(x, floorY), binW, binH, staticMaterial, worldRoot));
            }

            float baseSize = Units.DpToWorld(56f);
            for (int i = 0; i < board.ObjectCount; i++)
            {
                var od = board.Objects[i];
                int units = board.Units(i);
                float size = baseSize * od.SizeScale * (units > 1 ? 1.3f : 1f);
                var o = SortObject.CreateFromDef(i, od, Color.white, size, new Vector2(-1000f, -1000f), objectMaterial,
                    NextPileOrder(), worldRoot);
                if (o == null) { Debug.LogError("No art for " + od.id + " in level " + Level.id); continue; }
                o.movable = def.IsCapacity;
                if (units > 1) AddUnitBadge(o, units);
                ToyStyle.AddObjectShadow(o); // visual style: stylised drop shadow
                o.gameObject.SetActive(false);
                Objects.Add(o);
            }
            // Pour order is shuffled so targets are not filled in authoring order.
            for (int i = Objects.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = Objects[i];
                Objects[i] = Objects[j];
                Objects[j] = tmp;
            }
            for (int t = 0; t < Bins.Count; t++) RefreshTarget(t, false);
            melody = MakeMelody(Mathf.Max(2, Objects.Count));
        }

        /// <summary>Large variants show their space cost ("2") on a small badge that travels with them.</summary>
        void AddUnitBadge(SortObject o, int units)
        {
            var go = new GameObject("UnitBadge");
            go.transform.SetParent(o.sr.transform, false);
            go.transform.localPosition = new Vector3(0.32f, -0.3f, 0f);
            go.transform.localScale = Vector3.one * 0.36f;
            var ring = go.AddComponent<SpriteRenderer>();
            ring.sprite = ProcSprites.Circle();
            ring.color = ToyStyle.Ink;
            var inner = new GameObject("Face");
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = Vector3.one * 0.78f;
            var face = inner.AddComponent<SpriteRenderer>();
            face.sprite = ProcSprites.Circle();
            face.color = ToyStyle.Reward;
            var txt = new GameObject("Text");
            txt.transform.SetParent(go.transform, false);
            var t = txt.AddComponent<TextMesh>();
            t.font = ToyStyle.Display;
            txt.GetComponent<MeshRenderer>().sharedMaterial = t.font.material;
            t.fontSize = 64;
            t.characterSize = 0.012f;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = ToyStyle.Ink;
            t.text = units.ToString();
            o.unitBadge = go.transform;
            o.SetOrder(o.sr.sortingOrder);
        }

        IEnumerator Intro(bool pour)
        {
            for (int i = 0; i < Bins.Count; i++) Bins[i].PopIn(0.1f + 0.06f * i);

            // Objects already placed in a resumed attempt go straight to their slots.
            var board = Attempt.Board;
            for (int i = 0; i < Objects.Count; i++)
            {
                var o = Objects[i];
                if (board.Location(o.id) < 0) continue;
                o.gameObject.SetActive(true);
                o.state = ObjState.Sorted;
                MakeKinematic(o);
            }
            for (int t = 0; t < Bins.Count; t++) LayoutTarget(t, true);

            yield return new WaitForSeconds(0.5f);
            InputLive = true;
            roundStart = Time.time;

            // Pour the remaining objects in from the top like tipping out a bag.
            float spread = (tableRight - tableLeft) / 2f - Units.DpToWorld(56f);
            float cx = (tableLeft + tableRight) / 2f;
            for (int i = 0; i < Objects.Count; i++)
            {
                var o = Objects[i];
                if (board.Location(o.id) >= 0) continue;
                float y = Mathf.Min(top - o.HalfExtent - 0.1f, TableTop + 4f + (i % 3) * 0.9f);
                o.transform.position = new Vector2(cx + Random.Range(-spread, spread), y);
                o.gameObject.SetActive(true);
                o.rb.SetVelocity(new Vector2(Random.Range(-1.5f, 1.5f), -2f));
                o.rb.angularVelocity = Random.Range(-200f, 200f);
                if (pour) yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>Rasterise the next level's art in the background (one object per frame, never during a drag).</summary>
        IEnumerator PrewarmArt()
        {
            yield return new WaitForSeconds(1.5f);
            var next = Proto.Flow != null ? Proto.Flow.PeekNext(Level) : null;
            if (next == null) yield break;
            foreach (var b in next.boards)
                foreach (var s in b.objects)
                {
                    var d = ObjectLibrary.Get(s.asset);
                    if (d == null || ObjectArt.IsCached(d.id)) continue;
                    while (Proto.Drag != null && Proto.Drag.IsHolding) yield return null;
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

        // ---- drag hooks ------------------------------------------------------------------------

        void OnPicked(SortObject o)
        {
            if (Attempt == null || !Objects.Contains(o)) return;
            Attempt.BeginPickup();
            if (FirstPickupAt < 0f) FirstPickupAt = Time.unscaledTime;
            StopTween(o);
            int from = Attempt.Board.Location(o.id);
            origins[o] = new Origin { target = from, pos = o.Position };
            if (from >= 0)
            {
                // Lifted out of a planning target: the target keeps its reservation until a move commits.
                MakeDynamic(o);
                o.rb.simulated = false; // the drag owns it while held
                o.transform.localScale = new Vector3(o.size, o.size, 1f);
                LayoutTarget(from, false);
            }
        }

        void OnReleased(SortObject o, bool cancelled)
        {
            if (Attempt == null || !Objects.Contains(o)) return;
            Origin origin;
            if (!origins.TryGetValue(o, out origin)) return;
            if (cancelled) { ReturnToCommitted(o); return; }
            if (origin.target >= 0) StartCoroutine(WatchReleaseFromTarget(o, origin.target));
            else origins.Remove(o); // from the pile: wherever it lands on the board is fine (no mistake)
        }

        /// <summary>
        /// An object lifted out of a planning target was let go: if it lands in a target the bin judges it; if it is
        /// left on the table it goes back to the board (a legal rearrangement); otherwise it returns to its slot.
        /// </summary>
        IEnumerator WatchReleaseFromTarget(SortObject o, int from)
        {
            float until = Time.time + 0.6f;
            while (Time.time < until)
            {
                if (o == null || Attempt == null) yield break;
                if (o.state == ObjState.Sorted || o.state == ObjState.Held || Attempt.Board.Location(o.id) != from) yield break;
                yield return null;
            }
            if (o == null || (o.state != ObjState.Pile && o.state != ObjState.Guided)) yield break;
            if (o.Position.y > TableTop - 0.3f && Attempt.ReturnToBoard(o.id))
            {
                origins.Remove(o);
                RefreshTarget(from, true);
                Changed();
            }
            else ReturnToCommitted(o);
        }

        // ---- placement -------------------------------------------------------------------------

        /// <summary>Called by a bin when an object's centre enters its mouth. The board model decides.</summary>
        public void Judge(Bin bin, SortObject o)
        {
            if (Attempt == null || Attempt.State == AttemptState.Complete || !Objects.Contains(o)) return;
            int from = Attempt.Board.Location(o.id);
            var r = Attempt.Drop(o.id, bin.index);
            switch (r)
            {
                case PlaceResult.Accepted:
                    origins.Remove(o);
                    o.state = ObjState.Sorted;
                    MakeKinematic(o);
                    if (from >= 0) RefreshTarget(from, true);
                    RefreshTarget(bin.index, true);
                    bin.PlayAccept(o);
                    OnCorrect(bin, o);
                    Changed();
                    CheckSolved();
                    break;
                case PlaceResult.WrongTarget:
                case PlaceResult.Full:
                    bin.PlayReject(o);
                    ShowReason(bin, r);
                    ReturnToCommitted(o);
                    audioStreak = 0;
                    Changed();
                    break;
                default:
                    // Dropped back into the target it came from.
                    if (from == bin.index)
                    {
                        origins.Remove(o);
                        o.state = ObjState.Sorted;
                        MakeKinematic(o);
                        LayoutTarget(from, false);
                    }
                    break;
            }
        }

        void ShowReason(Bin bin, PlaceResult r)
        {
            ReasonText = r == PlaceResult.Full ? "FULL" : bin.def.DisplayLabel + " ONLY";
            ReasonAt = Time.unscaledTime;
            ReasonWorldPos = bin.MouthCenter + Vector2.up * 0.6f;
        }

        /// <summary>Sends an object back to its last committed spot: its slot, or where it was picked up on the pile.</summary>
        void ReturnToCommitted(SortObject o)
        {
            int at = Attempt.Board.Location(o.id);
            Origin origin;
            bool hasOrigin = origins.TryGetValue(o, out origin);
            origins.Remove(o);
            if (at >= 0)
            {
                o.state = ObjState.Sorted;
                MakeKinematic(o);
                LayoutTarget(at, false);
                return;
            }
            Vector2 to = hasOrigin ? origin.pos : RandomPileLandingPoint();
            MakeKinematic(o);
            o.state = ObjState.Spitting;
            StopTween(o);
            tweens[o] = StartCoroutine(TweenTo(o, to, o.size, 0.28f, true, () =>
            {
                MakeDynamic(o);
                o.state = ObjState.Pile;
                o.rb.SetVelocity(Vector2.zero);
            }));
        }

        /// <summary>Places every committed object of a target into its deterministic slot.</summary>
        void LayoutTarget(int target, bool instant)
        {
            var board = Attempt.Board;
            int slots = board.Def.targets[target].Unlimited ? Mathf.Max(1, board.Expected(target)) : board.Capacity(target);
            slots = Mathf.Max(slots, board.Count(target));
            int k = 0;
            for (int i = 0; i < Objects.Count; i++)
            {
                var o = Objects[i];
                if (board.Location(o.id) != target || o.state != ObjState.Sorted) continue;
                float size;
                Vector2 pos = Bins[target].SlotPosition(k, slots, out size);
                float scale = Mathf.Min(o.size, size * (board.Units(o.id) > 1 ? 1.2f : 1f));
                o.SetOrder(310 + k * 2);
                if (instant)
                {
                    StopTween(o);
                    o.transform.position = pos;
                    o.transform.rotation = Quaternion.identity;
                    o.transform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    StopTween(o);
                    tweens[o] = StartCoroutine(TweenTo(o, pos, scale, 0.18f, false, null));
                }
                k++;
            }
        }

        void RefreshTarget(int target, bool layout)
        {
            var board = Attempt.Board;
            var bin = Bins[target];
            if (board.Def.targets[target].Unlimited) bin.SetCounter(board.Count(target) + "/" + board.Expected(target));
            else bin.SetCounter(board.Used(target) + "/" + board.Capacity(target));
            if (layout) LayoutTarget(target, false);
        }

        IEnumerator TweenTo(SortObject o, Vector2 to, float scale, float duration, bool arc, System.Action done)
        {
            Vector2 from = o.Position;
            float s0 = o.transform.localScale.x;
            float a0 = o.transform.eulerAngles.z;
            float h = arc ? Mathf.Max(0.8f, (to - from).magnitude * 0.25f) : 0f;
            if (PlayerSettings.ReducedMotion) duration *= 0.6f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (o == null) yield break;
                float lin = Mathf.Clamp01(t / duration);
                float k = PlayerSettings.ReducedMotion ? lin : Juice.EaseOutBack(lin);
                o.transform.position = Vector2.LerpUnclamped(from, to, arc ? lin : k) + Vector2.up * h * 4f * lin * (1f - lin);
                o.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(a0, 0f, lin));
                float s = Mathf.LerpUnclamped(s0, scale, k);
                o.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (o == null) yield break;
            o.transform.position = to;
            o.transform.localScale = new Vector3(scale, scale, 1f);
            o.transform.rotation = Quaternion.identity;
            tweens.Remove(o);
            if (done != null) done();
        }

        void StopTween(SortObject o)
        {
            Coroutine c;
            if (tweens.TryGetValue(o, out c) && c != null) StopCoroutine(c);
            tweens.Remove(o);
        }

        /// <summary>Placed objects are kinematic triggers: they stay put, never block a drop, and stay pickable.</summary>
        static void MakeKinematic(SortObject o)
        {
            o.rb.simulated = true;
            o.rb.SetVelocity(Vector2.zero);
            o.rb.angularVelocity = 0f;
            o.rb.bodyType = RigidbodyType2D.Kinematic;
            o.col.isTrigger = true;
        }

        static void MakeDynamic(SortObject o)
        {
            o.rb.bodyType = RigidbodyType2D.Dynamic;
            o.col.isTrigger = false;
            o.rb.simulated = true;
        }

        void CheckSolved()
        {
            if (!Attempt.BoardSolved) return;
            InputLive = false;
            if (Proto.Drag != null) Proto.Drag.ReleaseAll();
            if (!Attempt.IsLastBoard) StartCoroutine(NextBoard());
            else if (Attempt.TryComplete()) StartCoroutine(Complete());
        }

        IEnumerator NextBoard()
        {
            for (int i = 0; i < Bins.Count; i++) { Bins[i].Hop(); yield return new WaitForSeconds(0.06f); }
            Proto.Audio.Stamp();
            yield return new WaitForSeconds(0.8f);
            Attempt.AdvanceBoard();
            Changed();
            BuildBoard(true);
        }

        void Changed()
        {
            if (BoardChanged != null) BoardChanged(Attempt);
        }

        // ---- feedback --------------------------------------------------------------------------

        void OnCorrect(Bin bin, SortObject o)
        {
            float now = Time.time;
            audioStreak = now - lastCorrect <= 1.2f ? audioStreak + 1 : 1;
            lastCorrect = now;

            // Mastery clean streak on ordinary boards (cosmetic): a small pop at 3 and 5.
            if (Attempt.StreakMilestone && PlayerSettings.Mastery)
            {
                Combo = Attempt.Streak;
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

            // Finishing action for ordinary targets: close the lid once it holds everything it takes.
            var board = Attempt.Board;
            if (board.Def.targets[bin.index].Unlimited && board.Count(bin.index) >= board.Expected(bin.index)) bin.CloseLid();
        }

        IEnumerator Complete()
        {
            RoundComplete = true;
            InputLive = false;
            CompleteTime = Time.unscaledTime;
            Proto.Telemetry.OnRoundComplete(Round, Time.time - roundStart);
            if (LevelCompleted != null) LevelCompleted(Attempt); // commit before any animation

            if (Proto.Config.juice && !PlayerSettings.ReducedMotion)
            {
                Time.timeScale = 0.35f;
                yield return new WaitForSecondsRealtime(0.25f);
                Time.timeScale = 1f;
            }

            int[] cadence = { 5, 7, 9, 12 };
            for (int i = 0; i < Bins.Count; i++)
            {
                Bins[i].Hop();
                Proto.Audio.Note(cadence[i % cadence.Length], 0.7f);
                yield return new WaitForSecondsRealtime(0.08f);
            }

            yield return new WaitForSecondsRealtime(0.25f);
            StampTime = Time.unscaledTime;
            Proto.Audio.Stamp();
            if (!PlayerSettings.ReducedMotion) Proto.Juice.Shake(4f, 0.12f);
            Haptics.Play(Haptics.Kind.Heavy);
            ShowNext = true;
        }

        // ---- helpers ---------------------------------------------------------------------------

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
