using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // Detached installed-assembly objects exercise native property/count/
        // Spend implementations. They do not simulate Rulebook or claim live
        // effects. No Unity native constructor or live Game state is invoked.
        private static T NativeObject<T>() where T : class
        {
            var value = (T)FormatterServices.GetUninitializedObject(typeof(T));
            if (value is UnityEngine.Object) SetNative(value, "m_CachedPtr", new IntPtr(1));
            return value;
        }

        private static void SetNative(object value, string name, object content)
        {
            for (Type type = value.GetType(); type != null; type = type.BaseType)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                FieldInfo field = type.GetField(name, flags) ??
                    type.GetField("<" + name + ">k__BackingField", flags);
                if (field != null) { field.SetValue(value, content); return; }
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null && property.GetSetMethod(true) != null)
                { property.SetValue(value, content, null); return; }
            }
            throw new InvalidOperationException("Native fixture member absent: " + value.GetType() + "." + name);
        }

        private static AbilityData NativeItemAbility(string itemId, int charges,
            out ItemEntity item, bool spendCharges = true)
        {
            var blueprint = NativeObject<BlueprintAbility>();
            SetNative(blueprint, "m_AssetGuid", "11111111111111111111111111111111");
            SetNative(blueprint, "Components", new BlueprintComponent[0]);
            SetNative(blueprint, "MaterialComponent", new BlueprintAbility.MaterialComponentData());
            var equipment = NativeObject<BlueprintItemEquipmentUsable>();
            equipment.Ability = blueprint;
            equipment.SpendCharges = spendCharges;
            equipment.Charges = 5;
            equipment.RestoreChargesOnRest = true;
            item = NativeObject<ItemEntityUsable>();
            SetNative(item, "m_Blueprint", equipment);
            SetNative(item, "m_Count", 1);
            SetNative(item, "Charges", charges);
            var fact = NativeObject<Ability>();
            SetNative(fact, "Blueprint", blueprint);
            SetNative(fact, "SourceItem", item);
            var caster = NativeObject<UnitDescriptor>();
            var state = NativeObject<UnitState>();
            var features = NativeObject<UnitMechanicsFeatures>();
            SetNative(features, "HandOfMagusDan", new Kingmaker.Utility.CountableFlag());
            SetNative(state, "Features", features);
            SetNative(caster, "State", state);
            var data = NativeObject<AbilityData>();
            SetNative(data, "Blueprint", blueprint);
            SetNative(data, "Caster", caster);
            SetNative(data, "Fact", fact);
            return data;
        }

        private static KingmakerFactResource ReadNativeFact(AbilityData data)
        {
            KingmakerFactResource result;
            string reason;
            if (!KingmakerFactResources.TryRead("unit-cleric", data, data, out result, out reason))
                throw new InvalidOperationException("Native fixture refused: " + reason);
            return result;
        }

        private static ProviderSnapshot FactFixtureProvider(AbilityData data, KingmakerFactResource resource,
            string variant = "")
        {
            return new ProviderSnapshot(new ProviderKey("unit-cleric", string.Empty,
                new AbilityKey(data.Blueprint.AssetGuid, variant, 0, resource.Kind,
                    resource.Kind == SourceKind.Item ? resource.InstanceId : string.Empty),
                resource.InstanceId), "Native item fixture", 0, resource.Pool.PoolKey, resource.Cost, null);
        }

        private static void TestNativeFactResources()
        {
            ItemEntity item;
            AbilityData data = NativeItemAbility("item-one", 3, out item);
            // The baseline's exact predicate was false => free, despite 3
            // native casts. This fixture establishes the contradiction without
            // identifying the reporter's item or compatibility profile.
            if (data.Resource != null || data.GetAvailableForCastCount() != 3)
                throw new InvalidOperationException("Finite-without-resource equivalent did not reproduce.");
            KingmakerFactResource resource = ReadNativeFact(data);
            ProviderSnapshot provider = FactFixtureProvider(data, resource);
            ItemEntity unusedItem;
            AbilityData siblingData = NativeItemAbility("sibling", 3, out unusedItem);
            SetNative(siblingData.Blueprint, "m_AssetGuid", "22222222222222222222222222222222");
            SetNative(siblingData.Fact, "SourceItem", item);
            KingmakerFactResource siblingResource = ReadNativeFact(siblingData);
            ProviderSnapshot sibling = FactFixtureProvider(siblingData, siblingResource);
            if (resource.Kind != SourceKind.Item || resource.Cost != 1 ||
                resource.Pool.Kind != ResourcePoolKind.ItemCharges || resource.Pool.Remaining != 3 ||
                !resource.InstanceId.StartsWith("session-item-", StringComparison.Ordinal) ||
                ReadNativeFact(data).InstanceId != resource.InstanceId)
                throw new InvalidOperationException("Native item was not offered with its real shared charge pool.");
            if (siblingResource.Pool.PoolKey != resource.Pool.PoolKey || siblingResource.InstanceId != resource.InstanceId)
                throw new InvalidOperationException("Two native abilities created separate budgets for one item.");
            var ledger = new ResourceLedger(new[] { resource.Pool });
            for (int i = 0; i < 3; i++)
            {
                ResourceReservation reservation;
                string reason;
                if (!ledger.TryReserve(i == 1 ? sibling : provider, out reservation, out reason) ||
                    reservation.Unlimited || reservation.Units != 1 ||
                    KingmakerFactResources.ValidateBinding("unit-cleric", i == 1 ? siblingData : data,
                        i == 1 ? siblingData : data, i == 1 ? sibling.Key : provider.Key, reservation) != null)
                    throw new InvalidOperationException("A legitimate item reservation/binding failed: " + reason);
                (i == 1 ? siblingData : data).Spend();
                if (item.Charges != 2 - i || data.GetAvailableForCastCount() != 2 - i)
                    throw new InvalidOperationException("Native Spend did not debit the exact source item once.");
            }
            ResourceReservation excess;
            string shortage;
            if (ledger.TryReserve(sibling, out excess, out shortage))
                throw new InvalidOperationException("Sibling abilities double-counted the item budget.");
            ItemEntity otherItem;
            AbilityData other = NativeItemAbility("item-two", 3, out otherItem);
            if (KingmakerFactResources.ValidateBinding("unit-cleric", other, other, provider.Key, null) == null)
                throw new InvalidOperationException("A same-ability different item silently replaced the selection.");
            SetNative(item, "m_Count", 2);
            KingmakerFactResource unsupported;
            string refusal;
            if (KingmakerFactResources.TryRead("unit-cleric", data, data, out unsupported, out refusal) ||
                unsupported != null || !refusal.Contains("unsupported"))
                throw new InvalidOperationException("An unsupported stack was offered as free.");
            ItemEntity unlimitedItem;
            AbilityData unlimited = NativeItemAbility("unlimited-item", 0, out unlimitedItem, false);
            KingmakerFactResource free = ReadNativeFact(unlimited);
            if (unlimited.GetAvailableForCastCount() != -1 || free.Cost != 0 ||
                free.Pool.Kind != ResourcePoolKind.Unlimited)
                throw new InvalidOperationException("A genuine unlimited native item lost its contract.");
        }

        private static void TestResourceFinalBoundary()
        {
            var free = new ResourceReservation("free", 0, new string[0], true);
            foreach (int? count in new int?[] { null, 0, 1, 5, -2 })
                if (AvailableCountJudgement.BeforeSubmission(free, count) == null)
                    throw new InvalidOperationException("Unlimited reservation accepted stale/unread state: " + count);
            if (AvailableCountJudgement.BeforeSubmission(free, -1) != null ||
                AvailableCountJudgement.BeforeSubmission(new ResourceReservation("paid", 2, new string[0]), 1) != null)
                throw new InvalidOperationException("Cast counts were confused with cost units.");
            foreach (bool animated in new[] { false, true })
            foreach (bool unread in new[] { false, true })
            {
                ItemEntity item;
                AbilityData data = NativeItemAbility("changing", 3, out item, false);
                ExplicitCastingPlan plan;
                CastingApplyDecision decision;
                CastingPlanDocument document;
                ExplicitStepConversion projection = NativeFactProjection(data, 2, out document, out plan, out decision);
                var runtime = new NativeFactRuntime(data)
                {
                    BeforeSubmit = () =>
                    {
                        if (unread)
                        {
                            SetNative(data.Fact, "Blueprint", null);
                            SetNative(data, "Blueprint", null);
                            if (KingmakerFactResources.KingmakerCount(data).HasValue)
                                throw new InvalidOperationException("Unread native equivalent still supplied a count.");
                        }
                        else ((BlueprintItemEquipment)item.Blueprint).SpendCharges = true;
                    }
                };
                var host = NativeFactHost(runtime, animated);
                new NativeCastingDispatchBoundary(host, () => null).Submit(plan, decision, "long", projection);
                while (host.IsRunning) host.Pump();
                if (!host.LastReport.Halted || host.LastReport.Submitted != 0 ||
                    runtime.Submissions != 0 || item.Charges != 3 || runtime.LeasesDisposed != 1 ||
                    runtime.LeasesAcquired != 1 || runtime.Effects.Count != 0 || host.LastReport.NotProcessed != 1)
                    throw new InvalidOperationException("Final mismatch submitted/replayed/leaked: animated=" +
                        animated + ";unread=" + unread);
            }
        }

        private static ExplicitStepConversion NativeFactProjection(AbilityData data, int count,
            out CastingPlanDocument document, out ExplicitCastingPlan plan, out CastingApplyDecision decision,
            CastingPlanDocument saved = null)
        {
            KingmakerFactResource resource = ReadNativeFact(data);
            ProviderSnapshot provider = FactFixtureProvider(data, resource);
            var units = new[] { "unit-cleric", "unit-t1", "unit-t2", "unit-t3" }
                .Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToArray();
            var snapshot = new PartyProviderSnapshot(units, new[] { provider }, new[] { resource.Pool });
            var options = new[] { new ProviderPlanningOption(provider, units.Select(unit => unit.UnitId),
                new[] { "unit-cleric" }, 1, 10) };
            EffectExpression expression = Leaf("native-buff");
            var effects = new Dictionary<string, EffectExpression>
            {
                { "native-source", expression }, { provider.Key.Ability.Canonical, expression }
            };
            document = saved ?? CastingDocument(Enumerable.Range(0, count).Select(index =>
                DirectCasting("native-cast-" + index, "long", "unit-cleric", "unit-t" + (index % 3 + 1),
                    "native-source", provider.Key.Ability)).ToArray());
            // Existing persistence format already retains the entire ability
            // key, including the item token. No migration/rebinding guess.
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(CastingPlanProfile.FromDocument(document));
            document = Newtonsoft.Json.JsonConvert.DeserializeObject<CastingPlanProfile>(json).ToDocument();
            if (saved == null && document.Castings.Any(casting => casting.Ability.SpecialSourceId != resource.InstanceId))
                throw new InvalidOperationException("Saved item selection lost its exact identity.");
            plan = new ExplicitCastingCompiler().Compile(document, snapshot, options, effects,
                new CastEnhancementSnapshot[0]);
            decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
            return ExplicitCastingStepConverter.Convert(plan, decision, options, effects);
        }

        private static CastingExecutionHost NativeFactHost(NativeFactRuntime runtime, bool animated)
        {
            return new CastingExecutionHost(settings => animated
                ? (ICastExecutor)new AnimatedCastExecutor(runtime, true)
                : new InstantCastExecutor(runtime, true), () => 0);
        }

        private static void TestNativeFactRoutine()
        {
            foreach (bool animated in new[] { false, true })
            foreach (bool unlimited in new[] { false, true })
            foreach (bool shortage in new[] { false, true })
            {
                ItemEntity item;
                AbilityData data = NativeItemAbility("routine", shortage ? 2 : 5, out item, !unlimited);
                ExplicitCastingPlan plan;
                CastingApplyDecision decision;
                CastingPlanDocument document;
                ExplicitStepConversion projection = NativeFactProjection(data, 5, out document, out plan, out decision);
                var runtime = new NativeFactRuntime(data);
                var host = NativeFactHost(runtime, animated);
                if (shortage && !unlimited)
                {
                    CastingDispatchOutcome rejected = new NativeCastingDispatchBoundary(host, () => null)
                        .Submit(plan, decision, "long", projection);
                    if (decision.Allowed || projection.Converted || rejected.Submitted || host.IsRunning ||
                        runtime.Submissions != 0 || item.Charges != 2)
                        throw new InvalidOperationException("Require full plan accepted a known item shortage.");
                    continue;
                }
                if (!projection.Converted) throw new InvalidOperationException("Native routine did not compile: " + projection.Refusal);
                new NativeCastingDispatchBoundary(host, () => null).Submit(plan, decision, "long", projection);
                while (host.IsRunning) host.Pump();
                string text = CastingRunPresentation.Describe(host.LastReport, "Long", null);
                if (!host.LastReport.Succeeded || host.LastReport.Confirmed != 5 || runtime.Submissions != 5 ||
                    runtime.Effects.Count != 3 || item.Charges != (unlimited ? (shortage ? 2 : 5) : 0) ||
                    host.LastReport.ResourcesSpent != (unlimited ? 0 : 5) ||
                    (unlimited && !text.Contains("plus 5 free")) ||
                    host.LastReport.Entries.Any(entry => entry.PlannedUnlimited != unlimited ||
                        entry.PlannedNativeCost != (unlimited ? 0 : 1)))
                    throw new InvalidOperationException("Native budgeted routine failed: " + text);
            }
        }

        private static void TestNativeFactSavedSelection()
        {
            ItemEntity item;
            AbilityData selected = NativeItemAbility("selected", 5, out item);
            CastingPlanDocument saved;
            ExplicitCastingPlan plan;
            CastingApplyDecision decision;
            NativeFactProjection(selected, 2, out saved, out plan, out decision);
            CastingPlanDocument rescanned;
            NativeFactProjection(selected, 2, out rescanned, out plan, out decision, saved);
            if (!decision.Allowed) throw new InvalidOperationException("A rescan lost the selected live item.");
            ItemEntity replacement;
            AbilityData other = NativeItemAbility("replacement", 5, out replacement);
            CastingPlanDocument retained;
            NativeFactProjection(other, 2, out retained, out plan, out decision, saved);
            if (decision.Allowed || retained.Castings.Count != 2 ||
                retained.Castings.Any(casting => casting.Ability.SpecialSourceId != saved.Castings[0].Ability.SpecialSourceId) ||
                retained.Castings[1].DirectTargetUnitId != saved.Castings[1].DirectTargetUnitId ||
                plan.Castings.Any(casting => casting.IsExecutable) || replacement.Charges != 5)
                throw new InvalidOperationException("An item replacement rebound/deleted saved intent.");
            // v0.6.6 stored this source as a resource-less Fact. Keep that
            // request, but require the owner to choose the correctly priced
            // Item provider; never migrate it to an arbitrary identical spell.
            CastingPlanDocument old = CastingDocument(saved.Castings.Select(casting => casting.WithProvider(
                casting.CasterUnitId, new AbilityKey(casting.Ability.BaseAbilityGuid, string.Empty, 0,
                    SourceKind.Fact, string.Empty), null, null)).ToArray());
            NativeFactProjection(selected, 2, out retained, out plan, out decision, old);
            if (decision.Allowed || retained.Castings.Count != 2 ||
                retained.Castings.Any(casting => casting.Ability.SourceKind != SourceKind.Fact))
                throw new InvalidOperationException("An old free Fact selection was silently replaced.");
        }

        // Only the rule/command/effect boundary is simulated. Classification,
        // reservations, exact native binding, native Spend, executors and
        // shared reports are production code.
        private sealed class NativeFactRuntime : IInstantCastRuntimeAdapter, ICastRuntimeAdapter,
            ICastEnhancementRuntimeAdapter
        {
            private readonly AbilityData _data;
            internal NativeFactRuntime(AbilityData data) { _data = data; }
            internal Action BeforeSubmit;
            internal Action AfterValidation;
            internal bool FailAfterSpend;
            internal bool ThrowEffectRead;
            internal int Submissions;
            internal int LeasesAcquired;
            internal int LeasesDisposed;
            internal readonly HashSet<string> Effects = new HashSet<string>(StringComparer.Ordinal);
            public bool IsInCombat { get { return false; } }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            {
                LeasesAcquired++;
                return CastEnhancementPreparation.Pass(new FactFixtureLease(() => LeasesDisposed++));
            }
            public CastRuntimeValidation Validate(CastStep step)
            {
                string refusal = KingmakerFactResources.ValidateBinding("unit-cleric", _data, _data,
                    step.Provider, step.Reservation) ??
                    AvailableCountJudgement.BeforeSubmission(step.Reservation, KingmakerFactResources.KingmakerCount(_data));
                return refusal == null ? CastRuntimeValidation.Pass() : CastRuntimeValidation.Fail(refusal);
            }
            public InstantCastResult Fire(CastStep step)
            {
                if (BeforeSubmit != null) BeforeSubmit();
                CastRuntimeValidation final = Validate(step);
                if (!final.Valid) return new InstantCastResult(false, false, false, false, false, final.Reason);
                if (AfterValidation != null) AfterValidation();
                int? before = KingmakerFactResources.KingmakerCount(_data);
                Submissions++;
                foreach (string target in step.TargetUnitIds) Effects.Add(target);
                _data.Spend();
                int? after = KingmakerFactResources.KingmakerCount(_data);
                return new InstantCastResult(true, !FailAfterSpend, true, AvailableCountJudgement.Spent(before, after),
                    true, "native-source-spend", AvailableCountJudgement.Violation(step.Reservation, before, after));
            }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                InstantCastResult result = Fire(step);
                if (!result.Submitted) throw new InvalidOperationException(result.Detail);
                return new FactFixtureOperation(result, ThrowEffectRead);
            }
            public bool EffectsObserved(CastStep step) { return step.TargetUnitIds.All(Effects.Contains); }
            public InstantCastCompletion InspectCompletion(CastStep step) { return InstantCastCompletion.Settled("fixture"); }
            public InstantCastCompletion Cleanup(CastStep step) { return InstantCastCompletion.Settled("fixture-clean"); }
        }

        private sealed class FactFixtureLease : IDisposable
        {
            private readonly Action _dispose;
            internal FactFixtureLease(Action dispose) { _dispose = dispose; }
            public void Dispose() { _dispose(); }
        }

        private sealed class FactFixtureOperation : IAnimatedCastOperation
        {
            private readonly InstantCastResult _result;
            private readonly bool _throwEffectRead;
            internal FactFixtureOperation(InstantCastResult result, bool throwEffectRead)
            { _result = result; _throwEffectRead = throwEffectRead; }
            public bool IsStarted { get { return true; } }
            public bool IsCompleted { get { return true; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return _result.Succeeded; } }
            public bool EffectsObserved
            {
                get
                {
                    if (_throwEffectRead) throw new InvalidOperationException("effect-reader-unavailable");
                    return _result.EffectsObserved;
                }
            }
            public bool ResourceSpent { get { return _result.ResourceSpent; } }
            public string ResourceCountViolation { get { return _result.ResourceCountViolation; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return _result.Detail; } }
            public void Dispose() { }
        }

        private static void TestUnexpectedSpendReport()
        {
            foreach (bool animated in new[] { false, true })
            {
                ItemEntity item;
                AbilityData data = NativeItemAbility("unexpected", 3, out item, false);
                ExplicitCastingPlan plan;
                CastingApplyDecision decision;
                CastingPlanDocument document;
                ExplicitStepConversion projection = NativeFactProjection(data, 2, out document, out plan, out decision);
                // A deliberately faulty external adapter changes the resource
                // contract after its final validation. Native before/after counts
                // and Spend are real; the production executor's last-resort guard
                // must still halt, even when an effect was reported.
                var runtime = new NativeFactRuntime(data)
                {
                    AfterValidation = () => ((BlueprintItemEquipment)item.Blueprint).SpendCharges = true,
                    FailAfterSpend = animated
                };
                var host = NativeFactHost(runtime, animated);
                new NativeCastingDispatchBoundary(host, () => null).Submit(plan, decision, "long", projection);
                for (int i = 0; host.IsRunning && i < 100; i++) host.Pump();
                CastingRunReport report = host.LastReport;
                if (report == null || !report.Halted || report.Confirmed != 0 || report.ResourcesSpent != 1 ||
                    !report.Entries[0].EffectObserved || report.Entries[0].EffectConfirmed ||
                    (!animated && !report.Entries[0].SpendInvoked) || report.Entries[0].FreeCast || report.NotProcessed != 1 ||
                    !report.Entries[0].PlannedUnlimited || report.Entries[0].PlannedNativeCost != 0 ||
                    item.Charges != 2 || runtime.Submissions != 1)
                    throw new InvalidOperationException("The halt hid observed spend/effect or validated a failed casting.");
                string runText = CastingRunPresentation.Describe(report, "Long", null);
                if (runText.Contains("plus 1 free") || !runText.Contains("observed resource use: 1") ||
                    !runText.Contains("effect observed on 1 unvalidated"))
                    throw new InvalidOperationException("The shared report contradicted the observations: " + runText);
                }
            // Contradiction of a FREE forecast, independent of the host fixture.
            var contradicted = new CastingOutcomeEntry("free", CastingOutcomeState.Failed, true, true, true,
                "unexpected-resource-spent-on-unlimited-source", true, true, true, true);
            var freeReport = new CastingRunReport("run", "long", CastingApplyMode.Ordinary, "p", "halted",
                false, true, new[] { contradicted }, new string[0]);
            string text = CastingRunPresentation.Describe(freeReport, "Long", null);
            if (!contradicted.ResourceSpent || contradicted.FreeCast || !contradicted.PlannedUnlimited ||
                text.Contains("plus 1 free") || !text.Contains("observed resource use: 1") ||
                !text.Contains("effect observed on 1 unvalidated"))
                throw new InvalidOperationException("A free forecast hid observed spending: " + text);

            ItemEntity observerItem;
            AbilityData observerData = NativeItemAbility("observer-failure", 3, out observerItem);
            ExplicitCastingPlan observerPlan;
            CastingApplyDecision observerDecision;
            CastingPlanDocument observerDocument;
            ExplicitStepConversion observerProjection = NativeFactProjection(observerData, 2,
                out observerDocument, out observerPlan, out observerDecision);
            var observerRuntime = new NativeFactRuntime(observerData) { ThrowEffectRead = true };
            var observerHost = NativeFactHost(observerRuntime, true);
            new NativeCastingDispatchBoundary(observerHost, () => null)
                .Submit(observerPlan, observerDecision, "long", observerProjection);
            while (observerHost.IsRunning) observerHost.Pump();
            if (!observerHost.LastReport.Halted || observerHost.LastReport.ResourcesSpent != 1 ||
                observerHost.LastReport.Confirmed != 0 || observerHost.LastReport.Entries[0].EffectObserved ||
                observerHost.LastReport.NotProcessed != 1 || observerItem.Charges != 2 || observerRuntime.Submissions != 1)
                throw new InvalidOperationException("An Animated effect-reader failure hid observed spending or replayed.");
        }

    }
}
