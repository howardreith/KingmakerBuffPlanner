using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KingmakerBuffPlanner.Planning
{
    // A deterministic signature of a compiled plan's material contents:
    // everything whose change the player must see and accept before the
    // plan may execute — identities, order, exact sources, targets and
    // origins, coverage, enabled modifiers, enhancement selections, cost
    // vectors, and readiness. Read-only diagnostics and preview bookkeeping
    // are deliberately excluded so harmless refreshes do not demand
    // ceremonial reconfirmation.
    public sealed class CastingPlanSignature
    {
        internal CastingPlanSignature(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; private set; }

        public static CastingPlanSignature For(ExplicitCastingPlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var builder = new StringBuilder();
            foreach (ResolvedCasting casting in plan.Castings)
            {
                builder.Append(casting.CastingId).Append('|')
                    .Append(casting.RoutineId).Append('|')
                    .Append(casting.Order).Append('|')
                    .Append(casting.Provider == null ? string.Empty
                        : casting.Provider.Canonical).Append('|')
                    .Append(casting.TargetMode).Append('|')
                    .Append(casting.DirectTargetUnitId ?? string.Empty).Append('|')
                    .Append(casting.Origin == null ? string.Empty
                        : casting.Origin.IsCasterCentered
                            ? "caster"
                            : "anchor:" + casting.Origin.AnchorUnitId).Append('|')
                    .Append(string.Join(",", casting.RequiredCoverageUnitIds)).Append('|')
                    .Append(string.Join(",", casting.TargetingModifiers
                        .Where(value => value.Enabled)
                        .Select(value => value.ModifierId))).Append('|')
                    .Append(string.Join(",", casting.Enhancements
                        .Select(value => value.EnhancementId + ":" +
                            (value.Required ? "R" : "O") + ":" +
                            (value.ExactSourceRef ?? string.Empty)))).Append('|')
                    .Append(string.Join(",", casting.Cost
                        .Select(value => value.Category + ":" + value.PoolKey + ":" +
                            value.Units))).Append('|')
                    .Append(casting.Readiness).Append(';');
            }
            foreach (CastingBudgetLine line in plan.BudgetLines)
                builder.Append("B:").Append(line.PoolKey).Append(':')
                    .Append(line.AllocatedUsage).Append(';');
            return new CastingPlanSignature(builder.ToString());
        }

        public bool Matches(CastingPlanSignature other)
        {
            return other != null &&
                string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
    }

    public enum CastingReviewStatus
    {
        NothingPresented,
        Presented,
        Accepted
    }

    public sealed class CastingReviewDecision
    {
        internal CastingReviewDecision(bool allowed, string reason)
        {
            Allowed = allowed;
            Reason = reason ?? string.Empty;
        }

        public bool Allowed { get; private set; }
        public string Reason { get; private set; }
    }

    // Coordinates the presented-plan contract for the new casting model:
    // a plan may execute only when the exact material contents the player
    // saw and accepted still match the plan being submitted. Computing a
    // preview — including an incidental preview of another routine — never
    // presents or approves anything; a refusal never authorizes the next
    // attempt; a refresh of the same contents preserves acceptance so safe
    // quick-runs need no ceremonial loop.
    //
    // This is the deterministic core of the review contract; the workspace
    // view calls Present when it renders a plan for review and Apply routes
    // through TrySubmit before any executor submission (wiring tracked as
    // the remaining integration step).
    public sealed class CastingReviewCoordinator
    {
        private CastingPlanSignature _presented;
        private CastingPlanSignature _accepted;

        public CastingReviewStatus Status
        {
            get
            {
                if (_accepted != null) return CastingReviewStatus.Accepted;
                if (_presented != null) return CastingReviewStatus.Presented;
                return CastingReviewStatus.NothingPresented;
            }
        }

        // Records that these contents are now shown to the player. The same
        // contents re-presented (a refresh) keep any existing acceptance;
        // different contents clear it — the player has not seen or accepted
        // the new material.
        public void Present(CastingPlanSignature current)
        {
            if (current == null) throw new ArgumentNullException("current");
            if (_presented != null && _presented.Matches(current)) return;
            _presented = current;
            _accepted = null;
        }

        // The player accepts exactly what is presented. Accepting contents
        // that were never presented is refused.
        public CastingReviewDecision Accept(CastingPlanSignature current)
        {
            if (current == null) throw new ArgumentNullException("current");
            if (_presented == null || !_presented.Matches(current))
                return new CastingReviewDecision(false, "not-presented");
            _accepted = current;
            return new CastingReviewDecision(true, string.Empty);
        }

        // A submission is allowed only when accepted contents match the
        // submitted contents exactly. A material change between acceptance
        // and submission refuses; the refusal itself changes nothing, so a
        // later attempt still requires the same accepted contents (or a
        // fresh presentation and acceptance of the new material).
        public CastingReviewDecision TrySubmit(CastingPlanSignature current)
        {
            if (current == null) throw new ArgumentNullException("current");
            if (_accepted == null)
                return new CastingReviewDecision(
                    false, _presented == null ? "nothing-presented" : "not-accepted");
            if (!_accepted.Matches(current))
                return new CastingReviewDecision(false, "material-change-requires-review");
            return new CastingReviewDecision(true, string.Empty);
        }
    }
}
