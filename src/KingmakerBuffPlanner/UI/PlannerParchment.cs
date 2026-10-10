using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // The native paper drawn under one owned surface (the workspace frame,
    // the spell scroll). 0.4.2: the paper is the native card sheet
    // (Card_Big) when it validates - drawn untinted, as the game draws it,
    // through a planner-owned nine-slice sprite over the same texture
    // (ParchmentSheetGeometry) - else the 0.4.1 scroll sheet
    // (dialogue_backsheet, layer-scaled by ParchmentLayerGeometry), else the
    // surface's previous flat tint and outline exactly. Donors are only
    // read. Two owned Images draw it - a soft shadow silhouette and the
    // sheet - inside a bounds rect that reaches the surface's outsets; both
    // reject raycasts and ignore layout; the surface's own Image stays its
    // hit surface.
    //
    // The scroll sheet's layer scale lives on the layers' own transforms: a
    // pass that resets localScale (KingmakerUiFactory.ForceLayoutAndSnap)
    // must never run over a parchment surface; Apply re-asserts the scale.
    internal sealed class ParchmentSurface
    {
        private enum Paper
        {
            Fallback,
            Scroll,
            Sheet
        }

        private sealed class Wash
        {
            internal Image Image;
            internal Color Fallback;
            internal float PaperAlpha;
            internal float SheetAlpha;
            internal Outline Outline;
            internal bool OutlineFallback;
        }

        private readonly string _name;
        private readonly RectTransform _surface;
        private readonly Image _ground;
        private readonly Color _groundFallback;
        private readonly Outline _outline;
        private readonly bool _outlineFallback;
        private readonly ParchmentInsets _outsets;
        private readonly ParchmentInsets _sheetOutsets;
        private readonly float _sheetUnitsPerTexel;
        private readonly RectTransform _bounds;
        private readonly Image _hit;
        private readonly Image _shadow;
        private readonly Image _paper;
        private readonly List<Wash> _washes = new List<Wash>();
        private Paper _mode = Paper.Fallback;
        private ParchmentLayerGeometry _geometry;
        private ParchmentSheetGeometry _sheetGeometry;
        private ParchmentInsets _drawnOutsets;
        private string _spriteName;
        private string _fallbackReason = "not-applied";

        private ParchmentSurface(string name, RectTransform surface, ParchmentInsets outsets,
            ParchmentInsets sheetOutsets, float sheetUnitsPerTexel)
        {
            _name = name;
            _surface = surface;
            _outsets = outsets;
            _sheetOutsets = sheetOutsets;
            _sheetUnitsPerTexel = sheetUnitsPerTexel;
            _ground = surface.GetComponent<Image>();
            _groundFallback = _ground == null ? Color.clear : _ground.color;
            _outline = surface.GetComponent<Outline>();
            _outlineFallback = _outline != null && _outline.enabled;
            _bounds = KingmakerUiFactory.CreateRect("Parchment", surface);
            _bounds.SetAsFirstSibling();
            _bounds.anchorMin = Vector2.zero;
            _bounds.anchorMax = Vector2.one;
            _bounds.pivot = new Vector2(0.5f, 0.5f);
            SetBounds(outsets);
            _bounds.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            // While the sheet shows, its visible edge beyond the surface is
            // part of the surface: a click there lands on the sheet (and is
            // consumed), never on whatever lies behind it. Without the
            // sheet nothing is drawn there, so nothing is hit there either.
            _hit = _bounds.gameObject.AddComponent<Image>();
            _hit.color = new Color(1f, 1f, 1f, 0f);
            _hit.raycastTarget = false;
            _shadow = Layer("ParchmentShadow");
            _paper = Layer("ParchmentSheet");
        }

        internal static ParchmentSurface Create(string name, RectTransform surface, ParchmentInsets outsets,
            ParchmentInsets sheetOutsets, float sheetUnitsPerTexel)
        {
            if (surface == null) throw new ArgumentNullException("surface");
            return new ParchmentSurface(name, surface, outsets, sheetOutsets, sheetUnitsPerTexel);
        }

        internal bool Native { get { return _mode != Paper.Fallback; } }
        internal bool NativeSheet { get { return _mode == Paper.Sheet; } }
        internal string Name { get { return _name; } }

        // A flat ground drawn over the paper (a lane's well, the footer
        // ledger): a light wash of its own colour on the scroll sheet, a
        // wash at sheetAlpha on the native card sheet (0: the lane is the
        // sheet itself, as in the native screens), and its exact previous
        // colour back on fallback.
        internal void AddWash(Image image, float paperAlpha, float sheetAlpha)
        {
            if (image == null) return;
            Outline outline = image.GetComponent<Outline>();
            var wash = new Wash
            {
                Image = image,
                Fallback = image.color,
                PaperAlpha = paperAlpha,
                SheetAlpha = sheetAlpha,
                Outline = outline,
                OutlineFallback = outline != null && outline.enabled
            };
            _washes.Add(wash);
            ApplyWash(wash);
        }

        internal void Apply(Image sheetDonor, Image scrollDonor, string unavailableReason, float screenHeight)
        {
            float reference = ReferencePixelsPerUnit(_surface);
            string sheetFailure = null;
            if (sheetDonor != null && sheetDonor.sprite != null && sheetDonor.sprite.texture != null)
            {
                ParchmentSheetGeometry sheet = ParchmentSheetGeometry.For(_sheetUnitsPerTexel, screenHeight,
                    reference);
                Sprite sliced = sheet.Valid ? PlannerSheetSprites.For(sheetDonor.sprite, sheet) : null;
                if (sliced != null)
                {
                    ShowSheet(sheetDonor, sliced, sheet);
                    return;
                }
                sheetFailure = sheet.Valid ? "sheet sprite could not be sliced" : sheet.Failure;
            }
            if (scrollDonor == null || scrollDonor.sprite == null)
            {
                Fallback(string.IsNullOrEmpty(unavailableReason) ? "donor unavailable" : unavailableReason);
                return;
            }
            ParchmentLayerGeometry geometry = ParchmentLayerGeometry.For(NativeSpriteContract.ScrollPaper,
                reference);
            if (!geometry.Valid)
            {
                Fallback(geometry.Failure);
                return;
            }
            _mode = Paper.Scroll;
            _geometry = geometry;
            _sheetGeometry = null;
            _spriteName = scrollDonor.sprite.name;
            _fallbackReason = sheetFailure ?? string.Empty;
            SetBounds(_outsets);
            Show(_shadow, scrollDonor.sprite, scrollDonor.material, geometry.AnchorMin, geometry.AnchorMax,
                geometry.Scale, new Vector2(ParchmentSurfaces.ShadowOffsetX, ParchmentSurfaces.ShadowOffsetY),
                KingmakerUiFactory.ToColor(PlannerParchmentPalette.Shadow, PlannerParchmentPalette.ShadowAlpha));
            Show(_paper, scrollDonor.sprite, scrollDonor.material, geometry.AnchorMin, geometry.AnchorMax,
                geometry.Scale, Vector2.zero, Color.white);
            YieldSurface();
        }

        private void ShowSheet(Image donor, Sprite sliced, ParchmentSheetGeometry sheet)
        {
            _mode = Paper.Sheet;
            _geometry = null;
            _sheetGeometry = sheet;
            _spriteName = donor.sprite.name;
            _fallbackReason = string.Empty;
            SetBounds(sheet.Scaled(_sheetOutsets));
            Show(_shadow, sliced, donor.material, 0f, 1f, 1f,
                new Vector2(ParchmentSurfaces.SheetShadowOffsetX * sheet.ScreenScale,
                    ParchmentSurfaces.SheetShadowOffsetY * sheet.ScreenScale),
                KingmakerUiFactory.ToColor(PlannerParchmentPalette.Shadow, PlannerParchmentPalette.SheetShadowAlpha));
            // Untinted, exactly as the game draws its card.
            Show(_paper, sliced, donor.material, 0f, 1f, 1f, Vector2.zero, Color.white);
            YieldSurface();
        }

        // The surface keeps its hit area (alpha 0 still raycasts); only the
        // flat tint and the outline drawn for flat panels yield.
        private void YieldSurface()
        {
            _hit.raycastTarget = true;
            if (_ground != null)
                _ground.color = new Color(_groundFallback.r, _groundFallback.g, _groundFallback.b, 0f);
            if (_outline != null) _outline.enabled = false;
            foreach (Wash wash in _washes) ApplyWash(wash);
        }

        private void Fallback(string reason)
        {
            _mode = Paper.Fallback;
            _geometry = null;
            _sheetGeometry = null;
            _spriteName = null;
            _fallbackReason = reason;
            SetBounds(_outsets);
            Hide(_shadow);
            Hide(_paper);
            _hit.raycastTarget = false;
            if (_ground != null) _ground.color = _groundFallback;
            if (_outline != null) _outline.enabled = _outlineFallback;
            foreach (Wash wash in _washes) ApplyWash(wash);
        }

        private void SetBounds(ParchmentInsets outsets)
        {
            _drawnOutsets = outsets;
            _bounds.offsetMin = new Vector2(-outsets.Left, -outsets.Bottom);
            _bounds.offsetMax = new Vector2(outsets.Right, outsets.Top);
        }

        private void ApplyWash(Wash wash)
        {
            if (wash.Image == null) return;
            Color fallback = wash.Fallback;
            float alpha = _mode == Paper.Sheet ? wash.SheetAlpha : wash.PaperAlpha;
            if (_mode == Paper.Sheet)
                wash.Image.color = KingmakerUiFactory.ToColor(PlannerParchmentPalette.SheetWash, alpha);
            else
                wash.Image.color = Native ? new Color(fallback.r, fallback.g, fallback.b, alpha) : fallback;
            if (wash.Outline != null)
                wash.Outline.enabled = ParchmentSurfaces.WashKeepsOutline(Native, alpha, wash.OutlineFallback);
        }

        // What was drawn, for the theme log and the runtime evidence.
        internal string Evidence
        {
            get
            {
                if (!Native) return _name + ":paper=fallback(" + _fallbackReason + ")";
                Rect rect = _surface.rect;
                float width = rect.width + _drawnOutsets.Left + _drawnOutsets.Right;
                float height = rect.height + _drawnOutsets.Bottom + _drawnOutsets.Top;
                return _name + (_mode == Paper.Sheet
                    ? ":paper=native-sheet;sprite=" + _spriteName + ";" + _sheetGeometry.Describe(width, height)
                    : ":paper=native;sprite=" + _spriteName + ";" + _geometry.Describe(width, height) +
                        (string.IsNullOrEmpty(_fallbackReason) ? string.Empty : ";sheet=" + _fallbackReason)) +
                    ";outsets=" + _drawnOutsets.Describe() + ";washes=" + _washes.Count;
            }
        }

        private Image Layer(string name)
        {
            RectTransform rect = KingmakerUiFactory.CreateRect(name, _bounds);
            Image image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void Show(Image layer, Sprite sprite, Material material, float anchorMin, float anchorMax,
            float scale, Vector2 offset, Color tint)
        {
            RectTransform rect = layer.rectTransform;
            rect.anchorMin = new Vector2(anchorMin, anchorMin);
            rect.anchorMax = new Vector2(anchorMax, anchorMax);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = offset;
            rect.localScale = new Vector3(scale, scale, 1f);
            layer.sprite = sprite;
            layer.material = material;
            layer.type = Image.Type.Sliced;
            layer.fillCenter = true;
            layer.preserveAspect = false;
            layer.color = tint;
            layer.raycastTarget = false;
            layer.enabled = true;
        }

        private static void Hide(Image layer)
        {
            layer.sprite = null;
            layer.enabled = false;
        }

        // The canvas the layer's Image measures its sliced border against
        // (Image.pixelsPerUnit reads the nearest Canvas). Inactive parents
        // are included: the spell scroll is built while hidden.
        internal static float ReferencePixelsPerUnit(Transform transform)
        {
            Canvas[] canvases = transform == null ? null : transform.GetComponentsInParent<Canvas>(true);
            return canvases == null || canvases.Length == 0 ? 0f : canvases[0].referencePixelsPerUnit;
        }
    }

    // 0.4.2: the planner-owned nine-slice sprites over the native card's
    // texture, one per texture and density, kept for the life of the game
    // (a handful of tiny objects; the texture itself stays the game's and is
    // never copied). The native sprite is only read.
    internal static class PlannerSheetSprites
    {
        private const int MaximumEntries = 8;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        internal static Sprite For(Sprite native, ParchmentSheetGeometry geometry)
        {
            if (native == null || native.texture == null || geometry == null || !geometry.Valid) return null;
            Rect rect = native.rect;
            if (rect.width < ParchmentSheetGeometry.SliceLeft + ParchmentSheetGeometry.SliceRight + 2f ||
                rect.height < ParchmentSheetGeometry.SliceBottom + ParchmentSheetGeometry.SliceTop + 2f)
                return null;
            string key = native.texture.GetInstanceID() + "|" +
                geometry.SpritePixelsPerUnit.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            Sprite cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;
            if (Cache.Count >= MaximumEntries)
            {
                foreach (Sprite stale in Cache.Values)
                    if (stale != null) UnityEngine.Object.Destroy(stale);
                Cache.Clear();
            }
            Sprite sliced = Sprite.Create(native.texture, rect, new Vector2(0.5f, 0.5f),
                geometry.SpritePixelsPerUnit, 0, SpriteMeshType.FullRect,
                new Vector4(ParchmentSheetGeometry.SliceLeft, ParchmentSheetGeometry.SliceBottom,
                    ParchmentSheetGeometry.SliceRight, ParchmentSheetGeometry.SliceTop));
            sliced.name = "KBP-sheet:" + native.name;
            Cache[key] = sliced;
            return sliced;
        }
    }

    // 0.4.2: the ground behind the planner. The game lays its service
    // windows on a dark wood table (ServiceWindow/Background); the planner's
    // full-screen blocker shows the same table, untinted and opaque, when
    // that donor validates, so the sheet's worn edges sit on native wood.
    // Otherwise the blocker keeps exactly its previous dimming tint. It
    // stays the planner's full-screen raycast blocker either way.
    internal sealed class ParchmentBackdrop
    {
        private readonly Image _image;
        private readonly Color _fallback;
        private string _evidence = "backdrop=fallback(not-applied)";

        internal ParchmentBackdrop(Image image)
        {
            _image = image ?? throw new ArgumentNullException("image");
            _fallback = image.color;
        }

        internal void Apply(Image donor, string unavailableReason)
        {
            if (donor != null && donor.sprite != null && donor.sprite.texture != null)
            {
                _image.sprite = donor.sprite;
                _image.material = donor.material;
                _image.type = Image.Type.Simple;
                _image.preserveAspect = false;
                _image.color = Color.white;
                _evidence = "backdrop=native;sprite=" + donor.sprite.name;
                return;
            }
            _image.sprite = null;
            _image.material = null;
            _image.color = _fallback;
            _evidence = "backdrop=fallback(" + (string.IsNullOrEmpty(unavailableReason)
                ? "donor unavailable" : unavailableReason) + ")";
        }

        internal string Evidence { get { return _evidence; } }
    }

    // One scroll rule: the native blockscroll ornament when its donor
    // validates, the Teleport modal's hairline otherwise, or nothing (see
    // ParchmentRulePolicy). The caller anchors it horizontally and at a
    // vertical point; the rule owns only its height.
    internal sealed class ParchmentRule
    {
        private readonly string _name;
        private readonly RectTransform _rect;
        private readonly Image _image;
        private readonly ParchmentSurface _paper;
        private readonly bool _structural;
        private readonly Color _hairline;
        private ParchmentRuleMode _mode = ParchmentRuleMode.Hidden;

        private ParchmentRule(string name, RectTransform parent, ParchmentSurface paper, bool structural,
            Color hairline)
        {
            _name = name;
            _paper = paper;
            _structural = structural;
            _hairline = hairline;
            _rect = KingmakerUiFactory.CreateRect(name, parent);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _image = _rect.gameObject.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.enabled = false;
        }

        internal static ParchmentRule Create(string name, RectTransform parent, ParchmentSurface paper,
            bool structuralWithoutPaper)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (paper == null) throw new ArgumentNullException("paper");
            return new ParchmentRule(name, parent, paper, structuralWithoutPaper,
                KingmakerUiFactory.ToColor(PlannerParchmentPalette.HeadingInk, PlannerParchmentPalette.HairlineAlpha));
        }

        internal RectTransform Rect { get { return _rect; } }
        internal ParchmentRuleMode Mode { get { return _mode; } }
        // Why the owning view placed or hid the rule (evidence only).
        internal string Placement { get; set; }

        internal void Apply(Image ornament)
        {
            bool ornamentAvailable = ornament != null && ornament.sprite != null;
            _mode = ParchmentRulePolicy.Resolve(ornamentAvailable, _paper.Native, _structural);
            switch (_mode)
            {
                case ParchmentRuleMode.Ornament:
                    _image.sprite = ornament.sprite;
                    _image.material = ornament.material;
                    _image.type = Image.Type.Sliced;
                    _image.fillCenter = true;
                    _image.color = Color.white;
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ParchmentLayerGeometry.RuleHeight(
                        NativeSpriteContract.ScrollRule, ParchmentSurface.ReferencePixelsPerUnit(_rect)));
                    _image.enabled = true;
                    break;
                case ParchmentRuleMode.Hairline:
                    _image.sprite = null;
                    _image.material = null;
                    _image.type = Image.Type.Simple;
                    _image.color = _hairline;
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ParchmentRulePolicy.HairlineHeight);
                    _image.enabled = true;
                    break;
                default:
                    _image.sprite = null;
                    _image.enabled = false;
                    break;
            }
        }

        internal string Evidence
        {
            get
            {
                return _name + ":rule=" + _mode.ToString().ToLowerInvariant() +
                    (_mode == ParchmentRuleMode.Ornament && _image.sprite != null ? "(" + _image.sprite.name + ")" : string.Empty) +
                    (string.IsNullOrEmpty(Placement) ? string.Empty : ";placement=" + Placement);
            }
        }
    }

    // The dimmed backdrop behind the open spell scroll: the only thing under
    // the pointer outside the scroll. It takes every press, click and wheel
    // there, so none reaches the planner, the graph or the game, and it
    // reports a click (any button, pressed and released on the backdrop) to
    // the scroll's input policy. The scroll is its sibling, never its child,
    // so clicks on the scroll never bubble here.
    internal sealed class SpellScrollBackdrop : MonoBehaviour,
        IPointerDownHandler, IPointerClickHandler, IScrollHandler
    {
        internal Action OutsideClick;
        internal Action Wheel;

        public void OnPointerDown(PointerEventData eventData)
        {
            eventData.Use();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
            if (OutsideClick != null) OutsideClick();
        }

        public void OnScroll(PointerEventData eventData)
        {
            eventData.Use();
            if (Wheel != null) Wheel();
        }
    }
}
