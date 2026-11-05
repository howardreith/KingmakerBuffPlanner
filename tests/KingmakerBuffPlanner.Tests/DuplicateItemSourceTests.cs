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
    // Discovery builds a separate expression for every physical item's
    // ability key. These tests carry those distinct objects through the
    // production catalogue, workspace, compiler and saved-plan repository.
    // The only fixture boundary is the discovered party/providers/pools.
    internal static partial class Program
    {
        private const string DuplicateItemCampaign = "campaign:duplicate-items";
        private const string DuplicateItemAbilityGuid = "duplicate-item-ability";
        private const string DuplicateItemSourceId = "ability|" + DuplicateItemAbilityGuid;

        private static void RunDuplicateItemSourceTests(string root)
        {
            Run("playtest-area-wording-two-item-owners", () => TestAreaItemWording(root));
            Run("duplicate-items-across-casters-keep-every-exact-source",
                () => TestDuplicateItemsAcrossCasters(root));
            Run("duplicate-items-on-one-caster-require-exact-selection",
                () => TestDuplicateItemsOnOneCaster(root));
            Run("duplicate-items-reserve-only-the-selected-copy-charges",
                () => TestDuplicateItemChargePools(root));
            Run("duplicate-items-rescan-and-reload-keep-exact-intent",
                () => TestDuplicateItemRescanAndReload(root));
            Run("duplicate-items-keep-spells-facts-and-variants-separate",
                () => TestDuplicateItemCatalogueMembership(root));
            Run("duplicate-items-legacy-aliases-never-create-ghost-buffs",
                () => TestDuplicateItemLegacyAliases(root));
        }

        private static void TestAreaItemWording(string root)
        {
            DuplicateItemParty party = DuplicateItemFixture(false);
            foreach (ProviderSnapshot provider in party.Providers)
                party.Effects[provider.Key.Ability.Canonical] = new TargetedEffectExpression(EffectTarget.AlliedAreaRecipients,
                    new EffectLeafExpression(EffectKind.AreaBuff, "area-item-buff", EffectTarget.AlliedAreaRecipients,
                        "AbilityAreaEffectBuff", "area/item"));
            party.Effects[DuplicateItemSourceId] = party.Effects[party.Providers[0].Key.Ability.Canonical];
            var inputs = new CastingWorkspaceInputs(party.Snapshot, party.Options, party.Effects, null);
            var session = new CastingWorkspaceSession(Path.Combine(root, "area-copy"), DuplicateItemCampaign);
            foreach (ProviderSnapshot provider in party.Providers.Take(2))
            {
                session.SelectGraphBuff(CatalogSourceIdentity.For(provider.Key.Ability,
                    party.Effects[provider.Key.Ability.Canonical]), inputs);
                session.SelectGraphCaster(provider.Key.CasterUnitId, inputs);
                Expect(session.SelectGraphSource(provider.Key.Canonical, inputs).Applied, "select area source");
                var added = session.AddGraphCasting(provider.Key.CasterUnitId, inputs);
                Expect(added.Applied, "add area casting: " + added.Edit.Reason);
            }
            string signature = session.DocumentIntentSignature();
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(view.SelectedSourceIsArea && CastingGraphText.TargetingLabel(view.SelectedSourceIsGroup, view.SelectedSourceIsArea) == "Area buff", "area targeting label");
            Expect(view.Inspector.TargetLabel == "Centred on item-caster-2" && view.Inspector.CoverageText.Contains("whoever is in the area"), "origin/coverage: " + view.Inspector.TargetLabel + ";" + view.Inspector.CoverageText);
            Expect(view.Casters.SelectMany(c => c.Sources).All(row => row.Label == "Item charge"), "resource labels: " + string.Join("|", view.Casters.SelectMany(c => c.Sources).Select(r => r.Label)));
            Expect(view.Castings.Count == 2 && view.Castings.Select(c => c.SourceProviderKey).Distinct().Count() == 2 &&
                view.Casters.Take(2).SelectMany(c => c.Sources).All(row => row.CapacityText.StartsWith("1 of 2", StringComparison.Ordinal)), "item counts: " + string.Join("|", view.Casters.SelectMany(c => c.Sources).Select(r => r.CapacityText)));
            Assert(session.DocumentIntentSignature() == signature);
            Assert(CastingGraphText.TargetingLabel(true, false) == "Group buff" && CastingGraphText.TargetingLabel(false, false) == "Single target");
        }

        private sealed class DuplicateItemParty
        {
            internal PartyProviderSnapshot Snapshot;
            internal List<ProviderSnapshot> Providers;
            internal List<ProviderPlanningOption> Options;
            internal Dictionary<string, EffectExpression> Effects;
            internal PlannerSetupModel Catalogue;
            internal CastingWorkspaceInputs Inputs;
        }

        private static DuplicateItemParty DuplicateItemFixture(bool sameCaster,
            int firstRemaining = 2)
        {
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            for (int index = 1; index <= 3; index++)
            {
                string token = "session-item-copy-" + index;
                string caster = "item-caster-" + (sameCaster ? 1 : index);
                var ability = new AbilityKey(DuplicateItemAbilityGuid, string.Empty, 0,
                    SourceKind.Item, token);
                providers.Add(DuplicateItemProvider(caster, ability, token,
                    "item|" + token, "Item blessing"));
                pools.Add(new ResourcePoolSnapshot("item|" + token, ResourcePoolKind.ItemCharges,
                    2, index == 1 ? firstRemaining : 2, null));
            }
            return DuplicateItemInputs(providers, pools);
        }

        private static ProviderSnapshot DuplicateItemProvider(string caster, AbilityKey ability,
            string instance, string pool, string displayName, string book = "")
        {
            return new ProviderSnapshot(new ProviderKey(caster, book, ability, instance),
                displayName, string.IsNullOrEmpty(book) ? 0 : 2, pool, 1, null, null,
                5, 50, "A beneficial effect on one recipient.", "5 minutes", displayName,
                0, string.IsNullOrEmpty(book) ? string.Empty : "Wizard");
        }

        private static DuplicateItemParty DuplicateItemInputs(IEnumerable<ProviderSnapshot> providers,
            IEnumerable<ResourcePoolSnapshot> pools)
        {
            List<ProviderSnapshot> sourceList = providers.ToList();
            string[] unitIds = sourceList.Select(provider => provider.Key.CasterUnitId)
                .Concat(new[] { "item-target-1", "item-target-2", "item-target-3" })
                .Distinct(StringComparer.Ordinal).ToArray();
            var units = unitIds.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))).ToList();
            var snapshot = new PartyProviderSnapshot(units, sourceList, pools);
            var options = sourceList.Select(provider => new ProviderPlanningOption(provider,
                unitIds, new[] { provider.Key.CasterUnitId }, 5, 50)).ToList();
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
            foreach (ProviderSnapshot provider in sourceList)
            {
                // Allocate independently, as the scanner does. In particular,
                // identical physical item copies never share this object.
                AbilityKey ability = provider.Key.Ability;
                effects[ability.Canonical] = new ReferencedAbilityExpression(
                    ability.BaseAbilityGuid, new SequenceEffectExpression(new EffectExpression[]
                    {
                        new EffectLeafExpression(EffectKind.Buff, "same-beneficial-buff",
                            EffectTarget.CurrentTarget, "ContextActionApplyBuff",
                            ability.BaseAbilityGuid + "/0:ContextActionApplyBuff")
                    }));
            }
            var catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault(DuplicateItemCampaign),
                snapshot, new ActiveEffectSnapshot(null), effects, options, ignored => { });
            return new DuplicateItemParty
            {
                Snapshot = snapshot,
                Providers = sourceList,
                Options = options,
                Effects = effects,
                Catalogue = catalogue,
                Inputs = new CastingWorkspaceInputs(snapshot, options, catalogue.EffectsBySource,
                    new CastEnhancementSnapshot[0])
            };
        }

        private static CastingWorkspaceSession DuplicateItemSession(string root, string name,
            out string directory)
        {
            directory = Path.Combine(root, "duplicate-items-" + name);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
            return new CastingWorkspaceSession(directory, DuplicateItemCampaign);
        }

        private static CastingGraphSourceRow DuplicateItemRow(CastingGraphView view,
            ProviderSnapshot provider)
        {
            CastingGraphCasterNode caster = view.CasterById(provider.Key.CasterUnitId);
            Expect(caster != null, "The item's owner is missing from the caster lane.");
            CastingGraphSourceRow row = caster.Sources.SingleOrDefault(value =>
                value.ProviderKey == provider.Key.Canonical);
            Expect(row != null, "A physical item is missing from the exact source choices.");
            return row;
        }

        private static string AddDuplicateItemCasting(CastingWorkspaceSession session,
            DuplicateItemParty party, ProviderSnapshot provider, string target)
        {
            session.SelectGraphBuff(CatalogSourceIdentity.For(provider.Key.Ability,
                party.Effects[provider.Key.Ability.Canonical]), party.Inputs);
            session.SelectGraphCaster(provider.Key.CasterUnitId, party.Inputs);
            Expect(session.SelectGraphSource(provider.Key.Canonical, party.Inputs).Applied,
                "The exact item source could not be selected.");
            CastingGraphEditResult added = session.AddGraphCasting(target, party.Inputs);
            Expect(added.Applied && !added.ShowedExisting && added.CastingId != null,
                "The selected item could not author its own casting: " +
                (added.Edit == null ? "no edit result" : added.Edit.Reason));
            return added.CastingId;
        }

        private static void TestDuplicateItemsAcrossCasters(string root)
        {
            DuplicateItemParty party = DuplicateItemFixture(false);
            Expect(party.Catalogue.Sources.Count == 1 &&
                party.Catalogue.Sources[0].Providers.Count == 3 &&
                party.Catalogue.Sources[0].Abilities.Count == 3,
                "The fixture did not discover three exact copies under one catalogue buff.");
            Expect(!ReferenceEquals(party.Effects[party.Providers[0].Key.Ability.Canonical],
                party.Effects[party.Providers[1].Key.Ability.Canonical]),
                "The fixture reused an expression object and cannot reproduce the reported defect.");
            string directory;
            CastingWorkspaceSession session = DuplicateItemSession(root, "across-casters", out directory);
            session.SelectGraphBuff(DuplicateItemSourceId, party.Inputs);
            CastingGraphView view = session.BuildGraph(party.Inputs);
            Expect(view.Catalogue.Count == 1 && view.Catalogue[0].SourceId == DuplicateItemSourceId &&
                view.Casters.Count == 3,
                "The workspace lost an item owner or duplicated the buff catalogue entry.");
            WorkspaceView workspace = session.BuildView(party.Inputs);
            Expect(workspace.Casters.Count(caster => caster.Capable) == 3,
                "The caster picker recognized only the catalogue representative's owner.");
            foreach (ProviderSnapshot provider in party.Providers)
            {
                Expect(DuplicateItemRow(view, provider).Pinnable,
                    "A unique item identity was incorrectly treated as an unpinnable twin.");
                session.SelectGraphCaster(provider.Key.CasterUnitId, party.Inputs);
                Expect(session.BuildGraph(party.Inputs).SelectedProviderKey == provider.Key.Canonical,
                    "A caster's sole item source was not selected.");
                AddDuplicateItemCasting(session, party, provider, provider.Key.CasterUnitId);
            }
            ExplicitCastingPlan plan = session.CompilePlan(party.Inputs);
            Expect(plan.ReadyInvocationCount == 3 && plan.Castings.All(casting =>
                party.Providers.Any(provider => provider.Key.Canonical == casting.Provider.Canonical &&
                    provider.Key.Ability.SpecialSourceId == casting.Ability.SpecialSourceId)),
                "The compiler substituted or discarded a selected physical item.");
            session.Save();
            CastingPlanLoadResult saved = new CastingPlanRepository(directory).Load(DuplicateItemCampaign);
            Expect(saved.Profile != null && saved.Profile.ToDocument().Castings.Select(casting =>
                    casting.Ability.SpecialSourceId).OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(party.Providers.Select(provider => provider.Key.Ability.SpecialSourceId)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "The saved plan did not retain the three exact item tokens.");
        }

        private static void TestDuplicateItemsOnOneCaster(string root)
        {
            DuplicateItemParty party = DuplicateItemFixture(true);
            string directory;
            CastingWorkspaceSession session = DuplicateItemSession(root, "one-caster", out directory);
            session.SelectGraphBuff(DuplicateItemSourceId, party.Inputs);
            session.SelectGraphCaster("item-caster-1", party.Inputs);
            CastingGraphView view = session.BuildGraph(party.Inputs);
            Expect(view.Catalogue.Count == 1 && view.Casters.Count == 1 &&
                view.Casters[0].Sources.Count == 3 && view.Casters[0].Sources.All(row => row.Pinnable) &&
                view.SelectedProviderKey == null,
                "Three owned copies were collapsed or one exact source was chosen silently.");
            Expect(session.BuildView(party.Inputs).DraftProviders.Count == 3,
                "The provider picker lost one of the caster's item copies.");
            CastingGraphEditResult ambiguous = session.AddGraphCasting("item-target-1", party.Inputs);
            Expect(!ambiguous.Applied && ambiguous.Edit.Reason.StartsWith(
                "draft-ability-unresolved:exact-source-ambiguous", StringComparison.Ordinal) &&
                session.Document.Castings.Count == 0,
                "Adding without choosing an exact physical item was not refused.");
            foreach (ProviderSnapshot provider in party.Providers)
                AddDuplicateItemCasting(session, party, provider, "item-target-1");
            view = session.BuildGraph(party.Inputs);
            Expect(view.Castings.Count == 3 && view.Castings.Select(casting => casting.SourceProviderKey)
                .Distinct(StringComparer.Ordinal).Count() == 3 &&
                session.CompilePlan(party.Inputs).ReadyInvocationCount == 3,
                "Different copies on one caster did not keep independent castings on the same recipient.");
            Expect(session.BuildView(party.Inputs).FocusedProviders.Count == 3,
                "The focused casting cannot be changed to another exact copy.");
        }

        private static void TestDuplicateItemChargePools(string root)
        {
            DuplicateItemParty party = DuplicateItemFixture(true, 1);
            string directory;
            CastingWorkspaceSession session = DuplicateItemSession(root, "charges", out directory);
            ProviderSnapshot first = party.Providers[0];
            string firstCasting = AddDuplicateItemCasting(session, party, first, "item-target-1");
            string shortCasting = AddDuplicateItemCasting(session, party, first, "item-target-2");
            string secondCasting = AddDuplicateItemCasting(session, party, party.Providers[1], "item-target-2");
            string thirdCasting = AddDuplicateItemCasting(session, party, party.Providers[2], "item-target-3");
            ExplicitCastingPlan plan = session.CompilePlan(party.Inputs);
            Expect(plan.CastingById(firstCasting).Readiness == ResolvedCastingReadiness.Ready &&
                plan.CastingById(shortCasting).Readiness == ResolvedCastingReadiness.Blocked &&
                plan.CastingById(shortCasting).Cost.Count == 0 &&
                plan.CastingById(secondCasting).Readiness == ResolvedCastingReadiness.Ready &&
                plan.CastingById(thirdCasting).Readiness == ResolvedCastingReadiness.Ready,
                "Exhausting one item blocked another copy or borrowed its charges.");
            foreach (ProviderSnapshot provider in party.Providers)
            {
                ResolvedCasting ready = plan.Castings.Single(casting =>
                    casting.Provider.Canonical == provider.Key.Canonical && casting.IsExecutable);
                Expect(ready.Cost.Count == 1 && ready.Cost[0].Category == CastingCostCategory.NativePool &&
                    ready.Cost[0].PoolKey == provider.ResourcePoolKey && ready.Cost[0].Units == 1,
                    "A casting reserved charges from a different physical copy.");
                CastingBudgetLine budget = plan.BudgetLineFor(provider.ResourcePoolKey);
                Expect(budget != null && budget.AllocatedUsage == 1 &&
                    budget.ForecastRemaining == (provider == first ? 0 : 1),
                    "The exact item's budget did not retain its independent balance.");
            }
            CastingGraphView view = session.BuildGraph(party.Inputs);
            Expect(!DuplicateItemRow(view, first).Usable &&
                DuplicateItemRow(view, first).Capacity.NativeRemaining == 0 &&
                DuplicateItemRow(view, party.Providers[1]).Capacity.NativeRemaining == 1 &&
                DuplicateItemRow(view, party.Providers[2]).Capacity.NativeRemaining == 1,
                "The graph hid the exhausted copy or displayed another item's capacity.");
            Expect(party.Snapshot.ResourcePools.Single(pool => pool.PoolKey == first.ResourcePoolKey)
                .Remaining == 1, "Planning mutated the discovered native charges.");
        }

        private static void TestDuplicateItemRescanAndReload(string root)
        {
            DuplicateItemParty first = DuplicateItemFixture(false);
            string directory;
            CastingWorkspaceSession session = DuplicateItemSession(root, "rescan", out directory);
            foreach (ProviderSnapshot provider in first.Providers)
                AddDuplicateItemCasting(session, first, provider, provider.Key.CasterUnitId);
            string intent = session.DocumentIntentSignature();
            session.Save();
            DuplicateItemParty rescanned = DuplicateItemFixture(false);
            Expect(!ReferenceEquals(first.Effects[first.Providers[0].Key.Ability.Canonical],
                rescanned.Effects[rescanned.Providers[0].Key.Ability.Canonical]),
                "A rescan must allocate fresh effect expression objects.");
            CastingGraphView refreshed = session.BuildGraph(rescanned.Inputs);
            Expect(refreshed.Catalogue.Count == 1 && refreshed.Casters.Count == 3 &&
                refreshed.Castings.Count == 3 && session.DocumentIntentSignature() == intent &&
                session.CompilePlan(rescanned.Inputs).ReadyInvocationCount == 3,
                "A fresh discovery pass lost item sources or changed saved casting intent.");
            var reopened = new CastingWorkspaceSession(directory, DuplicateItemCampaign);
            CastingGraphView restored = reopened.BuildGraph(rescanned.Inputs);
            Expect(restored.SelectedSourceId == DuplicateItemSourceId && restored.Catalogue.Count == 1 &&
                restored.Casters.Count == 3 && restored.Castings.Count == 3 &&
                reopened.DocumentIntentSignature() == intent &&
                reopened.CompilePlan(rescanned.Inputs).ReadyInvocationCount == 3,
                "Reopening the saved plan did not reconstruct all exact item sources.");
            Expect(restored.Castings.Select(casting => casting.SourceProviderKey)
                .OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(rescanned.Providers.Select(provider => provider.Key.Canonical)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "The saved graph silently rebound a casting to another item.");
        }

        private static void TestDuplicateItemCatalogueMembership(string root)
        {
            DuplicateItemParty items = DuplicateItemFixture(false);
            var providers = items.Providers.ToList();
            var pools = items.Snapshot.ResourcePools.ToList();
            var fact = new AbilityKey("class-benefit", string.Empty, 0, SourceKind.AbilityResource, string.Empty);
            providers.Add(DuplicateItemProvider("class-caster", fact, string.Empty, "class-pool", "Class benefit"));
            pools.Add(new ResourcePoolSnapshot("class-pool", ResourcePoolKind.AbilityResource, 2, 2, null));
            for (int index = 1; index <= 2; index++)
            {
                var spell = new AbilityKey("equivalent-spell-" + index, string.Empty, 0,
                    SourceKind.Spellbook, string.Empty);
                providers.Add(DuplicateItemProvider("spell-caster-" + index, spell, "level-2",
                    "spell-pool-" + index, "Spell benefit", "spellbook-" + index));
                pools.Add(new ResourcePoolSnapshot("spell-pool-" + index,
                    ResourcePoolKind.SpontaneousLevel, 2, 2, null));
                string token = "session-item-variant-" + index;
                var variant = new AbilityKey("item-variant-parent", "item-variant-child-" + index,
                    0, SourceKind.Item, token);
                providers.Add(DuplicateItemProvider("variant-caster-" + index, variant, token,
                    "item|" + token, "Variant benefit " + index));
                pools.Add(new ResourcePoolSnapshot("item|" + token, ResourcePoolKind.ItemCharges, 2, 2, null));
            }
            DuplicateItemParty party = DuplicateItemInputs(providers, pools);
            string directory;
            CastingWorkspaceSession session = DuplicateItemSession(root, "catalogue-membership", out directory);
            CastingGraphView view = session.BuildGraph(party.Inputs);
            Expect(party.Catalogue.Sources.Count == 5 && view.Catalogue.Count == 5,
                "Equal payloads merged an item, class ability or concrete variant, or split equivalent spells.");
            var abilityKeys = new HashSet<string>(providers.Select(provider => provider.Key.Ability.Canonical),
                StringComparer.Ordinal);
            Expect(view.Catalogue.All(entry => !abilityKeys.Contains(entry.SourceId)) &&
                view.Catalogue.Select(entry => entry.SourceId).OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(party.Catalogue.Sources.Select(source => source.SourceId)
                        .OrderBy(value => value, StringComparer.Ordinal)),
                "A provider's raw ability identity appeared as a ghost catalogue entry.");
            foreach (SetupSourceRow source in party.Catalogue.Sources)
            {
                session.SelectGraphBuff(source.SourceId, party.Inputs);
                view = session.BuildGraph(party.Inputs);
                string[] expected = source.Providers.Select(provider => provider.Key.Canonical)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                Expect(view.Casters.SelectMany(caster => caster.Sources).Select(row => row.ProviderKey)
                    .OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(expected),
                    "A catalogue entry hid its own provider or accepted another ability's equal payload.");
            }
            session.SelectGraphBuff(DuplicateItemSourceId, party.Inputs);
            Expect(!session.SelectGraphSource(providers.Single(provider => provider.Key.Ability == fact)
                    .Key.Canonical, party.Inputs).Applied && session.Document.Castings.Count == 0,
                "A class ability was accepted as an exact source of the item buff.");
        }

        private static void TestDuplicateItemLegacyAliases(string root)
        {
            DuplicateItemParty party = DuplicateItemFixture(true);
            EffectExpression expression = party.Effects[party.Providers[0].Key.Ability.Canonical];
            var aliases = party.Providers.ToDictionary(provider => provider.Key.Ability.Canonical,
                provider => expression, StringComparer.Ordinal);
            aliases["legacy-item-source"] = expression;
            var inputs = new CastingWorkspaceInputs(party.Snapshot, party.Options, aliases,
                new CastEnhancementSnapshot[0]);
            string directory;
            CastingWorkspaceSession session = DuplicateItemSession(root, "legacy-aliases", out directory);
            CastingGraphView view = session.BuildGraph(inputs);
            Expect(view.Catalogue.Count == 1 && view.Catalogue[0].SourceId == "legacy-item-source" &&
                view.SelectedSourceId == "legacy-item-source" && view.Casters.Count == 1 &&
                view.Casters[0].Sources.Count == 3,
                "Legacy aliases exposed peer ability keys as catalogue entries or lost an exact source.");
        }
    }
}
