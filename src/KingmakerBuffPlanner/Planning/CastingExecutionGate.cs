using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerBuffPlanner.Planning
{
    // One casting that this apply decision does not execute, with the exact
    // reasons. Omissions are always disclosed — never silently dropped.
    public sealed class CastingOmission
    {
        internal CastingOmission(string castingId, string routineId,
            IEnumerable<string> reasons)
        {
            CastingId = castingId;
            RoutineId = routineId ?? string.Empty;
            Reasons = new ReadOnlyCollection<string>((reasons ?? new string[0])
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public string CastingId { get; private set; }
        public string RoutineId { get; private set; }
        public IReadOnlyList<string> Reasons { get; private set; }
    }

    public sealed class CastingApplyDecision
    {
        internal CastingApplyDecision(
            bool allowed,
            CastingApplyMode mode,
            string scopeRoutineId,
            IEnumerable<string> executableCastingIds,
            IEnumerable<CastingOmission> omissions,
            IEnumerable<string> blockingReasons)
        {
            Allowed = allowed;
            Mode = mode;
            ScopeRoutineId = scopeRoutineId ?? string.Empty;
            ExecutableCastingIds = new ReadOnlyCollection<string>(
                executableCastingIds.ToList());
            Omissions = new ReadOnlyCollection<CastingOmission>(omissions.ToList());
            BlockingReasons = new ReadOnlyCollection<string>(blockingReasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public bool Allowed { get; private set; }
        public CastingApplyMode Mode { get; private set; }
        // Empty scope means the one-pass sequence over every routine.
        public string ScopeRoutineId { get; private set; }
        public IReadOnlyList<string> ExecutableCastingIds { get; private set; }
        public IReadOnlyList<CastingOmission> Omissions { get; private set; }
        public IReadOnlyList<string> BlockingReasons { get; private set; }
    }

    public enum CastingApplyMode
    {
        // The ordinary policy: every casting the run asks for must be ready;
        // any blocked or unresolved request refuses the whole apply.
        Ordinary,
        // The explicit fallback: execute ready castings only, disclose every
        // omitted casting with reasons, and preserve the saved plan.
        ReadyCastsOnly
    }

    // Stateless apply policy over a compiled plan, scoped to one routine for
    // a selected run or to every routine for the one-pass sequence. Ordinary
    // Apply counts every saved casting in scope — a blocked request blocks,
    // and a saved Draft is an unresolved request that must be resolved,
    // explicitly disabled, or deliberately omitted via Ready Casts Only; it
    // is never an implicit opt-out. An explicitly Disabled casting is
    // disclosed as an omission but never blocks a run. Out-of-scope castings
    // are ignored entirely so unrelated valid runs are not held hostage.
    //
    // Evaluating this gate is pure: a preview, a refused attempt, or a
    // recomputed plan never authorizes a later attempt by itself. The
    // caller must present the mode and the plan explicitly every time; the
    // execution coordinator (a later phase) consumes the same decision.
    public sealed class CastingExecutionGate
    {
        public CastingApplyDecision Evaluate(
            ExplicitCastingPlan plan, CastingApplyMode mode,
            string scopeRoutineId = null)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var executable = new List<string>();
            var omissions = new List<CastingOmission>();
            var blocking = new List<string>();
            foreach (ResolvedCasting casting in plan.Castings)
            {
                if (scopeRoutineId != null &&
                    casting.RoutineId != scopeRoutineId)
                    continue;
                if (casting.IsExecutable)
                {
                    executable.Add(casting.CastingId);
                    continue;
                }
                string fallback;
                bool blocks;
                if (casting.Readiness == ResolvedCastingReadiness.Disabled)
                {
                    fallback = "explicitly-disabled";
                    blocks = false;
                }
                else if (casting.Readiness == ResolvedCastingReadiness.Draft)
                {
                    // A saved unresolved request is not an implicit opt-out:
                    // ordinary Apply demands explicit resolution.
                    fallback = "unresolved-saved-request";
                    blocks = true;
                }
                else if (casting.Readiness == ResolvedCastingReadiness.AlreadySatisfied)
                {
                    fallback = "already-satisfied";
                    blocks = false;
                }
                else
                {
                    fallback = "blocked";
                    blocks = true;
                }
                var reasons = casting.ReadinessReasons.ToList();
                if (reasons.Count == 0) reasons.Add(fallback);
                omissions.Add(new CastingOmission(
                    casting.CastingId, casting.RoutineId, reasons));
                if (blocks && mode == CastingApplyMode.Ordinary)
                    blocking.Add(fallback + "-casting:" + casting.CastingId + ":" +
                        reasons[0]);
            }
            if (mode == CastingApplyMode.Ordinary && blocking.Count != 0)
                return new CastingApplyDecision(
                    false, mode, scopeRoutineId, executable, omissions, blocking);
            return new CastingApplyDecision(
                true, mode, scopeRoutineId, executable, omissions, new string[0]);
        }
    }
}
