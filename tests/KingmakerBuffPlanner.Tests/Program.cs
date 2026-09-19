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
                    "this spell: 3 allocated / " + targetCount + " requested",
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
                    "this assignment: 2 allocated / 2 requested",
                    StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "The assignment-scoped chooser lost its own coverage note: " +
                    choice.BudgetNote);
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
                o["expectedOptionalMods"] = Enumerable.Range(0, 4).Select(index =>
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
                request.ProfileId != "human-reproduction" || request.ExpectedOptionalMods.Count != 4)
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
    }
}
