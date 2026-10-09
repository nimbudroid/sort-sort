using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Gives each room its own identity behind the play area: a wall in the room's colours with a simple pattern
    /// (tiles, planks, dots or stripes) made from the existing toy sprites. Decoration only: no colliders, nothing
    /// shaped like an object, sorted behind the play zone. Built once per board; nothing animates.
    /// </summary>
    public static class RoomBackdrop
    {
        public static void Build(Transform parent, RoomTheme theme, float left, float right, float tableTop, float top)
        {
            if (theme == null) return;
            var root = new GameObject("RoomBackdrop").transform;
            root.SetParent(parent, false);

            Color wall = ToyStyle.Hex(theme.wallHex);
            Color accent = ToyStyle.Hex(theme.wallAccentHex);
            float bottom = tableTop - 0.4f;
            float h = top + 3f - bottom;
            var rr = ProcSprites.RoundedRect();
            Sliced(root, rr, new Vector2(0f, bottom + h / 2f), new Vector2(right - left + 2f, h), wall, -96);

            switch (theme.pattern)
            {
                case "tiles":
                {
                    float cell = 1.05f, sz = 0.9f;
                    for (float y = bottom + cell * 0.6f; y < top + 1f; y += cell)
                        for (float x = left + cell * 0.5f; x < right + cell; x += cell)
                            Sliced(root, rr, new Vector2(x, y), new Vector2(sz, sz), new Color(accent.r, accent.g, accent.b, 0.55f), -95);
                    break;
                }
                case "planks":
                {
                    for (float y = bottom + 0.5f; y < top + 1f; y += 0.95f)
                        Sliced(root, rr, new Vector2(0f, y), new Vector2(right - left + 1f, 0.78f), new Color(accent.r, accent.g, accent.b, 0.6f), -95);
                    break;
                }
                case "dots":
                {
                    var circle = ProcSprites.Circle();
                    int row = 0;
                    for (float y = bottom + 0.8f; y < top + 1f; y += 1.1f, row++)
                        for (float x = left + (row % 2) * 0.6f; x < right + 1f; x += 1.2f)
                            Disc(root, circle, new Vector2(x, y), 0.32f, new Color(accent.r, accent.g, accent.b, 0.8f), -95);
                    break;
                }
                case "stripes":
                {
                    for (float x = left + 0.3f; x < right + 1f; x += 1.2f)
                        Sliced(root, rr, new Vector2(x, bottom + h / 2f), new Vector2(0.5f, h), new Color(accent.r, accent.g, accent.b, 0.55f), -95);
                    break;
                }
            }
            // A soft skirting band where the wall meets the table, so the table reads as standing against it.
            Sliced(root, rr, new Vector2(0f, tableTop + 0.15f), new Vector2(right - left + 2f, 0.5f),
                Color.Lerp(accent, ToyStyle.Ink, 0.15f), -94);
        }

        static void Sliced(Transform parent, Sprite sprite, Vector2 center, Vector2 size, Color c, int order)
        {
            var go = new GameObject("Pattern");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = c;
            sr.sortingOrder = order;
        }

        static void Disc(Transform parent, Sprite sprite, Vector2 center, float d, Color c, int order)
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(d, d, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = order;
        }
    }
}
