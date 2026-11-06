using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    // The workspace's one tooltip (owner request after 0.6.11): the
    // project's PlannerHoverTooltip reports pointer hover, the existing
    // keyboard/controller navigator reports focus, WorkspaceTooltipState
    // decides which owner shows, and this presenter draws one native paper
    // slip above (or below) that control, clamped to the screen. It never
    // takes input: every graphic rejects raycasts and its group blocks none.
    internal sealed partial class CastingWorkspaceScreenView
    {
        private sealed class TooltipTarget
        {
            internal RectTransform Rect;
            internal Func<WorkspaceTooltipContent> Content;
            internal bool Modal;
        }

        private const float TooltipWidth = 400f;
        private const float TooltipPadding = 14f;
        private const float TooltipGap = 8f;
        private const float TooltipScreenMargin = 8f;

        // Lazy: detached view fixtures construct the view without field
        // initializers; the tooltip must never be what fails there.
        private WorkspaceTooltipState _tooltipStateValue;
        private Dictionary<string, TooltipTarget> _tooltipTargetsValue;

        private WorkspaceTooltipState _tooltipState
        {
            get { return _tooltipStateValue ?? (_tooltipStateValue = new WorkspaceTooltipState()); }
        }

        private Dictionary<string, TooltipTarget> _tooltipTargets
        {
            get
            {
                return _tooltipTargetsValue ?? (_tooltipTargetsValue =
                    new Dictionary<string, TooltipTarget>(StringComparer.Ordinal));
            }
        }
        private RectTransform _tooltipPanel;
        private TMP_Text _tooltipTitle;
        private TMP_Text _tooltipBody;

        internal string TooltipKeyForRuntime { get { return _tooltipState.Key; } }
        internal string TooltipSourceForRuntime { get { return _tooltipState.Source.ToString(); } }
        internal string TooltipTextForRuntime
        {
            get
            {
                if (!_tooltipState.Visible || _tooltipPanel == null || !_tooltipPanel.gameObject.activeSelf) return null;
                return _tooltipTitle.text.Length == 0 ? _tooltipBody.text : _tooltipTitle.text + "\n" + _tooltipBody.text;
            }
        }

        internal string TooltipFor(string key)
        {
            TooltipTarget target;
            return _tooltipTargets.TryGetValue(key ?? string.Empty, out target) && target.Content != null
                ? target.Content().Text : null;
        }

        // region: the navigator region for a control without an identity
        // yet (null: no focus identity, hover only). The identity's
        // Explanation is the same live text, so a controller's Inspect reads
        // exactly what the tooltip says - for disabled controls too.
        private void RegisterTooltip(Component control, string key, string region,
            Func<WorkspaceTooltipContent> content, bool modal = false)
        {
            if (control == null || string.IsNullOrEmpty(key) || content == null) return;
            _tooltipTargets[key] = new TooltipTarget { Rect = control.transform as RectTransform, Content = content, Modal = modal };
            PlannerHoverTooltip hover = control.GetComponent<PlannerHoverTooltip>() ??
                control.gameObject.AddComponent<PlannerHoverTooltip>();
            hover.Key = key;
            hover.Hover = OnTooltipHover;
            if (region == null) return;
            ControllerControlIdentity identity = control.GetComponent<ControllerControlIdentity>();
            if (identity == null)
            {
                ControllerControlIdentity.Set(control, key, region);
                identity = control.GetComponent<ControllerControlIdentity>();
            }
            identity.Explanation = () => content().Body;
        }

        private void OnTooltipHover(string key, bool entered)
        {
            if (_disposed) return;
            if (entered) _tooltipState.PointerEnter(key);
            else _tooltipState.PointerExit(key);
            RetainTooltip();
        }

        // Called every frame: keyboard/controller focus is followed only
        // while it is actually in use; hover is separate.
        private void TickTooltip()
        {
            if (_disposed) return;
            string focus = null;
            // Review P2: while the navigator's own dialog (Control details,
            // Unavailable, search keyboard) owns input, its retained focus key
            // belongs to the control BEHIND it: no background owner shows.
            if (_controllerNavigation != null && !NavigatorModalOpen &&
                (_controllerAuthoring || _controllerNavigation.KeyboardFocusActive))
            {
                focus = _controllerNavigation.FocusKey;
                if (focus == null || !_tooltipTargets.ContainsKey(focus)) focus = null;
            }
            _tooltipState.FocusChanged(focus);
            RetainTooltip();
        }

        // Modal transitions, Resources, reload and close: nothing stays shown.
        private void DismissTooltip()
        {
            _tooltipState.Dismiss();
            RenderTooltip();
        }

        // An owner whose control was destroyed, hidden or is behind the open
        // modal is forgotten; then draw the current owner's live text.
        private void RetainTooltip()
        {
            _tooltipState.Retain(TooltipTargetUsable);
            RenderTooltip();
        }

        // The navigator's own modal (a Control details / Unavailable reader or
        // the search keyboard) - built above every workspace layer, including
        // the Clear dialog.
        internal bool NavigatorModalOpen
        {
            get { return _controllerNavigation != null && _controllerNavigation.ModalOpen; }
        }

        // The navigator opened its dialog: drop the background owner now,
        // not one frame later, so nothing is raised over the dialog.
        internal void NavigatorModalOpened()
        {
            if (!_disposed) DismissTooltip();
        }

        // Test seam for detached fixtures (no native runtime): whether the
        // control still exists and is shown. Production uses the native read.
#pragma warning disable 0649 // assigned only by detached test fixtures
        private Func<RectTransform, bool> _tooltipControlLive;
#pragma warning restore 0649

        private bool TooltipTargetUsable(string key)
        {
            TooltipTarget target;
            if (!_tooltipTargets.TryGetValue(key, out target) || ReferenceEquals(target.Rect, null) ||
                !(_tooltipControlLive ?? NativeControlLive)(target.Rect)) return false;
            return TooltipOwnerAllows(key, target);
        }

        // Which layer owns presentation, topmost first: the navigator's
        // dialog suppresses everything; the Clear dialog shows only its own
        // buttons' help; Resources suppresses the footer; otherwise the
        // workspace's controls (the notice only while it says something).
        private bool TooltipOwnerAllows(string key, TooltipTarget target)
        {
            if (NavigatorModalOpen) return false;
            if (ClearPlanOpen) return target.Modal;
            if (target.Modal || (!ReferenceEquals(_resources, null) && ResourcesShown())) return false;
            return key != "notice" || (_noticeText ?? string.Empty).Length != 0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool NativeControlLive(RectTransform rect)
        {
            return rect != null && rect.gameObject.activeInHierarchy;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private bool ResourcesShown()
        {
            return _resources != null && _resources.gameObject.activeSelf;
        }

        // Reference checks only: no Unity method is even compiled into this
        // frame before the view exists (detached fixtures drive commands
        // without a native runtime).
        private void RenderTooltip()
        {
            if (_disposed || ReferenceEquals(_root, null)) return;
            DrawTooltip();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void DrawTooltip()
        {
            TooltipTarget target;
            // Never raised over a dialog that does not own this tooltip.
            if (!_tooltipState.Visible || !_tooltipTargets.TryGetValue(_tooltipState.Key, out target) ||
                target.Rect == null || !TooltipOwnerAllows(_tooltipState.Key, target))
            {
                if (_tooltipPanel != null && _tooltipPanel.gameObject.activeSelf) _tooltipPanel.gameObject.SetActive(false);
                return;
            }
            WorkspaceTooltipContent content = target.Content();
            if (content == null || (content.Title.Length == 0 && content.Body.Length == 0))
            {
                if (_tooltipPanel != null) _tooltipPanel.gameObject.SetActive(false);
                return;
            }
            EnsureTooltipPanel();
            float width = Mathf.Min(TooltipWidth, Mathf.Max(160f, _root.rect.width - 2f * TooltipScreenMargin));
            float inner = width - 2f * TooltipPadding;
            if (!string.Equals(_tooltipTitle.text, content.Title, StringComparison.Ordinal)) _tooltipTitle.text = content.Title;
            if (!string.Equals(_tooltipBody.text, content.Body, StringComparison.Ordinal)) _tooltipBody.text = content.Body;
            _tooltipTitle.gameObject.SetActive(content.Title.Length != 0);
            float titleHeight = content.Title.Length == 0 ? 0f : _tooltipTitle.GetPreferredValues(content.Title, inner, 0f).y + 4f;
            float bodyHeight = _tooltipBody.GetPreferredValues(content.Body, inner, 0f).y;
            float height = TooltipPadding * 2f + titleHeight + bodyHeight;
            KingmakerUiFactory.SetAnchors(_tooltipTitle.rectTransform, 0f, 1f, 1f, 1f, TooltipPadding, TooltipPadding, 0f, 0f);
            _tooltipTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            _tooltipTitle.rectTransform.sizeDelta = new Vector2(-2f * TooltipPadding, titleHeight);
            _tooltipTitle.rectTransform.anchoredPosition = new Vector2(0f, -TooltipPadding);
            KingmakerUiFactory.Stretch(_tooltipBody.rectTransform, TooltipPadding, TooltipPadding,
                TooltipPadding, TooltipPadding + titleHeight);

            // Above the control, else below it; clamped to the screen.
            Rect bounds = _root.rect;
            Vector3[] corners = new Vector3[4];
            target.Rect.GetWorldCorners(corners);
            Vector3 low = _root.InverseTransformPoint(corners[0]);
            Vector3 high = _root.InverseTransformPoint(corners[2]);
            float centre = (low.x + high.x) / 2f;
            float x = Mathf.Clamp(centre, bounds.xMin + TooltipScreenMargin + width / 2f,
                bounds.xMax - TooltipScreenMargin - width / 2f);
            float y = high.y + TooltipGap;
            if (y + height > bounds.yMax - TooltipScreenMargin) y = low.y - TooltipGap - height;
            y = Mathf.Clamp(y, bounds.yMin + TooltipScreenMargin, Mathf.Max(bounds.yMin + TooltipScreenMargin,
                bounds.yMax - TooltipScreenMargin - height));
            _tooltipPanel.anchorMin = _tooltipPanel.anchorMax = _root.pivot;
            _tooltipPanel.pivot = new Vector2(0.5f, 0f);
            _tooltipPanel.sizeDelta = new Vector2(width, height);
            _tooltipPanel.anchoredPosition = new Vector2(x, y);
            if (!_tooltipPanel.gameObject.activeSelf) _tooltipPanel.gameObject.SetActive(true);
            _tooltipPanel.SetAsLastSibling();
        }

        private void EnsureTooltipPanel()
        {
            if (_tooltipPanel != null) return;
            _tooltipPanel = KingmakerUiFactory.CreateRect("WorkspaceTooltip", _root);
            _tooltipPanel.gameObject.layer = _uiLayer;
            CanvasGroup group = _tooltipPanel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            // Opaque native paper so text never reads through to the lanes.
            Color ground = _theme.ParchmentRaised;
            ground.a = 1f;
            KingmakerUiFactory.AddPanel(_tooltipPanel, ground).raycastTarget = false;
            RectTransform paper = KingmakerUiFactory.CreateRect("Paper", _tooltipPanel);
            KingmakerUiFactory.Stretch(paper);
            Image slip = _kit.Paint(paper, PlannerNativeKit.PaperSlip, Color.white, false);
            if (slip != null) slip.raycastTarget = false;
            Image frame = _kit.Frame(_tooltipPanel, PlannerNativeKit.ThinFrame, Color.white);
            if (frame != null) frame.raycastTarget = false;
            _tooltipTitle = _kit.Text(_tooltipPanel, "Title", string.Empty, PlannerTextRole.Strong, 16f,
                TextAlignmentOptions.TopLeft);
            _tooltipTitle.color = PlannerNativeKit.HeadingInk;
            _tooltipBody = _kit.Text(_tooltipPanel, "Body", string.Empty, PlannerTextRole.Body, 15f,
                TextAlignmentOptions.TopLeft);
            _tooltipBody.enableWordWrapping = true;
            foreach (Graphic graphic in _tooltipPanel.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            foreach (Transform node in _tooltipPanel.GetComponentsInChildren<Transform>(true))
                node.gameObject.layer = _uiLayer;
            _tooltipPanel.gameObject.SetActive(false);
        }
    }
}
