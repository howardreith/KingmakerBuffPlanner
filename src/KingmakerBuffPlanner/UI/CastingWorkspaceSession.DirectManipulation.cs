using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    public enum CastingGraphClickOutcome
    {
        Added,
        Focused,
        DisambiguationRequired,
        Removed,
        Retargeted,
        Recentred,
        ProviderChanged,
        DraftConfigured,
        Refused
    }

    public enum CastingGraphEditIntent
    {
        None,
        ChangeRecipient,
        ChangeProvider
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
                    Outcome != CastingGraphClickOutcome.DisambiguationRequired &&
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
        public CastingGraphEditIntent GraphEditIntent { get; private set; }

        public AuthoringEditResult BeginChangeRecipient()
        {
            if (FocusedCasting() == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            GraphEditIntent = CastingGraphEditIntent.ChangeRecipient;
            return AuthoringEditResult.Accept("change-recipient-armed", new string[0]);
        }

        public AuthoringEditResult BeginChangeProvider()
        {
            if (FocusedCasting() == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            GraphEditIntent = CastingGraphEditIntent.ChangeProvider;
            return AuthoringEditResult.Accept("change-provider-armed", new string[0]);
        }

        public void CancelGraphEditIntent()
        {
            GraphEditIntent = CastingGraphEditIntent.None;
        }

        // Normal recipient clicks continuously toggle assignments for the
        // displayed buff and routine. Focus is inspection only. Retargeting
        // is available solely through the explicit one-shot intent above.
        public CastingGraphClickResult ClickGraphRecipient(string unitId,
            CastingWorkspaceInputs inputs = null)
        {
            LeaveProblemNavigation();
            if (inputs != null) _lastInputs = inputs;
            if (string.IsNullOrEmpty(unitId))
                return Refused(AuthoringEditResult.Refuse("target-not-in-party:"));
            PlannedCasting focused = FocusedCasting();
            if (GraphEditIntent == CastingGraphEditIntent.ChangeRecipient)
            {
                if (focused == null)
                {
                    CancelGraphEditIntent();
                    return Refused(AuthoringEditResult.Refuse("no-editing-focus"));
                }
                bool editingGroup = focused.TargetMode != CastingTargetMode.DirectTarget;
                string editingRefusal = RecipientRefusal(focused, unitId, editingGroup);
                if (editingRefusal != null)
                    return Refused(AuthoringEditResult.Refuse(editingRefusal), focused.CastingId);
                AuthoringEditResult edit = RetargetFocusedCasting(unitId);
                if (!edit.Applied) return Refused(edit, focused.CastingId);
                CancelGraphEditIntent();
                return new CastingGraphClickResult(editingGroup
                    ? CastingGraphClickOutcome.Recentred
                    : CastingGraphClickOutcome.Retargeted, edit, focused.CastingId);
            }

            bool? group = IsGroupSource(_lastInputs, SelectedSourceId);
            if (group == null)
                return Refused(AuthoringEditResult.Refuse(
                    "ability-targeting-unsupported:" + (SelectedSourceId ?? string.Empty)));
            List<PlannedCasting> same = _authoring.Document.Castings.Where(casting =>
                    casting != null &&
                    string.Equals(casting.SourceId, SelectedSourceId, StringComparison.Ordinal) &&
                    string.Equals(casting.RoutineId, SelectedRoutineId, StringComparison.Ordinal) &&
                    MatchesSelectedVariant(casting, Draft.Ability) &&
                    (casting.TargetMode != CastingTargetMode.DirectTarget) == group.Value)
                .OrderBy(casting => casting.Order)
                .ThenBy(casting => casting.CastingId, StringComparer.Ordinal).ToList();
            List<PlannedCasting> centred = same.Where(casting =>
                string.Equals(RecipientOf(casting), unitId, StringComparison.Ordinal)).ToList();
            if (centred.Count > 1)
            {
                // Never choose one legacy duplicate arbitrarily. Both cards
                // remain visible in the graph so the player can explicitly
                // inspect one and use its Remove casting control.
                return new CastingGraphClickResult(
                    CastingGraphClickOutcome.DisambiguationRequired, null, null);
            }
            if (centred.Count == 1)
            {
                string removed = centred[0].CastingId;
                AuthoringEditResult removal = _authoring.RemoveCasting(removed);
                if (string.Equals(EditingFocusCastingId, removed, StringComparison.Ordinal))
                    EditingFocusCastingId = null;
                return removal.Applied
                    ? new CastingGraphClickResult(CastingGraphClickOutcome.Removed, removal, removed)
                    : Refused(removal);
            }
            if (group.Value && same.Count != 0 && _lastInputs != null)
            {
                ExplicitCastingPlan plan = Compile(_lastInputs, SelectedRoutineId, false);
                PlannedCasting covering = same.FirstOrDefault(casting =>
                {
                    ResolvedCasting resolved = plan.CastingById(casting.CastingId);
                    return resolved != null &&
                        resolved.PredictedBeneficiaryUnitIds.Contains(unitId);
                });
                if (covering != null)
                {
                    FocusGraphCasting(covering.CastingId);
                    return new CastingGraphClickResult(CastingGraphClickOutcome.Focused, null,
                        covering.CastingId);
                }
            }
            CastingGraphEditResult added = AddGraphCasting(unitId);
            if (added.ShowedExisting)
                return new CastingGraphClickResult(CastingGraphClickOutcome.Focused, null,
                    added.CastingId);
            return added.Applied
                ? new CastingGraphClickResult(CastingGraphClickOutcome.Added, added.Edit,
                    added.CastingId)
                : Refused(added.Edit);
        }

        // A caster header normally chooses the next casting's caster. Only
        // the explicit one-shot provider edit changes a focused casting; with
        // several exact sources, one source row must then be clicked.
        public CastingGraphClickResult ClickGraphCaster(string casterUnitId,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            PlannedCasting focused = FocusedCasting();
            if (GraphEditIntent != CastingGraphEditIntent.ChangeProvider || focused == null)
            {
                CancelGraphEditIntent();
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
            {
                // The caster casts it in more than one way: show that
                // caster's exact source rows (the graph draws rows for the
                // chosen caster only) so the next click on one changes the
                // provider. The focused casting is kept and unchanged.
                ChooseDraftCaster(casterUnitId);
                Draft.Ability = null;
                Draft.SpellbookGuid = null;
                _draftProviderKey = string.Empty;
                _resolvedDraftKey = string.Empty;
                return Refused(AuthoringEditResult.Refuse("exact-source-ambiguous:" + sources.Count),
                    focused.CastingId);
            }
            CastingGraphClickResult changed = ChangeFocusedProvider(focused, sources[0]);
            if (changed.Outcome == CastingGraphClickOutcome.ProviderChanged)
                CancelGraphEditIntent();
            return changed;
        }

        // An exact source row normally configures the next casting. Only the
        // explicit one-shot provider edit changes a focused casting.
        public CastingGraphClickResult ClickGraphSource(string providerKey,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            PlannedCasting focused = FocusedCasting();
            if (GraphEditIntent != CastingGraphEditIntent.ChangeProvider || focused == null)
            {
                CancelGraphEditIntent();
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
            CastingGraphClickResult changed = ChangeFocusedProvider(focused, option);
            if (changed.Outcome == CastingGraphClickOutcome.ProviderChanged)
                CancelGraphEditIntent();
            return changed;
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

        private static bool MatchesSelectedVariant(PlannedCasting casting, AbilityKey ability)
        {
            // SourceId is the normalized catalogue entry; AbilityKey keeps
            // variants/metamagic forms distinct inside that entry. Before an
            // exact source has been chosen there is no variant to narrow by.
            return ability == null || (casting.Ability != null &&
                string.Equals(casting.Ability.Canonical, ability.Canonical,
                    StringComparison.Ordinal));
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
                    MatchesSelectedVariant(value, focused.Ability) &&
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
