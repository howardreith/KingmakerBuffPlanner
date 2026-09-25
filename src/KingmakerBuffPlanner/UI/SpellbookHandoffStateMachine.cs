using System;

namespace KingmakerBuffPlanner.UI
{
    internal enum SpellbookHandoffState
    {
        Idle,
        WaitingModeRelease,
        OpeningPlanner,
        WaitingPresentation,
        Completed,
        Failed
    }

    // Deterministic handoff transitions. The Unity side only feeds
    // observations in; every bounded wait and rollback decision lives here so
    // it is provable without the game.
    internal sealed class SpellbookHandoffStateMachine
    {
        internal const int MaximumWaitFrames = 90;

        internal SpellbookHandoffState State { get; private set; }
        internal int WaitedFrames { get; private set; }
        internal int OpenAttempts { get; private set; }
        internal string Failure { get; private set; }

        internal void Begin()
        {
            State = SpellbookHandoffState.WaitingModeRelease;
            WaitedFrames = 0;
            OpenAttempts = 0;
            Failure = string.Empty;
        }

        // True exactly once when native ownership has been released and the
        // planner opener should be invoked.
        internal bool ObserveRelease(bool fullScreenUiActive)
        {
            if (State != SpellbookHandoffState.WaitingModeRelease) return false;
            if (!fullScreenUiActive)
            {
                State = SpellbookHandoffState.OpeningPlanner;
                return true;
            }
            WaitedFrames++;
            if (WaitedFrames >= MaximumWaitFrames)
            {
                State = SpellbookHandoffState.Failed;
                Failure = "mode-release-timeout";
            }
            return false;
        }

        // The opener ran; openerAccepted=false records an immediate refusal.
        internal void ObserveOpenResult(bool openerAccepted)
        {
            if (State != SpellbookHandoffState.OpeningPlanner) return;
            OpenAttempts++;
            if (!openerAccepted)
            {
                State = SpellbookHandoffState.Failed;
                Failure = "planner-open-refused";
                return;
            }
            State = SpellbookHandoffState.WaitingPresentation;
            WaitedFrames = 0;
        }

        // True exactly once when the deferred presentation lifecycle reached
        // the open, input-owning state.
        internal bool ObservePresentation(bool plannerOpen)
        {
            if (State != SpellbookHandoffState.WaitingPresentation) return false;
            if (plannerOpen)
            {
                State = SpellbookHandoffState.Completed;
                return true;
            }
            WaitedFrames++;
            if (WaitedFrames >= MaximumWaitFrames)
            {
                State = SpellbookHandoffState.Failed;
                Failure = "presentation-timeout";
            }
            return false;
        }

        // A failed handoff is recoverable from any non-completed state,
        // including after the opener already ran.
        internal void Rollback(string reason)
        {
            if (State == SpellbookHandoffState.Completed) return;
            State = SpellbookHandoffState.Failed;
            Failure = reason ?? string.Empty;
        }

        internal void Reset()
        {
            State = SpellbookHandoffState.Idle;
            WaitedFrames = 0;
            OpenAttempts = 0;
            Failure = string.Empty;
        }
    }
}
