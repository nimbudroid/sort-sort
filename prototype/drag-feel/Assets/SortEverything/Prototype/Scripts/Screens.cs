using System.Collections.Generic;
using UnityEngine;

namespace SortEverything.Prototype
{
    public enum MenuScreen { None, Home, House, Room, Settings, Collection, Records }

    /// <summary>
    /// The out-of-level screens and the in-level overlays, drawn with IMGUI in the existing chunky toy style:
    /// Home / Continue, the whole-house overview (room cards, area restoration, locked rooms "coming soon"),
    /// room → area → section select, Pause, Inspect Scene, Settings, Collection and Records.
    /// Reads progress from AppFlow.Progress; never changes rules or rewards itself.
    /// </summary>
    public class Screens : MonoBehaviour
    {
        public MenuScreen Current { get; private set; }
        public bool Paused { get; private set; }
        public bool Inspecting { get; private set; }
        /// <summary>A full-screen menu is showing (the board is cleared).</summary>
        public bool MenuOpen { get { return Current != MenuScreen.None; } }

        RoomDef room;                       // room shown on the Room screen
        MenuScreen backFromSettings = MenuScreen.Home;
        bool settingsFromPause;

        float u;
        GUIStyle title, head, body, small, button, bigButton, tile, label;
        float scrollY, contentH, viewH;
        Vector2 touchStart;
        bool touchScrolling, suppressClicks;
        readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // ---- navigation -------------------------------------------------------------------------

        public void Show(MenuScreen s, RoomDef forRoom = null)
        {
            if (s == MenuScreen.Room && forRoom != null) room = forRoom;
            Current = s;
            Paused = false;
            Inspecting = false;
            scrollY = 0f;
            if (Proto.Director != null) Proto.Director.ExternallyPaused = false;
        }

        public void Close()
        {
            Current = MenuScreen.None;
            Paused = false;
            Inspecting = false;
            if (Proto.Director != null) Proto.Director.ExternallyPaused = false;
        }

        public void Pause()
        {
            if (MenuOpen || Proto.Director == null || Proto.Director.Level == null) return;
            if (Proto.Drag != null) Proto.Drag.ReleaseAll(); // cancelled drags return to their committed spot
            Paused = true;
            Proto.Director.ExternallyPaused = true;
        }

        public void Resume()
        {
            Paused = false;
            if (Proto.Director != null) Proto.Director.ExternallyPaused = false;
        }

        public void SetInspecting(bool on)
        {
            Inspecting = on;
            if (Proto.Director != null) Proto.Director.ExternallyPaused = on;
        }

        /// <summary>Screen point (IMGUI coordinates) over an element this component owns.</summary>
        public bool BlocksInput(Vector2 gui)
        {
            if (MenuOpen || Paused) return true;
            if (Inspecting) return InspectBackRect.Contains(gui);
            var d = Proto.Director;
            if (d != null && d.Level != null && PauseRect.Contains(gui)) return true;
            return false;
        }

        // ---- layout helpers ---------------------------------------------------------------------

        Rect Safe
        {
            get
            {
                Rect s = Screen.safeArea;
                return new Rect(s.x, Screen.height - s.yMax, s.width, s.height);
            }
        }

        /// <summary>Pause button: top-right, left of the observer's ••• button.</summary>
        public Rect PauseRect
        {
            get
            {
                float p = Units.DpToPx(1f);
                Rect safe = Safe;
                float size = 44f * p, pad = 8f * p;
                return new Rect(safe.xMax - 2f * size - 2f * pad, safe.y + pad, size, size);
            }
        }

        Rect InspectBackRect
        {
            get
            {
                float p = Units.DpToPx(1f);
                Rect safe = Safe;
                float w = Mathf.Min(safe.width * 0.7f, 280f * p), h = 56f * p;
                return new Rect(safe.center.x - w / 2f, safe.yMax - h - 18f * p, w, h);
            }
        }

        int Px(float dp) { return Mathf.RoundToInt(dp * u); }

