using System;
using KingmakerBuffPlanner.Planning;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal sealed partial class CastingWorkspaceScreenView
    {
        private Button _clearPlanButton;
        private RectTransform _clearOverlay;
        // The dialog's native DialogMessageBox scroll paper and its scroll
        // rule, registered with the theme only while the dialog exists.
        private ParchmentSurface _clearParchment;
        private ParchmentRule _clearRule;
        private ClearPlanConfirmation _clearConfirmation;
        private readonly WorkspaceModalInputGate _clearInput = new WorkspaceModalInputGate();
        private CanvasGroup _clearBackground;
        private bool _clearPriorInteractable;
        private EventSystem _clearEvents;
        private bool _clearRestoreToUndo;
        private readonly WorkspaceKeyboardInput _workspaceKeyboardInput = new WorkspaceKeyboardInput();
        private readonly WorkspaceModalInputGate _descriptionInput = new WorkspaceModalInputGate();

        internal bool NativeDescriptionBlocksWorkspace
        {
            get
            {
                int frame = _workspaceKeyboardInput.Frame();
                bool open = _workspaceKeyboardInput.DescriptionOpen();
                if (open && !_descriptionInput.IsOpen) _descriptionInput.Open(frame);
                if (!open && _descriptionInput.IsOpen) _descriptionInput.Close(frame);
                _descriptionInput.Observe(_workspaceKeyboardInput.Neutral(), frame);
                return _descriptionInput.BlocksBackground;
            }
        }

        private void NoteNativeDescriptionOpened()
        {
            _descriptionInput.Open(_workspaceKeyboardInput.Frame());
        }
        internal bool ClearPlanOpen { get { return _clearOverlay != null; } }
        internal bool ClearPlanBlocksBackground { get { return _clearInput.BlocksBackground; } }
        internal bool ClearPlanAllowsCommand(ControllerWorkspaceCommand command) { return _clearInput.AllowsCommand(command); }

        private void OpenClearPlan()
        {
            if (_disposed || ClearPlanOpen || _clearInput.BlocksBackground || NativeDescriptionBlocksWorkspace) return;
            _clearConfirmation = _session.BeginClearPlan(_inputs());
            if (_clearConfirmation == null)
            { Feedback(_session.ClearPlanUnavailableReason, true); return; }
            CloseResources();
            NativeDescriptionPresenter.CloseIfOpen();
            _descriptionShownLastFrame = false;
            // A queued pointer/Submit callback cannot activate background controls.
            _clearBackground = _frame.GetComponent<CanvasGroup>() ?? _frame.gameObject.AddComponent<CanvasGroup>();
            _clearPriorInteractable = _clearBackground.interactable;
            _clearBackground.interactable = false;
            _clearEvents = EventSystem.current;
            if (_clearEvents != null)
            {
                // ControllerEditorInputBoundary or the keyboard navigator
                // owns navigation for this workspace's entire lifetime.
                // The modal borrows selection only, never that owner's state.
                TMP_InputField field = _clearEvents.currentSelectedGameObject == null ? null :
                    _clearEvents.currentSelectedGameObject.GetComponent<TMP_InputField>();
                if (field != null) field.DeactivateInputField();
                _clearEvents.SetSelectedGameObject(null);
            }
            _clearInput.Open(Time.frameCount);
            if (ControllerEditorInputBoundary.Active != null) ControllerEditorInputBoundary.Active.ResetWorkspaceActivation();
            DismissTooltip();
            _clearOverlay = KingmakerUiFactory.CreateRect("ClearPlanConfirmation", _root);
            KingmakerUiFactory.Stretch(_clearOverlay);
            RectTransform blocker = KingmakerUiFactory.CreateRect("Blocker", _clearOverlay);
            KingmakerUiFactory.Stretch(blocker);
            KingmakerUiFactory.AddPanel(blocker, new Color(.08f, .05f, .03f, .58f)).raycastTarget = true;
            // The backdrop intercepts clicks; cancellation is explicit or Back/Escape.
            BuildClearDialog(_clearConfirmation.Message, _clearConfirmation.CastingCount);
            if (_nativeTheme != null) _nativeTheme.ApplyTo(_clearOverlay);
            KingmakerUiFactory.OwnPointerHighlights(_clearOverlay);
            PropagateUiLayer();
            _clearOverlay.SetAsLastSibling();
            if (_controllerNavigation == null) _controllerNavigation = new ControllerWorkspaceNavigator(this, _kit, true);
            _controllerNavigation.Rebuild();
            _controllerNavigation.RestoreFocus("clear/cancel");
            // Semantic navigation retains Cancel as its safe destination.
            // Unity selection also forces Highlighted after pointer exit.
            // These pointer controls must stay unselected.
        }

        private void ConfirmClearPlan()
        {
            if (_disposed || !ClearPlanOpen || NativeDescriptionBlocksWorkspace || !_clearInput.Take(Time.frameCount)) return;
            AuthoringEditResult result = _session.ClearPlan(_clearConfirmation, _inputs());
            CloseClearPlan(result.Applied);
            ReportClearOutcome(result);
            RefreshView();
            RestoreClearFocus(result.Applied);
        }

        // Review P3: Clear is a command like any other (it does not go
        // through Command()), so an applied Clear retires the previous
        // command's problem - a refusal about castings that no longer exist -
        // instead of leaving it as the standing notice. A current save
        // failure is a separate standing source and stays shown. A refused
        // Clear reports its own problem.
        private void ReportClearOutcome(AuthoringEditResult result)
        {
            if (result.Applied)
            {
                _feedbackShown = false;
                _commandProblem = null;
                _loadNote = null;
            }
            Feedback(result.Applied ? "Plan cleared. Undo restores it." : result.Reason, !result.Applied);
            if (result.Applied) ShowStandingNotice(WorkspaceFooterText.SaveProblem(_session.FooterSaveState));
        }

        // Owner request after 0.6.11: a genuine native scroll, not a tan box.
        // The dialog draws the game's own DialogMessageBox paper
        // (dialogue_backsheet) through the same ParchmentSurface as the
        // workspace sheet - no opaque rectangle under it to defeat the
        // scroll's silhouette; the flat framed tint is only its exact
        // fallback when the native paper is unavailable. Native title face
        // (ornamental initial) centred, the native scroll rule under it,
        // native body text, ordinary native buttons, and a height that
        // follows the content.
        internal const float ClearDialogMaximumWidth = 640f;
        private const float ClearSidePadding = 56f;
        private const float ClearTopPadding = 34f;
        private const float ClearBottomPadding = 30f;
        private const float ClearTitleHeight = 40f;
        private const float ClearRuleGap = 6f;
        private const float ClearRuleHeight = 11f;
        private const float ClearBodyGap = 14f;
        private const float ClearActionsGap = 26f;
        private const float ClearButtonHeight = 46f;
        private const float ClearButtonWidth = 200f;
        private const float ClearButtonGap = 28f;
        private static readonly ParchmentInsets ClearPaperOutsets = new ParchmentInsets(18f, 16f, 18f, 16f);

        private void BuildClearDialog(string message, int castingCount)
        {
            RectTransform dialog = KingmakerUiFactory.CreateRect("Dialog", _clearOverlay);
            KingmakerUiFactory.SetAnchors(dialog, .5f, .5f, .5f, .5f);
            dialog.pivot = new Vector2(.5f, .5f);
            float width = Mathf.Min(ClearDialogMaximumWidth, Mathf.Max(360f, _root.rect.width - 96f));
            // Exact fallback ground (yielded while the native paper draws).
            Color fallback = _theme.ParchmentRaised; fallback.a = 1;
            KingmakerUiFactory.AddFramedPanel(dialog, fallback, _theme.GoldAccent).raycastTarget = true;
            TMP_Text title = _kit.Text(dialog, "Title", "Clear entire plan?", PlannerTextRole.Title, 26f,
                TextAlignmentOptions.Center);
            title.enableWordWrapping = false;
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0f, 1f, 1f, 1f, ClearSidePadding, ClearSidePadding, 0f, 0f);
            title.rectTransform.pivot = new Vector2(.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(-2f * ClearSidePadding, ClearTitleHeight);
            title.rectTransform.anchoredPosition = new Vector2(0f, -ClearTopPadding);
            TMP_Text body = _kit.Text(dialog, "Warning", message, PlannerTextRole.Body, 19f,
                TextAlignmentOptions.Top);
            body.enableWordWrapping = true;
            float textWidth = width - 2f * ClearSidePadding;
            float bodyHeight = Mathf.Ceil(body.GetPreferredValues(message, textWidth, 0f).y) + 4f;
            float bodyTop = ClearTopPadding + ClearTitleHeight + ClearRuleGap + ClearRuleHeight + ClearBodyGap;
            KingmakerUiFactory.SetAnchors(body.rectTransform, 0f, 1f, 1f, 1f, ClearSidePadding, ClearSidePadding, 0f, 0f);
            body.rectTransform.pivot = new Vector2(.5f, 1f);
            body.rectTransform.sizeDelta = new Vector2(-2f * ClearSidePadding, bodyHeight);
            body.rectTransform.anchoredPosition = new Vector2(0f, -bodyTop);
            float height = bodyTop + bodyHeight + ClearActionsGap + ClearButtonHeight + ClearBottomPadding;
            // Content-appropriate, never taller than the screen allows.
            height = Mathf.Min(height, Mathf.Max(200f, _root.rect.height - 64f));
            dialog.sizeDelta = new Vector2(width, height);
            Button cancel = _kit.Button(dialog, "Cancel", "Cancel", () =>
            {
                if (_clearInput.Take(Time.frameCount)) CloseClearPlan(false);
            }, PlannerTextRole.Button, 18f);
            Button clear = _kit.Button(dialog, "Clear", "Clear plan", ConfirmClearPlan, PlannerTextRole.Button, 18f);
            ControllerControlIdentity.Set(cancel, "clear/cancel", "ClearPlan");
            ControllerControlIdentity.Set(clear, "clear/confirm", "ClearPlan");
            foreach (Button button in new[] { cancel, clear })
            {
                RectTransform rect = RectOf(button);
                KingmakerUiFactory.SetAnchors(rect, .5f, 0f, .5f, 0f);
                rect.pivot = new Vector2(button == cancel ? 1f : 0f, 0f);
                rect.sizeDelta = new Vector2(ClearButtonWidth, ClearButtonHeight);
                rect.anchoredPosition = new Vector2(button == cancel ? -ClearButtonGap / 2f : ClearButtonGap / 2f,
                    ClearBottomPadding);
            }
            RegisterTooltip(cancel, "clear/cancel", "ClearPlan", WorkspaceFooterTooltips.ClearCancel, true);
            RegisterTooltip(clear, "clear/confirm", "ClearPlan", () => WorkspaceFooterTooltips.ClearConfirm(castingCount), true);
            _clearParchment = ParchmentSurface.Create("ClearPlanDialog", dialog, ClearPaperOutsets, ClearPaperOutsets,
                ParchmentSurfaces.FrameSheetUnitsPerTexel, true);
            _clearRule = ParchmentRule.Create("TitleRule", dialog, _clearParchment, false);
            KingmakerUiFactory.SetAnchors(_clearRule.Rect, .5f, 1f, .5f, 1f);
            _clearRule.Rect.sizeDelta = new Vector2(Mathf.Min(320f, textWidth), ClearRuleHeight);
            _clearRule.Rect.anchoredPosition = new Vector2(0f, -(ClearTopPadding + ClearTitleHeight + ClearRuleGap +
                ClearRuleHeight / 2f));
            if (_nativeTheme != null)
            {
                _nativeTheme.RegisterParchment(_clearParchment);
                _nativeTheme.RegisterRule(_clearRule);
            }
        }

        internal string ClearDialogPaperEvidenceForRuntime
        {
            get { return _clearParchment == null ? "clear-dialog=closed" : _clearParchment.Evidence; }
        }

        private void CloseClearPlan(bool cleared)
        {
            if (!ClearPlanOpen) return;
            _session.CancelClearPlan();
            _clearConfirmation = null;
            DismissTooltip();
            _tooltipTargets.Remove("clear/cancel");
            _tooltipTargets.Remove("clear/confirm");
            if (_nativeTheme != null)
            {
                _nativeTheme.UnregisterParchment(_clearParchment);
                _nativeTheme.UnregisterRule(_clearRule);
            }
            _clearParchment = null;
            _clearRule = null;
            _clearOverlay.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_clearOverlay.gameObject);
            _clearOverlay = null;
            _clearRestoreToUndo = cleared;
            _clearInput.Close(Time.frameCount);
            if (ControllerEditorInputBoundary.Active != null) ControllerEditorInputBoundary.Active.ResetWorkspaceActivation();
            if (_controllerNavigation != null) _controllerNavigation.Rebuild();
            RestoreClearFocus(cleared);
        }

        private void RestoreClearFocus(bool cleared)
        {
            Button target = cleared && _session.CanUndo ? _undoButton : _clearPlanButton;
            if (target == null || !target.interactable) target = _undoButton != null && _session.CanUndo ? _undoButton : _resourcesButton;
            string key = target == _undoButton ? "plan/undo" : target == _clearPlanButton ? "plan/clear" : null;
            if (_controllerNavigation != null) _controllerNavigation.RestoreFocus(key);
            if (_clearEvents != null) _clearEvents.SetSelectedGameObject(null);
        }

        private void RestoreClearInputOwnership()
        {
            if (_clearBackground != null) RestoreClearBackground();
            _clearBackground = null;
            _clearEvents = null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void RestoreClearBackground()
        {
            // Keep this Unity-native call out of managed lifecycle cleanup.
            _clearBackground.interactable = _clearPriorInteractable;
        }

        private void TickClearPlan()
        {
            if (_disposed) return;
            if (ClearPlanOpen && !_session.IsClearPlanCurrent(_clearConfirmation))
            {
                CloseClearPlan(false);
                Feedback("The plan changed. Open Clear plan again to review it.", true);
            }
            bool neutral = !Input.anyKey &&
                (!_controllerAuthoring || ControllerRoutineMenu.ControlsAreNeutral());
            _clearInput.Observe(neutral, Time.frameCount);
            if (!ClearPlanOpen)
            {
                if (!_clearInput.BlocksBackground && _clearBackground != null)
                {
                    RestoreClearInputOwnership();
                    if (_controllerNavigation != null) _controllerNavigation.Rebuild();
                    RestoreClearFocus(_clearRestoreToUndo);
                }
                return;
            }
            if (NativeDescriptionBlocksWorkspace) return;
            if (Input.GetKeyDown(KeyCode.Tab))
                _controllerNavigation.CycleKeyboardFocus(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Left);
            if (Input.GetKeyDown(KeyCode.RightArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Right);
            if (Input.GetKeyDown(KeyCode.UpArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Up);
            if (Input.GetKeyDown(KeyCode.DownArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Down);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                _controllerNavigation.Execute(ControllerWorkspaceCommand.Confirm);
        }

        private void TickWorkspaceKeyboard()
        {
            if (_disposed || _controllerAuthoring || _clearInput.BlocksBackground || _controllerNavigation == null ||
                NativeDescriptionBlocksWorkspace) return;
            TMP_InputField field = EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null ? null :
                EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            bool tab = _workspaceKeyboardInput.KeyDown(KeyCode.Tab);
            if (field != null && field.isFocused)
            {
                if (!tab) return;
                field.DeactivateInputField();
            }
            if (tab) _controllerNavigation.CycleKeyboardFocus(_workspaceKeyboardInput.KeyHeld(KeyCode.LeftShift) || _workspaceKeyboardInput.KeyHeld(KeyCode.RightShift) ? -1 : 1);
            if (!_controllerNavigation.KeyboardFocusActive) return;
            if (_workspaceKeyboardInput.KeyDown(KeyCode.LeftArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Left);
            if (_workspaceKeyboardInput.KeyDown(KeyCode.RightArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Right);
            if (_workspaceKeyboardInput.KeyDown(KeyCode.UpArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Up);
            if (_workspaceKeyboardInput.KeyDown(KeyCode.DownArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Down);
            if (_workspaceKeyboardInput.KeyDown(KeyCode.Return) || _workspaceKeyboardInput.KeyDown(KeyCode.KeypadEnter) || _workspaceKeyboardInput.KeyDown(KeyCode.Space))
                _controllerNavigation.Execute(ControllerWorkspaceCommand.Confirm);
        }
    }
}
