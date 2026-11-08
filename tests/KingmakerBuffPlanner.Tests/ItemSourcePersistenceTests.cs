using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private const string ItemSourceItemGuid = "aaaa0000000000000000000000000001";
        private const string ItemSourceAbilityGuid = "90e59f4a4ada87243b7b3535a06d0638";

        private static void RunItemSourcePersistenceTests(string root)
        {
            Run("item-source-survives-item-serialization-roundtrip", TestItemSourceSurvivesRoundtrip);
            Run("item-source-reload-and-reopen-keep-selection", () => TestItemSourceReloadAndReopen(root));
            Run("item-source-native-charges-keep-selection", TestItemSourceNativeCharges);
            Run("item-source-duplicate-copies-one-caster", () => TestItemSourceDuplicateCopies(root));
            Run("item-source-copies-on-different-casters-stay-separate", TestItemSourceCopiesAcrossCasters);
            Run("item-source-sibling-abilities-share-charges", TestItemSourceSiblingAbilities);
            Run("item-source-transitions-follow-the-contract", TestItemSourceTransitions);
            Run("item-source-legacy-token-reassignment", () => TestItemSourceReassignment(root, SourceKind.Item));
            Run("item-source-legacy-fact-reassignment", () => TestItemSourceReassignment(root, SourceKind.Fact));
            Run("item-source-corrupt-selection-is-refused", TestItemSourceCorruptSelections);
            Run("item-source-unresolved-wording-is-truthful", () => TestItemSourceWording(root));
            Run("item-source-diagnostics-report-changes-once", () => TestItemSourceDiagnostics(root));
            Run("item-source-execution-binds-the-live-item", TestItemSourceExecutionBinding);
            Run("item-source-slot-rule-binds-its-slot", TestItemSourceSlotBinding);
        }

        // The Kingmaker save path deserializes ItemEntity through its
        // JsonConstructorMark constructor under OptInContractResolver. Only
        // the blueprint library is a fixture: ResourcesLibrary needs a game.
        private sealed class ItemFixtureBlueprintConverter : JsonConverter
        {
            private readonly Dictionary<string, BlueprintScriptableObject> _known;
            internal ItemFixtureBlueprintConverter(IEnumerable<BlueprintScriptableObject> known)
            {
                _known = known.ToDictionary(value => value.AssetGuid, StringComparer.Ordinal);
            }
            public override bool CanConvert(Type objectType)
            {
                return typeof(BlueprintScriptableObject).IsAssignableFrom(objectType);
            }
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((BlueprintScriptableObject)value).AssetGuid);
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                return reader.Value == null ? null : _known[(string)reader.Value];
            }
        }

        // The game's resolver chooses the persisted members and the load
        // constructor. BlueprintConverter is attached to the blueprint type,
        // so only that one contract is redirected to the fixture library.
        private sealed class ItemFixtureContractResolver : OptInContractResolver
        {
            private readonly ItemFixtureBlueprintConverter _blueprints;
            internal ItemFixtureContractResolver(ItemFixtureBlueprintConverter blueprints)
            {
                _blueprints = blueprints;
            }
            protected override Newtonsoft.Json.Serialization.JsonContract CreateContract(Type objectType)
            {
                Newtonsoft.Json.Serialization.JsonContract contract = base.CreateContract(objectType);
                if (_blueprints.CanConvert(objectType)) contract.Converter = _blueprints;
                return contract;
            }
        }

        private static ItemEntity ReloadItemThroughSaveContract(ItemEntity item,
            params BlueprintScriptableObject[] known)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new ItemFixtureContractResolver(new ItemFixtureBlueprintConverter(known)),
                TypeNameHandling = TypeNameHandling.Auto
            };
            string json = JsonConvert.SerializeObject(item, typeof(ItemEntity), settings);
            var loaded = JsonConvert.DeserializeObject<ItemEntity>(json, settings);
            if (loaded == null || ReferenceEquals(loaded, item) || !(loaded is ItemEntityUsable) ||
                !ReferenceEquals(loaded.Blueprint, item.Blueprint) || loaded.Charges != item.Charges)
                throw new InvalidOperationException("The fixture did not reconstruct the saved item: " + json);
            return loaded;
        }

        // A load rebuilds the ability fact as well as the item; nothing is
        // reused from the pre-load object graph except the blueprints.
        private static AbilityData ItemAbilityAfterLoad(ItemEntity item, AbilityData before)
        {
            var fact = NativeObject<Ability>();
            SetNative(fact, "Blueprint", before.Blueprint);
            SetNative(fact, "SourceItem", item);
            var data = NativeObject<AbilityData>();
            SetNative(data, "Blueprint", before.Blueprint);
            SetNative(data, "Caster", before.Caster);
            SetNative(data, "Fact", fact);
            return data;
        }

        private static AbilityData ItemSourceFixture(int charges, out ItemEntity item)
        {
            AbilityData data = NativeItemAbility("signet", charges, out item);
            SetNative(data.Blueprint, "m_AssetGuid", ItemSourceAbilityGuid);
            SetNative(item.Blueprint, "m_AssetGuid", ItemSourceItemGuid);
            return data;
        }

        // The provider exactly as discovery publishes it for this item.
        private static ProviderSnapshot ItemSourceProvider(AbilityData data, KingmakerFactResource resource)
        {
            return FactFixtureProvider(data, resource);
        }

        private static ExplicitCastingPlan CompileItemSource(CastingPlanDocument document,
            ProviderSnapshot provider, ResourcePoolSnapshot pool)
        {
            UnitSnapshot[] units = new[] { "unit-cleric", "unit-t1" }.Select(id => new UnitSnapshot(id, id, false,
                string.Empty, new TargetValidationSnapshot(true, true, true, true))).ToArray();
            var snapshot = new PartyProviderSnapshot(units, new[] { provider }, new[] { pool });
            var options = new[] { new ProviderPlanningOption(provider, units.Select(unit => unit.UnitId),
                new[] { "unit-cleric" }, 1, 10) };
            EffectExpression expression = Leaf("signet-buff");
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { "ability|" + ItemSourceAbilityGuid, expression },
                { provider.Key.Ability.Canonical, expression }
            };
            return new ExplicitCastingCompiler().Compile(document, snapshot, options, effects,
                new CastEnhancementSnapshot[0]);
        }

        private static CastingPlanDocument ThroughPlanFile(CastingPlanDocument document)
        {
            string json = JsonConvert.SerializeObject(CastingPlanProfile.FromDocument(document));
            return JsonConvert.DeserializeObject<CastingPlanProfile>(json).ToDocument();
        }

        private static void TestItemSourceSurvivesRoundtrip()
        {
            ItemEntity item;
            AbilityData before = ItemSourceFixture(3, out item);
            KingmakerFactResource selected = ReadNativeFact(before);
            ProviderSnapshot authored = ItemSourceProvider(before, selected);
            CastingPlanDocument saved = ThroughPlanFile(CastingDocument(DirectCasting("cast-4", "long",
                "unit-cleric", "unit-t1", "ability|" + ItemSourceAbilityGuid, authored.Key.Ability)));
            Expect(CompileItemSource(saved, authored, selected.Pool).Castings.Single().IsExecutable,
                "The casting was not executable from its original item.");

            ItemEntity loaded = ReloadItemThroughSaveContract(item, item.Blueprint);
            AbilityData after = ItemAbilityAfterLoad(loaded, before);
            KingmakerFactResource live = ReadNativeFact(after);
            Expect(!ReferenceEquals(loaded, item) && live.InstanceId != selected.InstanceId,
                "The fixture did not cross a real object-identity boundary.");
            ProviderSnapshot current = ItemSourceProvider(after, live);
            ResolvedCasting casting = CompileItemSource(ThroughPlanFile(saved), current, live.Pool)
                .Castings.Single();
            Expect(casting.IsExecutable,
                "A saved item casting lost its source across a save/load item reconstruction: " +
                string.Join(",", casting.ReadinessReasons.ToArray()));
            Expect(casting.Provider.Canonical == current.Key.Canonical &&
                KingmakerFactResources.ValidateBinding("unit-cleric", after, after, casting.Provider,
                    new ResourceReservation(live.Pool.PoolKey, 1, new string[0])) == null,
                "Compilation and native binding disagree about the live item.");
            after.Spend();
            Expect(loaded.Charges == 2 && item.Charges == 3,
                "Native spending did not debit exactly the reconstructed item.");
            string file = JsonConvert.SerializeObject(CastingPlanProfile.FromDocument(saved));
            Expect(!file.Contains(selected.InstanceId) && !file.Contains(ItemSourceSelector.LegacySessionPrefix),
                "A live object token reached the saved plan.");
        }

        // ------------------------------------------------------------------
        // Workspace fixtures: the discovered party is the only fixture
        // boundary; catalogue, session, repository and compiler are real.
        // ------------------------------------------------------------------

        private const string ItemPlanCampaign = "campaign:item-source";
        private const string ItemPlanSource = "ability|" + ItemSourceAbilityGuid;
        private static readonly string[] ItemPartyIds = { "unit-sayan", "unit-raine", "unit-tias" };

        private sealed class LiveItemCopy
        {
            internal ProviderSnapshot Provider;
            internal int Remaining;
        }

        private static AbilityKey ItemRule(string type)
        {
            return new AbilityKey(ItemSourceAbilityGuid, string.Empty, 0, SourceKind.Item,
                ItemSourceSelector.ForEquippedType(type));
        }

        private static AbilityKey ItemSlotRule(string type, string slot)
        {
            return new AbilityKey(ItemSourceAbilityGuid, string.Empty, 0, SourceKind.Item,
                ItemSourceSelector.ForEquippedSlot(type, slot));
        }

        private static LiveItemCopy Copy(string caster, AbilityKey ability, string token, int remaining,
            string type = null, string slot = null)
        {
            string slotSelector = slot == null ? string.Empty : ItemSourceSelector.ForEquippedSlot(type, slot);
            return new LiveItemCopy
            {
                Remaining = remaining,
                Provider = new ProviderSnapshot(new ProviderKey(caster, string.Empty, ability, token),
                    "Bless", 0, "item|" + token, 1, null, null, 5, 100, string.Empty, string.Empty,
                    "Blessed Signet", 0, string.Empty, slotSelector)
            };
        }

        private static CastingWorkspaceInputs ItemInputs(params LiveItemCopy[] copies)
        {
            UnitSnapshot[] units = ItemPartyIds.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                new TargetValidationSnapshot(true, true, true, true))).ToArray();
            List<ProviderSnapshot> providers = copies.Select(copy => copy.Provider).ToList();
            var snapshot = new PartyProviderSnapshot(units, providers, copies.Select(copy =>
                new ResourcePoolSnapshot(copy.Provider.ResourcePoolKey, ResourcePoolKind.ItemCharges,
                    Math.Max(3, copy.Remaining), copy.Remaining, null)));
            List<ProviderPlanningOption> options = providers.Select(provider =>
                new ProviderPlanningOption(provider, ItemPartyIds, new[] { provider.Key.CasterUnitId }, 5, 100,
                    recipientIdsByAnchor: new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
                    {
                        { provider.Key.CasterUnitId, ItemPartyIds }
                    })).ToList();
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal);
            foreach (ProviderSnapshot provider in providers)
                effects[provider.Key.Ability.Canonical] = new EffectLeafExpression(EffectKind.Buff,
                    "signet-buff", EffectTarget.Party, "fixture", "fixture/signet");
            var model = new PlannerSetupModel(BuffPlannerProfile.CreateDefault(ItemPlanCampaign), snapshot,
                new ActiveEffectSnapshot(null), effects, options, ignored => { });
            return new CastingWorkspaceInputs(snapshot, options, model.EffectsBySource,
                new CastEnhancementSnapshot[0]);
        }

        private static PlannedCasting SavedGroupCasting(string id, int order, AbilityKey ability, string caster)
        {
            return new PlannedCasting(id, "long", order, ItemPlanSource, ability, caster, null,
                CastingTargetMode.CasterCenteredOrigin, null, CastingOrigin.CasterCentered(), null,
                new[] { new TargetingModifierSelection("unused-modifier", false, null) },
                new[] { new AuthoredEnhancementSelection("optional-rod", false, null) },
                ExistingEffectPolicy.Overwrite, new[] { "marker" }, CastingAuthoringState.Ready, null);
        }

        private static CastingPlanDocument ItemPlan(params PlannedCasting[] castings)
        {
            return new CastingPlanDocument(ItemPlanCampaign, new[]
            {
                new RoutineDefinition("long", "Long"),
                new RoutineDefinition("important", "Important"),
                new RoutineDefinition("short", "Short")
            }, castings);
        }

        private static string StoreItemPlan(string root, string name, CastingPlanDocument document)
        {
            string directory = System.IO.Path.Combine(root, "item-source-" + name);
            if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
            System.IO.Directory.CreateDirectory(directory);
            new CastingPlanRepository(directory).Save(CastingPlanProfile.FromDocument(document));
            return directory;
        }

        private static UI.CastingWorkspaceSession OpenItemPlan(string directory, CastingWorkspaceInputs inputs)
        {
            var session = new UI.CastingWorkspaceSession(directory, ItemPlanCampaign);
            session.SelectGraphBuff(ItemPlanSource, inputs);
            return session;
        }

        private static ResolvedCasting CompileOneItem(CastingPlanDocument document, CastingWorkspaceInputs inputs)
        {
            return new ExplicitCastingCompiler().Compile(document, inputs.Snapshot, inputs.ProviderOptions,
                inputs.EffectsBySource, new CastEnhancementSnapshot[0]).Castings.Single();
        }

        private static void TestItemSourceReloadAndReopen(string root)
        {
            AbilityKey rule = ItemRule("signet");
            LiveItemCopy before = Copy("unit-sayan", rule, "session-item-before", 3);
            CastingWorkspaceInputs authored = ItemInputs(before);
            string directory = StoreItemPlan(root, "reload", ItemPlan());
            UI.CastingWorkspaceSession session = OpenItemPlan(directory, authored);
            session.SelectGraphCaster("unit-sayan", authored);
            Expect(session.SelectGraphSource(before.Provider.Key.Canonical, authored).Applied, "select source");
            UI.CastingGraphEditResult added = session.AddGraphCasting("unit-sayan", authored);
            Expect(added.Applied, "add casting");
            session.Save();
            string intent = session.DocumentIntentSignature();

            CastingWorkspaceInputs rescan = ItemInputs(Copy("unit-sayan", rule, "session-item-before", 3));
            Expect(session.BuildGraph(rescan).CastingById(added.CastingId).Readiness ==
                ResolvedCastingReadiness.Ready && session.DocumentIntentSignature() == intent,
                "A rescan changed the saved selection.");

            LiveItemCopy after = Copy("unit-sayan", rule, "session-item-after-load", 3);
            CastingWorkspaceInputs loaded = ItemInputs(after);
            UI.CastingWorkspaceSession reopened = OpenItemPlan(directory, loaded);
            UI.CastingGraphView view = reopened.BuildGraph(loaded);
            UI.CastingGraphCasting chip = view.CastingById(added.CastingId);
            Expect(reopened.DocumentIntentSignature() == intent, "Reopening rewrote the saved selection.");
            Expect(chip.Readiness == ResolvedCastingReadiness.Ready &&
                chip.SourceProviderKey == after.Provider.Key.Canonical && !chip.NeedsItemSourceChoice &&
                chip.Beneficiaries.Count == ItemPartyIds.Length,
                "The saved casting did not reconnect to the item discovered after a load.");
            var metrics = new UI.GraphLayoutMetrics();
            UI.GraphLayoutResult layout = UI.CastingGraphLayout.Compute(view, metrics);
            float rowCentre = layout.SourceRowTops[UI.CastingGraphLayout.SourceRowKey("unit-sayan",
                after.Provider.Key.Canonical)] + metrics.SourceRowHeight / 2f;
            Expect(Math.Abs(layout.Connections.Single().Inbound.From.Y - rowCentre) < 0.01f,
                "A resolved casting's connector did not attach to its source row.");
            Expect(reopened.CompileForRuntime(loaded, "long").CastingById(added.CastingId).Cost
                .Single().PoolKey == after.Provider.ResourcePoolKey,
                "The reservation did not bind to the live item's pool.");
        }

        private static void TestItemSourceNativeCharges()
        {
            ItemEntity item;
            AbilityData data = ItemSourceFixture(3, out item);
            KingmakerFactResource resource = ReadNativeFact(data);
            ProviderSnapshot provider = ItemSourceProvider(data, resource);
            CastingPlanDocument saved = ThroughPlanFile(CastingDocument(DirectCasting("cast-4", "long",
                "unit-cleric", "unit-t1", ItemPlanSource, provider.Key.Ability)));
            for (int spent = 1; spent <= 3; spent++)
            {
                data.Spend();
                resource = ReadNativeFact(data);
                ResolvedCasting casting = CompileItemSource(saved, provider, resource.Pool).Castings.Single();
                Expect(resource.Pool.Remaining == 3 - spent && casting.Provider != null &&
                    !casting.ReadinessReasons.Any(ItemSourceResolution.IsRefusal),
                    "Spending charges disturbed the saved selection.");
                Expect(casting.IsExecutable == (spent < 3) &&
                    (spent < 3 || casting.ReadinessReasons.Any(reason => reason.StartsWith(
                        "resource-pool-exhausted", StringComparison.Ordinal))),
                    "Exhausted charges were not reported as exhaustion.");
            }
            item.RestoreCharges();
            resource = ReadNativeFact(data);
            Expect(resource.Pool.Remaining == 5 && CompileItemSource(saved, provider, resource.Pool)
                .Castings.Single().IsExecutable, "Restored native charges did not restore the casting.");
        }

        private static void TestItemSourceDuplicateCopies(string root)
        {
            const string Type = "signet";
            Func<string, string, LiveItemCopy[]> party = (ringOne, ringTwo) => new[]
            {
                Copy("unit-sayan", ItemSlotRule(Type, "Ring1"), ringOne, 3, Type, "Ring1"),
                Copy("unit-sayan", ItemSlotRule(Type, "Ring2"), ringTwo, 1, Type, "Ring2")
            };
            LiveItemCopy[] session1 = party("session-item-a", "session-item-b");
            CastingWorkspaceInputs inputs = ItemInputs(session1);
            string directory = StoreItemPlan(root, "copies", ItemPlan(
                SavedGroupCasting("cast-1", 0, ItemRule(Type), "unit-sayan")));
            UI.CastingWorkspaceSession session = OpenItemPlan(directory, inputs);
            ResolvedCasting ambiguous = session.CompilePlan(inputs).CastingById("cast-1");
            Expect(ambiguous.Provider == null && !ambiguous.IsExecutable &&
                ambiguous.ReadinessReasons.Contains(ItemSourceResolution.Ambiguous + ":2"),
                "A plain rule silently chose between identical copies.");
            session.FocusGraphCasting("cast-1");
            Expect(session.BeginChangeProvider().Applied, "arm provider change");
            Expect(session.ClickGraphSource(session1[1].Provider.Key.Canonical, inputs).Outcome ==
                UI.CastingGraphClickOutcome.ProviderChanged, "The explicit copy choice was refused.");

            // A load: new objects, new tokens, and the copies may list in any order.
            LiveItemCopy[] loaded = party("session-item-c", "session-item-d");
            CastingWorkspaceInputs after = ItemInputs(loaded);
            var reopened = new UI.CastingWorkspaceSession(directory, ItemPlanCampaign);
            ResolvedCasting resolved = reopened.CompilePlan(after).CastingById("cast-1");
            Expect(resolved.IsExecutable && resolved.Provider.Canonical == loaded[1].Provider.Key.Canonical &&
                resolved.Cost.Single().PoolKey == loaded[1].Provider.ResourcePoolKey,
                "The explicit copy choice did not survive a load.");

            CastingPlanDocument two = ItemPlan(
                SavedGroupCasting("cast-1", 0, ItemSlotRule(Type, "Ring2"), "unit-sayan"),
                SavedGroupCasting("cast-2", 1, ItemSlotRule(Type, "Ring2"), "unit-sayan"));
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(two, after.Snapshot,
                after.ProviderOptions, after.EffectsBySource, new CastEnhancementSnapshot[0]);
            Expect(plan.CastingById("cast-1").IsExecutable && !plan.CastingById("cast-2").IsExecutable &&
                plan.CastingById("cast-2").Cost.Count == 0 &&
                plan.Castings.All(casting => casting.Cost.All(line =>
                    line.PoolKey != loaded[0].Provider.ResourcePoolKey)),
                "Identical copies merged their charges.");
        }

        private static void TestItemSourceCopiesAcrossCasters()
        {
            AbilityKey rule = ItemRule("signet");
            LiveItemCopy sayan = Copy("unit-sayan", rule, "session-item-s", 1);
            LiveItemCopy raine = Copy("unit-raine", rule, "session-item-r", 1);
            CastingWorkspaceInputs inputs = ItemInputs(sayan, raine);
            CastingPlanDocument document = ItemPlan(
                SavedGroupCasting("cast-1", 0, rule, "unit-sayan"),
                SavedGroupCasting("cast-2", 1, rule, "unit-sayan"),
                SavedGroupCasting("cast-3", 2, rule, "unit-raine"));
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document, inputs.Snapshot,
                inputs.ProviderOptions, inputs.EffectsBySource, new CastEnhancementSnapshot[0]);
            Expect(plan.CastingById("cast-1").Provider.Canonical == sayan.Provider.Key.Canonical &&
                plan.CastingById("cast-3").Provider.Canonical == raine.Provider.Key.Canonical,
                "A casting used another caster's copy.");
            Expect(plan.CastingById("cast-1").IsExecutable && !plan.CastingById("cast-2").IsExecutable &&
                plan.CastingById("cast-3").IsExecutable,
                "Sayan borrowed a charge from Raine's copy.");
        }

        private static void TestItemSourceSiblingAbilities()
        {
            ItemEntity item;
            AbilityData first = ItemSourceFixture(2, out item);
            ItemEntity unused;
            AbilityData second = NativeItemAbility("sibling", 2, out unused);
            SetNative(second.Blueprint, "m_AssetGuid", "90e59f4a4ada87243b7b3535a06d0639");
            ItemEntity loaded = ReloadItemThroughSaveContract(item, item.Blueprint);
            AbilityData firstLoaded = ItemAbilityAfterLoad(loaded, first);
            AbilityData secondLoaded = ItemAbilityAfterLoad(loaded, second);
            KingmakerFactResource firstResource = ReadNativeFact(firstLoaded);
            KingmakerFactResource secondResource = ReadNativeFact(secondLoaded);
            Expect(firstResource.Pool.PoolKey == secondResource.Pool.PoolKey,
                "One reconstructed item produced two charge pools.");
            ProviderSnapshot a = ItemSourceProvider(firstLoaded, firstResource);
            ProviderSnapshot b = ItemSourceProvider(secondLoaded, secondResource);
            UnitSnapshot[] units = new[] { "unit-cleric", "unit-t1" }.Select(id => new UnitSnapshot(id, id, false,
                string.Empty, new TargetValidationSnapshot(true, true, true, true))).ToArray();
            var snapshot = new PartyProviderSnapshot(units, new[] { a, b }, new[] { firstResource.Pool });
            ProviderPlanningOption[] options = new[] { a, b }.Select(provider => new ProviderPlanningOption(
                provider, units.Select(unit => unit.UnitId), new[] { "unit-cleric" }, 1, 10)).ToArray();
            EffectExpression expression = Leaf("signet-buff");
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { "source-a", expression }, { "source-b", expression },
                { a.Key.Ability.Canonical, expression }, { b.Key.Ability.Canonical, expression }
            };
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                DirectCasting("c1", "long", "unit-cleric", "unit-t1", "source-a", a.Key.Ability),
                DirectCasting("c2", "long", "unit-cleric", "unit-cleric", "source-b", b.Key.Ability),
                DirectCasting("c3", "long", "unit-cleric", "unit-cleric", "source-a", a.Key.Ability)),
                snapshot, options, effects, new CastEnhancementSnapshot[0]);
            Expect(plan.CastingById("c1").IsExecutable && plan.CastingById("c2").IsExecutable &&
                !plan.CastingById("c3").IsExecutable,
                "Sibling abilities did not share the item's real charge budget.");
        }

        private static void TestItemSourceTransitions()
        {
            const string Type = "signet";
            CastingPlanDocument plain = ItemPlan(SavedGroupCasting("cast-1", 0, ItemRule(Type), "unit-sayan"));
            CastingPlanDocument slotted = ItemPlan(
                SavedGroupCasting("cast-1", 0, ItemSlotRule(Type, "Ring2"), "unit-sayan"));
            LiveItemCopy unrelated = Copy("unit-tias", ItemRule("other-item"), "session-item-x", 3);

            ResolvedCasting removed = CompileOneItem(plain, ItemInputs(unrelated));
            Expect(removed.Provider == null && removed.ReadinessReasons.Contains(
                ItemSourceResolution.Missing + ":unit-sayan") &&
                !removed.ReadinessReasons.Any(reason => reason.StartsWith("caster-not-capable", StringComparison.Ordinal)),
                "An unequipped source was not reported as a missing saved item.");

            ResolvedCasting transferred = CompileOneItem(plain, ItemInputs(
                Copy("unit-raine", ItemRule(Type), "session-item-moved", 3)));
            Expect(transferred.Provider == null && transferred.CasterUnitId == "unit-sayan" &&
                transferred.ReadinessReasons.Contains(ItemSourceResolution.Missing + ":unit-sayan"),
                "A transferred item silently moved the casting to its new owner.");

            LiveItemCopy replacement = Copy("unit-sayan", ItemRule(Type), "session-item-new-copy", 2);
            ResolvedCasting replaced = CompileOneItem(plain, ItemInputs(replacement));
            Expect(replaced.IsExecutable && replaced.Provider.Canonical == replacement.Provider.Key.Canonical,
                "Another equipped copy of the type did not satisfy the rule.");

            ResolvedCasting otherType = CompileOneItem(plain, ItemInputs(
                Copy("unit-sayan", ItemRule("other-item"), "session-item-other", 3)));
            Expect(otherType.Provider == null, "A different item type satisfied the rule.");

            LiveItemCopy occupant = Copy("unit-sayan", ItemRule(Type), "session-item-one", 3, Type, "Ring2");
            ResolvedCasting kept = CompileOneItem(slotted, ItemInputs(occupant));
            Expect(kept.IsExecutable && kept.Provider.Canonical == occupant.Provider.Key.Canonical,
                "The named slot's occupant no longer satisfied the slot rule.");

            ResolvedCasting moved = CompileOneItem(slotted, ItemInputs(
                Copy("unit-sayan", ItemRule(Type), "session-item-one", 3, Type, "Ring1")));
            Expect(moved.Provider == null && moved.ReadinessReasons.Contains(ItemSourceResolution.SlotEmpty + ":Ring2"),
                "A copy in another slot silently satisfied the slot rule.");
        }

        private static void TestItemSourceReassignment(string root, SourceKind oldKind)
        {
            var oldAbility = new AbilityKey(ItemSourceAbilityGuid, string.Empty, 0, oldKind,
                oldKind == SourceKind.Item ? "session-item-dc140b0e464a45e69a8479e619fb1094" : string.Empty);
            AbilityKey rule = ItemRule("signet");
            LiveItemCopy live = Copy("unit-sayan", rule, "session-item-6cda24df", 3);
            CastingWorkspaceInputs inputs = ItemInputs(live);
            PlannedCasting original = SavedGroupCasting("cast-4", 1, oldAbility, "unit-sayan");
            string directory = StoreItemPlan(root, "reassign-" + oldKind, ItemPlan(
                SavedGroupCasting("cast-0", 0, rule, "unit-sayan"), original));
            UI.CastingWorkspaceSession session = OpenItemPlan(directory, inputs);
            string before = session.DocumentIntentSignature();
            ResolvedCasting stale = session.CompilePlan(inputs).CastingById("cast-4");
            Expect(stale.Provider == null && stale.ReadinessReasons.Any(reason => reason.StartsWith(
                oldKind == SourceKind.Item ? ItemSourceResolution.LegacySession : ItemSourceResolution.LegacyFact,
                StringComparison.Ordinal)) && session.DocumentIntentSignature() == before,
                "An old selection was silently reinterpreted or misreported.");

            session.FocusGraphCasting("cast-4");
            Expect(session.ClickGraphSource(live.Provider.Key.Canonical, inputs).Outcome ==
                UI.CastingGraphClickOutcome.DraftConfigured && session.DocumentIntentSignature() == before,
                "An ordinary source-row click repaired the focused casting implicitly.");
            session.FocusGraphCasting("cast-4");
            Expect(session.BeginChangeProvider().Applied, "arm provider change");
            Expect(session.ClickGraphSource(live.Provider.Key.Canonical, inputs).Outcome ==
                UI.CastingGraphClickOutcome.ProviderChanged, "The explicit reassignment was refused.");
            PlannedCasting repaired = session.Document.Castings.Single(value => value.CastingId == "cast-4");
            Expect(repaired.Ability.Equals(rule) && repaired.RoutineId == original.RoutineId &&
                repaired.Order == original.Order && repaired.CasterUnitId == original.CasterUnitId &&
                repaired.TargetMode == original.TargetMode && repaired.Origin.IsCasterCentered &&
                repaired.ExistingEffectPolicy == original.ExistingEffectPolicy &&
                repaired.IgnoredPresenceMarkers.SequenceEqual(original.IgnoredPresenceMarkers) &&
                repaired.Enhancements.Single().EnhancementId == "optional-rod" &&
                !repaired.Enhancements.Single().Required &&
                repaired.TargetingModifiers.Single().ModifierId == "unused-modifier" &&
                repaired.State == original.State && session.Document.Castings.Count == 2,
                "Reassignment changed more than the source.");
            Expect(!session.IsDirty, "The reassignment was not saved.");

            Expect(session.Undo() && session.DocumentIntentSignature() == before, "Undo did not restore the old source.");
            Expect(new UI.CastingWorkspaceSession(directory, ItemPlanCampaign).DocumentIntentSignature() == before,
                "Undo was not persisted.");
            session.FocusGraphCasting("cast-4");
            session.BeginChangeProvider();
            session.ClickGraphSource(live.Provider.Key.Canonical, inputs);

            LiveItemCopy reloaded = Copy("unit-sayan", rule, "session-item-after-repair", 3);
            CastingWorkspaceInputs after = ItemInputs(reloaded);
            ResolvedCasting resolved = new UI.CastingWorkspaceSession(directory, ItemPlanCampaign)
                .CompilePlan(after).CastingById("cast-4");
            Expect(resolved.IsExecutable && resolved.Provider.Canonical == reloaded.Provider.Key.Canonical,
                "The one-time repair did not survive the next load: " +
                string.Join(",", resolved.ReadinessReasons.ToArray()));
        }

        private static void TestItemSourceCorruptSelections()
        {
            CastingWorkspaceInputs inputs = ItemInputs(Copy("unit-sayan", ItemRule("signet"), "session-item-live", 3));
            foreach (string corrupt in new[] { string.Empty, "equipped-item:", "equipped-item:signet@", "equipped-item:signet@Pocket", "garbage" })
            {
                var ability = new AbilityKey(ItemSourceAbilityGuid, string.Empty, 0, SourceKind.Item, corrupt);
                ResolvedCasting casting = CompileOneItem(ThroughPlanFile(ItemPlan(
                    SavedGroupCasting("cast-1", 0, ability, "unit-sayan"))), inputs);
                Expect(casting.Provider == null && casting.ReadinessReasons.Contains(
                    ItemSourceResolution.Invalid + ":unit-sayan"),
                    "A corrupt selection was not refused: '" + corrupt + "'.");
            }
            Expect(ItemSourceSelector.ForDiscoveredItem("x", "Ring2", 1) == ItemSourceSelector.ForEquippedType("x") &&
                ItemSourceSelector.ForDiscoveredItem("x", "Ring2", 2) == ItemSourceSelector.ForEquippedSlot("x", "Ring2") &&
                ItemSourceSelector.ForDiscoveredItem("x", null, 2) == ItemSourceSelector.ForEquippedType("x"),
                "Discovery published the wrong selection rule.");
            Expect(ItemSourceSelector.Parse(ItemSourceSelector.ForEquippedSlot("x", "Quick3")).Kind ==
                ItemSelectionKind.EquippedSlot && ItemSourceSelector.SlotLabel("Set1Secondary") == "Weapon set 2 off hand" &&
                ItemSourceSelector.Parse("equipped-item:x").ItemBlueprintGuid == "x",
                "A valid selector did not parse.");
        }

        private static void TestItemSourceWording(string root)
        {
            var old = new AbilityKey(ItemSourceAbilityGuid, string.Empty, 0, SourceKind.Item, "session-item-old");
            CastingWorkspaceInputs inputs = ItemInputs(Copy("unit-sayan", ItemRule("signet"), "session-item-live", 3));
            string directory = StoreItemPlan(root, "wording", ItemPlan(SavedGroupCasting("cast-4", 0, old, "unit-sayan")));
            UI.CastingWorkspaceSession session = OpenItemPlan(directory, inputs);
            session.FocusGraphCasting("cast-4");
            UI.CastingGraphView view = session.BuildGraph(inputs);
            UI.CastingGraphCasting chip = view.CastingById("cast-4");
            Expect(chip.NeedsItemSourceChoice && chip.StatusLabel == UI.CastingGraphText.ItemSourceNeeded &&
                string.IsNullOrEmpty(chip.SourceProviderKey),
                "An unresolved item source was not shown as needing a source.");
            Expect(view.CasterById("unit-sayan").Note.Contains("Change caster/source") &&
                view.Inspector.ItemSourceNote.Contains("Change caster/source") &&
                view.Inspector.Reasons.All(reason => !reason.Contains("cannot cast")),
                "The unresolved source was not explained with its repair.");
            foreach (string code in new[] { ItemSourceResolution.Missing, ItemSourceResolution.Ambiguous,
                ItemSourceResolution.SlotEmpty, ItemSourceResolution.LegacySession, ItemSourceResolution.LegacyFact,
                ItemSourceResolution.Invalid })
            {
                string text = UI.WorkspaceReasonText.Describe(code + ":detail");
                Expect(text.Contains("Change caster/source") && !text.Contains("cannot cast") &&
                    !text.Contains("no casts left") && !text.Contains("prepared"),
                    "Reason text for " + code + " is not truthful: " + text);
            }
            Expect(UI.WorkspaceReasonText.Describe("resource-pool-exhausted:item") == "no casts left",
                "Exhausted charges changed meaning.");

            CastingWorkspaceInputs resolvedInputs = ItemInputs(Copy("unit-sayan", ItemRule("signet"), "session-item-live", 3));
            string resolvedDirectory = StoreItemPlan(root, "wording-resolved",
                ItemPlan(SavedGroupCasting("cast-4", 0, ItemRule("signet"), "unit-sayan")));
            UI.CastingWorkspaceSession resolvedSession = OpenItemPlan(resolvedDirectory, resolvedInputs);
            resolvedSession.FocusGraphCasting("cast-4");
            UI.CastingGraphView resolvedView = resolvedSession.BuildGraph(resolvedInputs);
            Expect(resolvedView.Inspector.ItemSourceNote.Contains("whichever copy") &&
                !resolvedView.Inspector.ItemSourceNote.Contains("select again") &&
                resolvedView.Catalogue.All(entry => !entry.Label.Contains("select again")),
                "A resolved source kept the old reselection warning or hid its rule.");
        }

        private static void TestItemSourceDiagnostics(string root)
        {
            var old = new AbilityKey(ItemSourceAbilityGuid, string.Empty, 0, SourceKind.Item, "session-item-old");
            CastingWorkspaceInputs inputs = ItemInputs(Copy("unit-sayan", ItemRule("signet"), "session-item-live", 3));
            string directory = StoreItemPlan(root, "diagnostics", ItemPlan(SavedGroupCasting("cast-4", 0, old, "unit-sayan")));
            UI.CastingWorkspaceSession session = OpenItemPlan(directory, inputs);
            var lines = new List<string>();
            session.DiagnosticLog = lines.Add;
            session.CompilePlan(inputs);
            session.BuildGraph(inputs);
            session.BuildGraph(ItemInputs(Copy("unit-sayan", ItemRule("signet"), "session-item-live", 3)));
            Expect(lines.Count == 1 && lines[0].StartsWith("[KBP-ITEM-SOURCE] resolution;casting=cast-4", StringComparison.Ordinal) &&
                lines[0].Contains("caster=unit-sayan") && lines[0].Contains("saved=Item:session-item-old") &&
                lines[0].Contains(ItemSourceResolution.LegacySession) && lines[0].Contains("session-item-live"),
                "Diagnostics did not report exactly one complete line: " + string.Join(" || ", lines.ToArray()));
            session.FocusGraphCasting("cast-4");
            session.BeginChangeProvider();
            session.ClickGraphSource(inputs.ProviderOptions.Single().Provider.Key.Canonical, inputs);
            Expect(lines.Any(line => line.Contains("reassigned;casting=cast-4") && line.Contains("from=Item:session-item-old")),
                "The explicit reassignment was not recorded.");
        }

        private static void TestItemSourceExecutionBinding()
        {
            ItemEntity item;
            AbilityData before = ItemSourceFixture(3, out item);
            CastingPlanDocument saved;
            ExplicitCastingPlan plan;
            CastingApplyDecision decision;
            NativeFactProjection(before, 1, out saved, out plan, out decision);
            ItemEntity loaded = ReloadItemThroughSaveContract(item, item.Blueprint);
            AbilityData after = ItemAbilityAfterLoad(loaded, before);
            CastingPlanDocument retained;
            ExplicitStepConversion projection = NativeFactProjection(after, 1, out retained, out plan, out decision, saved);
            Expect(projection.Converted, "The reconstructed item did not project: " + projection.Refusal);
            var runtime = new NativeFactRuntime(after);
            CastingExecutionHost host = NativeFactHost(runtime, false);
            new NativeCastingDispatchBoundary(host, () => null).Submit(plan, decision, "long", projection);
            while (host.IsRunning) host.Pump();
            Expect(host.LastReport.Succeeded && runtime.Submissions == 1 && loaded.Charges == 2 && item.Charges == 3,
                "Discovery, reservation, binding and native spending disagreed about the live item.");
            KingmakerFactResource live = ReadNativeFact(after);
            ProviderSnapshot provider = ItemSourceProvider(after, live);
            ItemEntity otherItem;
            AbilityData other = ItemSourceFixture(3, out otherItem);
            Expect(KingmakerFactResources.ValidateBinding("unit-cleric", other, other, provider.Key, null) != null,
                "Execution bound to a different live copy than the one planned.");
        }

        private static void TestItemSourceSlotBinding()
        {
            ItemEntity item;
            AbilityData data = ItemSourceFixture(3, out item);
            var body = NativeObject<UnitBody>();
            var ringOne = NativeObject<EquipmentSlot<BlueprintItemEquipmentRing>>();
            var ringTwo = NativeObject<EquipmentSlot<BlueprintItemEquipmentRing>>();
            SetNative(body, "Ring1", ringOne);
            SetNative(body, "Ring2", ringTwo);
            SetNative(data.Caster, "Body", body);
            SetNative(item, "HoldingSlot", ringTwo);
            Expect(KingmakerFactResources.EquippedSlotId(data.Caster, item) == "Ring2", "The worn slot was not located.");
            KingmakerFactResource resource = ReadNativeFact(data);
            var key = new ProviderKey("unit-cleric", string.Empty, ItemSlotRule(ItemSourceItemGuid, "Ring2"), resource.InstanceId);
            Expect(KingmakerFactResources.ValidateBinding("unit-cleric", data, data, key, null) == null,
                "The slot rule refused the item in its slot.");
            SetNative(item, "HoldingSlot", ringOne);
            Expect(KingmakerFactResources.ValidateBinding("unit-cleric", data, data, key, null) != null,
                "The slot rule bound an item in another slot.");
        }
    }
}
