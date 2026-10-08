using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Builds the portrait diorama (pile on a table, three bins along the bottom thumb zone) and runs an
    /// endless series of 10-object colour sorts. P1 is about feel only: no timer, no fail state, no Oops.
    /// </summary>
    public class RoundDirector : MonoBehaviour
    {
        public const int ObjectsPerRound = 10;
        public const int BinsPerRound = 3;

        static readonly Color[] Palette =
        {
            new Color32(0xF2, 0x56, 0x3A, 0xFF), // tomato red   — solid
            new Color32(0x3D, 0x7B, 0xF2, 0xFF), // blue         — dots
            new Color32(0xF5, 0xC3, 0x27, 0xFF), // yellow       — stripes
            new Color32(0x43, 0xC4, 0x6B, 0xFF), // green        — waves
            new Color32(0x9B, 0x5D, 0xE5, 0xFF), // purple       — checker
        };

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
        float lastCorrect = -10f;

        // Library object ids used in the last rounds, so consecutive rounds don't repeat objects.
        const int RecentRounds = 2;
        readonly Queue<List<string>> recentRounds = new Queue<List<string>>();

        /// <summary>Content mode the current round was actually built with (falls back to Shapes if the pool is too small).</summary>
        public ContentMode RoundMode { get; private set; }

        void Awake()
        {
            objectMaterial = new PhysicsMaterial2D("Toy") { friction = 0.7f, bounciness = 0.08f };
            staticMaterial = new PhysicsMaterial2D("Static") { friction = 0.6f, bounciness = 0f };
            RoundMode = ContentSettings.Mode;
        }

        public int NextPileOrder() { return ++pileOrder; }

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
            Combo = 0;
            noteIndex = 0;
            pileOrder = 100;

            ComputeLayout();
            BuildStatics();
            BuildBinsAndObjects();
            StartCoroutine(Intro());
            StartCoroutine(PrewarmArt());
        }

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

        void BuildBinsAndObjects()
        {
            var mode = ContentSettings.Mode;
            var pool = ContentSettings.Pool;

            // Three of the five colour categories.
            var cats = new List<int> { 0, 1, 2, 3, 4 };
            if (mode != ContentMode.Shapes)
            {
                // Only colours with enough library objects to fill a 4-object bin without repeating an object.
                cats.Clear();
                foreach (var c in RoundContent.ColorsWithAtLeast(pool, 4)) cats.Add((int)c);
                if (cats.Count < BinsPerRound)
                {
                    Debug.LogWarning("Object pool too small for a real-object round; using shapes.");
                    mode = ContentMode.Shapes;
                    cats = new List<int> { 0, 1, 2, 3, 4 };
                }
            }
            RoundMode = mode;
            Shuffle(cats);
            cats.RemoveRange(BinsPerRound, cats.Count - BinsPerRound);
            cats.Sort();

            var counts = new List<int> { 4, 3, 3 };
            Shuffle(counts);

            float gap = Units.DpToWorld(12f);
            float margin = Units.DpToWorld(16f);
            float binW = ((right - left) - 2f * margin - (BinsPerRound - 1) * gap) / BinsPerRound;
            float binH = BinsTop - floorY;
            for (int i = 0; i < BinsPerRound; i++)
            {
                float x = left + margin + binW / 2f + i * (binW + gap);
                var bin = Bin.Create(cats[i], Palette[cats[i]], cats[i], counts[i], new Vector2(x, floorY), binW, binH,
                    staticMaterial, worldRoot);
                Bins.Add(bin);
            }

            // Mass classes 1..5 twice over, so every round exercises light and heavy drags.
            var masses = new List<int> { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 };
            Shuffle(masses);
            var categories = new List<int>();
            for (int i = 0; i < BinsPerRound; i++)
                for (int k = 0; k < counts[i]; k++) categories.Add(cats[i]);
            Shuffle(categories);

            // Real / Mixed: ask the object library which recognisable objects fill which slots.
            RoundSlot[] plan = null;
            if (mode != ContentMode.Shapes)
            {
                var real = new bool[ObjectsPerRound];
                var order = new List<int>();
                for (int i = 0; i < ObjectsPerRound; i++) order.Add(i);
                Shuffle(order);
                int realCount = mode == ContentMode.RealObjects ? ObjectsPerRound : ObjectsPerRound / 2;
                for (int i = 0; i < realCount; i++) real[order[i]] = true;

                var colors = new SortColor[ObjectsPerRound];
                for (int i = 0; i < ObjectsPerRound; i++) colors[i] = (SortColor)categories[i];
                var recent = new HashSet<string>();
                foreach (var ids in recentRounds) recent.UnionWith(ids);
                plan = RoundContent.Fill(colors, real, pool, recent, n => Random.Range(0, n));
            }

            var shapes = (ShapeKind[])System.Enum.GetValues(typeof(ShapeKind));
            float baseSize = Units.DpToWorld(56f);
            var usedIds = new List<string>();
            for (int i = 0; i < ObjectsPerRound; i++)
            {
                SortObject o = null;
                if (plan != null && plan[i].def != null)
                {
                    var def = plan[i].def;
                    o = SortObject.CreateFromDef(i, def, Palette[categories[i]], baseSize * def.SizeScale,
                        new Vector2(-1000f, -1000f), objectMaterial, NextPileOrder(), worldRoot);
                    if (o != null) usedIds.Add(def.id);
                }
                if (o == null)
                {
                    int m = masses[i];
                    float size = baseSize * (1f + 0.075f * (m - 1)); // 56 dp .. ~73 dp
                    var shape = shapes[Random.Range(0, shapes.Length)];
                    o = SortObject.Create(i, categories[i], categories[i], Palette[categories[i]], shape, m, size,
                        new Vector2(-1000f, -1000f), objectMaterial, NextPileOrder(), worldRoot);
                }
                ToyStyle.AddObjectShadow(o); // visual style: stylised drop shadow
                o.gameObject.SetActive(false);
                Objects.Add(o);
            }

            recentRounds.Enqueue(usedIds);
            while (recentRounds.Count > RecentRounds) recentRounds.Dequeue();

            melody = MakeMelody(ObjectsPerRound);
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
        IEnumerator PrewarmArt()
        {
            if (ContentSettings.Mode == ContentMode.Shapes) yield break;
            yield return new WaitForSeconds(1.5f);
            var all = ObjectLibrary.All;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (!d.ColorSortable || !d.InPool(ContentSettings.Pool) || ObjectArt.IsCached(d.id)) continue;
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

        public void OnCorrect(Bin bin, SortObject o)
        {
            float now = Time.time;
            Combo = now - lastCorrect <= 1.2f ? Combo + 1 : 1;
            lastCorrect = now;
            if (Combo >= 2) { ComboTime = Time.unscaledTime; ComboWorldPos = bin.MouthCenter; }

            int degree = melody[Mathf.Min(noteIndex, melody.Length - 1)];
            noteIndex++;
            if (Proto.Config.juice)
            {
                Proto.Audio.Note(degree);
                if (Combo >= 3) Proto.Audio.Note(degree + 2, 0.35f); // harmony a third above
            }
            else
            {
                Proto.Audio.Pop(1f);
            }

            bool allSorted = true;
            for (int i = 0; i < Objects.Count; i++)
                if (Objects[i].state != ObjState.Sorted) { allSorted = false; break; }
            if (allSorted) StartCoroutine(Complete());
        }

        public void OnWrong(Bin bin, SortObject o)
        {
            Combo = 0;
        }

        IEnumerator Complete()
        {
            RoundComplete = true;
            InputLive = false;
            CompleteTime = Time.unscaledTime;
            Proto.Telemetry.OnRoundComplete(Round, Time.time - roundStart);

            if (Proto.Config.juice)
            {
                Time.timeScale = 0.35f;
                yield return new WaitForSecondsRealtime(0.25f);
                Time.timeScale = 1f;
            }

            // Cadence: bins hop left to right on an ascending phrase.
            int[] cadence = { 5, 7, 9 };
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

        public void Next()
        {
            if (!ShowNext) return;
            Proto.Telemetry.OnNextTapped(Time.unscaledTime - CompleteTime);
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

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
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
