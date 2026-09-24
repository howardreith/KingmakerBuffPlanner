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

        // The one exception an allowance-bound classic cast run arms: a
        // single execution of exactly its approved classic plan. Armed only
        // inside a locked session; every other classic execution stays
        // refused.
        internal static Execution.ClassicCastGrant ClassicGrant { get; private set; }

        internal static bool ArmClassicGrant(Execution.ClassicCastGrant grant)
        {
            if (!Locked || grant == null || ClassicGrant != null) return false;
            ClassicGrant = grant;
            return true;
        }

        // Whether the locked classic route may execute this plan now (once).
        internal static bool TryConsumeClassicGrant(string routineId, string planDigest,
            string executionMode, int plannedSteps, out string refusal)
        {
            Execution.ClassicCastGrant grant = ClassicGrant;
            if (grant == null)
            {
                refusal = "classic-grant-absent";
                return false;
            }
            return grant.TryConsume(routineId, planDigest, executionMode, plannedSteps, out refusal);
        }
    }
}
