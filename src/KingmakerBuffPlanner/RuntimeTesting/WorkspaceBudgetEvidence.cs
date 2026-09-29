using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // The live workspace's global-budget proof (graph addendum v1.1 §4).
    // Contract (review E1-E4):
    // - A phase outcome is exactly one of: "proved", "not-run:unsupported:"
    //   plus a concrete scan finding (the fixture genuinely lacks the
    //   capability), or "error:" plus a cause. Anything else - empty,
    //   unknown, raw exceptions, missing session/record - is an error.
    // - Evaluate() is the single decision path the runtime host uses:
    //   required phases must be "proved"; an error is ALWAYS a violation
    //   even for optional phases; a genuine unsupported capability is a
    //   violation only when its phase is required on that fixture.
    // - "proved" demands the complete numbers: the raw ledger values AND the
    //   other buff's displayed additional capacity before/add/remove, the
    //   exact cost-aware deltas (a displayed count that stays stale while
    //   the raw pool moves is a violation), and the second casting's
    //   enhancement-pool observation (never optional).
    // - The atomic refusal observes the WHOLE cost vector of the blocked
    //   casting: every pool it touches after the blocked toggle must equal
    //   the state that casting's absence would leave (its prior base
    //   reservation legitimately released, nothing new reserved anywhere).
    public sealed class WorkspaceBudgetEvidence
    {
        public bool CrossBuffAttempted { get; set; }
        public string CrossBuffStatus { get; set; }
        public string PoolKey { get; set; }
        public string PoolKind { get; set; }
        public string BuffA { get; set; }
        public string BuffB { get; set; }
        public string CasterUnitId { get; set; }
        public string RowALabel { get; set; }
        public string RowBLabel { get; set; }
        public int? BeforeRemaining { get; set; }
        public int? AfterAddRemaining { get; set; }
        public int? AfterRemoveRemaining { get; set; }
        public int ConsumedUnitsPerCastA { get; set; }
        public int ConsumedUnitsPerCastB { get; set; }
        public bool AdditionalBIsLowerBound { get; set; }
        public int? BeforeBuffBAdditional { get; set; }
        public int? AfterAddBuffBAdditional { get; set; }
        public int? AfterRemoveBuffBAdditional { get; set; }
        public string AddedCastingId { get; set; }

        public bool SharedEnhancementAttempted { get; set; }
        public string SharedEnhancementStatus { get; set; }
        public string EnhancementId { get; set; }
        public string EnhancementName { get; set; }
        public string UsagePoolId { get; set; }
        public string UsagePoolLabel { get; set; }
        public int UsageUnitsPerCast { get; set; }
        public string FirstCastingId { get; set; }
        public string SecondCastingId { get; set; }
        public int? BeforePoolRemaining { get; set; }
        public int? AfterFirstOn { get; set; }
        public int? AfterSecondOn { get; set; }
        public int? AfterFirstOff { get; set; }
        public string AtomicRefusalStatus { get; set; }
        public string AtomicRefusalEvidence { get; set; }
        // The blocked casting's complete cost vector, observed per pool
        // immediately before and after the blocked toggle, with the base
        // reservation the blocking legitimately releases.
        public List<BlockedPoolObservation> BlockedTogglePools { get; } =
            new List<BlockedPoolObservation>();
        public bool CleanupVerified { get; set; }
        public string CleanupEvidence { get; set; }

        public List<string> Notes { get; } = new List<string>();

        public void AddNote(string note)
        {
            if (!string.IsNullOrEmpty(note)) Notes.Add(note);
        }

        public bool IsProved(string status)
        {
            return string.Equals(status, "proved", StringComparison.Ordinal);
        }

        public static bool IsUnsupported(string status)
        {
            return status != null && status.StartsWith("not-run:unsupported:",
                StringComparison.Ordinal);
        }

        public static bool IsError(string status)
        {
            if (string.IsNullOrEmpty(status)) return true;
            if (status.StartsWith("error:", StringComparison.Ordinal)) return true;
            return !string.Equals(status, "proved", StringComparison.Ordinal) &&
                !IsUnsupported(status);
        }

        // The single decision path used by the runtime host: the violation
        // list plus a per-phase manifest. A required phase must be proved;
        // errors always violate; genuine unsupported only when required.
        public IList<string> Evaluate(bool crossBuffRequired, bool enhancementRequired)
        {
            var violations = new List<string>();
            EvaluatePhase("cross-buff", CrossBuffAttempted, CrossBuffStatus, crossBuffRequired,
                violations, EvaluateCrossBuffNumbers);
            EvaluatePhase("enhancement", SharedEnhancementAttempted, SharedEnhancementStatus,
                enhancementRequired, violations, EvaluateEnhancementNumbers);
            if (!CleanupVerified)
                violations.Add("cleanup-not-verified");
            if (!string.IsNullOrEmpty(CleanupEvidence) &&
                CleanupEvidence.StartsWith("failed", StringComparison.Ordinal))
                violations.Add("cleanup-failed:" + CleanupEvidence);
            return violations;
        }

        private void EvaluatePhase(string phase, bool attempted, string status, bool required,
            List<string> violations, Action<List<string>> numberChecks)
        {
            if (!attempted)
            {
                violations.Add(phase + ":not-attempted");
                return;
            }
            if (IsError(status))
            {
                violations.Add(phase + ":error:" + (string.IsNullOrEmpty(status) ? "empty" : status));
                return;
            }
            bool proved = IsProved(status);
            if (!proved)
            {
                // The only remaining outcome is not-run:unsupported:...
                if (required)
                    violations.Add(phase + ":required-phase-not-run:" + status);
                return;
            }
            numberChecks(violations);
        }

        private void EvaluateCrossBuffNumbers(List<string> violations)
        {
            if (string.IsNullOrEmpty(BuffB) || string.Equals(BuffA, BuffB, StringComparison.Ordinal))
            {
                violations.Add("cross-buff-same-buff");
                return;
            }
            if (BeforeRemaining == null || AfterAddRemaining == null || AfterRemoveRemaining == null ||
                BeforeBuffBAdditional == null || AfterAddBuffBAdditional == null ||
                AfterRemoveBuffBAdditional == null)
            {
                violations.Add("cross-buff-numbers-missing");
                return;
            }
            if (ConsumedUnitsPerCastA <= 0 || ConsumedUnitsPerCastB <= 0)
            {
                violations.Add("cross-buff-costs-unknown");
                return;
            }
            if (AfterAddRemaining.Value != BeforeRemaining.Value - ConsumedUnitsPerCastA)
                violations.Add("cross-buff-add-delta=" + AfterAddRemaining.Value + "!=" +
                    (BeforeRemaining.Value - ConsumedUnitsPerCastA));
            if (AfterRemoveRemaining.Value != BeforeRemaining.Value)
                violations.Add("cross-buff-remove-did-not-restore");
            // The displayed count must say what the raw ledger means: after
            // the add, buff B's additional castings are exactly the shared
            // pool's remaining units over B's own per-cast cost (unless the
            // count is a capped lower bound); after removal it is exactly
            // what it was before. A number that sits still while the pool
            // drains, or missing numbers, fail the proof.
            if (AdditionalBIsLowerBound && AfterAddBuffBAdditional.Value <
                    BeforeBuffBAdditional.Value)
            {
                violations.Add("cross-buff-lower-bound-should-not-drop");
            }
            else if (!AdditionalBIsLowerBound)
            {
                int expected = AfterAddRemaining.Value / ConsumedUnitsPerCastB;
                if (AfterAddBuffBAdditional.Value != expected)
                    violations.Add("cross-buff-display-stale=" + AfterAddBuffBAdditional.Value +
                        "!=" + expected);
            }
            if (AfterRemoveBuffBAdditional.Value != BeforeBuffBAdditional.Value)
                violations.Add("cross-buff-b-count-not-restored");
        }

        private void EvaluateEnhancementNumbers(List<string> violations)
        {
            if (string.IsNullOrEmpty(SecondCastingId) || string.Equals(FirstCastingId,
                    SecondCastingId, StringComparison.Ordinal))
            {
                violations.Add("enhancement-same-casting");
                return;
            }
            if (UsageUnitsPerCast <= 0)
            {
                violations.Add("enhancement-cost-unknown");
                return;
            }
            if (BeforePoolRemaining == null || AfterFirstOn == null || AfterFirstOff == null ||
                AfterSecondOn == null)
            {
                violations.Add("enhancement-numbers-missing");
                return;
            }
            if (AfterFirstOn.Value != BeforePoolRemaining.Value - UsageUnitsPerCast)
                violations.Add("enhancement-on-delta=" + AfterFirstOn.Value + "!=" +
                    (BeforePoolRemaining.Value - UsageUnitsPerCast));
            if (AfterFirstOff.Value != BeforePoolRemaining.Value)
                violations.Add("enhancement-off-did-not-restore");
            // The second observation follows the second casting's ACTUAL
            // compiled readiness: a blocked candidate reserved nothing
            // anywhere (the whole-cost atomic refusal), so every observed
            // number stands still; an executable candidate consumes
            // normally from the same shared pool.
            int expectedSecond = string.Equals(AtomicRefusalStatus, "observed",
                StringComparison.Ordinal)
                ? AfterFirstOn.Value
                : BeforePoolRemaining.Value - 2 * UsageUnitsPerCast;
            if (AfterSecondOn.Value != expectedSecond)
                violations.Add("enhancement-second-casting-did-not-see-the-same-pool");
            EvaluateAtomicRefusal(violations);
        }

        private void EvaluateAtomicRefusal(List<string> violations)
        {
            if (string.IsNullOrEmpty(AtomicRefusalStatus))
            {
                violations.Add("atomic-refusal-unrecorded");
                return;
            }
            if (string.Equals(AtomicRefusalStatus, "not-provable-on-this-fixture",
                    StringComparison.Ordinal))
                return;
            if (!string.Equals(AtomicRefusalStatus, "observed", StringComparison.Ordinal))
            {
                violations.Add("atomic-refusal:" + AtomicRefusalStatus + ":" + AtomicRefusalEvidence);
                return;
            }
            if (BlockedTogglePools.Count == 0)
            {
                violations.Add("atomic-refusal-no-cost-vector");
                return;
            }
            foreach (BlockedPoolObservation pool in BlockedTogglePools)
            {
                if (pool.BeforeToggle == null || pool.AfterToggle == null)
                {
                    violations.Add("atomic-refusal-pool-unreadable:" + pool.PoolId);
                    continue;
                }
                // Across the blocked candidate's toggle, NOTHING may change
                // in any pool it touches: the unfundable casting allocated
                // no component. (Whether a prior base reservation existed
                // and was released is an authoring-time observation carried
                // in the notes, not an arithmetic assumption.)
                int expected = pool.BeforeToggle.Value;
                if (pool.AfterToggle.Value != expected)
                    violations.Add("atomic-refusal-reserved:" + pool.PoolId + "=" +
                        pool.AfterToggle.Value + "!=" + expected);
            }
        }

        public string Describe()
        {
            var text = new StringBuilder();
            text.Append("crossBuff=").Append(CrossBuffAttempted
                ? CrossBuffStatus : "not-attempted");
            if (CrossBuffAttempted && !string.IsNullOrEmpty(PoolKey))
                text.Append(";pool=").Append(PoolKey).Append('/').Append(PoolKind)
                    .Append(";A=").Append(BuffA).Append("/u").Append(ConsumedUnitsPerCastA)
                    .Append(";B=").Append(BuffB).Append("/u").Append(ConsumedUnitsPerCastB)
                    .Append(";remain=").Append(BeforeRemaining).Append('-')
                    .Append(AfterAddRemaining).Append('-').Append(AfterRemoveRemaining)
                    .Append(";bAdd=").Append(BeforeBuffBAdditional).Append('>')
                    .Append(AfterAddBuffBAdditional).Append('>')
                    .Append(AfterRemoveBuffBAdditional)
                    .Append(AdditionalBIsLowerBound ? ";bAddIsLowerBound" : string.Empty);
            text.Append(";enhancement=").Append(SharedEnhancementAttempted
                ? SharedEnhancementStatus : "not-attempted");
            if (SharedEnhancementAttempted && !string.IsNullOrEmpty(EnhancementId))
                text.Append(";e=").Append(EnhancementName).Append("/u").Append(UsageUnitsPerCast)
                    .Append(";pool=").Append(UsagePoolLabel)
                    .Append(";remain=").Append(BeforePoolRemaining).Append('-')
                    .Append(AfterFirstOn).Append('-').Append(AfterSecondOn).Append('-')
                    .Append(AfterFirstOff);
            if (!string.IsNullOrEmpty(AtomicRefusalStatus))
            {
                text.Append(";atomic=").Append(AtomicRefusalStatus);
                foreach (BlockedPoolObservation pool in BlockedTogglePools)
                    text.Append(";blockedPool=").Append(pool.PoolId)
                        .Append('/').Append(pool.BeforeToggle).Append('>')
                        .Append(pool.AfterToggle)
                        .Append("/base=").Append(pool.BaseReservationReleased);
            }
            text.Append(";cleanup=").Append(CleanupVerified ? "verified" : CleanupEvidence);
            foreach (string note in Notes)
                text.Append(';').Append(note);
            return text.ToString();
        }
    }

    // One pool of the blocked casting's cost vector: its balance immediately
    // before the blocked toggle, immediately after, and the base reservation
    // the blocking legitimately releases (0 for pools the base cost did not
    // draw on).
    public sealed class BlockedPoolObservation
    {
        public string PoolId { get; set; }
        public string Kind { get; set; }
        public int? BeforeToggle { get; set; }
        public int? AfterToggle { get; set; }
        public int BaseReservationReleased { get; set; }
    }
}
