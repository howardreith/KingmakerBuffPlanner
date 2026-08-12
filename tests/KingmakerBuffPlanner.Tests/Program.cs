using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.Discovery;
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

        private static int Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveInstalledAssembly;
            string root = Path.Combine(
                RuntimeTestProtocol.EvidenceRoot,
                "source-only-protocol-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Run("absent-activation-is-inert", TestAbsentActivation);
                Run("valid-request-is-accepted", () => TestValidRequest(root));
                Run("valid-catalog-request-is-accepted", () => TestValidCatalogRequest(root));
                Run("valid-call-of-the-wild-request-is-accepted", () => TestValidCallOfTheWildRequest(root));
                Run("valid-human-reproduction-request-is-accepted", () => TestValidHumanReproductionRequest(root));
                Run("valid-ui-request-is-accepted", () => TestValidUiRequest(root));
                Run("valid-live-ui-request-is-accepted", () => TestValidLiveUiRequest(root));
                Run("valid-native-ui-probe-request-is-accepted", () => TestValidNativeUiProbeRequest(root));
                Run("valid-final-core-request-is-accepted", () => TestValidFinalCoreRequest(root));
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
                Run("native-candidate-classification-is-structural", TestNativeCandidateClassification);
                Run("optional-blueprint-ownership-is-exact", TestBlueprintOwnership);
                Run("harmony-target-identities-are-stable", TestHarmonyTargetIdentity);
                Run("installed-harmony-inventory-api-is-callable", TestHarmonyInventoryApi);
                Run("effect-overrides-are-versioned-and-branch-preserving", TestEffectOverrides);
                Run("stable-keys-distinguish-variants-and-metamagic", TestStableKeys);
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
                Run("routine-service-reports-unsupported-sources", TestRoutineServiceUnsupportedSources);
                Run("profile-round-trip-preserves-stable-ids", () => TestProfileRoundTrip(root));
                Run("profile-recovers-valid-bounded-backup", () => TestProfileBackupRecovery(root));
                Run("profile-migrates-schema-one", () => TestProfileMigration(root));
                Run("profile-migrates-hidden-and-f10-state", () => TestGridProfileMigration(root));
                Run("profile-malformed-json-recovers-default", () => TestProfileMalformed(root));
                Run("setup-model-direct-targets-are-routine-local", TestSetupModel);
                Run("catalog-filter-selected-category-and-reset-contract", TestCatalogFilterState);
                Run("presentation-view-models-use-player-facing-deterministic-state", TestPresentationModels);
                Run("four-column-grid-metrics-have-no-horizontal-scroll", TestGridMetrics);
                Run("planner-hotkey-chord-consumes-native-primary-key", TestPlannerHotkeyBinding);
                Run("input-lease-restores-on-close-and-acquire-failure", TestInputLease);
                Run("screen-state-machine-is-idempotent", TestScreenStateMachine);
                Run("ui-readiness-is-deferred-across-frames", TestDeferredUiReadiness);
                Run("quick-execution-instruments-and-presents-empty-group", TestQuickExecutionFlow);
                Run("animated-executor-validates-before-queue-and-reports", TestAnimatedExecutor);
                Run("instant-executor-revalidates-batches-and-reports", TestInstantExecutor);
                Run("submitted-without-effect-is-not-success", TestUnconfirmedExecution);
                Run("hybrid-executor-routes-and-blocks-fallbacks", TestHybridExecutor);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
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
            if (hostile.Disposition != "exclude" || !hostile.Reason.StartsWith("hostile-only:", StringComparison.Ordinal))
                throw new InvalidOperationException("Hostile current-target effect was mistaken for a self buff.");

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

        private static NativeCandidateEffectFacts CandidateEffect(
            string kind, string target, bool? harmful, string source, string path)
        {
            return new NativeCandidateEffectFacts
            {
                Kind = kind,
                Target = target,
                Harmful = harmful,
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
                new EffectLeafExpression(EffectKind.AreaBuff, "required", EffectTarget.AreaRecipients,
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
                assignment.Ability.ToKey().Canonical != Ability("persisted", "variant", 8).Canonical)
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
            if (!migrated.Migrated || migrated.Profile.SchemaVersion != 3 ||
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
            string path = repository.GetProfilePath("campaign:grid-migration");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, document.ToString());
            ProfileLoadResult migrated = repository.Load("campaign:grid-migration");
            if (!migrated.Migrated || migrated.Profile.SchemaVersion != 3 ||
                migrated.Profile.Ui.Hotkey != "Ctrl+Shift+B" ||
                migrated.Profile.HiddenSourceIds.Count != 0 || migrated.Profile.Execution.RecastExisting)
                throw new InvalidOperationException("Grid UI migration did not reveal hidden entries or replace F10.");
            if (migrated.Profile.Routines[0].Assignments.Count != 1 ||
                migrated.Profile.Routines[0].Assignments[0].WantedTargetUnitIds.Count != 2)
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
                "\"schemaVersion\": 3,", "\"schemaVersion\": 3,\r\n  \"schemaVersion\": 3,");
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
                IgnoredPresenceMarkers = new List<string> { "shared-marker" }
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
            model.CycleProviderPreference(provider.Key.Canonical);
            if (model.GetProviderPreference(provider.Key.Canonical).Priority != 0)
                throw new InvalidOperationException("Automatic provider did not enter explicit-priority state.");
            model.CycleProviderPreference(provider.Key.Canonical);
            if (!model.GetProviderPreference(provider.Key.Canonical).Banned)
                throw new InvalidOperationException("Provider preference did not enter banned state.");
            model.CycleProviderPreference(provider.Key.Canonical);
            if (model.GetProviderPreference(provider.Key.Canonical) != null)
                throw new InvalidOperationException("Provider preference did not reset to automatic.");
            model.AdjustProviderCap(provider.Key.Canonical, 1);
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
            TargetPortraitViewModel target = TargetPortraitViewModel.Create(
                model.Sources[0], model, "long", unit);
            TargetPortraitViewModel invalidTarget = TargetPortraitViewModel.Create(
                model.Sources[0], model, "long", invalidUnit);
            var warningTarget = new TargetPortraitViewModel(unit, true, true, false, false);
            var routine = new RoutineSummaryViewModel("long", "Long", 1, 1);
            var settings = new PlannerSettingsViewModel(profile);
            if (target.Status != PlannerPresentationStatus.Success ||
                warningTarget.Status != PlannerPresentationStatus.Warning ||
                invalidTarget.Status != PlannerPresentationStatus.Failure ||
                routine.Label != "Long     1/1 ready" || settings.CastingMode != "Animated" ||
                saves != beforePreview)
                throw new InvalidOperationException("Player-facing presentation summaries are invalid.");
            model.SetAllValidTargets("long", false);
            if (model.IsTargetWanted("long", "unit-a") || saves != beforePreview + 1)
                throw new InvalidOperationException("Bulk target edit did not save once.");
        }

        private static void TestGridMetrics()
        {
            BuffGridMetrics fullHd = BuffGridMetrics.Calculate(1740f, 610f);
            BuffGridMetrics compact = BuffGridMetrics.Calculate(1420f, 500f);
            if (fullHd.Columns != 4 || compact.Columns != 4 ||
                fullHd.HorizontalScrolling || compact.HorizontalScrolling ||
                fullHd.CellWidth <= 0 || compact.CellWidth <= 0)
                throw new InvalidOperationException("Grid metrics did not preserve four columns without horizontal scrolling.");
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
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 || request.RunId != "valid")
                throw new InvalidOperationException("Valid request was rejected: " + rejection);
        }

        private static void TestValidCatalogRequest(string root)
        {
            string path = WriteRequest(root, "valid-catalog", o => o["scenario"] = "native-buff-catalog");
            string rejection;
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
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
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
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
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                request.ProfileId != "human-reproduction" || request.ExpectedOptionalMods.Count != 4)
                throw new InvalidOperationException("Valid human reproduction request was rejected: " + rejection);
        }

        private static void TestValidUiRequest(string root)
        {
            string path = WriteRequest(root, "valid-ui", o => o["scenario"] = "ui-root-smoke");
            string rejection;
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
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
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
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
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsNativeUiProbeScenario(request.Scenario))
                throw new InvalidOperationException("Valid native UI probe request was rejected: " + rejection);
        }

        private static void TestValidFinalCoreRequest(string root)
        {
            string path = WriteRequest(root, "valid-final-core", o => o["scenario"] = "final-no-save-core");
            string rejection;
            RuntimeTestRequest request = RuntimeTestProtocol.TryRead(
                new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection);
            if (request == null || rejection.Length != 0 ||
                !RuntimeTestProtocol.IsCatalogScenario(request.Scenario) ||
                RuntimeTestProtocol.IsUiScenario(request.Scenario))
                throw new InvalidOperationException("Valid final core request was rejected: " + rejection);
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
            if (RuntimeTestProtocol.TryRead(args, out rejection) != null || string.IsNullOrWhiteSpace(rejection))
                throw new InvalidOperationException("Invalid request was not rejected.");
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
