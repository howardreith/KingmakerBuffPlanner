using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Compatibility;
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
                Run("cast-enhancement-selection-is-assignment-scoped", TestCastEnhancementSelection);
                Run("casting-section-presents-caster-and-enhancement-choices", TestCastingSectionPresentation);
                Run("casting-section-layout-keeps-button-labels-visible", TestCastingSectionLayout);
                Run("cast-enhancement-execution-is-fail-closed-and-cleaned-up", TestCastEnhancementExecution);
                Run("consumed-one-shot-enhancement-is-not-rearmed", TestOneShotEnhancementRestoration);
                Run("personal-target-eligibility-is-provider-relative", TestPersonalTargetEligibility);
                Run("area-coverage-preview-distinguishes-direct-and-indirect", TestAreaCoveragePresentation);
                Run("per-anchor-mass-coverage-avoids-duplicate-communal-casts",
                    TestPerAnchorMassCoverage);
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

            profile.Routines[0].Assignments.Add(new SourceAssignmentProfile
            {
                SourceId = "variant|parent-guid|ungranted-guid",
                Ability = AbilityKeyProfile.FromKey(Ability(
                    "parent-guid", "ungranted-guid", 0)),
                WantedTargetUnitIds = new List<string> { "unit-owner" },
                ExistingEffectPolicy =
                    ExistingEffectPolicy.SkipAlreadyActive,
                IgnoredPresenceMarkers = new List<string>(),
                SelectedEnhancementIds = new List<string>()
            });
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
            profile.Routines[0].Assignments.Add(new SourceAssignmentProfile
            {
                SourceId = sourceId,
                Ability = AbilityKeyProfile.FromKey(child),
                WantedTargetUnitIds = new List<string> { "target-a" },
                ExistingEffectPolicy = ExistingEffectPolicy.Overwrite,
                IgnoredPresenceMarkers = new List<string>(),
                SelectedEnhancementIds = new List<string>()
            });
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
            profile.Routines[0].Assignments.Add(new SourceAssignmentProfile
            {
                SourceId = parent.Canonical,
                Ability = AbilityKeyProfile.FromKey(parent),
                WantedTargetUnitIds = new List<string> { "target-a" },
                ExistingEffectPolicy = ExistingEffectPolicy.Overwrite,
                IgnoredPresenceMarkers = new List<string>(),
                SelectedEnhancementIds = new List<string>()
            });
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
            return new SourceAssignmentProfile
            {
                SourceId = ability.Canonical,
                Ability = AbilityKeyProfile.FromKey(ability),
                WantedTargetUnitIds = new List<string> { target },
                ExistingEffectPolicy = ExistingEffectPolicy.SkipAlreadyActive,
                IgnoredPresenceMarkers = new List<string>()
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
            profile.Routines[0].Assignments.Add(new SourceAssignmentProfile
            {
                SourceId = supported.Canonical,
                Ability = AbilityKeyProfile.FromKey(supported),
                WantedTargetUnitIds = new List<string> { "unit-a" },
                ExistingEffectPolicy = ExistingEffectPolicy.Overwrite,
                IgnoredPresenceMarkers = new List<string>()
            });
            profile.Routines[0].Assignments.Add(new SourceAssignmentProfile
            {
                SourceId = unsupported.Canonical,
                Ability = AbilityKeyProfile.FromKey(unsupported),
                WantedTargetUnitIds = new List<string> { "unit-a" },
                ExistingEffectPolicy = ExistingEffectPolicy.Overwrite,
                IgnoredPresenceMarkers = new List<string>()
            });
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

        private static void TestProfileMigration(string root)
        {
            string modPath = Path.Combine(root, "profile-migration");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            JObject document = JObject.FromObject(ProfileFixture("campaign:migration"));
            document["schemaVersion"] = 1;
            document.Remove("ui");
            document.Remove("execution");
            string path = repository.GetProfilePath("campaign:migration");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, document.ToString());
            ProfileLoadResult migrated = repository.Load("campaign:migration");
            if (!migrated.Migrated || migrated.Profile.SchemaVersion != 4 ||
                migrated.Profile.Ui.Scale != 1.0f || migrated.Profile.Execution.Mode != "animated" ||
                migrated.Profile.Ui.Hotkey != "Ctrl+Shift+B")
                throw new InvalidOperationException("Schema-one profile was not migrated with safe defaults.");
        }

        private static void TestGridProfileMigration(string root)
        {
            string modPath = Path.Combine(root, "profile-grid-migration");
            Directory.CreateDirectory(modPath);
            var repository = new ProfileRepository(modPath);
            JObject document = JObject.FromObject(ProfileFixture("campaign:grid-migration"));
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
            if (!migrated.Migrated || migrated.Profile.SchemaVersion != 4 ||
                migrated.Profile.Ui.Hotkey != "Ctrl+Shift+B" ||
                migrated.Profile.HiddenSourceIds.Count != 0 || migrated.Profile.Execution.RecastExisting)
                throw new InvalidOperationException("Grid UI migration did not reveal hidden entries or replace F10.");
            if (migrated.Profile.Routines[0].Assignments.Count != 1 ||
                migrated.Profile.Routines[0].Assignments[0].WantedTargetUnitIds.Count != 2 ||
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
                "\"schemaVersion\": 4,", "\"schemaVersion\": 4,\r\n  \"schemaVersion\": 4,");
            File.WriteAllText(duplicatePath, duplicateJson);
            ProfileLoadResult rejected = repository.Load("campaign:duplicate");
            if (!rejected.Warning.Contains("duplicate-property"))
                throw new InvalidOperationException("Duplicate JSON property was not rejected.");
        }

        private static BuffPlannerProfile ProfileFixture(string campaignId)
        {
            BuffPlannerProfile profile = BuffPlannerProfile.CreateDefault(campaignId);
            profile.Routines[0].Assignments.Add(new SourceAssignmentProfile
            {
                SourceId = "source-persisted",
                Ability = AbilityKeyProfile.FromKey(Ability("persisted", "variant", 8)),
                WantedTargetUnitIds = new List<string> { "unit-z", "unit-a" },
                ExistingEffectPolicy = ExistingEffectPolicy.SkipAlreadyActive,
                IgnoredPresenceMarkers = new List<string> { "shared-marker" },
                SelectedEnhancementIds = new List<string> { "metamagic-rod|unit-a|persisted" }
            });
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
            if (loaded.Profile.SchemaVersion != 4 ||
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

        private static void TestPerAnchorMassCoverage()
        {
            AssertPerAnchorMassCoverage("protection-from-arrows-communal-fixture");
            AssertPerAnchorMassCoverage("good-hope-fixture");
            AssertPerAnchorMassCoverage("existing-communal-positive-control");
        }

        private static void AssertPerAnchorMassCoverage(string sourceId)
        {
            AbilityKey ability = Ability(sourceId, string.Empty, 0);
            var pool = new ResourcePoolSnapshot(sourceId + "-pool",
                ResourcePoolKind.SpontaneousLevel, 3, 3, null);
            ProviderSnapshot provider = PlannerProvider("caster", "communal-book",
                ability, pool.PoolKey, 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider },
                new[] { pool }, "caster", "anchor", "ally", "pet");
            string[] recipients = { "caster", "anchor", "ally", "pet" };
            var coverage = new Dictionary<string, IEnumerable<string>>
            {
                { "anchor", recipients }
            };
            var option = new ProviderPlanningOption(provider, recipients,
                new[] { "anchor" }, 5, 50, false, coverage);
            var effect = new EffectLeafExpression(EffectKind.AreaBuff,
                sourceId + "-effect", EffectTarget.AlliedAreaRecipients,
                "AbilityTargetsAround+friend-only", "root/area");
            var source = new BuffSourceDefinition(sourceId, ability, effect,
                CastGroupingKind.MassConfiguredTargets);
            var planner = new CastPlanner();
            CastPlan persistedAnchor = planner.Plan(snapshot, new BuffCastRequest(source,
                new[] { "anchor" }, ExistingEffectPolicy.Overwrite, null),
                new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (persistedAnchor.Steps.Count != 1 ||
                persistedAnchor.Steps.Single().AnchorUnitId != "anchor" ||
                !persistedAnchor.Steps.Single().TargetUnitIds.SequenceEqual(
                    new[] { "anchor" }) ||
                !persistedAnchor.Steps.Single().ExpectedRecipientUnitIds.SequenceEqual(
                    recipients.OrderBy(value => value, StringComparer.Ordinal)))
                throw new InvalidOperationException(
                    "A single selected communal anchor did not produce complete indirect recipient coverage: " + sourceId);

            CastPlan explicitEveryRecipient = planner.Plan(snapshot,
                new BuffCastRequest(source, recipients,
                    ExistingEffectPolicy.Overwrite, null), new[] { option },
                EmptyPolicy(), new ActiveEffectSnapshot(null));
            if (explicitEveryRecipient.Steps.Count != 1 ||
                explicitEveryRecipient.Steps.Single().Reservation.Units != 1 ||
                explicitEveryRecipient.Outcomes.Count(outcome =>
                    outcome.Kind == TargetOutcomeKind.Fulfilled) != recipients.Length)
                throw new InvalidOperationException(
                    "Mass coverage scheduled one cast per teammate instead of one structural cast: " + sourceId);

            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault(
                "coverage:" + sourceId), snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, effect } },
                new[] { option }, ignored => { });
            model.ToggleTarget("short", "anchor");
            RoutinePlanResult preview = new RoutinePlanService().Plan(model.Profile,
                "short", snapshot, new ActiveEffectSnapshot(null),
                new Dictionary<string, EffectExpression> { { ability.Canonical, effect } },
                new[] { option });
            TargetPortraitViewModel anchor = TargetPortraitViewModel.Create(
                model.Sources.Single(), model, "short", snapshot.Units.Single(unit =>
                    unit.UnitId == "anchor"), preview);
            TargetPortraitViewModel teammate = TargetPortraitViewModel.Create(
                model.Sources.Single(), model, "short", snapshot.Units.Single(unit =>
                    unit.UnitId == "ally"), preview);
            if (anchor.State != TargetPortraitState.DirectSelectedAndCovered ||
                teammate.State != TargetPortraitState.IndirectlyCovered ||
                teammate.Wanted || !teammate.Indirect)
                throw new InvalidOperationException(
                    "Anchor/direct and teammate/indirect presentation diverged from the structural plan: " + sourceId);
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
                new SourceAssignmentProfile
                {
                    SourceId = modelA.SelectedSource.SourceId,
                    Ability = AbilityKeyProfile.FromKey(personalAbility),
                    WantedTargetUnitIds = new List<string> { "unit-b" },
                    ExistingEffectPolicy = ExistingEffectPolicy.Overwrite,
                    IgnoredPresenceMarkers = new List<string>()
                });
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
            internal FakeRoutineRunner(QuickExecutionResult result) { _result = result; }
            public bool TryStart(string routineId, Action<QuickExecutionResult> completed)
            {
                StartCount++;
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
            public string Detail { get { return "command-success"; } }
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
        }

        private sealed class AlwaysAnimatedRuntime : ICastRuntimeAdapter
        {
            internal int StartCount;
            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
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
