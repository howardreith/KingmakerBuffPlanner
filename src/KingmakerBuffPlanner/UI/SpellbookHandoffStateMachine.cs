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

    // What a failed handoff does to land the player in a usable interface.
    internal enum SpellbookHandoffRecovery
    {
        // The native window never closed: it is still the player's UI.
        KeepNativeWindow,
        // The native window closed but the planner did not become usable:
        // dispose any half-open planner and reopen the native spellbook
        // through the game's own open contract.
        ReopenNativeSpellbook
    }

    // Deterministic handoff transitions. The Unity side only feeds
    // observations in; every bounded wait and rollback decision lives here so
    // it is provable without the game.
    internal sealed class SpellbookHandoffStateMachine
    {
        internal const int MaximumWaitFrames = 90;

        // WP2B: a click is admitted only when the handoff can actually run.
        // Every route closes the native window through its own close
        // affordance first; there is no direct-open path that would leave
        // the spellbook open underneath the planner. Returns null when
        // admitted, otherwise the exact refusal reason for the log.
        internal static string Admit(bool handoffActive, bool gameAvailable,
            bool plannerOpen, bool nativeCloseUsable)
        {
            if (handoffActive) return "handoff-active";
            if (!gameAvailable) return "no-game";
            if (plannerOpen) return "planner-already-open";
            if (!nativeCloseUsable) return "native-close-affordance-missing";
            return null;
        }

        internal static SpellbookHandoffRecovery RecoveryFor(string failure)
        {
            return string.Equals(failure, "mode-release-timeout", StringComparison.Ordinal)
                ? SpellbookHandoffRecovery.KeepNativeWindow
                : SpellbookHandoffRecovery.ReopenNativeSpellbook;
        }

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
        // planner opener should be invoked. The caller reports the native
        // owner as active while either the FullScreenUi mode or the native
        // window itself is still shown.
        internal bool ObserveRelease(bool nativeOwnerActive)
        {
            if (State != SpellbookHandoffState.WaitingModeRelease) return false;
            if (!nativeOwnerActive)
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
