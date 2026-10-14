using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>Small helpers to build uGUI hierarchies from code.</summary>
    public static class UIFactory
    {
        public static readonly Color Cream = new Color32(255, 247, 228, 255);
        public static readonly Color CreamDark = new Color32(244, 228, 196, 255);
        public static readonly Color Wood = new Color32(139, 90, 43, 255);
        public static readonly Color WoodDark = new Color32(98, 60, 26, 255);
        public static readonly Color Green = new Color32(102, 187, 66, 255);
        public static readonly Color GreenDark = new Color32(56, 128, 40, 255);
        public static readonly Color Orange = new Color32(245, 166, 35, 255);
        public static readonly Color Red = new Color32(224, 90, 71, 255);
        public static readonly Color Blue = new Color32(66, 150, 220, 255);
        public static readonly Color Gray = new Color32(170, 170, 170, 255);
        public static readonly Color TextDark = new Color32(80, 50, 20, 255);

        private static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel", bool raycast = true)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProcSprites.Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Solid(Transform parent, Color color, string name = "Solid")
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool bold = false, bool outline = false)
        {
            RectTransform rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.25f, 0.15f, 0.05f, 0.9f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static Image Icon(Transform parent, string spriteName, float size)
        {
            RectTransform rt = Rect("Icon", parent);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteDB.Get(spriteName);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Button MakeButton(Transform parent, string label, Color color, UnityAction onClick, int fontSize = 30)
        {
            Image bg = Panel(parent, color, "Button");
            var b = bg.gameObject.AddComponent<Button>();
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.8f);
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(onClick);
            if (!string.IsNullOrEmpty(label))
            {
                Text t = Label(bg.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, true, true);
                t.rectTransform.Stretch(6f);
            }
            return b;
        }

        /// <summary>Round HUD-style button using one of the hud_* sprites, with a caption under it.</summary>
        public static Button IconButton(Transform parent, string sprite, string caption, float size, UnityAction onClick)
        {
            RectTransform rt = Rect("IconButton", parent);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteDB.Get(sprite);
            img.preserveAspect = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.onClick.AddListener(onClick);
            if (!string.IsNullOrEmpty(caption))
            {
                Text t = Label(rt, caption, 24, Color.white, TextAnchor.UpperCenter, true, true);
                t.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0, 6), new Vector2(size + 60, 34));
            }
            return b;
        }

        public static ScrollRect Scroll(Transform parent, bool horizontal, out RectTransform content)
        {
            RectTransform root = Rect("Scroll", parent);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            var img = root.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            RectTransform vp = Rect("Viewport", root).Stretch();
            vp.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", vp);
            if (horizontal)
            {
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 0.5f);
            }
            else
            {
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0.5f, 1f);
            }
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = horizontal ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp;
            sr.content = content;
            sr.horizontal = horizontal;
            sr.vertical = !horizontal;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
            return sr;
        }

        public static GridLayoutGroup Grid(RectTransform content, Vector2 cell, Vector2 spacing, bool horizontal)
        {
            var g = content.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.padding = new RectOffset(16, 16, 16, 16);
            g.childAlignment = horizontal ? TextAnchor.MiddleLeft : TextAnchor.UpperCenter;
            if (horizontal)
            {
                g.startAxis = GridLayoutGroup.Axis.Vertical;
                g.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                g.constraintCount = 1;
            }
            return g;
        }

        public static HorizontalLayoutGroup HRow(RectTransform rt, float spacing)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }

        public static void SetInteractable(Button b, bool on)
        {
            b.interactable = on;
        }

        public static void Delay(MonoBehaviour host, float seconds, Action a)
        {
            host.StartCoroutine(DelayCo(seconds, a));
        }

        private static System.Collections.IEnumerator DelayCo(float s, Action a)
        {
            yield return new WaitForSeconds(s);
            a();
        }
    }
}
