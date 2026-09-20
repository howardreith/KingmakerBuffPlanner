using System;

namespace KingmakerBuffPlanner.UI
{
    // Session-scoped development selection of the casting-first workspace.
    // The flag is never persisted and never part of ordinary settings: it
    // exists so developer tooling can route the screen controller to the
    // new workspace for a whole session while the legacy screen remains the
    // default. One canonical writer (the CastingWorkspaceSession) owns the
    // candidate records either way; the legacy and new authoring paths are
    // never active together.
    internal static class CastingWorkspaceDevSelection
    {
        private static bool _enabled;

        internal static bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        // While the casting-first workspace owns this session, legacy quick
        // execution is not permitted: the workspace's dispatch boundary is
        // the only submission route, and it is explicitly disabled. Sharing
        // read-only discovery data is fine; a second executing writer is not.
        internal static bool LegacyExecutionPermitted
        {
            get { return !Enabled; }
        }

        internal const string LegacyExecutionRefusal =
            "legacy-execution-disabled-while-casting-workspace-selected";
    }
}