        void EnsureStyles()
        {
            float nu = Units.DpToPx(1f);
            if (title != null && Mathf.Approximately(nu, u)) return;
            u = nu;
            var display = ToyStyle.Display;
            title = new GUIStyle(GUI.skin.label) { font = display, fontSize = Px(34), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
            head = new GUIStyle(title) { fontSize = Px(20) };
            body = new GUIStyle(title) { fontSize = Px(15) };
            small = new GUIStyle(title) { fontSize = Px(12) };
            label = new GUIStyle(GUI.skin.label) { font = ToyStyle.Body, fontSize = Px(13), alignment = TextAnchor.MiddleLeft, wordWrap = true };
            label.normal.textColor = ToyGui.TextCream;
            button = new GUIStyle(title) { fontSize = Px(16) };
            bigButton = new GUIStyle(title) { fontSize = Px(28) };
            tile = new GUIStyle(title) { fontSize = Px(18) };
        }

        Progression Prog { get { return Proto.Flow != null ? Proto.Flow.Progress : null; } }

        bool Btn(Rect r, string text, ToyTone tone, GUIStyle style, ToySize size = ToySize.Small)
        {
            bool c = ToyGui.Button(r, text, tone, style, null, size);
            return c && !suppressClicks;
        }

        void Backdrop(Color wall)
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            GUI.color = wall;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            float step = 36f * u;
            for (float x = 0; x < Screen.width; x += step * 2f)
                GUI.DrawTexture(new Rect(x, 0, step, Screen.height), Texture2D.whiteTexture);
            GUI.color = ToyStyle.FloorColor;
            GUI.DrawTexture(new Rect(0, Screen.height - 40f * u, Screen.width, 40f * u), Texture2D.whiteTexture);
            GUI.color = old;
        }

        Sprite SpriteFor(string id)
        {
            Sprite s;
            if (spriteCache.TryGetValue(id, out s)) return s;
            var def = ObjectLibrary.Get(id);
            Vector2[] hull;
            if (def == null || !ObjectArt.TryGet(def, out s, out hull)) s = null;
            spriteCache[id] = s;
            return s;
        }

        void DrawSprite(Rect r, string id, Color tint)
        {
            if (Event.current.type != EventType.Repaint) return;
            var s = SpriteFor(id);
            if (s == null) return;
            var tex = s.texture;
            Rect tr = s.textureRect;
            float aspect = tr.width / tr.height;
            Rect fit = r;
            if (aspect > r.width / r.height) { fit.height = r.width / aspect; fit.y += (r.height - fit.height) / 2f; }
            else { fit.width = r.height * aspect; fit.x += (r.width - fit.width) / 2f; }
            var old = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(fit, tex, new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height), true);
            GUI.color = old;
        }

        void Bar(Rect r, float t, Color fill)
        {
            ToyGui.Pill(r, ToyTone.Inactive, ToySize.Small);
            if (t <= 0f || Event.current.type != EventType.Repaint) return;
            float inset = 4f * u;
            var inner = new Rect(r.x + inset, r.y + inset, Mathf.Max(r.height - 2f * inset, (r.width - 2f * inset) * Mathf.Clamp01(t)),
                r.height - 2f * inset - ToyGui.Depth(ToySize.Small));
            var old = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(inner, Texture2D.whiteTexture);
            GUI.color = old;
        }

        string LevelName(LevelDef l)
        {
            var c = Proto.Flow.Campaign;
            var r = c.Room(l.roomId);
            return (r != null ? r.name + " " + l.section : l.id) + " · " + l.title;
        }

        // ---- frame ------------------------------------------------------------------------------

