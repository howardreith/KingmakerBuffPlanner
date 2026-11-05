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

        // Native pointer transitions on the actual modal, with rendered captures.
        // Cancel is the only activated action; the authored plan must survive.
        private bool UpdatePlaytestClearPlanProbe(CastingWorkspaceScreenView view)
        {
            if (_playtestClearStep >= 7) return true;
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
                view.FocusSearchForRuntime(true);
                _playtestClearStep++; _nativeUiClock.Restart(); return false;
            }
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

        private static bool SamePlaytestTint(float[] expected, float[] actual)
        {
            return expected != null && actual != null && expected.Length == actual.Length &&
                expected.Where((value, index) => Math.Abs(value - actual[index]) > .03f).Count() == 0;
        }
    }
}
