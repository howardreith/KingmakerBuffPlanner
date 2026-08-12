using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class PlannerUiTheme
    {
        internal Font Font;
        internal Sprite PanelSprite;
        internal Color Background = new Color(0.075f, 0.055f, 0.035f, 1f);
        internal Color Panel = new Color(0.16f, 0.115f, 0.07f, 1f);
        internal Color PanelLight = new Color(0.25f, 0.18f, 0.11f, 1f);
        internal Color Accent = new Color(0.72f, 0.52f, 0.22f, 1f);
        internal Color AccentSelected = new Color(0.45f, 0.20f, 0.08f, 1f);
        internal Color Text = new Color(0.94f, 0.86f, 0.68f, 1f);
        internal Color MutedText = new Color(0.72f, 0.66f, 0.55f, 1f);
        internal Color Disabled = new Color(0.22f, 0.20f, 0.18f, 0.9f);

        internal static PlannerUiTheme Resolve(Component nativeRoot)
        {
            var theme = new PlannerUiTheme();
            Text nativeText = nativeRoot == null ? null : nativeRoot.GetComponentInChildren<Text>(true);
            theme.Font = nativeText == null || nativeText.font == null
                ? Resources.GetBuiltinResource<Font>("Arial.ttf") : nativeText.font;
            if (nativeRoot != null)
            {
                Image[] images = nativeRoot.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    RectTransform rect = image == null ? null : image.rectTransform;
                    if (image != null && image.sprite != null && rect != null &&
                        rect.rect.width >= 600 && rect.rect.height >= 400)
                    {
                        theme.PanelSprite = image.sprite;
                        break;
                    }
                }
            }
            return theme;
        }
    }

    internal static class KingmakerUiFactory
    {
        internal static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        internal static Image AddPanel(RectTransform rect, Color color, Sprite sprite = null)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        internal static Text CreateText(
            string name,
            Transform parent,
            PlannerUiTheme theme,
            string value,
            int size,
            TextAnchor alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = theme.Font;
            text.fontSize = size;
            text.color = theme.Text;
            text.alignment = alignment;
            text.text = value ?? string.Empty;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        internal static Button CreateButton(
            string name,
            Transform parent,
            PlannerUiTheme theme,
            string label,
            UnityAction action)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = AddPanel(rect, theme.PanelLight, theme.PanelSprite);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.10f, 0.95f, 1f);
            colors.pressedColor = new Color(0.72f, 0.62f, 0.48f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (action != null) button.onClick.AddListener(action);
            Text text = CreateText("Label", rect, theme, label, 17, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 5, 5, 3, 3);
            return button;
        }

        internal static InputField CreateInputField(
            string name,
            Transform parent,
            PlannerUiTheme theme,
            string placeholder)
        {
            RectTransform rect = CreateRect(name, parent);
            AddPanel(rect, new Color(0.07f, 0.06f, 0.05f, 1f));
            Text inputText = CreateText("Text", rect, theme, string.Empty, 17, TextAnchor.MiddleLeft);
            inputText.supportRichText = false;
            Stretch(inputText.rectTransform, 10, 8, 5, 5);
            Text hint = CreateText("Placeholder", rect, theme, placeholder, 17, TextAnchor.MiddleLeft);
            hint.color = theme.MutedText;
            hint.fontStyle = FontStyle.Italic;
            Stretch(hint.rectTransform, 10, 8, 5, 5);
            InputField field = rect.gameObject.AddComponent<InputField>();
            field.textComponent = inputText;
            field.placeholder = hint;
            field.targetGraphic = rect.GetComponent<Image>();
            return field;
        }

        internal static ScrollRect CreateScrollView(
            string name,
            Transform parent,
            PlannerUiTheme theme,
            out RectTransform content)
        {
            RectTransform root = CreateRect(name, parent);
            AddPanel(root, new Color(0.055f, 0.045f, 0.035f, 1f));
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            RectTransform viewport = CreateRect("Viewport", root);
            Stretch(viewport, 3, 3, 3, 3);
            Image viewportImage = AddPanel(viewport, Color.white);
            // Mask uses the graphic's alpha-clipped pixels to write its stencil.
            // showMaskGraphic=false already suppresses the visible color, so the
            // stencil source itself must remain opaque.
            viewportImage.color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        internal static void Stretch(RectTransform rect, float left = 0, float right = 0,
            float bottom = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static void SetAnchors(RectTransform rect, float minX, float minY,
            float maxX, float maxY, float left = 0, float right = 0, float bottom = 0, float top = 0)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static LayoutElement AddLayout(RectTransform rect, float height)
        {
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            return layout;
        }

        internal static void DestroyChildren(RectTransform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                GameObject child = parent.GetChild(index).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }
    }
}
