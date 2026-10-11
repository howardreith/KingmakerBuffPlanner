using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // Native UI finish (UI-03, UI-11): the game's own typography and paper
    // kit, read from the loaded campaign UI (2.1.7b facts, runtime
    // kbpnui-facts-03): TextMeshPro "NexusSerif-Regular SDF" (Book Antiqua)
    // with its role materials, the "Saber_Dist32" ornamental initial, and
    // the sprites the spellbook, its spell description and the service
    // windows draw with. Assets are only referenced (never copied, cloned
    // or modified); one resolution serves one StaticCanvas instance.
    internal enum PlannerTextRole
    {
        // The window title: bold, with the native ornamental initial.
        Title,
        // Section and column headings: small capitals in the native rust ink.
        Heading,
        // Ordinary reading text.
        Body,
        // Emphasised body text (names, the selected item).
        Strong,
        // Quiet secondary lines.
        Secondary,
        // Small notes and counts.
        Small,
        // Compact button captions (the spellbook class bookmarks).
        Button,
        // Main action captions (the spellbook's Metamagic button).
        Action
    }

    internal sealed class PlannerNativeKit
    {
        // Native inks measured from the spellbook and its spell description.
        internal static readonly Color TitleInk = new Color(0.188f, 0.133f, 0.075f, 1f);
        internal static readonly Color HeadingInk = new Color(0.588f, 0.243f, 0.106f, 1f);
        internal static readonly Color BodyInk = new Color(0.173f, 0.090f, 0.047f, 1f);
        internal static readonly Color SecondaryInk = new Color(0.173f, 0.090f, 0.047f, 0.72f);
        internal static readonly Color ButtonInk = new Color(0.894f, 0.776f, 0.522f, 1f);
        internal static readonly Color ActionInk = new Color(0.878f, 0.792f, 0.541f, 1f);
        internal const string InitialColor = "#983F1D";

        internal const string BodyFontName = "NexusSerif-Regular SDF";
        internal const string InitialFontName = "Saber_Dist32";
        internal const string BodyMaterialName = "NexusSerif-Regular SDF - Cue Answer";
        internal const string ButtonMaterialName = "NexusSerif-Regular SDF - ButtonLabel";
        internal const string ActionMaterialName = "NexusSerif-Regular SDF - MainMenu";

        // The native sprites the planner draws with, by role.
        internal static readonly string[] SpriteNames =
        {
            "button_normal", "button_hover", "button_pressed", "button_disable", "main_menu_back",
            "spellbook_frame_back", "spellbook_frame_back2", "map_mask_frame", "common_frame",
            "spellbook_metamagic_decor_2", "camp_frameportrait", "camp_frameportrait_selected",
            "back_blockscroll", "back_blockscroll_simple", "blockscroll_bottom", "class_blockframe__no_left_border",
            "Separator_left", "Separator_middle", "Separator_right", "spellbook_esc",
            "Inventory_Filter_Icon_Background_Normal", "Inventory_Filter_Icon_Background_Selected",
            "CheckMark", "SliderArea", "ScrollBar_Big_Handler", "dialogue_backsheet"
        };

        private static PlannerNativeKit _cached;
        private static int _cachedCanvas;

        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>(StringComparer.Ordinal);

        internal TMP_FontAsset BodyFont { get; private set; }
        internal bool HasInitialFont { get; private set; }

        private PlannerNativeKit()
        {
        }

        internal static PlannerNativeKit Resolve(StaticCanvas canvas)
        {
            int id = canvas == null ? 0 : canvas.GetInstanceID();
            if (_cached != null && _cachedCanvas == id && _cached.BodyFont != null) return _cached;
            var kit = new PlannerNativeKit();
            kit.Load(canvas);
            _cached = kit;
            _cachedCanvas = id;
            return kit;
        }

        private void Load(StaticCanvas canvas)
        {
            foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (font == null) continue;
                if (BodyFont == null && string.Equals(font.name, BodyFontName, StringComparison.Ordinal))
                    BodyFont = font;
                if (string.Equals(font.name, InitialFontName, StringComparison.Ordinal)) HasInitialFont = true;
            }
            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
                if (material != null && !_materials.ContainsKey(material.name) &&
                    (material.name == BodyMaterialName || material.name == ButtonMaterialName ||
                     material.name == ActionMaterialName))
                    _materials[material.name] = material;
            if (canvas == null) return;
            foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
                if (image != null && image.sprite != null && !_sprites.ContainsKey(image.sprite.name) &&
                    Array.IndexOf(SpriteNames, image.sprite.name) >= 0)
                    _sprites[image.sprite.name] = image.sprite;
        }

        internal Sprite Sprite(string name)
        {
            Sprite sprite;
            return name != null && _sprites.TryGetValue(name, out sprite) && sprite != null ? sprite : null;
        }

        internal string Evidence
        {
            get
            {
                return "font=" + (BodyFont == null ? "missing" : BodyFont.name) + ";initial=" + HasInitialFont +
                    ";materials=" + string.Join(",", _materials.Keys.OrderBy(value => value).ToArray()) +
                    ";sprites=" + _sprites.Count + "/" + SpriteNames.Length + ";missing=" +
                    string.Join(",", SpriteNames.Where(name => !_sprites.ContainsKey(name)).ToArray());
            }
        }

        // The native title treatment: the first letter in the ornamental
        // initial face and ink, the rest in the body face (as the spell
        // description and the Buff Planner tooltip draw their titles).
        internal string Ornamented(string title)
        {
            if (string.IsNullOrEmpty(title) || !HasInitialFont) return title ?? string.Empty;
            return "<font=\"" + InitialFontName + "\"><color=" + InitialColor + "><size=140%>" + title.Substring(0, 1) +
                "</size></color></font>" + title.Substring(1);
        }

        // A TextMeshPro label in the native face, material and ink of its
        // role. Created inactive so TextMeshPro never falls back to its own
        // default font while the native one is assigned.
        internal TextMeshProUGUI Text(Transform parent, string name, string text, PlannerTextRole role,
            float size = 0f, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.layer = parent == null ? 0 : parent.gameObject.layer;
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            Style(label, role, size);
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = true;
            label.raycastTarget = false;
            label.text = role == PlannerTextRole.Title ? Ornamented(text) : text ?? string.Empty;
            go.SetActive(true);
            return label;
        }

        internal void Style(TMP_Text label, PlannerTextRole role, float size = 0f)
        {
            if (label == null) return;
            if (BodyFont != null) label.font = BodyFont;
            Material material = MaterialFor(role);
            if (material != null) label.fontSharedMaterial = material;
            label.fontSize = size > 0f ? size : DefaultSize(role);
            label.color = InkFor(role);
            label.fontStyle = StyleFor(role);
            label.characterSpacing = 0f;
        }

        private Material MaterialFor(PlannerTextRole role)
        {
            Material material;
            string name = role == PlannerTextRole.Button ? ButtonMaterialName
                : role == PlannerTextRole.Action ? ActionMaterialName : BodyMaterialName;
            return _materials.TryGetValue(name, out material) ? material : null;
        }

        internal static float DefaultSize(PlannerTextRole role)
        {
            switch (role)
            {
                case PlannerTextRole.Title: return 26f;
                case PlannerTextRole.Heading: return 20f;
                case PlannerTextRole.Strong: return 18f;
                case PlannerTextRole.Body: return 17f;
                case PlannerTextRole.Secondary: return 15f;
                case PlannerTextRole.Small: return 14f;
                case PlannerTextRole.Action: return 20f;
                default: return 17f;
            }
        }

        internal static Color InkFor(PlannerTextRole role)
        {
            switch (role)
            {
                case PlannerTextRole.Title: return TitleInk;
                case PlannerTextRole.Heading: return HeadingInk;
                case PlannerTextRole.Secondary:
                case PlannerTextRole.Small: return SecondaryInk;
                case PlannerTextRole.Button: return ButtonInk;
                case PlannerTextRole.Action: return ActionInk;
                default: return BodyInk;
            }
        }

        private static FontStyles StyleFor(PlannerTextRole role)
        {
            switch (role)
            {
                case PlannerTextRole.Title:
                case PlannerTextRole.Strong: return FontStyles.Bold;
                case PlannerTextRole.Heading: return FontStyles.SmallCaps;
                case PlannerTextRole.Button: return FontStyles.Bold;
                case PlannerTextRole.Action: return FontStyles.UpperCase;
                default: return FontStyles.Normal;
            }
        }

        // An Image drawing a native sprite (sliced when the sprite has
        // borders), or null when the sprite is not loaded.
        internal Image Paint(RectTransform target, string sprite, Color? tint = null, bool raycast = false)
        {
            Sprite native = Sprite(sprite);
            if (target == null || native == null) return null;
            Image image = target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
            image.sprite = native;
            image.type = native.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = tint ?? Color.white;
            image.raycastTarget = raycast;
            return image;
        }

        // A native button: the spellbook's own button art and state sprites
        // (button_normal / hover / pressed / disable) with a native caption.
        internal Button Button(Transform parent, string name, string caption, Action action,
            PlannerTextRole role = PlannerTextRole.Button, float size = 0f)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            Sprite normal = Sprite("button_normal");
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.color = normal == null ? new Color(0.24f, 0.20f, 0.16f, 1f) : Color.white;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (normal != null && Sprite("button_hover") != null && Sprite("button_pressed") != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = Sprite("button_hover"),
                    pressedSprite = Sprite("button_pressed"),
                    disabledSprite = Sprite("button_disable") ?? normal
                };
            }
            else button.transition = Selectable.Transition.ColorTint;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            if (action != null) button.onClick.AddListener(() => action());
            TextMeshProUGUI label = Text(rect, "Text", caption, role, size, TextAlignmentOptions.Center);
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = Mathf.Max(12f, label.fontSize - 5f);
            KingmakerUiFactory.Stretch(label.rectTransform, 10, 10, 2, 2);
            return button;
        }

        internal static void SetCaption(Button button, string caption)
        {
            TMP_Text label = button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = caption ?? string.Empty;
        }

        internal static void SetEnabled(Button button, bool enabled)
        {
            if (button == null) return;
            button.interactable = enabled;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.alpha = enabled ? 1f : 0.45f;
        }

        // The native ornamental rule (Separator_left / middle / right), as
        // the spell description divides its header; null without the art.
        internal RectTransform Rule(Transform parent, string name, float width, float height = 11f)
        {
            if (Sprite("Separator_left") == null || Sprite("Separator_middle") == null ||
                Sprite("Separator_right") == null) return null;
            RectTransform rule = KingmakerUiFactory.CreateRect(name, parent);
            rule.sizeDelta = new Vector2(width, height);
            float middle = 25f;
            float side = Mathf.Max(0f, (width - middle) / 2f);
            RectTransform left = KingmakerUiFactory.CreateRect("Left", rule);
            KingmakerUiFactory.SetAnchors(left, 0f, 0f, 0.5f, 1f, 0f, middle / 2f, 0f, 0f);
            Paint(left, "Separator_left");
            RectTransform centre = KingmakerUiFactory.CreateRect("Middle", rule);
            centre.anchorMin = centre.anchorMax = new Vector2(0.5f, 0.5f);
            centre.sizeDelta = new Vector2(middle, height + 0.5f);
            Paint(centre, "Separator_middle");
            RectTransform right = KingmakerUiFactory.CreateRect("Right", rule);
            KingmakerUiFactory.SetAnchors(right, 0.5f, 0f, 1f, 1f, middle / 2f, 0f, 0f, 0f);
            Paint(right, "Separator_right");
            if (side <= 0f) rule.gameObject.SetActive(false);
            return rule;
        }
    }
}
