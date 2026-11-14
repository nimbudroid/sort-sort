using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SortEverything.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Renders one board from committed state. Coordinates are board-local, top-left origin, y downward.
    /// Never reads animation positions as assignments; <see cref="SnapAll"/> reconstructs from the model.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public const float Width = 360f;
        public const float SourceHeight = 156f;
        public const float TargetHeight = 322f;
        public const float SlotPitch = 52f;
        public const float Header = 36f;
        public const float Forgiveness = 6f;

        public RectTransform Root { get; private set; }
        public RectTransform SourceRegion { get; private set; }
        public RectTransform TargetRegion { get; private set; }
        public RectTransform ObjectLayer { get; private set; }
        public RectTransform DragLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }
        public float SourceTop { get; private set; }
        public float TargetTop { get; private set; }

        public BoardDef Def { get; private set; }
        public readonly Dictionary<string, ObjectView> Objects = new Dictionary<string, ObjectView>();
        public readonly Dictionary<string, TargetView> Targets = new Dictionary<string, TargetView>();
        public Image ReturnArea { get; private set; }
        public Image PropBacking { get; private set; }
        public Camera UiCamera;
        public Func<ObjectView, PointerEventData, bool> PointerDown;
        public Action<ObjectView, PointerEventData> PointerDrag;
        public Action<ObjectView, PointerEventData> PointerUp;
        public bool ReducedMotion;

        public static BoardView Create(Transform parent, BoardDef def, float sourceTop, float targetTop, Camera cam, string purposeProp)
        {
            var rt = UIKit.Rect(parent, "Board_" + def.BoardId);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(-Width / 2, 0);
            rt.sizeDelta = new Vector2(Width, targetTop + TargetHeight);
            var v = rt.gameObject.AddComponent<BoardView>();
            v.Root = rt; v.Def = def; v.UiCamera = cam; v.SourceTop = sourceTop; v.TargetTop = targetTop;
            v.Build(purposeProp);
            return v;
        }

        void Build(string purposeProp)
        {
            BuildRoom();

            // Source region: a cutting mat on the counter, with a return-area guide on planning boards.
            var src = UIKit.Panel(Root, "SourceRegion", new Color(1f, 0.97f, 0.9f, 0.28f), 18);
            SourceRegion = src.rectTransform.TopLeft(6, SourceTop, Width - 12, SourceHeight);
            ReturnArea = UIKit.Panel(SourceRegion, "ReturnArea", new Color(1, 1, 1, 0), 18);
            ReturnArea.rectTransform.Stretch();
            var returnLabel = UIKit.Label(ReturnArea.transform, "Label", "Return to counter", 12, UIKit.InkSoft, TextAnchor.LowerCenter);
            returnLabel.rectTransform.Stretch(4, 4, 4, 4);
            ReturnArea.gameObject.SetActive(false);

            TargetRegion = UIKit.Rect(Root, "TargetRegion").TopLeft(0, TargetTop, Width, TargetHeight);
            PropBacking = UIKit.Panel(TargetRegion, "PurposeProp", PropColor(purposeProp), 22);
            PropBacking.rectTransform.Stretch(2, -6, 2, -6);
            var (pattern, patternTint) = KitchenArt.PropPattern(purposeProp);
            KitchenArt.Tiled(PropBacking.transform, "Pattern", pattern, patternTint).rectTransform.Stretch(6, 6, 6, 6);
            var tag = UIKit.Panel(PropBacking.transform, "Tag", new Color(1, 0.98f, 0.94f, 0.95f), 8);
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(1, 0);
            tag.rectTransform.pivot = new Vector2(1, 0);
            tag.rectTransform.anchoredPosition = new Vector2(-8, 4);
            tag.rectTransform.sizeDelta = new Vector2(140, 18);
            var propLabel = UIKit.Label(tag.transform, "Label", PropName(purposeProp), 11, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            propLabel.rectTransform.Stretch(4, 0, 4, 0);
            _propGroup = PropBacking.gameObject.AddComponent<CanvasGroup>();
            _propGroup.alpha = 0f;
            _propGroup.blocksRaycasts = false;

            foreach (var t in Def.Targets)
            {
                var tv = TargetView.Create(TargetRegion, Def, t);
                Targets[t.TargetId] = tv;
            }

            ObjectLayer = UIKit.Rect(Root, "Objects").Stretch();
            OverlayLayer = UIKit.Rect(Root, "Overlay").Stretch();
            DragLayer = UIKit.Rect(Root, "Drag").Stretch();

            // Foreground priority: later siblings receive raycasts first; keep authored order stable.
            foreach (var o in Def.Objects)
            {
                var ov = ObjectView.Create(ObjectLayer, this, o, Def.IsPlanning);
                Objects[o.InstanceId] = ov;
            }
        }

        static Color PropColor(string prop)
        {
            switch (prop)
            {
                case "serving_cloth_tray": return SpriteFactory.Hex("#F3D7D0");
                case "pantry_shelf": return SpriteFactory.Hex("#D8B98F");
                case "carry_handle_tray": return SpriteFactory.Hex("#E9D3A8");
                case "prep_surface": return SpriteFactory.Hex("#F4EFE6");
                case "utensil_drawer": return SpriteFactory.Hex("#C6A27A");
                case "bottle_crate": return SpriteFactory.Hex("#B9CCD8");
                case "baking_mat": return SpriteFactory.Hex("#E7C7C0");
                default: return SpriteFactory.Hex("#E6CFA8");
            }
        }

        static string PropName(string prop)
        {
            switch (prop)
            {
                case "serving_cloth_tray": return "Serving tray ready";
                case "pantry_shelf": return "Pantry shelf stocked";
                case "carry_handle_tray": return "Ready to carry";
                case "prep_surface": return "Clean work surface";
                case "utensil_drawer": return "Drawer tidy";
                case "bottle_crate": return "Drinks station set";
                case "baking_mat": return "Baking station ready";
                default: return "Breakfast tray ready";
            }
        }

        CanvasGroup _propGroup;

        public void ShowPurposeProp(bool show, bool instant)
        {
            float a0 = _propGroup.alpha, a1 = show ? 1f : 0f;
            if (instant || ReducedMotion) { _propGroup.alpha = a1; return; }
            StartCoroutine(UIKit.Tween(0.35f, t => _propGroup.alpha = Mathf.Lerp(a0, a1, t)));
        }

        /// <summary>
        /// Kitchen room behind the board: wooden counter under the clutter and an open shelf unit holding the
        /// containers. Extends past the 360-unit board for wide screens. Never receives raycasts.
        /// </summary>
        void BuildRoom()
        {
            const float bleed = 400f;
            var room = UIKit.Rect(Root, "Room");
            room.TopLeft(-bleed, 0, Width + bleed * 2, TargetTop + TargetHeight + 40);

            // Backsplash shadow + counter top + front lip.
            float counterTop = SourceTop - 12, counterBottom = SourceTop + SourceHeight + 4;
            var shadow = UIKit.Panel(room, "CounterShadow", new Color(0.35f, 0.25f, 0.18f, 0.18f), 0);
            shadow.rectTransform.TopLeft(0, counterTop - 4, Width + bleed * 2, 6);
            var counter = KitchenArt.Tiled(room, "Counter", KitchenArt.Wood(), Color.white);
            counter.rectTransform.TopLeft(0, counterTop, Width + bleed * 2, counterBottom - counterTop);
            var lip = UIKit.Panel(room, "CounterLip", SpriteFactory.Hex("#A87444"), 0);
            lip.rectTransform.TopLeft(0, counterBottom, Width + bleed * 2, 8);
            var lipShade = UIKit.Panel(room, "LipShade", new Color(0.3f, 0.2f, 0.12f, 0.25f), 0);
            lipShade.rectTransform.TopLeft(0, counterBottom + 8, Width + bleed * 2, 5);

            // Open shelf unit for the containers.
            float shelfTop = TargetTop - 14, shelfBottom = TargetTop + TargetHeight + 10;
            var back = KitchenArt.Tiled(room, "ShelfBack", KitchenArt.ShelfBack(), new Color(1, 1, 1, 0.9f));
            back.rectTransform.TopLeft(0, shelfTop, Width + bleed * 2, shelfBottom - shelfTop);
            var topRail = KitchenArt.Tiled(room, "ShelfTop", KitchenArt.Wood(), SpriteFactory.Hex("#E0C29C"));
            topRail.rectTransform.TopLeft(0, shelfTop - 6, Width + bleed * 2, 10);
            var bottomRail = KitchenArt.Tiled(room, "ShelfBottom", KitchenArt.Wood(), SpriteFactory.Hex("#E0C29C"));
            bottomRail.rectTransform.TopLeft(0, shelfBottom - 4, Width + bleed * 2, 12);
            var bottomShade = UIKit.Panel(room, "ShelfShade", new Color(0.3f, 0.2f, 0.12f, 0.3f), 0);
            bottomShade.rectTransform.TopLeft(0, shelfBottom + 8, Width + bleed * 2, 6);
        }

        // ---------- Geometry ----------

        public Vector2 SourcePosition(ObjectDef o) =>
            new Vector2(6 + o.SourceX * (Width - 12), SourceTop + o.SourceY * SourceHeight);

        public Vector2 SlotPosition(string targetId, int slot)
        {
            var tv = Targets[targetId];
            return new Vector2(0, TargetTop) + tv.SlotCenter(slot);
        }

        public Vector2 ToBoard(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, UiCamera, out var local);
            return new Vector2(local.x, -local.y);
        }

        public Rect TargetRectBoard(string targetId)
        {
            var tv = Targets[targetId];
            var r = tv.LocalRect;
            return new Rect(r.x, r.y + TargetTop, r.width, r.height);
        }

        /// <summary>Target under the projected center within forgiving bounds; overlaps resolve by closest center then ID.</summary>
        public string TargetAt(Vector2 p)
        {
            string best = null; float bestD = float.MaxValue;
            foreach (var t in Def.Targets.OrderBy(t => t.TargetId, StringComparer.Ordinal))
            {
                var r = TargetRectBoard(t.TargetId);
                var ex = new Rect(r.x - Forgiveness, r.y - Forgiveness, r.width + 2 * Forgiveness, r.height + 2 * Forgiveness);
                if (!ex.Contains(p)) continue;
                float d = (r.center - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = t.TargetId; }
            }
            return best;
        }

        public bool OverSource(Vector2 p) => new Rect(6, SourceTop, Width - 12, SourceHeight).Contains(p);

        // ---------- State sync ----------

        public void SnapAll(BoardState s, bool beforeProfile)
        {
            foreach (var o in Def.Objects)
            {
                var view = Objects[o.InstanceId];
                view.transform.SetParent(ObjectLayer, false);
                view.SetDragging(false, Def.IsPlanning);
                var target = s.TargetOf(o.InstanceId);
                view.Place(target == null ? SourcePosition(o) : SlotPosition(target, s.SlotOf(o.InstanceId)));
                view.SetRotation(target == null && beforeProfile ? BeforeRotation(o.SourceIndex) : 0f);
                view.SetAssigned(target != null, Def.IsPlanning);
            }
            foreach (var t in Def.Targets) Targets[t.TargetId].SetUsage(s.UnitsIn(t.TargetId));
        }

        public static float BeforeRotation(int index)
        {
            float[] r = { -10, 7, -4, 9 };
            return r[index % 4];
        }

        public void SetHover(string targetId)
        {
            foreach (var kv in Targets) kv.Value.SetHover(kv.Key == targetId);
        }

        public void ShowReturnArea(bool show)
        {
            ReturnArea.gameObject.SetActive(show);
            ReturnArea.color = new Color(1, 1, 1, show ? 0.25f : 0f);
        }

        public IEnumerator Move(ObjectView v, Vector2 to, float duration, bool overshoot)
        {
            var from = v.Position;
            yield return UIKit.Tween(ReducedMotion ? Mathf.Min(duration, 0.08f) : duration, t =>
            {
                float e = overshoot && !ReducedMotion ? UIKit.EaseOutBack(t) : UIKit.EaseOut(t);
                v.Place(Vector2.LerpUnclamped(from, to, e));
            });
        }

        public void Pulse(TargetView tv, float amount = 0.05f, float duration = 0.25f)
        {
            if (ReducedMotion) return;
            StartCoroutine(UIKit.Tween(duration, t =>
            {
                float s = 1 + Mathf.Sin(t * Mathf.PI) * amount;
                tv.transform.localScale = new Vector3(s, s, 1);
            }));
        }

        public void Shake(TargetView tv)
        {
            if (ReducedMotion) return;
            var basePos = tv.Rt.anchoredPosition;
            StartCoroutine(UIKit.Tween(0.2f, t =>
            {
                tv.Rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * Mathf.PI * 6) * 4 * (1 - t), 0);
            }));
        }

        /// <summary>Final micro-action (300–700 ms). Never hides targets; contents stay visible.</summary>
        public IEnumerator FinishMicroAction(string kind)
        {
            if (ReducedMotion) { foreach (var tv in Targets.Values) tv.ShowFinished(kind, 1f); yield break; }
            switch (kind)
            {
                case "basket_alignment":
                    yield return UIKit.Tween(0.45f, t =>
                    {
                        foreach (var tv in Targets.Values)
                        {
                            tv.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2) * 2.5f * (1 - t));
                            tv.ShowFinished(kind, t);
                        }
                    });
                    break;
                case "tray_settle":
                    yield return UIKit.Tween(0.4f, t =>
                    {
                        foreach (var tv in Targets.Values)
                        {
                            tv.transform.localPosition = tv.BaseLocal + new Vector3(0, -Mathf.Sin(t * Mathf.PI) * 5, 0);
                            tv.ShowFinished(kind, t);
                        }
                    });
                    break;
                default:
                    yield return UIKit.Tween(0.5f, t => { foreach (var tv in Targets.Values) tv.ShowFinished(kind, UIKit.EaseOut(t)); });
                    break;
            }
        }
    }

    public sealed class TargetView : MonoBehaviour
    {
        public TargetDef Def;
        public RectTransform Rt;
        public Rect LocalRect; // within TargetRegion, top-left origin
        public Vector3 BaseLocal;
        Image _frame;
        Image _hover;
        Image _hint;
        Image _finish;
        Text _usage;
        Text _status;
        int _cols, _rows;
        Coroutine _statusRoutine;

        public static TargetView Create(RectTransform region, BoardDef board, TargetDef t)
        {
            var (w, h, cols, rows) = Size(board, t);
            float cx = t.X * BoardView.Width, cy = t.Y * BoardView.TargetHeight;
            if (t.RectProfile == "full_width") cy = t.Y * BoardView.TargetHeight;
            var frame = UIKit.Panel(region, "Target_" + t.TargetId, Color.white, 16);
            var rt = frame.rectTransform.Center(cx, cy, w, h);
            var v = frame.gameObject.AddComponent<TargetView>();
            v.Def = t; v.Rt = rt; v._frame = frame; v._cols = cols; v._rows = rows;
            v.LocalRect = new Rect(cx - w / 2, cy - h / 2, w, h);
            v.Build(board);
            v.BaseLocal = rt.localPosition;
            return v;
        }

        /// <summary>Card size and slot grid by profile (Section 25 target geometry).</summary>
        public static (float w, float h, int cols, int rows) Size(BoardDef board, TargetDef t)
        {
            int cols, rows;
            bool uNarrow = board.TemplateId == TemplateIds.Unlimited && t.RectProfile == "narrow";
            switch (t.SlotProfile)
            {
                case "one": cols = 1; rows = 1; break;
                case "two": cols = uNarrow ? 1 : 2; rows = uNarrow ? 2 : 1; break;
                case "three_vertical": cols = 1; rows = 3; break;
                case "four_vertical": cols = 1; rows = 4; break;
                case "four_square": cols = 2; rows = 2; break;
                case "six": cols = 3; rows = 2; break;
                case "nine": cols = 3; rows = 3; break;
                default: cols = 2; rows = 2; break;
            }
            float w;
            switch (t.RectProfile)
            {
                case "wide": case "quad": w = 0.46f * BoardView.Width; break;
                case "narrow": w = 0.31f * BoardView.Width; break;
                default: w = 0.96f * BoardView.Width; break;
            }
            float h;
            if (board.TemplateId == TemplateIds.P2 || board.TemplateId == TemplateIds.P3)
                h = t.RectProfile == "full_width" ? (board.TemplateId == TemplateIds.P3 ? 204f : 152f) : 100f;
            else if (board.TemplateId == TemplateIds.P4) h = 100f;
            else if (t.RectProfile == "quad") h = Mathf.Min(0.46f * BoardView.TargetHeight, BoardView.Header + rows * BoardView.SlotPitch + 8);
            else h = Mathf.Max(100f, BoardView.Header + rows * BoardView.SlotPitch + 12);
            if (board.TemplateId == TemplateIds.Unlimited && t.RectProfile == "wide")
            {
                // Uniform height across the pair so the layout reads calmly.
                int maxRows = 1;
                foreach (var other in board.Targets) maxRows = Mathf.Max(maxRows, other.SlotCount > 2 ? 2 : 1);
                h = Mathf.Max(h, BoardView.Header + maxRows * BoardView.SlotPitch + 12);
            }
            return (w, h, cols, rows);
        }

        void Build(BoardDef board)
        {
            bool shared = Def.Accepts.Count > 1;
            string styleKey = shared ? "shared" : Def.Accepts[0];
            var style = KitchenArt.ContainerStyle(styleKey);
            _frame.sprite = KitchenArt.Frame(style.key, style.rim, style.inner);
            _frame.type = Image.Type.Sliced;
            _frame.color = Color.white;
            var drop = _frame.gameObject.AddComponent<Shadow>();
            drop.effectColor = new Color(0.25f, 0.16f, 0.1f, 0.35f);
            drop.effectDistance = new Vector2(0, -3);
            if (style.pattern != null)
                KitchenArt.Tiled(Rt, "Pattern", style.pattern, style.patternTint).rectTransform.Stretch(7, BoardView.Header, 7, 7);
            if (styleKey == "kitchenware")
            {
                // Drawer front with a metal pull, below the slots so contents stay visible.
                var pull = UIKit.Panel(Rt, "DrawerPull", SpriteFactory.Hex("#9AA3AE"), 4);
                pull.rectTransform.anchorMin = pull.rectTransform.anchorMax = new Vector2(0.5f, 0);
                pull.rectTransform.anchoredPosition = new Vector2(0, 5);
                pull.rectTransform.sizeDelta = new Vector2(Mathf.Min(48, LocalRect.width * 0.4f), 5);
            }
            if (shared)
            {
                // Tray handles on both sides.
                foreach (var side in new[] { 0f, 1f })
                {
                    var h = UIKit.Panel(Rt, "Handle", style.rim, 4);
                    h.rectTransform.anchorMin = h.rectTransform.anchorMax = new Vector2(side, 0.5f);
                    h.rectTransform.anchoredPosition = new Vector2(side == 0 ? -3 : 3, 0);
                    h.rectTransform.sizeDelta = new Vector2(8, 26);
                    h.transform.SetAsFirstSibling();
                }
            }

            // Header: a paper label tag carrying category symbols, short label and usage (readable over any pattern).
            var labelTag = UIKit.Panel(Rt, "LabelTag", new Color(1f, 0.985f, 0.95f, 0.95f), 9);
            labelTag.rectTransform.TopLeft(5, 4, LocalRect.width - 10, BoardView.Header - 6);
            var header = UIKit.Rect(Rt, "Header").TopLeft(9, 3, LocalRect.width - 18, BoardView.Header - 4);
            float x = 0;
            float iconSize = Def.Accepts.Count >= 4 ? 18 : 22;
            foreach (var cat in Def.Accepts)
            {
                var ic = UIKit.Icon(header, "Icon_" + cat, SpriteFactory.CategoryIcon(cat), iconSize);
                ic.rectTransform.TopLeft(x, (BoardView.Header - 4 - iconSize) / 2, iconSize, iconSize);
                x += iconSize + 1;
            }
            string label = shared
                ? (Def.Accepts.Count == 2 && Def.TargetId.Length == 2 ? "" : Strings.Get("target.shared"))
                : Strings.Get("category." + Def.Accepts[0]);
            bool limited = !Def.IsUnlimited;
            // Narrow limited cards carry their count in a corner pill so the label keeps one readable line.
            bool pill = limited && Def.RectProfile == "narrow";
            float headerW = LocalRect.width - 18;
            var title = UIKit.Label(header, "Label", label, 13, UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.resizeTextMinSize = 9;
            title.rectTransform.TopLeft(x + 3, (BoardView.Header - 4 - 18) / 2, headerW - x - 3 - (limited && !pill ? 34 : 0), 18);
            if (limited && !pill)
            {
                _usage = UIKit.Label(header, "Usage", "", 13, UIKit.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
                _usage.rectTransform.TopLeft(headerW - 40, 0, 40, BoardView.Header - 4);
            }
            else if (pill)
            {
                var chip = UIKit.Panel(Rt, "UsagePill", UIKit.Ink, 9);
                chip.rectTransform.anchorMin = chip.rectTransform.anchorMax = new Vector2(1, 1);
                chip.rectTransform.pivot = new Vector2(1, 0.5f);
                chip.rectTransform.anchoredPosition = new Vector2(-6, 0);
                chip.rectTransform.sizeDelta = new Vector2(38, 18);
                _usage = UIKit.Label(chip.transform, "Usage", "", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                _usage.rectTransform.Stretch();
            }

            // Faint slot guides.
            for (int i = 0; i < Mathf.Max(1, Def.SlotCount); i++)
            {
                var c = SlotCenter(i);
                var g = UIKit.Panel(Rt, "Slot" + i, new Color(1, 1, 1, 0.42f), 10);
                g.rectTransform.Center(c.x - LocalRect.x, c.y - LocalRect.y, 46, 46);
            }

            _finish = UIKit.Panel(Rt, "Finish", new Color(1, 1, 1, 0), 16);
            _finish.rectTransform.Stretch();
            _hover = UIKit.Panel(Rt, "Hover", new Color(1, 1, 1, 0), 16);
            _hover.rectTransform.Stretch(-3, -3, -3, -3);
            _hint = UIKit.Panel(Rt, "Hint", new Color(0, 0, 0, 0), 16);
            _hint.rectTransform.Stretch(-5, -5, -5, -5);
            _status = UIKit.Label(Rt, "Status", "", 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var sbg = _status.gameObject.AddComponent<Shadow>();
            sbg.effectColor = new Color(0, 0, 0, 0.6f);
            _status.rectTransform.Center(LocalRect.width / 2, LocalRect.height / 2, LocalRect.width, 30);
            _status.gameObject.SetActive(false);
        }

        /// <summary>Slot center relative to TargetRegion top-left.</summary>
        public Vector2 SlotCenter(int slot)
        {
            int col = slot % _cols, row = slot / _cols;
            int count = Mathf.Max(1, Def.SlotCount);
            int rowCount = Mathf.Min(_cols, count - row * _cols); // center incomplete rows
            float gridW = rowCount * BoardView.SlotPitch - 4;
            float x0 = LocalRect.x + (LocalRect.width - gridW) / 2 + 24;
            float y0 = LocalRect.y + BoardView.Header + 4 + 24;
            return new Vector2(x0 + col * BoardView.SlotPitch, y0 + row * BoardView.SlotPitch);
        }

        public void SetUsage(int units)
        {
            if (_usage != null) _usage.text = $"{units}/{Def.Capacity}";
        }

        public void SetHover(bool on) => _hover.color = on ? new Color(1, 1, 1, 0.35f) : new Color(1, 1, 1, 0);

        public void SetHint(bool on) => _hint.color = on ? new Color(1f, 0.85f, 0.3f, 0.55f) : new Color(0, 0, 0, 0);

        public void ShowStatus(string text, MonoBehaviour host)
        {
            if (_statusRoutine != null) host.StopCoroutine(_statusRoutine);
            _statusRoutine = host.StartCoroutine(StatusRoutine(text));
        }

        IEnumerator StatusRoutine(string text)
        {
            _status.text = text;
            _status.gameObject.SetActive(true);
            var bg = _hover;
            bg.color = new Color(UIKit.Warn.r, UIKit.Warn.g, UIKit.Warn.b, 0.75f);
            yield return new WaitForSecondsRealtime(0.9f);
            _status.gameObject.SetActive(false);
            bg.color = new Color(1, 1, 1, 0);
        }

        /// <summary>Finished presentation: a soft glow and, for drawers, an opened drawer front that keeps contents visible.</summary>
        public void ShowFinished(string kind, float t)
        {
            _finish.color = new Color(1f, 0.97f, 0.85f, 0.18f * t);
            if (kind == "drawer_close" && _finish.transform.childCount == 0 && t > 0)
            {
                var front = UIKit.Panel(_finish.transform, "DrawerFront", UIKit.CategoryColor("kitchenware"), 8);
                front.rectTransform.Bottom(12, -6);
            }
            if (kind == "drawer_close" && _finish.transform.childCount > 0)
                ((RectTransform)_finish.transform.GetChild(0)).anchoredPosition = new Vector2(0, -6 + 4 * t);
        }
    }

    public sealed class ObjectView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        public ObjectDef Def;
        public RectTransform Rt;
        BoardView _board;
        RectTransform _art;
        Image _sprite;
        Image _shadow;
        GameObject _badge;
        Image _hint;
        public Vector2 Position { get; private set; }

        public static ObjectView Create(RectTransform layer, BoardView board, ObjectDef def, bool planning)
        {
            var rt = UIKit.Rect(layer, "Obj_" + def.InstanceId);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48, 48); // invisible hit region ≥ 48 units
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;
            var v = rt.gameObject.AddComponent<ObjectView>();
            v.Def = def; v.Rt = rt; v._board = board;

            v._hint = UIKit.Icon(rt, "Hint", SpriteFactory.Ring(), 58, new Color(1f, 0.8f, 0.2f, 0f));
            v._shadow = UIKit.Icon(rt, "Shadow", SpriteFactory.Shadow(), 40);
            v._shadow.rectTransform.sizeDelta = new Vector2(38, 14);
            v._shadow.rectTransform.anchoredPosition = new Vector2(0, -18);
            v._shadow.preserveAspect = false;
            v._art = UIKit.Rect(rt, "Art");
            v._art.sizeDelta = new Vector2(44, 44);
            v._sprite = v._art.gameObject.AddComponent<Image>();
            v._sprite.sprite = SpriteFactory.Object(def.AssetId);
            v._sprite.preserveAspect = true;
            v._sprite.raycastTarget = false;
            if (def.Large) v._art.localScale = new Vector3(1.08f, 1.08f, 1);

            // Unit badge: always visible for 2-unit objects, shown on pickup for 1-unit planning objects.
            var badge = UIKit.Icon(rt, "Badge", SpriteFactory.Circle(), 18, def.Large ? UIKit.Ink : UIKit.InkSoft);
            badge.rectTransform.anchoredPosition = new Vector2(17, 17);
            var bt = UIKit.Label(badge.transform, "Units", def.Units.ToString(), 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bt.rectTransform.Stretch();
            v._badge = badge.gameObject;
            v._badge.SetActive(def.Large);
            return v;
        }

        public void Place(Vector2 boardPos)
        {
            Position = boardPos;
            Rt.anchoredPosition = new Vector2(boardPos.x, -boardPos.y);
        }

        public void SetRotation(float deg) => _art.localRotation = Quaternion.Euler(0, 0, deg);

        public void SetAssigned(bool assigned, bool planning) { }

        public void SetDragging(bool on, bool planning)
        {
            float s = on ? 1.08f : 1f;
            Rt.localScale = new Vector3(s, s, 1);
            _shadow.rectTransform.sizeDelta = on ? new Vector2(46, 18) : new Vector2(38, 14);
            _shadow.rectTransform.anchoredPosition = on ? new Vector2(0, -26) : new Vector2(0, -18);
            _shadow.color = new Color(1, 1, 1, on ? 0.8f : 1f);
            _badge.SetActive(Def.Large || (on && planning));
            if (on) _art.localRotation = Quaternion.identity;
        }

        public void SetHint(bool on) => _hint.color = new Color(1f, 0.8f, 0.2f, on ? 0.95f : 0f);

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;
        public void OnPointerDown(PointerEventData e) => _board.PointerDown?.Invoke(this, e);
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => _board.PointerDrag?.Invoke(this, e);
        public void OnEndDrag(PointerEventData e) { }
        public void OnPointerUp(PointerEventData e) => _board.PointerUp?.Invoke(this, e);
    }
}
