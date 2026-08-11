using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Planning;
using Newtonsoft.Json;

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
                Run("duplicate-flag-rejected", () => TestDuplicateFlag(root));
                Run("outside-path-rejected", TestOutsidePath);
                Run("unknown-member-rejected", () => TestMutation(root, "unknown-member", AddUnknownMember));
                Run("duplicate-member-rejected", () => TestDuplicateMember(root));
                Run("wrong-scenario-rejected", () => TestMutation(root, "wrong-scenario", o => o["scenario"] = "unknown"));
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
                Run("stable-keys-distinguish-variants-and-metamagic", TestStableKeys);
                Run("spontaneous-providers-share-one-pool", TestSpontaneousSharedPool);
                Run("prepared-opposition-consumes-linked-slots", TestPreparedLinkedSlots);
                Run("prepared-domain-slot-eligibility-is-preserved", TestPreparedDomainEligibility);
                Run("unlimited-pool-is-explicit", TestUnlimitedPool);
                Run("party-snapshot-orders-by-stable-id", TestPartySnapshotOrdering);
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
                { "expectedModVersion", "0.0.1" },
                { "expectedCommit", "TEST-COMMIT" },
                { "evidenceDirectory", root },
                { "expectedPackageSha256", new string('a', 64) },
                { "expectedDllSha256", new string('b', 64) },
                { "timeoutSeconds", 30 },
                { "exitAfterCompletion", false },
                { "parameters", new Dictionary<string, object>() }
            };
        }
    }
}
