using System;
using System.Linq;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public sealed class ClearPlanConfirmation
    {
        internal readonly CastingAuthoringService Authoring;
        internal readonly int Revision;

        internal ClearPlanConfirmation(CastingAuthoringService authoring)
        {
            Authoring = authoring;
            Revision = authoring.CurrentRevision;
            CastingCount = authoring.Document.Castings.Count;
        }

        public int CastingCount { get; private set; }
        public string Message
        {
            get
            {
                return "This removes " + (CastingCount == 1 ? "the 1 planned casting" :
                    "all " + CastingCount + " planned castings") +
                    " from every routine. Active buffs will stay in place. You can restore the plan with Undo.";
            }
        }
    }

    public sealed partial class CastingWorkspaceSession
    {
        private ClearPlanConfirmation _pendingClearPlan;
        private readonly Func<bool> _isRunActive;

        public string ClearPlanUnavailableReason
        {
            get
            {
                if (_submissionInFlight || (_isRunActive != null && _isRunActive()) ||
                    (_lastInputs != null && _lastInputs.RunActive))
                    return "Stop the active routine before editing the plan.";
                return Document.Castings.Count == 0 ? "The entire plan is already empty." : null;
            }
        }

        public bool CanClearPlan { get { return ClearPlanUnavailableReason == null; } }

        public ClearPlanConfirmation BeginClearPlan(CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            if (!CanClearPlan) return null;
            return _pendingClearPlan ?? (_pendingClearPlan = new ClearPlanConfirmation(_authoring));
        }

        public bool IsClearPlanCurrent(ClearPlanConfirmation confirmation)
        {
            return confirmation != null && ReferenceEquals(confirmation, _pendingClearPlan) &&
                ReferenceEquals(confirmation.Authoring, _authoring) &&
                confirmation.Revision == _authoring.CurrentRevision;
        }

        public void CancelClearPlan() { _pendingClearPlan = null; }

        public AuthoringEditResult ClearPlan(ClearPlanConfirmation confirmation,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            if (!IsClearPlanCurrent(confirmation))
                return AuthoringEditResult.Refuse("The plan changed. Open Clear plan again to review it.");
            CancelClearPlan();
            if (!CanClearPlan) return AuthoringEditResult.Refuse(ClearPlanUnavailableReason);
            AuthoringEditResult result = _authoring.RemoveCastings(
                Document.Castings.Select(value => value.CastingId).ToArray(), "clear-entire-plan");
            if (result.Applied)
            {
                ClearGraphFocus();
                // Review P3: a refused attempt's blockers named castings that
                // no longer exist; its navigation/message must not outlive them.
                LeaveProblemNavigation();
                _clearedAuthoring = _authoring;
                _clearedRevision = _authoring.CurrentRevision;
                _clearedCount = confirmation.CastingCount;
            }
            return result;
        }

        // The castings the NEXT Undo restores when that Undo reverts a Clear
        // plan (0 otherwise): the Undo tooltip names the real pending action.
        private CastingAuthoringService _clearedAuthoring;
        private int _clearedRevision = -1;
        private int _clearedCount;

        public int UndoRestoresClearedCount
        {
            get
            {
                return CanUndo && ReferenceEquals(_clearedAuthoring, _authoring) &&
                    _clearedRevision == _authoring.CurrentRevision ? _clearedCount : 0;
            }
        }
    }

    // A fresh neutral frame separates modal ownership transitions from activation.
    // Reuse the editor's edge latch without its native handoff behavior.
    internal sealed class WorkspaceModalInputGate
    {
        private readonly ControllerActivationLatch _latch = new ControllerActivationLatch();
        private int _transitionFrame;
        internal bool IsOpen { get; private set; }
        internal bool BlocksBackground { get; private set; }
        internal void Open(int frame)
        { IsOpen = BlocksBackground = true; _transitionFrame = frame; _latch.Reset(); }
        internal void Close(int frame)
        { IsOpen = false; BlocksBackground = true; _transitionFrame = frame; _latch.Reset(); }
        internal void Observe(bool neutral, int frame)
        {
            if (frame <= _transitionFrame || !BlocksBackground) return;
            _latch.Observe(neutral, frame);
            if (!IsOpen && _latch.Ready(frame)) BlocksBackground = false;
        }
        internal bool Take(int frame) { return IsOpen && _latch.Take(frame); }
        internal bool AllowsCommand(ControllerWorkspaceCommand command)
        {
            if (!IsOpen) return !BlocksBackground;
            return command == ControllerWorkspaceCommand.Confirm || command == ControllerWorkspaceCommand.Up ||
                command == ControllerWorkspaceCommand.Down || command == ControllerWorkspaceCommand.Left ||
                command == ControllerWorkspaceCommand.Right;
        }
    }
}
