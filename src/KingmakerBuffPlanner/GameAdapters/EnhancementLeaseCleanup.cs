using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.GameAdapters
{
    // C853-1: the seam over native activatable state, so the production
    // cleanup algorithm is exercised by tests with stubs ONLY at native
    // state access (never a re-implementation inside a test).
    public interface IActivatableStateAccess
    {
        string Identity { get; }
        bool IsOn { get; set; }
        bool IsRunning { get; }
        void Stop();
    }

    // The production enhancement-lease cleanup (C853-1): restores every
    // owned state to its POLICY-EXPECTED FINAL value - the captured
    // original for ordinary states, the consumed OFF state for a verified
    // consumed one-shot (never resurrected) - verifies the postcondition by
    // reading back, keeps cleaning independent states after a failure, and
    // makes NO positive restoration decision from unreadable consumption.
    // Returns the observable failure record (empty when verified clean).
    public static class EnhancementLeaseCleanup
    {
        public sealed class OwnedState
        {
            public IActivatableStateAccess Access;
            public bool OriginalIsOn;
            public bool OneShot;
            public bool Selected;
            public bool ArmedByLease;
            public bool Rod;
            public string ActivationGroupId;
        }

        public static string Cleanup(IList<OwnedState> states, bool exact)
        {
            var failures = new List<string>();
            HashSet<string> consumedGroups = null;
            try
            {
                consumedGroups = new HashSet<string>(
                    states.Where(state => state.OneShot && state.Selected &&
                            state.ArmedByLease && !state.Access.IsOn)
                        .Select(state => state.ActivationGroupId),
                    StringComparer.Ordinal);
            }
            catch (Exception exception)
            {
                // Consumption could not be read: a one-shot MIGHT be
                // consumed. No positive restoration decision is made for
                // any one-shot (restoring could resurrect a consumed
                // feature - the harmful direction); the failure is
                // reported and the executors halt later casts.
                failures.Add("consumption-unreadable:" + exception.GetType().Name);
            }
            foreach (OwnedState state in states.Reverse())
            {
                bool tryRestore;
                bool expected;
                if (!state.OneShot)
                {
                    // An ordinary state always returns to its captured
                    // original value (OFF or ON alike - the restore DECISION
                    // is not the expected value).
                    expected = state.OriginalIsOn;
                    tryRestore = true;
                }
                else if (consumedGroups == null)
                {
                    continue;
                }
                else if (CastEnhancementActivationPolicy.RestoreOriginalState(
                    state.OneShot, state.ActivationGroupId, consumedGroups))
                {
                    expected = state.OriginalIsOn;
                    tryRestore = true;
                }
                else
                {
                    // Verified consumed: stays consumed (OFF).
                    expected = false;
                    tryRestore = false;
                }
                if (tryRestore)
                {
                    try { state.Access.IsOn = expected; }
                    catch (Exception exception)
                    {
                        failures.Add("restore-exception:" + state.Access.Identity + ":" +
                            exception.GetType().Name);
                        continue;
                    }
                }
                try
                {
                    if (state.Access.IsOn != expected)
                        failures.Add("restore-mismatch:" + state.Access.Identity +
                            ";expected=" + expected + ";actual=" + state.Access.IsOn);
                }
                catch (Exception exception)
                {
                    failures.Add("postcondition-unreadable:" + state.Access.Identity +
                        ":" + exception.GetType().Name);
                }
            }
            if (!exact) return string.Join("|", failures.ToArray());
            foreach (OwnedState state in states.Where(value => value.Rod))
            {
                bool wasRunning;
                try { wasRunning = !state.Access.IsOn && state.Access.IsRunning; }
                catch (Exception exception)
                {
                    failures.Add("rod-state-unreadable:" + exception.GetType().Name);
                    continue;
                }
                if (!wasRunning) continue;
                try { state.Access.Stop(); }
                catch (Exception exception)
                {
                    failures.Add("rod-stop-exception:" + state.Access.Identity + ":" +
                        exception.GetType().Name);
                    continue;
                }
                // A non-throwing Stop is not proof: verify the run stopped.
                try
                {
                    if (state.Access.IsRunning)
                        failures.Add("rod-stop-mismatch:" + state.Access.Identity);
                }
                catch (Exception exception)
                {
                    failures.Add("rod-postcondition-unreadable:" +
                        exception.GetType().Name);
                }
            }
            return string.Join("|", failures.ToArray());
        }
    }
}
