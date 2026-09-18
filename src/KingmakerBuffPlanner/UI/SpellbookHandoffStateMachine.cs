using System;

namespace KingmakerBuffPlanner.UI
{
    internal enum SpellbookHandoffState
    {
        Idle,
        WaitingModeRelease,
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
        internal string Failure { get; private set; }

        internal void Begin()
        {
            State = SpellbookHandoffState.WaitingModeRelease;
            WaitedFrames = 0;
            Failure = string.Empty;
        }

        // Called once per frame while a handoff is in flight. Returns true
        // when the planner may take over the input mode.
        internal bool Observe(bool fullScreenUiActive)
        {
            if (State != SpellbookHandoffState.WaitingModeRelease) return false;
            if (!fullScreenUiActive)
            {
                State = SpellbookHandoffState.Completed;
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
            Failure = string.Empty;
        }
    }
}
