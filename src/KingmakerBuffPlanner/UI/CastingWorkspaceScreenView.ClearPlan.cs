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
        private ClearPlanConfirmation _clearConfirmation;
        private readonly WorkspaceModalInputGate _clearInput = new WorkspaceModalInputGate();
        private CanvasGroup _clearBackground;
        private bool _clearPriorInteractable, _clearPriorNavigation;
        private EventSystem _clearEvents;
        private bool _clearRestoreToUndo;
        internal bool ClearPlanOpen { get { return _clearOverlay != null; } }
        internal bool ClearPlanBlocksBackground { get { return _clearInput.BlocksBackground; } }
        internal bool ClearPlanAllowsCommand(ControllerWorkspaceCommand command) { return _clearInput.AllowsCommand(command); }

        private void OpenClearPlan()
        {
            if (_disposed || ClearPlanOpen || _clearInput.BlocksBackground) return;
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
                _clearPriorNavigation = _clearEvents.sendNavigationEvents;
                _clearEvents.sendNavigationEvents = false;
                TMP_InputField field = _clearEvents.currentSelectedGameObject == null ? null :
                    _clearEvents.currentSelectedGameObject.GetComponent<TMP_InputField>();
                if (field != null) field.DeactivateInputField();
                _clearEvents.SetSelectedGameObject(null);
            }
            _clearInput.Open(Time.frameCount);
            if (ControllerEditorInputBoundary.Active != null) ControllerEditorInputBoundary.Active.ResetWorkspaceActivation();
            _clearOverlay = KingmakerUiFactory.CreateRect("ClearPlanConfirmation", _root);
            KingmakerUiFactory.Stretch(_clearOverlay);
            RectTransform blocker = KingmakerUiFactory.CreateRect("Blocker", _clearOverlay);
            KingmakerUiFactory.Stretch(blocker);
            KingmakerUiFactory.AddPanel(blocker, new Color(.08f, .05f, .03f, .58f)).raycastTarget = true;
            // The backdrop intercepts clicks; cancellation is explicit or Back/Escape.
            RectTransform backing = KingmakerUiFactory.CreateRect("OpaqueParchment", _clearOverlay);
            PlaceClearPaper(backing);
            Color parchment = _theme.ParchmentRaised; parchment.a = 1;
            KingmakerUiFactory.AddPanel(backing, parchment).raycastTarget = false;
            RectTransform dialog = KingmakerUiFactory.CreateRect("Dialog", _clearOverlay);
            PlaceClearPaper(dialog);
            Image paper = _kit.Paint(dialog, PlannerNativeKit.PaperSlip, Color.white, true);
            if (paper == null) paper = KingmakerUiFactory.AddFramedPanel(dialog, parchment, _theme.GoldAccent);
            paper.raycastTarget = true;
            _kit.Frame(dialog, PlannerNativeKit.ThinFrame, Color.white);
            TMP_Text title = _kit.Text(dialog, "Title", "Clear entire plan?", PlannerTextRole.Heading, 24);
            KingmakerUiFactory.SetAnchors(title.rectTransform, 0, 1, 1, 1, 24, 24, 0, 0);
            title.rectTransform.pivot = new Vector2(.5f, 1);
            title.rectTransform.sizeDelta = new Vector2(-48, 42);
            title.rectTransform.anchoredPosition = new Vector2(0, -20);
            TMP_Text body = _kit.Text(dialog, "Warning", _clearConfirmation.Message, PlannerTextRole.Body, 20);
            body.enableWordWrapping = true;
            KingmakerUiFactory.Stretch(body.rectTransform, 24, 24, 80, 70);
            Button cancel = _kit.Button(dialog, "Cancel", "Cancel", () =>
            {
                if (_clearInput.Take(Time.frameCount)) CloseClearPlan(false);
            }, PlannerTextRole.Button, 20);
            Button clear = _kit.Button(dialog, "Clear", "Clear plan", ConfirmClearPlan, PlannerTextRole.Button, 20);
            ControllerControlIdentity.Set(cancel, "clear/cancel", "ClearPlan");
            ControllerControlIdentity.Set(clear, "clear/confirm", "ClearPlan");
            KingmakerUiFactory.SetAnchors(RectOf(cancel), .05f, 0, .48f, 0, 0, 0, 0, 0);
            KingmakerUiFactory.SetAnchors(RectOf(clear), .52f, 0, .95f, 0, 0, 0, 0, 0);
            foreach (Button button in new[] { cancel, clear })
            {
                RectOf(button).pivot = new Vector2(.5f, 0);
                RectOf(button).sizeDelta = new Vector2(RectOf(button).sizeDelta.x, 44);
                RectOf(button).anchoredPosition = new Vector2(0, 22);
            }
            // A strong caption and explicit action distinguish the destructive choice.
            TMP_Text destructive = clear.GetComponentInChildren<TMP_Text>();
            if (destructive != null) { destructive.fontStyle |= FontStyles.Bold; destructive.color = Burgundy; }
            TMP_Text warning = _kit.Text(clear.transform, "WarningMark", "!", PlannerTextRole.Strong, 24, TextAlignmentOptions.Center);
            KingmakerUiFactory.SetAnchors(warning.rectTransform, 0, 0, 0, 1);
            warning.rectTransform.pivot = new Vector2(0, .5f);
            warning.rectTransform.sizeDelta = new Vector2(32, 0);
            warning.rectTransform.anchoredPosition = new Vector2(8, 0);
            if (_nativeTheme != null) _nativeTheme.ApplyTo(_clearOverlay);
            KingmakerUiFactory.OwnPointerHighlights(_clearOverlay);
            PropagateUiLayer();
            _clearOverlay.SetAsLastSibling();
            if (_controllerNavigation == null) _controllerNavigation = new ControllerWorkspaceNavigator(this, _kit, true);
            _controllerNavigation.Rebuild();
            _controllerNavigation.RestoreFocus("clear/cancel");
            if (_clearEvents != null) _clearEvents.SetSelectedGameObject(cancel.gameObject);
        }

        private void ConfirmClearPlan()
        {
            if (_disposed || !ClearPlanOpen || !_clearInput.Take(Time.frameCount)) return;
            AuthoringEditResult result = _session.ClearPlan(_clearConfirmation, _inputs());
            CloseClearPlan(result.Applied);
            Feedback(result.Applied ? "Plan cleared. Undo restores it." : result.Reason, !result.Applied);
            RefreshView();
            RestoreClearFocus(result.Applied);
        }

        private void PlaceClearPaper(RectTransform rect)
        {
            KingmakerUiFactory.SetAnchors(rect, .5f, .5f, .5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(Math.Min(640, _root.rect.width - 64), 320);
        }

        private void CloseClearPlan(bool cleared)
        {
            if (!ClearPlanOpen) return;
            _session.CancelClearPlan();
            _clearConfirmation = null;
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
            if (_clearEvents != null && target != null) _clearEvents.SetSelectedGameObject(target.gameObject);
        }

        private void RestoreClearInputOwnership()
        {
            if (_clearBackground != null) _clearBackground.interactable = _clearPriorInteractable;
            _clearBackground = null;
            if (_clearEvents != null) _clearEvents.sendNavigationEvents = _clearPriorNavigation;
            _clearEvents = null;
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
            if (_disposed || _controllerAuthoring || _clearInput.BlocksBackground || _controllerNavigation == null) return;
            TMP_InputField field = EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null ? null :
                EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            bool tab = Input.GetKeyDown(KeyCode.Tab);
            if (field != null && field.isFocused)
            {
                if (!tab) return;
                field.DeactivateInputField();
            }
            if (tab) _controllerNavigation.CycleKeyboardFocus(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1);
            if (!_controllerNavigation.KeyboardFocusActive) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Left);
            if (Input.GetKeyDown(KeyCode.RightArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Right);
            if (Input.GetKeyDown(KeyCode.UpArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Up);
            if (Input.GetKeyDown(KeyCode.DownArrow)) _controllerNavigation.Execute(ControllerWorkspaceCommand.Down);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                _controllerNavigation.Execute(ControllerWorkspaceCommand.Confirm);
        }
    }
}
