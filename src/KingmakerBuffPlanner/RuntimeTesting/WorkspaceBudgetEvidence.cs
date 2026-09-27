using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // The live workspace's global-budget proof (graph addendum v1.1 §4):
    // one shared spontaneous pool observed across TWO buffs before/after
    // one production casting, and one shared enhancement pool observed
    // across TWO castings before/after one toggle, with the restore and
    // (when the fixture permits) the atomic insufficiency refusal. Every
    // number comes from the compiled plans' own capacity (the read model
    // the UI shows); the scenario never counts anything itself. A phase
    // the fixture cannot support is recorded as "unsupported", never
    // faked and never a violation.
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
        // The atomic refusal: a combined demand the pool cannot fund must
        // block the whole casting and reserve nothing.
        public string AtomicRefusalStatus { get; set; }
        public string AtomicRefusalEvidence { get; set; }

        public List<string> Notes { get; } = new List<string>();

        public void AddNote(string note)
        {
            if (!string.IsNullOrEmpty(note)) Notes.Add(note);
        }

        public IEnumerable<string> Violations()
        {
            if (CrossBuffAttempted && string.Equals(CrossBuffStatus, "proved", StringComparison.Ordinal))
            {
                if (BeforeRemaining == null || AfterAddRemaining == null || AfterRemoveRemaining == null)
                    yield return "cross-buff-numbers-missing";
                else
                {
                    if (AfterAddRemaining.Value != BeforeRemaining.Value - ConsumedUnitsPerCastA)
                        yield return "cross-buff-add-delta=" + AfterAddRemaining.Value + "!=" +
                            (BeforeRemaining.Value - ConsumedUnitsPerCastA);
                    if (AfterRemoveRemaining.Value != BeforeRemaining.Value)
                        yield return "cross-buff-remove-did-not-restore";
                    if (AfterAddBuffBAdditional != null && BeforeBuffBAdditional != null &&
                        AfterAddBuffBAdditional.Value > BeforeBuffBAdditional.Value)
                        yield return "cross-buff-b-count-rose";
                    if (AfterRemoveBuffBAdditional != null && BeforeBuffBAdditional != null &&
                        AfterRemoveBuffBAdditional.Value != BeforeBuffBAdditional.Value)
                        yield return "cross-buff-b-count-not-restored";
                }
                if (string.IsNullOrEmpty(BuffB) || string.Equals(BuffA, BuffB, StringComparison.Ordinal))
                    yield return "cross-buff-same-buff";
            }
            if (SharedEnhancementAttempted &&
                string.Equals(SharedEnhancementStatus, "proved", StringComparison.Ordinal))
            {
                if (BeforePoolRemaining == null || AfterFirstOn == null || AfterFirstOff == null)
                    yield return "enhancement-numbers-missing";
                else
                {
                    if (AfterFirstOn.Value != BeforePoolRemaining.Value - UsageUnitsPerCast)
                        yield return "enhancement-on-delta=" + AfterFirstOn.Value + "!=" +
                            (BeforePoolRemaining.Value - UsageUnitsPerCast);
                    if (AfterFirstOff.Value != BeforePoolRemaining.Value)
                        yield return "enhancement-off-did-not-restore";
                    if (AfterSecondOn != null)
                    {
                        // A blocked casting reserved nothing: its toggle moves
                        // the authored plan but not the pool. An affordable
                        // second casting consumes normally.
                        int expectedSecond = string.Equals(AtomicRefusalStatus, "observed",
                            StringComparison.Ordinal)
                            ? AfterFirstOn.Value
                            : BeforePoolRemaining.Value - 2 * UsageUnitsPerCast;
                        if (AfterSecondOn.Value != expectedSecond)
                            yield return "enhancement-second-casting-did-not-see-the-same-pool";
                    }
                }
                if (string.IsNullOrEmpty(SecondCastingId) || string.Equals(FirstCastingId, SecondCastingId,
                        StringComparison.Ordinal))
                    yield return "enhancement-same-casting";
            }
            if (!string.IsNullOrEmpty(AtomicRefusalStatus) &&
                string.Equals(AtomicRefusalStatus, "violated", StringComparison.Ordinal))
                yield return "atomic-refusal:" + AtomicRefusalEvidence;
        }

        public string Describe()
        {
            var text = new StringBuilder();
            text.Append("crossBuff=").Append(CrossBuffAttempted
                ? CrossBuffStatus : "not-attempted");
            if (CrossBuffAttempted && !string.IsNullOrEmpty(PoolKey))
                text.Append(";pool=").Append(PoolKey).Append('/').Append(PoolKind)
                    .Append(";A=").Append(BuffA).Append("/u").Append(ConsumedUnitsPerCastA)
                    .Append(";B=").Append(BuffB)
                    .Append(";remain=").Append(BeforeRemaining).Append('-')
                    .Append(AfterAddRemaining).Append('-').Append(AfterRemoveRemaining)
                    .Append(";bAdd=").Append(BeforeBuffBAdditional).Append('>')
                    .Append(AfterAddBuffBAdditional).Append('>')
                    .Append(AfterRemoveBuffBAdditional);
            text.Append(";enhancement=").Append(SharedEnhancementAttempted
                ? SharedEnhancementStatus : "not-attempted");
            if (SharedEnhancementAttempted && !string.IsNullOrEmpty(EnhancementId))
                text.Append(";e=").Append(EnhancementName).Append("/u").Append(UsageUnitsPerCast)
                    .Append(";pool=").Append(UsagePoolLabel)
                    .Append(";remain=").Append(BeforePoolRemaining).Append('-')
                    .Append(AfterFirstOn).Append('-').Append(AfterSecondOn).Append('-')
                    .Append(AfterFirstOff);
            if (!string.IsNullOrEmpty(AtomicRefusalStatus))
                text.Append(";atomic=").Append(AtomicRefusalStatus);
            foreach (string note in Notes)
                text.Append(';').Append(note);
            return text.ToString();
        }
    }
}
