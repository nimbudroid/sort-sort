using System.Linq;
using SortEverything.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>
    /// Compact kitchen overview with four predefined restoration regions (Section 12). Each first completion adds a
    /// stable trace miniature; anchors replace that region's miniatures with a neat composition. Never shows a
    /// restored region as dirty again. Decorative images have no raycast target.
    /// </summary>
    public static class KitchenOverviewView
    {
        static readonly string[][] Compositions =
        {
            new[] { "apple", "orange", "milk_carton", "mug" },
            new[] { "carrot", "broccoli", "corn", "banana" },
            new[] { "pan", "spatula", "spoon", "plate" },
            new[] { "plate", "mug", "water_bottle", "strawberry" },
        };
        static readonly string[] Titles = { "Breakfast corner", "Pantry corner", "Cooking area", "Finishing touches" };
        static readonly Vector2[] TraceSlots = { new Vector2(0.18f, 0.40f), new Vector2(0.50f, 0.40f), new Vector2(0.82f, 0.40f), new Vector2(0.34f, 0.72f), new Vector2(0.66f, 0.72f) };

        /// <param name="previewVariant">Temporary presentation-only variant (anchorId → variantId); null for saved state.</param>
        public static RectTransform Build(Transform parent, SaveData save, Catalogue cat, float width, float height,
            System.Collections.Generic.Dictionary<string, string> previewVariant = null)
        {
            var root = UIKit.Panel(parent, "KitchenOverview", UIKit.Wall, 18);
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(width, height);
            float pad = 6, cw = (width - pad * 3) / 2, ch = (height - pad * 3) / 2;

            for (int r = 0; r < 4; r++)
            {
                var anchor = KitchenCatalogue.Anchors[r];
                bool restored = save.anchorIds.Contains(anchor.AnchorId);
                string variant = previewVariant != null && previewVariant.TryGetValue(anchor.AnchorId, out var pv) ? pv : save.Equipped(anchor.AnchorId);
                float x = pad + (r % 2) * (cw + pad), y = pad + (r / 2) * (ch + pad);
                var card = UIKit.Panel(rt, "Region_" + KitchenCatalogue.Regions[r], restored ? SpriteFactory.Hex("#FBF3E4") : SpriteFactory.Hex("#E9DDCC"), 12);
                card.rectTransform.TopLeft(x, y, cw, ch);
                var title = UIKit.Label(card.transform, "Title", Titles[r], 11, UIKit.InkSoft, TextAnchor.UpperLeft, FontStyle.Bold);
                title.rectTransform.TopLeft(6, 3, cw - 12, 16);

                if (restored)
                {
                    // Counter strip and a neat arrangement of existing assets.
                    var strip = UIKit.Panel(card.transform, "Counter", UIKit.CounterDark, 6);
                    strip.rectTransform.TopLeft(6, ch - 18, cw - 12, 10);
                    var items = Compositions[r];
                    float step = (cw - 16) / items.Length;
                    for (int i = 0; i < items.Length; i++)
                    {
                        var ic = UIKit.Icon(card.transform, items[i], SpriteFactory.Object(items[i]), Mathf.Min(step, ch - 30));
                        ic.rectTransform.Center(8 + step * (i + 0.5f), ch - 18 - Mathf.Min(step, ch - 30) / 2 + 2, Mathf.Min(step, ch - 30), Mathf.Min(step, ch - 30));
                    }
                    if (!string.IsNullOrEmpty(variant))
                    {
                        // Optional appearance accent: presentation only, never changes rules.
                        var accent = UIKit.Panel(card.transform, "Accent_" + variant, AccentColor(variant), 4);
                        accent.rectTransform.TopLeft(6, 20, cw - 12, 5);
                        var tag = UIKit.Label(card.transform, "AccentName", Strings.Get("variant." + variant), 9, UIKit.InkSoft, TextAnchor.UpperRight);
                        tag.rectTransform.TopLeft(6, 3, cw - 12, 14);
                    }
                }
                else
                {
                    // Unrestored: faint clutter plus stable trace miniatures for completed scenes in this region.
                    var items = Compositions[r];
                    for (int i = 0; i < items.Length; i++)
                    {
                        var ic = UIKit.Icon(card.transform, "Clutter" + i, SpriteFactory.Object(items[i]), 18, new Color(1, 1, 1, 0.22f));
                        ic.rectTransform.Center(14 + i * 14, ch - 14 - (i % 2) * 6, 18, 18);
                        ic.rectTransform.localRotation = Quaternion.Euler(0, 0, BoardView.BeforeRotation(i) * 2);
                    }
                    foreach (var level in cat.Levels.Where(l => l.ProjectRegion == KitchenCatalogue.Regions[r]))
                    {
                        if (!save.traceIds.Contains(Progression.TraceId(level.LevelId))) continue;
                        var slot = TraceSlots[level.TraceIndex];
                        var tray = UIKit.Panel(card.transform, "Trace_" + level.LevelId, SpriteFactory.Hex("#E3C79C"), 6);
                        float tw = Mathf.Min(34, cw * 0.28f), th = 18;
                        tray.rectTransform.Center(slot.x * cw, slot.y * ch, tw, th);
                        var objs = level.RequiredBoard.Objects;
                        for (int i = 0; i < 2 && i < objs.Count; i++)
                        {
                            var mini = UIKit.Icon(tray.transform, "Mini" + i, SpriteFactory.Object(objs[i * (objs.Count / 2)].AssetId), 14);
                            mini.rectTransform.anchoredPosition = new Vector2(-7 + i * 14, 2);
                        }
                    }
                }
            }
            return rt;
        }

        public static Color AccentColor(string variant)
        {
            switch (variant)
            {
                case "woven_basket": return SpriteFactory.Hex("#C28B4E");
                case "shelf_trim": return SpriteFactory.Hex("#4F8C7A");
                case "utensil_rail": return SpriteFactory.Hex("#9AA3AE");
                default: return SpriteFactory.Hex("#D27A5B");
            }
        }
    }
}
