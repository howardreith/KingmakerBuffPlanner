using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KingmakerBuffPlanner.Planning
{
    // A deterministic signature of a compiled plan's material contents:
    // everything whose change the player must see and accept before the
    // plan may execute — identities, order, exact sources, targets and
    // origins, coverage, enabled modifiers, enhancement selections, the
    // existing-effect policy, each casting's would-be cost vector, and its
    // readiness class. Read-only diagnostics and preview bookkeeping are
    // deliberately excluded so harmless refreshes do not demand ceremonial
    // reconfirmation.
    //
    // Harmless by design (production execution slice): whether a runnable
    // casting is skipped right now because its effect is already active.
    // Ready and AlreadySatisfied share one readiness class and the cost is
    // the casting's would-be cost shape, so a buff that expires (or is
    // applied) between acceptance and a quick-run does not demand
    // re-review, while any change to WHAT a casting would do or spend, and
    // any casting becoming blocked, draft or disabled, stays material.
    // Budget balances are not signed: they follow from the per-casting
    // costs and readiness classes, which are.
    //
    // Execution settings (Animated/Instant mode, the animated fallback) are
    // deliberately NOT signed: they choose how the game performs the same
    // castings, never which castings run, from which source, on whom, with
    // which enhancements or at what cost. The mode in effect is shown on
    // the routine tooltip and the planner, and recorded with every run.
    public sealed class CastingPlanSignature
    {
        internal CastingPlanSignature(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; private set; }

        public static CastingPlanSignature For(ExplicitCastingPlan plan)
        {
            return For(plan, null);
        }

        // A routine scope signs only that routine's castings: an edit to
        // another routine is not a material change to this one.
        public static CastingPlanSignature For(ExplicitCastingPlan plan,
            string scopeRoutineId)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var builder = new StringBuilder();
            builder.Append("scope:").Append(scopeRoutineId ?? "*").Append(';');
            foreach (ResolvedCasting casting in plan.Castings)
            {
                if (scopeRoutineId != null &&
                    !string.Equals(casting.RoutineId, scopeRoutineId, StringComparison.Ordinal))
                    continue;
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
                    .Append(casting.ExistingEffectPolicy).Append('|')
                    .Append(string.Join(",", casting.CostShape)).Append('|')
                    .Append(ReadinessClass(casting.Readiness)).Append(';');
            }
            return new CastingPlanSignature(builder.ToString());
        }

        private static string ReadinessClass(ResolvedCastingReadiness readiness)
        {
            return readiness == ResolvedCastingReadiness.Ready ||
                readiness == ResolvedCastingReadiness.AlreadySatisfied
                    ? "Runnable" : readiness.ToString();
        }

        public bool Matches(CastingPlanSignature other)
        {
            return other != null &&
                string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        // SHA-256 of Value (lower-case hex): what review state stores and
        // compares, so an acceptance can persist without the full contents.
        public string Digest
        {
            get
            {
                if (_digest == null)
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                        _digest = BitConverter.ToString(sha.ComputeHash(
                                Encoding.UTF8.GetBytes(Value)))
                            .Replace("-", string.Empty).ToLowerInvariant();
                return _digest;
            }
        }

        private string _digest;
    }

    public enum CastingReviewStatus
    {
        NothingPresented,
        Presented,
        Accepted
    }

    // What the player can be told about a scope's acceptance without
    // recomputing the plan: none; on file (accepted, possibly in an earlier
    // session, and not presented since, so it runs only if still
    // identical); current (what is presented is what was accepted); or
    // changed (what is presented differs, which may be temporary).
    public enum CastingAcceptanceStanding
    {
        None,
        OnFile,
        Current,
        Changed
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
    // preview (including an incidental preview of another routine) never
    // presents or approves anything; a refusal never authorizes the next
    // attempt; a refresh of the same contents preserves acceptance so safe
    // quick-runs need no ceremonial loop.
    //
    // Review state is kept PER SCOPE (a routine id; "" for the unscoped
    // legacy calls), so accepting Long and later reviewing Short never
    // disturbs Long's acceptance, and a quick-run of one routine checks
    // only that routine's accepted contents. An acceptance restored from
    // storage counts only while it still matches what is presented or
    // submitted.
    public sealed class CastingReviewCoordinator
    {
        private const string DefaultScope = "";
        private readonly Dictionary<string, string> _presented =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _accepted =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public CastingReviewStatus Status
        {
            get { return StatusFor(DefaultScope); }
        }

        // Accepted only while the accepted digest is what is presented (or
        // nothing has been presented yet, e.g. an acceptance restored from
        // storage before the planner opens).
        public CastingReviewStatus StatusFor(string scope)
        {
            string key = scope ?? DefaultScope;
            string accepted;
            string presented;
            bool hasAccepted = _accepted.TryGetValue(key, out accepted);
            bool hasPresented = _presented.TryGetValue(key, out presented);
            if (hasAccepted && (!hasPresented ||
                    string.Equals(accepted, presented, StringComparison.Ordinal)))
                return CastingReviewStatus.Accepted;
            if (hasPresented) return CastingReviewStatus.Presented;
            return CastingReviewStatus.NothingPresented;
        }

        public CastingAcceptanceStanding StandingFor(string scope)
        {
            string key = scope ?? DefaultScope;
            string accepted;
            string presented;
            if (!_accepted.TryGetValue(key, out accepted)) return CastingAcceptanceStanding.None;
            if (!_presented.TryGetValue(key, out presented)) return CastingAcceptanceStanding.OnFile;
            return string.Equals(accepted, presented, StringComparison.Ordinal)
                ? CastingAcceptanceStanding.Current
                : CastingAcceptanceStanding.Changed;
        }

        // The accepted contents' digests by scope (for persistence).
        public IReadOnlyDictionary<string, string> AcceptedDigests
        {
            get
            {
                return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(_accepted, StringComparer.Ordinal));
            }
        }

        public void Present(CastingPlanSignature current)
        {
            Present(DefaultScope, current);
        }

        // Records that these contents are now shown to the player. Showing
        // DIFFERENT contents never revokes an acceptance: an acceptance only
        // ever authorizes its exact digest, so a temporary difference (a
        // slot spent, a buff expired, the planner opened mid-run) cannot
        // turn into a permanent "accept again", and the new material still
        // needs its own acceptance before it can run.
        public void Present(string scope, CastingPlanSignature current)
        {
            if (current == null) throw new ArgumentNullException("current");
            _presented[scope ?? DefaultScope] = current.Digest;
        }

        public CastingReviewDecision Accept(CastingPlanSignature current)
        {
            return Accept(DefaultScope, current);
        }

        // The player accepts exactly what is presented. Accepting contents
        // that were never presented is refused.
        public CastingReviewDecision Accept(string scope, CastingPlanSignature current)
        {
            if (current == null) throw new ArgumentNullException("current");
            string key = scope ?? DefaultScope;
            string presented;
            if (!_presented.TryGetValue(key, out presented) ||
                !string.Equals(presented, current.Digest, StringComparison.Ordinal))
                return new CastingReviewDecision(false, "not-presented");
            _accepted[key] = current.Digest;
            return new CastingReviewDecision(true, string.Empty);
        }

        public CastingReviewDecision TrySubmit(CastingPlanSignature current)
        {
            return TrySubmit(DefaultScope, current);
        }

        // A submission is allowed only when accepted contents match the
        // submitted contents exactly. A material change between acceptance
        // and submission refuses; the refusal itself changes nothing, so a
        // later attempt still requires the same accepted contents (or a
        // fresh presentation and acceptance of the new material).
        public CastingReviewDecision TrySubmit(string scope, CastingPlanSignature current)
        {
            if (current == null) throw new ArgumentNullException("current");
            string key = scope ?? DefaultScope;
            string accepted;
            if (!_accepted.TryGetValue(key, out accepted))
                return new CastingReviewDecision(
                    false, _presented.ContainsKey(key) ? "not-accepted" : "nothing-presented");
            if (!string.Equals(accepted, current.Digest, StringComparison.Ordinal))
                return new CastingReviewDecision(false, "material-change-requires-review");
            return new CastingReviewDecision(true, string.Empty);
        }

        // Restores an acceptance recorded in an earlier session. It is not
        // a presentation: it authorizes only contents whose digest still
        // matches exactly (presenting different contents does not clear it;
        // see Present).
        public void RestoreAccepted(string scope, string digest)
        {
            if (string.IsNullOrEmpty(digest) || digest.Length != 64 ||
                digest.Any(value => !((value >= '0' && value <= '9') || (value >= 'a' && value <= 'f'))))
                return;
            _accepted[scope ?? DefaultScope] = digest;
        }

        // Explicit withdrawal (for example the player revoking acceptance).
        public bool Revoke(string scope)
        {
            return _accepted.Remove(scope ?? DefaultScope);
        }
    }
}
