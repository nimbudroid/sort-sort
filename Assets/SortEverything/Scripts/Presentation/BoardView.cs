using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SortEverything.Domain;
using SortEverything.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Renders one board from committed state as a kitchen moment: a countertop (play surface) with the item
    /// pile, and real containers sitting on the cabinet below it. Geometry comes from <see cref="BoardLayout"/>;
    /// coordinates are board-local, top-left origin, y downward. Never reads animation positions as assignments;
    /// <see cref="SnapAll"/> reconstructs everything from the model.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        /// <summary>Design width of the 360-unit reference phone (editor previews, ghost demos).</summary>
        public const float Width = 360f;
        public const float Forgiveness = 6f;

        public RectTransform Root { get; private set; }
        public RectTransform SourceRegion { get; private set; }
        public RectTransform ObjectLayer { get; private set; }
        public RectTransform FrontLayer { get; private set; }
        public RectTransform DragLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }
        public RectTransform FxLayer { get; private set; }
        public RectTransform ShineMask { get; private set; }
        public BoardLayout Layout { get; private set; }
        public float W => Layout.Width;
        public AreaData Area { get; private set; }

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
        MessLayer _mess;
        CanvasGroup _propGroup;
        public SceneData Scene { get; private set; }
        public bool HasComposition => Scene != null && Scene.HasComposition;
        SceneComposition _composition;
        CanvasGroup _binsGroup, _objectsGroup, _frontsGroup;

        public static BoardView Create(Transform parent, BoardDef def, BoardLayout layout, Camera cam, string purposeProp, AreaData area, MessData mess, SceneData scene = null)
        {
            var rt = UIKit.Rect(parent, "Board_" + def.BoardId);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(-layout.Width / 2, 0);
            rt.sizeDelta = new Vector2(layout.Width, layout.PlayH + layout.ContainersH);
            var v = rt.gameObject.AddComponent<BoardView>();
            v.Root = rt; v.Def = def; v.UiCamera = cam; v.Layout = layout; v.Area = area; v.Scene = scene;
            v.Build(purposeProp, mess);
            return v;
        }

        void Build(string purposeProp, MessData mess)
        {
            BuildRoom();

            // Play surface: the counter itself (no panel over it). Used for "put back" drops on planning boards.
            SourceRegion = UIKit.Rect(Root, "SourceRegion").TopLeft(Layout.PlayRect.x, Layout.PlayRect.y, Layout.PlayRect.width, Layout.PlayRect.height);
            ReturnArea = KitchenArt.Sliced(SourceRegion, "ReturnArea", KitchenArt.Frame("return", new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0f), 3, 18), new Color(1, 1, 1, 0.8f));
            ReturnArea.rectTransform.Stretch(4, 4, 4, 4);
            var returnLabel = UIKit.Label(ReturnArea.transform, "Label", "Drop on the counter to put it back", 12, UIKit.Ink, TextAnchor.LowerCenter, FontStyle.Bold);
            returnLabel.rectTransform.Stretch(4, 4, 4, 8);
            ReturnArea.gameObject.SetActive(false);

            var messHolder = UIKit.Rect(Root, "MessHolder").Stretch();

            // Purpose prop: appears behind the containers when the scene is finished (GDD Section 25 backings).
            var back = Layout.Bins.Values;
            float bTop = back.Min(b => b.Rect.yMin) - 6, bBottom = back.Max(b => b.Rect.yMax) + 6;
            PropBacking = UIKit.Panel(Root, "PurposeProp", PropColor(purposeProp), 18);
            PropBacking.rectTransform.TopLeft(4, bTop, W - 8, bBottom - bTop);
            var (pattern, patternTint) = KitchenArt.PropPattern(purposeProp);
            KitchenArt.Tiled(PropBacking.transform, "Pattern", pattern, patternTint).rectTransform.Stretch(6, 6, 6, 6);
            var tag = UIKit.Panel(PropBacking.transform, "Tag", new Color(1, 0.98f, 0.94f, 0.95f), 8);
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(1, 0);
            tag.rectTransform.pivot = new Vector2(1, 0);
            tag.rectTransform.anchoredPosition = new Vector2(-8, -16);
            tag.rectTransform.sizeDelta = new Vector2(150, 18);
            var propLabel = UIKit.Label(tag.transform, "Label", PropName(purposeProp), 11, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            propLabel.rectTransform.Stretch(4, 0, 4, 0);
            _propGroup = PropBacking.gameObject.AddComponent<CanvasGroup>();
            _propGroup.alpha = 0f;
            _propGroup.blocksRaycasts = false;

            var binLayer = UIKit.Rect(Root, "Containers").Stretch();
            ObjectLayer = UIKit.Rect(Root, "Objects").Stretch();
            FrontLayer = UIKit.Rect(Root, "ContainerFronts").Stretch();
            _binsGroup = binLayer.gameObject.AddComponent<CanvasGroup>();
            _objectsGroup = ObjectLayer.gameObject.AddComponent<CanvasGroup>();
            _frontsGroup = FrontLayer.gameObject.AddComponent<CanvasGroup>();
            if (HasComposition) _composition = SceneComposition.Create(Root);
            OverlayLayer = UIKit.Rect(Root, "Overlay").Stretch();
            ShineMask = UIKit.Rect(Root, "ShineMask").TopLeft(-400, 0, W + 800, Layout.PlayH - 10);
            ShineMask.gameObject.AddComponent<RectMask2D>();
            FxLayer = UIKit.Rect(Root, "Fx").Stretch();
            DragLayer = UIKit.Rect(Root, "Drag").Stretch();

            foreach (var t in Def.Targets)
                Targets[t.TargetId] = TargetView.Create(binLayer, FrontLayer, Def, t, Layout.Bins[t.TargetId], ContentDb.Containers.ForAccepts(t.Accepts, Area) ?? Fallback);

            // Draw order: big items underneath, small ones on top (raycasts hit the visible top item first).
            foreach (var o in Def.Objects.OrderBy(o => Layout.Pile[o.InstanceId].Layer))
                Objects[o.InstanceId] = ObjectView.Create(ObjectLayer, this, o);

            var messCfg = mess ?? new MessData();
            bool messOn = BoardLayoutConfig.Current.messEnabled && messCfg.enabled;
            _mess = MessLayer.Create(messHolder, this, FxLayer, Def, Layout, messOn ? messCfg : new MessData { enabled = false, density = 0 });
        }

        static readonly ContainerArchetypeData Fallback = new ContainerArchetypeData { id = "fallback", style = "tray" };

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

        public void ShowPurposeProp(bool show, bool instant)
        {
            if (HasComposition) return; // the scene's own composition is the payoff
            float a0 = _propGroup.alpha, a1 = show ? 1f : 0f;
            if (instant || ReducedMotion) { _propGroup.alpha = a1; return; }
            StartCoroutine(UIKit.Tween(0.35f, t => _propGroup.alpha = Mathf.Lerp(a0, a1, t)));
        }

        /// <summary>
        /// The room in one consistent view: a countertop seen slightly from above (planks run one way), a rounded
        /// front lip with its shadow, then the painted cabinet the containers sit on. Materials come from area data.
        /// Bleeds past the board for wide screens. Never receives raycasts.
        /// </summary>
        void BuildRoom()
        {
            const float bleed = 600f;
            var room = UIKit.Rect(Root, "Room");
            float total = Layout.PlayH + Layout.ContainersH;
            room.TopLeft(-bleed, 0, W + bleed * 2, total + 400);
            float fullW = W + bleed * 2;
            float lipTop = Layout.PlayH - 10;
            float counterTop = Layout.BacksplashH > 0 ? Layout.BacksplashH + 10 : 0;

            var counter = KitchenArt.Tiled(room, "Countertop", KitchenArt.Surface(Area.surface, Area.surfaceColor), SpriteFactory.Hex(Area.surfaceTint));
            counter.rectTransform.TopLeft(0, counterTop, fullW, lipTop - counterTop);
            // A faint darker band where the backsplash meets the counter (depth cue from the top-left light).
            var backShade = UIKit.Panel(room, "BacksplashShade", new Color(0.3f, 0.18f, 0.1f, 0.16f), 0);
            backShade.rectTransform.TopLeft(0, counterTop, fullW, 5);
            if (Scene != null) SceneDressing.BuildArrival(room, this, Scene, bleed);
            var lip = UIKit.Panel(room, "CounterLip", SpriteFactory.Hex(Area.lip), 0);
            lip.rectTransform.TopLeft(0, lipTop, fullW, 10);
            var lipHi = UIKit.Panel(room, "LipHighlight", new Color(1, 1, 1, 0.28f), 0);
            lipHi.rectTransform.TopLeft(0, lipTop, fullW, 2);

            var cabinet = UIKit.Panel(room, "Cabinet", SpriteFactory.Hex(Area.cabinet), 0);
            cabinet.rectTransform.TopLeft(0, Layout.PlayH, fullW, Layout.ContainersH + 400);
            var shadow = UIKit.Panel(room, "LipShadow", new Color(0.2f, 0.12f, 0.08f, 0.28f), 0);
            shadow.rectTransform.TopLeft(0, Layout.PlayH, fullW, 7);
            // Door seams so the cabinet reads as furniture, not a flat panel.
            var seam = SpriteFactory.Hex(Area.cabinetShade);
            float step = W / 2f;
            for (float x = bleed - Mathf.Ceil(bleed / step) * step; x < fullW; x += step)
            {
                var s = UIKit.Panel(room, "DoorSeam", seam, 0);
                s.rectTransform.TopLeft(x - 1, Layout.PlayH + 7, 2, Layout.ContainersH + 400);
            }
            var edge = UIKit.Panel(room, "ShelfEdge", new Color(1, 1, 1, 0.2f), 0);
            edge.rectTransform.TopLeft(0, Layout.PlayH + 7, fullW, 2);
        }

        // ---------- Geometry ----------

        public Vector2 SourcePosition(ObjectDef o) => Layout.Pile[o.InstanceId].Center;
        public float SourceRotation(ObjectDef o) => Layout.Pile[o.InstanceId].Rotation;
        public Vector2 SlotPosition(string targetId, int slot) => Layout.SlotPosition(targetId, slot, Def.Target(targetId).SlotCount);

        /// <summary>Sets an item's resting look (size, tilt, touch area) for where it now belongs.</summary>
        public void RestOn(ObjectView v, string targetId, bool instant = false)
        {
            if (targetId == null)
            {
                var p = Layout.Pile[v.Def.InstanceId];
                v.SetRest(p.Visible, p.Rotation, p.Hit, false, instant);
            }
            else
            {
                float size = Layout.BinItemSize(targetId, v.Def.AssetId);
                v.SetRest(size, 0f, Mathf.Max(BoardLayoutConfig.Current.itemHitMin, size), true, instant);
            }
        }

        public Vector2 ToBoard(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, UiCamera, out var local);
            return new Vector2(local.x, -local.y);
        }

        public Rect TargetRectBoard(string targetId) => Layout.Bins[targetId].Rect;
        public Vector2 TargetCenter(string targetId) => TargetRectBoard(targetId).center;

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

        public bool OverSource(Vector2 p) => new Rect(0, 0, W, Layout.PlayH).Contains(p);

        // ---------- State sync ----------

        public void SnapAll(BoardState s, bool beforeProfile)
        {
            foreach (var o in Def.Objects)
            {
                var view = Objects[o.InstanceId];
                view.transform.SetParent(ObjectLayer, false);
                view.SetDragging(false);
                var target = s.TargetOf(o.InstanceId);
                view.Place(target == null ? SourcePosition(o) : SlotPosition(target, s.SlotOf(o.InstanceId)));
                RestOn(view, target, instant: true);
                view.SnapMotion();
            }
            // Keep the pile's draw order after re-parenting.
            foreach (var o in Def.Objects.OrderBy(o => Layout.Pile[o.InstanceId].Layer)) Objects[o.InstanceId].transform.SetAsLastSibling();
            foreach (var t in Def.Targets) Targets[t.TargetId].SetUsage(s.UnitsIn(t.TargetId));
            RefreshProgress(s, animate: false);
        }

        /// <summary>Mess under sorted items clears and containers get their done stamp.</summary>
        public void RefreshProgress(BoardState s, bool animate)
        {
            _mess.Refresh(s, animate && !ReducedMotion);
            foreach (var t in Def.Targets)
            {
                bool any = s.UnitsIn(t.TargetId) > 0;
                bool allHome = Def.Objects.Where(o => t.AcceptsCategory(o.CategoryId)).All(o => s.TargetOf(o.InstanceId) != null);
                Targets[t.TargetId].SetDone(any && allHome, animate && !ReducedMotion);
            }
        }

        /// <summary>GDD before-profile rotations (kept for the overview's clutter miniatures).</summary>
        public static float BeforeRotation(int index)
        {
            float[] r = { -10, 7, -4, 9 };
            return r[index % 4];
        }

        public void SetHover(string targetId)
        {
            foreach (var kv in Targets) kv.Value.SetHover(kv.Key == targetId, ReducedMotion);
        }

        public void ShowReturnArea(bool show) => ReturnArea.gameObject.SetActive(show);

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
                tv.SetBounce(new Vector3(s, 2 - s, 1)); // a little squash: wider and shorter
            }, () => tv.SetBounce(Vector3.one)));
        }

        /// <summary>A soft wobble for a refused drop (no red, no harsh motion).</summary>
        public void Shake(TargetView tv)
        {
            if (ReducedMotion) return;
            StartCoroutine(UIKit.Tween(0.32f, t => tv.SetTilt(Mathf.Sin(t * Mathf.PI * 4) * 2.2f * (1 - t)), () => tv.SetTilt(0)));
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
                        foreach (var tv in Targets.Values) { tv.SetTilt(Mathf.Sin(t * Mathf.PI * 2) * 2.5f * (1 - t)); tv.ShowFinished(kind, t); }
                    });
                    foreach (var tv in Targets.Values) tv.SetTilt(0);
                    break;
                case "tray_settle":
                    yield return UIKit.Tween(0.4f, t =>
                    {
                        foreach (var tv in Targets.Values) { tv.SetBounce(new Vector3(1, 1 - Mathf.Sin(t * Mathf.PI) * 0.04f, 1)); tv.ShowFinished(kind, t); }
                    });
                    foreach (var tv in Targets.Values) tv.SetBounce(Vector3.one);
                    break;
                default:
                    yield return UIKit.Tween(0.5f, t => { foreach (var tv in Targets.Values) tv.ShowFinished(kind, UIKit.EaseOut(t)); });
                    break;
            }
        }

        /// <summary>
        /// The scene's finished composition: the tray arrives, the sorted things move onto it (the real containers fade
        /// as their contents leave), then the supporting props. Board state is unchanged.
        /// </summary>
        public IEnumerator PlayComposition(BoardState s)
        {
            if (!HasComposition) yield break;
            // The real containers' contents leave with the copies; the empty containers fade out behind them.
            yield return _composition.Play(this, s, Scene.composition, ReducedMotion, () =>
            {
                _objectsGroup.alpha = 0;
                if (ReducedMotion) { _binsGroup.alpha = _frontsGroup.alpha = 0; return; }
                StartCoroutine(UIKit.Tween(0.15f, t => { _binsGroup.alpha = _frontsGroup.alpha = 1 - t; }));
            });
        }

        /// <summary>The "clean shine": a bright band sweeping across the counter before the result screen.</summary>
        public IEnumerator CleanShine()
        {
            if (ReducedMotion) yield break;
            var band = UIKit.Icon(ShineMask, "Shine", Fx.ShineBand(), 10, new Color(1f, 0.98f, 0.9f, 0f));
            band.preserveAspect = false;
            var rt = band.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(Layout.PlayH * 0.5f, Layout.PlayH * 1.8f);
            rt.localRotation = Quaternion.Euler(0, 0, -24f);
            float from = 400 - Layout.PlayH * 0.6f, to = 400 + W + Layout.PlayH * 0.6f;
            yield return UIKit.Tween(0.7f, t =>
            {
                if (band == null) return;
                rt.anchoredPosition = new Vector2(Mathf.Lerp(from, to, UIKit.EaseOut(t)), -Layout.PlayH * 0.45f);
                band.color = new Color(1f, 0.98f, 0.9f, 0.9f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.05f)));
            });
            if (band != null) Destroy(band.gameObject);
        }
    }

    /// <summary>
    /// A real kitchen container (archetype from data): ground shadow, body and interior behind the items, a front
    /// lip drawn over them, and a small sticker tag with icon, name and (for limited bins) spaces used.
    /// States: idle, hover-valid (warm glow, the lip dips as if the container opens), accept bounce, soft reject.
    /// </summary>
    public sealed class TargetView : MonoBehaviour
    {
        public TargetDef Def;
        public RectTransform Rt;
        public Rect LocalRect;
        public Vector3 BaseLocal;
        public ContainerArchetypeData Archetype;
        RectTransform _front;
        Vector2 _frontBase;
        Image _glow, _rimGlow, _hint, _finish, _done;
        Text _usage, _status;
        RectTransform _statusTag;
        bool _isDone, _hover;
        float _open, _openVel;
        Vector3 _bounce = Vector3.one;
        float _tilt;
        Coroutine _statusRoutine;

        public static TargetView Create(RectTransform layer, RectTransform frontLayer, BoardDef board, TargetDef t, BinPlacement place, ContainerArchetypeData arch)
        {
            var r = place.Rect;
            var rt = UIKit.Rect(layer, "Target_" + t.TargetId);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(r.center.x, -r.center.y);
            rt.sizeDelta = r.size;
            var v = rt.gameObject.AddComponent<TargetView>();
            v.Def = t; v.Rt = rt; v.LocalRect = r; v.Archetype = arch;
            v.Build(frontLayer, place);
            v.BaseLocal = rt.localPosition;
            return v;
        }

        static Color H(string hex) => SpriteFactory.Hex(hex);

        void Build(RectTransform frontLayer, BinPlacement place)
        {
            var a = Archetype;
            float w = LocalRect.width, h = LocalRect.height;
            var ground = KitchenArt.Sliced(Rt, "GroundShadow", ContainerArt.GroundShadow(), Color.white);
            ground.rectTransform.Stretch(-4, 10, -10, -9); // offset down-right: light from the top-left
            _glow = UIKit.Icon(Rt, "HoverGlow", Fx.Glow(), 10, new Color(1f, 0.92f, 0.62f, 0f));
            _glow.preserveAspect = false;
            _glow.rectTransform.Stretch(-26, -26, -26, -26);
            _rimGlow = KitchenArt.Sliced(Rt, "RimGlow", KitchenArt.Frame("rimglow", new Color(1f, 0.86f, 0.42f, 1f), new Color(1, 1, 1, 0), 5, 20), new Color(1, 1, 1, 0));
            _rimGlow.rectTransform.Stretch(-4, -4, -4, -4);
            _hint = KitchenArt.Sliced(Rt, "Hint", KitchenArt.Frame("hint", new Color(1f, 0.82f, 0.3f, 1f), new Color(1, 1, 1, 0), 4, 18), new Color(1, 1, 1, 0));
            _hint.rectTransform.Stretch(-5, -5, -5, -5);

            var back = KitchenArt.Sliced(Rt, "Body", ContainerArt.Back(a), Color.white);
            back.type = back.sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            back.rectTransform.Stretch();
            var (pattern, tint) = ContainerArt.Pattern(a);
            if (pattern != null) KitchenArt.Tiled(Rt, "Pattern", pattern, tint).rectTransform.Stretch(9, 10, 9, 9);
            if (a.style == "drawer" && place.Cols > 1)
            {
                // Cutlery tray dividers between the columns.
                for (int c = 1; c < place.Cols; c++)
                {
                    float x = place.Interior.x - LocalRect.x + place.Interior.width * c / place.Cols;
                    var d = UIKit.Panel(Rt, "Divider", H(a.accent), 2);
                    d.rectTransform.TopLeft(x - 2, place.Interior.y - LocalRect.y - 2, 4, place.Interior.height + 4);
                }
            }
            if (a.style == "fridge")
            {
                var light = UIKit.Icon(Rt, "InteriorLight", Fx.Glow(), 10, new Color(0.85f, 0.95f, 1f, 0.55f));
                light.preserveAspect = false;
                light.rectTransform.TopLeft(w * 0.1f, 4, w * 0.8f, h * 0.45f);
            }

            // Sticker tag: icon + short name; limited bins add a small "used/capacity" pill.
            bool shared = Def.Accepts.Count > 1;
            // Shared containers carry their own name (the Kitchen's "Shared tray", the Living Room's "Storage bench").
            string label = shared ? (Def.Accepts.Count == 2 && Def.TargetId.Length == 2 ? "" : (!string.IsNullOrEmpty(a.label) && a.id != "fallback" ? a.label : Strings.Get("target.shared"))) : Strings.Get("category." + Def.Accepts[0]);
            const float iconSize = 16;
            float iconsW = Def.Accepts.Count * (iconSize + 1);
            float textW = label.Length > 0 ? Mathf.Min(w - 22 - iconsW, 7.4f * label.Length + 6) : 0;
            float tagW = 10 + iconsW + (textW > 0 ? textW + 4 : 0);
            var tag = UIKit.Panel(Rt, "Tag", new Color(1f, 0.985f, 0.95f, 1f), 7);
            tag.rectTransform.TopLeft(6, 4, tagW, 20);
            var tagShadow = tag.gameObject.AddComponent<Shadow>();
            tagShadow.effectColor = new Color(0.3f, 0.18f, 0.1f, 0.3f);
            tagShadow.effectDistance = new Vector2(1, -1);
            float x0 = 5;
            foreach (var cat in Def.Accepts)
            {
                var ic = UIKit.Icon(tag.transform, "Icon_" + cat, SpriteFactory.CategoryIcon(cat), iconSize);
                ic.rectTransform.TopLeft(x0, 2, iconSize, iconSize);
                x0 += iconSize + 1;
            }
            if (textW > 0)
            {
                var title = UIKit.Label(tag.transform, "Label", label, 12, UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                title.resizeTextMinSize = 8;
                title.rectTransform.TopLeft(x0 + 2, 1, textW + 2, 18);
            }

            _finish = UIKit.Panel(Rt, "Finish", new Color(1, 1, 1, 0), 16);
            _finish.rectTransform.Stretch();

            // Front lip (drawn above the items, in the board's front layer).
            float lipH = Mathf.Min(Mathf.Max(12, h * a.lipShare), place.LipHeight);
            _front = UIKit.Rect(frontLayer, "Front_" + Def.TargetId);
            _front.anchorMin = _front.anchorMax = new Vector2(0, 1);
            _front.pivot = new Vector2(0.5f, 0f);
            _frontBase = new Vector2(LocalRect.center.x, -LocalRect.yMax);
            _front.anchoredPosition = _frontBase;
            _front.sizeDelta = new Vector2(w, lipH);
            var front = _front.gameObject.AddComponent<Image>();
            front.sprite = ContainerArt.Front(a);
            front.type = front.sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            front.raycastTarget = false;

            // Limited containers show "used/capacity" on a small pill on the front lip (keeps the tag readable).
            if (!Def.IsUnlimited)
            {
                var chip = UIKit.Panel(_front, "UsagePill", UIKit.Ink, 9);
                chip.rectTransform.anchorMin = chip.rectTransform.anchorMax = new Vector2(0, 0.5f);
                chip.rectTransform.pivot = new Vector2(0, 0.5f);
                chip.rectTransform.anchoredPosition = new Vector2(8, 0);
                chip.rectTransform.sizeDelta = new Vector2(36, Mathf.Min(18, lipH - 2));
                _usage = UIKit.Label(chip.transform, "Usage", "", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                _usage.rectTransform.Stretch();
            }

            // "Done" stamp: a green check that pops onto the container when everything of its kind is home.
            _done = UIKit.Icon(_front, "Done", SpriteFactory.Circle(), 22, UIKit.Primary);
            _done.rectTransform.anchorMin = _done.rectTransform.anchorMax = new Vector2(1, 1);
            _done.rectTransform.anchoredPosition = new Vector2(-14, 4);
            UIKit.Icon(_done.transform, "Check", ContainerArt.Check(), 18, Color.white).rectTransform.anchoredPosition = Vector2.zero;
            _done.gameObject.AddComponent<Outline>().effectColor = new Color(1, 1, 1, 0.9f);
            _done.gameObject.SetActive(false);

            // Neutral status note for a refused drop ("Fruit only", "Full"): never red.
            _statusTag = UIKit.Panel(Rt, "Status", new Color(1f, 0.985f, 0.95f, 0.97f), 9).rectTransform;
            _statusTag.anchorMin = _statusTag.anchorMax = new Vector2(0.5f, 0.5f);
            _statusTag.pivot = new Vector2(0.5f, 0.5f);
            _statusTag.anchoredPosition = new Vector2(0, -4);
            _statusTag.sizeDelta = new Vector2(Mathf.Min(w - 8, 120), 24);
            _status = UIKit.Label(_statusTag, "Text", "", 12, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _status.rectTransform.Stretch(4, 0, 4, 0);
            _statusTag.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float target = _hover ? 1f : 0f;
            _openVel += (180f * (target - _open) - 16f * _openVel) * dt;
            _open += _openVel * dt;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (_front != null) _front.anchoredPosition = _frontBase + new Vector2(0, -5f * _open);
            if (_glow != null) _glow.color = new Color(1f, 0.92f, 0.62f, 0.9f * Mathf.Clamp01(_open));
            if (_rimGlow != null) _rimGlow.color = new Color(1, 1, 1, 0.95f * Mathf.Clamp01(_open));
            float s = 1f + 0.025f * Mathf.Clamp01(_open);
            Rt.localScale = new Vector3(s * _bounce.x, s * _bounce.y, 1);
            Rt.localRotation = Quaternion.Euler(0, 0, _tilt);
            if (_front != null) { _front.localScale = Rt.localScale; _front.localRotation = Rt.localRotation; }
        }

        public void SetBounce(Vector3 scale) { _bounce = scale; ApplyVisual(); }
        public void SetTilt(float deg) { _tilt = deg; ApplyVisual(); }

        public void SetDone(bool on, bool animate)
        {
            if (on == _isDone) return;
            _isDone = on;
            _done.gameObject.SetActive(on);
            if (!on) return;
            var rt = _done.rectTransform;
            if (!animate || !isActiveAndEnabled) { rt.localScale = Vector3.one; return; }
            StartCoroutine(UIKit.Tween(0.35f, t => rt.localScale = Vector3.one * Mathf.Max(0.01f, UIKit.EaseOutBack(t))));
        }

        public void SetUsage(int units)
        {
            if (_usage != null) _usage.text = $"{units}/{Def.Capacity}";
        }

        public void SetHover(bool on, bool reduced)
        {
            _hover = on;
            if (reduced) { _open = on ? 1 : 0; _openVel = 0; ApplyVisual(); }
        }

        public void SetHint(bool on) => _hint.color = on ? new Color(1, 1, 1, 0.95f) : new Color(1, 1, 1, 0);

        public void ShowStatus(string text, MonoBehaviour host)
        {
            if (_statusRoutine != null) host.StopCoroutine(_statusRoutine);
            _statusRoutine = host.StartCoroutine(StatusRoutine(text));
        }

        IEnumerator StatusRoutine(string text)
        {
            _status.text = text;
            _statusTag.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(1.0f);
            if (_statusTag != null) _statusTag.gameObject.SetActive(false);
        }

        /// <summary>Finished presentation: a soft warm glow; contents stay visible.</summary>
        public void ShowFinished(string kind, float t) => _finish.color = new Color(1f, 0.97f, 0.85f, 0.16f * t);
    }

    /// <summary>
    /// One draggable item. It rests at a size from its metadata (relativeSize) with a contact shadow and a soft
    /// drop shadow, both cast from the top-left light. Lift, tilt while dragging, squash on landing and hops run
    /// on small springs; positions still come only from committed state.
    /// </summary>
    public sealed class ObjectView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        public ObjectDef Def;
        public RectTransform Rt;
        BoardView _board;
        RectTransform _art;
        Image _sprite, _contact, _drop, _hint;
        GameObject _badge;
        public Vector2 Position { get; private set; }
        public float VisibleSize => _sizeTarget;
        public float HitSize => Rt.sizeDelta.x;

        float _size = 56, _sizeVel, _sizeTarget = 56;
        float _lift = 1, _liftVel;
        float _rot, _rotVel, _restRot;
        Vector2 _squash = Vector2.one, _squashVel;
        bool _dragging, _inBin;
        Vector2 _lastPos;
        float _tilt;
        float _hopT = -1, _hopDelay, _hopOffset;

        public static ObjectView Create(RectTransform layer, BoardView board, ObjectDef def)
        {
            var rt = UIKit.Rect(layer, "Obj_" + def.InstanceId);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(56, 56);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;
            var v = rt.gameObject.AddComponent<ObjectView>();
            v.Def = def; v.Rt = rt; v._board = board;

            v._hint = UIKit.Icon(rt, "Hint", SpriteFactory.Ring(), 62, new Color(1f, 0.8f, 0.2f, 0f));
            v._drop = UIKit.Icon(rt, "DropShadow", SpriteFactory.Shadow(), 40, new Color(1, 1, 1, 0.5f));
            v._drop.preserveAspect = false;
            v._contact = UIKit.Icon(rt, "ContactShadow", SpriteFactory.Shadow(), 40, new Color(0.3f, 0.2f, 0.14f, 0.85f));
            v._contact.preserveAspect = false;
            v._art = UIKit.Rect(rt, "Art");
            v._sprite = v._art.gameObject.AddComponent<Image>();
            v._sprite.sprite = SpriteFactory.Object(def.AssetId);
            v._sprite.preserveAspect = true;
            v._sprite.raycastTarget = false;

            // Unit badge: always visible for 2-unit objects (rules stay visible without a lesson).
            var badge = UIKit.Icon(rt, "Badge", SpriteFactory.Circle(), 18, UIKit.Ink);
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

        /// <summary>Resting look: drawn size, tilt and touch square for the counter or a container.</summary>
        public void SetRest(float visible, float rotation, float hit, bool inBin, bool instant)
        {
            _sizeTarget = visible; _restRot = rotation; _inBin = inBin;
            Rt.sizeDelta = new Vector2(hit, hit);
            if (instant || _board.ReducedMotion) { _size = visible; _sizeVel = 0; _rot = rotation; _rotVel = 0; }
            ApplyVisual();
        }

        public void SnapMotion()
        {
            _size = _sizeTarget; _sizeVel = 0; _lift = _dragging ? BoardLayoutConfig.Current.dragLiftScale : 1; _liftVel = 0;
            _rot = _restRot; _rotVel = 0; _squash = Vector2.one; _squashVel = Vector2.zero; _hopT = -1; _hopOffset = 0; _tilt = 0;
            _lastPos = Position;
            ApplyVisual();
        }

        public void SetDragging(bool on)
        {
            if (_dragging == on) return;
            _dragging = on;
            if (on && !_board.ReducedMotion) { _liftVel += 3f; _squash = new Vector2(0.92f, 1.08f); }
            _lastPos = Position;
            if (_board.ReducedMotion) SnapMotion();
        }

        public void Land(float strength = 1f)
        {
            if (_board.ReducedMotion) return;
            _squash = new Vector2(1f + 0.22f * strength, 1f - 0.22f * strength);
            _squashVel = Vector2.zero;
        }

        public void Wobble() { if (!_board.ReducedMotion) _rotVel += 700f; }

        public void Hop(float delay) { if (!_board.ReducedMotion) { _hopDelay = delay; _hopT = 0; } }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (dt <= 0) return;
            if (_board.ReducedMotion) { SnapMotion(); return; }

            if (_dragging)
            {
                float vx = (Position.x - _lastPos.x) / dt;
                _tilt = Mathf.Lerp(_tilt, Mathf.Clamp(-vx * 0.03f, -16f, 16f), 1 - Mathf.Exp(-12f * dt));
            }
            else _tilt = Mathf.Lerp(_tilt, 0, 1 - Mathf.Exp(-12f * dt));
            _lastPos = Position;

            float liftTarget = _dragging ? BoardLayoutConfig.Current.dragLiftScale : 1f;
            _liftVel += (240f * (liftTarget - _lift) - 18f * _liftVel) * dt;
            _lift += _liftVel * dt;
            _sizeVel += (200f * (_sizeTarget - _size) - 20f * _sizeVel) * dt;
            _size += _sizeVel * dt;
            float rotTarget = _dragging ? _tilt : _restRot;
            _rotVel += (160f * (rotTarget - _rot) - 11f * _rotVel) * dt;
            _rot += _rotVel * dt;
            _squashVel += (260f * (Vector2.one - _squash) - 13f * _squashVel) * dt;
            _squash += _squashVel * dt;

            _hopOffset = 0;
            if (_hopT >= 0)
            {
                _hopT += dt;
                float t = _hopT - _hopDelay;
                if (t >= 0.4f) { _hopT = -1; Land(0.6f); }
                else if (t > 0) _hopOffset = Mathf.Sin(t / 0.4f * Mathf.PI) * _size * 0.25f;
            }
            ApplyVisual();
        }

        void ApplyVisual()
        {
            float size = Mathf.Max(8f, _size);
            Rt.localScale = new Vector3(_lift, _lift, 1);
            _art.sizeDelta = new Vector2(size, size);
            _art.localScale = new Vector3(_squash.x, _squash.y, 1);
            _art.localRotation = Quaternion.Euler(0, 0, _rot);
            float lift01 = Mathf.Clamp01((_lift - 1f) / 0.08f) + Mathf.Clamp01(_hopOffset / (size * 0.25f + 0.01f));
            lift01 = Mathf.Clamp01(lift01);
            _art.anchoredPosition = new Vector2(0, _hopOffset + lift01 * size * 0.08f);
            // Contact shadow: tight and dark right under the item; fades as it lifts.
            _contact.rectTransform.sizeDelta = new Vector2(size * 0.62f, size * 0.15f);
            _contact.rectTransform.anchoredPosition = new Vector2(size * 0.04f, -size * 0.38f);
            _contact.color = new Color(0.3f, 0.2f, 0.14f, (_inBin ? 0.55f : 0.85f) * (1f - 0.8f * lift01));
            // Drop shadow: soft, offset down-right (light from the top-left); moves out and softens when lifted.
            float off = 0.08f + 0.14f * lift01;
            _drop.rectTransform.sizeDelta = new Vector2(size * (0.86f + 0.12f * lift01), size * (0.34f + 0.06f * lift01));
            _drop.rectTransform.anchoredPosition = new Vector2(size * off, -size * (0.3f + off));
            _drop.color = new Color(1, 1, 1, _inBin ? 0.25f : 0.5f - 0.18f * lift01);
            _badge.transform.localPosition = new Vector3(size * 0.36f, size * 0.36f, 0);
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
