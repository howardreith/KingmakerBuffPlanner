using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.Tests
{
    // Greater Magic Fang repair. Reported: "Greater Magic Fang from the Druid
    // spellbook only apply to the Druid's own pet, not other pets." The
    // native spell targets any friendly unit in Close range; the defect was
    // that discovery required all five enhancement tiers at once, so no cast
    // was ever confirmed and the run stopped after the first pet.
    //
    // These regressions run the production adapter, scanner, admission,
    // targeting, compiler, gate, converter, judgement, both executors and
    // both run halters on the committed native 2.1.7b blueprints. Only the
    // game boundary is simulated: GmfWorld applies what native MagicFang and
    // ContextActionApplyBuff apply (its tier from its own restatement of the
    // native formula) and reports the instances the readers would see.
    internal static partial class Program
    {
        private const string GmfAbility = "f1100650705a69c4384d3edd88ba0f52";
        private const string GmfBuff = "ca90cc4cd9fc592419c5e58f0a9ead64";
        private const string GmfOrdinaryAbility = "403cf599412299a4f9d5d925c7b9fb33";
        private const string GmfOrdinaryBuff = "e7646b1dfdd22ce4ab340f295938ab8e";

        // TemporaryEnhancement1..5, in MagicFangGreater's native array order.
        private static readonly string[] GmfTiers =
        {
            "d704f90f54f813043a525f304f6c0050", "9e9bab3020ec5f64499e007880b37e52",
            "d072b841ba0668846adeb007f623bd6c", "6a6a0901d799ceb49b33d4851ff72132",
            "746ee366e50611146821d61e391edf16"
        };

        // The regressions that read native fixture blueprints (kept by the
        // explicit fixture capture with the missing-beneficial-buffs ones).
        private static readonly KeyValuePair<string, Action>[] GmfNativeRegressions =
        {
            Mbb("greater-magic-fang-discovery-binds-the-native-tier-to-caster-level", TestGmfDiscovery),
            Mbb("greater-magic-fang-replacement-enchantments-follow-array-order", TestGmfReplacementEnchantments),
            Mbb("greater-magic-fang-confirmation-needs-the-cast-tier-and-buff-fresh-on-the-recipient",
                TestGmfConfirmation),
            Mbb("greater-magic-fang-existing-effects-keep-the-casting-policy", TestGmfExistingEffects),
            Mbb("greater-magic-fang-druid-casts-on-pets-of-two-masters-in-either-order", TestGmfTwoPets),
            Mbb("greater-magic-fang-refusals-and-unconfirmed-casts-still-stop-the-run", TestGmfRefusals),
            Mbb("greater-magic-fang-neighbours-keep-their-semantics", TestGmfNeighbours)
        };

        private static void RunGreaterMagicFangTests()
        {
            Run("greater-magic-fang-native-contract-pinned", TestGmfNativeContract);
            foreach (KeyValuePair<string, Action> regression in GmfNativeRegressions)
                Run(regression.Key, regression.Value);
            Run("greater-magic-fang-natural-weapon-observation", TestGmfNaturalWeaponObservation);
        }

        // The oracle, restated from native 2.1.7b and independent of the
        // adapter: MagicFangGreater's ProjectilesCount rank is caster level
        // DivStep 4 clamped to [1, 5] (its ContextRankConfig), and
        // MagicFang.RunAction applies Enchantment[Math.Min(rank, n) - 1].
        private static int GmfNativeRank(int casterLevel)
        {
            return Math.Max(1, Math.Min(5, casterLevel / 4));
        }

        private static NativeBlueprintDump GmfDump()
        {
            return new NativeBlueprintDump(new[] { GmfAbility, GmfBuff, GmfOrdinaryAbility, GmfOrdinaryBuff }
                .Concat(GmfTiers).ToArray());
        }

        private static EffectExpression GmfScan(BlueprintAbility ability)
        {
            return GmfScanResult(ability).Expression;
        }

        private static DiscoveryScanResult GmfScanResult(BlueprintAbility ability)
        {
            return new ActionGraphScanner().Scan(new KingmakerActionGraphAdapter().Adapt(ability));
        }

        private static MagicFang GmfFang(BlueprintAbility ability)
        {
            return (MagicFang)ability.GetComponent<AbilityEffectRunAction>().Actions.Actions
                .Single(action => action is MagicFang);
        }

        // Every leaf as "Kind:id", sorted.
        private static string[] GmfLeaves(EffectExpression expression)
        {
            return EffectApplicationLifetime.Leaves(expression).Select(leaf => leaf.Kind + ":" + leaf.EffectId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static string[] GmfExpected(string buff, string enchantment)
        {
            return new[] { EffectKind.Buff + ":" + buff, EffectKind.WornItemEnchantment + ":" + enchantment }
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        // The raw expression bound for a caster level exactly as a provider
        // option binds it (ProviderTargetingPlanner.CasterFacts is the option
        // builder's caster-fact read).
        private static EffectExpression GmfBind(EffectExpression raw, int casterLevel)
        {
            return EffectConditionBinding.Bind(raw, ProviderTargetingPlanner.CasterFacts(raw, casterLevel, id => null));
        }

        private static List<UnitSnapshot> GmfUnits()
        {
            var valid = new TargetValidationSnapshot(true, true, true, true);
            return new List<UnitSnapshot>
            {
                new UnitSnapshot("druid", "druid", false, string.Empty, valid),
                new UnitSnapshot("ranger", "ranger", false, string.Empty, valid),
                new UnitSnapshot("druid-pet", "druid-pet", true, "druid", valid),
                new UnitSnapshot("ranger-pet", "ranger-pet", true, "ranger", valid)
            };
        }

        private sealed class GmfParty
        {
            internal PartyProviderSnapshot Snapshot;
            internal List<ProviderPlanningOption> Options;
            internal Dictionary<string, EffectExpression> Effects;
            internal ProviderSnapshot Provider;
            internal string SourceId;
            internal ProviderPlanningOption Option { get { return Options.Single(); } }
            internal EffectExpression Raw { get { return Effects[Provider.Key.Ability.Canonical]; } }
        }

        // A Druid who knows the spell, with its spell-level pool. Only the
        // live reads are injected (the spellbook, the slot balance, the
        // caster level and native AbilityData.CanTarget per unit); the
        // provider, its expression, targets and option are production.
        private static GmfParty GmfDruidParty(NativeBlueprintDump dump, string spell, int casterLevel, int slots,
            Func<string, bool> nativeCanTarget = null)
        {
            List<UnitSnapshot> units = GmfUnits();
            PartyProviderAdmission admission = MbbAdmission(dump);
            int spellLevel = spell == GmfAbility ? 3 : 1;
            ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("druid", "book-druid", spellLevel,
                slots, Math.Max(1, slots));
            var providers = new List<ProviderSnapshot>();
            ProviderSnapshot provider = admission.AdmitSpell("druid", "book-druid", false, "Druid",
                MbbRead(dump, spell, spell, SourceKind.Spellbook, casterLevel), spellLevel, 0, pool.PoolKey, 1,
                new string[0], providers);
            MbbExpect(provider != null && providers.Count == 1, "The production admission refused " + spell + ".");
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            EffectExpression expression = effects[provider.Key.Ability.Canonical];
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(spell);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            IReadOnlyDictionary<string, bool> facts = ProviderTargetingPlanner.CasterFacts(expression,
                provider.EffectiveCasterLevel, id => null);
            ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true,
                false, area, nativeCanTarget ?? (id => true), (from, to) => 1f, facts, (unit, fact) => null,
                fact => false);
            return new GmfParty
            {
                Snapshot = new PartyProviderSnapshot(units, providers, new[] { pool }),
                Options = new List<ProviderPlanningOption>
                {
                    ProviderTargetingPlanner.BuildOption(provider, decision, CastExecutionStrategy.DirectRuleCast,
                        "ordinary-direct-rule-cast", facts)
                },
                Effects = effects,
                Provider = provider,
                SourceId = CatalogSourceIdentity.For(provider.Key.Ability, expression)
            };
        }

        private static ActiveEffectSnapshot GmfLive(IDictionary<string, ActiveEffectInstance[]> instances = null)
        {
            return ActiveEffectSnapshot.FromInstances(GmfUnits().ToDictionary(unit => unit.UnitId,
                unit =>
                {
                    ActiveEffectInstance[] values;
                    return (IEnumerable<ActiveEffectInstance>)(instances != null &&
                        instances.TryGetValue(unit.UnitId, out values) ? values : new ActiveEffectInstance[0]);
                }, StringComparer.Ordinal));
        }

        private static ExplicitCastingPlan GmfCompile(GmfParty party, IEnumerable<string> targets,
            ActiveEffectSnapshot live = null)
        {
            PlannedCasting[] castings = targets.Select((target, index) => DirectCasting("gmf-" + index, "long",
                "druid", target, party.SourceId, party.Provider.Key.Ability)).ToArray();
            return new ExplicitCastingCompiler().Compile(CastingDocument(castings), party.Snapshot, party.Options,
                party.Effects, null, "long", null, false, live ?? GmfLive());
        }

        private static ExplicitStepConversion GmfConvert(GmfParty party, ExplicitCastingPlan plan)
        {
            CastingApplyDecision gate = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
            return ExplicitCastingStepConverter.Convert(plan, gate, party.Options, party.Effects);
        }

        private static void GmfDrain(System.Collections.IEnumerator run)
        {
            int moves = 0;
            while (run.MoveNext())
                if (++moves > 2000) throw new InvalidOperationException("The run did not terminate.");
        }

        private static ICastExecutor GmfExecutor(GmfWorld world, bool animated)
        {
            return animated ? (ICastExecutor)new AnimatedCastExecutor(world, true) : new InstantCastExecutor(world, true);
        }

        // Casting-first: the explicit coordinator over the converted projection.
        private static ExplicitCastingRunOutcome GmfRunExplicit(GmfWorld world, ExplicitStepConversion steps,
            bool animated)
        {
            ExplicitCastingRunOutcome outcome = null;
            GmfDrain(new ExplicitCastingRunCoordinator(GmfExecutor(world, animated)).Run(steps, value => outcome = value));
            MbbExpect(outcome != null, "The explicit run reported no outcome.");
            return outcome;
        }

        // Classic: the halting runner over the same steps.
        private static ExecutionReport GmfRunClassic(GmfWorld world, CastPlan plan, bool animated,
            out int? haltedAfter)
        {
            var runner = new HaltingPlanRunner(GmfExecutor(world, animated));
            var report = new ExecutionReport(plan);
            GmfDrain(runner.Run(plan, report));
            haltedAfter = runner.HaltedAfterStep;
            return report;
        }

        // The game boundary. Fire applies what the native actions apply:
        // ContextActionApplyBuff puts a new instance of the duration buff on
        // the target; MagicFang.EnchantSlot removes the selected tier's
        // temporary enchantment from the natural weapon and adds it again (a
        // new instance), leaving any other tier in place. The tier comes from
        // CasterLevel, the native context's caster level at cast time.
        private sealed class GmfWorld : IInstantCastRuntimeAdapter, ICastRuntimeAdapter
        {
            private readonly string _buff;
            private readonly string[] _tiers;
            private readonly bool _greater;
            private readonly Dictionary<string, List<ObservedEffectInstance>> _active =
                new Dictionary<string, List<ObservedEffectInstance>>(StringComparer.Ordinal);
            private readonly Dictionary<CastStep, EffectBaseline> _baselines = new Dictionary<CastStep, EffectBaseline>();
            private int _instance;
            internal int CasterLevel;
            internal int Slots;
            // "native", or "buff-only" (no natural weapon took the enchantment).
            internal string Application = "native";
            internal Func<string, bool> NativeCanTarget = id => true;
            internal readonly List<string> Fired = new List<string>();

            internal GmfWorld(string buff, string[] tiers, bool greater)
            {
                _buff = buff;
                _tiers = tiers;
                _greater = greater;
            }

            public bool IsInCombat { get { return false; } }

            internal IEnumerable<ObservedEffectInstance> Read(string unit)
            {
                List<ObservedEffectInstance> values;
                return _active.TryGetValue(unit, out values) ? values.ToArray() : new ObservedEffectInstance[0];
            }

            internal string[] Effects(string unit)
            {
                return Read(unit).Select(value => value.Kind + ":" + value.EffectId)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }

            public CastRuntimeValidation Validate(CastStep step)
            {
                string target = step.TargetUnitIds.FirstOrDefault();
                if (target == null || !NativeCanTarget(target)) return CastRuntimeValidation.Fail("target-invalid");
                return Slots >= step.Reservation.Units ? CastRuntimeValidation.Pass()
                    : CastRuntimeValidation.Fail("simulated-exhausted-source");
            }

            public InstantCastResult Fire(CastStep step)
            {
                _baselines[step] = new EffectBaseline(step.EffectObservationUnitIds.ToDictionary(id => id, Read,
                    StringComparer.Ordinal));
                int before = Slots;
                Slots -= step.Reservation.Units;
                string target = step.TargetUnitIds[0];
                Fired.Add(target);
                Put(target, EffectKind.Buff, _buff, step.Provider.CasterUnitId);
                if (Application != "buff-only")
                    Put(target, EffectKind.WornItemEnchantment,
                        _tiers[_greater ? Math.Min(GmfNativeRank(CasterLevel), _tiers.Length) - 1 : 0], null);
                return new InstantCastResult(true, true, false, Slots < before, true, "simulated-native-magic-fang",
                    AvailableCountJudgement.Violation(step.Reservation, before, Slots));
            }

            internal void Put(string unit, EffectKind kind, string effect, string caster)
            {
                List<ObservedEffectInstance> values;
                if (!_active.TryGetValue(unit, out values)) _active[unit] = values = new List<ObservedEffectInstance>();
                values.RemoveAll(value => value.Kind == kind && value.EffectId == effect);
                values.Add(new ObservedEffectInstance(kind, effect, "instance-" + (++_instance), 1000000, false, null,
                    caster));
            }

            public bool EffectsObserved(CastStep step)
            {
                EffectBaseline baseline;
                return _baselines.TryGetValue(step, out baseline) &&
                    AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds, step.ExpectedEffects, baseline,
                        Read, step.PreCoveredRecipientUnitIds, step.Provider.CasterUnitId);
            }

            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("simulated-settled"); }

            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("simulated-clean"); }

            public IAnimatedCastOperation StartAnimated(CastStep step)
            { return new GmfOperation(this, step, Fire(step)); }
        }

        private sealed class GmfOperation : IAnimatedCastOperation
        {
            private readonly GmfWorld _world;
            private readonly CastStep _step;
            private readonly InstantCastResult _result;
            internal GmfOperation(GmfWorld world, CastStep step, InstantCastResult result)
            { _world = world; _step = step; _result = result; }
            public bool IsCompleted { get { return true; } }
            public bool IsStarted { get { return true; } }
            public bool TimedOut { get { return false; } }
            public bool Succeeded { get { return _result.Succeeded; } }
            public bool EffectsObserved { get { return _world.EffectsObserved(_step); } }
            public bool ResourceSpent { get { return _result.ResourceSpent; } }
            public string ResourceCountViolation { get { return _result.ResourceCountViolation; } }
            public bool HasResidualDeliveryState { get { return false; } }
            public string Detail { get { return _result.Detail; } }
            public void Dispose() { }
        }

        // Native 2.1.7b contract (Assembly-CSharp metadata and IL; nothing is
        // executed): what MagicFang.RunAction selects and enchants, what
        // EnchantSlot does to an existing instance, how ContextRankConfig
        // computes the rank the adapter binds, and that UnitBody's
        // CurrentEquipmentSlots does not reach the empty-hand weapon or the
        // additional limbs.
        private static void TestGmfNativeContract()
        {
            MethodInfo run = typeof(MagicFang).GetMethod("RunAction", BindingFlags.Instance | BindingFlags.Public);
            List<string> members = MbbIlMembers(run);
            foreach (string required in new[]
            {
                "Kingmaker.UnitLogic.Mechanics.Actions.MagicFang::Greater",
                "Kingmaker.UnitLogic.Mechanics.Actions.MagicFang::EnchantLevel",
                "Kingmaker.UnitLogic.Mechanics.ContextValue::Calculate",
                "Kingmaker.Items.UnitBody::get_EmptyHandWeapon",
                "Kingmaker.Items.UnitBody::get_HandsAreEnabled",
                "Kingmaker.Items.UnitBody::get_PrimaryHand",
                "Kingmaker.Items.UnitBody::get_SecondaryHand",
                "Kingmaker.Items.UnitBody::get_AdditionalLimbs",
                "Kingmaker.Items.Slots.WeaponSlot::get_MaybeWeapon",
                "Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon::get_IsNatural",
                "Kingmaker.UnitLogic.Mechanics.Actions.MagicFang::EnchantSlot"
            })
                MbbExpect(members.Any(member => member.EndsWith(" " + required, StringComparison.Ordinal)),
                    "Native MagicFang.RunAction no longer references " + required + ".");
            List<string> opcodes;
            MbbIlAllOpcodes = true;
            try { opcodes = MbbIlMembers(run); }
            finally { MbbIlAllOpcodes = false; }
            const string enchantment = "ldfld Kingmaker.UnitLogic.Mechanics.Actions.MagicFang::Enchantment";
            // Greater: Enchantment[Math.Min(EnchantLevel.Calculate(context), Enchantment.Length) - 1].
            MbbExpect(GmfContains(opcodes, "callvirt Kingmaker.UnitLogic.Mechanics.ContextValue::Calculate",
                    "ldarg.0", enchantment, "ldlen", "conv.i4", "call System.Math::Min", "ldc.i4.1", "sub"),
                "Native Greater Magic Fang no longer selects Enchantment[min(level, n) - 1].");
            // Ordinary: Enchantment[0].
            MbbExpect(GmfContains(opcodes, enchantment, "ldc.i4.0", "ldelem.ref"),
                "Native ordinary Magic Fang no longer applies Enchantment[0].");
            List<string> enchantSlot = MbbIlMembers(typeof(MagicFang).GetMethod("EnchantSlot"));
            foreach (string required in new[]
            {
                "Kingmaker.Blueprints.Items.Ecnchantments.ItemEnchantment::get_IsTemporary",
                "Kingmaker.Items.ItemEntity::RemoveEnchantment",
                "Kingmaker.Items.ItemEntity::AddEnchantment"
            })
                MbbExpect(enchantSlot.Any(member => member.EndsWith(" " + required, StringComparison.Ordinal)),
                    "Native MagicFang.EnchantSlot no longer references " + required + ".");
            // ContextRankConfig.GetValue = ApplyMinMax(ApplyProgression(GetBaseValue(context))).
            List<string> rank = MbbIlMembers(typeof(ContextRankConfig).GetMethod("GetValue"));
            MbbExpect(GmfContains(rank, "call Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig::GetBaseValue",
                    "call Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig::ApplyProgression",
                    "call Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig::ApplyMinMax"),
                "Native ContextRankConfig.GetValue changed its rank pipeline.");
            Type iterator = typeof(UnitBody).GetNestedTypes(BindingFlags.NonPublic)
                .Single(type => type.Name.StartsWith("<get_CurrentEquipmentSlots>", StringComparison.Ordinal));
            List<string> slots = MbbIlMembers(iterator.GetMethod("MoveNext",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            MbbExpect(slots.Any(member => member.EndsWith("HandsEquipmentSet::PrimaryHand", StringComparison.Ordinal)) &&
                !slots.Any(member => member.Contains("EmptyHandWeapon") || member.Contains("AdditionalLimbs")),
                "Native UnitBody.CurrentEquipmentSlots changed which weapons it reaches.");
            // The fixture's own data: the rank the adapter binds.
            BlueprintAbility greater = GmfDump().Get<BlueprintAbility>(GmfAbility);
            MagicFang fang = GmfFang(greater);
            ContextRankConfig config = greater.ComponentsArray.OfType<ContextRankConfig>()
                .Single(value => Equals(GmfField(value, "m_Type"), fang.EnchantLevel.ValueRank));
            MbbExpect(fang.Greater && fang.EnchantLevel.ValueType == ContextValueType.Rank &&
                fang.Enchantment.Select(value => value.AssetGuid).SequenceEqual(GmfTiers) &&
                GmfField(config, "m_BaseValueType").ToString() == "CasterLevel" &&
                GmfField(config, "m_Progression").ToString() == "DivStep" && (int)GmfField(config, "m_StepLevel") == 4 &&
                (bool)GmfField(config, "m_UseMin") && (int)GmfField(config, "m_Min") == 1 &&
                (bool)GmfField(config, "m_UseMax") && (int)GmfField(config, "m_Max") == 5,
                "The committed MagicFangGreater blueprint no longer has the recorded tier contract.");
        }

        private static bool GmfContains(List<string> values, params string[] run)
        {
            for (int start = 0; start + run.Length <= values.Count; start++)
                if (run.Select((value, offset) => values[start + offset] == value).All(match => match)) return true;
            return false;
        }

        private static object GmfField(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
            }
            throw new InvalidOperationException("Native member absent: " + target.GetType() + "." + name);
        }

        private static void GmfSet(object target, string name, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new InvalidOperationException("Native member absent: " + target.GetType() + "." + name);
        }

        // 1. The production adapter on the native blueprint, bound for every
        // caster level the way a provider option binds it, yields the buff and
        // exactly the tier native MagicFang applies - never several tiers.
        private static void TestGmfDiscovery()
        {
            NativeBlueprintDump dump = GmfDump();
            EffectExpression raw = GmfScan(dump.Get<BlueprintAbility>(GmfAbility));
            for (int level = 1; level <= KingmakerActionGraphAdapter.MagicFangLevelProbe + 5; level++)
            {
                EffectExpression bound = GmfBind(raw, level);
                MbbExpect(!CasterLevelPredicate.HasUnresolved(bound) &&
                    GmfLeaves(bound).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[GmfNativeRank(level) - 1])),
                    "Caster level " + level + " expects " + string.Join(",", GmfLeaves(bound)) + ", not the buff and +" +
                    GmfNativeRank(level) + ".");
            }
            // The rank boundaries, explicitly.
            var boundaries = new Dictionary<int, int>
            {
                { 1, 1 }, { 3, 1 }, { 5, 1 }, { 7, 1 }, { 8, 2 }, { 11, 2 }, { 12, 3 }, { 15, 3 }, { 16, 4 },
                { 19, 4 }, { 20, 5 }, { 30, 5 }
            };
            foreach (KeyValuePair<int, int> boundary in boundaries)
                MbbExpect(GmfLeaves(GmfBind(raw, boundary.Key)).Contains(EffectKind.WornItemEnchantment + ":" +
                    GmfTiers[boundary.Value - 1]), "Caster level " + boundary.Key + " is not tier +" + boundary.Value + ".");
            // An unknown caster level leaves the selection unresolved.
            MbbExpect(CasterLevelPredicate.HasUnresolved(GmfBind(raw, 0)),
                "An unknown caster level resolved a Greater Magic Fang tier.");
            // Through production admission and the provider option.
            foreach (int level in new[] { 5, 8, 12, 16, 20 })
            {
                GmfParty party = GmfDruidParty(dump, GmfAbility, level, 1);
                EffectExpression bound = party.Option.BindEffects(party.Raw, "ranger-pet");
                MbbExpect(party.Provider.EffectiveCasterLevel == level &&
                    GmfLeaves(bound).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[GmfNativeRank(level) - 1])),
                    "The admitted level-" + level + " provider expects " + string.Join(",", GmfLeaves(bound)) + ".");
            }
            // Ordinary Magic Fang: the buff and Enchantment[0] at every level,
            // under its unchanged catalogue identity.
            EffectExpression ordinary = GmfScan(dump.Get<BlueprintAbility>(GmfOrdinaryAbility));
            foreach (int level in new[] { 0, 1, 12, 40 })
                MbbExpect(GmfLeaves(GmfBind(ordinary, level)).SequenceEqual(GmfExpected(GmfOrdinaryBuff, GmfTiers[0])),
                    "Ordinary Magic Fang changed at caster level " + level + ".");
            MbbExpect(EffectAggregateIdentity.Describe(ordinary) == GmfOrdinaryIdentity,
                "Ordinary Magic Fang's catalogue identity changed: " + EffectAggregateIdentity.Describe(ordinary));
        }

        // The semantic identity ordinary Magic Fang had before the repair
        // (recorded from the baseline 9137987 adapter on the same fixture).
        private const string GmfOrdinaryIdentity = "ability-reference(sequence[sequence[sequence[" +
            "leaf(2,32:d704f90f54f813043a525f304f6c0050,0)];leaf(0,32:e7646b1dfdd22ce4ab340f295938ab8e,0)]])";

        // 8. Call of the Wild replaces MagicFangGreater's array with five
        // ordered enchantments of its own; the binding follows the ARRAY
        // ORDER, whatever the identities (here sorted differently from their
        // positions), and never a native GUID.
        private static void TestGmfReplacementEnchantments()
        {
            string[] replaced =
            {
                "f5c0000000000000000000000000c0e1", "a4c0000000000000000000000000c0e2",
                "e3c0000000000000000000000000c0e3", "b2c0000000000000000000000000c0e4",
                "c1c0000000000000000000000000c0e5"
            };
            foreach (bool reversed in new[] { false, true })
            {
                BlueprintAbility ability = GmfDump().Get<BlueprintAbility>(GmfAbility);
                string[] order = reversed ? replaced.Reverse().ToArray() : replaced;
                GmfFang(ability).Enchantment = order.Select(id =>
                    (BlueprintItemEnchantment)NativeBlueprintDump.Stub<BlueprintWeaponEnchantment>(id)).ToArray();
                EffectExpression raw = GmfScan(ability);
                MbbExpect(!GmfLeaves(raw).Any(leaf => GmfTiers.Any(leaf.EndsWith)),
                    "A replaced Greater Magic Fang still expects a native enchantment.");
                foreach (int level in new[] { 1, 7, 8, 12, 16, 20, 40 })
                    MbbExpect(GmfLeaves(GmfBind(raw, level)).SequenceEqual(GmfExpected(GmfBuff,
                            order[GmfNativeRank(level) - 1])),
                        "Replaced tiers (" + (reversed ? "reversed" : "in order") + ") at caster level " + level +
                        " expect " + string.Join(",", GmfLeaves(GmfBind(raw, level))) + ".");
            }
            BlueprintAbility ordinary = GmfDump().Get<BlueprintAbility>(GmfOrdinaryAbility);
            GmfFang(ordinary).Enchantment[0] = NativeBlueprintDump.Stub<BlueprintWeaponEnchantment>(replaced[0]);
            MbbExpect(GmfLeaves(GmfBind(GmfScan(ordinary), 12)).SequenceEqual(GmfExpected(GmfOrdinaryBuff, replaced[0])),
                "A replaced ordinary Magic Fang enchantment is not the expected one.");

            // Robustness (deliberately modified blueprints, not observations of
            // the installed game): a level contract the adapter cannot prove
            // leaves the tier unknown - never every tier, never any tier.
            var modifications = new Dictionary<string, Action<BlueprintAbility>>
            {
                { "no rank config", ability => GmfSet(ability, "Components", ability.ComponentsArray
                    .Where(component => !(component is ContextRankConfig) ||
                        !Equals(GmfField(component, "m_Type"), GmfFang(ability).EnchantLevel.ValueRank)).ToArray()) },
                { "two rank configs", ability => GmfSet(ability, "Components", ability.ComponentsArray
                    .Concat(ability.ComponentsArray.OfType<ContextRankConfig>().Where(component =>
                        Equals(GmfField(component, "m_Type"), GmfFang(ability).EnchantLevel.ValueRank))).ToArray()) },
                { "class-level rank", ability => GmfSet(ability.ComponentsArray.OfType<ContextRankConfig>().Single(
                        component => Equals(GmfField(component, "m_Type"), GmfFang(ability).EnchantLevel.ValueRank)),
                    "m_BaseValueType", ContextRankBaseValueType.ClassLevel) },
                { "shared value", ability => GmfFang(ability).EnchantLevel.ValueType = ContextValueType.Shared },
                { "constant zero", ability =>
                    {
                        GmfFang(ability).EnchantLevel.ValueType = ContextValueType.Simple;
                        GmfFang(ability).EnchantLevel.Value = 0;
                    } }
            };
            foreach (KeyValuePair<string, Action<BlueprintAbility>> modification in modifications)
            {
                BlueprintAbility ability = GmfDump().Get<BlueprintAbility>(GmfAbility);
                modification.Value(ability);
                DiscoveryScanResult result = GmfScanResult(ability);
                MbbExpect(!GmfLeaves(result.Expression).Any(leaf => leaf.StartsWith(
                        EffectKind.WornItemEnchantment.ToString(), StringComparison.Ordinal)) &&
                    result.Diagnostics.Any(value => value.Detail == "MagicFang:enchant-level-unverified"),
                    "Modified (" + modification.Key + ") Greater Magic Fang was not left unknown: " +
                    string.Join(",", GmfLeaves(result.Expression)));
            }
            // A constant level is provable: the native index of that constant.
            BlueprintAbility constant = GmfDump().Get<BlueprintAbility>(GmfAbility);
            GmfFang(constant).EnchantLevel.ValueType = ContextValueType.Simple;
            GmfFang(constant).EnchantLevel.Value = 9;
            MbbExpect(GmfLeaves(GmfBind(GmfScan(constant), 3)).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[4])),
                "A constant enchant level of 9 is not the last tier.");
        }

        private static ObservedEffectInstance GmfSeen(EffectKind kind, string effect, string key, long end = 100,
            bool suppressed = false)
        {
            return new ObservedEffectInstance(kind, effect, key, end, suppressed, null,
                kind == EffectKind.Buff ? "druid" : null);
        }

        // 2 and 3. Confirmation on the intended recipient needs the buff AND
        // the cast's own tier, with something fresh; nothing weaker, stronger,
        // unrelated, suppressed, misplaced or old proves the application.
        private static void TestGmfConfirmation()
        {
            NativeBlueprintDump dump = GmfDump();
            GmfParty party = GmfDruidParty(dump, GmfAbility, 12, 1);
            EffectExpression expected = party.Option.BindEffects(party.Raw, "ranger-pet");
            var none = new ObservedEffectInstance[0];
            Func<IEnumerable<ObservedEffectInstance>, IDictionary<string, ObservedEffectInstance[]>, EffectExpression, bool>
                reached = (before, after, expression) => AppliedEffectJudgement.AllReached(new[] { "ranger-pet" },
                    expression, new EffectBaseline(GmfUnits().ToDictionary(unit => unit.UnitId,
                        unit => unit.UnitId == "ranger-pet" ? before : none, StringComparer.Ordinal)),
                    unit =>
                    {
                        ObservedEffectInstance[] values;
                        return after.TryGetValue(unit, out values) ? values : none;
                    }, null, "druid");
            Func<string, ObservedEffectInstance[], IDictionary<string, ObservedEffectInstance[]>> on =
                (unit, values) => new Dictionary<string, ObservedEffectInstance[]>(StringComparer.Ordinal) { { unit, values } };
            ObservedEffectInstance buff = GmfSeen(EffectKind.Buff, GmfBuff, "buff-1");
            Func<int, ObservedEffectInstance> tier = rank => GmfSeen(EffectKind.WornItemEnchantment, GmfTiers[rank - 1],
                "tier-" + rank);
            MbbExpect(reached(none, on("ranger-pet", new[] { buff, tier(3) }), expected),
                "The buff and the cast's +3 on the other character's pet did not confirm.");
            var refused = new Dictionary<string, KeyValuePair<IEnumerable<ObservedEffectInstance>,
                IDictionary<string, ObservedEffectInstance[]>>>
            {
                { "missing buff", Pair(none, on("ranger-pet", new[] { tier(3) })) },
                { "missing enchantment", Pair(none, on("ranger-pet", new[] { buff })) },
                { "weaker tier", Pair(none, on("ranger-pet", new[] { buff, tier(2) })) },
                { "stronger tier", Pair(none, on("ranger-pet", new[] { buff, tier(4) })) },
                { "unrelated buff", Pair(none, on("ranger-pet", new[] { GmfSeen(EffectKind.Buff, GmfOrdinaryBuff, "o"), tier(3) })) },
                { "wrong recipient", Pair(none, on("druid-pet", new[] { buff, tier(3) })) },
                { "suppressed buff", Pair(none, on("ranger-pet", new[] { GmfSeen(EffectKind.Buff, GmfBuff, "s", 100, true), tier(3) })) },
                { "unchanged old instances", Pair(new[] { buff, tier(3) }, on("ranger-pet", new[] { buff, tier(3) })) }
            };
            foreach (KeyValuePair<string, KeyValuePair<IEnumerable<ObservedEffectInstance>,
                IDictionary<string, ObservedEffectInstance[]>>> refusal in refused)
                MbbExpect(!reached(refusal.Value.Key, refusal.Value.Value, expected),
                    "Greater Magic Fang confirmed from " + refusal.Key + ".");
            // An unknown caster level: whatever tier appears, nothing proves this cast's.
            GmfParty unknown = GmfDruidParty(dump, GmfAbility, 0, 1);
            EffectExpression unresolved = unknown.Option.BindEffects(unknown.Raw, "ranger-pet");
            MbbExpect(CasterLevelPredicate.HasUnresolved(unresolved) &&
                !Enumerable.Range(1, 5).Any(rank => reached(none, on("ranger-pet", new[] { buff, tier(rank) }), unresolved)),
                "A cast whose caster level was unknown was confirmed.");
        }

        private static KeyValuePair<IEnumerable<ObservedEffectInstance>, IDictionary<string, ObservedEffectInstance[]>> Pair(
            IEnumerable<ObservedEffectInstance> before, IDictionary<string, ObservedEffectInstance[]> after)
        {
            return new KeyValuePair<IEnumerable<ObservedEffectInstance>, IDictionary<string, ObservedEffectInstance[]>>(
                before, after);
        }

        // Existing effects (casting-first policy, unchanged): a weaker earlier
        // tier never satisfies the stronger casting; a stronger earlier tier
        // is not the cast's tier, and native MagicFang adds the cast's tier
        // beside it (enhancement bonuses do not stack), so it casts; and an
        // enchantment's caster level is unreadable, so even the same tier is
        // never provably sufficient. Neither enchantment nor buff is ever
        // accepted alone.
        private static void TestGmfExistingEffects()
        {
            NativeBlueprintDump dump = GmfDump();
            GmfParty party = GmfDruidParty(dump, GmfAbility, 12, 1);
            EffectExpression expected = party.Option.BindEffects(party.Raw, "ranger-pet");
            var requirement = new ExistingEffectRequirement(12, 0, 7200, true, null, true, "druid");
            Func<int, ActiveEffectInstance> buff = level => new ActiveEffectInstance(EffectKind.Buff, GmfBuff, 5000, level,
                0, false, true, "druid");
            Func<int, ActiveEffectInstance> tier = rank => new ActiveEffectInstance(EffectKind.WornItemEnchantment,
                GmfTiers[rank - 1], null, null, null);
            Func<ActiveEffectInstance[], ExistingEffectRecipientAssessment> assess = instances =>
                ExistingEffectSufficiency.AssessScoped("druid", "ranger-pet", expected,
                    unit => unit == "ranger-pet" ? instances : new ActiveEffectInstance[0], null, requirement);
            MbbExpect(assess(new[] { buff(8), tier(2) }).Verdict == ExistingEffectVerdict.NotPresent,
                "An earlier +2 satisfied a +3 casting.");
            MbbExpect(assess(new[] { buff(16), tier(4) }).Verdict == ExistingEffectVerdict.NotPresent,
                "An earlier +4 was taken as the +3 casting's own effect.");
            ExistingEffectRecipientAssessment same = assess(new[] { buff(12), tier(3) });
            MbbExpect(same.Verdict == ExistingEffectVerdict.Insufficient &&
                same.Reasons.Any(reason => reason.StartsWith("caster-level-unverified", StringComparison.Ordinal)),
                "An existing +3 became provably sufficient: " + same.Verdict + " " + string.Join(",", same.Reasons));
            MbbExpect(assess(new[] { buff(20) }).Verdict == ExistingEffectVerdict.NotPresent &&
                assess(new[] { tier(3) }).Verdict == ExistingEffectVerdict.NotPresent,
                "The buff or the enchantment alone was taken as the effect.");
            // The compiler with that live state: the casting stays Ready and
            // reserves one slot.
            ResolvedCasting casting = GmfCompile(party, new[] { "ranger-pet" }, GmfLive(
                new Dictionary<string, ActiveEffectInstance[]> { { "ranger-pet", new[] { buff(16), tier(4) } } }))
                .Castings.Single();
            MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(cost => cost.Units) == 1,
                "An earlier +4 changed the +3 casting: " + casting.Readiness + " " +
                string.Join(",", casting.ReadinessReasons));
        }

        // 4. The report: one Druid, two pets of different masters. Native
        // targeting admits both; both castings plan, convert, cast and
        // confirm - in either order, instant and animated, through the
        // casting-first coordinator and the Classic halting runner - and
        // exactly two slots are spent.
        private static void TestGmfTwoPets()
        {
            NativeBlueprintDump dump = GmfDump();
            foreach (bool animated in new[] { false, true })
                foreach (bool classic in new[] { false, true })
                    foreach (string[] order in new[]
                    {
                        new[] { "druid-pet", "ranger-pet" }, new[] { "ranger-pet", "druid-pet" }
                    })
                    {
                        string label = (animated ? "animated" : "instant") + "/" + (classic ? "classic" : "casting-first") +
                            "/" + string.Join(">", order);
                        GmfParty party = GmfDruidParty(dump, GmfAbility, 12, 2);
                        MbbExpect(order.All(party.Option.ReachableTargetIds.Contains),
                            label + ": native targeting did not offer both pets: " +
                            string.Join(",", party.Option.ReachableTargetIds));
                        ExplicitCastingPlan plan = GmfCompile(party, order);
                        MbbExpect(plan.Castings.All(casting => casting.Readiness == ResolvedCastingReadiness.Ready &&
                                casting.Cost.Sum(cost => cost.Units) == 1),
                            label + ": " + string.Join(" / ", plan.Castings.Select(casting => casting.CastingId + "=" +
                                casting.Readiness + ":" + string.Join(",", casting.ReadinessReasons))));
                        ExplicitStepConversion steps = GmfConvert(party, plan);
                        MbbExpect(steps.Converted && steps.Plan.Steps.Select(step => step.TargetUnitIds.Single())
                                .SequenceEqual(order),
                            label + ": the castings did not convert in order: " + steps.Refusal);
                        var world = new GmfWorld(GmfBuff, GmfTiers, true) { CasterLevel = 12, Slots = 2 };
                        if (classic)
                        {
                            int? halted;
                            ExecutionReport report = GmfRunClassic(world, steps.Plan, animated, out halted);
                            MbbExpect(halted == null && report.Confirmed == 2 && report.ResourcesSpent == 2 &&
                                report.Failed == 0,
                                label + ": halted after " + halted + "; " + string.Join(" | ", report.Records.Select(
                                    record => record.Status + ":" + record.Detail)));
                        }
                        else
                        {
                            ExplicitCastingRunOutcome outcome = GmfRunExplicit(world, steps, animated);
                            MbbExpect(outcome.AllConfirmed && outcome.Entries.Count == 2 &&
                                outcome.Entries.All(entry => entry.Confirmed && entry.ResourceSpentReported),
                                label + ": halted after " + outcome.HaltedAfterCastingId + " " + outcome.HaltReason + "; " +
                                string.Join(" | ", outcome.Entries.Select(entry => entry.FinalStatus + ":" + entry.Detail)));
                        }
                        MbbExpect(world.Fired.SequenceEqual(order) && world.Slots == 0,
                            label + ": fired " + string.Join(",", world.Fired) + " with " + world.Slots + " slots left.");
                        foreach (string pet in order)
                            MbbExpect(world.Effects(pet).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[2])),
                                label + ": " + pet + " has " + string.Join(",", world.Effects(pet)));
                        foreach (CastStep step in steps.Plan.Steps)
                            MbbExpect(GmfLeaves(step.ExpectedEffects).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[2])) &&
                                step.ExpectedRecipientUnitIds.SequenceEqual(step.TargetUnitIds),
                                label + ": a step expects " + string.Join(",", GmfLeaves(step.ExpectedEffects)) + " on " +
                                string.Join(",", step.ExpectedRecipientUnitIds));
                    }
        }

        // 5. Preserved refusals: native target rejection (while planning and
        // at cast time) and a genuinely unconfirmed cast (no natural weapon
        // took the enchantment, or the native context's caster level gave
        // another tier) still stop every later casting.
        private static void TestGmfRefusals()
        {
            NativeBlueprintDump dump = GmfDump();
            GmfParty refusedParty = GmfDruidParty(dump, GmfAbility, 12, 2, id => id != "ranger-pet");
            MbbExpect(!refusedParty.Option.ReachableTargetIds.Contains("ranger-pet") &&
                refusedParty.Option.ReachableTargetIds.Contains("druid-pet"),
                "Native target rejection was not honoured: " + string.Join(",", refusedParty.Option.ReachableTargetIds));
            ExplicitCastingPlan refusedPlan = GmfCompile(refusedParty, new[] { "ranger-pet" });
            ResolvedCasting blocked = refusedPlan.Castings.Single();
            ExplicitStepConversion refusedSteps = GmfConvert(refusedParty, refusedPlan);
            MbbExpect(blocked.Readiness != ResolvedCastingReadiness.Ready && blocked.Cost.Count == 0 &&
                (!refusedSteps.Converted || refusedSteps.Plan.Steps.Count == 0),
                "A natively refused pet was planned: " + blocked.Readiness);
            string[] order = { "ranger-pet", "druid-pet" };
            var cases = new Dictionary<string, Action<GmfWorld>>
            {
                { "cast-time target rejection", world => world.NativeCanTarget = id => id != "ranger-pet" },
                { "no natural weapon enchanted", world => world.Application = "buff-only" },
                { "native context caster level 8", world => world.CasterLevel = 8 }
            };
            foreach (KeyValuePair<string, Action<GmfWorld>> scenario in cases)
                foreach (bool animated in new[] { false, true })
                    foreach (bool classic in new[] { false, true })
                    {
                        string label = scenario.Key + "/" + (animated ? "animated" : "instant") + "/" +
                            (classic ? "classic" : "casting-first");
                        GmfParty party = GmfDruidParty(dump, GmfAbility, 12, 2);
                        ExplicitStepConversion steps = GmfConvert(party, GmfCompile(party, order));
                        MbbExpect(steps.Converted && steps.Plan.Steps.Count == 2, label + ": " + steps.Refusal);
                        var world = new GmfWorld(GmfBuff, GmfTiers, true) { CasterLevel = 12, Slots = 2 };
                        scenario.Value(world);
                        bool rejected = scenario.Key.StartsWith("cast-time", StringComparison.Ordinal);
                        if (classic)
                        {
                            int? halted;
                            ExecutionReport report = GmfRunClassic(world, steps.Plan, animated, out halted);
                            MbbExpect(halted == 0 && report.Confirmed == 0 &&
                                report.Records.Any(record => record.Status == (rejected
                                    ? CastExecutionStatus.FailedValidation : CastExecutionStatus.TimedOutUnconfirmed)) &&
                                report.Records.Any(record => record.Detail == HaltingPlanRunner.HaltedDetailPrefix + "0"),
                                label + ": " + string.Join(" | ", report.Records.Select(record => record.Status + ":" +
                                    record.Detail)));
                        }
                        else
                        {
                            ExplicitCastingRunOutcome outcome = GmfRunExplicit(world, steps, animated);
                            MbbExpect(outcome.Halted && !outcome.AllConfirmed &&
                                outcome.HaltedAfterCastingId == "gmf-0" && !outcome.Entries.Any(entry => entry.Confirmed),
                                label + ": halted after " + outcome.HaltedAfterCastingId + "; " +
                                string.Join(" | ", outcome.Entries.Select(entry => entry.FinalStatus + ":" + entry.Detail)));
                        }
                        MbbExpect(world.Fired.SequenceEqual(rejected ? new string[0] : new[] { "ranger-pet" }) &&
                            world.Slots == (rejected ? 2 : 1),
                            label + ": fired " + string.Join(",", world.Fired) + " with " + world.Slots + " slots left.");
                    }
        }

        // 6. Neighbours: ordinary Magic Fang still casts and confirms on both
        // pets with its buff and Enchantment[0]; an ordinary sequence of
        // independent effects still needs every effect; and an effect aimed
        // at a pet (ContextActionsOnPet) still reaches only the caster's own.
        private static void TestGmfNeighbours()
        {
            NativeBlueprintDump dump = GmfDump();
            string[] order = { "ranger-pet", "druid-pet" };
            foreach (bool animated in new[] { false, true })
            {
                GmfParty party = GmfDruidParty(dump, GmfOrdinaryAbility, 12, 2);
                ExplicitStepConversion steps = GmfConvert(party, GmfCompile(party, order));
                MbbExpect(steps.Converted && steps.Plan.Steps.Count == 2, "Ordinary Magic Fang: " + steps.Refusal);
                var world = new GmfWorld(GmfOrdinaryBuff, new[] { GmfTiers[0] }, false) { CasterLevel = 12, Slots = 2 };
                ExplicitCastingRunOutcome outcome = GmfRunExplicit(world, steps, animated);
                MbbExpect(outcome.AllConfirmed && world.Slots == 0 &&
                    order.All(pet => world.Effects(pet).SequenceEqual(GmfExpected(GmfOrdinaryBuff, GmfTiers[0]))),
                    "Ordinary Magic Fang did not confirm on both pets: " + string.Join(" | ",
                        outcome.Entries.Select(entry => entry.FinalStatus + ":" + entry.Detail)));
            }
            var sequence = new SequenceEffectExpression(new EffectExpression[]
            {
                new EffectLeafExpression(EffectKind.Buff, "first-effect", EffectTarget.CurrentTarget, "fixture", "first"),
                new EffectLeafExpression(EffectKind.Buff, "second-effect", EffectTarget.CurrentTarget, "fixture", "second")
            });
            var none = new ObservedEffectInstance[0];
            Func<ObservedEffectInstance[], bool> reached = after => AppliedEffectJudgement.AllReached(new[] { "ally" },
                sequence, new EffectBaseline(new Dictionary<string, IEnumerable<ObservedEffectInstance>>
                    { { "ally", none } }), unit => after, null, "druid");
            MbbExpect(!reached(new[] { GmfSeen(EffectKind.Buff, "first-effect", "a") }) &&
                reached(new[] { GmfSeen(EffectKind.Buff, "first-effect", "a"), GmfSeen(EffectKind.Buff, "second-effect", "b") }),
                "An ordinary sequence of independent effects no longer needs every effect.");
            GmfParty owner = GmfDruidParty(dump, GmfAbility, 12, 1);
            var petOnly = new TargetedEffectExpression(EffectTarget.Pet, new SequenceEffectExpression(new EffectExpression[]
            {
                new EffectLeafExpression(EffectKind.Buff, "pet-buff", EffectTarget.CurrentTarget, "ContextActionApplyBuff", "pet")
            }));
            ProviderTargetingDecision petDecision = ProviderTargetingPlanner.Decide(owner.Provider, petOnly, GmfUnits(),
                true, false, null, id => true, (from, to) => 1f);
            MbbExpect(petDecision.ReachableIds.SequenceEqual(new[] { "druid-pet" }),
                "A pet-directed effect reached " + string.Join(",", petDecision.ReachableIds));
        }

        // 7. Observation: the planner's item read on a detached native
        // UnitBody (no Unity call) reaches every weapon native MagicFang
        // enchants - the hand slots, the empty-hand weapon and the
        // additional (or polymorph) limbs - and each item once.
        private static void TestGmfNaturalWeaponObservation()
        {
            ItemEntity claw = GmfDetached<ItemEntityWeapon>(), otherClaw = GmfDetached<ItemEntityWeapon>(),
                bite = GmfDetached<ItemEntityWeapon>(), unarmed = GmfDetached<ItemEntityWeapon>(),
                armor = GmfDetached<ItemEntityArmor>(), polymorphBite = GmfDetached<ItemEntityWeapon>();
            // A bear companion (AnimalCompanionUnitBear: Claw1d4 in both hands,
            // Bite1d6 as an additional limb, WeaponEmptyHand), with barding.
            GmfExpectItems(KingmakerEnchantedItems.Read(GmfBody(claw, otherClaw, armor, unarmed, new[] { bite })),
                new[] { claw, otherClaw, armor, unarmed, bite }, "bear companion");
            // Empty hands (an unarmed humanoid): only the empty-hand weapon,
            // which the hand slots never return.
            GmfExpectItems(KingmakerEnchantedItems.Read(GmfBody(null, null, null, unarmed, new ItemEntity[0])),
                new[] { unarmed }, "empty hands");
            // Polymorphed: the polymorph limbs, not the unit's own.
            UnitBody polymorphed = GmfBody(null, null, null, unarmed, new[] { bite });
            GmfSet(polymorphed, "m_PolymorphAdditionalLimbs", GmfLimbs(new[] { polymorphBite }));
            GmfSet(polymorphed, "<IsPolymorphed>k__BackingField", true);
            GmfExpectItems(KingmakerEnchantedItems.Read(polymorphed), new[] { unarmed, polymorphBite }, "polymorphed");
            // One instance reached through several sources is observed once.
            GmfExpectItems(KingmakerEnchantedItems.Read(GmfBody(claw, null, null, claw, new[] { bite, claw, null })),
                new[] { claw, bite }, "duplicated item");
            GmfExpectItems(KingmakerEnchantedItems.Read(null), new ItemEntity[0], "no body");
        }

        private static void GmfExpectItems(IReadOnlyList<ItemEntity> actual, ItemEntity[] expected, string label)
        {
            MbbExpect(actual.Count == expected.Length &&
                actual.Select((item, index) => ReferenceEquals(item, expected[index])).All(same => same),
                label + ": the planner observes " + actual.Count + " items, expected " + expected.Length + ".");
        }

        private static T GmfDetached<T>() where T : class
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }

        private static List<WeaponSlot> GmfLimbs(IEnumerable<ItemEntity> items)
        {
            return items.Select(item =>
            {
                if (item == null) return null;
                var slot = GmfDetached<WeaponSlot>();
                GmfSet(slot, "m_Item", item);
                return slot;
            }).ToList();
        }

        private static UnitBody GmfBody(ItemEntity primary, ItemEntity secondary, ItemEntity armor,
            ItemEntity emptyHand, IEnumerable<ItemEntity> limbs)
        {
            var body = GmfDetached<UnitBody>();
            var hands = GmfDetached<HandsEquipmentSet>();
            var primarySlot = GmfDetached<HandSlot>();
            var secondarySlot = GmfDetached<HandSlot>();
            GmfSet(primarySlot, "m_Item", primary);
            GmfSet(secondarySlot, "m_Item", secondary);
            GmfSet(hands, "PrimaryHand", primarySlot);
            GmfSet(hands, "SecondaryHand", secondarySlot);
            GmfSet(body, "m_HandsEquipmentSets", new[] { hands });
            GmfSet(body, "m_CurrentHandsEquipmentSetIndex", 0);
            if (armor != null)
            {
                var armorSlot = GmfDetached<ArmorSlot>();
                GmfSet(armorSlot, "m_Item", armor);
                GmfSet(body, "Armor", armorSlot);
            }
            GmfSet(body, "m_EmptyHandWeapon", emptyHand);
            GmfSet(body, "m_AdditionalLimbs", GmfLimbs(limbs));
            return body;
        }
    }
}
