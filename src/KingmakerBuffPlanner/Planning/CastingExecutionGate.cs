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
            IEnumerable<string> executableCastingIds,
            IEnumerable<CastingOmission> omissions,
            IEnumerable<string> blockingReasons)
        {
            Allowed = allowed;
            Mode = mode;
            ExecutableCastingIds = new ReadOnlyCollection<string>(
                executableCastingIds.ToList());
            Omissions = new ReadOnlyCollection<CastingOmission>(omissions.ToList());
            BlockingReasons = new ReadOnlyCollection<string>(blockingReasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public bool Allowed { get; private set; }
        public CastingApplyMode Mode { get; private set; }
        public IReadOnlyList<string> ExecutableCastingIds { get; private set; }
        public IReadOnlyList<CastingOmission> Omissions { get; private set; }
        public IReadOnlyList<string> BlockingReasons { get; private set; }
    }

    public enum CastingApplyMode
    {
        // The ordinary policy: every casting the run asks for must be ready;
        // any blocked request refuses the whole apply.
        Ordinary,
        // The explicit fallback: execute ready castings only, disclose every
        // omitted casting with reasons, and preserve the saved plan.
        ReadyCastsOnly
    }

    // Stateless apply policy over a compiled plan. Ordinary Apply counts
    // every saved casting — missing casters, unresolved sources, exhausted
    // budgets and unavailable enhancements block instead of hiding — while
    // drafts (authored intent deliberately not enabled) are disclosed as
    // omissions. Ready-Casts-Only is an explicit mode that never silently
    // falls back: it lists exactly what it omits.
    //
    // Evaluating this gate is pure: a preview, a refused attempt, or a
    // recomputed plan never authorizes a later attempt by itself. The
    // caller must present the mode and the plan explicitly every time; the
    // execution coordinator (a later phase) consumes the same decision.
    public sealed class CastingExecutionGate
    {
        public CastingApplyDecision Evaluate(
            ExplicitCastingPlan plan, CastingApplyMode mode)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var executable = new List<string>();
            var omissions = new List<CastingOmission>();
            var blocking = new List<string>();
            foreach (ResolvedCasting casting in plan.Castings)
            {
                if (casting.IsExecutable)
                {
                    executable.Add(casting.CastingId);
                    continue;
                }
                var reasons = casting.ReadinessReasons.ToList();
                if (reasons.Count == 0)
                    reasons.Add(casting.Readiness == ResolvedCastingReadiness.Draft
                        ? "draft-not-enabled"
                        : "blocked");
                omissions.Add(new CastingOmission(
                    casting.CastingId, casting.RoutineId, reasons));
                if (casting.Readiness == ResolvedCastingReadiness.Blocked)
                    blocking.Add("blocked-casting:" + casting.CastingId + ":" +
                        reasons[0]);
            }
            if (mode == CastingApplyMode.Ordinary && blocking.Count != 0)
                return new CastingApplyDecision(
                    false, mode, executable, omissions, blocking);
            return new CastingApplyDecision(
                true, mode, executable, omissions, new string[0]);
        }
    }
}
