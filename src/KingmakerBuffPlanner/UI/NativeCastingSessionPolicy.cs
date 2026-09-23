using System;

namespace KingmakerBuffPlanner.UI
{
    // Whether this game session may submit native casts through the
    // planner's player routes: the casting-first dispatch boundary AND the
    // classic planner's routine execution. Ordinary play: yes. A runtime-
    // test session: no - the lock is taken before any planner session
    // exists and nothing in production code releases it, so no scenario,
    // supervised manual session or physical HUD click during automation
    // casts through those routes (each refuses with LockReason). The only
    // native submissions an automation session can make go through the
    // harness's own allowance-bound boundaries (the single-cast probe and
    // the casting qualification), which never use the player routes.
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
