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
            var root = UIKit.Panel(parent, "KitchenOverview", SpriteFactory.Hex("#B07C4D"), 18);
            var rt = root.rectTransform;
            rt.sizeDelta = new Vector2(width, height);
            float pad = 6, cw = (width - pad * 3) / 2, ch = (height - pad * 3) / 2;

            for (int r = 0; r < 4; r++)
            {
                var anchor = KitchenCatalogue.Anchors[r];
                bool restored = save.anchorIds.Contains(anchor.AnchorId);
                string variant = previewVariant != null && previewVariant.TryGetValue(anchor.AnchorId, out var pv) ? pv : save.Equipped(anchor.AnchorId);
                float x = pad + (r % 2) * (cw + pad), y = pad + (r / 2) * (ch + pad);

                // A small corner of the room: tiled wall, furniture, wooden counter.
                var card = UIKit.Panel(rt, "Region_" + KitchenCatalogue.Regions[r], SpriteFactory.Hex("#E9DCC8"), 12);
                card.rectTransform.TopLeft(x, y, cw, ch);
                card.gameObject.AddComponent<RectMask2D>();
                KitchenArt.Tiled(card.transform, "Wall", KitchenArt.WallTile(), Color.white).rectTransform.Stretch();
                float counterH = 20, counterY = ch - counterH;
                foreach (var (prop, px, size) in Furniture(r, cw, ch))
                {
                    var f = UIKit.Icon(card.transform, "Prop_" + prop, KitchenArt.Prop(prop), size);
                    f.rectTransform.TopLeft(px, counterY - size + 4, size, size);
                }
                var counter = KitchenArt.Tiled(card.transform, "Counter", KitchenArt.Wood(), Color.white);
                counter.rectTransform.TopLeft(0, counterY, cw, counterH);
                var lip = UIKit.Panel(card.transform, "Lip", SpriteFactory.Hex("#A87444"), 0);
                lip.rectTransform.TopLeft(0, ch - 5, cw, 5);

                if (restored)
                {
                    var items = Compositions[r];
                    float iconSize = Mathf.Min(26, (cw - 16) / items.Length);
                    float step = (cw - 16) / items.Length;
                    for (int i = 0; i < items.Length; i++)
                    {
                        var ic = UIKit.Icon(card.transform, items[i], SpriteFactory.Object(items[i]), iconSize);
                        ic.rectTransform.TopLeft(8 + step * i + (step - iconSize) / 2, counterY - iconSize + 8, iconSize, iconSize);
                    }
                    if (!string.IsNullOrEmpty(variant))
                    {
                        // Optional appearance accent: presentation only, never changes rules.
                        var accent = UIKit.Panel(card.transform, "Accent_" + variant, KitchenOverviewView.AccentColor(variant), 2);
                        accent.rectTransform.TopLeft(0, counterY - 1, cw, 4);
                    }
                }
                else
                {
                    // Unrestored: dusty and cluttered, with a stable trace miniature for each completed scene.
                    var dust = UIKit.Panel(card.transform, "Dust", new Color(0.55f, 0.48f, 0.42f, 0.38f), 0);
                    dust.rectTransform.Stretch();
                    var items = Compositions[r];
                    for (int i = 0; i < items.Length; i++)
                    {
                        var ic = UIKit.Icon(card.transform, "Clutter" + i, SpriteFactory.Object(items[i]), 20, new Color(1, 1, 1, 0.75f));
                        ic.rectTransform.Center(16 + i * 15, counterY + 4 - (i % 2) * 5, 20, 20);
                        ic.rectTransform.localRotation = Quaternion.Euler(0, 0, BoardView.BeforeRotation(i) * 3);
                    }
                    foreach (var level in cat.Levels.Where(l => l.ProjectRegion == KitchenCatalogue.Regions[r]))
                    {
                        if (!save.traceIds.Contains(Progression.TraceId(level.LevelId))) continue;
                        var slot = TraceSlots[level.TraceIndex];
                        float tw = Mathf.Min(36, cw * 0.28f), th = 20;
                        var tray = UIKit.Panel(card.transform, "Trace_" + level.LevelId, SpriteFactory.Hex("#F4E3C4"), 6);
                        tray.rectTransform.Center(slot.x * cw, 18 + slot.y * (counterY - 22), tw, th);
                        var objs = level.RequiredBoard.Objects;
                        for (int i = 0; i < 2 && i < objs.Count; i++)
                        {
                            var mini = UIKit.Icon(tray.transform, "Mini" + i, SpriteFactory.Object(objs[i * (objs.Count / 2)].AssetId), 16);
                            mini.rectTransform.anchoredPosition = new Vector2(-8 + i * 16, 2);
                        }
                    }
                }

                var tag = UIKit.Panel(card.transform, "TitleTag", new Color(1f, 0.985f, 0.95f, 0.93f), 7);
                tag.rectTransform.TopLeft(4, 4, Mathf.Min(cw - 8, 112), 17);
                var title = UIKit.Label(tag.transform, "Title", (restored ? "✓ " : "") + Titles[r], 10, restored ? UIKit.Primary : UIKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                title.rectTransform.Stretch(5, 0, 4, 0);
            }
            return rt;
        }

        static (string prop, float x, float size)[] Furniture(int region, float cw, float ch)
        {
            float big = Mathf.Min(ch - 12, 70);
            switch (region)
            {
                case 0: return new[] { ("window", cw * 0.5f - big * 0.45f, big * 0.9f), ("plant", cw - big * 0.5f - 4, big * 0.5f) };
                case 1: return new[] { ("pantry_shelf", cw * 0.5f - big * 0.5f, big) };
                case 2: return new[] { ("rail", 6, big * 0.6f), ("stove", cw * 0.5f - big * 0.4f + 10, big * 0.85f) };
                default: return new[] { ("plate_shelf", cw * 0.5f - big * 0.55f, big * 0.85f), ("basket", cw - big * 0.45f - 6, big * 0.42f) };
            }
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
