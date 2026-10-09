using System;
using System.Collections;

namespace KingmakerBuffPlanner.UI
{
    // The order in which a Classic routine run is admitted (WP4, rc4 review).
    // A run already in progress, then combat, refuse before anything else:
    // before the profile is refreshed, rebound or saved, before the plan is
    // previewed or compiled, before review state is read or spent and before
    // any executor or native call. Only an admitted run reaches its
    // preparation, which checks combat once more at its final boundary
    // (combat that began while it prepared).
    // What a Classic routine press touched (runtime evidence for the rc4
    // review, finding 2): the result, whether the run yielded, how often the
    // session refreshed and previewed, whether its execution report was
    // replaced, whether it was left executing, whether its profile file kept
    // its bytes.
    internal sealed class ClassicCombatProbe
    {
        internal string Refusal { get; set; }
        internal int Yielded { get; set; }
        internal int Refreshes { get; set; }
        internal int Previews { get; set; }
        internal bool ReportChanged { get; set; }
        internal bool ExecutingAfter { get; set; }
        internal bool ProfileUnchanged { get; set; }
    }

    internal static class ClassicRoutineAdmission
    {
        internal static IEnumerator Run(
            Func<bool> anotherRunExecuting, Action refuseAnotherRun,
            Func<bool> combatActive, Action refuseCombat,
            Func<IEnumerator> prepareAndCast)
        {
            if (anotherRunExecuting == null) throw new ArgumentNullException("anotherRunExecuting");
            if (refuseAnotherRun == null) throw new ArgumentNullException("refuseAnotherRun");
            if (combatActive == null) throw new ArgumentNullException("combatActive");
            if (refuseCombat == null) throw new ArgumentNullException("refuseCombat");
            if (prepareAndCast == null) throw new ArgumentNullException("prepareAndCast");
            return Admit(anotherRunExecuting, refuseAnotherRun, combatActive, refuseCombat,
                prepareAndCast);
        }

        private static IEnumerator Admit(
            Func<bool> anotherRunExecuting, Action refuseAnotherRun,
            Func<bool> combatActive, Action refuseCombat,
            Func<IEnumerator> prepareAndCast)
        {
            if (anotherRunExecuting())
            {
                refuseAnotherRun();
                yield break;
            }
            if (combatActive())
            {
                refuseCombat();
                yield break;
            }
            IEnumerator work = prepareAndCast();
            try
            {
                while (work.MoveNext()) yield return work.Current;
            }
            finally
            {
                // Disposing the run disposes its preparation and casting
                // phase, which cleans up a cast in progress.
                IDisposable disposable = work as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
        }
    }
}
