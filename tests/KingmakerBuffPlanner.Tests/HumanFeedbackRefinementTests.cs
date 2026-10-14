using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunHumanFeedbackRefinementTests(string root)
        {
            Run("human-feedback-populated-plan-performance-measurement",
                () => MeasurePopulatedPlanPerformance(root));
            Run("human-feedback-seamantle-continuous-assignment",
                () => TestSeamantleContinuousAssignment(root));
            Run("human-feedback-resinous-share-continuous-assignment",
                () => TestResinousShareContinuousAssignment(root));
            Run("human-feedback-draft-context-clears-backing-choices",
                () => TestDraftContextLifetime(root));
            Run("human-feedback-availability-policy-persists-and-gates",
                () => TestAvailabilityPolicy(root));
            Run("human-feedback-resources-use-structured-accounting",
                () => TestResourceAccounting(root));
            Run("human-feedback-native-resources-and-run-casing-contract",
                TestNativeResourceViewContract);
            Run("human-feedback-calculation-cache-invalidates-exactly",
                () => TestCalculationCacheInvalidation(root));
            Run("human-feedback-variant-membership-is-exact",
                () => TestVariantMembership(root));
        }

        private static readonly AbilityKey SeamantleAbility = new AbilityKey(
            "51000000000000000000000000000001", string.Empty, 0,
            SourceKind.Spellbook, string.Empty);
        private static readonly AbilityKey ResinousAbility = new AbilityKey(
            "beast-shape", string.Empty, 0, SourceKind.Spellbook, string.Empty);
        private static readonly AbilityKey ProtectionAbility = new AbilityKey(
            "52000000000000000000000000000001", string.Empty, 0,
            SourceKind.Spellbook, string.Empty);
        private static readonly AbilityKey VariantAbility = new AbilityKey(
            "53000000000000000000000000000001",
            "53000000000000000000000000000002", 0,
            SourceKind.Spellbook, string.Empty);

        private static CastingWorkspaceInputs HumanFeedbackInputs()
        {
            var units = new[]
            {
                new UnitSnapshot("felix", "Felix", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("leinna", "Leinna", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("tias", "Tias", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("raine", "Raine", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("sayan", "Sayan", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))
            };
            var pools = new[]
            {
                new ResourcePoolSnapshot("pool-felix", ResourcePoolKind.SpontaneousLevel, 20, 20, null),
                new ResourcePoolSnapshot("pool-leinna", ResourcePoolKind.SpontaneousLevel, 20, 20, null),
                new ResourcePoolSnapshot("pool-sayan", ResourcePoolKind.SpontaneousLevel, 20, 20, null)
            };
            ProviderSnapshot felixSeamantle = FeedbackProvider(
                "felix", "book", SeamantleAbility, "seamantle", "Seamantle", "pool-felix");
            ProviderSnapshot leinnaSeamantle = FeedbackProvider(
                "leinna", "book-leinna", SeamantleAbility, "seamantle", "Seamantle", "pool-leinna");
            ProviderSnapshot resinous = FeedbackProvider(
                "felix", "book", ResinousAbility, "resinous", "Resinous Skin", "pool-felix");
            ProviderSnapshot leinnaResinous = FeedbackProvider(
                "leinna", "book-leinna", ResinousAbility, "resinous", "Resinous Skin", "pool-leinna");
            ProviderSnapshot protection = FeedbackProvider(
                "sayan", "book-sayan", ProtectionAbility, "protection", "Protection from Evil", "pool-sayan");
            var snapshot = new PartyProviderSnapshot(units,
                new[] { felixSeamantle, leinnaSeamantle, resinous, leinnaResinous, protection }, pools);
            string[] all = units.Select(value => value.UnitId).ToArray();
            var options = new[]
            {
                new ProviderPlanningOption(felixSeamantle, all, new[] { "felix" }, 18, 1800),
                new ProviderPlanningOption(leinnaSeamantle, all, new[] { "leinna" }, 18, 1800),
                new ProviderPlanningOption(resinous, new[] { "felix" }, new string[0], 18, 1800),
                new ProviderPlanningOption(leinnaResinous, new[] { "leinna" }, new string[0], 18, 1800),
                new ProviderPlanningOption(protection, all, new[] { "sayan" }, 18, 1800)
            };
            EffectExpression seamantle = Leaf("seamantle-effect");
            EffectExpression resin = Leaf("resinous-effect");
            EffectExpression protectionEffect = Leaf("protection-effect");
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { "seamantle", seamantle }, { SeamantleAbility.Canonical, seamantle },
                { "resinous-skin", resin }, { ResinousAbility.Canonical, resin },
                { "protection-from-evil", protectionEffect },
                { ProtectionAbility.Canonical, protectionEffect }
            };
            var share = new ShareCastingModifier(new[]
            {
                Capability("felix", "reservoir-felix", 1, 6, all)
            });
            return new CastingWorkspaceInputs(snapshot, options, effects,
                new[] { ShareEnhancement("felix", "reservoir-felix", 6) },
                new ICastingTargetingModifier[] { share });
        }

        private static ProviderSnapshot FeedbackProvider(string caster, string book,
            AbilityKey ability, string instance, string name, string pool)
        {
            return new ProviderSnapshot(new ProviderKey(caster, book, ability, instance),
                name, 9, pool, 1, null, null, 18, 1800,
                name + " description", "30 minutes", name, 0,
                caster == "felix" ? "Wizard" : "Sorcerer");
        }

        private static CastingWorkspaceSession FeedbackSession(string root, string name,
            out CastingWorkspaceInputs inputs)
        {
            string dir = Path.Combine(root, "human-feedback-" + name);
            Directory.CreateDirectory(dir);
            inputs = HumanFeedbackInputs();
            return new CastingWorkspaceSession(dir, "feedback-campaign");
        }

        private static void TestSeamantleContinuousAssignment(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = FeedbackSession(root, "seamantle", out inputs);
            session.SelectGraphBuff("seamantle", inputs);
            Expect(session.ClickGraphCaster("felix", inputs).Outcome ==
                CastingGraphClickOutcome.DraftConfigured, "Felix was not chosen for the next casting");
            CastingGraphClickResult felix = session.ClickGraphRecipient("felix", inputs);
            Expect(felix.Outcome == CastingGraphClickOutcome.Added &&
                session.EditingFocusCastingId == felix.CastingId &&
                session.GraphEditIntent == CastingGraphEditIntent.None,
                "Felix did not receive an inspection-only Seamantle focus");
            string afterFelix = session.DocumentIntentSignature();
            Expect(session.ClickGraphCaster("leinna", inputs).Outcome ==
                    CastingGraphClickOutcome.DraftConfigured &&
                session.DocumentIntentSignature() == afterFelix &&
                session.EditingFocusCastingId == null &&
                session.GraphEditIntent == CastingGraphEditIntent.None,
                "choosing Leinna retargeted Felix's saved casting");
            CastingGraphClickResult leinna = session.ClickGraphRecipient("leinna", inputs);
            Expect(leinna.Outcome == CastingGraphClickOutcome.Added,
                "Leinna did not receive a distinct Seamantle casting");
            Expect(session.Document.Castings.Count == 2 &&
                session.Document.Castings.Any(value => value.CasterUnitId == "felix" &&
                    value.DirectTargetUnitId == "felix") &&
                session.Document.Castings.Any(value => value.CasterUnitId == "leinna" &&
                    value.DirectTargetUnitId == "leinna"),
                "the Seamantle sequence did not retain two independent assignments");
            Expect(session.ClickGraphRecipient("felix", inputs).Outcome ==
                    CastingGraphClickOutcome.Removed &&
                session.Document.Castings.Count == 1 &&
                session.Document.Castings.Single().CasterUnitId == "leinna",
                "Leinna's selected source prevented removal of Felix's saved assignment");
            Expect(session.Undo() && session.Document.Castings.Count == 2,
                "Undo did not restore Felix's exact Seamantle assignment");
        }

        private static void TestResinousShareContinuousAssignment(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = FeedbackSession(root, "resinous", out inputs);
            session.SelectGraphBuff("resinous-skin", inputs);
            session.ClickGraphCaster("felix", inputs);
            Expect(session.ToggleDraftTargetingModifier(ShareCastingModifier.Id, inputs).Applied,
                "Share was not enabled for Resinous Skin");
            CastingGraphClickResult tias = session.ClickGraphRecipient("tias", inputs);
            Expect(tias.Outcome == CastingGraphClickOutcome.Added &&
                session.Draft.TargetingModifiers.Count == 1,
                "the Tias assignment cleared the unchanged Share context");
            // Inspecting the first casting is harmless: the next normal
            // portrait still adds, rather than moving, that casting.
            session.FocusGraphCasting(tias.CastingId);
            CastingGraphClickResult raine = session.ClickGraphRecipient("raine", inputs);
            Expect(raine.Outcome == CastingGraphClickOutcome.Added &&
                session.Document.Castings.Count == 2 &&
                session.Document.Castings.All(value => value.TargetingModifiers.Any(modifier =>
                    modifier.ModifierId == ShareCastingModifier.Id && modifier.Enabled)),
                "the Raine assignment retargeted Tias or lost Share");
            CastingGraphClickResult removed = session.ClickGraphRecipient("tias", inputs);
            Expect(removed.Outcome == CastingGraphClickOutcome.Removed &&
                session.Document.Castings.Count == 1 &&
                session.Document.Castings.Single().DirectTargetUnitId == "raine",
                "clicking Tias did not remove only Tias's assignment");
            Expect(session.Undo() && session.Document.Castings.Count == 2 &&
                session.Document.Castings.Any(value => value.DirectTargetUnitId == "tias") &&
                session.Document.Castings.Any(value => value.DirectTargetUnitId == "raine"),
                "Undo did not restore the exact Tias assignment");
            CastingWorkspaceSession reopened = new CastingWorkspaceSession(
                Path.Combine(root, "human-feedback-resinous"), "feedback-campaign");
            Expect(reopened.Document.Castings.Count == 2 &&
                reopened.Document.Castings.All(value => value.TargetingModifiers.Any(modifier =>
                    modifier.ModifierId == ShareCastingModifier.Id && modifier.Enabled)),
                "reload did not preserve both shared Resinous Skin assignments");
        }

        private static void TestDraftContextLifetime(string root)
        {
            CastingWorkspaceInputs inputs;
            CastingWorkspaceSession session = FeedbackSession(root, "draft-context", out inputs);
            session.SelectGraphBuff("resinous-skin", inputs);
            session.ClickGraphCaster("felix", inputs);
            Expect(session.ToggleDraftTargetingModifier(ShareCastingModifier.Id, inputs).Applied,
                "fixture Share choice was refused");
            session.Draft.Enhancements.Add(new AuthoredEnhancementSelection("draft-only", true, null));
            CastingGraphClickResult added = session.ClickGraphRecipient("tias", inputs);
            PlannedCasting saved = session.Document.Castings.Single();
            Expect(added.Outcome == CastingGraphClickOutcome.Added &&
                session.Draft.TargetingModifiers.Count == 1 && session.Draft.Enhancements.Count == 1,
                "adding under an unchanged context cleared transient choices");
            session.SelectGraphBuff("seamantle", inputs);
            Expect(session.Draft.TargetingModifiers.Count == 0 && session.Draft.Enhancements.Count == 0,
                "changing the buff did not clear the backing draft choices");
            Expect(saved.TargetingModifiers.Count == 1 && saved.Enhancements.Count == 1,
                "navigating away stripped choices from a saved casting");

            // The owner's unrelated-buff sequence: Share belongs to Felix's
            // Resinous Skin draft, not Protection from Evil or Sayan.
            session.SelectGraphBuff("resinous-skin", inputs);
            session.ClickGraphCaster("felix", inputs);
            Expect(session.ToggleDraftTargetingModifier(ShareCastingModifier.Id, inputs).Applied,
                "fixture Share re-arm was refused");
            session.SelectGraphBuff("protection-from-evil", inputs);
            session.ClickGraphCaster("sayan", inputs);
            Expect(session.Draft.TargetingModifiers.Count == 0 &&
                session.ClickGraphRecipient("tias", inputs).Outcome == CastingGraphClickOutcome.Added,
                "Protection from Evil inherited Share or could not be assigned by Sayan");

            // Same buff, different provider: Leinna has Resinous Skin but no
            // Share capability, so changing only that context clears Share.
            session.SelectGraphBuff("resinous-skin", inputs);
            session.ClickGraphCaster("felix", inputs);
            Expect(session.ToggleDraftTargetingModifier(ShareCastingModifier.Id, inputs).Applied,
                "fixture Share re-arm for provider change was refused");
            session.ClickGraphCaster("leinna", inputs);
            Expect(session.Draft.TargetingModifiers.Count == 0,
                "Share survived a change to a non-Share provider of the same buff");

            session.SelectGraphBuff("resinous-skin", inputs);
            session.ClickGraphCaster("felix", inputs);
            session.Draft.TargetingModifiers.Add(
                new TargetingModifierSelection(ShareCastingModifier.Id, true, null));
            session.Draft.Enhancements.Add(new AuthoredEnhancementSelection("draft-only", true, null));
            session.SelectRoutine("important");
            Expect(session.Draft.TargetingModifiers.Count == 0 && session.Draft.Enhancements.Count == 0,
                "changing the routine did not clear transient choices");

            CastingWorkspaceInputs graph = GraphInputs();
            string bardLevel2 = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            string bardLevel1 = GraphProviderKey("unit-bard", "book-bard-2", GraphAbilityA, "level-1");
            session = new CastingWorkspaceSession(Path.Combine(root, "human-feedback-provider-context"),
                "graph-campaign");
            session.SelectGraphBuff(GraphSourceA, graph);
            session.SelectGraphCaster("unit-bard", graph);
            Expect(session.SelectGraphSource(bardLevel2, graph).Applied, "fixture provider was refused");
            session.Draft.Enhancements.Add(new AuthoredEnhancementSelection(
                "reservoir-extend", true, null));
            CastingGraphClickResult providerSaved = session.ClickGraphRecipient("unit-t1", graph);
            Expect(providerSaved.Outcome == CastingGraphClickOutcome.Added &&
                session.Document.Castings.Single().Enhancements.Any(value =>
                    value.EnhancementId == "reservoir-extend"),
                "the exact-provider fixture did not save its applicable enhancement");

            // Re-clicking a multi-source caster header leaves the exact
            // provider context. That transition must clear the backing
            // choices before source B is picked, even though the caster is
            // unchanged, and must not rewrite the saved casting.
            session.SelectGraphCaster("unit-bard", graph);
            Expect(session.Draft.Enhancements.Count == 0 &&
                session.BuildGraph(graph).SelectedProviderKey == null,
                "same-caster header reselection retained source A's choices");
            Expect(session.SelectGraphSource(bardLevel1, graph).Applied &&
                session.Draft.Enhancements.Count == 0 &&
                session.Document.Castings.Single().Enhancements.Any(value =>
                    value.EnhancementId == "reservoir-extend"),
                "source B inherited source A's choice or the saved casting was stripped");

            Expect(session.SelectGraphSource(bardLevel2, graph).Applied,
                "fixture provider could not be restored");
            session.Draft.TargetingModifiers.Add(new TargetingModifierSelection("draft-only", true, null));
            session.Draft.Enhancements.Add(new AuthoredEnhancementSelection("reservoir-extend", true, null));
            Expect(session.SelectGraphSource(bardLevel2, graph).Applied &&
                session.Draft.TargetingModifiers.Count == 1 && session.Draft.Enhancements.Count == 1,
                "reselecting the same exact provider cleared transient choices");
            Expect(session.SelectGraphSource(bardLevel1, graph).Applied &&
                session.Draft.TargetingModifiers.Count == 0 && session.Draft.Enhancements.Count == 0,
                "changing only the exact provider did not clear transient choices");
            Expect(session.Document.Castings.Single().Enhancements.Any(value =>
                    value.EnhancementId == "reservoir-extend"),
                "direct provider switching stripped a previously saved casting");
        }

        private static void TestAvailabilityPolicy(string root)
        {
            string dir = Path.Combine(root, "human-feedback-policy");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "policy-campaign");
            Expect(session.ApplyMode == CastingApplyMode.Ordinary &&
                session.AvailabilityPolicy == ExecutionProfile.RequireFullPlan,
                "a new profile did not default to Require full plan");
            session.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
            var reopened = new CastingWorkspaceSession(dir, "policy-campaign");
            Expect(reopened.ApplyMode == CastingApplyMode.ReadyCastsOnly &&
                reopened.AvailabilityPolicy == ExecutionProfile.CastAvailable,
                "Cast available was not remembered per profile");

            string path = reopened.PlanPathForRuntime;
            JObject malformed = JObject.Parse(File.ReadAllText(path));
            malformed["execution"]["availabilityPolicy"] = "surprising-new-value";
            File.WriteAllText(path, malformed.ToString());
            var failedClosed = new CastingWorkspaceSession(dir, "policy-campaign");
            Expect(failedClosed.ApplyMode == CastingApplyMode.Ordinary,
                "a malformed availability policy did not fail closed to strict");

            string oldDir = Path.Combine(root, "human-feedback-policy-old");
            Directory.CreateDirectory(oldDir);
            var oldSession = new CastingWorkspaceSession(oldDir, "old-policy");
            oldSession.SetExecutionMode("instant");
            JObject old = JObject.Parse(File.ReadAllText(oldSession.PlanPathForRuntime));
            ((JObject)old["execution"]).Remove("availabilityPolicy");
            old["formatRevision"] = 3;
            File.WriteAllText(oldSession.PlanPathForRuntime, old.ToString());
            Expect(new CastingWorkspaceSession(oldDir, "old-policy").ApplyMode ==
                CastingApplyMode.Ordinary,
                "an older profile silently opted into Cast available");

            CastingWorkspaceInputs inputs = GraphInputs(bardLevel2: 1);
            var gated = new CastingWorkspaceSession(
                Path.Combine(root, "human-feedback-policy-gate"), "graph-campaign");
            GraphAdd(gated, inputs, GraphSourceA, "unit-bard",
                GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2"), "unit-t1");
            GraphAdd(gated, inputs, GraphSourceA, "unit-bard",
                GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2"), "unit-t2");
            CastingGraphView strict = gated.BuildGraph(inputs);
            Expect(!strict.SelectedRoutineGate.Allowed &&
                strict.SelectedRoutineGate.BlockingCastings.Count == 1,
                "Require full plan did not refuse the shortage");
            gated.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
            CastingGraphView available = gated.BuildGraph(inputs);
            WorkspaceResourceReport report = gated.BuildResourceReport(inputs,
                WorkspaceResourceScope.SelectedRoutine);
            Expect(available.SelectedRoutineGate.Allowed &&
                available.SelectedRoutineGate.ExecutableCastingIds.Count == 1 &&
                report.Decision.Mode == CastingApplyMode.ReadyCastsOnly &&
                report.ExecutableCastingCount == 1 && report.OmissionCount == 1 &&
                gated.Document.Castings.Count == 2,
                "planner policy, Resources and saved assignments disagreed");
            WorkspaceApplyResult combat = gated.Apply(gated.ApplyMode, "long",
                new CastingWorkspaceInputs(inputs.Snapshot, inputs.ProviderOptions,
                    inputs.EffectsBySource, inputs.Enhancements, inputs.TargetingModifiers,
                    inputs.LiveEffects, true));
            Expect(!combat.Allowed && combat.ReviewReason == CastingExecutionPolicy.CombatActive,
                "Cast available bypassed a global combat refusal");

            string rootSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src",
                "KingmakerBuffPlanner", "UI", "BuffPlannerUiRoot.cs"));
            Expect(rootSource.Contains("StartCastingFirstRoutine(routineId, completed, null)") &&
                rootSource.Contains("CastingRunPolicy.Resolve(session, requestedMode)") &&
                rootSource.Contains("CastingRunPolicy.Apply(session, requestedMode, routineId, inputs)"),
                "the ordinary HUD route does not resolve the remembered policy through the session");
        }

        private static void TestResourceAccounting(string root)
        {
            CastingWorkspaceInputs inputs = GraphInputs(bardLevel2: 1);
            var session = new CastingWorkspaceSession(
                Path.Combine(root, "human-feedback-resources"), "graph-campaign");
            string provider = GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2");
            GraphAdd(session, inputs, GraphSourceA, "unit-bard", provider, "unit-t1");
            GraphAdd(session, inputs, GraphSourceA, "unit-bard", provider, "unit-t2");
            WorkspaceResourceReport strict = session.BuildResourceReport(inputs,
                WorkspaceResourceScope.SelectedRoutine);
            WorkspaceResourceRow pool = strict.Groups.SelectMany(value => value.Rows)
                .Single(value => value.PoolKey == "bard-l2");
            Expect(strict.ScopeLabel == "Long only" && !strict.Decision.Allowed &&
                pool.AvailableNow == 1 && pool.Needed == 2 && pool.ShortBy == 1 &&
                pool.WouldSpend == 0 && strict.Summary.Contains("nothing would be spent"),
                "strict Resources accounting did not show available/needed/short and zero spend");
            session.SetAvailabilityPolicy(ExecutionProfile.CastAvailable);
            WorkspaceResourceReport available = session.BuildResourceReport(inputs,
                WorkspaceResourceScope.SelectedRoutine);
            pool = available.Groups.SelectMany(value => value.Rows)
                .Single(value => value.PoolKey == "bard-l2");
            Expect(available.Decision.Allowed && pool.WouldSpend == 1 &&
                available.ExecutableCastingCount == 1 && available.OmissionCount == 1,
                "Cast available Resources accounting did not use the executable subset");
            session.SelectRoutine("important");
            GraphAdd(session, inputs, GraphSourceD, "unit-bard", null, "unit-t3");
            WorkspaceResourceReport all = session.BuildResourceReport(inputs,
                WorkspaceResourceScope.AllRoutinesNoRest);
            Expect(all.ScopeLabel == "All routines — no rest" &&
                all.SavedCastingCount == 3 && all.Groups.SelectMany(value => value.Rows)
                    .Any(value => value.Unlimited && value.NeededText.Contains("cast")),
                "all-routines Resources did not disclose the no-rest scope or unlimited source");

            var preparedAbility = new AbilityKey(
                "54000000000000000000000000000001", string.Empty, 0,
                SourceKind.Spellbook, string.Empty);
            var preparedProvider = new ProviderSnapshot(
                new ProviderKey("prep-caster", "prep-book", preparedAbility, "prepared"),
                "Bless", 1, "prepared-pool", 1, new[] { "prepared-slot" },
                null, 8, 800, "", "", "Bless", 0, "Cleric");
            var preparedSnapshot = new PartyProviderSnapshot(new[]
            {
                new UnitSnapshot("prep-caster", "Harrim", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true)),
                new UnitSnapshot("prep-target", "Valerie", false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))
            }, new[] { preparedProvider }, new[]
            {
                new ResourcePoolSnapshot("prepared-pool", ResourcePoolKind.PreparedSlots,
                    1, 1, new[]
                    {
                        new ResourceTokenSnapshot("prepared-slot", preparedAbility, 1,
                            PreparedSlotKind.Common, true, true, null)
                    })
            });
            EffectExpression preparedEffect = Leaf("prepared-effect");
            var preparedInputs = new CastingWorkspaceInputs(preparedSnapshot,
                new[]
                {
                    new ProviderPlanningOption(preparedProvider,
                        new[] { "prep-caster", "prep-target" },
                        new[] { "prep-caster" }, 8, 800)
                },
                new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { "prepared-source", preparedEffect },
                    { preparedAbility.Canonical, preparedEffect }
                }, new CastEnhancementSnapshot[0]);
            var preparedSession = new CastingWorkspaceSession(
                Path.Combine(root, "human-feedback-resources-prepared"), "prepared-campaign");
            preparedSession.SelectGraphBuff("prepared-source", preparedInputs);
            preparedSession.SelectGraphCaster("prep-caster", preparedInputs);
            Expect(preparedSession.ClickGraphRecipient("prep-target", preparedInputs).Outcome ==
                    CastingGraphClickOutcome.Added,
                "prepared Resources fixture assignment was refused");
            WorkspaceResourceRow prepared = preparedSession.BuildResourceReport(preparedInputs,
                    WorkspaceResourceScope.SelectedRoutine).Groups.SelectMany(value => value.Rows)
                .Single();
            Expect(prepared.AvailableNow == 1 && prepared.Needed == 1 &&
                prepared.Label.Contains("exact prepared: Bless"),
                "prepared Resources row implied fungible slots or hid its exact restriction");
        }

        private static void TestNativeResourceViewContract()
        {
            string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src",
                "KingmakerBuffPlanner", "UI", "CastingWorkspaceScreenView.cs"));
            foreach (string required in new[]
            {
                "CreateScrollView(\"Accounting\"", "WorkspaceResourceReport",
                "\"Available now\"", "\"Saved need\"", "\"Would spend\"", "\"Short\"",
                "_resourcesCloseButton", "CloseResources();", "SetResourceScope",
                "row.gameObject.layer = _uiLayer", "text.gameObject.layer = _uiLayer",
                "ResourcesVisibleAccountingCellsForRuntime", "ResourcesAccountingUsesNativeLayerForRuntime",
                "handlerToFrameEndMs", "expectedState=", "outcome=superseded",
                "requestToRefreshMs", "discoveryMs", "sessionBindingAndPersistenceMs",
                "inputLeaseMs", "viewConstructionMs", "openReconciliationMs", "displaySnapshotMs",
                "cause=resources-open", "resources-scope-", "handlerSynchronousMs",
                "commandAndPersistenceMs", "calculationMs", "regionsMs", "themeMs", "layoutMs",
                "GraphEditIntent.ChangeRecipient", "GraphEditIntent.ChangeProvider"
            })
                Expect(source.Contains(required), "native refinement control is missing: " + required);
            Expect(!source.Contains("inputToFirstCorrectRenderedFrameMs"),
                "EndOfFrame is still mislabeled as a proven correct rendered frame");
            Expect(!source.Contains("PlannerTextRole.Action") &&
                source.Contains("WorkspaceFooterText.RunCaption"),
                "the Run control still forces uppercase or lost its native caption");
            string rootSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src",
                "KingmakerBuffPlanner", "UI", "BuffPlannerUiRoot.cs"));
            Expect(!rootSource.Contains("WorkspaceInputsRefreshMinimum") &&
                rootSource.Contains("if (_workspaceInputs == null)") &&
                rootSource.Contains("_workspaceInputs = CurrentCastingInputs"),
                "stable workspace gestures still force periodic discovery");
        }

        private static void TestCalculationCacheInvalidation(string root)
        {
            CastingWorkspaceInputs inputs = GraphInputs();
            var session = new CastingWorkspaceSession(
                Path.Combine(root, "human-feedback-cache"), "graph-campaign");
            GraphAdd(session, inputs, GraphSourceA, "unit-bard",
                GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2"), "unit-t1");
            session.BuildGraph(inputs);
            session.PresentForReview(inputs);
            int compiled = session.CompilationCountForRuntime;
            int forecast = session.ForecastCountForRuntime;
            session.BuildGraph(inputs);
            session.PresentForReview(inputs);
            session.SelectGraphBuff(GraphSourceB, inputs);
            session.BuildGraph(inputs);
            Expect(session.CompilationCountForRuntime == compiled &&
                session.ForecastCountForRuntime == forecast,
                "unchanged inputs or a pure buff selection recompiled the plan");

            session.SelectGraphBuff(GraphSourceA, inputs);
            session.SelectGraphCaster("unit-bard", inputs);
            session.SelectGraphSource(
                GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2"), inputs);
            Expect(session.ClickGraphRecipient("unit-t2", inputs).Outcome ==
                CastingGraphClickOutcome.Added, "cache fixture edit was refused");
            session.BuildGraph(inputs);
            Expect(session.CompilationCountForRuntime > compiled &&
                session.ForecastCountForRuntime > forecast,
                "an authored-plan revision did not invalidate calculations");

            int afterEdit = session.CompilationCountForRuntime;
            var fresh = new CastingWorkspaceInputs(inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, inputs.Enhancements, inputs.TargetingModifiers,
                inputs.LiveEffects, inputs.CombatActive, inputs.SpellbookMembership, inputs.RunActive);
            session.CompileForRuntime(fresh, "long");
            Expect(session.CompilationCountForRuntime > afterEdit,
                "a fresh authoritative snapshot reused a display calculation");

            // Removal stays possible even when the selected provider has no
            // remaining casts: membership comes from saved intent.
            CastingWorkspaceInputs exhausted = GraphInputs(bardLevel2: 0);
            var removal = new CastingWorkspaceSession(
                Path.Combine(root, "human-feedback-exhausted-removal"), "graph-campaign");
            GraphAdd(removal, exhausted, GraphSourceA, "unit-bard",
                GraphProviderKey("unit-bard", "book-bard", GraphAbilityA, "level-2"), "unit-t1");
            string selectedProvider = GraphProviderKey("unit-bard", "book-bard",
                GraphAbilityA, "level-2");
            var narrowedOptions = exhausted.ProviderOptions.Select(option =>
                option.Provider.Key.Canonical == selectedProvider
                    ? new ProviderPlanningOption(option.Provider, new[] { "unit-bard" },
                        option.LegalAnchorIds, 5, 100)
                    : option).ToList();
            var narrowed = new CastingWorkspaceInputs(exhausted.Snapshot, narrowedOptions,
                exhausted.EffectsBySource, exhausted.Enhancements,
                exhausted.TargetingModifiers, exhausted.LiveEffects,
                exhausted.CombatActive, exhausted.SpellbookMembership, exhausted.RunActive);
            CastingGraphTargetNode removalNode = removal.BuildGraph(narrowed).TargetById("unit-t1");
            Expect(removalNode.Action == CastingGraphRecipientAction.Remove &&
                removalNode.Legality == CastingGraphTargetLegality.Legal &&
                removalNode.Hint.Contains("remove") && removalNode.IllegalReason.Length == 0,
                "an unreachable current provider made a removable assignment look disabled");
            Expect(removal.ClickGraphRecipient("unit-t1", narrowed).Outcome ==
                    CastingGraphClickOutcome.Removed && removal.Document.Castings.Count == 0,
                "an exhausted provider prevented removal of its saved assignment");
        }

        private static void TestVariantMembership(string root)
        {
            CastingWorkspaceInputs basis = GraphInputs();
            var variant = new ProviderSnapshot(
                new ProviderKey("unit-bard", "book-variant", VariantAbility, "level-2-variant"),
                "Graph A variant", 2, "bard-l2", 1, null, null, 10, 100,
                "Variant description", "10 rounds", "Graph A variant", 1, "Variant Book");
            var providers = basis.Snapshot.Providers.Concat(new[] { variant }).ToList();
            var options = basis.ProviderOptions.Concat(new[]
            {
                new ProviderPlanningOption(variant, GraphUnits, new[] { "unit-bard" }, 10, 100)
            }).ToList();
            var effects = basis.EffectsBySource.ToDictionary(pair => pair.Key, pair => pair.Value,
                StringComparer.Ordinal);
            effects[VariantAbility.Canonical] = effects[GraphSourceA];
            var inputs = new CastingWorkspaceInputs(
                new PartyProviderSnapshot(basis.Snapshot.Units, providers,
                    basis.Snapshot.ResourcePools),
                options, effects, basis.Enhancements, basis.TargetingModifiers,
                basis.LiveEffects, basis.CombatActive, basis.SpellbookMembership,
                basis.RunActive);
            var session = new CastingWorkspaceSession(
                Path.Combine(root, "human-feedback-variant"), "graph-campaign");
            Expect(session.AddCastingForRuntime(DirectCasting("base-form", "long",
                    "unit-bard", "unit-t1", GraphSourceA, GraphAbilityA, null,
                    "book-bard")).Applied,
                "variant fixture base casting was refused");
            session.SelectGraphBuff(GraphSourceA, inputs);
            session.SelectGraphCaster("unit-bard", inputs);
            Expect(session.SelectGraphSource(variant.Key.Canonical, inputs).Applied,
                "variant fixture exact source was refused");
            CastingGraphTargetNode variantTarget = session.BuildGraph(inputs).TargetById("unit-t1");
            Expect(variantTarget.Action == CastingGraphRecipientAction.Add &&
                variantTarget.Legality == CastingGraphTargetLegality.Legal &&
                variantTarget.Hint == "Click to add a casting",
                "a different exact variant advertised removal of the base-form assignment");
            Expect(session.ClickGraphRecipient("unit-t1", inputs).Outcome ==
                    CastingGraphClickOutcome.Added && session.Document.Castings.Count == 2,
                "a different exact variant was mistaken for the saved assignment");
            Expect(session.ClickGraphRecipient("unit-t1", inputs).Outcome ==
                    CastingGraphClickOutcome.Removed &&
                session.Document.Castings.Count == 1 &&
                session.Document.Castings.Single().Ability.Canonical == GraphAbilityA.Canonical,
                "variant toggle removed the other exact ability form");
        }

        // A short, non-gating calculation benchmark. It deliberately has no
        // wall-clock assertion: the durable before/after receipt reports the
        // measurements, while behavioral assertions keep the fixture honest.
        private static void MeasurePopulatedPlanPerformance(string root)
        {
            CastingWorkspaceInputs inputs = ExpandedGraphInputs(72);
            string dir = Path.Combine(root, "human-feedback-performance");
            Directory.CreateDirectory(dir);
            var session = new CastingWorkspaceSession(dir, "graph-campaign");
            string[] routines = { "long", "important", "short" };
            string[] sources = { GraphSourceA, GraphSourceB, GraphSourceD };
            AbilityKey[] abilities = { GraphAbilityA, GraphAbilityB, GraphAbilityD };

            for (int index = 0; index < 67; index++)
            {
                int sourceIndex = index % sources.Length;
                string target = GraphUnits[index % GraphUnits.Length];
                var result = session.AddCastingForRuntime(DirectCasting(
                    "perf-cast-" + index.ToString("D2"), routines[index % routines.Length],
                    "unit-bard", target, sources[sourceIndex], abilities[sourceIndex],
                    null, "book-bard"));
                Expect(result.Applied, "populated fixture casting was refused: " + result.Reason);
            }

            var cold = Stopwatch.StartNew();
            CastingGraphView view = session.BuildGraph(inputs);
            cold.Stop();
            Expect(view.Catalogue.Count >= 72, "performance fixture catalogue is not long");
            Expect(session.CompileForRuntime(inputs, null).Castings.Count == 67,
                "performance fixture is not populated");

            TimingSummary warm = Measure(11, () => session.BuildGraph(inputs));
            int switchIndex = 0;
            TimingSummary buffSwitch = Measure(11, () =>
            {
                session.SelectGraphBuff("perf-source-" + (switchIndex++ % 24).ToString("D2"), inputs);
                session.BuildGraph(inputs);
            });

            session.SelectGraphBuff("perf-source-00", inputs);
            session.SelectGraphCaster("unit-bard", inputs);
            string providerKey = inputs.Snapshot.Providers.First(value =>
                value.Key.Ability.BaseAbilityGuid == "00000000000000000000000000000001").Key.Canonical;
            Expect(session.SelectGraphSource(providerKey, inputs).Applied,
                "performance fixture provider was not selectable");
            int toggleIndex = 0;
            TimingSummary recipientToggle = Measure(10, () =>
            {
                string target = GraphUnits[3 + (toggleIndex++ % 3)];
                CastingGraphClickResult result = session.ClickGraphRecipient(target, inputs);
                Expect(result.Outcome != CastingGraphClickOutcome.Refused,
                    "performance fixture recipient toggle was refused");
                session.BuildGraph(inputs);
            });

            Console.WriteLine(
                "HUMAN_FEEDBACK_PERF calculations-only catalog=" + view.Catalogue.Count +
                " castings=67 coldMs=" + Milliseconds(cold.ElapsedTicks).ToString("F3") +
                " warmMedianMs=" + warm.MedianMilliseconds.ToString("F3") +
                " warmSlowestMs=" + warm.SlowestMilliseconds.ToString("F3") +
                " buffMedianMs=" + buffSwitch.MedianMilliseconds.ToString("F3") +
                " buffSlowestMs=" + buffSwitch.SlowestMilliseconds.ToString("F3") +
                " toggleMedianMs=" + recipientToggle.MedianMilliseconds.ToString("F3") +
                " toggleSlowestMs=" + recipientToggle.SlowestMilliseconds.ToString("F3"));
        }

        private static CastingWorkspaceInputs ExpandedGraphInputs(int catalogSize)
        {
            CastingWorkspaceInputs basis = GraphInputs(bardLevel2: 80, diamonds: 80);
            var providers = basis.Snapshot.Providers.ToList();
            var options = basis.ProviderOptions.ToList();
            var effects = basis.EffectsBySource.ToDictionary(pair => pair.Key, pair => pair.Value,
                StringComparer.Ordinal);
            string[] all = basis.Snapshot.Units.Select(value => value.UnitId).ToArray();

            for (int index = 0; index < catalogSize; index++)
            {
                string guid = (index + 1).ToString("x32");
                string source = "perf-source-" + index.ToString("D2");
                var ability = new AbilityKey(guid, string.Empty, 0, SourceKind.Spellbook, string.Empty);
                var provider = new ProviderSnapshot(
                    new ProviderKey("unit-bard", "book-perf", ability, "level-0"),
                    "Performance Buff " + index.ToString("D2"), 0, "bard-cantrips", 0, null,
                    null, 12, 1200, string.Empty, "120 rounds",
                    "Performance Buff " + index.ToString("D2"), index, "Performance Book");
                providers.Add(provider);
                options.Add(new ProviderPlanningOption(provider, all, new[] { "unit-bard" }, 12, 1200));
                EffectExpression expression = Leaf("perf-effect-" + index.ToString("D2"));
                effects[source] = expression;
                effects[ability.Canonical] = expression;
            }

            return new CastingWorkspaceInputs(
                new PartyProviderSnapshot(basis.Snapshot.Units, providers, basis.Snapshot.ResourcePools),
                options, effects, basis.Enhancements, basis.TargetingModifiers, basis.LiveEffects,
                basis.CombatActive, basis.SpellbookMembership, basis.RunActive);
        }

        private static TimingSummary Measure(int count, Action action)
        {
            var samples = new List<double>();
            for (int index = 0; index < count; index++)
            {
                var clock = Stopwatch.StartNew();
                action();
                clock.Stop();
                samples.Add(Milliseconds(clock.ElapsedTicks));
            }
            samples.Sort();
            return new TimingSummary(samples[samples.Count / 2], samples[samples.Count - 1]);
        }

        private static double Milliseconds(long ticks)
        {
            return ticks * 1000.0 / Stopwatch.Frequency;
        }

        private sealed class TimingSummary
        {
            internal TimingSummary(double medianMilliseconds, double slowestMilliseconds)
            {
                MedianMilliseconds = medianMilliseconds;
                SlowestMilliseconds = slowestMilliseconds;
            }

            internal double MedianMilliseconds { get; private set; }
            internal double SlowestMilliseconds { get; private set; }
        }
    }
}
