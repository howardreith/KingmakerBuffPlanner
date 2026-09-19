using System;
using Kingmaker;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // The Buff Planner's own campaign theme adapter. It adapts the shared,
    // engine-agnostic capability machinery to the installed Kingmaker UI:
    // bounded node access, structural donor validation, presentation-property
    // borrowers, and the single native click-sound route. Donors are only
    // read; nothing native is cloned, destroyed, or re-parented, and no
    // controller/event trees are copied.
    internal sealed class PlannerNativeTheme
    {
        internal readonly NativeThemeResolution Resources;
        internal string ResolutionSummary { get { return Resources.Summary; } }

        private PlannerNativeTheme(NativeThemeResolution resources)
        {
            Resources = resources;
        }

        // Donor lookup and ownership are NATIVE-canvas scoped: every recorded
        // path (ServiceWindow/... ) descends from the StaticCanvas, never from
        // the planner's own overlay. The planner root remains only the
        // application scope and must never be passed here — resolving from it
        // walks the planner's own children and rejects every donor.
        internal static PlannerNativeTheme Resolve(Component nativeLookupRoot)
        {
            if (nativeLookupRoot == null) throw new ArgumentNullException("nativeLookupRoot");
            return new PlannerNativeTheme(NativeThemeResolver.Resolve(
                nativeLookupRoot.transform, new UnitySource()));
        }

        private sealed class UnitySource : INativeThemeSource
        {
            public bool IsAlive(object value)
            {
                return value is UnityEngine.Object && (UnityEngine.Object)value != null;
            }

            public bool SameNode(object first, object second)
            {
                return (UnityEngine.Object)first == (UnityEngine.Object)second;
            }

            public string Name(object node) { return ((Transform)node).name; }
            public object Parent(object node) { return ((Transform)node).parent; }
            public int ChildCount(object node) { return ((Transform)node).childCount; }
            public object Child(object node, int index)
            {
                return ((Transform)node).GetChild(index);
            }

            public object[] Components(object node, NativeThemeComponent component)
            {
                Type type;
                switch (component)
                {
                    case NativeThemeComponent.Image: type = typeof(Image); break;
                    case NativeThemeComponent.Button: type = typeof(Button); break;
                    case NativeThemeComponent.Text: type = typeof(Text); break;
                    case NativeThemeComponent.InputField: type = typeof(InputField); break;
                    case NativeThemeComponent.Scrollbar: type = typeof(Scrollbar); break;
                    default:
                        throw new ArgumentOutOfRangeException("component");
                }
                return ((Transform)node).GetComponents(type);
            }

            public NativeThemeResource SoundResource()
            {
                UICommon common = Game.Instance == null || Game.Instance.UI == null
                    ? null : Game.Instance.UI.Common;
                if (common == null || common.UISound == null)
                    throw new InvalidOperationException(
                        "Native UI sound player is unavailable in this game state.");
                return new NativeThemeResource
                {
                    Nodes = new object[0],
                    Components = new object[] { common },
                    Identity = "locator=UICommon.UISound"
                };
            }

            public void Validate(NativeThemeCapability capability, object[] values)
            {
                // Structural validation only. Exact sprite-name/border/pixel
                // contracts for every donor are intentionally not asserted
                // here: the live campaign inventory lane is still open, and a
                // wrong hard assumption would disable a whole surface. Each
                // borrower re-checks what it actually consumes.
                switch (capability)
                {
                    case NativeThemeCapability.Paper:
                    case NativeThemeCapability.Ornament:
                        RequireSlicedSprite((Image)values[0]);
                        break;
                    case NativeThemeCapability.Buttons:
                        var button = (Button)values[0];
                        if (button.targetGraphic as Image == null)
                            throw new InvalidOperationException(
                                "Native button donor has no Image target graphic.");
                        RequireSlicedSprite((Image)button.targetGraphic);
                        break;
                    case NativeThemeCapability.ButtonText:
                    case NativeThemeCapability.Body:
                        if (((Text)values[0]).font == null)
                            throw new InvalidOperationException(
                                "Native text donor has no font.");
                        break;
                    case NativeThemeCapability.Input:
                        if (((InputField)values[0]).targetGraphic as Image == null)
                            throw new InvalidOperationException(
                                "Native input donor has no Image target graphic.");
                        break;
                    case NativeThemeCapability.Scrollbar:
                        var scrollbar = (Scrollbar)values[0];
                        if (scrollbar.handleRect == null ||
                            scrollbar.handleRect.GetComponent<Image>() == null)
                            throw new InvalidOperationException(
                                "Native scrollbar donor has no handle image.");
                        break;
                    case NativeThemeCapability.Sound:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("capability");
                }
            }

            private static void RequireSlicedSprite(Image image)
            {
                if (image == null || image.sprite == null || image.sprite.texture == null)
                    throw new InvalidOperationException(
                        "Native sliced Image unavailable for theme borrowing.");
                if (image.type != Image.Type.Sliced && image.sprite.border.sqrMagnitude <= 0f)
                    throw new InvalidOperationException(
                        "Native donor image is neither sliced nor borderless-safe.");
            }
        }

        // True only when the donor provides the complete native state set; a
        // partial donor keeps the readable tint transition instead of a
        // half-native button (never present an unverified state as native).
        internal static bool HasCompleteSpriteStates(Button donor)
        {
            return donor != null && donor.targetGraphic is Image &&
                ((Image)donor.targetGraphic).sprite != null &&
                donor.spriteState.highlightedSprite != null &&
                donor.spriteState.pressedSprite != null &&
                donor.spriteState.disabledSprite != null;
        }

        // Factory fallback tints that the borrowed native artwork must
        // replace; any other color is a deliberate status tint (selected
        // rows, warnings) and survives theming untouched.
        private static readonly Color[] FactoryFallbackTints =
        {
            new Color(0.985f, 0.925f, 0.795f, 0.96f),   // ParchmentRaised
            new Color(0.965f, 0.890f, 0.725f, 0.88f),   // ParchmentPanel
            new Color(0.965f, 0.865f, 0.665f, 0.70f),   // ServiceSurface
            new Color(0.922f, 0.871f, 0.765f, 1f)       // ParchmentBackground
        };

        internal static bool IsFactoryFallbackTint(Color color)
        {
            foreach (Color fallback in FactoryFallbackTints)
                if (Mathf.Approximately(color.r, fallback.r) &&
                    Mathf.Approximately(color.g, fallback.g) &&
                    Mathf.Approximately(color.b, fallback.b))
                    return true;
            return false;
        }

        internal static void ApplyButton(Button donor, Button button)
        {
            if (button == null) return;
            if (HasCompleteSpriteStates(donor))
            {
                ApplyImage((Image)donor.targetGraphic, button.image);
                if (IsFactoryFallbackTint(button.image.color))
                    button.image.color = Color.white;
                button.image.raycastTarget = true;
                button.targetGraphic = button.image;
                button.spriteState = donor.spriteState;
                button.transition = Selectable.Transition.SpriteSwap;
            }
            else if (donor != null && donor.targetGraphic is Image)
            {
                Image image = (Image)donor.targetGraphic;
                button.image.sprite = image.sprite;
                button.image.type = image.type;
                if (donor.spriteState.pressedSprite != null)
                {
                    SpriteState sprites = button.spriteState;
                    sprites.pressedSprite = donor.spriteState.pressedSprite;
                    if (donor.spriteState.highlightedSprite != null)
                        sprites.highlightedSprite = donor.spriteState.highlightedSprite;
                    button.spriteState = sprites;
                }
            }
        }

        internal static void ApplyText(Text donor, Text target)
        {
            if (donor == null || target == null) return;
            if (donor.font != null) target.font = donor.font;
            if (donor.material != null) target.material = donor.material;
        }

        internal static void ApplyImage(Image donor, Image target)
        {
            if (donor == null || target == null) return;
            target.sprite = donor.sprite;
            target.material = donor.material;
            target.type = donor.sprite != null && donor.sprite.border.sqrMagnitude > 0
                ? Image.Type.Sliced : donor.type;
            target.preserveAspect = donor.preserveAspect;
            target.fillCenter = donor.fillCenter;
        }

        internal static void PlayClick(object uiCommon)
        {
            UICommon common = uiCommon as UICommon;
            if (common == null || common.UISound == null) return;
            // The single verified native UI click route. Owned planner
            // buttons are plain uGUI Buttons, so this listener is the only
            // click cue they produce — no duplicated ButtonPF sound.
            common.UISound.Play(UISoundType.ButtonClick);
        }
    }
}
