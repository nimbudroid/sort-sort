using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SortEverything.Domain;
using SortEverything.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>Applies Screen.safeArea as normalized anchors. All gameplay UI lives inside it.</summary>
    public sealed class SafeAreaRoot : MonoBehaviour
    {
        Rect _last;
        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == _last || Screen.width == 0 || Screen.height == 0) return;
            _last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Bootstrap and screen controller. Owns exactly one primary screen and at most one modal.
    /// Navigation cancels drag, flushes the snapshot and switches only after durable success.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        public Camera UiCamera;
        public Canvas Canvas;
        public RectTransform SafeArea;

        public GameSession Session { get; private set; }
        public AudioFeedback Audio { get; private set; }
        RectTransform _screenLayer, _modalLayer, _toastLayer;
        BoardScreen _board;
        string _lastBrowseScreen = "area";
        bool _laterFilter;
        bool _navigating;
        readonly Dictionary<string, string> _projectPreview = new Dictionary<string, string>();

        public SaveData Save => Session.Save;
        public Catalogue Catalogue => Session.Catalogue;
        public float ScreenHeight => SafeArea.rect.height > 1 ? SafeArea.rect.height : 780f;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            EnsureSceneObjects();
        }

        void Start() => StartCoroutine(Boot());

        void EnsureSceneObjects()
        {
            if (UiCamera == null)
            {
                var camGo = new GameObject("UICamera");
                UiCamera = camGo.AddComponent<Camera>();
                UiCamera.orthographic = true;
                UiCamera.clearFlags = CameraClearFlags.SolidColor;
                UiCamera.backgroundColor = UIKit.Wall;
                UiCamera.transform.position = new Vector3(0, 0, -10);
            }
            if (Canvas == null)
            {
                var go = new GameObject("Canvas", typeof(RectTransform));
                Canvas = go.AddComponent<Canvas>();
                Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                Canvas.worldCamera = UiCamera;
                Canvas.planeDistance = 5;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(360, 780);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
            }
            if (SafeArea == null)
            {
                SafeArea = UIKit.Rect(Canvas.transform, "SafeAreaRoot").Stretch();
                SafeArea.gameObject.AddComponent<SafeAreaRoot>();
            }
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
            var bg = UIKit.Panel(SafeArea, "Background", UIKit.Wall, 0);
            bg.rectTransform.Stretch(0, -200, 0, -200);
            _screenLayer = UIKit.Rect(SafeArea, "Screens").Stretch();
            _modalLayer = UIKit.Rect(SafeArea, "Modals").Stretch();
            _toastLayer = UIKit.Rect(SafeArea, "Toasts").Stretch();
            Audio = gameObject.AddComponent<AudioFeedback>();
        }

        IEnumerator Boot()
        {
            yield return null; // let the safe area settle
            var config = GameConfig.LoadFromResources();
            var catalogue = Catalogue.CreateKitchen(config.buildProfileId);
            var store = FileSaveStore.Default();
            var telemetry = new Telemetry(System.IO.Path.Combine(Application.persistentDataPath, "SortEverything", "telemetry.jsonl"), config.telemetryEnabled)
            {
                BuildInfo = Application.version + "/content" + KitchenCatalogue.ContentVersion + "/" + config.buildProfileId,
            };
            Session = new GameSession(catalogue, config, store, telemetry);
            var outcome = Session.Boot(out var diag);
            if (outcome == SaveLoadOutcome.AllCorrupt)
            {
                ShowCorruptModal(diag, store);
                yield break;
            }
            telemetry.ParticipantId = Save.participantId;
            telemetry.Emit("session_start", "\"entry\":\"" + outcome + "\"");
            Audio.Init(Save.settings);
            if (outcome == SaveLoadOutcome.NewSave) OpenLevel(KitchenCatalogue.LevelId(1), BoardScreen.EntryMode.Default);
            else ShowHome();
        }

        void OnApplicationQuit() => Session?.Telemetry?.Emit("session_end", "\"reason\":\"quit\"");

        // ---------- Navigation ----------

        void ClearScreen()
        {
            if (_board != null) { _board.Unload(); _board = null; }
            UIKit.Clear(_screenLayer);
            CloseModal();
        }

        public void OpenLevel(string levelId, BoardScreen.EntryMode mode)
        {
            if (_navigating) return;
            var level = Catalogue.Level(levelId);
            if (level == null || !Catalogue.IsInstalled(levelId)) { ShowHome(); return; }
            ClearScreen();
            var rt = UIKit.Rect(_screenLayer, "BoardScreen").Stretch();
            _board = rt.gameObject.AddComponent<BoardScreen>();
            _board.Init(this, level, mode);
        }

        /// <summary>Opens a scene from a list: resumes its saved attempt or starts a first/replay attempt.</summary>
        public void OpenFromList(LevelDef level)
        {
            if (Progression.HasLaterOptionalBoard(Save, level))
            {
                ShowChoice(Strings.Get(level.PurposeKey),
                    (Strings.Get("action.resume_puzzle"), () => OpenLevel(level.LevelId, BoardScreen.EntryMode.ResumePuzzle)),
                    (Strings.Get("action.replay_finish"), () => OpenLevel(level.LevelId, BoardScreen.EntryMode.ReplayFinish)));
                return;
            }
            OpenLevel(level.LevelId, BoardScreen.EntryMode.Default);
        }

        public void ShowHome()
        {
            ClearScreen();
            Session.Telemetry.Emit("project_view", "\"anchors\":" + Save.anchorIds.Count);
            var root = UIKit.Rect(_screenLayer, "Home").Stretch();
            var header = UIKit.Rect(root, "Header").TopLeft(12, 8, 336, 40);
            var title = UIKit.Label(header, "Title", "Sort Everything", 22, UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.Stretch(0, 0, 110, 0);
            CoinChip(header, 336);

            var list = UIKit.ScrollList(root, "HomeList", 10);
            ((RectTransform)list.parent).Stretch(12, 54, 12, 8);

            // Resume card or Continue.
            var selected = Save.Attempt(Save.selectedAttemptId);
            var selectedLevel = selected != null ? Catalogue.Level(selected.levelId) : null;
            if (selectedLevel != null && Catalogue.IsInstalled(selectedLevel.LevelId))
            {
                Card(list, "Resume", $"{Strings.Get(selectedLevel.PurposeKey)}", $"{Strings.Get("status.in_progress")} · Kitchen {selectedLevel.OrdinalInArea}",
                    Strings.Get("action.resume"), () => OpenFromList(selectedLevel), primary: true);
            }
            var cursor = Progression.CursorLevel(Save, Catalogue, KitchenCatalogue.AreaId);
            if (cursor != null && (selectedLevel == null || cursor.LevelId != selectedLevel.LevelId))
            {
                Card(list, "Continue", Strings.Get(cursor.PurposeKey), $"Next task · Kitchen {cursor.OrdinalInArea}",
                    Strings.Get("action.continue"), () => OpenFromList(cursor), primary: selectedLevel == null);
            }
            else if (cursor == null && selectedLevel == null)
            {
                bool ready = Save.recognitions.Contains(Progression.KitchenReady);
                bool sample = !ready;
                Card(list, "Done", ready ? Strings.Get("result.kitchen_ready") : Strings.Get("result.sample_complete"),
                    sample ? "Every installed kitchen task has been visited." : "Your kitchen is restored. Replay any scene or try a saved puzzle.",
                    Strings.Get("action.replay_scenes"), ShowArea, primary: true);
            }

            // House overview / kitchen project.
            var overviewHolder = UIKit.Rect(list, "OverviewHolder");
            overviewHolder.Size(206);
            var lbl = UIKit.Label(overviewHolder, "Label", $"{Strings.Get("destination.house")} · {Strings.Get("area.house_kitchen")} — {CompletedCount()}/{Catalogue.InstalledInArea(KitchenCatalogue.AreaId).Count} scenes sorted", 12, UIKit.InkSoft, TextAnchor.MiddleLeft);
            lbl.rectTransform.TopLeft(2, 0, 330, 18);
            var ov = KitchenOverviewView.Build(overviewHolder, Save, Catalogue, 336, 184);
            ov.TopLeft(0, 20, 336, 184);

            var later = Progression.LaterPuzzles(Save, Catalogue, KitchenCatalogue.AreaId).ToList();
            var row1 = UIKit.Rect(list, "Row1"); row1.Size(46); UIKit.HStack(row1, 8);
            UIKit.Button(row1, "Kitchen", "Kitchen scenes", () => { _laterFilter = false; ShowArea(); });
            UIKit.Button(row1, "Later", later.Count > 0 ? $"{Strings.Get("action.later_puzzles")} ({later.Count})" : Strings.Get("action.later_puzzles"), () => { _laterFilter = true; ShowArea(); });
            var row2 = UIKit.Rect(list, "Row2"); row2.Size(46); UIKit.HStack(row2, 8);
            UIKit.Button(row2, "Project", "Kitchen Project", ShowProject);
            UIKit.Button(row2, "Collection", "Collection", ShowCollection);
            var row3 = UIKit.Rect(list, "Row3"); row3.Size(46); UIKit.HStack(row3, 8);
            UIKit.Button(row3, "Records", "Records", ShowRecords);
            UIKit.Button(row3, "Settings", Strings.Get("action.settings"), ShowSettings);
        }

        int CompletedCount() => Catalogue.InstalledInArea(KitchenCatalogue.AreaId).Count(l => Save.HasSeal(l.LevelId));

        void CoinChip(RectTransform parent, float parentWidth)
        {
            var chip = UIKit.Panel(parent, "Coins", Color.white, 14);
            chip.rectTransform.TopLeft(parentWidth - 104, 4, 104, 32);
            var ic = UIKit.Icon(chip.transform, "Coin", SpriteFactory.Circle(), 18, UIKit.Gold);
            ic.rectTransform.TopLeft(8, 7, 18, 18);
            var t = UIKit.Label(chip.transform, "Amount", Save.wallet.ToString("N0"), 15, UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.TopLeft(32, 0, 68, 32);
        }

        void Card(RectTransform list, string name, string title, string subtitle, string action, Action onClick, bool primary)
        {
            var card = UIKit.Panel(list, name, primary ? SpriteFactory.Hex("#FFFFFF") : SpriteFactory.Hex("#FBF5EC"), 16);
            card.Size(88);
            var t = UIKit.Label(card.transform, "Title", title, 18, UIKit.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            t.rectTransform.TopLeft(14, 10, 200, 26);
            var s = UIKit.Label(card.transform, "Sub", subtitle, 12, UIKit.InkSoft, TextAnchor.UpperLeft);
            s.rectTransform.TopLeft(14, 38, 200, 40);
            var b = UIKit.Button(card.transform, "Action", action, () => onClick(), primary);
            ((RectTransform)b.transform).TopLeft(218, 20, 110, 48);
        }

        public void ShowArea()
        {
            ClearScreen();
            _lastBrowseScreen = "area";
            var root = UIKit.Rect(_screenLayer, "Area").Stretch();
            var top = UIKit.Rect(root, "Top").TopLeft(12, 8, 336, 48);
            var back = UIKit.Button(top, "Home", "‹ " + Strings.Get("action.home"), ShowHome);
            ((RectTransform)back.transform).TopLeft(0, 2, 84, 44);
            var title = UIKit.Label(top, "Title", Strings.Get("area.house_kitchen"), 20, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.TopLeft(90, 0, 156, 48);
            var filter = UIKit.Button(top, "Filter", _laterFilter ? "All scenes" : Strings.Get("action.later_puzzles"), () => { _laterFilter = !_laterFilter; ShowArea(); });
            ((RectTransform)filter.transform).TopLeft(252, 2, 84, 44);

            var list = UIKit.ScrollList(root, "Scenes", 6);
            ((RectTransform)list.parent).Stretch(12, 62, 12, 8);
            var levels = _laterFilter ? Progression.LaterPuzzles(Save, Catalogue, KitchenCatalogue.AreaId).ToList() : Catalogue.InstalledInArea(KitchenCatalogue.AreaId).ToList();
            if (levels.Count == 0)
            {
                var empty = UIKit.Label(list, "Empty", _laterFilter ? "No puzzles saved for later." : "No scenes installed.", 14, UIKit.InkSoft);
                empty.Size(60);
            }
            foreach (var l in levels)
            {
                var status = Progression.Status(Save, Catalogue, l);
                bool laterOptional = Progression.HasLaterOptionalBoard(Save, l);
                var row = UIKit.Panel(list, "Row_" + l.LevelId, status == LevelStatus.Locked ? SpriteFactory.Hex("#EFE6DA") : Color.white, 14, raycast: true);
                row.Size(58);
                var n = UIKit.Label(row.transform, "N", l.OrdinalInArea.ToString(), 18, UIKit.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
                n.rectTransform.TopLeft(6, 0, 34, 58);
                var name = UIKit.Label(row.transform, "Name", Strings.Get(l.PurposeKey), 15, status == LevelStatus.Locked ? UIKit.InkSoft : UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.rectTransform.TopLeft(44, 6, 180, 26);
                string kind = l.Boards.Any(b => b.IsPlanning) ? (l.HasOptionalBoard ? "Optional puzzle + final placements" : "Planning puzzle · can be left for later") : "Sorting";
                var sub = UIKit.Label(row.transform, "Kind", kind, 11, UIKit.InkSoft, TextAnchor.MiddleLeft);
                sub.rectTransform.TopLeft(44, 32, 190, 20);
                string chip = status == LevelStatus.Completed ? (laterOptional ? "Sorted · puzzle later" : "Sorted ✓")
                    : status == LevelStatus.Later ? Strings.Get("status.later")
                    : status == LevelStatus.InProgress ? Strings.Get("status.in_progress")
                    : status == LevelStatus.Locked ? "Locked" : "Ready";
                var c = UIKit.Label(row.transform, "Status", chip, 12, status == LevelStatus.Completed ? UIKit.Primary : UIKit.InkSoft, TextAnchor.MiddleRight, FontStyle.Bold);
                c.rectTransform.TopLeft(220, 0, 108, 58);
                if (status != LevelStatus.Locked)
                {
                    var btn = row.gameObject.AddComponent<Button>();
                    var captured = l;
                    btn.onClick.AddListener(() => OpenFromList(captured));
                }
            }
        }

        public void ShowExplore()
        {
            if (_lastBrowseScreen == "area") ShowArea(); else ShowHome();
        }

        public void ShowProject()
        {
            ClearScreen();
            _projectPreview.Clear();
            BuildProject();
        }

        void BuildProject()
        {
            UIKit.Clear(_screenLayer);
            var root = UIKit.Rect(_screenLayer, "Project").Stretch();
            var top = UIKit.Rect(root, "Top").TopLeft(12, 8, 336, 48);
            var back = UIKit.Button(top, "Back", "‹ " + Strings.Get("action.back"), () => { _projectPreview.Clear(); ShowHome(); });
            ((RectTransform)back.transform).TopLeft(0, 2, 84, 44);
            var title = UIKit.Label(top, "Title", "Kitchen Project", 20, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.TopLeft(86, 0, 140, 48);
            CoinChip(top, 336);

            var list = UIKit.ScrollList(root, "List", 8);
            ((RectTransform)list.parent).Stretch(12, 62, 12, 8);
            var holder = UIKit.Rect(list, "OverviewHolder"); holder.Size(190);
            var ov = KitchenOverviewView.Build(holder, Save, Catalogue, 336, 184, _projectPreview);
            ov.TopLeft(0, 0, 336, 184);
            if (_projectPreview.Count > 0)
            {
                var note = UIKit.Label(list, "PreviewNote", "Previewing — nothing is spent until you buy.", 12, UIKit.InkSoft);
                note.Size(20);
            }

            foreach (var a in KitchenCatalogue.Anchors)
            {
                bool unlocked = Save.anchorIds.Contains(a.AnchorId);
                var card = UIKit.Panel(list, "Anchor_" + a.AnchorId, unlocked ? Color.white : SpriteFactory.Hex("#EFE6DA"), 14);
                card.Size(unlocked ? 112 : 56);
                var t = UIKit.Label(card.transform, "Title", Strings.Get("anchor." + a.AnchorId), 15, UIKit.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
                t.rectTransform.TopLeft(12, 8, 312, 22);
                if (!unlocked)
                {
                    var l = UIKit.Label(card.transform, "Locked", $"Unlocks when Kitchen {Catalogue.Level(a.TriggerLevelId).OrdinalInArea} is sorted", 12, UIKit.InkSoft, TextAnchor.UpperLeft);
                    l.rectTransform.TopLeft(12, 30, 312, 20);
                    continue;
                }
                string equipped = Save.Equipped(a.AnchorId);
                bool owned = Save.ownedCosmetics.Contains(a.VariantId);
                var row = UIKit.Rect(card.transform, "Buttons").TopLeft(10, 56, 316, 46);
                UIKit.HStack(row, 6);
                var captured = a;
                UIKit.Button(row, "Default", string.IsNullOrEmpty(equipped) ? "Default ✓" : "Use default", () => DoEquip(captured.AnchorId, ""));
                if (owned)
                    UIKit.Button(row, "Variant", equipped == a.VariantId ? Strings.Get("variant." + a.VariantId) + " ✓" : "Use " + Strings.Get("variant." + a.VariantId), () => DoEquip(captured.AnchorId, captured.VariantId), fontSize: 12);
                else
                {
                    UIKit.Button(row, "Preview", "Preview", () =>
                    {
                        _projectPreview[captured.AnchorId] = captured.VariantId;
                        Session.Telemetry.Emit("cosmetic_preview", $"\"variant\":\"{captured.VariantId}\",\"cost\":{captured.VariantPrice}");
                        BuildProject();
                    });
                    var buy = UIKit.Button(row, "Buy", $"Buy · {a.VariantPrice}", () => ConfirmPurchase(captured), primary: true);
                    buy.interactable = Progression.CanPurchase(Save, a.VariantId);
                }
                var sub = UIKit.Label(card.transform, "Variant", "Optional: " + Strings.Get("variant." + a.VariantId) + (owned ? " (owned)" : $" · {a.VariantPrice} coins"), 12, UIKit.InkSoft, TextAnchor.UpperLeft);
                sub.rectTransform.TopLeft(12, 30, 312, 20);
            }
        }

        void ConfirmPurchase(AnchorDef a)
        {
            ShowChoice($"Buy {Strings.Get("variant." + a.VariantId)} for {a.VariantPrice} coins?",
                ($"Buy for {a.VariantPrice}", () =>
                {
                    int before = Save.wallet;
                    if (Session.Commit(s => Progression.Purchase(s, a.VariantId)))
                    {
                        _projectPreview.Remove(a.AnchorId);
                        Audio.Play("restoration");
                        Session.Telemetry.Emit("cosmetic_purchase", $"\"variant\":\"{a.VariantId}\",\"cost\":{a.VariantPrice},\"walletDelta\":{Save.wallet - before},\"tx\":\"{Progression.TransactionId(a.VariantId)}\"");
                    }
                    else if (Session.SaveBlocked) ShowSaveError(BuildProject);
                    BuildProject();
                }),
                ("Not now", () => BuildProject()));
        }

        void DoEquip(string anchorId, string variantId)
        {
            _projectPreview.Remove(anchorId);
            if (Session.Commit(s => Progression.Equip(s, anchorId, variantId)))
                Session.Telemetry.Emit("cosmetic_equip", $"\"anchor\":\"{anchorId}\",\"variant\":\"{variantId}\"");
            else if (Session.SaveBlocked) { ShowSaveError(BuildProject); return; }
            BuildProject();
        }

        public void ShowCollection()
        {
            ClearScreen();
            Session.Telemetry.Emit("collection_view", "\"count\":" + Save.discoveries.Count);
            var root = UIKit.Rect(_screenLayer, "Collection").Stretch();
            var top = UIKit.Rect(root, "Top").TopLeft(12, 8, 336, 48);
            var back = UIKit.Button(top, "Back", "‹ " + Strings.Get("action.back"), ShowHome);
            ((RectTransform)back.transform).TopLeft(0, 2, 84, 44);
            int found = KitchenCatalogue.Assets.Count(a => Save.discoveries.Contains(a.AssetId));
            var title = UIKit.Label(top, "Title", $"Kitchen shelf {found}/18", 20, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.TopLeft(90, 0, 200, 48);
            var grid = UIKit.Rect(root, "Grid").TopLeft(12, 64, 336, 480);
            for (int i = 0; i < KitchenCatalogue.Assets.Length; i++)
            {
                var a = KitchenCatalogue.Assets[i];
                bool d = Save.discoveries.Contains(a.AssetId);
                var cell = UIKit.Panel(grid, a.AssetId, d ? Color.white : SpriteFactory.Hex("#E9DFD2"), 12);
                cell.rectTransform.TopLeft((i % 4) * 85, (i / 4) * 92, 80, 86);
                var ic = UIKit.Icon(cell.transform, "Icon", SpriteFactory.Object(a.AssetId), 50, d ? Color.white : new Color(0.25f, 0.2f, 0.18f, 0.35f));
                ic.rectTransform.TopLeft(15, 6, 50, 50);
                var l = UIKit.Label(cell.transform, "Name", d ? Strings.Get("asset." + a.AssetId) : "?", 11, UIKit.Ink, TextAnchor.MiddleCenter);
                l.rectTransform.TopLeft(2, 58, 76, 24);
            }
            if (Save.recognitions.Contains(Progression.KitchenCollectionComplete))
            {
                var badge = UIKit.Label(root, "Badge", "★ Kitchen shelf complete", 15, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                badge.rectTransform.TopLeft(12, 530, 336, 30);
            }
        }

        public void ShowRecords()
        {
            ClearScreen();
            var root = UIKit.Rect(_screenLayer, "Records").Stretch();
            var top = UIKit.Rect(root, "Top").TopLeft(12, 8, 336, 48);
            var back = UIKit.Button(top, "Back", "‹ " + Strings.Get("action.back"), ShowHome);
            ((RectTransform)back.transform).TopLeft(0, 2, 84, 44);
            var title = UIKit.Label(top, "Title", "Records", 20, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.TopLeft(90, 0, 156, 48);
            var list = UIKit.ScrollList(root, "List", 4);
            ((RectTransform)list.parent).Stretch(12, 62, 12, 8);
            var rec = new List<string>();
            if (Save.recognitions.Contains(Progression.KitchenReady)) rec.Add("Kitchen Ready");
            if (Save.recognitions.Contains(Progression.KitchenMaster)) rec.Add("Kitchen Master");
            if (rec.Count > 0) UIKit.Label(list, "Recognitions", "★ " + string.Join("  ★ ", rec), 14, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold).Size(28);
            foreach (var l in Catalogue.InstalledInArea(KitchenCatalogue.AreaId))
                foreach (var b in l.Boards)
                {
                    var r = Save.records.FirstOrDefault(x => x.levelId == l.LevelId && x.boardId == b.BoardId && x.boardContentVersion == l.BoardContentVersion);
                    bool badge = Save.badges.Contains(Progression.BadgeId(b.RecordRule, l.LevelId, b.BoardId));
                    string boardLabel = l.Boards.Count > 1 ? (b.Required ? " (final)" : " (puzzle)") : "";
                    string line = $"{l.OrdinalInArea}. {Strings.Get(l.PurposeKey)}{boardLabel}   best {UIKit.FormatMs(r?.bestMs ?? -1)}   unassisted {UIKit.FormatMs(r?.bestUnassistedMs ?? -1)}"
                        + (badge ? (b.RecordRule == RecordRules.CleanSort ? "   ✓ Clean Sort" : "   ✓ Unassisted Planner") : "");
                    UIKit.Label(list, "Rec_" + l.LevelId + b.BoardId, line, 12, UIKit.Ink, TextAnchor.MiddleLeft).Size(26);
                }
        }

        public void ShowSettings() => ShowSettingsModal(null);

        public void ShowSettingsModal(Action onClose)
        {
            var panel = OpenModal("Settings", 360);
            var list = UIKit.Rect(panel, "List").Stretch(16, 16, 16, 16);
            UIKit.VStack(list, 8);
            UIKit.Label(list, "Title", Strings.Get("action.settings"), 20, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Size(32);
            var st = Save.settings;
            Toggle(list, "Preference: " + (st.preference == "mastery" ? "Mastery (records shown)" : "Calm"), s => s.settings.preference = s.settings.preference == "mastery" ? "calm" : "mastery", onClose);
            Toggle(list, "Music: " + (st.music ? "On" : "Off"), s => s.settings.music = !s.settings.music, onClose);
            Toggle(list, "Effects: " + (st.effects ? "On" : "Off"), s => s.settings.effects = !s.settings.effects, onClose);
            Toggle(list, "Haptics: " + (st.haptics ? "On" : "Off"), s => s.settings.haptics = !s.settings.haptics, onClose);
            Toggle(list, "Reduced motion: " + (st.reducedMotion ? "On" : "Off"), s => s.settings.reducedMotion = !s.settings.reducedMotion, onClose);
            UIKit.Button(list, "Close", "Close", () => { CloseModal(); onClose?.Invoke(); }, primary: true).Size(46);
        }

        void Toggle(RectTransform list, string text, Action<SaveData> change, Action onClose)
        {
            UIKit.Button(list, text, text, () =>
            {
                string before = Save.settings.preference;
                if (Session.Commit(s => { change(s); return true; }))
                {
                    Audio.ApplySettings(Save.settings);
                    if (before != Save.settings.preference)
                        Session.Telemetry.Emit("preference_changed", $"\"preference\":\"{Save.settings.preference}\"");
                    _board?.OnSettingsChanged();
                }
                CloseModal();
                ShowSettingsModal(onClose);
            }).Size(46);
        }

        // ---------- Modals and toasts ----------

        public RectTransform OpenModal(string name, float height)
        {
            CloseModal();
            var scrim = UIKit.Panel(_modalLayer, "Scrim_" + name, UIKit.Scrim, 0, raycast: true);
            scrim.rectTransform.Stretch(0, -200, 0, -200);
            var panel = UIKit.Panel(scrim.transform, name, UIKit.Paper, 20, raycast: true);
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(324, height);
            return panel.rectTransform;
        }

        public bool HasModal => _modalLayer.childCount > 0;
        public void CloseModal() => UIKit.Clear(_modalLayer);

        public void ShowChoice(string question, params (string label, Action action)[] options)
        {
            var panel = OpenModal("Choice", 120 + options.Length * 54);
            var list = UIKit.Rect(panel, "List").Stretch(16, 16, 16, 16);
            UIKit.VStack(list, 8);
            UIKit.Label(list, "Q", question, 17, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Size(60);
            for (int i = 0; i < options.Length; i++)
            {
                var o = options[i];
                UIKit.Button(list, "Opt" + i, o.label, () => { CloseModal(); o.action(); }, primary: i == 0).Size(46);
            }
            UIKit.Button(list, "Cancel", "Cancel", CloseModal).Size(40);
        }

        public void ShowSaveError(Action onRecovered)
        {
            var panel = OpenModal("SaveError", 200);
            var list = UIKit.Rect(panel, "List").Stretch(16, 16, 16, 16);
            UIKit.VStack(list, 10);
            UIKit.Label(list, "Msg", Strings.Get("error.save_retry"), 16, UIKit.Ink).Size(70);
            UIKit.Button(list, "Retry", "Retry", () =>
            {
                if (Session.RetryFailedCommit()) { CloseModal(); onRecovered?.Invoke(); }
                else Toast("Still unable to save");
            }, primary: true).Size(48);
        }

        void ShowCorruptModal(string diagnostic, FileSaveStore store)
        {
            var panel = OpenModal("Corrupt", 260);
            var list = UIKit.Rect(panel, "List").Stretch(16, 16, 16, 16);
            UIKit.VStack(list, 10);
            UIKit.Label(list, "Msg", Strings.Get("error.save_corrupt"), 16, UIKit.Ink).Size(70);
            UIKit.Button(list, "Retry", "Retry", () => { CloseModal(); StartCoroutine(Boot()); }, primary: true).Size(48);
            UIKit.Button(list, "Reset", "Reset progress…", () =>
            {
                ShowChoice("Start over? Your old files are kept aside, but progress will begin from Kitchen 1.",
                    ("Reset and start over", () =>
                    {
                        Session.ResetAfterConfirmation(System.IO.Path.GetDirectoryName(store.MainPath));
                        Audio.Init(Save.settings);
                        OpenLevel(KitchenCatalogue.LevelId(1), BoardScreen.EntryMode.Default);
                    }));
            }).Size(44);
        }

        public void Toast(string text, float seconds = 1.6f) => StartCoroutine(ToastRoutine(text, seconds));

        IEnumerator ToastRoutine(string text, float seconds)
        {
            var bg = UIKit.Panel(_toastLayer, "Toast", new Color(0.2f, 0.16f, 0.14f, 0.92f), 16);
            bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = new Vector2(0.5f, 0);
            bg.rectTransform.pivot = new Vector2(0.5f, 0);
            bg.rectTransform.anchoredPosition = new Vector2(0, 24);
            bg.rectTransform.sizeDelta = new Vector2(300, 44);
            var t = UIKit.Label(bg.transform, "Text", text, 15, Color.white);
            t.rectTransform.Stretch(10, 2, 10, 2);
            yield return new WaitForSecondsRealtime(seconds);
            if (bg != null) Destroy(bg.gameObject);
        }
    }
}
