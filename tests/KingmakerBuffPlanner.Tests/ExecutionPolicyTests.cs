using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // WP4 (0.4.0): buff routines never run during combat, and Instant is
        // strict - a casting without a qualified instant route is Not Ready
        // in Instant mode and is never silently animated.
        private static void RunExecutionPolicyTests(string root)
        {
            Run("execution-policy-combat-refuses-both-modes-before-anything",
                () => TestCombatRefusal(root));
            Run("execution-policy-strict-instant-blocker-is-casting-specific",
                () => TestStrictInstantBlocker(root));
            Run("execution-policy-legacy-fallback-profile-never-animates",
                () => TestLegacyFallbackProfile(root));
            Run("execution-policy-strict-hybrid-never-starts-a-native-command",
                TestStrictHybridExecutor);
        }

        private sealed class CountingDispatchBoundary : ICastingDispatchBoundary
        {
            internal int Submissions;
            internal ExplicitStepConversion LastProjection;

            public string DispositionReason { get { return "counting-fixture-boundary"; } }

            public CastingDispatchOutcome Submit(ExplicitCastingPlan plan,
                CastingApplyDecision decision, string scopeRoutineId,
                ExplicitStepConversion projection)
            {
                Submissions++;
                LastProjection = projection;
                return new CastingDispatchOutcome(true, "submitted", decision.ExecutableCastingIds);
            }
        }

        private static CastingWorkspaceInputs PolicyInputs(CastingWorkspaceInputs inputs,
            bool combat, string fallbackCaster = null,
            CastExecutionStrategy strategy = CastExecutionStrategy.AnimatedFallback,
            string reason = "sticky-delivery-hostile-targeting-ambiguous")
        {
            List<ProviderPlanningOption> options = inputs.ProviderOptions.Select(option =>
                fallbackCaster != null && option.Provider.Key.CasterUnitId == fallbackCaster
                    ? new ProviderPlanningOption(option.Provider, option.ReachableTargetIds,
                        option.LegalAnchorIds, option.EffectiveCasterLevel,
                        option.ExpectedDurationRounds, strategy, reason)
                    : option).ToList();
            return new CastingWorkspaceInputs(inputs.Snapshot, options, inputs.EffectsBySource,
                inputs.Enhancements, inputs.TargetingModifiers, inputs.LiveEffects, combat);
        }

        private static string PolicyFileHash(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
        }

        private static void TestCombatRefusal(string root)
        {
            foreach (string mode in new[] { "instant", "animated" })
            {
                string dir = Path.Combine(root, "policy-combat-" + mode);
                Directory.CreateDirectory(dir);
                PartyProviderSnapshot snapshot;
                CastingWorkspaceInputs inputs = WorkspaceInputs(out snapshot);
                var dispatch = new CountingDispatchBoundary();
                var session = new CastingWorkspaceSession(dir, "workspace-campaign", dispatch);
                Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
                session.SetExecutionMode(mode);
                session.ClearGraphFocus();
                string profile = new CastingPlanRepository(dir).GetProfilePath("workspace-campaign");
                string bytesBefore = PolicyFileHash(profile);
                string documentBefore = session.DocumentIntentSignature();
                bool undoBefore = session.CanUndo;
                CastingWorkspaceInputs combat = PolicyInputs(inputs, true);
                foreach (CastingApplyMode apply in new[] { CastingApplyMode.Ordinary,
                    CastingApplyMode.ReadyCastsOnly })
                {
                    WorkspaceApplyResult refused = session.Apply(apply, "long", combat);
                    if (refused.Allowed || refused.ReviewReason != CastingExecutionPolicy.CombatActive ||
                        refused.GateDecision != null || refused.Dispatch != null ||
                        refused.Projection != null || refused.BlockingCastings.Count != 0)
                        throw new InvalidOperationException(mode + "/" + apply +
                            ": combat did not refuse globally before planning: " + refused.ReviewReason);
                    if (CastingRunPresentation.DescribeRefusal("Long", refused, session) !=
                            "Buff routines cannot run during combat." ||
                        CastingRunPresentation.OpensPlanner(refused))
                        throw new InvalidOperationException(mode + ": the combat refusal is not the " +
                            "exact global sentence, or it opens the planner.");
                }
                if (dispatch.Submissions != 0 || session.ProblemNavigation.Active ||
                    session.EditingFocusCastingId != null || session.CanUndo != undoBefore ||
                    session.DocumentIntentSignature() != documentBefore ||
                    PolicyFileHash(profile) != bytesBefore)
                    throw new InvalidOperationException(mode + ": the combat refusal submitted, " +
                        "focused a casting, or changed intent/persistence.");
                // Combat over: a new deliberate press runs normally.
                WorkspaceApplyResult after = session.Apply(CastingApplyMode.Ordinary, "long",
                    PolicyInputs(inputs, false));
                if (!after.Allowed || dispatch.Submissions != 1)
                    throw new InvalidOperationException(mode + ": a press after combat did not run: " +
                        after.ReviewReason);
            }
        }

        private static void TestStrictInstantBlocker(string root)
        {
            string dir = Path.Combine(root, "policy-strict-instant");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs plain = WorkspaceInputs(out snapshot);
            CastingWorkspaceInputs inputs = PolicyInputs(plain, false, "unit-wizard");
            var dispatch = new CountingDispatchBoundary();
            var session = new CastingWorkspaceSession(dir, "workspace-campaign", dispatch);
            Assert(AddDraftCasting(session, inputs, "unit-cleric", "unit-t1").Applied);
            Assert(AddDraftCasting(session, inputs, "unit-wizard", "unit-t2").Applied);
            string wizardCasting = session.Document.Castings
                .Single(value => value.CasterUnitId == "unit-wizard").CastingId;
            session.ClearGraphFocus();
            if (session.ExecutionMode != "instant")
                throw new InvalidOperationException("Fixture is not in Instant mode.");
            ResolvedCasting resolved = session.CompileForRuntime(inputs, "long").CastingById(wizardCasting);
            string reason = resolved.ReadinessReasons.FirstOrDefault(value => value.StartsWith(
                CastingExecutionPolicy.InstantRouteUnavailable + ":", StringComparison.Ordinal));
            if (resolved.Readiness != ResolvedCastingReadiness.Blocked || reason !=
                    "instant-route-unavailable:AnimatedFallback:sticky-delivery-hostile-targeting-ambiguous")
                throw new InvalidOperationException("An animated-only casting is not Not Ready in Instant: " +
                    resolved.Readiness + " " + string.Join(",", resolved.ReadinessReasons.ToArray()));
            if (!WorkspaceReasonText.Describe(reason).Contains("cannot be cast instantly"))
                throw new InvalidOperationException("The strict Instant reason is not actionable.");
            // The ordinary run refuses with this casting identified
            // structurally and WP2A navigation focuses it; nothing submits.
            WorkspaceApplyResult refused = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (refused.Allowed || dispatch.Submissions != 0 ||
                !refused.BlockingCastings.Select(value => value.CastingId).SequenceEqual(new[] { wizardCasting }) ||
                !session.ProblemNavigation.Active || session.EditingFocusCastingId != wizardCasting ||
                !CastingRunPresentation.OpensPlanner(refused))
                throw new InvalidOperationException("The strict Instant blocker was not casting specific: " +
                    refused.ReviewReason);
            // Explicit Animated runs the same casting through its normal route.
            session.ClearGraphFocus();
            session.SetExecutionMode("animated");
            WorkspaceApplyResult animated = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (!animated.Allowed || dispatch.Submissions != 1 ||
                dispatch.LastProjection.Plan.Steps.Count != 2 ||
                dispatch.LastProjection.Plan.Steps.Count(step =>
                    step.ExecutionStrategy == CastExecutionStrategy.AnimatedFallback) != 1)
                throw new InvalidOperationException("Animated mode did not run the animated-only casting: " +
                    animated.ReviewReason);
            // A native-command requirement is equally not instant.
            session.SetExecutionMode("instant");
            CastingWorkspaceInputs native = PolicyInputs(plain, false, "unit-wizard",
                CastExecutionStrategy.NativeCommandRequired, "enhancement-native-command-required:quicken");
            if (!session.CompileForRuntime(native, "long").CastingById(wizardCasting).ReadinessReasons.Contains(
                    "instant-route-unavailable:NativeCommandRequired:enhancement-native-command-required:quicken"))
                throw new InvalidOperationException("A native-command casting was not Not Ready in Instant.");
            // Instant-capable routes stay Ready.
            if (session.CompileForRuntime(plain, "long").Castings.Any(value => !value.IsExecutable))
                throw new InvalidOperationException("A direct rule cast was blocked by strict Instant.");
        }

        private static void TestLegacyFallbackProfile(string root)
        {
            string dir = Path.Combine(root, "policy-legacy-fallback");
            Directory.CreateDirectory(dir);
            PartyProviderSnapshot snapshot;
            CastingWorkspaceInputs inputs = PolicyInputs(WorkspaceInputs(out snapshot), false, "unit-wizard");
            var seed = new CastingWorkspaceSession(dir, "workspace-campaign", new CountingDispatchBoundary());
            Assert(AddDraftCasting(seed, inputs, "unit-wizard", "unit-t2").Applied);
            // A 0.3.0 plan saved with the animated fallback switched on and
            // combat casting allowed, exactly as that version wrote it.
            var repository = new CastingPlanRepository(dir);
            CastingPlanProfile stored = repository.Load("workspace-campaign").Profile;
            stored.Execution.Mode = "instant";
            stored.Execution.AllowAnimatedFallback = true;
            stored.Execution.OutOfCombatOnly = false;
            File.WriteAllText(repository.GetProfilePath("workspace-campaign"),
                Newtonsoft.Json.JsonConvert.SerializeObject(stored, Newtonsoft.Json.Formatting.Indented));
            if (!repository.Load("workspace-campaign").Profile.Execution.AllowAnimatedFallback)
                throw new InvalidOperationException("Fixture did not store the legacy preference.");
            var dispatch = new CountingDispatchBoundary();
            var session = new CastingWorkspaceSession(dir, "workspace-campaign", dispatch);
            if (session.LoadStatus != CastingPlanLoadStatus.Loaded ||
                !session.LegacyExecutionPreferencesOverridden)
                throw new InvalidOperationException("The legacy plan was not read as such.");
            WorkspaceApplyResult result = session.Apply(CastingApplyMode.Ordinary, "long", inputs);
            if (result.Allowed || dispatch.Submissions != 0 || result.BlockingCastings.Count != 1)
                throw new InvalidOperationException("A stored animated-fallback choice kept animating in Instant.");
            if (session.Apply(CastingApplyMode.Ordinary, "long", PolicyInputs(inputs, true)).ReviewReason !=
                    CastingExecutionPolicy.CombatActive)
                throw new InvalidOperationException("A stored combat choice let a routine run in combat.");
        }

        private static void TestStrictHybridExecutor()
        {
            AbilityKey ability = Ability("strict-hybrid", string.Empty, 0);
            var pool = new ResourcePoolSnapshot("strict-free", ResourcePoolKind.SpontaneousLevel, 50, 50, null);
            ProviderSnapshot provider = PlannerProvider("unit-a", "book-a", ability, "strict-free", 1);
            PartyProviderSnapshot snapshot = PlannerSnapshot(new[] { provider }, new[] { pool },
                "unit-a", "unit-b");
            foreach (CastExecutionStrategy strategy in new[] { CastExecutionStrategy.AnimatedFallback,
                CastExecutionStrategy.NativeCommandRequired })
            {
                var option = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                    new[] { "unit-a", "unit-b" }, 1, 1, strategy, "fixture-animated-only");
                CastPlan plan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                    new[] { "unit-a" }, new[] { option }, EmptyPolicy(), new ActiveEffectSnapshot(null));
                var animated = new AlwaysAnimatedRuntime();
                var instant = new AlwaysInstantRuntime();
                var report = new ExecutionReport(plan);
                // Even a caller that still asks for the fallback cannot get it.
                Drain(new HybridCastExecutor(instant, animated, true, true, null, null,
                    strictInstant: true).Execute(plan, report));
                if (animated.StartCount != 0 || instant.FireCount != 0 || report.Failed != 1 ||
                    !report.Records.Any(record => record.Detail ==
                        "instant-route-unavailable:" + strategy + ":fixture-animated-only"))
                    throw new InvalidOperationException("Strict Instant animated or fired a " + strategy + " step.");
            }
            var direct = new ProviderPlanningOption(provider, new[] { "unit-a", "unit-b" },
                new[] { "unit-a", "unit-b" }, 1, 1);
            CastPlan directPlan = PlannerPlan(snapshot, ability, CastGroupingKind.PerTarget,
                new[] { "unit-a" }, new[] { direct }, EmptyPolicy(), new ActiveEffectSnapshot(null));
            var directAnimated = new AlwaysAnimatedRuntime();
            var directInstant = new AlwaysInstantRuntime();
            var directReport = new ExecutionReport(directPlan);
            Drain(new HybridCastExecutor(directInstant, directAnimated, false, true, null, null,
                strictInstant: true).Execute(directPlan, directReport));
            if (directAnimated.StartCount != 0 || directInstant.FireCount != 1 || directReport.Confirmed != 1)
                throw new InvalidOperationException("A strict instant direct cast did not run instantly.");
        }
    }
}