        void Update()
        {
            // Touch-drag scrolling for tall screens; a drag never counts as a tap.
            if (!MenuOpen) { touchScrolling = false; suppressClicks = false; return; }
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { touchStart = t.position; touchScrolling = false; suppressClicks = false; }
                else if (t.phase == TouchPhase.Moved)
                {
                    if (!touchScrolling && (t.position - touchStart).magnitude > 12f * Units.DpToPx(1f)) touchScrolling = suppressClicks = true;
                    if (touchScrolling) scrollY = Mathf.Clamp(scrollY + t.deltaPosition.y, 0f, Mathf.Max(0f, contentH - viewH));
                }
            }
            else if (Input.touchCount == 0 && touchScrolling)
            {
                touchScrolling = false;
            }
            else if (Input.touchCount == 0 && suppressClicks && !Input.GetMouseButton(0)) suppressClicks = false;
        }

        void OnGUI()
        {
            if (Proto.Flow == null || Proto.Flow.Campaign == null) return;
            ToyGui.Begin(Units.DpToPx(1f));
            EnsureStyles();
            GUI.depth = 1; // under the observer panel (Hud draws at depth 0)
            if (Event.current.type == EventType.ScrollWheel && MenuOpen)
                scrollY = Mathf.Clamp(scrollY + Event.current.delta.y * 20f * u, 0f, Mathf.Max(0f, contentH - viewH));

            switch (Current)
            {
                case MenuScreen.Home: DrawHome(); return;
                case MenuScreen.House: DrawHouse(); return;
                case MenuScreen.Room: DrawRoom(); return;
                case MenuScreen.Settings: DrawSettings(); return;
                case MenuScreen.Collection: DrawCollection(); return;
                case MenuScreen.Records: DrawRecords(); return;
            }

            var d = Proto.Director;
            if (d == null || d.Level == null) return;
            if (Inspecting)
            {
                if (Btn(InspectBackRect, "BACK TO RESULT", ToyTone.Purple, button, ToySize.Medium)) SetInspecting(false);
                return;
            }
            if (Paused) { DrawPause(); return; }
            if (!d.RoundComplete && Btn(PauseRect, "II", ToyTone.Secondary, button, ToySize.Medium)) Pause();
        }

        // ---- Home -------------------------------------------------------------------------------

        void DrawHome()
        {
            var p = Prog;
            var c = Proto.Flow.Campaign;
            Backdrop(ToyStyle.Hex("FCE7C8"));
            Rect safe = Safe;
            float x = safe.x + 16f * u, w = safe.width - 32f * u, y = safe.y + 16f * u;

            CoinsPill(new Vector2(x, y));
            y += 56f * u;
            ToyGui.Logo(new Rect(x, y, w, 50f * u), "SORT", title, ToyGui.TextCream, ToyGui.TextDepthWarm, 4.4f, 6f);
            y += 46f * u;
            ToyGui.Logo(new Rect(x, y, w, 50f * u), "EVERYTHING", title, ToyGui.TextCream, ToyGui.TextDepthWarm, 4.4f, 6f);
            y += 70f * u;

            // Whole-house restoration.
            int restored, planned;
            p.HouseProgress(out restored, out planned);
            ToyGui.Text(new Rect(x, y, w, 24f * u), "YOUR HOUSE  " + restored + " / " + planned + " AREAS RESTORED", small, ToyGui.TextCream);
            y += 26f * u;
            Bar(new Rect(x, y, w, 26f * u), planned > 0 ? (float)restored / planned : 0f, ToyStyle.Reward);
            y += 44f * u;

            // The next action.
            var resume = p.Save.resume != null ? c.Get(p.Save.resume.levelId) : null;
            bool resumable = resume != null && p.Save.resume.started;
            var next = resumable ? resume : p.NextUnfinished();
            var card = new Rect(x, y, w, 190f * u);
            ToyGui.Card(card);
            if (next != null)
            {
                var r = c.Room(next.roomId);
                int done = r != null ? p.CompletedInRoom(r) : 0;
                int total = r != null ? c.LevelsInRoom(r.id).Count : 0;
                ToyGui.Text(new Rect(card.x, card.y + 14f * u, card.width, 26f * u), (r != null ? r.name.ToUpperInvariant() : "") + "  " + done + " / " + total, body, ToyGui.TextCream);
                ToyGui.Text(new Rect(card.x + 12f * u, card.y + 42f * u, card.width - 24f * u, 30f * u), next.title, head, ToyStyle.Hex("FFE58A"));
                var br = new Rect(card.x + 20f * u, card.y + 90f * u, card.width - 40f * u, 76f * u);
                if (Btn(br, resumable ? "RESUME" : "CONTINUE", ToyTone.Primary, bigButton, ToySize.Large)) Proto.Flow.ContinueFromHome();
            }
            else
            {
                ToyGui.Text(new Rect(card.x, card.y + 24f * u, card.width, 30f * u), "EVERY BUILT ROOM IS ORGANIZED!", body, ToyStyle.Hex("FFE58A"));
                ToyGui.Text(new Rect(card.x, card.y + 54f * u, card.width, 24f * u), "More rooms are coming soon.", small, ToyGui.TextCream);
                var br = new Rect(card.x + 20f * u, card.y + 90f * u, card.width - 40f * u, 76f * u);
                if (Btn(br, "SEE THE HOUSE", ToyTone.Reward, bigButton, ToySize.Large)) Show(MenuScreen.House);
            }
            y = card.yMax + 18f * u;

            float bw = (w - 12f * u) / 2f, bh = 52f * u;
            if (Btn(new Rect(x, y, bw, bh), "HOUSE", ToyTone.Purple, button, ToySize.Medium)) Show(MenuScreen.House);
            if (Btn(new Rect(x + bw + 12f * u, y, bw, bh), "COLLECTION", ToyTone.Secondary, button, ToySize.Medium)) Show(MenuScreen.Collection);
            y += bh + 12f * u;
            if (Btn(new Rect(x, y, bw, bh), "RECORDS", ToyTone.Secondary, button, ToySize.Medium)) Show(MenuScreen.Records);
            if (Btn(new Rect(x + bw + 12f * u, y, bw, bh), "SETTINGS", ToyTone.Inactive, button, ToySize.Medium)) OpenSettings(false);
        }

        void CoinsPill(Vector2 at)
        {
            string text = Prog.Save.coins + " COINS";
            float w = button.CalcSize(new GUIContent(text)).x + 30f * u;
            var r = new Rect(at.x, at.y, w, 40f * u);
            ToyGui.Pill(r, ToyTone.Reward, ToySize.Medium);
            ToyGui.Text(new Rect(r.x, r.y - ToyGui.Depth(ToySize.Medium) * 0.5f, r.width, r.height), text, button, ToyGui.TextCream);
        }

        void Header(string text, MenuScreen back, out float y, out float x, out float w)
        {
            Rect safe = Safe;
            x = safe.x + 16f * u;
            w = safe.width - 32f * u;
            y = safe.y + 12f * u;
            if (Btn(new Rect(x, y, 84f * u, 44f * u), "< BACK", ToyTone.Inactive, small, ToySize.Medium)) Show(back);
            ToyGui.Logo(new Rect(x + 84f * u, y - 2f * u, w - 168f * u, 44f * u), text, head, ToyGui.TextCream, ToyGui.TextDepthWarm, 3f, 3f, false);
            y += 60f * u;
        }

        void BeginScroll(float top)
        {
            Rect safe = Safe;
            viewH = safe.yMax - top;
            GUI.BeginGroup(new Rect(0, top, Screen.width, viewH));
        }

        void EndScroll(float contentHeight)
        {
            GUI.EndGroup();
            contentH = contentHeight;
            scrollY = Mathf.Clamp(scrollY, 0f, Mathf.Max(0f, contentH - viewH));
        }

        // ---- House overview ---------------------------------------------------------------------

        void DrawHouse()
        {
            var p = Prog;
            var c = Proto.Flow.Campaign;
            Backdrop(ToyStyle.Hex("DDEBFF"));
            float x, y, w;
            Header("YOUR HOUSE", MenuScreen.Home, out y, out x, out w);

            int restored, planned;
            p.HouseProgress(out restored, out planned);
            Bar(new Rect(x, y, w, 26f * u), planned > 0 ? (float)restored / planned : 0f, ToyStyle.Reward);
            ToyGui.Text(new Rect(x, y - 2f * u, w, 26f * u), restored + " / " + planned + " AREAS", small, ToyGui.TextCream);
            y += 40f * u;

            BeginScroll(y);
            float cy = -scrollY;
            float gap = 12f * u, cw = (w - gap) / 2f, ch = 176f * u;
            var rooms = c.House.rooms;
            for (int i = 0; i < rooms.Length; i++)
            {
                var r = rooms[i];
                var rect = new Rect(x + (i % 2) * (cw + gap), cy + (i / 2) * (ch + gap), cw, ch);
                RoomCard(rect, r, p);
            }
            EndScroll(((rooms.Length + 1) / 2) * (ch + gap) + 20f * u);
        }

        void RoomCard(Rect r, RoomDef room, Progression p)
        {
            bool unlocked = p.IsRoomUnlocked(room);
            bool complete = p.IsRoomComplete(room);
            int areas = room.available ? room.areas.Length : room.plannedAreas;
            int restored = room.available ? p.RestoredAreas(room) : 0;

            // Card face: the room's wall colour, brighter as areas are restored; gold rim once finished.
            if (complete) ToyGui.Pill(new Rect(r.x - 4f * u, r.y - 4f * u, r.width + 8f * u, r.height + 8f * u), ToyTone.Reward, ToySize.Medium);
            ToyGui.Pill(r, unlocked ? ToyTone.Paper : ToyTone.Inactive, ToySize.Medium);
            if (Event.current.type == EventType.Repaint)
            {
                var old = GUI.color;
                Color wall = ToyStyle.Hex(room.theme.wallHex);
                float clean = areas > 0 ? (float)restored / areas : 0f;
                GUI.color = unlocked ? Color.Lerp(Color.Lerp(wall, ToyStyle.Inactive, 0.35f), wall, clean) : new Color(0.75f, 0.72f, 0.82f);
                GUI.DrawTexture(new Rect(r.x + 8f * u, r.y + 8f * u, r.width - 16f * u, r.height * 0.5f), Texture2D.whiteTexture);
                GUI.color = old;
            }
            // Icons: the room's own objects (dimmed until the room is unlocked).
            int n = room.icons.Length;
            float isz = Mathf.Min(44f * u, (r.width - 24f * u) / Mathf.Max(1, n));
            for (int i = 0; i < n; i++)
            {
                var ir = new Rect(r.center.x - n * isz / 2f + i * isz, r.y + 18f * u, isz, isz);
                DrawSprite(ir, room.icons[i], unlocked ? Color.white : new Color(0.15f, 0.12f, 0.25f, 0.35f));
            }
            ToyGui.Text(new Rect(r.x, r.y + r.height * 0.5f + 6f * u, r.width, 24f * u), room.name.ToUpperInvariant(), body,
                unlocked ? ToyGui.TextCream : ToyStyle.Hex("E8E4F4"));

            // Area pips: one per area, gold when restored.
            float pip = 14f * u, py = r.y + r.height * 0.5f + 36f * u;
            for (int i = 0; i < areas; i++)
            {
                var pr = new Rect(r.center.x - areas * (pip + 6f * u) / 2f + i * (pip + 6f * u), py, pip, pip);
                ToyGui.Pill(pr, i < restored ? ToyTone.Reward : ToyTone.Inactive, ToySize.Small);
            }

            string status = !room.available ? "COMING SOON" : complete ? (room.masterName ?? "DONE").ToUpperInvariant()
                : !unlocked ? "LOCKED" : p.CompletedInRoom(room) + " / " + Proto.Flow.Campaign.LevelsInRoom(room.id).Count;
            ToyGui.Text(new Rect(r.x, r.yMax - 40f * u, r.width, 22f * u), status, small, complete ? ToyStyle.Hex("FFE58A") : ToyGui.TextCream);

            if (unlocked && GUI.Button(r, GUIContent.none, GUIStyle.none) && !suppressClicks) Show(MenuScreen.Room, room);
        }

        // ---- Room → area → section --------------------------------------------------------------

        void DrawRoom()
        {
            var p = Prog;
            var c = Proto.Flow.Campaign;
            if (room == null) { Show(MenuScreen.House); return; }
            Backdrop(ToyStyle.Hex(room.theme.wallHex));
            float x, y, w;
            Header(room.name.ToUpperInvariant(), MenuScreen.House, out y, out x, out w);
            int done = p.CompletedInRoom(room), total = c.LevelsInRoom(room.id).Count;
            Bar(new Rect(x, y, w, 26f * u), total > 0 ? (float)done / total : 0f, ToyStyle.Primary);
            ToyGui.Text(new Rect(x, y - 2f * u, w, 26f * u), done + " / " + total + " SECTIONS", small, ToyGui.TextCream);
            y += 40f * u;

            BeginScroll(y);
            float cy = -scrollY;
            const int perRow = 5;
            float gap = 8f * u, ts = Mathf.Min(64f * u, (w - (perRow - 1) * gap) / perRow);
            foreach (var area in room.areas)
            {
                bool restored = p.IsAreaRestored(area);
                var hr = new Rect(x, cy, w, 44f * u);
                ToyGui.Pill(hr, restored ? ToyTone.Reward : ToyTone.Purple, ToySize.Small);
                string name = area.name.ToUpperInvariant();
                ToyGui.Text(new Rect(hr.x + 12f * u, hr.y - 3f * u, hr.width * 0.62f, hr.height), name, body, ToyGui.TextCream);
                // Optional appearance variant for a restored area (coins; never required).
                if (restored && area.variantPrice > 0)
                {
                    var vr = new Rect(hr.xMax - 128f * u, hr.y + 5f * u, 120f * u, 30f * u);
                    if (p.Save.ownedVariants.Contains(area.id))
                    {
                        bool on;
                        p.Save.variantEquipped.TryGetValue(area.id, out on);
                        if (Btn(vr, on ? (area.variantName ?? "STYLE") + ": ON" : "STYLE: OFF", on ? ToyTone.Primary : ToyTone.Inactive, small))
                        {
                            p.Save.variantEquipped[area.id] = !on;
                            Proto.Flow.SaveSettings();
                        }
                    }
                    else if (Btn(vr, area.variantPrice + " COINS", p.Save.coins >= area.variantPrice ? ToyTone.Secondary : ToyTone.Inactive, small))
                    {
                        if (p.TryBuyVariant(area)) Proto.Flow.SaveSettings();
                    }
                }
                cy += 52f * u;
                for (int i = 0; i < area.sections.Length; i++)
                {
                    var level = c.Get(area.sections[i]);
                    if (level == null) continue;
                    int col = i % perRow;
                    if (i > 0 && col == 0) cy += ts + gap;
                    var tr = new Rect(x + col * (ts + gap), cy, ts, ts);
                    bool unlockedLevel = p.IsUnlocked(level), completed = p.IsCompleted(level);
                    var tone = completed ? ToyTone.Primary : unlockedLevel ? ToyTone.Secondary : ToyTone.Inactive;
                    string text = unlockedLevel ? level.section.ToString() : "·";
                    if (Btn(tr, text, tone, tile, ToySize.Medium) && unlockedLevel) Proto.Flow.PlayFromMenu(level);
                    if (completed) ToyGui.Pill(new Rect(tr.xMax - 16f * u, tr.y - 4f * u, 18f * u, 18f * u), ToyTone.Reward, ToySize.Small);
                }
                cy += ts + 18f * u;
            }
            EndScroll(cy + scrollY + 20f * u);
        }

        // ---- Pause ------------------------------------------------------------------------------

        void DrawPause()
        {
            var d = Proto.Director;
            ToyGui.Dim(0.5f);
            Rect safe = Safe;
            float w = Mathf.Min(safe.width - 40f * u, 340f * u), h = 420f * u;
            var card = new Rect(safe.center.x - w / 2f, safe.center.y - h / 2f, w, h);
            ToyGui.Card(card);
            ToyGui.Logo(new Rect(card.x, card.y + 16f * u, card.width, 40f * u), "PAUSED", head, ToyGui.TextCream, ToyGui.TextDepthWarm, 3f, 3f, false);
            ToyGui.Text(new Rect(card.x + 12f * u, card.y + 58f * u, card.width - 24f * u, 24f * u), d.Level.title, small, ToyStyle.Hex("FFE58A"));
            float bx = card.x + 24f * u, bw = card.width - 48f * u, bh = 54f * u, y = card.y + 98f * u;
            if (Btn(new Rect(bx, y, bw, bh), "RESUME", ToyTone.Primary, button, ToySize.Medium)) Resume();
            y += bh + 10f * u;
            if (Btn(new Rect(bx, y, bw, bh), "RESTART", ToyTone.Secondary, button, ToySize.Medium)) { Resume(); Proto.Flow.Restart(); }
            y += bh + 10f * u;
            var room = Proto.Flow.Campaign.Room(d.Level.roomId);
            if (room != null && Btn(new Rect(bx, y, bw, bh), "SECTIONS", ToyTone.Purple, button, ToySize.Medium)) Proto.Flow.GoToMenu(MenuScreen.Room, room);
            y += bh + 10f * u;
            float hw = (bw - 10f * u) / 2f;
            if (Btn(new Rect(bx, y, hw, bh), "HOME", ToyTone.Purple, button, ToySize.Medium)) Proto.Flow.GoToMenu(MenuScreen.Home);
            if (Btn(new Rect(bx + hw + 10f * u, y, hw, bh), "SETTINGS", ToyTone.Inactive, button, ToySize.Medium)) OpenSettings(true);
        }

        // ---- Settings ---------------------------------------------------------------------------

        void OpenSettings(bool fromPause)
        {
            settingsFromPause = fromPause;
            backFromSettings = Current == MenuScreen.None ? MenuScreen.Home : Current;
            if (fromPause)
            {
                // Settings over a paused level: keep the board, show the settings card instead of the pause card.
                Paused = false;
                Current = MenuScreen.Settings;
                if (Proto.Director != null) Proto.Director.ExternallyPaused = true;
            }
            else Show(MenuScreen.Settings);
        }

        void DrawSettings()
        {
            if (settingsFromPause) ToyGui.Dim(0.55f);
            else Backdrop(ToyStyle.Hex("EDE4F7"));
            Rect safe = Safe;
            float x = safe.x + 16f * u, w = safe.width - 32f * u, y = safe.y + 12f * u;
            if (Btn(new Rect(x, y, 84f * u, 44f * u), "< BACK", ToyTone.Inactive, small, ToySize.Medium))
            {
                if (settingsFromPause) { Current = MenuScreen.None; Pause(); }
                else Show(backFromSettings);
                return;
            }
            ToyGui.Logo(new Rect(x + 84f * u, y - 2f * u, w - 168f * u, 44f * u), "SETTINGS", head, ToyGui.TextCream, ToyGui.TextDepthWarm, 3f, 3f, false);
            y += 70f * u;
            var cfg = Proto.Config;
            bool changed = false;
            changed |= Toggle(ref y, x, w, "MASTERY", "Show active time and personal bests. Never affects rewards.", ref PlayerSettings.Mastery);
            changed |= Toggle(ref y, x, w, "REDUCED MOTION", "Shorter, calmer movement.", ref PlayerSettings.ReducedMotion);
            bool sound = cfg.sound, haptics = cfg.haptics;
            if (Toggle(ref y, x, w, "SOUND", null, ref sound)) { cfg.sound = sound; FeelConfig.Save(Proto.Variant, cfg, Proto.CustomTuning); }
            if (Toggle(ref y, x, w, "HAPTICS", null, ref haptics)) { cfg.haptics = haptics; FeelConfig.Save(Proto.Variant, cfg, Proto.CustomTuning); }
            if (changed) PlayerSettings.Save();
        }

        bool Toggle(ref float y, float x, float w, string name, string note, ref bool v)
        {
            float bh = 52f * u;
            ToyGui.Text(new Rect(x, y, w * 0.6f, bh), name, body, ToyGui.TextCream);
            bool clicked = Btn(new Rect(x + w - 110f * u, y + 4f * u, 110f * u, bh - 8f * u), v ? "ON" : "OFF", v ? ToyTone.Primary : ToyTone.Inactive, button, ToySize.Medium);
            y += bh;
            if (note != null)
            {
                GUI.Label(new Rect(x, y - 6f * u, w, 34f * u), note, label);
                y += 30f * u;
            }
            y += 8f * u;
            if (clicked) v = !v;
            return clicked;
        }

        // ---- Collection and Records -------------------------------------------------------------

        List<string> collectionIds;

        void DrawCollection()
        {
            var p = Prog;
            var c = Proto.Flow.Campaign;
            if (collectionIds == null)
            {
                collectionIds = new List<string>();
                foreach (var l in c.Levels) foreach (var b in l.boards) foreach (var o in b.objects)
                    if (!collectionIds.Contains(o.asset)) collectionIds.Add(o.asset);
            }
            Backdrop(ToyStyle.Hex("E3EEDD"));
            float x, y, w;
            Header("COLLECTION", MenuScreen.Home, out y, out x, out w);
            int found = 0;
            foreach (var id in collectionIds) if (p.Save.discoveries.Contains(id)) found++;
            ToyGui.Text(new Rect(x, y, w, 24f * u), found + " / " + collectionIds.Count + " OBJECTS FOUND", small, ToyGui.TextCream);
            y += 34f * u;
            BeginScroll(y);
            const int perRow = 4;
            float gap = 10f * u, cs = (w - (perRow - 1) * gap) / perRow, cy = -scrollY;
            for (int i = 0; i < collectionIds.Count; i++)
            {
                var r = new Rect(x + (i % perRow) * (cs + gap), cy + (i / perRow) * (cs + 22f * u + gap), cs, cs);
                bool has = p.Save.discoveries.Contains(collectionIds[i]);
                ToyGui.Pill(r, has ? ToyTone.Paper : ToyTone.Inactive, ToySize.Small);
                DrawSprite(new Rect(r.x + 10f * u, r.y + 8f * u, r.width - 20f * u, r.height - 20f * u), collectionIds[i],
                    has ? Color.white : new Color(0.15f, 0.12f, 0.25f, 0.4f));
                var def = ObjectLibrary.Get(collectionIds[i]);
                ToyGui.Text(new Rect(r.x - 6f * u, r.yMax, r.width + 12f * u, 20f * u), has && def != null ? def.displayName : "?", small, ToyGui.TextCream, 1.6f, 1f);
            }
            EndScroll(((collectionIds.Count + perRow - 1) / perRow) * (cs + 22f * u + gap) + 20f * u);
        }

        void DrawRecords()
        {
            var p = Prog;
            var c = Proto.Flow.Campaign;
            Backdrop(ToyStyle.Hex("D8F3F1"));
            float x, y, w;
            Header("RECORDS", MenuScreen.Home, out y, out x, out w);
            if (!PlayerSettings.Mastery)
            {
                GUI.Label(new Rect(x, y, w, 40f * u), "Records are optional. Turn on Mastery in Settings to time your clears; time never affects rewards or unlocks.", label);
                y += 48f * u;
            }
            BeginScroll(y);
            float cy = -scrollY, rh = 34f * u;
            int shown = 0;
            foreach (var l in c.Levels)
            {
                float best;
                if (!p.Save.bestTimes.TryGetValue(l.id, out best)) continue;
                ToyGui.Text(new Rect(x, cy, w * 0.72f, rh), LevelName(l), small, ToyGui.TextCream);
                var tt = Proto.Director.TimerText ?? new TimerText(3600f);
                ToyGui.Text(new Rect(x + w * 0.72f, cy, w * 0.28f, rh), tt.For((int)System.Math.Floor(best * 10f + 1e-3f)) + " s", small, ToyStyle.Hex("FFE58A"));
                cy += rh;
                shown++;
            }
            if (shown == 0) ToyGui.Text(new Rect(x, cy, w, rh), "NO RECORDS YET", small, ToyGui.TextCream);
            EndScroll(shown * rh + 40f * u);
        }
    }
}
