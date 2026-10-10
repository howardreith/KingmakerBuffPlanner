using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public enum SpellbookReconciliationStatus
    {
        // Nothing was observed that could remove a casting (no membership
        // read, a run in progress, combat, unresolved stored data).
        Skipped,
        // Observed, and no saved casting depends on a removed spell.
        Unchanged,
        // One compound edit removed the dependent castings.
        Applied,
        // The authoring owner refused the edit (nothing changed).
        Refused
    }

    public sealed class SpellbookReconciliationOutcome
    {
        internal SpellbookReconciliationOutcome(SpellbookReconciliationStatus status,
            string reason, SpellbookReconciliationDecision decision,
            IEnumerable<string> removedCastingIds, string notice, bool durable)
        {
            Status = status;
            Reason = reason ?? string.Empty;
            Decision = decision;
            RemovedCastingIds = new ReadOnlyCollection<string>(
                (removedCastingIds ?? new string[0]).ToList());
            Notice = notice ?? string.Empty;
            Durable = durable;
        }

        public SpellbookReconciliationStatus Status { get; private set; }
        public string Reason { get; private set; }
        public SpellbookReconciliationDecision Decision { get; private set; }
        public IReadOnlyList<string> RemovedCastingIds { get; private set; }
        public string Notice { get; private set; }
        // The removal is on disk (false: kept in memory and for recovery,
        // and the run durability barrier refuses).
        public bool Durable { get; private set; }
        public bool Applied { get { return Status == SpellbookReconciliationStatus.Applied; } }

        internal static SpellbookReconciliationOutcome Skip(string reason)
        {
            return new SpellbookReconciliationOutcome(SpellbookReconciliationStatus.Skipped,
                reason, null, null, null, true);
        }
    }

    // 0.4.2 (B): a deliberate spellbook removal retires the saved castings
    // that depend on the removed spell, automatically, at an idle boundary
    // (planner open, and every run attempt before its gate). The decision
    // is the pure SpellbookRemovalReconciliation over native membership
    // facts; the edit is ONE authoring command (one history entry, one
    // autosave, one Undo); the stored plan is archived byte-exact before
    // that save. An Undo restores the castings and they stay until the same
    // book is observed with different contents (a new deliberate edit).
    public sealed partial class CastingWorkspaceSession
    {
        // Casting id -> the observation that removed it this session; a
        // casting back under the same observation was restored by Undo.
        private readonly Dictionary<string, string> _spellbookRemovedBy =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private int _spellbookReconciliationRevision = -1;
        private bool _spellbookArchivePending;

        // The last applied reconciliation's player notice (nonmodal, shown
        // in the planner footer until Undo or the next reconciliation).
        public string SpellbookReconciliationNotice { get; private set; }
        public SpellbookReconciliationOutcome LastSpellbookReconciliation { get; private set; }
        // Where the pre-reconciliation plan was archived, once it was.
        public string SpellbookReconciliationArchivePath { get; private set; }

        public SpellbookReconciliationOutcome ReconcileSpellbookRemovals(
            CastingWorkspaceInputs inputs)
        {
            SpellbookReconciliationOutcome outcome = ReconcileCore(inputs);
            LastSpellbookReconciliation = outcome;
            return outcome;
        }

        private SpellbookReconciliationOutcome ReconcileCore(CastingWorkspaceInputs inputs)
        {
            if (inputs == null || inputs.SpellbookMembership == null)
                return SpellbookReconciliationOutcome.Skip(
                    SpellbookRemovalReconciliation.MembershipUnknown);
            // Never during a run: its plan, decision and projection are
            // immutable once started.
            if (_submissionInFlight || inputs.RunActive)
                return SpellbookReconciliationOutcome.Skip("run-in-progress");
            if (inputs.CombatActive) return SpellbookReconciliationOutcome.Skip("combat");
            if (PersistenceBlocked || LegacyImportBlocked)
                return SpellbookReconciliationOutcome.Skip("stored-plan-unresolved");
            SpellbookReconciliationDecision decision = SpellbookRemovalReconciliation.Decide(
                _authoring.Document, inputs.SpellbookMembership,
                new ReadOnlyDictionary<string, string>(_spellbookRemovedBy));
            if (decision.Removals.Count == 0)
                return new SpellbookReconciliationOutcome(SpellbookReconciliationStatus.Unchanged,
                    string.Empty, decision, null, null, IntentIsDurable);
            List<string> ids = decision.Removals.Select(value => value.CastingId).ToList();
            string notice = ComposeRemovalNotice(decision.Removals, inputs);
            _spellbookArchivePending = true;
            AuthoringEditResult edit = _authoring.RemoveCastings(ids,
                "spellbook-removal:" + string.Join(",", ids.ToArray()));
            if (!edit.Applied)
            {
                _spellbookArchivePending = false;
                return new SpellbookReconciliationOutcome(SpellbookReconciliationStatus.Refused,
                    edit.Reason, decision, null, null, IntentIsDurable);
            }
            _spellbookReconciliationRevision = _authoring.CurrentRevision;
            foreach (SpellbookRemovalCandidate removal in decision.Removals)
                _spellbookRemovedBy[removal.CastingId] = removal.ObservationKey;
            if (EditingFocusCastingId != null && ids.Contains(EditingFocusCastingId))
                EditingFocusCastingId = null;
            bool durable = IntentIsDurable;
            SpellbookReconciliationNotice = durable ? notice + " Undo available."
                : notice + " Not saved yet (" + (_autosaveStatus ?? "unknown") +
                    "): nothing will be cast until the plan is saved. Undo available.";
            return new SpellbookReconciliationOutcome(SpellbookReconciliationStatus.Applied,
                string.Empty, decision, ids, SpellbookReconciliationNotice, durable);
        }

        // "Removed 2 Mind Blank castings: no longer prepared in Felix's and
        // Leinna's spellbooks." - the actual spell, reason and casters.
        private string ComposeRemovalNotice(IEnumerable<SpellbookRemovalCandidate> removals,
            CastingWorkspaceInputs inputs)
        {
            var parts = new List<string>();
            foreach (var group in removals.GroupBy(value =>
                    new { value.Ability.BaseAbilityGuid, value.Reason })
                .OrderBy(value => SpellName(value.Key.BaseAbilityGuid, inputs),
                    StringComparer.OrdinalIgnoreCase))
            {
                int count = group.Count();
                List<string> casters = group.Select(value => UnitDisplayName(inputs,
                        value.CasterUnitId)).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
                List<string> possessive = casters.Select(value => value + "'s").ToList();
                string owners = possessive.Count == 1 ? possessive[0] + " spellbook"
                    : string.Join(", ", possessive.Take(possessive.Count - 1).ToArray()) + " and " +
                        possessive[possessive.Count - 1] + " spellbooks";
                parts.Add("Removed " + count + " " + SpellName(group.Key.BaseAbilityGuid, inputs) +
                    (count == 1 ? " casting" : " castings") + ": no longer " +
                    (group.Key.Reason == SpellbookRemovalReason.NoLongerPrepared
                        ? "prepared" : "known") + " in " + owners);
            }
            return string.Join("; ", parts.ToArray()) + ".";
        }

        private static string SpellName(string baseAbilityGuid, CastingWorkspaceInputs inputs)
        {
            string name = inputs == null || inputs.SpellbookMembership == null ? null
                : inputs.SpellbookMembership.NameOf(baseAbilityGuid);
            return name ?? "removed spell";
        }

        // An Undo of the reconciliation edit restores the castings (planner
        // intent, not the native book); the notice no longer applies.
        private void NoteUndoForSpellbookReconciliation(int revisionBeforeUndo)
        {
            if (revisionBeforeUndo == _spellbookReconciliationRevision)
            {
                SpellbookReconciliationNotice = null;
                _spellbookReconciliationRevision = -1;
            }
        }

        // The exact stored plan before the reconciliation's save (SaveProfile).
        private void ArchiveBeforeSpellbookReconciliation()
        {
            if (!_spellbookArchivePending) return;
            SpellbookReconciliationArchivePath = _repository.ArchivePrimaryOnce(
                CampaignId, CastingPlanRepository.SpellbookRemovalArchiveLabel);
            _spellbookArchivePending = false;
        }
    }
}
