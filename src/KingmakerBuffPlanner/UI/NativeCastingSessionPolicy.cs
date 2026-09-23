using System;

namespace KingmakerBuffPlanner.UI
{
    // Whether this game session may submit native casts from the production
    // planner. Ordinary play: yes. A runtime-test session: no - the lock is
    // taken before any planner session exists and nothing in production
    // code releases it, so a qualification scenario, a supervised manual
    // session or a physical HUD click during automation can never reach the
    // native executors through the ordinary UI. (The guarded single-cast
    // probe keeps its own allowance-bound boundary.)
    internal static class NativeCastingSessionPolicy
    {
        internal static bool Locked { get; private set; }
        internal static string LockReason { get; private set; }

        internal static void LockForRuntimeTest(string scenario)
        {
            Locked = true;
            LockReason = "native-submission-disabled:runtime-test-session:" +
                (string.IsNullOrEmpty(scenario) ? "unknown" : scenario);
        }
    }
}
