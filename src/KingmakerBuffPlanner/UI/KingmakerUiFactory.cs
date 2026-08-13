using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class PlannerUiTheme
    {
        internal const string ParchmentPath = "ServiceWindow/CharacterScreen/BookBackground";
        internal const string HeaderFramePath =
            "ServiceWindow/CharacterScreen/BuffsAndConditions/Label/Background";
        internal const string CardFramePath =
            "ServiceWindow/SpellBook/Container_Book/Book/Image_Book/Container_SpellsLeft/" +
            "Spells_Container/SpellBookItem/Item/BakgroundBorder";
        internal const string CardNamePath =
            "ServiceWindow/SpellBook/Container_Book/Book/Image_Book/Container_SpellsLeft/" +
            "Spells_Container/SpellBookItem/Item/BakgroundFillSpellName";
        internal const string ButtonNormalPath =
            "ServiceWindow/CharacterScreen/LevelBox/Button_LevelUp";
        internal const string ButtonPressedPath =
            "ServiceWindow/SpellBook/Container_Book/BookDescription/SpellBookToggles/" +
            "ClassBookmark/BackgroundMark";
        internal const string ToggleNormalPath =
            "ServiceWindow/Inventory/Stash/Filters/SwitchBar/All";
        internal const string ToggleOnPath =
            "ServiceWindow/Inventory/Stash/Filters/SwitchBar/All/Selected";
        internal const string PortraitFramePath = "Party/Character/Frame";
        internal const string SelectedOrnamentPath = "Party/Character/Highlight";

        internal Font BodyFont;
        internal Font HeaderFont;
        internal Material BodyTextMaterial;
        internal Sprite ParchmentBackgroundSprite;
        internal Sprite NativeFrameSprite;
        internal Sprite NativeCardSprite;
        internal Sprite NativeCardNameSprite;
        internal Sprite NativeButtonNormal;
        internal Sprite NativeButtonPressed;
        internal Sprite NativeToggleOff;
        internal Sprite NativeToggleOn;
        internal Sprite NativePortraitFrame;
        internal Sprite NativeSelectedOrnament;
        internal Color ParchmentBackground = new Color(0.922f, 0.871f, 0.765f, 1f);
        internal Color ParchmentPanel = new Color(0.965f, 0.890f, 0.725f, 0.88f);
        internal Color ParchmentRaised = new Color(0.985f, 0.925f, 0.795f, 0.96f);
        internal Color ServiceSurface = new Color(0.965f, 0.865f, 0.665f, 0.70f);
        internal Color DarkBrownText = new Color(0.235f, 0.22f, 0.188f, 1f);
        internal Color MutedBrownText = new Color(0.541f, 0.392f, 0.271f, 1f);
        internal Color ButtonText = new Color(0.965f, 0.894f, 0.710f, 1f);
        internal Color BurgundyPrimary = new Color(0.493f, 0.168f, 0.098f, 1f);
        internal Color GoldAccent = new Color(0.588f, 0.243f, 0.106f, 1f);
        internal Color GreenSuccess = new Color(0.329f, 0.569f, 0.357f, 1f);
        internal Color AmberWarning = new Color(0.82f, 0.62f, 0.20f, 1f);
        internal Color RedFailure = new Color(0.843f, 0.475f, 0.412f, 1f);
        internal Color DisabledGray = new Color(0.547f, 0.547f, 0.547f, 0.8f);
        internal string ResolutionSummary;

        internal static PlannerUiTheme Resolve(Component nativeRoot)
        {
            var theme = new PlannerUiTheme();
            Text nativeText = nativeRoot == null ? null : nativeRoot.GetComponentInChildren<Text>(true);
            theme.BodyFont = nativeText == null || nativeText.font == null
                ? Resources.GetBuiltinResource<Font>("Arial.ttf") : nativeText.font;
            theme.HeaderFont = theme.BodyFont;
            theme.BodyTextMaterial = nativeText == null ? null : nativeText.material;
            if (nativeRoot != null)
            {
                Transform root = nativeRoot.transform;
                theme.ParchmentBackgroundSprite = SpriteAt(root, ParchmentPath);
                theme.NativeFrameSprite = SpriteAt(root, HeaderFramePath);
                theme.NativeCardSprite = SpriteAt(root, CardFramePath);
                theme.NativeCardNameSprite = SpriteAt(root, CardNamePath);
                theme.NativeButtonNormal = SpriteAt(root, ButtonNormalPath);
                theme.NativeButtonPressed = SpriteAt(root, ButtonPressedPath);
                theme.NativeToggleOff = SpriteAt(root, ToggleNormalPath);
                theme.NativeToggleOn = SpriteAt(root, ToggleOnPath);
                theme.NativePortraitFrame = SpriteAt(root, PortraitFramePath);
                theme.NativeSelectedOrnament = SpriteAt(root, SelectedOrnamentPath);
            }
            theme.ResolutionSummary = "parchment=" + Name(theme.ParchmentBackgroundSprite) +
                ";frame=" + Name(theme.NativeFrameSprite) + ";card=" +
                Name(theme.NativeCardSprite) + ";cardName=" + Name(theme.NativeCardNameSprite) +
                ";button=" + Name(theme.NativeButtonNormal) + ";pressed=" +
                Name(theme.NativeButtonPressed) + ";toggleOff=" + Name(theme.NativeToggleOff) +
                ";toggleOn=" + Name(theme.NativeToggleOn) + ";portrait=" +
                Name(theme.NativePortraitFrame) + ";selected=" +
                Name(theme.NativeSelectedOrnament) + ";font=" +
                (theme.BodyFont == null ? "missing" : theme.BodyFont.name) + ";textMaterial=" +
                (theme.BodyTextMaterial == null || theme.BodyTextMaterial.shader == null
                    ? "UI/Default" : theme.BodyTextMaterial.shader.name);
            return theme;
        }

        private static Sprite SpriteAt(Transform root, string path)
        {
            Transform transform = root == null ? null : root.Find(path);
            Image image = transform == null ? null : transform.GetComponent<Image>();
            return image == null ? null : image.sprite;
        }

        private static string Name(Sprite sprite)
        {
            return sprite == null ? "fallback" : sprite.name;
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
                image.type = sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
            }
            return image;
        }

        internal static Image AddFramedPanel(
            RectTransform rect,
            Color color,
            Color border,
            float thickness = 1f)
        {
            Image image = AddPanel(rect, color);
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(thickness, -thickness);
            outline.useGraphicAlpha = true;
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
            text.font = theme.BodyFont;
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = size;
            text.resizeTextMaxSize = size;
            if (theme.BodyTextMaterial != null) text.material = theme.BodyTextMaterial;
            text.color = theme.DarkBrownText;
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
            Image image = AddPanel(rect, theme.ParchmentRaised, theme.NativeButtonNormal);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.72f, 1f);
            colors.pressedColor = new Color(0.76f, 0.60f, 0.43f, 1f);
            colors.disabledColor = theme.DisabledGray;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (theme.NativeButtonPressed != null)
            {
                SpriteState sprites = button.spriteState;
                sprites.pressedSprite = theme.NativeButtonPressed;
                button.spriteState = sprites;
            }
            if (action != null) button.onClick.AddListener(action);
            Text text = CreateText("Label", rect, theme, label, 17, TextAnchor.MiddleCenter);
            text.color = theme.ButtonText;
            text.fontStyle = FontStyle.Bold;
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
            AddFramedPanel(rect, theme.ParchmentRaised, theme.GoldAccent);
            Text inputText = CreateText("Text", rect, theme, string.Empty, 17, TextAnchor.MiddleLeft);
            inputText.supportRichText = false;
            Stretch(inputText.rectTransform, 10, 8, 5, 5);
            Text hint = CreateText("Placeholder", rect, theme, placeholder, 17, TextAnchor.MiddleLeft);
            hint.color = theme.MutedBrownText;
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
            AddFramedPanel(root, theme.ParchmentPanel, theme.GoldAccent);
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
            layout.childControlHeight = true;
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
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
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

        internal static void ForceLayoutAndSnap(RectTransform root)
        {
            if (root == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                rect.localScale = Vector3.one;
                Vector2 anchored = rect.anchoredPosition;
                Vector2 size = rect.sizeDelta;
                rect.anchoredPosition = new Vector2(Mathf.Round(anchored.x), Mathf.Round(anchored.y));
                rect.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
                if (rect.anchorMin.x != rect.anchorMax.x || rect.anchorMin.y != rect.anchorMax.y)
                {
                    Vector2 min = rect.offsetMin;
                    Vector2 max = rect.offsetMax;
                    rect.offsetMin = new Vector2(Mathf.Round(min.x), Mathf.Round(min.y));
                    rect.offsetMax = new Vector2(Mathf.Round(max.x), Mathf.Round(max.y));
                }
            }
            Canvas.ForceUpdateCanvases();
        }
    }
}
