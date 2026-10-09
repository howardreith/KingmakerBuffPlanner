using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public enum CastingGraphClickOutcome
    {
        Added,
        Focused,
        Removed,
        Retargeted,
        Recentred,
        ProviderChanged,
        DraftConfigured,
        Refused
    }

    public sealed class CastingGraphClickResult
    {
        internal CastingGraphClickResult(CastingGraphClickOutcome outcome,
            AuthoringEditResult edit, string castingId)
        {
            Outcome = outcome;
            Edit = edit;
            CastingId = castingId;
        }

        public CastingGraphClickOutcome Outcome { get; private set; }
        // The authoring result of a mutation or refusal (null for a pure
        // focus or draft choice, which never edits the document).
        public AuthoringEditResult Edit { get; private set; }
        public string CastingId { get; private set; }
        public bool Changed
        {
            get
            {
                return Outcome != CastingGraphClickOutcome.Refused &&
                    Outcome != CastingGraphClickOutcome.Focused &&
                    Outcome != CastingGraphClickOutcome.DraftConfigured;
            }
        }
    }

    // WP3 (0.4.0) direct manipulation: the casting graph's portraits, caster
    // headers and source rows are the authoring gestures. Every mutation goes
    // through the canonical authoring service (one Undo step, autosaved) and
    // touches exactly one casting; focus and draft choices never edit the
    // document.
    public sealed partial class CastingWorkspaceSession
    {
        // A recipient portrait. With no focused casting: a recipient that
        // already has a casting of the selected buff in the selected routine
        // has it focused (the first in routine order when an earlier version
        // left duplicates), otherwise one casting is added and focused. With
        // a focused casting: its current recipient (direct target or group
        // centre) removes it; another legal recipient retargets (direct) or
        // recentres (group) it; an illegal one is refused and the casting is
        // kept unchanged.
        public CastingGraphClickResult ClickGraphRecipient(string unitId,
            CastingWorkspaceInputs inputs = null)
        {
            LeaveProblemNavigation();
            if (inputs != null) _lastInputs = inputs;
            if (string.IsNullOrEmpty(unitId))
                return Refused(AuthoringEditResult.Refuse("target-not-in-party:"));
            PlannedCasting focused = FocusedCasting();
            if (focused == null)
            {
                CastingGraphEditResult added = AddGraphCasting(unitId);
                if (added.ShowedExisting)
                    return new CastingGraphClickResult(CastingGraphClickOutcome.Focused, null,
                        added.CastingId);
                return added.Applied
                    ? new CastingGraphClickResult(CastingGraphClickOutcome.Added, added.Edit,
                        added.CastingId)
                    : Refused(added.Edit);
            }
            bool group = focused.TargetMode != CastingTargetMode.DirectTarget;
            if (string.Equals(RecipientOf(focused), unitId, StringComparison.Ordinal))
            {
                string removed = focused.CastingId;
                AuthoringEditResult removal = RemoveFocusedCasting();
                return removal.Applied
                    ? new CastingGraphClickResult(CastingGraphClickOutcome.Removed, removal, removed)
                    : Refused(removal);
            }
            string refusal = RecipientRefusal(focused, unitId, group);
            if (refusal != null) return Refused(AuthoringEditResult.Refuse(refusal), focused.CastingId);
            AuthoringEditResult edit = RetargetFocusedCasting(unitId);
            if (!edit.Applied) return Refused(edit, focused.CastingId);
            return new CastingGraphClickResult(group ? CastingGraphClickOutcome.Recentred
                : CastingGraphClickOutcome.Retargeted, edit, focused.CastingId);
        }

        // A caster header. With no focused casting it chooses the next
        // casting's caster (as before). With a focused casting it changes
        // that casting's provider when the caster has exactly one way to
        // cast the buff; with several, the exact source row must be clicked.
        public CastingGraphClickResult ClickGraphCaster(string casterUnitId,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            PlannedCasting focused = FocusedCasting();
            if (focused == null)
            {
                LeaveProblemNavigation();
                SelectGraphCaster(casterUnitId);
                return new CastingGraphClickResult(CastingGraphClickOutcome.DraftConfigured, null, null);
            }
            LeaveProblemNavigation();
            List<ProviderPlanningOption> sources = SourceOptionsFor(_lastInputs, focused.SourceId,
                casterUnitId);
            if (sources.Count == 0)
                return Refused(AuthoringEditResult.Refuse("caster-not-capable:" + casterUnitId),
                    focused.CastingId);
            if (sources.Count > 1)
                return Refused(AuthoringEditResult.Refuse("exact-source-ambiguous:" + sources.Count),
                    focused.CastingId);
            return ChangeFocusedProvider(focused, sources[0]);
        }

        // An exact source row (spellbook level, item or ability). With no
        // focused casting it configures the next casting (as before); with a
        // focused casting it changes that casting's provider.
        public CastingGraphClickResult ClickGraphSource(string providerKey,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            PlannedCasting focused = FocusedCasting();
            if (focused == null)
            {
                AuthoringEditResult draft = SelectGraphSource(providerKey);
                return draft.Applied
                    ? new CastingGraphClickResult(CastingGraphClickOutcome.DraftConfigured, draft, null)
                    : Refused(draft);
            }
            LeaveProblemNavigation();
            ProviderPlanningOption option = FindProviderOption(_lastInputs, focused.SourceId, providerKey);
            if (option == null)
                return Refused(AuthoringEditResult.Refuse("provider-unavailable:" + providerKey),
                    focused.CastingId);
            return ChangeFocusedProvider(focused, option);
        }

        // The provider change keeps target/origin, routine and order, policy,
        // the targeting modifiers and every enhancement the new caster has
        // (SetFocusedProvider names any it cannot take; Undo restores them).
        // A provider that cannot reach the casting's current recipient is
        // refused and the casting is kept.
        private CastingGraphClickResult ChangeFocusedProvider(PlannedCasting focused,
            ProviderPlanningOption option)
        {
            string refusal = ProviderReachRefusal(focused, option);
            if (refusal != null)
                return Refused(AuthoringEditResult.Refuse(refusal), focused.CastingId);
            AuthoringEditResult edit = SetFocusedProvider(option.Provider.Key.Canonical);
            if (!edit.Applied) return Refused(edit, focused.CastingId);
            return new CastingGraphClickResult(CastingGraphClickOutcome.ProviderChanged, edit,
                focused.CastingId);
        }

        private string ProviderReachRefusal(PlannedCasting focused, ProviderPlanningOption option)
        {
            if (focused.TargetMode == CastingTargetMode.DirectTarget)
            {
                if (string.IsNullOrEmpty(focused.DirectTargetUnitId)) return null;
                var prospective = focused.WithProvider(option.Provider.Key.CasterUnitId,
                    option.Provider.Key.Ability, NullIfEmpty(option.Provider.Key.SpellbookGuid),
                    focused.Enhancements);
                string modifierRefusal;
                ProviderPlanningOption effective = focused.TargetingModifiers.Any(value => value.Enabled)
                    ? ApplyGraphTargetingModifiers(focused.TargetingModifiers, _lastInputs, prospective,
                        option, out modifierRefusal)
                    : option;
                if (effective == null || !effective.ReachableTargetIds.Contains(focused.DirectTargetUnitId))
                    return "provider-cannot-reach-target:" + focused.DirectTargetUnitId;
                return null;
            }
            string centre = focused.TargetMode == CastingTargetMode.CasterCenteredOrigin
                ? option.Provider.Key.CasterUnitId
                : focused.Origin == null ? null : focused.Origin.AnchorUnitId;
            if (centre == null || !option.LegalAnchorIds.Contains(centre))
                return focused.TargetMode == CastingTargetMode.CasterCenteredOrigin
                    ? "origin-caster-illegal:" + centre : "origin-anchor-illegal:" + centre;
            return null;
        }

        // The recipient a portrait click compares with: the direct target, or
        // the group centre (the caster for a caster-centred casting).
        private static string RecipientOf(PlannedCasting casting)
        {
            if (casting.TargetMode == CastingTargetMode.DirectTarget) return casting.DirectTargetUnitId;
            if (casting.TargetMode == CastingTargetMode.CasterCenteredOrigin || casting.Origin == null ||
                casting.Origin.IsCasterCentered)
                return casting.CasterUnitId;
            return casting.Origin.AnchorUnitId;
        }

        // Why the focused casting may not move to this recipient, or null.
        private string RecipientRefusal(PlannedCasting focused, string unitId, bool group)
        {
            if (_lastInputs == null) return "draft-ability-unresolved:no-discovery-inputs";
            UnitSnapshot unit = _lastInputs.Snapshot.Units.FirstOrDefault(value =>
                string.Equals(value.UnitId, unitId, StringComparison.Ordinal));
            if (unit == null) return "target-not-in-party:" + unitId;
            ProviderPlanningOption option = focused.CasterUnitId == null ? null
                : FindRecordOption(_lastInputs, focused);
            if (option == null) return "caster-unresolved";
            // Another casting of this buff in this routine already belongs
            // to that recipient: moving this one there would double it.
            PlannedCasting other = _authoring.Document.Castings
                .Where(value => value != null && !string.Equals(value.CastingId, focused.CastingId,
                        StringComparison.Ordinal) &&
                    string.Equals(value.SourceId, focused.SourceId, StringComparison.Ordinal) &&
                    string.Equals(value.RoutineId, focused.RoutineId, StringComparison.Ordinal) &&
                    (value.TargetMode != CastingTargetMode.DirectTarget) == group &&
                    string.Equals(RecipientOf(value), unitId, StringComparison.Ordinal))
                .OrderBy(value => value.Order).FirstOrDefault();
            if (other != null) return "recipient-already-has-casting:" + other.CastingId;
            if (group)
            {
                if (!option.LegalAnchorIds.Contains(unitId))
                    return string.Equals(unitId, focused.CasterUnitId, StringComparison.Ordinal)
                        ? "origin-caster-illegal:" + unitId : "origin-anchor-illegal:" + unitId;
                return null;
            }
            PlannedCasting prospective = focused.WithTargeting(CastingTargetMode.DirectTarget, unitId,
                null, null);
            string modifierRefusal = null;
            ProviderPlanningOption effective = focused.TargetingModifiers.Any(value => value.Enabled)
                ? ApplyGraphTargetingModifiers(focused.TargetingModifiers, _lastInputs, prospective,
                    option, out modifierRefusal)
                : option;
            if (effective == null) return modifierRefusal ?? "target-unreachable:" + unitId;
            if (!effective.ReachableTargetIds.Contains(unitId)) return "target-unreachable:" + unitId;
            string invalid = InvalidTargetReason(unit);
            return invalid == null ? null : invalid + ":" + unitId;
        }

        private static CastingGraphClickResult Refused(AuthoringEditResult edit, string castingId = null)
        {
            return new CastingGraphClickResult(CastingGraphClickOutcome.Refused, edit, castingId);
        }
    }
}
