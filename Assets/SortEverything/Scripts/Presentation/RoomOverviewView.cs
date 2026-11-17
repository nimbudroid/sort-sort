using System.Linq;
using SortEverything.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Overview of a data room (any room after the Kitchen), drawn only from the saved state: each region starts
    /// untidy, gains a small neat trace for every completed scene, and shows its composed, restored look once all its
    /// scenes are completed. Regions whose chapter is not authored yet say so honestly. Decorative only: no raycasts.
    /// </summary>
    public static class RoomOverviewView
    {
        public static RectTransform Build(Transform parent, SaveData save, Catalogue cat, AreaData area, float width, float height, bool dim = false)
        {
            var root = UIKit.Panel(parent, "RoomOverview_" + area.id, SpriteFactory.Hex(area.lip), 18);
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(width, height);
            var regions = area.regions.Length > 0 ? area.regions : new[] { new OverviewRegionData { name = area.name, from = 1, to = 99 } };
            int n = regions.Length;
            float pad = 6, cw = (width - pad * (n + 1)) / n, ch = height - pad * 2;
            var levels = cat.InstalledInArea(area.id);

            for (int r = 0; r < n; r++)
            {
                var reg = regions[r];
                var regionLevels = levels.Where(l => l.OrdinalInArea >= reg.from && l.OrdinalInArea <= reg.to).ToList();
                bool planned = regionLevels.Count == 0;
                bool restored = !planned && regionLevels.All(l => save.HasSeal(l.LevelId));
                float x = pad + r * (cw + pad);

                var card = UIKit.Panel(rt, "Region_" + r, SpriteFactory.Hex("#E9DCC8"), 12);
                card.rectTransform.TopLeft(x, pad, cw, ch);
                card.gameObject.AddComponent<RectMask2D>();
                float floorTop = ch * 0.62f;
                KitchenArt.Tiled(card.transform, "Wall", KitchenArt.Surface(area.wall), Color.white).rectTransform.TopLeft(0, 0, cw, floorTop);
                KitchenArt.Tiled(card.transform, "Floor", KitchenArt.Surface(area.surface, area.surfaceColor), Color.white).rectTransform.TopLeft(0, floorTop, cw, ch - floorTop);
                UIKit.Panel(card.transform, "Skirting", new Color(1, 1, 1, 0.55f), 0).rectTransform.TopLeft(0, floorTop - 3, cw, 3);

                // Furniture stands on the floor line.
                float fx = 4;
                foreach (var f in reg.furniture)
                {
                    float size = Mathf.Min(cw * 0.7f, floorTop + 10);
                    var img = UIKit.Icon(card.transform, "Furniture_" + f, SceneArt.Prop(f), size);
                    img.rectTransform.TopLeft(fx + (reg.furniture.Length == 1 ? (cw - size) / 2 - 4 : 0), floorTop - size * 0.78f, size, size);
                    fx += size * 0.6f;
                }

                if (restored)
                {
                    // The composed result: the region's objects arranged neatly along the floor.
                    var items = reg.restored;
                    float step = (cw - 12) / Mathf.Max(1, items.Length), icon = Mathf.Min(24, step);
                    for (int i = 0; i < items.Length; i++)
                    {
                        var ic = UIKit.Icon(card.transform, items[i], SpriteFactory.Object(items[i]), icon);
                        ic.rectTransform.TopLeft(6 + step * i + (step - icon) / 2, floorTop + (ch - floorTop - icon) / 2, icon, icon);
                    }
                    UIKit.Panel(card.transform, "Glow", new Color(1f, 0.95f, 0.78f, 0.18f), 0).rectTransform.Stretch();
                }
                else if (!planned)
                {
                    // Untidy until restored: clutter on the floor, softened by dust; neat traces for completed scenes.
                    UIKit.Panel(card.transform, "Dust", new Color(0.55f, 0.48f, 0.42f, 0.28f), 0).rectTransform.Stretch();
                    for (int i = 0; i < reg.clutter.Length; i++)
                    {
                        var ic = UIKit.Icon(card.transform, "Clutter" + i, SpriteFactory.Object(reg.clutter[i]), 18, new Color(1, 1, 1, 0.8f));
                        ic.rectTransform.Center(10 + i * Mathf.Min(17, (cw - 20) / Mathf.Max(1, reg.clutter.Length)), floorTop + 12 + (i % 2) * 8, 18, 18);
                        ic.rectTransform.localRotation = Quaternion.Euler(0, 0, BoardView.BeforeRotation(i) * 3);
                    }
                    int k = 0;
                    foreach (var l in regionLevels.Where(l => save.traceIds.Contains(Progression.TraceId(l.LevelId))))
                    {
                        var first = l.RequiredBoard.Objects.First().AssetId;
                        var tray = UIKit.Panel(card.transform, "Trace_" + l.LevelId, SpriteFactory.Hex("#F4E3C4"), 5);
                        float tx = 6 + (k % 3) * (cw - 12) / 3f, ty = floorTop - 28 - (k / 3) * 20;
                        tray.rectTransform.TopLeft(tx, ty, Mathf.Min(26, (cw - 12) / 3f - 2), 16);
                        var mini = UIKit.Icon(tray.transform, "Mini", SpriteFactory.Object(first), 14);
                        mini.rectTransform.anchoredPosition = new Vector2(0, 2);
                        k++;
                    }
                }
                else
                {
                    UIKit.Panel(card.transform, "Planned", new Color(0.93f, 0.9f, 0.85f, 0.72f), 0).rectTransform.Stretch();
                    var soon = UIKit.Label(card.transform, "Soon", "Coming later", 10, UIKit.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
                    soon.rectTransform.TopLeft(0, ch * 0.45f, cw, 16);
                }

                var tag = UIKit.Panel(card.transform, "TitleTag", new Color(1f, 0.985f, 0.95f, 0.93f), 7);
                tag.rectTransform.TopLeft(4, 4, cw - 8, 16);
                var title = UIKit.Label(tag.transform, "Title", (restored ? "Restored · " : "") + reg.name, 10, restored ? UIKit.Primary : UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                title.resizeTextMinSize = 7;
                title.rectTransform.Stretch(5, 0, 4, 0);
            }
            if (dim) UIKit.Panel(rt, "Locked", new Color(0.95f, 0.92f, 0.87f, 0.6f), 18).rectTransform.Stretch();
            return rt;
        }
    }
}
