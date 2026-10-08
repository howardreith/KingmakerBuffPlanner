using System;
using System.Collections.Generic;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // Transient inspection state. Owns no document, readiness rules or
    // persistence. The session focuses identities through FocusGraphCasting.
    public sealed class CastingProblemNavigation
    {
        private IReadOnlyList<CastingBlocker> _blockers = new CastingBlocker[0];
        private int _index;

        public bool Active { get { return _blockers.Count != 0; } }
        public int Count { get { return _blockers.Count; } }
        public int Position { get { return Active ? _index + 1 : 0; } }
        public bool CanPrevious { get { return Active && _index > 0; } }
        public bool CanNext { get { return Active && _index + 1 < Count; } }
        public CastingBlocker Current { get { return Active ? _blockers[_index] : null; } }
        public string RoutineId { get; private set; }
        public string PendingRevealCastingId { get; private set; }

        internal void Begin(CastingApplyDecision decision)
        {
            Clear();
            if (decision == null || decision.Allowed || decision.Mode != CastingApplyMode.Ordinary ||
                decision.BlockingCastings.Count == 0) return;
            _blockers = decision.BlockingCastings;
            RoutineId = Current.RoutineId;
            RequestReveal();
        }

        // If repaired, prefer the next surviving old problem; after the
        // last, use the new last. Retaining focus never requests another reveal.
        internal bool Reconcile(CastingApplyDecision decision)
        {
            if (!Active) return false;
            string currentId = Current.CastingId;
            bool wasLast = _index + 1 == _blockers.Count;
            IReadOnlyList<CastingBlocker> next = decision == null
                ? new CastingBlocker[0] : decision.BlockingCastings;
            if (next.Count == 0)
            {
                Clear();
                return false;
            }
            int retained = IndexOf(next, currentId);
            if (retained >= 0)
            {
                _blockers = next;
                _index = retained;
                return false;
            }
            int replacement = -1;
            for (int index = _index + 1; index < _blockers.Count && replacement < 0; index++)
                replacement = IndexOf(next, _blockers[index].CastingId);
            _index = replacement >= 0 ? replacement
                : wasLast ? next.Count - 1 : Math.Min(_index, next.Count - 1);
            _blockers = next;
            RequestReveal();
            return true;
        }

        internal bool Move(int delta)
        {
            if (!Active || (delta != -1 && delta != 1)) return false;
            int next = _index + delta;
            if (next < 0 || next >= Count) return false;
            _index = next;
            RequestReveal();
            return true;
        }

        internal void CompleteReveal(string castingId)
        {
            if (string.Equals(castingId, PendingRevealCastingId, StringComparison.Ordinal))
                PendingRevealCastingId = null;
        }

        internal void RequestReveal()
        {
            PendingRevealCastingId = Current == null ? null : Current.CastingId;
        }

        internal void Clear()
        {
            _blockers = new CastingBlocker[0];
            _index = 0;
            RoutineId = null;
            PendingRevealCastingId = null;
        }

        private static int IndexOf(IReadOnlyList<CastingBlocker> values, string id)
        {
            for (int index = 0; index < values.Count; index++)
                if (string.Equals(values[index].CastingId, id, StringComparison.Ordinal)) return index;
            return -1;
        }
    }

    public sealed partial class CastingWorkspaceSession
    {
        private bool _focusingProblem;
        public CastingProblemNavigation ProblemNavigation { get; } = new CastingProblemNavigation();

        public void LeaveProblemNavigation()
        {
            if (_focusingProblem) return;
            if (ProblemNavigation.Active) LastAttemptMessage = null;
            ProblemNavigation.Clear();
        }

        // External refresh failures remain global even after an earlier
        // casting-specific refusal. No stale problem may overwrite them.
        public void RecordGlobalRefusal(string message)
        {
            if (ProblemNavigation.Active) ClearGraphFocus();
            else LeaveProblemNavigation();
            RecordAttempt(message);
        }

        public bool NavigateProblem(int delta)
        {
            if (!ProblemNavigation.Move(delta)) return false;
            FocusCurrentProblem();
            return true;
        }

        private void FocusCurrentProblem()
        {
            if (!ProblemNavigation.Active) return;
            _focusingProblem = true;
            try { FocusGraphCasting(ProblemNavigation.Current.CastingId); }
            finally { _focusingProblem = false; }
            RecordAttempt(CastingRunPresentation.DescribeProblem(this));
        }

        private void RefreshProblemNavigation(CastingApplyDecision gate)
        {
            if (!ProblemNavigation.Active) return;
            if (!string.Equals(SelectedRoutineId, ProblemNavigation.RoutineId, StringComparison.Ordinal))
            {
                LeaveProblemNavigation();
                return;
            }
            string routineName = RoutineDisplayName(ProblemNavigation.RoutineId);
            bool moved = ProblemNavigation.Reconcile(gate);
            if (!ProblemNavigation.Active)
            {
                RecordAttempt(gate.Allowed ? routineName + " is ready."
                    : CastingRunPresentation.DescribeRefusal(routineName,
                        new WorkspaceApplyResult(false, string.Join(",", gate.BlockingReasons), gate, null)));
                return;
            }
            // A retained blocker still owns its inspector. Explicit manual
            // operations leave problem mode before changing focus; recover
            // a lost focus defensively through the canonical boundary.
            bool focusLost = !string.Equals(EditingFocusCastingId,
                ProblemNavigation.Current.CastingId, StringComparison.Ordinal);
            if (focusLost) ProblemNavigation.RequestReveal();
            if (moved || focusLost) FocusCurrentProblem();
            else RecordAttempt(CastingRunPresentation.DescribeProblem(this));
        }
    }

    // Coordinates come from actual viewport and target bounds in the same
    // local space. Positive offset moves the content up; no ordinal or
    // guessed scroll percentage participates in reveal.
    public static class CastingProblemScrollReveal
    {
        public static float VerticalOffset(float viewportBottom, float viewportTop,
            float targetBottom, float targetTop, float margin)
        {
            float height = viewportTop - viewportBottom;
            if (height <= 0) return 0;
            margin = Math.Max(0, Math.Min(margin, height * 0.1f));
            float bottom = viewportBottom + margin;
            float top = viewportTop - margin;
            if (targetTop - targetBottom > top - bottom) return top - targetTop;
            if (targetBottom < bottom) return bottom - targetBottom;
            if (targetTop > top) return top - targetTop;
            return 0;
        }
    }
}
