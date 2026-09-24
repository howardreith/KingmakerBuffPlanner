using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.UI
{
    // The game-world inputs one workspace refresh needs. In production these
    // come from the discovery/party/compatibility adapters; deterministic
    // fixtures supply them in integration tests. They are external game
    // boundaries, not substitutes for planner business services.
    public sealed class CastingWorkspaceInputs
    {
        public CastingWorkspaceInputs(
            PartyProviderSnapshot snapshot,
            IEnumerable<ProviderPlanningOption> providerOptions,
            IReadOnlyDictionary<string, EffectExpression> effectsBySource,
            IEnumerable<CastEnhancementSnapshot> enhancements,
            IEnumerable<ICastingTargetingModifier> targetingModifiers = null,
            ActiveEffectSnapshot liveEffects = null)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException("snapshot");
            LiveEffects = liveEffects;
            ProviderOptions = (providerOptions ?? new ProviderPlanningOption[0])
                .Where(value => value != null).ToList();
            EffectsBySource = effectsBySource ??
                throw new ArgumentNullException("effectsBySource");
            Enhancements = (enhancements ?? new CastEnhancementSnapshot[0])
                .Where(value => value != null).ToList();
            TargetingModifiers = (targetingModifiers ?? new ICastingTargetingModifier[0])
                .Where(value => value != null).ToList();
        }

        public PartyProviderSnapshot Snapshot { get; private set; }
        public IReadOnlyList<ProviderPlanningOption> ProviderOptions { get; private set; }
        public IReadOnlyDictionary<string, EffectExpression> EffectsBySource { get; private set; }
        public IReadOnlyList<CastEnhancementSnapshot> Enhancements { get; private set; }
        public IReadOnlyList<ICastingTargetingModifier> TargetingModifiers
        { get; private set; }
        // Live effects on the party (with instance detail in production);
        // null when unknown, which disables the live existing-effect skip.
        public ActiveEffectSnapshot LiveEffects { get; private set; }
    }

    // The boundary between the reviewed casting plan and native submission.
    // Production uses NativeCastingDispatchBoundary (the execution host runs
    // the exact projection through the existing executors); a runtime-test
    // session and the default constructor use DisabledCastingDispatchBoundary,
    // which records the attempted identity and refuses. Legacy expansion or
    // substitution paths never receive new-model intent.
    public interface ICastingDispatchBoundary
    {
        string DispositionReason { get; }

        // The boundary receives the EXACT approved projection (review K4):
        // a native adapter must execute these steps, identified by
        // projection.ProjectionId, never re-plan from the plan/decision.
        CastingDispatchOutcome Submit(
            ExplicitCastingPlan plan,
            CastingApplyDecision decision,
            string scopeRoutineId,
            ExplicitStepConversion projection);
    }

    public sealed class CastingDispatchOutcome
    {
        internal CastingDispatchOutcome(bool submitted, string reason,
            IReadOnlyList<string> castingIds)
        {
            Submitted = submitted;
            Reason = reason ?? string.Empty;
            CastingIds = castingIds;
        }

        public bool Submitted { get; private set; }
        public string Reason { get; private set; }
        public IReadOnlyList<string> CastingIds { get; private set; }
    }

    public sealed class DisabledCastingDispatchBoundary : ICastingDispatchBoundary
    {
        internal readonly List<string> RecordedSubmissions =
            new List<string>();
        internal ExplicitStepConversion LastProjection;
        private readonly string _reason;

        public DisabledCastingDispatchBoundary()
            : this(null)
        {
        }

        // A specific refusal (for example the runtime-test session lock);
        // the reason always starts with "native-submission-disabled".
        public DisabledCastingDispatchBoundary(string reason)
        {
            _reason = string.IsNullOrEmpty(reason) ||
                !reason.StartsWith("native-submission-disabled", StringComparison.Ordinal)
                    ? "native-submission-disabled:no-qualified-casting-first-executor"
                    : reason;
        }

        public string DispositionReason
        {
            get { return _reason; }
        }

        public CastingDispatchOutcome Submit(
            ExplicitCastingPlan plan,
            CastingApplyDecision decision,
            string scopeRoutineId,
            ExplicitStepConversion projection)
        {
            // Policy proof only: the exact reviewed identity is recorded and
            // refused. This is never a cast, never a gameplay result, and
            // never routes through a legacy expansion/substitution path.
            RecordedSubmissions.Add(
                (scopeRoutineId ?? "one-pass") + "|" +
                string.Join(",", decision.ExecutableCastingIds));
            LastProjection = projection;
            return new CastingDispatchOutcome(
                false, DispositionReason, decision.ExecutableCastingIds);
        }
    }

    public sealed class WorkspaceApplyResult
    {
        internal WorkspaceApplyResult(
            bool allowed,
            string reviewReason,
            CastingApplyDecision gateDecision,
            CastingDispatchOutcome dispatch,
            ExplicitStepConversion projection = null)
        {
            Allowed = allowed;
            ReviewReason = reviewReason ?? string.Empty;
            GateDecision = gateDecision;
            Dispatch = dispatch;
            Projection = projection;
        }

        // The exact executor steps an allowed decision projects to (one per
        // approved casting); null when the decision never reached
        // projection. Present even while native dispatch is disabled, so
        // the result can say precisely what WOULD run.
        public ExplicitStepConversion Projection { get; private set; }

        public bool Allowed { get; private set; }
        // Empty when the review coordinator permitted the submission.
        public string ReviewReason { get; private set; }
        public CastingApplyDecision GateDecision { get; private set; }
        public CastingDispatchOutcome Dispatch { get; private set; }
    }

    // The primary casting-first workspace session: one connected controller
    // over the real authoring service (the only mutation authority), the
    // shared compiler/forecast/gate services, candidate persistence, and the
    // review coordinator. Browsing and previewing never mutate the document
    // and never approve anything; every mutation is an explicit command with
    // a disclosed scope and Undo; Apply routes through presented-plan review
    // (per routine, restorable across sessions) and preflight on the
    // caller's inputs before the injected dispatch boundary.
    public sealed class CastingWorkspaceSession
    {
        private readonly CastingPlanRepository _repository;
        private readonly CastingExecutionGate _gate = new CastingExecutionGate();
        private CastingWorkspaceInputs _lastInputs;
        private string _savedIntentSignature = string.Empty;
        private string _resolvedDraftKey = string.Empty;
        private string _rememberedDirectRecipient = string.Empty;
        private readonly CastingForecastService _forecast =
            new CastingForecastService();
        private readonly CastingReviewCoordinator _review =
            new CastingReviewCoordinator();
        private readonly ICastingDispatchBoundary _dispatch;
        private readonly ExplicitCastingCompiler _compiler =
            new ExplicitCastingCompiler();
        private CastingAuthoringService _authoring;
        private bool _submissionInFlight;
        // Player settings stored beside the document in the candidate
        // profile; loaded with it and written back by Save (never reset).
        private UiProfile _uiSettings = UiProfile.Default();
        private ExecutionProfile _executionSettings = ExecutionProfile.Default();
        private readonly CastingReviewStore _reviewStore;

        public CastingWorkspaceSession(
            string modPath, string campaignId,
            ICastingDispatchBoundary dispatchBoundary = null,
            IDictionary<string, CastGroupingKind> legacyGroupings = null)
        {
            if (string.IsNullOrWhiteSpace(modPath))
                throw new ArgumentException("Absolute mod path is required.", "modPath");
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("Exact campaign ID is required.",
                    "campaignId");
            _repository = new CastingPlanRepository(modPath);
            _modPath = modPath;
            _legacyGroupings = legacyGroupings;
            _dispatch = dispatchBoundary ?? new DisabledCastingDispatchBoundary();
            CampaignId = campaignId;
            CastingPlanLoadResult loaded = _repository.Load(campaignId);
            LoadStatus = loaded.Status;
            LoadWarning = loaded.Warning;
            switch (loaded.Status)
            {
                case CastingPlanLoadStatus.Loaded:
                case CastingPlanLoadStatus.RecoveredFromBackup:
                    _authoring = new CastingAuthoringService(
                        loaded.Profile.ToDocument());
                    AdoptSettings(loaded.Profile);
                    break;
                case CastingPlanLoadStatus.Absent:
                    // First open in this campaign: import the legacy
                    // (schema-5) plan through the migration boundary —
                    // exact original archived, legacy file untouched,
                    // candidate written and reopened — so the player's
                    // existing buffs are not silently lost (charter §7).
                    _authoring = new CastingAuthoringService(
                        MigrateLegacyOrEmpty(modPath, campaignId, legacyGroupings));
                    break;
                default:
                    // Corrupt or unsupported candidate data is never
                    // overwritten by this session: editing starts from an
                    // empty in-memory document and saving stays refused
                    // until the operator resolves the stored bytes.
                    PersistenceBlocked = true;
                    _authoring = new CastingAuthoringService(NewDocument());
                    break;
            }
            // The dirty baseline covers every load state — an absent or
            // blocked candidate starts exactly as clean as a loaded one.
            _savedIntentSignature = DocumentIntentSignature();
            SelectedRoutineId = "long";
            // Accepted review state from an earlier session: it authorizes
            // only contents whose digest still matches exactly.
            _reviewStore = Path.IsPathRooted(modPath)
                ? new CastingReviewStore(modPath) : null;
            if (_reviewStore != null)
            {
                CastingReviewStoreLoad review = _reviewStore.Load(campaignId);
                ReviewStoreWarning = review.Warning;
                foreach (KeyValuePair<string, string> pair in review.AcceptedDigests)
                    _review.RestoreAccepted(pair.Key, pair.Value);
            }
        }

        // Persisted review state problems (unreadable file, failed write);
        // review then simply requires a fresh acceptance.
        public string ReviewStoreWarning { get; private set; }

        private void AdoptSettings(CastingPlanProfile profile)
        {
            _uiSettings = profile == null || profile.Ui == null
                ? UiProfile.Default()
                : new UiProfile { Scale = profile.Ui.Scale, Hotkey = profile.Ui.Hotkey };
            _executionSettings = profile == null || profile.Execution == null
                ? ExecutionProfile.Default()
                : CopyOf(profile.Execution);
        }

        private static ExecutionProfile CopyOf(ExecutionProfile value)
        {
            return new ExecutionProfile
            {
                Mode = value.Mode,
                AllowAnimatedFallback = value.AllowAnimatedFallback,
                OutOfCombatOnly = value.OutOfCombatOnly,
                RecastExisting = value.RecastExisting
            };
        }

        // The execution settings a run uses (a copy; edits go through the
        // setters below and are saved with the document).
        public ExecutionProfile ExecutionSettings
        {
            get { return CopyOf(_executionSettings); }
        }

        public string ExecutionMode
        {
            get { return _executionSettings.Mode; }
        }

        public bool AllowAnimatedFallback
        {
            get { return _executionSettings.AllowAnimatedFallback; }
        }

        // "animated" (native casting animations, the default) or "instant".
        public void SetExecutionMode(string mode)
        {
            if (mode != "animated" && mode != "instant")
                throw new ArgumentException("Unknown execution mode.", "mode");
            ExecutionProfile next = CopyOf(_executionSettings);
            next.Mode = mode;
            _executionSettings = next;
        }

        public void SetAllowAnimatedFallback(bool allow)
        {
            ExecutionProfile next = CopyOf(_executionSettings);
            next.AllowAnimatedFallback = allow;
            _executionSettings = next;
        }

        public string CampaignId { get; private set; }
        public CastingPlanLoadStatus LoadStatus { get; private set; }
        public string LoadWarning { get; private set; }
        public bool PersistenceBlocked { get; private set; }

        // Browsing selection (focus only — never a mutation).
        public string SelectedSourceId { get; private set; }
        public string SelectedRoutineId { get; private set; }
        public string SelectedCasterUnitId { get; private set; }
        public string EditingFocusCastingId { get; private set; }

        // Draft defaults for the NEXT casting only; changes never
        // retroactively edit existing castings.
        public CastingDraft Draft { get; } = new CastingDraft();

        public CastingPlanDocument Document
        {
            get { return _authoring.Document; }
        }

        public bool CanUndo
        {
            get { return _authoring.CanUndo; }
        }

        public string DispatchDisposition
        {
            get { return _dispatch.DispositionReason; }
        }

        // ------------------------------------------------------------------
        // Browsing (pure)
        // ------------------------------------------------------------------

        public void SelectBuff(string sourceId)
        {
            string resolved = string.IsNullOrWhiteSpace(sourceId)
                ? string.Empty : sourceId;
            if (!string.Equals(resolved, SelectedSourceId,
                    StringComparison.Ordinal))
                // A stale ability under a different displayed source must
                // never survive selection (review F1).
                Draft.Ability = null;
            SelectedSourceId = resolved;
        }

        public void SelectRoutine(string routineId)
        {
            if (_authoring.Document.Routines.All(
                    value => value.RoutineId != routineId))
                throw new ArgumentException("Unknown routine.", "routineId");
            SelectedRoutineId = routineId;
        }

        // Selecting another caster changes editing focus only — existing
        // assignments and connections are untouched.
        public void SelectCaster(string casterUnitId)
        {
            string resolved = string.IsNullOrWhiteSpace(casterUnitId)
                ? null : casterUnitId;
            if (!string.Equals(resolved, SelectedCasterUnitId,
                    StringComparison.Ordinal))
                Draft.Ability = null;
            SelectedCasterUnitId = resolved;
        }

        // The caster lane's primary action: use this caster for the NEXT
        // casting (draft) and focus it. Never edits an existing casting.
        public void ChooseDraftCaster(string casterUnitId)
        {
            SelectCaster(casterUnitId);
            Draft.CasterUnitId = SelectedCasterUnitId;
        }

        private static string SourceKindName(SourceKind kind)
        {
            switch (kind)
            {
                case SourceKind.Spellbook: return "spell";
                case SourceKind.AbilityResource: return "ability";
                case SourceKind.Item: return "item";
                default: return "feature";
            }
        }

        private static string UnitDisplayName(CastingWorkspaceInputs inputs,
            string unitId)
        {
            if (string.IsNullOrEmpty(unitId) || inputs == null ||
                inputs.Snapshot == null) return unitId;
            foreach (var unit in inputs.Snapshot.Units)
                if (unit != null && string.Equals(unit.UnitId, unitId,
                        StringComparison.Ordinal))
                    return string.IsNullOrWhiteSpace(unit.DisplayName)
                        ? unitId : unit.DisplayName;
            return unitId;
        }

        // Legacy import outcome on first open (null when a candidate already
        // existed). The report counts are shown to the player; import is
        // not execution readiness — imported drafts still need review.
        public CastingMigrationStatus? MigrationStatus { get; private set; }

        // Review K1: a legacy plan that exists but could not be imported
        // (unreadable, newer schema, unusable candidate, importer/archive
        // failure) BLOCKS the workspace: no empty replacement is saved and
        // Apply is refused, so missing work is never presented as absent.
        // Reload retries the import once the legacy file is repaired.
        public bool LegacyImportBlocked { get; private set; }
        public string LegacyImportBlockReason { get; private set; }
        private readonly string _modPath;
        private readonly IDictionary<string, CastGroupingKind> _legacyGroupings;

        private CastingPlanDocument BlockLegacyImport(string reason)
        {
            LegacyImportBlocked = true;
            LegacyImportBlockReason = reason ?? "unknown";
            PersistenceBlocked = true;
            return NewDocument();
        }
        public CastingImportReport ImportReport { get; private set; }
        public string MigrationWarning { get; private set; }

        private CastingPlanDocument MigrateLegacyOrEmpty(string modPath,
            string campaignId, IDictionary<string, CastGroupingKind> groupings)
        {
            if (!Path.IsPathRooted(modPath)) return NewDocument();
            try
            {
                CastingMigrationResult migration = new CastingPlanMigrationService(modPath)
                    .Migrate(campaignId, groupings);
                MigrationStatus = migration.Status;
                MigrationWarning = migration.Warning;
                switch (migration.Status)
                {
                    case CastingMigrationStatus.Migrated:
                        CastingPlanLoadResult reloaded = _repository.Load(campaignId);
                        if (reloaded.Status == CastingPlanLoadStatus.Loaded)
                        {
                            ImportReport = migration.ImportReport;
                            LoadStatus = reloaded.Status;
                            LoadWarning = reloaded.Warning;
                            // The legacy execution/UI settings travel with
                            // the import (the migration wrote them).
                            AdoptSettings(reloaded.Profile);
                            return reloaded.Profile.ToDocument();
                        }
                        MigrationWarning = "migrated-candidate-did-not-reopen:" +
                            reloaded.Status;
                        return BlockLegacyImport(MigrationWarning);
                    case CastingMigrationStatus.LegacyAbsent:
                        // Genuinely no previous plan: an empty, writable
                        // document is the correct start.
                        return NewDocument();
                    default:
                        // LegacyUnreadable / CandidateUnusable /
                        // NewerCandidateRefused: the old plan exists but is
                        // not represented — block, never replace it.
                        return BlockLegacyImport(migration.Status + ":" +
                            (migration.Warning ?? string.Empty));
                }
            }
            catch (Exception exception)
            {
                MigrationWarning = "migration-exception:" + exception.GetType().Name +
                    ":" + exception.Message;
                return BlockLegacyImport(MigrationWarning);
            }
        }

        // Explicit acknowledgement of plan-wide legacy import notices;
        // undoable, and saved like any other edit.
        public AuthoringEditResult AcknowledgeImportNotices()
        {
            return _authoring.AcknowledgeImportNotices();
        }

        public IReadOnlyList<string> PendingImportNotices
        {
            get { return _authoring.Document.PendingImportNotices; }
        }

        // Review L1: explicit resolution of the focused imported casting's
        // review items (undoable; discloses the legacy constraints).
        public AuthoringEditResult ResolveFocusedImportReview()
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            return _authoring.ResolveImportReview(EditingFocusCastingId);
        }

        public void FocusCasting(string castingId)
        {
            if (castingId != null &&
                _authoring.Document.Castings.All(
                    value => value.CastingId != castingId))
                throw new ArgumentException("Unknown casting.", "castingId");
            EditingFocusCastingId = castingId;
        }

        // ------------------------------------------------------------------
        // Read models
        // ------------------------------------------------------------------

        // Compiles the whole-document plan (calculated state only — this
        // never presents or approves anything).
        public ExplicitCastingPlan CompilePlan(CastingWorkspaceInputs inputs)
        {
            return Compile(inputs, null, false);
        }

        public WorkspaceView BuildView(CastingWorkspaceInputs inputs)
        {
            // The selected-run plan reserves resources for the SELECTED
            // routine only; the explicitly labeled one-pass forecast is a
            // separate whole-document construction with effect projection
            // (review R3).
            ExplicitCastingPlan plan = Compile(inputs, SelectedRoutineId, false);
            RefreshPoolLabels(inputs);
            // The buff shown selected is the one the draft and Add use (review
            // of 1332ed8..542cd66, P2-2): the first buff is committed as the
            // selection, not only highlighted.
            if (string.IsNullOrEmpty(SelectedSourceId))
            {
                string first = FirstSourceId(inputs);
                if (!string.IsNullOrEmpty(first)) SelectedSourceId = first;
            }
            string selectedSource = SelectedSourceId ?? string.Empty;
            var casters = BuildCasterRows(plan, inputs, selectedSource);
            var cards = BuildCards(plan, selectedSource);
            var namesByUnit = inputs.Snapshot.Units.ToDictionary(
                unit => unit.UnitId,
                unit => string.IsNullOrEmpty(unit.DisplayName)
                    ? unit.UnitId : unit.DisplayName,
                StringComparer.Ordinal);
            foreach (WorkspaceCastingCard card in cards)
                ApplyNames(card, namesByUnit);
            var budget = plan.BudgetLines
                .Select(line => new WorkspaceBudgetRow(line, BudgetLabel(line))).ToList();
            WorkspaceBudgetRow.DisambiguateLabels(budget);
            CastingApplyDecision selectedGate = _gate.Evaluate(
                plan, CastingApplyMode.Ordinary, SelectedRoutineId);
            CastingForecast onePass = _forecast.ForecastOnePass(
                _authoring.Document, inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements,
                inputs.TargetingModifiers);
            CastingApplyDecision onePassGate = _gate.Evaluate(
                onePass.Plan, CastingApplyMode.Ordinary);
            WorkspaceEditingScope scope = EditingFocusCastingId == null
                ? WorkspaceEditingScope.ConfigureNextCasting
                : WorkspaceEditingScope.EditingSingleCasting;
            // The casting as the player reads it (its place in the routine,
            // who casts it on whom), never its internal id; found in the whole
            // plan, not only among the selected buff's cards (review of
            // f7726c9..1332ed8, P3-F).
            WorkspaceCastingCard focusedCard = EditingFocusCastingId == null ? null
                : cards.FirstOrDefault(value => string.Equals(value.CastingId,
                    EditingFocusCastingId, StringComparison.Ordinal));
            if (EditingFocusCastingId != null && focusedCard == null)
            {
                ResolvedCasting focused = plan.Castings.FirstOrDefault(value => string.Equals(
                    value.CastingId, EditingFocusCastingId, StringComparison.Ordinal));
                focusedCard = focused == null ? null : BuildCards(plan, focused.SourceId, false)
                    .FirstOrDefault(value => string.Equals(value.CastingId,
                        EditingFocusCastingId, StringComparison.Ordinal));
                if (focusedCard != null) ApplyNames(focusedCard, namesByUnit);
            }
            string scopeLabel = EditingFocusCastingId == null
                ? "Configure next casting"
                : focusedCard == null ? "Editing one casting"
                : "Editing casting " + (focusedCard.Order + 1) + ": " + focusedCard.Headline;
            var view = new WorkspaceView(
                selectedSource, SelectedRoutineId, casters, cards, budget,
                _authoring.Document.Routines.Select(value => value.RoutineId)
                    .ToList(),
                selectedGate, onePassGate, _review.StatusFor(SelectedRoutineId),
                scope, scopeLabel,
                plan.Diagnostics,
                BuildDraftView(inputs, selectedSource, casters));
            List<ResolvedCasting> routineCastings = plan.Castings.Where(value => string.Equals(
                value.RoutineId, SelectedRoutineId, StringComparison.Ordinal)).ToList();
            view.RoutineCastingCount = routineCastings.Count;
            view.RoutineReadyCount = routineCastings.Count(value =>
                value.Readiness == ResolvedCastingReadiness.Ready);
            BuildFocusedEnhancements(view, inputs);
            foreach (WorkspaceOriginOption origin in BuildFocusedOrigins(inputs))
                view._focusedOrigins.Add(origin);
            // Final review B2/B3: the provider pickers.
            PlannedCasting focusedRecord = FocusedCasting();
            if (focusedRecord != null)
                view._focusedProviders.AddRange(ProviderChoices(inputs, focusedRecord.SourceId,
                    null, focusedRecord.CasterUnitId, focusedRecord.Ability,
                    focusedRecord.SpellbookGuid));
            string providerDraftCaster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            if (!string.IsNullOrEmpty(providerDraftCaster))
                view._draftProviders.AddRange(ProviderChoices(inputs,
                    string.IsNullOrEmpty(Draft.SourceId) ? selectedSource : Draft.SourceId,
                    providerDraftCaster, providerDraftCaster, Draft.Ability, Draft.SpellbookGuid));
            view._selectedBuffCards.AddRange(ShowWholeRoutine
                ? BuildCards(plan, selectedSource, false)
                : cards);
            return view;
        }

        // Origin options for the FOCUSED record, derived from THAT
        // record's own provider option — never the next-casting draft's
        // (review H2d).
        internal List<WorkspaceOriginOption> BuildFocusedOrigins(
            CastingWorkspaceInputs inputs)
        {
            var origins = new List<WorkspaceOriginOption>();
            if (EditingFocusCastingId == null || inputs == null) return origins;
            PlannedCasting focused = _authoring.Document.Castings
                .FirstOrDefault(value => value != null && string.Equals(
                    value.CastingId, EditingFocusCastingId,
                    StringComparison.Ordinal));
            if (focused == null || string.IsNullOrEmpty(focused.CasterUnitId))
                return origins;
            ProviderPlanningOption option = FindRecordOption(inputs, focused);
            if (option == null) return origins;
            foreach (string anchor in option.LegalAnchorIds)
                origins.Add(new WorkspaceOriginOption(anchor,
                    focused.Origin != null && !focused.Origin.IsCasterCentered &&
                    string.Equals(focused.Origin.AnchorUnitId, anchor,
                        StringComparison.Ordinal)));
            return origins;
        }

        // Enhancement options for the FOCUSED record, derived from that
        // record's own caster and ability — not the draft's (review G1).
        private void BuildFocusedEnhancements(
            WorkspaceView view, CastingWorkspaceInputs inputs)
        {
            if (EditingFocusCastingId == null ||
                inputs == null || inputs.Enhancements == null) return;
            PlannedCasting focused = _authoring.Document.Castings
                .FirstOrDefault(value => value != null && string.Equals(
                    value.CastingId, EditingFocusCastingId,
                    StringComparison.Ordinal));
            if (focused == null || focused.Ability == null ||
                string.IsNullOrEmpty(focused.CasterUnitId)) return;
            string baseGuid = focused.Ability.BaseAbilityGuid;
            string variantGuid = focused.Ability.VariantGuid;
            foreach (CastEnhancementSnapshot enhancement in inputs.Enhancements)
            {
                if (enhancement == null ||
                    !string.Equals(enhancement.CasterUnitId,
                        focused.CasterUnitId, StringComparison.Ordinal))
                    continue;
                bool qualified = enhancement.AbilityWhiteList.Count == 0 ||
                    enhancement.AbilityWhiteList.Contains(baseGuid) ||
                    enhancement.AbilityWhiteList.Contains(variantGuid);
                if (!qualified) continue;
                view._focusedEnhancements.Add(new WorkspaceEnhancementOption(
                    enhancement.EnhancementId, enhancement.DisplayName,
                    focused.Enhancements.Any(selection => selection != null &&
                        string.Equals(selection.EnhancementId,
                            enhancement.EnhancementId,
                            StringComparison.Ordinal))));
            }
        }

        // The buff grid's sources in the order a player reads them:
        // alphabetical by display name, then the disambiguating detail, then
        // the source id (the classic catalogue's order; the id order looked
        // arbitrary in live frame casting-ws-qual-20260923-r1-01).
        private List<WorkspaceSourceOption> BuildSourceOptions(
            CastingWorkspaceInputs inputs, string selectedSource)
        {
            var sources = new List<WorkspaceSourceOption>();
            if (inputs.EffectsBySource != null && inputs.ProviderOptions != null)
            {
                var sourceIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (ProviderPlanningOption option in inputs.ProviderOptions)
                {
                    if (option == null || option.Provider == null) continue;
                    AbilityKey ability = option.Provider.Key.Ability;
                    EffectExpression abilityExpression = null;
                    if (ability != null && inputs.EffectsBySource.TryGetValue(
                            ability.Canonical, out abilityExpression))
                    {
                        foreach (KeyValuePair<string, EffectExpression> pair in
                            inputs.EffectsBySource)
                            if (!string.Equals(pair.Key, ability.Canonical,
                                    StringComparison.Ordinal) &&
                                ReferenceEquals(pair.Value, abilityExpression))
                                sourceIds.Add(pair.Key);
                    }
                }
                var descriptors = new List<WorkspaceSourceDescriptor>();
                var displays = new Dictionary<string, string>(StringComparer.Ordinal);
                var icons = new Dictionary<string, AbilityKey>(StringComparer.Ordinal);
                var kinds = new Dictionary<string, List<SourceKind>>(StringComparer.Ordinal);
                foreach (string sourceId in sourceIds.OrderBy(
                         value => value, StringComparer.Ordinal))
                {
                    // The label comes from THIS source's own providers only
                    // (review F5): the first nonempty SourceDisplayName of
                    // the whole list would give every buff the same name.
                    EffectExpression labelExpression;
                    string display = inputs.EffectsBySource.TryGetValue(
                            sourceId, out labelExpression)
                        ? inputs.ProviderOptions
                            .Where(value => value != null &&
                                value.Provider != null &&
                                OptionServesExpression(inputs, value,
                                    labelExpression))
                            .Select(value => value.Provider.SourceDisplayName)
                            .FirstOrDefault(name =>
                                !string.IsNullOrWhiteSpace(name))
                        : null;
                    List<ProviderPlanningOption> serving = labelExpression == null
                        ? new List<ProviderPlanningOption>()
                        : inputs.ProviderOptions.Where(value => value != null &&
                            value.Provider != null &&
                            OptionServesExpression(inputs, value, labelExpression))
                            .ToList();
                    displays[sourceId] = display;
                    kinds[sourceId] = serving
                        .Select(value => value.Provider.Key.Ability.SourceKind).ToList();
                    ProviderPlanningOption iconOption = serving
                        .OrderBy(value => value.Provider.VariantOrder)
                        .FirstOrDefault();
                    if (iconOption != null)
                        icons[sourceId] = iconOption.Provider.Key.Ability;
                    descriptors.Add(new WorkspaceSourceDescriptor(sourceId,
                        string.IsNullOrWhiteSpace(display) ? sourceId : display,
                        serving.Select(value => value.Provider.DisplayName),
                        serving.Select(value => SourceKindName(
                            value.Provider.Key.Ability.SourceKind)),
                        serving.Select(value => UnitDisplayName(inputs,
                            value.Provider.Key.CasterUnitId))));
                }
                IReadOnlyDictionary<string, string> details =
                    WorkspaceSourceLabels.Details(descriptors);
                foreach (WorkspaceSourceDescriptor descriptor in descriptors)
                {
                    string detail;
                    details.TryGetValue(descriptor.SourceId, out detail);
                    AbilityKey icon;
                    icons.TryGetValue(descriptor.SourceId, out icon);
                    sources.Add(new WorkspaceSourceOption(
                        descriptor.SourceId, displays[descriptor.SourceId],
                        string.Equals(descriptor.SourceId, selectedSource,
                            StringComparison.Ordinal), detail, icon,
                        kinds[descriptor.SourceId]));
                }
            }
            return WorkspaceSourceLabels.GridOrder(sources);
        }

        private WorkspaceDraftView BuildDraftView(
            CastingWorkspaceInputs inputs, string selectedSource,
            List<WorkspaceCasterRow> casters)
        {
            List<WorkspaceSourceOption> sources = inputs == null
                ? new List<WorkspaceSourceOption>() : BuildSourceOptions(inputs, selectedSource);
            string draftSource = string.IsNullOrEmpty(Draft.SourceId)
                ? selectedSource : Draft.SourceId;
            string draftCaster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            int draftCandidates;
            ProviderPlanningOption draftOption = FindDraftOption(
                inputs, draftSource, draftCaster, out draftCandidates);
            var targets = inputs.Snapshot.Units
                .Select(unit => new WorkspaceTargetOption(
                    unit.UnitId, unit.DisplayName,
                    string.Equals(unit.UnitId, Draft.DirectTargetUnitId,
                        StringComparison.Ordinal),
                    draftOption == null ? (bool?)null
                        : draftOption.ReachableTargetIds.Contains(unit.UnitId)))
                .ToList();
            var origins = new List<WorkspaceOriginOption>();
            if (draftOption != null)
                foreach (string anchor in draftOption.LegalAnchorIds)
                    origins.Add(new WorkspaceOriginOption(anchor,
                        Draft.Origin != null && !Draft.Origin.IsCasterCentered &&
                        string.Equals(Draft.Origin.AnchorUnitId, anchor,
                            StringComparison.Ordinal)));
            var enhancements = new List<WorkspaceEnhancementOption>();
            AbilityKey enhancementAbility = Draft.Ability ??
                DraftAbilityFor(inputs);
            if (inputs.Enhancements != null && enhancementAbility != null &&
                !string.IsNullOrEmpty(draftCaster))
            {
                string baseGuid = enhancementAbility.BaseAbilityGuid;
                string variantGuid = enhancementAbility.VariantGuid;
                foreach (CastEnhancementSnapshot enhancement in inputs.Enhancements)
                {
                    if (enhancement == null ||
                        !string.Equals(enhancement.CasterUnitId, draftCaster,
                            StringComparison.Ordinal)) continue;
                    bool qualified = enhancement.AbilityWhiteList.Count == 0 ||
                        enhancement.AbilityWhiteList.Contains(baseGuid) ||
                        enhancement.AbilityWhiteList.Contains(variantGuid);
                    if (!qualified) continue;
                    enhancements.Add(new WorkspaceEnhancementOption(
                        enhancement.EnhancementId, enhancement.DisplayName,
                        Draft.Enhancements.Any(selection => selection != null &&
                            string.Equals(selection.EnhancementId,
                                enhancement.EnhancementId,
                                StringComparison.Ordinal))));
                }
            }
            return new WorkspaceDraftView(
                draftSource,
                draftCaster ?? string.Empty,
                Draft.TargetMode,
                Draft.DirectTargetUnitId ?? string.Empty,
                Draft.Origin == null || Draft.Origin.IsCasterCentered
                    ? string.Empty : Draft.Origin.AnchorUnitId,
                _rememberedDirectRecipient,
                Draft.State,
                sources,
                casters.Where(row => row.Capable).ToList(),
                targets, origins, enhancements);
        }

        // The spellbook the draft's current (source, caster) pair resolves
        // to — the exact expected authored spellbook for Add assertions.
        public string DraftSpellbookFor(CastingWorkspaceInputs inputs)
        {
            if (inputs == null) return null;
            string draftSource = string.IsNullOrEmpty(Draft.SourceId)
                ? SelectedSourceId : Draft.SourceId;
            int candidates;
            ProviderPlanningOption option = FindDraftOption(
                inputs, draftSource,
                Draft.CasterUnitId ?? SelectedCasterUnitId, out candidates);
            return candidates == 1 && option != null
                ? option.Provider.Key.SpellbookGuid : null;
        }

        // Resolves the exact discovered provider option serving the draft's
        // current source and caster (the ability the Add control authors).
        public AbilityKey DraftAbilityFor(CastingWorkspaceInputs inputs)
        {
            if (inputs == null) return null;
            string draftSource = string.IsNullOrEmpty(Draft.SourceId)
                ? SelectedSourceId : Draft.SourceId;
            int candidates;
            ProviderPlanningOption option = FindDraftOption(
                inputs, draftSource,
                Draft.CasterUnitId ?? SelectedCasterUnitId, out candidates);
            return candidates == 1 && option != null
                ? option.Provider.Key.Ability : null;
        }

        private ProviderPlanningOption FindDraftOption(
            CastingWorkspaceInputs inputs, string draftSource, string draftCaster,
            out int candidateCount)
        {
            candidateCount = 0;
            if (inputs.ProviderOptions == null ||
                inputs.EffectsBySource == null ||
                string.IsNullOrEmpty(draftCaster)) return null;
            EffectExpression sourceExpression;
            if (!inputs.EffectsBySource.TryGetValue(
                    draftSource, out sourceExpression)) return null;
            ProviderPlanningOption match = null;
            foreach (ProviderPlanningOption option in inputs.ProviderOptions)
            {
                if (option == null || option.Provider == null ||
                    !string.Equals(option.Provider.Key.CasterUnitId,
                        draftCaster, StringComparison.Ordinal) ||
                    !OptionServesExpression(inputs, option, sourceExpression))
                    continue;
                candidateCount++;
                if (match == null ||
                    string.Compare(option.Provider.Key.Canonical,
                        match.Provider.Key.Canonical,
                        StringComparison.Ordinal) < 0)
                    match = option;
            }
            return match;
        }

        // The record's own provider: its caster, ability and spellbook when
        // they single one out (final review B3: a caster with several ways
        // to cast the buff), else the draft rule's choice.
        private ProviderPlanningOption FindRecordOption(CastingWorkspaceInputs inputs,
            PlannedCasting record)
        {
            int candidates;
            ProviderPlanningOption fallback = FindDraftOption(
                inputs, record.SourceId, record.CasterUnitId, out candidates);
            if (candidates <= 1 || record.Ability == null) return fallback;
            ProviderPlanningOption exact = inputs.ProviderOptions.FirstOrDefault(option =>
                option != null && option.Provider != null &&
                string.Equals(option.Provider.Key.CasterUnitId, record.CasterUnitId,
                    StringComparison.Ordinal) &&
                string.Equals(option.Provider.Key.Ability.Canonical, record.Ability.Canonical,
                    StringComparison.Ordinal) &&
                (record.SpellbookGuid == null || string.Equals(option.Provider.Key.SpellbookGuid,
                    record.SpellbookGuid, StringComparison.Ordinal)));
            return exact ?? fallback;
        }

        // Final review B2/B3: every exact provider that can cast the given
        // buff - each capable caster, and each spellbook level, item or
        // ability of a caster that has several. onlyCaster narrows the list
        // to one caster (the next casting's chosen caster).
        internal List<WorkspaceProviderChoice> ProviderChoices(CastingWorkspaceInputs inputs,
            string sourceId, string onlyCaster, string selectedCaster, AbilityKey selectedAbility,
            string selectedSpellbook)
        {
            var choices = new List<WorkspaceProviderChoice>();
            EffectExpression expression;
            if (inputs == null || inputs.ProviderOptions == null || inputs.EffectsBySource == null ||
                string.IsNullOrEmpty(sourceId) ||
                !inputs.EffectsBySource.TryGetValue(sourceId, out expression))
                return choices;
            List<ProviderPlanningOption> options = inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null &&
                    (onlyCaster == null || string.Equals(value.Provider.Key.CasterUnitId,
                        onlyCaster, StringComparison.Ordinal)) &&
                    OptionServesExpression(inputs, value, expression))
                .OrderBy(value => UnitDisplayName(inputs, value.Provider.Key.CasterUnitId),
                    StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal)
                .ToList();
            foreach (ProviderPlanningOption option in options)
            {
                ProviderKey key = option.Provider.Key;
                bool sameAbility = selectedAbility != null && string.Equals(key.CasterUnitId,
                        selectedCaster, StringComparison.Ordinal) &&
                    string.Equals(key.Ability.Canonical, selectedAbility.Canonical,
                        StringComparison.Ordinal);
                int sameCount = options.Count(value => string.Equals(
                        value.Provider.Key.CasterUnitId, key.CasterUnitId, StringComparison.Ordinal) &&
                    string.Equals(value.Provider.Key.Ability.Canonical, key.Ability.Canonical,
                        StringComparison.Ordinal));
                bool selected = sameAbility && (selectedSpellbook == null
                    ? sameCount == 1
                    : string.Equals(key.SpellbookGuid, selectedSpellbook, StringComparison.Ordinal));
                string casterName = UnitDisplayName(inputs, key.CasterUnitId);
                choices.Add(new WorkspaceProviderChoice(key.Canonical, key.CasterUnitId, casterName,
                    WorkspaceProviderLabels.Describe(casterName, PoolLabel(option.Provider.ResourcePoolKey),
                        option.Provider.EffectiveCasterLevel, key.Ability.MetamagicMask), selected));
            }
            WorkspaceProviderLabels.Disambiguate(choices);
            return choices;
        }

        private static ProviderPlanningOption FindProviderOption(CastingWorkspaceInputs inputs,
            string sourceId, string providerKey)
        {
            EffectExpression expression;
            if (inputs == null || inputs.ProviderOptions == null || inputs.EffectsBySource == null ||
                string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(providerKey) ||
                !inputs.EffectsBySource.TryGetValue(sourceId, out expression))
                return null;
            return inputs.ProviderOptions.FirstOrDefault(option => option != null &&
                option.Provider != null && string.Equals(option.Provider.Key.Canonical,
                    providerKey, StringComparison.Ordinal) &&
                OptionServesExpression(inputs, option, expression));
        }

        private static bool OptionServesExpression(
            CastingWorkspaceInputs inputs,
            ProviderPlanningOption option,
            EffectExpression sourceExpression)
        {
            EffectExpression abilityExpression;
            return option.Provider.Key.Ability != null &&
                inputs.EffectsBySource.TryGetValue(
                    option.Provider.Key.Ability.Canonical, out abilityExpression) &&
                ReferenceEquals(abilityExpression, sourceExpression);
        }

        // ------------------------------------------------------------------
        // Review and Apply (the actual presentation boundary)
        // ------------------------------------------------------------------

        // The view calls this when it actually renders a plan for review —
        // computing a preview alone is not presentation. The presented
        // contract is the SELECTED-RUN scope Apply will submit.
        public CastingPlanSignature PresentForReview(CastingWorkspaceInputs inputs)
        {
            ExplicitCastingPlan plan = Compile(inputs, SelectedRoutineId, false);
            CastingPlanSignature signature = CastingPlanSignature.For(plan, SelectedRoutineId);
            // Presentation never revokes a stored acceptance (it authorizes
            // only its exact digest); only Accept writes review state.
            _review.Present(SelectedRoutineId, signature);
            return signature;
        }

        public CastingReviewStatus ReviewStatusFor(string routineId)
        {
            return _review.StatusFor(routineId);
        }

        public CastingAcceptanceStanding AcceptanceStandingFor(string routineId)
        {
            return _review.StandingFor(routineId);
        }

        // The digest accepted for a routine (restored or given in this
        // session), or null.
        public string AcceptedDigestFor(string routineId)
        {
            string digest;
            return _review.AcceptedDigests.TryGetValue(routineId ?? string.Empty, out digest)
                ? digest : null;
        }

        private void PersistReviewState()
        {
            if (_reviewStore == null) return;
            try
            {
                _reviewStore.Save(CampaignId, _review.AcceptedDigests);
                // Saved: an earlier load or save problem no longer applies.
                ReviewStoreWarning = string.Empty;
            }
            catch (Exception exception)
            {
                ReviewStoreWarning = "review-state-save-failed:" +
                    exception.GetType().Name + ":" + exception.Message;
            }
        }

        // The player accepts the contents as currently compiled; the
        // coordinator refuses when those contents were never presented or
        // have materially changed since presentation.
        public bool AcceptPresentedPlan(CastingWorkspaceInputs inputs)
        {
            ExplicitCastingPlan plan = Compile(inputs, SelectedRoutineId, false);
            bool accepted = _review.Accept(SelectedRoutineId,
                CastingPlanSignature.For(plan, SelectedRoutineId)).Allowed;
            if (accepted) PersistReviewState();
            return accepted;
        }

        public WorkspaceApplyResult Apply(
            CastingApplyMode mode,
            string scopeRoutineId,
            CastingWorkspaceInputs inputs)
        {
            if (_submissionInFlight)
                return RefusedInFlight(null);
            if (LegacyImportBlocked)
                return new WorkspaceApplyResult(false,
                    "legacy-import-unresolved:" + LegacyImportBlockReason,
                    null, null);
            // Review K3/L1: unacknowledged legacy provider bans/caps/
            // priorities are requested constraints; NO apply mode ignores
            // them (the gate enforces the same rule on the compiled plan).
            if (_authoring.Document.PendingImportNotices.Count != 0)
                return new WorkspaceApplyResult(false,
                    "import-notices-pending:" + _authoring.Document.PendingImportNotices.Count,
                    null, null);
            // Normalize the optional scope ONCE (review C1): the compiler,
            // gate, and dispatch must all see the same selected-run scope —
            // a null scope must never mean whole-plan to one consumer and
            // selected-routine to another.
            string scope = string.IsNullOrEmpty(scopeRoutineId)
                ? SelectedRoutineId : scopeRoutineId;
            // The decision is computed from the selected-run plan as it
            // exists right now — the same scope that was presented — not
            // from any earlier preview or a whole-document compile whose
            // cross-routine reservations the scope filter cannot undo.
            ExplicitCastingPlan plan = Compile(inputs, scope, false);
            CastingApplyDecision decision = _gate.Evaluate(plan, mode, scope);
            // A refused decision never reaches the boundary in any mode.
            if (!decision.Allowed)
                return new WorkspaceApplyResult(false,
                    "apply-refused:" + string.Join(",", decision.BlockingReasons.ToArray()),
                    decision, null);
            // Nothing to submit (every casting already active, disabled or
            // deliberately omitted): an honest no-op, not a failure and not
            // an execution.
            if (decision.ExecutableCastingIds.Count == 0)
                return new WorkspaceApplyResult(false,
                    "nothing-to-cast:" + decision.Omissions.Count, decision, null);
            CastingPlanSignature signature = CastingPlanSignature.For(plan, scope);
            CastingReviewDecision review = _review.TrySubmit(scope, signature);
            if (!review.Allowed)
                return new WorkspaceApplyResult(
                    false, review.Reason, decision, null);
            // Project the approved castings onto executor steps BEFORE the
            // dispatch boundary: a plan that cannot become exactly one step
            // per approved casting is refused here, never partially run.
            ExplicitStepConversion projection = decision.Allowed
                ? ExplicitCastingStepConverter.Convert(plan, decision,
                    inputs.ProviderOptions, inputs.EffectsBySource)
                : null;
            if (projection != null && !projection.Converted)
                return new WorkspaceApplyResult(false,
                    "execution-projection-refused:" + projection.Refusal,
                    decision, null, projection);
            try
            {
                _submissionInFlight = true;
                CastingDispatchOutcome dispatch = _dispatch.Submit(
                    plan, decision, scope, projection);
                return new WorkspaceApplyResult(
                    dispatch.Submitted, dispatch.Submitted
                        ? string.Empty : dispatch.Reason,
                    decision, dispatch, projection);
            }
            finally
            {
                _submissionInFlight = false;
            }
        }

        // The last production run started from this session (any route),
        // recorded by the run owner when the run reached its terminal.
        public CastingRunReport LastRunReport { get; private set; }

        public void RecordRunReport(CastingRunReport report)
        {
            if (report == null) return;
            LastRunReport = report;
            LastAttemptMessage = null;
        }

        // The last refused attempt from any route (HUD or planner), shown in
        // the planner footer until a later run reports.
        public string LastAttemptMessage { get; private set; }

        public void RecordAttempt(string message)
        {
            LastAttemptMessage = string.IsNullOrEmpty(message) ? null : message;
        }

        // "Spell (caster -> target)" for results and logs, from the last
        // discovery inputs; falls back to identifiers, never throws.
        public string CastingLabel(string castingId)
        {
            PlannedCasting casting = _authoring.Document.Castings.FirstOrDefault(
                value => value != null && string.Equals(value.CastingId, castingId,
                    StringComparison.Ordinal));
            if (casting == null) return castingId ?? string.Empty;
            string spell = casting.SourceId;
            if (_lastInputs != null && casting.Ability != null)
            {
                ProviderPlanningOption option = _lastInputs.ProviderOptions.FirstOrDefault(
                    value => value != null && value.Provider != null &&
                        string.Equals(value.Provider.Key.Ability.Canonical,
                            casting.Ability.Canonical, StringComparison.Ordinal));
                if (option != null && !string.IsNullOrWhiteSpace(option.Provider.DisplayName))
                    spell = option.Provider.DisplayName;
            }
            string target = casting.TargetMode == CastingTargetMode.DirectTarget
                ? UnitDisplayName(_lastInputs, casting.DirectTargetUnitId)
                : "group";
            return spell + " (" + UnitDisplayName(_lastInputs, casting.CasterUnitId) +
                " -> " + target + ")";
        }

        // Player-facing names of resource pools (whose, and what kind), from
        // the party snapshot the view was built from. Pool keys are internal
        // identifiers and never shown.
        private Dictionary<string, string> _poolLabels =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private Dictionary<string, string> _enhancementPoolLabels =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static void ApplyNames(WorkspaceCastingCard card, Dictionary<string, string> namesByUnit)
        {
            card.ApplyDisplayNames(
                namesByUnit.TryGetValue(card.CasterUnitId ?? string.Empty,
                    out string casterName) ? casterName : null,
                card.DirectTargetUnitId == null ? null
                    : namesByUnit.TryGetValue(card.DirectTargetUnitId,
                        out string targetName) ? targetName : null,
                id => id != null && namesByUnit.TryGetValue(id,
                    out string unitName) ? unitName : id);
        }

        private void RefreshPoolLabels(CastingWorkspaceInputs inputs)
        {
            var labels = new Dictionary<string, string>(StringComparer.Ordinal);
            if (inputs != null)
            {
                var names = inputs.Snapshot.Units.ToDictionary(unit => unit.UnitId,
                    unit => string.IsNullOrWhiteSpace(unit.DisplayName) ? unit.UnitId : unit.DisplayName,
                    StringComparer.Ordinal);
                foreach (ResourcePoolSnapshot pool in inputs.Snapshot.ResourcePools)
                {
                    var drawing = inputs.Snapshot.Providers.Where(value =>
                        string.Equals(value.ResourcePoolKey, pool.PoolKey, StringComparison.Ordinal)).ToList();
                    // A pool shared by several units (item charges) has no
                    // single owner; a prepared pool spans every spell level,
                    // so only a spontaneous pool names its level.
                    var owners = drawing.Select(value => value.Key.CasterUnitId)
                        .Distinct(StringComparer.Ordinal).ToList();
                    var levels = drawing.Select(value => value.SpellLevel).Distinct().ToList();
                    var sourceNames = drawing.Select(value => value.SourceDisplayName)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.Ordinal).ToList();
                    string owner;
                    if (owners.Count != 1 || !names.TryGetValue(owners[0], out owner))
                        owner = null;
                    labels[pool.PoolKey] = WorkspacePoolLabels.Describe(pool.Kind,
                        pool.Kind == ResourcePoolKind.SpontaneousLevel && levels.Count == 1
                            ? levels[0] : (int?)null, owner,
                        sourceNames.Count == 1 ? sourceNames[0] : null);
                }
            }
            _poolLabels = labels;
            // Enhancement pools (rods and the like): whose, and which pool.
            var enhancementLabels = new Dictionary<string, string>(StringComparer.Ordinal);
            if (inputs != null && inputs.Enhancements != null)
            {
                var unitNames = inputs.Snapshot.Units.ToDictionary(unit => unit.UnitId,
                    unit => string.IsNullOrWhiteSpace(unit.DisplayName) ? unit.UnitId : unit.DisplayName,
                    StringComparer.Ordinal);
                foreach (IGrouping<string, CastEnhancementSnapshot> pool in inputs.Enhancements
                    .Where(value => value != null && !string.IsNullOrEmpty(value.UsagePoolId))
                    .GroupBy(value => value.UsagePoolId, StringComparer.Ordinal))
                {
                    var owners = pool.Select(value => value.CasterUnitId).Distinct(StringComparer.Ordinal).ToList();
                    var poolNames = pool.Select(value => value.UsagePoolDisplayName)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.Ordinal).ToList();
                    string owner;
                    if (owners.Count != 1 || !unitNames.TryGetValue(owners[0], out owner)) owner = null;
                    string what = poolNames.Count == 1 ? poolNames[0] + " uses" : "enhancement uses";
                    enhancementLabels[pool.Key] = owner == null ? what : owner + ": " + what;
                }
            }
            _enhancementPoolLabels = enhancementLabels;
        }

        // A budget row's player-facing name by category (review of
        // f7726c9..1332ed8, P3-D): native pools as above, enhancement pools
        // by owner and pool name, material components by kind.
        internal string BudgetLabel(CastingBudgetLine line)
        {
            if (line == null) return "resource";
            switch (line.Category)
            {
                case CastingCostCategory.NativePool:
                    return PoolLabel(line.PoolKey);
                case CastingCostCategory.EnhancementPool:
                    string label;
                    return line.PoolKey != null && _enhancementPoolLabels.TryGetValue(line.PoolKey, out label)
                        ? label : "enhancement uses";
                default:
                    return "material component";
            }
        }

        internal string PoolLabel(string poolKey)
        {
            string label;
            return poolKey != null && _poolLabels.TryGetValue(poolKey, out label) ? label : "resource";
        }

        internal string CostLabel(CastingCostLine line)
        {
            string count = line.Units > 1 ? " x" + line.Units : string.Empty;
            switch (line.Category)
            {
                case CastingCostCategory.NativePool:
                    return PoolLabel(line.PoolKey) + (line.Unlimited ? string.Empty : count);
                case CastingCostCategory.Material:
                    return "material component" + count;
                default:
                    string label;
                    return (line.PoolKey != null && _enhancementPoolLabels.TryGetValue(line.PoolKey, out label)
                        ? label : "enhancement use") + count;
            }
        }

        public string RoutineDisplayName(string routineId)
        {
            RoutineDefinition routine = _authoring.Document.Routines.FirstOrDefault(
                value => value != null && string.Equals(value.RoutineId, routineId,
                    StringComparison.Ordinal));
            if (routine != null && !string.IsNullOrWhiteSpace(routine.Name)) return routine.Name;
            return string.IsNullOrEmpty(routineId) ? "Routine"
                : char.ToUpperInvariant(routineId[0]) + routineId.Substring(1);
        }

        // ------------------------------------------------------------------
        // Authoring commands (explicit mutations with disclosed scope)
        // ------------------------------------------------------------------

        // Guarded-qualification seam: adds one exact, recipe-selected casting
        // through the authoring service (an ordinary undoable edit). The
        // player UI never calls it.
        internal AuthoringEditResult AddCastingForRuntime(PlannedCasting casting)
        {
            return _authoring.AddCasting(casting);
        }

        public AuthoringEditResult AddCastingFromDraft(
            CastingWorkspaceInputs inputs = null)
        {
            // The view supplies fresh discovery inputs at click time; a
            // headless caller may pass them explicitly. Resolution never
            // depends on a prior BuildView having happened.
            if (inputs != null) _lastInputs = inputs;
            // A draft authored without clicking a buff uses the buff shown
            // selected, never "unsourced" (review of 1332ed8..542cd66, P2-2).
            if (string.IsNullOrEmpty(Draft.SourceId) && !string.IsNullOrEmpty(SelectedSourceId))
                Draft.SourceId = SelectedSourceId;
            // The visible controls set source and caster; the exact ability
            // is resolved HERE through the same discovery the plan compiles
            // from (review F1) — callers never compensate privately, and an
            // unresolvable or ambiguous draft is an explained refusal, never
            // a throw out of a UI callback or a substitute casting.
            string draftKey = DraftResolutionKey();
            if (Draft.Ability == null || !string.Equals(draftKey,
                    _resolvedDraftKey, StringComparison.Ordinal))
            {
                // Re-resolve whenever the (source, caster) pair changed, not
                // only when the ability is null: a stale ability or
                // spellbook under a different selection must never survive
                // to an authored record (review F1). Deliberately unresolved
                // intent keeps an explicitly supplied ability (the plan
                // marks it blocked); only a UI draft with no ability at all
                // refuses with an explanation.
                string refusal = ResolveDraftAbility();
                if (refusal != null)
                {
                    if (Draft.Ability == null)
                        return AuthoringEditResult.Refuse(refusal);
                    Draft.SpellbookGuid = null;
                }
            }
            try
            {
                PlannedCasting casting = Draft.Materialize(
                    NextCastingId(), SelectedRoutineId);
                return _authoring.AddCasting(casting);
            }
            catch (ArgumentException exception)
            {
                return AuthoringEditResult.Refuse("draft-invalid:" +
                    exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return AuthoringEditResult.Refuse("draft-invalid:" +
                    exception.Message);
            }
        }

        // Returns null when the draft ability was resolved; otherwise an
        // explained refusal reason. Resolution writes the ability and its
        // spellbook so Materialize and the compiler see the same option.
        private string ResolveDraftAbility()
        {
            if (_lastInputs == null)
                return "draft-ability-unresolved:no-discovery-inputs";
            string source = string.IsNullOrEmpty(Draft.SourceId)
                ? SelectedSourceId : Draft.SourceId;
            string caster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            if (string.IsNullOrEmpty(caster))
                return "draft-ability-unresolved:no-caster-selected";
            int candidates;
            ProviderPlanningOption option = FindDraftOption(
                _lastInputs, source, caster, out candidates);
            if (candidates == 0)
                return "draft-ability-unresolved:no-provider-for-source-and-caster";
            if (candidates > 1)
                return "draft-ability-unresolved:exact-source-ambiguous:" +
                    candidates;
            Draft.Ability = option.Provider.Key.Ability;
            Draft.SpellbookGuid = option.Provider.Key.SpellbookGuid;
            _resolvedDraftKey = DraftResolutionKey();
            return null;
        }

        private string DraftResolutionKey()
        {
            string source = string.IsNullOrEmpty(Draft.SourceId)
                ? SelectedSourceId : Draft.SourceId;
            string caster = Draft.CasterUnitId ?? SelectedCasterUnitId;
            return (source ?? string.Empty) + "" + (caster ?? string.Empty);
        }

        // Group-aware focused-casting targeting edit: mode, recipient,
        // origin, and coverage change together through the canonical
        // UpdateFocusedCasting command — never direct-target cloning on a
        // group record (review G2).
        public AuthoringEditResult SetFocusedTargeting(
            CastingTargetMode mode,
            string directTargetUnitId,
            string originAnchorUnitId,
            IEnumerable<string> requiredCoverageUnitIds)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            PlannedCasting focused = _authoring.Document.Castings
                .FirstOrDefault(value => value != null && string.Equals(
                    value.CastingId, EditingFocusCastingId,
                    StringComparison.Ordinal));
            if (focused == null)
                return AuthoringEditResult.Refuse("focused-casting-missing");
            if (!Enum.IsDefined(typeof(CastingTargetMode), mode))
                return AuthoringEditResult.Refuse("targeting-mode-unsupported");
            string direct = mode == CastingTargetMode.DirectTarget
                ? directTargetUnitId : null;
            CastingOrigin origin = mode == CastingTargetMode.DirectTarget
                ? null
                : mode == CastingTargetMode.AnchoredOrigin
                    ? CastingOrigin.Anchored(originAnchorUnitId)
                    : CastingOrigin.CasterCentered();
            try
            {
                return UpdateFocusedCasting(focused.WithTargeting(
                    mode, direct, origin, requiredCoverageUnitIds));
            }
            catch (ArgumentException exception)
            {
                return AuthoringEditResult.Refuse(
                    "targeting-invalid:" + exception.Message);
            }
        }

        // One coherent targeting-shape operation (reviews F3, H2): the
        // candidate state — recipient (given, remembered, or none), anchor,
        // and a SNAPSHOTTED coverage list — resolves completely before any
        // owned state changes, so refusals preserve the entire prior draft
        // and callers may safely pass the draft's own live coverage list.
        public AuthoringEditResult SetDraftTargeting(
            CastingTargetMode mode,
            string directTargetUnitId,
            string originAnchorUnitId,
            IEnumerable<string> requiredCoverageUnitIds)
        {
            if (!Enum.IsDefined(typeof(CastingTargetMode), mode))
                return AuthoringEditResult.Refuse("targeting-mode-unsupported");
            // H2c: snapshot the incoming coverage BEFORE touching the draft —
            // callers legitimately pass Draft.RequiredCoverageUnitIds.
            var coverage = new List<string>();
            foreach (string unitId in requiredCoverageUnitIds ?? new string[0])
                if (!string.IsNullOrWhiteSpace(unitId) &&
                    !coverage.Contains(unitId))
                    coverage.Add(unitId);
            if (mode == CastingTargetMode.DirectTarget)
            {
                string recipient = directTargetUnitId;
                bool restored = false;
                if (string.IsNullOrWhiteSpace(recipient) &&
                    !string.IsNullOrEmpty(_rememberedDirectRecipient))
                {
                    // H2a: the remembered-recipient restore resolves HERE,
                    // before validation, so the Restore control's
                    // null-recipient call can actually reach it.
                    recipient = _rememberedDirectRecipient;
                    restored = true;
                }
                if (string.IsNullOrWhiteSpace(recipient))
                    return AuthoringEditResult.Refuse(
                        "targeting-requires-direct-target:" +
                        "pick a recipient to switch back");
                if (!string.IsNullOrEmpty(Draft.DirectTargetUnitId))
                    _rememberedDirectRecipient = Draft.DirectTargetUnitId;
                Draft.TargetMode = mode;
                Draft.DirectTargetUnitId = recipient;
                Draft.RequiredCoverageUnitIds.Clear();
                Draft.Origin = null;
                return AuthoringEditResult.Accept(
                    restored ? "draft-targeting:restored-recipient"
                        : "draft-targeting",
                    new string[0]);
            }
            if (mode == CastingTargetMode.AnchoredOrigin &&
                string.IsNullOrWhiteSpace(originAnchorUnitId))
                return AuthoringEditResult.Refuse("targeting-requires-anchor");
            if (!string.IsNullOrEmpty(Draft.DirectTargetUnitId))
                _rememberedDirectRecipient = Draft.DirectTargetUnitId;
            Draft.TargetMode = mode;
            Draft.DirectTargetUnitId = null;
            Draft.RequiredCoverageUnitIds.Clear();
            foreach (string unitId in coverage)
                Draft.RequiredCoverageUnitIds.Add(unitId);
            Draft.Origin = mode == CastingTargetMode.AnchoredOrigin
                ? CastingOrigin.Anchored(originAnchorUnitId)
                : CastingOrigin.CasterCentered();
            return AuthoringEditResult.Accept("draft-targeting", new string[0]);
        }

        private PlannedCasting FocusedCasting()
        {
            return EditingFocusCastingId == null ? null : _authoring.Document.Castings
                .FirstOrDefault(value => value != null && string.Equals(
                    value.CastingId, EditingFocusCastingId, StringComparison.Ordinal));
        }

        // Final review B3: the next casting uses exactly this provider (its
        // caster, and its spellbook level, item or ability), so a caster who
        // can cast the buff in several ways can still author it.
        public AuthoringEditResult ChooseDraftProvider(string providerKey,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            string source = string.IsNullOrEmpty(Draft.SourceId) ? SelectedSourceId : Draft.SourceId;
            ProviderPlanningOption option = FindProviderOption(_lastInputs, source, providerKey);
            if (option == null)
                return AuthoringEditResult.Refuse("provider-unavailable:" + providerKey);
            ChooseDraftCaster(option.Provider.Key.CasterUnitId);
            Draft.Ability = option.Provider.Key.Ability;
            Draft.SpellbookGuid = option.Provider.Key.SpellbookGuid;
            _resolvedDraftKey = DraftResolutionKey();
            return AuthoringEditResult.Accept("draft-provider", new string[0]);
        }

        // Final review B2/B3: the focused casting is cast by exactly this
        // provider instead - its caster, ability and spellbook change
        // together and everything else stays. This is how an imported
        // casting whose old plan let the planner pick any caster gets its
        // caster in place. Enhancements belong to their caster (a rod in
        // someone's pack), so only those the new caster has are kept.
        public AuthoringEditResult SetFocusedProvider(string providerKey,
            CastingWorkspaceInputs inputs = null)
        {
            if (inputs != null) _lastInputs = inputs;
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            PlannedCasting focused = FocusedCasting();
            if (focused == null)
                return AuthoringEditResult.Refuse("focused-casting-missing");
            ProviderPlanningOption option = FindProviderOption(_lastInputs, focused.SourceId, providerKey);
            if (option == null)
                return AuthoringEditResult.Refuse("provider-unavailable:" + providerKey);
            ProviderKey key = option.Provider.Key;
            if (string.Equals(focused.CasterUnitId, key.CasterUnitId, StringComparison.Ordinal) &&
                focused.Ability != null && string.Equals(focused.Ability.Canonical,
                    key.Ability.Canonical, StringComparison.Ordinal) &&
                string.Equals(focused.SpellbookGuid, key.SpellbookGuid, StringComparison.Ordinal))
                return AuthoringEditResult.Refuse("provider-unchanged");
            IEnumerable<CastEnhancementSnapshot> available = _lastInputs.Enhancements ??
                (IEnumerable<CastEnhancementSnapshot>)new CastEnhancementSnapshot[0];
            var owned = new HashSet<string>(available.Where(value => value != null &&
                    string.Equals(value.CasterUnitId, key.CasterUnitId, StringComparison.Ordinal))
                .Select(value => value.EnhancementId), StringComparer.Ordinal);
            try
            {
                return UpdateFocusedCasting(focused.WithProvider(key.CasterUnitId, key.Ability,
                    key.SpellbookGuid, focused.Enhancements.Where(value => value != null &&
                        owned.Contains(value.EnhancementId)).ToList()));
            }
            catch (ArgumentException exception)
            {
                return AuthoringEditResult.Refuse("provider-invalid:" + exception.Message);
            }
        }

        // Final review B2: skip the casting while its effect is already on
        // the target, or cast it again anyway.
        public AuthoringEditResult SetFocusedRecastPolicy(ExistingEffectPolicy policy)
        {
            PlannedCasting focused = FocusedCasting();
            if (focused == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            if (focused.ExistingEffectPolicy == policy)
                return AuthoringEditResult.Refuse("recast-policy-unchanged");
            return UpdateFocusedCasting(focused.WithExistingEffectPolicy(policy));
        }

        // Final review B2: move the focused casting to the end of another
        // routine, or one place earlier or later in its own.
        public AuthoringEditResult MoveFocusedCastingToRoutine(string routineId)
        {
            PlannedCasting focused = FocusedCasting();
            if (focused == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            if (string.Equals(focused.RoutineId, routineId, StringComparison.Ordinal))
                return AuthoringEditResult.Refuse("routine-unchanged");
            int count = _authoring.Document.Castings.Count(value => value != null &&
                string.Equals(value.RoutineId, routineId, StringComparison.Ordinal));
            return MoveFocusedCasting(routineId, count);
        }

        public AuthoringEditResult MoveFocusedCastingWithinRoutine(int delta)
        {
            PlannedCasting focused = FocusedCasting();
            if (focused == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            List<PlannedCasting> routine = _authoring.Document.Castings.Where(value =>
                value != null && string.Equals(value.RoutineId, focused.RoutineId,
                    StringComparison.Ordinal)).ToList();
            int position = routine.FindIndex(value => string.Equals(value.CastingId,
                focused.CastingId, StringComparison.Ordinal)) + delta;
            if (position < 0) return AuthoringEditResult.Refuse("already-first");
            if (position >= routine.Count) return AuthoringEditResult.Refuse("already-last");
            return MoveFocusedCasting(focused.RoutineId, position);
        }

        public AuthoringEditResult UpdateFocusedCasting(PlannedCasting replacement)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            if (!string.Equals(replacement.CastingId, EditingFocusCastingId,
                    StringComparison.Ordinal))
                return AuthoringEditResult.Refuse(
                    "replacement-does-not-match-focus:" + replacement.CastingId);
            return _authoring.UpdateCasting(replacement);
        }

        public AuthoringEditResult RemoveFocusedCasting()
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            AuthoringEditResult result = _authoring.RemoveCasting(
                EditingFocusCastingId);
            if (result.Applied)
                // A removed record must not leave a stale focus id behind
                // (review F4).
                EditingFocusCastingId = null;
            return result;
        }

        public AuthoringEditResult MoveFocusedCasting(
            string targetRoutineId, int targetPosition)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            return _authoring.MoveCasting(
                EditingFocusCastingId, targetRoutineId, targetPosition);
        }

        public AuthoringEditResult SetFocusedCastingState(CastingAuthoringState state)
        {
            if (EditingFocusCastingId == null)
                return AuthoringEditResult.Refuse("no-editing-focus");
            return _authoring.SetCastingState(EditingFocusCastingId, state);
        }

        public bool Undo()
        {
            return _authoring.Undo();
        }

        // ------------------------------------------------------------------
        // Persistence (candidate storage; one canonical writer)
        // ------------------------------------------------------------------

        public void Save()
        {
            if (PersistenceBlocked)
                throw new InvalidOperationException(
                    "Candidate persistence is blocked: " + LoadStatus +
                    " " + LoadWarning);
            // The loaded (or imported) player settings are written back
            // unchanged unless the player changed them - never reset to
            // defaults by a document save.
            _repository.Save(CastingPlanProfile.FromDocument(
                _authoring.Document, _uiSettings, _executionSettings));
            _savedIntentSignature = DocumentIntentSignature();
        }

        // Canonical authored-document comparison (review G5): the EXISTING
        // serialization model — every persisted authored field including
        // targeting-modifier parameters, exact sources, policies, and the
        // ordered routine definitions — not a selectively maintained
        // delimiter string. Derived observations are excluded by the
        // profile model itself.
        public string DocumentIntentSignature()
        {
            return ProfileIntentSignature(CampaignId, CastingPlanProfile.FromDocument(
                _authoring.Document, _uiSettings, _executionSettings));
        }

        // What a fresh session would read for this campaign, as the same
        // intent signature (a plain read: no import, no session), so the
        // in-game reload compares disk with what was saved, not the retained
        // session with itself (review of 1332ed8..542cd66, P2-1).
        internal static string SavedIntentSignature(string modPath, string campaignId, out string status)
        {
            CastingPlanLoadResult loaded = new CastingPlanRepository(modPath).Load(campaignId);
            status = loaded.Status.ToString();
            if (loaded.Status != CastingPlanLoadStatus.Loaded || loaded.Profile == null) return null;
            CastingPlanProfile profile = loaded.Profile;
            UiProfile ui = profile.Ui == null ? UiProfile.Default()
                : new UiProfile { Scale = profile.Ui.Scale, Hotkey = profile.Ui.Hotkey };
            ExecutionProfile execution = profile.Execution == null
                ? ExecutionProfile.Default() : CopyOf(profile.Execution);
            return ProfileIntentSignature(campaignId,
                CastingPlanProfile.FromDocument(profile.ToDocument(), ui, execution));
        }

        private static string ProfileIntentSignature(string campaignId, CastingPlanProfile profile)
        {
            // Normalize the volatile schema stamp so equality reflects
            // CONTENT, and stamp the campaign binding explicitly.
            profile.SchemaVersion = 0;
            return campaignId + "" +
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    profile, Newtonsoft.Json.Formatting.None,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
                    });
        }

        // True when authored intent differs from the last explicitly saved
        // (or loaded) document — the dirty-state contract for close/reopen
        // preservation (review F6).
        public bool IsDirty
        {
            get { return !string.Equals(DocumentIntentSignature(),
                _savedIntentSignature ?? string.Empty,
                StringComparison.Ordinal); }
        }

        public CastingPlanLoadStatus Reload()
        {
            CastingPlanLoadResult loaded = _repository.Load(CampaignId);
            if (LegacyImportBlocked && loaded.Status == CastingPlanLoadStatus.Absent)
            {
                // Retry the blocked legacy import (after the owner repaired
                // the legacy file); still blocked if it fails again.
                LegacyImportBlocked = false;
                LegacyImportBlockReason = null;
                PersistenceBlocked = false;
                ImportReport = null;
                CastingPlanDocument retried = MigrateLegacyOrEmpty(
                    _modPath, CampaignId, _legacyGroupings);
                _authoring = new CastingAuthoringService(retried);
                EditingFocusCastingId = null;
                _savedIntentSignature = DocumentIntentSignature();
                if (!LegacyImportBlocked && MigrationStatus == CastingMigrationStatus.Migrated)
                    return LoadStatus;
                return CastingPlanLoadStatus.Absent;
            }
            LoadStatus = loaded.Status;
            LoadWarning = loaded.Warning;
            switch (loaded.Status)
            {
                case CastingPlanLoadStatus.Loaded:
                case CastingPlanLoadStatus.RecoveredFromBackup:
                    _authoring = new CastingAuthoringService(
                        loaded.Profile.ToDocument());
                    AdoptSettings(loaded.Profile);
                    PersistenceBlocked = false;
                    // Focus cannot survive a document swap (review F4).
                    EditingFocusCastingId = null;
                    _savedIntentSignature = DocumentIntentSignature();
                    break;
                default:
                    PersistenceBlocked = true;
                    break;
            }
            return loaded.Status;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private ExplicitCastingPlan Compile(
            CastingWorkspaceInputs inputs, string routineScope, bool projectEffects)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            _lastInputs = inputs;
            return _compiler.Compile(
                _authoring.Document, inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements,
                routineScope, inputs.TargetingModifiers, projectEffects,
                inputs.LiveEffects);
        }

        private WorkspaceApplyResult RefusedInFlight(CastingApplyDecision decision)
        {
            return new WorkspaceApplyResult(
                false, "submission-already-in-flight", decision, null);
        }

        private string FirstSourceId(CastingWorkspaceInputs inputs)
        {
            PlannedCasting first = _authoring.Document.Castings.FirstOrDefault();
            if (first != null) return first.SourceId;
            // With nothing authored, the initial selection is the first
            // buff of the grid (alphabetical), not a blank buff list (review
            // C1).
            if (inputs == null || inputs.ProviderOptions == null ||
                inputs.EffectsBySource == null || inputs.ProviderOptions.Count == 0)
                return string.Empty;
            WorkspaceSourceOption firstShown = BuildSourceOptions(inputs, null).FirstOrDefault();
            if (firstShown != null) return firstShown.SourceId;
            ProviderPlanningOption firstOption = inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal)
                .First();
            AbilityKey ability = firstOption.Provider.Key.Ability;
            EffectExpression abilityExpression;
            if (ability == null ||
                !inputs.EffectsBySource.TryGetValue(
                    ability.Canonical, out abilityExpression))
                return string.Empty;
            foreach (KeyValuePair<string, EffectExpression> pair in
                inputs.EffectsBySource.OrderBy(
                    value => value.Key, StringComparer.Ordinal))
                if (!string.Equals(pair.Key, ability.Canonical,
                        StringComparison.Ordinal) &&
                    ReferenceEquals(pair.Value, abilityExpression))
                    return pair.Key;
            return string.Empty;
        }

        // Eligible casters for a source derive from the DISCOVERED provider
        // options, so a fresh buff shows its casters before anything is
        // authored (review C1). The source/ability linkage uses the same
        // alias-instance contract the legacy catalogue model establishes
        // (PlannerSetupModel aliases effects[sourceId] = effects[ability]):
        // a provider serves a source when its ability's expression IS the
        // source's expression instance. Plan-derived readiness stays
        // separate and untouched.
        private static HashSet<string> CapableCasterUnitIdsForSource(
            CastingWorkspaceInputs inputs, string selectedSource)
        {
            var capable = new HashSet<string>(StringComparer.Ordinal);
            EffectExpression sourceExpression;
            if (inputs == null || inputs.ProviderOptions == null ||
                inputs.EffectsBySource == null ||
                string.IsNullOrEmpty(selectedSource) ||
                !inputs.EffectsBySource.TryGetValue(
                    selectedSource, out sourceExpression))
                return capable;
            foreach (ProviderPlanningOption option in inputs.ProviderOptions)
            {
                if (option == null || option.Provider == null) continue;
                AbilityKey ability = option.Provider.Key.Ability;
                EffectExpression abilityExpression;
                if (ability == null ||
                    !inputs.EffectsBySource.TryGetValue(
                        ability.Canonical, out abilityExpression) ||
                    !ReferenceEquals(abilityExpression, sourceExpression)) continue;
                capable.Add(option.Provider.Key.CasterUnitId);
            }
            return capable;
        }

        private List<WorkspaceCasterRow> BuildCasterRows(
            ExplicitCastingPlan plan, CastingWorkspaceInputs inputs,
            string selectedSource)
        {
            // Capability per caster for the selected buff, separated from
            // this plan's current readiness reasons for that caster.
            HashSet<string> capableIds =
                CapableCasterUnitIdsForSource(inputs, selectedSource);
            var rows = new List<WorkspaceCasterRow>();
            foreach (UnitSnapshot unit in inputs.Snapshot.Units)
            {
                bool capable = capableIds.Contains(unit.UnitId);
                List<string> reasons = plan.Castings
                    .Where(value => value.CasterUnitId == unit.UnitId)
                    .SelectMany(value => value.ReadinessReasons)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToList();
                rows.Add(new WorkspaceCasterRow(
                    unit.UnitId, unit.DisplayName, capable, reasons,
                    unit.UnitId == SelectedCasterUnitId));
            }
            return rows;
        }

        // Castings lane scope: the selected buff's castings (default), or
        // every casting in the selected routine — the whole plan for that
        // routine, like Bubble Buffs' "Only Requested" overview. Browsing
        // scope never mutates the document.
        public bool ShowWholeRoutine { get; set; }

        private List<WorkspaceCastingCard> BuildCards(
            ExplicitCastingPlan plan, string selectedSource)
        {
            return BuildCards(plan, selectedSource, ShowWholeRoutine);
        }

        private List<WorkspaceCastingCard> BuildCards(
            ExplicitCastingPlan plan, string selectedSource, bool wholeRoutine)
        {
            var cards = new List<WorkspaceCastingCard>();
            foreach (ResolvedCasting casting in plan.Castings
                .Where(value => wholeRoutine
                    ? string.Equals(value.RoutineId, SelectedRoutineId,
                        StringComparison.Ordinal)
                    : string.Equals(value.SourceId, selectedSource,
                        StringComparison.Ordinal)))
            {
                string modeLabel;
                string originLabel = string.Empty;
                switch (casting.TargetMode)
                {
                    case CastingTargetMode.CasterCenteredOrigin:
                        modeLabel = "Group · caster-centered";
                        originLabel = "Origin: caster";
                        break;
                    case CastingTargetMode.AnchoredOrigin:
                        modeLabel = "Group · anchored";
                        originLabel = "Origin: " + casting.Origin.AnchorUnitId;
                        break;
                    default:
                        modeLabel = "Direct target";
                        break;
                }
                cards.Add(new WorkspaceCastingCard(
                    casting.CastingId, casting.RoutineId, casting.Order,
                    casting.CasterUnitId, casting.SourceId, modeLabel,
                    casting.DirectTargetUnitId, originLabel,
                    casting.RequiredCoverageUnitIds,
                    casting.PredictedBeneficiaryUnitIds,
                    casting.CoverageGaps.Select(gap => gap.UnitId).ToList(),
                    casting.Enhancements
                        .Select(value => value.EnhancementId +
                            (value.Required ? " (required)" : " (optional)"))
                        .ToList(),
                    casting.Cost.Select(CostLabel).ToList(),
                    casting.Readiness, casting.ReadinessReasons,
                    casting.CastingId == EditingFocusCastingId));
                cards[cards.Count - 1].ApplyRoutineName(RoutineDisplayName(casting.RoutineId));
                if (casting.Provenance != null)
                    cards[cards.Count - 1].ApplyReviewItems(
                        casting.Provenance.UnresolvedReviewItems,
                        casting.Provenance.ResolvedReviewItems);
                cards[cards.Count - 1].ApplyExecutionDetail(
                    ExplicitCastingStepConverter.StandardExecutionLimitation(casting),
                    casting.ExistingEffectNotes);
                CastingOutcomeEntry lastRun = LastRunReport == null ? null
                    : LastRunReport.Entries.FirstOrDefault(entry => string.Equals(
                        entry.CastingId, casting.CastingId, StringComparison.Ordinal));
                cards[cards.Count - 1].ApplyLastRun(
                    CastingRunPresentation.DescribeEntry(lastRun));
            }
            return cards;
        }

        private CastingPlanDocument NewDocument()
        {
            return new CastingPlanDocument(CampaignId,
                new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                },
                new PlannedCasting[0]);
        }

        // Final review B7: an id is never issued twice in a session. A new
        // casting gets an id above every id the document, the last run or
        // an earlier Add has used, so its card never shows the run of a
        // removed casting that once had the same id.
        private int _highestIssuedCastingIndex;

        private string NextCastingId()
        {
            int highest = _highestIssuedCastingIndex;
            IEnumerable<string> used = _authoring.Document.Castings.Select(value => value.CastingId)
                .Concat(LastRunReport == null ? new string[0]
                    : LastRunReport.Entries.Select(entry => entry.CastingId));
            foreach (string id in used)
            {
                int index;
                if (id != null && id.StartsWith("cast-", StringComparison.Ordinal) &&
                    int.TryParse(id.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture,
                        out index) && index > highest)
                    highest = index;
            }
            string candidate;
            do
            {
                highest++;
                candidate = "cast-" + highest;
            }
            while (_authoring.Document.Castings.Any(
                value => value.CastingId == candidate));
            _highestIssuedCastingIndex = highest;
            return candidate;
        }
    }

    // Player-facing defaults for the NEXT casting. Carrying defaults
    // forward never edits existing castings; Materialize builds one new
    // record from the current values.
    public sealed class CastingDraft
    {
        public string SourceId { get; set; }
        public AbilityKey Ability { get; set; }
        public string CasterUnitId { get; set; }
        public string SpellbookGuid { get; set; }
        public CastingTargetMode TargetMode { get; set; }
        public string DirectTargetUnitId { get; set; }
        public CastingOrigin Origin { get; set; }
        public List<string> RequiredCoverageUnitIds { get; } = new List<string>();
        public List<TargetingModifierSelection> TargetingModifiers { get; } =
            new List<TargetingModifierSelection>();
        public List<AuthoredEnhancementSelection> Enhancements { get; } =
            new List<AuthoredEnhancementSelection>();
        // New castings are Ready by default: a fully specified Add should
        // produce a casting that will run (first supervised session: the
        // extra Draft->Ready toggle was a stumbling block). A draft that
        // cannot be Ready (no caster) is refused with its reason.
        public CastingAuthoringState State { get; set; } =
            CastingAuthoringState.Ready;
        // Final review B2: whether the new casting is skipped while its
        // effect is already on the target (the default) or cast again.
        public ExistingEffectPolicy ExistingEffectPolicy { get; set; } =
            ExistingEffectPolicy.SkipAlreadyActive;

        internal PlannedCasting Materialize(string castingId, string routineId)
        {
            if (string.IsNullOrWhiteSpace(castingId))
                throw new ArgumentException("Casting ID is required.", "castingId");
            return new PlannedCasting(
                castingId, routineId, 0,
                string.IsNullOrWhiteSpace(SourceId) ? "unsourced" : SourceId,
                Ability ?? throw new InvalidOperationException(
                    "The draft needs an ability before it can become a casting."),
                CasterUnitId, SpellbookGuid, TargetMode, DirectTargetUnitId,
                Origin, RequiredCoverageUnitIds, TargetingModifiers,
                Enhancements, ExistingEffectPolicy, null,
                // A brand-new casting is a draft or ready intent; parking a
                // record before it exists is not a meaningful action.
                State == CastingAuthoringState.Disabled
                    ? CastingAuthoringState.Draft
                    : State,
                null);
        }
    }
}
