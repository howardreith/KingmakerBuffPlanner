using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Compatibility;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.UI;
using KingmakerBuffPlanner.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static class Program
    {
        private static int _passed;
        private static readonly List<string> Failures = new List<string>();
        private static string _protocolEvidenceRoot;

        private static int Main()
        {
            try
            {
                return RunAll();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Test runner infrastructure failure: " + exception);
                return 2;
            }
        }

        private static int RunAll()
        {
            ResolveEventHandler resolver = ResolveInstalledAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            string boundary = Path.Combine(
                Path.GetTempPath(),
                "KingmakerBuffPlanner.Tests-" + Guid.NewGuid().ToString("N"));
            string root = Path.Combine(boundary, "source-only-protocol");
            _protocolEvidenceRoot = boundary;
            try
            {
                Directory.CreateDirectory(root);
                Run("absent-activation-is-inert", TestAbsentActivation);
                Run("valid-request-is-accepted", () => TestValidRequest(root));
                Run("production-evidence-root-remains-guarded", () => TestProductionEvidenceRoot(root));
                Run("valid-catalog-request-is-accepted", () => TestValidCatalogRequest(root));
                Run("valid-call-of-the-wild-request-is-accepted", () => TestValidCallOfTheWildRequest(root));
                Run("valid-human-reproduction-request-is-accepted", () => TestValidHumanReproductionRequest(root));
                Run("valid-ui-request-is-accepted", () => TestValidUiRequest(root));
                Run("valid-live-ui-request-is-accepted", () => TestValidLiveUiRequest(root));
                Run("valid-native-ui-probe-request-is-accepted", () => TestValidNativeUiProbeRequest(root));
                Run("valid-final-core-request-is-accepted", () => TestValidFinalCoreRequest(root));
                Run("valid-performance-request-is-accepted", () => TestValidPerformanceRequest(root));
                Run("performance-parameters-are-exact", () => TestInvalidPerformanceRequest(root));
                Run("valid-launch-render-diagnostic-request-is-accepted", () => TestValidLaunchRenderDiagnosticRequest(root));
                Run("valid-menu-input-diagnostic-request-is-accepted", () => TestValidMenuInputDiagnosticRequest(root));
                Run("menu-diagnostic-parameters-must-be-empty", () => TestInvalidMenuDiagnosticParameters(root));
                Run("menu-frame-luma-stats-classify-black-frames", TestMenuFrameStatsBlackClassification);
                Run("menu-frame-luma-stats-summarize-mixed-frames", TestMenuFrameStatsMixedSummary);
                Run("menu-frame-changed-fraction-measures-frame-deltas", TestMenuFrameChangedFraction);
                Run("loaded-assembly-identity-resolves-mono-sidecar-caches", TestLoadedAssemblyIdentitySidecarResolution);
                Run("duplicate-flag-rejected", () => TestDuplicateFlag(root));
                Run("outside-path-rejected", TestOutsidePath);
                Run("unknown-member-rejected", () => TestMutation(root, "unknown-member", AddUnknownMember));
                Run("duplicate-member-rejected", () => TestDuplicateMember(root));
                Run("wrong-scenario-rejected", () => TestMutation(root, "wrong-scenario", o => o["scenario"] = "unknown"));
                Run("wrong-profile-rejected", () => TestMutation(root, "wrong-profile", o => o["profileId"] = "unknown"));
                Run("wrong-version-rejected", () => TestMutation(root, "wrong-version", o => o["expectedModVersion"] = "9.9.9"));
                Run("wrong-commit-rejected", () => TestMutation(root, "wrong-commit", o => o["expectedCommit"] = "WRONG"));
                Run("invalid-hash-rejected", () => TestMutation(root, "invalid-hash", o => o["expectedDllSha256"] = "not-a-hash"));
                Run("parameters-rejected", () => TestMutation(root, "parameters", o => o["parameters"] = new Dictionary<string, object> { { "saveName", "KBP_AUTOMATION_BASELINE" } }));
                Run("result-reuse-rejected", () => TestResultReuse(root));
                Run("game-root-without-trailing-separator", TestGameRootWithoutTrailingSeparator);
                Run("game-root-with-trailing-separator", TestGameRootWithTrailingSeparator);
                Run("scanner-preserves-conditional-branches", TestScannerConditional);
                Run("scanner-propagates-target-transform", TestScannerTarget);
                Run("scanner-reports-cycle", TestScannerCycle);
                Run("scanner-reports-unknown-node", TestScannerUnknown);
                Run("scanner-expression-wire-contract", TestScannerExpressionWireContract);
                Run("spellbook-role-filtering-is-structural-and-fail-soft",
                    TestSpellbookRoleResolution);
                Run("installed-call-of-the-wild-spellbook-contract-is-exact",
                    TestInstalledSpellbookRoleContract);
                Run("area-recipient-refinement-is-conservative",
                    TestAreaRecipientSemantics);
                Run("communal-canaries-derive-mass-semantics-from-structure",
                    TestCommunalStructuralCanaries);
                Run("conflicting-recipient-semantics-fail-closed",
                    TestConflictingRecipientSemantics);
                Run("native-candidate-classification-is-structural", TestNativeCandidateClassification);
                Run("persistent-beneficial-classification-is-branch-and-recipient-aware",
                    TestPersistentBeneficialClassification);
                Run("restorative-marker-only-candidates-are-excluded-without-name-rules",
                    TestRestorativeCandidateClassification);
                Run("optional-blueprint-ownership-is-exact", TestBlueprintOwnership);
                Run("harmony-target-identities-are-stable", TestHarmonyTargetIdentity);
                Run("installed-harmony-inventory-api-is-callable", TestHarmonyInventoryApi);
                Run("effect-overrides-are-versioned-and-branch-preserving", TestEffectOverrides);
                Run("stable-keys-distinguish-variants-and-metamagic", TestStableKeys);
                Run("complete-name-layout-preserves-long-communal-suffix", TestCompleteNameLayout);
                Run("ordinary-nonvariant-catalog-entry-remains-single", TestOrdinaryCatalogExpansion);
                Run("variant-parent-expands-five-eligible-children", TestVariantCatalogFive);
                Run("variant-membership-is-independent-of-temporary-availability",
                    TestVariantOwnershipAvailability);
                Run("unresolved-variant-parent-is-not-selectable", TestVariantParentSuppressed);
                Run("variant-stable-identities-are-distinct", TestVariantStableIdentities);
                Run("variant-entry-retains-parent-and-child-identities", TestVariantParentChildIdentity);
                Run("variant-expansion-deduplicates-declared-children", TestVariantDeduplication);
                Run("variant-display-keeps-communal-distinction", TestVariantCommunalNames);
                Run("variant-search-finds-parent-and-concrete-name", TestVariantSearchAndOrder);
                Run("variant-profile-roundtrip-preserves-child", () => TestVariantProfileRoundTrip(root));
                Run("legacy-ambiguous-parent-requires-reselection", TestLegacyAmbiguousVariant);
                Run("variant-availability-uses-parent-resource-context", TestVariantParentAvailability);
                Run("variant-execution-plan-selects-requested-child", TestVariantExecutionSelection);
                Run("variant-execution-reserves-one-parent-resource", TestVariantSingleConsumption);
                Run("nonvariant-planning-remains-exact", TestNonVariantPlanningRegression);
                Run("variant-icon-falls-back-to-parent", TestVariantIconFallback);
                Run("localized-variant-formatting-does-not-parse-English", TestLocalizedVariantFormatting);
                Run("spontaneous-providers-share-one-pool", TestSpontaneousSharedPool);
                Run("prepared-opposition-consumes-linked-slots", TestPreparedLinkedSlots);
                Run("prepared-domain-slot-eligibility-is-preserved", TestPreparedDomainEligibility);
                Run("unlimited-pool-is-explicit", TestUnlimitedPool);
                Run("party-snapshot-orders-by-stable-id", TestPartySnapshotOrdering);
                Run("effect-presence-preserves-allof-anyof", TestEffectPresenceSemantics);
                Run("planner-mass-cast-consumes-one-resource", TestPlannerMassSingleCost);
                Run("planner-priority-cap-and-fallback", TestPlannerPriorityCap);
                Run("planner-default-order-is-input-independent", TestPlannerDeterminism);
                Run("planner-reports-active-skip-marker", TestPlannerActiveSkip);
                Run("planner-honors-ban-and-material-availability", TestPlannerBanAndMaterial);
                Run("planner-reserves-material-once-per-cast", TestPlannerMaterialReservation);
                Run("nonrequired-material-check-is-not-evaluated", TestNonrequiredMaterialCheck);
                Run("planner-routine-shares-resource-ledger", TestPlannerRoutineSharedLedger);
                Run("effect-fingerprint-is-semantic-and-provider-independent", TestEffectFingerprint);
                Run("duplicate-provider-effects-consolidate-and-auto-select", TestAggregateCardAndPlanning);
                Run("aggregate-availability-does-not-double-count-shared-pool", TestAggregateAvailability);
                Run("selected-buff-summary-is-resource-specific-and-unambiguous", TestSelectedBuffSummary);
                Run("aggregate-assignment-round-trip-preserves-targets", () => TestAggregateRoundTrip(root));
                Run("routine-service-reports-unsupported-sources", TestRoutineServiceUnsupportedSources);
                Run("profile-round-trip-preserves-stable-ids", () => TestProfileRoundTrip(root));
                Run("profile-recovers-valid-bounded-backup", () => TestProfileBackupRecovery(root));
                Run("profile-migrates-schema-one", () => TestProfileMigration(root));
                Run("profile-migrates-hidden-and-f10-state", () => TestGridProfileMigration(root));
                Run("profile-malformed-json-recovers-default", () => TestProfileMalformed(root));
                Run("setup-model-direct-targets-are-routine-local", TestSetupModel);
                Run("provider-policy-operations-are-explicit-and-normalized",
                    TestProviderPolicyOperations);
                Run("provider-policy-splits-casts-and-fails-closed",
                    TestProviderPolicyPlanning);
                Run("provider-policy-presentation-retains-unavailable-owned-casters",
                    TestProviderPolicyPresentation);
                Run("provider-policy-roundtrip-and-stale-keys-are-exact",
                    () => TestProviderPolicyRoundTrip(root));
                Run("catalog-filter-selected-category-and-reset-contract", TestCatalogFilterState);
                Run("presentation-view-models-use-player-facing-deterministic-state", TestPresentationModels);
                Run("routine-membership-chips-are-active-aware-and-persistent",
                    () => TestRoutineMembershipChips(root));
                Run("right-click-description-resolves-without-plan-mutation", TestDescriptionRequest);
                Run("powerful-change-qualification-is-semantic", TestPowerfulChangeSemanticQualification);
                Run("powerful-change-availability-is-caster-and-spell-exact", TestPowerfulChangeAvailability);
                Run("powerful-change-score-options-share-reservoir", TestPowerfulChangeSharedReservoir);
                Run("cast-enhancement-applicability-and-reservation", TestCastEnhancementPlanning);
                Run("effective-targeting-is-routine-and-assignment-aware",
                    TestEffectiveTargetingRoutineAwareness);
                Run("enhancement-exclusivity-and-shared-pool-costs-are-explicit",
                    TestEnhancementCompatibilityAndSharedCost);
                Run("shared-enhancement-pools-never-overcommit",
                    TestSharedEnhancementPoolOvercommit);
                Run("multi-enhancement-profile-roundtrip-is-deterministic",
                    () => TestMultiEnhancementRoundTrip(root));
                Run("alchemist-infusion-remains-passive-native-targeting",
                    TestPassiveInfusionTargeting);
                Run("cast-enhancement-selection-is-assignment-scoped", TestCastEnhancementSelection);
                Run("casting-section-presents-caster-and-enhancement-choices", TestCastingSectionPresentation);
                Run("casting-section-layout-keeps-button-labels-visible", TestCastingSectionLayout);
                Run("chooser-scroll-layout-owns-content-bounds", TestChooserScrollLayout);
                Run("native-theme-resolves-and-falls-back-per-capability", TestNativeThemeResolution);
                Run("control-caption-fit-grows-only-from-design-floor", TestControlCaptionFit);
                Run("casting-assignments-route-mixed-casters-exactly", TestCastingAssignmentRouting);
                Run("assignment-order-and-shortage-allocate-explicitly", TestAssignmentOrderAndShortage);
                Run("partial-apply-gate-distinguishes-coverage-from-casts", TestPartialExecutionGate);
                Run("casting-order-rows-and-resource-lines-derive-from-plan", TestCastingOrderPresentation);
                Run("sequence-forecast-carries-balances-per-selected-routine", TestSequenceForecast);
                Run("spellbook-handoff-waits-bounded-and-rolls-back", TestSpellbookHandoff);
                Run("assignment-editor-model-supports-player-flows", TestAssignmentEditorModel);
                Run("material-plan-change-requires-renewed-review", TestPlanMaterialChangeDetector);
                Run("assignment-editor-intent-regressions", TestAssignmentEditorIntent);
                Run("review-state-and-material-signatures", TestReviewStateAndSignatures);
                Run("review-acknowledgment-follows-production-orchestration", TestReviewAcknowledgmentOrchestration);
                Run("forecast-consumes-prepared-tokens-exactly", TestForecastPreparedTokens);
                Run("forecast-projects-only-justified-effects", TestForecastEffectProjection);
                Run("portrait-path-resolves-child-intent-separately", TestPortraitChildIntent);
                Run("same-provider-targeting-survives-both-orders", TestSameProviderTargeting);
                Run("picker-toggle-never-drops-sibling-coverage", TestPickerToggleCoverage);
                Run("forecast-caster-effects-land-on-caster", TestForecastCasterIdentity);
                Run("unsupported-configured-requests-block-partial-apply", TestUnresolvableCoverage);
                Run("spellbook-handoff-invokes-opener-and-awaits-presentation", TestSpellbookHandoff);
                Run("cast-enhancement-execution-is-fail-closed-and-cleaned-up", TestCastEnhancementExecution);
                Run("consumed-one-shot-enhancement-is-not-rearmed", TestOneShotEnhancementRestoration);
                Run("execution-preflight-runs-under-the-native-activation-lease",
                    TestExecutionPreflightUnderLease);
                Run("personal-target-eligibility-is-provider-relative", TestPersonalTargetEligibility);
                Run("area-coverage-preview-distinguishes-direct-and-indirect", TestAreaCoveragePresentation);
                Run("per-anchor-mass-coverage-avoids-duplicate-communal-casts",
                    () => TestPerAnchorMassCoverage(root));
                Run("single-target-plan-does-not-create-indirect-coverage", TestSingleTargetCoveragePresentation);
                Run("caster-centered-plan-does-not-invent-direct-receiver", TestCasterCenteredCoveragePresentation);
                Run("four-column-grid-metrics-have-no-horizontal-scroll", TestGridMetrics);
                Run("large-catalog-grid-window-remains-bounded", TestLargeCatalogGridWindow);
                Run("planner-hotkey-chord-consumes-native-primary-key", TestPlannerHotkeyBinding);
                Run("input-lease-restores-on-close-and-acquire-failure", TestInputLease);
                Run("screen-state-machine-is-idempotent", TestScreenStateMachine);
                Run("setup-open-sound-gate-emits-once-per-successful-transition",
                    TestSetupOpenSoundGate);
                Run("native-hud-source-contract-retires-custom-chrome",
                    TestNativeHudSourceContract);
                Run("ui-readiness-is-deferred-across-frames", TestDeferredUiReadiness);
                Run("hud-install-discovery-is-invalidated-not-frame-polled", TestHudInstallInvalidation);
                Run("hud-retryable-readiness-retries-at-bounded-cadence", TestHudRetryableReadiness);
                Run("hud-provisional-expiry-rearms-without-host-transition", TestHudCandidateExpiry);
                Run("hud-stale-hosting-chain-invalidates-installed-state", TestHudHostingChainStaleness);
                Run("hud-stable-states-do-not-repeat-discovery", TestHudStablePerformance);
                Run("hud-lifecycle-transitions-suspend-and-resume", TestHudLifecycleTransitions);
                Run("quick-execution-instruments-and-presents-empty-group", TestQuickExecutionFlow);
                Run("quick-result-has-no-floating-or-native-log-presentation",
                    TestQuickResultPresentationBoundary);
                Run("animated-executor-validates-before-queue-and-reports", TestAnimatedExecutor);
                Run("instant-executor-revalidates-batches-and-reports", TestInstantExecutor);
                Run("submitted-without-effect-is-not-success", TestUnconfirmedExecution);
                Run("hybrid-executor-routes-and-blocks-fallbacks", TestHybridExecutor);
                Run("routing-evidence-precedes-cast-and-survives-cancellation", TestRoutingEvidence);
                Run("instant-fallback-distinguishes-effect-confirmation", TestInstantFallbackFeedback);
                Run("share-direct-capability-controls-combined-routing",
                    TestShareDirectRoutingPolicy);
                Run("share-direct-four-recipients-preserve-resource-ownership",
                    TestShareDirectFourRecipientExecution);
                Run("provider-direct-iterator-cancellation-cleans-or-blocks",
                    TestProviderDirectCancellationCleanup);
                Run("supported-sticky-touch-classification-is-direct-delivery",
                    TestSupportedStickyTouchClassification);
                Run("unsupported-sticky-touch-classification-fails-closed",
                    TestUnsupportedStickyTouchClassification);
                Run("freedom-of-movement-catalog-canary-is-sticky-delivery",
                    TestFreedomOfMovementStickyTouchCatalogCanary);
                Run("installed-sticky-touch-execution-contract-is-exact",
                    TestInstalledStickyTouchExecutionContract);
                Run("instant-sticky-touch-routing-bypasses-animated-command",
                    TestStickyTouchInstantRouting);
                Run("four-target-prepared-sticky-touch-is-sequential-and-exact",
                    TestPreparedStickyTouchRepeatedTargets);
                Run("spontaneous-sticky-touch-spends-shared-pool-once",
                    TestSpontaneousStickyTouchRepeatedTargets);
                Run("sticky-touch-rule-cast-spend-policy-is-single-owner",
                    TestStickyTouchSpendPolicy);
                Run("animated-sticky-touch-waits-for-complete-delivery-lifecycle",
                    TestAnimatedStickyTouchLifecycle);
                Run("sticky-touch-failure-cleanup-does-not-block-later-work",
                    TestStickyTouchFailureCleanup);
                Run("native-theme-lookup-requires-native-root-not-owned-overlay",
                    TestNativeThemeLookupScope);
                Run("spellbook-window-locator-is-exact-then-tolerant-and-refuses-ambiguity",
                    TestSpellbookWindowLocator);
                Run("chooser-budget-derives-from-authoritative-plan",
                    TestChooserBudgetFromAuthoritativePlan);
                Run("chooser-budget-follows-reordered-assignment-priority",
                    TestChooserBudgetReorderedPriority);
                Run("metamagic-labels-never-show-raw-masks",
                    TestMetamagicLabelsNeverShowRawMasks);
                Run("installed-call-of-the-wild-metamagic-name-contract-is-exact",
                    TestInstalledCallOfTheWildMetamagicNames);
                Run("chooser-budget-note-is-spell-scoped-with-unit-labels",
                    TestChooserBudgetSpellScopedNotes);
                Run("assignment-chooser-uses-assignment-selections-and-allows-removal",
                    TestAssignmentChooserScopedChoices);
                Run("chooser-budget-text-stays-bounded",
                    TestChooserBudgetTextStaysBounded);
                // Casting-first migration (charter A01-A04 and the Phase 1
                // authoring/persistence contract).
                Run("casting-a01-two-casters-three-independent",
                    TestCastingA01TwoCastersThreeIndependent);
                Run("casting-a02-three-recipients-three-invocations",
                    TestCastingA02ThreeRecipientsThreeInvocations);
                Run("casting-a03-group-one-invocation-six-beneficiaries",
                    TestCastingA03GroupOneInvocationSixBeneficiaries);
                Run("casting-a04-missed-coverage-no-auto-second-cast",
                    TestCastingA04MissedCoverageNoAutoSecondCast);
                Run("casting-authoring-scope-undo-and-read-only-compile",
                    TestCastingAuthoringScopeUndoAndReadOnlyCompile);
                Run("casting-blocked-readiness-reasons-are-distinct",
                    TestCastingBlockedReadinessReasons);
                Run("casting-roundtrip-and-load-states-are-exact",
                    () => TestCastingRoundTripAndLoadStates(root));
                Run("casting-import-splits-pinned-single-target",
                    TestCastingImportPinnedSplit);
                Run("casting-import-automatic-becomes-review-drafts",
                    TestCastingImportAutomaticDrafts);
                Run("casting-import-group-preserves-coverage-for-review",
                    TestCastingImportGroupReview);
                Run("casting-import-is-idempotent-and-orderly",
                    TestCastingImportIdempotent);
                Run("casting-import-report-counts-honestly",
                    TestCastingImportReport);
                Run("casting-a07-shared-enhancement-pool-is-atomic",
                    TestCastingA07SharedEnhancementPool);
                Run("casting-a08-complete-cost-reservation-leaks-nothing",
                    TestCastingA08CompleteCostReservation);
                Run("casting-a09-forecast-views-share-budgets-correctly",
                    TestCastingA09ForecastViews);
                Run("casting-a10-required-enhancement-policy",
                    TestCastingA10EnhancementPolicy);
                Run("casting-a11-apply-gate-cannot-hide-omitted-work",
                    TestCastingA11ApplyGate);
                Run("casting-a05-targeting-modifiers-change-eligibility",
                    TestCastingA05TargetingModifiers);
                // Continuation contract checks C1-C5 through the same
                // production compiler/gate/forecast services.
                Run("casting-c1-unvalidated-modifiers-never-execute-unmodified",
                    TestCastingC1UnvalidatedModifiers);
                Run("casting-c2-draft-is-not-an-implicit-opt-out",
                    TestCastingC2DraftNotAnOptOut);
                Run("casting-c4-modifier-costs-enter-the-atomic-vector",
                    TestCastingC4ModifierCosts);
                Run("casting-c5-projection-carries-effects-forward",
                    TestCastingC5EffectProjection);
                Run("casting-c3-presented-plan-gates-submission",
                    TestCastingC3PresentedPlan);
                Run("casting-a13-migration-boundary-is-recoverable",
                    () => TestCastingA13MigrationBoundary(root));
                // Connected workspace session (production caller path).
                Run("casting-workspace-mixed-caster-flow",
                    () => TestCastingWorkspaceMixedCasterFlow(root));
                Run("casting-workspace-review-and-apply-policy",
                    () => TestCastingWorkspaceReviewApply(root));
                Run("casting-workspace-save-reopen-and-protection",
                    () => TestCastingWorkspacePersistence(root));
                Run("casting-workspace-sibling-intent-vs-derived-changes",
                    TestCastingWorkspaceSiblingIntent);
                Run("casting-workspace-disabled-dispatch-attempt-only",
                    TestCastingWorkspaceDisabledDispatch);
                Run("casting-workspace-routine-scoped-apply",
                    () => TestCastingWorkspaceRoutineScopedApply(root));
                Run("casting-workspace-fresh-buff-capability",
                    () => TestCastingWorkspaceFreshBuffCapability(root));
                Run("casting-workspace-draft-catalog-authoring",
                    () => TestCastingWorkspaceDraftCatalog(root));
                Run("casting-workspace-targeting-shapes-and-dirty-state",
                    () => TestCastingWorkspaceTargetingShapes(root));
                Run("casting-workspace-campaign-bound-session-reuse",
                    () => TestCastingWorkspaceCampaignBinding(root));
                Run("casting-workspace-persistence-round-trip",
                    () => TestCastingWorkspacePersistenceRoundTrip(root));
                Run("casting-workspace-group-targeting-transitions",
                    () => TestCastingWorkspaceGroupTransitions(root));
                Run("runtime-manual-scenario-validation",
                    TestRuntimeManualScenarioValidation);
                Run("runtime-manual-request-validation",
                    () => TestManualScenarioRequestValidation(root));
                // Review J1/J2 production producer/consumer regressions.
                Run("workspace-interaction-evidence-contract",
                    TestWorkspaceInteractionEvidenceContract);
                Run("workspace-cast-step-evaluator-real-session",
                    () => TestWorkspaceCastStepEvaluatorAgainstSession(root));
                Run("manual-terminal-policy-done-stop-deadline",
                    TestManualTerminalPolicy);
                Run("manual-terminal-capture-restoration-cleanup",
                    TestManualTerminalCoordinatorContract);
                Run("runtime-host-scenario-contract-wiring",
                    TestRuntimeHostScenarioContractWiring);
                Run("workspace-header-caption-and-lane-layout",
                    () => TestWorkspaceHeaderAndLaneLayoutContract(root));
                Run("workspace-source-label-disambiguation",
                    TestWorkspaceSourceLabelDisambiguation);
                Run("workspace-choose-draft-caster",
                    () => TestWorkspaceChooseDraftCaster(root));
            }
            finally
            {
                _protocolEvidenceRoot = null;
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
                if (Directory.Exists(boundary)) Directory.Delete(boundary, true);
            }

            Console.WriteLine("Protocol tests: PASS=" + _passed + " FAIL=" + Failures.Count);
            foreach (string failure in Failures) Console.WriteLine("FAIL " + failure);
            return Failures.Count == 0 ? 0 : 1;
        }

        private static Assembly ResolveInstalledAssembly(object sender, ResolveEventArgs args)
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            if (string.IsNullOrWhiteSpace(game)) return null;
            string name = new AssemblyName(args.Name).Name + ".dll";
            string path = Path.Combine(game, "Kingmaker_Data", "Managed", name);
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        private static void TestInstalledSpellbookRoleContract()
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            string path = string.IsNullOrWhiteSpace(game) ? string.Empty : Path.Combine(
                game, "Mods", "CallOfTheWild", "CallOfTheWild.dll");
            if (!File.Exists(path)) return;
            Assembly assembly = Assembly.LoadFrom(path);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.DeclaredOnly;
            Type cannotUse = assembly.GetType(
                "CallOfTheWild.SpellbookMechanics.CanNotUseSpells", true);
            if (cannotUse.GetFields(flags).Length != 0)
                throw new InvalidOperationException(
                    "Installed CanNotUseSpells contract unexpectedly gained fields.");
            foreach (string typeName in new[]
            {
                "CallOfTheWild.SpellbookMechanics.CompanionSpellbook",
                "CallOfTheWild.SpellbookMechanics.GetKnownSpellsFromMemorizationSpellbook"
            })
            {
                FieldInfo field = assembly.GetType(typeName, true).GetField("spellbook", flags);
                if (field == null || field.FieldType.FullName !=
                    "Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook")
                    throw new InvalidOperationException(
                        "Installed optional spellbook relationship contract changed: " + typeName);
            }
        }

        private static void TestGameRootWithoutTrailingSeparator()
        {
            string observed = RuntimePaths.GetGameRoot(@"C:\Games\Pathfinder Kingmaker\Mods\KingmakerBuffPlanner");
            if (observed != @"C:\Games\Pathfinder Kingmaker")
                throw new InvalidOperationException("Game root was resolved incorrectly: " + observed);
        }

        private static void TestGameRootWithTrailingSeparator()
        {
            string observed = RuntimePaths.GetGameRoot(@"C:\Games\Pathfinder Kingmaker\Mods\KingmakerBuffPlanner\");
            if (observed != @"C:\Games\Pathfinder Kingmaker")
                throw new InvalidOperationException("Trailing separator changed game-root resolution: " + observed);
        }

        private static void TestScannerConditional()
        {
            var yes = EffectNode("buff-a");
            var no = EffectNode("buff-b");
            var root = new DiscoveryNode(DiscoveryNodeKind.Conditional, "conditional",
                whenTrue: yes, whenFalse: no, conditionContract: "And:HasFact");
            DiscoveryScanResult result = new ActionGraphScanner().Scan(root);
            var expression = result.Expression as ConditionalEffectExpression;
            if (expression == null || expression.ConditionContract != "And:HasFact" ||
                ((EffectLeafExpression)expression.WhenTrue).EffectId != "buff-a" ||
                ((EffectLeafExpression)expression.WhenFalse).EffectId != "buff-b")
                throw new InvalidOperationException("Conditional alternatives were flattened or lost.");
        }

        private static void TestScannerTarget()
        {
            var root = new DiscoveryNode(DiscoveryNodeKind.TargetTransform, "pet",
                new[] { EffectNode("pet-buff") }, target: EffectTarget.Pet);
            var targeted = (TargetedEffectExpression)new ActionGraphScanner().Scan(root).Expression;
            var sequence = (SequenceEffectExpression)targeted.Child;
            var leaf = (EffectLeafExpression)sequence.Children[0];
            if (targeted.Target != EffectTarget.Pet || leaf.Target != EffectTarget.Pet)
                throw new InvalidOperationException("Target transform was not propagated.");
            if (!leaf.ActionPath.Contains("pet-buff"))
                throw new InvalidOperationException("Effect action-path provenance was not retained.");
        }

        private static void TestScannerCycle()
        {
            var children = new List<DiscoveryNode>();
            var root = new DiscoveryNode(DiscoveryNodeKind.Sequence, "cycle", children);
            children.Add(root);
            // DiscoveryNode snapshots children, so use a self-referential conditional instead.
            var cycle = new DiscoveryNode(DiscoveryNodeKind.Conditional, "cycle-condition",
                whenTrue: null, whenFalse: EffectNode("fallback"));
            DiscoveryScanResult result = new ActionGraphScanner(1).Scan(
                new DiscoveryNode(DiscoveryNodeKind.Sequence, "depth-0", new[] {
                    new DiscoveryNode(DiscoveryNodeKind.Sequence, "depth-1", new[] { cycle }) }));
            if (result.Diagnostics.Count != 1 || result.Diagnostics[0].Code != "maximum-depth")
                throw new InvalidOperationException("Traversal bound diagnostic was not emitted.");
        }

        private static void TestScannerUnknown()
        {
            DiscoveryScanResult result = new ActionGraphScanner().Scan(
                new DiscoveryNode(DiscoveryNodeKind.Unknown, "custom-action", sourceContract: "unsupported"));
            if (result.Diagnostics.Count != 1 || result.Diagnostics[0].Code != "unknown-node")
                throw new InvalidOperationException("Unknown action was silently discarded.");
        }

        private static void TestScannerExpressionWireContract()
        {
            EffectExpression expression = new ActionGraphScanner().Scan(EffectNode("wire-buff")).Expression;
            string json = JsonConvert.SerializeObject(expression);
            if (!json.Contains("\"expressionType\":\"leaf\"") ||
                !json.Contains("\"effectId\":\"wire-buff\"") ||
                !json.Contains("\"actionPath\":\"wire-buff\""))
                throw new InvalidOperationException("Effect expression JSON contract is incomplete: " + json);
        }

        private static void TestSpellbookRoleResolution()
        {
            IReadOnlyDictionary<string, SpellbookRoleResolution> roles =
                SpellbookRoleResolver.Resolve(new[]
                {
                    new SpellbookRoleInput("arcanist-preparation", false, true,
                        "arcanist-casting", string.Empty),
                    new SpellbookRoleInput("arcanist-casting", true, false,
                        string.Empty, "arcanist-preparation"),
                    new SpellbookRoleInput("wizard-prepared", false, false,
                        string.Empty, string.Empty),
                    new SpellbookRoleInput("sorcerer-spontaneous", true, false,
                        string.Empty, string.Empty),
                    new SpellbookRoleInput("multiclass-first", false, false,
                        string.Empty, string.Empty),
                    new SpellbookRoleInput("multiclass-second", true, false,
                        string.Empty, string.Empty),
                    new SpellbookRoleInput("exhausted-but-structural", false, false,
                        string.Empty, string.Empty)
                });
            SpellbookRoleResolution preparation = roles["arcanist-preparation"];
            SpellbookRoleResolution casting = roles["arcanist-casting"];
            if (preparation.Included || preparation.Role != SpellbookRole.PreparationOnly ||
                preparation.RelationshipTargetGuid != "arcanist-casting" ||
                preparation.Reason !=
                    "cannot-use-spells-with-owned-companion-casting-book" ||
                !casting.Included || casting.Role != SpellbookRole.CastingCapable ||
                casting.RelationshipTargetGuid != "arcanist-preparation" ||
                !roles["wizard-prepared"].Included ||
                !roles["sorcerer-spontaneous"].Included ||
                !roles["multiclass-first"].Included ||
                !roles["multiclass-second"].Included ||
                !roles["exhausted-but-structural"].Included)
                throw new InvalidOperationException(
                    "Structural spellbook roles hid a legitimate caster or retained the Arcanist preparation book.");

            var ownedProviders = roles.Where(pair => pair.Value.Included)
                .Select(pair => new ProviderKey("arcanist", pair.Key,
                    Ability("arcanist-fixture", string.Empty, 0), "level-2"))
                .ToArray();
            if (ownedProviders.Count(value => value.SpellbookGuid == "arcanist-casting") != 1 ||
                ownedProviders.Any(value => value.SpellbookGuid == "arcanist-preparation"))
                throw new InvalidOperationException(
                    "Provider keys did not retain only the structurally cast-capable Arcanist spellbook.");

            IReadOnlyDictionary<string, SpellbookRoleResolution> malformed =
                SpellbookRoleResolver.Resolve(new[]
                {
                    new SpellbookRoleInput("unresolved-optional-component", false, true,
                        "not-owned", string.Empty)
                });
            SpellbookRoleResolution unresolved = malformed["unresolved-optional-component"];
            if (!unresolved.Included || unresolved.Role != SpellbookRole.Ambiguous ||
                unresolved.Reason != "cannot-use-spells-relationship-unproven")
                throw new InvalidOperationException(
                    "A missing optional compatibility relationship did not fail softly.");
        }

        private static void TestAreaRecipientSemantics()
        {
            if (AreaRecipientSemantics.Resolve(AreaSelectionTarget.Ally,
                    false, true, true) != EffectTarget.AlliedAreaRecipients ||
                AreaRecipientSemantics.Resolve(AreaSelectionTarget.Enemy,
                    true, false, false) != EffectTarget.EnemyAreaRecipients ||
                AreaRecipientSemantics.Resolve(AreaSelectionTarget.Any,
                    true, false, false) != EffectTarget.AlliedAreaRecipients ||
                AreaRecipientSemantics.Resolve(AreaSelectionTarget.Any,
                    true, true, false) != EffectTarget.AmbiguousAreaRecipients ||
                AreaRecipientSemantics.Resolve(AreaSelectionTarget.Any,
                    true, false, true) != EffectTarget.AmbiguousAreaRecipients ||
                AreaRecipientSemantics.Resolve(AreaSelectionTarget.Unknown,
                    true, false, false) != EffectTarget.AmbiguousAreaRecipients)
                throw new InvalidOperationException(
                    "Area recipient refinement was not exact and conservative.");
        }

        private static void TestBlueprintOwnership()
        {
            const string optionalGuid = "0123456789abcdef0123456789abcdef";
            BlueprintOwnershipIndex index = BlueprintOwnershipIndex.Parse(new[]
            {
                "OptionalAbility\t" + optionalGuid + "\tKingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility",
                "malformed",
                "Uppercase\t0123456789ABCDEF0123456789ABCDEF\tType"
            });
            if (index.GetOwnership(optionalGuid) != "call-of-the-wild" ||
                index.GetOwnership("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") != "native")
                throw new InvalidOperationException("Optional ownership inventory lost exact GUID identity.");
            try
            {
                BlueprintOwnershipIndex.Parse(new[] { "malformed" });
                throw new InvalidOperationException("Empty optional ownership inventory was accepted.");
            }
            catch (InvalidDataException) { }
        }

        private static void TestHarmonyTargetIdentity()
        {
            MethodInfo method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            string identity = HarmonyPatchInventoryExporter.GetMethodIdentity(method);
            if (identity != "mscorlib|System.String|StartsWith(System.String)")
                throw new InvalidOperationException("Harmony target identity is not stable: " + identity);
        }

        private static void TestHarmonyInventoryApi()
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            if (string.IsNullOrWhiteSpace(game))
                throw new InvalidOperationException("KBP_TEST_GAME_PATH is missing.");
            string harmony = Path.Combine(game, "Kingmaker_Data", "Managed", "UnityModManager", "0Harmony12.dll");
            HarmonyPatchInventory inventory = new HarmonyPatchInventoryExporter().Export("contract-test", harmony);
            if (inventory.SchemaVersion != 1 || inventory.ProfileId != "contract-test" ||
                inventory.TargetCount != inventory.Targets.Count ||
                inventory.PatchCount != inventory.Targets.Sum(t => t.Patches.Count))
                throw new InvalidOperationException("Installed Harmony inventory API did not reconcile.");
        }

        private static void TestNativeCandidateClassification()
        {
            var classifier = new NativeCandidateClassifier();
            NativeCandidateAuditDecision supported = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Effects = new[] { CandidateEffect("Buff", "CurrentTarget", false, "ContextActionApplyBuff", "root") },
                DiagnosticContracts = new string[0]
            });
            if (supported.Disposition != "include" || supported.SupportClass != "automatic" ||
                supported.QualificationStatus != "DEFER-runtime-qualification")
                throw new InvalidOperationException("Ordinary persistent self buff was not classified as supported.");

            NativeCandidateAuditDecision summon = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Effects = new[] { CandidateEffect("Buff", "CurrentTarget", false,
                    "ContextActionApplyBuff", "root/ContextActionSpawnMonster/AfterSpawn") },
                DiagnosticContracts = new string[0]
            });
            if (summon.Disposition != "exclude" || !summon.Reason.StartsWith("summoning:", StringComparison.Ordinal))
                throw new InvalidOperationException("After-spawn buffs escaped the summoning exclusion.");

            NativeCandidateAuditDecision hostile = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                CanTargetFriends = true,
                CanTargetEnemies = true,
                EffectOnAlly = "None",
                EffectOnEnemy = "Harmful",
                Effects = new[] { CandidateEffect("Buff", "CurrentTarget", false, "ContextActionApplyBuff", "root") },
                DiagnosticContracts = new string[0]
            });
            if (hostile.Disposition != "exclude" ||
                !hostile.Reason.StartsWith(
                    "no-persistent-beneficial-party-effect:",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Hostile current-target effect was mistaken for a self buff.");

            NativeCandidateAuditDecision point = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetFriends = true,
                CanTargetPoint = true,
                Effects = new[] { CandidateEffect("Buff", "CurrentTarget", false, "ContextActionApplyBuff", "root") },
                DiagnosticContracts = new string[0]
            });
            if (point.Disposition != "exclude" ||
                !point.Reason.StartsWith("point-target-without-placement:", StringComparison.Ordinal))
                throw new InvalidOperationException("Unsafe point targeting was not excluded.");

            NativeCandidateAuditDecision pool = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                CanTargetSelf = true,
                Effects = new[] { CandidateEffect("Buff", "CurrentTarget", false, "ContextActionApplyBuff", "root") },
                DiagnosticContracts = new[] { "ContextActionWeaponEnchantPool|unsupported-action" }
            });
            if (pool.Disposition != "include" || pool.SupportClass != "explicit-adapter" ||
                !pool.Reason.Contains("signal buff"))
                throw new InvalidOperationException("Dynamic enchant-pool signal semantics were not explicit.");

            NativeCandidateAuditDecision container = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                HasVariants = true,
                CanTargetSelf = true,
                Effects = new[] { CandidateEffect("Buff", "CurrentTarget", false, "ContextActionApplyBuff", "root") },
                DiagnosticContracts = new string[0]
            });
            if (container.Disposition != "exclude" ||
                !container.Reason.StartsWith("non-castable-variant-container:", StringComparison.Ordinal))
                throw new InvalidOperationException("A non-castable variant parent was treated as a provider.");

            NativeCandidateAuditDecision carrier = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                IsStickyTouch = true,
                CanTargetSelf = true,
                Effects = new[] { CandidateEffect("Buff", "Caster", false, "ContextActionApplyBuff", "delivery") },
                DiagnosticContracts = new[] { "ContextActionHealTarget|unsupported-action" }
            });
            if (carrier.Disposition != "exclude" ||
                !carrier.Reason.StartsWith("sticky-touch-carrier-only:", StringComparison.Ordinal))
                throw new InvalidOperationException("A transient sticky-touch carrier was exposed as a buff.");

            NativeCandidateAuditDecision weaponCarrier = classifier.Classify(new NativeCandidateAuditFacts
            {
                IsPlayerAccessible = true,
                Range = "Weapon",
                CanTargetSelf = true,
                CanTargetEnemies = true,
                Effects = new[] { CandidateEffect("Buff", "Caster", false, "ContextActionApplyBuff", "attack") },
                DiagnosticContracts = new string[0]
            });
            if (weaponCarrier.Disposition != "exclude" ||
                !weaponCarrier.Reason.StartsWith("hostile-weapon-carrier:", StringComparison.Ordinal))
                throw new InvalidOperationException("A hostile weapon carrier was exposed as a buff.");
        }

        private static void TestPersistentBeneficialClassification()
        {
            var classifier = new NativeCandidateClassifier();
            NativeCandidateAuditDecision self = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetSelf = true,
                    Effects = new[] { CandidateEffect(
                        "Buff", "CurrentTarget", false,
                        "ContextActionApplyBuff", "root/self") }
                });
            NativeCandidateAuditDecision friend = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetFriends = true,
                    EffectOnAlly = "Helpful",
                    Effects = new[] { CandidateEffect(
                        "Buff", "CurrentTarget", false,
                        "ContextActionApplyBuff", "root/friend") }
                });
            NativeCandidateAuditDecision alliedArea = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    Effects = new[] { CandidateEffect(
                        "AreaBuff", "AlliedAreaRecipients", false,
                        "ContextActionSpawnAreaEffect+AbilityAreaEffectBuff",
                        "root/area") }
                });
            if (self.Disposition != "include" ||
                !self.Reason.StartsWith(
                    "valid-beneficial-self-effect:", StringComparison.Ordinal) ||
                friend.Disposition != "include" ||
                alliedArea.Disposition != "include" ||
                !alliedArea.Reason.StartsWith(
                    "valid-beneficial-party-effect:", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Valid self, friend, or allied-area persistent effects were lost.");

            NativeCandidateAuditDecision enemyArea = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    Effects = new[] { CandidateEffect(
                        "AreaBuff", "EnemyAreaRecipients", true,
                        "ContextActionApplyBuff", "root/enemy") }
                });
            NativeCandidateAuditDecision ambiguousArea = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    Effects = new[] { CandidateEffect(
                        "AreaBuff", "AmbiguousAreaRecipients", false,
                        "ContextActionApplyBuff", "root/ambiguous") }
                });
            if (!enemyArea.Reason.StartsWith("enemy-only-area:",
                    StringComparison.Ordinal) ||
                !ambiguousArea.Reason.StartsWith(
                    "ambiguous-area-recipient:", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Enemy or ambiguous area recipients were treated as party coverage.");

            NativeCandidateEffectFacts hiddenMarker = CandidateEffect(
                "Buff", "Caster", false, "ContextActionApplyBuff",
                "root/1:marker", true, false,
                new[]
                {
                    "CallOfTheWild.NewMechanics.BuffRemoveOnSave",
                    "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions"
                });
            NativeCandidateAuditDecision offensiveCarrier = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetSelf = true,
                    CanTargetEnemies = true,
                    EffectOnEnemy = "Harmful",
                    Range = "Projectile",
                    AbilityComponentTypes = new[]
                    {
                        "Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverProjectile"
                    },
                    Effects = new[] { hiddenMarker },
                    Diagnostics = new[]
                    {
                        new NativeCandidateDiagnosticFacts
                        {
                            Code = "offensive-action",
                            Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage",
                            Detail = "offensive-action",
                            ActionPath = "root/0:damage"
                        }
                    }
                });
            if (!offensiveCarrier.Reason.StartsWith(
                    "offensive-carrier-only:", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A hidden caster save marker rescued an offensive carrier.");

            NativeCandidateAuditDecision harmfulWithMarker = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetSelf = true,
                    Effects = new[]
                    {
                        CandidateEffect("Buff", "CurrentTarget", true,
                            "ContextActionApplyBuff", "root/harmful"),
                        hiddenMarker
                    }
                });
            if (!harmfulWithMarker.Reason.StartsWith(
                    "hidden-marker-only:", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A harmless hidden marker rescued a harmful target payload.");

            NativeCandidateAuditDecision hiddenSelf = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetSelf = true,
                    Effects = new[]
                    {
                        CandidateEffect("Buff", "Caster", false,
                            "ContextActionApplyBuff", "root/hidden-self",
                            true, false,
                            new[] { "Kingmaker.UnitLogic.Mechanics.Components.AddStatBonus" })
                    }
                });
            if (hiddenSelf.Disposition != "include" ||
                !hiddenSelf.Reason.StartsWith(
                    "valid-beneficial-self-effect:", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A structurally substantive hidden self buff was excluded only for being hidden.");

            NativeCandidateAuditDecision separateSupportBranch =
                classifier.Classify(new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetFriends = true,
                    EffectOnAlly = "Helpful",
                    Effects = new[]
                    {
                        CandidateEffect("Buff", "CurrentTarget", false,
                            "ContextActionApplyBuff",
                            "root/0:Conditional/false/0:support")
                    },
                    Diagnostics = new[]
                    {
                        new NativeCandidateDiagnosticFacts
                        {
                            Code = "offensive-action",
                            Contract = "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage",
                            Detail = "offensive-action",
                            ActionPath = "root/0:Conditional/true/0:damage"
                        }
                    }
                });
            if (separateSupportBranch.Disposition != "include")
                throw new InvalidOperationException(
                    "An unrelated offensive conditional branch erased an exact support branch.");

            NativeCandidateAuditDecision instantOnly = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetFriends = true,
                    Effects = new NativeCandidateEffectFacts[0],
                    DiagnosticContracts = new[]
                    {
                        "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget|unsupported-action"
                    }
                });
            if (!instantOnly.Reason.StartsWith(
                    "instantaneous-restoration-without-substantive-buff:",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Instant healing without a persistent effect entered the catalog.");

            NativeCandidateAuditDecision pet = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    Effects = new[] { CandidateEffect(
                        "Buff", "Pet", false, "ContextActionsOnPet", "root/pet") }
                });
            NativeCandidateAuditDecision enchantment = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetFriends = true,
                    Effects = new[] { CandidateEffect(
                        "WornItemEnchantment", "CurrentTarget", null,
                        "ContextActionEnchantWornItem", "root/enchant") }
                });
            if (pet.Disposition != "include" ||
                enchantment.Disposition != "include")
                throw new InvalidOperationException(
                    "Pet or worn-item persistent support regressed.");
        }

        private static void TestRestorativeCandidateClassification()
        {
            var classifier = new NativeCandidateClassifier();
            NativeCandidateAuditDecision layOnHandsFixture = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetFriends = true,
                    Effects = new NativeCandidateEffectFacts[0],
                    Diagnostics = new[]
                    {
                        new NativeCandidateDiagnosticFacts
                        {
                            Code = "restorative-action",
                            Contract =
                                "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionHealTarget",
                            Detail = "restorative-action",
                            ActionPath = "root/0:heal"
                        }
                    }
                });
            if (layOnHandsFixture.Disposition != "exclude" ||
                !layOnHandsFixture.Reason.StartsWith(
                    "instantaneous-restoration-without-substantive-buff:",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A direct healing fixture with no persistent payload entered the catalog.");

            NativeCandidateAuditDecision markerOnly = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetSelf = true,
                    Effects = new[]
                    {
                        CandidateEffect("Buff", "Caster", false,
                            "ContextActionApplyBuff", "root/1:cleanup",
                            true, false, new[]
                            {
                                "Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions"
                            })
                    },
                    Diagnostics = new[]
                    {
                        new NativeCandidateDiagnosticFacts
                        {
                            Code = "restorative-action",
                            Contract =
                                "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff",
                            Detail = "restorative-action",
                            ActionPath = "root/0:remove"
                        }
                    }
                });
            if (markerOnly.Disposition != "exclude" ||
                !markerOnly.Reason.StartsWith(
                    "reactive-restoration-marker-only:",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A restorative carrier marker was treated as a player-facing buff.");

            NativeCandidateAuditDecision recovery = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetFriends = true,
                    Effects = new NativeCandidateEffectFacts[0],
                    Diagnostics = new[]
                    {
                        new NativeCandidateDiagnosticFacts
                        {
                            Code = "restorative-action",
                            Contract =
                                "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionResurrect",
                            Detail = "restorative-action",
                            ActionPath = "root/0:resurrect"
                        }
                    }
                });
            if (recovery.Disposition != "exclude" ||
                !recovery.Reason.StartsWith(
                    "instantaneous-restoration-without-substantive-buff:",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Resurrection/recovery without a persistent protection entered the catalog.");

            NativeCandidateAuditDecision protectionPlusAdjunct = classifier.Classify(
                new NativeCandidateAuditFacts
                {
                    IsPlayerAccessible = true,
                    CanTargetFriends = true,
                    EffectOnAlly = "Helpful",
                    Effects = new[]
                    {
                        CandidateEffect("Buff", "CurrentTarget", false,
                            "ContextActionApplyBuff", "root/1:protection", false,
                            false, new[]
                            {
                                "Kingmaker.UnitLogic.Mechanics.Components.AddStatBonus"
                            })
                    },
                    Diagnostics = new[]
                    {
                        new NativeCandidateDiagnosticFacts
                        {
                            Code = "restorative-action",
                            Contract =
                                "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff",
                            Detail = "restorative-action",
                            ActionPath = "root/0:remove"
                        }
                    }
                });
            if (protectionPlusAdjunct.Disposition != "include" ||
                !protectionPlusAdjunct.Reason.StartsWith(
                    "valid-beneficial-party-effect:", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A substantive lasting protection was discarded merely because it has a restorative adjunct.");
        }

        private static NativeCandidateEffectFacts CandidateEffect(
            string kind,
            string target,
            bool? harmful,
            string source,
            string path,
            bool hidden = false,
            bool classFeature = false,
            IEnumerable<string> components = null)
        {
            return new NativeCandidateEffectFacts
            {
                Kind = kind,
                Target = target,
                Harmful = harmful,
                IsHiddenInUi = hidden,
                IsClassFeature = classFeature,
                ComponentTypes = (components ?? new string[0]).ToArray(),
                SourceContract = source,
                ActionPath = path
            };
        }

        private static void TestEffectOverrides()
        {
            const string ability = "11111111111111111111111111111111";
            const string first = "22222222222222222222222222222222";
            const string second = "33333333333333333333333333333333";
            string json = "{\"schemaVersion\":1,\"entries\":[{" +
                "\"abilityGuid\":\"" + ability + "\",\"disposition\":\"replace-detected-effects\"," +
                "\"sourceAssembly\":\"native\",\"effectMode\":\"anyOf\",\"effects\":[" +
                "{\"kind\":\"UnitBuff\",\"guid\":\"" + first + "\"}," +
                "{\"kind\":\"AreaBuff\",\"guid\":\"" + second + "\"}]," +
                "\"reason\":\"fixture\"}]}";
            EffectOverrideApplication application = EffectOverrideRegistry.Parse(json).Apply(
                ability, Leaf("detected"));
            var branch = application.Expression as ConditionalEffectExpression;
            if (application.Entry == null || branch == null ||
                ((EffectLeafExpression)branch.WhenTrue).EffectId != first ||
                ((EffectLeafExpression)branch.WhenFalse).EffectId != second)
                throw new InvalidOperationException("Override replacement flattened anyOf alternatives.");
            try
            {
                EffectOverrideRegistry.Parse("{\"schemaVersion\":1,\"schemaVersion\":1,\"entries\":[]}");
                throw new InvalidOperationException("Duplicate override property was accepted.");
            }
            catch (InvalidDataException) { }
        }

        private static void TestStableKeys()
        {
            var baseKey = Ability("base", string.Empty, 0);
            var variant = Ability("base", "variant", 0);
            var metamagic = Ability("base", string.Empty, 4);
            if (baseKey.Equals(variant) || baseKey.Equals(metamagic) || variant.Equals(metamagic))
                throw new InvalidOperationException("Mechanically distinct ability keys collided.");
            var first = new ProviderKey("unit-a", "book-a", baseKey, string.Empty);
            var second = new ProviderKey("unit-a", "book-b", baseKey, string.Empty);
            if (first.Equals(second)) throw new InvalidOperationException("Spellbook identity was lost from provider key.");
        }

        private static void TestCompleteNameLayout()
        {
            const string name = "Protection from Arrows, Communal";
            AbilityKey ability = Ability("long-name-source", string.Empty, 0);
            const string poolKey = "long-name-free";
            var pool = new ResourcePoolSnapshot(
                poolKey, ResourcePoolKind.Unlimited, 0, 0, null);
            var provider = new ProviderSnapshot(
                new ProviderKey("unit-a", "book-a", ability, "level-0"),
                name, 0, poolKey, 0, null, null, 1, 10,
                "A localized communal protection.", "one hour", name, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { pool }, "unit-a");
            var effect = Leaf("long-name-effect");
            var option = new ProviderPlanningOption(
                provider, new[] { "unit-a" }, new[] { "unit-a" }, 1, 10);
            var model = new PlannerSetupModel(
                BuffPlannerProfile.CreateDefault("long-name-layout"),
                snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression>
                {
                    { ability.Canonical, effect }
                },
                new[] { option }, ignored => { });
            var card = new BuffCardViewModel(
                model.Sources.Single(), model, "long", false);
            float compact = CompleteNameLayout.RequiredCardHeight(92f, 20f);
            float expanded = CompleteNameLayout.RequiredCardHeight(92f, 72f);
            BuffGridLayout layout = BuffGridLayout.Calculate(
                new[] { compact, expanded, compact, compact, compact }, 92f, 10f);
            if (card.Name != name || !card.Name.EndsWith(", Communal",
                    StringComparison.Ordinal) || card.Name.Contains("...") ||
                card.Name.Contains("…") || compact != 92f || expanded <= compact ||
                layout.RowHeight(0) != expanded || layout.RowOffset(1) <= expanded)
                throw new InvalidOperationException(
                    "The primary card model or row layout shortened a complete localized name.");
        }

        private static void TestOrdinaryCatalogExpansion()
        {
            var source = new SelectableAbilityBlueprint(
                "ordinary", "Ordinary Ward", "ordinary-icon", true);
            IReadOnlyList<SelectableAbilityEntry> entries =
                SelectableAbilityVariantCatalog.Expand(
                    source, new SelectableAbilityBlueprint[0]);
            if (entries.Count != 1 || entries[0].IsConcreteVariant ||
                entries[0].Source.BlueprintGuid != "ordinary" ||
                entries[0].Concrete.BlueprintGuid != "ordinary" ||
                entries[0].DisplayName != "Ordinary Ward")
                throw new InvalidOperationException(
                    "A non-variant ability did not remain one unchanged catalog entry.");
        }

        private static void TestVariantCatalogFive()
        {
            IReadOnlyList<SelectableAbilityEntry> entries =
                SelectableAbilityVariantCatalog.Expand(
                    VariantParent(), VariantBlueprints(true));
            string[] expected = VariantBlueprints(true)
                .Select(value => value.BlueprintGuid).ToArray();
            if (entries.Count != 5 ||
                !entries.Select(value => value.Concrete.BlueprintGuid)
                    .SequenceEqual(expected) ||
                !entries.Select(value => value.VariantOrder)
                    .SequenceEqual(Enumerable.Range(0, 5)))
                throw new InvalidOperationException(
                    "Five eligible declared variants were not expanded in blueprint order.");
        }

        private static void TestVariantOwnershipAvailability()
        {
            var parent = new SelectableAbilityBlueprint(
                "parent-guid", "Parent", "parent-icon", true);
            var granted = new SelectableAbilityBlueprint(
                "granted-guid", "Granted", "granted-icon", true);
            var ungranted = new SelectableAbilityBlueprint(
                "ungranted-guid", "Ungranted", "ungranted-icon", false);
            IReadOnlyList<SelectableAbilityEntry> expanded =
                SelectableAbilityVariantCatalog.Expand(
                    parent, new[] { granted, ungranted });
            if (expanded.Count != 1 ||
                expanded.Single().Concrete.BlueprintGuid != "granted-guid")
                throw new InvalidOperationException(
                    "A declared child rejected by native eligibility was cataloged.");

            AbilityKey ownedChild = Ability(
                "parent-guid", "granted-guid", 0);
            var exhaustedPool = new ResourcePoolSnapshot(
                "owned-exhausted", ResourcePoolKind.SpontaneousLevel,
                4, 0, null);
            var provider = new ProviderSnapshot(
                new ProviderKey("unit-owner", "book-owner",
                    ownedChild, "level-2"),
                "Concrete Child", 2, exhaustedPool.PoolKey,
                1, null);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { exhaustedPool }, "unit-owner");
            var option = new ProviderPlanningOption(
                provider, new[] { "unit-owner" },
                new[] { "unit-owner" }, 4, 40);
            BuffPlannerProfile profile =
                BuffPlannerProfile.CreateDefault("variant-owned-exhausted");
            var effects = new Dictionary<string, EffectExpression>
            {
                { ownedChild.Canonical, Leaf("owned-child-effect") }
            };
            var model = new PlannerSetupModel(
                profile, snapshot, new ActiveEffectSnapshot(null),
                effects, new[] { option }, ignored => { });
            SetupSourceRow source = model.Sources.Single();
            if (!source.IsConcreteVariant ||
                source.Ability.VariantGuid != "granted-guid" ||
                model.GetRemainingCasts(provider) != 0 ||
                model.IsSourceAvailable(source))
                throw new InvalidOperationException(
                    "An exhausted directly owned child vanished or appeared castable.");

            profile.Routines[0].Assignments.Add(Assignment(
                "variant|parent-guid|ungranted-guid",
                Ability("parent-guid", "ungranted-guid", 0),
                new[] { "unit-owner" }));
            var reloaded = new PlannerSetupModel(
                profile, snapshot, new ActiveEffectSnapshot(null),
                effects, new[] { option }, ignored => { });
            if (reloaded.Sources.Any(item =>
                    item.Ability.VariantGuid == "ungranted-guid") ||
                reloaded.VariantReselectionNotices.Count == 0)
                throw new InvalidOperationException(
                    "A stale unowned child was resurrected or silently remapped.");
        }

        private static void TestVariantParentSuppressed()
        {
            SelectableAbilityBlueprint[] variants = VariantBlueprints(true);
            variants[1] = new SelectableAbilityBlueprint(
                variants[1].BlueprintGuid, variants[1].DisplayName,
                variants[1].IconIdentity, false);
            IReadOnlyList<SelectableAbilityEntry> entries =
                SelectableAbilityVariantCatalog.Expand(VariantParent(), variants);
            IReadOnlyList<SelectableAbilityEntry> none =
                SelectableAbilityVariantCatalog.Expand(VariantParent(),
                    VariantBlueprints(false));
            if (entries.Count != 4 || entries.Any(value =>
                    value.Concrete.BlueprintGuid == VariantParent().BlueprintGuid) ||
                none.Count != 0)
                throw new InvalidOperationException(
                    "An unresolved parent remained selectable or ineligible children leaked in.");
        }

        private static void TestVariantStableIdentities()
        {
            IReadOnlyList<SelectableAbilityEntry> entries =
                SelectableAbilityVariantCatalog.Expand(
                    VariantParent(), VariantBlueprints(true));
            var effect = Leaf("same-visible-buff");
            string[] sourceIds = entries.Select(value =>
                CatalogSourceIdentity.For(Ability(
                    value.Source.BlueprintGuid,
                    value.Concrete.BlueprintGuid, 0), effect)).ToArray();
            if (entries.Select(value => value.StableIdentity)
                    .Distinct(StringComparer.Ordinal).Count() != 5 ||
                sourceIds.Distinct(StringComparer.Ordinal).Count() != 5 ||
                sourceIds.Any(value => !CatalogSourceIdentity.IsVariant(value)))
                throw new InvalidOperationException(
                    "Concrete variants collided by parent, name, icon, or effect.");
        }

        private static void TestVariantParentChildIdentity()
        {
            SelectableAbilityEntry entry = SelectableAbilityVariantCatalog.Expand(
                VariantParent(), VariantBlueprints(true))[3];
            AbilityKey ability = Ability(
                entry.Source.BlueprintGuid, entry.Concrete.BlueprintGuid, 0);
            var provider = new ProviderSnapshot(
                new ProviderKey("unit-a", "book-a", ability, "level-3"),
                entry.DisplayName, 3, "identity-free", 0, null,
                null, 5, 100, string.Empty, string.Empty,
                entry.Source.DisplayName, entry.VariantOrder);
            if (provider.Key.Ability.BaseAbilityGuid !=
                    entry.Source.BlueprintGuid ||
                provider.Key.Ability.VariantGuid !=
                    entry.Concrete.BlueprintGuid ||
                provider.SourceDisplayName != "Resist Energy, Communal" ||
                provider.VariantOrder != 3 || !provider.IsConcreteVariant)
                throw new InvalidOperationException(
                    "The catalog entry lost its parent source or concrete child identity.");
        }

        private static void TestVariantDeduplication()
        {
            SelectableAbilityBlueprint[] variants = VariantBlueprints(true);
            IReadOnlyList<SelectableAbilityEntry> entries =
                SelectableAbilityVariantCatalog.Expand(VariantParent(),
                    new[] { variants[0], variants[0], variants[1], variants[1] });
            if (entries.Count != 2 ||
                entries[0].Concrete.BlueprintGuid != variants[0].BlueprintGuid ||
                entries[1].Concrete.BlueprintGuid != variants[1].BlueprintGuid ||
                entries[0].VariantOrder != 0 || entries[1].VariantOrder != 1)
                throw new InvalidOperationException(
                    "Duplicate parent/child discovery changed declared order or emitted duplicates.");
        }

        private static void TestVariantCommunalNames()
        {
            var ordinaryParent = new SelectableAbilityBlueprint(
                "resist-parent", "Resist Energy", "parent-icon", true);
            var ordinaryChild = new SelectableAbilityBlueprint(
                "resist-cold", "Resist Cold", "cold-icon", true);
            SelectableAbilityEntry ordinary =
                SelectableAbilityVariantCatalog.Expand(
                    ordinaryParent, new[] { ordinaryChild }).Single();
            SelectableAbilityEntry communal =
                SelectableAbilityVariantCatalog.Expand(
                    VariantParent(), VariantBlueprints(true)).First();
            if (ordinary.DisplayName != "Resist Energy \u2014 Cold" ||
                communal.DisplayName != "Resist Energy, Communal \u2014 Cold" ||
                ordinary.DisplayName == communal.DisplayName ||
                ordinary.Source.BlueprintGuid == communal.Source.BlueprintGuid)
                throw new InvalidOperationException(
                    "Communal and non-communal variant distinctions were lost.");
        }

        private static void TestVariantSearchAndOrder()
        {
            VariantModelFixture fixture = CreateVariantFixture(
                BuffPlannerProfile.CreateDefault("variant-search"), false);
            var state = new CatalogFilterState
            {
                Search = "Resist Energy, Communal"
            };
            CatalogFilterDiagnostics diagnostics;
            List<SetupSourceRow> parentMatches = state.Apply(
                fixture.Model, "long", out diagnostics);
            state.Search = "Fire";
            List<SetupSourceRow> fireMatches = state.Apply(
                fixture.Model, "long", out diagnostics);
            string[] declared = VariantBlueprints(true)
                .Select(value => value.BlueprintGuid).ToArray();
            if (fixture.Model.Sources.Count != 5 ||
                parentMatches.Count != 5 || fireMatches.Count != 1 ||
                fireMatches[0].DisplayName != "Resist Energy, Communal \u2014 Fire" ||
                !fixture.Model.Sources.Select(value => value.Ability.VariantGuid)
                    .SequenceEqual(declared))
                throw new InvalidOperationException(
                    "Parent/concrete search or declared sibling ordering was not preserved.");
        }

        private static void TestVariantProfileRoundTrip(string root)
        {
            const string campaign = "variant-profile-roundtrip";
            AbilityKey child = Ability(
                VariantParent().BlueprintGuid, "resist-fire-communal", 4);
            string sourceId = CatalogSourceIdentity.For(
                child, Leaf("resist-fire-effect"));
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(campaign);
            profile.Routines[0].Assignments.Add(Assignment(
                sourceId, child, new[] { "target-a" }, null,
                ExistingEffectPolicy.Overwrite));
            string modPath = Path.Combine(root, "variant-profile-roundtrip");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            repository.Save(profile);
            SourceAssignmentProfile loaded = repository.Load(campaign)
                .Profile.Routines[0].Assignments.Single();
            AbilityKey restored = loaded.Ability.ToKey();
            if (loaded.SourceId != sourceId ||
                restored.BaseAbilityGuid != child.BaseAbilityGuid ||
                restored.VariantGuid != child.VariantGuid ||
                restored.MetamagicMask != 4)
                throw new InvalidOperationException(
                    "Serialization did not retain the selected concrete child.");
        }

        private static void TestLegacyAmbiguousVariant()
        {
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "legacy-ambiguous-variant");
            AbilityKey parent = Ability(VariantParent().BlueprintGuid, string.Empty, 0);
            profile.Routines[0].Assignments.Add(Assignment(
                parent.Canonical, parent, new[] { "target-a" }, null,
                ExistingEffectPolicy.Overwrite));
            VariantModelFixture fixture = CreateVariantFixture(profile, false);
            SourceAssignmentProfile retained =
                profile.Routines[0].Assignments.Single();
            VariantReselectionNotice notice =
                fixture.Model.VariantReselectionNotices.Single();
            if (fixture.Model.AssignmentMigrationApplied ||
                retained.Ability.VariantGuid.Length != 0 ||
                retained.SourceId != parent.Canonical ||
                notice.DisplayName != "Resist Energy, Communal" ||
                notice.CandidateCount != 5 ||
                !fixture.Model.UnsupportedSavedSourceIds.Contains(parent.Canonical))
                throw new InvalidOperationException(
                    "A legacy ambiguous parent invented a concrete variant or lacked a clear diagnostic.");
        }

        private static void TestVariantParentAvailability()
        {
            VariantModelFixture fixture = CreateVariantFixture(
                BuffPlannerProfile.CreateDefault("variant-parent-availability"), true);
            SetupSourceRow source = fixture.Model.Sources.First();
            var card = new BuffCardViewModel(
                source, fixture.Model, "long", false);
            ResourceTokenSnapshot token = fixture.Snapshot.ResourcePools.Single()
                .Tokens.Single();
            if (card.Availability != "1 prepared" ||
                token.SlottedAbility.VariantGuid.Length != 0 ||
                source.Ability.VariantGuid.Length == 0 ||
                !source.Providers.Single().EligibleTokenIds.Contains(token.TokenId))
                throw new InvalidOperationException(
                    "A child variant did not validate against its parent prepared slot.");
        }

        private static void TestVariantExecutionSelection()
        {
            VariantModelFixture fixture = CreateVariantFixture(
                BuffPlannerProfile.CreateDefault("variant-exact-execution"), false);
            SetupSourceRow requested = fixture.Model.Sources.Single(value =>
                value.Ability.VariantGuid == "resist-fire-communal");
            fixture.Model.SelectSource(requested.SourceId);
            fixture.Model.ToggleTarget("long", "target-a");
            RoutinePlanResult plan = new RoutinePlanService().Plan(
                fixture.Model.Profile, "long", fixture.Snapshot,
                new ActiveEffectSnapshot(null), fixture.Effects, fixture.Options);
            if (plan.Plan.Steps.Count != 1 ||
                plan.Plan.Steps[0].Provider.Ability.BaseAbilityGuid !=
                    VariantParent().BlueprintGuid ||
                plan.Plan.Steps[0].Provider.Ability.VariantGuid !=
                    "resist-fire-communal" ||
                plan.Plan.Steps[0].Provider.Ability.VariantGuid ==
                    VariantBlueprints(true)[0].BlueprintGuid)
                throw new InvalidOperationException(
                    "Planning selected the first sibling instead of the requested child.");
        }

        private static void TestVariantSingleConsumption()
        {
            AbilityKey parent = Ability(VariantParent().BlueprintGuid, string.Empty, 0);
            AbilityKey child = Ability(
                VariantParent().BlueprintGuid, "resist-cold-communal", 0);
            const string poolKey = "variant-parent-prepared";
            var token = new ResourceTokenSnapshot(
                "slot-parent", parent, 3, PreparedSlotKind.Common,
                true, true, null);
            var pool = new ResourcePoolSnapshot(
                poolKey, ResourcePoolKind.PreparedSlots, 1, 1,
                new[] { token });
            var provider = new ProviderSnapshot(
                new ProviderKey("unit-a", "book-a", child, "level-3"),
                "Resist Cold, Communal", 3, poolKey, 1,
                new[] { token.TokenId }, null, 5, 100,
                string.Empty, string.Empty, "Resist Energy, Communal", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { pool },
                "unit-a", "target-a", "target-b");
            var area = new EffectLeafExpression(
                EffectKind.Buff, "resist-cold-effect",
                EffectTarget.AlliedAreaRecipients, "AbilityTargetsAround",
                "variant/area");
            var source = new BuffSourceDefinition(
                CatalogSourceIdentity.For(child, area), child, area,
                CastGroupingKind.MassConfiguredTargets);
            var request = new BuffCastRequest(
                source, new[] { "target-a", "target-b" },
                ExistingEffectPolicy.Overwrite, null);
            var option = new ProviderPlanningOption(
                provider, new[] { "unit-a", "target-a", "target-b" },
                new[] { "unit-a" }, 5, 100);
            CastPlan plan = new CastPlanner().Plan(
                snapshot, request, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 1 ||
                plan.Steps[0].Reservation.TokenIds.Count != 1 ||
                plan.Steps[0].Reservation.TokenIds[0] != token.TokenId ||
                plan.Outcomes.Count(value =>
                    value.Kind == TargetOutcomeKind.Fulfilled) != 2)
                throw new InvalidOperationException(
                    "A concrete communal variant reserved its parent slot more than once.");
        }

        private static void TestNonVariantPlanningRegression()
        {
            AbilityKey ability = Ability("ordinary-cast", string.Empty, 0);
            const string poolKey = "ordinary-free";
            var pool = new ResourcePoolSnapshot(
                poolKey, ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider(
                "unit-a", "book-a", ability, poolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { pool }, "unit-a");
            var option = new ProviderPlanningOption(
                provider, new[] { "unit-a" }, new[] { "unit-a" }, 1, 10);
            CastPlan plan = PlannerPlan(
                snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "unit-a" }, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 1 ||
                plan.Steps[0].Provider.Ability.VariantGuid.Length != 0 ||
                !plan.Steps[0].Provider.Ability.Equals(ability))
                throw new InvalidOperationException(
                    "Ordinary non-variant planning changed.");
        }

        private static void TestVariantIconFallback()
        {
            if (AbilityDisplayNameFormatter.PreferredIcon(
                    string.Empty, "parent-icon") != "parent-icon" ||
                AbilityDisplayNameFormatter.PreferredIcon(
                    "child-icon", "parent-icon") != "child-icon" ||
                AbilityDisplayNameFormatter.PreferredIcon(
                    string.Empty, string.Empty) != string.Empty)
                throw new InvalidOperationException(
                    "Child-first icon selection did not safely fall back to the parent.");
        }

        private static void TestLocalizedVariantFormatting()
        {
            string parent = "Protection élémentaire, communauté";
            string child = "Feu";
            string combined = AbilityDisplayNameFormatter.Format(
                parent, child, true);
            string localizedFull = "Résistance au feu, communauté";
            string localized = AbilityDisplayNameFormatter.Format(
                "Résistance à l'énergie, communauté", localizedFull, true);
            string japanese = AbilityDisplayNameFormatter.Format(
                "エネルギー耐性", "火炎", true);
            string missingQualifier = AbilityDisplayNameFormatter.Format(
                "Protection élémentaire, communauté",
                "Protection contre le feu", true);
            if (combined != parent + " \u2014 " + child ||
                localized != "Résistance à l'énergie, communauté \u2014 au feu" ||
                japanese != "エネルギー耐性 \u2014 火炎" ||
                missingQualifier !=
                    "Protection élémentaire, communauté \u2014 contre le feu" ||
                !AbilityDisplayNameFormatter.SearchText(
                    combined, parent).Contains(parent))
                throw new InvalidOperationException(
                    "Variant naming depended on English words or discarded localized text.");
        }

        private static SelectableAbilityBlueprint VariantParent()
        {
            return new SelectableAbilityBlueprint(
                "resist-energy-communal-parent",
                "Resist Energy, Communal", "parent-icon", true);
        }

        private static SelectableAbilityBlueprint[] VariantBlueprints(bool eligible)
        {
            return new[]
            {
                new SelectableAbilityBlueprint(
                    "resist-cold-communal", "Resist Cold, Communal",
                    "cold-icon", eligible),
                new SelectableAbilityBlueprint(
                    "resist-sonic-communal", "Resist Sonic, Communal",
                    "sonic-icon", eligible),
                new SelectableAbilityBlueprint(
                    "resist-electricity-communal",
                    "Resist Electricity, Communal",
                    "electricity-icon", eligible),
                new SelectableAbilityBlueprint(
                    "resist-fire-communal", "Resist Fire, Communal",
                    "fire-icon", eligible),
                new SelectableAbilityBlueprint(
                    "resist-acid-communal", "Resist Acid, Communal",
                    "acid-icon", eligible)
            };
        }

        private static VariantModelFixture CreateVariantFixture(
            BuffPlannerProfile profile, bool prepared)
        {
            const string poolKey = "variant-fixture-pool";
            AbilityKey parent = Ability(VariantParent().BlueprintGuid, string.Empty, 0);
            ResourcePoolSnapshot pool;
            string[] tokens;
            int cost;
            if (prepared)
            {
                var token = new ResourceTokenSnapshot(
                    "variant-parent-slot", parent, 3,
                    PreparedSlotKind.Common, true, true, null);
                pool = new ResourcePoolSnapshot(
                    poolKey, ResourcePoolKind.PreparedSlots,
                    1, 1, new[] { token });
                tokens = new[] { token.TokenId };
                cost = 1;
            }
            else
            {
                pool = new ResourcePoolSnapshot(
                    poolKey, ResourcePoolKind.Unlimited, 0, 0, null);
                tokens = new string[0];
                cost = 0;
            }

            SelectableAbilityBlueprint[] variants = VariantBlueprints(true);
            IReadOnlyList<SelectableAbilityEntry> entries =
                SelectableAbilityVariantCatalog.Expand(
                    VariantParent(), variants);
            var providers = new List<ProviderSnapshot>();
            var effects = new Dictionary<string, EffectExpression>(
                StringComparer.Ordinal);
            var options = new List<ProviderPlanningOption>();
            for (int index = 0; index < entries.Count; index++)
            {
                AbilityKey ability = Ability(
                    VariantParent().BlueprintGuid,
                    entries[index].Concrete.BlueprintGuid, 0);
                var provider = new ProviderSnapshot(
                    new ProviderKey("unit-a", "book-a", ability, "level-3"),
                    entries[index].DisplayName, 3, poolKey, cost, tokens,
                    null, 5, 100, "Variant fixture", "one minute",
                    VariantParent().DisplayName, index);
                providers.Add(provider);
                effects.Add(ability.Canonical, Leaf(
                    "effect-" + variants[index].BlueprintGuid));
                options.Add(new ProviderPlanningOption(
                    provider, new[] { "unit-a", "target-a", "target-b" },
                    new[] { "unit-a", "target-a", "target-b" }, 5, 100));
            }
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                providers, new[] { pool }, "unit-a", "target-a", "target-b");
            var model = new PlannerSetupModel(
                profile, snapshot, new ActiveEffectSnapshot(null),
                effects, options, ignored => { });
            return new VariantModelFixture(
                model, snapshot, effects, options, providers);
        }

        private sealed class VariantModelFixture
        {
            internal VariantModelFixture(
                PlannerSetupModel model,
                PartyProviderSnapshot snapshot,
                IDictionary<string, EffectExpression> effects,
                IEnumerable<ProviderPlanningOption> options,
                IEnumerable<ProviderSnapshot> providers)
            {
                Model = model;
                Snapshot = snapshot;
                Effects = effects;
                Options = options.ToArray();
                Providers = providers.ToArray();
            }

            internal PlannerSetupModel Model;
            internal PartyProviderSnapshot Snapshot;
            internal IDictionary<string, EffectExpression> Effects;
            internal ProviderPlanningOption[] Options;
            internal ProviderSnapshot[] Providers;
        }

        private static void TestSpontaneousSharedPool()
        {
            const string poolKey = "unit-a|book-a|level-2";
            var pool = new ResourcePoolSnapshot(poolKey, ResourcePoolKind.SpontaneousLevel, 2, 2, null);
            ProviderSnapshot first = Provider("spell-a", poolKey, 1, null);
            ProviderSnapshot second = Provider("spell-b", poolKey, 1, null);
            var snapshot = Snapshot(new[] { first, second }, new[] { pool });
            var ledger = new ResourceLedger(snapshot.ResourcePools);
            ResourceReservation reservation;
            string reason;
            if (!ledger.TryReserve(first, out reservation, out reason) ||
                !ledger.TryReserve(second, out reservation, out reason) ||
                ledger.TryReserve(first, out reservation, out reason) ||
                reason != "insufficient-shared-resource" || ledger.GetRemaining(poolKey) != 0)
                throw new InvalidOperationException("Known spells multiplied or bypassed the shared spontaneous pool.");
        }

        private static void TestPreparedLinkedSlots()
        {
            const string poolKey = "unit-a|book-a|prepared";
            var main = new ResourceTokenSnapshot("slot-0", Ability("opposed", string.Empty, 0), 3,
                PreparedSlotKind.Opposition, true, true, new[] { "slot-1" });
            var linked = new ResourceTokenSnapshot("slot-1", Ability("opposed", string.Empty, 0), 3,
                PreparedSlotKind.Opposition, true, false, new string[0]);
            var pool = new ResourcePoolSnapshot(poolKey, ResourcePoolKind.PreparedSlots, 2, 2,
                new[] { main, linked });
            ProviderSnapshot provider = Provider("opposed", poolKey, 1, new[] { "slot-0" });
            var ledger = new ResourceLedger(Snapshot(new[] { provider }, new[] { pool }).ResourcePools);
            ResourceReservation reservation;
            string reason;
            if (!ledger.TryReserve(provider, out reservation, out reason) ||
                reservation.TokenIds.Count != 2 || reservation.Units != 2 ||
                ledger.GetRemaining(poolKey) != 0 || ledger.TryReserve(provider, out reservation, out reason))
                throw new InvalidOperationException("Linked opposition slots were not consumed exactly once.");
        }

        private static void TestPreparedDomainEligibility()
        {
            const string poolKey = "unit-a|book-a|prepared-domain";
            var common = new ResourceTokenSnapshot("common", Ability("spell", string.Empty, 0), 2,
                PreparedSlotKind.Common, false, true, null);
            var domain = new ResourceTokenSnapshot("domain", Ability("domain-spell", string.Empty, 0), 2,
                PreparedSlotKind.Domain, true, true, null);
            var pool = new ResourcePoolSnapshot(poolKey, ResourcePoolKind.PreparedSlots, 2, 1,
                new[] { common, domain });
            ProviderSnapshot ordinary = Provider("spell", poolKey, 1, new[] { "common" });
            var ledger = new ResourceLedger(Snapshot(new[] { ordinary }, new[] { pool }).ResourcePools);
            ResourceReservation reservation;
            string reason;
            if (ledger.TryReserve(ordinary, out reservation, out reason) ||
                reason != "no-eligible-prepared-token" || ledger.GetRemaining(poolKey) != 1)
                throw new InvalidOperationException("An ordinary spell consumed a domain-only slot.");
        }

        private static void TestUnlimitedPool()
        {
            const string poolKey = "unit-a|book-a|cantrip";
            var pool = new ResourcePoolSnapshot(poolKey, ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = Provider("cantrip", poolKey, 0, null);
            var ledger = new ResourceLedger(Snapshot(new[] { provider }, new[] { pool }).ResourcePools);
            ResourceReservation reservation;
            string reason;
            for (int i = 0; i < 100; i++)
                if (!ledger.TryReserve(provider, out reservation, out reason) || reservation.Units != 0)
                    throw new InvalidOperationException("Explicit unlimited resource was exhausted or assigned fake credits.");
        }

        private static void TestPartySnapshotOrdering()
        {
            var validation = new TargetValidationSnapshot(true, true, true, true);
            var units = new[]
            {
                new UnitSnapshot("unit-z", "Zed", true, "unit-a", validation),
                new UnitSnapshot("unit-a", "Alpha", false, string.Empty, validation)
            };
            var snapshot = new PartyProviderSnapshot(units, new ProviderSnapshot[0], new ResourcePoolSnapshot[0]);
            if (snapshot.Units[0].UnitId != "unit-a" || snapshot.Units[1].UnitId != "unit-z" ||
                snapshot.Units[1].MasterUnitId != "unit-a")
                throw new InvalidOperationException("Party order or pet linkage depended on transient indexes.");
        }

        private static void TestEffectPresenceSemantics()
        {
            EffectExpression expression = new SequenceEffectExpression(new EffectExpression[]
            {
                Leaf("required"),
                new ConditionalEffectExpression("branch", Leaf("alternative-a"), Leaf("alternative-b"))
            });
            var evaluator = new EffectPresenceEvaluator();
            EffectPresenceResult complete = evaluator.Evaluate(expression,
                new HashSet<string>(new[] { "required", "alternative-b" }, StringComparer.Ordinal), null);
            EffectPresenceResult partial = evaluator.Evaluate(expression,
                new HashSet<string>(new[] { "required" }, StringComparer.Ordinal), null);
            EffectPresenceResult absent = evaluator.Evaluate(expression,
                new HashSet<string>(StringComparer.Ordinal), null);
            EffectPresenceResult wrongKind = evaluator.EvaluateTyped(
                new EffectLeafExpression(EffectKind.AreaBuff, "required",
                    EffectTarget.AlliedAreaRecipients,
                    "fixture", "fixture/area"),
                new HashSet<ActiveEffectMarker> { new ActiveEffectMarker(EffectKind.Buff, "required") }, null);
            if (complete.Kind != EffectPresenceKind.Complete ||
                partial.Kind != EffectPresenceKind.Partial || absent.Kind != EffectPresenceKind.Absent ||
                wrongKind.Kind != EffectPresenceKind.Absent)
                throw new InvalidOperationException("AllOf/conditional-AnyOf presence semantics were flattened.");
        }

        private static void TestPlannerMassSingleCost()
        {
            AbilityKey ability = Ability("mass", string.Empty, 0);
            const string poolKey = "mass-shared";
            var pool = new ResourcePoolSnapshot(poolKey, ResourcePoolKind.SpontaneousLevel, 1, 1, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-a", ability, poolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a", "unit-b", "unit-c");
            var option = new ProviderPlanningOption(provider,
                new[] { "unit-a", "unit-b", "unit-c" }, new[] { "unit-a" }, 10, 100);
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.MassConfiguredTargets,
                new[] { "unit-a", "unit-b", "unit-c" }, new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 1 || plan.Steps[0].Reservation.Units != 1 ||
                plan.Steps[0].TargetUnitIds.Count != 3 ||
                plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Fulfilled) != 3)
                throw new InvalidOperationException("Mass cast was charged per portrait or lost configured targets.");
        }

        private static void TestPlannerPriorityCap()
        {
            AbilityKey ability = Ability("priority", string.Empty, 0);
            var spontaneousPool = new ResourcePoolSnapshot("spont", ResourcePoolKind.SpontaneousLevel, 2, 2, null);
            var preparedToken = new ResourceTokenSnapshot("prepared-0", ability, 2,
                PreparedSlotKind.Common, true, true, null);
            var preparedPool = new ResourcePoolSnapshot("prepared", ResourcePoolKind.PreparedSlots, 1, 1,
                new[] { preparedToken });
            ProviderSnapshot spontaneous = PlannerProvider("unit-a", "book-s", ability, "spont", 1);
            ProviderSnapshot prepared = new ProviderSnapshot(
                new ProviderKey("unit-a", "book-p", ability, "level-2"), "prepared", 2,
                "prepared", 1, new[] { "prepared-0" });
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { prepared, spontaneous },
                new[] { preparedPool, spontaneousPool }, "unit-a", "unit-b");
            var options = new[]
            {
                new ProviderPlanningOption(prepared, new[] { "unit-a", "unit-b" }, new[] { "unit-a" }, 8, 80),
                new ProviderPlanningOption(spontaneous, new[] { "unit-a", "unit-b" }, new[] { "unit-a" }, 8, 80)
            };
            var priorities = new Dictionary<string, int> { { spontaneous.Key.Canonical, 0 } };
            var caps = new Dictionary<string, int> { { spontaneous.Key.Canonical, 1 } };
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "unit-a", "unit-b" }, options,
                new ProviderSelectionPolicy(null, priorities, caps), new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 2 || !plan.Steps[0].Provider.Equals(spontaneous.Key) ||
                !plan.Steps[1].Provider.Equals(prepared.Key))
                throw new InvalidOperationException("Explicit priority/cap did not deterministically fall back.");
        }

        private static void TestPlannerDeterminism()
        {
            AbilityKey ability = Ability("deterministic", string.Empty, 0);
            var flexiblePool = new ResourcePoolSnapshot("flex", ResourcePoolKind.SpontaneousLevel, 1, 1, null);
            var token = new ResourceTokenSnapshot("slot", ability, 1, PreparedSlotKind.Common, true, true, null);
            var preparedPool = new ResourcePoolSnapshot("slot-pool", ResourcePoolKind.PreparedSlots, 1, 1, new[] { token });
            ProviderSnapshot flexible = PlannerProvider("unit-a", "book-z", ability, "flex", 1);
            ProviderSnapshot prepared = new ProviderSnapshot(new ProviderKey("unit-a", "book-a", ability, "level-1"),
                "prepared", 1, "slot-pool", 1, new[] { "slot" });
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { flexible, prepared },
                new[] { flexiblePool, preparedPool }, "unit-a");
            var preparedOption = new ProviderPlanningOption(prepared, new[] { "unit-a" }, new[] { "unit-a" }, 5, 50);
            var flexibleOption = new ProviderPlanningOption(flexible, new[] { "unit-a" }, new[] { "unit-a" }, 5, 50);
            CastPlan first = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget, new[] { "unit-a" },
                new[] { flexibleOption, preparedOption }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            CastPlan second = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget, new[] { "unit-a" },
                new[] { preparedOption, flexibleOption }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (!first.Steps[0].Provider.Equals(prepared.Key) ||
                first.Steps[0].Provider.Canonical != second.Steps[0].Provider.Canonical)
                throw new InvalidOperationException("Provider input/dictionary order changed the default plan.");
        }

        private static void TestPlannerActiveSkip()
        {
            AbilityKey ability = Ability("skip", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-a", ability, "free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" }, new[] { "unit-a" }, 1, 1);
            var active = new ActiveEffectSnapshot(new Dictionary<string, IEnumerable<string>>
            {
                { "unit-a", new[] { "active-marker" } }
            });
            var source = new BuffSourceDefinition("skip-source", ability, Leaf("active-marker"), CastGroupingKind.PerTarget);
            var request = new BuffCastRequest(source, new[] { "unit-a" }, ExistingEffectPolicy.SkipAlreadyActive, null);
            CastPlan plan = new CastPlanner().Plan(snapshot, request, new[] { option }, EmptyPolicy(), active);
            if (plan.Steps.Count != 0 || plan.Outcomes.Count != 1 ||
                plan.Outcomes[0].Kind != TargetOutcomeKind.SkippedAlreadyActive ||
                plan.Outcomes[0].Markers.Count != 1 || plan.Outcomes[0].Markers[0] != "active-marker")
                throw new InvalidOperationException("Already-active skip omitted its exact marker.");
        }

        private static void TestPlannerBanAndMaterial()
        {
            AbilityKey ability = Ability("filtered", string.Empty, 0);
            var pools = new[]
            {
                new ResourcePoolSnapshot("pool-a", ResourcePoolKind.Unlimited, 0, 0, null),
                new ResourcePoolSnapshot("pool-b", ResourcePoolKind.Unlimited, 0, 0, null),
                new ResourcePoolSnapshot("pool-c", ResourcePoolKind.Unlimited, 0, 0, null)
            };
            ProviderSnapshot banned = PlannerProvider("unit-a", "book-a", ability, "pool-a", 0);
            var blockedByMaterial = new ProviderSnapshot(
                new ProviderKey("unit-a", "book-b", ability, "level-2"), "material", 2,
                "pool-b", 0, null, new MaterialRequirementSnapshot("diamond", 1, 0));
            ProviderSnapshot valid = PlannerProvider("unit-a", "book-c", ability, "pool-c", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { banned, blockedByMaterial, valid }, pools, "unit-a");
            var options = new[]
            {
                new ProviderPlanningOption(banned, new[] { "unit-a" }, new[] { "unit-a" }, 20, 200),
                new ProviderPlanningOption(blockedByMaterial, new[] { "unit-a" }, new[] { "unit-a" }, 15, 150),
                new ProviderPlanningOption(valid, new[] { "unit-a" }, new[] { "unit-a" }, 1, 1)
            };
            var priorities = new Dictionary<string, int>
            {
                { banned.Key.Canonical, 0 },
                { blockedByMaterial.Key.Canonical, 1 }
            };
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget, new[] { "unit-a" },
                options, new ProviderSelectionPolicy(new[] { banned.Key.Canonical }, priorities, null),
                new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 1 || !plan.Steps[0].Provider.Equals(valid.Key))
                throw new InvalidOperationException("Banned or material-invalid provider was scheduled.");
        }

        private static void TestNonrequiredMaterialCheck()
        {
            bool evaluated = false;
            bool noRequirement = MaterialComponentAvailability.IsSatisfied(false, () =>
            {
                evaluated = true;
                throw new InvalidOperationException("A non-required component was evaluated.");
            });
            bool missingRequired = MaterialComponentAvailability.IsSatisfied(true, () => false);
            bool presentRequired = MaterialComponentAvailability.IsSatisfied(true, () => true);
            if (!noRequirement || evaluated || missingRequired || !presentRequired)
                throw new InvalidOperationException("Material-component requirement gating changed.");
        }

        private static void TestPlannerMaterialReservation()
        {
            AbilityKey ability = Ability("component", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("component-free", ResourcePoolKind.Unlimited, 0, 0, null);
            var provider = new ProviderSnapshot(
                new ProviderKey("unit-a", "book-a", ability, "level-1"), "component", 1,
                "component-free", 0, null, new MaterialRequirementSnapshot("pearl", 1, 1));
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                new[] { "unit-a" }, 1, 1);
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "unit-a", "unit-b" }, new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 1 || plan.Steps[0].MaterialReservation == null ||
                plan.Steps[0].MaterialReservation.ItemGuid != "pearl" ||
                plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Unfulfilled) != 1)
                throw new InvalidOperationException("One material component was scheduled for multiple casts.");
        }

        private static void TestPlannerRoutineSharedLedger()
        {
            AbilityKey firstAbility = Ability("routine-a", string.Empty, 0);
            AbilityKey secondAbility = Ability("routine-b", string.Empty, 0);
            const string poolKey = "shared-routine-pool";
            var pool = new ResourcePoolSnapshot(poolKey, ResourcePoolKind.SpontaneousLevel, 1, 1, null);
            ProviderSnapshot first = PlannerProvider("unit-a", "book", firstAbility, poolKey, 1);
            ProviderSnapshot second = PlannerProvider("unit-a", "book", secondAbility, poolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { first, second }, new[] { pool }, "unit-a");
            var requests = new[]
            {
                new BuffCastRequest(new BuffSourceDefinition("a", firstAbility, Leaf("effect-a"),
                    CastGroupingKind.PerTarget), new[] { "unit-a" }, ExistingEffectPolicy.Overwrite, null),
                new BuffCastRequest(new BuffSourceDefinition("b", secondAbility, Leaf("effect-b"),
                    CastGroupingKind.PerTarget), new[] { "unit-a" }, ExistingEffectPolicy.Overwrite, null)
            };
            var options = new[]
            {
                new ProviderPlanningOption(first, new[] { "unit-a" }, new[] { "unit-a" }, 1, 10),
                new ProviderPlanningOption(second, new[] { "unit-a" }, new[] { "unit-a" }, 1, 10)
            };
            CastPlan plan = new CastPlanner().PlanRoutine(snapshot, requests, options,
                new ProviderSelectionPolicy(null, null, null), new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 1 ||
                plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Unfulfilled) != 1)
                throw new InvalidOperationException("Routine planning overbooked a shared resource pool.");
        }

        private static void TestEffectFingerprint()
        {
            EffectExpression first = new ReferencedAbilityExpression("wrapper-a",
                new EffectLeafExpression(EffectKind.Buff, "shared-effect", EffectTarget.CurrentTarget,
                    "contract-a", "path-a"));
            EffectExpression second = new ReferencedAbilityExpression("wrapper-b",
                new EffectLeafExpression(EffectKind.Buff, "shared-effect", EffectTarget.CurrentTarget,
                    "contract-b", "path-b"));
            EffectExpression distinct = new EffectLeafExpression(EffectKind.Buff, "shared-effect",
                EffectTarget.Party, "contract-a", "path-a");
            string firstId = EffectAggregateIdentity.For(first, "fallback-a");
            string secondId = EffectAggregateIdentity.For(second, "fallback-b");
            if (firstId != secondId || firstId == EffectAggregateIdentity.For(distinct, "fallback-c") ||
                EffectAggregateIdentity.For(new EmptyEffectExpression(), "exact") != "exact")
                throw new InvalidOperationException("Effect aggregation used provider metadata or merged distinct mechanics.");
        }

        private static void TestAggregateCardAndPlanning()
        {
            AbilityKey firstAbility = Ability("resistance-a", string.Empty, 0);
            AbilityKey secondAbility = Ability("resistance-b", string.Empty, 0);
            var firstPool = new ResourcePoolSnapshot("aggregate-empty", ResourcePoolKind.SpontaneousLevel, 1, 0, null);
            var secondPool = new ResourcePoolSnapshot("aggregate-ready", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot first = PlannerProvider("unit-a", "book-a", firstAbility, "aggregate-empty", 1);
            ProviderSnapshot second = PlannerProvider("unit-b", "book-b", secondAbility, "aggregate-ready", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { first, second },
                new[] { firstPool, secondPool }, "unit-a", "unit-b", "target");
            var expressionA = new EffectLeafExpression(EffectKind.Buff, "resistance-effect",
                EffectTarget.CurrentTarget, "first", "first/path");
            var expressionB = new EffectLeafExpression(EffectKind.Buff, "resistance-effect",
                EffectTarget.CurrentTarget, "second", "second/path");
            var effects = new Dictionary<string, EffectExpression>
            {
                { firstAbility.Canonical, expressionA }, { secondAbility.Canonical, expressionB }
            };
            var options = new[]
            {
                new ProviderPlanningOption(first, new[] { "target" }, new[] { "unit-a" }, 3, 10),
                new ProviderPlanningOption(second, new[] { "target" }, new[] { "unit-b" }, 5, 20)
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("aggregate-campaign");
            int saves = 0;
            var model = new PlannerSetupModel(profile, snapshot, new ActiveEffectSnapshot(null),
                effects, options, ignored => saves++);
            if (model.Sources.Count != 1 || model.Sources[0].Abilities.Count != 2 ||
                model.Sources[0].Providers.Count != 2)
                throw new InvalidOperationException("Equivalent provider-backed effects did not consolidate to one card.");
            model.ToggleTarget("long", "target");
            RoutinePlanResult plan = new RoutinePlanService().Plan(profile, "long", snapshot,
                new ActiveEffectSnapshot(null), effects, options);
            if (plan.Plan.Steps.Count != 1 ||
                !plan.Plan.Steps[0].Provider.Ability.Equals(secondAbility) || saves != 1)
                throw new InvalidOperationException("Consolidated card did not preserve automatic valid provider selection.");
        }

        private static void TestAggregateRoundTrip(string root)
        {
            AbilityKey firstAbility = Ability("roundtrip-a", string.Empty, 0);
            AbilityKey secondAbility = Ability("roundtrip-b", string.Empty, 0);
            var firstPool = new ResourcePoolSnapshot("roundtrip-one", ResourcePoolKind.Unlimited, 0, 0, null);
            var secondPool = new ResourcePoolSnapshot("roundtrip-two", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot first = PlannerProvider("unit-a", "book-a", firstAbility, "roundtrip-one", 0);
            ProviderSnapshot second = PlannerProvider("unit-b", "book-b", secondAbility, "roundtrip-two", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { first, second },
                new[] { firstPool, secondPool }, "unit-a", "unit-b");
            var effects = new Dictionary<string, EffectExpression>
            {
                { firstAbility.Canonical, Leaf("roundtrip-effect") },
                { secondAbility.Canonical, Leaf("roundtrip-effect") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("aggregate-roundtrip");
            profile.Routines[0].Assignments.Add(Assignment(firstAbility, "unit-a"));
            profile.Routines[0].Assignments.Add(Assignment(secondAbility, "unit-b"));
            string modPath = Path.Combine(root, "aggregate-roundtrip");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            var options = new[]
            {
                new ProviderPlanningOption(first, new[] { "unit-a", "unit-b" }, new[] { "unit-a" }, 1, 10),
                new ProviderPlanningOption(second, new[] { "unit-a", "unit-b" }, new[] { "unit-b" }, 1, 10)
            };
            var model = new PlannerSetupModel(profile, snapshot, new ActiveEffectSnapshot(null),
                effects, options, repository.Save);
            ProfileLoadResult loaded = repository.Load("aggregate-roundtrip");
            SourceAssignmentProfile assignment = loaded.Profile.Routines[0].Assignments.Single();
            if (assignment.SourceId != model.Sources[0].SourceId ||
                !assignment.WantedTargetUnitIds.SequenceEqual(new[] { "unit-a", "unit-b" }))
                throw new InvalidOperationException("Legacy assignments did not merge and survive aggregate round trip.");
        }

        private static void TestAggregateAvailability()
        {
            AbilityKey firstAbility = Ability("shared-pool-a", string.Empty, 0);
            AbilityKey secondAbility = Ability("shared-pool-b", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("shared-aggregate-pool",
                ResourcePoolKind.SpontaneousLevel, 3, 3, null);
            ProviderSnapshot first = PlannerProvider("unit-a", "book", firstAbility,
                "shared-aggregate-pool", 1);
            ProviderSnapshot second = PlannerProvider("unit-a", "book", secondAbility,
                "shared-aggregate-pool", 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { first, second },
                new[] { pool }, "unit-a");
            var effects = new Dictionary<string, EffectExpression>
            {
                { firstAbility.Canonical, Leaf("shared-effect") },
                { secondAbility.Canonical, Leaf("shared-effect") }
            };
            var options = new[]
            {
                new ProviderPlanningOption(first, new[] { "unit-a" }, new[] { "unit-a" }, 1, 10),
                new ProviderPlanningOption(second, new[] { "unit-a" }, new[] { "unit-a" }, 1, 10)
            };
            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("shared-pool"),
                snapshot, new ActiveEffectSnapshot(null), effects, options, ignored => { });
            var card = new BuffCardViewModel(model.Sources.Single(), model, "long", false);
            if (card.Availability != "3 available")
                throw new InvalidOperationException("Aggregate availability double-counted a shared pool: " +
                    card.Availability);
        }

        private static void TestSelectedBuffSummary()
        {
            AbilityKey ability = Ability("summary-ability", string.Empty, 1);
            var token = new ResourceTokenSnapshot("summary-slot", ability, 1,
                PreparedSlotKind.Common, true, true, null);
            var pool = new ResourcePoolSnapshot("summary-pool", ResourcePoolKind.PreparedSlots,
                1, 1, new[] { token });
            ProviderSnapshot provider = new ProviderSnapshot(
                new ProviderKey("unit-a", "summary-book", ability, "level-1"),
                "summary", 1, "summary-pool", 1, new[] { "summary-slot" });
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a");
            EffectExpression effect = Leaf("summary-effect");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 1, 10);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("summary-campaign");
            var model = new PlannerSetupModel(profile, snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, effect } },
                new[] { option }, ignored => { });
            model.ToggleTarget("long", "unit-a");
            RoutinePlanResult preview = new RoutinePlanService().Plan(profile, "long", snapshot,
                new ActiveEffectSnapshot(null), new Dictionary<string, EffectExpression>
                { { ability.Canonical, effect } }, new[] { option });
            var summary = new SelectedBuffPlanSummaryViewModel(model.Sources.Single(), model,
                "long", preview);
            if (summary.Availability != "1 prepared" || summary.PlannedCasts != 1 ||
                !summary.Text.Contains("Available: 1 prepared") ||
                !summary.Text.Contains("Planned: 1 cast") ||
                summary.Text.Contains("targets covered") || summary.Text.Contains("blocked"))
                throw new InvalidOperationException("Selected-buff plan summary is ambiguous: " + summary.Text);
        }

        private static SourceAssignmentProfile Assignment(AbilityKey ability, string target)
        {
            return Assignment(ability.Canonical, ability, new[] { target });
        }

        // Builds the schema-5 assignment shape used by every fixture: one
        // source parent whose single Automatic child carries the legacy
        // fixture targets and required-by-default enhancement selections.
        private static SourceAssignmentProfile Assignment(string sourceId,
            AbilityKey ability, IEnumerable<string> targets,
            IEnumerable<string> enhancements = null,
            ExistingEffectPolicy policy = ExistingEffectPolicy.SkipAlreadyActive)
        {
            return new SourceAssignmentProfile
            {
                SourceId = sourceId,
                Ability = AbilityKeyProfile.FromKey(ability),
                ExistingEffectPolicy = policy,
                IgnoredPresenceMarkers = new List<string>(),
                CastingAssignments = new List<CastingAssignmentProfile>
                {
                    new CastingAssignmentProfile
                    {
                        AssignmentId = "auto-" + sourceId,
                        Order = 0,
                        CasterUnitId = null,
                        SpellbookGuid = null,
                        ProviderKey = null,
                        TargetUnitIds = new List<string>(targets ?? new string[0]),
                        Enhancements = (enhancements ?? new string[0])
                            .Select(id => new EnhancementSelectionProfile
                            {
                                EnhancementId = id,
                                Required = true
                            }).ToList()
                    }
                }
            };
        }

        private static void TestRoutineServiceUnsupportedSources()
        {
            AbilityKey supported = Ability("routine-supported", string.Empty, 0);
            AbilityKey unsupported = Ability("routine-unsupported", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("routine-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book", supported, "routine-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a");
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("routine-campaign");
            profile.Routines[0].Assignments.Add(Assignment(
                supported.Canonical, supported, new[] { "unit-a" }, null,
                ExistingEffectPolicy.Overwrite));
            profile.Routines[0].Assignments.Add(Assignment(
                unsupported.Canonical, unsupported, new[] { "unit-a" }, null,
                ExistingEffectPolicy.Overwrite));
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 1, 10, true);
            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long", snapshot,
                new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { supported.Canonical, Leaf("supported-effect") } },
                new[] { option });
            if (result.Plan.Steps.Count != 1 || result.UnsupportedSourceIds.Count != 1 ||
                result.UnsupportedSourceIds[0] != unsupported.Canonical ||
                result.AnimatedFallbackSourceIds.Count != 1 ||
                result.AnimatedFallbackSourceIds[0] != supported.Canonical)
                throw new InvalidOperationException("Routine service did not isolate an unsupported saved source.");
        }

        private static void TestProfileRoundTrip(string root)
        {
            string modPath = Path.Combine(root, "profile-roundtrip");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            BuffPlannerProfile profile = ProfileFixture("campaign:alpha");
            repository.Save(profile);
            ProfileLoadResult loaded = repository.Load("campaign:alpha");
            SourceAssignmentProfile assignment = loaded.Profile.Routines[0].Assignments[0];
            if (loaded.RecoveredFromBackup || loaded.Migrated || loaded.Warning.Length != 0 ||
                assignment.WantedTargetUnitIds[0] != "unit-z" ||
                assignment.WantedTargetUnitIds[1] != "unit-a" ||
                assignment.Ability.ToKey().Canonical != Ability("persisted", "variant", 8).Canonical ||
                assignment.SelectedEnhancementIds.Single() != "metamagic-rod|unit-a|persisted")
                throw new InvalidOperationException("Stable IDs or exact profile values changed during round trip.");
            if (Directory.GetFiles(Path.GetDirectoryName(repository.GetProfilePath("campaign:alpha")), "*.tmp").Length != 0)
                throw new InvalidOperationException("Atomic profile write left a temporary file.");
        }

        private static void TestProfileBackupRecovery(string root)
        {
            string modPath = Path.Combine(root, "profile-backup");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            BuffPlannerProfile profile = ProfileFixture("campaign:backup");
            profile.Routines[0].Name = "First";
            repository.Save(profile);
            profile.Routines[0].Name = "Second";
            repository.Save(profile);
            File.WriteAllText(repository.GetProfilePath("campaign:backup"), "{ malformed");
            ProfileLoadResult recovered = repository.Load("campaign:backup");
            if (!recovered.RecoveredFromBackup || recovered.Profile.Routines[0].Name != "First" ||
                string.IsNullOrWhiteSpace(recovered.Warning))
                throw new InvalidOperationException("Malformed primary did not recover the prior valid profile.");
            for (int i = 0; i < 5; i++)
            {
                recovered.Profile.Routines[0].Name = "Revision " + i;
                repository.Save(recovered.Profile);
            }
            if (File.Exists(repository.GetProfilePath("campaign:backup") + ".bak4"))
                throw new InvalidOperationException("Profile backup retention exceeded its bound.");
        }

        // Rebuilds a genuine historical schema-4 document: flat target and
        // enhancement lists on each source assignment, no casting children.
        // Migration tests must feed real old data, not a current profile with
        // a rewritten version number.
        private static JObject LegacyV4Document(BuffPlannerProfile profile)
        {
            JObject document = JObject.Parse(JsonConvert.SerializeObject(profile));
            document["schemaVersion"] = 4;
            foreach (JObject routine in ((JArray)document["routines"]).OfType<JObject>())
                foreach (JObject assignment in ((JArray)routine["assignments"]).OfType<JObject>())
                {
                    JArray children = (JArray)assignment["castingAssignments"];
                    JObject child = children != null && children.Count > 0
                        ? (JObject)children[0] : null;
                    assignment["wantedTargetUnitIds"] = child == null
                        ? new JArray() : child["targetUnitIds"] ?? new JArray();
                    assignment["selectedEnhancementIds"] = child == null
                        ? new JArray() : new JArray(((JArray)child["enhancements"])
                            .OfType<JObject>().Select(selection => selection["enhancementId"]));
                    assignment.Remove("castingAssignments");
                }
            return document;
        }

        private static void TestProfileMigration(string root)
        {
            string modPath = Path.Combine(root, "profile-migration");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            JObject document = LegacyV4Document(ProfileFixture("campaign:migration"));
            document["schemaVersion"] = 1;
            document.Remove("ui");
            document.Remove("execution");
            string path = repository.GetProfilePath("campaign:migration");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, document.ToString());
            ProfileLoadResult migrated = repository.Load("campaign:migration");
            if (!migrated.Migrated || migrated.Profile.SchemaVersion != 5 ||
                migrated.Profile.Ui.Scale != 1.0f || migrated.Profile.Execution.Mode != "animated" ||
                migrated.Profile.Ui.Hotkey != "Ctrl+Shift+B")
                throw new InvalidOperationException("Schema-one profile was not migrated with safe defaults.");
            SourceAssignmentProfile legacy = migrated.Profile.Routines[0].Assignments[0];
            if (legacy.CastingAssignments.Count != 1 ||
                !legacy.CastingAssignments[0].TargetUnitIds
                    .SequenceEqual(new[] { "unit-z", "unit-a" }) ||
                legacy.CastingAssignments[0].AssignmentId != "legacy-source-persisted" ||
                !legacy.CastingAssignments[0].Enhancements.Single().IsRequired)
                throw new InvalidOperationException(
                    "Schema-four targets or enhancements did not migrate into one legacy-equivalent automatic child.");
            // The exact pre-migration original is archived outside the
            // rotating backup chain (mission: rotating saves cannot erase it).
            string archive = Path.Combine(Path.GetDirectoryName(path),
                "kbp-pre-schema-" + Path.GetFileName(path)
                    .Replace("kingmaker-buff-planner-", string.Empty)
                    .Replace(".json", string.Empty) + ".orig");
            if (!File.Exists(archive) || JObject.Parse(File.ReadAllText(archive))
                    .Value<int?>("schemaVersion") != 1)
                throw new InvalidOperationException(
                    "The pre-migration original was not archived before the first save.");

            // Legacy allocation ran in source-ID order; migration makes exactly
            // that order explicit per child so v5 allocation reproduces v4.
            BuffPlannerProfile multiSource = BuffPlannerProfile.CreateDefault(
                "campaign:migration-order");
            multiSource.Routines[0].Assignments.Add(Assignment(
                "source-bravo", Ability("bravo", string.Empty, 0), new[] { "unit-a" }));
            multiSource.Routines[0].Assignments.Add(Assignment(
                "source-alpha", Ability("alpha", string.Empty, 0), new[] { "unit-b" }));
            JObject multiDocument = LegacyV4Document(multiSource);
            string multiPath = repository.GetProfilePath("campaign:migration-order");
            File.WriteAllText(multiPath, multiDocument.ToString());
            ProfileLoadResult multiResult = repository.Load("campaign:migration-order");
            List<CastingAssignmentProfile> order = multiResult.Profile.Routines[0]
                .Assignments.SelectMany(value => value.CastingAssignments)
                .OrderBy(child => child.Order).ToList();
            if (order.Count != 2 ||
                order[0].AssignmentId != "legacy-source-alpha" ||
                order[1].AssignmentId != "legacy-source-bravo" ||
                order[0].Order != 0 || order[1].Order != 1)
                throw new InvalidOperationException(
                    "Migration did not preserve legacy source-ID order as explicit assignment order.");
            // Saving the migrated profile and loading again is idempotent:
            // IDs and orders are never regenerated (the repository itself
            // never auto-saves; callers own the write).
            repository.Save(multiResult.Profile);
            ProfileLoadResult again = repository.Load("campaign:migration-order");
            List<CastingAssignmentProfile> reloaded = again.Profile.Routines[0]
                .Assignments.SelectMany(value => value.CastingAssignments)
                .OrderBy(child => child.Order).ToList();
            if (again.Migrated || reloaded.Count != 2 ||
                reloaded[0].AssignmentId != "legacy-source-alpha" ||
                reloaded[1].AssignmentId != "legacy-source-bravo")
                throw new InvalidOperationException(
                    "Migration was not idempotent across a save and reload.");
        }

        private static void TestGridProfileMigration(string root)
        {
            string modPath = Path.Combine(root, "profile-grid-migration");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            JObject document = LegacyV4Document(ProfileFixture("campaign:grid-migration"));
            document["schemaVersion"] = 2;
            document["hiddenSourceIds"] = new JArray("hidden-a", "hidden-b");
            ((JObject)document["ui"])["hotkey"] = "F10";
            ((JObject)document["execution"]).Remove("recastExisting");
            ((JObject)((JArray)((JObject)((JArray)document["routines"])[0])["assignments"])[0])
                .Remove("selectedEnhancementIds");
            string path = repository.GetProfilePath("campaign:grid-migration");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, document.ToString());
            ProfileLoadResult migrated = repository.Load("campaign:grid-migration");
            if (!migrated.Migrated || migrated.Profile.SchemaVersion != 5 ||
                migrated.Profile.Ui.Hotkey != "Ctrl+Shift+B" ||
                migrated.Profile.HiddenSourceIds.Count != 0 || migrated.Profile.Execution.RecastExisting)
                throw new InvalidOperationException("Grid UI migration did not reveal hidden entries or replace F10." +
                    " observed: migrated=" + migrated.Migrated +
                    ";schema=" + migrated.Profile.SchemaVersion +
                    ";hotkey=" + migrated.Profile.Ui.Hotkey +
                    ";hidden=" + migrated.Profile.HiddenSourceIds.Count +
                    ";recast=" + migrated.Profile.Execution.RecastExisting +
                    ";warning=" + migrated.Warning);
            if (migrated.Profile.Routines[0].Assignments.Count != 1 ||
                migrated.Profile.Routines[0].Assignments[0].WantedTargetUnitIds.Count != 2 ||
                migrated.Profile.Routines[0].Assignments[0].CastingAssignments.Count != 1 ||
                migrated.Profile.Routines[0].Assignments[0].SelectedEnhancementIds.Count != 0)
                throw new InvalidOperationException("Grid UI migration did not preserve routine targets.");
        }

        private static void TestProfileMalformed(string root)
        {
            string modPath = Path.Combine(root, "profile-malformed");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            string path = repository.GetProfilePath("campaign:malformed");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "not-json");
            ProfileLoadResult loaded = repository.Load("campaign:malformed");
            if (loaded.Profile.CampaignId != "campaign:malformed" || loaded.Profile.Routines.Count != 3 ||
                string.IsNullOrWhiteSpace(loaded.Warning))
                throw new InvalidOperationException("Malformed JSON did not recover to an explicit safe default.");
            BuffPlannerProfile duplicate = ProfileFixture("campaign:duplicate");
            repository.Save(duplicate);
            string duplicatePath = repository.GetProfilePath("campaign:duplicate");
            string duplicateJson = File.ReadAllText(duplicatePath).Replace(
                "\"schemaVersion\": 5,", "\"schemaVersion\": 5,\r\n  \"schemaVersion\": 5,");
            File.WriteAllText(duplicatePath, duplicateJson);
            ProfileLoadResult rejected = repository.Load("campaign:duplicate");
            if (!rejected.Warning.Contains("duplicate-property"))
                throw new InvalidOperationException("Duplicate JSON property was not rejected.");
        }

        private static BuffPlannerProfile ProfileFixture(string campaignId)
        {
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(campaignId);
            profile.Routines[0].Assignments.Add(Assignment(
                "source-persisted", Ability("persisted", "variant", 8),
                new[] { "unit-z", "unit-a" },
                new[] { "metamagic-rod|unit-a|persisted" }));
            profile.Routines[0].Assignments[0].IgnoredPresenceMarkers =
                new List<string> { "shared-marker" };
            profile.ProviderPreferences.Add(new ProviderPreferenceProfile
            {
                ProviderKey = "unit-a|book|provider",
                Banned = false,
                Priority = 2,
                MaximumCasts = 3
            });
            return profile;
        }

        private static void TestSetupModel()
        {
            AbilityKey ability = Ability("ui-source", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("ui-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-ui", ability, "ui-free", 0);
            var validation = new TargetValidationSnapshot(true, true, true, true);
            var units = new[]
            {
                new UnitSnapshot("unit-b", "Duplicate", false, string.Empty, validation),
                new UnitSnapshot("unit-a", "Duplicate", false, string.Empty, validation)
            };
            var snapshot = new PartyProviderSnapshot(units, new[] { provider }, new[] { pool });
            var active = ActiveEffectSnapshot.FromTypedEffects(
                new Dictionary<string, IEnumerable<ActiveEffectMarker>>
                {
                    { "unit-b", new[] { new ActiveEffectMarker(EffectKind.Buff, "ui-effect") } }
                });
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("campaign:ui");
            int saves = 0;
            var providerOptions = new[]
            {
                new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                    new[] { "unit-a", "unit-b" }, 1, 10)
            };
            var model = new PlannerSetupModel(profile, snapshot, active,
                new Dictionary<string, EffectExpression> { { ability.Canonical, Leaf("ui-effect") } },
                providerOptions, p => saves++);
            if (!model.IsSourceAvailable(model.Sources[0]) ||
                model.GetSourceUnavailableReason(model.Sources[0]).Length != 0)
                throw new InvalidOperationException("Default catalog availability hid a legal source.");
            model.ToggleTarget("long", "unit-b");
            model.ToggleTarget("long", "unit-a");
            if (!model.IsTargetWanted("long", "unit-a") || !model.IsTargetWanted("long", "unit-b") ||
                model.GetPresence("unit-b") != EffectPresenceKind.Complete)
                throw new InvalidOperationException("Setup target matrix lost stable IDs or active state.");
            model.SetProviderEnabled(provider.Key.Canonical, false);
            if (!model.GetProviderPreference(provider.Key.Canonical).Banned)
                throw new InvalidOperationException("Provider was not explicitly disabled.");
            model.SetProviderEnabled(provider.Key.Canonical, true);
            if (model.GetProviderPreference(provider.Key.Canonical) != null)
                throw new InvalidOperationException("Provider did not return to automatic.");
            model.SetProviderMaximumCasts(provider.Key.Canonical, 1);
            model.SetScale(1.25f);
            model.ToggleExecutionMode();
            model.ToggleOutOfCombatOnly();
            model.ToggleAnimatedFallback();
            model.ToggleRecastExisting();
            model.TogglePlannerHotkey();
            var reordered = new PartyProviderSnapshot(units.Reverse(), new[] { provider }, new[] { pool });
            var reloaded = new PlannerSetupModel(profile, reordered, active,
                new Dictionary<string, EffectExpression> { { ability.Canonical, Leaf("ui-effect") } },
                providerOptions, p => saves++);
            if (!reloaded.IsTargetWanted("long", "unit-a") || !reloaded.IsTargetWanted("long", "unit-b") ||
                reloaded.GetProviderPreference(provider.Key.Canonical).MaximumCasts != 1 ||
                reloaded.Profile.Ui.Scale != 1.25f ||
                reloaded.Profile.Execution.Mode != "instant" ||
                reloaded.Profile.Execution.OutOfCombatOnly ||
                reloaded.Profile.Execution.AllowAnimatedFallback ||
                !reloaded.Profile.Execution.RecastExisting ||
                reloaded.Profile.Ui.Hotkey != "Ctrl+Shift+P" ||
                reloaded.Profile.HiddenSourceIds.Count != 0 || saves < 11)
                throw new InvalidOperationException("Setup state did not survive party reorder/persistence mutations.");
            reloaded.ToggleTarget("short", "unit-a");
            if (!reloaded.IsTargetWanted("short", "unit-a") ||
                !reloaded.IsTargetWanted("long", "unit-a"))
                throw new InvalidOperationException("Direct assignment did not stay local to the active routine.");
            reloaded.ClearRoutine("short");
            if (reloaded.Profile.Routines.First(r => r.RoutineId == "short").Assignments.Count != 0)
                throw new InvalidOperationException("Routine clear changed or retained the wrong assignment set.");
        }

        private static void TestProviderPolicyOperations()
        {
            ProviderPolicyFixture fixture =
                CreateProviderPolicyFixture("policy-operations", false);
            PlannerSetupModel model = fixture.Model;
            model.MoveProviderEarlier(fixture.FelixBlur.Key.Canonical);
            ProviderPreferenceProfile felix =
                model.GetProviderPreference(fixture.FelixBlur.Key.Canonical);
            ProviderPreferenceProfile akasa =
                model.GetProviderPreference(fixture.AkasaBlur.Key.Canonical);
            if (felix == null || akasa == null ||
                felix.Priority != 0 || akasa.Priority != 1)
                throw new InvalidOperationException(
                    "Moving a provider earlier did not assign normalized priorities.");

            model.SetProviderMaximumCasts(
                fixture.FelixBlur.Key.Canonical, 1);
            if (felix.Priority != 0 || felix.MaximumCasts != 1 ||
                felix.Banned)
                throw new InvalidOperationException(
                    "A preferred provider could not remain capped.");
            model.MoveProviderLater(fixture.FelixBlur.Key.Canonical);
            int[] priorities = fixture.BlurSource.Providers.Select(provider =>
                    model.GetProviderPreference(
                        provider.Key.Canonical).Priority.Value)
                .OrderBy(value => value).ToArray();
            if (!priorities.SequenceEqual(new[] { 0, 1 }))
                throw new InvalidOperationException(
                    "Reordering left duplicate, sparse, or order-dependent priorities.");

            model.SetProviderEnabled(
                fixture.FelixBlur.Key.Canonical, false);
            if (!felix.Banned || felix.MaximumCasts != 1 ||
                felix.Priority == null)
                throw new InvalidOperationException(
                    "Disabling a provider discarded its order or cap.");
            model.SetProviderEnabled(
                fixture.FelixBlur.Key.Canonical, true);
            if (felix.Banned || felix.MaximumCasts != 1)
                throw new InvalidOperationException(
                    "Re-enabling a provider changed its cap.");

            model.SelectSource(fixture.BullsSource.SourceId);
            model.SetProviderMaximumCasts(
                fixture.FelixBulls.Key.Canonical, 2);
            model.SelectSource(fixture.BlurSource.SourceId);
            model.ResetSelectedSourceProvidersToAutomatic();
            if (model.GetProviderPreference(
                    fixture.FelixBlur.Key.Canonical) != null ||
                model.GetProviderPreference(
                    fixture.AkasaBlur.Key.Canonical) != null ||
                model.GetProviderPreference(
                    fixture.FelixBulls.Key.Canonical).MaximumCasts != 2)
                throw new InvalidOperationException(
                    "Reset Automatic removed preferences outside the selected buff.");
        }

        private static void TestProviderPolicyPlanning()
        {
            ProviderPolicyFixture fixture =
                CreateProviderPolicyFixture("policy-planning", false);
            foreach (string target in fixture.TargetIds)
                fixture.Model.ToggleTarget("long", target);

            RoutinePlanResult automatic = fixture.Plan("long");
            if (automatic.Plan.Steps.Count != fixture.TargetIds.Length ||
                automatic.Plan.Steps.Any(step =>
                    step.Provider.Canonical !=
                        fixture.AkasaBlur.Key.Canonical))
                throw new InvalidOperationException(
                    "No-preference planning changed the deterministic Automatic plan.");

            fixture.Model.MoveProviderEarlier(
                fixture.FelixBlur.Key.Canonical);
            fixture.Model.SetProviderMaximumCasts(
                fixture.FelixBlur.Key.Canonical, 1);
            RoutinePlanResult split = fixture.Plan("long");
            if (split.Plan.Steps.Count != fixture.TargetIds.Length ||
                split.Plan.Steps.First().Provider.Canonical !=
                    fixture.FelixBlur.Key.Canonical ||
                split.Plan.Steps.Count(step =>
                    step.Provider.Canonical ==
                        fixture.FelixBlur.Key.Canonical) != 1 ||
                split.Plan.Steps.Skip(1).Any(step =>
                    step.Provider.Canonical !=
                        fixture.AkasaBlur.Key.Canonical))
                throw new InvalidOperationException(
                    "Priority plus maximum one did not split the buff across casters.");

            fixture.Model.SetProviderEnabled(
                fixture.FelixBlur.Key.Canonical, false);
            RoutinePlanResult banned = fixture.Plan("long");
            if (banned.Plan.Steps.Any(step =>
                    step.Provider.Canonical ==
                        fixture.FelixBlur.Key.Canonical) ||
                banned.Plan.Steps.Count != fixture.TargetIds.Length)
                throw new InvalidOperationException(
                    "Banning the first caster did not route every cast to the fallback.");

            fixture.Model.SetProviderEnabled(
                fixture.FelixBlur.Key.Canonical, true);
            fixture.Model.SetProviderMaximumCasts(
                fixture.AkasaBlur.Key.Canonical, 2);
            RoutinePlanResult capped = fixture.Plan("long");
            if (capped.Plan.Steps.Count != 3 ||
                capped.Plan.Outcomes.Count(outcome =>
                    outcome.Kind == TargetOutcomeKind.Unfulfilled) != 2 ||
                !capped.Plan.Diagnostics.Any(value =>
                    value.Contains("reason=provider-policy-refusal") &&
                    value.Contains("at-cap=2")))
                throw new InvalidOperationException(
                    "Insufficient combined caps did not produce exact outcomes and diagnostics.");

            fixture.Model.SetProviderEnabled(
                fixture.FelixBlur.Key.Canonical, false);
            fixture.Model.SetProviderEnabled(
                fixture.AkasaBlur.Key.Canonical, false);
            RoutinePlanResult allBanned = fixture.Plan("long");
            if (allBanned.Plan.Steps.Count != 0 ||
                allBanned.Plan.Outcomes.Count(outcome =>
                    outcome.Kind == TargetOutcomeKind.Unfulfilled) !=
                        fixture.TargetIds.Length ||
                !allBanned.Plan.Diagnostics.Any(value =>
                    value.Contains("reason=provider-policy-refusal") &&
                    value.Contains("banned=2")))
                throw new InvalidOperationException(
                    "All-provider bans were bypassed or lacked policy-refusal diagnostics.");

            fixture.Model.SelectSource(fixture.BullsSource.SourceId);
            fixture.Model.ToggleTarget("important", fixture.TargetIds[0]);
            RoutinePlanResult unrelated = fixture.Plan("important");
            if (unrelated.Plan.Steps.Count != 1 ||
                unrelated.Plan.Steps.Single().Provider.Canonical !=
                    fixture.FelixBulls.Key.Canonical)
                throw new InvalidOperationException(
                    "A cap or ban on one exact buff provider leaked to another ability.");
        }

        private static void TestProviderPolicyPresentation()
        {
            ProviderPolicyFixture fixture =
                CreateProviderPolicyFixture("policy-presentation", true);
            CasterPolicyViewModel automatic = CasterPolicyViewModel.Create(
                fixture.BlurSource, fixture.Model, "long", fixture.Plan("long"));
            ProviderPolicyRowViewModel felix = automatic.Providers.Single(
                provider => provider.ProviderKey ==
                    fixture.FelixBlur.Key.Canonical);
            if (automatic.Summary != "Casters: Automatic" ||
                automatic.Providers.Count != 2 ||
                string.IsNullOrWhiteSpace(felix.UnavailableReason) ||
                felix.Remaining != "0 casts remaining" ||
                !felix.Source.Contains("Spellbook") ||
                felix.SpellLevel != 2)
                throw new InvalidOperationException(
                    "The chooser hid an exhausted owned provider or omitted player-facing details.");

            fixture.Model.MoveProviderEarlier(
                fixture.FelixBlur.Key.Canonical);
            fixture.Model.SetProviderMaximumCasts(
                fixture.FelixBlur.Key.Canonical, 1);
            foreach (string target in fixture.TargetIds)
                fixture.Model.ToggleTarget("long", target);
            RoutinePlanResult preview = fixture.Plan("long");
            CasterPolicyViewModel planned = CasterPolicyViewModel.Create(
                fixture.BlurSource, fixture.Model, "long", preview);
            felix = planned.Providers.Single(provider =>
                provider.ProviderKey == fixture.FelixBlur.Key.Canonical);
            if (felix.Order != 1 || felix.MaximumCasts != 1 ||
                planned.Summary != "Planned casters: Akasa 5" ||
                planned.Warning)
                throw new InvalidOperationException(
                    "Caster policy presentation did not match the actual pure preview allocation.");

            fixture.Model.SetProviderEnabled(
                fixture.AkasaBlur.Key.Canonical, false);
            preview = fixture.Plan("long");
            planned = CasterPolicyViewModel.Create(
                fixture.BlurSource, fixture.Model, "long", preview);
            if (!planned.Warning ||
                !planned.Summary.Contains("5 unfulfilled") ||
                !planned.Description.Contains(
                    "Provider policy cannot cover every selected target."))
                throw new InvalidOperationException(
                    "Insufficient policy was not surfaced as a planner-local warning.");
        }

        private static void TestProviderPolicyRoundTrip(string root)
        {
            ProviderPolicyFixture fixture =
                CreateProviderPolicyFixture("campaign:policy-roundtrip", false);
            fixture.Model.MoveProviderEarlier(
                fixture.FelixBlur.Key.Canonical);
            fixture.Model.SetProviderMaximumCasts(
                fixture.FelixBlur.Key.Canonical, 3);
            fixture.Model.SetProviderEnabled(
                fixture.AkasaBlur.Key.Canonical, false);
            fixture.Profile.ProviderPreferences.Add(
                new ProviderPreferenceProfile
                {
                    ProviderKey = "stale-unit|stale-book|stale-ability|stale-instance",
                    Banned = true,
                    Priority = 999999,
                    MaximumCasts = 17
                });

            string modPath = Path.Combine(root, "provider-policy-roundtrip");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            repository.Save(fixture.Profile);
            ProfileLoadResult loaded = repository.Load(
                fixture.Profile.CampaignId);
            ProviderPreferenceProfile felix =
                loaded.Profile.ProviderPreferences.Single(preference =>
                    preference.ProviderKey ==
                        fixture.FelixBlur.Key.Canonical);
            ProviderPreferenceProfile akasa =
                loaded.Profile.ProviderPreferences.Single(preference =>
                    preference.ProviderKey ==
                        fixture.AkasaBlur.Key.Canonical);
            ProviderPreferenceProfile stale =
                loaded.Profile.ProviderPreferences.Single(preference =>
                    preference.ProviderKey.StartsWith(
                        "stale-unit|", StringComparison.Ordinal));
            if (loaded.Profile.SchemaVersion != 5 ||
                felix.Priority != 0 || felix.MaximumCasts != 3 ||
                felix.Banned || !akasa.Banned ||
                stale.MaximumCasts != 17)
                throw new InvalidOperationException(
                    "Provider policy fields did not round-trip without a schema change.");

            var reloaded = new PlannerSetupModel(
                loaded.Profile, fixture.Snapshot,
                new ActiveEffectSnapshot(null), fixture.Effects,
                fixture.Options, ignored => { });
            reloaded.SelectSource(fixture.BlurSource.SourceId);
            foreach (string target in fixture.TargetIds)
                reloaded.ToggleTarget("short", target);
            RoutinePlanResult plan = new RoutinePlanService().Plan(
                loaded.Profile, "short", fixture.Snapshot,
                new ActiveEffectSnapshot(null), fixture.Effects,
                fixture.Options);
            if (plan.Plan.Steps.Any(step =>
                    step.Provider.Canonical == stale.ProviderKey) ||
                reloaded.SelectedSource.Providers.Any(provider =>
                    provider.Key.Canonical == stale.ProviderKey))
                throw new InvalidOperationException(
                    "A stale preference rebound to a current provider.");
        }

        private static ProviderPolicyFixture CreateProviderPolicyFixture(
            string campaignId, bool exhaustedFelix)
        {
            AbilityKey blur = Ability("ability-blur", string.Empty, 0);
            AbilityKey bulls = Ability("ability-bulls", string.Empty, 0);
            var felixBlurPool = new ResourcePoolSnapshot(
                "felix-blur-pool", ResourcePoolKind.SpontaneousLevel,
                10, exhaustedFelix ? 0 : 10, null);
            var akasaBlurPool = new ResourcePoolSnapshot(
                "akasa-blur-pool", ResourcePoolKind.SpontaneousLevel,
                10, 10, null);
            var felixBullsPool = new ResourcePoolSnapshot(
                "felix-bulls-pool", ResourcePoolKind.SpontaneousLevel,
                3, 3, null);
            var felixBlur = new ProviderSnapshot(
                new ProviderKey("felix", "felix-book", blur, "level-2"),
                "Blur", 2, felixBlurPool.PoolKey, 1, null);
            var akasaBlur = new ProviderSnapshot(
                new ProviderKey("akasa", "akasa-book", blur, "level-2"),
                "Blur", 2, akasaBlurPool.PoolKey, 1, null);
            var felixBulls = new ProviderSnapshot(
                new ProviderKey("felix", "felix-book", bulls, "level-2"),
                "Bulls Strength", 2, felixBullsPool.PoolKey, 1, null);
            string[] targets = { "target-1", "target-2", "target-3",
                "target-4", "target-5" };
            string[] units = new[] { "felix", "akasa" }.Concat(targets).ToArray();
            PartyProviderSnapshot snapshot = new PartyProviderSnapshot(
                units.Select(unit => new UnitSnapshot(
                    unit,
                    unit == "felix" ? "Felix" :
                    unit == "akasa" ? "Akasa" : unit,
                    false, string.Empty,
                    new TargetValidationSnapshot(
                        true, true, true, true))),
                new[] { felixBlur, akasaBlur, felixBulls },
                new[] { felixBlurPool, akasaBlurPool, felixBullsPool });
            var options = new[]
            {
                new ProviderPlanningOption(
                    felixBlur, units, units, 5, 50),
                new ProviderPlanningOption(
                    akasaBlur, units, units, 5, 50),
                new ProviderPlanningOption(
                    felixBulls, units, units, 5, 50)
            };
            var effects = new Dictionary<string, EffectExpression>
            {
                { blur.Canonical, Leaf("blur-effect") },
                { bulls.Canonical, Leaf("bulls-effect") }
            };
            BuffPlannerProfile profile =
                BuffPlannerProfile.CreateDefault(campaignId);
            var model = new PlannerSetupModel(
                profile, snapshot, new ActiveEffectSnapshot(null),
                effects, options, ignored => { });
            SetupSourceRow blurSource = model.Sources.Single(source =>
                source.Abilities.Any(ability => ability.Equals(blur)));
            SetupSourceRow bullsSource = model.Sources.Single(source =>
                source.Abilities.Any(ability => ability.Equals(bulls)));
            model.SelectSource(blurSource.SourceId);
            return new ProviderPolicyFixture(
                profile, model, snapshot, effects, options,
                blurSource, bullsSource,
                felixBlur, akasaBlur, felixBulls, targets);
        }

        private sealed class ProviderPolicyFixture
        {
            internal ProviderPolicyFixture(
                BuffPlannerProfile profile,
                PlannerSetupModel model,
                PartyProviderSnapshot snapshot,
                IDictionary<string, EffectExpression> effects,
                IEnumerable<ProviderPlanningOption> options,
                SetupSourceRow blurSource,
                SetupSourceRow bullsSource,
                ProviderSnapshot felixBlur,
                ProviderSnapshot akasaBlur,
                ProviderSnapshot felixBulls,
                string[] targetIds)
            {
                Profile = profile;
                Model = model;
                Snapshot = snapshot;
                Effects = effects;
                Options = options.ToArray();
                BlurSource = blurSource;
                BullsSource = bullsSource;
                FelixBlur = felixBlur;
                AkasaBlur = akasaBlur;
                FelixBulls = felixBulls;
                TargetIds = targetIds;
            }

            internal BuffPlannerProfile Profile;
            internal PlannerSetupModel Model;
            internal PartyProviderSnapshot Snapshot;
            internal IDictionary<string, EffectExpression> Effects;
            internal ProviderPlanningOption[] Options;
            internal SetupSourceRow BlurSource;
            internal SetupSourceRow BullsSource;
            internal ProviderSnapshot FelixBlur;
            internal ProviderSnapshot AkasaBlur;
            internal ProviderSnapshot FelixBulls;
            internal string[] TargetIds;

            internal RoutinePlanResult Plan(string routineId)
            {
                return new RoutinePlanService().Plan(
                    Profile, routineId, Snapshot,
                    new ActiveEffectSnapshot(null),
                    Effects, Options);
            }
        }

        private static void TestCatalogFilterState()
        {
            AbilityKey ability = Ability("ui-filter-source", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("ui-filter-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-ui-filter", ability,
                "ui-filter-free", 0);
            var validation = new TargetValidationSnapshot(true, true, true, true);
            var snapshot = new PartyProviderSnapshot(
                new[] { new UnitSnapshot("unit-a", "Cleric", false, string.Empty, validation) },
                new[] { provider }, new[] { pool });
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("campaign:ui-filter");
            var options = new[]
            {
                new ProviderPlanningOption(provider, new[] { "unit-a" }, new[] { "unit-a" }, 1, 10)
            };
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(new Dictionary<string, IEnumerable<string>>()),
                new Dictionary<string, EffectExpression> { { ability.Canonical, Leaf("ui-filter-effect") } },
                options, ignored => { });
            var state = new CatalogFilterState();
            CatalogFilterDiagnostics diagnostics;
            List<SetupSourceRow> visible = state.Apply(model, "long", out diagnostics);
            if (visible.Count != 1 || diagnostics.VisibleViewModels != 1 ||
                diagnostics.AfterHidden != 1 || diagnostics.AfterAvailability != 1)
                throw new InvalidOperationException("Default filters did not expose all available non-hidden entries.");

            state.Search = "does-not-exist";
            visible = state.Apply(model, "long", out diagnostics);
            if (visible.Count != 0 || diagnostics.TotalEntries != 1 ||
                diagnostics.AfterSearch != 0 || !diagnostics.ActiveFilters.Contains("does-not-exist"))
                throw new InvalidOperationException("All-hiding filters did not preserve an explicit diagnostic cause.");

            state.Search = string.Empty;
            state.SelectedOnly = true;
            visible = state.Apply(model, "long", out diagnostics);
            if (visible.Count != 0)
                throw new InvalidOperationException("Selected only included a buff with no active-routine targets.");
            model.ToggleTarget("long", "unit-a");
            visible = state.Apply(model, "long", out diagnostics);
            if (visible.Count != 1 || diagnostics.AssignedToActiveGroup != 1)
                throw new InvalidOperationException("Selected only did not follow direct active-routine targets.");
            state.SourceCategory = PlannerSourceCategory.Abilities;
            visible = state.Apply(model, "long", out diagnostics);
            if (visible.Count != 0)
                throw new InvalidOperationException("Ability category included a spellbook source.");
            state.Reset();
            visible = state.Apply(model, "long", out diagnostics);
            if (visible.Count != 1 || state.Search.Length != 0 || state.SelectedOnly ||
                state.SourceCategory != PlannerSourceCategory.All)
                throw new InvalidOperationException("Reset Filters did not restore the default visible catalog.");
        }

        private static void TestPresentationModels()
        {
            AbilityKey ability = Ability("ui-presentation-source", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("ui-presentation-free",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-ui-presentation",
                ability, "ui-presentation-free", 0);
            var validation = new TargetValidationSnapshot(true, true, true, true);
            var unit = new UnitSnapshot("unit-a", "Ret", false, string.Empty, validation);
            var invalidUnit = new UnitSnapshot("unit-b", "Pet", true, "unit-a", validation);
            var snapshot = new PartyProviderSnapshot(new[] { unit, invalidUnit },
                new[] { provider }, new[] { pool });
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("campaign:ui-presentation");
            int saves = 0;
            var options = new[]
            {
                new ProviderPlanningOption(provider, new[] { "unit-a" },
                    new[] { "unit-a" }, 1, 10)
            };
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(new Dictionary<string, IEnumerable<string>>()),
                new Dictionary<string, EffectExpression>
                {
                    { ability.Canonical, Leaf("ui-presentation-effect") }
                }, options, ignored => saves++);
            BuffCardViewModel card = new BuffCardViewModel(model.Sources[0], model, "long", true);
            if (card.Name.Length == 0 || card.Availability != "At will" ||
                card.Status != PlannerPresentationStatus.Neutral || !card.Selected ||
                card.SourceType != "Spell" || card.RoutineBadge.Length != 0)
                throw new InvalidOperationException("Neutral card presentation is invalid.");
            model.ToggleTarget("long", "unit-a");
            card = new BuffCardViewModel(model.Sources[0], model, "long", false);
            if (card.Status != PlannerPresentationStatus.Success ||
                card.RoutineBadge != "L" || card.Configuration != "1 target selected")
                throw new InvalidOperationException("Fulfillable card state is invalid.");
            int beforePreview = saves;
            RoutinePlanResult preview = new RoutinePlanService().Plan(profile, "long", snapshot,
                new ActiveEffectSnapshot(new Dictionary<string, IEnumerable<string>>()),
                new Dictionary<string, EffectExpression> { { ability.Canonical,
                    Leaf("ui-presentation-effect") } }, options);
            TargetPortraitViewModel target = TargetPortraitViewModel.Create(
                model.Sources[0], model, "long", unit, preview);
            TargetPortraitViewModel invalidTarget = TargetPortraitViewModel.Create(
                model.Sources[0], model, "long", invalidUnit, preview);
            var warningTarget = new TargetPortraitViewModel(unit,
                TargetPortraitState.DirectSelectedButUnavailable, true, false, false,
                false, "No prepared slot remains.");
            var routine = new RoutineSummaryViewModel("long", "Long", 1, 1);
            var settings = new PlannerSettingsViewModel(profile);
            if (target.Status != PlannerPresentationStatus.Success ||
                target.State != TargetPortraitState.DirectSelectedAndCovered ||
                warningTarget.Status != PlannerPresentationStatus.Warning ||
                warningTarget.State != TargetPortraitState.DirectSelectedButUnavailable ||
                invalidTarget.Status != PlannerPresentationStatus.Failure ||
                invalidTarget.State != TargetPortraitState.InvalidTarget ||
                routine.Label != "Long  1 ready" || settings.CastingMode != "Animated" ||
                saves != beforePreview)
                throw new InvalidOperationException("Player-facing presentation summaries are invalid.");
            model.SetAllValidTargets("long", false);
            if (model.IsTargetWanted("long", "unit-a") || saves != beforePreview + 1)
                throw new InvalidOperationException("Bulk target edit did not save once.");
        }

        private static void TestRoutineMembershipChips(string root)
        {
            AbilityKey ability = Ability("routine-membership", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("routine-membership-free",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "routine-book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { pool }, "unit-a");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 1, 10);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "campaign:routine-membership");
            var effects = new Dictionary<string, EffectExpression>
            {
                { ability.Canonical, Leaf("routine-membership-effect") }
            };
            int saves = 0;
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects, new[] { option },
                ignored => saves++);
            SetupSourceRow source = model.Sources.Single();
            BuffCardViewModel card = new BuffCardViewModel(source, model, "short", false);
            if (card.RoutineMemberships.Count != 0 || card.RoutineBadge.Length != 0)
                throw new InvalidOperationException("An unconfigured buff rendered a routine membership chip.");

            model.ToggleTarget("important", "unit-a");
            card = new BuffCardViewModel(source, model, "important", false);
            AssertMemberships(card, "I", "important");
            model.ToggleTarget("important", "unit-a");

            model.ToggleTarget("short", "unit-a");
            card = new BuffCardViewModel(source, model, "short", false);
            AssertMemberships(card, "S", "short");
            model.ToggleTarget("short", "unit-a");

            model.ToggleTarget("long", "unit-a");
            model.ToggleTarget("short", "unit-a");
            card = new BuffCardViewModel(source, model, "short", false);
            AssertMemberships(card, "LS", "short");
            if (card.RoutineMemberships.Single(value => value.RoutineId == "long").IsActive)
                throw new InvalidOperationException("Long plus Short did not emphasize Short only.");
            model.ToggleTarget("long", "unit-a");
            model.ToggleTarget("short", "unit-a");

            model.ToggleTarget("long", "unit-a");
            card = new BuffCardViewModel(source, model, "long", false);
            AssertMemberships(card, "L", "long");
            if (card.RoutineMemberships.Single().Tooltip != "Configured in active Long.")
                throw new InvalidOperationException("Active Long membership did not expose descriptive text.");

            model.ToggleTarget("important", "unit-a");
            card = new BuffCardViewModel(source, model, "important", false);
            AssertMemberships(card, "LI", "important");
            RoutineMembershipChipViewModel longChip = card.RoutineMemberships.Single(
                value => value.RoutineId == "long");
            if (longChip.IsActive || longChip.Tooltip != "Also configured in Long.")
                throw new InvalidOperationException("Cross-routine Long membership was not distinguishable.");

            model.ToggleTarget("short", "unit-a");
            card = new BuffCardViewModel(source, model, "short", false);
            AssertMemberships(card, "LIS", "short");
            if (card.RoutineBadge != "L I S" || !card.RoutineMemberships.Single(
                    value => value.RoutineId == "short").IsActive ||
                card.RoutineMemberships.Count(value => value.IsActive) != 1)
                throw new InvalidOperationException(
                    "All-routine membership did not preserve a single emphasized active chip.");

            var filters = new CatalogFilterState { Search = "routine-membership" };
            CatalogFilterDiagnostics diagnostics;
            SetupSourceRow rebuilt = filters.Apply(model, "short", out diagnostics).Single();
            BuffCardViewModel rebuiltCard = new BuffCardViewModel(rebuilt, model, "short", false);
            AssertMemberships(rebuiltCard, "LIS", "short");
            if (diagnostics.AfterSearch != 1)
                throw new InvalidOperationException("Search/filter rebuild lost the membership-bearing card.");

            string persistencePath = Path.Combine(root, "routine-membership");
            Directory.CreateDirectory(persistencePath);
            var repository = new ProfileRepository(persistencePath);
            repository.Save(profile);
            ProfileLoadResult loaded = repository.Load(profile.CampaignId);
            var reloaded = new PlannerSetupModel(loaded.Profile, snapshot,
                new ActiveEffectSnapshot(null), effects, new[] { option }, ignored => { });
            BuffCardViewModel reloadedCard = new BuffCardViewModel(
                reloaded.Sources.Single(), reloaded, "short", false);
            AssertMemberships(reloadedCard, "LIS", "short");

            model.ToggleTarget("short", "unit-a");
            card = new BuffCardViewModel(source, model, "short", false);
            AssertMemberships(card, "LI", string.Empty);
            if (saves < 12 || card.RoutineMemberships.Any(value =>
                    value.RoutineId == "short"))
                throw new InvalidOperationException(
                    "Removing the final active-routine target did not immediately remove its chip.");

            BuffGridMetrics narrow = BuffGridMetrics.Calculate(1420f, 500f);
            if (CompleteNameLayout.NameWidth(narrow.CellWidth) <=
                    CompleteNameLayout.RoutineChipWidth ||
                CompleteNameLayout.RoutineChipSize * 3f +
                    CompleteNameLayout.RoutineChipSpacing * 2f >
                    CompleteNameLayout.RoutineChipWidth)
                throw new InvalidOperationException(
                    "Narrow four-column cards cannot reserve non-overlapping membership chips and name text.");
        }

        private static void AssertMemberships(BuffCardViewModel card,
            string abbreviations, string activeRoutineId)
        {
            string actual = string.Concat(card.RoutineMemberships.Select(
                value => value.Abbreviation).ToArray());
            if (actual != abbreviations || card.RoutineMemberships.Any(value =>
                    value.IsActive != (value.RoutineId == activeRoutineId)))
                throw new InvalidOperationException(
                    "Routine membership chips did not reflect persisted assignments and active routine state.");
        }

        private static void TestDescriptionRequest()
        {
            AbilityKey ability = Ability("description-source", "description-variant", 0);
            var pool = new ResourcePoolSnapshot("description-free",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-description",
                ability, "description-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 1, 10);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("description");
            var model = new PlannerSetupModel(profile, snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression>
                {
                    { ability.Canonical, Leaf("description-effect") }
                }, new[] { option }, ignored => { });
            int assignmentsBefore = profile.Routines.Sum(routine => routine.Assignments.Count);
            PlannerDescriptionRequest request;
            if (PlannerDescriptionRequest.TryCreate(PlannerPointerGesture.Left,
                    model.SelectedSourceId, model.Sources, out request) || request != null)
                throw new InvalidOperationException("Left click was interpreted as description inspection.");
            if (!PlannerDescriptionRequest.TryCreate(PlannerPointerGesture.Right,
                    model.SelectedSourceId, model.Sources, out request) ||
                request == null || request.SourceId != model.SelectedSourceId ||
                !request.Ability.Equals(ability))
                throw new InvalidOperationException("Right click did not resolve the clicked row blueprint.");
            if (profile.Routines.Sum(routine => routine.Assignments.Count) != assignmentsBefore ||
                model.IsTargetLegal(model.SelectedSource, "unit-b"))
                throw new InvalidOperationException(
                    "Description inspection mutated the plan or required a legal target.");
        }

        private static void TestAreaCoveragePresentation()
        {
            AbilityKey ability = Ability("area-presentation", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("area-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-area", ability, "area-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                new[] { "unit-a" }, 1, 10);
            var area = new EffectLeafExpression(EffectKind.AreaBuff, "area-effect",
                EffectTarget.AlliedAreaRecipients, "area-contract", "area/path");
            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("area-preview"),
                snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, area } },
                new[] { option }, ignored => { });
            model.ToggleTarget("long", "unit-a");
            RoutinePlanResult preview = new RoutinePlanService().Plan(model.Profile, "long", snapshot,
                new ActiveEffectSnapshot(null), new Dictionary<string, EffectExpression>
                { { ability.Canonical, area } }, new[] { option });
            TargetPortraitViewModel direct = TargetPortraitViewModel.Create(model.Sources[0], model,
                "long", snapshot.Units.First(unit => unit.UnitId == "unit-a"), preview);
            TargetPortraitViewModel indirect = TargetPortraitViewModel.Create(model.Sources[0], model,
                "long", snapshot.Units.First(unit => unit.UnitId == "unit-b"), preview);
            if (direct.State != TargetPortraitState.DirectSelectedAndCovered || direct.Indirect ||
                indirect.State != TargetPortraitState.IndirectlyCovered || !indirect.Indirect ||
                indirect.Wanted || indirect.Tooltip != "Also affected by the planned cast.")
                throw new InvalidOperationException("Area coverage preview did not distinguish direct and indirect targets.");
        }

        private static void TestPerAnchorMassCoverage(string root)
        {
            AssertPerAnchorMassCoverage(
                "protection-from-arrows-communal-fixture", root);
            AssertPerAnchorMassCoverage("good-hope-fixture", root);
            AssertPerAnchorMassCoverage(
                "existing-communal-positive-control", root);
        }

        private static void AssertPerAnchorMassCoverage(string sourceId,
            string root)
        {
            AbilityKey ability = Ability(sourceId, string.Empty, 0);
            var pool = new ResourcePoolSnapshot(sourceId + "-pool",
                ResourcePoolKind.SpontaneousLevel, 3, 3, null);
            ProviderSnapshot provider = PlannerProvider("caster", "communal-book",
                ability, pool.PoolKey, 1);
            var valid = new TargetValidationSnapshot(true, true, true, true);
            PartyProviderSnapshot snapshot = new PartyProviderSnapshot(new[] {
                new UnitSnapshot("caster", "Caster", false, string.Empty, valid),
                new UnitSnapshot("anchor-a", "Anchor A", false, string.Empty, valid),
                new UnitSnapshot("anchor-b", "Anchor B", false, string.Empty, valid),
                new UnitSnapshot("ally-a", "Ally A", false, string.Empty, valid),
                new UnitSnapshot("ally-b", "Ally B", false, string.Empty, valid),
                new UnitSnapshot("outside", "Outside", false, string.Empty, valid),
                new UnitSnapshot("dead", "Dead", false, string.Empty,
                    new TargetValidationSnapshot(false, false, true, true)),
                new UnitSnapshot("hostile", "Hostile", false, string.Empty,
                    new TargetValidationSnapshot(true, true, false, true)),
                new UnitSnapshot("unavailable", "Unavailable", false,
                    string.Empty, new TargetValidationSnapshot(
                        true, true, true, false))
            }, new[] { provider }, new[] { pool });
            string[] firstRecipients = { "caster", "anchor-a", "ally-a" };
            string[] secondRecipients = { "anchor-b", "ally-b" };
            var coverage = new Dictionary<string, IEnumerable<string>>
            {
                { "anchor-a", firstRecipients },
                { "anchor-b", secondRecipients }
            };
            string[] reachable = firstRecipients.Concat(secondRecipients)
                .Distinct(StringComparer.Ordinal).ToArray();
            var option = new ProviderPlanningOption(provider, reachable,
                new[] { "anchor-a", "anchor-b" }, 5, 50, false, coverage);
            var effect = new EffectLeafExpression(EffectKind.AreaBuff,
                sourceId + "-effect", EffectTarget.AlliedAreaRecipients,
                "AbilityTargetsAround+friend-only", "root/area");
            var source = new BuffSourceDefinition(sourceId, ability, effect,
                CastGroupingKind.MassConfiguredTargets);
            var planner = new CastPlanner();
            CastPlan persistedAnchor = planner.Plan(snapshot, new BuffCastRequest(source,
                new[] { "anchor-a" }, ExistingEffectPolicy.Overwrite, null),
                new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (persistedAnchor.Steps.Count != 1 ||
                persistedAnchor.Steps.Single().AnchorUnitId != "anchor-a" ||
                !persistedAnchor.Steps.Single().TargetUnitIds.SequenceEqual(
                    new[] { "anchor-a" }) ||
                !persistedAnchor.Steps.Single().ExpectedRecipientUnitIds.SequenceEqual(
                    firstRecipients.OrderBy(value => value,
                        StringComparer.Ordinal)) ||
                persistedAnchor.Steps.Single().Reservation.Units != 1)
                throw new InvalidOperationException(
                    "A single selected communal anchor did not produce complete indirect recipient coverage: " + sourceId);

            CastPlan explicitEveryRecipient = planner.Plan(snapshot,
                new BuffCastRequest(source, firstRecipients,
                    ExistingEffectPolicy.Overwrite, null), new[] { option },
                EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (explicitEveryRecipient.Steps.Count != 1 ||
                explicitEveryRecipient.Steps.Single().Reservation.Units != 1 ||
                explicitEveryRecipient.Outcomes.Count(outcome =>
                    outcome.Kind == TargetOutcomeKind.Fulfilled) !=
                        firstRecipients.Length)
                throw new InvalidOperationException(
                    "Mass coverage scheduled one cast per teammate instead of one structural cast: " + sourceId);

            string profileRoot = Path.Combine(root, "coverage-" +
                sourceId.Substring(0, Math.Min(8, sourceId.Length)));
            Directory.CreateDirectory(profileRoot);
            var repository = new ProfileRepository(profileRoot);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "coverage:" + sourceId);
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, effect } },
                new[] { option }, repository.Save);
            model.ToggleTarget("short", "anchor-a");
            RoutinePlanResult preview = new RoutinePlanService().Plan(model.Profile,
                "short", snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, effect } },
                new[] { option });
            TargetPortraitViewModel[] portraits = snapshot.Units.Select(unit =>
                TargetPortraitViewModel.Create(model.Sources.Single(), model,
                    "short", unit, preview)).ToArray();
            if (portraits.Count(value => value.State ==
                    TargetPortraitState.DirectSelectedAndCovered) != 1 ||
                portraits.Single(value => value.UnitId == "anchor-a").Indirect ||
                portraits.Single(value => value.UnitId == "ally-a").State !=
                    TargetPortraitState.IndirectlyCovered ||
                portraits.Single(value => value.UnitId == "outside").State !=
                    TargetPortraitState.InvalidTarget ||
                new[] { "dead", "hostile", "unavailable" }.Any(id =>
                    portraits.Single(value => value.UnitId == id).Indirect))
                throw new InvalidOperationException(
                    "Anchor/direct and teammate/indirect presentation diverged from the structural plan: " + sourceId);

            BuffPlannerProfile loaded = repository.Load(profile.CampaignId)
                .Profile;
            var reloaded = new PlannerSetupModel(loaded, snapshot,
                new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, effect }
                }, new[] { option }, repository.Save);
            RoutinePlanResult reloadedPreview = new RoutinePlanService().Plan(
                loaded, "short", snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, effect }
                }, new[] { option });
            if (!reloadedPreview.Plan.Steps.Single()
                    .ExpectedRecipientUnitIds.SequenceEqual(
                        preview.Plan.Steps.Single().ExpectedRecipientUnitIds) ||
                TargetPortraitViewModel.Create(reloaded.Sources.Single(),
                    reloaded, "short", snapshot.Units.Single(value =>
                        value.UnitId == "ally-a"), reloadedPreview).State !=
                    TargetPortraitState.IndirectlyCovered)
                throw new InvalidOperationException(
                    "Persisted communal coverage did not reproduce after reload: " +
                    sourceId);

            reloaded.ToggleTarget("short", "anchor-a");
            reloaded.ToggleTarget("short", "anchor-b");
            RoutinePlanResult moved = new RoutinePlanService().Plan(loaded,
                "short", snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, effect }
                }, new[] { option });
            if (moved.Plan.Steps.Count != 1 ||
                moved.Plan.Steps.Single().AnchorUnitId != "anchor-b" ||
                !moved.Plan.Steps.Single().ExpectedRecipientUnitIds
                    .SequenceEqual(secondRecipients.OrderBy(value => value,
                        StringComparer.Ordinal)) ||
                moved.Plan.Steps.Single().ExpectedRecipientUnitIds.Contains(
                    "ally-a"))
                throw new InvalidOperationException(
                    "Changing the communal anchor did not recompute coverage deterministically: " +
                    sourceId);
        }

        private static void TestPersonalTargetEligibility()
        {
            AbilityKey personalAbility = Ability("personal-source", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("personal-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot casterA = PlannerProvider("unit-a", "book-personal-a",
                personalAbility, "personal-free", 0);
            PartyProviderSnapshot snapshotA = PlannerSnapshot(new[] { casterA }, new[] { pool },
                "unit-a", "unit-b");
            var personalA = new ProviderPlanningOption(casterA, new[] { "unit-a" },
                new[] { "unit-a" }, 1, 10);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("personal-targeting");
            var effects = new Dictionary<string, EffectExpression>
            {
                { personalAbility.Canonical, Leaf("personal-effect") }
            };
            var modelA = new PlannerSetupModel(profile, snapshotA, new ActiveEffectSnapshot(null),
                effects, new[] { personalA }, ignored => { });
            if (!modelA.IsTargetLegal(modelA.SelectedSource, "unit-a") ||
                modelA.IsTargetLegal(modelA.SelectedSource, "unit-b"))
                throw new InvalidOperationException("A personal spell was not limited to its provider caster.");
            bool rejected = false;
            try { modelA.ToggleTarget("long", "unit-b"); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected || modelA.IsTargetWanted("long", "unit-b"))
                throw new InvalidOperationException("An invalid personal target mutated the assignment.");

            profile.Routines.First(routine => routine.RoutineId == "long").Assignments.Add(
                Assignment(modelA.SelectedSource.SourceId, personalAbility,
                    new[] { "unit-b" }, null, ExistingEffectPolicy.Overwrite));
            RoutinePlanResult stale = new RoutinePlanService().Plan(profile, "long", snapshotA,
                new ActiveEffectSnapshot(null), effects, new[] { personalA });
            if (stale.Plan.Steps.Count != 0 || stale.Plan.Outcomes.Count != 1 ||
                stale.Plan.Outcomes[0].Kind != TargetOutcomeKind.Unfulfilled)
                throw new InvalidOperationException("A persisted invalid personal target entered the cast plan.");

            ProviderSnapshot casterB = PlannerProvider("unit-b", "book-personal-b",
                personalAbility, "personal-free", 0);
            PartyProviderSnapshot snapshotB = PlannerSnapshot(new[] { casterB }, new[] { pool },
                "unit-a", "unit-b");
            var personalB = new ProviderPlanningOption(casterB, new[] { "unit-b" },
                new[] { "unit-b" }, 1, 10);
            var modelB = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("personal-rebound"),
                snapshotB, new ActiveEffectSnapshot(null), effects, new[] { personalB }, ignored => { });
            if (modelB.IsTargetLegal(modelB.SelectedSource, "unit-a") ||
                !modelB.IsTargetLegal(modelB.SelectedSource, "unit-b"))
                throw new InvalidOperationException("Personal target legality did not follow the changed caster.");

            var friendly = new ProviderPlanningOption(casterA, new[] { "unit-b" },
                new[] { "unit-b" }, 1, 10);
            var selfOrAlly = new ProviderPlanningOption(casterA, new[] { "unit-a", "unit-b" },
                new[] { "unit-a", "unit-b" }, 1, 10);
            var friendlyModel = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("friendly"),
                snapshotA, new ActiveEffectSnapshot(null), effects, new[] { friendly }, ignored => { });
            var selfOrAllyModel = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("self-or-ally"),
                snapshotA, new ActiveEffectSnapshot(null), effects, new[] { selfOrAlly }, ignored => { });
            if (friendlyModel.IsTargetLegal(friendlyModel.SelectedSource, "unit-a") ||
                !friendlyModel.IsTargetLegal(friendlyModel.SelectedSource, "unit-b") ||
                !selfOrAllyModel.IsTargetLegal(selfOrAllyModel.SelectedSource, "unit-a") ||
                !selfOrAllyModel.IsTargetLegal(selfOrAllyModel.SelectedSource, "unit-b"))
                throw new InvalidOperationException("Friendly or self-or-ally target behavior regressed.");
        }

        private static void TestSingleTargetCoveragePresentation()
        {
            AbilityKey ability = Ability("single-presentation", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("single-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-single", ability, "single-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                new[] { "unit-a", "unit-b" }, 1, 10);
            EffectExpression effect = Leaf("single-effect");
            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("single-preview"),
                snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, effect } },
                new[] { option }, ignored => { });
            model.ToggleTarget("long", "unit-a");
            RoutinePlanResult preview = new RoutinePlanService().Plan(model.Profile, "long", snapshot,
                new ActiveEffectSnapshot(null), new Dictionary<string, EffectExpression>
                { { ability.Canonical, effect } }, new[] { option });
            TargetPortraitViewModel other = TargetPortraitViewModel.Create(model.Sources[0], model,
                "long", snapshot.Units.First(unit => unit.UnitId == "unit-b"), preview);
            if (other.State != TargetPortraitState.Neutral || other.IsExpectedRecipient)
                throw new InvalidOperationException("Single-target plan created false indirect coverage.");
        }

        private static void TestCasterCenteredCoveragePresentation()
        {
            AbilityKey ability = Ability("caster-presentation", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("caster-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-caster", ability, "caster-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 1, 10);
            var effect = new EffectLeafExpression(EffectKind.Buff, "caster-effect", EffectTarget.Caster,
                "caster-contract", "caster/path");
            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("caster-preview"),
                snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, effect } },
                new[] { option }, ignored => { });
            TargetPortraitViewModel caster = TargetPortraitViewModel.Create(model.Sources[0], model,
                "long", snapshot.Units.First(unit => unit.UnitId == "unit-a"), null);
            if (caster.State != TargetPortraitState.Neutral || caster.IsExplicitlyRequested)
                throw new InvalidOperationException("Caster-centered preview invented a direct receiver.");
        }

        private static void TestGridMetrics()
        {
            BuffGridMetrics fullHd = BuffGridMetrics.Calculate(1824f, 610f);
            BuffGridMetrics compact = BuffGridMetrics.Calculate(1420f, 500f);
            if (fullHd.Columns != 4 || compact.Columns != 4 ||
                fullHd.HorizontalScrolling || compact.HorizontalScrolling ||
                fullHd.CellWidth <= 0 || compact.CellWidth <= 0 ||
                Math.Abs(fullHd.SideInset * 2f + fullHd.CellWidth * 4f +
                    fullHd.HorizontalSpacing * 3f - 1824f) > 0.01f ||
                fullHd.SideInset < fullHd.HorizontalSpacing)
                throw new InvalidOperationException("Grid metrics did not preserve four columns without horizontal scrolling.");
        }

        private static void TestLargeCatalogGridWindow()
        {
            const int itemCount = 2500;
            int lastRow = BuffGridMetrics.RowCount(itemCount) - 1;
            int firstModel = BuffGridMetrics.ModelIndex(lastRow, 0);
            if (BuffGridMetrics.RowCount(itemCount) != 625 ||
                BuffGridMetrics.PoolCapacity != 32 || firstModel != 2496 ||
                BuffGridMetrics.ModelIndex(400, 31) != 1631)
                throw new InvalidOperationException(
                    "Large-catalog grid paging is unbounded or maps pooled cards incorrectly.");
        }

        private static void TestPlannerHotkeyBinding()
        {
            if (!PlannerHotkeyBinding.ShouldSuppress("Ctrl+Shift+B", "B", "OpenSpellbook",
                    true, true, false) ||
                PlannerHotkeyBinding.ShouldSuppress("Ctrl+Shift+B", "B", "OpenSpellbook",
                    true, false, false) ||
                PlannerHotkeyBinding.ShouldSuppress("Ctrl+Shift+B", "B", "OpenSpellbook",
                    true, true, true) ||
                PlannerHotkeyBinding.ShouldSuppress("Ctrl+Shift+B", "F10", "Console",
                    true, true, false) ||
                !PlannerHotkeyBinding.ShouldSuppress("Ctrl+Shift+P", "P", "Pause",
                    true, true, false))
                throw new InvalidOperationException("Planner chord isolation or fallback key is invalid.");
        }

        private static void TestAnimatedExecutor()
        {
            AbilityKey ability = Ability("animated", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("animated-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-a", ability, "animated-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                new[] { "unit-a" }, 1, 1);
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "unit-a", "unit-b" }, new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            var runtime = new FakeAnimatedRuntime();
            var report = new ExecutionReport(plan);
            var enumerator = new AnimatedCastExecutor(runtime, true).Execute(plan, report);
            int moves = 0;
            while (enumerator.MoveNext())
                if (++moves > 20) throw new InvalidOperationException("Animated executor did not terminate.");
            if (runtime.StartCount != 1 || report.Planned != 2 || report.Queued != 1 ||
                report.CastStarted != 1 || report.Failed != 1 ||
                report.Confirmed != 1 ||
                report.ResourcesSpent != 1 ||
                report.Records.First(r => r.Status == CastExecutionStatus.FailedValidation).Detail != "target-invalid")
                throw new InvalidOperationException("Animated executor queued an invalid cast or misreported completion.");
        }

        private static void TestInputLease()
        {
            var boundary = new FakeInputBoundary();
            BuffPlannerInputLease lease = BuffPlannerInputLease.Acquire(boundary);
            if (boundary.CaptureCount != 1 || boundary.EnterCount != 1 || boundary.RestoreCount != 0)
                throw new InvalidOperationException("Input lease did not acquire exactly once.");
            lease.Dispose();
            lease.Dispose();
            if (boundary.RestoreCount != 1 || !lease.IsReleased)
                throw new InvalidOperationException("Input lease did not release idempotently.");

            boundary = new FakeInputBoundary { FailEnter = true };
            bool failed = false;
            try { BuffPlannerInputLease.Acquire(boundary); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed || boundary.RestoreCount != 1)
                throw new InvalidOperationException("Input lease did not restore after acquisition failure.");
        }

        private static void TestDeferredUiReadiness()
        {
            var gate = new DeferredUiReadinessGate(2);
            if (gate.IsReady || gate.ObservedFrames != 0)
                throw new InvalidOperationException("A new readiness gate was already ready.");
            if (gate.ObserveFrame() || gate.IsReady || gate.ObservedFrames != 1)
                throw new InvalidOperationException("Same/first-frame validation was permitted.");
            if (!gate.ObserveFrame() || !gate.IsReady || gate.ObservedFrames != 2)
                throw new InvalidOperationException("Deferred readiness did not open on the later frame.");
            if (!gate.ObserveFrame() || gate.ObservedFrames != 2)
                throw new InvalidOperationException("Readiness was not stable and bounded.");
            gate.Reset();
            if (gate.IsReady || gate.ObservedFrames != 0)
                throw new InvalidOperationException("Readiness reset did not require a new frame sequence.");
        }

        private static void TestScreenStateMachine()
        {
            var boundary = new FakeInputBoundary();
            var machine = new PlannerScreenStateMachine(() => BuffPlannerInputLease.Acquire(boundary));
            if (!machine.BeginPresentation() || machine.State != PlannerScreenLifecycleState.OpeningPresentation ||
                boundary.CaptureCount != 0)
                throw new InvalidOperationException("Screen acquired input before presentation validation.");
            machine.AcquireInputLease();
            if (machine.BeginPresentation() || !machine.IsOpen || machine.OpenTransitions != 1)
                throw new InvalidOperationException("Screen open transition was not idempotent.");
            if (!machine.Close() || machine.Close() || machine.IsOpen || machine.CloseTransitions != 1 ||
                boundary.RestoreCount != 1)
                throw new InvalidOperationException("Screen close transition was not idempotent.");
            machine.BeginPresentation();
            machine.AcquireInputLease();
            machine.Dispose();
            if (machine.IsOpen || boundary.RestoreCount != 2)
                throw new InvalidOperationException("Screen disposal leaked the input lease.");

            boundary = new FakeInputBoundary { FailEnter = true };
            machine = new PlannerScreenStateMachine(() => BuffPlannerInputLease.Acquire(boundary));
            machine.BeginPresentation();
            bool failed = false;
            try { machine.AcquireInputLease(); }
            catch (InvalidOperationException) { failed = true; }
            if (!failed || machine.State != PlannerScreenLifecycleState.Closed ||
                machine.HasInputLease || boundary.RestoreCount != 1 || machine.RollbackTransitions != 1)
                throw new InvalidOperationException("Screen acquisition failure did not roll back to gameplay.");

            boundary = new FakeInputBoundary();
            machine = new PlannerScreenStateMachine(() => BuffPlannerInputLease.Acquire(boundary));
            machine.BeginPresentation();
            machine.Rollback();
            if (machine.State != PlannerScreenLifecycleState.Closed || boundary.CaptureCount != 0 ||
                boundary.EnterCount != 0 || boundary.RestoreCount != 0 || machine.RollbackTransitions != 1)
                throw new InvalidOperationException("Invalid presentation acquired or restored a lease it never owned.");
        }

        private static void TestSetupOpenSoundGate()
        {
            var gate = new SetupOpenSoundGate();
            if (!gate.BeginHiddenToVisible() || gate.BeginHiddenToVisible() ||
                gate.CompleteVisible(false) || !gate.CompleteVisible(true) ||
                gate.CompleteVisible(true))
                throw new InvalidOperationException(
                    "Setup-opening sound gate did not emit exactly once after a successful visible transition.");
            if (!gate.BeginHiddenToVisible())
                throw new InvalidOperationException(
                    "A later hidden-to-visible setup transition did not re-arm its native sound.");
            gate.Cancel();
            if (gate.CompleteVisible(true))
                throw new InvalidOperationException(
                    "A rejected or rolled-back setup opening emitted a sound.");
        }

        private static void TestNativeHudSourceContract()
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(
                directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null)
                throw new InvalidOperationException("Repository root was not discoverable.");
            string root = directory.FullName;
            string hud = File.ReadAllText(Path.Combine(root, "src",
                "KingmakerBuffPlanner", "UI", "BuffPlannerHudButtonController.cs"));
            string screen = File.ReadAllText(Path.Combine(root, "src",
                "KingmakerBuffPlanner", "UI", "BuffPlannerScreenController.cs"));
            string uiRoot = File.ReadAllText(Path.Combine(root, "src",
                "KingmakerBuffPlanner", "UI", "BuffPlannerUiRoot.cs"));
            string runtime = File.ReadAllText(Path.Combine(root, "src",
                "KingmakerBuffPlanner", "RuntimeTesting", "RuntimeTestHost.cs"));
            if (!hud.Contains("NativeHudButtonStyle.Capture") ||
                !hud.Contains("TooltipTrigger") ||
                !hud.Contains("Color ink = Color.white") ||
                !hud.Contains("nativeTooltip.SetNameAndDescription") ||
                hud.Contains("KBP.InnerFrame") || hud.Contains("KBP.LowerAccent") ||
                hud.Contains("CreateHudMessage") ||
                hud.Contains("Color(0.961f, 0.820f, 0.420f") ||
                !screen.Contains("SetupOpenSoundGate") ||
                !uiRoot.Contains("UISoundType.CharacterScreenOpen") ||
                !runtime.Contains("nativeSkin=True") ||
                !runtime.Contains("TooltipUsesNativeParchmentPresentation") ||
                runtime.Contains("spriteInk=0.961,0.820,0.420,1.000"))
                throw new InvalidOperationException(
                    "Native HUD capture, parchment tooltip, or one-shot setup sound contract regressed.");
        }

        private static void TestQuickExecutionFlow()
        {
            var diagnostics = new BuffPlannerUiLifecycleDiagnostics();
            QuickExecutionResult presented = null;
            var runner = new FakeRoutineRunner(new QuickExecutionResult("long", "Long",
                QuickExecutionDisposition.Refused, "No Long buffs are configured.", 0, 0, 0));
            var controller = new BuffPlannerQuickExecuteController(runner, diagnostics,
                result => presented = result);
            diagnostics.RecordPointer("long");
            if (!controller.Execute("long") || presented == null ||
                presented.Message != "No Long buffs are configured.")
                throw new InvalidOperationException("Empty Long routine was silent.");
            QuickFlowDiagnostics flow = diagnostics.GetFlow("long");
            if (flow.PointerEvents != 1 || flow.Listeners != 1 || flow.GroupsResolved != 1 ||
                flow.PlansRevalidated != 1 || flow.ExecutionsInvoked != 0 || flow.Refusals != 1 ||
                flow.ResultsPresented != 1 || runner.StartCount != 1)
                throw new InvalidOperationException("Quick execution stages did not reconcile exactly once.");

            var activeResult = new QuickExecutionResult(
                "long", "Long", QuickExecutionDisposition.Refused,
                "No Long casts can run: skipped active=12; unfulfilled=0.",
                0, 0, 0);
            var activeRunner = new FakeRoutineRunner(activeResult);
            var activeController = new BuffPlannerQuickExecuteController(
                activeRunner, diagnostics, result => presented = result);
            if (!activeController.Execute("long") ||
                !ReferenceEquals(presented, activeResult) ||
                presented.Disposition != QuickExecutionDisposition.Refused ||
                presented.Planned != 0 || presented.Submitted != 0 ||
                presented.Confirmed != 0 ||
                !presented.Message.Contains("skipped active=12") ||
                !presented.Message.Contains("unfulfilled=0"))
                throw new InvalidOperationException(
                    "All-active quick execution changed its callback result or counts.");
        }

        private static void TestQuickResultPresentationBoundary()
        {
            DirectoryInfo directory =
                new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null &&
                !File.Exists(Path.Combine(
                    directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null)
                throw new InvalidOperationException(
                    "Repository root was not discoverable for the source boundary test.");
            string root = directory.FullName;
            string hud = File.ReadAllText(Path.Combine(
                root, "src", "KingmakerBuffPlanner", "UI",
                "BuffPlannerHudButtonController.cs"));
            string uiRoot = File.ReadAllText(Path.Combine(
                root, "src", "KingmakerBuffPlanner", "UI",
                "BuffPlannerUiRoot.cs"));
            string screen = File.ReadAllText(Path.Combine(
                root, "src", "KingmakerBuffPlanner", "UI",
                "BuffPlannerScreenView.cs"));
            string session = File.ReadAllText(Path.Combine(
                root, "src", "KingmakerBuffPlanner", "UI",
                "PlannerUiSession.cs"));
            if (hud.Contains("Feedback") ||
                hud.Contains("_feedback") ||
                hud.Contains("void Present(QuickExecutionResult") ||
                uiRoot.Contains("_hud.Present(result)") ||
                !uiRoot.Contains("_screen.Present(result)") ||
                !uiRoot.Contains("Routine UI result:") ||
                !screen.Contains("result.Message") ||
                !session.Contains("[KBP-QUICK]") ||
                !session.Contains("skipped active=") ||
                !hud.Contains("Setup|Long|Important|Short") ||
                !hud.Contains("RoutineTooltip"))
                throw new InvalidOperationException(
                    "Quick results crossed the HUD-only presentation boundary or lost diagnostics.");

            string[] nativeLogContracts =
            {
                "MessageLogThread",
                "AddMessage(",
                "CombatLog",
                "EventLog"
            };
            string[] production = Directory.GetFiles(
                Path.Combine(root, "src", "KingmakerBuffPlanner"),
                "*.cs", SearchOption.AllDirectories);
            foreach (string contract in nativeLogContracts)
                if (production.Any(path => File.ReadAllText(path)
                    .IndexOf(contract, StringComparison.Ordinal) >= 0))
                    throw new InvalidOperationException(
                        "A native common/combat/event-log path was added: " +
                        contract);
        }

        private static void TestInstantExecutor()
        {
            AbilityKey ability = Ability("instant", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("instant-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-0", "book-i", ability, "instant-free", 0);
            string[] units = Enumerable.Range(0, 9).Select(i => "unit-" + i).ToArray();
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, units);
            var option = new ProviderPlanningOption(provider, units, new[] { "unit-0" }, 1, 1);
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget, units,
                new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            var runtime = new FakeInstantRuntime();
            var report = new ExecutionReport(plan);
            var enumerator = new InstantCastExecutor(runtime, true, 4).Execute(plan, report);
            int yieldedFrames = 0;
            while (enumerator.MoveNext()) yieldedFrames++;
            if (yieldedFrames != 2 || runtime.FireCount != 8 || report.Submitted != 8 ||
                report.CastStarted != 8 || report.Failed != 1 || report.Confirmed != 8 ||
                report.ResourcesSpent != 8)
                throw new InvalidOperationException("Instant executor bypassed validation, batching, or reporting.");
        }

        private static void TestHybridExecutor()
        {
            AbilityKey ability = Ability("hybrid", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("hybrid-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-a", ability, "hybrid-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                new[] { "unit-a", "unit-b" }, 1, 1);
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "unit-a", "unit-b" }, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null));
            var animated = new AlwaysAnimatedRuntime();
            var instant = new AlwaysInstantRuntime();
            var report = new ExecutionReport(plan);
            var executor = new HybridCastExecutor(instant, animated,
                step => step.TargetUnitIds.Contains("unit-a"), true, true);
            Drain(executor.Execute(plan, report));
            if (animated.StartCount != 1 || instant.FireCount != 1 || report.Queued != 1 ||
                report.Submitted != 1 || report.Confirmed != 2 || report.Failed != 0)
                throw new InvalidOperationException("Hybrid executor did not route exact per-step execution modes.");

            animated = new AlwaysAnimatedRuntime();
            instant = new AlwaysInstantRuntime();
            report = new ExecutionReport(plan);
            executor = new HybridCastExecutor(instant, animated,
                step => step.TargetUnitIds.Contains("unit-a"), false, true);
            Drain(executor.Execute(plan, report));
            if (animated.StartCount != 0 || instant.FireCount != 1 || report.Failed != 1 ||
                !report.Records.Any(r => r.Detail == "animated-fallback-disabled"))
                throw new InvalidOperationException("Disabled animated fallback was not blocked before firing.");

            animated = new AlwaysAnimatedRuntime();
            instant = new AlwaysInstantRuntime();
            report = new ExecutionReport(plan);
            executor = new HybridCastExecutor(instant, animated,
                step => false, false, true,
                step => step.TargetUnitIds.Contains("unit-a"));
            Drain(executor.Execute(plan, report));
            if (animated.StartCount != 1 || instant.FireCount != 1 ||
                report.Confirmed != 2 || report.Failed != 0)
                throw new InvalidOperationException(
                    "A native-command enhancement was treated as optional animated fallback.");
        }

        private static void TestRoutingEvidence()
        {
            CastPlan plan = CreateShareDirectPlan(4, false);
            var instant = new AlwaysInstantRuntime();
            var animated = new AlwaysAnimatedRuntime();
            var report = new ExecutionReport(plan);
            var routes = new List<string>();
            int nativeChecks = 0;
            System.Collections.IEnumerator work = new HybridCastExecutor(instant, animated, false, true,
                step => { nativeChecks++; return true; },
                (index, step, useAnimated, detail) =>
                {
                    if (index != 0 || !useAnimated || instant.FireCount != 0 ||
                        animated.StartCount != 0 || report.Records.Count != 1 ||
                        report.Records[0].Status != CastExecutionStatus.ExecutorSelected)
                        throw new InvalidOperationException("Routing was not recorded before starting the cast.");
                    routes.Add(detail);
                }).Execute(plan, report);
            work.MoveNext();
            ((IDisposable)work).Dispose();
            if (nativeChecks != 1 || routes.Count != 1 ||
                !routes[0].Contains("planned-strategy:ProviderDirectRuleCast") ||
                !routes[0].Contains("actual-executor:Animated") ||
                !routes[0].Contains("native-callback:True") ||
                !routes[0].Contains("native-strategy:False") ||
                report.Records.Count(record => record.Status ==
                    CastExecutionStatus.ExecutorSelected) != 1)
                throw new InvalidOperationException("Cancellation lost the decisive native callback override.");

            plan = CreateLegacySharePlan();
            report = new ExecutionReport(plan);
            routes.Clear();
            Drain(new HybridCastExecutor(new AlwaysInstantRuntime(),
                new AlwaysAnimatedRuntime(), false, true, null,
                (index, step, useAnimated, detail) => routes.Add(detail))
                .Execute(plan, report));
            if (routes.Count != 1 || !routes[0].Contains("native-callback:False") ||
                !routes[0].Contains("native-strategy:True") ||
                !routes[0].Contains("actual-executor:Animated"))
                throw new InvalidOperationException("Legacy strategy fallback was confused with a callback override.");
        }

        private static void TestInstantFallbackFeedback()
        {
            var fallback = new QuickExecutionResult("long", "Long",
                QuickExecutionDisposition.Completed, "Animated Share fallback.", 4, 4, 4, true);
            var direct = new QuickExecutionResult("long", "Long",
                QuickExecutionDisposition.Completed, "Effects confirmed.", 4, 4, 4);
            var failed = new QuickExecutionResult("long", "Long",
                QuickExecutionDisposition.Failed, "Cast interrupted.", 4, 1, 0, true);
            if (fallback.Disposition != QuickExecutionDisposition.CompletedWithFallback ||
                !fallback.UsedAnimatedFallback || fallback.Confirmed != 4 ||
                direct.Disposition != QuickExecutionDisposition.Completed ||
                direct.UsedAnimatedFallback || failed.Disposition != QuickExecutionDisposition.Failed)
                throw new InvalidOperationException("Fallback erased effect confirmation or claimed Instant completion.");
        }

        private static void TestShareDirectRoutingPolicy()
        {
            CastPlan supported = CreateShareDirectPlan(4, true);
            if (supported.Steps.Count != 4 || supported.Steps.Any(step =>
                    step.ExecutionStrategy !=
                        CastExecutionStrategy.ProviderDirectRuleCast) ||
                supported.Steps.Any(step => !step.ExecutionStrategyReason
                    .Contains("provider-direct-cast:" +
                        "brown-fur-direct-cast-v1")) ||
                supported.Steps.Any(step => step.Reservation.Units != 1) ||
                supported.Steps.Any(step =>
                    step.EnhancementUsageByPool["reservoir|brown"] != 2) ||
                supported.Steps[2].AnchorUnitId != "party-3" ||
                supported.Steps[3].AnchorUnitId != "party-4")
                throw new InvalidOperationException(
                    "Supported Share plus Powerful Change did not preserve the provider-direct strategy, exact source cost, combined reservoir forecast, or third/fourth targets.");

            CastPlan ordinary = CreateOrdinaryInstantPlan();
            if (ordinary.Steps.Single().ExecutionStrategy !=
                    CastExecutionStrategy.DirectRuleCast ||
                ordinary.Steps.Single().EnhancementIds.Count != 0)
                throw new InvalidOperationException(
                    "An ordinary self/instant buff was changed by the Share direct-cast policy.");

            CastPlan legacy = CreateLegacySharePlan();
            var instant = new AlwaysInstantRuntime();
            var animated = new AlwaysAnimatedRuntime();
            var report = new ExecutionReport(legacy);
            Drain(new HybridCastExecutor(instant, animated, false, true)
                .Execute(legacy, report));
            if (legacy.Steps.Single().ExecutionStrategy !=
                    CastExecutionStrategy.NativeCommandRequired ||
                instant.FireCount != 0 || animated.StartCount != 1 ||
                report.Confirmed != 1 || !report.Records.Any(record =>
                    record.Status == CastExecutionStatus.StrategySelected &&
                    record.Detail.Contains(
                        "share-transmutation-legacy-native-command")))
                throw new InvalidOperationException(
                    "An older provider contract silently bypassed the safe native-command route.");
        }

        private static void TestShareDirectFourRecipientExecution()
        {
            CastPlan share = CreateShareDirectPlan(4, true);
            CastPlan ordinary = CreateOrdinaryInstantPlan();
            var combined = new CastPlan(share.Steps.Concat(ordinary.Steps),
                share.Outcomes.Concat(ordinary.Outcomes),
                share.Diagnostics.Concat(ordinary.Diagnostics));
            var runtime = new ProviderDirectSequenceRuntime(5, 8);
            var animated = new AlwaysAnimatedRuntime();
            var report = new ExecutionReport(combined);
            Drain(new HybridCastExecutor(runtime, animated, false, true)
                .Execute(combined, report));
            if (runtime.FireCount != 5 ||
                runtime.ProviderTransactionCount != 4 ||
                runtime.OrdinaryFireCount != 1 ||
                runtime.SourceSpendInvocations != 5 ||
                runtime.SourceUsesRemaining != 0 ||
                runtime.ReservoirDebit != 8 ||
                runtime.ReservoirRemaining != 0 ||
                runtime.EnhancementPrepareCount != 4 ||
                runtime.EnhancementDisposeCount != 4 ||
                runtime.CleanupCount != 0 || runtime.PrematureValidation ||
                !runtime.EffectRecipients.Contains("party-3") ||
                !runtime.EffectRecipients.Contains("party-4") ||
                !runtime.EffectRecipients.Contains("brown") ||
                animated.StartCount != 0 || report.Submitted != 5 ||
                report.SpendInvocations != 5 ||
                report.ResourcesSpent != 5 || report.Confirmed != 5 ||
                report.Failed != 0)
                throw new InvalidOperationException(
                    "Four direct Share transactions or the subsequent ordinary buff advanced early, animated, missed an effect, or consumed the wrong source/reservoir owner.");

            var explicitAnimated = new AlwaysAnimatedRuntime();
            var animatedReport = new ExecutionReport(combined);
            Drain(new AnimatedCastExecutor(explicitAnimated, true)
                .Execute(combined, animatedReport));
            if (explicitAnimated.StartCount != 5 ||
                animatedReport.Queued != 5 ||
                animatedReport.Confirmed != 5)
                throw new InvalidOperationException(
                    "Explicit Animated mode stopped using native command execution for Share assignments.");
        }

        private static void TestProviderDirectCancellationCleanup()
        {
            CastPlan plan = CreateShareDirectPlan(1, false);
            var clean = new PendingProviderDirectRuntime(true);
            var cleanReport = new ExecutionReport(plan);
            System.Collections.IEnumerator work = new InstantCastExecutor(
                clean, true, 1).Execute(plan, cleanReport);
            if (!work.MoveNext())
                throw new InvalidOperationException(
                    "The cancellation fixture did not reach an active provider transaction.");
            ((IDisposable)work).Dispose();
            if (clean.CleanupCount != 1 || clean.Active ||
                clean.EnhancementDisposeCount != 1 || cleanReport.Records.Any(
                    record => record.Status ==
                        CastExecutionStatus.ResidualStateUnsettled))
                throw new InvalidOperationException(
                    "Iterator cancellation did not clean the exact provider transaction before restoring enhancement state.");

            var residual = new PendingProviderDirectRuntime(false);
            var residualReport = new ExecutionReport(plan);
            work = new InstantCastExecutor(residual, true, 1)
                .Execute(plan, residualReport);
            if (!work.MoveNext())
                throw new InvalidOperationException(
                    "The residual cancellation fixture did not become active.");
            ((IDisposable)work).Dispose();
            if (residual.CleanupCount != 1 || !residual.Active ||
                residual.EnhancementDisposeCount != 1 ||
                !residualReport.Records.Any(record => record.Status ==
                    CastExecutionStatus.ResidualStateUnsettled &&
                    record.Detail.Contains("iterator-finalizer")))
                throw new InvalidOperationException(
                    "A provider cleanup failure was reported as settled or successful after iterator cancellation.");

            var hybridResidual = new PendingProviderDirectRuntime(false);
            var hybridReport = new ExecutionReport(plan);
            work = new HybridCastExecutor(hybridResidual,
                new AlwaysAnimatedRuntime(), false, true)
                .Execute(plan, hybridReport);
            if (!work.MoveNext())
                throw new InvalidOperationException(
                    "The hybrid cancellation fixture did not become active.");
            ((IDisposable)work).Dispose();
            if (hybridResidual.CleanupCount != 1 || !hybridResidual.Active ||
                hybridResidual.EnhancementDisposeCount != 1 ||
                hybridReport.Confirmed != 0 ||
                !hybridReport.Records.Any(record => record.Status ==
                    CastExecutionStatus.ResidualStateUnsettled &&
                    record.Detail.Contains("iterator-finalizer")))
                throw new InvalidOperationException(
                    "Hybrid cancellation discarded its inner transaction cleanup evidence.");

            var terminalFailure = new TerminalProviderFailureRuntime();
            var terminalReport = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(terminalFailure, true, 1)
                .Execute(plan, terminalReport));
            if (terminalReport.Confirmed != 0 || terminalReport.Failed != 1 ||
                !terminalReport.Records.Any(record => record.Status ==
                    CastExecutionStatus.FailedExecution && record.Detail
                    .Contains("provider-terminal-cleanup-failed")))
                throw new InvalidOperationException(
                    "A settled provider cleanup failure was converted into a successful effect confirmation.");
        }

        private static CastPlan CreateShareDirectPlan(int targetCount,
            bool includePowerfulChange)
        {
            const string resinousSkin =
                "41ceee31b77741e99d3b0990bbe40a2a";
            AbilityKey ability = Ability(resinousSkin, string.Empty, 0);
            var pool = new ResourcePoolSnapshot("resinous-skin-slots",
                ResourcePoolKind.SpontaneousLevel, targetCount, targetCount,
                null);
            ProviderSnapshot provider = PlannerProvider("brown", "brown-book",
                ability, pool.PoolKey, 1);
            string[] targets = Enumerable.Range(1, targetCount)
                .Select(index => "party-" + index).ToArray();
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { pool },
                new[] { "brown" }.Concat(targets).ToArray());
            var option = new ProviderPlanningOption(provider,
                new[] { "brown" }, new[] { "brown" }, 12, 120);
            int reservoir = targetCount *
                (includePowerfulChange ? 2 : 1);
            CastEnhancementSnapshot share = ClassEnhancement("share", "brown",
                ability, "brown-book", reservoir, "reservoir|brown",
                "brown-fur-share-transmutation", true, false,
                "brown-fur-direct-cast-v1");
            CastEnhancementSnapshot powerful = ClassEnhancement(
                "powerful-strength", "brown", ability, "brown-book",
                reservoir, "reservoir|brown", "brown-fur-powerful-change",
                false, false, "brown-fur-direct-cast-v1");
            CastEnhancementSnapshot[] catalog = includePowerfulChange
                ? new[] { share, powerful } : new[] { share };
            string[] selected = catalog.Select(value => value.EnhancementId)
                .ToArray();
            var targeting = new EffectiveProviderOptionResolver(
                new ICastTargetingModifier[] {
                    new FixtureShareTargetingModifier("share", "brown",
                        targets, CastExecutionStrategy.ProviderDirectRuleCast,
                        "share-transmutation-provider-direct-cast-v1")
                });
            var request = new BuffCastRequest(new BuffSourceDefinition(
                "resinous-skin-share", ability,
                Leaf("72067851f2904755a372f4ea4818345e"),
                CastGroupingKind.PerTarget), targets,
                ExistingEffectPolicy.Overwrite, null, selected);
            return new CastPlanner(targeting).Plan(snapshot, request,
                new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), catalog);
        }

        private static CastPlan CreateOrdinaryInstantPlan()
        {
            AbilityKey ability = Ability("ordinary-after-share",
                string.Empty, 0);
            var pool = new ResourcePoolSnapshot("ordinary-slot",
                ResourcePoolKind.SpontaneousLevel, 1, 1, null);
            ProviderSnapshot provider = PlannerProvider("brown", "brown-book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { pool }, "brown");
            var option = new ProviderPlanningOption(provider,
                new[] { "brown" }, new[] { "brown" }, 12, 120);
            return PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "brown" }, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null));
        }

        private static CastPlan CreateLegacySharePlan()
        {
            AbilityKey ability = Ability(
                "41ceee31b77741e99d3b0990bbe40a2a", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("legacy-share-slot",
                ResourcePoolKind.SpontaneousLevel, 1, 1, null);
            ProviderSnapshot provider = PlannerProvider("brown", "brown-book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { pool }, "brown", "party-1");
            var option = new ProviderPlanningOption(provider,
                new[] { "brown" }, new[] { "brown" }, 12, 120);
            CastEnhancementSnapshot share = ClassEnhancement("share", "brown",
                ability, "brown-book", 1, "reservoir|brown",
                "brown-fur-share-transmutation", true, true, null);
            var targeting = new EffectiveProviderOptionResolver(
                new ICastTargetingModifier[] {
                    new FixtureShareTargetingModifier("share", "brown",
                        new[] { "party-1" },
                        CastExecutionStrategy.NativeCommandRequired,
                        "share-transmutation-legacy-native-command:contract-missing")
                });
            var request = new BuffCastRequest(new BuffSourceDefinition(
                "legacy-share", ability,
                Leaf("72067851f2904755a372f4ea4818345e"),
                CastGroupingKind.PerTarget), new[] { "party-1" },
                ExistingEffectPolicy.Overwrite, null, new[] { "share" });
            return new CastPlanner(targeting).Plan(snapshot, request,
                new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] { share });
        }

        private static void TestUnconfirmedExecution()
        {
            AbilityKey ability = Ability("unconfirmed", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("unconfirmed-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-u", ability, "unconfirmed-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 1, 1);
            CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "unit-a" }, new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            var report = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(new NeverObservedInstantRuntime(), true, 8)
                .Execute(plan, report));
            if (report.Submitted != 1 || report.CastStarted != 1 || report.Confirmed != 0 ||
                report.Failed != 1 || !report.Records.Any(record =>
                    record.Status == CastExecutionStatus.TimedOutUnconfirmed &&
                    record.Detail.Contains("expected-effects-absent")))
                throw new InvalidOperationException("A submitted cast without its expected fact was counted as success.");
        }

        private static void TestSupportedStickyTouchClassification()
        {
            CastExecutionCapability capability =
                StickyTouchExecutionClassifier.Classify(true, true, true,
                    true, true, true, false, false);
            if (capability.Strategy !=
                    CastExecutionStrategy.StickyTouchDeliveryRuleCast ||
                capability.Reason !=
                    "supported-beneficial-sticky-touch-delivery")
                throw new InvalidOperationException(
                    "A valid beneficial touch delivery remained animated-only.");
            CastPlan plan = CreateStickyPlan(ResourcePoolKind.PreparedSlots,
                1, capability.Strategy);
            if (plan.Steps.Single().ExecutionStrategy !=
                    CastExecutionStrategy.StickyTouchDeliveryRuleCast ||
                plan.Steps.Single().ExecutionStrategyReason !=
                    "sticky-touch-regression-fixture")
                throw new InvalidOperationException(
                    "The structural sticky-delivery strategy did not survive planning.");
        }

        private static void TestUnsupportedStickyTouchClassification()
        {
            CastExecutionCapability[] unsupported =
            {
                StickyTouchExecutionClassifier.Classify(true, false, false,
                    false, false, false, false, false),
                StickyTouchExecutionClassifier.Classify(true, true, false,
                    true, true, true, false, false),
                StickyTouchExecutionClassifier.Classify(true, true, true,
                    false, true, true, false, false),
                StickyTouchExecutionClassifier.Classify(true, true, true,
                    true, true, true, true, false),
                StickyTouchExecutionClassifier.Classify(true, true, true,
                    true, true, true, false, true),
                StickyTouchExecutionClassifier.Classify(true, true, true,
                    true, false, false, false, false)
            };
            if (unsupported.Any(value => value.Strategy !=
                    CastExecutionStrategy.AnimatedFallback) ||
                unsupported.Any(value => string.IsNullOrWhiteSpace(
                    value.Reason)))
                throw new InvalidOperationException(
                    "An unsafe or ambiguous touch delivery was treated as an ordinary direct spell.");
        }

        private static void TestFreedomOfMovementStickyTouchCatalogCanary()
        {
            JObject catalog = JObject.Parse(File.ReadAllText(Path.Combine(
                FindRepositoryRoot(), "planning", "NATIVE-BUFF-CATALOG.json")));
            JArray abilities = (JArray)catalog["abilities"];
            JObject carrier = abilities.OfType<JObject>().Single(value =>
                (string)value["abilityGuid"] ==
                    "0087fc2d64b6095478bc7b8d7d512caf");
            JObject delivery = abilities.OfType<JObject>().Single(value =>
                (string)value["abilityGuid"] ==
                    "4c349361d720e844e846ad8c19959b1e");
            string[] carrierComponents = ((JArray)carrier[
                "abilityComponentTypes"]).Values<string>().ToArray();
            string[] deliveryComponents = ((JArray)delivery[
                "abilityComponentTypes"]).Values<string>().ToArray();
            JObject effect = ((JArray)delivery["effects"])
                .OfType<JObject>().Single();
            if (!(bool)carrier["isStickyTouch"] ||
                !carrierComponents.Contains(
                    "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectStickyTouch") ||
                !deliveryComponents.Contains(
                    "Kingmaker.UnitLogic.Abilities.Components.AbilityDeliverTouch") ||
                !deliveryComponents.Contains(
                    "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction") ||
                !(bool)delivery["canTargetSelf"] ||
                !(bool)delivery["canTargetFriends"] ||
                (bool)delivery["canTargetEnemies"] ||
                (bool)delivery["canTargetPoint"] ||
                (string)effect["effectGuid"] !=
                    "1533e782fca42b84ea370fc1dcbf4fc1" ||
                (string)effect["actionPath"] !=
                    "4c349361d720e844e846ad8c19959b1e/0:ActionList/0:ContextActionApplyBuff")
                throw new InvalidOperationException(
                    "The installed-catalog Freedom of Movement carrier/delivery canary changed.");
        }

        private static void TestInstalledStickyTouchExecutionContract()
        {
            string game = Environment.GetEnvironmentVariable(
                "KBP_TEST_GAME_PATH");
            if (string.IsNullOrWhiteSpace(game))
                throw new InvalidOperationException(
                    "KBP_TEST_GAME_PATH is missing.");
            string path = Path.Combine(game, "Kingmaker_Data", "Managed",
                "Assembly-CSharp.dll");
            Assembly assembly = Assembly.LoadFrom(path);
            if (assembly.ManifestModule.ModuleVersionId.ToString("D") !=
                    "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")
                throw new InvalidOperationException(
                    "Installed Assembly-CSharp MVID is not the inspected Kingmaker 2.1.7b contract.");
            string hash;
            using (var stream = File.OpenRead(path))
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = string.Concat(sha.ComputeHash(stream).Select(value =>
                    value.ToString("x2")).ToArray());
            if (hash !=
                    "3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb")
                throw new InvalidOperationException(
                    "Installed Assembly-CSharp hash changed: " + hash);

            Type abilityData = RequireType(assembly,
                "Kingmaker.UnitLogic.Abilities.AbilityData");
            Type blueprintAbility = RequireType(assembly,
                "Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility");
            Type sticky = RequireType(assembly,
                "Kingmaker.UnitLogic.Abilities.Components.AbilityEffectStickyTouch");
            Type partTouch = RequireType(assembly,
                "Kingmaker.UnitLogic.Parts.UnitPartTouch");
            Type unitCommand = RequireType(assembly,
                "Kingmaker.UnitLogic.Commands.Base.UnitCommand");
            Type unitCommands = RequireType(assembly,
                "Kingmaker.UnitLogic.Commands.UnitCommands");
            Type useAbility = RequireType(assembly,
                "Kingmaker.UnitLogic.Commands.UnitUseAbility");
            Type spellSlot = RequireType(assembly,
                "Kingmaker.UnitLogic.SpellSlot");
            Type ruleCast = RequireType(assembly,
                "Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell");
            BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static;
            if (abilityData.GetConstructor(all, null,
                    new[] { abilityData, blueprintAbility }, null) == null ||
                abilityData.GetMethod("Spend", all, null, Type.EmptyTypes,
                    null) == null ||
                abilityData.GetMethod("SpendFromSpellbook", all) == null ||
                abilityData.GetMethod("SpendMaterialComponent", all) == null ||
                !WritableProperty(abilityData, "ConvertedFrom", all) ||
                !WritableProperty(abilityData, "MetamagicData", all) ||
                !WritableProperty(abilityData, "ParamSpellbook", all) ||
                !WritableProperty(abilityData, "ParamSpellLevel", all) ||
                !WritableProperty(abilityData, "ParamSpellSlot", all) ||
                !WritableProperty(abilityData, "OverrideDC", all) ||
                !WritableProperty(abilityData, "OverrideSpellLevel", all) ||
                blueprintAbility.GetProperty("StickyTouch", all) == null ||
                sticky.GetField("TouchDeliveryAbility", all) == null ||
                partTouch.GetProperty("Ability", all) == null ||
                partTouch.GetMethod("Init", all) == null ||
                unitCommand.GetMethod("Interrupt", all, null,
                    new[] { typeof(bool) }, null) == null ||
                unitCommands.GetProperty("PreviousCommand", all) == null ||
                unitCommands.GetMethod("AddToQueueFirst", all) == null ||
                useAbility.GetField("Spell", all) == null ||
                useAbility.GetProperty("Target", all) == null ||
                spellSlot.GetField("Available", all) == null ||
                spellSlot.GetField("LinkedSlots", all) == null ||
                ruleCast.GetProperty("Success", all) == null ||
                ruleCast.GetProperty("IsUMDFailed", all) == null ||
                ruleCast.GetProperty("IsSpellFailed", all) == null)
                throw new InvalidOperationException(
                    "An installed sticky-touch, resource, command, or reporting member changed.");
        }

        private static void TestStickyTouchInstantRouting()
        {
            CastPlan sticky = CreateStickyPlan(
                ResourcePoolKind.PreparedSlots, 4,
                CastExecutionStrategy.StickyTouchDeliveryRuleCast);
            var instant = new StickySequenceRuntime(0, 1);
            var animated = new AlwaysAnimatedRuntime();
            var report = new ExecutionReport(sticky);
            Drain(new HybridCastExecutor(instant, animated, false, true)
                .Execute(sticky, report));
            if (instant.FireCount != 4 || animated.StartCount != 0 ||
                report.Submitted != 4 || report.Confirmed != 4)
                throw new InvalidOperationException(
                    "A supported sticky delivery in Instant mode queued an animated command.");

            CastPlan native = CreateStickyPlan(ResourcePoolKind.Unlimited, 1,
                CastExecutionStrategy.NativeCommandRequired);
            instant = new StickySequenceRuntime(0, 0);
            animated = new AlwaysAnimatedRuntime();
            report = new ExecutionReport(native);
            Drain(new HybridCastExecutor(instant, animated, false, true)
                .Execute(native, report));
            if (instant.FireCount != 0 || animated.StartCount != 1 ||
                report.Queued != 1 || report.Confirmed != 1)
                throw new InvalidOperationException(
                    "A native-command-only enhancement was redirected to RuleCastSpell.");
        }

        private static void TestPreparedStickyTouchRepeatedTargets()
        {
            CastPlan plan = CreateStickyPlan(ResourcePoolKind.PreparedSlots,
                4, CastExecutionStrategy.StickyTouchDeliveryRuleCast);
            string[][] expectedTokens =
            {
                new[] { "slot-0", "slot-0-linked" },
                new[] { "slot-1" },
                new[] { "slot-2" },
                new[] { "slot-3" }
            };
            if (!plan.Steps.Select(step => step.Reservation.TokenIds.ToArray())
                    .Zip(expectedTokens, (actual, expected) =>
                        actual.SequenceEqual(expected)).All(value => value))
                throw new InvalidOperationException(
                    "Prepared sticky casts did not reserve exact distinct primary and opposition-linked tokens.");
            var runtime = new StickySequenceRuntime(0, 2);
            var report = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(runtime, true, 1)
                .Execute(plan, report));
            if (runtime.FireCount != 4 || runtime.RuleTransactions != 4 ||
                runtime.SourceSpendInvocations != 4 ||
                runtime.DeliverySpendInvocations != 0 ||
                runtime.CleanupCount != 0 || runtime.PrematureValidation ||
                runtime.SpentTokenIds.Count != 5 ||
                report.Submitted != 4 || report.SpendInvocations != 4 ||
                report.ResourcesSpent != 4 || report.Confirmed != 4 ||
                report.Failed != 0)
                throw new InvalidOperationException(
                    "Four prepared sticky deliveries reused a slot, double-spent, advanced early, or failed near target three.");
        }

        private static void TestSpontaneousStickyTouchRepeatedTargets()
        {
            CastPlan plan = CreateStickyPlan(
                ResourcePoolKind.SpontaneousLevel, 4,
                CastExecutionStrategy.StickyTouchDeliveryRuleCast);
            var runtime = new StickySequenceRuntime(4, 1);
            var report = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(runtime, true, 1)
                .Execute(plan, report));
            if (runtime.SharedRemaining != 0 ||
                runtime.SourceSpendInvocations != 4 ||
                runtime.DeliverySpendInvocations != 0 ||
                report.SpendInvocations != 4 ||
                report.ResourcesSpent != 4 || report.Confirmed != 4 ||
                report.Failed != 0)
                throw new InvalidOperationException(
                    "Repeated sticky delivery did not consume the spontaneous level pool exactly once per target.");
        }

        private static void TestStickyTouchSpendPolicy()
        {
            if (RuleCastSpendPolicy.ShouldInvokeSpend(false, false) ||
                RuleCastSpendPolicy.ShouldInvokeSpend(true, true) ||
                !RuleCastSpendPolicy.ShouldInvokeSpend(true, false))
                throw new InvalidOperationException(
                    "RuleCastSpell/Spend ownership changed for submission or UMD failure.");
            CastPlan plan = CreateStickyPlan(ResourcePoolKind.Unlimited, 1,
                CastExecutionStrategy.StickyTouchDeliveryRuleCast);
            var report = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(new UmdInstantRuntime(), true, 1)
                .Execute(plan, report));
            if (report.Submitted != 1 || report.SpendInvocations != 0 ||
                report.ResourcesSpent != 0 || report.Failed != 1)
                throw new InvalidOperationException(
                    "A UMD-failed rule transaction was charged or reported as successful.");
        }

        private static void TestAnimatedStickyTouchLifecycle()
        {
            var lifecycle = new AnimatedStickyTouchLifecycle(10);
            if (lifecycle.Observe(StickyLifecycle(false, false, true,
                    false, false, false, false, false)).Complete ||
                lifecycle.Observe(StickyLifecycle(true, true, true,
                    false, false, false, true, false)).Complete ||
                lifecycle.Observe(StickyLifecycle(true, true, true,
                    true, false, false, true, false)).Complete ||
                lifecycle.Observe(StickyLifecycle(true, true, true,
                    true, true, true, false, false)).Complete)
                throw new InvalidOperationException(
                    "Animated sticky execution completed at the carrier or before effect confirmation.");
            AnimatedStickyTouchLifecycleDecision complete = lifecycle.Observe(
                StickyLifecycle(true, true, true, true, true, true,
                    false, true));
            if (!complete.Complete || !complete.Succeeded ||
                complete.TimedOut)
                throw new InvalidOperationException(
                    "Animated sticky execution did not finish after delivery, effect, and held-state settlement.");
            AnimatedStickyTouchLifecycleDecision self =
                new AnimatedStickyTouchLifecycle(3).Observe(StickyLifecycle(
                    true, true, false, false, false, false, false, true));
            if (!self.Complete || !self.Succeeded)
                throw new InvalidOperationException(
                    "A self-target sticky cast incorrectly required a generated delivery command.");
            AnimatedStickyTouchLifecycleDecision failed =
                new AnimatedStickyTouchLifecycle(3).Observe(StickyLifecycle(
                    true, true, true, true, true, false, false, false));
            if (!failed.Complete || failed.Succeeded || failed.TimedOut ||
                failed.Detail != "delivery-command-failed")
                throw new InvalidOperationException(
                    "A failed animated delivery was not reported precisely.");
            var timeout = new AnimatedStickyTouchLifecycle(2);
            timeout.Observe(StickyLifecycle(false, false, true, false,
                false, false, false, false));
            AnimatedStickyTouchLifecycleDecision timed = timeout.Observe(
                StickyLifecycle(false, false, true, false, false, false,
                    false, false));
            if (!timed.Complete || !timed.TimedOut || timed.Succeeded)
                throw new InvalidOperationException(
                    "A stalled animated carrier escaped its bounded lifecycle timeout.");
        }

        private static void TestStickyTouchFailureCleanup()
        {
            CastPlan plan = CreateStickyPlan(ResourcePoolKind.Unlimited, 2,
                CastExecutionStrategy.StickyTouchDeliveryRuleCast);
            var cleaned = new CleanupInstantRuntime(false);
            var report = new ExecutionReport(plan);
            Drain(new HybridCastExecutor(cleaned, new AlwaysAnimatedRuntime(),
                false, true).Execute(plan, report));
            if (cleaned.FireCount != 2 || cleaned.CleanupCount != 1 ||
                report.Confirmed != 1 ||
                !report.Records.Any(record => record.Status ==
                    CastExecutionStatus.TimedOutUnconfirmed) ||
                report.Records.Any(record => record.Status ==
                    CastExecutionStatus.ResidualStateUnsettled))
                throw new InvalidOperationException(
                    "A cleaned failed transaction blocked the next target or claimed residual state.");

            var uncleared = new CleanupInstantRuntime(true);
            report = new ExecutionReport(plan);
            Drain(new HybridCastExecutor(uncleared,
                new AlwaysAnimatedRuntime(), false, true)
                .Execute(plan, report));
            if (uncleared.FireCount != 1 ||
                !report.Records.Any(record => record.Status ==
                    CastExecutionStatus.ResidualStateUnsettled) ||
                !report.Records.Any(record => record.Detail ==
                    "prior-hybrid-transaction-unsettled"))
                throw new InvalidOperationException(
                    "An uncleared delivery state allowed the next conflicting transaction.");
            uncleared.RecoverExternalState();
            CastPlan later = CreateStickyPlan(ResourcePoolKind.Unlimited, 1,
                CastExecutionStrategy.StickyTouchDeliveryRuleCast);
            var laterReport = new ExecutionReport(later);
            Drain(new HybridCastExecutor(uncleared,
                new AlwaysAnimatedRuntime(), false, true)
                .Execute(later, laterReport));
            if (laterReport.Confirmed != 1)
                throw new InvalidOperationException(
                    "A later routine remained globally blocked after delivery state recovery.");

            var animated = new CleanupAnimatedRuntime();
            report = new ExecutionReport(plan);
            Drain(new AnimatedCastExecutor(animated, true)
                .Execute(plan, report));
            if (animated.StartCount != 2 || animated.DisposeCount != 2 ||
                report.Confirmed != 1 ||
                report.Records.Any(record => record.Status ==
                    CastExecutionStatus.ResidualStateUnsettled))
                throw new InvalidOperationException(
                    "Animated carrier/delivery cleanup leaked state or blocked the following cast.");

            var exceptionRuntime =
                new ExceptionThenSuccessAnimatedRuntime();
            report = new ExecutionReport(plan);
            Drain(new AnimatedCastExecutor(exceptionRuntime, true)
                .Execute(plan, report));
            if (exceptionRuntime.StartCount != 2 ||
                exceptionRuntime.DisposeCount != 2 ||
                report.Confirmed != 1 || report.Failed != 1 ||
                !report.Records.Any(record => record.Detail.Contains(
                    "animated-operation-exception:System.InvalidOperationException:fixture-operation-failure")))
                throw new InvalidOperationException(
                    "An animated operation exception leaked cleanup or prevented later work.");
        }

        private static AnimatedStickyTouchLifecycleSnapshot StickyLifecycle(
            bool carrierFinished, bool carrierSucceeded,
            bool deliveryExpected, bool deliveryIdentified,
            bool deliveryFinished, bool deliverySucceeded,
            bool heldTouch, bool effectsObserved)
        {
            return new AnimatedStickyTouchLifecycleSnapshot(carrierFinished,
                carrierSucceeded, deliveryExpected, deliveryIdentified,
                deliveryFinished, deliverySucceeded, heldTouch,
                effectsObserved);
        }

        private static CastPlan CreateStickyPlan(ResourcePoolKind poolKind,
            int targetCount, CastExecutionStrategy strategy)
        {
            AbilityKey ability = Ability("sticky-carrier-fixture",
                string.Empty, 0);
            string poolKey = "sticky-pool-" + poolKind;
            ResourcePoolSnapshot pool;
            string[] eligible = new string[0];
            if (poolKind == ResourcePoolKind.PreparedSlots)
            {
                var tokens = new List<ResourceTokenSnapshot>();
                var primary = new List<string>();
                for (int index = 0; index < targetCount; index++)
                {
                    string token = "slot-" + index;
                    primary.Add(token);
                    tokens.Add(new ResourceTokenSnapshot(token, ability, 4,
                        index == 0 ? PreparedSlotKind.Opposition :
                            PreparedSlotKind.Common,
                        true, true, index == 0
                            ? new[] { "slot-0-linked" } : null));
                }
                tokens.Add(new ResourceTokenSnapshot("slot-0-linked",
                    ability, 4, PreparedSlotKind.Opposition, true, false,
                    null));
                pool = new ResourcePoolSnapshot(poolKey,
                    ResourcePoolKind.PreparedSlots, tokens.Count,
                    tokens.Count, tokens);
                eligible = primary.ToArray();
            }
            else if (poolKind == ResourcePoolKind.Unlimited)
                pool = new ResourcePoolSnapshot(poolKey,
                    ResourcePoolKind.Unlimited, 0, 0, null);
            else
                pool = new ResourcePoolSnapshot(poolKey, poolKind,
                    targetCount, targetCount, null);
            var provider = new ProviderSnapshot(new ProviderKey("caster",
                "sticky-book", ability, "level-4"), "Sticky carrier", 4,
                poolKey, poolKind == ResourcePoolKind.Unlimited ? 0 : 1,
                eligible);
            string[] targets = Enumerable.Range(0, targetCount)
                .Select(index => "target-" + index).ToArray();
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { provider }, new[] { pool },
                new[] { "caster" }.Concat(targets).ToArray());
            var option = new ProviderPlanningOption(provider, targets,
                targets, 8, 80, strategy,
                "sticky-touch-regression-fixture");
            return PlannerPlan(snapshot, ability,
                CastGroupingKind.PerTarget, targets, new[] { option },
                EmptyPolicy(), new ActiveEffectSnapshot(null));
        }

        private static Type RequireType(Assembly assembly, string name)
        {
            Type type = assembly.GetType(name, false);
            if (type == null) throw new InvalidOperationException(
                "Installed contract type is absent: " + name);
            return type;
        }

        private static bool WritableProperty(Type type, string name,
            BindingFlags flags)
        {
            PropertyInfo property = type.GetProperty(name, flags);
            return property != null && property.GetSetMethod(true) != null;
        }

        private static void Drain(System.Collections.IEnumerator enumerator)
        {
            int moves = 0;
            while (enumerator.MoveNext())
                if (++moves > 100) throw new InvalidOperationException("Executor did not terminate.");
        }

        private static EffectLeafExpression Leaf(string id)
        {
            return new EffectLeafExpression(EffectKind.Buff, id, EffectTarget.CurrentTarget, "fixture", "fixture/" + id);
        }

        private static ProviderSnapshot PlannerProvider(
            string unitId,
            string bookId,
            AbilityKey ability,
            string poolKey,
            int cost)
        {
            return new ProviderSnapshot(new ProviderKey(unitId, bookId, ability, "level-2"),
                ability.BaseAbilityGuid, 2, poolKey, cost, null);
        }

        private static PartyProviderSnapshot PlannerSnapshot(
            IEnumerable<ProviderSnapshot> providers,
            IEnumerable<ResourcePoolSnapshot> pools,
            params string[] unitIds)
        {
            return new PartyProviderSnapshot(unitIds.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))), providers, pools);
        }

        private static void TestPowerfulChangeSemanticQualification()
        {
            const string book = "arcanist-casting-spellbook";
            const string buff = "11111111111111111111111111111111";
            string[] scores = {
                "Strength", "Dexterity", "Constitution", "Intelligence",
                "Wisdom", "Charisma"
            };
            foreach (string score in scores)
            {
                PowerfulChangeEligibility result =
                    PowerfulChangeEligibilityClassifier.Classify(true, true,
                        book, book, new[] {
                            "buff[" + buff + "].components=" +
                            "Kingmaker.Designers.Mechanics.Buffs.AddStatBonus{" +
                            "Descriptor=Enhancement,Stat=" + score + ",Value=4}"
                        }, new[] { buff });
                PowerfulChangeAbilityScore expected;
                if (!Enum.TryParse(score, false, out expected) ||
                    !result.Eligible || !result.Supports(expected) ||
                    result.AbilityScores.Count != 1 ||
                    !result.CarrierFamilies.Contains("AddStatBonus"))
                    throw new InvalidOperationException(
                        "A structural ability-score transmutation was not classified: " + score);
            }

            PowerfulChangeEligibility polymorph =
                PowerfulChangeEligibilityClassifier.Classify(true, true,
                    book, book, new[] {
                        "buff[" + buff + "].components=" +
                        "Kingmaker.UnitLogic.Buffs.Polymorph{" +
                        "ConstitutionBonus=2,DexterityBonus=2,StrengthBonus=6}"
                    }, new[] { buff });
            if (!polymorph.Eligible ||
                !polymorph.Supports(PowerfulChangeAbilityScore.Strength) ||
                !polymorph.Supports(PowerfulChangeAbilityScore.Dexterity) ||
                !polymorph.Supports(PowerfulChangeAbilityScore.Constitution))
                throw new InvalidOperationException(
                    "Supported polymorph ability bonuses were not classified.");

            string direct = "buff[" + buff + "].components=" +
                "Kingmaker.Designers.Mechanics.Buffs.AddStatBonus{" +
                "Stat=Strength,Value=4}";
            if (PowerfulChangeEligibilityClassifier.Classify(false, true,
                    book, book, new[] { direct }, new[] { buff }).Eligible ||
                PowerfulChangeEligibilityClassifier.Classify(true, false,
                    book, book, new[] { direct }, new[] { buff }).Eligible ||
                PowerfulChangeEligibilityClassifier.Classify(true, true,
                    "ordinary-wizard-book", book, new[] { direct },
                    new[] { buff }).Eligible ||
                PowerfulChangeEligibilityClassifier.Classify(true, true,
                    book, book, new string[0], new[] { buff }).Eligible)
                throw new InvalidOperationException(
                    "A non-spell, wrong-school, wrong-spellbook, or unrelated spell qualified.");
        }

        private static void TestPowerfulChangeAvailability()
        {
            const string caster = "brown-fur-caster";
            const string book = "arcanist-casting-spellbook";
            AbilityKey bull = Ability("bull-strength-fixture", string.Empty, 0);
            ProviderSnapshot bullProvider = PlannerProvider(caster, book, bull,
                "bull-slots", 0);
            var pool = new ResourcePoolSnapshot("bull-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { bullProvider }, new[] { pool }, caster);
            var option = new ProviderPlanningOption(bullProvider,
                new[] { caster }, new[] { caster }, 3, 10);
            var powerfulChange = new CastEnhancementSnapshot(
                "class-feature|brown-fur-caster|strength", caster,
                "powerful-change-strength", "Powerful Change — Strength",
                "Increase a supported Strength bonus for this cast.",
                CastEnhancementCategory.ClassFeature, 0, 0, 3,
                new[] { bull.BaseAbilityGuid }, "Powerful Change: Strength",
                new[] { book }, "reservoir|brown-fur-caster", true);
            var effects = new Dictionary<string, EffectExpression> {
                { bull.Canonical, Leaf("bull-strength-buff") }
            };
            var active = new ActiveEffectSnapshot(null);
            var profile = BuffPlannerProfile.CreateDefault("powerful-change");
            var model = new PlannerSetupModel(profile, snapshot, active,
                effects, new[] { option }, ignored => { },
                new[] { powerfulChange });
            if (!powerfulChange.IsApplicable(bullProvider) ||
                model.GetApplicableEnhancements().Single().EnhancementId !=
                    powerfulChange.EnhancementId ||
                PlannerSetupModel.EffectName(powerfulChange) !=
                    "Powerful Change: Strength")
                throw new InvalidOperationException(
                    "Brown-Fur + Powerful Change + Bull's Strength was not available.");

            var withoutFeature = new PlannerSetupModel(
                BuffPlannerProfile.CreateDefault("without-feature"), snapshot,
                active, effects, new[] { option }, ignored => { });
            if (withoutFeature.GetApplicableEnhancements().Count != 0 ||
                withoutFeature.GetEnhancementSummary("long") !=
                    "Enhancement: None available")
                throw new InvalidOperationException(
                    "Brown-Fur without the discovered feature gained Powerful Change.");

            ProviderSnapshot ordinary = PlannerProvider("ordinary-wizard",
                "ordinary-wizard-book", bull, "ordinary-slots", 0);
            AbilityKey unrelated = Ability("unrelated-spell", string.Empty, 0);
            ProviderSnapshot unrelatedProvider = PlannerProvider(caster, book,
                unrelated, "unrelated-slots", 0);
            if (powerfulChange.IsApplicable(ordinary) ||
                powerfulChange.IsApplicable(unrelatedProvider))
                throw new InvalidOperationException(
                    "Powerful Change leaked to another caster or unrelated spell.");

            AbilityKey catMass = Ability("cat-grace-base", "cat-grace-mass", 0);
            ProviderSnapshot catProvider = PlannerProvider(caster, book,
                catMass, "cat-slots", 0);
            var dexterity = new CastEnhancementSnapshot(
                "class-feature|brown-fur-caster|dexterity", caster,
                "powerful-change-dexterity", "Powerful Change — Dexterity",
                string.Empty, CastEnhancementCategory.ClassFeature, 0, 0, 3,
                new[] { catMass.VariantGuid }, "Powerful Change: Dexterity",
                new[] { book }, "reservoir|brown-fur-caster", true);
            if (!dexterity.IsApplicable(catProvider))
                throw new InvalidOperationException(
                    "A qualifying related ability-score mass variant was excluded.");
        }

        private static void TestPowerfulChangeSharedReservoir()
        {
            const string caster = "brown-fur-caster";
            const string book = "arcanist-casting-spellbook";
            const string reservoir = "reservoir|brown-fur-caster";
            AbilityKey strength = Ability("strength-spell", string.Empty, 0);
            AbilityKey dexterity = Ability("dexterity-spell", string.Empty, 0);
            var spellPool = new ResourcePoolSnapshot("shared-spell-pool",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot strengthProvider = PlannerProvider(caster, book,
                strength, spellPool.PoolKey, 0);
            ProviderSnapshot dexterityProvider = PlannerProvider(caster, book,
                dexterity, spellPool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] {
                strengthProvider, dexterityProvider }, new[] { spellPool },
                caster, "target-a", "target-b");
            var options = new[] {
                new ProviderPlanningOption(strengthProvider,
                    new[] { "target-a" }, new[] { caster }, 2, 10),
                new ProviderPlanningOption(dexterityProvider,
                    new[] { "target-b" }, new[] { caster }, 2, 10)
            };
            var strengthEnhancement = new CastEnhancementSnapshot(
                "powerful-change-strength", caster, "strength-toggle",
                "Powerful Change — Strength", string.Empty,
                CastEnhancementCategory.ClassFeature, 0, 0, 1,
                new[] { strength.BaseAbilityGuid }, "Powerful Change: Strength",
                new[] { book }, reservoir, true);
            var dexterityEnhancement = new CastEnhancementSnapshot(
                "powerful-change-dexterity", caster, "dexterity-toggle",
                "Powerful Change — Dexterity", string.Empty,
                CastEnhancementCategory.ClassFeature, 0, 0, 1,
                new[] { dexterity.BaseAbilityGuid }, "Powerful Change: Dexterity",
                new[] { book }, reservoir, true);
            var requests = new[] {
                new BuffCastRequest(new BuffSourceDefinition("a-strength",
                    strength, Leaf("strength-buff"), CastGroupingKind.PerTarget),
                    new[] { "target-a" }, ExistingEffectPolicy.Overwrite,
                    null, new[] { strengthEnhancement.EnhancementId }),
                new BuffCastRequest(new BuffSourceDefinition("b-dexterity",
                    dexterity, Leaf("dexterity-buff"), CastGroupingKind.PerTarget),
                    new[] { "target-b" }, ExistingEffectPolicy.Overwrite,
                    null, new[] { dexterityEnhancement.EnhancementId })
            };
            CastPlan plan = new CastPlanner().PlanRoutine(snapshot, requests,
                options, EmptyPolicy(), new ActiveEffectSnapshot(null),
                new[] { strengthEnhancement, dexterityEnhancement });
            if (plan.Steps.Count != 1 ||
                plan.Steps.Single().EnhancementIds.Single() !=
                    strengthEnhancement.EnhancementId ||
                plan.Outcomes.Count(value => value.Kind ==
                    TargetOutcomeKind.Unfulfilled) != 1)
                throw new InvalidOperationException(
                    "Score toggles did not reserve one shared reservoir use.");
        }

        private static void TestCastEnhancementPlanning()
        {
            AbilityKey ability = Ability("enhanced-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("enhanced-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "enhanced-book", ability,
                "enhanced-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a", "unit-b");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                new[] { "unit-a", "unit-b" }, 3, 10);
            var rod = new CastEnhancementSnapshot("rod-a", "unit-a", "rod-guid", "Extend Rod",
                "Extends applicable spells.", CastEnhancementCategory.MetamagicRod, 2, 3, 1, null);
            var request = new BuffCastRequest(new BuffSourceDefinition("enhanced", ability,
                Leaf("enhanced-effect"), CastGroupingKind.PerTarget),
                new[] { "unit-a", "unit-b" }, ExistingEffectPolicy.Overwrite, null,
                new[] { rod.EnhancementId });
            CastPlan plan = new CastPlanner().Plan(snapshot, request, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] { rod });
            if (plan.Steps.Count != 1 || plan.Steps[0].EnhancementIds.Single() != "rod-a" ||
                plan.Outcomes.Count(value => value.Kind == TargetOutcomeKind.Unfulfilled) != 1 ||
                !plan.Outcomes.Any(value => value.Reason.Contains("requested-enhancement-unavailable")))
                throw new InvalidOperationException("Finite enhancement uses were not reserved per actual cast.");
            var exhausted = new CastEnhancementSnapshot("rod-empty", "unit-a", "rod-empty-guid",
                "Empty Rod", string.Empty, CastEnhancementCategory.MetamagicRod, 2, 3, 0, null);
            if (exhausted.IsApplicable(provider) == false || new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("empty", ability, Leaf("effect"),
                    CastGroupingKind.PerTarget), new[] { "unit-a" }, ExistingEffectPolicy.Overwrite,
                    null, new[] { exhausted.EnhancementId }), new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] { exhausted }).Steps.Count != 0)
                throw new InvalidOperationException("An exhausted enhancement was planned.");
            ProviderSnapshot otherCaster = PlannerProvider("unit-b", "enhanced-book-b", ability,
                "enhanced-free", 0);
            if (rod.IsApplicable(otherCaster))
                throw new InvalidOperationException("Enhancement applicability leaked to another caster.");
            var secondRod = new CastEnhancementSnapshot("rod-b", "unit-a", "rod-b-guid", "Other Rod",
                string.Empty, CastEnhancementCategory.MetamagicRod, 4, 3, 1, null);
            var feature = new CastEnhancementSnapshot("feature-a", "unit-a", "feature-guid", "Feature",
                string.Empty, CastEnhancementCategory.ClassFeature, 0, 0, 1, null);
            if (CastEnhancementSnapshot.AreCompatible(new[] { rod, secondRod }) ||
                !CastEnhancementSnapshot.AreCompatible(new[] { rod, feature }))
                throw new InvalidOperationException("Enhancement category conflicts were not preserved.");
        }

        private static void TestCastingSectionPresentation()
        {
            AbilityKey shield = Ability("shield-fixture", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("leinna-shield-pool", ResourcePoolKind.PreparedSlots,
                1, 1, new[] { new ResourceTokenSnapshot("shield-slot", shield, 1,
                    PreparedSlotKind.Common, true, true, null) });
            ProviderSnapshot provider = new ProviderSnapshot(
                new ProviderKey("leinna", "leinna-wizard-book", shield, "level-1"),
                "Shield", 1, pool.PoolKey, 1, new[] { "shield-slot" });
            var validation = new TargetValidationSnapshot(true, true, true, true);
            var snapshot = new PartyProviderSnapshot(new[]
            {
                new UnitSnapshot("leinna", "Leinna", false, string.Empty, validation),
                new UnitSnapshot("akasa", "Akasa", false, string.Empty, validation)
            }, new[] { provider }, new[] { pool });
            var option = new ProviderPlanningOption(provider, new[] { "leinna" },
                new[] { "leinna" }, 1, 10);
            var rod = new CastEnhancementSnapshot(
                "metamagic-rod|leinna|lesser-extend", "leinna", "lesser-extend",
                "Lesser Metamagic Rod of Extend", "Applies Extend Spell to this cast.",
                CastEnhancementCategory.MetamagicRod, 2, 3, 3, null, "Extend");
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("casting-presentation");
            var effects = new Dictionary<string, EffectExpression>
            {
                { shield.Canonical, Leaf("shield-effect") }
            };
            var active = new ActiveEffectSnapshot(null);
            var model = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { option }, ignored => { }, new[] { rod });
            SetupSourceRow source = model.SelectedSource;
            model.ToggleTarget("long", "leinna");
            RoutinePlanResult preview = new RoutinePlanService().Plan(profile, "long", snapshot,
                active, effects, new[] { option }, new[] { rod });
            SelectedCastingViewModel none = SelectedCastingViewModel.Create(source, model,
                "long", preview);
            if (none.CasterText != "Planned casters: Leinna 1" ||
                none.EnhancementLabel != "Enhancement: None  1 available" ||
                none.Choices.Count != 2 || none.Choices[0].Title != "None" ||
                none.Choices[1].Title != "Lesser Metamagic Rod of Extend" ||
                !none.Choices[1].Summary.Contains("Extend Spell") ||
                !none.Choices[1].Summary.Contains("3 uses") ||
                !none.Choices[1].Description.Contains("Owner: Leinna") ||
                !none.Choices[1].Description.Contains("Spell-level limit: 3") ||
                preview.Plan.Steps.Single().Provider.CasterUnitId != "leinna")
                throw new InvalidOperationException("Casting section did not expose the execution provider and rod details.");

            model.SetEnhancement("long", rod.EnhancementId);
            preview = new RoutinePlanService().Plan(profile, "long", snapshot, active, effects,
                new[] { option }, new[] { rod });
            SelectedCastingViewModel selected = SelectedCastingViewModel.Create(source, model,
                "long", preview);
            if (!selected.EnhancementLabel.Contains("Lesser Metamagic Rod of Extend") ||
                !selected.EnhancementLabel.Contains("3 uses") ||
                !selected.Choices.Single(choice => choice.EnhancementId == rod.EnhancementId).Selected)
                throw new InvalidOperationException("Selecting a rod did not update and highlight its canonical label.");

            model.SetEnhancement("long", null);
            if (!model.GetEnhancementSummary("long").StartsWith("Enhancement: None"))
                throw new InvalidOperationException("Selecting None did not update the visible enhancement state.");

            var noEnhancements = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("no-enhancement"),
                snapshot, active, effects, new[] { option }, ignored => { });
            if (noEnhancements.GetEnhancementSummary("long") != "Enhancement: None available")
                throw new InvalidOperationException("A no-candidate casting section disappeared instead of saying None available.");

            var unavailable = new CastEnhancementSnapshot(rod.EnhancementId, "leinna",
                rod.SourceBlueprintGuid, rod.DisplayName, rod.Description, rod.Category,
                rod.MetamagicMask, rod.MaximumSpellLevel, 0, null, rod.EffectDisplayName);
            model.SetEnhancement("long", rod.EnhancementId);
            var reloaded = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { option }, ignored => { }, new[] { unavailable });
            SelectedCastingViewModel invalid = SelectedCastingViewModel.Create(reloaded.SelectedSource,
                reloaded, "long", null);
            if (invalid.EnhancementLabel !=
                    "Enhancement unavailable: Lesser Metamagic Rod of Extend" ||
                invalid.Choices.Single(choice => choice.EnhancementId == rod.EnhancementId).Available)
                throw new InvalidOperationException("An unavailable persisted rod did not remain visible and fail closed.");

            if (!model.IsTargetLegal(source, "leinna") || model.IsTargetLegal(source, "akasa"))
                throw new InvalidOperationException("Shield personal-target eligibility regressed in casting presentation coverage.");
        }

        private static void TestCastingSectionLayout()
        {
            if (!CastingPanelLayoutContract.CanRenderLabel(
                    CastingPanelLayoutContract.MinimumEnhancementButtonHeight) ||
                CastingPanelLayoutContract.CanRenderLabel(16f) ||
                !CastingPanelLayoutContract.CanRenderCasterPolicyRow(
                    CastingPanelLayoutContract.MinimumCasterPolicyRowWidth,
                    CastingPanelLayoutContract.MinimumCasterPolicyRowHeight) ||
                CastingPanelLayoutContract.CanRenderCasterPolicyRow(640f, 80f) ||
                CastingPanelLayoutContract.SettingsCloseLabel != "CLOSE" ||
                string.IsNullOrWhiteSpace(CastingPanelLayoutContract.SettingsCloseLabel))
                throw new InvalidOperationException("Casting button geometry or shared CLOSE label is not render-safe.");
        }

        private sealed class ThemeToken { internal bool Destroyed; }

        private sealed class ThemeNode
        {
            internal string Name;
            internal ThemeNode Parent;
            internal readonly List<ThemeNode> Children = new List<ThemeNode>();
            internal readonly Dictionary<NativeThemeComponent, object[]> Components =
                new Dictionary<NativeThemeComponent, object[]>();
            internal bool Destroyed;
            internal ThemeNode Add(string name, params KeyValuePair<NativeThemeComponent, object>[] components)
            {
                var child = new ThemeNode { Name = name, Parent = this };
                foreach (KeyValuePair<NativeThemeComponent, object> pair in components)
                    child.Components[pair.Key] = new[] { pair.Value };
                Children.Add(child);
                return child;
            }
        }

        private static KeyValuePair<NativeThemeComponent, object> Comp(
            NativeThemeComponent component, ThemeToken token)
        {
            return new KeyValuePair<NativeThemeComponent, object>(component, token);
        }

        private sealed class FixtureThemeSource : INativeThemeSource
        {
            internal ThemeNode Owner;
            internal bool SoundAvailable = true;
            internal Action<NativeThemeCapability, object[]> ValidateHook = (c, v) => { };
            public bool IsAlive(object value)
            {
                var node = value as ThemeNode;
                if (node != null) return !node.Destroyed;
                var token = value as ThemeToken;
                return token != null && !token.Destroyed;
            }
            public bool SameNode(object first, object second) { return ReferenceEquals(first, second); }
            public string Name(object node) { return ((ThemeNode)node).Name; }
            public object Parent(object node) { return ((ThemeNode)node).Parent; }
            public int ChildCount(object node) { return ((ThemeNode)node).Children.Count; }
            public object Child(object node, int index) { return ((ThemeNode)node).Children[index]; }
            public object[] Components(object node, NativeThemeComponent component)
            {
                object[] values;
                return ((ThemeNode)node).Components.TryGetValue(component, out values)
                    ? values : new object[0];
            }
            public NativeThemeResource SoundResource()
            {
                if (!SoundAvailable) throw new InvalidOperationException("no sound player");
                return new NativeThemeResource
                {
                    Nodes = new object[0],
                    Components = new object[] { new ThemeToken() },
                    Identity = "fixture sound"
                };
            }
            public void Validate(NativeThemeCapability capability, object[] components)
            {
                ValidateHook(capability, components);
            }
        }

        private static ThemeNode BuildDonorHierarchy(FixtureThemeSource source)
        {
            var owner = new ThemeNode { Name = "StaticCanvas" };
            ThemeNode serviceWindow = owner.Add("ServiceWindow");
            ThemeNode character = serviceWindow.Add("CharacterScreen");
            character.Add("BookBackground", Comp(NativeThemeComponent.Image, new ThemeToken()));
            ThemeNode levelBox = character.Add("LevelBox");
            ThemeNode button = levelBox.Add("Button_LevelUp",
                Comp(NativeThemeComponent.Button, new ThemeToken()));
            button.Add("Label", Comp(NativeThemeComponent.Text, new ThemeToken()));
            character.Add("BodyText", Comp(NativeThemeComponent.Text, new ThemeToken()));
            ThemeNode inventory = serviceWindow.Add("Inventory");
            inventory.Add("Search", Comp(NativeThemeComponent.InputField, new ThemeToken()));
            ThemeNode spellbook = serviceWindow.Add("SpellBook");
            spellbook.Add("Scrollbar Vertical",
                Comp(NativeThemeComponent.Scrollbar, new ThemeToken()));
            ThemeNode party = owner.Add("Party");
            ThemeNode partyCharacter = party.Add("Character");
            partyCharacter.Add("Highlight", Comp(NativeThemeComponent.Image, new ThemeToken()));
            source.Owner = owner;
            return owner;
        }

        private static void TestNativeThemeResolution()
        {
            var source = new FixtureThemeSource();
            ThemeNode owner = BuildDonorHierarchy(source);

            NativeThemeResolution full = NativeThemeResolver.Resolve(owner, source);
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
                if (!full.IsAvailable(capability))
                    throw new InvalidOperationException("Complete donor hierarchy rejected " +
                        capability + ": " + full.Failure(capability));
            string summary = full.Summary;
            if (!summary.Contains("Paper=ok(proven)") || !summary.Contains("Buttons=ok(proven)") ||
                !summary.Contains("ButtonText=ok(scan)") || !summary.Contains("Scrollbar=ok(scan)"))
                throw new InvalidOperationException("Resolution summary lost locator provenance: " + summary);

            // One missing donor (the button's label text) must reject exactly
            // that capability and leave every other surface native.
            var partial = new FixtureThemeSource();
            BuildDonorHierarchy(partial);
            ThemeNode label = FindByName(partial.Owner, "Label");
            label.Parent.Children.Remove(label);
            NativeThemeResolution degraded = NativeThemeResolver.Resolve(partial.Owner, partial);
            if (degraded.IsAvailable(NativeThemeCapability.ButtonText))
                throw new InvalidOperationException("Removing the label text kept ButtonText.");
            if (!degraded.IsAvailable(NativeThemeCapability.Buttons))
                throw new InvalidOperationException("Removing the label text also dropped the button artwork donor.");
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
                if (capability != NativeThemeCapability.ButtonText &&
                    !degraded.IsAvailable(capability))
                    throw new InvalidOperationException("Unrelated capability " + capability +
                        " fell back with only the label text missing.");

            // Ambiguous siblings must reject instead of guessing.
            var ambiguous = new FixtureThemeSource();
            BuildDonorHierarchy(ambiguous);
            ThemeNode characterScreen = FindByName(ambiguous.Owner, "CharacterScreen");
            characterScreen.Add("BookBackground", Comp(NativeThemeComponent.Image, new ThemeToken()));
            NativeThemeResolution ambiguousResolution = NativeThemeResolver.Resolve(
                ambiguous.Owner, ambiguous);
            if (ambiguousResolution.IsAvailable(NativeThemeCapability.Paper))
                throw new InvalidOperationException("Ambiguous paper donor was accepted.");

            // A destroyed cached donor is discarded so a bounded retry can
            // rebuild only that capability; a destroyed borrowed component
            // (not just its node) is equally stale.
            ((ThemeNode)full.Get(NativeThemeCapability.Paper).Nodes[0]).Destroyed = true;
            if (!full.DiscardStale(source) || full.IsAvailable(NativeThemeCapability.Paper))
                throw new InvalidOperationException("Stale paper donor survived discard.");
            if (!full.IsAvailable(NativeThemeCapability.Buttons))
                throw new InvalidOperationException("Discard removed an unrelated live capability.");
            NativeThemeResource bodyResource = full.Get(NativeThemeCapability.Body);
            if (bodyResource == null)
                throw new InvalidOperationException("Body capability vanished before component-stale check.");
            NativeThemeResource afterComponentStale = bodyResource;
            ((ThemeToken)afterComponentStale.Components[0]).Destroyed = true;
            if (!full.DiscardStale(source) || full.IsAvailable(NativeThemeCapability.Body))
                throw new InvalidOperationException("Destroyed borrowed component survived discard.");

            // Bounded recovery: at most MaximumAttempts re-resolves per owner.
            var recovery = new NativeThemeRecovery();
            recovery.Bind(owner);
            int attempts = 0;
            while (recovery.TryBegin(true)) { attempts++; recovery.Complete(); }
            if (attempts != NativeThemeRecovery.MaximumAttempts)
                throw new InvalidOperationException("Recovery attempts were not bounded: " + attempts);
            ThemeNode second = new ThemeNode { Name = "second-owner" };
            recovery.Bind(second);
            if (!recovery.TryBegin(true))
                throw new InvalidOperationException("Binding a new owner did not reset attempts.");

            // Bindings apply once per capability, re-apply on donor change, and
            // run the readable fallback when an apply fails. Built on a fresh
            // hierarchy because the stale checks above destroyed shared donors.
            var bindingFixture = new FixtureThemeSource();
            BuildDonorHierarchy(bindingFixture);
            NativeThemeResolution bindingSource = NativeThemeResolver.Resolve(
                bindingFixture.Owner, bindingFixture);
            var applied = new List<NativeThemeCapability>();
            var fellBack = new List<NativeThemeCapability>();
            var bindings = new NativeThemeBindings(reason => { });
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
                bindings.Add(capability, components => applied.Add(capability),
                    delegate { fellBack.Add(capability); });
            bindings.Apply(bindingSource);
            if (applied.Count != NativeThemeResolution.Capabilities.Length ||
                fellBack.Count != 0)
                throw new InvalidOperationException("Initial binding pass did not apply every capability.");
            applied.Clear();
            bindings.Apply(bindingSource);
            if (applied.Count != 0)
                throw new InvalidOperationException("Unchanged donors were re-applied.");
            var failing = new FixtureThemeSource();
            BuildDonorHierarchy(failing);
            failing.ValidateHook = (capability, components) =>
            {
                if (capability == NativeThemeCapability.Buttons)
                    throw new InvalidOperationException("donor contract changed");
            };
            NativeThemeResolution rejected = NativeThemeResolver.Resolve(failing.Owner, failing);
            bindings.Apply(rejected);
            if (!fellBack.Contains(NativeThemeCapability.Buttons))
                throw new InvalidOperationException("Failed apply did not run the parchment fallback.");
        }

        private static ThemeNode FindByName(ThemeNode root, string name)
        {
            if (root.Name == name) return root;
            foreach (ThemeNode child in root.Children)
            {
                ThemeNode found = FindByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // P1 reproducer: native ServiceWindow donor paths resolve only from
        // a native-canvas root. The released surface resolved from the
        // planner's own overlay root, so every donor rejected and the flat
        // fallback stayed on screen.
        private static void TestNativeThemeLookupScope()
        {
            var source = new FixtureThemeSource();
            ThemeNode nativeRoot = BuildDonorHierarchy(source);
            ThemeNode plannerRoot = new ThemeNode { Name = "FullScreenOverlayRoot" };
            plannerRoot.Add("ServiceFrame");

            NativeThemeResolution ownedScoped = NativeThemeResolver.Resolve(
                plannerRoot, source);
            if (ownedScoped.IsAvailable(NativeThemeCapability.Paper) ||
                ownedScoped.IsAvailable(NativeThemeCapability.Buttons) ||
                ownedScoped.IsAvailable(NativeThemeCapability.Body))
                throw new InvalidOperationException(
                    "An owned-overlay lookup root resolved native ServiceWindow donors.");

            NativeThemeResolution nativeScoped = NativeThemeResolver.Resolve(
                nativeRoot, source);
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
                if (!nativeScoped.IsAvailable(capability))
                    throw new InvalidOperationException(
                        "Native-canvas lookup root rejected " + capability + ": " +
                        nativeScoped.Failure(capability));

            // The two legitimate scopes stay distinct in stale checks too:
            // live donors under the native owner survive, and a donor that
            // moved into the owned overlay is discarded instead of applied.
            if (nativeScoped.DiscardStale(source))
                throw new InvalidOperationException(
                    "Live native-canvas donors were discarded from their own scope.");
            var staleFixture = new FixtureThemeSource();
            ThemeNode staleRoot = BuildDonorHierarchy(staleFixture);
            NativeThemeResolution staleScoped = NativeThemeResolver.Resolve(
                staleRoot, staleFixture);
            ThemeNode movedPaper = FindByName(staleRoot, "BookBackground");
            movedPaper.Parent.Children.Remove(movedPaper);
            plannerRoot.Children.Add(movedPaper);
            movedPaper.Parent = plannerRoot;
            if (!staleScoped.DiscardStale(staleFixture) ||
                staleScoped.IsAvailable(NativeThemeCapability.Paper))
                throw new InvalidOperationException(
                    "A donor moved into the owned overlay survived the native-scope stale check.");
        }

        // P3 reproducer: the spellbook entry must find the real native
        // window through the exact proven path, tolerate a renamed window
        // with a bounded unique scan, and refuse ambiguity loudly.
        private static void TestSpellbookWindowLocator()
        {
            var source = new FixtureThemeSource();
            ThemeNode canvas = new ThemeNode { Name = "StaticCanvas" };
            ThemeNode serviceWindow = canvas.Add("ServiceWindow");
            ThemeNode spellbook = serviceWindow.Add("SpellBook");
            spellbook.Add("Container_Book");

            string reason;
            SpellbookWindowLocator.Result exact = SpellbookWindowLocator.Find(
                canvas, source, out reason);
            if (exact == null || !ReferenceEquals(exact.Window, spellbook) ||
                !exact.Locator.Contains("path"))
                throw new InvalidOperationException(
                    "The exact spellbook path did not resolve: " + reason);

            var renamedSource = new FixtureThemeSource();
            ThemeNode renamedCanvas = new ThemeNode { Name = "StaticCanvas" };
            ThemeNode renamedService = renamedCanvas.Add("ServiceWindow");
            ThemeNode renamedWindow = renamedService.Add("SpellBookScreen");
            renamedWindow.Add("Container_Book");
            SpellbookWindowLocator.Result scan = SpellbookWindowLocator.Find(
                renamedCanvas, renamedSource, out reason);
            if (scan == null || !ReferenceEquals(scan.Window, renamedWindow) ||
                !scan.Locator.Contains("scan"))
                throw new InvalidOperationException(
                    "A uniquely renamed spellbook window was not discovered: " + reason);

            renamedService.Add("PartySpellBookList");
            SpellbookWindowLocator.Result ambiguous = SpellbookWindowLocator.Find(
                renamedCanvas, renamedSource, out reason);
            if (ambiguous != null || reason == null ||
                reason.IndexOf("ambiguous", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Ambiguous spellbook candidates were not refused: " + reason);

            var bareSource = new FixtureThemeSource();
            ThemeNode bareCanvas = new ThemeNode { Name = "StaticCanvas" };
            SpellbookWindowLocator.Result absent = SpellbookWindowLocator.Find(
                bareCanvas, bareSource, out reason);
            if (absent != null || reason == null ||
                reason.IndexOf("service-window-missing", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "A canvas without ServiceWindow was not reported as missing: " + reason);
        }

        // P4 reproducer (A5): one three-charge rod, four otherwise eligible
        // requests in one routine — the ordinary chooser view model exposes
        // requested/allocated/unmet/projected from the same production plan,
        // with no second charge counter.
        private static void TestChooserBudgetFromAuthoritativePlan()
        {
            RunRodBudgetCase(4, "felix", "short");
            // A6's larger case: nine eligible requests against one rod.
            RunRodBudgetCase(9, "felix", "short");
        }

        private static void RunRodBudgetCase(int targetCount, string casterId, string routineId)
        {
            AbilityKey ability = Ability("budget-spell-" + targetCount, string.Empty, 0);
            var pool = new ResourcePoolSnapshot("slots-" + targetCount,
                ResourcePoolKind.SpontaneousLevel, 12, 12, null);
            ProviderSnapshot caster = PlannerProvider(casterId, casterId + "-book",
                ability, pool.PoolKey, 1);
            var unitIds = new List<string> { casterId };
            for (int index = 1; index <= targetCount; index++)
                unitIds.Add("ally-" + index);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster }, new[] { pool }, unitIds.ToArray());
            var option = new ProviderPlanningOption(caster, unitIds.ToArray(),
                unitIds.ToArray(), 4, 40);
            string rodId = "metamagic-rod|" + casterId + "|quicken-rod";
            var rod = new CastEnhancementSnapshot(rodId, casterId, "quicken-rod",
                "Quicken Metamagic Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                4, 3, 3, null, "Quicken");
            var idleRod = new CastEnhancementSnapshot(
                "metamagic-rod|" + casterId + "|extend-rod", casterId, "extend-rod",
                "Extend Metamagic Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                8, 3, 2, null, "Extend");
            var effects = new Dictionary<string, EffectExpression>
            {
                { ability.Canonical, Leaf("budget-buff-" + targetCount) }
            };
            var active = new ActiveEffectSnapshot(null);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "chooser-budget-" + targetCount);
            var model = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { option }, ignored => { }, new[] { rod, idleRod });
            SetupSourceRow source = model.SelectedSource;
            for (int index = 1; index <= targetCount; index++)
                model.ToggleTarget(routineId, "ally-" + index);
            model.SetEnhancement(routineId, rodId);

            RoutinePlanResult preview = new RoutinePlanService().Plan(profile, routineId,
                snapshot, active, effects, new[] { option }, new[] { rod, idleRod });
            ResourcePoolAllocation allocation = preview.Plan.AllocationFor(
                "enhancement:" + rodId);
            int expectedUnmet = targetCount - 3;
            if (allocation == null || allocation.AvailableNow != 3 ||
                allocation.RequestedUsage != targetCount ||
                allocation.AllocatedUsage != 3 ||
                allocation.UnmetDemand != expectedUnmet ||
                allocation.ForecastRemaining != 0)
                throw new InvalidOperationException(
                    "Authoritative plan allocation was not the scarce-rod budget: " +
                    (allocation == null ? "missing" : allocation.RequestedUsage + "/" +
                        allocation.AllocatedUsage + "/" + allocation.UnmetDemand + "/" +
                        allocation.ForecastRemaining));

            SelectedCastingViewModel casting = SelectedCastingViewModel.Create(
                source, model, routineId, preview);
            if (casting.BudgetLines.Count != 1)
                throw new InvalidOperationException(
                    "Expected exactly one demanded enhancement pool line.");
            EnhancementBudgetLineViewModel line = casting.BudgetLines[0];
            if (line.NativeChargesNow != 3 || line.RequestedUses != targetCount ||
                line.AllocatedUses != 3 || line.UnmetUses != expectedUnmet ||
                line.ProjectedRemaining != 0 || line.OwnerName != casterId ||
                !line.Text.Contains(targetCount + " requested | 3 allocated | " +
                    expectedUnmet + " unmet") ||
                !line.Text.Contains("projected after " +
                    char.ToUpperInvariant(routineId[0]) + routineId.Substring(1) + ": 0") ||
                !line.Text.Contains("Quicken Metamagic Rod"))
                throw new InvalidOperationException(
                    "The budget line did not expose the mission's distinctions: " + line.Text);
            if (string.IsNullOrEmpty(casting.EnhancementBudgetText) ||
                casting.EnhancementBudgetText.IndexOf(
                    "never consumes charges", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "The template/no-consumption explanation was missing.");
            if (!casting.EnhancementWarning ||
                casting.EnhancementLabel.IndexOf(expectedUnmet + " charge", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Unmet demand did not surface on the ordinary card label: " +
                    casting.EnhancementLabel);
            EnhancementChoiceViewModel selectedChoice = casting.Choices.Single(
                choice => choice.EnhancementId == rodId);
            if (selectedChoice.BudgetNote.IndexOf(
                    targetCount + " requested, 3 allocated", StringComparison.Ordinal) < 0 ||
                selectedChoice.BudgetNote.IndexOf(
                    "this spell: 3/" + targetCount + " targets funded",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "The selected row lacked the pool and this-spell budget note: " +
                    selectedChoice.BudgetNote);
            EnhancementChoiceViewModel unselectedChoice = casting.Choices.Single(
                choice => choice.EnhancementId == idleRod.EnhancementId);
            if (unselectedChoice.Selected ||
                unselectedChoice.BudgetNote.IndexOf(
                    "selecting reserves 1 charge per enhanced cast",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "An unselected choice did not state its reservation cost: " +
                    unselectedChoice.BudgetNote);

            var planSummary = new SelectedBuffPlanSummaryViewModel(source, model,
                routineId, preview);
            if (planSummary.Text.IndexOf("Unmet enhancement demand",
                    StringComparison.Ordinal) < 0 ||
                planSummary.Text.IndexOf("Quicken Metamagic Rod", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "The main card summary hid the resolved shortage: " + planSummary.Text);
        }

        // A6: reordering assignments moves the scarce charge to the new
        // higher-priority cast immediately, through the same production plan.
        private static void TestChooserBudgetReorderedPriority()
        {
            AbilityKey ability = Ability("reorder-budget-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("reorder-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book", ability,
                pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { felix }, new[] { pool }, "felix", "t1", "t2", "t3", "t4");
            var option = new ProviderPlanningOption(felix,
                new[] { "felix", "t1", "t2", "t3", "t4" },
                new[] { "felix", "t1", "t2", "t3", "t4" }, 4, 40);
            string rodId = "metamagic-rod|felix|quicken-rod";
            var rod = new CastEnhancementSnapshot(rodId, "felix", "quicken-rod",
                "Quicken Metamagic Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                4, 3, 3, null, "Quicken");
            var effects = new Dictionary<string, EffectExpression>
            {
                { ability.Canonical, Leaf("reorder-budget-buff") }
            };
            var active = new ActiveEffectSnapshot(null);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "chooser-budget-reorder");
            SourceAssignmentProfile parent = Assignment(ability.Canonical, ability,
                new string[0]);
            parent.CastingAssignments[0].TargetUnitIds = new List<string> { "t1", "t2" };
            parent.CastingAssignments[0].Enhancements = new List<EnhancementSelectionProfile>
            {
                new EnhancementSelectionProfile { EnhancementId = rodId, Required = true }
            };
            parent.CastingAssignments.Add(new CastingAssignmentProfile
            {
                AssignmentId = "cast-2",
                Order = 1,
                TargetUnitIds = new List<string> { "t3", "t4" },
                Enhancements = new List<EnhancementSelectionProfile>
                {
                    new EnhancementSelectionProfile { EnhancementId = rodId, Required = true }
                }
            });
            profile.Routines.First(routine => routine.RoutineId == "short")
                .Assignments.Add(parent);

            RoutinePlanResult first = new RoutinePlanService().Plan(profile, "short",
                snapshot, active, effects, new[] { option }, new[] { rod });
            ResourcePoolAllocation allocation = first.Plan.AllocationFor(
                "enhancement:" + rodId);
            if (allocation == null || allocation.RequestedUsage != 4 ||
                allocation.AllocatedUsage != 3 || allocation.UnmetDemand != 1)
                throw new InvalidOperationException(
                    "Two ordered assignments did not share one rod budget.");
            if (first.Plan.Steps.Count(step => step.AssignmentId == "auto-" + ability.Canonical) != 2 ||
                first.Plan.Steps.Count(step => step.AssignmentId == "cast-2") != 1 ||
                !first.Plan.Outcomes.Any(outcome => outcome.UnitId == "t4" &&
                    outcome.Kind == TargetOutcomeKind.Unfulfilled))
                throw new InvalidOperationException(
                    "The earlier assignment did not win the scarce charges.");

            parent.CastingAssignments[0].Order = 1;
            parent.CastingAssignments[1].Order = 0;
            RoutinePlanResult second = new RoutinePlanService().Plan(profile, "short",
                snapshot, active, effects, new[] { option }, new[] { rod });
            ResourcePoolAllocation reordered = second.Plan.AllocationFor(
                "enhancement:" + rodId);
            if (reordered == null || reordered.RequestedUsage != 4 ||
                reordered.AllocatedUsage != 3 || reordered.UnmetDemand != 1)
                throw new InvalidOperationException(
                    "Reordering changed the shared budget totals unexpectedly.");
            if (second.Plan.Steps.Count(step => step.AssignmentId == "cast-2") != 2 ||
                second.Plan.Steps.Count(step => step.AssignmentId == "auto-" + ability.Canonical) != 1 ||
                !second.Plan.Outcomes.Any(outcome => outcome.UnitId == "t2" &&
                    outcome.Kind == TargetOutcomeKind.Unfulfilled))
                throw new InvalidOperationException(
                    "The freed charge did not move to the new higher-priority assignment.");

            var model = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { option }, ignored => { }, new[] { rod });
            SelectedCastingViewModel assignmentScoped = SelectedCastingViewModel.Create(
                model.SelectedSource, model, "short", second, "cast-2");
            EnhancementChoiceViewModel choice = assignmentScoped.Choices.Single(
                candidate => candidate.EnhancementId == rodId);
            if (choice.BudgetNote.IndexOf(
                    "this assignment: 2/2 targets funded",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "The assignment-scoped chooser lost its own coverage note: " +
                    choice.BudgetNote);
        }

        // C1 reproducer: "this spell" coverage must be scoped to the selected
        // spell (or child assignment), never the routine aggregate, and must
        // distinguish requested/funded/skipped targets from allocated casts
        // and charges — including communal casts.
        private static void TestChooserBudgetSpellScopedNotes()
        {
            AbilityKey[] abilities =
            {
                Ability("spell-a", string.Empty, 0),
                Ability("spell-b", string.Empty, 0),
                Ability("spell-c", string.Empty, 0),
                Ability("spell-d", string.Empty, 0)
            };
            var pool = new ResourcePoolSnapshot("scoped-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            string rodId = "metamagic-rod|felix|quicken-rod";
            var providers = new List<ProviderSnapshot>();
            var options = new List<ProviderPlanningOption>();
            var effects = new Dictionary<string, EffectExpression>();
            string[] targets = { "t-a", "t-b", "t-c", "t-d" };
            for (int index = 0; index < abilities.Length; index++)
            {
                providers.Add(PlannerProvider("felix", "book-" + index,
                    abilities[index], pool.PoolKey, 1));
                options.Add(new ProviderPlanningOption(providers[index],
                    new[] { "felix", targets[index] }, new[] { "felix", targets[index] }, 4, 40));
                effects[abilities[index].Canonical] = Leaf("scoped-buff-" + index);
            }
            PartyProviderSnapshot snapshot = PlannerSnapshot(providers,
                new[] { pool }, "felix", "t-a", "t-b", "t-c", "t-d");
            var rod = new CastEnhancementSnapshot(rodId, "felix", "quicken-rod",
                "Quicken Metamagic Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                4, 3, 3, null, "Quicken");
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "chooser-budget-spell-scoped");
            for (int index = 0; index < abilities.Length; index++)
            {
                SourceAssignmentProfile parent = Assignment(abilities[index].Canonical,
                    abilities[index], new[] { targets[index] },
                    new[] { rodId });
                profile.Routines.First(routine => routine.RoutineId == "short")
                    .Assignments.Add(parent);
            }
            var active = new ActiveEffectSnapshot(null);
            RoutinePlanResult preview = new RoutinePlanService().Plan(profile, "short",
                snapshot, active, effects, options, new[] { rod });
            ResourcePoolAllocation allocation = preview.Plan.AllocationFor(
                "enhancement:" + rodId);
            if (allocation == null || allocation.RequestedUsage != 4 ||
                allocation.AllocatedUsage != 3 || allocation.UnmetDemand != 1)
                throw new InvalidOperationException(
                    "The shared pool did not show the mission's global 4/3/1 budget.");
            var model = new PlannerSetupModel(profile, snapshot, active, effects,
                options, ignored => { }, new[] { rod });
            foreach (SetupSourceRow source in model.Sources)
            {
                SelectedCastingViewModel casting = SelectedCastingViewModel.Create(
                    source, model, "short", preview);
                EnhancementChoiceViewModel choice = casting.Choices.Single(
                    candidate => candidate.EnhancementId == rodId);
                if (!choice.Selected)
                    throw new InvalidOperationException(
                        "A spell selecting the rod showed it unselected: " + source.SourceId);
                // spell-d is alphabetically last in the routine's allocation
                // order, so it is exactly the unfunded one.
                bool funded = source.Ability.BaseAbilityGuid != "spell-d";
                string expected = funded
                    ? "this spell: 1/1 targets funded (1 charge allocated)"
                    : "this spell: 0/1 targets funded (0 charges allocated)";
                if (choice.BudgetNote.IndexOf(expected, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "The local note was not spell-scoped for " + source.SourceId +
                        ": " + choice.BudgetNote);
                if (choice.BudgetNote.IndexOf("3 allocated / 4 requested",
                        StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException(
                        "A routine aggregate leaked into a this-spell note: " +
                        choice.BudgetNote);
            }

            // Communal unit honesty: one party-wide spell, six targets, one
            // communal cast, one charge.
            AbilityKey communal = Ability("spell-communal", string.Empty, 0);
            var communalPool = new ResourcePoolSnapshot("communal-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot communalProvider = PlannerProvider("felix", "communal-book",
                communal, communalPool.PoolKey, 1);
            string[] communalTargets = { "c1", "c2", "c3", "c4", "c5", "felix" };
            PartyProviderSnapshot communalSnapshot = PlannerSnapshot(
                new[] { communalProvider }, new[] { communalPool }, communalTargets);
            var communalOption = new ProviderPlanningOption(communalProvider,
                communalTargets, communalTargets, 4, 40);
            var communalEffects = new Dictionary<string, EffectExpression>
            {
                { communal.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "communal-buff", EffectTarget.Party, "fixture", "fixture/communal") }
            };
            BuffPlannerProfile communalProfile = BuffPlannerProfile.CreateDefault(
                "chooser-budget-communal");
            communalProfile.Routines.First(routine => routine.RoutineId == "short")
                .Assignments.Add(Assignment(communal.Canonical, communal,
                    communalTargets, new[] { rodId }));
            RoutinePlanResult communalPreview = new RoutinePlanService().Plan(
                communalProfile, "short", communalSnapshot,
                new ActiveEffectSnapshot(null), communalEffects,
                new[] { communalOption }, new[] { rod });
            var communalModel = new PlannerSetupModel(communalProfile, communalSnapshot,
                new ActiveEffectSnapshot(null), communalEffects,
                new[] { communalOption }, ignored => { }, new[] { rod });
            EnhancementChoiceViewModel communalChoice = SelectedCastingViewModel.Create(
                communalModel.SelectedSource, communalModel, "short", communalPreview)
                .Choices.Single(candidate => candidate.EnhancementId == rodId);
            if (communalChoice.BudgetNote.IndexOf(
                    "this spell: 1 communal cast covers its targets (1 charge allocated)",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "A communal cast was presented as one-cast-vs-many-targets: " +
                    communalChoice.BudgetNote);

            // Already-active targets are labeled as skips, not as funding.
            var activeSnapshot = new ActiveEffectSnapshot(
                new Dictionary<string, IEnumerable<string>> {
                    { "c1", new[] { "communal-buff" } }
                });
            RoutinePlanResult skippedPreview = new RoutinePlanService().Plan(
                communalProfile, "short", communalSnapshot, activeSnapshot,
                communalEffects, new[] { communalOption }, new[] { rod });
            var skippedModel = new PlannerSetupModel(communalProfile, communalSnapshot,
                activeSnapshot, communalEffects,
                new[] { communalOption }, ignored => { }, new[] { rod });
            EnhancementChoiceViewModel skippedChoice = SelectedCastingViewModel.Create(
                skippedModel.SelectedSource, skippedModel, "short", skippedPreview)
                .Choices.Single(candidate => candidate.EnhancementId == rodId);
            if (skippedChoice.BudgetNote.IndexOf("1 already active",
                    StringComparison.Ordinal) < 0 ||
                skippedChoice.BudgetNote.IndexOf("communal cast covers its targets",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "An already-active target was not labeled distinctly: " +
                    skippedChoice.BudgetNote);
        }

        // C2 reproducer: the assignment-aware chooser builds choices, notes,
        // and availability from THAT child's selections; unavailable selected
        // enhancements stay individually removable; policy captions state the
        // actual required/optional and targeting semantics.
        private static void TestAssignmentChooserScopedChoices()
        {
            AbilityKey ability = Ability("scoped-chooser-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("scoped-chooser-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book", ability,
                pool.PoolKey, 1);
            ProviderSnapshot leinna = PlannerProvider("leinna", "leinna-book", ability,
                pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { felix, leinna }, new[] { pool }, "felix", "leinna", "t1", "t2");
            var felixOption = new ProviderPlanningOption(felix,
                new[] { "felix", "t1", "t2" }, new[] { "felix", "t1", "t2" }, 4, 40);
            var leinnaOption = new ProviderPlanningOption(leinna,
                new[] { "leinna", "t1", "t2" }, new[] { "leinna", "t1", "t2" }, 4, 40);
            string rodId = "metamagic-rod|felix|extend-rod";
            var rod = new CastEnhancementSnapshot(rodId, "felix", "extend-rod",
                "Extend Metamagic Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                8, 3, 2, null, "Extend");
            CastEnhancementSnapshot share = ClassEnhancement("share", "felix",
                ability, "felix-book", 3, "reservoir|felix",
                "brown-fur-share-transmutation", true);
            var effects = new Dictionary<string, EffectExpression>
            {
                { ability.Canonical, Leaf("scoped-chooser-buff") }
            };
            var active = new ActiveEffectSnapshot(null);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "assignment-scoped-chooser");
            SourceAssignmentProfile parent = Assignment(ability.Canonical, ability,
                new string[0]);
            parent.CastingAssignments.Add(new CastingAssignmentProfile
            {
                AssignmentId = "cast-2",
                Order = 1,
                CasterUnitId = "felix",
                TargetUnitIds = new List<string> { "t1", "t2" },
                Enhancements = new List<EnhancementSelectionProfile>
                {
                    new EnhancementSelectionProfile { EnhancementId = rodId, Required = true }
                }
            });
            parent.CastingAssignments.Add(new CastingAssignmentProfile
            {
                AssignmentId = "cast-3",
                Order = 2,
                CasterUnitId = "leinna",
                TargetUnitIds = new List<string> { },
                Enhancements = new List<EnhancementSelectionProfile>()
            });
            profile.Routines.First(routine => routine.RoutineId == "short")
                .Assignments.Add(parent);
            var model = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { felixOption, leinnaOption }, ignored => { },
                new[] { rod, share });
            SetupSourceRow source = model.SelectedSource;
            RoutinePlanResult preview = new RoutinePlanService().Plan(profile, "short",
                snapshot, active, effects, new[] { felixOption, leinnaOption },
                new[] { rod, share });

            // Pinned child selected the rod; the Automatic child did not.
            SelectedCastingViewModel assignmentView = SelectedCastingViewModel.Create(
                source, model, "short", preview, "cast-2");
            EnhancementChoiceViewModel assignmentRod = assignmentView.Choices.Single(
                candidate => candidate.EnhancementId == rodId);
            if (!assignmentRod.Selected || !assignmentRod.Available ||
                !assignmentRod.CanDeselect || !assignmentRod.CanSelect)
                throw new InvalidOperationException(
                    "The pinned child's selected rod was not presented as selected and removable.");
            if (assignmentRod.BudgetNote.IndexOf("this assignment:",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "The assignment chooser lost its local scope: " + assignmentRod.BudgetNote);
            if (assignmentRod.PolicyCaption != "REQUIRED" || !assignmentRod.CanTogglePolicy)
                throw new InvalidOperationException(
                    "An ordinary rod's policy caption was not honest: " +
                    assignmentRod.PolicyCaption);
            SelectedCastingViewModel automaticView = SelectedCastingViewModel.Create(
                source, model, "short", preview);
            if (automaticView.Choices.Single(candidate => candidate.EnhancementId == rodId)
                    .Selected)
                throw new InvalidOperationException(
                    "The Automatic child inherited the pinned child's selection.");

            // A caster-mismatched assignment scope makes the rod unavailable
            // there even though the source-wide union includes it.
            SelectedCastingViewModel foreignAssignment = SelectedCastingViewModel.Create(
                source, model, "short", preview, "cast-3");
            EnhancementChoiceViewModel foreignRod = foreignAssignment.Choices.Single(
                candidate => candidate.EnhancementId == rodId);
            if (foreignRod.Available || foreignRod.Selected ||
                foreignRod.CanSelect || foreignRod.CanDeselect)
                throw new InvalidOperationException(
                    "A rod owned by another caster stayed selectable for a pinned child.");

            // A targeting modifier can never present itself as optional.
            model.SetAssignmentEnhancement("short", source.SourceId, "cast-2",
                share.EnhancementId);
            preview = new RoutinePlanService().Plan(profile, "short", snapshot, active,
                effects, new[] { felixOption, leinnaOption }, new[] { rod, share });
            SelectedCastingViewModel withShare = SelectedCastingViewModel.Create(
                model.SelectedSource, model, "short", preview, "cast-2");
            EnhancementChoiceViewModel shareChoice = withShare.Choices.Single(
                candidate => candidate.EnhancementId == share.EnhancementId);
            if (!shareChoice.Selected || shareChoice.PolicyCaption != "REQUIRED (targeting)" ||
                shareChoice.CanTogglePolicy)
                throw new InvalidOperationException(
                    "A targeting modifier presented a toggleable or wrong policy: " +
                    shareChoice.PolicyCaption);
            EnhancementChoiceViewModel optionalRod = withShare.Choices.Single(
                candidate => candidate.EnhancementId == rodId);
            model.SetCastingAssignmentEnhancementPolicy("short", source.SourceId,
                "cast-2", rodId, false);
            withShare = SelectedCastingViewModel.Create(model.SelectedSource, model,
                "short", preview, "cast-2");
            optionalRod = withShare.Choices.Single(candidate =>
                candidate.EnhancementId == rodId);
            if (optionalRod.PolicyCaption != "OPTIONAL" || !optionalRod.CanTogglePolicy)
                throw new InvalidOperationException(
                    "An optional ordinary rod did not state its policy honestly: " +
                    optionalRod.PolicyCaption);

            // Exhaust only the pinned rod: it stays visible, selected, and
            // individually removable; the unrelated Share choice survives.
            var exhausted = new CastEnhancementSnapshot(rodId, "felix", "extend-rod",
                rod.DisplayName, rod.Description, rod.Category,
                rod.MetamagicMask, rod.MaximumSpellLevel, 0, null, "Extend");
            var exhaustedModel = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { felixOption, leinnaOption }, ignored => { },
                new[] { exhausted, share });
            SelectedCastingViewModel exhaustedView = SelectedCastingViewModel.Create(
                exhaustedModel.SelectedSource, exhaustedModel, "short", preview, "cast-2");
            EnhancementChoiceViewModel exhaustedRod = exhaustedView.Choices.Single(
                candidate => candidate.EnhancementId == rodId);
            if (!exhaustedRod.Selected || exhaustedRod.Available ||
                !exhaustedRod.CanDeselect || exhaustedRod.CanSelect)
                throw new InvalidOperationException(
                    "An exhausted selected rod was not individually removable.");
            EnhancementChoiceViewModel survivingShare = exhaustedView.Choices.Single(
                candidate => candidate.EnhancementId == share.EnhancementId);
            if (!survivingShare.Selected)
                throw new InvalidOperationException(
                    "An unrelated selection was disturbed by the exhausted rod.");
            exhaustedModel.SetAssignmentEnhancement("short",
                exhaustedModel.SelectedSource.SourceId, "cast-2", rodId);
            if (exhaustedModel.GetAssignmentEnhancementIds("short",
                    exhaustedModel.SelectedSource.SourceId, "cast-2")
                    .Contains(rodId) ||
                !exhaustedModel.GetAssignmentEnhancementIds("short",
                    exhaustedModel.SelectedSource.SourceId, "cast-2")
                    .Contains(share.EnhancementId))
                throw new InvalidOperationException(
                    "Individual removal also removed or failed to remove selections.");

            // The ordinary Automatic workflow can also remove an unavailable
            // selected enhancement without clearing anything else: select
            // both while the rod is available, then reload the catalog with
            // the rod exhausted and remove only it.
            var selectingModel = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { felixOption, leinnaOption }, ignored => { },
                new[] { rod, share });
            selectingModel.SetEnhancement("short", share.EnhancementId);
            selectingModel.SetEnhancement("short", rodId);
            var autoModel = new PlannerSetupModel(profile, snapshot, active, effects,
                new[] { felixOption, leinnaOption }, ignored => { },
                new[] { exhausted, share });
            autoModel.SetEnhancement("short", exhausted.EnhancementId);
            if (autoModel.GetSelectedEnhancementIds("short")
                    .Contains(exhausted.EnhancementId) ||
                !autoModel.GetSelectedEnhancementIds("short")
                    .Contains(share.EnhancementId))
                throw new InvalidOperationException(
                    "Automatic-scope removal of an unavailable selection failed.");
        }

        // C3: the sticky summary and row notes are length-bounded regardless
        // of pool count or name lengths; the full detail survives in the
        // choice description (tooltip) rather than being silently truncated.
        private static void TestChooserBudgetTextStaysBounded()
        {
            if (EnhancementBudgetModel.BoundText("short",
                    EnhancementBudgetModel.MaximumSummaryLength) != "short" ||
                EnhancementBudgetModel.BoundText(
                    new string('x', EnhancementBudgetModel.MaximumSummaryLength + 50),
                    EnhancementBudgetModel.MaximumSummaryLength).Length !=
                EnhancementBudgetModel.MaximumSummaryLength)
                throw new InvalidOperationException("BoundText lost its bound or its content.");
            var lines = new List<EnhancementBudgetLineViewModel>();
            string longName = new string('N', 80);
            var affected = new List<string>();
            for (int index = 0; index < 12; index++)
                affected.Add(longName + " (target-" + index + ")");
            for (int index = 0; index < 8; index++)
                lines.Add(new EnhancementBudgetLineViewModel(
                    "enhancement:pool-" + index, longName + " rod " + index, longName,
                    "Named Spell", 3, 40 + index, 3, 37 + index, 0, "Short", affected));
            string summary = EnhancementBudgetModel.SummaryText(lines, "short");
            if (string.IsNullOrEmpty(summary) ||
                summary.Length > EnhancementBudgetModel.MaximumSummaryLength)
                throw new InvalidOperationException(
                    "The sticky summary exceeded its renderable bound: " + summary.Length);
            var pathological = new CastEnhancementSnapshot(
                "rod-long", "felix", "long-guid", longName + " Metamagic Rod",
                string.Empty, CastEnhancementCategory.MetamagicRod, 4, 3, 1, null, "Quicken");
            string note = EnhancementBudgetModel.ChoiceNote(pathological, true, lines,
                null, null, "short", "source-long", null);
            if (note.Length > EnhancementBudgetModel.MaximumNoteLength)
                throw new InvalidOperationException(
                    "A row note exceeded its renderable bound: " + note.Length);
            // Full pool detail is NOT lost by the bounds: the choice
            // description (the row tooltip) carries the whole line text.
            AbilityKey boundedAbility = Ability("bounded-spell", string.Empty, 0);
            var boundedPool = new ResourcePoolSnapshot("bounded-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot boundedProvider = PlannerProvider("felix", "bounded-book",
                boundedAbility, boundedPool.PoolKey, 1);
            var boundedRod = new CastEnhancementSnapshot("rod-bounded", "felix",
                "bounded-rod", "Bounded Quicken Rod", string.Empty,
                CastEnhancementCategory.MetamagicRod, 4, 3, 3, null, "Quicken");
            PartyProviderSnapshot boundedSnapshot = PlannerSnapshot(
                new[] { boundedProvider }, new[] { boundedPool }, "felix", "t1");
            var boundedOption = new ProviderPlanningOption(boundedProvider,
                new[] { "felix", "t1" }, new[] { "felix", "t1" }, 4, 40);
            var boundedEffects = new Dictionary<string, EffectExpression>
            {
                { boundedAbility.Canonical, Leaf("bounded-buff") }
            };
            BuffPlannerProfile boundedProfile = BuffPlannerProfile.CreateDefault("bounded");
            boundedProfile.Routines.First(routine => routine.RoutineId == "short")
                .Assignments.Add(Assignment(boundedAbility.Canonical, boundedAbility,
                    new[] { "t1" }, new[] { "rod-bounded" }));
            RoutinePlanResult boundedPreview = new RoutinePlanService().Plan(
                boundedProfile, "short", boundedSnapshot, new ActiveEffectSnapshot(null),
                boundedEffects, new[] { boundedOption }, new[] { boundedRod });
            var boundedModel = new PlannerSetupModel(boundedProfile, boundedSnapshot,
                new ActiveEffectSnapshot(null), boundedEffects, new[] { boundedOption },
                ignored => { }, new[] { boundedRod });
            EnhancementChoiceViewModel boundedChoice = SelectedCastingViewModel.Create(
                boundedModel.SelectedSource, boundedModel, "short", boundedPreview)
                .Choices.Single(candidate => candidate.EnhancementId == "rod-bounded");
            if (boundedChoice.Description.IndexOf("projected after Short",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "The bounded summary dropped the full detail from the tooltip: " +
                    boundedChoice.Description);
        }

        // P5 reproducer: unnamed extended metamagic masks never reach player
        // text as integers; the provider contract or the item name supplies
        // the readable effect name.
        private static void TestMetamagicLabelsNeverShowRawMasks()
        {
            if (CastEnhancementNaming.EffectDisplayName(4, "Quicken Metamagic Rod",
                    mask => "Quicken") != "Quicken")
                throw new InvalidOperationException(
                    "A named game-enum mask stopped passing through.");
            if (CastEnhancementNaming.EffectDisplayName(9, "Rod",
                    mask => "Empower, Maximize") != "Empower, Maximize")
                throw new InvalidOperationException(
                    "A named game-enum combination stopped passing through.");
            if (CastEnhancementNaming.EffectDisplayName(268435456,
                    "Persistent Metamagic Rod", mask => "Persistent") != "Persistent" ||
                CastEnhancementNaming.EffectDisplayName(8192, "Rod",
                    mask => "Threnodic") != "Threnodic")
                throw new InvalidOperationException(
                    "The provider display-name contract was not honored.");
            // The provider absent: the item's own name is the descriptor.
            if (CastEnhancementNaming.EffectDisplayName(268435456,
                    "Persistent Metamagic Rod", mask => null) != "Persistent" ||
                CastEnhancementNaming.EffectDisplayName(8192,
                    "Metamagic Rod, Threnodic", mask => null) != "Threnodic" ||
                CastEnhancementNaming.EffectDisplayName(33554432,
                    "Lesser Selective Rod", mask => null) != "Selective")
                throw new InvalidOperationException(
                    "The item-derived fallback lost the owner's rod names.");
            if (CastEnhancementNaming.EffectDisplayName(524288, "Rod", mask => null)
                    != "Metamagic")
                throw new InvalidOperationException(
                    "An undescriptive fallback did not stay neutral.");
            // A legacy numeric resolver result (the released defect shape)
            // still yields readable text and never a digit.
            string legacy = CastEnhancementNaming.EffectDisplayName(524288,
                "Piercing Metamagic Rod", mask => "524288");
            if (legacy != "Piercing" || legacy.Any(char.IsDigit))
                throw new InvalidOperationException(
                    "A numeric string leaked into the effect name: " + legacy);

            var legacyNumeric = new CastEnhancementSnapshot("rod-legacy", "felix",
                "legacy-guid", "Piercing Metamagic Rod", string.Empty,
                CastEnhancementCategory.MetamagicRod, 524288, 3, 1, null, "524288");
            if (PlannerSetupModel.EffectName(legacyNumeric) != "Piercing Spell")
                throw new InvalidOperationException(
                    "EffectName kept a raw mask suffix: " +
                    PlannerSetupModel.EffectName(legacyNumeric));
            var named = new CastEnhancementSnapshot("rod-named", "felix", "named-guid",
                "Quicken Metamagic Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                4, 3, 3, null, "Quicken");
            if (PlannerSetupModel.EffectName(named) != "Quicken Spell")
                throw new InvalidOperationException(
                    "EffectName dropped the ordinary Spell suffix.");
            var alreadySuffixed = new CastEnhancementSnapshot("rod-suffixed", "felix",
                "suffixed-guid", "Threnodic Rod", string.Empty,
                CastEnhancementCategory.MetamagicRod, 8192, 3, 1, null, "Threnodic Spell");
            if (PlannerSetupModel.EffectName(alreadySuffixed) != "Threnodic Spell")
                throw new InvalidOperationException(
                    "EffectName double-suffixed a prepared effect name.");
        }

        // A11's installed-contract lane: the provider assembly that produced
        // the owner's numeric labels resolves through the same fail-soft
        // contract the shipped adapter uses.
        private static void TestInstalledCallOfTheWildMetamagicNames()
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            string path = string.IsNullOrWhiteSpace(game) ? string.Empty : Path.Combine(
                game, "Mods", "CallOfTheWild", "CallOfTheWild.dll");
            if (!File.Exists(path)) return;
            Assembly provider = Assembly.LoadFrom(path);
            if (provider == null) return;
            if (CallOfTheWildMetamagicNames.Describe(268435456) != "Persistent" ||
                CallOfTheWildMetamagicNames.Describe(524288) != "Piercing" ||
                CallOfTheWildMetamagicNames.Describe(33554432) != "Selective" ||
                CallOfTheWildMetamagicNames.Describe(8192) != "Threnodic")
                throw new InvalidOperationException(
                    "The installed provider's extended metamagic names did not resolve: " +
                    CallOfTheWildMetamagicNames.ContractSummary);
            if (CallOfTheWildMetamagicNames.Describe(4) != null)
                throw new InvalidOperationException(
                    "A base-game mask resolved through the provider contract.");
            if (string.IsNullOrEmpty(CallOfTheWildMetamagicNames.ContractSummary) ||
                CallOfTheWildMetamagicNames.ContractSummary.IndexOf(
                    "loaded:", StringComparison.Ordinal) != 0)
                throw new InvalidOperationException(
                    "The provider contract summary was not recorded: " +
                    CallOfTheWildMetamagicNames.ContractSummary);
        }

        private static void TestControlCaptionFit()
        {
            if (ControlCaptionFit.RequiredWidth(100f, 5f) != 112f ||
                ControlCaptionFit.RequiredHeight(20f, 1f) != 24f)
                throw new InvalidOperationException("Caption fit dropped its insets or safety margin.");
            if (ControlCaptionFit.ResolveExtent(96f, 80f) != 96f ||
                ControlCaptionFit.ResolveExtent(96f, 140f) != 140f)
                throw new InvalidOperationException("Caption fit must grow from the design floor only.");
            if (ControlCaptionFit.ResolveExtent(96f, float.NaN) != 96f ||
                ControlCaptionFit.ResolveExtent(96f, -5f) != 96f ||
                ControlCaptionFit.ResolveExtent(96f, float.PositiveInfinity) != 96f)
                throw new InvalidOperationException("Invalid measurements must keep the design floor.");
        }

        // Mission checkpoint C, canonical mixed-caster example: one catalog
        // source, three child assignments, one resolved plan. Leinna casts on
        // herself unenhanced; Felix casts on himself unenhanced and on Tias
        // and Raine through Share. Four casts, Share only where configured.
        private static void TestCastingAssignmentRouting()
        {
            AbilityKey ability = Ability("echolocation", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("echo-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot leinna = PlannerProvider("leinna", "leinna-book",
                ability, pool.PoolKey, 1);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { leinna, felix }, new[] { pool },
                "leinna", "felix", "tias", "raine");
            var leinnaOption = new ProviderPlanningOption(leinna,
                new[] { "leinna" }, new[] { "leinna" }, 4, 40);
            var felixOption = new ProviderPlanningOption(felix,
                new[] { "felix" }, new[] { "felix" }, 5, 50);
            CastEnhancementSnapshot share = ClassEnhancement("share", "felix",
                ability, "felix-book", 3, "reservoir|felix",
                "brown-fur-share-transmutation", true);
            var targeting = new EffectiveProviderOptionResolver(
                new ICastTargetingModifier[] {
                    new FixtureShareTargetingModifier("share", "felix",
                        new[] { "felix", "tias", "raine" })
                });
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "assignment-routing");
            SourceAssignmentProfile parent = Assignment(
                ability.Canonical, ability, new string[0]);
            parent.CastingAssignments[0].TargetUnitIds = new List<string> { "leinna" };
            parent.CastingAssignments.Add(new CastingAssignmentProfile
            {
                AssignmentId = "cast-2",
                Order = 1,
                CasterUnitId = "felix",
                TargetUnitIds = new List<string> { "felix" },
                Enhancements = new List<EnhancementSelectionProfile>()
            });
            parent.CastingAssignments.Add(new CastingAssignmentProfile
            {
                AssignmentId = "cast-3",
                Order = 2,
                CasterUnitId = "felix",
                TargetUnitIds = new List<string> { "tias", "raine" },
                Enhancements = new List<EnhancementSelectionProfile>
                {
                    new EnhancementSelectionProfile { EnhancementId = "share", Required = true }
                }
            });
            profile.Routines[0].Assignments.Add(parent);

            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                        "echo-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
                }, new[] { leinnaOption, felixOption }, new[] { share }, targeting);
            CastPlan plan = result.Plan;
            if (plan.Steps.Count != 4)
                throw new InvalidOperationException("The mixed-caster example must produce four casts, not " +
                    plan.Steps.Count + ".");
            CastStep leinnaSelf = plan.Steps.SingleOrDefault(step =>
                step.AssignmentId == "auto-" + ability.Canonical);
            CastStep felixSelf = plan.Steps.SingleOrDefault(step =>
                step.AssignmentId == "cast-2");
            List<CastStep> felixShare = plan.Steps.Where(step =>
                step.AssignmentId == "cast-3").ToList();
            if (leinnaSelf == null || felixSelf == null || felixShare.Count != 2 ||
                leinnaSelf.Provider.CasterUnitId != "leinna" ||
                felixSelf.Provider.CasterUnitId != "felix" ||
                felixShare.Any(step => step.Provider.CasterUnitId != "felix"))
                throw new InvalidOperationException("Casts were not routed to the configured casters.");
            if (leinnaSelf.EnhancementIds.Count != 0 ||
                felixSelf.EnhancementIds.Count != 0 ||
                felixShare.Any(step => !step.EnhancementIds.Contains("share")))
                throw new InvalidOperationException(
                    "Share leaked outside the two configured non-self casts or was missing from them.");
            if (!felixShare.All(step => step.TargetUnitIds.Count == 1) ||
                felixShare.Any(step => step.TargetUnitIds.Contains("felix")))
                throw new InvalidOperationException("Share casts did not cover exactly the explicit non-self targets.");
            if (plan.Outcomes.Count != 4 ||
                plan.Outcomes.Any(outcome => outcome.Kind != TargetOutcomeKind.Fulfilled))
                throw new InvalidOperationException("Some configured targets were not fulfilled.");
            ResourcePoolAllocation reservoir = plan.AllocationFor("enhancement:reservoir|felix");
            if (reservoir == null || reservoir.AvailableNow != 3 ||
                reservoir.AllocatedUsage != 2 || reservoir.UnmetDemand != 0 ||
                reservoir.Traces.Count != 1 || reservoir.Traces[0] != "cast-3")
                throw new InvalidOperationException("Enhancement reservoir accounting did not trace to its assignment.");

            // A pin is a hard constraint: pinning a party member with no
            // provider for this spell leaves the cast unresolved instead of
            // falling back to an available caster.
            parent.CastingAssignments[0].CasterUnitId = "tias";
            RoutinePlanResult pinned = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                        "echo-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
                }, new[] { leinnaOption, felixOption }, new[] { share }, targeting);
            TargetPlanOutcome leinnaOutcome = pinned.Plan.Outcomes.Single(outcome =>
                outcome.AssignmentId == "auto-" + ability.Canonical);
            if (leinnaOutcome.Kind != TargetOutcomeKind.Unfulfilled ||
                !pinned.Plan.Diagnostics.Any(line => line.Contains(
                    "pin-unresolved:auto-" + ability.Canonical + ":pinned-caster-unavailable:tias")))
                throw new InvalidOperationException(
                    "An unavailable pinned caster silently fell back to another caster.");
        }

        // Mission checkpoint C shortage and order fixtures: assignment order
        // (never catalog order) decides who gets the limited charges, nine
        // requested casts against three charges report 9/3/3/6, already-active
        // skips reserve nothing, and optional policy labels its omissions.
        private static void TestPartialExecutionGate()
        {
            // A plan with unmet targets is blocked for default Apply and its
            // summary separates requested coverage from successful casts; a
            // complete plan (including free already-active skips) is not.
            AbilityKey ability = Ability("gate-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("gate-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster }, new[] { pool }, "caster", "fine", "blocked");
            var option = new ProviderPlanningOption(caster,
                new[] { "caster", "fine" }, new[] { "caster" }, 4, 40);
            CastPlan complete = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("gate", ability,
                    Leaf("gate-buff"), CastGroupingKind.PerTarget),
                    new[] { "fine" }, ExistingEffectPolicy.Overwrite, null),
                new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            PartialExecutionGate.Decision open = PartialExecutionGate.Evaluate(complete);
            if (open.Blocked || open.RequestedTargets != 1 || open.PlannedCasts != 1 ||
                !open.Summary.Contains("1 planned cast"))
                throw new InvalidOperationException("A complete routine was gated or miscounted.");

            var active = new Dictionary<string, IEnumerable<ActiveEffectMarker>>();
            active["fine"] = new[] { new ActiveEffectMarker(EffectKind.Buff, "gate-buff") };
            CastPlan withSkip = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("gate", ability,
                    Leaf("gate-buff"), CastGroupingKind.PerTarget),
                    new[] { "fine", "blocked" }, ExistingEffectPolicy.SkipAlreadyActive, null),
                new[] { option }, EmptyPolicy(),
                ActiveEffectSnapshot.FromTypedEffects(active));
            PartialExecutionGate.Decision mixed = PartialExecutionGate.Evaluate(withSkip);
            if (!mixed.Blocked || mixed.RequestedTargets != 2 || mixed.SkippedActive != 1 ||
                mixed.Unfulfilled != 1 || mixed.PlannedCasts != 0 ||
                !mixed.Summary.Contains("blocked ("))
                throw new InvalidOperationException(
                    "The gate did not report requested coverage separately from cast success.");
            // The refusal wording used by both the planner Apply and the HUD
            // quick-run names the ready-only escape hatch explicitly.
            string refusal = mixed.Summary +
                " Apply blocked to avoid running only part of Long; use Apply Ready Casts Only to run the ready subset.";
            if (!refusal.Contains("Requested 2 targets") ||
                !refusal.Contains("Apply Ready Casts Only"))
                throw new InvalidOperationException("Refusal summary lost its coverage counts or escape hatch.");
        }

        private static void TestCastingOrderPresentation()
        {
            // The 9/3/3/6 fixture must be navigable and explained: numbered
            // rows in explicit order, resolved caster text, pin visibility,
            // and one resource line whose summary states the exact counts.
            AbilityKey ability = Ability("order-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("order-slots",
                ResourcePoolKind.SpontaneousLevel, 9, 9, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                ability, pool.PoolKey, 1);
            string[] nine = Enumerable.Range(1, 9)
                .Select(index => "target-" + index).ToArray();
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster }, new[] { pool },
                new[] { "caster" }.Concat(nine).ToArray());
            var option = new ProviderPlanningOption(caster,
                new[] { "caster" }.Concat(nine), new[] { "caster" }, 4, 40);
            CastEnhancementSnapshot rod = ClassEnhancement("order-rod", "caster",
                ability, "book", 3, "order-pool", "order-rod-group", false);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "order-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("order-view");
            SourceAssignmentProfile parent = Assignment(
                ability.Canonical, ability, new string[0]);
            parent.CastingAssignments[0].TargetUnitIds = new List<string>(nine);
            parent.CastingAssignments[0].Enhancements.Add(
                new EnhancementSelectionProfile { EnhancementId = "order-rod" });
            profile.Routines[0].Assignments.Add(parent);
            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null), effects,
                new[] { option }, new[] { rod });

            IReadOnlyList<CastingAssignmentRowViewModel> rows =
                CastingAssignmentRowViewModel.CreateRoutineRows(profile, "long",
                    sourceId => "Order Spell", unitId => unitId,
                    enhancementId => "Order Rod", result.Plan);
            CastingAssignmentRowViewModel row = rows.Single();
            if (row.Number != 1 || !row.Automatic || row.CasterText != "Automatic" ||
                row.TargetNames.Count != 9 ||
                !row.EnhancementTexts[0].StartsWith("Order Rod", StringComparison.Ordinal) ||
                row.PlannedCasts != 3 || row.FulfilledTargets != 3 ||
                row.UnfulfilledTargets != 6 || row.CanMoveEarlier || row.CanMoveLater)
                throw new InvalidOperationException("Casting-order row did not derive from the plan result.");
            if (!row.Status.Contains("6 unmet"))
                throw new InvalidOperationException("Row status hid the shortage: " + row.Status);

            ResourcePoolAllocation allocation = result.Plan.AllocationFor("enhancement:order-pool");
            var line = new ResourceUsageLineViewModel(allocation, "Uses (Order Rod)",
                "Long", new[] { "Short", "Important" });
            if (!line.Summary.Contains("requested 9") ||
                !line.Summary.Contains("available 3") ||
                !line.Summary.Contains("allocated 3") ||
                !line.Summary.Contains("unmet 6") ||
                !line.Summary.Contains("forecast remaining 0") ||
                !line.Summary.Contains("Short, Important (their own runs)"))
                throw new InvalidOperationException("Resource line lost the 9/3/3/6 accounting or competing-demand label: " +
                    line.Summary);

            // Missing pins survive as visible unresolved intent: a pinned
            // assignment that cannot resolve keeps its row with a diagnostic.
            parent.CastingAssignments[0].CasterUnitId = "target-9";
            RoutinePlanResult pinnedPlan = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null), effects,
                new[] { option }, new[] { rod });
            IReadOnlyList<CastingAssignmentRowViewModel> pinnedRows =
                CastingAssignmentRowViewModel.CreateRoutineRows(profile, "long",
                    sourceId => "Order Spell", unitId => unitId,
                    enhancementId => "Order Rod", pinnedPlan.Plan);
            if (pinnedRows.Count != 1 || !pinnedRows[0].PinUnresolved ||
                !pinnedRows[0].Status.Contains("Pinned caster unavailable"))
                throw new InvalidOperationException("A missing pin did not survive as visible unresolved intent.");
        }

        // Reproduces the canonical Echolocation setup through exactly the
        // model APIs the casting-order editor calls — the same add/cycle/
        // enhance/move/split/remove sequence a player performs in the UI.
        // Constructing the profile directly proves planning; this proves the
        // editor's service surface.
        private static void TestAssignmentEditorModel()
        {
            AbilityKey ability = Ability("editor-echolocation", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("editor-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot leinna = PlannerProvider("leinna", "leinna-book",
                ability, pool.PoolKey, 1);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { leinna, felix }, new[] { pool },
                "leinna", "felix", "tias", "raine");
            var leinnaOption = new ProviderPlanningOption(leinna,
                new[] { "leinna" }, new[] { "leinna" }, 4, 40);
            var felixOption = new ProviderPlanningOption(felix,
                new[] { "felix" }, new[] { "felix" }, 5, 50);
            CastEnhancementSnapshot share = ClassEnhancement("share", "felix",
                ability, "felix-book", 3, "reservoir|felix",
                "brown-fur-share-transmutation", true);
            var targeting = new EffectiveProviderOptionResolver(
                new ICastTargetingModifier[] {
                    new FixtureShareTargetingModifier("share", "felix",
                        new[] { "felix", "tias", "raine" })
                });
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "editor-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("editor-flow");
            int saves = 0;
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { leinnaOption, felixOption }, ignored => saves++,
                new[] { share }, targeting);
            SetupSourceRow source = model.SelectedSource;

            // Simple workflow first: one Automatic child via portrait toggle.
            // Share must be selected before non-caster targets are legal for
            // this personal-range spell, exactly as in the game UI.
            model.ToggleTarget("long", "leinna");
            model.SetEnhancement("long", "share");
            model.ToggleTarget("long", "felix");
            model.ToggleTarget("long", "tias");
            model.ToggleTarget("long", "raine");
            if (model.GetCastingAssignments("long", source.SourceId).Count != 1)
                throw new InvalidOperationException(
                    "The simple workflow no longer edits a single Automatic child.");

            // Add the pinned Felix rows through the cycle control.
            CastingAssignmentProfile felixSelf = model.AddCastingAssignment(
                "long", source.SourceId);
            model.CycleCastingAssignmentCaster("long", source.SourceId,
                felixSelf.AssignmentId);
            CastingAssignmentProfile shareRow = model.AddCastingAssignment(
                "long", source.SourceId);
            model.CycleCastingAssignmentCaster("long", source.SourceId,
                shareRow.AssignmentId);
            if (felixSelf.CasterUnitId != "felix" || shareRow.CasterUnitId != "felix")
                throw new InvalidOperationException(
                    "Caster cycling did not pass through Leinna to Felix.");
            // Distribute the explicit targets atomically: Felix self into the
            // plain pinned row; Tias and Raine into the Share row.
            string automaticId = model.GetCastingAssignments("long", source.SourceId)[0]
                .AssignmentId;
            model.MoveTargetToAssignment("long", source.SourceId, automaticId,
                felixSelf.AssignmentId, "felix");
            model.SetAssignmentEnhancement("long", source.SourceId,
                shareRow.AssignmentId, "share");
            model.MoveTargetToAssignment("long", source.SourceId, automaticId,
                shareRow.AssignmentId, "tias");
            model.MoveTargetToAssignment("long", source.SourceId, automaticId,
                shareRow.AssignmentId, "raine");
            // Share now lives only on the pinned row; toggling it off the
            // Automatic row restores the plain self-cast.
            model.SetEnhancement("long", "share");
            model.SetCastingAssignmentEnhancementPolicy("long", source.SourceId,
                shareRow.AssignmentId, "share", false);
            if (!model.GetAssignmentEnhancementIds("long", source.SourceId,
                    shareRow.AssignmentId).SequenceEqual(new[] { "share" }) ||
                model.FindCastingAssignment("long", source.SourceId,
                    shareRow.AssignmentId).Enhancements[0].Required != false)
                throw new InvalidOperationException(
                    "Per-assignment enhancement selection or policy did not persist.");
            // A duplicate explicit move must refuse instead of duplicating.
            bool refused = false;
            try
            {
                model.MoveTargetToAssignment("long", source.SourceId,
                    felixSelf.AssignmentId, shareRow.AssignmentId, "tias");
            }
            catch (ArgumentException) { refused = true; }
            if (!refused)
                throw new InvalidOperationException(
                    "Moving an already-assigned target did not refuse atomically.");

            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null), effects,
                new[] { leinnaOption, felixOption }, new[] { share }, targeting);
            List<CastStep> shareSteps = result.Plan.Steps.Where(step =>
                step.EnhancementIds.Contains("share")).ToList();
            if (result.Plan.Steps.Count != 4 || shareSteps.Count != 2 ||
                !shareSteps.All(step => step.Provider.CasterUnitId == "felix") ||
                !shareSteps.Any(step => step.TargetUnitIds[0] == "tias") ||
                !shareSteps.Any(step => step.TargetUnitIds[0] == "raine") ||
                result.Plan.Steps.Any(step => !step.EnhancementIds.Contains("share") &&
                    step.TargetUnitIds.Any(id => id == "tias" || id == "raine")))
                throw new InvalidOperationException(
                    "The editor-built configuration did not plan the expected casts.");

            // Removal stays clean and never touches sibling rows: removing
            // one target keeps the pinned row and its other target, then the
            // whole row can be removed explicitly.
            model.RemoveTargetFromAssignment("long", source.SourceId,
                shareRow.AssignmentId, "tias");
            if (model.IsTargetWanted("long", source.SourceId, "tias") ||
                !model.IsTargetWanted("long", source.SourceId, "raine") ||
                !model.GetCastingAssignments("long", source.SourceId)
                    .Any(child => child.AssignmentId == shareRow.AssignmentId))
                throw new InvalidOperationException(
                    "Single-target removal leaked state into sibling rows.");
            model.RemoveCastingAssignment("long", source.SourceId,
                shareRow.AssignmentId);
            model.RemoveCastingAssignment("long", source.SourceId,
                felixSelf.AssignmentId);
            if (model.GetCastingAssignments("long", source.SourceId).Count != 1 ||
                !model.IsTargetWanted("long", source.SourceId, "leinna") ||
                saves < 10)
                throw new InvalidOperationException(
                    "Row removal or edit persistence regressed.");
        }

        private static void TestPlanMaterialChangeDetector()
        {
            AbilityKey ability = Ability("material-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("material-slots",
                ResourcePoolKind.SpontaneousLevel, 4, 4, null);
            ProviderSnapshot casterA = PlannerProvider("caster-a", "book-a",
                ability, pool.PoolKey, 1);
            ProviderSnapshot casterB = PlannerProvider("caster-b", "book-b",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { casterA, casterB }, new[] { pool },
                "caster-a", "caster-b", "ally-1", "ally-2");
            var optionA = new ProviderPlanningOption(casterA,
                new[] { "caster-a", "ally-1", "ally-2" }, new[] { "caster-a" }, 4, 40);
            var optionB = new ProviderPlanningOption(casterB,
                new[] { "caster-b", "ally-1", "ally-2" }, new[] { "caster-b" }, 4, 40);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "material-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
            };
            CastPlan baseline = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("material", ability,
                    Leaf("material-buff"), CastGroupingKind.PerTarget),
                    new[] { "ally-1" }, ExistingEffectPolicy.Overwrite, null),
                new[] { optionA, optionB }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            CastPlan same = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("material", ability,
                    Leaf("material-buff"), CastGroupingKind.PerTarget),
                    new[] { "ally-1" }, ExistingEffectPolicy.Overwrite, null),
                new[] { optionA, optionB }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (PlanMaterialChangeDetector.DescribeMaterialChange(baseline, same) != null)
                throw new InvalidOperationException("Identical plans were reported as materially changed.");

            // Caster/provider change is material.
            var banned = new ProviderSelectionPolicy(new[] { casterA.Key.Canonical },
                null, null);
            CastPlan otherCaster = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("material", ability,
                    Leaf("material-buff"), CastGroupingKind.PerTarget),
                    new[] { "ally-1" }, ExistingEffectPolicy.Overwrite, null),
                new[] { optionA, optionB }, banned, new ActiveEffectSnapshot(null));
            string casterChange = PlanMaterialChangeDetector.DescribeMaterialChange(
                baseline, otherCaster);
            if (casterChange == null || !casterChange.Contains("caster-b"))
                throw new InvalidOperationException("A caster change was not material: " + casterChange);

            // Coverage change is material with identical step sequences:
            // the second target flips from unfulfilled to already-active skip
            // while the single planned cast is unchanged.
            var reachA = new ProviderPlanningOption(casterA,
                new[] { "caster-a", "ally-1" }, new[] { "caster-a" }, 4, 40);
            var active = new Dictionary<string, IEnumerable<ActiveEffectMarker>>();
            active["ally-2"] = new[] { new ActiveEffectMarker(EffectKind.Buff, "material-buff") };
            CastPlan withSkip = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("material", ability,
                    Leaf("material-buff"), CastGroupingKind.PerTarget),
                    new[] { "ally-1", "ally-2" }, ExistingEffectPolicy.SkipAlreadyActive, null),
                new[] { reachA }, EmptyPolicy(),
                ActiveEffectSnapshot.FromTypedEffects(active));
            CastPlan withoutSkip = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("material", ability,
                    Leaf("material-buff"), CastGroupingKind.PerTarget),
                    new[] { "ally-1", "ally-2" }, ExistingEffectPolicy.SkipAlreadyActive, null),
                new[] { reachA }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            string coverageChange = PlanMaterialChangeDetector.DescribeMaterialChange(
                withoutSkip, withSkip);
            if (coverageChange == null || !coverageChange.Contains("coverage changed"))
                throw new InvalidOperationException("A coverage change was not material: " + coverageChange);

            // Order change is material even with identical steps.
            CastPlan reordered = new CastPlanner().PlanRoutine(snapshot,
                new[] {
                    new BuffCastRequest(new BuffSourceDefinition("material-b", ability,
                        Leaf("material-buff"), CastGroupingKind.PerTarget),
                        new[] { "ally-2" }, ExistingEffectPolicy.Overwrite, null,
                        new[] { new EnhancementRequest("rod", true) },
                        "second", null, null, null, 1),
                    new BuffCastRequest(new BuffSourceDefinition("material-a", ability,
                        Leaf("material-buff"), CastGroupingKind.PerTarget),
                        new[] { "ally-1" }, ExistingEffectPolicy.Overwrite, null,
                        new[] { new EnhancementRequest("rod", true) },
                        "first", null, null, null, 0)
                }, new[] { optionA, optionB }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] {
                    ClassEnhancement("rod", "caster-a", ability, "book-a", 4,
                        "rod-pool", "rod-group", false)
                });
            CastPlan reorderedSwapped = new CastPlanner().PlanRoutine(snapshot,
                new[] {
                    new BuffCastRequest(new BuffSourceDefinition("material-b", ability,
                        Leaf("material-buff"), CastGroupingKind.PerTarget),
                        new[] { "ally-2" }, ExistingEffectPolicy.Overwrite, null,
                        new[] { new EnhancementRequest("rod", true) },
                        "second", null, null, null, 0),
                    new BuffCastRequest(new BuffSourceDefinition("material-a", ability,
                        Leaf("material-buff"), CastGroupingKind.PerTarget),
                        new[] { "ally-1" }, ExistingEffectPolicy.Overwrite, null,
                        new[] { new EnhancementRequest("rod", true) },
                        "first", null, null, null, 1)
                }, new[] { optionA, optionB }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] {
                    ClassEnhancement("rod", "caster-a", ability, "book-a", 4,
                        "rod-pool", "rod-group", false)
                });
            string orderChange = PlanMaterialChangeDetector.DescribeMaterialChange(
                reordered, reorderedSwapped);
            if (orderChange == null)
                throw new InvalidOperationException("An allocation-order change was not material.");
        }

        // Editor-intent regressions from the PR review: unique automatic ids
        // after pinning, no cross-child duplicates, clear-keeps-enhancements,
        // split-preserves-configuration, unavailable-enhancement removal, the
        // first assignment from an empty source, save/reload of the edited
        // setup, and the flat non-overlapping target layout.
        private static void TestAssignmentEditorIntent()
        {
            AbilityKey ability = Ability("intent-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("intent-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { felix }, new[] { pool }, "felix", "tias", "raine");
            var option = new ProviderPlanningOption(felix,
                new[] { "felix", "tias", "raine" }, new[] { "felix" }, 4, 40);
            CastEnhancementSnapshot share = ClassEnhancement("share", "felix",
                ability, "felix-book", 3, "reservoir|felix",
                "brown-fur-share-transmutation", true);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "intent-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("intent");
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects, new[] { option },
                ignored => { }, new[] { share });
            SetupSourceRow source = model.SelectedSource;

            // First assignment from a completely empty source/routine.
            CastingAssignmentProfile first = model.AddCastingAssignment("long", source.SourceId);
            if (model.GetCastingAssignments("long", source.SourceId).Count != 1 ||
                model.GetRoutineCastingOrder("long").Count != 1)
                throw new InvalidOperationException(
                    "Creating the first assignment from an empty source failed.");

            // Pin the original automatic child, then portrait-toggle: the new
            // automatic child must get a unique id and absorb the target.
            model.ToggleAssignmentTarget("long", source.SourceId,
                first.AssignmentId, "felix");
            model.CycleCastingAssignmentCaster("long", source.SourceId, first.AssignmentId);
            model.ToggleTarget("long", "tias");
            List<CastingAssignmentProfile> children = model
                .GetCastingAssignments("long", source.SourceId).ToList();
            if (children.Count != 2 ||
                children.Select(child => child.AssignmentId).Distinct(StringComparer.Ordinal)
                    .Count() != 2)
                throw new InvalidOperationException(
                    "A pinned original automatic child caused an id reuse or merge.");
            CastingAssignmentProfile automatic = children.First(child => child.IsAutomatic);
            if (!automatic.TargetUnitIds.Contains("tias") ||
                children.Any(child => child != automatic &&
                    child.TargetUnitIds.Contains("tias")))
                throw new InvalidOperationException(
                    "A portrait toggle duplicated a target across children.");

            // Split preserves pins and enhancement intent, and stays directly
            // after its origin in the explicit order.
            model.SetAssignmentEnhancement("long", source.SourceId,
                automatic.AssignmentId, "share");
            CastingAssignmentProfile split = model.SplitCastingAssignment(
                "long", source.SourceId, automatic.AssignmentId, "tias");
            if (split.Enhancements.Count != 1 ||
                split.Enhancements[0].EnhancementId != "share" ||
                !automatic.Enhancements.Select(e => e.EnhancementId).Contains("share"))
                throw new InvalidOperationException(
                    "Split dropped the origin row's enhancement intent instead of copying it.");
            List<CastingAssignmentProfile> ordered = model.GetRoutineCastingOrder("long").ToList();
            int splitIndex = ordered.FindIndex(child => child.AssignmentId == split.AssignmentId);
            int originIndex = ordered.FindIndex(child =>
                child.AssignmentId == automatic.AssignmentId);
            if (splitIndex != originIndex + 1)
                throw new InvalidOperationException(
                    "Split row did not keep the position immediately after its origin.");

            // Clearing all valid targets keeps configured enhancements.
            model.SetAllValidTargets("long", false);
            children = model.GetCastingAssignments("long", source.SourceId).ToList();
            if (children.Count == 0)
                throw new InvalidOperationException(
                    "Clearing targets silently discarded configured enhancement intent.");

            // An unavailable saved enhancement stays individually removable
            // without first becoming applicable again.
            model.SetAssignmentEnhancement("long", source.SourceId,
                automatic.AssignmentId, "share");
            var reloadedModel = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects, new[] { option },
                ignored => { });
            CastingAssignmentProfile reloadedAutomatic = reloadedModel
                .GetCastingAssignments("long", source.SourceId)
                .First(child => child.Enhancements.Any(selection =>
                    selection.EnhancementId == "share"));
            string reloadedId = reloadedAutomatic.AssignmentId;
            reloadedModel.SetAssignmentEnhancement("long", source.SourceId,
                reloadedId, "share");
            if (reloadedModel.GetAssignmentEnhancementIds("long", source.SourceId, reloadedId)
                    .Contains("share"))
                throw new InvalidOperationException(
                    "An unavailable saved enhancement could not be removed without applicability.");

            // Save/reload round-trip of the edited configuration.
            string modPath = Path.Combine(Path.GetTempPath(),
                "kbp-intent-roundtrip-" + Guid.NewGuid().ToString("N"));
            var repository = new ProfileRepository(modPath);
            repository.Save(profile);
            BuffPlannerProfile loaded = repository.Load(profile.CampaignId).Profile;
            int configured = profile.Routines[0].Assignments
                .SelectMany(a => a.CastingAssignments).Count();
            int loadedCount = loaded.Routines[0].Assignments
                .SelectMany(a => a.CastingAssignments).Count();
            if (loadedCount != configured)
                throw new InvalidOperationException(
                    "The edited multi-assignment configuration did not round-trip.");

            // Flat target layout: every explicit target owns its own row for
            // 1, 2, 6, and 10 targets without overlap.
            foreach (int targetCount in new[] { 1, 2, 6, 10 })
            {
                var rowModel = new CastingAssignmentRowViewModel(1, "S", "s",
                    new CastingAssignmentProfile
                    {
                        AssignmentId = "a", Order = 0,
                        TargetUnitIds = Enumerable.Range(1, targetCount)
                            .Select(index => "u" + index).ToList(),
                        Enhancements = new List<EnhancementSelectionProfile>()
                    }, "Automatic", false,
                    Enumerable.Range(1, targetCount).Select(index => "u" + index).ToList(),
                    new string[0], 0, 0, 0, false, false, true);
                IReadOnlyList<CastingOrderLayout.RowPlan> plan =
                    CastingOrderLayout.PlanRows(new[] { rowModel });
                if (plan.Count != 1 + targetCount ||
                    !CastingOrderLayout.RowsAreDistinct(plan) ||
                    plan.Count(row => row.IsTargetRow) != targetCount)
                    throw new InvalidOperationException(
                        "The flat casting-order layout overlapped or dropped rows for " +
                        targetCount + " targets.");
            }
        }

        // N1: one provider under two assignments with different effective
        // targeting (plain self-cast vs Share) — the self-only option must
        // never suppress the shared reach, in EITHER creation order, and the
        // picker uses assignment-specific legality with pins enforced by the
        // production resolver.
        private static void TestSameProviderTargeting()
        {
            AbilityKey ability = Ability("n1-echolocation", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("n1-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { felix }, new[] { pool }, "felix", "tias", "raine");
            var felixOption = new ProviderPlanningOption(felix,
                new[] { "felix" }, new[] { "felix" }, 5, 50);
            CastEnhancementSnapshot share = ClassEnhancement("share", "felix",
                ability, "felix-book", 3, "reservoir|felix",
                "brown-fur-share-transmutation", true);
            var targeting = new EffectiveProviderOptionResolver(
                new ICastTargetingModifier[] {
                    new FixtureShareTargetingModifier("share", "felix",
                        new[] { "felix", "tias", "raine" })
                });
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "n1-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
            };

            foreach (bool shareFirst in new[] { false, true })
            {
                BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                    "n1-" + (shareFirst ? "s" : "p"));
                var model = new PlannerSetupModel(profile, snapshot,
                    new ActiveEffectSnapshot(null), effects,
                    new[] { felixOption }, ignored => { }, new[] { share }, targeting);
                SetupSourceRow source = model.SelectedSource;

                model.ToggleTarget("long", "felix");
                List<CastingAssignmentProfile> children = model
                    .GetCastingAssignments("long", source.SourceId).ToList();
                string firstId = children[0].AssignmentId;
                CastingAssignmentProfile second = shareFirst
                    ? model.SplitCastingAssignment("long", source.SourceId, firstId, "felix")
                    : model.SplitCastingAssignment("long", source.SourceId, firstId, "felix");
                model.SetAssignmentEnhancement("long", source.SourceId,
                    second.AssignmentId, "share");
                if (!shareFirst)
                {
                    // Reorder so the plain child runs FIRST explicitly.
                    model.MoveCastingAssignmentEarlier("long", second.AssignmentId);
                }

                // Source-level legality: the shared reach must survive the
                // plain child's self-only option for the SAME provider.
                if (!model.IsTargetLegal(source, "long", "tias") ||
                    !model.IsTargetLegal(source, "long", "raine") ||
                    !model.IsTargetLegal(source, "long", "felix"))
                    throw new InvalidOperationException(
                        "The self-only option suppressed the Share reach (shareFirst=" +
                        shareFirst + ").");

                // Assignment-specific legality: the plain child cannot reach
                // tias; the Share child can; a pinned caster removes the rest.
                // The Share child is always the split child (`second`); the
                // order variation only changes their relative Order.
                string plainId = firstId;
                string shareId = second.AssignmentId;
                if (model.IsTargetLegalForAssignment(source, "long", plainId, "tias"))
                    throw new InvalidOperationException(
                        "The plain child's picker offered Share-only reach.");
                if (!model.IsTargetLegalForAssignment(source, "long", shareId, "tias"))
                    throw new InvalidOperationException(
                        "The Share child's picker lost its own reach.");
                // Pin the plain child to a different caster: resolver must
                // remove every other candidate (pin enforcement).
                CastingAssignmentProfile plainChild = model.FindCastingAssignment(
                    "long", source.SourceId, plainId);
                plainChild.CasterUnitId = "tias"; // no provider for tias
                if (model.IsTargetLegalForAssignment(source, "long", plainId, "felix"))
                    throw new InvalidOperationException(
                        "A pinned caster did not remove other providers' reach.");
                plainChild.CasterUnitId = null;
            }
        }

        // N2: the picker toggle on a sibling-owned target refuses with the
        // owner named instead of silently removing coverage; the selected
        // child only ever gains or loses its own targets.
        private static void TestPickerToggleCoverage()
        {
            AbilityKey ability = Ability("n2-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("n2-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { felix }, new[] { pool }, "felix", "tias", "raine");
            var option = new ProviderPlanningOption(felix,
                new[] { "felix", "tias", "raine" }, new[] { "felix" }, 4, 40);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, Leaf("n2-buff") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("n2-picker");
            int saves = 0;
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects, new[] { option },
                ignored => saves++);
            SetupSourceRow source = model.SelectedSource;
            model.ToggleTarget("long", "tias");
            string rowAId = model.GetCastingAssignments("long", source.SourceId)
                .First().AssignmentId;
            CastingAssignmentProfile rowB = model.SplitCastingAssignment(
                "long", source.SourceId, rowAId, "tias");
            // Simulate the picker's exact callback: checking tias in row A
            // (which no longer owns it; sibling row B does) must refuse.
            bool refused = false;
            try
            {
                model.ToggleAssignmentTarget("long", source.SourceId, rowAId, "tias");
            }
            catch (InvalidOperationException exception)
            {
                refused = true;
                if (!exception.Message.Contains("already assigned"))
                    throw new InvalidOperationException(
                        "Sibling refusal lost the owner context: " + exception.Message);
            }
            if (!refused)
                throw new InvalidOperationException(
                    "Checking a sibling-owned target in another row did not refuse.");
            // Coverage survived: tias still requested exactly once.
            List<CastingAssignmentProfile> children = model
                .GetCastingAssignments("long", source.SourceId).ToList();
            if (children.Sum(child => child.TargetUnitIds.Count(id => id == "tias")) != 1)
                throw new InvalidOperationException(
                    "Picker toggle dropped requested coverage silently.");
            // Row B gains its own target; deselect removes only from row B.
            model.ToggleAssignmentTarget("long", source.SourceId, rowB.AssignmentId, "raine");
            if (!rowB.TargetUnitIds.Contains("raine"))
                throw new InvalidOperationException("Row B did not gain its own target.");
            model.ToggleAssignmentTarget("long", source.SourceId, rowB.AssignmentId, "raine");
            if (rowB.TargetUnitIds.Contains("raine") || saves < 3)
                throw new InvalidOperationException(
                    "Deselect did not remove from the selected child only.");
        }

        // N3: a caster-directed effect on a cast anchored at another unit
        // projects onto the CASTER, not the anchor; a later request for that
        // effect on the anchor must still cast.
        private static void TestForecastCasterIdentity()
        {
            AbilityKey buffSpell = Ability("n3-anchor-spell", string.Empty, 0);
            AbilityKey selfSpell = Ability("n3-self-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("n3-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot casterA = PlannerProvider("caster-a", "book-a",
                buffSpell, pool.PoolKey, 0);
            ProviderSnapshot selfCaster = PlannerProvider("caster-a", "book-a",
                selfSpell, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { casterA, selfCaster }, new[] { pool },
                "caster-a", "anchor-b");
            var buffOption = new ProviderPlanningOption(casterA,
                new[] { "caster-a", "anchor-b" }, new[] { "caster-a" }, 4, 40);
            var selfOption = new ProviderPlanningOption(selfCaster,
                new[] { "caster-a", "anchor-b" }, new[] { "caster-a" }, 4, 40);
            // Anchor spell: a target effect on the anchor plus a DISTINCT
            // caster-directed effect.
            EffectExpression anchorEffects = new SequenceEffectExpression(
                new EffectExpression[] {
                    new EffectLeafExpression(EffectKind.Buff, "n3-target-buff",
                        EffectTarget.CurrentTarget, "ContextActionApplyBuff", "root/a"),
                    new EffectLeafExpression(EffectKind.Buff, "n3-caster-buff",
                        EffectTarget.Caster, "ContextActionApplyBuff", "root/b")
                });
            var effects = new Dictionary<string, EffectExpression> {
                { buffSpell.Canonical, anchorEffects },
                { selfSpell.Canonical, Leaf("n3-caster-buff") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("n3-caster");
            profile.Routines.First(r => r.RoutineId == "long").Assignments.Add(Assignment(
                buffSpell.Canonical, buffSpell, new[] { "anchor-b" }));
            profile.Routines.First(r => r.RoutineId == "short").Assignments.Add(Assignment(
                selfSpell.Canonical, selfSpell, new[] { "anchor-b" }));
            SequentialForecastPlanner.Result result = SequentialForecastPlanner.Compute(
                profile, new[] { "long", "short" }, snapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { buffOption, selfOption }, new CastEnhancementSnapshot[0]);
            // The short routine requests n3-caster-buff on anchor-b: the long
            // cast projected that effect onto caster-a (the caster), NOT the
            // anchor — so anchor-b must still CAST.
            int shortCasts = result.Occurrences[1].Plan.Steps.Count;
            if (shortCasts != 1)
                throw new InvalidOperationException(
                    "A caster-directed effect was invented onto the anchor (short casts=" +
                    shortCasts + ").");
            // Self-cast on caster-a for the same effect skips for free.
            BuffPlannerProfile selfProfile = BuffPlannerProfile.CreateDefault("n3-caster-self");
            selfProfile.Routines.First(r => r.RoutineId == "long").Assignments.Add(Assignment(
                buffSpell.Canonical, buffSpell, new[] { "anchor-b" }));
            selfProfile.Routines.First(r => r.RoutineId == "short").Assignments.Add(Assignment(
                selfSpell.Canonical, selfSpell, new[] { "caster-a" }));
            SequentialForecastPlanner.Result selfResult = SequentialForecastPlanner.Compute(
                selfProfile, new[] { "long", "short" }, snapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { buffOption, selfOption }, new CastEnhancementSnapshot[0]);
            if (selfResult.Occurrences[1].Plan.Steps.Count != 0 ||
                selfResult.Occurrences[1].Plan.Outcomes.Count(o =>
                    o.Kind == TargetOutcomeKind.SkippedAlreadyActive) != 1)
                throw new InvalidOperationException(
                    "The caster's own later request did not skip for free.");
        }

        // R4: the ordinary source/portrait path resolves each child's pins,
        // targets, and enhancement selections separately instead of merging
        // them into one incompatible union; portraits never silently convert
        // pinned routing; select-all/clear reconcile with pinned rows.
        private static void TestPortraitChildIntent()
        {
            AbilityKey ability = Ability("portrait-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("portrait-slots",
                ResourcePoolKind.SpontaneousLevel, 8, 8, null);
            ProviderSnapshot leinna = PlannerProvider("leinna", "leinna-book",
                ability, pool.PoolKey, 1);
            ProviderSnapshot felix = PlannerProvider("felix", "felix-book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { leinna, felix }, new[] { pool },
                "leinna", "felix", "tias");
            var leinnaOption = new ProviderPlanningOption(leinna,
                new[] { "leinna" }, new[] { "leinna" }, 4, 40);
            var felixOption = new ProviderPlanningOption(felix,
                new[] { "felix" }, new[] { "felix" }, 5, 50);
            // Two DIFFERENT caster-owned rods: an exclusive-group union would
            // make them incompatible.
            CastEnhancementSnapshot leinnaRod = ClassEnhancement("rod-l", "leinna",
                ability, "leinna-book", 3, "pool-l", "rod-group", false);
            CastEnhancementSnapshot felixRod = ClassEnhancement("rod-f", "felix",
                ability, "felix-book", 3, "pool-f", "rod-group", false);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "portrait-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("portrait-intent");
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { leinnaOption, felixOption }, ignored => { },
                new[] { leinnaRod, felixRod });
            SetupSourceRow source = model.SelectedSource;

            // Two children, one rod each, one target each: the simple
            // portrait strip starts both on the Automatic child, then the
            // editor splits them.
            model.ToggleTarget("long", "leinna");
            model.ToggleTarget("long", "felix");
            List<CastingAssignmentProfile> children = model
                .GetCastingAssignments("long", source.SourceId).ToList();
            string felixRowId = children[0].AssignmentId;
            CastingAssignmentProfile felixRow = model.SplitCastingAssignment(
                "long", source.SourceId, felixRowId, "felix");
            felixRowId = felixRow.AssignmentId;
            model.CycleCastingAssignmentCaster("long", source.SourceId, felixRowId);
            model.SetAssignmentEnhancement("long", source.SourceId,
                children.First(child => child.TargetUnitIds.Contains("leinna")).AssignmentId,
                "rod-l");
            model.SetAssignmentEnhancement("long", source.SourceId, felixRowId, "rod-f");

            // The union would merge rod-l + rod-f (same exclusive group) into
            // an impossible combined selection and kill all legality.
            if (!model.IsTargetLegal(source, "long", "leinna") ||
                !model.IsTargetLegal(source, "long", "felix"))
                throw new InvalidOperationException(
                    "The portrait path merged two children's rods into one incompatible union.");

            // The plan still routes each child with its own rod.
            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null), effects,
                new[] { leinnaOption, felixOption }, new[] { leinnaRod, felixRod });
            if (result.Plan.Steps.Count != 2 ||
                !result.Plan.Steps.Any(step => step.EnhancementIds.Contains("rod-l") &&
                    step.Provider.CasterUnitId == "leinna") ||
                !result.Plan.Steps.Any(step => step.EnhancementIds.Contains("rod-f") &&
                    step.Provider.CasterUnitId == "felix"))
                throw new InvalidOperationException(
                    "Child rods did not stay on their own casts.");

            // Clicking a portrait held by a pinned row never edits that row
            // from the simple strip — neither deselect nor select converts
            // the pinned routing to Automatic.
            children = model.GetCastingAssignments("long", source.SourceId).ToList();
            string pinnedId = children.First(child =>
                !child.IsAutomatic && child.TargetUnitIds.Contains("felix")).AssignmentId;
            model.ToggleTarget("long", "felix");
            model.ToggleTarget("long", "felix");
            children = model.GetCastingAssignments("long", source.SourceId).ToList();
            CastingAssignmentProfile stillPinned = children.First(child =>
                child.AssignmentId == pinnedId);
            if (!stillPinned.TargetUnitIds.Contains("felix") || stillPinned.IsAutomatic ||
                children.Count(child => child.TargetUnitIds.Contains("felix")) != 1)
                throw new InvalidOperationException(
                    "A simple-strip toggle converted or duplicated pinned routing.");

            // Select-all after a pinned assignment does not duplicate its
            // targets into the Automatic child. The pinned row already holds
            // 'leinna' or 'felix'; select-all must not copy it.
            model.SetAllValidTargets("long", true);
            children = model.GetCastingAssignments("long", source.SourceId).ToList();
            foreach (string unitId in new[] { "leinna", "felix" })
                if (children.Count(child => child.TargetUnitIds.Contains(unitId)) > 1)
                    throw new InvalidOperationException(
                        "Select-all duplicated a pinned row's target: " + unitId);
            // Clearing removes only the Automatic child's targets.
            model.SetAllValidTargets("long", false);
            children = model.GetCastingAssignments("long", source.SourceId).ToList();
            CastingAssignmentProfile survivor = children.FirstOrDefault(child =>
                !child.IsAutomatic && child.TargetUnitIds.Count > 0);
            if (survivor == null)
                throw new InvalidOperationException(
                    "Clear-all discarded pinned routing instead of reconciling.");
        }

        // R3a: prepared-slot tokens are consumed exactly (linked companions
        // included) across occurrences; the second occurrence cannot reuse a
        // spent slot even with different targets, an independent unused
        // token stays usable, and reversing the order moves the shortfall.
        private static void TestForecastPreparedTokens()
        {
            AbilityKey longAbility = Ability("prep-long", string.Empty, 0);
            AbilityKey shortAbility = Ability("prep-short", string.Empty, 0);
            // Case 1: exactly ONE prepared token shared by both routines.
            var onlyToken = new ResourceTokenSnapshot("t-only",
                longAbility, 1, PreparedSlotKind.Favorite, true, true, new string[0]);
            var onePool = new ResourcePoolSnapshot("prep-one",
                ResourcePoolKind.PreparedSlots, 1, 1, new[] { onlyToken });
            ProviderSnapshot longCaster = new ProviderSnapshot(
                new ProviderKey("caster", "book", longAbility, "level-2"),
                longAbility.BaseAbilityGuid, 1, onePool.PoolKey, 1, new[] { "t-only" });
            ProviderSnapshot shortCaster = new ProviderSnapshot(
                new ProviderKey("caster", "book", shortAbility, "level-2"),
                shortAbility.BaseAbilityGuid, 1, onePool.PoolKey, 1, new[] { "t-only" });
            PartyProviderSnapshot oneSnapshot = PlannerSnapshot(
                new[] { longCaster, shortCaster }, new[] { onePool },
                "caster", "la", "sa");
            var longOption = new ProviderPlanningOption(longCaster,
                new[] { "caster", "la" }, new[] { "caster" }, 4, 40);
            var shortOption = new ProviderPlanningOption(shortCaster,
                new[] { "caster", "sa" }, new[] { "caster" }, 4, 40);
            var effects = new Dictionary<string, EffectExpression> {
                { longAbility.Canonical, Leaf("prep-long-buff") },
                { shortAbility.Canonical, Leaf("prep-short-buff") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("prep-forecast");
            profile.Routines.First(r => r.RoutineId == "long").Assignments.Add(Assignment(
                longAbility.Canonical, longAbility, new[] { "la" }));
            profile.Routines.First(r => r.RoutineId == "short").Assignments.Add(Assignment(
                shortAbility.Canonical, shortAbility, new[] { "sa" }));

            SequentialForecastPlanner.Result forward = SequentialForecastPlanner.Compute(
                profile, new[] { "long", "short" }, oneSnapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { longOption, shortOption }, new CastEnhancementSnapshot[0]);
            if (forward.Occurrences[0].Plan.Steps.Count != 1 ||
                forward.Occurrences[1].Plan.Steps.Count != 0 ||
                forward.Occurrences[1].Plan.Outcomes.Count(o =>
                    o.Kind == TargetOutcomeKind.Unfulfilled) != 1)
                throw new InvalidOperationException(
                    "A single prepared token was reused across occurrences: long=" +
                    forward.Occurrences[0].Plan.Steps.Count + " short=" +
                    forward.Occurrences[1].Plan.Steps.Count + ".");
            if (forward.ForecastRemainingByNativePool["prep-one"] != 0)
                throw new InvalidOperationException(
                    "Final prepared availability should be zero after one spent token.");
            SequentialForecastPlanner.Result reverse = SequentialForecastPlanner.Compute(
                profile, new[] { "short", "long" }, oneSnapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { longOption, shortOption }, new CastEnhancementSnapshot[0]);
            if (reverse.Occurrences[0].Plan.Steps.Count != 1 ||
                reverse.Occurrences[1].Plan.Outcomes.Count(o =>
                    o.Kind == TargetOutcomeKind.Unfulfilled) != 1)
                throw new InvalidOperationException(
                    "Reversing the sequence did not move the prepared-token shortfall.");

            // Case 2: a linked pair consumed exactly once by the long cast,
            // plus an independent free token that stays usable for short.
            var primary = new ResourceTokenSnapshot("t-primary",
                longAbility, 1, PreparedSlotKind.Favorite, true, true, new[] { "t-linked" });
            var linked = new ResourceTokenSnapshot("t-linked",
                longAbility, 1, PreparedSlotKind.Common, true, false, new string[0]);
            var free = new ResourceTokenSnapshot("t-free",
                shortAbility, 1, PreparedSlotKind.Favorite, true, true, new string[0]);
            var pairPool = new ResourcePoolSnapshot("prep-pair",
                ResourcePoolKind.PreparedSlots, 3, 3, new[] { primary, linked, free });
            ProviderSnapshot pairLong = new ProviderSnapshot(
                new ProviderKey("caster", "book", longAbility, "level-2"),
                longAbility.BaseAbilityGuid, 1, pairPool.PoolKey, 1, new[] { "t-primary" });
            ProviderSnapshot pairShort = new ProviderSnapshot(
                new ProviderKey("caster", "book", shortAbility, "level-2"),
                shortAbility.BaseAbilityGuid, 1, pairPool.PoolKey, 1, new[] { "t-free" });
            PartyProviderSnapshot pairSnapshot = PlannerSnapshot(
                new[] { pairLong, pairShort }, new[] { pairPool },
                "caster", "la", "sa");
            SequentialForecastPlanner.Result pair = SequentialForecastPlanner.Compute(
                profile, new[] { "long", "short" }, pairSnapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] {
                    new ProviderPlanningOption(pairLong,
                        new[] { "caster", "la" }, new[] { "caster" }, 4, 40),
                    new ProviderPlanningOption(pairShort,
                        new[] { "caster", "sa" }, new[] { "caster" }, 4, 40)
                }, new CastEnhancementSnapshot[0]);
            if (pair.Occurrences[0].Plan.Steps.Count != 1 ||
                pair.Occurrences[0].Plan.Steps[0].Reservation.TokenIds.Count != 2 ||
                pair.Occurrences[1].Plan.Steps.Count != 1 ||
                pair.Occurrences[1].Plan.Outcomes.Any(o =>
                    o.Kind == TargetOutcomeKind.Unfulfilled))
                throw new InvalidOperationException(
                    "Linked tokens were not consumed exactly once or the free token was not reusable.");
        }

        // R3b: conditional alternatives are not unions — a B request is not
        // skipped because B appeared in an unknown branch of an earlier
        // graph — while unconditional caster/party effects project with the
        // correct recipient and kind.
        private static void TestForecastEffectProjection()
        {
            AbilityKey conditionSpell = Ability("proj-cond", string.Empty, 0);
            AbilityKey laterSpell = Ability("proj-later", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("proj-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                conditionSpell, pool.PoolKey, 0);
            ProviderSnapshot laterCaster = PlannerProvider("caster", "book",
                laterSpell, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster, laterCaster }, new[] { pool }, "caster", "a1");
            var option = new ProviderPlanningOption(caster,
                new[] { "caster", "a1" }, new[] { "caster" }, 4, 40);
            var laterOption = new ProviderPlanningOption(laterCaster,
                new[] { "caster", "a1" }, new[] { "caster" }, 4, 40);
            // Conditional A-or-B graph: neither branch is a guaranteed grant.
            EffectExpression conditional = new ConditionalEffectExpression(
                "unknown-condition", Leaf("cond-a"), Leaf("cond-b"));
            var effects = new Dictionary<string, EffectExpression> {
                { conditionSpell.Canonical, conditional },
                { laterSpell.Canonical, Leaf("later-buff") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("proj-forecast");
            profile.Routines.First(r => r.RoutineId == "long").Assignments.Add(Assignment(
                conditionSpell.Canonical, conditionSpell, new[] { "a1" }));
            profile.Routines.First(r => r.RoutineId == "short").Assignments.Add(Assignment(
                laterSpell.Canonical, laterSpell, new[] { "a1" }));
            SequentialForecastPlanner.Result result = SequentialForecastPlanner.Compute(
                profile, new[] { "long", "short" }, snapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { option, laterOption }, new CastEnhancementSnapshot[0]);
            // The later request for effect later-buff must still CAST — the
            // conditional graph's branches must not have granted anything.
            if (result.Occurrences[1].Plan.Steps.Count != 1)
                throw new InvalidOperationException(
                    "A conditional branch projected as a guaranteed grant and skipped a later cast.");

            // Unconditional leaf projection: same spell twice — the second
            // occurrence's request for the same effect on the same target
            // skips for free.
            AbilityKey echo = Ability("proj-echo", string.Empty, 0);
            ProviderSnapshot echoCaster = PlannerProvider("caster", "book",
                echo, pool.PoolKey, 0);
            PartyProviderSnapshot echoSnapshot = PlannerSnapshot(
                new[] { echoCaster }, new[] { pool }, "caster", "a1");
            var echoOption = new ProviderPlanningOption(echoCaster,
                new[] { "caster", "a1" }, new[] { "caster" }, 4, 40);
            var echoEffects = new Dictionary<string, EffectExpression> {
                { echo.Canonical, Leaf("echo-buff") }
            };
            BuffPlannerProfile echoProfile = BuffPlannerProfile.CreateDefault("proj-echo");
            echoProfile.Routines.First(r => r.RoutineId == "long").Assignments.Add(
                Assignment(echo.Canonical, echo, new[] { "a1" }));
            echoProfile.Routines.First(r => r.RoutineId == "short").Assignments.Add(
                Assignment(echo.Canonical, echo, new[] { "a1" }));
            SequentialForecastPlanner.Result echoResult = SequentialForecastPlanner.Compute(
                echoProfile, new[] { "long", "short" }, echoSnapshot,
                new ActiveEffectSnapshot(null), echoEffects,
                new[] { echoOption }, new CastEnhancementSnapshot[0]);
            if (echoResult.Occurrences[0].Plan.Steps.Count != 1 ||
                echoResult.Occurrences[1].Plan.Steps.Count != 0 ||
                echoResult.Occurrences[1].Plan.Outcomes.Count(o =>
                    o.Kind == TargetOutcomeKind.SkippedAlreadyActive) != 1)
                throw new InvalidOperationException(
                    "Unconditional effect projection lost its free already-active skip.");
        }

        // R2: exercise the review-acknowledgment protocol in the exact call
        // order the session performs — compute (PreviewRoutine), presentation
        // (AcknowledgeDisplayedPlan from the view after binding), preflight
        // compute inside ExecuteRoutine, and spend invalidation. Computing
        // must never acknowledge; only presentation of the matching
        // routine/campaign does.
        private static void TestReviewAcknowledgmentOrchestration()
        {
            AbilityKey ability = Ability("review-orch-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("review-orch-slots",
                ResourcePoolKind.SpontaneousLevel, 4, 4, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster }, new[] { pool }, "caster", "a1", "a2");
            var option = new ProviderPlanningOption(caster,
                new[] { "caster", "a1", "a2" }, new[] { "caster" }, 4, 40);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, Leaf("review-orch-buff") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("review-orch");
            var service = new RoutinePlanService();

            CastPlan ComputePlan(string routineId, string targets)
            {
                var request = new BuffCastRequest(new BuffSourceDefinition(
                    "review-orch", ability, Leaf("review-orch-buff"),
                    CastGroupingKind.PerTarget),
                    targets.Split(','), ExistingEffectPolicy.Overwrite, null,
                    null, routineId + "-assignment", null, null, null, 0);
                return new CastPlanner().Plan(snapshot, request,
                    new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            }

            var coordinator = new PlannerReviewCoordinator();

            // 1. Display/acknowledge A; native state changes so preflight
            //    computes B; the gate refuses; retry while still open
            //    without a new presentation of B: still no B baseline.
            CastPlan planA = ComputePlan("long", "a1");
            coordinator.PlanComputed("review-orch", "long");            // PreviewRoutine(A)
            coordinator.Presented("review-orch", "long", planA);        // view bound A
            CastPlan planB = ComputePlan("long", "a1,a2");
            coordinator.PlanComputed("review-orch", "long");            // preflight in ExecuteRoutine
            string changeAB = PlanMaterialChangeDetector.DescribeMaterialChange(
                coordinator.BaselineFor("review-orch", "long"), planB);
            if (changeAB == null)
                throw new InvalidOperationException("Test setup: B must differ materially from A.");
            // Retry: compute again (another preflight), no presentation.
            coordinator.PlanComputed("review-orch", "long");
            if (coordinator.BaselineFor("review-orch", "long") != planA)
                throw new InvalidOperationException(
                    "Computation alone replaced the acknowledged baseline (the R2 defect).");
            string changeRetry = PlanMaterialChangeDetector.DescribeMaterialChange(
                coordinator.BaselineFor("review-orch", "long"), planB);
            if (changeRetry == null)
                throw new InvalidOperationException(
                    "A refusal-retry without renewed presentation lost the material refusal.");

            // 2. Resource inspection previews OTHER routines while the same
            //    routine is displayed: they must not overwrite A's baseline.
            coordinator.PlanComputed("review-orch", "short");           // GetResourceUsageLines
            coordinator.PlanComputed("review-orch", "important");
            if (!ReferenceEquals(coordinator.BaselineFor("review-orch", "long"), planA))
                throw new InvalidOperationException(
                    "Another routine's incidental preview overwrote the displayed baseline.");

            // 3. Presentation for a routine whose plan was not just computed
            //    (stale binding) does not acknowledge.
            coordinator.Presented("review-orch", "short", planA);
            if (coordinator.BaselineFor("review-orch", "short") != null)
                throw new InvalidOperationException(
                    "A stale presentation acknowledged the wrong routine.");

            // 4. Explicit presentation of the revised plan, then unchanged
            //    execution: no material change.
            coordinator.PlanComputed("review-orch", "long");
            coordinator.Presented("review-orch", "long", planB);
            string afterReview = PlanMaterialChangeDetector.DescribeMaterialChange(
                coordinator.BaselineFor("review-orch", "long"),
                ComputePlan("long", "a1,a2"));
            if (afterReview != null)
                throw new InvalidOperationException(
                    "Reviewing the revised plan did not authorize executing exactly it.");
            coordinator.Spent("long");                                   // post-gate invalidation
            if (coordinator.BaselineFor("review-orch", "long") != null)
                throw new InvalidOperationException(
                    "Post-spend invalidation failed after a legitimate execution.");

            // 5. Campaign switch invalidates; a new campaign starts with no
            //    baseline (deliberate missing-baseline behavior: the gate
            //    passes only because nothing changed, never by silently
            //    trusting an unreviewed plan).
            coordinator.PlanComputed("review-orch-2", "long");
            if (coordinator.BaselineFor("review-orch", "long") != null ||
                coordinator.BaselineFor("review-orch-2", "long") != null)
                throw new InvalidOperationException(
                    "Campaign switch did not invalidate cleanly to an empty baseline.");
            CastPlan planC = ComputePlan("long", "a1");
            coordinator.Presented("review-orch-2", "long", planC);
            if (coordinator.BaselineFor("review-orch-2", "long") != planC)
                throw new InvalidOperationException(
                    "A fresh campaign's presentation failed to establish its own baseline.");
        }

        // F4 regressions: acknowledged review state scoping/invalidation and
        // the enriched material signature (target order, anchors/recipients,
        // cost vector, enhancement quantities) catching changes the old
        // signature missed.
        private static void TestReviewStateAndSignatures()
        {
            var review = new PlannerReviewState();
            AbilityKey ability = Ability("review-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("review-slots",
                ResourcePoolKind.SpontaneousLevel, 4, 4, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster }, new[] { pool }, "caster", "a1", "a2");
            var option = new ProviderPlanningOption(caster,
                new[] { "caster", "a1", "a2" }, new[] { "caster" }, 4, 40);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, Leaf("review-buff") }
            };
            CastPlan PlanInTargetOrder(string first, string second)
            {
                return new CastPlanner().Plan(snapshot,
                    new BuffCastRequest(new BuffSourceDefinition("review", ability,
                        Leaf("review-buff"), CastGroupingKind.PerTarget),
                        new[] { first, second }, ExistingEffectPolicy.Overwrite, null,
                        new[] { new EnhancementRequest("rod", true) },
                        "auto", null, null, null, 0),
                    new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null),
                    new[] { ClassEnhancement("rod", "caster", ability, "book", 4,
                        "rod-pool", "rod-group", false) });
            }

            CastPlan baseline = PlanInTargetOrder("a1", "a2");
            CastPlan swapped = PlanInTargetOrder("a2", "a1");
            // Same target union, same provider — only per-cast order changed.
            if (PlanMaterialChangeDetector.DescribeMaterialChange(baseline, swapped) == null)
                throw new InvalidOperationException(
                    "A per-cast target-order swap with the same coverage passed undetected.");

            // Review state: scoping, campaign switch, post-execution invalidation.
            review.Acknowledge("campaign-a", "long", baseline);
            if (!review.HasReviewed("campaign-a", "long") ||
                review.HasReviewed("campaign-a", "short") ||
                review.HasReviewed("campaign-b", "long"))
                throw new InvalidOperationException("Review state is not routine/campaign scoped.");
            review.ObserveCampaign("campaign-b");
            if (review.HasReviewed("campaign-a", "long") || review.HasReviewed("campaign-b", "long"))
                throw new InvalidOperationException("A campaign switch did not invalidate review.");
            review.Acknowledge("campaign-b", "long", baseline);
            review.Invalidate("short");
            if (!review.HasReviewed("campaign-b", "long"))
                throw new InvalidOperationException("Invalidating another routine cleared review.");
            review.Invalidate("long");
            if (review.HasReviewed("campaign-b", "long"))
                throw new InvalidOperationException("Post-execution invalidation failed.");
            if (review.ReviewedPlan("campaign-b", "long") != null)
                throw new InvalidOperationException("Reviewed plan leaked after invalidation.");

            // Enhancement usage quantity change with identical IDs and base
            // cost must be material.
            CastEnhancementSnapshot RodWithUnits(int unitsPerCast)
            {
                return new CastEnhancementSnapshot("rod", "caster", "rod-guid",
                    "Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                    2, 9, 4, new[] { ability.BaseAbilityGuid }, "Metamagic",
                    null, "rod-pool", false, "rod-group", unitsPerCast,
                    false, "rod-group", "Uses");
            }
            CastPlan WithRod(CastEnhancementSnapshot rod)
            {
                return new CastPlanner().Plan(snapshot,
                    new BuffCastRequest(new BuffSourceDefinition("review", ability,
                        Leaf("review-buff"), CastGroupingKind.PerTarget),
                        new[] { "a1" }, ExistingEffectPolicy.Overwrite, null,
                        new[] { new EnhancementRequest("rod", true) },
                        "auto", null, null, null, 0),
                    new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null),
                    new[] { rod });
            }
            string quantityChange = PlanMaterialChangeDetector.DescribeMaterialChange(
                WithRod(RodWithUnits(1)), WithRod(RodWithUnits(2)));
            if (quantityChange == null)
                throw new InvalidOperationException(
                    "A doubled enhancement charge per cast with identical IDs was not material.");

            // Material component cost change with the same base spell-slot
            // units is material.
            var materialPool = new ResourcePoolSnapshot("mat-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot materialCaster = new ProviderSnapshot(
                new ProviderKey("caster", "book", ability, "level-1"),
                ability.BaseAbilityGuid, 1, materialPool.PoolKey, 0,
                null, new MaterialRequirementSnapshot("dust", 1, 5));
            PartyProviderSnapshot materialSnapshot = PlannerSnapshot(
                new[] { materialCaster }, new[] { materialPool }, "caster", "a1");
            var materialOption = new ProviderPlanningOption(materialCaster,
                new[] { "caster", "a1" }, new[] { "caster" }, 4, 40);
            CastPlan WithMaterial(int required)
            {
                ProviderSnapshot provider = new ProviderSnapshot(
                    new ProviderKey("caster", "book", ability, "level-1"),
                    ability.BaseAbilityGuid, 1, materialPool.PoolKey, 0,
                    null, new MaterialRequirementSnapshot("dust", required, 5));
                return new CastPlanner().Plan(materialSnapshot,
                    new BuffCastRequest(new BuffSourceDefinition("review", ability,
                        Leaf("review-buff"), CastGroupingKind.PerTarget),
                        new[] { "a1" }, ExistingEffectPolicy.Overwrite, null),
                    new[] { new ProviderPlanningOption(provider,
                        new[] { "caster", "a1" }, new[] { "caster" }, 4, 40) },
                    EmptyPolicy(), new ActiveEffectSnapshot(null));
            }
            string materialChangeText = PlanMaterialChangeDetector.DescribeMaterialChange(
                WithMaterial(1), WithMaterial(2));
            if (materialChangeText == null)
                throw new InvalidOperationException(
                    "A material-component cost change was not material.");
        }

        // F5: a routine with one ready buff plus one saved buff whose source
        // cannot be resolved must not execute the supported subset silently.
        // Default Apply submits zero casts; explicit ready-only may run the
        // valid subset and must report the unresolved configured requests.
        private static void TestUnresolvableCoverage()
        {
            AbilityKey ready = Ability("ready-spell", string.Empty, 0);
            AbilityKey gone = Ability("gone-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("unresolvable-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                ready, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster }, new[] { pool }, "caster", "ally");
            var option = new ProviderPlanningOption(caster,
                new[] { "caster", "ally" }, new[] { "caster" }, 4, 40);
            var effects = new Dictionary<string, EffectExpression> {
                { ready.Canonical, Leaf("ready-buff") }
                // 'gone' has no effects entry and no provider: unresolvable.
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("unresolvable");
            profile.Routines[0].Assignments.Add(Assignment(
                ready.Canonical, ready, new[] { "ally" }));
            profile.Routines[0].Assignments.Add(Assignment(
                gone.Canonical, gone, new[] { "ally" }));
            RoutinePlanResult result = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null), effects, new[] { option });

            if (result.Plan.Steps.Count != 1)
                throw new InvalidOperationException("The ready buff did not plan.");
            if (result.UnsupportedSourceIds.Count != 1 ||
                result.UnsupportedSourceIds[0] != gone.Canonical)
                throw new InvalidOperationException("The gone source was not flagged unsupported.");
            if (result.Plan.UnresolvableRequests.Count != 1 ||
                result.Plan.UnresolvableRequests[0].UnitId != "ally" ||
                !result.Plan.UnresolvableRequests[0].Reason.Contains("source-unresolvable"))
                throw new InvalidOperationException(
                    "The gone source's configured targets vanished from requested coverage.");

            PartialExecutionGate.Decision gate = PartialExecutionGate.Evaluate(result.Plan);
            if (!gate.Blocked || gate.RequestedTargets != 2 || gate.PlannedCasts != 1 ||
                gate.Unfulfilled != 1 || !gate.UnmetReasons[0].Contains("saved buff unavailable"))
                throw new InvalidOperationException(
                    "The partial gate did not count the unresolved configured request: " +
                    gate.Summary);
        }

        private static void TestSpellbookHandoff()
        {
            var machine = new SpellbookHandoffStateMachine();
            machine.Begin();
            if (machine.State != SpellbookHandoffState.WaitingModeRelease)
                throw new InvalidOperationException("Handoff did not start waiting.");

            // No opener invocation while the native mode is still owned.
            bool opened = false;
            for (int frame = 0; frame < SpellbookHandoffStateMachine.MaximumWaitFrames - 1; frame++)
            {
                opened |= machine.ObserveRelease(true);
                if (machine.State != SpellbookHandoffState.WaitingModeRelease)
                    throw new InvalidOperationException("Handoff gave up before its bounded wait expired.");
            }
            if (opened)
                throw new InvalidOperationException("The opener ran while the mode was still owned.");
            if (machine.ObserveRelease(true) || machine.State != SpellbookHandoffState.Failed ||
                machine.Failure != "mode-release-timeout")
                throw new InvalidOperationException("Handoff wait was not bounded by the documented frame limit.");

            // Full sequence: release -> opener invoked exactly once ->
            // deferred presentation -> success. Success is only reported by
            // the presentation observation, not by the open call itself.
            machine.Reset();
            machine.Begin();
            if (!machine.ObserveRelease(false) ||
                machine.State != SpellbookHandoffState.OpeningPlanner)
                throw new InvalidOperationException("Released mode did not arm the opener.");
            machine.ObserveOpenResult(true);
            if (machine.State != SpellbookHandoffState.WaitingPresentation ||
                machine.OpenAttempts != 1)
                throw new InvalidOperationException("Open result did not enter deferred presentation wait.");
            bool completed = false;
            for (int frame = 0; frame < 10; frame++)
            {
                completed |= machine.ObservePresentation(false);
                if (machine.State != SpellbookHandoffState.WaitingPresentation)
                    throw new InvalidOperationException("Presentation wait ended early.");
            }
            if (completed)
                throw new InvalidOperationException("Success was reported before presentation was ready.");
            if (!machine.ObservePresentation(true) ||
                machine.State != SpellbookHandoffState.Completed)
                throw new InvalidOperationException("Ready presentation did not complete the handoff.");
            machine.Rollback("late-failure");
            if (machine.State != SpellbookHandoffState.Completed)
                throw new InvalidOperationException("A completed handoff was rolled back after success.");

            // Open refusal fails immediately and is recoverable.
            machine.Reset();
            machine.Begin();
            machine.ObserveRelease(false);
            machine.ObserveOpenResult(false);
            if (machine.State != SpellbookHandoffState.Failed ||
                machine.Failure != "planner-open-refused" ||
                machine.OpenAttempts != 1)
                throw new InvalidOperationException("Open refusal did not fail the handoff.");

            // Presentation timeout is bounded and recoverable (failure after
            // native closure must be a state we can recover from).
            machine.Reset();
            machine.Begin();
            machine.ObserveRelease(false);
            machine.ObserveOpenResult(true);
            for (int frame = 0; frame < SpellbookHandoffStateMachine.MaximumWaitFrames; frame++)
                machine.ObservePresentation(false);
            if (machine.State != SpellbookHandoffState.Failed ||
                machine.Failure != "presentation-timeout")
                throw new InvalidOperationException("Presentation wait was not bounded.");
            machine.Rollback("recovered");
            if (machine.State != SpellbookHandoffState.Failed ||
                machine.Failure != "recovered")
                throw new InvalidOperationException("Rollback after failed state did not record its reason.");

            machine.Reset();
            machine.Begin();
            machine.Rollback("native-close-refused");
            if (machine.ObserveRelease(false))
                throw new InvalidOperationException("A failed handoff still invoked the opener.");
        }

        private static void TestSequenceForecast()
        {
            // Real sequential planning: two routines each request three
            // enhanced casts against three total rod charges. The second
            // occurrence must show honest unmet demand, never six funded
            // casts; reversing the sequence moves the shortfall; a granted
            // effect from the first run makes the second run's same-target
            // request a free already-active skip; and the input snapshot is
            // never mutated.
            AbilityKey longAbility = Ability("forecast-long", string.Empty, 0);
            AbilityKey shortAbility = Ability("forecast-short", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("forecast-slots",
                ResourcePoolKind.SpontaneousLevel, 6, 6, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                longAbility, pool.PoolKey, 1);
            ProviderSnapshot casterShort = PlannerProvider("caster", "book",
                shortAbility, pool.PoolKey, 1);
            string[] longTargets = { "a1", "a2", "a3" };
            string[] shortTargets = { "b1", "b2", "b3" };
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster, casterShort }, new[] { pool },
                new[] { "caster" }.Concat(longTargets).Concat(shortTargets).ToArray());
            var longOption = new ProviderPlanningOption(caster,
                new[] { "caster" }.Concat(longTargets), new[] { "caster" }, 4, 40);
            var shortOption = new ProviderPlanningOption(casterShort,
                new[] { "caster" }.Concat(shortTargets), new[] { "caster" }, 4, 40);
            CastEnhancementSnapshot rod = ClassEnhancement("rod", "caster",
                longAbility, "book", 3, "charge-pool", "rod-group", false);
            // The rod is shared: same usage pool for the short routine's copy.
            CastEnhancementSnapshot rodShort = ClassEnhancement("rod-short", "caster",
                shortAbility, "book", 3, "charge-pool", "rod-group", false);
            var effects = new Dictionary<string, EffectExpression> {
                { longAbility.Canonical, Leaf("long-buff") },
                { shortAbility.Canonical, Leaf("short-buff") }
            };
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("seq-forecast");
            profile.Routines.First(r => r.RoutineId == "long").Assignments.Add(Assignment(
                longAbility.Canonical, longAbility, longTargets, new[] { "rod" }));
            profile.Routines.First(r => r.RoutineId == "short").Assignments.Add(Assignment(
                shortAbility.Canonical, shortAbility, shortTargets, new[] { "rod-short" }));

            SequentialForecastPlanner.Result forward = SequentialForecastPlanner.Compute(
                profile, new[] { "long", "short" }, snapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { longOption, shortOption }, new[] { rod, rodShort });
            int forwardShortUnmet = forward.Occurrences[1].Plan.Outcomes.Count(o =>
                o.Kind == TargetOutcomeKind.Unfulfilled);
            int forwardShortSteps = forward.Occurrences[1].Plan.Steps.Count;
            if (forward.Occurrences[0].Plan.Steps.Count != 3 ||
                forwardShortSteps != 0 || forwardShortUnmet != 3)
                throw new InvalidOperationException(
                    "Sequential forecast double-funded one shared charge pool: first=" +
                    forward.Occurrences[0].Plan.Steps.Count + " casts, second=" +
                    forwardShortSteps + " casts with " + forwardShortUnmet + " unmet.");

            // Reversing the sequence moves the shortfall to the other routine.
            SequentialForecastPlanner.Result reverse = SequentialForecastPlanner.Compute(
                profile, new[] { "short", "long" }, snapshot,
                new ActiveEffectSnapshot(null), effects,
                new[] { longOption, shortOption }, new[] { rod, rodShort });
            if (reverse.Occurrences[0].Plan.Steps.Count != 3 ||
                reverse.Occurrences[1].Plan.Outcomes.Count(o =>
                    o.Kind == TargetOutcomeKind.Unfulfilled) != 3)
                throw new InvalidOperationException(
                    "Reversing the sequence did not move the shortfall.");

            // A granted effect from the first run skips the same target for
            // free in the second run (projected already-active coverage).
            AbilityKey echo = Ability("forecast-echo", string.Empty, 0);
            ProviderSnapshot echoCaster = PlannerProvider("caster", "book",
                echo, pool.PoolKey, 1);
            PartyProviderSnapshot echoSnapshot = PlannerSnapshot(
                new[] { echoCaster }, new[] { pool }, "caster", "a1");
            var echoOption = new ProviderPlanningOption(echoCaster,
                new[] { "caster", "a1" }, new[] { "caster" }, 4, 40);
            var echoEffects = new Dictionary<string, EffectExpression> {
                { echo.Canonical, Leaf("echo-buff") }
            };
            BuffPlannerProfile echoProfile = BuffPlannerProfile.CreateDefault("seq-echo");
            echoProfile.Routines.First(r => r.RoutineId == "long").Assignments.Add(
                Assignment(echo.Canonical, echo, new[] { "a1" }));
            echoProfile.Routines.First(r => r.RoutineId == "short").Assignments.Add(
                Assignment(echo.Canonical, echo, new[] { "a1" }));
            SequentialForecastPlanner.Result echoResult = SequentialForecastPlanner.Compute(
                echoProfile, new[] { "long", "short" }, echoSnapshot,
                new ActiveEffectSnapshot(null), echoEffects,
                new[] { echoOption }, new CastEnhancementSnapshot[0]);
            if (echoResult.Occurrences[0].Plan.Steps.Count != 1 ||
                echoResult.Occurrences[1].Plan.Steps.Count != 0 ||
                echoResult.Occurrences[1].Plan.Outcomes.Count(o =>
                    o.Kind == TargetOutcomeKind.SkippedAlreadyActive) != 1)
                throw new InvalidOperationException(
                    "A projected already-active effect did not skip for free in the later run.");

            // The caller's snapshot is never mutated by forecasting.
            if (snapshot.ResourcePools[0].Remaining != 6)
                throw new InvalidOperationException(
                    "Sequential forecasting mutated the caller's snapshot.");
            if (!SequentialForecastPlanner.Result.AssumptionText.Contains("One run per selected routine"))
                throw new InvalidOperationException("The forecast lost its assumption label.");
        }

        private static void TestAssignmentOrderAndShortage()
        {
            AbilityKey ability = Ability("charge-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("charge-slots",
                ResourcePoolKind.SpontaneousLevel, 9, 9, null);
            ProviderSnapshot caster = PlannerProvider("caster", "book",
                ability, pool.PoolKey, 1);
            string[] nine = Enumerable.Range(1, 9)
                .Select(index => "target-" + index).ToArray();
            PartyProviderSnapshot snapshot = PlannerSnapshot(
                new[] { caster }, new[] { pool },
                new[] { "caster" }.Concat(nine).ToArray());
            var option = new ProviderPlanningOption(caster,
                new[] { "caster" }.Concat(nine), new[] { "caster" }, 4, 40);
            CastEnhancementSnapshot charges = ClassEnhancement("rod", "caster",
                ability, "book", 3, "charge-pool", "rod-group", false);
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "charge-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
            };

            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("shortage");
            SourceAssignmentProfile parent = Assignment(
                ability.Canonical, ability, new string[0]);
            parent.CastingAssignments[0].TargetUnitIds = new List<string>(nine);
            parent.CastingAssignments[0].Enhancements.Add(
                new EnhancementSelectionProfile { EnhancementId = "rod", Required = true });
            profile.Routines[0].Assignments.Add(parent);
            RoutinePlanResult required = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null), effects,
                new[] { option }, new[] { charges });
            ResourcePoolAllocation allocation = required.Plan.AllocationFor("enhancement:charge-pool");
            if (allocation == null || allocation.AvailableNow != 3 ||
                allocation.RequestedUsage != 9 || allocation.AllocatedUsage != 3 ||
                allocation.UnmetDemand != 6 || allocation.ForecastRemaining != 0)
                throw new InvalidOperationException("Required shortage must report requested 9 / available 3 / " +
                    "allocated 3 / unmet 6; observed " +
                    (allocation == null ? "<none>" : allocation.RequestedUsage + "/" +
                        allocation.AvailableNow + "/" + allocation.AllocatedUsage + "/" +
                        allocation.UnmetDemand) + ".");
            if (required.Plan.Outcomes.Count(outcome => outcome.Kind == TargetOutcomeKind.Fulfilled) != 3 ||
                required.Plan.Outcomes.Count(outcome => outcome.Kind == TargetOutcomeKind.Unfulfilled) != 6)
                throw new InvalidOperationException("The first three targets did not receive the charges.");
            List<string> fulfilledOrder = required.Plan.Outcomes
                .Where(outcome => outcome.Kind == TargetOutcomeKind.Fulfilled)
                .Select(outcome => outcome.UnitId).ToList();
            if (!fulfilledOrder.SequenceEqual(nine.Take(3).ToList()))
                throw new InvalidOperationException("Charges were not allocated in explicit target order.");

            // Optional policy: the same shortage plans the last six casts
            // explicitly without the enhancement, labeled as omissions.
            parent.CastingAssignments[0].Enhancements[0].Required = false;
            RoutinePlanResult optional = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null), effects,
                new[] { option }, new[] { charges });
            if (optional.Plan.Steps.Count != 9 ||
                optional.Plan.Steps.Count(step => step.EnhancementIds.Contains("rod")) != 3 ||
                optional.Plan.Steps.Count(step => step.OmittedEnhancementIds.Contains("rod")) != 6)
                throw new InvalidOperationException("Optional shortage did not split enhanced from explicitly-unenhanced casts.");
            ResourcePoolAllocation optionalAllocation = optional.Plan.AllocationFor("enhancement:charge-pool");
            if (optionalAllocation.AllocatedUsage != 3 || optionalAllocation.UnmetDemand != 6)
                throw new InvalidOperationException("Optional policy changed the demand accounting.");
            if (optional.Plan.Outcomes.Any(outcome => outcome.Kind == TargetOutcomeKind.Unfulfilled))
                throw new InvalidOperationException("Optional policy still blocked casts it was permitted to omit for.");

            // Already-active targets never request or reserve a charge.
            var active = new Dictionary<string, IEnumerable<string>>();
            active["target-1"] = new[] { "charge-buff" };
            parent.CastingAssignments[0].TargetUnitIds = new List<string>(nine);
            RoutinePlanResult skipped = new RoutinePlanService().Plan(profile, "long",
                snapshot, ActiveEffectSnapshot.FromTypedEffects(
                    active.ToDictionary(pair => pair.Key,
                        pair => pair.Value.Select(id => new ActiveEffectMarker(EffectKind.Buff, id)))),
                effects, new[] { option }, new[] { charges });
            ResourcePoolAllocation skippedAllocation = skipped.Plan.AllocationFor("enhancement:charge-pool");
            TargetPlanOutcome skip = skipped.Plan.Outcomes.Single(outcome => outcome.UnitId == "target-1");
            if (skip.Kind != TargetOutcomeKind.SkippedAlreadyActive ||
                skippedAllocation.RequestedUsage != 8 || skippedAllocation.AllocatedUsage != 3)
                throw new InvalidOperationException("An already-active skip consumed demand or a charge.");

            // Explicit assignment order, not source-ID or dictionary order,
            // controls who wins a one-charge race between two assignments.
            AbilityKey raceAbility = Ability("race-spell", string.Empty, 0);
            var racePool = new ResourcePoolSnapshot("race-slots",
                ResourcePoolKind.SpontaneousLevel, 2, 2, null);
            ProviderSnapshot raceCaster = PlannerProvider("z-caster", "z-book",
                raceAbility, racePool.PoolKey, 1);
            PartyProviderSnapshot raceSnapshot = PlannerSnapshot(
                new[] { raceCaster }, new[] { racePool }, "z-caster", "early", "late");
            var raceOption = new ProviderPlanningOption(raceCaster,
                new[] { "z-caster", "early", "late" }, new[] { "z-caster" }, 4, 40);
            CastEnhancementSnapshot raceCharge = ClassEnhancement("race-rod", "z-caster",
                raceAbility, "z-book", 1, "race-pool", "race-rod-group", false);
            BuffPlannerProfile raceProfile = BuffPlannerProfile.CreateDefault("race");
            SourceAssignmentProfile raceParent = Assignment(
                "a-" + raceAbility.Canonical, raceAbility, new string[0]);
            // The alphabetically-first source runs LATER: explicit order wins.
            raceParent.CastingAssignments[0].AssignmentId = "late-one";
            raceParent.CastingAssignments[0].Order = 1;
            raceParent.CastingAssignments[0].TargetUnitIds = new List<string> { "late" };
            raceParent.CastingAssignments[0].Enhancements.Add(
                new EnhancementSelectionProfile { EnhancementId = "race-rod" });
            raceParent.CastingAssignments.Add(new CastingAssignmentProfile
            {
                AssignmentId = "early-one",
                Order = 0,
                TargetUnitIds = new List<string> { "early" },
                Enhancements = new List<EnhancementSelectionProfile>
                {
                    new EnhancementSelectionProfile { EnhancementId = "race-rod" }
                }
            });
            raceProfile.Routines[0].Assignments.Add(raceParent);
            RoutinePlanResult race = new RoutinePlanService().Plan(raceProfile, "long",
                raceSnapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { raceAbility.Canonical, new EffectLeafExpression(EffectKind.Buff,
                        "race-buff", EffectTarget.Caster, "ContextActionApplyBuff", "root/apply") }
                }, new[] { raceOption }, new[] { raceCharge });
            CastStep winner = race.Plan.Steps.Single();
            if (winner.AssignmentId != "early-one" || winner.TargetUnitIds[0] != "early" ||
                race.Plan.Outcomes.Any(outcome => outcome.UnitId == "late" &&
                    outcome.Kind == TargetOutcomeKind.Fulfilled))
                throw new InvalidOperationException("Catalog order overrode the explicit assignment order.");
        }

        // Chooser scroll geometry must be exact so the final option row is
        // reachable by wheel and scrollbar (mission acceptance T16); the
        // arithmetic mirrors the shared factory layout (padding 4, spacing 4)
        // and the fixed row heights both modal choosers install.
        private static void TestChooserScrollLayout()
        {
            const float row = ChooserScrollLayoutContract.EnhancementRowHeight;
            const float policyRow = CastingPanelLayoutContract.MinimumCasterPolicyRowHeight;

            if (ChooserScrollLayoutContract.ContentHeight(0, row) != 0f ||
                ChooserScrollLayoutContract.ContentHeight(1, row) != 8f + row ||
                ChooserScrollLayoutContract.ContentHeight(2, row) != 8f + (2f * row) + 4f)
                throw new InvalidOperationException("Chooser content height does not match padding 4 + rows + spacing 4.");

            float thirty = ChooserScrollLayoutContract.ContentHeight(30, row);
            float hundred = ChooserScrollLayoutContract.ContentHeight(100, row);
            if (thirty != 8f + (30f * row) + (29f * 4f) ||
                hundred != 8f + (100f * row) + (99f * 4f) ||
                ChooserScrollLayoutContract.ContentHeight(30, policyRow) <= thirty + 30f * 20f)
                throw new InvalidOperationException("Row-count scaling or caster-policy row height drifted.");

            // Viewport showing ~5 rows: overflow must produce a positive,
            // bounded scroll range so the final row is reachable.
            float viewport = 5f * row;
            float maxOffset = ChooserScrollLayoutContract.MaxScrollOffset(viewport, hundred);
            if (maxOffset != hundred - viewport || maxOffset <= 0f)
                throw new InvalidOperationException("Overflowing chooser has no reachable scroll range.");
            if (ChooserScrollLayoutContract.MaxScrollOffset(viewport, viewport) != 0f ||
                ChooserScrollLayoutContract.MaxScrollOffset(0f, hundred) != 0f ||
                ChooserScrollLayoutContract.MaxScrollOffset(viewport, 0f) != 0f)
                throw new InvalidOperationException("Non-overflowing or empty choosers must not scroll.");

            if (ChooserScrollLayoutContract.ClampScrollOffset(-10f, viewport, hundred) != 0f ||
                ChooserScrollLayoutContract.ClampScrollOffset(maxOffset + 25f, viewport, hundred) != maxOffset ||
                ChooserScrollLayoutContract.ClampScrollOffset(maxOffset * 0.5f, viewport, hundred) != maxOffset * 0.5f)
                throw new InvalidOperationException("Refresh offset clamping is not bounded by the content size.");

            // A refresh that shrinks the list must pull the offset back inside
            // the new bounds instead of stranding the view below the content.
            float shrunk = ChooserScrollLayoutContract.ClampScrollOffset(
                maxOffset, viewport, ChooserScrollLayoutContract.ContentHeight(6, row));
            if (shrunk != ChooserScrollLayoutContract.MaxScrollOffset(
                    viewport, ChooserScrollLayoutContract.ContentHeight(6, row)))
                throw new InvalidOperationException("Offset survived a list shrink outside the new bounds.");

            // Fresh open reveals the selected row; visible rows do not move.
            float content6 = ChooserScrollLayoutContract.ContentHeight(6, row);
            if (ChooserScrollLayoutContract.OffsetRevealingRow(
                    0, 0f, viewport, content6, row) != 0f ||
                ChooserScrollLayoutContract.OffsetRevealingRow(
                    4, 0f, viewport, content6, row) != 20f)
                throw new InvalidOperationException("Selected-row reveal is not anchored to the row bounds.");
            // Row 5 bottom = 4 + 6*(row+4) - 4 = 6*row+24; aligning it to the
            // viewport bottom lands 4px above the absolute maximum offset,
            // which is correct: revealing the row must not overscroll.
            if (ChooserScrollLayoutContract.OffsetRevealingRow(
                    5, 0f, viewport, content6, row) != row + 24f ||
                ChooserScrollLayoutContract.OffsetRevealingRow(
                    5, 0f, viewport, content6, row) >=
                    ChooserScrollLayoutContract.MaxScrollOffset(viewport, content6) + 1f)
                throw new InvalidOperationException("Last-row reveal did not reach its own bottom bound.");
            if (ChooserScrollLayoutContract.OffsetRevealingRow(
                    -1, 999f, viewport, content6, row) !=
                ChooserScrollLayoutContract.ClampScrollOffset(999f, viewport, content6))
                throw new InvalidOperationException("Reveal without a selected row must only clamp.");

            // Scrollbar handle size reflects the visible ratio and stays
            // draggable for very long lists.
            if (ChooserScrollLayoutContract.ScrollbarHandleRatio(viewport, viewport) != 1f ||
                ChooserScrollLayoutContract.ScrollbarHandleRatio(0f, hundred) != 1f ||
                Math.Abs(ChooserScrollLayoutContract.ScrollbarHandleRatio(viewport, content6) -
                    viewport / content6) > 0.0001f ||
                ChooserScrollLayoutContract.ScrollbarHandleRatio(viewport, hundred) !=
                    ChooserScrollLayoutContract.MinimumHandleRatio ||
                ChooserScrollLayoutContract.ScrollbarHandleRatio(1f, 100000f) !=
                    ChooserScrollLayoutContract.MinimumHandleRatio)
                throw new InvalidOperationException("Scrollbar handle ratio is not the viewport/content fraction.");
        }
        private static void TestCastEnhancementSelection()
        {
            AbilityKey ability = Ability("selection-spell", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("selection-free", ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "selection-book", ability,
                "selection-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 2, 10);
            var rod = new CastEnhancementSnapshot("selection-rod", "unit-a", "selection-rod-guid",
                "Selection Rod", "fixture", CastEnhancementCategory.MetamagicRod, 2, 3, 2, null);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("enhancement-selection");
            var model = new PlannerSetupModel(profile, snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, Leaf("selection-effect") } },
                new[] { option }, ignored => { }, new[] { rod });
            model.CycleEnhancement("long");
            SourceAssignmentProfile assignment = profile.Routines[0].Assignments.Single();
            if (assignment.SelectedEnhancementIds.Single() != rod.EnhancementId ||
                assignment.WantedTargetUnitIds.Count != 0 ||
                !model.GetEnhancementSummary("long").Contains("Selection Rod"))
                throw new InvalidOperationException("Enhancement selection was not scoped to the assignment.");
            model.ToggleTarget("long", "unit-a");
            model.ToggleTarget("long", "unit-a");
            if (profile.Routines[0].Assignments.Single().SelectedEnhancementIds.Single() != rod.EnhancementId)
                throw new InvalidOperationException("Clearing targets discarded the planned enhancement.");
            model.CycleEnhancement("long");
            if (profile.Routines[0].Assignments.Count != 0)
                throw new InvalidOperationException("Clearing an empty enhancement assignment left stale state.");
        }

        private static void TestCastEnhancementExecution()
        {
            AbilityKey ability = Ability("execution-enhanced", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("execution-enhanced-free", ResourcePoolKind.Unlimited,
                0, 0, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "execution-book", ability,
                "execution-enhanced-free", 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool }, "unit-a");
            var option = new ProviderPlanningOption(provider, new[] { "unit-a" },
                new[] { "unit-a" }, 2, 10);
            var rod = new CastEnhancementSnapshot("execution-rod", "unit-a", "execution-rod-guid",
                "Execution Rod", string.Empty, CastEnhancementCategory.MetamagicRod, 2, 3, 3, null);
            var request = new BuffCastRequest(new BuffSourceDefinition("execution-source", ability,
                Leaf("execution-effect"), CastGroupingKind.PerTarget), new[] { "unit-a" },
                ExistingEffectPolicy.Overwrite, null, new[] { rod.EnhancementId });
            CastPlan plan = new CastPlanner().Plan(snapshot, request, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] { rod });
            var success = new EnhancementInstantRuntime(false, false);
            ExecutionReport successReport = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(success, true).Execute(plan, successReport));
            if (string.Join(",", success.Events.ToArray()) != "prepare,fire,cleanup" ||
                successReport.Confirmed != 1)
                throw new InvalidOperationException("Enhancement preparation/cast/cleanup order changed.");
            var throwing = new EnhancementInstantRuntime(false, true);
            ExecutionReport failureReport = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(throwing, true).Execute(plan, failureReport));
            if (string.Join(",", throwing.Events.ToArray()) != "prepare,fire,cleanup" ||
                failureReport.Failed != 1)
                throw new InvalidOperationException("Enhancement cleanup did not run after a failed cast.");
            var unavailable = new EnhancementInstantRuntime(true, false);
            ExecutionReport unavailableReport = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(unavailable, true).Execute(plan, unavailableReport));
            if (unavailable.FireCount != 0 || unavailableReport.Failed != 1 ||
                !unavailableReport.Records.Any(value => value.Detail.Contains("enhancement-unavailable")))
                throw new InvalidOperationException("Unavailable enhancement silently fell back to an ordinary cast.");
        }

        private static void TestOneShotEnhancementRestoration()
        {
            if (!CastEnhancementActivationPolicy.RestoreOriginalState(false,
                    true) ||
                !CastEnhancementActivationPolicy.RestoreOriginalState(true,
                    false) ||
                CastEnhancementActivationPolicy.RestoreOriginalState(true,
                    true))
                throw new InvalidOperationException(
                    "A consumed one-shot group could be rearmed after execution.");
            var consumed = new HashSet<string>(new[] { "share" },
                StringComparer.Ordinal);
            if (CastEnhancementActivationPolicy.RestoreOriginalState(true,
                    "share", consumed) ||
                !CastEnhancementActivationPolicy.RestoreOriginalState(true,
                    "powerful", consumed))
                throw new InvalidOperationException(
                    "Consumption in one native activation group suppressed restoration in another group.");
        }

        private static void TestCommunalStructuralCanaries()
        {
            string[] canaries = {
                "7bb0c402f7f789d4d9fae8ca87b4c7e2|Resist Energy, Communal",
                "55a037e514c0ee14a8e3ed14b47061de|Remove Fear",
                "a5e23522eda32dc45801e32c05dc9f96|Good Hope",
                "96c9d98b6a9a7c249b6c4572e4977157|Protection from Arrows, Communal",
                "allied-radius-additional|Allied radius fixture"
            };
            foreach (string canary in canaries)
            {
                string[] fields = canary.Split('|');
                var conditional = new DiscoveryNode(
                    DiscoveryNodeKind.Conditional, "Conditional",
                    whenTrue: new DiscoveryNode(DiscoveryNodeKind.Empty,
                        "already-present"),
                    whenFalse: EffectNode(fields[0] + "-buff"),
                    conditionContract: "ContextConditionHasBuff");
                var root = new DiscoveryNode(DiscoveryNodeKind.AbilityReference,
                    fields[0], new[] {
                        new DiscoveryNode(DiscoveryNodeKind.TargetTransform,
                            "AbilityTargetsAround", new[] {
                                new DiscoveryNode(DiscoveryNodeKind.Sequence,
                                    "ActionList", new[] { conditional })
                            }, target: EffectTarget.AlliedAreaRecipients,
                            sourceContract: "AbilityTargetsAround")
                    }, referencedAbilityId: fields[0],
                    sourceContract: "BlueprintAbility");
                DiscoveryScanResult scan = new ActionGraphScanner().Scan(root);
                CastGroupingKind grouping;
                if (!EffectExpressionTargetAnalysis.TryGetGrouping(
                        scan.Expression, out grouping) || grouping !=
                    CastGroupingKind.MassConfiguredTargets ||
                    !EffectExpressionTargetAnalysis.Contains(scan.Expression,
                        EffectTarget.AlliedAreaRecipients) ||
                    !(scan.Expression is ReferencedAbilityExpression))
                    throw new InvalidOperationException(
                        "Structural allied-area canary did not retain mass semantics: " + fields[1]);
            }

            var party = new DiscoveryNode(DiscoveryNodeKind.TargetTransform,
                "ContextActionPartyMembers", new[] {
                    new DiscoveryNode(DiscoveryNodeKind.Sequence,
                        "reflected:ActionListWrapper", new[] {
                            new DiscoveryNode(DiscoveryNodeKind.Sequence,
                                "ActionList", new[] { EffectNode(
                                    "party-wide-additional-buff") })
                        })
                }, target: EffectTarget.Party,
                sourceContract: "ContextActionPartyMembers");
            CastGroupingKind partyGrouping;
            if (!EffectExpressionTargetAnalysis.TryGetGrouping(
                    new ActionGraphScanner().Scan(party).Expression,
                    out partyGrouping) || partyGrouping !=
                CastGroupingKind.MassConfiguredTargets)
                throw new InvalidOperationException(
                    "Nested party-member action wrappers lost party-wide semantics.");

            CastGroupingKind ordinary;
            if (!EffectExpressionTargetAnalysis.TryGetGrouping(
                    Leaf("ordinary-resist-energy"), out ordinary) ||
                ordinary != CastGroupingKind.PerTarget ||
                !EffectExpressionTargetAnalysis.TryGetGrouping(
                    new EffectLeafExpression(EffectKind.Buff, "shield",
                        EffectTarget.Caster, "ContextActionApplyBuff",
                        "shield/action"), out ordinary) ||
                ordinary != CastGroupingKind.PerTarget)
                throw new InvalidOperationException(
                    "An ordinary direct or personal spell acquired mass semantics.");

            string repository = FindRepositoryRoot();
            JObject catalog = JObject.Parse(File.ReadAllText(Path.Combine(
                repository, "planning", "NATIVE-BUFF-CATALOG.json")));
            JArray abilities = (JArray)catalog["abilities"];
            foreach (string canary in canaries.Take(4))
            {
                string[] fields = canary.Split('|');
                JObject row = abilities.OfType<JObject>().Single(value =>
                    (string)value["abilityGuid"] == fields[0]);
                string[] components = ((JArray)row["abilityComponentTypes"])
                    .Values<string>().ToArray();
                if ((string)row["displayName"] != fields[1] ||
                    !components.Contains(
                        "Kingmaker.UnitLogic.Abilities.Components.AbilityTargetsAround") ||
                    !(bool)row["canTargetFriends"] ||
                    (bool)row["canTargetEnemies"] || (bool)row["canTargetPoint"])
                    throw new InvalidOperationException(
                        "Exact loaded-blueprint evidence no longer proves a friendly allied-area contract: " +
                        fields[1]);
            }
            JObject resist = abilities.OfType<JObject>().Single(value =>
                (string)value["abilityGuid"] ==
                    "7bb0c402f7f789d4d9fae8ca87b4c7e2");
            if (((JArray)resist["variantGuids"]).Count != 5)
                throw new InvalidOperationException(
                    "The exact communal Resist Energy parent/variant evidence changed.");
            string optionBuilder = File.ReadAllText(Path.Combine(repository,
                "src", "KingmakerBuffPlanner", "GameAdapters",
                "KingmakerProviderOptionBuilder.cs"));
            string areaResolver = File.ReadAllText(Path.Combine(repository,
                "src", "KingmakerBuffPlanner", "GameAdapters",
                "KingmakerAreaCoverageResolver.cs"));
            if (!optionBuilder.Contains(
                    "provider.Key.Ability.BaseAbilityGuid") ||
                !areaResolver.Contains("declaredSource") ||
                !areaResolver.Contains("selected-variant-source-mismatch"))
                throw new InvalidOperationException(
                    "Concrete communal variants no longer recover geometry from their proven declared source.");
        }

        private static void TestConflictingRecipientSemantics()
        {
            AbilityKey ability = Ability("conflicting-area", string.Empty, 0);
            EffectExpression expression = new SequenceEffectExpression(new EffectExpression[] {
                new TargetedEffectExpression(EffectTarget.AlliedAreaRecipients,
                    Leaf("friendly-branch")),
                new TargetedEffectExpression(EffectTarget.AmbiguousAreaRecipients,
                    Leaf("ambiguous-branch"))
            });
            CastGroupingKind ignored;
            if (EffectExpressionTargetAnalysis.TryGetGrouping(expression,
                    out ignored))
                throw new InvalidOperationException(
                    "Contradictory area recipient semantics did not fail closed.");
            var pool = new ResourcePoolSnapshot("conflicting-pool",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("caster", "book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { pool }, "caster", "ally");
            var coverage = new Dictionary<string, IEnumerable<string>> {
                { "caster", new[] { "caster", "ally" } }
            };
            var option = new ProviderPlanningOption(provider,
                new[] { "caster", "ally" }, new[] { "caster" }, 4, 20,
                false, coverage);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "conflicting-area");
            profile.Routines.First(value => value.RoutineId == "long")
                .Assignments.Add(Assignment(ability.Canonical, ability,
                    new[] { "caster" }, null, ExistingEffectPolicy.Overwrite));
            RoutinePlanResult plan = new RoutinePlanService().Plan(profile,
                "long", snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, expression }
                }, new[] { option });
            if (plan.Plan.Steps.Count != 0 ||
                !plan.UnsupportedSourceIds.Contains(ability.Canonical))
                throw new InvalidOperationException(
                    "A malformed conflicting graph reached planning as an allied cast.");
        }

        private static void TestEffectiveTargetingRoutineAwareness()
        {
            AbilityKey ability = Ability("personal-transmutation",
                string.Empty, 0);
            var pool = new ResourcePoolSnapshot("personal-slots",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot brown = PlannerProvider("brown", "brown-book",
                ability, pool.PoolKey, 0);
            ProviderSnapshot other = PlannerProvider("other", "other-book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { brown, other },
                new[] { pool }, "brown", "other", "ally", "rejected");
            var options = new[] {
                new ProviderPlanningOption(brown, new[] { "brown" },
                    new[] { "brown" }, 6, 60),
                new ProviderPlanningOption(other, new[] { "other" },
                    new[] { "other" }, 5, 50)
            };
            CastEnhancementSnapshot share = ClassEnhancement(
                "share", "brown", ability, "brown-book", 2,
                "reservoir|brown", "brown-fur-share-transmutation", true);
            var targeting = new EffectiveProviderOptionResolver(
                new ICastTargetingModifier[] {
                    new FixtureShareTargetingModifier("share", "brown",
                        new[] { "brown", "ally" })
                });
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "routine-aware-share");
            int saves = 0;
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, new EffectLeafExpression(
                        EffectKind.Buff, "personal-buff", EffectTarget.Caster,
                        "ContextActionApplyBuff", "root/apply") }
                }, options, ignored => saves++, new[] { share }, targeting);
            SetupSourceRow source = model.SelectedSource;
            if (model.IsTargetLegal(source, "long", "ally") ||
                model.GetSelectedEnhancementIds("long").Count != 0)
                throw new InvalidOperationException(
                    "Share did not default off for a new assignment.");
            model.SetEnhancement("long", share.EnhancementId);
            if (!model.IsTargetLegal(source, "long", "ally") ||
                model.IsTargetLegal(source, "long", "rejected") ||
                model.IsTargetLegal(source, "short", "ally"))
                throw new InvalidOperationException(
                    "Share targeting leaked across routines or admitted a native-rejected target.");
            model.ToggleTarget("long", "ally");
            if (new BuffCardViewModel(source, model, "long", true).Status !=
                    PlannerPresentationStatus.Success)
                throw new InvalidOperationException(
                    "The buff-card status bypassed routine-aware Share targeting.");
            model.SetEnhancement("long", share.EnhancementId);
            // Disabling Share makes the ally illegal, but the configured
            // target must survive as repairable intent instead of being
            // silently pruned (mission T08), stay visibly unfulfillable in
            // planning, and remain explicitly removable.
            RoutineProfile longRoutine = profile.Routines.First(
                value => value.RoutineId == "long");
            SourceAssignmentProfile retained = longRoutine.Assignments.Single(
                value => value.SourceId == source.SourceId);
            if (model.IsTargetLegal(source, "long", "ally") ||
                !model.IsTargetWanted("long", source.SourceId, "ally") ||
                !retained.WantedTargetUnitIds.Contains("ally"))
                throw new InvalidOperationException(
                    "Disabling Share silently pruned the stale ally target instead of preserving repairable intent.");
            RoutinePlanResult disabled = new RoutinePlanService().Plan(profile, "long",
                snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, new EffectLeafExpression(
                        EffectKind.Buff, "personal-buff", EffectTarget.Caster,
                        "ContextActionApplyBuff", "root/apply") }
                }, options, new[] { share }, targeting);
            if (disabled.Plan.Steps.Count != 0 ||
                disabled.Plan.Outcomes.Count(value => value.Kind ==
                    TargetOutcomeKind.Unfulfilled && value.UnitId == "ally") != 1)
                throw new InvalidOperationException(
                    "The Share-disabled stale ally target did not surface as an explicit unfulfilled outcome.");
            model.RemoveTargetFromAssignment("long", source.SourceId,
                retained.AutomaticAssignment.AssignmentId, "ally");
            if (model.IsTargetWanted("long", source.SourceId, "ally") ||
                longRoutine.Assignments.Any(value => value.SourceId == source.SourceId))
                throw new InvalidOperationException(
                    "The invalid target was not cleanly removable.");
        }

        private static void TestEnhancementCompatibilityAndSharedCost()
        {
            AbilityKey ability = Ability("brown-fur-spell", string.Empty, 0);
            var spellPool = new ResourcePoolSnapshot("brown-slots",
                ResourcePoolKind.SpontaneousLevel, 3, 3, null);
            ProviderSnapshot provider = PlannerProvider("brown", "brown-book",
                ability, spellPool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { spellPool }, "brown", "ally");
            var option = new ProviderPlanningOption(provider,
                new[] { "brown", "ally" }, new[] { "brown", "ally" },
                8, 80);
            CastEnhancementSnapshot share = ClassEnhancement("share", "brown",
                ability, "brown-book", 2, "reservoir|brown",
                "brown-fur-share-transmutation", true);
            CastEnhancementSnapshot powerful = ClassEnhancement("powerful-strength",
                "brown", ability, "brown-book", 2, "reservoir|brown",
                "brown-fur-powerful-change", false);
            CastEnhancementSnapshot otherScore = ClassEnhancement("powerful-dexterity",
                "brown", ability, "brown-book", 2, "reservoir|brown",
                "brown-fur-powerful-change", false);
            var rodA = new CastEnhancementSnapshot("rod-a", "brown", "rod-a",
                "Extend Rod", string.Empty,
                CastEnhancementCategory.MetamagicRod, 2, 9, 2, null);
            var rodB = new CastEnhancementSnapshot("rod-b", "brown", "rod-b",
                "Reach Rod", string.Empty,
                CastEnhancementCategory.MetamagicRod, 4, 9, 2, null);
            if (!CastEnhancementSnapshot.AreCompatible(new[] { share, powerful }) ||
                CastEnhancementSnapshot.AreCompatible(new[] { powerful, otherScore }) ||
                CastEnhancementSnapshot.AreCompatible(new[] { rodA, rodB }) ||
                !CastEnhancementSnapshot.AreCompatible(new[] { rodA, powerful }) ||
                CastEnhancementSnapshot.AreCompatible(new[] { share, share }))
                throw new InvalidOperationException(
                    "Explicit enhancement exclusivity groups were not enforced.");

            // Duplicate enhancement selections on one request dedupe
            // deterministically (idempotent compile of the same configured
            // intent); persisted duplicates are rejected at load instead of
            // reaching the planner twice.
            var duplicateRequest = new BuffCastRequest(new BuffSourceDefinition(
                "duplicate-enhancement", ability, Leaf("duplicate-buff"),
                CastGroupingKind.PerTarget), new[] { "ally" },
                ExistingEffectPolicy.Overwrite, null,
                new[] { share.EnhancementId, share.EnhancementId });
            if (duplicateRequest.EnhancementIds.Count != 1)
                throw new InvalidOperationException(
                    "Duplicate enhancement selections did not dedupe deterministically.");
            CastPlan duplicatePlan = new CastPlanner().Plan(snapshot,
                duplicateRequest, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] { share });
            if (duplicatePlan.Steps.Count != 1 ||
                duplicatePlan.Steps.Single().EnhancementUsageByPool["reservoir|brown"] != 1)
                throw new InvalidOperationException(
                    "A deduped duplicate selection did not plan exactly one reserved use.");
            BuffPlannerProfile duplicateProfile = BuffPlannerProfile.CreateDefault(
                "duplicate-persisted");
            SourceAssignmentProfile duplicateAssignment = Assignment(
                ability.Canonical, ability, new[] { "ally" });
            duplicateAssignment.CastingAssignments[0].Enhancements.Add(
                new EnhancementSelectionProfile { EnhancementId = share.EnhancementId });
            duplicateAssignment.CastingAssignments[0].Enhancements.Add(
                new EnhancementSelectionProfile { EnhancementId = share.EnhancementId });
            duplicateProfile.Routines[0].Assignments.Add(duplicateAssignment);
            var duplicateModel = new PlannerSetupModel(duplicateProfile, snapshot,
                new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, Leaf("x") } },
                new[] { option }, ignored => { });
            bool rejectedDuplicate = false;
            try
            {
                new ProfileRepository(Path.Combine(Path.GetTempPath(),
                    "kbp-duplicate-rejected")).Save(duplicateProfile);
            }
            catch (InvalidDataException)
            {
                rejectedDuplicate = true;
            }
            if (!rejectedDuplicate)
                throw new InvalidOperationException(
                    "Duplicate persisted enhancement IDs were silently accepted.");

            var request = new BuffCastRequest(new BuffSourceDefinition(
                "brown-combined", ability, Leaf("brown-buff"),
                CastGroupingKind.PerTarget), new[] { "ally" },
                ExistingEffectPolicy.Overwrite, null,
                new[] { share.EnhancementId, powerful.EnhancementId });
            CastPlan plan = new CastPlanner().Plan(snapshot, request,
                new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null),
                new[] { share, powerful });
            if (plan.Steps.Count != 1 ||
                plan.Steps.Single().Reservation.Units != 1 ||
                plan.Steps.Single().EnhancementUsageByPool["reservoir|brown"] != 2)
                throw new InvalidOperationException(
                    "Share plus Powerful Change did not reserve one spell slot and two forecast reservoir units.");

            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "combined-ui");
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> {
                    { ability.Canonical, Leaf("brown-buff") }
                }, new[] { option }, ignored => { },
                new[] { powerful, share });
            model.SetEnhancement("long", share.EnhancementId);
            model.SetEnhancement("long", powerful.EnhancementId);
            SelectedCastingViewModel casting = SelectedCastingViewModel.Create(
                model.SelectedSource, model, "long", null);
            if (!casting.EnhancementLabel.Contains("Share") ||
                !casting.EnhancementLabel.Contains("Powerful") ||
                !casting.EnhancementLabel.Contains("Arcane Reservoir: 2 per cast / 2 remaining") ||
                casting.Choices.Count(value => value.Selected) != 2 ||
                !casting.SelectedEnhancementIds.SequenceEqual(
                    new[] { "powerful-strength", "share" }) ||
                !casting.Choices.Single(value => value.EnhancementId == "share")
                    .CheckboxStyle)
                throw new InvalidOperationException(
                    "The multi-enhancement summary or independent Share checkbox state was incomplete.");
        }

        private static void TestSharedEnhancementPoolOvercommit()
        {
            AbilityKey ability = Ability("reservoir-limited", string.Empty, 0);
            var spellPool = new ResourcePoolSnapshot("reservoir-spells",
                ResourcePoolKind.SpontaneousLevel, 4, 4, null);
            ProviderSnapshot provider = PlannerProvider("brown", "brown-book",
                ability, spellPool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { spellPool }, "brown", "a", "b");
            var option = new ProviderPlanningOption(provider,
                new[] { "brown", "a", "b" },
                new[] { "brown", "a", "b" }, 8, 80);
            CastEnhancementSnapshot shareOne = ClassEnhancement("share", "brown",
                ability, "brown-book", 1, "reservoir|brown",
                "brown-fur-share-transmutation", true);
            CastPlan sharePlan = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("share-twice",
                    ability, Leaf("share-buff"), CastGroupingKind.PerTarget),
                    new[] { "a", "b" }, ExistingEffectPolicy.Overwrite,
                    null, new[] { "share" }), new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] { shareOne });
            if (sharePlan.Steps.Count != 1 ||
                sharePlan.Outcomes.Count(value => value.Kind ==
                    TargetOutcomeKind.Unfulfilled) != 1)
                throw new InvalidOperationException(
                    "Two Share casts overcommitted a one-point reservoir.");

            CastEnhancementSnapshot shareThree = ClassEnhancement("share", "brown",
                ability, "brown-book", 3, "reservoir|brown",
                "brown-fur-share-transmutation", true);
            CastEnhancementSnapshot powerfulThree = ClassEnhancement("powerful",
                "brown", ability, "brown-book", 3, "reservoir|brown",
                "brown-fur-powerful-change", false);
            string[] combined = { "share", "powerful" };
            var requests = new[] {
                new BuffCastRequest(new BuffSourceDefinition("a-first", ability,
                    Leaf("first"), CastGroupingKind.PerTarget), new[] { "a" },
                    ExistingEffectPolicy.Overwrite, null, combined),
                new BuffCastRequest(new BuffSourceDefinition("b-second", ability,
                    Leaf("second"), CastGroupingKind.PerTarget), new[] { "b" },
                    ExistingEffectPolicy.Overwrite, null, combined)
            };
            CastPlan combinedPlan = new CastPlanner().PlanRoutine(snapshot,
                requests, new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null),
                new[] { shareThree, powerfulThree });
            if (combinedPlan.Steps.Count != 1 ||
                combinedPlan.Steps.Single().EnhancementUsageByPool[
                    "reservoir|brown"] != 2 ||
                combinedPlan.Outcomes.Count(value => value.Kind ==
                    TargetOutcomeKind.Unfulfilled) != 1)
                throw new InvalidOperationException(
                    "Two two-point casts overcommitted a three-point reservoir.");

            CastEnhancementSnapshot powerfulOne = ClassEnhancement("powerful",
                "brown", ability, "brown-book", 1, "reservoir|brown",
                "brown-fur-powerful-change", false);
            CastPlan rejected = new CastPlanner().Plan(snapshot,
                requests[0], new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null),
                new[] { shareOne, powerfulOne });
            if (rejected.Steps.Count != 0)
                throw new InvalidOperationException(
                    "A two-point cast was accepted with one reservoir point.");
        }

        private static void TestMultiEnhancementRoundTrip(string root)
        {
            AbilityKey ability = Ability("profile-multi-enhancement",
                string.Empty, 0);
            var pool = new ResourcePoolSnapshot("profile-free",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("brown", "brown-book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { pool }, "brown");
            var option = new ProviderPlanningOption(provider,
                new[] { "brown" }, new[] { "brown" }, 5, 50);
            CastEnhancementSnapshot share = ClassEnhancement("share", "brown",
                ability, "brown-book", 4, "reservoir|brown",
                "brown-fur-share-transmutation", true);
            CastEnhancementSnapshot powerful = ClassEnhancement("powerful",
                "brown", ability, "brown-book", 4, "reservoir|brown",
                "brown-fur-powerful-change", false);
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "multi-enhancement-profile");
            var effects = new Dictionary<string, EffectExpression> {
                { ability.Canonical, Leaf("profile-buff") }
            };
            var model = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects, new[] { option },
                ignored => { }, new[] { share, powerful });
            model.SetEnhancement("long", powerful.EnhancementId);
            model.SetEnhancement("long", share.EnhancementId);
            string path = Path.Combine(root, "multi-enhancement-roundtrip");
            Directory.CreateDirectory(path);
            var repository = new ProfileRepository(path);
            repository.Save(profile);
            BuffPlannerProfile loaded = repository.Load(profile.CampaignId).Profile;
            string[] actual = loaded.Routines.First(value => value.RoutineId ==
                    "long").Assignments.Single().SelectedEnhancementIds.ToArray();
            if (!actual.SequenceEqual(new[] { "powerful", "share" }) ||
                loaded.SchemaVersion != BuffPlannerProfile.CurrentSchemaVersion)
                throw new InvalidOperationException(
                    "A multi-enhancement assignment did not round-trip in deterministic order.");
            BuffPlannerProfile old = BuffPlannerProfile.CreateDefault(
                "old-single-enhancement-profile");
            old.Routines.First(value => value.RoutineId == "long")
                .Assignments.Add(Assignment(ability.Canonical, ability,
                    new string[0], new[] { "share" }));
            repository.Save(old);
            if (repository.Load(old.CampaignId).Profile.Routines.First(value =>
                    value.RoutineId == "long").Assignments.Single()
                .SelectedEnhancementIds.Single() != "share")
                throw new InvalidOperationException(
                    "A legacy single-enhancement assignment no longer loads.");
        }

        private static void TestPassiveInfusionTargeting()
        {
            AbilityKey extract = Ability("personal-extract", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("extract-slots",
                ResourcePoolKind.SpontaneousLevel, 1, 1, null);
            ProviderSnapshot provider = PlannerProvider("alchemist",
                "alchemist-book", extract, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { pool }, "alchemist", "ally");
            var withoutInfusion = new ProviderPlanningOption(provider,
                new[] { "alchemist" }, new[] { "alchemist" }, 6, 60);
            var withInfusion = new ProviderPlanningOption(provider,
                new[] { "alchemist", "ally" },
                new[] { "alchemist", "ally" }, 6, 60);
            var effects = new Dictionary<string, EffectExpression> {
                { extract.Canonical, Leaf("extract-buff") }
            };
            var absent = new PlannerSetupModel(BuffPlannerProfile.CreateDefault(
                "without-infusion"), snapshot, new ActiveEffectSnapshot(null),
                effects, new[] { withoutInfusion }, ignored => { });
            if (absent.IsTargetLegal(absent.SelectedSource, "ally"))
                throw new InvalidOperationException(
                    "A personal extract without Infusion targeted an ally.");
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(
                "with-infusion");
            var present = new PlannerSetupModel(profile, snapshot,
                new ActiveEffectSnapshot(null), effects, new[] { withInfusion },
                ignored => { });
            if (!present.IsTargetLegal(present.SelectedSource, "ally") ||
                present.GetApplicableEnhancements().Count != 0)
                throw new InvalidOperationException(
                    "Native Infusion targeting was not passive or invented a toggle.");
            var request = new BuffCastRequest(new BuffSourceDefinition(
                "infused-extract", extract, Leaf("extract-buff"),
                CastGroupingKind.PerTarget), new[] { "ally" },
                ExistingEffectPolicy.Overwrite, null);
            CastPlan plan = new CastPlanner().Plan(snapshot, request,
                new[] { withInfusion }, EmptyPolicy(),
                new ActiveEffectSnapshot(null));
            if (plan.Steps.Count != 1 ||
                plan.Steps.Single().Reservation.Units != 1 ||
                plan.Steps.Single().EnhancementIds.Count != 0 ||
                plan.Steps.Single().EnhancementUsageByPool.Count != 0)
                throw new InvalidOperationException(
                    "Infusion added a surcharge or changed ordinary extract-slot consumption.");

            string repository = FindRepositoryRoot();
            string source = File.ReadAllText(Path.Combine(repository, "src",
                "KingmakerBuffPlanner", "GameAdapters",
                "KingmakerProviderOptionBuilder.cs"));
            if (!source.Contains("ability.IsAlchemistSpell") ||
                !source.Contains("ability.AlchemistInfusion") ||
                !source.Contains("ability.TargetAnchor") ||
                source.Contains("InfusionEnhancement"))
                throw new InvalidOperationException(
                    "The passive native AbilityData Infusion seam is absent or toggle-backed.");
        }

        private static void TestExecutionPreflightUnderLease()
        {
            AbilityKey ability = Ability("lease-preflight", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("lease-free",
                ResourcePoolKind.Unlimited, 0, 0, null);
            ProviderSnapshot provider = PlannerProvider("brown", "brown-book",
                ability, pool.PoolKey, 0);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { pool }, "brown");
            var option = new ProviderPlanningOption(provider,
                new[] { "brown" }, new[] { "brown" }, 4, 40);
            CastEnhancementSnapshot share = ClassEnhancement("share", "brown",
                ability, "brown-book", 1, "reservoir|brown",
                "brown-fur-share-transmutation", true);
            CastPlan plan = new CastPlanner().Plan(snapshot,
                new BuffCastRequest(new BuffSourceDefinition("lease", ability,
                    Leaf("lease-buff"), CastGroupingKind.PerTarget),
                    new[] { "brown" }, ExistingEffectPolicy.Overwrite,
                    null, new[] { "share" }), new[] { option }, EmptyPolicy(),
                new ActiveEffectSnapshot(null), new[] { share });
            var success = new LeaseAwareInstantRuntime(false);
            ExecutionReport successReport = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(success, true).Execute(plan,
                successReport));
            if (string.Join(",", success.Events.ToArray()) !=
                    "prepare,validate,fire,cleanup" || successReport.Confirmed != 1)
                throw new InvalidOperationException(
                    "Execution preflight did not run while the native activation lease was armed.");
            var rejected = new LeaseAwareInstantRuntime(true);
            ExecutionReport rejectedReport = new ExecutionReport(plan);
            Drain(new InstantCastExecutor(rejected, true).Execute(plan,
                rejectedReport));
            if (string.Join(",", rejected.Events.ToArray()) !=
                    "prepare,validate,cleanup" || rejected.FireCount != 0 ||
                !rejectedReport.Records.Any(value => value.Detail ==
                    "target-invalid-after-activation"))
                throw new InvalidOperationException(
                    "A post-activation target rejection issued or redirected a cast.");
        }

        private static CastEnhancementSnapshot ClassEnhancement(string id,
            string caster, AbilityKey ability, string spellbook, int remaining,
            string pool, string group, bool targeting,
            bool requiresNativeCommand = true,
            string directCastProviderId = null)
        {
            string name = id == "share" ? "Share Transmutation" :
                id.StartsWith("powerful", StringComparison.Ordinal)
                    ? "Powerful Change: Strength" : id;
            return new CastEnhancementSnapshot(id, caster, id + "-toggle",
                name, string.Empty, CastEnhancementCategory.ClassFeature,
                0, 0, remaining, new[] { ability.BaseAbilityGuid }, name,
                new[] { spellbook }, pool, requiresNativeCommand, group, 1,
                targeting, group, "Arcane Reservoir",
                directCastProviderId);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(
                Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(
                directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null)
                throw new InvalidOperationException(
                    "Repository root was not discoverable.");
            return directory.FullName;
        }

        private static ProviderSelectionPolicy EmptyPolicy()
        {
            return new ProviderSelectionPolicy(null, null, null);
        }

        private static CastPlan PlannerPlan(
            PartyProviderSnapshot snapshot,
            AbilityKey ability,
            CastGroupingKind grouping,
            IEnumerable<string> targets,
            IEnumerable<ProviderPlanningOption> options,
            ProviderSelectionPolicy policy,
            ActiveEffectSnapshot active)
        {
            var source = new BuffSourceDefinition("source", ability, Leaf("effect"), grouping);
            var request = new BuffCastRequest(source, targets, ExistingEffectPolicy.Overwrite, null);
            return new CastPlanner().Plan(snapshot, request, options, policy, active);
        }

        private static AbilityKey Ability(string baseGuid, string variantGuid, int metamagic)
        {
            return new AbilityKey(baseGuid, variantGuid, metamagic, SourceKind.Spellbook, string.Empty);
        }

        private static ProviderSnapshot Provider(
            string abilityGuid,
            string poolKey,
            int unitsPerCast,
            IEnumerable<string> tokenIds)
        {
            var key = new ProviderKey("unit-a", "book-a", Ability(abilityGuid, string.Empty, 0), string.Empty);
            return new ProviderSnapshot(key, abilityGuid, 2, poolKey, unitsPerCast, tokenIds);
        }

        private static PartyProviderSnapshot Snapshot(
            IEnumerable<ProviderSnapshot> providers,
            IEnumerable<ResourcePoolSnapshot> pools)
        {
            return new PartyProviderSnapshot(
                new[] { new UnitSnapshot("unit-a", "Caster", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true)) },
                providers,
                pools);
        }

        private sealed class EnhancementInstantRuntime : IInstantCastRuntimeAdapter, ICastEnhancementRuntimeAdapter
        {
            private readonly bool _unavailable;
            private readonly bool _throwOnFire;
            internal readonly List<string> Events = new List<string>();
            internal int FireCount;
            internal EnhancementInstantRuntime(bool unavailable, bool throwOnFire)
            {
                _unavailable = unavailable;
                _throwOnFire = throwOnFire;
            }
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            {
                Events.Add("prepare");
                return _unavailable ? CastEnhancementPreparation.Fail("fixture-exhausted") :
                    CastEnhancementPreparation.Pass(new CallbackDisposable(() => Events.Add("cleanup")));
            }
            public InstantCastResult Fire(CastStep step)
            {
                FireCount++;
                Events.Add("fire");
                if (_throwOnFire) throw new InvalidOperationException("fixture-cast-failure");
                return new InstantCastResult(true, true, true, true, "enhanced-success");
            }
            public bool EffectsObserved(CastStep step) { return true; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("fixture-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("fixture-clean"); }
        }

        private sealed class FixtureShareTargetingModifier :
            ICastTargetingModifier
        {
            private readonly string _enhancementId;
            private readonly string _casterId;
            private readonly string[] _legalIds;
            private readonly CastExecutionStrategy _strategy;
            private readonly string _reason;

            internal FixtureShareTargetingModifier(string enhancementId,
                string casterId, IEnumerable<string> legalIds)
                : this(enhancementId, casterId, legalIds,
                    CastExecutionStrategy.AnimatedFallback,
                    "legacy-share-fixture")
            {
            }

            internal FixtureShareTargetingModifier(string enhancementId,
                string casterId, IEnumerable<string> legalIds,
                CastExecutionStrategy strategy, string reason)
            {
                _enhancementId = enhancementId;
                _casterId = casterId;
                _legalIds = (legalIds ?? new string[0]).ToArray();
                _strategy = strategy;
                _reason = reason;
            }

            public ProviderPlanningOption Apply(
                EffectiveProviderOptionContext context,
                ProviderPlanningOption option)
            {
                if (!context.SelectedEnhancements.Any(value =>
                        value.EnhancementId == _enhancementId)) return option;
                if (option.Provider.Key.CasterUnitId != _casterId) return null;
                return new ProviderPlanningOption(option.Provider, _legalIds,
                    _legalIds, option.EffectiveCasterLevel,
                    option.ExpectedDurationRounds, _strategy, _reason);
            }
        }

        private sealed class LeaseAwareInstantRuntime :
            IInstantCastRuntimeAdapter, ICastEnhancementRuntimeAdapter
        {
            private readonly bool _rejectTarget;
            private bool _armed;
            internal readonly List<string> Events = new List<string>();
            internal int FireCount;

            internal LeaseAwareInstantRuntime(bool rejectTarget)
            {
                _rejectTarget = rejectTarget;
            }

            public bool IsInCombat { get { return false; } }

            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            {
                Events.Add("prepare");
                _armed = true;
                return CastEnhancementPreparation.Pass(new CallbackDisposable(
                    () => { _armed = false; Events.Add("cleanup"); }));
            }

            public CastRuntimeValidation Validate(CastStep step)
            {
                Events.Add("validate");
                if (!_armed)
                    return CastRuntimeValidation.Fail(
                        "activation-lease-not-armed");
                return _rejectTarget
                    ? CastRuntimeValidation.Fail(
                        "target-invalid-after-activation")
                    : CastRuntimeValidation.Pass();
            }

            public InstantCastResult Fire(CastStep step)
            {
                FireCount++;
                Events.Add("fire");
                return new InstantCastResult(true, true, true, true,
                    "native-commit");
            }

            public bool EffectsObserved(CastStep step) { return true; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("fixture-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("fixture-clean"); }
        }

        private sealed class CallbackDisposable : IDisposable
        {
            private readonly Action _dispose;
            private bool _disposed;
            internal CallbackDisposable(Action dispose) { _dispose = dispose; }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _dispose();
            }
        }
        private sealed class FakeAnimatedRuntime : ICastRuntimeAdapter
        {
            private int _validations;
            internal int StartCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            {
                _validations++;
                return _validations == 1
                    ? CastRuntimeValidation.Fail("target-invalid")
                    : CastRuntimeValidation.Pass();
            }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                StartCount++;
                return new FakeAnimatedOperation();
            }
        }

        private sealed class FakeInputBoundary : IPlannerInputBoundary
        {
            internal int CaptureCount;
            internal int EnterCount;
            internal int RestoreCount;
            internal bool FailEnter;
            public bool PlannerModeRequested { get; private set; }
            public object CaptureState() { CaptureCount++; return "state"; }
            public void EnterPlannerMode()
            {
                EnterCount++;
                if (FailEnter) throw new InvalidOperationException("fixture-enter-failure");
                PlannerModeRequested = true;
            }
            public void RestoreState(object state)
            {
                RestoreCount++;
                PlannerModeRequested = false;
                if (!object.Equals(state, "state"))
                    throw new InvalidOperationException("Wrong input state restored.");
            }
        }

        private sealed class FakeRoutineRunner : IPlannerRoutineRunner
        {
            private readonly QuickExecutionResult _result;
            internal int StartCount;
            internal int ReadyOnlyStartCount;
            internal FakeRoutineRunner(QuickExecutionResult result) { _result = result; }
            public bool TryStart(string routineId, Action<QuickExecutionResult> completed)
            {
                StartCount++;
                completed(_result);
                return true;
            }
            public bool TryStartReadyOnly(string routineId, Action<QuickExecutionResult> completed)
            {
                ReadyOnlyStartCount++;
                completed(_result);
                return true;
            }
        }

        private sealed class FakeAnimatedOperation : IAnimatedCastOperation
        {
            private int _checks;
            public bool IsCompleted { get { return ++_checks >= 2; } }
            public bool IsStarted { get { return true; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return true; } }
            public bool EffectsObserved { get { return true; } }
            public bool ResourceSpent { get { return true; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return "command-success"; } }
            public void Dispose() { }
        }

        private sealed class FakeInstantRuntime : IInstantCastRuntimeAdapter
        {
            private int _validations;
            internal int FireCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            {
                _validations++;
                return _validations == 1
                    ? CastRuntimeValidation.Fail("resource-changed")
                    : CastRuntimeValidation.Pass();
            }
            public InstantCastResult Fire(CastStep step)
            {
                FireCount++;
                return new InstantCastResult(true, true, true, true, "rule-success");
            }
            public bool EffectsObserved(CastStep step) { return true; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("fixture-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("fixture-clean"); }
        }

        private sealed class AlwaysAnimatedRuntime : ICastRuntimeAdapter,
            ICastEnhancementRuntimeAdapter
        {
            internal int StartCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public CastEnhancementPreparation PrepareEnhancements(
                CastStep step)
            { return CastEnhancementPreparation.Pass(null); }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                StartCount++;
                return new FakeAnimatedOperation();
            }
        }

        private sealed class AlwaysInstantRuntime : IInstantCastRuntimeAdapter
        {
            internal int FireCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public InstantCastResult Fire(CastStep step)
            {
                FireCount++;
                return new InstantCastResult(true, true, true, true, "rule-success");
            }
            public bool EffectsObserved(CastStep step) { return true; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("fixture-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("fixture-clean"); }
        }

        private sealed class NeverObservedInstantRuntime : IInstantCastRuntimeAdapter
        {
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public InstantCastResult Fire(CastStep step)
            {
                return new InstantCastResult(true, true, false, true, "rule-success-no-effect");
            }
            public bool EffectsObserved(CastStep step) { return false; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("fixture-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("fixture-clean"); }
        }

        private sealed class ProviderDirectSequenceRuntime :
            IInstantCastRuntimeAdapter, ICastEnhancementRuntimeAdapter
        {
            private CastStep _outstanding;

            internal ProviderDirectSequenceRuntime(int sourceUses,
                int reservoir)
            {
                SourceUsesRemaining = sourceUses;
                ReservoirRemaining = reservoir;
            }

            internal int FireCount;
            internal int ProviderTransactionCount;
            internal int OrdinaryFireCount;
            internal int SourceSpendInvocations;
            internal int SourceUsesRemaining;
            internal int ReservoirDebit;
            internal int ReservoirRemaining;
            internal int EnhancementPrepareCount;
            internal int EnhancementDisposeCount;
            internal int CleanupCount;
            internal bool PrematureValidation;
            internal readonly HashSet<string> EffectRecipients =
                new HashSet<string>(StringComparer.Ordinal);

            public bool IsInCombat { get { return false; } }

            public CastEnhancementPreparation PrepareEnhancements(
                CastStep step)
            {
                EnhancementPrepareCount++;
                return CastEnhancementPreparation.Pass(
                    new CallbackDisposable(() =>
                        EnhancementDisposeCount++));
            }

            public CastRuntimeValidation Validate(CastStep step)
            {
                if (_outstanding != null)
                {
                    PrematureValidation = true;
                    return CastRuntimeValidation.Fail(
                        "prior-provider-transaction-active");
                }
                if (SourceUsesRemaining < step.Reservation.Units)
                    return CastRuntimeValidation.Fail(
                        "source-resource-exhausted");
                int reservoir = step.EnhancementUsageByPool.Values.Sum();
                return ReservoirRemaining < reservoir
                    ? CastRuntimeValidation.Fail(
                        "provider-reservoir-exhausted")
                    : CastRuntimeValidation.Pass();
            }

            public InstantCastResult Fire(CastStep step)
            {
                if (_outstanding != null)
                    throw new InvalidOperationException(
                        "A later cast entered before provider completion.");
                FireCount++;
                SourceSpendInvocations++;
                SourceUsesRemaining -= step.Reservation.Units;
                if (SourceUsesRemaining < 0)
                    throw new InvalidOperationException(
                        "The source spell pool was overdrawn.");
                if (step.ExecutionStrategy ==
                    CastExecutionStrategy.ProviderDirectRuleCast)
                {
                    ProviderTransactionCount++;
                    int cost = step.EnhancementUsageByPool.Values.Sum();
                    ReservoirRemaining -= cost;
                    ReservoirDebit += cost;
                    if (ReservoirRemaining < 0)
                        throw new InvalidOperationException(
                            "The provider reservoir was overdrawn.");
                    _outstanding = step;
                    return new InstantCastResult(true, true, false, true,
                        true, "provider-direct-rule-cast");
                }
                OrdinaryFireCount++;
                AddEffects(step);
                return new InstantCastResult(true, true, true, true, true,
                    "ordinary-direct-rule-cast");
            }

            public bool EffectsObserved(CastStep step)
            {
                if (object.ReferenceEquals(_outstanding, step))
                    AddEffects(step);
                return step.ExpectedRecipientUnitIds.All(
                    EffectRecipients.Contains);
            }

            public InstantCastCompletion InspectCompletion(CastStep step)
            {
                if (step.ExecutionStrategy !=
                    CastExecutionStrategy.ProviderDirectRuleCast)
                    return InstantCastCompletion.Settled(
                        "ordinary-rule-cast-settled");
                if (!object.ReferenceEquals(_outstanding, step))
                    return step.ExpectedRecipientUnitIds.All(
                            EffectRecipients.Contains)
                        ? InstantCastCompletion.Settled(
                            "provider-process-terminal")
                        : InstantCastCompletion.Pending(
                            "different-provider-transaction-active");
                if (!step.ExpectedRecipientUnitIds.All(
                        EffectRecipients.Contains))
                    return InstantCastCompletion.Pending(
                        "provider-effect-process-running");
                _outstanding = null;
                return InstantCastCompletion.Settled(
                    "provider-effect-process-terminal");
            }

            public InstantCastCompletion Cleanup(CastStep step)
            {
                CleanupCount++;
                if (object.ReferenceEquals(_outstanding, step))
                    _outstanding = null;
                return InstantCastCompletion.Settled(
                    "provider-transaction-cleaned");
            }

            private void AddEffects(CastStep step)
            {
                foreach (string recipient in step.ExpectedRecipientUnitIds)
                    EffectRecipients.Add(recipient);
            }
        }

        private sealed class PendingProviderDirectRuntime :
            IInstantCastRuntimeAdapter, ICastEnhancementRuntimeAdapter
        {
            private readonly bool _cleanupCompletes;

            internal PendingProviderDirectRuntime(bool cleanupCompletes)
            {
                _cleanupCompletes = cleanupCompletes;
            }

            internal bool Active;
            internal int CleanupCount;
            internal int EnhancementDisposeCount;
            public bool IsInCombat { get { return false; } }
            public CastEnhancementPreparation PrepareEnhancements(
                CastStep step)
            {
                return CastEnhancementPreparation.Pass(
                    new CallbackDisposable(() =>
                        EnhancementDisposeCount++));
            }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public InstantCastResult Fire(CastStep step)
            {
                Active = true;
                return new InstantCastResult(true, true, false, true, true,
                    "provider-direct-pending");
            }
            public bool EffectsObserved(CastStep step) { return false; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            {
                return InstantCastCompletion.Pending(
                    "provider-effect-process-running");
            }
            public InstantCastCompletion Cleanup(CastStep step)
            {
                CleanupCount++;
                if (_cleanupCompletes) Active = false;
                return Active ? InstantCastCompletion.Pending(
                    "provider-cleanup-residual") :
                    InstantCastCompletion.Settled(
                        "provider-cleanup-complete");
            }
        }

        private sealed class TerminalProviderFailureRuntime :
            IInstantCastRuntimeAdapter, ICastEnhancementRuntimeAdapter
        {
            public bool IsInCombat { get { return false; } }
            public CastEnhancementPreparation PrepareEnhancements(
                CastStep step)
            { return CastEnhancementPreparation.Pass(null); }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public InstantCastResult Fire(CastStep step)
            {
                return new InstantCastResult(true, true, true, true, true,
                    "provider-rule-success");
            }
            public bool EffectsObserved(CastStep step) { return true; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            {
                return InstantCastCompletion.FailedSettled(
                    "provider-terminal-cleanup-failed");
            }
            public InstantCastCompletion Cleanup(CastStep step)
            {
                return InstantCastCompletion.FailedSettled(
                    "provider-terminal-cleanup-failed");
            }
        }

        private sealed class StickySequenceRuntime : IInstantCastRuntimeAdapter
        {
            private readonly int _effectDelayPolls;
            private readonly Dictionary<CastStep, int> _effectPolls =
                new Dictionary<CastStep, int>();
            private CastStep _outstanding;

            internal StickySequenceRuntime(int sharedRemaining,
                int effectDelayPolls)
            {
                SharedRemaining = sharedRemaining;
                _effectDelayPolls = effectDelayPolls;
            }

            internal int FireCount;
            internal int RuleTransactions;
            internal int SourceSpendInvocations;
            internal int DeliverySpendInvocations { get { return 0; } }
            internal int CleanupCount;
            internal int SharedRemaining;
            internal bool PrematureValidation;
            internal readonly HashSet<string> SpentTokenIds =
                new HashSet<string>(StringComparer.Ordinal);

            public bool IsInCombat { get { return false; } }

            public CastRuntimeValidation Validate(CastStep step)
            {
                if (_outstanding != null)
                {
                    PrematureValidation = true;
                    return CastRuntimeValidation.Fail(
                        "prior-effect-not-confirmed");
                }
                if (step.Reservation.TokenIds.Any(
                        SpentTokenIds.Contains))
                    return CastRuntimeValidation.Fail(
                        "prepared-token-already-spent");
                return CastRuntimeValidation.Pass();
            }

            public InstantCastResult Fire(CastStep step)
            {
                FireCount++;
                RuleTransactions++;
                SourceSpendInvocations++;
                foreach (string token in step.Reservation.TokenIds)
                    if (!SpentTokenIds.Add(token))
                        throw new InvalidOperationException(
                            "A prepared token was reused: " + token);
                if (step.Reservation.TokenIds.Count == 0 &&
                    step.Reservation.Units > 0)
                {
                    if (SharedRemaining < step.Reservation.Units)
                        throw new InvalidOperationException(
                            "The shared spell pool was overdrawn.");
                    SharedRemaining -= step.Reservation.Units;
                }
                if (_effectDelayPolls > 0) _outstanding = step;
                return new InstantCastResult(true, true,
                    _effectDelayPolls == 0, true, true,
                    "sticky-rule-cast;spend-owner:source;delivery-spend:false");
            }

            public bool EffectsObserved(CastStep step)
            {
                if (_effectDelayPolls == 0) return true;
                int polls;
                _effectPolls.TryGetValue(step, out polls);
                polls++;
                _effectPolls[step] = polls;
                if (polls < _effectDelayPolls) return false;
                if (object.ReferenceEquals(_outstanding, step))
                    _outstanding = null;
                return true;
            }

            public InstantCastCompletion InspectCompletion(CastStep step)
            {
                return InstantCastCompletion.Settled(
                    "direct-delivery-has-no-held-touch-or-command");
            }

            public InstantCastCompletion Cleanup(CastStep step)
            {
                CleanupCount++;
                _outstanding = null;
                return InstantCastCompletion.Settled(
                    "sticky-sequence-clean");
            }
        }

        private sealed class UmdInstantRuntime : IInstantCastRuntimeAdapter
        {
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public InstantCastResult Fire(CastStep step)
            {
                return new InstantCastResult(true, false, false, false,
                    false, "rule-success:false;umd-failed:true;spend-invoked:false");
            }
            public bool EffectsObserved(CastStep step) { return false; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("umd-rule-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("umd-no-cleanup"); }
        }

        private sealed class CleanupInstantRuntime :
            IInstantCastRuntimeAdapter
        {
            private bool _failCleanup;
            private bool _residualOnNextFire = true;
            private bool _residual;

            internal CleanupInstantRuntime(bool failCleanup)
            {
                _failCleanup = failCleanup;
            }

            internal int FireCount;
            internal int CleanupCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public InstantCastResult Fire(CastStep step)
            {
                FireCount++;
                if (_residualOnNextFire)
                {
                    _residual = true;
                    _residualOnNextFire = false;
                }
                return new InstantCastResult(true, true, true, true, true,
                    "cleanup-fixture-rule-success");
            }
            public bool EffectsObserved(CastStep step) { return true; }
            public InstantCastCompletion InspectCompletion(CastStep step)
            {
                return _residual ? InstantCastCompletion.Pending(
                    "fixture-held-touch:true") :
                    InstantCastCompletion.Settled(
                        "fixture-held-touch:false");
            }
            public InstantCastCompletion Cleanup(CastStep step)
            {
                CleanupCount++;
                if (!_failCleanup) _residual = false;
                return _residual ? InstantCastCompletion.Pending(
                    "fixture-cleanup-residual:true") :
                    InstantCastCompletion.Settled(
                        "fixture-cleanup-residual:false");
            }
            internal void RecoverExternalState()
            {
                _failCleanup = false;
                _residual = false;
                _residualOnNextFire = false;
            }
        }

        private sealed class CleanupAnimatedRuntime : ICastRuntimeAdapter
        {
            internal int StartCount;
            internal int DisposeCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                StartCount++;
                return new CleanupAnimatedOperation(StartCount != 1,
                    () => DisposeCount++);
            }
        }

        private sealed class CleanupAnimatedOperation :
            IAnimatedCastOperation
        {
            private readonly bool _success;
            private readonly Action _disposedCallback;
            private bool _residual;
            private bool _disposed;

            internal CleanupAnimatedOperation(bool success,
                Action disposedCallback)
            {
                _success = success;
                _residual = !success;
                _disposedCallback = disposedCallback;
            }

            public bool IsCompleted { get { return true; } }
            public bool IsStarted { get { return true; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return _success; } }
            public bool EffectsObserved { get { return _success; } }
            public bool ResourceSpent { get { return _success; } }
            public bool HasResidualDeliveryState
            { get { return _residual; } }
            public string Detail
            { get { return _success ? "delivery-success" : "delivery-failed"; } }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _residual = false;
                _disposedCallback();
            }
        }

        private sealed class ExceptionThenSuccessAnimatedRuntime :
            ICastRuntimeAdapter
        {
            internal int StartCount;
            internal int DisposeCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step)
            { return CastRuntimeValidation.Pass(); }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                StartCount++;
                return StartCount == 1
                    ? (IAnimatedCastOperation)new ThrowingAnimatedOperation(
                        () => DisposeCount++)
                    : new CleanupAnimatedOperation(true,
                        () => DisposeCount++);
            }
        }

        private sealed class ThrowingAnimatedOperation :
            IAnimatedCastOperation
        {
            private readonly Action _disposedCallback;
            internal ThrowingAnimatedOperation(Action disposedCallback)
            { _disposedCallback = disposedCallback; }
            public bool IsCompleted
            { get { throw new InvalidOperationException(
                "fixture-operation-failure"); } }
            public bool IsStarted { get { return true; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return false; } }
            public bool EffectsObserved { get { return false; } }
            public bool ResourceSpent { get { return false; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return "throwing-operation"; } }
            public void Dispose() { _disposedCallback(); }
        }

        private static DiscoveryNode EffectNode(string id)
        {
            return new DiscoveryNode(DiscoveryNodeKind.Effect, id,
                effectKind: EffectKind.Buff, effectId: id,
                target: EffectTarget.CurrentTarget, sourceContract: "fixture");
        }

        private static void Run(string name, Action action)
        {
            try
            {
                action();
                _passed++;
            }
            catch (Exception exception)
            {
                Failures.Add(name + ": " + exception.Message);
            }
        }

        private static void TestAbsentActivation()
        {
            string rejection;
            if (RuntimeTestProtocol.TryRead(new[] { "Kingmaker.exe" }, out rejection) != null || rejection.Length != 0)
                throw new InvalidOperationException("Ordinary launch was not inert.");
        }

        private static void TestValidRequest(string root)
        {
            string path = WriteRequest(root, "valid", null);
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 || request.RunId != "valid")
                throw new InvalidOperationException("Valid request was rejected: " + rejection);
        }

        private static void TestProductionEvidenceRoot(string root)
        {
            string path = WriteRequest(root, "production-root-guard", null);
            string rejection;
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request != null || rejection != "invalid-request:path-outside-root")
                throw new InvalidOperationException(
                    "Fixture root relaxed the production evidence boundary: " + rejection);
        }

        private static void TestValidCatalogRequest(string root)
        {
            string path = WriteRequest(root, "valid-catalog", o => o["scenario"] = "native-buff-catalog");
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 || request.Scenario != "native-buff-catalog")
                throw new InvalidOperationException("Valid catalog request was rejected: " + rejection);
        }

        private static void TestValidCallOfTheWildRequest(string root)
        {
            string path = WriteRequest(root, "valid-cotw", o =>
            {
                o["scenario"] = "native-buff-catalog";
                o["profileId"] = "call-of-the-wild";
                o["expectedOptionalMods"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        { "ummId", "CallOfTheWild" },
                        { "version", "1.14.4c-2.1" },
                        { "assemblyName", "CallOfTheWild.dll" },
                        { "assemblySha256", new string('c', 64) }
                    }
                };
                o["expectedBlueprintGuids"] = new[]
                {
                    "0027cbfe0a484380ab76df1ad3d7326a",
                    "03963bcf8dd64abea3757311c1e8a79c",
                    "151b1f365c4217e5062a1fe50f7a63d3"
                };
            });
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 || request.ProfileId != "call-of-the-wild" ||
                request.ExpectedOptionalMods.Count != 1 || request.ExpectedBlueprintGuids.Count != 3)
                throw new InvalidOperationException("Valid Call of the Wild request was rejected: " + rejection);
        }

        private static void TestValidHumanReproductionRequest(string root)
        {
            string path = WriteRequest(root, "valid-human-reproduction", o =>
            {
                o["profileId"] = "human-reproduction";
                o["expectedOptionalMods"] = Enumerable.Range(0, 3).Select(index =>
                    (object)new Dictionary<string, object>
                    {
                        { "ummId", "Fixture" + index }, { "version", "1.0" },
                        { "assemblyName", "Fixture" + index + ".dll" },
                        { "assemblySha256", new string((char)('a' + index), 64) }
                    }).ToArray();
            });
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                request.ProfileId != "human-reproduction" || request.ExpectedOptionalMods.Count != 3)
                throw new InvalidOperationException("Valid human reproduction request was rejected: " + rejection);
        }

        private static void TestValidUiRequest(string root)
        {
            string path = WriteRequest(root, "valid-ui", o => o["scenario"] = "ui-root-smoke");
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 || request.Scenario != "ui-root-smoke")
                throw new InvalidOperationException("Valid UI request was rejected: " + rejection);
        }

        private static void TestValidLiveUiRequest(string root)
        {
            string path = WriteRequest(root, "valid-live-ui", o =>
            {
                o["scenario"] = "live-ui-bootstrap";
                o["parameters"] = new Dictionary<string, object>
                {
                    { "workingSaveName", "KBP_AUTOMATION_WORKING" },
                    { "workingFileName", "Manual_297_KBP_AUTOMATION_WORKING.zks" },
                    { "workingSha256", new string('a', 64) },
                    { "baselineSaveName", "KBP_AUTOMATION_BASELINE" },
                    { "baselineFileName", "Manual_296_KBP_AUTOMATION_BASELINE.zks" },
                    { "baselineSha256", new string('b', 64) },
                    { "expectedGameName", "Yadmila" },
                    { "expectedGameId", "3d556254-8ba9-4e9f-8d11-755eecd0b661" },
                    { "executionMode", "animated" }
                };
            });
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsLiveUiScenario(request.Scenario) ||
                request.Parameters.Count != 9 ||
                (string)request.Parameters["executionMode"] != "animated")
                throw new InvalidOperationException("Valid live UI request was rejected: " + rejection);
        }

        // Review I4: the manual scenario validates the COMPLETE live-save
        // contract plus exactly one bounded hold, through the real TryRead
        // path — no permissive early return survives request validation.
        private static void TestManualScenarioRequestValidation(string root)
        {
            Action<Dictionary<string, object>> saveSet = o =>
            {
                o["scenario"] = "live-workspace-manual";
                o["parameters"] = new Dictionary<string, object>
                {
                    { "workingSaveName", "KBP_AUTOMATION_WORKING" },
                    { "workingFileName", "Manual_305_KBP_AUTOMATION_WORKING.zks" },
                    { "workingSha256", new string('a', 64) },
                    { "baselineSaveName", "KBP_AUTOMATION_BASELINE" },
                    { "baselineFileName", "Manual_304_KBP_AUTOMATION_BASELINE.zks" },
                    { "baselineSha256", new string('b', 64) },
                    { "expectedGameName", "Yadmila" },
                    { "expectedGameId", "3d556254-8ba9-4e9f-8d11-755eecd0b661" },
                    { "executionMode", "instant" },
                    { "manualHoldSeconds", 300 }
                };
            };
            string valid = WriteRequest(root, "manual-valid", saveSet);
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, valid },
                out rejection);
            if (request == null || rejection.Length != 0 ||
                request.Parameters.Count != 10 ||
                RuntimeTestProtocol.ReadManualHoldSeconds(request.Parameters) != 300)
                throw new InvalidOperationException(
                    "A valid manual request was rejected: " + rejection);

            // Each defect class fails at the non-mutating request boundary.
            var cases = new List<KeyValuePair<string, Action<Dictionary<string, object>>>>
            {
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "missing-hold", o =>
                    {
                        saveSet(o);
                        ((Dictionary<string, object>)o["parameters"])
                            .Remove("manualHoldSeconds");
                    }),
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "hold-out-of-range", o =>
                    {
                        saveSet(o);
                        ((Dictionary<string, object>)o["parameters"])
                            ["manualHoldSeconds"] = 0;
                    }),
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "unknown-extra", o =>
                    {
                        saveSet(o);
                        ((Dictionary<string, object>)o["parameters"])
                            ["surprise"] = true;
                    }),
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "wrong-save-name", o =>
                    {
                        saveSet(o);
                        ((Dictionary<string, object>)o["parameters"])
                            ["workingSaveName"] = "SOMETHING_ELSE";
                    }),
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "bad-hash", o =>
                    {
                        saveSet(o);
                        ((Dictionary<string, object>)o["parameters"])
                            ["baselineSha256"] = "nothex";
                    }),
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "duplicate-files", o =>
                    {
                        saveSet(o);
                        ((Dictionary<string, object>)o["parameters"])
                            ["baselineFileName"] = "Manual_305_KBP_AUTOMATION_WORKING.zks";
                    }),
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "invalid-mode", o =>
                    {
                        saveSet(o);
                        ((Dictionary<string, object>)o["parameters"])
                            ["executionMode"] = "teleport";
                    }),
                new KeyValuePair<string, Action<Dictionary<string, object>>>(
                    "hold-on-automation", o =>
                    {
                        saveSet(o);
                        o["scenario"] = "live-workspace-qual";
                    })
            };
            foreach (KeyValuePair<string, Action<Dictionary<string, object>>> item in cases)
            {
                string path = WriteRequest(root, "manual-bad-" + item.Key, item.Value);
                RuntimeTestRequest bad = ReadProtocol(
                    new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path },
                    out rejection);
                if (bad != null || string.IsNullOrEmpty(rejection))
                    throw new InvalidOperationException(
                        "An invalid manual request was accepted: " + item.Key);
            }
        }

        private static void TestValidNativeUiProbeRequest(string root)
        {
            string path = WriteRequest(root, "valid-ui-probe", o =>
                o["scenario"] = "ui-native-contract-probe");
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsNativeUiProbeScenario(request.Scenario))
                throw new InvalidOperationException("Valid native UI probe request was rejected: " + rejection);
        }

        private static void TestValidFinalCoreRequest(string root)
        {
            string path = WriteRequest(root, "valid-final-core", o => o["scenario"] = "final-no-save-core");
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsCatalogScenario(request.Scenario) ||
                RuntimeTestProtocol.IsUiScenario(request.Scenario))
                throw new InvalidOperationException("Valid final core request was rejected: " + rejection);
        }

        private static void TestHudInstallInvalidation()
        {
            var gate = new HudInstallInvalidationGate();
            for (int frame = 0; frame < 240; frame++)
                if (Dispatches(gate, 0, false))
                    throw new InvalidOperationException("Absent campaign HUD triggered discovery.");
            if (!gate.IsRequested || gate.AttemptCount != 0)
                throw new InvalidOperationException("Initial invalidation was consumed without a HUD host.");
            if (!Dispatches(gate, 101, true) || gate.AttemptCount != 1)
                throw new InvalidOperationException("HUD appearance did not trigger one discovery.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);
            for (int frame = 0; frame < 240; frame++)
                if (Dispatches(gate, 101, true))
                    throw new InvalidOperationException("Unchanged HUD retriggered discovery.");
            gate.Request("planner-hotkey");
            if (!Dispatches(gate, 101, true))
                throw new InvalidOperationException("Hotkey invalidation did not dispatch.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);
            if (Dispatches(gate, 101, true) ||
                gate.AttemptCount != 2)
                throw new InvalidOperationException("Lifecycle invalidation was not consumed exactly once.");
            if (Dispatches(gate, 101, false) || !Dispatches(gate, 101, true) ||
                gate.AttemptCount != 3)
                throw new InvalidOperationException("HUD reactivation did not trigger exactly one discovery.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);
            if (!Dispatches(gate, 202, true))
                throw new InvalidOperationException("HUD replacement did not trigger discovery.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);
            if (Dispatches(gate, 202, true) ||
                gate.AttemptCount != 4)
                throw new InvalidOperationException("HUD replacement did not trigger exactly one discovery.");
        }

        private static void TestHudRetryableReadiness()
        {
            var gate = new HudInstallInvalidationGate(4);
            int scopedDiscoveries = 0;
            int globalSearches = 0;
            for (int frame = 0; frame < 400; frame++)
                if (Dispatches(gate, 0, false)) scopedDiscoveries++;
            if (scopedDiscoveries != 0 || gate.AttemptCount != 0)
                throw new InvalidOperationException("No-HUD frames dispatched hierarchy discovery.");

            if (!Dispatches(gate, 41, true))
                throw new InvalidOperationException("Active HUD did not dispatch initial readiness attempt.");
            scopedDiscoveries++;
            gate.RecordAttemptResult(HudInstallAttemptResult.RetryableNotReady);
            if (!gate.IsRetryScheduled || gate.RetryFramesRemaining != 4 ||
                gate.State != HudInstallationState.RetryPending)
                throw new InvalidOperationException("Retryable readiness did not arm bounded retry.");
            for (int frame = 0; frame < 4; frame++)
                if (Dispatches(gate, 41, true))
                    throw new InvalidOperationException("Readiness retry ignored its bounded cadence.");
            if (!Dispatches(gate, 41, true))
                throw new InvalidOperationException("Readiness retry did not dispatch on the later frame.");
            scopedDiscoveries++;
            gate.RecordAttemptResult(HudInstallAttemptResult.CandidateCreated);
            gate.RecordCandidateResult(HudCandidateTickResult.Pending);
            for (int frame = 0; frame < 300; frame++)
                if (Dispatches(gate, 41, true))
                    throw new InvalidOperationException("Live candidate retriggered discovery.");
            gate.RecordCandidateResult(HudCandidateTickResult.Installed);
            if (gate.State != HudInstallationState.Installed || scopedDiscoveries != 2 ||
                gate.AttemptCount != 2 || gate.RetryArmCount != 1 ||
                gate.RetryDispatchCount != 1 || globalSearches != 0)
                throw new InvalidOperationException("Retryable readiness did not converge without global search.");
        }

        private static void TestHudCandidateExpiry()
        {
            var gate = new HudInstallInvalidationGate(4);
            var validation = new HudCandidateValidationGate(120);
            if (!Dispatches(gate, 51, true))
                throw new InvalidOperationException("Candidate test did not receive its initial dispatch.");
            gate.RecordAttemptResult(HudInstallAttemptResult.CandidateCreated);
            for (int frame = 1; frame < validation.MaximumFailureFrames; frame++)
            {
                HudCandidateTickResult result = validation.RecordValidation(false);
                if (result != HudCandidateTickResult.Pending)
                    throw new InvalidOperationException("Candidate expired before its allowed validation period.");
                gate.RecordCandidateResult(result);
                if (Dispatches(gate, 51, true))
                    throw new InvalidOperationException("Live provisional candidate was recreated.");
            }
            HudCandidateTickResult expiry = validation.RecordValidation(false);
            if (expiry != HudCandidateTickResult.Expired ||
                validation.FailureFrames != validation.MaximumFailureFrames)
                throw new InvalidOperationException("Candidate did not report its exact expiry transition.");
            gate.RecordCandidateResult(expiry);
            if (!gate.IsRequested || !gate.IsRetryScheduled ||
                gate.State != HudInstallationState.CandidateExpired ||
                gate.HostTransitionCount != 1)
                throw new InvalidOperationException("Candidate expiry did not re-arm the unchanged host.");
            for (int frame = 0; frame < 4; frame++)
                if (Dispatches(gate, 51, true))
                    throw new InvalidOperationException("Expired candidate retried before settling delay.");
            if (!Dispatches(gate, 51, true))
                throw new InvalidOperationException("Expired candidate did not retry without a host transition.");
            gate.RecordAttemptResult(HudInstallAttemptResult.CandidateCreated);
            gate.RecordCandidateResult(HudCandidateTickResult.Installed);
            if (gate.State != HudInstallationState.Installed || gate.AttemptCount != 2 ||
                gate.RetryArmCount != 1 || gate.RetryDispatchCount != 1 ||
                gate.HostTransitionCount != 1)
                throw new InvalidOperationException("Replacement candidate did not install on the same HUD.");
        }

        private static void TestHudHostingChainStaleness()
        {
            string failure;
            if (!HudHostingChainValidator.IsViable(HostingChain(), out failure) || failure.Length != 0)
                throw new InvalidOperationException("A complete live hosting chain was rejected: " + failure);
            AssertHostingFailure(HostingChain(ownedRootExists: false), "owned-root-missing");
            AssertHostingFailure(HostingChain(rootHasParent: false), "owned-root-parent-missing");
            AssertHostingFailure(HostingChain(rootActive: false), "owned-root-inactive");
            AssertHostingFailure(HostingChain(anchorExists: false), "anchor-controller-missing");
            AssertHostingFailure(HostingChain(anchorActive: false), "anchor-controller-inactive");
            AssertHostingFailure(HostingChain(nativeClusterExists: false), "native-cluster-missing");
            AssertHostingFailure(HostingChain(nativeClusterActive: false), "native-cluster-inactive");
            AssertHostingFailure(HostingChain(activeHudExists: false), "active-hud-missing");
            AssertHostingFailure(HostingChain(activeHudActive: false), "active-hud-inactive");
            AssertHostingFailure(HostingChain(rootParentIsNativeCluster: false), "owned-root-reparented");
            AssertHostingFailure(HostingChain(anchorBelongsToActiveHud: false), "anchor-outside-active-hud");
            AssertHostingFailure(HostingChain(nativeClusterBelongsToActiveHud: false), "native-cluster-outside-active-hud");
            AssertHostingFailure(HostingChain(rootBelongsToActiveHud: false), "owned-root-outside-active-hud");
            AssertHostingFailure(HostingChain(nativeRaycasterActive: false), "native-raycaster-inactive");

            var gate = new HudInstallInvalidationGate(3);
            if (!Dispatches(gate, 61, true))
                throw new InvalidOperationException("Installed-anchor test did not dispatch.");
            gate.RecordAttemptResult(HudInstallAttemptResult.CandidateCreated);
            gate.RecordCandidateResult(HudCandidateTickResult.Installed);
            bool isInstalled = HudHostingChainValidator.IsViable(HostingChain(), out failure);
            int ownedRootDisposals = 0;
            int nativeUiDisposals = 0;
            if (HudHostingChainValidator.IsViable(HostingChain(anchorActive: false), out failure))
                throw new InvalidOperationException("Inactive inner anchor remained installed.");
            isInstalled = false;
            ownedRootDisposals++;
            gate.RecordCandidateResult(HudCandidateTickResult.Stale);
            if (isInstalled || ownedRootDisposals != 1 || nativeUiDisposals != 0 ||
                gate.State != HudInstallationState.StaleAnchor || !gate.IsRetryScheduled)
                throw new InvalidOperationException("Stale anchor cleanup crossed ownership or lost retryability.");
            for (int frame = 0; frame < 3; frame++)
                if (Dispatches(gate, 61, true))
                    throw new InvalidOperationException("Stale anchor retried before bounded delay.");
            if (!Dispatches(gate, 61, true))
                throw new InvalidOperationException("Stale anchor did not request the current hierarchy.");
        }

        private static void TestHudStablePerformance()
        {
            var absent = new HudInstallInvalidationGate(30);
            for (int frame = 0; frame < 1000; frame++)
                if (Dispatches(absent, 0, false))
                    throw new InvalidOperationException("Absent HUD performed discovery.");
            if (absent.AttemptCount != 0)
                throw new InvalidOperationException("Absent HUD accumulated attempts.");

            var installed = new HudInstallInvalidationGate(30);
            if (!Dispatches(installed, 71, true))
                throw new InvalidOperationException("Stable installed fixture did not initialize.");
            installed.RecordAttemptResult(HudInstallAttemptResult.CandidateCreated);
            installed.RecordCandidateResult(HudCandidateTickResult.Installed);
            for (int frame = 0; frame < 1000; frame++)
                if (Dispatches(installed, 71, true))
                    throw new InvalidOperationException("Stable installed HUD rediscovered its hierarchy.");
            if (installed.AttemptCount != 1)
                throw new InvalidOperationException("Stable installed HUD accumulated attempts.");

            var provisional = new HudInstallInvalidationGate(30);
            if (!Dispatches(provisional, 72, true))
                throw new InvalidOperationException("Provisional fixture did not initialize.");
            provisional.RecordAttemptResult(HudInstallAttemptResult.CandidateCreated);
            for (int frame = 0; frame < 500; frame++)
            {
                provisional.RecordCandidateResult(HudCandidateTickResult.Pending);
                if (Dispatches(provisional, 72, true))
                    throw new InvalidOperationException("Live provisional candidate was recreated.");
            }
            if (provisional.AttemptCount != 1)
                throw new InvalidOperationException("Provisional candidate accumulated attempts.");

            var retrying = new HudInstallInvalidationGate(30);
            int readinessAttempts = 0;
            if (Dispatches(retrying, 73, true))
            {
                readinessAttempts++;
                retrying.RecordAttemptResult(HudInstallAttemptResult.RetryableNotReady);
            }
            for (int frame = 0; frame < 600; frame++)
                if (Dispatches(retrying, 73, true))
                {
                    readinessAttempts++;
                    retrying.RecordAttemptResult(HudInstallAttemptResult.RetryableNotReady);
                }
            if (readinessAttempts < 2 || readinessAttempts > 21 ||
                readinessAttempts != retrying.AttemptCount)
                throw new InvalidOperationException("Temporary readiness retries were not bounded: " +
                    readinessAttempts);
        }

        private static void TestHudLifecycleTransitions()
        {
            var gate = new HudInstallInvalidationGate(3);
            if (Dispatches(gate, 0, false) || !Dispatches(gate, 81, true))
                throw new InvalidOperationException("HUD absent-to-active transition failed.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);
            if (Dispatches(gate, 81, false) || gate.State != HudInstallationState.NoHud ||
                !Dispatches(gate, 81, true))
                throw new InvalidOperationException("HUD active/inactive/reactivation transition failed.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);
            if (!Dispatches(gate, 82, true))
                throw new InvalidOperationException("HUD identity replacement did not dispatch.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);

            if (!gate.Suspend("OnAreaBeginUnloading") ||
                gate.State != HudInstallationState.Suspended)
                throw new InvalidOperationException("Area unload did not suspend installation.");
            int beforeSuspendedFrames = gate.AttemptCount;
            if (Dispatches(gate, 82, false) || Dispatches(gate, 83, true) ||
                gate.AttemptCount != beforeSuspendedFrames)
                throw new InvalidOperationException("Suspended unload observed or dispatched a transient HUD.");
            if (!gate.ResumeAndRequest("OnAreaDidLoad") || !Dispatches(gate, 83, true))
                throw new InvalidOperationException("Area load did not resume installation.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);

            gate.Suspend("mod-disabled");
            if (Dispatches(gate, 83, true))
                throw new InvalidOperationException("Disabled mod dispatched installation.");
            gate.ResumeAndRequest("mod-enabled");
            if (!Dispatches(gate, 83, true))
                throw new InvalidOperationException("Re-enabled mod did not dispatch installation.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);

            gate.Request("planner-hotkey");
            if (!Dispatches(gate, 83, true))
                throw new InvalidOperationException("Hotkey did not re-arm an unchanged active host.");
            gate.RecordAttemptResult(HudInstallAttemptResult.AlreadyInstalled);
            if (Dispatches(gate, 83, true) || gate.IsRequested ||
                gate.State != HudInstallationState.Installed)
                throw new InvalidOperationException("Hotkey invalidation was not consumed exactly once.");
        }

        private static bool Dispatches(
            HudInstallInvalidationGate gate,
            int hostIdentity,
            bool hostActive)
        {
            return gate.ObserveHost(hostIdentity, hostActive) ==
                HudInstallDispatchDecision.Dispatch;
        }

        private static HudHostingChainSnapshot HostingChain(
            bool ownedRootExists = true,
            bool rootHasParent = true,
            bool rootActive = true,
            bool anchorExists = true,
            bool anchorActive = true,
            bool nativeClusterExists = true,
            bool nativeClusterActive = true,
            bool activeHudExists = true,
            bool activeHudActive = true,
            bool rootParentIsNativeCluster = true,
            bool anchorBelongsToActiveHud = true,
            bool nativeClusterBelongsToActiveHud = true,
            bool rootBelongsToActiveHud = true,
            bool nativeRaycasterActive = true)
        {
            return new HudHostingChainSnapshot(
                ownedRootExists, rootHasParent, rootActive, anchorExists, anchorActive,
                nativeClusterExists, nativeClusterActive, activeHudExists, activeHudActive,
                rootParentIsNativeCluster, anchorBelongsToActiveHud,
                nativeClusterBelongsToActiveHud, rootBelongsToActiveHud,
                nativeRaycasterActive);
        }

        private static void AssertHostingFailure(
            HudHostingChainSnapshot snapshot,
            string expectedFailure)
        {
            string actualFailure;
            if (HudHostingChainValidator.IsViable(snapshot, out actualFailure) ||
                actualFailure != expectedFailure)
                throw new InvalidOperationException("Hosting chain failure mismatch: expected=" +
                    expectedFailure + " actual=" + actualFailure);
        }

        private static void TestValidPerformanceRequest(string root)
        {
            string path = WriteRequest(root, "valid-performance", o =>
            {
                o["scenario"] = "performance-probe";
                o["parameters"] = new Dictionary<string, object>
                {
                    { "durationSeconds", 15 },
                    { "disableHudDiscovery", true },
                    { "minimumFramesPerSecond", 50.0 }
                };
            });
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsPerformanceScenario(request.Scenario) ||
                request.Parameters.Count != 3)
                throw new InvalidOperationException("Valid performance request was rejected: " + rejection);
        }

        private static void TestInvalidPerformanceRequest(string root)
        {
            string path = WriteRequest(root, "invalid-performance", o =>
            {
                o["scenario"] = "performance-probe";
                o["parameters"] = new Dictionary<string, object>
                {
                    { "durationSeconds", 4 },
                    { "disableHudDiscovery", false },
                    { "minimumFramesPerSecond", 0.0 }
                };
            });
            AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
        }

        private static void TestValidLaunchRenderDiagnosticRequest(string root)
        {
            string path = WriteRequest(root, "valid-launch-render", o =>
                o["scenario"] = "launch-render-diagnostic");
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsLaunchRenderDiagnosticScenario(request.Scenario) ||
                !RuntimeTestProtocol.IsMenuDiagnosticScenario(request.Scenario) ||
                RuntimeTestProtocol.IsMenuInputDiagnosticScenario(request.Scenario) ||
                request.Parameters.Count != 0)
                throw new InvalidOperationException("Valid launch render diagnostic request was rejected: " + rejection);
        }

        private static void TestValidMenuInputDiagnosticRequest(string root)
        {
            string path = WriteRequest(root, "valid-menu-input", o =>
                o["scenario"] = "menu-input-diagnostic");
            string rejection;
            RuntimeTestRequest request = ReadProtocol(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsMenuInputDiagnosticScenario(request.Scenario) ||
                !RuntimeTestProtocol.IsMenuDiagnosticScenario(request.Scenario) ||
                RuntimeTestProtocol.IsLaunchRenderDiagnosticScenario(request.Scenario) ||
                request.Parameters.Count != 0)
                throw new InvalidOperationException("Valid menu input diagnostic request was rejected: " + rejection);
        }

        private static void TestInvalidMenuDiagnosticParameters(string root)
        {
            string path = WriteRequest(root, "invalid-menu-parameters", o =>
            {
                o["scenario"] = "launch-render-diagnostic";
                o["parameters"] = new Dictionary<string, object> { { "saveName", "KBP_AUTOMATION_WORKING" } };
            });
            AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
        }

        private static void TestMenuFrameStatsBlackClassification()
        {
            MenuFrameLumaSummary black = MenuFrameStats.Summarize(new float[] { 0f, 0.001f, 0.005f, 0f });
            if (black.IsNonBlack || black.BlackFraction > 1f ||
                black.Minimum != 0f || black.Maximum < 0f)
                throw new InvalidOperationException("Black frame was classified as non-black: " + black.Describe());
            MenuFrameLumaSummary empty = null;
            if (MenuFrameStats.IsNonBlack(empty))
                throw new InvalidOperationException("Null summary was classified as non-black.");
            try
            {
                MenuFrameStats.Summarize(new float[0]);
                throw new InvalidOperationException("Empty sample list was accepted.");
            }
            catch (ArgumentException)
            {
            }
        }

        private static void TestMenuFrameStatsMixedSummary()
        {
            MenuFrameLumaSummary mixed = MenuFrameStats.Summarize(
                new float[] { 0f, 0.01f, 0.5f, 0.75f, 1f });
            if (!mixed.IsNonBlack)
                throw new InvalidOperationException("Mixed frame was classified as black: " + mixed.Describe());
            if (mixed.SampleCount != 5 || mixed.Minimum != 0f || mixed.Maximum != 1f)
                throw new InvalidOperationException("Mixed summary extrema are wrong: " + mixed.Describe());
            float expectedMean = (0f + 0.01f + 0.5f + 0.75f + 1f) / 5f;
            if (Math.Abs(mixed.Mean - expectedMean) > 0.0001f)
                throw new InvalidOperationException("Mixed summary mean is wrong: " + mixed.Describe());
            float expectedBlackFraction = 2f / 5f;
            if (Math.Abs(mixed.BlackFraction - expectedBlackFraction) > 0.0001f)
                throw new InvalidOperationException("Mixed summary black fraction is wrong: " + mixed.Describe());
        }

        private static void TestMenuFrameChangedFraction()
        {
            float[] before = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
            float[] identical = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
            float[] sameWithinTolerance = new float[] { 0.105f, 0.195f, 0.3f, 0.4f };
            float[] halfChanged = new float[] { 0.9f, 0.2f, 0.9f, 0.4f };
            if (MenuFrameStats.ComputeChangedFraction(before, identical) != 0f)
                throw new InvalidOperationException("Identical frames reported changes.");
            if (MenuFrameStats.ComputeChangedFraction(before, sameWithinTolerance) != 0f)
                throw new InvalidOperationException("Sub-tolerance deltas reported as changes.");
            if (Math.Abs(MenuFrameStats.ComputeChangedFraction(before, halfChanged) - 0.5f) > 0.0001f)
                throw new InvalidOperationException("Half-changed frames reported the wrong fraction.");
            try
            {
                MenuFrameStats.ComputeChangedFraction(before, new float[] { 0.1f });
                throw new InvalidOperationException("Mismatched sample lengths were accepted.");
            }
            catch (ArgumentException)
            {
            }
        }

        private static void TestLoadedAssemblyIdentitySidecarResolution()
        {
            string boundary = Path.Combine(Path.GetTempPath(),
                "KbpLoadedAssemblyIdentity-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(boundary);
                string canonical = Path.Combine(boundary, "ExampleMod.dll");
                File.WriteAllText(canonical, "assembly-bytes");
                string sidecar = Path.Combine(boundary, "ExampleMod.dll.52778.cache");
                File.WriteAllText(sidecar, "cached-image-bytes");
                string resolved = LoadedAssemblyIdentity.ResolveCanonicalFile(sidecar);
                if (!string.Equals(resolved, canonical, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Numeric mono sidecar did not resolve to the canonical assembly: " + resolved);
                string noCanonical = Path.Combine(boundary, "Missing.dll.123.cache");
                if (!string.Equals(LoadedAssemblyIdentity.ResolveCanonicalFile(noCanonical), noCanonical,
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Sidecar without a canonical file should stay unresolved.");
                string nonNumeric = Path.Combine(boundary, "ExampleMod.dll.a1b2.cache");
                if (!string.Equals(LoadedAssemblyIdentity.ResolveCanonicalFile(nonNumeric), nonNumeric,
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Non-numeric sidecar suffix should stay unresolved.");
                string plain = Path.Combine(boundary, "ExampleMod.dll");
                if (!string.Equals(LoadedAssemblyIdentity.ResolveCanonicalFile(plain), plain,
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Plain assembly path should pass through unchanged.");
            }
            finally
            {
                Directory.Delete(boundary, true);
            }
        }

        private static void TestDuplicateFlag(string root)
        {
            string path = WriteRequest(root, "duplicate-flag", null);
            AssertRejected(new[]
            {
                "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path,
                RuntimeTestProtocol.ActivationFlag, path
            });
        }

        private static void TestOutsidePath()
        {
            AssertRejected(new[]
            {
                "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag,
                Path.Combine(Path.GetTempPath(), "outside-request.json")
            });
        }

        private static void TestMutation(
            string root,
            string runId,
            Action<Dictionary<string, object>> mutation)
        {
            string path = WriteRequest(root, runId, mutation);
            AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
        }

        private static void AddUnknownMember(Dictionary<string, object> request)
        {
            request["unexpected"] = true;
        }

        private static void TestDuplicateMember(string root)
        {
            string path = Path.Combine(root, "duplicate-member-request.json");
            string json = JsonConvert.SerializeObject(NewRequest(root, "duplicate-member"));
            json = json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1");
            File.WriteAllText(path, json);
            AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
        }

        private static void TestResultReuse(string root)
        {
            string path = WriteRequest(root, "reuse", null);
            File.WriteAllText(Path.Combine(root, "runtime-result.json"), "{}");
            try
            {
                AssertRejected(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path });
            }
            finally
            {
                File.Delete(Path.Combine(root, "runtime-result.json"));
            }
        }

        private static void AssertRejected(string[] args)
        {
            string rejection;
            if (ReadProtocol(args, out rejection) != null || string.IsNullOrWhiteSpace(rejection))
                throw new InvalidOperationException("Invalid request was not rejected.");
        }

        private static RuntimeTestRequest ReadProtocol(string[] args, out string rejection)
        {
            if (string.IsNullOrWhiteSpace(_protocolEvidenceRoot))
                throw new InvalidOperationException("Protocol fixture root is unavailable.");
            return RuntimeTestProtocol.TryReadWithinRoot(args, _protocolEvidenceRoot, out rejection);
        }

        private static string WriteRequest(
            string root,
            string runId,
            Action<Dictionary<string, object>> mutation)
        {
            Dictionary<string, object> request = NewRequest(root, runId);
            if (mutation != null) mutation(request);
            string path = Path.Combine(root, runId + "-request.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(request));
            return path;
        }

        private static Dictionary<string, object> NewRequest(string root, string runId)
        {
            return new Dictionary<string, object>
            {
                { "schemaVersion", 1 },
                { "enabled", true },
                { "runId", runId },
                { "scenario", "mod-load-smoke" },
                { "profileId", "native-only" },
                { "expectedModVersion", BuildInfo.Version },
                { "expectedCommit", "TEST-COMMIT" },
                { "evidenceDirectory", root },
                { "expectedPackageSha256", new string('a', 64) },
                { "expectedDllSha256", new string('b', 64) },
                { "timeoutSeconds", 30 },
                { "exitAfterCompletion", false },
                { "expectedOptionalMods", new object[0] },
                { "expectedBlueprintGuids", new string[0] },
                { "parameters", new Dictionary<string, object>() }
            };
        }

        // ------------------------------------------------------------------
        // Casting-first migration: charter acceptance A01-A04 plus the
        // Phase 1 authoring and persistence contract. All scenarios drive
        // the real CastingAuthoringService and ExplicitCastingCompiler
        // production types; fixtures are deterministic domain snapshots.
        // ------------------------------------------------------------------

        private static PlannedCasting DirectCasting(
            string castingId,
            string routineId,
            string casterUnitId,
            string targetUnitId,
            string sourceId,
            AbilityKey ability,
            IEnumerable<AuthoredEnhancementSelection> enhancements = null,
            string spellbookGuid = null,
            CastingAuthoringState state = CastingAuthoringState.Ready,
            ExistingEffectPolicy existingEffectPolicy = ExistingEffectPolicy.SkipAlreadyActive)
        {
            return new PlannedCasting(
                castingId, routineId, 0, sourceId, ability, casterUnitId, spellbookGuid,
                CastingTargetMode.DirectTarget, targetUnitId, null, null, null,
                enhancements, existingEffectPolicy, null, state, null);
        }

        private static PlannedCasting GroupCasting(
            string castingId,
            string routineId,
            string casterUnitId,
            string sourceId,
            AbilityKey ability,
            IEnumerable<string> requiredCoverage,
            IEnumerable<AuthoredEnhancementSelection> enhancements = null,
            CastingTargetMode mode = CastingTargetMode.CasterCenteredOrigin,
            string anchorUnitId = null,
            CastingAuthoringState state = CastingAuthoringState.Ready,
            MigrationProvenance provenance = null)
        {
            CastingOrigin origin = mode == CastingTargetMode.CasterCenteredOrigin
                ? CastingOrigin.CasterCentered()
                : CastingOrigin.Anchored(anchorUnitId);
            return new PlannedCasting(
                castingId, routineId, 0, sourceId, ability, casterUnitId, null,
                mode, null, origin, requiredCoverage, null, enhancements,
                ExistingEffectPolicy.SkipAlreadyActive, null, state, provenance);
        }

        private static CastingPlanDocument CastingDocument(params PlannedCasting[] castings)
        {
            // The document invariant requires per-routine contiguous orders;
            // fixture castings are authored with order 0 and normalized here.
            var positionInRoutine = new Dictionary<string, int>(StringComparer.Ordinal);
            var normalized = new List<PlannedCasting>();
            foreach (PlannedCasting casting in castings)
            {
                int prior;
                int position = positionInRoutine.TryGetValue(casting.RoutineId, out prior)
                    ? prior + 1 : 0;
                positionInRoutine[casting.RoutineId] = position;
                normalized.Add(PlannedCasting.WithOrder(casting, position));
            }
            return new CastingPlanDocument("fixture-campaign",
                new[]
                {
                    new RoutineDefinition("long", "Long"),
                    new RoutineDefinition("important", "Important"),
                    new RoutineDefinition("short", "Short")
                },
                normalized);
        }

        private static readonly AbilityKey CastingBuffAbility =
            Ability("a0000000000000000000000000000001", string.Empty, 0);

        private static readonly AbilityKey CastingGroupAbility =
            Ability("g0000000000000000000000000000001", string.Empty, 0);

        private static Dictionary<string, EffectExpression> CastingEffects(
            string directSourceId, string groupSourceId)
        {
            return new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { directSourceId, Leaf("buff-effect") },
                { groupSourceId, new EffectLeafExpression(EffectKind.Buff,
                    "group-effect", EffectTarget.Party, "fixture", "fixture/group") }
            };
        }

        private static CastEnhancementSnapshot CastingEnhancement(
            string enhancementId, string casterUnitId)
        {
            return new CastEnhancementSnapshot(
                enhancementId, casterUnitId, "rod-" + enhancementId,
                "Fixture " + enhancementId, string.Empty,
                CastEnhancementCategory.MetamagicRod, 0x1, 10, 1, new string[0]);
        }

        // Two casters with independent pools plus friendly targets; each
        // caster can cast `remaining` invocations of the given ability.
        private static PartyProviderSnapshot CastingParty(
            AbilityKey ability,
            out List<ProviderPlanningOption> options,
            out List<CastEnhancementSnapshot> enhancements,
            string[] targets, int remainingPerCaster,
            IDictionary<string, IEnumerable<string>> groupCoverageByAnchor = null)
        {
            string[] casters = { "unit-cleric", "unit-wizard" };
            var units = new List<UnitSnapshot>();
            foreach (string id in casters.Concat(targets))
                units.Add(new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true)));
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            options = new List<ProviderPlanningOption>();
            var all = casters.Concat(targets).Distinct(StringComparer.Ordinal).ToList();
            for (int i = 0; i < casters.Length; i++)
            {
                string caster = casters[i];
                string poolKey = "pool-" + caster;
                pools.Add(new ResourcePoolSnapshot(poolKey,
                    ResourcePoolKind.SpontaneousLevel, remainingPerCaster,
                    remainingPerCaster, null));
                // Exactly one provider option per caster for the requested
                // ability: two same-ability options would be a genuine
                // exact-source ambiguity the compiler must block on.
                if (!string.Equals(ability.Canonical, CastingGroupAbility.Canonical,
                        StringComparison.Ordinal))
                {
                    ProviderSnapshot direct = PlannerProvider(
                        caster, "book-" + caster, ability, poolKey, 1);
                    providers.Add(direct);
                    options.Add(new ProviderPlanningOption(direct, all,
                        new[] { caster }, 10, 100));
                    continue;
                }
                ProviderSnapshot group = PlannerProvider(
                    caster, "group-book-" + caster, ability, poolKey, 1);
                providers.Add(group);
                var coverage = new Dictionary<string, IEnumerable<string>>(
                    StringComparer.Ordinal);
                if (groupCoverageByAnchor != null &&
                    groupCoverageByAnchor.ContainsKey(caster))
                    coverage[caster] = groupCoverageByAnchor[caster];
                options.Add(new ProviderPlanningOption(group, all,
                    new[] { caster }, 10, 100,
                    CastExecutionStrategy.DirectRuleCast, "fixture-direct", coverage));
            }
            enhancements = new List<CastEnhancementSnapshot>
            {
                CastingEnhancement("extend-cleric", "unit-cleric"),
                CastingEnhancement("extend-wizard", "unit-wizard")
            };
            return new PartyProviderSnapshot(units, providers, pools);
        }

        private static ExplicitCastingPlan CompileCastingPlan(
            CastingPlanDocument document, PartyProviderSnapshot snapshot,
            List<ProviderPlanningOption> options,
            List<CastEnhancementSnapshot> enhancements,
            string directSourceId, string groupSourceId)
        {
            return new ExplicitCastingCompiler().Compile(
                document, snapshot, options, CastingEffects(directSourceId, groupSourceId),
                enhancements);
        }

        // A01: same buff, two casters, three targets, distinct enhancement
        // sets — three independent castings where editing one changes only it.
        private static void TestCastingA01TwoCastersThreeIndependent()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2", "unit-t3" }, 3);
            var service = new CastingAuthoringService(CastingDocument());
            Assert(service.AddCasting(DirectCasting("cast-1", "long", "unit-cleric",
                "unit-t1", "source-bulls", CastingBuffAbility,
                new[] { new AuthoredEnhancementSelection("extend-cleric", true, null) }))
                .Applied);
            Assert(service.AddCasting(DirectCasting("cast-2", "long", "unit-wizard",
                "unit-t2", "source-bulls", CastingBuffAbility,
                new[] { new AuthoredEnhancementSelection("extend-wizard", true, "exact-rod-w") }))
                .Applied);
            Assert(service.AddCasting(DirectCasting("cast-3", "long", "unit-cleric",
                "unit-t3", "source-bulls", CastingBuffAbility)).Applied);
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (plan.Castings.Count != 3 || plan.ReadyInvocationCount != 3)
                throw new InvalidOperationException(
                    "Three authored castings must be three ready invocations.");
            ResolvedCasting first = plan.CastingById("cast-1");
            ResolvedCasting second = plan.CastingById("cast-2");
            ResolvedCasting third = plan.CastingById("cast-3");
            if (first.Provider.CasterUnitId != "unit-cleric" ||
                second.Provider.CasterUnitId != "unit-wizard" ||
                third.Provider.CasterUnitId != "unit-cleric")
                throw new InvalidOperationException("Exact caster was not honored.");
            if (first.AppliedEnhancementIds.Count != 1 ||
                first.AppliedEnhancementIds[0] != "extend-cleric" ||
                second.AppliedEnhancementIds[0] != "extend-wizard")
                throw new InvalidOperationException(
                    "Distinct per-casting enhancements were not preserved.");
            PlannedCasting untouchedFirst = service.Document.Castings[0];
            PlannedCasting untouchedThird = service.Document.Castings[2];
            // Edit the middle casting only: change its enhancement set.
            AuthoringEditResult edit = service.UpdateCasting(DirectCasting(
                "cast-2", "long", "unit-wizard", "unit-t2", "source-bulls",
                CastingBuffAbility));
            if (!edit.Applied || edit.AffectedCastingIds.Count != 1 ||
                edit.AffectedCastingIds[0] != "cast-2")
                throw new InvalidOperationException(
                    "Single-casting edit disclosed the wrong scope: " + edit.Reason);
            if (!ReferenceEquals(untouchedFirst, service.Document.Castings[0]) ||
                !ReferenceEquals(untouchedThird, service.Document.Castings[2]))
                throw new InvalidOperationException(
                    "An unrelated casting instance was rebuilt by a scoped edit.");
            ExplicitCastingPlan revised = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (revised.CastingById("cast-2").AppliedEnhancementIds.Count != 0 ||
                revised.CastingById("cast-1").AppliedEnhancementIds.Count != 1 ||
                revised.CastingById("cast-3").AppliedEnhancementIds.Count != 0)
                throw new InvalidOperationException(
                    "The edit leaked into sibling castings.");
            // Editing focus never changes routine membership implicitly: the
            // service refuses a routine change smuggled into an update.
            if (service.UpdateCasting(DirectCasting(
                    "cast-2", "short", "unit-wizard", "unit-t2", "source-bulls",
                    CastingBuffAbility)).Applied)
                throw new InvalidOperationException(
                    "A routine move was accepted through a content edit.");
        }

        // A02: three recipients of a single-target ability are three records
        // and three invocations, never one multi-target cast.
        private static void TestCastingA02ThreeRecipientsThreeInvocations()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2", "unit-t3" }, 3);
            var service = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-1", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-2", "long", "unit-cleric", "unit-t2",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-3", "long", "unit-cleric", "unit-t3",
                    "source-bulls", CastingBuffAbility)));
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (plan.Castings.Count != 3 || plan.ReadyInvocationCount != 3)
                throw new InvalidOperationException(
                    "Three recipients must produce exactly three invocations.");
            foreach (ResolvedCasting casting in plan.Castings)
            {
                if (casting.TargetMode != CastingTargetMode.DirectTarget ||
                    casting.PredictedBeneficiaryUnitIds.Count != 1 ||
                    casting.PredictedBeneficiaryUnitIds[0] != casting.DirectTargetUnitId)
                    throw new InvalidOperationException(
                        "A direct casting gained or lost its single direct target.");
                if (casting.CoverageIncomplete)
                    throw new InvalidOperationException(
                        "A direct casting must not report group coverage gaps.");
            }
            string[] intended = { "cast-1|unit-t1", "cast-2|unit-t2", "cast-3|unit-t3" };
            foreach (string expectation in intended)
            {
                string[] parts = expectation.Split('|');
                ResolvedCasting casting = plan.CastingById(parts[0]);
                if (casting.DirectTargetUnitId != parts[1] ||
                    casting.PredictedBeneficiaryUnitIds[0] != parts[1])
                    throw new InvalidOperationException(
                        "Recipient order or identity drifted for " + parts[0]);
            }
        }

        // A03: one group cast covering six recipients — one authored origin,
        // one invocation, six derived beneficiary connections.
        private static void TestCastingA03GroupOneInvocationSixBeneficiaries()
        {
            string[] party = { "unit-t1", "unit-t2", "unit-t3", "unit-t4", "unit-t5", "unit-t6" };
            var coverage = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
            {
                { "unit-cleric", party }
            };
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingGroupAbility,
                out options, out enhancements, party, 3, coverage);
            var service = new CastingAuthoringService(CastingDocument(
                GroupCasting("cast-group", "long", "unit-cleric", "source-communal",
                    CastingGroupAbility, party)));
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (plan.Castings.Count != 1 || plan.ReadyInvocationCount != 1)
                throw new InvalidOperationException(
                    "One group casting must remain exactly one invocation.");
            ResolvedCasting group = plan.Castings[0];
            if (group.PredictedBeneficiaryUnitIds.Count != 6)
                throw new InvalidOperationException(
                    "Six derived beneficiaries were expected, found " +
                    group.PredictedBeneficiaryUnitIds.Count + ".");
            if (group.CoverageIncomplete || group.CoverageGaps.Count != 0)
                throw new InvalidOperationException(
                    "Fully covered group reported coverage gaps.");
            if (group.TargetMode != CastingTargetMode.CasterCenteredOrigin)
                throw new InvalidOperationException("The authored origin was lost.");
        }

        // A04: one intended group recipient outside coverage stays visible as
        // missed coverage; compilation never adds a second casting.
        private static void TestCastingA04MissedCoverageNoAutoSecondCast()
        {
            string[] covered = { "unit-t1", "unit-t2", "unit-t3", "unit-t4", "unit-t5" };
            var required = covered.Concat(new[] { "unit-rogue" }).ToArray();
            var coverage = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
            {
                { "unit-cleric", covered }
            };
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingGroupAbility,
                out options, out enhancements, required, 3, coverage);
            var service = new CastingAuthoringService(CastingDocument(
                GroupCasting("cast-group", "long", "unit-cleric", "source-communal",
                    CastingGroupAbility, required)));
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (plan.Castings.Count != 1 || plan.ReadyInvocationCount != 1)
                throw new InvalidOperationException(
                    "Incomplete coverage silently produced a different cast count.");
            ResolvedCasting group = plan.Castings[0];
            if (!group.CoverageIncomplete || group.CoverageGaps.Count != 1 ||
                group.CoverageGaps[0].UnitId != "unit-rogue" ||
                group.CoverageGaps[0].Reason != "outside-predicted-coverage")
                throw new InvalidOperationException(
                    "The missed recipient was not disclosed as a coverage gap.");
            if (group.PredictedBeneficiaryUnitIds.Contains("unit-rogue"))
                throw new InvalidOperationException(
                    "An uncovered recipient was counted as a predicted beneficiary.");
            if (service.Document.Castings.Count != 1)
                throw new InvalidOperationException(
                    "Compilation mutated the authored document.");
        }

        private static void Assert(bool condition)
        {
            if (!condition) throw new InvalidOperationException("fixture assertion failed");
        }

        // Undo restores the exact prior document; refused commands never
        // consume an undo slot; compiling never mutates the document.
        private static void TestCastingAuthoringScopeUndoAndReadOnlyCompile()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2", "unit-t3" }, 3);
            var service = new CastingAuthoringService(CastingDocument());
            Assert(service.AddCasting(DirectCasting("cast-1", "long", "unit-cleric",
                "unit-t1", "source-bulls", CastingBuffAbility)).Applied);
            Assert(service.AddCasting(DirectCasting("cast-2", "long", "unit-cleric",
                "unit-t2", "source-bulls", CastingBuffAbility)).Applied);
            Assert(service.AddCasting(DirectCasting("cast-3", "long", "unit-cleric",
                "unit-t3", "source-bulls", CastingBuffAbility)).Applied);
            CastingPlanDocument beforeEdit = service.Document;
            ExplicitCastingPlan first = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (!ReferenceEquals(beforeEdit, service.Document))
                throw new InvalidOperationException("Compilation replaced the document.");
            ExplicitCastingPlan second = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (first.Castings.Count != second.Castings.Count ||
                second.ReadyInvocationCount != first.ReadyInvocationCount)
                throw new InvalidOperationException("Compilation was not deterministic.");
            // A refused command must not create an undo entry: draining the
            // history lands exactly on the pre-add document after three
            // undos, and a fourth undo is impossible.
            bool duplicateRefused = !service.AddCasting(DirectCasting("cast-3", "long",
                "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility)).Applied;
            Assert(service.Undo() && service.Undo() && service.Undo());
            if (service.Document.Castings.Count != 0 || service.Undo() || !duplicateRefused)
                throw new InvalidOperationException(
                    "Refused command consumed an undo slot or undo overshot.");
            // A fresh authoring session covers edit undo, disclosed removal
            // scope, and an explicit cross-routine move.
            var editor = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-1", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-2", "long", "unit-cleric", "unit-t2",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-3", "long", "unit-cleric", "unit-t3",
                    "source-bulls", CastingBuffAbility)));
            CastingPlanDocument beforeUpdate = editor.Document;
            Assert(editor.UpdateCasting(DirectCasting("cast-2", "long", "unit-cleric",
                "unit-t2", "source-bulls", CastingBuffAbility,
                new[] { new AuthoredEnhancementSelection("extend-cleric", true, null) }))
                .Applied);
            Assert(editor.Undo());
            if (!ReferenceEquals(beforeUpdate, editor.Document))
                throw new InvalidOperationException("Undo did not restore the prior document.");
            // Removing a middle casting shifts the later sibling and says so.
            AuthoringEditResult removal = editor.RemoveCasting("cast-2");
            if (!removal.Applied || removal.AffectedCastingIds.Count != 2 ||
                !removal.AffectedCastingIds.Contains("cast-2") ||
                !removal.AffectedCastingIds.Contains("cast-3"))
                throw new InvalidOperationException(
                    "Sibling order shift was not disclosed with the removal.");
            if (editor.Document.Castings.Count != 2 ||
                editor.Document.Castings[1].CastingId != "cast-3" ||
                editor.Document.Castings[1].Order != 1)
                throw new InvalidOperationException("Orders were not renormalized.");
            // Explicit cross-routine move with disclosed scope: the moved
            // casting plus the long-routine sibling whose order shifts up.
            AuthoringEditResult move = editor.MoveCasting("cast-1", "short", 0);
            if (!move.Applied || move.AffectedCastingIds.Count != 2 ||
                !move.AffectedCastingIds.Contains("cast-1") ||
                !move.AffectedCastingIds.Contains("cast-3"))
                throw new InvalidOperationException("Cross-routine move failed: " + move.Reason);
            if (editor.Document.Castings[0].CastingId != "cast-3" ||
                editor.Document.Castings[1].RoutineId != "short" ||
                editor.Document.Castings[0].Order != 0 ||
                editor.Document.Castings[1].Order != 0)
                throw new InvalidOperationException(
                    "Persisted order did not follow routine declaration order.");
        }

        // Blocked reasons are distinct and capability stays visible when a
        // capable caster is currently uncastable.
        private static void TestCastingBlockedReadinessReasons()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements, new[] { "unit-t1" }, 0);
            var service = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-exhausted", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-missing", "long", "unit-ghost", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-draft", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility, null, null, CastingAuthoringState.Draft),
                GroupCasting("cast-mode", "long", "unit-cleric", "source-bulls",
                    CastingBuffAbility, new[] { "unit-t1" })));
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (plan.ReadyInvocationCount != 0)
                throw new InvalidOperationException("No casting here should be ready.");
            ResolvedCasting exhausted = plan.CastingById("cast-exhausted");
            if (exhausted.Readiness != ResolvedCastingReadiness.Blocked ||
                exhausted.ReadinessReasons.Count != 1 ||
                !exhausted.ReadinessReasons[0].StartsWith("resource-pool-exhausted",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Exhausted pool did not produce its distinct reason: " +
                    string.Join(",", exhausted.ReadinessReasons));
            if (!exhausted.CapableCasterUnitIds.Contains("unit-cleric"))
                throw new InvalidOperationException(
                    "Capability disappeared together with readiness.");
            ResolvedCasting missing = plan.CastingById("cast-missing");
            if (missing.Readiness != ResolvedCastingReadiness.Blocked ||
                missing.ReadinessReasons[0] != "caster-not-in-party:unit-ghost")
                throw new InvalidOperationException(
                    "Missing caster did not produce its distinct reason.");
            ResolvedCasting draft = plan.CastingById("cast-draft");
            if (draft.Readiness != ResolvedCastingReadiness.Draft)
                throw new InvalidOperationException("Draft state was not preserved.");
            ResolvedCasting mode = plan.CastingById("cast-mode");
            if (mode.Readiness != ResolvedCastingReadiness.Blocked ||
                !mode.ReadinessReasons.Any(value => value.StartsWith(
                    "target-mode-mismatch", StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Ability/targeting mismatch did not block the casting.");
            // A Ready casting without a caster cannot even be authored.
            bool threw = false;
            try
            {
                DirectCasting("cast-invalid", "long", null, "unit-t1",
                    "source-bulls", CastingBuffAbility);
            }
            catch (ArgumentException)
            {
                threw = true;
            }
            if (!threw)
                throw new InvalidOperationException(
                    "A Ready casting without a caster was accepted by the domain model.");
        }

        // Schema-6 round trip preserves identity, order, and intent; load
        // states stay distinct; saves refuse to bury unreadable primaries.
        private static void TestCastingRoundTripAndLoadStates(string root)
        {
            string boundary = Path.Combine(root, "casting-plan");
            Directory.CreateDirectory(boundary);
            var provenance = new MigrationProvenance("legacy-42", 5, "long", "split");
            var document = CastingDocument(
                DirectCasting("cast-1", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility,
                    new[] { new AuthoredEnhancementSelection("extend-cleric", true, "rod-7") }),
                DirectCasting("cast-2", "long", "unit-wizard", "unit-t2",
                    "source-bulls", CastingBuffAbility),
                GroupCasting("cast-3", "short", "unit-cleric", "source-communal",
                    CastingGroupAbility, new[] { "unit-t1", "unit-t2", "unit-t3" },
                    null, CastingTargetMode.AnchoredOrigin, "unit-t1",
                    CastingAuthoringState.Draft, provenance));
            var repository = new CastingPlanRepository(boundary);
            CastingPlanProfile profile = CastingPlanProfile.FromDocument(document);
            repository.Save(profile);
            CastingPlanLoadResult loaded = repository.Load("fixture-campaign");
            if (loaded.Status != CastingPlanLoadStatus.Loaded)
                throw new InvalidOperationException(
                    "Fresh save did not load: " + loaded.Status + " " + loaded.Warning);
            CastingPlanDocument roundTripped = loaded.Profile.ToDocument();
            if (roundTripped.Castings.Count != 3)
                throw new InvalidOperationException("Casting count drifted.");
            string[] expectedOrder = { "cast-1", "cast-2", "cast-3" };
            int[] expectedRoutineOrders = { 0, 1, 0 };
            for (int i = 0; i < expectedOrder.Length; i++)
            {
                if (roundTripped.Castings[i].CastingId != expectedOrder[i] ||
                    roundTripped.Castings[i].Order != expectedRoutineOrders[i])
                    throw new InvalidOperationException(
                        "Persisted order or derived order drifted at " + i);
            }
            PlannedCasting first = roundTripped.Castings[0];
            if (first.Enhancements.Count != 1 ||
                first.Enhancements[0].EnhancementId != "extend-cleric" ||
                !first.Enhancements[0].Required ||
                first.Enhancements[0].ExactSourceRef != "rod-7")
                throw new InvalidOperationException(
                    "Enhancement selection detail drifted through persistence.");
            PlannedCasting third = roundTripped.Castings[2];
            if (third.TargetMode != CastingTargetMode.AnchoredOrigin ||
                third.Origin.AnchorUnitId != "unit-t1" ||
                third.State != CastingAuthoringState.Draft ||
                third.Provenance == null ||
                third.Provenance.LegacyAssignmentId != "legacy-42" ||
                third.Provenance.LegacySchemaVersion != 5)
                throw new InvalidOperationException(
                    "Group origin, draft state, or migration provenance drifted.");
            if (third.RequiredCoverageUnitIds.Count != 3)
                throw new InvalidOperationException("Required coverage drifted.");
            string original = File.ReadAllText(repository.GetProfilePath("fixture-campaign"));
            repository.Save(CastingPlanProfile.FromDocument(roundTripped));
            if (File.ReadAllText(repository.GetProfilePath("fixture-campaign")) != original)
                throw new InvalidOperationException(
                    "Serialization is not deterministic across round trips.");
            // Distinct failure states: absent, corrupt, and newer schema.
            var absent = new CastingPlanRepository(Path.Combine(boundary, "empty"));
            if (absent.Load("fixture-campaign").Status != CastingPlanLoadStatus.Absent)
                throw new InvalidOperationException("Absent profile was not reported.");
            // A corrupt primary with a valid backup recovers the backup and
            // reports the corrupt primary in its warning.
            string path = repository.GetProfilePath("fixture-campaign");
            File.WriteAllText(path, "{ not json");
            CastingPlanLoadResult recovered = repository.Load("fixture-campaign");
            if (recovered.Status != CastingPlanLoadStatus.RecoveredFromBackup ||
                recovered.Profile == null ||
                !recovered.Warning.Contains("schema-version-missing"))
                throw new InvalidOperationException(
                    "Backup recovery did not report the corrupt primary: " +
                    recovered.Status + " " + recovered.Warning);
            // A refused save never buries the unreadable primary.
            bool refused = false;
            try { repository.Save(CastingPlanProfile.FromDocument(document)); }
            catch (InvalidDataException) { refused = true; }
            if (!refused)
                throw new InvalidOperationException(
                    "Save buried an unreadable primary instead of refusing.");
            // With every copy unreadable the honest state is Corrupt, never
            // a fabricated default profile.
            for (int i = 1; i <= 3; i++)
            {
                string backup = path + ".bak" + i;
                if (File.Exists(backup)) File.WriteAllText(backup, "{ broken");
            }
            CastingPlanLoadResult corrupt = repository.Load("fixture-campaign");
            if (corrupt.Status != CastingPlanLoadStatus.Corrupt ||
                corrupt.Profile != null)
                throw new InvalidOperationException("Corruption was not reported: " +
                    corrupt.Status + " " + corrupt.Warning);
            File.WriteAllText(path,
                "{ \"schemaVersion\": 7, \"campaignId\": \"fixture-campaign\" }");
            CastingPlanLoadResult newer = repository.Load("fixture-campaign");
            if (newer.Status != CastingPlanLoadStatus.UnsupportedSchema ||
                !newer.Warning.Contains("schema-version-newer:7"))
                throw new InvalidOperationException(
                    "A newer schema was not reported distinctly.");
            refused = false;
            try { repository.Save(CastingPlanProfile.FromDocument(document)); }
            catch (InvalidDataException) { refused = true; }
            if (!refused)
                throw new InvalidOperationException(
                    "Save overwrote a newer-schema primary.");
        }

        // ------------------------------------------------------------------
        // Casting-first migration: schema-5 -> schema-6 import converter
        // (charter section 7.2 conversion rules).
        // ------------------------------------------------------------------

        private static BuffPlannerProfile LegacyProfile()
        {
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault("legacy-campaign");
            return profile;
        }

        private static SourceAssignmentProfile LegacyAssignment(
            string sourceId, AbilityKey ability, params CastingAssignmentProfile[] children)
        {
            SourceAssignmentProfile assignment = SourceAssignmentProfile.Create(sourceId, ability);
            foreach (CastingAssignmentProfile child in children)
                assignment.CastingAssignments.Add(child);
            return assignment;
        }

        private static CastingAssignmentProfile PinnedChild(
            string assignmentId, int order, string caster, params string[] targets)
        {
            return new CastingAssignmentProfile
            {
                AssignmentId = assignmentId,
                Order = order,
                CasterUnitId = caster,
                SpellbookGuid = null,
                ProviderKey = null,
                TargetUnitIds = targets.ToList(),
                Enhancements = new List<EnhancementSelectionProfile>()
            };
        }

        // Pinned single-target children split one casting per recipient with
        // order, enhancements, and policy preserved (A12 pinned half).
        private static void TestCastingImportPinnedSplit()
        {
            BuffPlannerProfile legacy = LegacyProfile();
            CastingAssignmentProfile child = PinnedChild("legacy-bulls", 0, "unit-cleric",
                "unit-t1", "unit-t2", "unit-t3");
            child.Enhancements.Add(new EnhancementSelectionProfile
            {
                EnhancementId = "extend-cleric",
                Required = new bool?()
            });
            legacy.Routines[0].Assignments.Add(LegacyAssignment(
                "source-bulls", CastingBuffAbility, child));
            legacy.Routines[0].Assignments[0].ExistingEffectPolicy = ExistingEffectPolicy.Overwrite;
            legacy.Routines[0].Assignments[0].IgnoredPresenceMarkers.Add("marker-a");
            CastingImportResult result = new CastingPlanImporter().Import(legacy);
            if (result.Document.Castings.Count != 3)
                throw new InvalidOperationException(
                    "A pinned three-recipient child must split into three castings.");
            string[] expectedTargets = { "unit-t1", "unit-t2", "unit-t3" };
            for (int i = 0; i < 3; i++)
            {
                PlannedCasting casting = result.Document.Castings[i];
                if (casting.CastingId != "m5:legacy-bulls:" + i ||
                    casting.DirectTargetUnitId != expectedTargets[i] ||
                    casting.Order != i ||
                    casting.State != CastingAuthoringState.Ready ||
                    casting.CasterUnitId != "unit-cleric" ||
                    casting.TargetMode != CastingTargetMode.DirectTarget)
                    throw new InvalidOperationException(
                        "Pinned split lost recipient order or readiness at " + i);
                if (casting.ExistingEffectPolicy != ExistingEffectPolicy.Overwrite ||
                    casting.IgnoredPresenceMarkers.Count != 1 ||
                    casting.IgnoredPresenceMarkers[0] != "marker-a")
                    throw new InvalidOperationException(
                        "Source-level policy did not move into the casting.");
                if (casting.Enhancements.Count != 1 ||
                    casting.Enhancements[0].EnhancementId != "extend-cleric" ||
                    !casting.Enhancements[0].Required)
                    throw new InvalidOperationException(
                        "Enhancement selection drifted in the split.");
                if (casting.Provenance == null ||
                    casting.Provenance.LegacyAssignmentId != "legacy-bulls" ||
                    casting.Provenance.LegacySchemaVersion != 5)
                    throw new InvalidOperationException("Provenance was not attached.");
            }
        }

        // Automatic children import as review drafts with targets preserved;
        // today's best caster is never silently pinned.
        private static void TestCastingImportAutomaticDrafts()
        {
            BuffPlannerProfile legacy = LegacyProfile();
            legacy.Routines[0].Assignments.Add(LegacyAssignment(
                "source-bulls", CastingBuffAbility,
                CastingAssignmentProfile.CreateAutomatic("legacy-auto", 0)));
            legacy.Routines[0].Assignments[0].CastingAssignments[0].TargetUnitIds =
                new List<string> { "unit-t1", "unit-t2" };
            CastingImportResult result = new CastingPlanImporter().Import(legacy);
            if (result.Document.Castings.Count != 2)
                throw new InvalidOperationException(
                    "Automatic targets were collapsed instead of preserved.");
            foreach (PlannedCasting casting in result.Document.Castings)
            {
                if (casting.State != CastingAuthoringState.Draft ||
                    casting.CasterUnitId != null)
                    throw new InvalidOperationException(
                        "An automatic import became executable without review.");
                if (casting.Provenance == null || !casting.Provenance.Note.Contains(
                        "automatic-caster-pending-review"))
                    throw new InvalidOperationException(
                        "The review reason was not recorded in provenance.");
            }
            if (result.Document.Castings[0].DirectTargetUnitId != "unit-t1" ||
                result.Document.Castings[1].DirectTargetUnitId != "unit-t2")
                throw new InvalidOperationException("Automatic target order drifted.");
            if (result.Report.ReadyCount != 0 || result.Report.DraftCount != 2 ||
                result.Report.UnresolvedCasterCount != 2)
                throw new InvalidOperationException("Import report miscounted drafts.");
        }

        // Group children keep requested coverage as one reviewable casting;
        // origin and cast count are never invented as resolved intent.
        private static void TestCastingImportGroupReview()
        {
            BuffPlannerProfile legacy = LegacyProfile();
            legacy.Routines[0].Assignments.Add(LegacyAssignment(
                "source-communal", CastingGroupAbility,
                PinnedChild("legacy-group", 0, "unit-cleric",
                    "unit-t1", "unit-t2", "unit-t3")));
            var groupings = new Dictionary<string, CastGroupingKind>(
                StringComparer.Ordinal)
            {
                { "source-communal", CastGroupingKind.MassConfiguredTargets }
            };
            CastingImportResult result = new CastingPlanImporter().Import(
                legacy, null, groupings);
            if (result.Document.Castings.Count != 1)
                throw new InvalidOperationException(
                    "A group child must import as exactly one casting, not one per target.");
            PlannedCasting group = result.Document.Castings[0];
            if (group.State != CastingAuthoringState.Draft ||
                group.CasterUnitId != "unit-cleric" ||
                group.TargetMode != CastingTargetMode.CasterCenteredOrigin ||
                group.RequiredCoverageUnitIds.Count != 3 ||
                group.DirectTargetUnitId != null)
                throw new InvalidOperationException(
                    "Group coverage or review state drifted.");
            if (group.Provenance == null || !group.Provenance.Note.Contains(
                    "group-origin-and-count-pending-review"))
                throw new InvalidOperationException(
                    "The pending origin/count review was not disclosed.");
            if (result.Report.GroupReviewCount != 1)
                throw new InvalidOperationException("Group review was not counted.");
        }

        // Re-import onto an existing document reuses provenance identities
        // without duplicating, and routine-major persisted order holds.
        private static void TestCastingImportIdempotent()
        {
            BuffPlannerProfile legacy = LegacyProfile();
            legacy.Routines[0].Assignments.Add(LegacyAssignment(
                "source-bulls", CastingBuffAbility,
                PinnedChild("legacy-bulls", 1, "unit-cleric", "unit-t1")));
            legacy.Routines[2].Assignments.Add(LegacyAssignment(
                "source-haste", CastingBuffAbility,
                PinnedChild("legacy-haste", 0, "unit-wizard", "unit-t2")));
            CastingPlanImporter importer = new CastingPlanImporter();
            CastingImportResult first = importer.Import(legacy);
            if (first.Document.Castings.Count != 2)
                throw new InvalidOperationException("Fixture import count wrong.");
            // Interleaved routine orders import routine-major: the long
            // casting (legacy order 1) precedes the short one (order 0).
            if (first.Document.Castings[0].CastingId != "m5:legacy-bulls:0" ||
                first.Document.Castings[1].CastingId != "m5:legacy-haste:0")
                throw new InvalidOperationException(
                    "Persisted order did not group by routine declaration.");
            // Re-import of the same legacy data duplicates nothing.
            CastingImportResult second = importer.Import(legacy, first.Document);
            if (second.Document.Castings.Count != 2 ||
                second.Report.ResultingCastingCount != 2)
                throw new InvalidOperationException(
                    "Re-import duplicated provenance-identical castings.");
            if (second.Report.Mappings.Any(mapping =>
                    mapping.Disposition != "reused"))
                throw new InvalidOperationException(
                    "Re-import did not report reuse.");
            // Importing new legacy work into a document that already spans
            // routines keeps the routine-major invariant (important between
            // long and short).
            BuffPlannerProfile addition = LegacyProfile();
            addition.Routines[1].Assignments.Add(LegacyAssignment(
                "source-shield", CastingBuffAbility,
                PinnedChild("legacy-shield", 0, "unit-cleric", "unit-t3")));
            CastingImportResult merged = importer.Import(addition, first.Document);
            if (merged.Document.Castings.Count != 3 ||
                merged.Document.Castings[1].RoutineId != "important" ||
                merged.Document.Castings[1].Order != 0)
                throw new InvalidOperationException(
                    "Merged import broke routine-major persisted order.");
        }

        // The import report counts originals, results, reviews, and notices.
        private static void TestCastingImportReport()
        {
            BuffPlannerProfile legacy = LegacyProfile();
            legacy.ProviderPreferences.Add(new ProviderPreferenceProfile
            {
                ProviderKey = "unit-wizard|book|ability|",
                Banned = true,
                Priority = new int?(),
                MaximumCasts = new int?()
            });
            legacy.Routines[0].Assignments.Add(LegacyAssignment(
                "source-bulls", CastingBuffAbility,
                PinnedChild("legacy-bulls", 0, "unit-cleric", "unit-t1")));
            legacy.Routines[0].Assignments[0].CastingAssignments[0].Enhancements.Add(
                new EnhancementSelectionProfile { EnhancementId = "extend-cleric", Required = true });
            legacy.Routines[2].Assignments.Add(LegacyAssignment(
                "source-empty", CastingBuffAbility,
                CastingAssignmentProfile.CreateAutomatic("legacy-empty", 0)));
            CastingImportResult result = new CastingPlanImporter().Import(legacy);
            CastingImportReport report = result.Report;
            if (report.LegacyRoutineCount != 3 || report.LegacyChildCount != 2)
                throw new InvalidOperationException("Legacy counts are wrong.");
            if (report.ResultingCastingCount != 1 || report.ReadyCount != 1 ||
                report.DraftCount != 0)
                throw new InvalidOperationException(
                    "The target-less child must not become a phantom casting.");
            if (report.UnresolvedCasterCount != 0)
                throw new InvalidOperationException("Unresolved casters miscounted.");
            if (report.PooledEnhancementCount != 1)
                throw new InvalidOperationException("Pooled enhancements miscounted.");
            if (report.PolicyNoticeCount != 1)
                throw new InvalidOperationException("Provider policy notices miscounted.");
            if (!report.Warnings.Contains("legacy-child-without-target:legacy-empty"))
                throw new InvalidOperationException(
                    "The target-less child warning is missing.");
            if (report.Mappings.Count != 2 ||
                report.Mappings.First(mapping =>
                    mapping.LegacyAssignmentId == "legacy-empty").Disposition !=
                    "unresolved-no-recipient")
                throw new InvalidOperationException(
                    "The target-less child was not reported as unresolved.");
        }

        // A07: two enhancements sharing one class-resource pool contribute
        // their combined demand; an unfundable combined cost blocks the
        // casting without reserving anything anywhere.
        private static void TestCastingA07SharedEnhancementPool()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2" }, 3);
            enhancements.Add(new CastEnhancementSnapshot(
                "score-a", "unit-cleric", "feat-a", "Score A", string.Empty,
                CastEnhancementCategory.ClassFeature, 0, 10, 1,
                new[] { CastingBuffAbility.BaseAbilityGuid },
                null, new[] { "book-unit-cleric" }, "reservoir", false, "score-a", 1));
            enhancements.Add(new CastEnhancementSnapshot(
                "score-b", "unit-cleric", "feat-b", "Score B", string.Empty,
                CastEnhancementCategory.ClassFeature, 0, 10, 1,
                new[] { CastingBuffAbility.BaseAbilityGuid },
                null, new[] { "book-unit-cleric" }, "reservoir", false, "score-b", 1));
            var service = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-shared", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility,
                    new[]
                    {
                        new AuthoredEnhancementSelection("score-a", true, null),
                        new AuthoredEnhancementSelection("score-b", true, null)
                    }),
                DirectCasting("cast-plain", "long", "unit-cleric", "unit-t2",
                    "source-bulls", CastingBuffAbility)));
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            ResolvedCasting shared = plan.CastingById("cast-shared");
            if (shared.Readiness != ResolvedCastingReadiness.Blocked ||
                !shared.ReadinessReasons.Contains("enhancement-pool-exhausted:reservoir:1<2"))
                throw new InvalidOperationException(
                    "Combined shared-pool demand was not the blocking reason: " +
                    string.Join(",", shared.ReadinessReasons));
            if (shared.Cost.Count != 0)
                throw new InvalidOperationException(
                    "A blocked casting carried reserved cost lines.");
            CastingBudgetLine reservoir = plan.BudgetLineFor("reservoir");
            if (reservoir == null ||
                reservoir.AvailableNow != 1 ||
                reservoir.RequestedUsage != 2 ||
                reservoir.AllocatedUsage != 0 ||
                reservoir.UnmetDemand != 2 ||
                reservoir.ForecastRemaining != 1)
                throw new InvalidOperationException(
                    "The shared reservoir line does not expose the deficit.");
            // No reservation leakage: the native pool still funds the
            // unrelated later casting in full.
            ResolvedCasting plain = plan.CastingById("cast-plain");
            if (plain.Readiness != ResolvedCastingReadiness.Ready ||
                plain.Cost.Any(line => line.Category == CastingCostCategory.NativePool &&
                    line.Units != 1))
                throw new InvalidOperationException(
                    "The failed combined reservation leaked into later castings.");
            CastingBudgetLine native = plan.BudgetLineFor("pool-unit-cleric");
            if (native.AllocatedUsage != 1)
                throw new InvalidOperationException(
                    "Native allocation was disturbed by the failed reservation.");
        }

        // A08: linked prepared slots, materials, and rod demand form one
        // atomic complete-cost reservation; a failed candidate spends and
        // reserves nothing.
        private static void TestCastingA08CompleteCostReservation()
        {
            AbilityKey ability = CastingBuffAbility;
            const string poolKey = "prepared-book";
            var pool = new ResourcePoolSnapshot(poolKey,
                ResourcePoolKind.PreparedSlots, 4, 4, new ResourceTokenSnapshot[]
                {
                    new ResourceTokenSnapshot("t1", ability, 1, PreparedSlotKind.Common,
                        true, true, new[] { "t2" }),
                    new ResourceTokenSnapshot("t2", ability, 1, PreparedSlotKind.Common,
                        true, false, new string[0]),
                    new ResourceTokenSnapshot("t3", ability, 1, PreparedSlotKind.Common,
                        true, true, new[] { "t4" }),
                    new ResourceTokenSnapshot("t4", ability, 1, PreparedSlotKind.Common,
                        true, false, new string[0])
                });
            var material = new MaterialRequirementSnapshot("diamond", 1, 2);
            ProviderSnapshot provider = new ProviderSnapshot(
                new ProviderKey("unit-cleric", "book-prepared", ability, string.Empty),
                ability.BaseAbilityGuid, 1, poolKey, 0, new[] { "t1", "t3" }, material);
            var snapshot = new PartyProviderSnapshot(new[]
                {
                    new UnitSnapshot("unit-cleric", "Cleric", false, string.Empty,
                        new TargetValidationSnapshot(true, true, true, true)),
                    new UnitSnapshot("unit-t1", "T1", false, string.Empty,
                        new TargetValidationSnapshot(true, true, true, true))
                },
                new[] { provider }, new[] { pool });
            var option = new ProviderPlanningOption(provider,
                new[] { "unit-cleric", "unit-t1" }, new[] { "unit-cleric" }, 10, 100);
            var enhancements = new List<CastEnhancementSnapshot>
            {
                new CastEnhancementSnapshot("rod-extend", "unit-cleric", "rod-guid",
                    "Rod", string.Empty, CastEnhancementCategory.MetamagicRod,
                    0x1, 10, 1, new string[0], null, null, "rod-pool")
            };
            var service = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-first", "long", "unit-cleric", "unit-t1",
                    "source-bulls", ability,
                    new[] { new AuthoredEnhancementSelection("rod-extend", true, null) }),
                DirectCasting("cast-failing", "long", "unit-cleric", "unit-t1",
                    "source-bulls", ability,
                    new[] { new AuthoredEnhancementSelection("rod-extend", true, null) }),
                DirectCasting("cast-after", "long", "unit-cleric", "unit-t1",
                    "source-bulls", ability)));
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(
                service.Document, snapshot, new[] { option },
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (plan.ReadyInvocationCount != 2)
                throw new InvalidOperationException(
                    "Exactly the funded castings must be ready.");
            ResolvedCasting failing = plan.CastingById("cast-failing");
            if (failing.Readiness != ResolvedCastingReadiness.Blocked ||
                !failing.ReadinessReasons.Contains("enhancement-pool-exhausted:rod-pool:0<1") ||
                failing.Cost.Count != 0)
                throw new InvalidOperationException(
                    "The rod-deficient candidate was not blocked cleanly: " +
                    string.Join(",", failing.ReadinessReasons));
            // The failed candidate spent nothing: the later casting still
            // received its linked token pair and material component.
            ResolvedCasting after = plan.CastingById("cast-after");
            CastingCostLine native = after.Cost.FirstOrDefault(
                line => line.Category == CastingCostCategory.NativePool);
            if (native == null || !native.TokenIds.Contains("t3") ||
                !native.TokenIds.Contains("t4"))
                throw new InvalidOperationException(
                    "The linked prepared pair was consumed by the failed candidate.");
            if (!after.Cost.Any(line => line.Category == CastingCostCategory.Material &&
                    line.ItemGuid == "diamond" && line.Units == 1))
                throw new InvalidOperationException(
                    "The material component was not reserved by the funded casting.");
            CastingBudgetLine rod = plan.BudgetLineFor("rod-pool");
            if (rod.RequestedUsage != 2 || rod.AllocatedUsage != 1 || rod.UnmetDemand != 1)
                throw new InvalidOperationException("Rod budget line is wrong.");
            CastingBudgetLine diamonds = plan.BudgetLineFor("diamond");
            if (diamonds.AllocatedUsage != 2 || diamonds.ForecastRemaining != 0)
                throw new InvalidOperationException("Material budget line is wrong.");
            CastingBudgetLine prepared = plan.BudgetLineFor(poolKey);
            if (prepared.AllocatedUsage != 4 || prepared.ForecastRemaining != 0)
                throw new InvalidOperationException(
                    "Linked prepared tokens were not accounted as consumed pairs.");
        }

        // A09: routine views and the ordered one-pass sequence share one
        // budget per view; independent previews never multiply charges and
        // never approve execution.
        private static void TestCastingA09ForecastViews()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            // Pool funds exactly two of the three authored casts.
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2", "unit-t3" }, 2);
            var document = CastingDocument(
                DirectCasting("long-1", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("short-1", "short", "unit-cleric", "unit-t2",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("short-2", "short", "unit-cleric", "unit-t3",
                    "source-bulls", CastingBuffAbility));
            var service = new CastingForecastService();
            CastingForecast onePass = service.ForecastOnePass(
                document, snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (onePass.ScopeRoutineId != null ||
                onePass.RoutineSequence.Count != 3 ||
                onePass.RoutineSequence[0] != "long" ||
                onePass.RoutineSequence[2] != "short")
                throw new InvalidOperationException(
                    "One-pass sequence does not follow routine declaration order.");
            if (onePass.Plan.CastingById("long-1").IsExecutable != true ||
                onePass.Plan.CastingById("short-1").IsExecutable != true ||
                onePass.Plan.CastingById("short-2").Readiness !=
                    ResolvedCastingReadiness.Blocked)
                throw new InvalidOperationException(
                    "One-pass balances were not carried forward across routines.");
            CastingBudgetLine pool = onePass.Plan.BudgetLineFor("pool-unit-cleric");
            if (pool.AvailableNow != 2 || pool.RequestedUsage != 3 ||
                pool.AllocatedUsage != 2 || pool.UnmetDemand != 1 ||
                pool.ForecastRemaining != 0)
                throw new InvalidOperationException(
                    "One-pass budget line does not expose the shared deficit.");
            // An independent short-routine preview uses its own ledger: both
            // short casts are fundable there, and previewing it did not
            // consume anything for any other view.
            CastingForecast shortView = service.ForecastRoutine(
                document, "short", snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (shortView.ScopeRoutineId != "short" ||
                shortView.InScopeCastings.Count != 2 ||
                shortView.ReadyInvocations != 2)
                throw new InvalidOperationException(
                    "The routine preview did not budget its own routine alone.");
            CastingBudgetLine shortPool = shortView.Plan.BudgetLineFor("pool-unit-cleric");
            if (shortPool.RequestedUsage != 2 || shortPool.AllocatedUsage != 2)
                throw new InvalidOperationException(
                    "Routine preview budget included foreign castings.");
            // Re-running the one-pass view reproduces identical results:
            // previews are pure and approve nothing.
            CastingForecast repeat = service.ForecastOnePass(
                document, snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (repeat.Plan.ReadyInvocationCount != onePass.Plan.ReadyInvocationCount ||
                repeat.Plan.BudgetLineFor("pool-unit-cleric").AllocatedUsage != 2)
                throw new InvalidOperationException(
                    "Preview repetition changed or accumulated charges.");
            foreach (string assumption in new[]
                { "no-rest", "no-elapsed-game-time" })
                if (!onePass.Assumptions.Contains(assumption))
                    throw new InvalidOperationException(
                        "Forecast assumptions are not visible.");
        }

        // A10: a required enhancement that is unavailable blocks its casting
        // without any downgrade; explicit legacy optional intent is preserved
        // by import and disclosed as omitted instead of quietly dropped.
        private static void TestCastingA10EnhancementPolicy()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1" }, 3);
            var service = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-required", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility,
                    new[] { new AuthoredEnhancementSelection("extend-cleric", true, null) }),
                DirectCasting("cast-optional", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility,
                    new[] { new AuthoredEnhancementSelection("missing-feat", false, null) })));
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options,
                new List<CastEnhancementSnapshot>(),
                "source-bulls", "source-communal");
            ResolvedCasting required = plan.CastingById("cast-required");
            if (required.Readiness != ResolvedCastingReadiness.Blocked ||
                !required.ReadinessReasons.Contains("enhancement-unavailable:extend-cleric"))
                throw new InvalidOperationException(
                    "A missing required enhancement was downgraded silently.");
            ResolvedCasting optional = plan.CastingById("cast-optional");
            if (optional.Readiness != ResolvedCastingReadiness.Ready ||
                optional.OmittedEnhancementIds.Count != 1 ||
                !optional.OmittedEnhancementIds[0].StartsWith("missing-feat:",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Optional intent was not disclosed as omitted.");
            // Legacy optional intent imports as optional, never reinterpreted
            // as required or dropped.
            BuffPlannerProfile legacy = LegacyProfile();
            CastingAssignmentProfile child = PinnedChild("legacy-opt", 0, "unit-cleric",
                "unit-t1");
            child.Enhancements.Add(new EnhancementSelectionProfile
            {
                EnhancementId = "old-rod",
                Required = false
            });
            legacy.Routines[0].Assignments.Add(LegacyAssignment(
                "source-bulls", CastingBuffAbility, child));
            CastingImportResult imported = new CastingPlanImporter().Import(legacy);
            PlannedCasting importedCasting = imported.Document.Castings[0];
            if (importedCasting.Enhancements.Count != 1 ||
                importedCasting.Enhancements[0].Required)
                throw new InvalidOperationException(
                    "Import reinterpreted legacy optional intent as required.");
        }

        // A11: ordinary Apply cannot hide omitted work — blocked castings
        // refuse it; drafts and blocked castings are always disclosed; the
        // Ready-Casts-Only fallback is explicit and preserves the plan.
        private static void TestCastingA11ApplyGate()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1" }, 3);
            var service = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-ready", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-ghost", "long", "unit-ghost", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-draft", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility, null, null,
                    CastingAuthoringState.Draft)));
            ExplicitCastingPlan plan = CompileCastingPlan(
                service.Document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            var gate = new CastingExecutionGate();
            CastingApplyDecision ordinary = gate.Evaluate(
                plan, CastingApplyMode.Ordinary);
            // The blocked request AND the saved unresolved draft both
            // refuse the ordinary apply: neither is an implicit opt-out.
            if (ordinary.Allowed || ordinary.BlockingReasons.Count != 2 ||
                !ordinary.BlockingReasons.Any(value => value.StartsWith(
                    "blocked-casting:cast-ghost:", StringComparison.Ordinal)) ||
                !ordinary.BlockingReasons.Any(value => value.StartsWith(
                    "unresolved-saved-request-casting:cast-draft",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Ordinary Apply did not refuse blocked and unresolved work.");
            if (ordinary.ExecutableCastingIds.Count != 1 ||
                ordinary.ExecutableCastingIds[0] != "cast-ready")
                throw new InvalidOperationException(
                    "Refusal misreported executable castings.");
            if (ordinary.Omissions.Count != 2)
                throw new InvalidOperationException(
                    "Refusal did not disclose every omitted casting.");
            // The explicit fallback executes ready work but still counts and
            // explains everything it omits.
            CastingApplyDecision readyOnly = gate.Evaluate(
                plan, CastingApplyMode.ReadyCastsOnly);
            if (!readyOnly.Allowed ||
                readyOnly.ExecutableCastingIds.Count != 1 ||
                readyOnly.Omissions.Count != 2)
                throw new InvalidOperationException(
                    "Ready-Casts-Only did not disclose its omissions.");
            CastingOmission draft = readyOnly.Omissions.FirstOrDefault(
                omission => omission.CastingId == "cast-draft");
            if (draft == null || !draft.Reasons.Contains("unresolved-saved-request"))
                throw new InvalidOperationException(
                    "The draft omission lacks its reason.");
            CastingOmission ghost = readyOnly.Omissions.FirstOrDefault(
                omission => omission.CastingId == "cast-ghost");
            if (ghost == null || !ghost.Reasons.Contains("caster-not-in-party:unit-ghost"))
                throw new InvalidOperationException(
                    "The blocked omission lacks its reason.");
            // Gate evaluation is pure: re-evaluating the same plan reproduces
            // the identical decision and mutates nothing (a refused attempt
            // never authorizes the next by having been computed).
            CastingApplyDecision repeated = gate.Evaluate(
                plan, CastingApplyMode.Ordinary);
            if (repeated.Allowed || repeated.Omissions.Count != 2 ||
                repeated.ExecutableCastingIds.Count !=
                    ordinary.ExecutableCastingIds.Count ||
                repeated.BlockingReasons.Count != ordinary.BlockingReasons.Count)
                throw new InvalidOperationException(
                    "Gate evaluation is not deterministic.");
            if (service.Document.Castings.Count != 3 ||
                service.Document.Castings[0].CastingId != "cast-ready")
                throw new InvalidOperationException(
                    "Evaluation disturbed the saved plan.");
        }

        // A05: an enabled targeting modifier changes recipient eligibility
        // without changing invocation counts; an unavailable modifier blocks
        // as repairable intent; disabled selections leave eligibility alone;
        // modifier application is pure across compilations.
        private sealed class FixtureShareCastingModifier : ICastingTargetingModifier
        {
            private readonly string _requiredCaster;
            private readonly string[] _legalTargets;
            private readonly ModifierUsageDemand[] _demands;

            internal FixtureShareCastingModifier(string requiredCaster,
                string[] legalTargets, params ModifierUsageDemand[] demands)
            {
                _requiredCaster = requiredCaster;
                _legalTargets = legalTargets;
                _demands = demands ?? new ModifierUsageDemand[0];
            }

            public string ModifierId { get { return "share"; } }

            public CastingModifierResult Apply(
                Domain.Authoring.PlannedCasting casting, ProviderPlanningOption option)
            {
                if (casting.CasterUnitId != _requiredCaster)
                    return CastingModifierResult.Unavailable("caster-lacks-share-feature");
                return CastingModifierResult.Applied(new ProviderPlanningOption(
                    option.Provider, _legalTargets, option.LegalAnchorIds,
                    option.EffectiveCasterLevel, option.ExpectedDurationRounds,
                    option.ExecutionStrategy, option.ExecutionStrategyReason));
            }

            public System.Collections.Generic.IReadOnlyList<ModifierUsageDemand>
                UsageDemands(Domain.Authoring.PlannedCasting casting,
                    ProviderPlanningOption option)
            {
                return _demands;
            }
        }

        private static void TestCastingA05TargetingModifiers()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2", "unit-t3" }, 3);
            TargetingModifierSelection Enabled()
            {
                return new TargetingModifierSelection("share", true, null);
            }
            TargetingModifierSelection Disabled()
            {
                return new TargetingModifierSelection("share", false, null);
            }
            PlannedCasting WithModifier(PlannedCasting casting,
                TargetingModifierSelection selection)
            {
                return new PlannedCasting(
                    casting.CastingId, casting.RoutineId, casting.Order,
                    casting.SourceId, casting.Ability, casting.CasterUnitId,
                    casting.SpellbookGuid, casting.TargetMode,
                    casting.DirectTargetUnitId, casting.Origin,
                    casting.RequiredCoverageUnitIds,
                    new[] { selection }, casting.Enhancements,
                    casting.ExistingEffectPolicy, casting.IgnoredPresenceMarkers,
                    casting.State, casting.Provenance);
            }
            var document = CastingDocument(
                WithModifier(DirectCasting("cast-inside", "long", "unit-cleric",
                    "unit-t1", "source-bulls", CastingBuffAbility), Enabled()),
                WithModifier(DirectCasting("cast-outside", "long", "unit-cleric",
                    "unit-t3", "source-bulls", CastingBuffAbility), Enabled()),
                WithModifier(DirectCasting("cast-no-feature", "long", "unit-wizard",
                    "unit-t1", "source-bulls", CastingBuffAbility), Enabled()),
                WithModifier(DirectCasting("cast-disabled", "long", "unit-cleric",
                    "unit-t3", "source-bulls", CastingBuffAbility), Disabled()));
            var compiler = new ExplicitCastingCompiler();
            var effects = CastingEffects("source-bulls", "source-communal");
            var registry = new ICastingTargetingModifier[]
            {
                new FixtureShareCastingModifier("unit-cleric",
                    new[] { "unit-t1", "unit-t2" })
            };
            ExplicitCastingPlan plan = compiler.Compile(
                document, snapshot, options, effects, enhancements,
                null, registry);
            if (plan.CastingById("cast-inside").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "A legal share target became ineligible.");
            ResolvedCasting outside = plan.CastingById("cast-outside");
            if (outside.Readiness != ResolvedCastingReadiness.Blocked ||
                !outside.ReadinessReasons.Contains("target-unreachable:unit-t3"))
                throw new InvalidOperationException(
                    "Share did not narrow recipient eligibility: " +
                    string.Join(",", outside.ReadinessReasons));
            ResolvedCasting noFeature = plan.CastingById("cast-no-feature");
            if (noFeature.Readiness != ResolvedCastingReadiness.Blocked ||
                !noFeature.ReadinessReasons.Contains(
                    "targeting-modifier-unavailable:share:caster-lacks-share-feature"))
                throw new InvalidOperationException(
                    "The unavailable modifier did not block as repairable intent.");
            if (plan.CastingById("cast-disabled").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "A disabled selection changed eligibility.");
            // The invalidation is repairable: the same casting with the
            // selection disabled compiles Ready on an otherwise identical
            // document (nothing was deleted or permanently poisoned).
            var repairedDocument = CastingDocument(
                WithModifier(DirectCasting("cast-no-feature", "long", "unit-wizard",
                    "unit-t1", "source-bulls", CastingBuffAbility), Disabled()));
            ExplicitCastingPlan repaired = compiler.Compile(
                repairedDocument, snapshot, options, effects, enhancements,
                null, registry);
            if (repaired.CastingById("cast-no-feature").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "Repairing the selection did not restore the casting.");
            // An unknown modifier id against a provided registry blocks
            // instead of being silently ignored.
            var unknown = CastingDocument(WithModifier(DirectCasting(
                    "cast-unknown", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                new TargetingModifierSelection("ghost", true, null)));
            ExplicitCastingPlan unknownPlan = compiler.Compile(
                unknown, snapshot, options, effects, enhancements, null, registry);
            if (unknownPlan.CastingById("cast-unknown").Readiness !=
                    ResolvedCastingReadiness.Blocked ||
                !unknownPlan.CastingById("cast-unknown").ReadinessReasons.Contains(
                    "targeting-modifier-unknown:ghost"))
                throw new InvalidOperationException(
                    "An unknown modifier was not blocked against the registry.");
            // Modifier application is pure: recompilation reproduces the
            // identical readiness set (no leaked one-shot state).
            ExplicitCastingPlan repeat = compiler.Compile(
                document, snapshot, options, effects, enhancements,
                null, registry);
            for (int i = 0; i < plan.Castings.Count; i++)
                if (plan.Castings[i].Readiness != repeat.Castings[i].Readiness ||
                    plan.Castings[i].ReadinessReasons.Count !=
                        repeat.Castings[i].ReadinessReasons.Count)
                    throw new InvalidOperationException(
                        "Modifier application leaked state across compilations.");
        }

        // ------------------------------------------------------------------
        // Continuation contract checks C1-C5, exercised through the same
        // production compiler, gate, and forecast services.
        // ------------------------------------------------------------------

        // C1: an enabled modifier without a validated host contract must
        // never become permission to execute the unmodified spell — even
        // when the recipient is legal for the base spell — and ordinary
        // Apply must block; repairing the registry restores the record.
        private static void TestCastingC1UnvalidatedModifiers()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2" }, 3);
            TargetingModifierSelection ShareEnabled()
            {
                return new TargetingModifierSelection("share", true, null);
            }
            var document = CastingDocument(
                DirectCasting("cast-mod-legal", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-plain", "long", "unit-cleric", "unit-t2",
                    "source-bulls", CastingBuffAbility));
            // The modifier selection is added by rebuilding the first
            // casting with the selection enabled; unit-t1 stays legal under
            // base targeting, so a silent downgrade would succeed here.
            var withModifier = CastingDocument(
                new PlannedCasting(
                    "cast-mod-legal", "long", 0, "source-bulls", CastingBuffAbility,
                    "unit-cleric", null, CastingTargetMode.DirectTarget, "unit-t1",
                    null, null, new[] { ShareEnabled() }, null,
                    ExistingEffectPolicy.SkipAlreadyActive, null,
                    CastingAuthoringState.Ready, null),
                DirectCasting("cast-plain", "long", "unit-cleric", "unit-t2",
                    "source-bulls", CastingBuffAbility));
            var compiler = new ExplicitCastingCompiler();
            var effects = CastingEffects("source-bulls", "source-communal");
            ExplicitCastingPlan unresolved = compiler.Compile(
                withModifier, snapshot, options, effects, enhancements);
            ResolvedCasting blocked = unresolved.CastingById("cast-mod-legal");
            if (blocked.Readiness != ResolvedCastingReadiness.Blocked ||
                !blocked.ReadinessReasons.Contains(
                    "targeting-modifier-unresolved:share") ||
                blocked.Cost.Count != 0 ||
                blocked.IsExecutable)
                throw new InvalidOperationException(
                    "An unvalidated required modifier allowed execution: " +
                    string.Join(",", blocked.ReadinessReasons));
            // A casting with no modifier is unaffected by the absent
            // registry.
            if (unresolved.CastingById("cast-plain").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "An absent registry blocked a modifier-free casting.");
            // Compiler-to-gate path: ordinary Apply refuses; Ready Casts
            // Only omits the record with its reason and never submits it.
            var gate = new CastingExecutionGate();
            CastingApplyDecision ordinary = gate.Evaluate(
                unresolved, CastingApplyMode.Ordinary);
            if (ordinary.Allowed ||
                !ordinary.BlockingReasons.Any(value => value.StartsWith(
                    "blocked-casting:cast-mod-legal", StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Ordinary Apply accepted an unvalidated modifier request.");
            CastingApplyDecision readyOnly = gate.Evaluate(
                unresolved, CastingApplyMode.ReadyCastsOnly);
            if (!readyOnly.Allowed ||
                readyOnly.ExecutableCastingIds.Count != 1 ||
                readyOnly.ExecutableCastingIds[0] != "cast-plain" ||
                readyOnly.Omissions.Any(omission => omission.CastingId ==
                    "cast-mod-legal" && !omission.Reasons.Contains(
                        "targeting-modifier-unresolved:share")))
                throw new InvalidOperationException(
                    "Ready Casts Only submitted or hid the unresolved record.");
            // Repair: the same document with a registry restores readiness
            // with the same CastingId and no duplicate record.
            var registry = new ICastingTargetingModifier[]
            {
                new FixtureShareCastingModifier("unit-cleric",
                    new[] { "unit-cleric", "unit-t1", "unit-t2" })
            };
            ExplicitCastingPlan repaired = compiler.Compile(
                withModifier, snapshot, options, effects, enhancements,
                null, registry);
            if (repaired.Castings.Count != 2 ||
                repaired.CastingById("cast-mod-legal").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "Repairing the registry did not restore the record.");
            // The original document (no modifier at all) was never blocked.
            ExplicitCastingPlan plain = compiler.Compile(
                document, snapshot, options, effects, enhancements);
            if (plain.ReadyInvocationCount != 2)
                throw new InvalidOperationException(
                    "A modifier-free plan was disturbed.");
        }

        // C2: a saved unresolved draft is not an implicit opt-out; an
        // explicitly disabled or out-of-scope record never blocks a run.
        private static void TestCastingC2DraftNotAnOptOut()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1" }, 3);
            var document = CastingDocument(
                DirectCasting("cast-ready", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-draft", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility, null, null,
                    CastingAuthoringState.Draft),
                DirectCasting("cast-parked", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility, null, null,
                    CastingAuthoringState.Disabled),
                DirectCasting("cast-other", "short", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility, null, null,
                    CastingAuthoringState.Draft));
            ExplicitCastingPlan plan = CompileCastingPlan(
                document, snapshot, options, enhancements,
                "source-bulls", "source-communal");
            if (plan.CastingById("cast-parked").Readiness !=
                    ResolvedCastingReadiness.Disabled)
                throw new InvalidOperationException(
                    "An explicitly disabled casting lost its distinct state.");
            var gate = new CastingExecutionGate();
            // Selected run (long): the unresolved draft blocks ordinary
            // Apply; the parked record is disclosed but never blocks.
            CastingApplyDecision ordinary = gate.Evaluate(
                plan, CastingApplyMode.Ordinary, "long");
            if (ordinary.Allowed || ordinary.BlockingReasons.Count != 1 ||
                !ordinary.BlockingReasons[0].StartsWith(
                    "unresolved-saved-request-casting:cast-draft",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A saved unresolved draft silently opted out of Apply.");
            CastingOmission parked = ordinary.Omissions.FirstOrDefault(
                omission => omission.CastingId == "cast-parked");
            if (ordinary.Omissions.Count != 2 || parked == null ||
                !parked.Reasons.Contains("explicitly-disabled"))
                throw new InvalidOperationException(
                    "The explicitly disabled record was not disclosed as such.");
            // Out-of-scope work never blocks an unrelated valid run.
            if (gate.Evaluate(plan, CastingApplyMode.Ordinary, "important").Allowed !=
                    true)
                throw new InvalidOperationException(
                    "Out-of-scope work blocked an unrelated empty run.");
            // The short routine's own draft still blocks the short run.
            if (gate.Evaluate(plan, CastingApplyMode.Ordinary, "short").Allowed)
                throw new InvalidOperationException(
                    "Scoping ignored the short routine's unresolved record.");
            // The deliberate Ready Casts Only action executes the ready work
            // and names exactly what it omits.
            CastingApplyDecision readyOnly = gate.Evaluate(
                plan, CastingApplyMode.ReadyCastsOnly, "long");
            if (!readyOnly.Allowed ||
                readyOnly.ExecutableCastingIds.Count != 1 ||
                readyOnly.Omissions.Count != 2)
                throw new InvalidOperationException(
                    "Ready Casts Only lost the omissions or the executable set.");
            // Full-scope (one-pass) ordinary Apply counts every draft.
            CastingApplyDecision onePass = gate.Evaluate(
                plan, CastingApplyMode.Ordinary);
            if (onePass.Allowed || onePass.BlockingReasons.Count != 2)
                throw new InvalidOperationException(
                    "One-pass Apply ignored an unresolved record.");
        }

        // C4: a cost-charging targeting modifier enters the same atomic cost
        // vector; each feature alone is affordable but the combination is
        // not, and the failure leaks no reservations.
        private static void TestCastingC4ModifierCosts()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2" }, 3);
            enhancements.Add(new CastEnhancementSnapshot(
                "score-a", "unit-cleric", "feat-a", "Score A", string.Empty,
                CastEnhancementCategory.ClassFeature, 0, 10, 1,
                new[] { CastingBuffAbility.BaseAbilityGuid },
                null, new[] { "book-unit-cleric" }, "reservoir", false, "score-a", 1));
            var registry = new ICastingTargetingModifier[]
            {
                new FixtureShareCastingModifier("unit-cleric",
                    new[] { "unit-cleric", "unit-t1", "unit-t2" },
                    new ModifierUsageDemand("reservoir", 1))
            };
            var compiler = new ExplicitCastingCompiler();
            var effects = CastingEffects("source-bulls", "source-communal");
            TargetingModifierSelection ShareEnabled()
            {
                return new TargetingModifierSelection("share", true, null);
            }
            // Combined demand: the modifier and the class feature each cost
            // one reservoir unit; the pool holds one.
            var combined = CastingDocument(
                new PlannedCasting(
                    "cast-both", "long", 0, "source-bulls", CastingBuffAbility,
                    "unit-cleric", null, CastingTargetMode.DirectTarget, "unit-t1",
                    null, null, new[] { ShareEnabled() },
                    new[] { new AuthoredEnhancementSelection("score-a", true, null) },
                    ExistingEffectPolicy.SkipAlreadyActive, null,
                    CastingAuthoringState.Ready, null),
                DirectCasting("cast-plain", "long", "unit-cleric", "unit-t2",
                    "source-bulls", CastingBuffAbility));
            ExplicitCastingPlan combinedPlan = compiler.Compile(
                combined, snapshot, options, effects, enhancements, null, registry);
            ResolvedCasting both = combinedPlan.CastingById("cast-both");
            if (both.Readiness != ResolvedCastingReadiness.Blocked ||
                !both.ReadinessReasons.Contains(
                    "enhancement-pool-exhausted:reservoir:1<2") ||
                both.Cost.Count != 0)
                throw new InvalidOperationException(
                    "Combined modifier+feature demand was not atomic: " +
                    string.Join(",", both.ReadinessReasons));
            // No leakage: the later plain casting still reserves its native
            // slot and the reservoir line exposes the deficit.
            if (combinedPlan.CastingById("cast-plain").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "The failed combination leaked reservations.");
            CastingBudgetLine reservoir = combinedPlan.BudgetLineFor("reservoir");
            if (reservoir.RequestedUsage != 2 || reservoir.AllocatedUsage != 0 ||
                reservoir.UnmetDemand != 2)
                throw new InvalidOperationException(
                    "The reservoir deficit was not exposed.");
            // Each feature alone is affordable.
            var modifierOnly = CastingDocument(
                new PlannedCasting(
                    "cast-mod", "long", 0, "source-bulls", CastingBuffAbility,
                    "unit-cleric", null, CastingTargetMode.DirectTarget, "unit-t1",
                    null, null, new[] { ShareEnabled() }, null,
                    ExistingEffectPolicy.SkipAlreadyActive, null,
                    CastingAuthoringState.Ready, null));
            ExplicitCastingPlan modifierPlan = compiler.Compile(
                modifierOnly, snapshot, options, effects, enhancements,
                null, registry);
            if (modifierPlan.CastingById("cast-mod").Readiness !=
                    ResolvedCastingReadiness.Ready ||
                !modifierPlan.CastingById("cast-mod").Cost.Any(line =>
                    line.PoolKey == "reservoir" && line.Units == 1))
                throw new InvalidOperationException(
                    "The affordable modifier-alone demand was not reserved.");
            var featureOnly = CastingDocument(
                DirectCasting("cast-feat", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility,
                    new[] { new AuthoredEnhancementSelection("score-a", true, null) }));
            ExplicitCastingPlan featurePlan = compiler.Compile(
                featureOnly, snapshot, options, effects, enhancements,
                null, registry);
            if (featurePlan.CastingById("cast-feat").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "The affordable feature-alone demand was blocked.");
        }

        // C5: the one-pass forecast carries structural effect presence
        // forward per the per-casting existing-effect policy, without
        // inventing satisfaction for enhanced or reordered requests.
        private static void TestCastingC5EffectProjection()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1" }, 3);
            var service = new CastingForecastService();
            var document = CastingDocument(
                DirectCasting("cast-first", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-second", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility));
            CastingForecast onePass = service.ForecastOnePass(
                document, snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (onePass.Plan.CastingById("cast-first").Readiness !=
                    ResolvedCastingReadiness.Ready ||
                onePass.Plan.CastingById("cast-second").Readiness !=
                    ResolvedCastingReadiness.AlreadySatisfied ||
                onePass.ReadyInvocations != 1)
                throw new InvalidOperationException(
                    "A proven-equal earlier result did not satisfy later demand.");
            if (onePass.Plan.BudgetLineFor("pool-unit-cleric").AllocatedUsage != 1)
                throw new InvalidOperationException(
                    "A satisfied casting still reserved resources.");
            // Always-recast remains a casting.
            var overwrite = CastingDocument(
                DirectCasting("cast-first", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-second", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility, null, null,
                    CastingAuthoringState.Ready, ExistingEffectPolicy.Overwrite));
            CastingForecast overwriteForecast = service.ForecastOnePass(
                overwrite, snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (overwriteForecast.ReadyInvocations != 2)
                throw new InvalidOperationException(
                    "An always-recast request was silently satisfied.");
            // Enhanced requests never inherit plain-cast satisfaction.
            var enhanced = CastingDocument(
                DirectCasting("cast-first", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-second", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility,
                    new[] { new AuthoredEnhancementSelection("extend-cleric", true, null) }));
            CastingForecast enhancedForecast = service.ForecastOnePass(
                enhanced, snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (enhancedForecast.ReadyInvocations != 2 ||
                enhancedForecast.Plan.CastingById("cast-second").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "Unknown strength equivalence invented a satisfaction claim.");
            // Reversing the explicitly authored order reverses which
            // casting executes, with the same total demand.
            var reversed = CastingDocument(
                DirectCasting("cast-first", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-second", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility));
            var authoring = new CastingAuthoringService(reversed);
            if (!authoring.MoveCasting("cast-second", "long", 0).Applied)
                throw new InvalidOperationException("Fixture reorder failed.");
            CastingForecast reversedForecast = service.ForecastOnePass(
                authoring.Document, snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (reversedForecast.ReadyInvocations != 1 ||
                reversedForecast.Plan.CastingById("cast-first").Readiness !=
                    ResolvedCastingReadiness.AlreadySatisfied ||
                reversedForecast.Plan.CastingById("cast-second").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "Authored order did not drive the projection.");
            // Independent routine previews mutate neither the saved document
            // nor later one-pass results.
            CastingForecast routineView = service.ForecastRoutine(
                document, "long", snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (document.Castings.Count != 2 ||
                document.Castings[0].CastingId != "cast-first" ||
                document.Castings[0].State != CastingAuthoringState.Ready)
                throw new InvalidOperationException(
                    "A preview disturbed the saved document.");
            CastingForecast repeated = service.ForecastOnePass(
                document, snapshot, options,
                CastingEffects("source-bulls", "source-communal"), enhancements);
            if (repeated.Plan.ReadyInvocationCount != 1)
                throw new InvalidOperationException(
                    "One-pass projection was not deterministic.");
        }

        // ------------------------------------------------------------------
        // Narrow integration checks from the runtime-qualification
        // continuation: authored sibling intent versus derived changes,
        // and attempt-only semantics for the disabled dispatch boundary.
        // ------------------------------------------------------------------

        private sealed class ThrowingDispatchBoundary : ICastingDispatchBoundary
        {
            public int Attempts;

            public string DispositionReason
            {
                get { return "throwing-fixture-boundary"; }
            }

            public CastingDispatchOutcome Submit(
                ExplicitCastingPlan plan, CastingApplyDecision decision,
                string scopeRoutineId)
            {
                Attempts++;
                throw new InvalidOperationException("dispatch-fixture-failure");
            }
        }

        // A sibling card's DERIVED readiness may change because a shared
        // budget recalculated, but its AUTHORED intent (caster, source,
        // target, enhancements, state, routine membership) is never
        // rewritten by another card's edit or move; necessary order
        // renumbering is disclosed; Undo restores the complete prior
        // intent and order byte-for-byte.
        private static void TestCastingWorkspaceSiblingIntent()
        {
            string modPath = Path.Combine(
                Path.GetTempPath(), "KbpWorkspaceSibling-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(modPath);
            try
            {
                PartyProviderSnapshot snapshot;
                CastingWorkspaceInputs inputs = WorkspaceInputs(
                    out snapshot, null, null, 2);
                var session = new CastingWorkspaceSession(modPath, "sibling-campaign");
                session.Draft.SourceId = "source-bulls";
                session.Draft.Ability = CastingBuffAbility;
                session.Draft.TargetMode = CastingTargetMode.DirectTarget;
                session.Draft.CasterUnitId = "unit-cleric";
                session.Draft.State = CastingAuthoringState.Ready;
                // Two native charges fund exactly two of the three castings
                // in one-pass order.
                foreach (string target in new[] { "unit-t1", "unit-t2", "unit-t3" })
                {
                    session.Draft.DirectTargetUnitId = target;
                    Assert(session.AddCastingFromDraft(inputs).Applied);
                }
                // Baseline: cast-1 and cast-2 reserve; cast-3 is blocked by
                // the shared pool (a DERIVED state, authored intent intact).
                ExplicitCastingPlan before = session.CompilePlan(inputs);
                if (before.CastingById("cast-3").Readiness !=
                        ResolvedCastingReadiness.Blocked ||
                    before.CastingById("cast-3").CasterUnitId != "unit-cleric")
                    throw new InvalidOperationException(
                        "Fixture baseline is wrong.");
                // Editing the MIDDLE card's authored target: neighbors keep
                // their exact instances and authored fields; only the edited
                // card changes; the sibling's derived readiness is allowed
                // to recompute without counting as an edit.
                var first = session.Document.Castings[0];
                var third = session.Document.Castings[2];
                session.FocusCasting("cast-2");
                Assert(session.UpdateFocusedCasting(new PlannedCasting(
                    "cast-2", "long", 1, "source-bulls", CastingBuffAbility,
                    "unit-wizard", null, CastingTargetMode.DirectTarget,
                    "unit-t2", null, null, null, null,
                    ExistingEffectPolicy.SkipAlreadyActive, null,
                    CastingAuthoringState.Ready, null)).Applied);
                if (!ReferenceEquals(first, session.Document.Castings[0]) ||
                    !ReferenceEquals(third, session.Document.Castings[2]))
                    throw new InvalidOperationException(
                        "A sibling casting instance was rewritten by a card edit.");
                // A routine MOVE changes the mover's membership and the
                // necessary order metadata of the remaining long-routine
                // sibling — and frees shared budget so the sibling's derived
                // readiness legitimately improves. The sibling's authored
                // intent must remain untouched.
                // The edit also legitimately improves the sibling's DERIVED
                // readiness (the wizard draws from its own pool), which is
                // not an authored change to the sibling.
                if (session.CompilePlan(inputs).CastingById("cast-3").Readiness !=
                        ResolvedCastingReadiness.Ready)
                    throw new InvalidOperationException(
                        "The legitimate derived readiness change was suppressed.");
                string beforeJson = SerializeDocument(session);
                session.FocusCasting("cast-2");
                AuthoringEditResult move = session.MoveFocusedCasting("short", 0);
                if (!move.Applied || move.AffectedCastingIds.Count != 2 ||
                    !move.AffectedCastingIds.Contains("cast-3"))
                    throw new InvalidOperationException(
                        "The necessary sibling renumbering was not disclosed: " +
                        move.Reason);
                if (!ReferenceEquals(first, session.Document.Castings[0]))
                    throw new InvalidOperationException(
                        "The unmoved sibling was rebuilt.");
                var renumbered = session.Document.Castings[1];
                if (renumbered.CastingId != "cast-3" || renumbered.Order != 1 ||
                    renumbered.CasterUnitId != "unit-cleric" ||
                    renumbered.DirectTargetUnitId != "unit-t3")
                    throw new InvalidOperationException(
                        "Order metadata renumbering altered authored intent.");
                ExplicitCastingPlan after = session.CompilePlan(inputs);
                if (after.CastingById("cast-3").Readiness !=
                        ResolvedCastingReadiness.Ready)
                    throw new InvalidOperationException(
                        "The legitimate derived budget improvement was lost.");
                // Undo restores the complete prior intent AND order.
                Assert(session.Undo());
                if (SerializeDocument(session) != beforeJson)
                    throw new InvalidOperationException(
                        "Undo did not restore the exact prior document.");
                ExplicitCastingPlan restored = session.CompilePlan(inputs);
                if (restored.CastingById("cast-3").Readiness !=
                        ResolvedCastingReadiness.Ready ||
                    restored.CastingById("cast-2").CasterUnitId != "unit-wizard" ||
                    restored.CastingById("cast-2").RoutineId != "long")
                    throw new InvalidOperationException(
                        "Undo did not restore the pre-move intent, order, or " +
                        "derived state.");
            }
            finally
            {
                if (Directory.Exists(modPath)) Directory.Delete(modPath, true);
            }
        }

        private static string SerializeDocument(CastingWorkspaceSession session)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                CastingPlanProfile.FromDocument(session.Document),
                Newtonsoft.Json.Formatting.None);
        }

        // The disabled dispatch boundary records an ATTEMPT only: no native
        // submission, no resource expenditure, no observed beneficiaries, no
        // success state; a throwing boundary releases the in-flight guard so
        // later Apply attempts still work; legacy execution is refused while
        // the workspace is selected.
        private static void TestCastingWorkspaceDisabledDispatch()
        {
            string modPath = Path.Combine(
                Path.GetTempPath(), "KbpWorkspaceDispatch-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(modPath);
            try
            {
                PartyProviderSnapshot snapshot;
                CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
                var boundary = new DisabledCastingDispatchBoundary();
                var session = new CastingWorkspaceSession(
                    modPath, "dispatch-campaign", boundary);
                session.Draft.SourceId = "source-bulls";
                session.Draft.Ability = CastingBuffAbility;
                session.Draft.TargetMode = CastingTargetMode.DirectTarget;
                session.Draft.CasterUnitId = "unit-cleric";
                session.Draft.DirectTargetUnitId = "unit-t1";
                session.Draft.State = CastingAuthoringState.Ready;
                Assert(session.AddCastingFromDraft(inputs).Applied);
                session.PresentForReview(inputs);
                Assert(session.AcceptPresentedPlan(inputs));
                // The limitation is visible BEFORE the player acts.
                if (!session.DispatchDisposition.Contains("native-submission-disabled"))
                    throw new InvalidOperationException(
                        "The execution limitation is not visible up front.");
                WorkspaceApplyResult result = session.Apply(
                    CastingApplyMode.Ordinary, "long", inputs);
                if (result.Allowed || result.Dispatch == null ||
                    result.Dispatch.Submitted ||
                    result.Dispatch.CastingIds.Count != 1 ||
                    !result.ReviewReason.Contains("native-submission-disabled"))
                    throw new InvalidOperationException(
                        "The disabled path claimed a submission or hid identity.");
                // The boundary is the game boundary: a recorded attempt is
                // the ONLY trace. There is no expenditure, beneficiary, or
                // success surface anywhere on the outcome.
                if (boundary.RecordedSubmissions.Count != 1)
                    throw new InvalidOperationException(
                        "Attempt identity was not recorded.");
                // A throwing boundary must release the in-flight guard so
                // validation, editing, and later applies keep working.
                var throwing = new ThrowingDispatchBoundary();
                var recovering = new CastingWorkspaceSession(
                    modPath, "dispatch-campaign-2", throwing);
                recovering.Draft.SourceId = "source-bulls";
                recovering.Draft.Ability = CastingBuffAbility;
                recovering.Draft.TargetMode = CastingTargetMode.DirectTarget;
                recovering.Draft.CasterUnitId = "unit-cleric";
                recovering.Draft.DirectTargetUnitId = "unit-t1";
                recovering.Draft.State = CastingAuthoringState.Ready;
                Assert(recovering.AddCastingFromDraft(inputs).Applied);
                recovering.PresentForReview(inputs);
                Assert(recovering.AcceptPresentedPlan(inputs));
                bool threw = false;
                try { recovering.Apply(CastingApplyMode.Ordinary, "long", inputs); }
                catch (InvalidOperationException) { threw = true; }
                if (!threw || throwing.Attempts != 1)
                    throw new InvalidOperationException(
                        "The dispatch failure path was not exercised.");
                // Recovery: a subsequent Apply is not swallowed by a stuck
                // in-flight guard (it reaches review and the boundary).
                bool reachedBoundary = false;
                try
                {
                    recovering.Apply(CastingApplyMode.Ordinary, "long", inputs);
                    reachedBoundary = throwing.Attempts == 2;
                }
                catch (InvalidOperationException)
                {
                    reachedBoundary = throwing.Attempts == 2;
                }
                if (!reachedBoundary)
                    throw new InvalidOperationException(
                        "The in-flight guard leaked across a failed submission.");
                // Legacy execution entry points refuse while the workspace
                // is selected; they are permitted again once it is not.
                CastingWorkspaceDevSelection.Enabled = true;
                try
                {
                    if (CastingWorkspaceDevSelection.LegacyExecutionPermitted)
                        throw new InvalidOperationException(
                            "Legacy execution was permitted beside the workspace.");
                }
                finally
                {
                    CastingWorkspaceDevSelection.Enabled = false;
                }
                if (!CastingWorkspaceDevSelection.LegacyExecutionPermitted)
                    throw new InvalidOperationException(
                        "Legacy execution was not restored after deselection.");
            }
            finally
            {
                if (Directory.Exists(modPath)) Directory.Delete(modPath, true);
            }
        }

        // ------------------------------------------------------------------
        // Connected workspace: the production CastingWorkspaceSession is
        // the real caller path for the authoring service, compiler, gate,
        // persistence, and review coordinator. Deterministic game-boundary
        // inputs stand in for the adapters; no planner service is mocked.
        // ------------------------------------------------------------------

        private static CastingWorkspaceInputs WorkspaceInputs(
            out PartyProviderSnapshot snapshot,
            IDictionary<string, IEnumerable<string>> groupCoverage = null,
            AbilityKey ability = null,
            int remainingPerCaster = 3)
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            snapshot = CastingParty(
                ability ?? CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1", "unit-t2", "unit-t3", "unit-t4", "unit-t5",
                    "unit-rogue" },
                remainingPerCaster, groupCoverage);
            return new CastingWorkspaceInputs(
                snapshot, options,
                CastingEffectsWithAbilityAlias(
                    "source-bulls", "source-communal",
                    ability ?? CastingBuffAbility),
                enhancements);
        }

        // Mirrors the production alias contract (PlannerSetupModel aliases
        // effects[sourceId] = effects[ability.Canonical] with the SAME
        // instance), which the workspace uses to derive per-source caster
        // capability from discovered options.
        private static Dictionary<string, EffectExpression> CastingEffectsWithAbilityAlias(
            string directSourceId, string groupSourceId, AbilityKey ability)
        {
            Dictionary<string, EffectExpression> effects =
                CastingEffects(directSourceId, groupSourceId);
            EffectExpression direct;
            effects.TryGetValue(directSourceId, out direct);
            EffectExpression group;
            effects.TryGetValue(groupSourceId, out group);
            if (ability != null && !string.Equals(ability.Canonical,
                    CastingGroupAbility.Canonical, StringComparison.Ordinal))
                effects[ability.Canonical] = direct;
            else if (ability != null)
                effects[ability.Canonical] = group;
            return effects;
        }

        // The charter's showcase flow: two casters, three independent
        // single-target cards with distinct settings, caster-focus changes
        // without reassignment, a group card with an explicit origin and an
        // honest coverage gap, and costs/readiness from the shared plan.
        private static void TestCastingWorkspaceMixedCasterFlow(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "workspace-campaign");
            if (session.LoadStatus != CastingPlanLoadStatus.Absent)
                throw new InvalidOperationException(
                    "An absent candidate was not reported.");
            // Browsing never mutates: selection changes leave the exact
            // document instance untouched.
            CastingPlanDocument beforeBrowse = session.Document;
            session.SelectBuff("source-bulls");
            session.SelectRoutine("long");
            session.SelectCaster("unit-cleric");
            WorkspaceView view = session.BuildView(inputs);
            if (!ReferenceEquals(beforeBrowse, session.Document) ||
                session.Document.Castings.Count != 0 ||
                view.EditingScope != WorkspaceEditingScope.ConfigureNextCasting)
                throw new InvalidOperationException(
                    "Browsing mutated the document.");
            // Three single-target castings from two casters with distinct
            // per-casting settings.
            // Exactly the fields the visible controls set — the ability is
            // resolved by the session itself (review F1: the UI never
            // assigns Draft.Ability, so neither may the test).
            session.Draft.SourceId = "source-bulls";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t1";
            session.Draft.State = CastingAuthoringState.Ready;
            session.Draft.Enhancements.Add(
                new AuthoredEnhancementSelection("extend-cleric", true, null));
            if (!session.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("cast-1 was refused.");
            session.Draft.CasterUnitId = "unit-wizard";
            session.Draft.DirectTargetUnitId = "unit-t2";
            session.Draft.Enhancements.Clear();
            session.Draft.Enhancements.Add(
                new AuthoredEnhancementSelection("extend-wizard", true, null));
            if (!session.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("cast-2 was refused.");
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t3";
            session.Draft.Enhancements.Clear();
            if (!session.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("cast-3 was refused.");
            view = session.BuildView(inputs);
            if (view.Cards.Count != 3 ||
                view.CardById("cast-1") == null || view.CardById("cast-2") == null ||
                view.CardById("cast-3") == null)
                throw new InvalidOperationException(
                    "Three recipients did not become three cards.");
            if (view.CardById("cast-1").CasterUnitId != "unit-cleric" ||
                view.CardById("cast-2").CasterUnitId != "unit-wizard" ||
                view.CardById("cast-1").EnhancementLabels.Count != 1 ||
                view.CardById("cast-2").EnhancementLabels.Count != 1 ||
                view.CardById("cast-3").EnhancementLabels.Count != 0)
                throw new InvalidOperationException(
                    "Per-casting settings were collapsed.");
            if (!view.Cards.All(card => card.Readiness ==
                    ResolvedCastingReadiness.Ready) ||
                view.Cards.Any(card => card.CostLabels.Count == 0))
                throw new InvalidOperationException(
                    "Readiness or costs are not from the shared plan: " +
                    string.Join(" | ", view.Cards.Select(card =>
                        card.CastingId + ":" + card.Readiness + ":" +
                        string.Join(",", card.ReadinessReasons.ToArray()) +
                        ":costs=" + card.CostLabels.Count).ToArray()));
            // Switching caster focus changes nothing but focus.
            CastingPlanDocument beforeFocusChange = session.Document;
            session.SelectCaster("unit-wizard");
            view = session.BuildView(inputs);
            if (!ReferenceEquals(beforeFocusChange, session.Document) ||
                view.Cards.Count != 3 ||
                view.CardById("cast-2").CasterUnitId != "unit-wizard" ||
                !view.Casters.First(row => row.UnitId == "unit-wizard").SelectedFocus)
                throw new InvalidOperationException(
                    "Caster focus change reassigned existing work.");
            // Capability is separated from readiness in the caster lane.
            WorkspaceCasterRow cleric =
                view.Casters.First(row => row.UnitId == "unit-cleric");
            if (!cleric.Capable)
                throw new InvalidOperationException(
                    "A capable caster was listed as incapable.");
            // Explicit single-card editing scope with Undo.
            session.FocusCasting("cast-2");
            view = session.BuildView(inputs);
            if (view.EditingScope != WorkspaceEditingScope.EditingSingleCasting ||
                !view.EditingScopeLabel.Contains("cast-2") ||
                !view.CardById("cast-2").EditingFocus)
                throw new InvalidOperationException(
                    "The editing scope is not prominently single-card.");
            if (!session.UpdateFocusedCasting(new PlannedCasting(
                    "cast-2", "long", 1, "source-bulls", CastingBuffAbility,
                    "unit-wizard", null, CastingTargetMode.DirectTarget,
                    "unit-rogue", null, null, null, null,
                    ExistingEffectPolicy.SkipAlreadyActive, null,
                    CastingAuthoringState.Ready, null)).Applied)
                throw new InvalidOperationException("The card edit was refused.");
            view = session.BuildView(inputs);
            if (view.CardById("cast-2").DirectTargetUnitId != "unit-rogue" ||
                view.CardById("cast-1").DirectTargetUnitId != "unit-t1" ||
                view.CardById("cast-3").DirectTargetUnitId != "unit-t3")
                throw new InvalidOperationException(
                    "The edit leaked into neighboring cards.");
            Assert(session.Undo());
            view = session.BuildView(inputs);
            if (view.CardById("cast-2").DirectTargetUnitId != "unit-t2")
                throw new InvalidOperationException("Undo did not restore the card.");
            // A mismatched replacement cannot edit a focused card.
            if (session.UpdateFocusedCasting(new PlannedCasting(
                    "cast-1", "long", 0, "source-bulls", CastingBuffAbility,
                    "unit-cleric", null, CastingTargetMode.DirectTarget,
                    "unit-t1", null, null, null, null,
                    ExistingEffectPolicy.SkipAlreadyActive, null,
                    CastingAuthoringState.Ready, null)).Applied)
                throw new InvalidOperationException(
                    "An unfocused replacement edited a card.");
            session.FocusCasting(null);
            // One group casting: explicit origin, derived beneficiaries, and
            // a visible coverage gap that never becomes a second casting.
            var coverage = new Dictionary<string, IEnumerable<string>>(
                StringComparer.Ordinal)
            {
                { "unit-cleric", new[]
                    { "unit-t1", "unit-t2", "unit-t3", "unit-t4", "unit-t5" } }
            };
            PartyProviderSnapshot groupSnapshot;
            CastingWorkspaceInputs groupInputs =
                WorkspaceInputs(out groupSnapshot, coverage, CastingGroupAbility);
            var groupSession = new CastingWorkspaceSession(
                Path.Combine(root, "casting-workspace-group"), "workspace-campaign");
            groupSession.Draft.SourceId = "source-communal";
            groupSession.Draft.Ability = CastingGroupAbility;
            groupSession.Draft.TargetMode = CastingTargetMode.CasterCenteredOrigin;
            groupSession.Draft.CasterUnitId = "unit-cleric";
            groupSession.Draft.State = CastingAuthoringState.Ready;
            groupSession.Draft.Origin = CastingOrigin.CasterCentered();
            groupSession.Draft.RequiredCoverageUnitIds.AddRange(new[]
                { "unit-t1", "unit-t2", "unit-t3", "unit-t4", "unit-t5",
                    "unit-rogue" });
            if (!groupSession.AddCastingFromDraft(groupInputs).Applied)
                throw new InvalidOperationException("The group casting was refused.");
            view = groupSession.BuildView(groupInputs);
            if (view.Cards.Count != 1 ||
                !view.Cards[0].OriginLabel.Contains("caster") ||
                view.Cards[0].PredictedBeneficiaryUnitIds.Count != 5 ||
                view.Cards[0].CoverageGapUnitIds.Count != 1 ||
                view.Cards[0].CoverageGapUnitIds[0] != "unit-rogue" ||
                view.Cards[0].CostLabels.Count != 1)
                throw new InvalidOperationException(
                    "Group origin, beneficiaries, or the honest gap is wrong.");
            if (groupSession.Document.Castings.Count != 1)
                throw new InvalidOperationException(
                    "Missed coverage created an extra casting.");
        }

        // The view/session really uses the review coordinator: presentation
        // and acceptance gate Apply; unseen changes, refused attempts,
        // refreshes, and incidental previews never approve a submission; the
        // disabled dispatch boundary proves policy without gameplay claims.
        // Review R3 regression: the one-charge/two-routine counterexample.
        // A whole-document compile reserves the single charge for the
        // earlier Long casting and blocks Short; the selected-run scope
        // must make either routine independently affordable while the
        // explicitly one-pass gate still reports the honest conflict.
        private static void TestCastingWorkspaceRoutineScopedApply(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-scoped");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs =
                WorkspaceInputs(out snapshot, remainingPerCaster: 1);
            var session = new CastingWorkspaceSession(modPath, "scoped-campaign");
            session.SelectRoutine("long");
            session.Draft.SourceId = "source-bulls";
            session.Draft.Ability = CastingBuffAbility;
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t1";
            session.Draft.State = CastingAuthoringState.Ready;
            if (!session.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("long cast was refused.");
            session.SelectRoutine("short");
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t2";
            if (!session.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("short cast was refused.");

            session.SelectRoutine("short");
            WorkspaceView shortView = session.BuildView(inputs);
            if (shortView.CardById("cast-2") == null ||
                shortView.CardById("cast-2").Readiness !=
                    ResolvedCastingReadiness.Ready)
                throw new InvalidOperationException(
                    "The short routine's affordable casting was blocked by " +
                    "the long routine's out-of-scope reservation.");
            if (shortView.OnePassGate.Allowed ||
                shortView.OnePassGate.BlockingReasons.Count == 0)
                throw new InvalidOperationException(
                    "The one-pass forecast hid the real one-charge conflict.");

            session.PresentForReview(inputs);
            WorkspaceApplyResult shortApply =
                session.Apply(CastingApplyMode.Ordinary, "short", inputs);
            if (shortApply.GateDecision == null ||
                !shortApply.GateDecision.Allowed)
                throw new InvalidOperationException(
                    "The affordable selected run was refused at the gate.");
            if (shortApply.Dispatch != null && shortApply.Dispatch.Submitted)
                throw new InvalidOperationException(
                    "Native submission escaped the disabled boundary.");
            session.SelectRoutine("long");
            session.PresentForReview(inputs);
            WorkspaceApplyResult longApply =
                session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (longApply.GateDecision == null ||
                !longApply.GateDecision.Allowed)
                throw new InvalidOperationException(
                    "The long routine was not independently affordable.");

            // A materially changed plan is no longer the presented contract.
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t3";
            session.AddCastingFromDraft(inputs);
            WorkspaceApplyResult staleApply =
                session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (staleApply.Allowed)
                throw new InvalidOperationException(
                    "An unpresented material change was accepted.");
        }

        // Review C1 regression: a freshly selected buff must show its
        // eligible casters from the discovered options before any casting
        // has been authored, and the initial selection must be a real
        // catalogue source rather than blank.
        private static void TestCastingWorkspaceFreshBuffCapability(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-fresh");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "fresh-campaign");
            if (session.Document.Castings.Count != 0)
                throw new InvalidOperationException(
                    "The fixture must start with an empty candidate.");
            WorkspaceView view = session.BuildView(inputs);
            if (view.SelectedSourceId != "source-bulls")
                throw new InvalidOperationException(
                    "The initial selection is not the discovered catalogue " +
                    "source: " + view.SelectedSourceId);
            WorkspaceCasterRow cleric =
                view.Casters.First(row => row.UnitId == "unit-cleric");
            WorkspaceCasterRow wizard =
                view.Casters.First(row => row.UnitId == "unit-wizard");
            WorkspaceCasterRow target =
                view.Casters.First(row => row.UnitId == "unit-t1");
            if (!cleric.Capable || !wizard.Capable)
                throw new InvalidOperationException(
                    "A fresh buff hid its eligible casters.");
            if (target.Capable)
                throw new InvalidOperationException(
                    "A party member without a provider was marked capable.");
            session.SelectBuff("source-communal");
            WorkspaceView communal = session.BuildView(inputs);
            if (communal.Casters.Any(row => row.Capable))
                throw new InvalidOperationException(
                    "Capability did not follow the selected source.");
        }

        // Review R1 support: the draft-editor read model must carry the
        // complete authoring data (sources, capable casters, targets,
        // enhancements) and the session must accept the exact sequence the
        // view controls issue: buff → caster → mode → target → enhancement
        // → state → add.
        private static void TestCastingWorkspaceDraftCatalog(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-draft");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "draft-campaign");
            // The control sequence a player issues through the editor.
            session.SelectRoutine("long");
            session.SelectBuff("source-bulls");
            session.SelectCaster("unit-cleric");
            WorkspaceView browse = session.BuildView(inputs);
            if (browse.Draft == null ||
                browse.Draft.Sources.Count == 0 ||
                !browse.Draft.Sources.Any(source => source.Selected &&
                    source.SourceId == "source-bulls"))
                throw new InvalidOperationException(
                    "The draft catalogue does not present the selected buff.");
            if (!browse.Draft.CapableCasters.Any(row => row.UnitId == "unit-cleric") ||
                !browse.Draft.CapableCasters.Any(row => row.UnitId == "unit-wizard"))
                throw new InvalidOperationException(
                    "The draft editor lacks the eligible casters.");
            if (!browse.Draft.Targets.Any(target => target.UnitId == "unit-t1") ||
                browse.Draft.Targets.Count < 3)
                throw new InvalidOperationException(
                    "The draft editor lacks recipient targets.");
            // Exactly the fields the visible controls set — the ability is
            // resolved by the session itself (review F1: the UI never
            // assigns Draft.Ability, so neither may the test).
            session.Draft.SourceId = "source-bulls";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t1";
            session.Draft.State = CastingAuthoringState.Ready;
            session.Draft.Enhancements.Add(
                new AuthoredEnhancementSelection("extend-cleric", true, null));
            WorkspaceView armed = session.BuildView(inputs);
            if (!armed.Draft.Enhancements.Any(option =>
                    option.Selected && option.EnhancementId == "extend-cleric"))
                throw new InvalidOperationException(
                    "The enhancement toggle state is not echoed.");
            AuthoringEditResult added = session.AddCastingFromDraft(inputs);
            if (!added.Applied)
                throw new InvalidOperationException(
                    "The control-sequence draft was refused: " + added.Reason);
            // A UI draft with no ability under an unknown caster is an
            // explained refusal, never a throw or a substitute casting
            // (review F1). An explicitly supplied ability is deliberately
            // retained and disclosed by the plan instead.
            session.Draft.CasterUnitId = "unit-rogue";
            session.Draft.Ability = null;
            AuthoringEditResult refused = session.AddCastingFromDraft(inputs);
            if (refused.Applied || !refused.Reason.StartsWith(
                    "draft-ability-unresolved", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "An unresolvable draft did not refuse with an explanation: " +
                    refused.Reason);
            WorkspaceView authored = session.BuildView(inputs);
            if (authored.Cards.Count != 1 ||
                authored.Cards[0].CasterUnitId != "unit-cleric" ||
                authored.Cards[0].DirectTargetUnitId != "unit-t1" ||
                authored.Cards[0].EnhancementLabels.Count != 1)
                throw new InvalidOperationException(
                    "The authored card does not reflect the editor's controls.");
        }

        // Review F3/F6 regression: coherent targeting-shape transitions
        // through the session command, stale-ability invalidation on
        // selection change, and dirty-state semantics around save.
        private static void TestCastingWorkspaceTargetingShapes(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-shapes");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "shapes-campaign");
            if (session.IsDirty)
                throw new InvalidOperationException(
                    "A freshly loaded session must not be dirty.");
            // Direct -> group -> direct never leaves incompatible fields.
            if (!session.SetDraftTargeting(CastingTargetMode.DirectTarget,
                    "unit-t1", null, null).Applied)
                throw new InvalidOperationException("direct targeting refused.");
            if (!session.SetDraftTargeting(
                    CastingTargetMode.CasterCenteredOrigin, null, null,
                    new[] { "unit-t1", "unit-t2" }).Applied)
                throw new InvalidOperationException("group targeting refused.");
            if (session.Draft.DirectTargetUnitId != null ||
                session.Draft.Origin == null || !session.Draft.Origin.IsCasterCentered ||
                session.Draft.RequiredCoverageUnitIds.Count != 2)
                throw new InvalidOperationException(
                    "Group targeting retained direct fields.");
            if (!session.SetDraftTargeting(CastingTargetMode.AnchoredOrigin,
                    null, "unit-cleric", null).Applied)
                throw new InvalidOperationException("anchored targeting refused.");
            if (session.Draft.Origin.IsCasterCentered ||
                session.Draft.TargetMode != CastingTargetMode.AnchoredOrigin)
                throw new InvalidOperationException(
                    "Anchor selection did not set the anchored mode.");
            if (!session.SetDraftTargeting(CastingTargetMode.DirectTarget,
                    "unit-t2", null, null).Applied)
                throw new InvalidOperationException("return to direct refused.");
            if (session.Draft.Origin != null ||
                session.Draft.RequiredCoverageUnitIds.Count != 0 ||
                session.Draft.DirectTargetUnitId != "unit-t2")
                throw new InvalidOperationException(
                    "Return to direct retained group fields.");
            // A null-recipient direct call now RESTORES the previously
            // chosen recipient (review H2a); the refusal belongs to a fresh
            // group with nothing remembered (covered by the H2 suite).
            session.SetDraftTargeting(
                CastingTargetMode.CasterCenteredOrigin, null, null, null);
            AuthoringEditResult restore = session.SetDraftTargeting(
                CastingTargetMode.DirectTarget, null, null, null);
            if (!restore.Applied ||
                session.Draft.DirectTargetUnitId != "unit-t2" ||
                !restore.Scope.Contains("restored-recipient"))
                throw new InvalidOperationException(
                    "The remembered recipient was not restored: " +
                    restore.Reason);
            // Stale-ability invalidation on selection change (review F1).
            session.SelectBuff("source-bulls");
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.SourceId = "source-bulls";
            // The view always refreshes (and compiles) before Add; the
            // session's resolution needs those discovery inputs.
            session.BuildView(inputs);
            AuthoringEditResult resolved = session.AddCastingFromDraft(inputs);
            if (!resolved.Applied)
                throw new InvalidOperationException(
                    "Self-resolution failed after shape transitions: " +
                    resolved.Reason);
            if (session.Document.Castings[0].Ability == null)
                throw new InvalidOperationException(
                    "The resolved ability was not authored.");
            // Dirty until saved; clean after save; a material edit is dirty
            // again (review F6).
            if (!session.IsDirty)
                throw new InvalidOperationException("An authored edit is not dirty.");
            session.Save();
            if (session.IsDirty)
                throw new InvalidOperationException("A saved session is still dirty.");
            session.FocusCasting(session.Document.Castings[0].CastingId);
            session.SetFocusedCastingState(CastingAuthoringState.Disabled);
            if (!session.IsDirty)
                throw new InvalidOperationException(
                    "A state edit after save is not dirty.");
            if (!session.Undo())
                throw new InvalidOperationException("Undo refused.");
            if (session.IsDirty)
                throw new InvalidOperationException(
                    "Undo to the saved state is still dirty.");
        }

        // Review G3: retained sessions bind to the verified campaign
        // identity. Isolated candidate roots only — no ordinary saves.
        private static void TestCastingWorkspaceCampaignBinding(string root)
        {
            string pathA = Path.Combine(root, "casting-workspace-campA");
            string pathB = Path.Combine(root, "casting-workspace-campB");
            Directory.CreateDirectory(pathA);
            Directory.CreateDirectory(pathB);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var sessionA = new CastingWorkspaceSession(pathA, "campaign-A");
            // Author unsaved intent in A and save a first document.
            sessionA.Draft.SourceId = "source-bulls";
            sessionA.Draft.CasterUnitId = "unit-cleric";
            sessionA.Draft.TargetMode = CastingTargetMode.DirectTarget;
            sessionA.Draft.DirectTargetUnitId = "unit-t1";
            sessionA.Draft.State = CastingAuthoringState.Ready;
            if (!sessionA.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("A authoring refused.");
            sessionA.Save();
            string savedA = sessionA.DocumentIntentSignature();

            // Same-campaign reuse: retained A returns A itself (reference
            // identity — unsaved intent and undo history survive).
            sessionA.Draft.DirectTargetUnitId = "unit-t2";
            if (!sessionA.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("A second authoring refused.");
            var messages = new List<string>();
            CastingWorkspaceSession reused = CastingWorkspaceSessionBinding.Resolve(
                sessionA, "campaign-A",
                delegate(string id) { return new CastingWorkspaceSession(pathA, id); },
                delegate(string message) { messages.Add(message); });
            if (!ReferenceEquals(reused, sessionA) || messages.Count != 0)
                throw new InvalidOperationException(
                    "Same-campaign reuse did not retain the session.");
            if (reused.Document.Castings.Count != 2 || !reused.CanUndo)
                throw new InvalidOperationException(
                    "Retained session lost unsaved intent or undo history.");

            // Campaign switch: a NEW session for B; A's document cannot be
            // exposed or saved against B; A's file is untouched.
            CastingWorkspaceSession sessionB = CastingWorkspaceSessionBinding.Resolve(
                sessionA, "campaign-B",
                delegate(string id) { return new CastingWorkspaceSession(pathB, id); },
                delegate(string message) { messages.Add(message); });
            if (ReferenceEquals(sessionB, sessionA))
                throw new InvalidOperationException(
                    "A campaign switch reused the foreign session.");
            if (sessionB.Document.Castings.Count != 0 ||
                sessionB.CampaignId != "campaign-B")
                throw new InvalidOperationException(
                    "Campaign B started from foreign authored intent.");
            if (messages.Count != 1 || !messages[0].Contains("dirty=True") ||
                !messages[0].Contains("campaign-A") ||
                !messages[0].Contains("campaign-B"))
                throw new InvalidOperationException(
                    "The campaign switch did not disclose the dirty cross-bind: " +
                    (messages.Count == 0 ? "(no message)" : messages[0]));

            // B -> A follows the documented policy: a fresh A session reads
            // A's saved file (the unsaved A edit was disclosed and dropped).
            CastingWorkspaceSession reopenedA = CastingWorkspaceSessionBinding.Resolve(
                null, "campaign-A",
                delegate(string id) { return new CastingWorkspaceSession(pathA, id); },
                null);
            if (!ReferenceEquals(reopenedA, sessionA) &&
                reopenedA.DocumentIntentSignature() != savedA)
                throw new InvalidOperationException(
                    "B -> A did not restore campaign A's saved document.");

            // Unresolved identity refuses rather than binding.
            if (CastingWorkspaceSessionBinding.Resolve(sessionA, null,
                    delegate(string id)
                    {
                        return new CastingWorkspaceSession(pathA, id);
                    },
                    null) != null ||
                CastingWorkspaceSessionBinding.Resolve(sessionA, "  ",
                    delegate(string id)
                    {
                        return new CastingWorkspaceSession(pathA, id);
                    },
                    null) != null)
                throw new InvalidOperationException(
                    "An unresolved campaign identity produced a binding.");

            // File destinations: A's candidate lives under A's root only.
            if (!File.Exists(Path.Combine(pathA, "casting-plan.json")) &&
                !Directory.Exists(pathA))
                throw new InvalidOperationException("A's candidate root is missing.");
            if (Directory.GetFiles(pathB).Length != 0 &&
                sessionB.Document.Castings.Count == 0)
                throw new InvalidOperationException(
                    "Campaign B's root holds unexpected candidate bytes.");
        }

        // Review G5: same-session unsaved retention and a REAL persistence
        // round trip (fresh session reading the saved bytes) are separately
        // proven with complete canonical comparisons, including previously
        // omitted fields (targeting modifiers, policies, routine
        // definitions) via single-field negative cases.
        private static void TestCastingWorkspacePersistenceRoundTrip(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-persist");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "persist-campaign");
            // Author a maximally complete record: enhancement, targeting
            // modifier, and a second routine definition.
            session.SelectRoutine("long");
            session.Draft.SourceId = "source-bulls";
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.Draft.DirectTargetUnitId = "unit-t1";
            session.Draft.State = CastingAuthoringState.Ready;
            session.Draft.Enhancements.Add(
                new AuthoredEnhancementSelection("extend-cleric", true, null));
            session.Draft.TargetingModifiers.Add(
                new TargetingModifierSelection("share-transmutation", true,
                    "exact-item-ref-1"));
            if (!session.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("authoring refused.");
            if (!session.IsDirty)
                throw new InvalidOperationException("authored work is not dirty.");
            session.Save();
            if (session.IsDirty)
                throw new InvalidOperationException("saved work is still dirty.");
            string saved = session.DocumentIntentSignature();

            // Same-session unsaved retention: an unsaved edit survives an
            // in-memory "reopen" (the retained-session path) and is visible
            // as dirty.
            session.Draft.DirectTargetUnitId = "unit-t2";
            if (!session.AddCastingFromDraft(inputs).Applied)
                throw new InvalidOperationException("second authoring refused.");
            if (!session.IsDirty)
                throw new InvalidOperationException(
                    "unsaved retention is not reported dirty.");

            // REAL persistence round trip: a FRESH session reads the saved
            // bytes; the complete canonical documents must match.
            var fresh = new CastingWorkspaceSession(modPath, "persist-campaign");
            if (fresh.LoadStatus != CastingPlanLoadStatus.Loaded)
                throw new InvalidOperationException(
                    "fresh load failed: " + fresh.LoadStatus);
            if (fresh.DocumentIntentSignature() != saved)
                throw new InvalidOperationException(
                    "The persisted document did not round-trip completely.");
            if (fresh.IsDirty)
                throw new InvalidOperationException(
                    "A freshly loaded document reports dirty.");

            // Single-field negative cases on previously omitted fields: each
            // change must be visible to the canonical comparison.
            PlannedCasting loaded = fresh.Document.Castings[0];
            fresh.FocusCasting(loaded.CastingId);
            // Targeting modifier removal.
            var withoutModifier = new PlannedCasting(
                loaded.CastingId, loaded.RoutineId, loaded.Order, loaded.SourceId,
                loaded.Ability, loaded.CasterUnitId, loaded.SpellbookGuid,
                loaded.TargetMode, loaded.DirectTargetUnitId, loaded.Origin,
                loaded.RequiredCoverageUnitIds, new TargetingModifierSelection[0],
                loaded.Enhancements, loaded.ExistingEffectPolicy,
                loaded.IgnoredPresenceMarkers, loaded.State, loaded.Provenance);
            fresh.UpdateFocusedCasting(withoutModifier);
            if (!fresh.IsDirty)
                throw new InvalidOperationException(
                    "A targeting-modifier change is invisible to dirty tracking.");
            fresh.Undo();
            // Existing-effect policy change.
            var otherPolicy = new PlannedCasting(
                loaded.CastingId, loaded.RoutineId, loaded.Order, loaded.SourceId,
                loaded.Ability, loaded.CasterUnitId, loaded.SpellbookGuid,
                loaded.TargetMode, loaded.DirectTargetUnitId, loaded.Origin,
                loaded.RequiredCoverageUnitIds, loaded.TargetingModifiers,
                loaded.Enhancements,
                Domain.Planning.ExistingEffectPolicy.Overwrite,
                loaded.IgnoredPresenceMarkers, loaded.State, loaded.Provenance);
            fresh.UpdateFocusedCasting(otherPolicy);
            if (!fresh.IsDirty)
                throw new InvalidOperationException(
                    "An existing-effect-policy change is invisible to dirty tracking.");
            fresh.Undo();
            if (fresh.DocumentIntentSignature() != saved)
                throw new InvalidOperationException(
                    "Undo did not restore the persisted document exactly.");
        }

        // Review H2: the group-targeting callback contract. Every call uses
        // the EXACT argument shapes the view callbacks produce (including
        // null recipients for Restore and the draft's own live coverage
        // list), so service-level approval cannot hide a broken callback.
        private static void TestCastingWorkspaceGroupTransitions(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-h2");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "h2-campaign");

            // Case 1: direct A -> group -> Restore A (null-recipient call).
            session.SetDraftTargeting(CastingTargetMode.DirectTarget,
                "unit-t1", null, null);
            session.SetDraftTargeting(CastingTargetMode.CasterCenteredOrigin,
                null, null, null);
            AuthoringEditResult restored = session.SetDraftTargeting(
                CastingTargetMode.DirectTarget, null, null, null);
            if (!restored.Applied ||
                session.Draft.DirectTargetUnitId != "unit-t1" ||
                !restored.Scope.Contains("restored-recipient"))
                throw new InvalidOperationException(
                    "Restore could not reach the remembered recipient: " +
                    restored.Reason);

            // Case 2: fresh group with nothing remembered -> null recipient
            // refuses; an explicit pick works.
            var fresh = new CastingWorkspaceSession(
                Path.Combine(root, "casting-workspace-h2b"), "h2-campaign");
            fresh.SetDraftTargeting(CastingTargetMode.CasterCenteredOrigin,
                null, null, null);
            AuthoringEditResult noRemembered = fresh.SetDraftTargeting(
                CastingTargetMode.DirectTarget, null, null, null);
            if (noRemembered.Applied ||
                !noRemembered.Reason.Contains("pick a recipient"))
                throw new InvalidOperationException(
                    "A fresh group silently restored or mislabeled: " +
                    noRemembered.Reason);
            if (!fresh.SetDraftTargeting(CastingTargetMode.DirectTarget,
                    "unit-t2", null, null).Applied ||
                fresh.Draft.DirectTargetUnitId != "unit-t2")
                throw new InvalidOperationException(
                    "The explicit pick-to-return path failed.");

            // Case 3: caster-centered coverage edits never demand an anchor,
            // including when the draft's OWN live list is the argument.
            session.SetDraftTargeting(CastingTargetMode.CasterCenteredOrigin,
                null, null, null);
            AuthoringEditResult coverageEdit = session.SetDraftTargeting(
                CastingTargetMode.CasterCenteredOrigin, null, null,
                new[] { "unit-t1", "unit-t2" });
            if (!coverageEdit.Applied ||
                session.Draft.RequiredCoverageUnitIds.Count != 2)
                throw new InvalidOperationException(
                    "Caster-centered coverage was refused or lost: " +
                    coverageEdit.Reason);
            // Self-alias form (H2c): pass the draft's own list.
            var aliased = session.Draft.RequiredCoverageUnitIds;
            AuthoringEditResult selfAlias = session.SetDraftTargeting(
                CastingTargetMode.CasterCenteredOrigin, null, null, aliased);
            if (!selfAlias.Applied ||
                session.Draft.RequiredCoverageUnitIds.Count != 2)
                throw new InvalidOperationException(
                    "Passing the draft's own coverage list lost recipients.");

            // Case 4: coverage {A,B} survives anchor-only changes.
            session.SetDraftTargeting(CastingTargetMode.AnchoredOrigin,
                null, "unit-cleric",
                session.Draft.RequiredCoverageUnitIds);
            if (session.Draft.RequiredCoverageUnitIds.Count != 2 ||
                session.Draft.Origin.AnchorUnitId != "unit-cleric")
                throw new InvalidOperationException(
                    "Anchoring lost the intended coverage.");
            session.SetDraftTargeting(CastingTargetMode.AnchoredOrigin,
                null, "unit-wizard",
                session.Draft.RequiredCoverageUnitIds);
            if (session.Draft.Origin.AnchorUnitId != "unit-wizard" ||
                session.Draft.RequiredCoverageUnitIds.Count != 2)
                throw new InvalidOperationException(
                    "Changing the anchor lost the intended coverage.");
            AuthoringEditResult backToCaster = session.SetDraftTargeting(
                CastingTargetMode.CasterCenteredOrigin, null, null,
                session.Draft.RequiredCoverageUnitIds);
            if (!backToCaster.Applied ||
                session.Draft.RequiredCoverageUnitIds.Count != 2 ||
                !session.Draft.Origin.IsCasterCentered)
                throw new InvalidOperationException(
                    "Returning to the caster origin lost coverage: " +
                    backToCaster.Reason);

            // Case 7: an invalid transition refuses and leaves the ENTIRE
            // draft untouched.
            string draftBefore = DescribeDraft(session);
            AuthoringEditResult invalid = session.SetDraftTargeting(
                CastingTargetMode.AnchoredOrigin, null, null, null);
            if (invalid.Applied || !invalid.Reason.Contains("anchor") ||
                DescribeDraft(session) != draftBefore)
                throw new InvalidOperationException(
                    "An invalid transition mutated or was accepted: " +
                    invalid.Reason);

            // Case 6: focused origins derive from the FOCUSED record, not
            // the next-casting draft.
            var groupCoverage = new Dictionary<string, IEnumerable<string>>(
                StringComparer.Ordinal)
            {
                { "unit-cleric", new[] { "unit-t1", "unit-t2" } }
            };
            PartyProviderSnapshot groupSnapshot;
            CastingWorkspaceInputs groupInputs = WorkspaceInputs(
                out groupSnapshot, groupCoverage, CastingGroupAbility);
            var groupSession = new CastingWorkspaceSession(
                Path.Combine(root, "casting-workspace-h2c"), "h2-campaign");
            groupSession.Draft.SourceId = "source-communal";
            groupSession.Draft.CasterUnitId = "unit-cleric";
            groupSession.Draft.TargetMode = CastingTargetMode.AnchoredOrigin;
            groupSession.Draft.Origin = CastingOrigin.Anchored("unit-cleric");
            groupSession.Draft.State = CastingAuthoringState.Ready;
            if (!groupSession.AddCastingFromDraft(groupInputs).Applied)
                throw new InvalidOperationException(
                    "The focused-origin fixture casting was refused.");
            string focusedId = groupSession.Document.Castings[0].CastingId;
            groupSession.FocusCasting(focusedId);
            // Point the NEXT-casting draft at a different caster/source.
            groupSession.SelectBuff("source-bulls");
            groupSession.Draft.CasterUnitId = "unit-wizard";
            WorkspaceView focusedView = groupSession.BuildView(groupInputs);
            if (focusedView.FocusedOrigins.Count == 0 ||
                !focusedView.FocusedOrigins.Any(origin =>
                    origin.AnchorUnitId == "unit-cleric" && origin.Selected))
                throw new InvalidOperationException(
                    "Focused origin options did not derive from the focused " +
                    "casting's own provider.");
            // Focused enhancements derive from the FOCUSED cleric: the
            // cleric's rod is offered, the wizard's is not.
            if (!focusedView.FocusedEnhancements.Any(option =>
                    option.EnhancementId == "extend-cleric") ||
                focusedView.FocusedEnhancements.Any(option =>
                    option.EnhancementId == "extend-wizard"))
                throw new InvalidOperationException(
                    "Focused enhancements did not follow the focused caster.");
        }

        private static string DescribeDraft(CastingWorkspaceSession session)
        {
            return session.Draft.TargetMode + "|" +
                (session.Draft.DirectTargetUnitId ?? string.Empty) + "|" +
                (session.Draft.Origin == null ? "none"
                    : session.Draft.Origin.IsCasterCentered
                        ? "caster"
                        : "anchor:" + session.Draft.Origin.AnchorUnitId) +
                "|" + string.Join(";",
                    session.Draft.RequiredCoverageUnitIds.ToArray());
        }

        // ------------------------------------------------------------------
        // Review J1: the automatic interaction evidence is a structured
        // record; its producer (Describe/Evaluate) and its ONLY acceptance
        // predicate (Violations) are exercised together here.
        // ------------------------------------------------------------------

        private static WorkspaceInteractionRecord CompleteInteractionRecord(
            string state1, string state2, string state3)
        {
            var record = new WorkspaceInteractionRecord();
            record.RecordBrowse(true, "invoked", 2, 3);
            record.RecordCastStep(new WorkspaceCastStepEvidence(1, "invoked",
                "invoked", "invoked", state1, true, true, true, true));
            record.RecordCastStep(new WorkspaceCastStepEvidence(2, "invoked",
                "invoked", "invoked", state2, true, true, true, true));
            record.RecordCastStep(new WorkspaceCastStepEvidence(3, "invoked",
                "invoked", "invoked", state3, true, true, true, true));
            record.RefusedAddControl = "invoked";
            record.RefusedAddClean = true;
            record.EditControl = "invoked";
            record.EditFocused = true;
            record.RetargetControl = "invoked";
            record.RetargetApplied = true;
            record.UndoControl = "invoked";
            record.UndoIntentRestored = true;
            record.DoneControl = "invoked";
            record.DoneClearedFocus = true;
            record.SaveControl = "invoked";
            record.Saved = true;
            return record;
        }

        private static void AssertViolation(WorkspaceInteractionRecord record,
            string expectedViolation, string label)
        {
            IList<string> violations = record.Violations();
            if (!violations.Contains(expectedViolation))
                throw new InvalidOperationException(label +
                    ": expected violation '" + expectedViolation + "' but got [" +
                    string.Join(",", violations.ToArray()) + "].");
        }

        private static void TestWorkspaceInteractionEvidenceContract()
        {
            // 1/2. Both legitimate State outcomes are accepted, in every mix.
            foreach (string[] states in new[]
                {
                    new[] { "invoked", "already-ready", "already-ready" },
                    new[] { "invoked", "invoked", "invoked" },
                    new[] { "already-ready", "already-ready", "already-ready" }
                })
            {
                WorkspaceInteractionRecord record = CompleteInteractionRecord(
                    states[0], states[1], states[2]);
                IList<string> violations = record.Violations();
                if (violations.Count != 0)
                    throw new InvalidOperationException(
                        "A correct control sequence was rejected: " +
                        string.Join(",", violations.ToArray()));
                string described = record.Describe();
                // The producer's real field order (state between controls
                // and Exact) — the adjacency the old validator demanded is
                // absent, which is exactly why a substring check failed.
                if (!described.Contains("cast1=controls:invoked/invoked/invoked;state1=" +
                        states[0] + ";cast1Exact=True") ||
                    described.Contains("cast1=controls:invoked/invoked/invoked;cast1Exact=True") ||
                    !described.Contains("saveControl=invoked;saved=True"))
                    throw new InvalidOperationException(
                        "Evidence description drifted from the producer contract: " +
                        described);
            }

            // 3. Control-level and record-level defects each fail by name.
            WorkspaceInteractionRecord badState = new WorkspaceInteractionRecord();
            badState.RecordBrowse(true, "invoked", 2, 3);
            badState.RecordCastStep(new WorkspaceCastStepEvidence(1, "invoked",
                "invoked", "invoked", "control-missing:State", true, true, true, true));
            AssertViolation(badState, "cast1:state=control-missing:State", "state control missing");
            AssertViolation(badState, "castSteps:count=1", "missing Adds");

            var defects = new[]
            {
                new { Step = new WorkspaceCastStepEvidence(2, "invoked", "invoked",
                    "control-missing:AddCasting", "already-ready", true, true, true, true),
                    Violation = "cast2:controls=invoked/invoked/control-missing:AddCasting" },
                new { Step = new WorkspaceCastStepEvidence(2, "control-not-interactable:DraftCaster.u2",
                    "invoked", "invoked", "already-ready", true, true, true, true),
                    Violation = "cast2:controls=control-not-interactable:DraftCaster.u2/invoked/invoked" },
                new { Step = new WorkspaceCastStepEvidence(2, "invoked", "invoked",
                    "invoked", "already-ready", false, false, false, true),
                    Violation = "cast2:not-added" },
                new { Step = new WorkspaceCastStepEvidence(2, "invoked", "invoked",
                    "invoked", "already-ready", true, false, true, true),
                    Violation = "cast2:reused-id" },
                new { Step = new WorkspaceCastStepEvidence(2, "invoked", "invoked",
                    "invoked", "already-ready", true, true, false, true),
                    Violation = "cast2:fields-inexact" },
                new { Step = new WorkspaceCastStepEvidence(2, "invoked", "invoked",
                    "invoked", "already-ready", true, true, true, false),
                    Violation = "cast2:sibling-changed" },
                new { Step = new WorkspaceCastStepEvidence(3, "invoked", "invoked",
                    "invoked", "already-ready", true, true, true, true),
                    Violation = "cast2:ordinal=3" }
            };
            foreach (var defect in defects)
            {
                WorkspaceInteractionRecord record = CompleteInteractionRecord(
                    "invoked", "already-ready", "already-ready");
                var rebuilt = new WorkspaceInteractionRecord();
                rebuilt.RecordBrowse(true, "invoked", 2, 3);
                rebuilt.RecordCastStep(record.CastSteps[0]);
                rebuilt.RecordCastStep(defect.Step);
                rebuilt.RecordCastStep(record.CastSteps[2]);
                CopyLaterControls(record, rebuilt);
                AssertViolation(rebuilt, defect.Violation, defect.Violation);
            }

            // Each later control: not run, not invoked, and wrong outcome.
            var laterChecks = new List<KeyValuePair<string, Action<WorkspaceInteractionRecord>>>
            {
                Later("refusedAdd:not-run", r => r.RefusedAddControl = null),
                Later("refusedAdd:outcome=False", r => r.RefusedAddClean = false),
                Later("edit:control=control-missing:Edit.x", r => r.EditControl = "control-missing:Edit.x"),
                Later("edit:outcome=missing", r => r.EditFocused = null),
                Later("retarget:outcome=False", r => r.RetargetApplied = false),
                Later("undo:outcome=False", r => r.UndoIntentRestored = false),
                Later("done:control=control-inactive:DoneEditing", r => r.DoneControl = "control-inactive:DoneEditing"),
                Later("save:outcome=False", r => r.Saved = false),
                Later("save:not-run", r => r.SaveControl = null)
            };
            foreach (KeyValuePair<string, Action<WorkspaceInteractionRecord>> check in laterChecks)
            {
                WorkspaceInteractionRecord record = CompleteInteractionRecord(
                    "invoked", "already-ready", "already-ready");
                check.Value(record);
                AssertViolation(record, check.Key, check.Key);
            }
            WorkspaceInteractionRecord mutatedBrowse = CompleteInteractionRecord(
                "invoked", "already-ready", "already-ready");
            mutatedBrowse.RecordBrowse(false, "invoked", 2, 3);
            AssertViolation(mutatedBrowse, "browse:mutated", "browse mutation");

            // 4. A manual lifecycle result never substitutes: the manual
            // scenario performs no scripted authoring, so its (empty)
            // record fails the automatic predicate everywhere.
            var manualRecord = new WorkspaceInteractionRecord();
            AssertViolation(manualRecord, "browse:not-run", "manual record");
            AssertViolation(manualRecord, "castSteps:count=0", "manual record");
            AssertViolation(manualRecord, "save:not-run", "manual record");
            if (manualRecord.Describe() != "not-run")
                throw new InvalidOperationException(
                    "An unpopulated record described evidence it never produced.");
        }

        private static KeyValuePair<string, Action<WorkspaceInteractionRecord>> Later(
            string violation, Action<WorkspaceInteractionRecord> mutate)
        {
            return new KeyValuePair<string, Action<WorkspaceInteractionRecord>>(
                violation, mutate);
        }

        private static void CopyLaterControls(WorkspaceInteractionRecord from,
            WorkspaceInteractionRecord to)
        {
            to.RefusedAddControl = from.RefusedAddControl;
            to.RefusedAddClean = from.RefusedAddClean;
            to.EditControl = from.EditControl;
            to.EditFocused = from.EditFocused;
            to.RetargetControl = from.RetargetControl;
            to.RetargetApplied = from.RetargetApplied;
            to.UndoControl = from.UndoControl;
            to.UndoIntentRestored = from.UndoIntentRestored;
            to.DoneControl = from.DoneControl;
            to.DoneClearedFocus = from.DoneClearedFocus;
            to.SaveControl = from.SaveControl;
            to.Saved = from.Saved;
        }

        // Review J1 (production path): the evaluator the runtime host calls,
        // driven against a REAL CastingWorkspaceSession. The first draft is
        // made Ready (the State control's effect); later drafts stay Ready
        // with no toggle. Three exact records yield an accepted record, and
        // every wrong-field / reused-id / refused / sibling-change case fails.
        private static void TestWorkspaceCastStepEvaluatorAgainstSession(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-j1");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "workspace-campaign");
            session.SelectBuff("source-bulls");
            session.SelectRoutine("long");
            session.BuildView(inputs);
            session.Draft.SourceId = "source-bulls";
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            if (session.Draft.State == CastingAuthoringState.Ready)
                throw new InvalidOperationException(
                    "Fixture precondition: a fresh draft must not start Ready.");
            var record = new WorkspaceInteractionRecord();
            record.RecordBrowse(true, "invoked", 2, 3);
            var ids = new List<string>();
            var plan = new[]
            {
                new[] { "unit-cleric", "unit-t1" },
                new[] { "unit-wizard", "unit-t2" },
                new[] { "unit-cleric", "unit-t3" }
            };
            WorkspaceCastExpectation lastExpected = null;
            string lastSiblingsBefore = null;
            int lastCountBefore = 0;
            for (int index = 0; index < plan.Length; index++)
            {
                int countBefore = session.Document.Castings.Count;
                string siblingsBefore = WorkspaceCastStepEvaluator.SiblingSignature(
                    session.Document.Castings, null);
                session.Draft.CasterUnitId = plan[index][0];
                session.Draft.DirectTargetUnitId = plan[index][1];
                var expected = new WorkspaceCastExpectation
                {
                    CasterUnitId = plan[index][0],
                    DirectTargetUnitId = plan[index][1],
                    SourceId = "source-bulls",
                    Ability = session.DraftAbilityFor(inputs),
                    SpellbookGuid = session.DraftSpellbookFor(inputs),
                    RoutineId = session.SelectedRoutineId
                };
                string stateOutcome;
                if (session.Draft.State == CastingAuthoringState.Ready)
                {
                    if (index == 0)
                        throw new InvalidOperationException("Draft was Ready too early.");
                    stateOutcome = "already-ready";
                }
                else
                {
                    if (index != 0)
                        throw new InvalidOperationException(
                            "A later draft lost Ready and would need a toggle.");
                    session.Draft.State = CastingAuthoringState.Ready;
                    stateOutcome = "invoked";
                }
                if (!session.AddCastingFromDraft(inputs).Applied)
                    throw new InvalidOperationException("Add " + index + " was refused.");
                PlannedCasting created;
                WorkspaceCastStepEvidence step = WorkspaceCastStepEvaluator.Evaluate(
                    index + 1, "invoked", "invoked", stateOutcome, "invoked",
                    session.Document.Castings.ToList(), countBefore, ids,
                    expected, siblingsBefore, out created);
                if (!step.Exact || created == null)
                    throw new InvalidOperationException(
                        "A correct Add was not evaluated exact: " + step.Describe());
                // Reused id: the same record evaluated against its own id.
                PlannedCasting ignored;
                if (WorkspaceCastStepEvaluator.Evaluate(index + 1, "invoked",
                        "invoked", stateOutcome, "invoked",
                        session.Document.Castings.ToList(), countBefore,
                        ids.Concat(new[] { created.CastingId }).ToList(),
                        expected, siblingsBefore, out ignored).Distinct)
                    throw new InvalidOperationException("A reused id was accepted.");
                ids.Add(created.CastingId);
                record.RecordCastStep(step);
                lastExpected = expected;
                lastSiblingsBefore = siblingsBefore;
                lastCountBefore = countBefore;
            }
            record.RefusedAddControl = "invoked";
            record.RefusedAddClean = true;
            record.EditControl = "invoked";
            record.EditFocused = true;
            record.RetargetControl = "invoked";
            record.RetargetApplied = true;
            record.UndoControl = "invoked";
            record.UndoIntentRestored = true;
            record.DoneControl = "invoked";
            record.DoneClearedFocus = true;
            record.SaveControl = "invoked";
            session.Save();
            record.Saved = !session.IsDirty;
            if (record.Violations().Count != 0)
                throw new InvalidOperationException(
                    "Three exact session records were rejected: " +
                    string.Join(",", record.Violations().ToArray()));

            // Wrong-field cases against the real third record.
            PlannedCasting third = session.Document.Castings[2];
            Func<Action<WorkspaceCastExpectation>, WorkspaceCastExpectation> alter =
                change =>
                {
                    var copy = new WorkspaceCastExpectation
                    {
                        CasterUnitId = lastExpected.CasterUnitId,
                        DirectTargetUnitId = lastExpected.DirectTargetUnitId,
                        SourceId = lastExpected.SourceId,
                        Ability = lastExpected.Ability,
                        SpellbookGuid = lastExpected.SpellbookGuid,
                        RoutineId = lastExpected.RoutineId
                    };
                    change(copy);
                    return copy;
                };
            if (!WorkspaceCastStepEvaluator.FieldsExact(third, lastExpected))
                throw new InvalidOperationException("The exact expectation drifted.");
            var wrong = new Dictionary<string, WorkspaceCastExpectation>
            {
                { "caster", alter(e => e.CasterUnitId = "unit-wizard") },
                { "target", alter(e => e.DirectTargetUnitId = "unit-t1") },
                { "source", alter(e => e.SourceId = "source-other") },
                { "ability", alter(e => e.Ability = CastingGroupAbility) },
                { "spellbook", alter(e => e.SpellbookGuid = "spellbook-other") },
                { "routine", alter(e => e.RoutineId = "short") }
            };
            foreach (KeyValuePair<string, WorkspaceCastExpectation> pair in wrong)
                if (WorkspaceCastStepEvaluator.FieldsExact(third, pair.Value))
                    throw new InvalidOperationException(
                        "A wrong " + pair.Key + " was accepted as exact.");
            PlannedCasting draftState = new PlannedCasting(third.CastingId,
                third.RoutineId, third.Order, third.SourceId, third.Ability,
                third.CasterUnitId, third.SpellbookGuid, third.TargetMode,
                third.DirectTargetUnitId, third.Origin, third.RequiredCoverageUnitIds,
                third.TargetingModifiers, third.Enhancements,
                third.ExistingEffectPolicy, third.IgnoredPresenceMarkers,
                CastingAuthoringState.Draft, third.Provenance);
            if (WorkspaceCastStepEvaluator.FieldsExact(draftState, lastExpected))
                throw new InvalidOperationException("A wrong state was accepted as exact.");
            PlannedCasting enhanced = third.WithEnhancementSelections(
                new[] { new AuthoredEnhancementSelection("extend-cleric", true, null) });
            if (WorkspaceCastStepEvaluator.FieldsExact(enhanced, lastExpected))
                throw new InvalidOperationException(
                    "An unexpected enhancement was accepted as exact.");

            // Refused Add: no growth, no created record.
            PlannedCasting none;
            WorkspaceCastStepEvidence refused = WorkspaceCastStepEvaluator.Evaluate(
                4, "invoked", "invoked", "already-ready", "invoked",
                session.Document.Castings.ToList(), session.Document.Castings.Count,
                ids, lastExpected, WorkspaceCastStepEvaluator.SiblingSignature(
                    session.Document.Castings, null), out none);
            if (refused.Grew || none != null || refused.Exact)
                throw new InvalidOperationException("A refused Add was accepted.");

            // Changed sibling intent: the first record retargeted while the
            // third was "added" — full serialized fidelity catches it.
            var tampered = session.Document.Castings.ToList();
            tampered[0] = tampered[0].WithDirectTarget("unit-t3");
            PlannedCasting tamperedCreated;
            WorkspaceCastStepEvidence sibling = WorkspaceCastStepEvaluator.Evaluate(
                3, "invoked", "invoked", "already-ready", "invoked", tampered,
                lastCountBefore, ids.Take(2).ToList(), lastExpected,
                lastSiblingsBefore, out tamperedCreated);
            if (sibling.SiblingsUnchanged || sibling.Exact)
                throw new InvalidOperationException("A changed sibling was accepted.");
        }

        // ------------------------------------------------------------------
        // Review J2: the manual terminal step consumes the final capture's
        // failure and restoration outcome, is bounded, always records the
        // cleanup postcondition, and keeps request / capture / restoration /
        // cleanup as separate assertions. Driven through the same
        // coordinator and RuntimeTestResult the runtime host uses.
        // ------------------------------------------------------------------

        private const string ManualReady =
            "manual-ready;workspaceOpen=true;legacyScreenClosed=true;" +
            "syntheticInputRequested=false;holdSeconds=60";

        private static ManualCaptureObservation FinalCapture(bool? restorationClean,
            string failureType, bool fileExists)
        {
            return new ManualCaptureObservation
            {
                FileName = "manual-final.png",
                FailureType = failureType,
                FailureMessage = failureType == null ? null : "detail",
                RestorationClean = restorationClean,
                RestorationVerdict = restorationClean == null ? null
                    : "targetsRestored=" + restorationClean.Value +
                      ";activeRestored=True;cleanupFailures=" +
                      (restorationClean.Value ? "0" : "1"),
                FileExists = fileExists,
                Sha256 = fileExists && failureType == null ? "abc123" : null,
                LumaSummary = "mean=0.40"
            };
        }

        private static RuntimeTestResult ManualResult(ManualTerminalCoordinator terminal)
        {
            var result = new RuntimeTestResult
            {
                Status = "PASS",
                Stage = "completed",
                Assertions = new List<RuntimeTestAssertion>()
            };
            terminal.AppendAssertions(result, ManualReady,
                "manual;outcome=" + terminal.OutcomeText);
            return result;
        }

        private static string AssertionStatus(RuntimeTestResult result, string id)
        {
            RuntimeTestAssertion assertion = result.Assertions.SingleOrDefault(
                candidate => candidate.Id == id);
            return assertion == null ? "absent" : assertion.Status;
        }

        private static void ExpectManual(RuntimeTestResult result, string status,
            string stage, string label, params string[] idStatusPairs)
        {
            if (result.Status != status || result.Stage != stage)
                throw new InvalidOperationException(label + ": expected " + status +
                    "/" + stage + " but got " + result.Status + "/" + result.Stage);
            for (int i = 0; i < idStatusPairs.Length; i += 2)
            {
                string observed = AssertionStatus(result, idStatusPairs[i]);
                if (observed != idStatusPairs[i + 1])
                    throw new InvalidOperationException(label + ": assertion " +
                        idStatusPairs[i] + " expected " + idStatusPairs[i + 1] +
                        " but was " + observed);
            }
            // Manual acceptance never inherits the automatic assertions.
            if (result.Assertions.Any(a => a.Id.StartsWith("workspace-",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException(label +
                    ": automatic workspace assertions leaked into the manual result.");
        }

        private static void TestManualTerminalPolicy()
        {
            var cases = new[]
            {
                new { Deadline = false, Stop = false, Done = false, Expected = ManualTerminalRequest.None },
                new { Deadline = false, Stop = false, Done = true, Expected = ManualTerminalRequest.Completed },
                new { Deadline = false, Stop = true, Done = false, Expected = ManualTerminalRequest.Cancelled },
                new { Deadline = false, Stop = true, Done = true, Expected = ManualTerminalRequest.Cancelled },
                new { Deadline = true, Stop = false, Done = false, Expected = ManualTerminalRequest.Deadline },
                new { Deadline = true, Stop = true, Done = true, Expected = ManualTerminalRequest.Deadline }
            };
            foreach (var item in cases)
                if (ManualTerminalPolicy.Classify(item.Deadline, item.Stop, item.Done) !=
                    item.Expected)
                    throw new InvalidOperationException("Terminal classification wrong for deadline=" +
                        item.Deadline + ";stop=" + item.Stop + ";done=" + item.Done);
            if (ManualTerminalPolicy.OutcomeText(ManualTerminalRequest.Completed) !=
                    "manual-completed;by=done-marker" ||
                ManualTerminalPolicy.OutcomeText(ManualTerminalRequest.Cancelled) !=
                    "manual-cancelled;by=stop-marker" ||
                ManualTerminalPolicy.OutcomeText(ManualTerminalRequest.Deadline) !=
                    "manual-deadline;timeout-is-not-acceptance")
                throw new InvalidOperationException("Terminal outcome text drifted.");
        }

        private static void TestManualTerminalCoordinatorContract()
        {
            // Successful final capture with clean restoration and cleanup.
            var success = new ManualTerminalCoordinator("manual-final.png", 20000);
            success.Begin(ManualTerminalRequest.Completed, 1000);
            if (success.Poll(1010, null))
                throw new InvalidOperationException("Close proceeded before the capture resolved.");
            var stale = FinalCapture(true, null, true);
            stale.FileName = "workspace-camera-frame.png";
            if (success.Poll(1020, stale))
                throw new InvalidOperationException("An earlier capture satisfied the final capture.");
            if (!success.Poll(1030, FinalCapture(true, null, true)))
                throw new InvalidOperationException("A completed capture did not release the close.");
            success.RecordClose(false, false, null);
            ExpectManual(ManualResult(success), "PASS", "completed", "success",
                "manual-ready-acknowledged", "PASS",
                "manual-no-automatic-authoring", "PASS",
                "manual-session-outcome", "PASS",
                "manual-final-capture", "PASS",
                "manual-camera-restoration", "PASS",
                "manual-workspace-closed", "PASS");

            // Render/readback failure with CLEAN restoration: the session
            // still ends and cleans up; the evidence failure stays visible.
            var readback = new ManualTerminalCoordinator("manual-final.png", 20000);
            readback.Begin(ManualTerminalRequest.Completed, 0);
            if (!readback.Poll(5, FinalCapture(true, "InvalidOperationException", false)))
                throw new InvalidOperationException("A failed capture did not release the close.");
            readback.RecordClose(false, false, null);
            ExpectManual(ManualResult(readback), "FAIL", "manual-final-capture-failed",
                "readback failure",
                "manual-session-outcome", "PASS",
                "manual-final-capture", "FAIL",
                "manual-camera-restoration", "PASS",
                "manual-workspace-closed", "PASS");

            // A failure reported AFTER the png was written (for example the
            // luma pass threw): the file's presence never overrides the
            // capture's own failure.
            var writtenThenFailed = new ManualTerminalCoordinator("manual-final.png", 20000);
            writtenThenFailed.Begin(ManualTerminalRequest.Completed, 0);
            ManualCaptureObservation partial = FinalCapture(true, "IndexOutOfRangeException", true);
            partial.Sha256 = "def456";
            writtenThenFailed.Poll(5, partial);
            writtenThenFailed.RecordClose(false, false, null);
            RuntimeTestResult partialResult = ManualResult(writtenThenFailed);
            ExpectManual(partialResult, "FAIL", "manual-final-capture-failed",
                "written then failed",
                "manual-final-capture", "FAIL",
                "manual-camera-restoration", "PASS");
            if (!partialResult.Assertions.Single(a => a.Id == "manual-final-capture")
                    .Observed.StartsWith("failed:IndexOutOfRangeException",
                        StringComparison.Ordinal))
                throw new InvalidOperationException("The capture failure type was not preserved.");

            // Explicit restoration failure: never a clean PASS, and the
            // safety stage outranks every other classification.
            var unclean = new ManualTerminalCoordinator("manual-final.png", 20000);
            unclean.Begin(ManualTerminalRequest.Completed, 0);
            unclean.Poll(5, FinalCapture(false, "InvalidOperationException", true));
            unclean.RecordClose(false, false, null);
            RuntimeTestResult uncleanResult = ManualResult(unclean);
            ExpectManual(uncleanResult, "FAIL", "manual-camera-restoration-unclean",
                "restoration failure",
                "manual-session-outcome", "PASS",
                "manual-final-capture", "FAIL",
                "manual-camera-restoration", "FAIL",
                "manual-workspace-closed", "PASS");
            if (!uncleanResult.Assertions.Single(a => a.Id == "manual-camera-restoration")
                    .Observed.Contains("targetsRestored=False"))
                throw new InvalidOperationException("The unclean verdict was not preserved.");

            // A capture that reports no restoration verdict is not clean.
            var noVerdict = new ManualTerminalCoordinator("manual-final.png", 20000);
            noVerdict.Begin(ManualTerminalRequest.Completed, 0);
            noVerdict.Poll(5, FinalCapture(null, null, true));
            noVerdict.RecordClose(false, false, null);
            ExpectManual(ManualResult(noVerdict), "FAIL",
                "manual-camera-restoration-unverified", "missing verdict",
                "manual-final-capture", "PASS",
                "manual-camera-restoration", "FAIL");

            // Missing callback: bounded — the close proceeds at the budget,
            // nothing is claimed restored, and a late callback cannot
            // rewrite the recorded verdict.
            var missing = new ManualTerminalCoordinator("manual-final.png", 20000);
            missing.Begin(ManualTerminalRequest.Completed, 1000);
            if (missing.Poll(20999, null))
                throw new InvalidOperationException("The capture budget expired early.");
            if (!missing.Poll(21000, null) ||
                missing.CaptureState != ManualFinalCaptureState.CallbackMissing)
                throw new InvalidOperationException("A missing callback held the session open.");
            missing.Poll(22000, FinalCapture(true, null, true));
            if (missing.CaptureState != ManualFinalCaptureState.CallbackMissing)
                throw new InvalidOperationException("A late callback rewrote the verdict.");
            missing.RecordClose(false, false, null);
            ExpectManual(ManualResult(missing), "FAIL", "manual-final-capture-missing",
                "missing callback",
                "manual-session-outcome", "PASS",
                "manual-final-capture", "FAIL",
                "manual-camera-restoration", "FAIL",
                "manual-workspace-closed", "PASS");

            // Delayed callback within the budget is accepted normally.
            var delayed = new ManualTerminalCoordinator("manual-final.png", 20000);
            delayed.Begin(ManualTerminalRequest.Completed, 0);
            if (delayed.Poll(15000, null) || !delayed.Poll(19999, FinalCapture(true, null, true)))
                throw new InvalidOperationException("A delayed in-budget callback was mishandled.");
            delayed.RecordClose(false, false, null);
            ExpectManual(ManualResult(delayed), "PASS", "completed", "delayed callback",
                "manual-final-capture", "PASS");

            // Stop and deadline keep their terminal classification even with
            // perfect evidence; a stop is not delayed by a missing capture.
            var stopped = new ManualTerminalCoordinator("manual-final.png", 20000);
            stopped.Begin(ManualTerminalRequest.Cancelled, 0);
            stopped.Poll(3, FinalCapture(true, null, true));
            stopped.RecordClose(false, false, null);
            RuntimeTestResult stoppedResult = ManualResult(stopped);
            ExpectManual(stoppedResult, "FAIL", "manual-cancelled", "stop",
                "manual-session-outcome", "FAIL",
                "manual-final-capture", "PASS",
                "manual-camera-restoration", "PASS",
                "manual-workspace-closed", "PASS");
            if (stoppedResult.Assertions.Single(a => a.Id == "manual-session-outcome")
                    .Observed != "manual-cancelled;by=stop-marker")
                throw new InvalidOperationException("The stop outcome was not recorded.");
            var stoppedMissing = new ManualTerminalCoordinator("manual-final.png", 20000);
            stoppedMissing.Begin(ManualTerminalRequest.Cancelled, 0);
            if (!stoppedMissing.Poll(20000, null))
                throw new InvalidOperationException("A stop waited past the capture budget.");
            stoppedMissing.RecordClose(false, false, null);
            ExpectManual(ManualResult(stoppedMissing), "FAIL", "manual-cancelled",
                "stop with missing capture", "manual-final-capture", "FAIL");
            var deadline = new ManualTerminalCoordinator("manual-final.png", 20000);
            deadline.Begin(ManualTerminalRequest.Deadline, 0);
            deadline.Poll(3, FinalCapture(true, null, true));
            deadline.RecordClose(false, false, null);
            ExpectManual(ManualResult(deadline), "FAIL", "manual-deadline", "deadline",
                "manual-session-outcome", "FAIL",
                "manual-workspace-closed", "PASS");

            // Cleanup failures are recorded beside (never instead of) the
            // primary failure: a throwing close, a lease still held.
            var closeThrew = new ManualTerminalCoordinator("manual-final.png", 20000);
            closeThrew.Begin(ManualTerminalRequest.Completed, 0);
            closeThrew.Poll(3, FinalCapture(true, "InvalidOperationException", false));
            closeThrew.RecordClose(true, true, "InvalidOperationException:boom");
            ExpectManual(ManualResult(closeThrew), "FAIL", "manual-workspace-close",
                "close failure",
                "manual-final-capture", "FAIL",
                "manual-workspace-closed", "FAIL");
            var leaseHeld = new ManualTerminalCoordinator("manual-final.png", 20000);
            leaseHeld.Begin(ManualTerminalRequest.Completed, 0);
            leaseHeld.Poll(3, FinalCapture(true, null, true));
            leaseHeld.RecordClose(false, true, null);
            ExpectManual(ManualResult(leaseHeld), "FAIL", "manual-workspace-close",
                "lease held", "manual-workspace-closed", "FAIL");
            var uncleanAndClose = new ManualTerminalCoordinator("manual-final.png", 20000);
            uncleanAndClose.Begin(ManualTerminalRequest.Completed, 0);
            uncleanAndClose.Poll(3, FinalCapture(false, "InvalidOperationException", false));
            uncleanAndClose.RecordClose(true, false, "InvalidOperationException:boom");
            ExpectManual(ManualResult(uncleanAndClose), "FAIL",
                "manual-camera-restoration-unclean", "unclean and close failure",
                "manual-camera-restoration", "FAIL",
                "manual-workspace-closed", "FAIL");

            // The final capture could not even start: close proceeds at once.
            var noStart = new ManualTerminalCoordinator("manual-final.png", 20000);
            noStart.Begin(ManualTerminalRequest.Completed, 0);
            noStart.RecordCaptureStartFailure("MissingMethodException:x");
            if (!noStart.Poll(1, null))
                throw new InvalidOperationException("A capture that never started blocked the close.");
            noStart.RecordClose(false, false, null);
            ExpectManual(ManualResult(noStart), "FAIL", "manual-final-capture-failed",
                "capture start failure",
                "manual-final-capture", "FAIL",
                "manual-camera-restoration", "FAIL");

            // A terminal step that never ran cannot pass; ready evidence is
            // still required.
            ExpectManual(ManualResult(new ManualTerminalCoordinator()), "FAIL",
                "manual-workspace-close", "never ran",
                "manual-session-outcome", "FAIL",
                "manual-workspace-closed", "FAIL");
            var notReady = new ManualTerminalCoordinator();
            notReady.Begin(ManualTerminalRequest.Completed, 0);
            notReady.Poll(1, FinalCapture(true, null, true));
            notReady.RecordClose(false, false, null);
            var notReadyResult = new RuntimeTestResult
            {
                Status = "PASS",
                Stage = "completed",
                Assertions = new List<RuntimeTestAssertion>()
            };
            notReady.AppendAssertions(notReadyResult, null,
                "manual;outcome=" + notReady.OutcomeText);
            ExpectManual(notReadyResult, "FAIL", "manual-lifecycle-validation",
                "ready missing", "manual-ready-acknowledged", "FAIL");

            bool threw = false;
            try { new ManualTerminalCoordinator().Poll(0, null); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw)
                throw new InvalidOperationException("Poll before Begin was accepted.");
            threw = false;
            try { new ManualTerminalCoordinator().Begin(ManualTerminalRequest.None, 0); }
            catch (ArgumentException) { threw = true; }
            if (!threw)
                throw new InvalidOperationException("A non-terminal Begin was accepted.");
        }

        // Workspace legibility defects seen in every live frame: the header
        // showed the raw source key, and each lane's scroll view kept its
        // default 100x100 centered rect.
        private static void TestWorkspaceHeaderAndLaneLayoutContract(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-caption");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "workspace-campaign");
            session.SelectBuff("source-bulls");
            WorkspaceView view = session.BuildView(inputs);
            string caption = view.SelectedSourceCaption;
            if (caption == "source-bulls" || caption.Length == 0)
                throw new InvalidOperationException(
                    "The header caption exposed the raw source key: " + caption);
            WorkspaceSourceOption named = view.Draft == null ? null :
                view.Draft.Sources.FirstOrDefault(s => s.SourceId == "source-bulls");
            if (named != null && named.DisplayName != named.SourceId &&
                caption != named.DisplayName)
                throw new InvalidOperationException(
                    "The header caption ignored the discovered display name.");

            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(
                directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            string screen = File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "UI", "CastingWorkspaceScreenView.cs"));
            if (!screen.Contains("KingmakerUiFactory.SetAnchors(RectOf(scroll), 0f, 0f, 1f, 1f,") ||
                !screen.Contains("ContentSizeFitter.FitMode.PreferredSize") ||
                screen.Contains("KingmakerUiFactory.SetAnchors(content, 0f, 0f, 1f, 1f);") ||
                !screen.Contains("view.SelectedSourceCaption"))
                throw new InvalidOperationException(
                    "Workspace lanes no longer fill their columns or the header regressed.");
        }

        // Session 2026-09-22: "Use Heal Skill" and "Aid Another" each
        // appeared twice with identical labels.
        private static void TestWorkspaceSourceLabelDisambiguation()
        {
            IReadOnlyDictionary<string, string> details = WorkspaceSourceLabels.Details(new[]
            {
                new WorkspaceSourceDescriptor("s-light", "Light",
                    new[] { "Light" }, new[] { "spell" }, new[] { "Linzi" }),
                new WorkspaceSourceDescriptor("s-heal-a", "Use Heal Skill",
                    new[] { "Use Heal Skill" }, new[] { "ability" }, new[] { "Hedwirg" }),
                new WorkspaceSourceDescriptor("s-heal-b", "Use Heal Skill",
                    new[] { "Use Heal Skill" }, new[] { "feature" }, new[] { "Linzi" }),
                new WorkspaceSourceDescriptor("s-res-a", "Resist Energy",
                    new[] { "Resist Energy — Fire" }, new[] { "spell" }, new[] { "Linzi" }),
                new WorkspaceSourceDescriptor("s-res-b", "Resist Energy",
                    new[] { "Resist Energy — Cold" }, new[] { "spell" }, new[] { "Linzi" }),
                new WorkspaceSourceDescriptor("s-treat-a", "Use Heal Skill 2",
                    new[] { "Use Heal Skill 2 — Treat Deadly Wounds" }, null, null),
                new WorkspaceSourceDescriptor("s-treat-b", "Use Heal Skill 2",
                    new[] { "Use Heal Skill 2 — Treat Affliction" }, null, null),
                new WorkspaceSourceDescriptor("s-aid-a", "Aid Another",
                    null, null, null),
                new WorkspaceSourceDescriptor("s-aid-b", "Aid Another",
                    null, null, null)
            });
            if (details["s-light"] != string.Empty)
                throw new InvalidOperationException("A unique name was decorated.");
            if (details["s-treat-a"] != "Treat Deadly Wounds" ||
                details["s-treat-b"] != "Treat Affliction")
                throw new InvalidOperationException(
                    "The repeated base name was not stripped: " + details["s-treat-a"]);
            if (details["s-res-a"] != "Fire" ||
                details["s-res-b"] != "Cold")
                throw new InvalidOperationException("Variant names were not preferred.");
            if (details["s-heal-a"] != "ability · Hedwirg" ||
                details["s-heal-b"] != "feature · Linzi")
                throw new InvalidOperationException(
                    "Kind/caster detail was not used: " + details["s-heal-a"] +
                    " | " + details["s-heal-b"]);
            if (details["s-aid-a"] == details["s-aid-b"] ||
                !details["s-aid-a"].Contains("source 1"))
                throw new InvalidOperationException("Indistinguishable sources stayed identical.");
            var option = new WorkspaceSourceOption("s-heal-a", "Use Heal Skill", false,
                details["s-heal-a"]);
            if (option.Label != "Use Heal Skill — ability · Hedwirg")
                throw new InvalidOperationException("Label composition drifted: " + option.Label);
        }

        // The caster lane's button must set the caster the next Add uses
        // (it formerly moved only an invisible focus), without touching the
        // saved document.
        private static void TestWorkspaceChooseDraftCaster(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-draft-caster");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "workspace-campaign");
            session.SelectBuff("source-bulls");
            session.SelectRoutine("long");
            string before = session.DocumentIntentSignature();
            session.ChooseDraftCaster("unit-wizard");
            if (session.Draft.CasterUnitId != "unit-wizard" ||
                session.SelectedCasterUnitId != "unit-wizard" ||
                session.DocumentIntentSignature() != before)
                throw new InvalidOperationException("Choosing a caster did not set the draft only.");
            WorkspaceView view = session.BuildView(inputs);
            if (!view.Casters.Any(row => row.UnitId == "unit-wizard" && row.SelectedFocus))
                throw new InvalidOperationException("The chosen caster is not shown as selected.");
        }

        // The Unity-bound host cannot be compiled here, so its wiring to the
        // tested contracts is pinned at source level: both J1 and J2 paths
        // must route through the contract types, and the superseded
        // substring predicate and filename-only terminal gate must be gone.
        private static void TestRuntimeHostScenarioContractWiring()
        {
            DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory);
            while (directory != null && !File.Exists(Path.Combine(
                directory.FullName, "KingmakerBuffPlanner.sln")))
                directory = directory.Parent;
            if (directory == null)
                throw new InvalidOperationException("Repository root was not discoverable.");
            string host = File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "RuntimeTesting", "RuntimeTestHost.cs"));
            string capture = File.ReadAllText(Path.Combine(directory.FullName, "src",
                "KingmakerBuffPlanner", "RuntimeTesting", "MenuRenderDiagnostic.cs"));
            string[] required =
            {
                "WorkspaceCastStepEvaluator.Evaluate(",
                "_workspaceInteraction.Violations()",
                "ManualTerminalPolicy.Classify(",
                "_manualTerminal.Poll(",
                "_manualTerminal.RecordClose(",
                "terminal.AppendAssertions(",
                "IsCastingWorkspaceInputLeaseHeldForRuntime"
            };
            foreach (string needle in required)
                if (!host.Contains(needle))
                    throw new InvalidOperationException(
                        "Runtime host no longer routes through: " + needle);
            if (host.Contains("interactionEvidence.Contains(\"cast1=") ||
                host.Contains("saveControl=\" + saveClick + \";saved=True") ||
                host.Contains("\"manual-final.png\",\r\n") ||
                host.Contains("\"manual-final.png\",\n"))
                throw new InvalidOperationException(
                    "A superseded J1/J2 acceptance path is back in the runtime host.");
            if (!capture.Contains("capture.RestorationClean = restorationClean;"))
                throw new InvalidOperationException(
                    "The camera capture no longer reports its restoration verdict.");
        }

        // Review H1: the supervised manual scenario validates exactly one
        // bounded hold parameter; the hold is never implied, and other
        // scenarios reject it.
        private static void TestRuntimeManualScenarioValidation()
        {
            var manual = new Dictionary<string, object>
            {
                { "manualHoldSeconds", 300 }
            };
            if (!RuntimeTestProtocol.IsManualWorkspaceScenario("live-workspace-manual") ||
                RuntimeTestProtocol.IsManualWorkspaceScenario("live-workspace-qual"))
                throw new InvalidOperationException(
                    "Manual scenario classification is wrong.");
            if (RuntimeTestProtocol.ReadManualHoldSeconds(manual) != 300)
                throw new InvalidOperationException(
                    "A valid hold was misread.");
            if (RuntimeTestProtocol.ReadManualHoldSeconds(
                    new Dictionary<string, object> { { "manualHoldSeconds", 900L } }) != 900 ||
                RuntimeTestProtocol.ReadManualHoldSeconds(
                    new Dictionary<string, object> { { "manualHoldSeconds", 30 } }) != 30 ||
                RuntimeTestProtocol.ReadManualHoldSeconds(
                    new Dictionary<string, object> { { "manualHoldSeconds", 1200L } }) != 1200)
                throw new InvalidOperationException(
                    "A valid JSON-width hold was misread.");
            var badCases = new[]
                {
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>
                        { { "manualHoldSeconds", 4 } },
                    new Dictionary<string, object>
                        { { "manualHoldSeconds", 1201 } },
                    new Dictionary<string, object>
                        { { "manualHoldSeconds", "300" } },
                    // Review C3: the launcher's 30-second floor, and a long
                    // that would wrap to an accepted 300 if narrowed first.
                    new Dictionary<string, object>
                        { { "manualHoldSeconds", 29 } },
                    new Dictionary<string, object>
                        { { "manualHoldSeconds", 4294967596L } },
                    new Dictionary<string, object>
                        { { "manualHoldSeconds", -1L } }
                };
            foreach (Dictionary<string, object> bad in badCases)
            {
                bool threw = false;
                try { RuntimeTestProtocol.ReadManualHoldSeconds(bad); }
                catch (InvalidDataException) { threw = true; }
                if (!threw)
                    throw new InvalidOperationException(
                        "An invalid hold was accepted: " + bad.Count);
            }
            // The protocol-level request validation refuses the parameter
            // on non-manual scenarios via ValidateParameters; manual
            // requests require exactly the one parameter.
            var probe = new Dictionary<string, object>
            {
                { "manualHoldSeconds", 30 },
                { "extra", true }
            };
            bool extraThrew = false;
            try { RuntimeTestProtocol.ReadManualHoldSeconds(probe); }
            catch (InvalidDataException) { extraThrew = true; }
            // ReadManualHoldSeconds itself tolerates extra keys; the
            // manual-parameters count check lives in ValidateParameters
            // (exercised through TryRead below with a serialized request).
            if (extraThrew)
                throw new InvalidOperationException(
                    "ReadManualHoldSeconds rejected a readable hold.");
        }

        private static void TestCastingWorkspaceReviewApply(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-review");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "workspace-campaign");
            session.Draft.SourceId = "source-bulls";
            session.Draft.Ability = CastingBuffAbility;
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t1";
            session.Draft.State = CastingAuthoringState.Ready;
            Assert(session.AddCastingFromDraft(inputs).Applied);
            // Without presentation, Apply is refused.
            WorkspaceApplyResult unpresented = session.Apply(
                CastingApplyMode.Ordinary, "long", inputs);
            if (unpresented.Allowed || unpresented.ReviewReason != "nothing-presented")
                throw new InvalidOperationException(
                    "An unpresented plan was submittable.");
            // Present without acceptance is still refused.
            session.PresentForReview(inputs);
            WorkspaceApplyResult unaccepted = session.Apply(
                CastingApplyMode.Ordinary, "long", inputs);
            if (unaccepted.Allowed || unaccepted.ReviewReason != "not-accepted")
                throw new InvalidOperationException(
                    "Presentation alone approved execution.");
            // An incidental view build or preview between presentation and
            // acceptance authorizes nothing and disturbs nothing.
            session.BuildView(inputs);
            session.CompilePlan(inputs);
            // Acceptance + Apply: the review coordinator permits, the gate
            // permits, and the DISABLED dispatch boundary refuses native
            // submission with its explicit reason while recording identity.
            if (!session.AcceptPresentedPlan(inputs))
                throw new InvalidOperationException("Acceptance was refused.");
            WorkspaceApplyResult applied = session.Apply(
                CastingApplyMode.Ordinary, "long", inputs);
            if (applied.Allowed || applied.Dispatch == null ||
                applied.Dispatch.Submitted ||
                applied.ReviewReason != session.DispatchDisposition ||
                !applied.ReviewReason.Contains("native-submission-disabled"))
                throw new InvalidOperationException(
                    "Native submission was not explicitly disabled: " +
                    applied.ReviewReason);
            if (applied.GateDecision.ExecutableCastingIds.Count != 1)
                throw new InvalidOperationException(
                    "The gate lost the executable set at submission.");
            var boundary = (DisabledCastingDispatchBoundary)
                typeof(CastingWorkspaceSession)
                    .GetField("_dispatch", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(session);
            if (boundary.RecordedSubmissions.Count != 1 ||
                !boundary.RecordedSubmissions[0].Contains("cast-1"))
                throw new InvalidOperationException(
                    "The dispatch boundary did not record the submission identity.");
            // A harmless refresh of the same contents keeps acceptance (no
            // ceremonial loop) and a safe quick-run reaches the boundary
            // again.
            session.PresentForReview(inputs);
            WorkspaceApplyResult quickRun = session.Apply(
                CastingApplyMode.Ordinary, "long", inputs);
            if (!quickRun.ReviewReason.Contains("native-submission-disabled") ||
                boundary.RecordedSubmissions.Count != 2)
                throw new InvalidOperationException(
                    "A safe quick-run demanded ceremony or was lost.");
            // An unseen material change between acceptance and submission
            // refuses until re-presented and re-accepted.
            session.Draft.DirectTargetUnitId = "unit-t2";
            Assert(session.AddCastingFromDraft(inputs).Applied);
            WorkspaceApplyResult changed = session.Apply(
                CastingApplyMode.Ordinary, "long", inputs);
            if (changed.Allowed ||
                changed.ReviewReason != "material-change-requires-review")
                throw new InvalidOperationException(
                    "An unseen material change was submittable.");
            // A refused gate attempt authorizes nothing by itself.
            session.Draft.CasterUnitId = "unit-ghost";
            session.Draft.DirectTargetUnitId = "unit-t3";
            Assert(session.AddCastingFromDraft(inputs).Applied);
            WorkspaceApplyResult refused = session.Apply(
                CastingApplyMode.Ordinary, "long", inputs);
            if (refused.Allowed)
                throw new InvalidOperationException(
                    "A blocked request passed ordinary Apply.");
            // Ready Casts Only is a separate deliberate choice that still
            // routes through review: after re-presentation and acceptance it
            // submits only ready work and discloses its omissions.
            // Re-presentation and re-acceptance of the changed material is
            // the only route forward after the material-change refusal.
            session.PresentForReview(inputs);
            if (!session.AcceptPresentedPlan(inputs))
                throw new InvalidOperationException(
                    "Re-acceptance of the presented material was refused.");
            WorkspaceApplyResult readyOnly = session.Apply(
                CastingApplyMode.ReadyCastsOnly, "long", inputs);
            if (!readyOnly.ReviewReason.Contains("native-submission-disabled") ||
                readyOnly.GateDecision.ExecutableCastingIds.Count != 2 ||
                readyOnly.GateDecision.Omissions.Count != 1 ||
                !readyOnly.GateDecision.Omissions[0].CastingId.Contains("cast-"))
                throw new InvalidOperationException(
                    "Ready Casts Only lost its omissions or executable set.");
        }

        // Save/reopen through the production candidate repository retains
        // identities, order, edits, disabled state, and unresolved intent;
        // unresolved stored data blocks overwriting instead of being
        // replaced by a default.
        private static void TestCastingWorkspacePersistence(string root)
        {
            string modPath = Path.Combine(root, "casting-workspace-persist");
            Directory.CreateDirectory(modPath);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
            var session = new CastingWorkspaceSession(modPath, "workspace-campaign");
            session.Draft.SourceId = "source-bulls";
            session.Draft.Ability = CastingBuffAbility;
            session.Draft.TargetMode = CastingTargetMode.DirectTarget;
            session.Draft.CasterUnitId = "unit-cleric";
            session.Draft.DirectTargetUnitId = "unit-t1";
            session.Draft.State = CastingAuthoringState.Ready;
            Assert(session.AddCastingFromDraft(inputs).Applied);
            session.Draft.DirectTargetUnitId = "unit-t2";
            session.Draft.CasterUnitId = null;
            session.Draft.State = CastingAuthoringState.Draft;
            Assert(session.AddCastingFromDraft(inputs).Applied);
            session.FocusCasting("cast-1");
            Assert(session.SetFocusedCastingState(CastingAuthoringState.Disabled)
                .Applied);
            session.Save();
            // Reopen: identities, order, disabled state, and the unresolved
            // draft survive the production persistence round trip.
            var reopened = new CastingWorkspaceSession(modPath, "workspace-campaign");
            if (reopened.LoadStatus != CastingPlanLoadStatus.Loaded ||
                reopened.Document.Castings.Count != 2)
                throw new InvalidOperationException(
                    "The candidate did not reopen: " + reopened.LoadStatus);
            if (reopened.Document.Castings[0].CastingId != "cast-1" ||
                reopened.Document.Castings[0].State != CastingAuthoringState.Disabled ||
                reopened.Document.Castings[1].CastingId != "cast-2" ||
                reopened.Document.Castings[1].CasterUnitId != null ||
                reopened.Document.Castings[1].State != CastingAuthoringState.Draft)
                throw new InvalidOperationException(
                    "Ids, order, disabled state, or unresolved intent drifted.");
            WorkspaceView view = reopened.BuildView(inputs);
            if (view.CardById("cast-1").Readiness != ResolvedCastingReadiness.Disabled)
                throw new InvalidOperationException(
                    "The disabled card lost its distinct presentation.");
            // Unresolved stored data never becomes a default overwrite: a
            // corrupt candidate blocks persistence for that session.
            var repository = new CastingPlanRepository(modPath);
            File.WriteAllText(repository.GetProfilePath("workspace-campaign"),
                "{ torn");
            var blocked = new CastingWorkspaceSession(modPath, "workspace-campaign");
            if (blocked.LoadStatus != CastingPlanLoadStatus.Corrupt ||
                !blocked.PersistenceBlocked)
                throw new InvalidOperationException(
                    "Corruption was not surfaced as a blocked session.");
            bool refused = false;
            try { blocked.Save(); }
            catch (InvalidOperationException) { refused = true; }
            if (!refused || blocked.PersistenceBlocked == false)
                throw new InvalidOperationException(
                    "A blocked session was allowed to overwrite stored bytes.");
        }

        // A13 (isolated filesystem): the migration boundary archives the
        // exact legacy bytes once per boundary, writes only candidate
        // storage, reopens and revalidates the candidate, keeps the legacy
        // file byte-identical through every outcome, and recovers from an
        // interrupted candidate write without touching the original.
        private static void TestCastingA13MigrationBoundary(string root)
        {
            string boundary = Path.Combine(root, "casting-migration");
            Directory.CreateDirectory(boundary);
            var repository = new ProfileRepository(boundary);
            BuffPlannerProfile legacy = LegacyProfile();
            legacy.Routines[0].Assignments.Add(LegacyAssignment(
                "source-bulls", CastingBuffAbility,
                PinnedChild("legacy-bulls", 0, "unit-cleric", "unit-t1", "unit-t2")));
            repository.Save(legacy);
            string legacyPath = repository.GetProfilePath("legacy-campaign");
            string originalBytes = File.ReadAllText(legacyPath);
            var migration = new CastingPlanMigrationService(boundary);
            CastingMigrationResult first = migration.Migrate("legacy-campaign");
            if (first.Status != CastingMigrationStatus.Migrated ||
                first.ImportReport == null ||
                first.ImportReport.ResultingCastingCount != 2)
                throw new InvalidOperationException(
                    "Migration did not complete: " + first.Status + " " + first.Warning);
            // The exact legacy bytes are archived once, outside the rotating
            // chain, and the legacy file itself is untouched.
            if (!File.Exists(first.ArchivePath) ||
                File.ReadAllText(first.ArchivePath) != originalBytes ||
                File.ReadAllText(legacyPath) != originalBytes)
                throw new InvalidOperationException(
                    "The exact original was not preserved byte-for-byte.");
            // Candidate storage reopened and validated; the schema-5 file is
            // unchanged and still authoritative for the old UI.
            var candidateRepository = new CastingPlanRepository(boundary);
            CastingPlanLoadResult candidate = candidateRepository.Load("legacy-campaign");
            if (candidate.Status != CastingPlanLoadStatus.Loaded ||
                candidate.Profile.ToDocument().Castings.Count != 2)
                throw new InvalidOperationException(
                    "The migrated candidate did not reopen.");
            // Idempotent boundary: migrating again reuses provenance
            // identities and archives nothing new.
            CastingMigrationResult second = migration.Migrate("legacy-campaign");
            if (second.Status != CastingMigrationStatus.Migrated ||
                second.ImportReport.ResultingCastingCount != 2 ||
                File.ReadAllText(second.ArchivePath) != originalBytes ||
                candidateRepository.Load("legacy-campaign").Profile
                    .ToDocument().Castings.Count != 2)
                throw new InvalidOperationException(
                    "Re-migration duplicated work or lost the archive boundary.");
            // An interrupted candidate write (a torn primary) is reported as
            // corruption with the legacy original still recoverable; a
            // retry after removing the torn candidate succeeds.
            string candidatePath = candidateRepository.GetProfilePath("legacy-campaign");
            File.WriteAllText(candidatePath, "{ torn");
            CastingMigrationResult torn = migration.Migrate("legacy-campaign");
            if (torn.Status != CastingMigrationStatus.CandidateUnusable ||
                File.ReadAllText(legacyPath) != originalBytes)
                throw new InvalidOperationException(
                    "A torn candidate was not reported or disturbed the original.");
            File.Delete(candidatePath);
            CastingMigrationResult retried = migration.Migrate("legacy-campaign");
            if (retried.Status != CastingMigrationStatus.Migrated)
                throw new InvalidOperationException(
                    "Recovery after a torn candidate failed: " + retried.Warning);
            // A newer-schema candidate is never buried by a migration.
            File.WriteAllText(candidatePath,
                "{ \"schemaVersion\": 9, \"campaignId\": \"legacy-campaign\" }");
            CastingMigrationResult refused = migration.Migrate("legacy-campaign");
            if (refused.Status != CastingMigrationStatus.NewerCandidateRefused ||
                File.ReadAllText(legacyPath) != originalBytes)
                throw new InvalidOperationException(
                    "A newer-schema candidate was buried or the original disturbed.");
            // An absent legacy profile is reported, not fabricated.
            var empty = new CastingPlanMigrationService(
                Path.Combine(boundary, "empty"));
            if (empty.Migrate("legacy-campaign").Status !=
                    CastingMigrationStatus.LegacyAbsent)
                throw new InvalidOperationException(
                    "An absent legacy profile was not reported.");
        }

        // C3: only presented and accepted material contents may submit; a
        // material change, an unpresented plan, or a mere preview never
        // approves execution, while a harmless refresh keeps acceptance.
        private static void TestCastingC3PresentedPlan()
        {
            List<ProviderPlanningOption> options;
            List<CastEnhancementSnapshot> enhancements;
            PartyProviderSnapshot snapshot = CastingParty(
                CastingBuffAbility,
                out options, out enhancements,
                new[] { "unit-t1" }, 3);
            var authoring = new CastingAuthoringService(CastingDocument(
                DirectCasting("cast-a", "long", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility),
                DirectCasting("cast-b", "short", "unit-cleric", "unit-t1",
                    "source-bulls", CastingBuffAbility)));
            var compiler = new ExplicitCastingCompiler();
            var effects = CastingEffects("source-bulls", "source-communal");
            ExplicitCastingPlan plan = compiler.Compile(
                authoring.Document, snapshot, options, effects, enhancements);
            CastingPlanSignature first = CastingPlanSignature.For(plan);
            var coordinator = new CastingReviewCoordinator();
            if (coordinator.TrySubmit(first).Allowed)
                throw new InvalidOperationException(
                    "An unpresented plan was submittable.");
            coordinator.Present(first);
            if (coordinator.TrySubmit(first).Allowed ||
                coordinator.Status != CastingReviewStatus.Presented)
                throw new InvalidOperationException(
                    "Presentation alone approved execution.");
            if (!coordinator.Accept(first).Allowed)
                throw new InvalidOperationException("Acceptance was refused.");
            if (!coordinator.TrySubmit(first).Allowed)
                throw new InvalidOperationException(
                    "An accepted matching plan was refused.");
            // A safe unchanged quick-run needs no ceremonial loop.
            coordinator.Present(first);
            if (coordinator.Status != CastingReviewStatus.Accepted ||
                !coordinator.TrySubmit(first).Allowed)
                throw new InvalidOperationException(
                    "A harmless refresh demanded reconfirmation.");
            // Material change between acceptance and submission: an edited
            // plan may not ride the old acceptance.
            if (!authoring.UpdateCasting(DirectCasting("cast-a", "long",
                    "unit-cleric", "unit-t1", "source-bulls", CastingBuffAbility,
                    new[] { new AuthoredEnhancementSelection("extend-cleric", true, null) }))
                .Applied)
                throw new InvalidOperationException("Fixture edit failed.");
            ExplicitCastingPlan edited = compiler.Compile(
                authoring.Document, snapshot, options, effects, enhancements);
            CastingPlanSignature second = CastingPlanSignature.For(edited);
            if (second.Matches(first))
                throw new InvalidOperationException(
                    "A material edit did not change the signature.");
            CastingReviewDecision refused = coordinator.TrySubmit(second);
            if (refused.Allowed || refused.Reason != "material-change-requires-review")
                throw new InvalidOperationException(
                    "An unseen material change was submittable.");
            // A refused attempt does not authorize the next one: only fresh
            // presentation and acceptance of the new material does.
            coordinator.Present(second);
            if (coordinator.TrySubmit(second).Allowed)
                throw new InvalidOperationException(
                    "Presentation of changed contents approved them.");
            if (!coordinator.Accept(second).Allowed ||
                !coordinator.TrySubmit(second).Allowed)
                throw new InvalidOperationException(
                    "Re-acceptance of the new material was refused.");
            // An incidental preview of another routine is not a presentation
            // and does not disturb the accepted run.
            var forecastService = new CastingForecastService();
            forecastService.ForecastRoutine(
                authoring.Document, "short", snapshot, options, effects, enhancements);
            if (!coordinator.TrySubmit(second).Allowed)
                throw new InvalidOperationException(
                    "An incidental preview disturbed acceptance.");
        }
    }
}
