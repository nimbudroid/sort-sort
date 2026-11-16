using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SortEverything.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Little Project Loop presentation for a scene profile (Content/scenes.json):
    /// DISCOVER — a wall band with window, shelf and props behind the counter, in morning light;
    /// TRANSFORM — on completion the sorted items move onto a purpose-specific composition (e.g. a breakfast tray),
    /// together with supporting props. Pure presentation: copies of the sorted items, no raycasts, no rule changes.
    /// </summary>
    public static class SceneDressing
    {
        /// <summary>Wall band + props + morning light. Called by BoardView while it builds the room.</summary>
        public static CanvasGroup BuildArrival(RectTransform room, BoardView v, SceneData scene, float bleed)
        {
            var L = v.Layout;
            float fullW = v.W + bleed * 2;
            if (L.BacksplashH > 0)
            {
                var wall = KitchenArt.Tiled(room, "SceneWall", KitchenArt.Surface(v.Area.wall), Color.white);
                wall.rectTransform.TopLeft(0, 0, fullW, L.BacksplashH);
                // Back edge of the counter where it meets the wall: a slim lit strip with a contact shadow above it.
                var edgeShadow = UIKit.Panel(room, "WallContact", new Color(0.3f, 0.18f, 0.1f, 0.22f), 0);
                edgeShadow.rectTransform.TopLeft(0, L.BacksplashH - 4, fullW, 4);
                var backEdge = UIKit.Panel(room, "CounterBackEdge", SpriteFactory.Hex(v.Area.lip), 0);
                backEdge.rectTransform.TopLeft(0, L.BacksplashH, fullW, 10);
                var backEdgeHi = UIKit.Panel(room, "CounterBackEdgeHi", new Color(1, 1, 1, 0.25f), 0);
                backEdgeHi.rectTransform.TopLeft(0, L.BacksplashH, fullW, 2);
                foreach (var p in scene.wallProps)
                {
                    float size = p.size * L.BacksplashH;
                    var img = UIKit.Icon(room, "WallProp_" + p.prop, SceneArt.Prop(p.prop), size);
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0, 1);
                    img.rectTransform.anchoredPosition = new Vector2(bleed + p.x * v.W, -(p.y * L.BacksplashH));
                    img.rectTransform.localRotation = Quaternion.Euler(0, 0, p.rotation);
                }
            }
            if (scene.light != "morning") return null;

            // Morning light: soft warm beams from the window (upper left) falling across the counter.
            var lightRt = UIKit.Rect(room, "MorningLight");
            lightRt.TopLeft(0, 0, fullW, L.PlayH - 10);
            lightRt.gameObject.AddComponent<RectMask2D>();
            var group = lightRt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.75f;
            var window = scene.wallProps.FirstOrDefault(p => p.prop.StartsWith("window"));
            float wx = bleed + (window != null ? window.x : 0.3f) * v.W, wy = (window != null ? window.y : 0.4f) * Mathf.Max(L.BacksplashH, 40);
            for (int i = 0; i < 3; i++)
            {
                var beam = UIKit.Icon(lightRt, "Beam" + i, Fx.ShineBand(), 10, new Color(1f, 0.9f, 0.62f, 0.2f - i * 0.04f));
                beam.preserveAspect = false;
                beam.rectTransform.anchorMin = beam.rectTransform.anchorMax = new Vector2(0, 1);
                beam.rectTransform.pivot = new Vector2(0.5f, 1f);
                beam.rectTransform.sizeDelta = new Vector2(v.W * (0.22f + i * 0.06f), L.PlayH * 1.5f);
                beam.rectTransform.anchoredPosition = new Vector2(wx - 10 + i * v.W * 0.12f, -wy);
                beam.rectTransform.localRotation = Quaternion.Euler(0, 0, 28f - i * 6f);
            }
            var sun = UIKit.Icon(lightRt, "WarmPool", Fx.Glow(), 10, new Color(1f, 0.88f, 0.6f, 0.22f));
            sun.preserveAspect = false;
            sun.rectTransform.anchorMin = sun.rectTransform.anchorMax = new Vector2(0, 1);
            sun.rectTransform.sizeDelta = new Vector2(v.W * 1.1f, L.PlayH * 0.8f);
            sun.rectTransform.anchoredPosition = new Vector2(wx + v.W * 0.25f, -(L.BacksplashH + L.PlayH * 0.35f));
            return group;
        }
    }

    /// <summary>
    /// The finished composition (TRANSFORM / REMEMBER): a tray on the counter that receives the sorted items as
    /// decorative copies — whole filled containers or items lined up on authored spots — plus supporting props.
    /// The real board state is unchanged; the real containers fade as their contents move up.
    /// </summary>
    public sealed class SceneComposition : MonoBehaviour
    {
        RectTransform _rt;
        Rect _tray;
        readonly List<(RectTransform rt, float depth)> _layers = new List<(RectTransform, float)>();
        public bool Shown { get; private set; }

        public static SceneComposition Create(RectTransform parent)
        {
            var rt = UIKit.Rect(parent, "Composition").Stretch();
            var c = rt.gameObject.AddComponent<SceneComposition>();
            c._rt = rt;
            return c;
        }

        static RectTransform Node(RectTransform parent, string name, Vector2 center, Vector2 size)
        {
            var rt = UIKit.Rect(parent, name);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(center.x, -center.y);
            rt.sizeDelta = size;
            return rt;
        }

        static void SetCenter(RectTransform rt, Vector2 c) => rt.anchoredPosition = new Vector2(c.x, -c.y);

        public IEnumerator Play(BoardView v, BoardState s, CompositionData comp, bool reduced, System.Action hideReal)
        {
            Shown = true;
            var L = v.Layout;
            var area = L.PlayRect;
            _tray = new Rect(area.x + (comp.x - comp.w / 2) * area.width, area.y + (comp.y - comp.h / 2) * area.height, comp.w * area.width, comp.h * area.height);
            Vector2 TrayPoint(float x, float y) => new Vector2(_tray.x + x * _tray.width, _tray.y + y * _tray.height);

            // Tray: ground shadow, the archetype body, a cloth, and side handles.
            var trayRt = Node(_rt, "Tray", _tray.center, _tray.size);
            var arch = Runtime.ContentDb.Containers.archetypes.FirstOrDefault(a => a.id == comp.tray) ?? Runtime.ContentDb.Containers.archetypes.First();
            var ground = KitchenArt.Sliced(trayRt, "Ground", ContainerArt.GroundShadow(), Color.white);
            ground.rectTransform.Stretch(-6, 8, -12, -12);
            foreach (var side in new[] { 0f, 1f })
            {
                var h = UIKit.Panel(trayRt, "Handle", SpriteFactory.Hex(arch.accent), 6);
                h.rectTransform.anchorMin = h.rectTransform.anchorMax = new Vector2(side, 0.5f);
                h.rectTransform.anchoredPosition = new Vector2(side == 0 ? -4 : 4, 0);
                h.rectTransform.sizeDelta = new Vector2(16, _tray.height * 0.32f);
                h.gameObject.AddComponent<Outline>().effectColor = new Color(0.29f, 0.2f, 0.14f, 0.9f);
            }
            var body = KitchenArt.Sliced(trayRt, "Body", ContainerArt.Back(arch), Color.white);
            body.rectTransform.Stretch();
            if (comp.cloth == "gingham")
            {
                var cloth = UIKit.Panel(trayRt, "Cloth", new Color(1f, 0.97f, 0.93f, 1f), 10);
                cloth.rectTransform.Stretch(12, 12, 12, 12);
                KitchenArt.Tiled(cloth.transform, "Check", KitchenArt.Gingham(), new Color(1, 1, 1, 0.9f)).rectTransform.Stretch(2, 2, 2, 2);
            }
            var trayGroup = trayRt.gameObject.AddComponent<CanvasGroup>();

            // Build every piece first, then animate. Depth (y) decides overlap: nearer things are drawn on top.
            var flights = new List<(RectTransform rt, Vector2 from, Vector2 to, float s0, float s1)>();
            var pops = new List<RectTransform>();
            foreach (var p in comp.props)
            {
                float size = p.size * _tray.height;
                var img = UIKit.Icon(_rt, "Prop_" + p.prop, SceneArt.Prop(p.prop), size);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                SetCenter(rt, TrayPoint(p.x, p.y));
                rt.localRotation = Quaternion.Euler(0, 0, p.rotation);
                rt.localScale = Vector3.zero;
                pops.Add(rt);
                _layers.Add((rt, TrayPoint(p.x, p.y).y + size * 0.4f));
            }
            foreach (var g in comp.groups)
            {
                if (!v.Targets.TryGetValue(g.target, out var tv)) continue;
                var ids = s.ObjectsIn(g.target).ToList();
                if (g.mode == "items")
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        var ov = v.Objects[ids[i]];
                        var spot = i < g.spots.Length ? g.spots[i] : new SpotData { x = 0.5f + i * 0.08f, y = 0.4f };
                        float size = L.Pile[ids[i]].Visible * g.itemScale;
                        var rt = Node(_rt, "Item_" + ids[i], ov.Position, new Vector2(size, size));
                        var sh = UIKit.Icon(rt, "Shadow", SpriteFactory.Shadow(), size, new Color(1, 1, 1, 0.7f));
                        sh.preserveAspect = false;
                        sh.rectTransform.sizeDelta = new Vector2(size * 0.7f, size * 0.18f);
                        sh.rectTransform.anchoredPosition = new Vector2(size * 0.05f, -size * 0.4f);
                        UIKit.Icon(rt, "Art", SpriteFactory.Object(ov.Def.AssetId), size);
                        float s0 = ov.VisibleSize / Mathf.Max(1f, size);
                        var to = TrayPoint(spot.x, spot.y);
                        flights.Add((rt, ov.Position, to, s0, 1f));
                        _layers.Add((rt, to.y + size * 0.4f));
                    }
                }
                else
                {
                    // The filled container itself, trimmed to the rows actually used (no sticker tag).
                    var bin = L.Bins[g.target];
                    int maxSlot = ids.Count == 0 ? 0 : ids.Max(id => s.SlotOf(id));
                    int rows = Mathf.Max(1, maxSlot / Mathf.Max(1, bin.Cols) + 1);
                    float h = rows * bin.CellSide + bin.LipHeight * 0.5f + 18f;
                    var r = new Rect(bin.Rect.x, bin.Rect.yMax - h, bin.Rect.width, h);
                    var rt = Node(_rt, "Container_" + g.target, r.center, r.size);
                    var gs = KitchenArt.Sliced(rt, "Ground", ContainerArt.GroundShadow(), Color.white);
                    gs.rectTransform.Stretch(-4, 8, -8, -8);
                    KitchenArt.Sliced(rt, "Back", ContainerArt.Back(tv.Archetype), Color.white).rectTransform.Stretch();
                    var (pattern, tint) = ContainerArt.Pattern(tv.Archetype);
                    if (pattern != null) KitchenArt.Tiled(rt, "Pattern", pattern, tint).rectTransform.Stretch(9, 10, 9, 9);
                    foreach (var id in ids)
                    {
                        var ov = v.Objects[id];
                        var icon = UIKit.Icon(rt, "Item_" + id, SpriteFactory.Object(ov.Def.AssetId), ov.VisibleSize);
                        icon.rectTransform.anchoredPosition = new Vector2(ov.Position.x - r.center.x, -(ov.Position.y - r.center.y));
                    }
                    var front = KitchenArt.Sliced(rt, "Front", ContainerArt.Front(tv.Archetype), Color.white);
                    front.rectTransform.anchorMin = new Vector2(0, 0); front.rectTransform.anchorMax = new Vector2(1, 0);
                    front.rectTransform.pivot = new Vector2(0.5f, 0);
                    front.rectTransform.sizeDelta = new Vector2(0, bin.LipHeight);
                    front.rectTransform.anchoredPosition = Vector2.zero;
                    var to = TrayPoint(g.x, g.y);
                    flights.Add((rt, r.center, to, 1f, g.scale));
                    _layers.Add((rt, to.y + r.height * g.scale * 0.5f));
                }
            }
            foreach (var l in _layers.OrderBy(l => l.depth)) l.rt.SetAsLastSibling();
            // Copies stay invisible until the moment they take off, then swap with the real ones in one frame.
            var flightGroups = flights.Select(f => f.rt.gameObject.AddComponent<CanvasGroup>()).ToList();
            foreach (var g in flightGroups) g.alpha = 0;

            // Reduced motion: the finished state appears at once.
            if (reduced)
            {
                hideReal?.Invoke();
                foreach (var g in flightGroups) g.alpha = 1;
                foreach (var f in flights) { SetCenter(f.rt, f.to); f.rt.localScale = Vector3.one * f.s1; }
                foreach (var p in pops) p.localScale = Vector3.one;
                yield break;
            }

            // 1. The tray slides onto the cleared counter.
            trayGroup.alpha = 0;
            var trayTo = _tray.center;
            yield return UIKit.Tween(0.35f, t =>
            {
                trayGroup.alpha = t;
                SetCenter(trayRt, trayTo + new Vector2(0, 24f * (1 - UIKit.EaseOut(t))));
                trayRt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, UIKit.EaseOutBack(t));
            });
            // 2. The sorted things travel up from their containers in a gentle arc and settle.
            hideReal?.Invoke();
            foreach (var g in flightGroups) g.alpha = 1;
            for (int i = 0; i < flights.Count; i++) StartCoroutine(Fly(flights[i], i * 0.07f, v));
            yield return new WaitForSecondsRealtime(0.55f + flights.Count * 0.07f);
            // 3. Supporting props pop in, one by one.
            for (int i = 0; i < pops.Count; i++)
            {
                var p = pops[i];
                StartCoroutine(UIKit.Tween(0.3f, t => { if (p != null) p.localScale = Vector3.one * Mathf.Max(0.01f, UIKit.EaseOutBack(t)); }));
                yield return new WaitForSecondsRealtime(0.09f);
            }
        }

        IEnumerator Fly((RectTransform rt, Vector2 from, Vector2 to, float s0, float s1) f, float delay, BoardView v)
        {
            yield return new WaitForSecondsRealtime(delay);
            float lift = 60f + Mathf.Abs(f.to.x - f.from.x) * 0.15f;
            yield return UIKit.Tween(0.55f, t =>
            {
                if (f.rt == null) return;
                float e = UIKit.EaseOut(t);
                var p = Vector2.Lerp(f.from, f.to, e) - new Vector2(0, Mathf.Sin(t * Mathf.PI) * lift);
                SetCenter(f.rt, p);
                float squash = t > 0.85f ? 1f + Mathf.Sin((t - 0.85f) / 0.15f * Mathf.PI) * 0.08f : 1f;
                f.rt.localScale = new Vector3(Mathf.Lerp(f.s0, f.s1, e) * squash, Mathf.Lerp(f.s0, f.s1, e) / squash, 1);
            });
            if (f.rt != null) Fx.Burst(v, v.FxLayer, f.to, new Color(1f, 0.95f, 0.75f), 6, 0.6f);
        }
    }
}
