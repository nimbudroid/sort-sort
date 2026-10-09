using System.Collections;
using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// A container with a face ("Binbuddy", GDD ch. 08). Counts an object the moment its centre is inside the
    /// mouth (GDD ch. 02 §2.5 rule 2), gulps correct objects and spits wrong ones back onto the pile.
    /// The bin never decides anything: when an object's centre enters its mouth it asks the RoundDirector, which
    /// asks the board model. The bin plays the accept / reject feedback, shows its rule as a text label and its
    /// counter, and provides deterministic display slots for the objects it holds.
    /// Colliders live on the root; everything that animates lives under visualRoot.
    /// </summary>
    public class Bin : MonoBehaviour
    {
        public int index;       // position in the level's bin list (what the session judges against)
        public TargetDef def;   // rule, capacity, label and colour
        public int category;    // legacy: colour index for pattern/animation seeding
        public bool Closed;
        public Color color;

        float width, height, wall;
        Vector2 bottomCenter;

        Transform visualRoot;
        Springy spring;
        SpriteRenderer back;
        Color backColor;
        Transform eyeL, eyeR, pupilL, pupilR;
        Vector3 pupilLBase, pupilRBase;
        float eyeRadius;
        TextMesh counter;
        TextMesh[] counterOutline; // visual style: ink copies around the counter for a chunky outline
        float counterSize;
        Transform lidHinge;
        SpriteRenderer lidSprite;
        BoxCollider2D lidCollider;

        bool hover;
        float nextBlink, blinkUntil;
        float happyUntil, disgustUntil, flashUntil;
        float wiggleStart = -10f;

        public float Top { get { return bottomCenter.y + height; } }
        public float Bottom { get { return bottomCenter.y; } }
        public float CenterX { get { return bottomCenter.x; } }
        public float InnerLeft { get { return bottomCenter.x - width / 2f + wall; } }
        public float InnerRight { get { return bottomCenter.x + width / 2f - wall; } }
        public Vector2 MouthCenter { get { return new Vector2(bottomCenter.x, Top); } }

        public static Bin Create(int index, TargetDef def, Vector2 bottomCenter,
            float width, float height, PhysicsMaterial2D material, Transform parent)
        {
            var go = new GameObject("Bin" + index + "_" + def.id);
            go.transform.SetParent(parent, false);
            go.transform.position = bottomCenter;
            var bin = go.AddComponent<Bin>();
            bin.index = index;
            bin.def = def;
            bin.category = def.color != SortColor.None ? (int)def.color : index;
            bin.color = ToyStyle.Hex(def.colorHex ?? "FF6FAE");
            int pattern = def.color != SortColor.None ? (int)def.color : -1; // colour chip only for colour rules
            bin.width = width;
            bin.height = height;
            bin.wall = Units.DpToWorld(8f);
            bin.bottomCenter = bottomCenter;
            bin.Build(pattern, material);
            return bin;
        }

        void Build(int pattern, PhysicsMaterial2D material)
        {
            // Physics: two walls and a floor. A lid collider switches on when the bin is full.
            AddBox("WallL", new Vector2(-width / 2f + wall / 2f, height / 2f), new Vector2(wall, height), material);
            AddBox("WallR", new Vector2(width / 2f - wall / 2f, height / 2f), new Vector2(wall, height), material);
            AddBox("Floor", new Vector2(0f, wall / 2f), new Vector2(width, wall), material);
            lidCollider = AddBox("LidCollider", new Vector2(0f, height + wall / 2f), new Vector2(width, wall), material);
            lidCollider.enabled = false;

            var vr = new GameObject("Visual");
            vr.transform.SetParent(transform, false);
            visualRoot = vr.transform;
            spring = vr.AddComponent<Springy>();

            // Visual style: chunky outlined toy panels (the back is tinted so the reject flash still works).
            ToyStyle.GroundShadow(transform, bottomCenter + new Vector2(0f, wall * 0.5f), width * 1.2f, height * 0.24f, -20);
            backColor = Color.Lerp(color, new Color(0.25f, 0.22f, 0.3f), 0.32f);
            back = Sliced("Back", ToyStyle.Panel(Color.white, false), new Vector2(0f, height / 2f), new Vector2(width, height), backColor, 0);
            Color side = Color.Lerp(color, Color.black, 0.25f);
            Sliced("SideL", ToyStyle.Strip(side), new Vector2(-width / 2f + wall * 0.7f, height / 2f), new Vector2(wall * 1.4f, height), Color.white, 300);
            Sliced("SideR", ToyStyle.Strip(side), new Vector2(width / 2f - wall * 0.7f, height / 2f), new Vector2(wall * 1.4f, height), Color.white, 300);
            float frontH = height * 0.45f;
            Sliced("Front", ToyStyle.Panel(Color.Lerp(color, Color.white, 0.18f), true), new Vector2(0f, frontH / 2f), new Vector2(width, frontH), Color.white, 301);
            // Lighter top rim across the mouth: reads as the container's molded lip.
            Sliced("Rim", ToyStyle.Strip(Color.Lerp(color, Color.white, 0.45f)), new Vector2(0f, height - wall * 0.8f),
                new Vector2(width + wall * 0.4f, wall * 2f), Color.white, 300);

            // Face: the eyes peek over the front lip so the front panel can carry the rule label.
            eyeRadius = Mathf.Min(width * 0.13f, height * 0.1f);
            var circle = ProcSprites.Circle();
            var eye = ToyStyle.Eye();
            eyeL = Disc("EyeL", eye, new Vector2(-width * 0.2f, EyeHeight), eyeRadius * 2f, Color.white, 302);
            eyeR = Disc("EyeR", eye, new Vector2(width * 0.2f, EyeHeight), eyeRadius * 2f, Color.white, 302);
            pupilL = Disc("PupilL", circle, Vector2.zero, 0.5f, new Color(0.12f, 0.1f, 0.14f), 303, eyeL);
            pupilR = Disc("PupilR", circle, Vector2.zero, 0.5f, new Color(0.12f, 0.1f, 0.14f), 303, eyeR);
            pupilLBase = pupilL.localPosition;
            pupilRBase = pupilR.localPosition;

            // Rule label (text is the rule; the colour chip only supports it) and capacity counter.
            BuildLabel(def != null ? def.DisplayLabel : "");
            bool chipShown = pattern >= 0;
            if (chipShown)
            {
                var chip = new GameObject("Chip");
                chip.transform.SetParent(visualRoot, false);
                chip.transform.localPosition = new Vector3(-width * 0.17f, height * 0.11f, 0f);
                chip.transform.localScale = Vector3.one * Mathf.Min(height * 0.16f, width * 0.26f);
                var chipSr = chip.AddComponent<SpriteRenderer>();
                chipSr.sprite = ProcSprites.Shape(ShapeKind.Circle, pattern);
                chipSr.color = color;
                chipSr.sortingOrder = 302;
            }

            float counterX = chipShown ? width * 0.14f : 0f;
            float badgeW = chipShown ? width * 0.46f : width * 0.56f;
            // The counter shrinks with narrow (four-bin) layouts so "4/4" always fits its badge.
            counterSize = Mathf.Min(height * 0.13f * 10f / 64f * 1.4f * 0.85f, badgeW * 0.75f / 1.9f * 10f / 64f);
            var txt = new GameObject("Counter");
            txt.transform.SetParent(visualRoot, false);
            txt.transform.localPosition = new Vector3(counterX, height * 0.11f, 0f);
            // Visual style: the counter sits on a small layered game-piece badge.
            var badge = new GameObject("CounterBadge");
            badge.transform.SetParent(visualRoot, false);
            badge.transform.localPosition = new Vector3(counterX, height * 0.105f, 0f);
            var badgeSr = badge.AddComponent<SpriteRenderer>();
            badgeSr.sprite = ToyStyle.Badge(ToyStyle.Hex("3B2F7A"));
            badgeSr.drawMode = SpriteDrawMode.Sliced;
            badgeSr.size = new Vector2(badgeW, height * 0.21f);
            badgeSr.sortingOrder = 303;

            counter = MakeCounterText(txt, Color.white, 305);
            // Chunky outline: eight ink copies around the white counter.
            counterOutline = new TextMesh[8];
            float ow = counterSize * 64f / 10f * 0.12f;
            for (int i = 0; i < counterOutline.Length; i++)
            {
                var o = new GameObject("CounterOutline");
                o.transform.SetParent(txt.transform, false);
                float a = i * Mathf.PI / 4f;
                o.transform.localPosition = new Vector3(Mathf.Cos(a) * ow, Mathf.Sin(a) * ow - ow * 0.4f, 0f);
                counterOutline[i] = MakeCounterText(o, ToyStyle.Ink, 304);
            }
            UpdateCounter();

            // Lid, hinged at the left rim; stands open (behind the pile) until the bin fills.
            var hinge = new GameObject("LidHinge");
            hinge.transform.SetParent(visualRoot, false);
            hinge.transform.localPosition = new Vector3(-width / 2f, height, 0f);
            hinge.transform.localRotation = Quaternion.Euler(0f, 0f, 100f);
            lidHinge = hinge.transform;
            var lid = new GameObject("Lid");
            lid.transform.SetParent(lidHinge, false);
            lid.transform.localPosition = new Vector3(width / 2f + wall * 0.3f, 0f, 0f);
            lidSprite = lid.AddComponent<SpriteRenderer>();
            lidSprite.sprite = ToyStyle.Strip(side);
            lidSprite.drawMode = SpriteDrawMode.Sliced;
            lidSprite.size = new Vector2(width + wall * 0.6f, wall * 1.6f);
            lidSprite.color = Color.white;
            lidSprite.sortingOrder = 1;

            nextBlink = Time.time + Random.Range(1.5f, 4f);
        }

        BoxCollider2D AddBox(string name, Vector2 local, Vector2 size, PhysicsMaterial2D material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.sharedMaterial = material;
            return box;
        }

        SpriteRenderer Sliced(string name, Sprite sprite, Vector2 local, Vector2 size, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        Transform Disc(string name, Sprite sprite, Vector2 local, float diameter, Color c, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : visualRoot, false);
            go.transform.localPosition = local;
            go.transform.localScale = Vector3.one * diameter;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = order;
            return go.transform;
        }

        public void PopIn(float delay)
        {
            spring.SetInstant(new Vector2(0.01f, 0.01f));
            spring.target = Vector2.zero;
            StartCoroutine(Juice.Delay(delay, () =>
            {
                spring.target = Vector2.one;
                if (!Proto.Config.juice) spring.SetInstant(Vector2.one);
            }));
        }

        /// <summary>True if x is over the open mouth (between the inner walls).</summary>
        public bool ColumnContains(float x) { return x > InnerLeft && x < InnerRight; }

        /// <summary>Horizontal distance from x to the mouth opening (0 when over it).</summary>
        public float MouthDistance(float x)
        {
            if (x < InnerLeft) return InnerLeft - x;
            if (x > InnerRight) return x - InnerRight;
            return 0f;
        }

        public void SetHover(bool on) { hover = on; }

        void FixedUpdate()
        {
            if (Closed || Proto.Director == null) return;
            var objects = Proto.Director.Objects;
            for (int i = 0; i < objects.Count; i++)
            {
                var o = objects[i];
                if (o.state != ObjState.Pile) continue;
                Vector2 p = o.Position;
                if (p.y >= Top || p.y <= Bottom || !ColumnContains(p.x)) continue;
                Proto.Director.Judge(this, o);
            }
        }

        /// <summary>Feedback for a committed placement (the director already moved the object into a slot).</summary>
        public void PlayAccept(SortObject o)
        {
            spring.Kick(new Vector2(1.12f, 0.88f));
            happyUntil = Time.time + 0.3f;
            Proto.Juice.Burst(MouthCenter, color, Random.Range(6, 11), 4f, o.size * 0.18f);
            Proto.Audio.Gulp();
            Haptics.Play(Haptics.Kind.Light);
            Proto.Telemetry.OnSorted(o, true);
        }

        /// <summary>Gentle rejection (the director returns the object to where it last was).</summary>
        public void PlayReject(SortObject o)
        {
            o.spring.Kick(new Vector2(0.82f, 1.18f)); // the object flinches too
            disgustUntil = Time.time + 0.6f;
            flashUntil = Time.time + 0.35f;
            spring.Kick(new Vector2(0.9f, 1.15f));
            Proto.Audio.Bwomp();
            Haptics.Play(Haptics.Kind.Medium);
            Proto.Telemetry.OnSorted(o, false);
        }

        /// <summary>
        /// Deterministic display slot k of n, above the front lip so held objects stay recognisable. `size` is the
        /// display size an object should be scaled to.
        /// </summary>
        public Vector2 SlotPosition(int k, int n, out float size)
        {
            n = Mathf.Max(1, n);
            int cols = n <= 3 ? n : Mathf.CeilToInt(n / 2f);
            int rows = Mathf.CeilToInt(n / (float)cols);
            float innerW = (InnerRight - InnerLeft);
            float cellW = innerW / cols;
            float bandBottom = height * 0.5f, bandTop = height * 1.02f;
            float cellH = (bandTop - bandBottom) / rows;
            size = Mathf.Min(cellW, cellH * 1.25f) * 0.95f;
            int row = k / cols, col = k % cols;
            int inRow = Mathf.Min(cols, n - row * cols);
            float rowW = inRow * cellW;
            float x = bottomCenter.x - rowW / 2f + cellW * (col + 0.5f);
            float y = Bottom + bandBottom + cellH * (row + 0.5f);
            return new Vector2(x, y);
        }

        /// <summary>The finishing action for ordinary targets: the lid closes once everything it takes is in.</summary>
        public void CloseLid()
        {
            if (Closed) return;
            Close();
        }

        void Close()
        {
            Closed = true;
            lidCollider.enabled = true;
            StartCoroutine(LidCloseAnim());
        }

        IEnumerator LidCloseAnim()
        {
            lidSprite.sortingOrder = 340;
            const float duration = 0.12f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                lidHinge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(100f, 0f, t / duration));
                yield return null;
            }
            lidHinge.localRotation = Quaternion.identity;
            Proto.Audio.Clack();
            Haptics.Play(Haptics.Kind.Tick);
            Hop();
            Proto.Juice.Ring(MouthCenter, Color.Lerp(color, Color.white, 0.4f), 14, 3.5f, height * 0.06f);
        }

        /// <summary>Little happy hop + wiggle, used when full and in the completion cascade.</summary>
        public void Hop()
        {
            if (!Proto.Config.juice) return;
            wiggleStart = Time.time;
            spring.Kick(new Vector2(0.92f, 1.1f));
        }

        float EyeHeight { get { return height * 0.58f; } }

        /// <summary>
        /// The rule as text on the front panel, between the counter badge and the front lip. Sized to fit the bin:
        /// one line when it fits, otherwise a two-word label wraps to two lines; never smaller than the smallest
        /// HUD text (12 dp).
        /// </summary>
        void BuildLabel(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var font = ToyStyle.Display;
            float maxW = width * 0.9f;
            float bandBottom = height * 0.215f, bandTop = height * 0.44f;
            float maxH = bandTop - bandBottom;
            float baseEm = Mathf.Min(Units.DpToWorld(18f), height * 0.13f);
            float minEm = Units.DpToWorld(12f);

            float singleEm = Mathf.Min(baseEm, maxW / Mathf.Max(0.01f, EmWidth(font, text)), maxH / 1.05f);
            string shown = text;
            float em = singleEm;
            int space = text.IndexOf(' ');
            if (space > 0 && singleEm < baseEm * 0.85f)
            {
                // Split at the space nearest the middle.
                int best = space;
                for (int i = space; i >= 0 && i < text.Length; i = text.IndexOf(' ', i + 1))
                    if (Mathf.Abs(i - text.Length / 2) < Mathf.Abs(best - text.Length / 2)) best = i;
                string l1 = text.Substring(0, best), l2 = text.Substring(best + 1);
                float twoEm = Mathf.Min(baseEm, maxW / Mathf.Max(0.01f, Mathf.Max(EmWidth(font, l1), EmWidth(font, l2))), maxH / 2.1f);
                if (twoEm > singleEm) { shown = l1 + "\n" + l2; em = twoEm; }
            }
            em = Mathf.Max(em, minEm);

            var go = new GameObject("RuleLabel");
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = new Vector3(0f, (bandBottom + bandTop) / 2f, 0f);
            float size = em * 10f / 64f;
            MakeLabelText(go, shown, ToyGui.TextCream, size, 307);
            float ow = em * 0.11f;
            for (int i = 0; i < 8; i++)
            {
                var o = new GameObject("RuleLabelOutline");
                o.transform.SetParent(go.transform, false);
                float a = i * Mathf.PI / 4f;
                o.transform.localPosition = new Vector3(Mathf.Cos(a) * ow, Mathf.Sin(a) * ow - ow * 0.45f, 0f);
                MakeLabelText(o, shown, ToyStyle.Ink, size, 306);
            }
        }

        /// <summary>Width of a single line in ems (advance at font size 64 / 64).</summary>
        static float EmWidth(Font font, string s)
        {
            font.RequestCharactersInTexture(s, 64);
            float w = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                CharacterInfo ci;
                if (font.GetCharacterInfo(s[i], out ci, 64)) w += ci.advance;
            }
            return w / 64f;
        }

        TextMesh MakeLabelText(GameObject go, string text, Color c, float size, int order)
        {
            var t = go.AddComponent<TextMesh>();
            t.font = ToyStyle.Display;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = t.font.material;
            mr.sortingOrder = order;
            t.fontSize = 64;
            t.characterSize = size;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.lineSpacing = 0.92f;
            t.color = c;
            t.text = text;
            return t;
        }

        /// <summary>Counter text: objects held / expected on ordinary targets, units used / capacity on planning targets.</summary>
        public void SetCounter(string text)
        {
            if (counter == null || counter.text == text) return;
            counter.text = text;
            if (counterOutline != null)
                for (int i = 0; i < counterOutline.Length; i++) counterOutline[i].text = text;
        }

        void UpdateCounter() { SetCounter("0/0"); }

        TextMesh MakeCounterText(GameObject go, Color c, int order)
        {
            var t = go.AddComponent<TextMesh>();
            t.font = ToyStyle.Display;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = t.font.material;
            mr.sortingOrder = order;
            t.fontSize = 64;
            t.characterSize = counterSize; // Titan One is wider than the old font; capped by the badge width
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = c;
            return t;
        }

        void Update()
        {
            bool juice = Proto.Config.juice;
            float now = Time.time;

            // Blink.
            if (now > nextBlink)
            {
                blinkUntil = now + 0.09f;
                nextBlink = now + Random.Range(2f, 5f);
            }

            Vector2 left = Vector2.one, right = Vector2.one;
            if (juice)
            {
                if (hover) left = right = new Vector2(1.18f, 1.18f);
                if (now < happyUntil) left = right = new Vector2(1.1f, 0.28f);
                if (now < disgustUntil) { left = new Vector2(1.05f, 0.35f); right = new Vector2(1f, 0.8f); }
                if (now < blinkUntil) { left.y = 0.1f; right.y = 0.1f; }
            }
            float k = 1f - Mathf.Exp(-Time.deltaTime * 30f);
            eyeL.localScale = Vector3.Lerp(eyeL.localScale, new Vector3(left.x, left.y, 1f) * eyeRadius * 2f, k);
            eyeR.localScale = Vector3.Lerp(eyeR.localScale, new Vector3(right.x, right.y, 1f) * eyeRadius * 2f, k);

            // Pupils track the object under the finger (or drift when idle).
            Vector2 look;
            Vector2 eyesWorld = (Vector2)visualRoot.position + new Vector2(0f, EyeHeight);
            Vector2 target;
            if (juice && Proto.Drag != null && Proto.Drag.TryGetHeldPosition(out target))
                look = Vector2.ClampMagnitude((target - eyesWorld) * 0.6f, 1f);
            else
                look = new Vector2(Mathf.Sin(now * 0.7f + category), Mathf.Cos(now * 0.5f + category * 2f) * 0.5f) * 0.4f;
            if (juice && now < disgustUntil) look = new Vector2(0f, -0.8f);
            pupilL.localPosition = Vector3.Lerp(pupilL.localPosition, pupilLBase + (Vector3)(look * 0.24f), k);
            pupilR.localPosition = Vector3.Lerp(pupilR.localPosition, pupilRBase + (Vector3)(look * 0.24f), k);

            // Red flash on reject.
            back.color = now < flashUntil && juice
                ? Color.Lerp(backColor, new Color(0.95f, 0.2f, 0.2f), (flashUntil - now) / 0.35f)
                : backColor;

            // Wiggle.
            float w = now - wiggleStart;
            float angle = w < 0.5f ? Mathf.Sin(w * 40f) * 6f * (1f - w / 0.5f) : 0f;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
