using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // The native paper drawn under one owned surface (the workspace frame,
    // the spell scroll). The donor sheet is only read: its sprite and
    // material are shown by two owned Images - a soft shadow silhouette and
    // the sheet - inside a bounds rect that reaches the surface's outsets,
    // each scaled by ParchmentLayerGeometry so the nine-slice borders draw
    // at 0.25 canvas units per sprite pixel. Both reject raycasts and ignore
    // layout; the surface's own Image stays its hit surface. When the donor
    // is missing or rejected, or the canvas cannot be measured, every owned
    // change is undone and the surface shows exactly its previous flat tint
    // and outline again.
    //
    // The layer scale lives on the layers' own transforms: a pass that
    // resets localScale (KingmakerUiFactory.ForceLayoutAndSnap) must never
    // run over a parchment surface; Apply re-asserts the scale.
    internal sealed class ParchmentSurface
    {
        private sealed class Wash
        {
            internal Image Image;
            internal Color Fallback;
            internal float PaperAlpha;
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
        private readonly RectTransform _bounds;
        private readonly Image _hit;
        private readonly Image _shadow;
        private readonly Image _paper;
        private readonly List<Wash> _washes = new List<Wash>();
        private ParchmentLayerGeometry _geometry;
        private string _spriteName;
        private string _fallbackReason = "not-applied";

        private ParchmentSurface(string name, RectTransform surface, ParchmentInsets outsets)
        {
            _name = name;
            _surface = surface;
            _outsets = outsets;
            _ground = surface.GetComponent<Image>();
            _groundFallback = _ground == null ? Color.clear : _ground.color;
            _outline = surface.GetComponent<Outline>();
            _outlineFallback = _outline != null && _outline.enabled;
            _bounds = KingmakerUiFactory.CreateRect("Parchment", surface);
            _bounds.SetAsFirstSibling();
            _bounds.anchorMin = Vector2.zero;
            _bounds.anchorMax = Vector2.one;
            _bounds.pivot = new Vector2(0.5f, 0.5f);
            _bounds.offsetMin = new Vector2(-outsets.Left, -outsets.Bottom);
            _bounds.offsetMax = new Vector2(outsets.Right, outsets.Top);
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

        internal static ParchmentSurface Create(string name, RectTransform surface, ParchmentInsets outsets)
        {
            if (surface == null) throw new ArgumentNullException("surface");
            return new ParchmentSurface(name, surface, outsets);
        }

        internal bool Native { get { return _geometry != null; } }
        internal string Name { get { return _name; } }

        // A flat ground drawn over the paper (a lane's well, the footer
        // ledger) that becomes a light wash while the paper is native and
        // gets its exact previous colour back on fallback.
        internal void AddWash(Image image, float paperAlpha)
        {
            if (image == null) return;
            Outline outline = image.GetComponent<Outline>();
            var wash = new Wash
            {
                Image = image,
                Fallback = image.color,
                PaperAlpha = paperAlpha,
                Outline = outline,
                OutlineFallback = outline != null && outline.enabled
            };
            _washes.Add(wash);
            ApplyWash(wash);
        }

        internal void Apply(Image donor, string unavailableReason)
        {
            if (donor == null || donor.sprite == null)
            {
                Fallback(string.IsNullOrEmpty(unavailableReason) ? "donor unavailable" : unavailableReason);
                return;
            }
            ParchmentLayerGeometry geometry = ParchmentLayerGeometry.For(NativeSpriteContract.ScrollPaper,
                ReferencePixelsPerUnit(_surface));
            if (!geometry.Valid)
            {
                Fallback(geometry.Failure);
                return;
            }
            _geometry = geometry;
            _spriteName = donor.sprite.name;
            Show(_shadow, donor, geometry, new Vector2(ParchmentSurfaces.ShadowOffsetX,
                ParchmentSurfaces.ShadowOffsetY), KingmakerUiFactory.ToColor(PlannerParchmentPalette.Shadow,
                PlannerParchmentPalette.ShadowAlpha));
            Show(_paper, donor, geometry, Vector2.zero, Color.white);
            _hit.raycastTarget = true;
            // The surface keeps its hit area (alpha 0 still raycasts); only
            // the flat tint and the outline drawn for flat panels yield.
            if (_ground != null)
                _ground.color = new Color(_groundFallback.r, _groundFallback.g, _groundFallback.b, 0f);
            if (_outline != null) _outline.enabled = false;
            foreach (Wash wash in _washes) ApplyWash(wash);
        }

        private void Fallback(string reason)
        {
            _geometry = null;
            _spriteName = null;
            _fallbackReason = reason;
            Hide(_shadow);
            Hide(_paper);
            _hit.raycastTarget = false;
            if (_ground != null) _ground.color = _groundFallback;
            if (_outline != null) _outline.enabled = _outlineFallback;
            foreach (Wash wash in _washes) ApplyWash(wash);
        }

        private void ApplyWash(Wash wash)
        {
            if (wash.Image == null) return;
            Color fallback = wash.Fallback;
            wash.Image.color = Native
                ? new Color(fallback.r, fallback.g, fallback.b, wash.PaperAlpha) : fallback;
            if (wash.Outline != null)
                wash.Outline.enabled = ParchmentSurfaces.WashKeepsOutline(Native, wash.PaperAlpha,
                    wash.OutlineFallback);
        }

        // What was drawn, for the theme log and the runtime evidence.
        internal string Evidence
        {
            get
            {
                if (!Native) return _name + ":paper=fallback(" + _fallbackReason + ")";
                Rect rect = _surface.rect;
                return _name + ":paper=native;sprite=" + _spriteName + ";" +
                    _geometry.Describe(rect.width + _outsets.Left + _outsets.Right,
                        rect.height + _outsets.Bottom + _outsets.Top) +
                    ";outsets=" + _outsets.Describe() + ";washes=" + _washes.Count;
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

        private static void Show(Image layer, Image donor, ParchmentLayerGeometry geometry, Vector2 offset,
            Color tint)
        {
            RectTransform rect = layer.rectTransform;
            rect.anchorMin = new Vector2(geometry.AnchorMin, geometry.AnchorMin);
            rect.anchorMax = new Vector2(geometry.AnchorMax, geometry.AnchorMax);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = offset;
            rect.localScale = new Vector3(geometry.Scale, geometry.Scale, 1f);
            layer.sprite = donor.sprite;
            layer.material = donor.material;
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
