using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SortEverything.Presentation
{
    /// <summary>Theme colors and small uGUI construction helpers. Decorative images never raycast.</summary>
    public static class UIKit
    {
        public static readonly Color Ink = SpriteFactory.Hex("#3A2E28");
        public static readonly Color InkSoft = SpriteFactory.Hex("#6E5F55");
        public static readonly Color Paper = SpriteFactory.Hex("#FFF8EE");
        public static readonly Color Counter = SpriteFactory.Hex("#EBD8BE");
        public static readonly Color CounterDark = SpriteFactory.Hex("#D9C0A0");
        public static readonly Color Wall = SpriteFactory.Hex("#F6EBDD");
        public static readonly Color Primary = SpriteFactory.Hex("#2F7D6D");
        public static readonly Color PrimaryText = Color.white;
        public static readonly Color Secondary = SpriteFactory.Hex("#FFFFFF");
        public static readonly Color Warn = SpriteFactory.Hex("#B5452F");
        public static readonly Color Gold = SpriteFactory.Hex("#C58B1B");
        public static readonly Color Scrim = new Color(0.16f, 0.12f, 0.1f, 0.45f);

        static Font _font;
        public static Font Font => _font ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Color CategoryColor(string categoryId)
        {
            switch (categoryId)
            {
                case "fruit": return SpriteFactory.Hex("#E9B48A");      // woven fruit basket
                case "vegetables": return SpriteFactory.Hex("#B9D49A"); // green wicker
                case "drinks": return SpriteFactory.Hex("#A9C6DD");     // drinks crate
                case "kitchenware": return SpriteFactory.Hex("#C9A983"); // wooden drawer
                default: return SpriteFactory.Hex("#F1E6CF");           // shared tray
            }
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Places a rect by its top-left offset from the parent's top-left (y downward).</summary>
        public static RectTransform TopLeft(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Places a rect by its center, measured from the parent's top-left (y downward).</summary>
        public static RectTransform Center(this RectTransform rt, float cx, float cy, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -cy);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Bottom(this RectTransform rt, float height, float margin = 0)
        {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, margin);
            rt.sizeDelta = new Vector2(0, height);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius = 14, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = radius > 0 ? SpriteFactory.RoundedRect(radius) : null;
            img.type = radius > 0 ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, float size, Color? tint = null)
        {
            var rt = Rect(parent, name);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = tint ?? Color.white;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, size - 6);
            t.resizeTextMaxSize = size;
            return t;
        }

        public static Button Button(Transform parent, string name, string text, UnityAction onClick, bool primary = false, int fontSize = 16)
        {
            var img = Panel(parent, name, primary ? Primary : Secondary, 16, raycast: true);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            if (!primary)
            {
                var outline = img.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.23f, 0.18f, 0.16f, 0.25f);
                outline.effectDistance = new Vector2(1, -1);
            }
            var label = Label(img.transform, "Label", text, fontSize, primary ? PrimaryText : Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Stretch(8, 2, 8, 2);
            return btn;
        }

        public static void SetText(this Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing, int padding = 0, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = align;
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return v;
        }

        public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlHeight = true; h.childControlWidth = true;
            h.childForceExpandHeight = true; h.childForceExpandWidth = true;
            return h;
        }

        public static LayoutElement Size(this Component c, float preferredHeight, float preferredWidth = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            if (preferredWidth >= 0) { le.preferredWidth = preferredWidth; le.minWidth = preferredWidth; }
            return le;
        }

        /// <summary>A scrollable vertical list. Returns the content rect.</summary>
        public static RectTransform ScrollList(Transform parent, string name, float spacing = 8)
        {
            var root = Rect(parent, name);
            var mask = root.gameObject.AddComponent<RectMask2D>();
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var content = Rect(root, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            VStack(content, spacing, 4);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = root;
            return content;
        }

        public static IEnumerator Tween(float duration, Action<float> step, Action done = null)
        {
            float t = 0;
            if (duration <= 0) { step(1); done?.Invoke(); yield break; }
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1);
            done?.Invoke();
        }

        public static float EaseOut(float t) => 1 - (1 - t) * (1 - t) * (1 - t);
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.4f, c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string FormatMs(long ms)
        {
            if (ms < 0) return "—";
            var ts = TimeSpan.FromMilliseconds(ms);
            return ts.TotalMinutes >= 1 ? $"{(int)ts.TotalMinutes}:{ts.Seconds:00}.{ts.Milliseconds / 100}" : $"{ts.Seconds}.{ts.Milliseconds / 100}s";
        }
    }
}
