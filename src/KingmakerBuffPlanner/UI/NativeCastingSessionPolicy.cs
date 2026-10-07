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
    // harness's own allowance-bound boundaries. The recovery qualification
    // alone delivers its exact ordered projections through the player route
    // to exercise the root-owned session and completion ownership.
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

        // Guarded recovery uses the existing qualification boundary and host,
        // through the root's normal routine entry. No unlocked session or
        // other recipe can arm it; its ordered projections, mode and total
        // submission cap remain checked by that boundary on every request.
        internal static Execution.CastingQualificationBoundary RecoveryBoundary { get; private set; }

        internal static bool ArmRecoveryBoundary(Execution.CastingQualificationBoundary boundary)
        {
            if (!Locked || LockReason != "native-submission-disabled:runtime-test-session:live-cast-qual" ||
                boundary == null || RecoveryBoundary != null ||
                !Execution.CastingQualificationRecipe.IsRecoveryRecipe(boundary.Recipe)) return false;
            RecoveryBoundary = boundary;
            return true;
        }

        internal static void DisarmRecoveryBoundary()
        {
            RecoveryBoundary = null;
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

        // Ends the single-use exception once its scenario ends (used or not).
        internal static void DisarmClassicGrant()
        {
            Execution.ClassicCastGrant grant = ClassicGrant;
            if (grant != null) grant.Disarm();
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

        // The parallel exception for the casting-first route (the physical
        // cold-moon scenario): a single execution of exactly its approved
        // routine plan, armed only inside a locked session from a validated
        // allowance. Every other casting-first execution stays refused.
        internal static Execution.CastingFirstCastGrant CastingFirstGrant { get; private set; }

        internal static bool ArmCastingFirstGrant(Execution.CastingFirstCastGrant grant)
        {
            if (!Locked || grant == null || CastingFirstGrant != null) return false;
            CastingFirstGrant = grant;
            return true;
        }

        internal static void DisarmCastingFirstGrant()
        {
            Execution.CastingFirstCastGrant grant = CastingFirstGrant;
            if (grant != null) grant.Disarm();
        }

        internal static bool TryConsumeCastingFirstGrant(string routineId, string planDigest,
            string executionMode, int plannedSteps, out string refusal)
        {
            Execution.CastingFirstCastGrant grant = CastingFirstGrant;
            if (grant == null)
            {
                refusal = "cf-grant-absent";
                return false;
            }
            return grant.TryConsume(routineId, planDigest, executionMode, plannedSteps, out refusal);
        }
    }
}
