using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SortEverything.Domain;
using SortEverything.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Runtime states Loading → Ready → Playing ⇄ Paused → Completing → Result → Unloading (Section 14).
    /// Input proposes; RuleEvaluator decides; the candidate save commits once; presentation reads committed state.
    /// </summary>
    public sealed class BoardScreen : MonoBehaviour
    {
        public enum EntryMode { Default, ResumePuzzle, ReplayFinish }
        enum Phase { Loading, Ready, Playing, Paused, Completing, Result, Unloading }

        GameApp _app;
        GameSession S => _app.Session;
        SaveData Save => _app.Session.Save;
        LevelDef _level;
        string _attemptId;
        BoardDef _def;
        BoardState _state;
        BoardView _view;
        Phase _phase = Phase.Loading;
        RectTransform _root, _hud, _boardHolder, _overlay, _resultSheet;
        Text _taskText, _countText, _timeText, _progressText;
        Button _deferButton, _hintChip;
        BoardLayout _layout;
        Text _instruction;
        Image _progressFill;
        string _instructionDefault = "";
        int _combo;
        float _lastAcceptTime = -10f;

        // Drag ownership
        ObjectView _drag;
        int _dragPointer = int.MinValue;
        Vector2 _dragOffset;
        int _dragNoPressFrames;

        // Time and inactivity
        double _unsavedMs;
        float _idleSeconds;
        float _readySeconds;
        bool _guided;
        bool _inspecting;
        bool _navLock;
        Coroutine _handRoutine;
        GameObject _handGo;
        Hint _activeHint;

        AttemptData Attempt => Save.Attempt(_attemptId);
        BoardRunData Run => _def == null ? null : Attempt?.boards.FirstOrDefault(b => b.boardId == _def.BoardId);
        bool Mastery => Save.settings.preference == "mastery";
        bool Reduced => Save.settings.reducedMotion;
        AreaData _area => ContentDb.Area(_level.AreaId);

        public void Init(GameApp app, LevelDef level, EntryMode mode)
        {
            _app = app;
            _level = level;
            _root = (RectTransform)transform;
            if (!PrepareAttempt(mode)) { _app.ShowSaveError(() => _app.ShowHome()); return; }
            BuildForCurrentBoard();
        }

        bool PrepareAttempt(EntryMode mode)
        {
            var existing = Save.AttemptFor(_level.LevelId);
            string sig = S.Config.Signature(Save.settings.reducedMotion);
            bool ok = S.Commit(s =>
            {
                var a = s.AttemptFor(_level.LevelId);
                if (mode == EntryMode.ReplayFinish && a != null)
                {
                    var req = a.Board(_level.RequiredBoard.BoardId);
                    Progression.RestartBoard(req, S.NewId);
                    a.currentBoardId = req.boardId;
                    a.entryKind = "replay_finish";
                }
                else if (mode == EntryMode.ResumePuzzle && a != null && _level.HasOptionalBoard)
                {
                    var opt = a.Board(_level.OptionalBoard.BoardId);
                    if (opt.status == BoardStatus.Deferred) opt.status = opt.assignments.Count > 0 ? BoardStatus.Playing : BoardStatus.Unstarted;
                    a.currentBoardId = opt.boardId;
                }
                else if (a == null)
                {
                    a = Progression.CreateAttempt(s, _level, s.HasSeal(_level.LevelId) ? "replay" : "first", S.NewId, sig);
                }
                else if (a.Current.status == BoardStatus.Deferred && _level.CanDeferLevel)
                {
                    // Resuming a scene left for later: same attempt, same snapshot.
                    a.Current.status = a.Current.assignments.Count > 0 ? BoardStatus.Playing : BoardStatus.Unstarted;
                }
                s.selectedAttemptId = a.attemptId;
                s.lastVisitedAreaId = _level.AreaId;
                _attemptId = a.attemptId;
                return true;
            });
            if (ok && existing != null) S.Telemetry.Emit("level_resume", Fields());
            return ok;
        }

        string Fields(string extra = "") =>
            $"\"levelId\":\"{_level.LevelId}\",\"attemptId\":\"{_attemptId}\",\"boardId\":\"{_def?.BoardId}\",\"boardRunId\":\"{Run?.boardRunId}\",\"required\":{(_def?.Required ?? true).ToString().ToLowerInvariant()},\"entry\":\"{Attempt?.entryKind}\"" + (extra.Length > 0 ? "," + extra : "");

        // ---------- Build ----------

        void BuildForCurrentBoard()
        {
            CancelDrag();
            StopHand();
            UIKit.Clear(_root);
            _phase = Phase.Loading;
            _def = _level.Board(Attempt.currentBoardId);
            _state = Progression.ReadBoard(_def, Run);
            _activeHint = default;
            _idleSeconds = _readySeconds = 0;
            _inspecting = false;

            // One layout system: zones are shares of the safe area (Resources/SortEverything/BoardLayout.json).
            var safe = _app.SafeArea.rect;
            float sw = safe.width > 1 ? safe.width : 360f, sh = _app.ScreenHeight;
            _layout = BoardLayout.Compute(_def, sw, sh);
            float hudH = _layout.HeaderH, instrH = _layout.InstructionH;
            _combo = 0;

            // Backsplash: the kitchen wall behind the header and the instruction line (decorative, no raycasts).
            KitchenArt.Tiled(_root, "Wall", KitchenArt.Surface(_area.wall), Color.white).rectTransform.Stretch(0, -200, 0, -200);
            BuildHud(hudH);
            BuildInstruction(hudH, instrH);
            _boardHolder = UIKit.Rect(_root, "BoardHolder").Stretch(0, hudH + instrH, 0, 0);
            _view = BoardView.Create(_boardHolder, _def, _layout, _app.UiCamera, _level.PurposeProp, _area, _area.MessFor(_level.LevelId));
            _view.ReducedMotion = Reduced;
            _view.PointerDown = OnPointerDown;
            _view.PointerDrag = OnPointerDrag;
            _view.PointerUp = OnPointerUp;
            _view.SnapAll(_state, beforeProfile: true);
            _overlay = UIKit.Rect(_root, "Overlay").Stretch();
            RefreshHud();
            EnterReady();
        }

        void BuildHud(float hudH)
        {
            _hud = UIKit.Rect(_root, "HUD").TopLeft(0, 0, 0, hudH);
            _hud.anchorMax = new Vector2(1, 1);
            _hud.sizeDelta = new Vector2(0, hudH);
            var strip = UIKit.Panel(_hud, "Strip", SpriteFactory.Hex("#FFF8EE"), 0);
            strip.rectTransform.Stretch(0, -200, 0, 0);
            var stripEdge = UIKit.Panel(_hud, "StripEdge", new Color(0.55f, 0.4f, 0.28f, 0.3f), 0);
            stripEdge.rectTransform.Bottom(2, -2);
            float bs = Mathf.Clamp(hudH - 12, 40, 48);
            var pause = UIKit.Button(_hud, "Pause", "II", OpenPause, fontSize: 16);
            ((RectTransform)pause.transform).TopLeft(8, (hudH - bs) / 2, bs, bs);
            float x = 8 + bs + 8;

            bool optional = !_def.Required;
            bool canDefer = optional || (_level.CanDeferLevel && !Save.HasSeal(_level.LevelId) && !S.Config.forcedPlanningResearch);
            float right = 8;
            if (canDefer)
            {
                string label = optional ? Strings.Get(Save.HasSeal(_level.LevelId) ? "action.leave_puzzle_for_later" : "action.finish_kitchen") : Strings.Get("action.leave_for_later");
                _deferButton = UIKit.Button(_hud, "Defer", label, OnDefer, fontSize: 12);
                var drt = (RectTransform)_deferButton.transform;
                drt.anchorMin = drt.anchorMax = new Vector2(1, 0.5f);
                drt.pivot = new Vector2(1, 0.5f);
                drt.anchoredPosition = new Vector2(-8, 0);
                drt.sizeDelta = new Vector2(100, bs);
                right += 108;
            }
            if (_def.IsPlanning)
            {
                _hintChip = UIKit.Button(_hud, "HintChip", Strings.Get("action.hint"), RequestHint, fontSize: 12);
                var hrt = (RectTransform)_hintChip.transform;
                hrt.anchorMin = hrt.anchorMax = new Vector2(1, 0.5f);
                hrt.pivot = new Vector2(1, 0.5f);
                hrt.anchoredPosition = new Vector2(-right, 0);
                hrt.sizeDelta = new Vector2(54, bs);
                _hintChip.gameObject.SetActive(false);
            }

            // Level name, then "Kitchen 1/20" and a small sorted-progress bar on one line.
            var info = UIKit.Rect(_hud, "Info");
            info.anchorMin = new Vector2(0, 0); info.anchorMax = new Vector2(1, 1);
            info.offsetMin = new Vector2(x, 4); info.offsetMax = new Vector2(-right - 4, -4);
            _taskText = UIKit.Label(info, "Task", Strings.Get(_level.PurposeKey), 16, UIKit.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _taskText.rectTransform.anchorMin = new Vector2(0, 0.5f); _taskText.rectTransform.anchorMax = new Vector2(1, 1);
            _taskText.rectTransform.offsetMin = Vector2.zero; _taskText.rectTransform.offsetMax = Vector2.zero;
            var line = UIKit.Rect(info, "ProgressLine");
            line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(1, 0.5f);
            line.offsetMin = new Vector2(0, 2); line.offsetMax = new Vector2(0, -2);
            _countText = UIKit.Label(line, "Count", "", 12, UIKit.InkSoft, TextAnchor.MiddleLeft);
            _countText.rectTransform.anchorMin = new Vector2(0, 0); _countText.rectTransform.anchorMax = new Vector2(0, 1);
            _countText.rectTransform.pivot = new Vector2(0, 0.5f);
            _countText.rectTransform.sizeDelta = new Vector2(78, 0);
            _countText.rectTransform.anchoredPosition = Vector2.zero;
            var track = UIKit.Panel(line, "Track", new Color(0.55f, 0.4f, 0.28f, 0.18f), 4);
            track.rectTransform.anchorMin = new Vector2(0, 0.5f); track.rectTransform.anchorMax = new Vector2(1, 0.5f);
            track.rectTransform.offsetMin = new Vector2(80, -4); track.rectTransform.offsetMax = new Vector2(-34, 4);
            _progressFill = UIKit.Panel(track.transform, "Fill", UIKit.Primary, 4);
            _progressFill.rectTransform.anchorMin = new Vector2(0, 0); _progressFill.rectTransform.anchorMax = new Vector2(0, 1);
            _progressFill.rectTransform.offsetMin = Vector2.zero; _progressFill.rectTransform.offsetMax = Vector2.zero;
            _progressText = UIKit.Label(line, "Sorted", "", 11, UIKit.InkSoft, TextAnchor.MiddleRight, FontStyle.Bold);
            _progressText.rectTransform.anchorMin = new Vector2(1, 0); _progressText.rectTransform.anchorMax = new Vector2(1, 1);
            _progressText.rectTransform.pivot = new Vector2(1, 0.5f);
            _progressText.rectTransform.sizeDelta = new Vector2(32, 0);
            _progressText.rectTransform.anchoredPosition = Vector2.zero;
            _timeText = UIKit.Label(_hud, "Time", "", 11, UIKit.InkSoft, TextAnchor.UpperRight);
            _timeText.rectTransform.TopLeft(x, 2, 140, 14);
        }

        /// <summary>The single instruction line (level instruction, or tutorial text in its place).</summary>
        void BuildInstruction(float top, float h)
        {
            var bar = UIKit.Rect(_root, "Instruction").TopLeft(0, top, 0, h);
            bar.anchorMax = new Vector2(1, 1);
            bar.sizeDelta = new Vector2(0, h);
            var paper = UIKit.Panel(bar, "Paper", new Color(1f, 0.985f, 0.95f, 0.96f), 10);
            paper.rectTransform.anchorMin = paper.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            paper.rectTransform.sizeDelta = new Vector2(Mathf.Min(_layout.Width - 24, 340), Mathf.Max(22, h - 10));
            var sh = paper.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.3f, 0.18f, 0.1f, 0.25f);
            sh.effectDistance = new Vector2(1, -1.5f);
            _instruction = UIKit.Label(paper.transform, "Text", "", 13, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _instruction.resizeTextMinSize = 10;
            _instruction.rectTransform.Stretch(10, 0, 10, 0);
        }

        void SetInstruction(string text) { if (_instruction != null) _instruction.text = text; }

        void RefreshHud()
        {
            if (_countText == null) return;
            int n = _level.OrdinalInArea, total = _app.Catalogue.InstalledInArea(_level.AreaId).Count;
            _countText.text = $"{Strings.Get("area.house_kitchen")} {n}/{total}";
            float f = _state.ObjectCount == 0 ? 0 : _state.SortedCount / (float)_state.ObjectCount;
            _progressFill.rectTransform.anchorMax = new Vector2(f, 1);
            _progressFill.gameObject.SetActive(f > 0);
            _progressText.text = $"{_state.SortedCount}/{_state.ObjectCount}";
            bool showTime = Mastery && _level.OrdinalInArea > 1;
            _timeText.gameObject.SetActive(showTime);
            if (showTime) _timeText.text = UIKit.FormatMs((Run?.activeMs ?? 0) + (long)_unsavedMs);
        }

        public void OnSettingsChanged()
        {
            if (_view != null) _view.ReducedMotion = Reduced;
            RefreshHud();
        }

        // ---------- Ready, lessons, tutorial ----------

        void EnterReady()
        {
            _phase = Phase.Ready;
            UIKit.Clear(_overlay);

            // One instruction line: the level's instruction (data) or its rule summary. Tutorials replace it.
            _instructionDefault = !string.IsNullOrEmpty(_level.InstructionOverride) ? _level.InstructionOverride : RuleSummary();
            if (!_def.Required) _instructionDefault = "Optional puzzle · " + _instructionDefault;
            else if (_level.Boards.Count > 1) _instructionDefault = "Final placements · " + _instructionDefault;
            SetInstruction(_instructionDefault);

            // Level 20 optional board: Begin Puzzle / Finish Kitchen.
            if (!_def.Required && !Save.HasSeal(_level.LevelId) && _state.SortedCount == 0)
            {
                var panel = UIKit.Panel(_overlay, "OptionalReady", UIKit.Paper, 18, raycast: true);
                panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                panel.rectTransform.sizeDelta = new Vector2(320, 190);
                var list = UIKit.Rect(panel.transform, "List").Stretch(14, 14, 14, 14);
                UIKit.VStack(list, 8);
                UIKit.Label(list, "Title", Strings.Get(_level.PurposeKey), 19, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Size(28);
                UIKit.Label(list, "Body", "An optional puzzle board is ready. You can solve it now, or finish the kitchen and come back to it later.", 13, UIKit.InkSoft).Size(44);
                UIKit.Button(list, "Begin", Strings.Get("action.begin_puzzle"), () => { Destroy(panel.gameObject); OfferLessonsOrStart(); }, primary: true).Size(44);
                UIKit.Button(list, "Finish", Strings.Get("action.finish_kitchen"), OnDefer).Size(40);
                return;
            }
            OfferLessonsOrStart();
        }

        void OfferLessonsOrStart()
        {
            var pending = PendingLessons();
            if (pending.Count > 0 && _state.SortedCount == 0) ShowLessonOffer(pending);
            else if (_level.TeachingId == TeachingIds.FirstDrag && _state.SortedCount == 0 && !Save.HasSeal(_level.LevelId)) ShowDragCue();
        }

        List<string> PendingLessons() =>
            TeachingIds.RequiredFor(_def.TemplateId).Where(id => !Save.tutorialOfferedIds.Contains(id)).ToList();

        void ShowLessonOffer(List<string> lessons)
        {
            var panel = UIKit.Panel(_overlay, "LessonOffer", UIKit.Paper, 18, raycast: true);
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0);
            panel.rectTransform.pivot = new Vector2(0.5f, 0);
            panel.rectTransform.anchoredPosition = new Vector2(0, 12);
            panel.rectTransform.sizeDelta = new Vector2(336, 150 + 40 * lessons.Count);
            var list = UIKit.Rect(panel.transform, "List").Stretch(14, 12, 14, 12);
            UIKit.VStack(list, 6);
            UIKit.Label(list, "Title", Strings.Get("lesson.title"), 18, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Size(26);
            foreach (var l in lessons)
                UIKit.Label(list, "Rule_" + l, RuleTeaser(l), 13, UIKit.InkSoft).Size(38);
            var row = UIKit.Rect(list, "Buttons"); row.Size(46); UIKit.HStack(row, 8);
            UIKit.Button(row, "Watch", Strings.Get("action.watch_example"), () => { Destroy(panel.gameObject); StartCoroutine(PlayLessons(lessons)); });
            UIKit.Button(row, "Start", Strings.Get("action.start_sorting"), () =>
            {
                Destroy(panel.gameObject);
                // Dismiss without revealing strategy or marking assistance.
                if (!S.Commit(s => { foreach (var l in lessons) if (!s.tutorialOfferedIds.Contains(l)) s.tutorialOfferedIds.Add(l); return true; }))
                    _app.ShowSaveError(null);
            }, primary: true);
        }

        static string RuleTeaser(string lesson)
        {
            switch (lesson)
            {
                case TeachingIds.SharedCapacityRecovery: return "Some containers share space. Every container shows what it accepts and how many spaces are used.";
                case TeachingIds.WeightedUnits: return "Items with a 2 badge are big: they use two spaces in one container.";
                default: return "Baskets here accept two kinds each. Watch the spaces left in each one.";
            }
        }

        IEnumerator PlayLessons(List<string> lessons)
        {
            _guided = true;
            foreach (var lesson in lessons)
            {
                bool skipped = false;
                var demo = GhostDemo.Build(_overlay, lesson, () => skipped = true);
                // Answer-revealing content appears: record exposure and assistance.
                if (!S.Commit(s =>
                {
                    if (!s.tutorialOfferedIds.Contains(lesson)) s.tutorialOfferedIds.Add(lesson);
                    if (!s.tutorialExposedIds.Contains(lesson)) s.tutorialExposedIds.Add(lesson);
                    Progression.MarkAssistance(s.Attempt(_attemptId).Board(_def.BoardId), "requested_demo");
                    return true;
                })) { _app.ShowSaveError(null); }
                S.Telemetry.Emit("tutorial_exposed", Fields($"\"teachingId\":\"{lesson}\",\"kind\":\"requested\""));
                yield return demo.Play(() => skipped, Reduced);
                if (demo != null) Destroy(demo.gameObject);
            }
            _guided = false;
        }

        void ShowDragCue() => SetInstruction(Strings.Get("tutorial.drag"));

        void Update()
        {
            if (_phase == Phase.Loading || _phase == Phase.Unloading) return;
            float dt = Time.unscaledDeltaTime;
            bool foreground = Application.isFocused;

            if (_phase == Phase.Playing && !_guided && !_inspecting && !_app.HasModal && foreground)
            {
                _unsavedMs += dt * 1000.0;
                if (_drag == null) _idleSeconds += dt;
                if (Mastery && Time.frameCount % 10 == 0) RefreshHud();
            }

            // Level 1: noninteractive hand illustration after four eligible foreground seconds without pickup.
            if (_phase == Phase.Ready && !_guided && !_app.HasModal && foreground && _level.TeachingId == TeachingIds.FirstDrag
                && _state.SortedCount == 0 && _handRoutine == null && !Save.HasSeal(_level.LevelId))
            {
                _readySeconds += dt;
                if (_readySeconds >= 4f) _handRoutine = StartCoroutine(HandIllustration());
            }

            // Subtle Hint chip after ~8 s without assignment/rearrangement on planning boards.
            if (_hintChip != null)
                _hintChip.gameObject.SetActive(_phase == Phase.Playing && _drag == null && !_guided && _idleSeconds >= 8f && !_state.IsSolved);

            // Safety net: a drag never follows a missing pointer.
            if (_drag != null)
            {
                bool anyPressed = (Pointer.current != null && Pointer.current.press.isPressed)
                    || (Touchscreen.current != null && Touchscreen.current.touches.Any(x => x.press.isPressed));
                _dragNoPressFrames = anyPressed ? 0 : _dragNoPressFrames + 1;
                if (_dragNoPressFrames > 3) CancelDrag();
            }
        }

        IEnumerator HandIllustration()
        {
            var first = _def.Objects.First();
            var target = _def.Targets.First(t => t.AcceptsCategory(first.CategoryId));
            // Assistance is recorded once the answer-revealing illustration is actually shown.
            if (!Run.assistanceKinds.Contains("automatic_demo"))
            {
                S.Commit(s => { Progression.MarkAssistance(s.Attempt(_attemptId).Board(_def.BoardId), "automatic_demo"); return true; });
                S.Telemetry.Emit("tutorial_exposed", Fields("\"teachingId\":\"first_drag\",\"kind\":\"automatic\""));
            }
            _handGo = new GameObject("HandIllustration", typeof(RectTransform));
            var rt = (RectTransform)_handGo.transform;
            rt.SetParent(_view.OverlayLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(44, 44);
            var ghost = UIKit.Icon(rt, "GhostObject", SpriteFactory.Object(first.AssetId), _layout.Pile[first.InstanceId].Visible, new Color(1, 1, 1, 0.5f));
            var hand = UIKit.Icon(rt, "Hand", SpriteFactory.Hand(), 46);
            hand.rectTransform.anchoredPosition = new Vector2(10, -18);
            Vector2 from = _view.SourcePosition(first), to = _view.SlotPosition(target.TargetId, 0);
            while (true)
            {
                yield return UIKit.Tween(Reduced ? 0.01f : 1.3f, t =>
                {
                    var p = Vector2.Lerp(from, to, UIKit.EaseOut(t));
                    rt.anchoredPosition = new Vector2(p.x, -p.y);
                });
                yield return new WaitForSecondsRealtime(0.7f);
            }
        }

        void StopHand()
        {
            if (_handRoutine != null) { StopCoroutine(_handRoutine); _handRoutine = null; }
            if (_handGo != null) { Destroy(_handGo); _handGo = null; }
        }

        string RuleSummary()
        {
            switch (_def.TemplateId)
            {
                case TemplateIds.Unlimited: return "Put each item with its group";
                case TemplateIds.P4: return "Each basket takes two kinds · 2 spaces each";
                default: return "Watch the spaces left in each container";
            }
        }

        // ---------- Pointer handling ----------

        bool OnPointerDown(ObjectView v, PointerEventData e)
        {
            if (_drag != null || _navLock || _inspecting || _guided || S.SaveBlocked || _app.HasModal) return false;
            if (_phase != Phase.Ready && _phase != Phase.Playing) return false;
            if (!RuleEvaluator.CanPickUp(_state, v.Def.InstanceId)) return false;
            if (_overlay.GetComponentInChildren<LessonBlocker>() != null) return false;

            if (_phase == Phase.Ready)
            {
                _phase = Phase.Playing;
                StopHand();
                SetInstruction(_instructionDefault); // the tutorial line gives way to the level instruction
                foreach (Transform child in _overlay)
                    if (child.name == "LessonOffer" || child.name == "OptionalReady") Destroy(child.gameObject);
                S.Telemetry.Emit("level_start", Fields("\"replay\":" + Save.HasSeal(_level.LevelId).ToString().ToLowerInvariant()));
            }

            ClearHint();
            _drag = v;
            _dragPointer = e.pointerId;
            _dragNoPressFrames = 0;
            var p = _view.ToBoard(e.position);
            _dragOffset = v.Position - p + new Vector2(0, -12); // stable offset, slightly above the fingertip
            v.transform.SetParent(_view.DragLayer, true);
            v.SetDragging(true);
            v.Place(p + _dragOffset);
            if (_def.IsPlanning && _state.IsAssigned(v.Def.InstanceId)) _view.ShowReturnArea(true);
            if (!Reduced && !_state.IsAssigned(v.Def.InstanceId)) Fx.Burst(_view, _view.FxLayer, v.Position + new Vector2(0, v.VisibleSize * 0.3f), new Color(1f, 0.95f, 0.85f), 4, 0.45f);
            _app.Audio.Play("pickup");
            return true;
        }

        void OnPointerDrag(ObjectView v, PointerEventData e)
        {
            if (v != _drag || e.pointerId != _dragPointer) return;
            var p = _view.ToBoard(e.position) + _dragOffset;
            p.x = Mathf.Clamp(p.x, 0, _view.W);
            p.y = Mathf.Clamp(p.y, 0, _layout.PlayH + _layout.ContainersH);
            v.Place(p);
            _view.SetHover(_view.TargetAt(p));
        }

        void OnPointerUp(ObjectView v, PointerEventData e)
        {
            if (v != _drag || e.pointerId != _dragPointer) return;
            Release(v);
        }

        void CancelDrag()
        {
            if (_drag == null) return;
            var v = _drag;
            _drag = null;
            _dragPointer = int.MinValue;
            _view.SetHover(null);
            _view.ShowReturnArea(false);
            _view.SnapAll(_state, beforeProfile: true);
            v.SetDragging(false);
        }

        void Release(ObjectView v)
        {
            var pos = v.Position;
            _drag = null;
            _dragPointer = int.MinValue;
            _view.SetHover(null);
            _view.ShowReturnArea(false);
            v.SetDragging(false);
            if (_phase != Phase.Playing) { _view.SnapAll(_state, true); return; } // drag ended after Completing

            var proposal = new DropProposal(v.Def.InstanceId, _view.TargetAt(pos), _view.OverSource(pos));
            var result = RuleEvaluator.Evaluate(_state, proposal);
            switch (result.Kind)
            {
                case DropKind.Accept: Accept(v, proposal, result); break;
                case DropKind.WrongCategory:
                case DropKind.Full: Reject(v, result); break;
                default: StartCoroutine(ReturnToCommitted(v, 0.14f)); break; // cancel / same-target no-op / locked
            }
        }

        IEnumerator ReturnToCommitted(ObjectView v, float duration)
        {
            v.transform.SetParent(_view.ObjectLayer, true);
            var target = _state.TargetOf(v.Def.InstanceId);
            var to = target == null ? _view.SourcePosition(v.Def) : _view.SlotPosition(target, _state.SlotOf(v.Def.InstanceId));
            _view.RestOn(v, target);
            yield return _view.Move(v, to, duration, false);
        }

        void Reject(ObjectView v, DropResult result)
        {
            long ms = FlushMs();
            bool ok = S.Commit(s =>
            {
                var run = s.Attempt(_attemptId).Board(_def.BoardId);
                run.activeMs += ms;
                Progression.RecordRejection(run, result.Kind);
                return true;
            });
            if (!ok) { _unsavedMs += ms; }
            S.Telemetry.Emit("drop_result", Fields($"\"object\":\"{v.Def.InstanceId}\",\"target\":\"{result.TargetId}\",\"units\":{v.Def.Units},\"result\":\"{result.Kind}\""));
            var tv = _view.Targets[result.TargetId];
            tv.ShowStatus(Strings.Get(result.Kind == DropKind.Full ? "status.full" : "status.wrong_category"), this);
            _view.Shake(tv);
            v.Wobble();
            _combo = 0;
            _app.Audio.Play("reject");
            StartCoroutine(ReturnToCommitted(v, 0.2f));
            if (!ok) _app.ShowSaveError(null);
        }

        long FlushMs()
        {
            long ms = (long)_unsavedMs;
            _unsavedMs -= ms;
            return ms;
        }

        void Accept(ObjectView v, DropProposal proposal, DropResult result)
        {
            var next = BoardReducer.Apply(_state, proposal, result, out var facts);
            long ms = FlushMs();
            CompletionFacts completion = null;
            bool ok = S.Commit(s =>
            {
                var a = s.Attempt(_attemptId);
                var run = a.Board(_def.BoardId);
                run.activeMs += ms;
                Progression.RecordAccepted(run, _def, next, facts);
                // Final placement, board checkpoint and reward commit as one transaction.
                if (facts.Solved) completion = Progression.CompleteBoard(s, _app.Catalogue, _level, a);
                return true;
            });
            if (!ok)
            {
                _unsavedMs += ms;
                StartCoroutine(ReturnToCommitted(v, 0.15f));
                _app.ShowSaveError(null);
                return;
            }

            _state = next;
            _idleSeconds = 0;
            S.Telemetry.Emit("drop_result", Fields($"\"object\":\"{v.Def.InstanceId}\",\"source\":\"{facts.FromTarget}\",\"target\":\"{facts.ToTarget}\",\"units\":{v.Def.Units},\"result\":\"{result}\",\"firstAssignment\":{facts.FirstAssignment.ToString().ToLowerInvariant()},\"rearrangement\":{(facts.Transfer || facts.Return).ToString().ToLowerInvariant()}"));

            v.transform.SetParent(_view.ObjectLayer, true);
            var dest = facts.Return ? _view.SourcePosition(v.Def) : _view.SlotPosition(facts.ToTarget, facts.Slot);
            _view.RestOn(v, facts.Return ? null : facts.ToTarget);
            foreach (var t in _def.Targets) _view.Targets[t.TargetId].SetUsage(_state.UnitsIn(t.TargetId));

            // Quick drops in a row build a gentle combo: rising pitch, bigger sparkles, a word of praise.
            bool sorted = !facts.Return;
            if (sorted && facts.FirstAssignment)
            {
                _combo = Time.unscaledTime - _lastAcceptTime < 3.5f ? _combo + 1 : 1;
                _lastAcceptTime = Time.unscaledTime;
            }
            int combo = _combo;
            StartCoroutine(LandRoutine(v, dest, facts, combo));
            _app.Audio.Haptic();
            if (sorted)
            {
                _app.Audio.Play(AudioFeedback.ForMaterial(ContentDb.Object(v.Def.AssetId).material, v.Def.CategoryId));
                _app.Audio.Play("sparkle", 1f + Mathf.Min(combo - 1, 8) * 0.06f);
            }
            else _app.Audio.Play("pickup", 0.8f);
            if (!_def.IsPlanning && facts.FirstAssignment && Mastery)
            {
                int streak = Run?.streak ?? 0;
                if (streak == 3 || streak == 5 || streak > 5 && streak % 5 == 0)
                    _app.Audio.Play("streak", 1f + Mathf.Min(streak, 8) * 0.03f);
            }
            RefreshHud();
            if (completion != null) OnBoardSolved(completion);
        }

        static readonly string[] Praise = { "Nice!", "Tidy!", "So neat!", "Lovely!", "Sparkling!" };

        /// <summary>Flies the item home, then lands it with squash, sparkles and a container bounce.</summary>
        IEnumerator LandRoutine(ObjectView v, Vector2 dest, BoardFacts facts, int combo)
        {
            yield return _view.Move(v, dest, 0.17f, overshoot: !facts.Return);
            if (_view == null || v == null) yield break;
            v.Land(facts.Return ? 0.5f : 1f);
            if (facts.Return) { _view.RefreshProgress(_state, animate: true); yield break; }

            var color = UIKit.CategoryColor(v.Def.CategoryId);
            var tv = _view.Targets[facts.ToTarget];
            if (!Reduced)
            {
                Fx.Burst(_view, _view.FxLayer, dest, color, 8 + Mathf.Min(combo, 6) * 2, 0.8f + Mathf.Min(combo, 6) * 0.08f);
                _view.Pulse(tv, 0.035f, 0.22f);
                if (facts.FirstAssignment && !facts.CategoryComplete && (combo == 3 || combo == 5 || combo == 8 || combo > 8 && combo % 4 == 0))
                    Fx.FloatText(_view, _view.FxLayer, dest + new Vector2(0, -36), Praise[Mathf.Min((combo - 3) / 2, Praise.Length - 1)], SpriteFactory.Hex("#FFE9A8"), 20);
            }
            _view.RefreshProgress(_state, animate: true);

            if (facts.CategoryComplete && !facts.Solved)
            {
                var center = _view.TargetCenter(facts.ToTarget);
                if (!Reduced)
                {
                    Fx.Flash(_view, _view.FxLayer, center, new Color(1f, 0.95f, 0.7f, 0.8f), 160f);
                    Fx.Ring(_view, _view.FxLayer, center, Color.Lerp(color, Color.white, 0.4f), 40f, 190f, 0.5f);
                    Fx.Burst(_view, _view.FxLayer, center, color, 16, 1.3f);
                    Fx.FloatText(_view, _view.FxLayer, center + new Vector2(0, -20), Strings.Get("category." + v.Def.CategoryId) + " done!", Color.white, 20);
                }
                _view.Pulse(tv, 0.08f, 0.3f);
                _app.Audio.Play("category");
            }
        }

        // ---------- Completion and result ----------

        void OnBoardSolved(CompletionFacts facts)
        {
            _phase = Phase.Completing;
            StopHand();
            ClearHint();
            S.Telemetry.Emit("board_complete", Fields($"\"activeMs\":{facts.ActiveMs}"));

            if (facts.OptionalBoardOnly && !Save.HasSeal(_level.LevelId))
            {
                // Optional board checkpointed before the finale: open the required board, no coins yet.
                _app.Toast("Puzzle solved · now the final placements", 1.6f);
                _app.Audio.Play("complete");
                StartCoroutine(AfterDelay(0.6f, () => { if (this != null) BuildForCurrentBoard(); }));
                return;
            }

            S.Telemetry.Emit("level_complete", Fields($"\"activeMs\":{facts.ActiveMs},\"coins\":{facts.CoinsEarned},\"newSeal\":{facts.NewSeal.ToString().ToLowerInvariant()},\"assisted\":{(Run?.assisted ?? false).ToString().ToLowerInvariant()},\"badges\":{facts.NewBadges.Count}"));
            if (facts.TraceAdded) S.Telemetry.Emit("task_trace_added", $"\"levelId\":\"{_level.LevelId}\",\"region\":\"{_level.ProjectRegion}\"");
            if (!string.IsNullOrEmpty(facts.AnchorUnlocked)) S.Telemetry.Emit("project_restored", $"\"anchor\":\"{facts.AnchorUnlocked}\"");
            if (facts.NewRecognitions.Contains(Progression.KitchenReady)) S.Telemetry.Emit("area_ready", "\"areaId\":\"house_kitchen\"");
            foreach (var d in facts.NewDiscoveries) S.Telemetry.Emit("discovery", $"\"assetId\":\"{d}\"");

            StartCoroutine(Celebrate(facts));
        }

        /// <summary>
        /// The payoff: the last bits of mess vanish, a glow sweeps the counter, every item hops in a wave,
        /// confetti falls and the scene's purpose appears. Then the result sheet slides in.
        /// </summary>
        IEnumerator Celebrate(CompletionFacts facts)
        {
            yield return new WaitForSecondsRealtime(Reduced ? 0f : 0.22f); // let the last item land
            if (this == null || _phase != Phase.Completing) yield break;
            _view.RefreshProgress(_state, animate: true);
            _view.ShowPurposeProp(true, Reduced);
            StartCoroutine(_view.FinishMicroAction(_level.FinishMicroAction));
            _app.Audio.Play("complete");
            if (!string.IsNullOrEmpty(facts.AnchorUnlocked)) _app.Audio.Play("restoration");
            else if (facts.NewDiscoveries.Count > 0) _app.Audio.Play("discovery");

            if (!Reduced)
            {
                // Glow sweep across the counter, left to right.
                StartCoroutine(_view.CleanShine());
                int k = 0;
                foreach (var o in _def.Objects.OrderBy(o => _view.Objects[o.InstanceId].Position.x).ThenBy(o => _view.Objects[o.InstanceId].Position.y))
                    _view.Objects[o.InstanceId].Hop(0.05f * k++);
                foreach (var t in _def.Targets)
                {
                    var c = _view.TargetCenter(t.TargetId);
                    Fx.Burst(_view, _view.FxLayer, c, UIKit.CategoryColor(t.Accepts[0]), 14, 1.4f);
                }
                Fx.Confetti(_view, _view.FxLayer, _view.W, -20f, 80);
                string cheer = _level.LevelId == KitchenCatalogue.LevelId(20) ? "Kitchen restored!" : "Sparkling clean!";
                Fx.FloatText(_view, _view.FxLayer, new Vector2(_view.W / 2, _layout.PlayH * 0.45f), cheer, SpriteFactory.Hex("#FFE9A8"), 30, 24f);
                yield return new WaitForSecondsRealtime(1.25f);
            }
            if (this == null || _phase != Phase.Completing) yield break;
            ShowResult(facts);
        }

        static IEnumerator AfterDelay(float s, Action a) { yield return new WaitForSecondsRealtime(s); a(); }

        void ShowResult(CompletionFacts facts)
        {
            _phase = Phase.Result;
            UIKit.Clear(_overlay);
            _hud.gameObject.SetActive(false);

            // Move the finished arrangement to the top; the result sheet uses the freed lower space.
            float shift = _layout.HeaderH + _layout.InstructionH + _layout.ContainersTop - 8;
            var boardRt = _view.Root;
            var basePos = boardRt.anchoredPosition;
            StartCoroutine(UIKit.Tween(Reduced ? 0 : 0.25f, t => boardRt.anchoredPosition = basePos + new Vector2(0, shift * UIKit.EaseOut(t))));
            _view.SourceRegion.gameObject.SetActive(false);

            float sheetTop = _layout.ContainersH + 8;
            foreach (Transform child in _root) if (child.name == "Instruction") child.gameObject.SetActive(false);
            var sheet = UIKit.Panel(_overlay, "ResultSheet", UIKit.Paper, 22, raycast: true);
            _resultSheet = sheet.rectTransform;
            _resultSheet.anchorMin = new Vector2(0, 0); _resultSheet.anchorMax = new Vector2(1, 1);
            _resultSheet.offsetMin = new Vector2(8, 8); _resultSheet.offsetMax = new Vector2(-8, -sheetTop);
            var list = UIKit.ScrollList(_resultSheet, "List", 6);
            ((RectTransform)list.parent).Stretch(12, 10, 12, 10);

            bool optionalAfterFinale = facts.OptionalBoardOnly;
            bool kitchenReadyNow = facts.NewRecognitions.Contains(Progression.KitchenReady);
            bool isFinale = _level.ProjectAnchorId == "finished_020" && !optionalAfterFinale;
            string preview = _app.Catalogue.PreviewFor(_level);
            bool sampleEnd = string.IsNullOrEmpty(preview) && !isFinale && !optionalAfterFinale;

            string title = optionalAfterFinale ? "Puzzle solved" : isFinale ? Strings.Get("result.kitchen_ready") : Strings.Get("result.sorted");
            UIKit.Label(list, "Title", title, 24, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Size(34);
            UIKit.Label(list, "Purpose", Strings.Get(_level.PurposeKey) + " — " + _view.PropBacking.GetComponentInChildren<Text>().text, 13, UIKit.InkSoft).Size(20);
            if (sampleEnd) UIKit.Label(list, "Sample", Strings.Get("result.sample_complete"), 14, UIKit.Primary, TextAnchor.MiddleCenter, FontStyle.Bold).Size(20);

            var gains = new List<string>();
            if (facts.CoinsEarned > 0) gains.Add($"+{facts.CoinsEarned} coins (total {Save.wallet})");
            if (facts.TraceAdded) gains.Add("Added to your kitchen");
            if (gains.Count > 0) UIKit.Label(list, "Gains", string.Join("  ·  ", gains), 14, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Size(22);

            if (facts.NewDiscoveries.Count > 0)
            {
                var row = UIKit.Rect(list, "Discoveries"); row.Size(40);
                var lbl = UIKit.Label(row, "Label", "New on your shelf:", 12, UIKit.InkSoft, TextAnchor.MiddleLeft);
                lbl.rectTransform.TopLeft(0, 0, 110, 40);
                for (int i = 0; i < facts.NewDiscoveries.Count && i < 6; i++)
                {
                    var ic = UIKit.Icon(row, facts.NewDiscoveries[i], SpriteFactory.Object(facts.NewDiscoveries[i]), 34);
                    ic.rectTransform.TopLeft(112 + i * 36, 3, 34, 34);
                }
            }
            if (Mastery)
            {
                if (facts.NewBadges.Count > 0) UIKit.Label(list, "Badges", facts.NewBadges[0].StartsWith(RecordRules.CleanSort) ? "Clean Sort badge" : "Unassisted Planner badge", 13, UIKit.Primary, TextAnchor.MiddleCenter, FontStyle.Bold).Size(20);
                if (facts.NewBestTime) UIKit.Label(list, "Best", "New best time · " + UIKit.FormatMs(facts.ActiveMs), 13, UIKit.Primary).Size(20);
            }
            if (facts.NewRecognitions.Contains(Progression.KitchenMaster))
                UIKit.Label(list, "Master", "★ " + Strings.Get("result.kitchen_master") + " — all twenty scenes sealed", 13, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold).Size(22);

            if (!string.IsNullOrEmpty(facts.AnchorUnlocked)) BuildAnchorChoice(list, facts.AnchorUnlocked);

            if (!string.IsNullOrEmpty(preview) && !optionalAfterFinale)
            {
                var next = _app.Catalogue.Level(preview);
                var card = UIKit.Panel(list, "NextPreview", Color.white, 14); card.Size(56);
                var nl = UIKit.Label(card.transform, "Name", $"Next · Kitchen {next.OrdinalInArea}: {Strings.Get(next.PurposeKey)}", 13, UIKit.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
                nl.rectTransform.TopLeft(10, 4, 300, 20);
                var objs = next.FirstBoard.Objects;
                for (int i = 0; i < objs.Count && i < 9; i++)
                {
                    var ic = UIKit.Icon(card.transform, "P" + i, SpriteFactory.Object(objs[i].AssetId), 24);
                    ic.rectTransform.TopLeft(10 + i * 26, 26, 24, 24);
                }
                S.Telemetry.Emit("next_preview_seen", $"\"levelId\":\"{_level.LevelId}\",\"next\":\"{preview}\"");
            }

            var buttons = UIKit.Rect(list, "Buttons"); buttons.Size(isFinale ? 150 : 104);
            UIKit.VStack(buttons, 6);
            if (optionalAfterFinale)
            {
                UIKit.Button(buttons, "Home", Strings.Get("action.home"), () => ResultAction("home", _app.ShowHome), primary: true).Size(46);
                UIKit.Button(buttons, "Inspect", Strings.Get("action.inspect"), Inspect).Size(44);
            }
            else if (isFinale)
            {
                var r1 = UIKit.Rect(buttons, "R1"); r1.Size(46); UIKit.HStack(r1, 6);
                UIKit.Button(r1, "InspectKitchen", Strings.Get("action.inspect_kitchen"), () => ResultAction("inspect_kitchen", _app.ShowProject), primary: true, fontSize: 14);
                UIKit.Button(r1, "Explore", Strings.Get("action.explore_places"), () => ResultAction("explore", _app.ShowArea), fontSize: 14);
                var r2 = UIKit.Rect(buttons, "R2"); r2.Size(44); UIKit.HStack(r2, 6);
                UIKit.Button(r2, "ReplayScenes", Strings.Get("action.replay_scenes"), () => ResultAction("replay_scenes", _app.ShowArea), fontSize: 14);
                if (Progression.LaterPuzzles(Save, _app.Catalogue, _level.AreaId).Any())
                    UIKit.Button(r2, "Later", Strings.Get("action.later_puzzles"), () => ResultAction("later", _app.ShowArea), fontSize: 14);
                var r3 = UIKit.Rect(buttons, "R3"); r3.Size(44); UIKit.HStack(r3, 6);
                UIKit.Button(r3, "Inspect", Strings.Get("action.inspect"), Inspect, fontSize: 14);
                UIKit.Button(r3, "Home", Strings.Get("action.home"), () => ResultAction("home", _app.ShowHome), fontSize: 14);
            }
            else
            {
                if (!string.IsNullOrEmpty(preview))
                    UIKit.Button(buttons, "Next", Strings.Get("action.next"), () => ResultAction("next", () => _app.OpenFromList(_app.Catalogue.Level(preview))), primary: true).Size(48);
                var row = UIKit.Rect(buttons, "Row"); row.Size(44); UIKit.HStack(row, 6);
                UIKit.Button(row, "Home", Strings.Get("action.home"), () => ResultAction("home", _app.ShowHome), primary: string.IsNullOrEmpty(preview), fontSize: 14);
                UIKit.Button(row, "Replay", Strings.Get("action.replay"), () => ResultAction("replay", Replay), fontSize: 14);
                UIKit.Button(row, "Inspect", Strings.Get("action.inspect"), Inspect, fontSize: 14);
                if (sampleEnd && Progression.LaterPuzzles(Save, _app.Catalogue, _level.AreaId).Any())
                    UIKit.Button(row, "Later", Strings.Get("action.later_puzzles"), () => ResultAction("later", _app.ShowArea), fontSize: 13);
            }
        }

        void BuildAnchorChoice(RectTransform list, string anchorId)
        {
            var anchor = KitchenCatalogue.Anchors.First(a => a.AnchorId == anchorId);
            var card = UIKit.Panel(list, "Anchor", SpriteFactory.Hex("#FFF1D6"), 14); card.Size(104);
            var t = UIKit.Label(card.transform, "Title", "Kitchen project: " + Strings.Get("anchor." + anchorId), 14, UIKit.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            t.rectTransform.TopLeft(10, 6, 310, 20);
            var s = UIKit.Label(card.transform, "Sub", $"Free default applied. Optional: {Strings.Get("variant." + anchor.VariantId)} · {anchor.VariantPrice} coins (also in Kitchen Project)", 11, UIKit.InkSoft, TextAnchor.UpperLeft);
            s.rectTransform.TopLeft(10, 26, 310, 30);
            var row = UIKit.Rect(card.transform, "Row").TopLeft(10, 58, 310, 40);
            UIKit.HStack(row, 6);
            UIKit.Button(row, "KeepDefault", "Keep default", () => { card.gameObject.SetActive(false); }, fontSize: 13);
            if (Save.ownedCosmetics.Contains(anchor.VariantId))
            {
                UIKit.Button(row, "Equip", "Use " + Strings.Get("variant." + anchor.VariantId), () =>
                {
                    S.Commit(x => Progression.Equip(x, anchorId, anchor.VariantId));
                    card.gameObject.SetActive(false);
                }, fontSize: 12);
            }
            else
            {
                var buy = UIKit.Button(row, "Buy", $"Buy · {anchor.VariantPrice}", () =>
                {
                    _app.ShowChoice($"Buy {Strings.Get("variant." + anchor.VariantId)} for {anchor.VariantPrice} coins?",
                        ($"Buy for {anchor.VariantPrice}", () =>
                        {
                            int before = Save.wallet;
                            if (S.Commit(x => Progression.Purchase(x, anchor.VariantId)))
                            {
                                S.Telemetry.Emit("cosmetic_purchase", $"\"variant\":\"{anchor.VariantId}\",\"cost\":{anchor.VariantPrice},\"walletDelta\":{Save.wallet - before}");
                                _app.Toast(Strings.Get("variant." + anchor.VariantId) + " added");
                                card.gameObject.SetActive(false);
                            }
                            else if (S.SaveBlocked) _app.ShowSaveError(null);
                        }));
                }, primary: false, fontSize: 13);
                buy.interactable = Progression.CanPurchase(Save, anchor.VariantId);
            }
        }

        void ResultAction(string action, Action go)
        {
            if (_navLock) return; // rapid taps coalesce to one navigation
            _navLock = true;
            S.Telemetry.Emit("result_action", $"\"levelId\":\"{_level.LevelId}\",\"action\":\"{action}\"");
            go();
            if (_app.HasModal) _navLock = false; // a choice opened instead of navigating
        }

        void Inspect()
        {
            if (_resultSheet == null) return;
            _inspecting = true;
            S.Telemetry.Emit("scene_inspect_start", $"\"levelId\":\"{_level.LevelId}\"");
            float start = Time.unscaledTime;
            _resultSheet.gameObject.SetActive(false);
            var back = UIKit.Button(_overlay, "InspectBack", "‹ " + Strings.Get("action.back"), null);
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(0, 24);
            brt.sizeDelta = new Vector2(160, 48);
            back.onClick.AddListener(() =>
            {
                S.Telemetry.Emit("scene_inspect_end", $"\"levelId\":\"{_level.LevelId}\",\"seconds\":{(Time.unscaledTime - start):0.0}");
                Destroy(back.gameObject);
                _resultSheet.gameObject.SetActive(true);
                _inspecting = false;
            });
        }

        void Replay()
        {
            // Full replay creates a fresh attempt for the whole scene (Level 20 with a saved puzzle offers choices).
            if (Progression.HasLaterOptionalBoard(Save, _level)) { _navLock = false; _app.OpenFromList(_level); return; }
            if (!S.Commit(s => { Progression.CreateAttempt(s, _level, "replay", S.NewId, S.Config.Signature(s.settings.reducedMotion)); return true; }))
            { _navLock = false; _app.ShowSaveError(null); return; }
            _app.OpenLevel(_level.LevelId, EntryMode.Default);
        }

        // ---------- Pause, defer, restart, hint ----------

        void OpenPause()
        {
            if (_phase != Phase.Ready && _phase != Phase.Playing) return;
            CancelDrag();
            var prev = _phase;
            FlushTimeToSave();
            _phase = Phase.Paused;
            S.Telemetry.Emit("pause", Fields());
            var panel = _app.OpenModal("Pause", _def.IsPlanning ? 470 : 360);
            var list = UIKit.Rect(panel, "List").Stretch(16, 16, 16, 16);
            UIKit.VStack(list, 7);
            UIKit.Label(list, "Title", Strings.Get(_level.PurposeKey), 18, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Size(28);
            UIKit.Button(list, "Resume", Strings.Get("action.resume"), () => { _app.CloseModal(); _phase = prev; }, primary: true).Size(46);
            if (_def.IsPlanning)
            {
                UIKit.Button(list, "Hint", Strings.Get("action.hint"), () => { _app.CloseModal(); _phase = prev == Phase.Ready ? Phase.Playing : prev; RequestHint(); }).Size(44);
                if (TeachingIds.RequiredFor(_def.TemplateId).Count > 0)
                    UIKit.Button(list, "Lesson", Strings.Get("action.watch_example"), () =>
                    {
                        _app.CloseModal(); _phase = prev;
                        StartCoroutine(PlayLessons(TeachingIds.RequiredFor(_def.TemplateId).ToList()));
                    }).Size(44);
            }
            if (_deferButton != null)
                UIKit.Button(list, "Defer", _deferButton.GetComponentInChildren<Text>().text, () => { _app.CloseModal(); OnDefer(); }).Size(44);
            UIKit.Button(list, "Restart", Strings.Get("action.restart_board"), () => { _app.CloseModal(); RestartBoard(); }).Size(44);
            UIKit.Button(list, "Explore", Strings.Get("action.save_explore"), () => { _app.CloseModal(); Leave(_app.ShowExplore, "explore"); }).Size(44);
            UIKit.Button(list, "Home", Strings.Get("action.save_home"), () => { _app.CloseModal(); Leave(_app.ShowHome, "home"); }).Size(44);
            UIKit.Button(list, "Settings", Strings.Get("action.settings"), () => _app.ShowSettingsModal(() => { _phase = prev; })).Size(44);
        }

        void FlushTimeToSave()
        {
            long ms = FlushMs();
            if (ms <= 0 || Attempt == null) return;
            if (!S.Commit(s => { s.Attempt(_attemptId).Board(_def.BoardId).activeMs += ms; return true; })) _unsavedMs += ms;
        }

        void Leave(Action go, string reason)
        {
            CancelDrag();
            FlushTimeToSave();
            if (S.SaveBlocked) { _app.ShowSaveError(() => Leave(go, reason)); return; }
            S.Telemetry.Emit("level_exit", Fields($"\"reason\":\"{reason}\",\"sorted\":{_state.SortedCount}"));
            go();
        }

        void OnDefer()
        {
            if (_phase == Phase.Completing || _phase == Phase.Result || _navLock) return;
            CancelDrag();
            StopHand();
            long ms = FlushMs();
            bool optional = !_def.Required;
            if (optional)
            {
                bool sealedAlready = Save.HasSeal(_level.LevelId);
                if (!S.Commit(s =>
                {
                    var a = s.Attempt(_attemptId);
                    a.Current.activeMs += ms;
                    if (sealedAlready) { a.Current.status = BoardStatus.Deferred; return true; }
                    return Progression.DeferOptionalBoard(s, _level, a);
                })) { _unsavedMs += ms; _app.ShowSaveError(null); return; }
                S.Telemetry.Emit("optional_board_deferred", Fields());
                if (sealedAlready) { _app.Toast(Strings.Get("status.saved_for_later")); _app.ShowHome(); return; }
                BuildForCurrentBoard();
                return;
            }

            int cursorBefore = Save.Cursor(_level.AreaId);
            if (!S.Commit(s =>
            {
                s.Attempt(_attemptId).Current.activeMs += ms;
                return Progression.DeferLevel(s, _app.Catalogue, _level, s.Attempt(_attemptId));
            })) { _unsavedMs += ms; if (S.SaveBlocked) _app.ShowSaveError(null); return; }
            S.Telemetry.Emit("level_deferred", Fields($"\"reason\":\"player_choice\",\"cursorBefore\":{cursorBefore},\"cursorAfter\":{Save.Cursor(_level.AreaId)}"));
            _app.Toast(Strings.Get("status.saved_for_later"));
            _navLock = true;
            // Open the next installed scene in this area (the area cursor), or Home at a sample endpoint.
            var next = Progression.CursorLevel(Save, _app.Catalogue, _level.AreaId);
            if (next != null) _app.OpenFromList(next);
            else _app.ShowHome();
        }

        void RestartBoard()
        {
            CancelDrag();
            long ms = FlushMs();
            if (!S.Commit(s => { Progression.RestartBoard(s.Attempt(_attemptId).Board(_def.BoardId), S.NewId); return true; }))
            { _unsavedMs += ms; _app.ShowSaveError(null); return; }
            S.Telemetry.Emit("board_restart", Fields());
            BuildForCurrentBoard();
        }

        void RequestHint()
        {
            if (_phase != Phase.Playing && _phase != Phase.Ready) return;
            if (_phase == Phase.Ready) _phase = Phase.Playing;
            var desired = HintService.DesiredAllocation(_level.LevelId, _level.BoardContentVersion, _def);
            var hint = HintService.Next(_state, desired);
            if (hint.Kind == HintKind.None)
            {
                _app.Toast("Try Restart Board, or leave this puzzle for later.");
                return;
            }
            ClearHint();
            _activeHint = hint;
            _view.Objects[hint.ObjectId].SetHint(true);
            string text;
            if (hint.Kind == HintKind.Return)
            {
                _view.ShowReturnArea(true);
                text = "Move this back to the counter to make room";
            }
            else
            {
                _view.Targets[hint.TargetId].SetHint(true);
                text = hint.Kind == HintKind.Transfer ? "Try moving this to the highlighted container" : "This fits well in the highlighted container";
            }
            _app.Toast(text, 2.2f);
            // Assistance counts only once an answer-revealing suggestion is displayed.
            S.Commit(s => { Progression.MarkAssistance(s.Attempt(_attemptId).Board(_def.BoardId), "hint"); return true; });
            S.Telemetry.Emit("hint_used", Fields($"\"kind\":\"{hint.Kind}\",\"object\":\"{hint.ObjectId}\",\"target\":\"{hint.TargetId}\""));
            _idleSeconds = 0;
        }

        void ClearHint()
        {
            if (_activeHint.ObjectId == null || _view == null) return;
            if (_view.Objects.TryGetValue(_activeHint.ObjectId, out var o)) o.SetHint(false);
            if (_activeHint.TargetId != null && _view.Targets.TryGetValue(_activeHint.TargetId, out var t)) t.SetHint(false);
            _view.ShowReturnArea(false);
            _activeHint = default;
        }

        // ---------- Lifecycle ----------

        void OnApplicationPause(bool paused)
        {
            if (paused) Interrupt();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) Interrupt();
        }

        void Interrupt()
        {
            if (_phase == Phase.Playing || _phase == Phase.Ready)
            {
                CancelDrag();
                FlushTimeToSave();
                if (_phase == Phase.Playing && !_app.HasModal) OpenPause();
            }
        }

        // ---------- Debug / capture hooks (used by the board-pass screenshot test and for manual QA) ----------

        ObjectView _debugDrag;

        /// <summary>Closes a lesson / optional-board offer if one is showing (returns false when there was none).</summary>
        public bool DebugDismissOffers()
        {
            bool any = false;
            foreach (Transform child in _overlay)
                if (child.name == "LessonOffer" || child.name == "OptionalReady") { Destroy(child.gameObject); any = true; }
            return any;
        }

        /// <summary>Starts play as a first pickup would: closes the lesson/optional offers and the tutorial line.</summary>
        void DebugBegin()
        {
            foreach (Transform child in _overlay)
                if (child.name == "LessonOffer" || child.name == "OptionalReady") Destroy(child.gameObject);
            if (_phase == Phase.Ready) { _phase = Phase.Playing; StopHand(); SetInstruction(_instructionDefault); }
        }

        /// <summary>Lifts the first unsorted item over a container that accepts it (hover-valid state). Commits nothing.</summary>
        public bool DebugHoverValid()
        {
            var o = _def.Objects.FirstOrDefault(x => !_state.IsAssigned(x.InstanceId));
            var t = o == null ? null : _def.Targets.FirstOrDefault(x => x.AcceptsCategory(o.CategoryId));
            if (t == null) return false;
            DebugBegin();
            var v = _view.Objects[o.InstanceId];
            _debugDrag = v;
            v.transform.SetParent(_view.DragLayer, true);
            v.SetDragging(true);
            v.Place(_view.TargetCenter(t.TargetId) + new Vector2(10, -6));
            _view.SetHover(t.TargetId);
            return true;
        }

        public bool DebugCancelDrag()
        {
            if (_debugDrag == null) return false;
            _debugDrag = null;
            _view.SetHover(null);
            _view.SnapAll(_state, true);
            return true;
        }

        /// <summary>Drops the first unsorted item on a container that refuses it (wrong kind or full): the real reject path.</summary>
        public bool DebugReject()
        {
            DebugBegin();
            foreach (var o in _def.Objects.Where(x => !_state.IsAssigned(x.InstanceId)))
                foreach (var t in _def.Targets)
                {
                    var proposal = new DropProposal(o.InstanceId, t.TargetId, false);
                    var result = RuleEvaluator.Evaluate(_state, proposal);
                    if (result.Kind != DropKind.WrongCategory && result.Kind != DropKind.Full) continue;
                    var v = _view.Objects[o.InstanceId];
                    v.Place(_view.TargetCenter(t.TargetId));
                    Reject(v, result);
                    return true;
                }
            return false;
        }

        /// <summary>Sorts items through the real Accept path (solver order) until the fraction is reached.</summary>
        public bool DebugSortFraction(float fraction)
        {
            DebugBegin();
            if (_phase != Phase.Playing) return false;
            var solution = Solver.Solve(_def).Allocation;
            if (solution == null) return false;
            int goal = Mathf.CeilToInt(_def.Objects.Count * Mathf.Clamp01(fraction));
            foreach (var o in _def.Objects)
            {
                if (_state.SortedCount >= goal || _phase != Phase.Playing) break;
                if (_state.IsAssigned(o.InstanceId)) continue;
                var proposal = new DropProposal(o.InstanceId, solution[o.InstanceId], false);
                var result = RuleEvaluator.Evaluate(_state, proposal);
                if (result.Kind != DropKind.Accept) continue;
                Accept(_view.Objects[o.InstanceId], proposal, result);
            }
            return true;
        }

        public void Unload()
        {
            if (_phase == Phase.Unloading) return;
            CancelDrag();
            if (_phase == Phase.Playing || _phase == Phase.Ready || _phase == Phase.Paused) FlushTimeToSave();
            _phase = Phase.Unloading;
            StopAllCoroutines();
        }
    }

    sealed class LessonBlocker : MonoBehaviour { }

    /// <summary>
    /// Skippable ghost-board demonstrations on a separate overlay. Never moves the player's real objects
    /// or changes the real allocation.
    /// </summary>
    sealed class GhostDemo : MonoBehaviour
    {
        RectTransform _rt;
        readonly List<RectTransform> _ghosts = new List<RectTransform>();
        readonly List<RectTransform> _bins = new List<RectTransform>();
        readonly List<Text> _counts = new List<Text>();
        Text _caption;
        string _lesson;

        public static GhostDemo Build(RectTransform parent, string lesson, Action onSkip)
        {
            var scrim = UIKit.Panel(parent, "GhostDemo", new Color(1, 0.98f, 0.94f, 0.97f), 0, raycast: true);
            scrim.rectTransform.Stretch();
            scrim.gameObject.AddComponent<LessonBlocker>();
            var d = scrim.gameObject.AddComponent<GhostDemo>();
            d._rt = scrim.rectTransform;
            d._lesson = lesson;
            var title = UIKit.Label(d._rt, "Title", "Example", 18, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.TopLeft(0, 30, 360, 30);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            d._caption = UIKit.Label(d._rt, "Caption", Strings.Get("lesson." + lesson), 14, UIKit.InkSoft);
            d._caption.rectTransform.TopLeft(20, 64, 320, 60);
            var skip = UIKit.Button(d._rt, "Skip", "Skip", () => onSkip());
            ((RectTransform)skip.transform).TopLeft(130, 470, 100, 44);
            d.Setup();
            return d;
        }

        RectTransform Bin(string label, int cap, float x, float y, float w)
        {
            var bin = UIKit.Panel(_rt, "Bin", new Color(0.9f, 0.82f, 0.68f, 0.8f), 12);
            bin.rectTransform.TopLeft(x, y, w, 90);
            var l = UIKit.Label(bin.transform, "L", label, 12, UIKit.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            l.rectTransform.TopLeft(6, 4, w - 40, 18);
            var c = UIKit.Label(bin.transform, "C", "0/" + cap, 12, UIKit.Ink, TextAnchor.UpperRight, FontStyle.Bold);
            c.rectTransform.TopLeft(w - 46, 4, 40, 18);
            _bins.Add(bin.rectTransform);
            _counts.Add(c);
            return bin.rectTransform;
        }

        RectTransform Ghost(string asset, float x, float y, bool large)
        {
            var g = UIKit.Icon(_rt, "Ghost_" + asset, SpriteFactory.Object(asset), 36, new Color(1, 1, 1, 0.75f));
            g.rectTransform.anchorMin = g.rectTransform.anchorMax = new Vector2(0, 1);
            g.rectTransform.anchoredPosition = new Vector2(x, -y);
            if (large)
            {
                var b = UIKit.Icon(g.transform, "Badge", SpriteFactory.Circle(), 16, UIKit.Ink);
                b.rectTransform.anchoredPosition = new Vector2(14, 14);
                var t = UIKit.Label(b.transform, "T", "2", 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Stretch();
            }
            _ghosts.Add(g.rectTransform);
            return g.rectTransform;
        }

        void Setup()
        {
            switch (_lesson)
            {
                case TeachingIds.SharedCapacityRecovery:
                    Bin("Fruit only", 2, 20, 260, 110);
                    Bin("Shared", 6, 140, 260, 200);
                    string[] a = { "apple", "banana", "orange", "strawberry", "carrot", "pepper", "broccoli", "corn" };
                    for (int i = 0; i < a.Length; i++) Ghost(a[i], 40 + (i % 4) * 90, 150 + (i / 4) * 50, false);
                    break;
                case TeachingIds.WeightedUnits:
                    Bin("Drinks", 2, 40, 260, 120);
                    Bin("Shared", 6, 180, 260, 140);
                    Ghost("water_bottle", 90, 170, true);
                    Ghost("milk_carton", 200, 170, false);
                    Ghost("juice_carton", 270, 170, false);
                    break;
                default:
                    Bin("Fruit/Veg", 2, 14, 260, 104);
                    Bin("Veg/Drinks", 2, 128, 260, 104);
                    Bin("Fruit/Drinks", 2, 242, 260, 104);
                    Ghost("apple", 60, 160, false); Ghost("orange", 130, 160, false);
                    Ghost("carrot", 200, 160, false); Ghost("corn", 270, 160, false);
                    Ghost("milk_carton", 180, 215, true);
                    break;
            }
        }

        Vector2 SlotIn(int bin, int index)
        {
            var b = _bins[bin];
            float x = b.anchoredPosition.x + 26 + (index % 4) * 42, y = -b.anchoredPosition.y + 46 + (index / 4) * 40;
            return new Vector2(x, y);
        }

        IEnumerator MoveGhost(int g, int bin, int index, Func<bool> skipped, bool reduced)
        {
            var rt = _ghosts[g];
            var from = new Vector2(rt.anchoredPosition.x, -rt.anchoredPosition.y);
            var to = SlotIn(bin, index);
            float dur = reduced ? 0.05f : 0.55f;
            float t = 0;
            while (t < dur && !skipped()) { t += Time.unscaledDeltaTime; var p = Vector2.Lerp(from, to, UIKit.EaseOut(Mathf.Clamp01(t / dur))); rt.anchoredPosition = new Vector2(p.x, -p.y); yield return null; }
            rt.anchoredPosition = new Vector2(to.x, -to.y);
        }

        IEnumerator Wait(float s, Func<bool> skipped) { float t = 0; while (t < s && !skipped()) { t += Time.unscaledDeltaTime; yield return null; } }

        void SetCount(int bin, int used, int cap) => _counts[bin].text = used + "/" + cap;

        public IEnumerator Play(Func<bool> skipped, bool reduced)
        {
            switch (_lesson)
            {
                case TeachingIds.SharedCapacityRecovery:
                    _caption.text = "Putting all the fruit in the shared tray is allowed…";
                    for (int i = 0; i < 4 && !skipped(); i++) { yield return MoveGhost(i, 1, i, skipped, reduced); SetCount(1, i + 1, 6); }
                    for (int i = 4; i < 6 && !skipped(); i++) { yield return MoveGhost(i, 1, i, skipped, reduced); SetCount(1, i + 1, 6); }
                    _caption.text = "…but then only two vegetables fit. The tray is full.";
                    yield return Wait(2f, skipped);
                    _caption.text = "Move two fruit into the Fruit-only basket to make room.";
                    for (int k = 0; k < 2 && !skipped(); k++) { yield return MoveGhost(k, 0, k, skipped, reduced); SetCount(0, k + 1, 2); SetCount(1, 5 - k, 6); }
                    // Re-pack the shared tray visually
                    for (int i = 6; i < 8 && !skipped(); i++) { yield return MoveGhost(i, 1, i - 4 + 2, skipped, reduced); SetCount(1, 4 + (i - 5), 6); }
                    _caption.text = "Everything fits. You can always move things back for free.";
                    yield return Wait(2.2f, skipped);
                    break;
                case TeachingIds.WeightedUnits:
                    _caption.text = "The big bottle has a 2 badge: it fills both spaces of the Drinks crate.";
                    yield return MoveGhost(0, 0, 0, skipped, reduced); SetCount(0, 2, 2);
                    yield return Wait(1.2f, skipped);
                    _caption.text = "Small items use one space each. The rest go in the shared tray.";
                    yield return MoveGhost(1, 1, 0, skipped, reduced); SetCount(1, 1, 6);
                    yield return MoveGhost(2, 1, 1, skipped, reduced); SetCount(1, 2, 6);
                    yield return Wait(2f, skipped);
                    break;
                default:
                    _caption.text = Strings.Get("lesson.overlapping_baskets");
                    yield return MoveGhost(0, 0, 0, skipped, reduced); SetCount(0, 1, 2);
                    yield return MoveGhost(1, 0, 1, skipped, reduced); SetCount(0, 2, 2);
                    yield return MoveGhost(2, 1, 0, skipped, reduced); SetCount(1, 1, 2);
                    yield return MoveGhost(3, 1, 1, skipped, reduced); SetCount(1, 2, 2);
                    _caption.text = "The Fruit/Drinks basket stayed completely empty for the large drink.";
                    yield return MoveGhost(4, 2, 0, skipped, reduced); SetCount(2, 2, 2);
                    yield return Wait(2.2f, skipped);
                    break;
            }
        }
    }
}
