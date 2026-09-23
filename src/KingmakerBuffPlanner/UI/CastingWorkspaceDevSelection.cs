using System;

namespace KingmakerBuffPlanner.UI
{
    // Session-scoped selection of the casting-first planner for runtime-test
    // workspace scenarios. It is never persisted: players choose the mode
    // through the saved planner-mode setting (UMM settings panel). While
    // either selects casting-first, every routine route (HUD, hotkey,
    // spellbook, planner Apply) goes to the casting-first pipeline; the
    // legacy executor is never reached. One canonical writer (the
    // CastingWorkspaceSession) owns the candidate records either way.
    internal static class CastingWorkspaceDevSelection
    {
        private static bool _enabled;

        internal static bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        // While the casting-first workspace owns this session, legacy quick
        // execution is not permitted: routine requests go to the workspace
        // dispatch boundary (itself locked in a runtime-test session).
        // Sharing read-only discovery data is fine; a second executing
        // writer is not.
        internal static bool LegacyExecutionPermitted
        {
            get { return !Enabled; }
        }

        internal const string LegacyExecutionRefusal =
            "legacy-execution-disabled-while-casting-workspace-selected";
    }
}
