using System;
using System.Linq;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal sealed partial class RuntimeTestHost
    {
        private int _playtestClearStep;
        private bool _playtestClearCaptured;
        private string _playtestClearBefore;
        private float[] _playtestCancelIdle, _playtestClearIdle;
        private readonly JObject _playtestClearPlan = new JObject();
        private int _playtestAvailabilityStep;
        private string _playtestAvailabilityBefore;

        private bool UpdatePlaytestAvailabilityProbe(CastingWorkspaceScreenView view)
        {
            if (_playtestAvailabilityStep >= 5) return true;
            if (view == null) throw new InvalidOperationException("Availability probe has no workspace.");
            if (_playtestAvailabilityStep == 0)
                _playtestAvailabilityBefore = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
            else if (!NativeUiWaited(600)) return false;
            if (_playtestAvailabilityStep == 0 || _playtestAvailabilityStep == 2)
            {
                var session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                string policy = session.AvailabilityPolicy;
                _nativeUi.AvailabilityStates.Add(policy + "|" + view.AvailabilityPolicyCaptionForRuntime);
                NativeUiCapture("nui-playtest-policy-" + policy + ".png");
                _playtestAvailabilityStep++;
                _nativeUiClock.Restart();
                return false;
            }
            if (_playtestAvailabilityStep == 1 || _playtestAvailabilityStep == 3)
            {
                string result = BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("AvailabilityPolicy");
                if (result != "invoked") throw new InvalidOperationException("Availability toggle refused: " + result);
                _playtestAvailabilityStep++; _nativeUiClock.Restart(); return false;
            }
            _nativeUi.AvailabilityIntentRestored =
                _playtestAvailabilityBefore == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
            _playtestAvailabilityStep++;
            return true;
        }

        // Native pointer transitions on the actual modal, with rendered captures.
        // Clear is followed by the production Undo; the authored plan must survive.
        private bool UpdatePlaytestClearPlanProbe(CastingWorkspaceScreenView view)
        {
            if (_playtestClearStep >= 14) return true;
            if (view == null) throw new InvalidOperationException("Clear plan probe has no workspace.");
            if (_playtestClearStep == 0)
            {
                _playtestClearBefore = BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                string result = BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("ClearPlan");
                if (result != "invoked" || !view.ClearPlanOpen)
                    throw new InvalidOperationException("Clear plan probe could not open: " + result);
                _playtestClearStep++; _nativeUiClock.Restart(); return false;
            }
            if (!NativeUiWaited(500)) return false;
            if (_playtestClearStep == 6)
            {
                bool kept = !view.ClearPlanOpen && _playtestClearBefore == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                _playtestClearPlan["cancelKeptPlan"] = kept;
                if (!kept) _nativeUi.Failures.Add("playtest-clear-cancel-changed-plan");
                _playtestClearStep++; _nativeUiClock.Restart(); return false;
            }
            if (_playtestClearStep >= 7) return UpdatePlaytestClearActions(view);
            Button[] buttons = view.ControllerFocusScope.GetComponentsInChildren<Button>(false);
            Button cancel = buttons.Single(button => button.name == "Cancel"), clear = buttons.Single(button => button.name == "Clear");
            float[] cancelTint = HighlightTint(cancel), clearTint = HighlightTint(clear);
            string state = new[] { "", "idle", "cancel-hover", "cancel-exit", "clear-hover", "clear-exit" }[_playtestClearStep];
            if (!_playtestClearCaptured)
            {
                _playtestClearPlan[state] = new JObject
                {
                    { "cancelTint", cancelTint == null ? null : new JArray(cancelTint) },
                    { "clearTint", clearTint == null ? null : new JArray(clearTint) },
                    { "selected", EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null
                        ? "none" : EventSystem.current.currentSelectedGameObject.name }
                };
                NativeUiCapture("nui-playtest-clear-" + state + ".png");
                _playtestClearCaptured = true;
                return false;
            }
            if (_playtestClearStep == 1)
            {
                _playtestCancelIdle = cancelTint; _playtestClearIdle = clearTint;
                Pointer(cancel.gameObject, ExecuteEvents.pointerEnterHandler);
            }
            else if (_playtestClearStep == 2) Pointer(cancel.gameObject, ExecuteEvents.pointerExitHandler);
            else if (_playtestClearStep == 3)
            {
                if (!SamePlaytestTint(_playtestCancelIdle, cancelTint)) _nativeUi.Failures.Add("playtest-cancel-hover-stuck");
                Pointer(clear.gameObject, ExecuteEvents.pointerEnterHandler);
            }
            else if (_playtestClearStep == 4) Pointer(clear.gameObject, ExecuteEvents.pointerExitHandler);
            else
            {
                if (!SamePlaytestTint(_playtestClearIdle, clearTint)) _nativeUi.Failures.Add("playtest-clear-hover-stuck");
                cancel.onClick.Invoke();
            }
            _playtestClearCaptured = false;
            _playtestClearStep++; _nativeUiClock.Restart(); return false;
        }

        private bool UpdatePlaytestClearActions(CastingWorkspaceScreenView view)
        {
            if (_playtestClearStep == 7 || _playtestClearStep == 9)
            {
                if (_playtestClearStep == 9)
                {
                    bool safe = !view.ClearPlanOpen && _playtestClearBefore == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    _playtestClearPlan["semanticConfirmCancelled"] = safe;
                    if (!safe) throw new InvalidOperationException("Semantic Cancel changed the plan.");
                }
                string opened = BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("ClearPlan");
                if (opened != "invoked" || !view.ClearPlanOpen) throw new InvalidOperationException("Clear plan reopen refused: " + opened);
                if (_playtestClearStep == 7)
                {
                    view.ControllerFocusScope.GetComponentsInChildren<Button>(false).Single(button => button.name == "Clear").onClick.Invoke();
                    bool blocked = view.ClearPlanOpen && _playtestClearBefore == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                    _playtestClearPlan["openingEdgeRejected"] = blocked;
                    if (!blocked) throw new InvalidOperationException("Clear accepted its opening input edge.");
                }
            }
            else if (_playtestClearStep == 8)
                view.ControllerCommand(ControllerWorkspaceCommand.Confirm);
            else if (_playtestClearStep == 10)
                view.ControllerFocusScope.GetComponentsInChildren<Button>(false).Single(button => button.name == "Clear").onClick.Invoke();
            else if (_playtestClearStep == 11)
            {
                var session = BuffPlannerUiRoot.CastingWorkspaceSessionForRuntime();
                bool cleared = !view.ClearPlanOpen && session.Document.Castings.Count == 0 && session.CanUndo;
                _playtestClearPlan["clearAppliedOnceUndoAvailable"] = cleared;
                if (!cleared) throw new InvalidOperationException("Clear plan did not clear with Undo available.");
                NativeUiCapture("nui-playtest-clear-applied.png");
            }
            else if (_playtestClearStep == 12)
            {
                string undone = BuffPlannerUiRoot.CastingWorkspaceInvokeControlForRuntime("Undo");
                if (undone != "invoked") throw new InvalidOperationException("Clear plan Undo refused: " + undone);
            }
            else
            {
                bool restored = _playtestClearBefore == BuffPlannerUiRoot.CastingSessionDocumentSignatureForRuntime;
                _playtestClearPlan["undoRestoredIntent"] = restored;
                if (!restored) throw new InvalidOperationException("Clear plan Undo changed authored intent.");
                view.FocusSearchForRuntime(true);
                NativeUiCapture("nui-playtest-clear-undone.png");
            }
            _playtestClearStep++; _nativeUiClock.Restart(); return false;
        }

        private static bool SamePlaytestTint(float[] expected, float[] actual)
        {
            return expected != null && actual != null && expected.Length == actual.Length &&
                expected.Where((value, index) => Math.Abs(value - actual[index]) > .03f).Count() == 0;
        }
    }
}
