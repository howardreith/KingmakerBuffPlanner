using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private const string StaleItemCampaign = "campaign:stale-item-casting";

        private static void RunStaleItemCastingTests(string root)
        {
            Run("stale-item-casting-removes-selected-duplicate-and-persists-undo",
                () => TestStaleItemCastingRemoval(root, SourceKind.Item));
            Run("stale-item-casting-removes-legacy-free-fact-and-persists-undo",
                () => TestStaleItemCastingRemoval(root, SourceKind.Fact));
        }

        // A saved plan like the report: #1 retains an old session's item
        // token or a legacy free Fact identity, #2 and #3 are unrelated
        // item buffs, and #4 is a current selection of the group buff.
        // The repository preserves those separate records; the catalogue,
        // compiler and session are real.
        private static void TestStaleItemCastingRemoval(string root, SourceKind oldKind)
        {
            ProviderSnapshot signet;
            ProviderSnapshot bow;
            ProviderSnapshot aegis;
            CastingWorkspaceInputs inputs = StaleItemInputs(out signet, out bow, out aegis);
            string sourceId = CatalogSourceIdentity.DerivableFor(signet.Key.Ability);
            var oldAbility = new AbilityKey(signet.Key.Ability.BaseAbilityGuid, string.Empty, 0,
                oldKind, oldKind == SourceKind.Item ? "session-item-old-signet" : string.Empty);
            var before = new CastingPlanDocument(StaleItemCampaign, new[]
            {
                new RoutineDefinition("long", "Long"),
                new RoutineDefinition("important", "Important"),
                new RoutineDefinition("short", "Short")
            }, new[]
            {
                StaleItemRecord("stale-signet", 0, oldAbility, "unit-miria", true),
                StaleItemRecord("saved-bow", 1, bow.Key.Ability, "unit-octavia", false),
                StaleItemRecord("saved-aegis", 2, aegis.Key.Ability, "unit-valerie", false),
                StaleItemRecord("current-signet", 3, signet.Key.Ability, "unit-miria", true)
            });
            string dir = Path.Combine(root, "stale-item-casting-removal-" + oldKind);
            var repository = new CastingPlanRepository(dir);
            repository.Save(CastingPlanProfile.FromDocument(before));
            var session = new CastingWorkspaceSession(dir, StaleItemCampaign);
            string original = session.DocumentIntentSignature();
            session.SelectGraphBuff(sourceId, inputs);

            CastingGraphView view = session.BuildGraph(inputs);
            CastingGraphCasting stale = view.CastingById("stale-signet");
            CastingGraphCasting current = view.CastingById("current-signet");
            Expect(view.Castings.Count == 2 && stale != null && current != null &&
                stale.OrderLabel == "#1" && current.OrderLabel == "#4",
                "the saved stale and current item castings were collapsed or renumbered");
            Expect(stale.Readiness == ResolvedCastingReadiness.Blocked &&
                string.IsNullOrEmpty(stale.SourceProviderKey) && stale.Beneficiaries.Count == 0,
                "the stale saved identity silently rebound to the current item");
            Expect(current.Readiness == ResolvedCastingReadiness.Ready &&
                current.SourceProviderKey == signet.Key.Canonical && current.Beneficiaries.Count == 6,
                "the current replacement did not retain its exact item and group coverage");
            CastingGraphTargetNode portrait = view.TargetById("unit-miria");
            Expect(portrait != null && portrait.Action == CastingGraphRecipientAction.Disambiguate &&
                portrait.CastingIds.SequenceEqual(new[] { "stale-signet", "current-signet" }) &&
                portrait.Hint.Contains("#1") && portrait.Hint.Contains("#4"),
                "the group centre did not identify both saved castings");
            CastingGraphClickResult ambiguous = session.ClickGraphRecipient("unit-miria", inputs);
            Expect(ambiguous.Outcome == CastingGraphClickOutcome.DisambiguationRequired &&
                session.EditingFocusCastingId == null && session.DocumentIntentSignature() == original,
                "an ambiguous portrait click selected or removed a casting implicitly");

            session.FocusGraphCasting("stale-signet");
            view = session.BuildGraph(inputs);
            Expect(view.Inspector != null && view.Inspector.CastingId == "stale-signet" &&
                view.Inspector.SourceLabel == "Source not resolved" &&
                view.Inspector.Chip != null && view.Inspector.Chip.Selected &&
                string.IsNullOrEmpty(view.Inspector.Chip.SourceProviderKey),
                "the stale casting was not inspectable as its own unresolved source selection");
            Expect(session.ClickGraphRecipient("unit-miria", inputs).Outcome ==
                CastingGraphClickOutcome.DisambiguationRequired &&
                session.EditingFocusCastingId == "stale-signet" &&
                session.DocumentIntentSignature() == original,
                "inspecting a duplicate changed the portrait's explicit disambiguation rule");

            Expect(session.RemoveFocusedCasting().Applied && session.EditingFocusCastingId == null &&
                session.Document.Castings.Select(value => value.CastingId).SequenceEqual(
                    new[] { "saved-bow", "saved-aegis", "current-signet" }),
                "removing the stale casting touched a sibling or the current replacement");
            Expect(!session.IsDirty && session.AutosaveStatus == "saved",
                "the explicit stale-casting removal was not saved");
            CastingPlanLoadResult saved = repository.Load(StaleItemCampaign);
            Expect(saved.Status == CastingPlanLoadStatus.Loaded &&
                saved.Profile.ToDocument().Castings.Select(value => value.CastingId).SequenceEqual(
                    new[] { "saved-bow", "saved-aegis", "current-signet" }),
                "the stored plan did not preserve the three surviving castings");
            var reopened = new CastingWorkspaceSession(dir, StaleItemCampaign);
            reopened.SelectGraphBuff(sourceId, inputs);
            CastingGraphView after = reopened.BuildGraph(inputs);
            Expect(after.Castings.Count == 1 && after.Castings[0].CastingId == "current-signet" &&
                after.Castings[0].SourceProviderKey == signet.Key.Canonical &&
                after.TargetById("unit-miria").Action == CastingGraphRecipientAction.Remove &&
                reopened.CompileForRuntime(inputs, "long").CastingById("current-signet").IsExecutable,
                "reopening lost or changed the executable replacement after removing the stale duplicate");

            Expect(session.Undo() && session.DocumentIntentSignature() == original && !session.CanUndo,
                "one Undo did not restore exactly the original saved records, order and source identities");
            var undone = new CastingWorkspaceSession(dir, StaleItemCampaign);
            Expect(undone.DocumentIntentSignature() == original,
                "Undo did not persist the restored stale casting alongside the current replacement");
            undone.SelectGraphBuff(sourceId, inputs);
            Expect(undone.BuildGraph(inputs).TargetById("unit-miria").Action ==
                CastingGraphRecipientAction.Disambiguate,
                "the restored duplicate no longer requires an explicit casting choice");
        }

        private static PlannedCasting StaleItemRecord(string castingId, int order,
            AbilityKey ability, string caster, bool group)
        {
            return new PlannedCasting(castingId, "long", order,
                CatalogSourceIdentity.DerivableFor(ability), ability, caster, null,
                group ? CastingTargetMode.CasterCenteredOrigin : CastingTargetMode.DirectTarget,
                group ? null : caster, group ? CastingOrigin.CasterCentered() : null,
                null, null, null, ExistingEffectPolicy.Overwrite, null,
                CastingAuthoringState.Ready, null);
        }

        // Structural fixtures reproduce the report's group and single
        // target shapes without claiming a native item blueprint contract.
        private static CastingWorkspaceInputs StaleItemInputs(out ProviderSnapshot signet,
            out ProviderSnapshot bow, out ProviderSnapshot aegis)
        {
            string[] names = { "Miria", "Valerie", "Amiri", "Octavia", "Linzi", "Jaethal" };
            UnitSnapshot[] units = names.Select(name => new UnitSnapshot(
                "unit-" + name.ToLowerInvariant(), name, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))).ToArray();
            signet = StaleItemProvider("f1000000000000000000000000000001", "current-signet",
                "unit-miria", "Blessed Signet");
            bow = StaleItemProvider("f1000000000000000000000000000002", "current-bow",
                "unit-octavia", "Savage Bow");
            aegis = StaleItemProvider("f1000000000000000000000000000003", "current-aegis",
                "unit-valerie", "Woodland's Aegis");
            ProviderSnapshot[] providers = { signet, bow, aegis };
            var snapshot = new PartyProviderSnapshot(units, providers,
                providers.Select(provider => new ResourcePoolSnapshot(provider.ResourcePoolKey,
                    ResourcePoolKind.ItemCharges, 2, 2, null)));
            string[] all = units.Select(unit => unit.UnitId).ToArray();
            var options = new[]
            {
                new ProviderPlanningOption(signet, all, new[] { "unit-miria" }, 5, 100,
                    recipientIdsByAnchor: new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                    {
                        { "unit-miria", all }
                    }),
                new ProviderPlanningOption(bow, new[] { "unit-octavia" }, new[] { "unit-octavia" }, 5, 100),
                new ProviderPlanningOption(aegis, new[] { "unit-valerie" }, new[] { "unit-valerie" }, 5, 100)
            };
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { signet.Key.Ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "stale-item-signet-buff", EffectTarget.Party, "fixture", "fixture/signet") },
                { bow.Key.Ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "stale-item-bow-buff", EffectTarget.CurrentTarget, "fixture", "fixture/bow") },
                { aegis.Key.Ability.Canonical, new EffectLeafExpression(EffectKind.Buff,
                    "stale-item-aegis-buff", EffectTarget.CurrentTarget, "fixture", "fixture/aegis") }
            };
            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault(StaleItemCampaign),
                snapshot, new ActiveEffectSnapshot(null), effects, options, ignored => { });
            return new CastingWorkspaceInputs(snapshot, options, model.EffectsBySource,
                new CastEnhancementSnapshot[0]);
        }

        private static ProviderSnapshot StaleItemProvider(string abilityGuid, string item,
            string caster, string name)
        {
            string token = "session-item-" + item;
            var ability = new AbilityKey(abilityGuid, string.Empty, 0, SourceKind.Item, token);
            return new ProviderSnapshot(new ProviderKey(caster, string.Empty, ability, token),
                name, 0, "item|" + token, 1, null, null, 5, 100);
        }
    }
}
