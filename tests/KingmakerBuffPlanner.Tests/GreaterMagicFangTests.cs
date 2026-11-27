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
using KingmakerBuffPlanner.Persistence;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    // Greater Magic Fang repair. Reported: "Greater Magic Fang from the Druid
    // spellbook only apply to the Druid's own pet, not other pets." The
    // native spell targets any friendly unit in Close range; the defect was
    // that discovery required all five enhancement tiers at once, so no cast
    // was ever confirmed and the run stopped after the first pet.
    //
    // These regressions run the production adapter, scanner, classifier,
    // admission, targeting, both planners (casting-first compiler, gate and
    // converter; Classic CastPlanner), judgement, both executors and both run
    // halters on the committed native 2.1.7b blueprints. Only the game
    // boundary is simulated: GmfWorld applies what native MagicFang and
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

        // Call of the Wild's pattern: five ordered enchantments of its own,
        // here with identities sorted differently from their positions.
        private static readonly string[] GmfReplacementTiers =
        {
            "f5c0000000000000000000000000c0e1", "a4c0000000000000000000000000c0e2",
            "e3c0000000000000000000000000c0e3", "b2c0000000000000000000000000c0e4",
            "c1c0000000000000000000000000c0e5"
        };

        // The regressions that read native fixture blueprints (kept by the
        // explicit fixture capture with the missing-beneficial-buffs ones).
        private static readonly KeyValuePair<string, Action>[] GmfNativeRegressions =
        {
            Mbb("greater-magic-fang-discovery-binds-the-native-tier-to-caster-level", TestGmfDiscovery),
            Mbb("greater-magic-fang-catalogue-identity-is-unchanged", TestGmfIdentity),
            Mbb("greater-magic-fang-replacement-enchantments-follow-array-order", TestGmfReplacementEnchantments),
            Mbb("greater-magic-fang-unproven-enchantment-is-never-admitted", TestGmfUnprovenContracts),
            Mbb("greater-magic-fang-confirmation-needs-the-cast-tier-and-buff-fresh-on-the-recipient",
                TestGmfConfirmation),
            Mbb("greater-magic-fang-unreadable-weapon-baseline-never-confirms-an-old-tier",
                TestGmfIncompleteWeaponBaseline),
            Mbb("greater-magic-fang-existing-effects-keep-the-casting-policy", TestGmfExistingEffects),
            Mbb("greater-magic-fang-druid-casts-on-pets-of-two-masters-in-either-order", TestGmfTwoPets),
            Mbb("greater-magic-fang-refusals-and-unconfirmed-casts-still-stop-the-run", TestGmfRefusals),
            Mbb("greater-magic-fang-unknown-caster-level-plans-nothing", TestGmfUnknownCasterLevel),
            Mbb("greater-magic-fang-weaker-funded-tier-never-satisfies-a-stronger-one", TestGmfForecastTiers),
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

        private static ContextRankConfig GmfRankConfig(BlueprintAbility ability)
        {
            return ability.ComponentsArray.OfType<ContextRankConfig>()
                .Single(value => Equals(GmfField(value, "m_Type"), GmfFang(ability).EnchantLevel.ValueRank));
        }

        private static void GmfReplaceTiers(BlueprintAbility ability, string[] ids)
        {
            GmfFang(ability).Enchantment = ids.Select(id =>
                (BlueprintItemEnchantment)NativeBlueprintDump.Stub<BlueprintWeaponEnchantment>(id)).ToArray();
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
            internal List<ProviderSnapshot> Providers;
            internal ProviderSnapshot Provider { get { return Providers[0]; } }
            internal ProviderPlanningOption Option { get { return Options[0]; } }
            internal string SourceId
            {
                get { return CatalogSourceIdentity.For(Provider.Key.Ability, Raw); }
            }
            internal EffectExpression Raw { get { return Effects[Provider.Key.Ability.Canonical]; } }
        }

        private static ProviderSnapshot GmfAdmit(NativeBlueprintDump dump, PartyProviderAdmission admission,
            string caster, string spell, int casterLevel, ResourcePoolSnapshot pool, List<ProviderSnapshot> providers)
        {
            return admission.AdmitSpell(caster, "book-" + caster, false, "Spellbook",
                MbbRead(dump, spell, spell, SourceKind.Spellbook, casterLevel), spell == GmfAbility ? 3 : 1, 0,
                pool.PoolKey, 1, new string[0], providers);
        }

        // Casters who know the spell. The druid is a prepared caster: each
        // slot is its own native token with the spell prepared in it; any
        // other caster is spontaneous, with a spell-level pool. Only the live
        // reads are injected (the spellbook, the slots, the caster level and
        // native AbilityData.CanTarget per unit); the providers, their
        // expression, targets and options are production.
        private static GmfParty GmfPartyOf(NativeBlueprintDump dump, string spell, Func<string, bool> nativeCanTarget,
            params Tuple<string, int, int>[] casters)
        {
            List<UnitSnapshot> units = GmfUnits();
            PartyProviderAdmission admission = MbbAdmission(dump);
            var providers = new List<ProviderSnapshot>();
            var pools = new List<ResourcePoolSnapshot>();
            int spellLevel = spell == GmfAbility ? 3 : 1;
            foreach (Tuple<string, int, int> caster in casters)
            {
                string book = "book-" + caster.Item1;
                bool prepared = caster.Item1 == "druid";
                ResourceTokenSnapshot[] tokens = prepared ? Enumerable.Range(0, caster.Item3).Select(index =>
                    new ResourceTokenSnapshot(PreparedSlotIds.Format(spellLevel, 0, index),
                        MbbSpellKey(spell), spellLevel, PreparedSlotKind.Common, true, true, null)).ToArray()
                    : new ResourceTokenSnapshot[0];
                ResourcePoolSnapshot pool = prepared
                    ? new ResourcePoolSnapshot(PartyProviderAdmission.PoolKey(caster.Item1, book, "prepared-" + spellLevel),
                        ResourcePoolKind.PreparedSlots, tokens.Length, tokens.Length, tokens)
                    : PartyProviderAdmission.SpontaneousLevelPool(caster.Item1, book, spellLevel, caster.Item3,
                        Math.Max(1, caster.Item3));
                pools.Add(pool);
                MbbExpect(admission.AdmitSpell(caster.Item1, book, prepared, "Spellbook",
                        MbbRead(dump, spell, spell, SourceKind.Spellbook, caster.Item2), spellLevel, 0, pool.PoolKey, 1,
                        tokens.Select(token => token.TokenId), providers) != null,
                    "The production admission refused " + spell + " for " + caster.Item1 + ".");
            }
            var effects = new Dictionary<string, EffectExpression>(admission.EffectsBySource, StringComparer.Ordinal);
            BlueprintAbility blueprint = dump.Get<BlueprintAbility>(spell);
            AlliedAreaCoverage area = new KingmakerAreaCoverageResolver().Resolve(blueprint, blueprint);
            var options = providers.Select(provider =>
            {
                EffectExpression expression = effects[provider.Key.Ability.Canonical];
                IReadOnlyDictionary<string, bool> facts = ProviderTargetingPlanner.CasterFacts(expression,
                    provider.EffectiveCasterLevel, id => null);
                ProviderTargetingDecision decision = ProviderTargetingPlanner.Decide(provider, expression, units, true,
                    false, area, nativeCanTarget ?? (id => true), (from, to) => 1f, facts, (unit, fact) => null,
                    fact => false);
                return ProviderTargetingPlanner.BuildOption(provider, decision, CastExecutionStrategy.DirectRuleCast,
                    "ordinary-direct-rule-cast", facts);
            }).ToList();
            return new GmfParty
            {
                Snapshot = new PartyProviderSnapshot(units, providers, pools),
                Options = options,
                Effects = effects,
                Providers = providers
            };
        }

        private static GmfParty GmfDruidParty(NativeBlueprintDump dump, string spell, int casterLevel, int slots,
            Func<string, bool> nativeCanTarget = null)
        {
            return GmfPartyOf(dump, spell, nativeCanTarget, Tuple.Create("druid", casterLevel, slots));
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

        // The Classic planner: one routine request per target, in order.
        private static CastPlan GmfClassicPlan(GmfParty party, IEnumerable<string> targets,
            ActiveEffectSnapshot live = null)
        {
            BuffCastRequest[] requests = targets.Select((target, index) => new BuffCastRequest(
                new BuffSourceDefinition(party.SourceId, party.Provider.Key.Ability, party.Raw,
                    CastGroupingKind.PerTarget),
                new[] { target }, ExistingEffectPolicy.SkipAlreadyActive, null, new EnhancementRequest[0],
                "gmf-" + index, null, null, null, index)).ToArray();
            return new CastPlanner().PlanRoutine(party.Snapshot, requests, party.Options,
                new ProviderSelectionPolicy(null, null, null), live ?? GmfLive());
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

        // Classic: the halting runner over the Classic planner's plan.
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
            // Spontaneous slots, and prepared slot tokens (true: unspent).
            internal int Slots;
            internal readonly Dictionary<string, bool> Tokens = new Dictionary<string, bool>(StringComparer.Ordinal);
            internal readonly List<string> SpentTokens = new List<string>();
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

            internal int Remaining { get { return Tokens.Count != 0 ? Tokens.Count(token => token.Value) : Slots; } }

            internal string[] Effects(string unit)
            {
                return Read(unit).Select(value => value.Kind + ":" + value.EffectId)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }

            public CastRuntimeValidation Validate(CastStep step)
            {
                string target = step.TargetUnitIds.FirstOrDefault();
                if (target == null || !NativeCanTarget(target)) return CastRuntimeValidation.Fail("target-invalid");
                bool available = step.Reservation.TokenIds.Count != 0
                    ? step.Reservation.TokenIds.All(id => Tokens.ContainsKey(id) && Tokens[id])
                    : Slots >= step.Reservation.Units;
                return available ? CastRuntimeValidation.Pass() : CastRuntimeValidation.Fail("simulated-exhausted-source");
            }

            public InstantCastResult Fire(CastStep step)
            {
                _baselines[step] = new EffectBaseline(step.EffectObservationUnitIds.ToDictionary(id => id, Read,
                    StringComparer.Ordinal));
                int before = Remaining;
                if (step.Reservation.TokenIds.Count != 0)
                    foreach (string token in step.Reservation.TokenIds)
                    {
                        Tokens[token] = false;
                        SpentTokens.Add(token);
                    }
                else Slots -= step.Reservation.Units;
                string target = step.TargetUnitIds[0];
                Fired.Add(target);
                Put(target, EffectKind.Buff, _buff, step.Provider.CasterUnitId);
                if (Application != "buff-only")
                    Put(target, EffectKind.WornItemEnchantment,
                        _tiers[_greater ? Math.Min(GmfNativeRank(CasterLevel), _tiers.Length) - 1 : 0], null);
                return new InstantCastResult(true, true, false, Remaining < before, true, "simulated-native-magic-fang",
                    AvailableCountJudgement.Violation(step.Reservation, before, Remaining));
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

        // The world for a party: its prepared tokens, its spontaneous slots.
        private static GmfWorld GmfWorldFor(GmfParty party, string buff, string[] tiers, bool greater, int casterLevel)
        {
            var world = new GmfWorld(buff, tiers, greater) { CasterLevel = casterLevel };
            foreach (ResourcePoolSnapshot pool in party.Snapshot.ResourcePools)
            {
                foreach (ResourceTokenSnapshot token in pool.Tokens) world.Tokens[token.TokenId] = token.Available;
                if (pool.Kind == ResourcePoolKind.SpontaneousLevel) world.Slots += pool.Remaining;
            }
            return world;
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

        // The planned steps of one run, by path: the casting-first
        // projection's or the Classic planner's.
        private static IReadOnlyList<CastStep> GmfSteps(GmfParty party, string[] order, bool classic,
            out ExplicitStepConversion explicitSteps, out CastPlan classicPlan, string label)
        {
            explicitSteps = null;
            classicPlan = null;
            if (classic)
            {
                classicPlan = GmfClassicPlan(party, order);
                MbbExpect(classicPlan.Outcomes.All(outcome => outcome.Kind == TargetOutcomeKind.Fulfilled),
                    label + ": Classic planned " + string.Join(",", classicPlan.Outcomes.Select(outcome =>
                        outcome.UnitId + "=" + outcome.Kind + ":" + outcome.Reason)));
                return classicPlan.Steps;
            }
            ExplicitCastingPlan plan = GmfCompile(party, order);
            MbbExpect(plan.Castings.All(casting => casting.Readiness == ResolvedCastingReadiness.Ready &&
                    casting.Cost.Sum(cost => cost.Units) == 1),
                label + ": " + string.Join(" / ", plan.Castings.Select(casting => casting.CastingId + "=" +
                    casting.Readiness + ":" + string.Join(",", casting.ReadinessReasons))));
            explicitSteps = GmfConvert(party, plan);
            MbbExpect(explicitSteps.Converted, label + ": " + explicitSteps.Refusal);
            return explicitSteps.Plan.Steps;
        }

        // Native 2.1.7b contract (Assembly-CSharp metadata and IL; nothing is
        // executed): what MagicFang.RunAction selects and enchants, what
        // EnchantSlot does to an existing instance, how ContextRankConfig
        // computes the rank the adapter binds, and which weapons UnitBody's
        // CurrentEquipmentSlots and polymorph-aware hand getters reach.
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
            MbbExpect(slots.Any(member => member.EndsWith("UnitBody::get_CurrentHandsEquipmentSet", StringComparison.Ordinal)) &&
                !slots.Any(member => member.Contains("EmptyHandWeapon") || member.Contains("AdditionalLimbs") ||
                    member.Contains("Polymo")),
                "Native UnitBody.CurrentEquipmentSlots changed which weapons it reaches.");
            List<string> primary = MbbIlMembers(typeof(UnitBody).GetProperty("PrimaryHand").GetGetMethod());
            MbbExpect(primary.Any(member => member.EndsWith("UnitBody::get_IsPolymorphed", StringComparison.Ordinal)) &&
                primary.Any(member => member.EndsWith("UnitBody::m_PolymoprphHandsEquipmentSet", StringComparison.Ordinal)),
                "Native UnitBody.PrimaryHand no longer follows the polymorph hand set.");
            // The fixture's own data: the rank the adapter binds.
            BlueprintAbility greater = GmfDump().Get<BlueprintAbility>(GmfAbility);
            MagicFang fang = GmfFang(greater);
            ContextRankConfig config = GmfRankConfig(greater);
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
                    GmfLeaves(bound).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[GmfNativeRank(level) - 1])) &&
                    GmfLeaves(party.Option.BindCasterLevel(party.Raw)).SequenceEqual(GmfLeaves(bound)),
                    "The admitted level-" + level + " provider expects " + string.Join(",", GmfLeaves(bound)) + ".");
            }
            // Ordinary Magic Fang: the buff and Enchantment[0] at every level.
            EffectExpression ordinary = GmfScan(dump.Get<BlueprintAbility>(GmfOrdinaryAbility));
            foreach (int level in new[] { 0, 1, 12, 40 })
                MbbExpect(GmfLeaves(GmfBind(ordinary, level)).SequenceEqual(GmfExpected(GmfOrdinaryBuff, GmfTiers[0])),
                    "Ordinary Magic Fang changed at caster level " + level + ".");
        }

        // The identity earlier versions gave Magic Fang: one sequence of every
        // array element in GUID order, then the duration buff.
        private static string GmfPriorIdentity(string buff, IEnumerable<string> tiers)
        {
            return "ability-reference(sequence[sequence[sequence[" + string.Join(";", tiers
                .OrderBy(id => id, StringComparer.Ordinal).Select(id => "leaf(2,32:" + id + ",0)").ToArray()) +
                "];leaf(0,32:" + buff + ",0)]])";
        }

        // Saved castings and assignments name a spell by its effect
        // aggregate. The repaired graphs keep the identity the pre-repair
        // adapter gave them - native, with replacement tiers, and ordinary
        // (this regression also passes on the 9137987 baseline, which is what
        // establishes the expected strings).
        private static void TestGmfIdentity()
        {
            NativeBlueprintDump dump = GmfDump();
            EffectExpression greater = GmfScan(dump.Get<BlueprintAbility>(GmfAbility));
            MbbExpect(EffectAggregateIdentity.Describe(greater) == GmfPriorIdentity(GmfBuff, GmfTiers),
                "Greater Magic Fang's catalogue identity changed: " + EffectAggregateIdentity.Describe(greater));
            BlueprintAbility replaced = GmfDump().Get<BlueprintAbility>(GmfAbility);
            GmfReplaceTiers(replaced, GmfReplacementTiers);
            MbbExpect(EffectAggregateIdentity.Describe(GmfScan(replaced)) == GmfPriorIdentity(GmfBuff, GmfReplacementTiers),
                "Greater Magic Fang with replacement tiers changed identity: " +
                EffectAggregateIdentity.Describe(GmfScan(replaced)));
            EffectExpression ordinary = GmfScan(dump.Get<BlueprintAbility>(GmfOrdinaryAbility));
            MbbExpect(EffectAggregateIdentity.Describe(ordinary) == GmfPriorIdentity(GmfOrdinaryBuff, new[] { GmfTiers[0] }),
                "Ordinary Magic Fang's catalogue identity changed: " + EffectAggregateIdentity.Describe(ordinary));
            AbilityKey key = MbbSpellKey(GmfAbility);
            MbbExpect(CatalogSourceIdentity.For(key, greater) == EffectAggregateIdentity.For(greater, key.Canonical) &&
                EffectAggregateIdentity.IsAggregate(CatalogSourceIdentity.For(key, greater)),
                "Greater Magic Fang is no longer a spell aggregate.");
        }

        // 8. Call of the Wild replaces MagicFangGreater's array with five
        // ordered enchantments of its own; the binding follows the ARRAY
        // ORDER, whatever the identities, and never a native GUID.
        private static void TestGmfReplacementEnchantments()
        {
            foreach (bool reversed in new[] { false, true })
            {
                BlueprintAbility ability = GmfDump().Get<BlueprintAbility>(GmfAbility);
                string[] order = reversed ? GmfReplacementTiers.Reverse().ToArray() : GmfReplacementTiers;
                GmfReplaceTiers(ability, order);
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
            GmfFang(ordinary).Enchantment[0] = NativeBlueprintDump.Stub<BlueprintWeaponEnchantment>(GmfReplacementTiers[0]);
            MbbExpect(GmfLeaves(GmfBind(GmfScan(ordinary), 12)).SequenceEqual(GmfExpected(GmfOrdinaryBuff,
                    GmfReplacementTiers[0])),
                "A replaced ordinary Magic Fang enchantment is not the expected one.");
            // A constant level is provable: the native index of that constant.
            BlueprintAbility constant = GmfDump().Get<BlueprintAbility>(GmfAbility);
            GmfFang(constant).EnchantLevel.ValueType = ContextValueType.Simple;
            GmfFang(constant).EnchantLevel.Value = 9;
            MbbExpect(GmfLeaves(GmfBind(GmfScan(constant), 3)).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[4])),
                "A constant enchant level of 9 is not the last tier.");
        }

        // Robustness (deliberately modified blueprints, not observations of
        // the installed game): a level contract the adapter cannot prove
        // leaves the tier unknown - never every tier, never any tier - and
        // the classifier refuses the spell, so its buff is never planned and
        // confirmed alone.
        private static void TestGmfUnprovenContracts()
        {
            var modifications = new Dictionary<string, Action<BlueprintAbility>>
            {
                { "no rank config", ability => GmfSet(ability, "Components", ability.ComponentsArray
                    .Where(component => !ReferenceEquals(component, GmfRankConfig(ability))).ToArray()) },
                { "two rank configs", ability => GmfSet(ability, "Components", ability.ComponentsArray
                    .Concat(new BlueprintComponent[] { GmfRankConfig(ability) }).ToArray()) },
                { "class-level rank", ability => GmfSet(GmfRankConfig(ability), "m_BaseValueType",
                    ContextRankBaseValueType.ClassLevel) },
                { "custom progression", ability => GmfSet(GmfRankConfig(ability), "m_Progression",
                    ContextRankProgression.Custom) },
                { "shared value", ability => GmfFang(ability).EnchantLevel.ValueType = ContextValueType.Shared },
                { "constant zero", ability =>
                    {
                        GmfFang(ability).EnchantLevel.ValueType = ContextValueType.Simple;
                        GmfFang(ability).EnchantLevel.Value = 0;
                    } },
                { "missing element", ability => GmfFang(ability).Enchantment[2] = null },
                { "overflowing step", ability =>
                    {
                        GmfSet(GmfRankConfig(ability), "m_Progression", ContextRankProgression.MultiplyByModifier);
                        GmfSet(GmfRankConfig(ability), "m_StepLevel", 43000000);
                    } }
            };
            foreach (KeyValuePair<string, Action<BlueprintAbility>> modification in modifications)
            {
                NativeBlueprintDump dump = GmfDump();
                BlueprintAbility ability = dump.Get<BlueprintAbility>(GmfAbility);
                modification.Value(ability);
                DiscoveryScanResult result = GmfScanResult(ability);
                MbbExpect(!GmfLeaves(result.Expression).Any(leaf => leaf.StartsWith(
                        EffectKind.WornItemEnchantment.ToString(), StringComparison.Ordinal)) &&
                    result.Diagnostics.Any(value => value.Detail == "MagicFang:enchant-level-unverified"),
                    "Modified (" + modification.Key + ") Greater Magic Fang was not left unknown: " +
                    string.Join(",", GmfLeaves(result.Expression)));
                var providers = new List<ProviderSnapshot>();
                ResourcePoolSnapshot pool = PartyProviderAdmission.SpontaneousLevelPool("druid", "book-druid", 3, 2, 2);
                MbbExpect(GmfAdmit(dump, MbbAdmission(dump), "druid", GmfAbility, 12, pool, providers) == null &&
                    providers.Count == 0,
                    "Modified (" + modification.Key + ") Greater Magic Fang was admitted with its buff alone.");
            }
            // Review round 2: cast through a referenced ability (a native
            // ContextActionCastSpell wrapper), Greater Magic Fang runs in a
            // context of its own whose caster level is not the wrapper
            // provider's: its tier is not bound to it.
            BlueprintAbility inner = GmfDump().Get<BlueprintAbility>(GmfAbility);
            var cast = GmfUnityDetached<ContextActionCastSpell>();
            GmfSet(cast, "Spell", inner);
            var list = GmfDetached<Kingmaker.ElementsSystem.ActionList>();
            GmfSet(list, "Actions", new Kingmaker.ElementsSystem.GameAction[] { cast });
            var runAction = GmfUnityDetached<AbilityEffectRunAction>();
            GmfSet(runAction, "Actions", list);
            BlueprintAbility wrapper = NativeBlueprintDump.Stub<BlueprintAbility>("0000000000000000000000000000c0a5");
            GmfSet(wrapper, "Components", new BlueprintComponent[] { runAction });
            DiscoveryScanResult nested = GmfScanResult(wrapper);
            MbbExpect(!GmfLeaves(nested.Expression).Any(leaf => leaf.StartsWith(
                    EffectKind.WornItemEnchantment.ToString(), StringComparison.Ordinal)) &&
                nested.Diagnostics.Any(value => value.Detail == "MagicFang:enchant-level-unverified"),
                "Greater Magic Fang cast through a wrapper was bound to the wrapper's caster level: " +
                string.Join(",", GmfLeaves(nested.Expression)));
        }

        // A detached Unity-derived native object that compares as non-null.
        private static T GmfUnityDetached<T>() where T : class
        {
            T value = GmfDetached<T>();
            GmfSet(value, "m_CachedPtr", new IntPtr(1));
            return value;
        }

        private static ObservedEffectInstance GmfSeen(EffectKind kind, string effect, string key, long end = 100,
            bool suppressed = false)
        {
            return new ObservedEffectInstance(kind, effect, key, end, suppressed, null,
                kind == EffectKind.Buff ? "druid" : null);
        }

        private static KeyValuePair<IEnumerable<ObservedEffectInstance>, IDictionary<string, ObservedEffectInstance[]>> Pair(
            IEnumerable<ObservedEffectInstance> before, IDictionary<string, ObservedEffectInstance[]> after)
        {
            return new KeyValuePair<IEnumerable<ObservedEffectInstance>, IDictionary<string, ObservedEffectInstance[]>>(
                before, after);
        }

        // 2 and 3. Confirmation on the intended recipient needs the buff AND
        // the cast's own tier freshly applied (native EnchantSlot re-adds it
        // on every cast); nothing weaker, stronger, unrelated, suppressed,
        // misplaced or left over from an earlier cast proves the application.
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
            ObservedEffectInstance oldBuff = GmfSeen(EffectKind.Buff, GmfBuff, "buff-0");
            Func<int, string, ObservedEffectInstance> tier = (rank, key) => GmfSeen(EffectKind.WornItemEnchantment,
                GmfTiers[rank - 1], key);
            MbbExpect(reached(none, on("ranger-pet", new[] { buff, tier(3, "t3-new") }), expected),
                "The buff and the cast's +3 on the other character's pet did not confirm.");
            MbbExpect(reached(new[] { oldBuff, tier(3, "t3-old") }, on("ranger-pet", new[] { buff, tier(3, "t3-new") }),
                    expected),
                "A recast that re-applied +3 over an earlier +3 did not confirm.");
            var refused = new Dictionary<string, KeyValuePair<IEnumerable<ObservedEffectInstance>,
                IDictionary<string, ObservedEffectInstance[]>>>
            {
                { "missing buff", Pair(none, on("ranger-pet", new[] { tier(3, "t3") })) },
                { "missing enchantment", Pair(none, on("ranger-pet", new[] { buff })) },
                { "weaker tier", Pair(none, on("ranger-pet", new[] { buff, tier(2, "t2") })) },
                { "stronger tier", Pair(none, on("ranger-pet", new[] { buff, tier(4, "t4") })) },
                { "unrelated buff", Pair(none, on("ranger-pet", new[] { GmfSeen(EffectKind.Buff, GmfOrdinaryBuff, "o"), tier(3, "t3") })) },
                { "wrong recipient", Pair(none, on("druid-pet", new[] { buff, tier(3, "t3") })) },
                { "suppressed buff", Pair(none, on("ranger-pet", new[] { GmfSeen(EffectKind.Buff, GmfBuff, "s", 100, true), tier(3, "t3") })) },
                { "unchanged old instances", Pair(new[] { oldBuff, tier(3, "t3-old") },
                    on("ranger-pet", new[] { oldBuff, tier(3, "t3-old") })) },
                // Native EnchantSlot at another caster level left the earlier
                // +3 untouched and applied +2: the fresh buff does not make the
                // old +3 this cast's.
                { "old tier left while another was applied", Pair(new[] { oldBuff, tier(3, "t3-old") },
                    on("ranger-pet", new[] { buff, tier(3, "t3-old"), tier(2, "t2-new") })) },
                // A permanent instance of the tier, which EnchantSlot never
                // touches: unproven, so unconfirmed (conservative).
                { "unchanged instance of the tier under a fresh buff", Pair(new[] { oldBuff, tier(3, "t3-old") },
                    on("ranger-pet", new[] { buff, tier(3, "t3-old") })) }
            };
            foreach (KeyValuePair<string, KeyValuePair<IEnumerable<ObservedEffectInstance>,
                IDictionary<string, ObservedEffectInstance[]>>> refusal in refused)
            {
                MbbExpect(!reached(refusal.Value.Key, refusal.Value.Value, expected),
                    "Greater Magic Fang confirmed from " + refusal.Key + ".");
                ObservedEffectInstance[] after;
                MbbExpect(!refusal.Value.Value.TryGetValue("ranger-pet", out after) ||
                    !AppliedEffectJudgement.Reached(expected, refusal.Value.Key, after),
                    "Greater Magic Fang reached (unscoped) from " + refusal.Key + ".");
            }
            // Review round 2: the tier seen by an earlier progress read but
            // gone at the read that judges the cast proves nothing.
            var progressBaseline = new EffectBaseline(GmfUnits().ToDictionary(unit => unit.UnitId,
                unit => (IEnumerable<ObservedEffectInstance>)none, StringComparer.Ordinal));
            progressBaseline.RecordObservation(new Dictionary<string, IEnumerable<ObservedEffectInstance>>
                { { "ranger-pet", new[] { buff, tier(3, "t3-new") } } });
            MbbExpect(!AppliedEffectJudgement.AllReached(new[] { "ranger-pet" }, expected, progressBaseline,
                    unit => unit == "ranger-pet" ? new[] { buff, tier(2, "t2-new") } : none, null, "druid"),
                "A +3 seen only by an earlier progress read confirmed the cast.");
            // An unknown caster level: whatever tier appears, nothing proves this cast's.
            EffectExpression unresolved = GmfBind(party.Raw, 0);
            MbbExpect(CasterLevelPredicate.HasUnresolved(unresolved) &&
                !Enumerable.Range(1, 5).Any(rank => reached(none, on("ranger-pet", new[] { buff, tier(rank, "t") }),
                    unresolved)) &&
                !AppliedEffectJudgement.StillCovered(unresolved, new[] { oldBuff, tier(3, "t3") },
                    new[] { oldBuff, tier(3, "t3") }) &&
                AppliedEffectJudgement.StillCovered(expected, new[] { oldBuff, tier(3, "t3") },
                    new[] { oldBuff, tier(3, "t3") }),
                "A Greater Magic Fang whose caster level was unknown was confirmed or kept as covered.");
        }

        // PR #29 review: "could not read" is not "read and found nothing".
        // The production enchantment read on detached native objects. The pet
        // is polymorphed and its polymorph hand set cannot be read before the
        // cast (native UnitBody.PrimaryHand dereferences
        // m_PolymoprphHandsEquipmentSet and throws), so the old +3 on its
        // polymorph claw is not seen. The cast applies +2 (a different caster
        // level) and refreshes the buff, and afterwards the claw is readable
        // with its unchanged +3. KingmakerEffectInstanceReader.ReadBaseline
        // turns a read that throws into a refused baseline (nothing is cast);
        // a read that returned only the readable remainder would make the old
        // +3 look freshly applied and confirm the cast.
        private static void TestGmfIncompleteWeaponBaseline()
        {
            NativeBlueprintDump dump = GmfDump();
            GmfParty party = GmfDruidParty(dump, GmfAbility, 12, 1);
            EffectExpression expected = party.Option.BindEffects(party.Raw, "ranger-pet");
            var ids = new HashSet<string>(GmfTiers.Concat(new[] { GmfBuff }), StringComparer.Ordinal);
            var none = new ObservedEffectInstance[0];
            ObservedEffectInstance oldBuff = GmfSeen(EffectKind.Buff, GmfBuff, "buff-old");
            ObservedEffectInstance freshBuff = GmfSeen(EffectKind.Buff, GmfBuff, "buff-new");
            // ReadBaseline's contract: a read that throws is no baseline.
            Func<UnitBody, List<ObservedEffectInstance>> baseline = body =>
            {
                try { return KingmakerEnchantedItems.Instances(body, ids).Concat(new[] { oldBuff }).ToList(); }
                catch (Exception) { return null; }
            };
            Func<UnitBody, List<ObservedEffectInstance>> after = body =>
                KingmakerEnchantedItems.Instances(body, ids).Concat(new[] { freshBuff }).ToList();
            Func<List<ObservedEffectInstance>, List<ObservedEffectInstance>, bool> confirms = (before, seen) =>
                AppliedEffectJudgement.AllReached(new[] { "ranger-pet" }, expected,
                    new EffectBaseline(GmfUnits().ToDictionary(unit => unit.UnitId,
                        unit => unit.UnitId == "ranger-pet" ? (IEnumerable<ObservedEffectInstance>)before : none,
                        StringComparer.Ordinal)),
                    unit => unit == "ranger-pet" ? (IEnumerable<ObservedEffectInstance>)seen : none, null, "druid");

            ItemEntityWeapon claw = GmfEnchanted(GmfEnchantment(GmfTiers[2])), unarmed = GmfEnchanted(),
                bite = GmfEnchanted();
            UnitBody pet = GmfPolymorphed(unarmed, bite);
            bool readFailed = GmfThrows(() => KingmakerEnchantedItems.ReadComplete(pet)) &&
                GmfThrows(() => KingmakerEnchantedItems.Instances(pet, ids));
            List<ObservedEffectInstance> unreadable = baseline(pet);
            GmfSet(pet, "m_PolymoprphHandsEquipmentSet", GmfHands(claw, null));
            GmfCast(claw, GmfTiers[1]);
            List<ObservedEffectInstance> recovered = after(pet);
            MbbExpect(recovered.Count(value => value.EffectId == GmfTiers[2]) == 1 &&
                recovered.Count(value => value.EffectId == GmfTiers[1]) == 1,
                "The recovered read does not show the old +3 and the new +2.");
            // What the refusal prevents: a baseline without the +3 would confirm.
            MbbExpect(confirms(new List<ObservedEffectInstance> { oldBuff }, recovered),
                "The hazard is not reproduced: a baseline without the +3 no longer confirms the old +3.");
            MbbExpect(unreadable == null || !confirms(unreadable, recovered),
                "An old +3 hidden by an unreadable baseline confirmed a cast that applied +2.");
            MbbExpect(unreadable == null && readFailed,
                "A pre-cast read that could not see the polymorph hands was accepted as a baseline (" +
                (unreadable == null ? 0 : unreadable.Count) + " instances).");
            // The planning view keeps the readable items (an omitted one only
            // makes the planner cast).
            GmfExpectItems(KingmakerEnchantedItems.Read(GmfPolymorphed(unarmed, bite)),
                new ItemEntity[] { unarmed, bite }, "planning view with unreadable polymorph hands");

            // A complete baseline that sees the old +3: the same cast is refused.
            ItemEntityWeapon seenClaw = GmfEnchanted(GmfEnchantment(GmfTiers[2]));
            UnitBody visible = GmfPolymorphed(unarmed, bite);
            GmfSet(visible, "m_PolymoprphHandsEquipmentSet", GmfHands(seenClaw, null));
            List<ObservedEffectInstance> complete = baseline(visible);
            GmfCast(seenClaw, GmfTiers[1]);
            MbbExpect(complete != null && complete.Count(value => value.EffectId == GmfTiers[2]) == 1 &&
                !confirms(complete, after(visible)),
                "A complete baseline holding the old +3 did not refuse a cast that applied +2.");

            // A complete baseline, then the cast's own +3 newly applied: confirmed.
            ItemEntityWeapon bareClaw = GmfEnchanted();
            UnitBody bare = GmfPolymorphed(unarmed, bite);
            GmfSet(bare, "m_PolymoprphHandsEquipmentSet", GmfHands(bareClaw, null));
            List<ObservedEffectInstance> empty = baseline(bare);
            GmfCast(bareClaw, GmfTiers[2]);
            MbbExpect(empty != null && confirms(empty, after(bare)),
                "A complete baseline followed by a newly applied +3 and a fresh buff did not confirm.");
        }

        private static UnitBody GmfPolymorphed(ItemEntity emptyHand, ItemEntity limb)
        {
            UnitBody body = GmfBody(null, null, null, emptyHand, new ItemEntity[0]);
            GmfSet(body, "<IsPolymorphed>k__BackingField", true);
            GmfSet(body, "m_PolymorphAdditionalLimbs", GmfLimbs(new[] { limb }));
            return body;
        }

        // A detached native enchantment of a blueprint, temporary (an end time).
        private static ItemEnchantment GmfEnchantment(string blueprint)
        {
            var enchantment = GmfDetached<ItemEnchantment>();
            BlueprintWeaponEnchantment stub = NativeBlueprintDump.Stub<BlueprintWeaponEnchantment>(blueprint);
            GmfSet(stub, "m_CachedPtr", new IntPtr(1));
            GmfSet(enchantment, "Blueprint", stub);
            GmfSet(enchantment, "<EndTime>k__BackingField", TimeSpan.FromMinutes(10));
            return enchantment;
        }

        // A detached weapon whose native Enchantments getter returns these
        // (ItemEntity.UpdateCachedEnchantments keeps the cache while the
        // enchantment collection is absent).
        private static ItemEntityWeapon GmfEnchanted(params ItemEnchantment[] enchantments)
        {
            var weapon = GmfDetached<ItemEntityWeapon>();
            GmfSet(weapon, "m_CachedEnchantments", enchantments.ToList());
            return weapon;
        }

        // Native EnchantSlot adds a new temporary instance of the tier.
        private static void GmfCast(ItemEntityWeapon weapon, string tier)
        {
            ((List<ItemEnchantment>)GmfField(weapon, "m_CachedEnchantments")).Add(GmfEnchantment(tier));
        }

        private static bool GmfThrows(Action read)
        {
            try { read(); return false; }
            catch (Exception) { return true; }
        }

        // Existing effects (casting-first policy, unchanged): a weaker earlier
        // tier never satisfies the stronger casting; a stronger earlier tier
        // is not the cast's tier, and native MagicFang adds the cast's tier
        // beside it (enhancement bonuses do not stack), so it casts; and an
        // enchantment's caster level is unreadable, so even the same tier is
        // never provably sufficient. Neither enchantment nor buff is ever
        // accepted alone, and the caster-free setup view never calls a tier
        // complete coverage.
        private static void TestGmfExistingEffects()
        {
            NativeBlueprintDump dump = GmfDump();
            GmfParty party = GmfDruidParty(dump, GmfAbility, 12, 1);
            EffectExpression expected = party.Option.BindEffects(party.Raw, "ranger-pet");
            var requirement = new ExistingEffectRequirement(12, 0, 7200, true, null, true, "druid");
            Func<string, int, ActiveEffectInstance> buff = (id, level) => new ActiveEffectInstance(EffectKind.Buff, id,
                5000, level, 0, false, true, "druid");
            Func<string, ActiveEffectInstance> tier = id => new ActiveEffectInstance(EffectKind.WornItemEnchantment, id,
                null, null, null);
            Func<ActiveEffectInstance[], ExistingEffectRecipientAssessment> assess = instances =>
                ExistingEffectSufficiency.AssessScoped("druid", "ranger-pet", expected,
                    unit => unit == "ranger-pet" ? instances : new ActiveEffectInstance[0], null, requirement);
            MbbExpect(assess(new[] { buff(GmfBuff, 8), tier(GmfTiers[1]) }).Verdict == ExistingEffectVerdict.NotPresent,
                "An earlier +2 satisfied a +3 casting.");
            MbbExpect(assess(new[] { buff(GmfBuff, 16), tier(GmfTiers[3]) }).Verdict == ExistingEffectVerdict.NotPresent,
                "An earlier +4 was taken as the +3 casting's own effect.");
            ExistingEffectRecipientAssessment same = assess(new[] { buff(GmfBuff, 12), tier(GmfTiers[2]) });
            MbbExpect(same.Verdict == ExistingEffectVerdict.Insufficient &&
                same.Reasons.Any(reason => reason.StartsWith("caster-level-unverified", StringComparison.Ordinal)),
                "An existing +3 became provably sufficient: " + same.Verdict + " " + string.Join(",", same.Reasons));
            MbbExpect(assess(new[] { buff(GmfBuff, 20) }).Verdict == ExistingEffectVerdict.NotPresent &&
                assess(new[] { tier(GmfTiers[2]) }).Verdict == ExistingEffectVerdict.NotPresent,
                "The buff or the enchantment alone was taken as the effect.");
            // Both planners with that live state: the casting stays planned
            // and reserves one slot.
            ActiveEffectSnapshot stronger = GmfLive(new Dictionary<string, ActiveEffectInstance[]>
                { { "ranger-pet", new[] { buff(GmfBuff, 16), tier(GmfTiers[3]) } } });
            ResolvedCasting casting = GmfCompile(party, new[] { "ranger-pet" }, stronger).Castings.Single();
            MbbExpect(casting.Readiness == ResolvedCastingReadiness.Ready && casting.Cost.Sum(cost => cost.Units) == 1,
                "An earlier +4 changed the +3 casting: " + casting.Readiness + " " +
                string.Join(",", casting.ReadinessReasons));
            CastPlan classic = GmfClassicPlan(party, new[] { "ranger-pet" }, stronger);
            MbbExpect(classic.Steps.Count == 1 && classic.Steps[0].Reservation.Units == 1,
                "Classic skipped the +3 casting over an earlier +4.");
            // Classic presence: even the cast's own tier and buff present
            // (the tier could be an Oil of Enhancement on a manufactured
            // weapon - the snapshot does not say which item has it) never
            // skip the casting, as in the casting-first policy.
            CastPlan sameTier = GmfClassicPlan(party, new[] { "ranger-pet" }, GmfLive(
                new Dictionary<string, ActiveEffectInstance[]>
                    { { "ranger-pet", new[] { buff(GmfBuff, 12), tier(GmfTiers[2]) } } }));
            MbbExpect(sameTier.Steps.Count == 1 && sameTier.Outcomes.All(outcome => outcome.Kind == TargetOutcomeKind.Fulfilled),
                "Classic took an existing +3 as Greater Magic Fang already active: " +
                string.Join(",", sameTier.Outcomes.Select(outcome => outcome.Kind + ":" + outcome.Reason)));
            // The setup view (no caster bound) with +1 and the buff present.
            var weak = GmfLive(new Dictionary<string, ActiveEffectInstance[]>
                { { "ranger-pet", new[] { buff(GmfBuff, 4), tier(GmfTiers[0]) } } });
            var catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("gmf-campaign"), party.Snapshot,
                weak, party.Effects, party.Options, ignored => { });
            MbbExpect(catalogue.GetPresence(party.SourceId, "ranger-pet") == EffectPresenceKind.Partial,
                "The setup view called +1 complete Greater Magic Fang coverage: " +
                catalogue.GetPresence(party.SourceId, "ranger-pet"));
            GmfParty ordinary = GmfDruidParty(dump, GmfOrdinaryAbility, 12, 1);
            var ordinaryCatalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("gmf-campaign"),
                ordinary.Snapshot, GmfLive(new Dictionary<string, ActiveEffectInstance[]>
                    { { "ranger-pet", new[] { buff(GmfOrdinaryBuff, 4), tier(GmfTiers[0]) } } }),
                ordinary.Effects, ordinary.Options, ignored => { });
            MbbExpect(ordinaryCatalogue.GetPresence(ordinary.SourceId, "ranger-pet") == EffectPresenceKind.Complete,
                "Ordinary Magic Fang's setup presence changed.");
        }

        // 4. The report: one Druid, two pets of different masters. Native
        // targeting admits both; both castings plan, cast and confirm - in
        // either order, instant and animated, through the casting-first
        // compiler/converter/coordinator and through the Classic planner and
        // halting runner - and exactly two slots are spent.
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
                        ExplicitStepConversion explicitSteps;
                        CastPlan classicPlan;
                        IReadOnlyList<CastStep> steps = GmfSteps(party, order, classic, out explicitSteps, out classicPlan,
                            label);
                        MbbExpect(steps.Select(step => step.TargetUnitIds.Single()).SequenceEqual(order) &&
                            steps.All(step => step.Reservation.Units == 1 && step.Reservation.TokenIds.Count == 1) &&
                            steps.Select(step => step.Reservation.TokenIds.Single()).Distinct(StringComparer.Ordinal)
                                .Count() == 2,
                            label + ": the castings did not plan in order with one prepared slot each: " +
                            string.Join(",", steps.Select(step => string.Join("+", step.Reservation.TokenIds))));
                        GmfWorld world = GmfWorldFor(party, GmfBuff, GmfTiers, true, 12);
                        if (classic)
                        {
                            int? halted;
                            ExecutionReport report = GmfRunClassic(world, classicPlan, animated, out halted);
                            MbbExpect(halted == null && report.Confirmed == 2 && report.ResourcesSpent == 2 &&
                                report.Failed == 0,
                                label + ": halted after " + halted + "; " + string.Join(" | ", report.Records.Select(
                                    record => record.Status + ":" + record.Detail)));
                        }
                        else
                        {
                            ExplicitCastingRunOutcome outcome = GmfRunExplicit(world, explicitSteps, animated);
                            MbbExpect(outcome.AllConfirmed && outcome.Entries.Count == 2 &&
                                outcome.Entries.All(entry => entry.Confirmed && entry.ResourceSpentReported),
                                label + ": halted after " + outcome.HaltedAfterCastingId + " " + outcome.HaltReason + "; " +
                                string.Join(" | ", outcome.Entries.Select(entry => entry.FinalStatus + ":" + entry.Detail)));
                        }
                        MbbExpect(world.Fired.SequenceEqual(order) && world.Remaining == 0 &&
                            world.SpentTokens.SequenceEqual(steps.Select(step => step.Reservation.TokenIds.Single())),
                            label + ": fired " + string.Join(",", world.Fired) + ", spent " +
                            string.Join(",", world.SpentTokens) + ", " + world.Remaining + " slots left.");
                        foreach (string pet in order)
                            MbbExpect(world.Effects(pet).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[2])),
                                label + ": " + pet + " has " + string.Join(",", world.Effects(pet)));
                        foreach (CastStep step in steps)
                            MbbExpect(GmfLeaves(step.ExpectedEffects).SequenceEqual(GmfExpected(GmfBuff, GmfTiers[2])) &&
                                step.ExpectedRecipientUnitIds.SequenceEqual(step.TargetUnitIds),
                                label + ": a step expects " + string.Join(",", GmfLeaves(step.ExpectedEffects)) + " on " +
                                string.Join(",", step.ExpectedRecipientUnitIds));
                    }
        }

        // 5. Preserved refusals: native target rejection (while planning and
        // at cast time) and a genuinely unconfirmed cast (no natural weapon
        // took the enchantment, the native context's caster level gave
        // another tier, or it did so beside an old copy of the planned tier)
        // still stop every later casting on both paths.
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
            CastPlan refusedClassic = GmfClassicPlan(refusedParty, new[] { "ranger-pet" });
            MbbExpect(blocked.Readiness != ResolvedCastingReadiness.Ready && blocked.Cost.Count == 0 &&
                (!refusedSteps.Converted || refusedSteps.Plan.Steps.Count == 0) && refusedClassic.Steps.Count == 0,
                "A natively refused pet was planned: " + blocked.Readiness);
            string[] order = { "ranger-pet", "druid-pet" };
            var cases = new Dictionary<string, Action<GmfWorld>>
            {
                { "cast-time target rejection", world => world.NativeCanTarget = id => id != "ranger-pet" },
                { "no natural weapon enchanted", world => world.Application = "buff-only" },
                { "native context caster level 8", world => world.CasterLevel = 8 },
                { "caster level 8 beside an old +3", world =>
                    {
                        world.CasterLevel = 8;
                        world.Put("ranger-pet", EffectKind.Buff, GmfBuff, "druid");
                        world.Put("ranger-pet", EffectKind.WornItemEnchantment, GmfTiers[2], null);
                    } }
            };
            foreach (KeyValuePair<string, Action<GmfWorld>> scenario in cases)
                foreach (bool animated in new[] { false, true })
                    foreach (bool classic in new[] { false, true })
                    {
                        string label = scenario.Key + "/" + (animated ? "animated" : "instant") + "/" +
                            (classic ? "classic" : "casting-first");
                        GmfParty party = GmfDruidParty(dump, GmfAbility, 12, 2);
                        ExplicitStepConversion explicitSteps;
                        CastPlan classicPlan;
                        IReadOnlyList<CastStep> steps = GmfSteps(party, order, classic, out explicitSteps, out classicPlan,
                            label);
                        MbbExpect(steps.Count == 2, label + ": " + steps.Count + " steps.");
                        GmfWorld world = GmfWorldFor(party, GmfBuff, GmfTiers, true, 12);
                        scenario.Value(world);
                        bool rejected = scenario.Key.StartsWith("cast-time", StringComparison.Ordinal);
                        if (classic)
                        {
                            int? halted;
                            ExecutionReport report = GmfRunClassic(world, classicPlan, animated, out halted);
                            MbbExpect(halted == 0 && report.Confirmed == 0 &&
                                report.Records.Any(record => record.Status == (rejected
                                    ? CastExecutionStatus.FailedValidation : CastExecutionStatus.TimedOutUnconfirmed)) &&
                                report.Records.Any(record => record.Detail == HaltingPlanRunner.HaltedDetailPrefix + "0"),
                                label + ": " + string.Join(" | ", report.Records.Select(record => record.Status + ":" +
                                    record.Detail)));
                        }
                        else
                        {
                            ExplicitCastingRunOutcome outcome = GmfRunExplicit(world, explicitSteps, animated);
                            MbbExpect(outcome.Halted && !outcome.AllConfirmed &&
                                outcome.HaltedAfterCastingId == "gmf-0" && !outcome.Entries.Any(entry => entry.Confirmed),
                                label + ": halted after " + outcome.HaltedAfterCastingId + "; " +
                                string.Join(" | ", outcome.Entries.Select(entry => entry.FinalStatus + ":" + entry.Detail)));
                        }
                        MbbExpect(world.Fired.SequenceEqual(rejected ? new string[0] : new[] { "ranger-pet" }) &&
                            world.Remaining == (rejected ? 2 : 1),
                            label + ": fired " + string.Join(",", world.Fired) + " with " + world.Remaining + " slots left.");
                    }
        }

        // An unreadable caster level (0) leaves the tier unresolved: neither
        // planner offers a target or reserves a slot, so nothing is cast that
        // could never be confirmed.
        private static void TestGmfUnknownCasterLevel()
        {
            NativeBlueprintDump dump = GmfDump();
            GmfParty party = GmfDruidParty(dump, GmfAbility, 0, 2);
            MbbExpect(party.Option.ReachableTargetIds.Count == 0 && party.Option.LegalAnchorIds.Count == 0,
                "A provider with an unknown caster level offered targets: " +
                string.Join(",", party.Option.ReachableTargetIds));
            ResolvedCasting casting = GmfCompile(party, new[] { "ranger-pet" }).Castings.Single();
            ExplicitStepConversion steps = GmfConvert(party, GmfCompile(party, new[] { "ranger-pet" }));
            CastPlan classic = GmfClassicPlan(party, new[] { "ranger-pet" }, GmfLive(
                new Dictionary<string, ActiveEffectInstance[]>
                {
                    { "ranger-pet", new[]
                        {
                            new ActiveEffectInstance(EffectKind.Buff, GmfBuff, 5000, 4, 0, false, true, "druid"),
                            new ActiveEffectInstance(EffectKind.WornItemEnchantment, GmfTiers[0], null, null, null)
                        } }
                }));
            MbbExpect(casting.Readiness != ResolvedCastingReadiness.Ready && casting.Cost.Count == 0 &&
                (!steps.Converted || steps.Plan.Steps.Count == 0) && classic.Steps.Count == 0 &&
                classic.Outcomes.All(outcome => outcome.Kind != TargetOutcomeKind.Fulfilled &&
                    outcome.Kind != TargetOutcomeKind.SkippedAlreadyActive),
                "An unknown caster level was planned or skipped as active: " + casting.Readiness + " / " +
                string.Join(",", classic.Outcomes.Select(outcome => outcome.Kind + ":" + outcome.Reason)));
        }

        // The one-pass forecast: a funded casting grants a free structural
        // repeat only to a casting that applies the same tier. A level-8
        // Druid's +2 never satisfies a level-12 caster's +3 on the same pet;
        // a second level-8 casting is still satisfied.
        private static void TestGmfForecastTiers()
        {
            NativeBlueprintDump dump = GmfDump();
            foreach (int rangerLevel in new[] { 12, 8 })
            {
                GmfParty party = GmfPartyOf(dump, GmfAbility, null, Tuple.Create("druid", 8, 1),
                    Tuple.Create("ranger", rangerLevel, 1));
                ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(CastingDocument(
                        DirectCasting("druid-cast", "long", "druid", "ranger-pet", party.SourceId, party.Provider.Key.Ability),
                        DirectCasting("ranger-cast", "long", "ranger", "ranger-pet", party.SourceId,
                            party.Providers[1].Key.Ability)),
                    party.Snapshot, party.Options, party.Effects, null, "long", null, true, GmfLive());
                ResolvedCasting first = plan.CastingById("druid-cast"), second = plan.CastingById("ranger-cast");
                bool stronger = rangerLevel == 12;
                MbbExpect(first.Readiness == ResolvedCastingReadiness.Ready && first.Cost.Sum(cost => cost.Units) == 1 &&
                    (stronger
                        ? second.Readiness == ResolvedCastingReadiness.Ready && second.Cost.Sum(cost => cost.Units) == 1
                        : second.Readiness == ResolvedCastingReadiness.AlreadySatisfied && second.Cost.Count == 0),
                    "Level-" + rangerLevel + " casting after a level-8 one: " + second.Readiness + " cost " +
                    second.Cost.Sum(cost => cost.Units) + " [" + string.Join(",", second.ReadinessReasons) + "]");
            }
        }

        // 6. Neighbours: ordinary Magic Fang still casts and confirms on both
        // pets with its buff and Enchantment[0] on both paths; an ordinary
        // sequence of independent effects still needs every effect; and an
        // effect aimed at a pet (ContextActionsOnPet) still reaches only the
        // caster's own.
        private static void TestGmfNeighbours()
        {
            NativeBlueprintDump dump = GmfDump();
            string[] order = { "ranger-pet", "druid-pet" };
            foreach (bool animated in new[] { false, true })
                foreach (bool classic in new[] { false, true })
                {
                    GmfParty party = GmfDruidParty(dump, GmfOrdinaryAbility, 12, 2);
                    ExplicitStepConversion explicitSteps;
                    CastPlan classicPlan;
                    IReadOnlyList<CastStep> steps = GmfSteps(party, order, classic, out explicitSteps, out classicPlan,
                        "ordinary Magic Fang");
                    GmfWorld world = GmfWorldFor(party, GmfOrdinaryBuff, new[] { GmfTiers[0] }, false, 12);
                    bool confirmed;
                    if (classic)
                    {
                        int? halted;
                        confirmed = GmfRunClassic(world, classicPlan, animated, out halted).Confirmed == 2 && halted == null;
                    }
                    else confirmed = GmfRunExplicit(world, explicitSteps, animated).AllConfirmed;
                    MbbExpect(confirmed && steps.Count == 2 && world.Remaining == 0 &&
                        order.All(pet => world.Effects(pet).SequenceEqual(GmfExpected(GmfOrdinaryBuff, GmfTiers[0]))),
                        "Ordinary Magic Fang did not confirm on both pets (" + (classic ? "classic" : "casting-first") +
                        ", " + (animated ? "animated" : "instant") + ").");
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

        // 7. Observation: the planner's item read on detached native UnitBody
        // instances (no Unity call) reaches every weapon native MagicFang
        // enchants - the hand slots, the polymorph hand set, the empty-hand
        // weapon and the additional (or polymorph) limbs - and each item once.
        private static void TestGmfNaturalWeaponObservation()
        {
            ItemEntity claw = GmfDetached<ItemEntityWeapon>(), otherClaw = GmfDetached<ItemEntityWeapon>(),
                bite = GmfDetached<ItemEntityWeapon>(), unarmed = GmfDetached<ItemEntityWeapon>(),
                armor = GmfDetached<ItemEntityArmor>(), sword = GmfDetached<ItemEntityWeapon>(),
                polymorphClaw = GmfDetached<ItemEntityWeapon>(), polymorphBite = GmfDetached<ItemEntityWeapon>();
            // A bear companion (AnimalCompanionUnitBear: Claw1d4 in both hands,
            // Bite1d6 as an additional limb, WeaponEmptyHand), with barding.
            GmfExpectItems(KingmakerEnchantedItems.Read(GmfBody(claw, otherClaw, armor, unarmed, new[] { bite })),
                new[] { claw, otherClaw, armor, unarmed, bite }, "bear companion");
            // Empty hands (an unarmed humanoid): only the empty-hand weapon,
            // which the hand slots never return.
            GmfExpectItems(KingmakerEnchantedItems.Read(GmfBody(null, null, null, unarmed, new ItemEntity[0])),
                new[] { unarmed }, "empty hands");
            // Polymorphed with a sword in the ordinary hand set: the polymorph
            // hand set and limbs (what native MagicFang enchants) are read.
            UnitBody polymorphed = GmfBody(sword, null, null, unarmed, new[] { bite });
            GmfSet(polymorphed, "m_PolymoprphHandsEquipmentSet", GmfHands(polymorphClaw, null));
            GmfSet(polymorphed, "m_PolymorphAdditionalLimbs", GmfLimbs(new[] { polymorphBite }));
            GmfSet(polymorphed, "<IsPolymorphed>k__BackingField", true);
            GmfExpectItems(KingmakerEnchantedItems.Read(polymorphed), new[] { sword, unarmed, polymorphClaw, polymorphBite },
                "polymorphed");
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

        private static HandsEquipmentSet GmfHands(ItemEntity primary, ItemEntity secondary)
        {
            var hands = GmfDetached<HandsEquipmentSet>();
            var primarySlot = GmfDetached<HandSlot>();
            var secondarySlot = GmfDetached<HandSlot>();
            GmfSet(primarySlot, "m_Item", primary);
            GmfSet(secondarySlot, "m_Item", secondary);
            GmfSet(hands, "PrimaryHand", primarySlot);
            GmfSet(hands, "SecondaryHand", secondarySlot);
            return hands;
        }

        private static UnitBody GmfBody(ItemEntity primary, ItemEntity secondary, ItemEntity armor,
            ItemEntity emptyHand, IEnumerable<ItemEntity> limbs)
        {
            var body = GmfDetached<UnitBody>();
            GmfSet(body, "m_HandsEquipmentSets", new[] { GmfHands(primary, secondary) });
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
