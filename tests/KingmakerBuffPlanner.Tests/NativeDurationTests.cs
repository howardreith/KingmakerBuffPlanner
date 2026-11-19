using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunNativeDurationTests()
        {
            Run("duration-native-deterministic-forms", TestNativeDurationForms);
            Run("duration-localized-plans-forecasts-and-reasons", TestDurationPlanParity);
            Run("duration-extend-source-rod-and-overlap", TestDurationExtend);
        }

        private static ActionList DurationList(params GameAction[] actions)
        {
            var list = NativeObject<ActionList>();
            list.Actions = actions;
            return list;
        }

        private static ContextActionApplyBuff DurationBuff(bool scaled, int value = 1)
        {
            var action = NativeObject<ContextActionApplyBuff>();
            action.Buff = NativeObject<BlueprintBuff>();
            var duration = NativeObject<ContextDurationValue>();
            duration.Rate = DurationRate.Minutes;
            duration.DiceType = DiceType.Zero;
            duration.DiceCountValue = NativeObject<ContextValue>();
            duration.BonusValue = NativeObject<ContextValue>();
            duration.BonusValue.ValueType = scaled ? ContextValueType.Rank : ContextValueType.Simple;
            duration.BonusValue.Value = value;
            SetNative(duration, "m_IsExtendable", true);
            action.DurationValue = duration;
            return action;
        }

        private static BlueprintAbility DurationAbility(ActionList actions, params BlueprintComponent[] extra)
        {
            var spell = NativeObject<BlueprintAbility>();
            var run = NativeObject<AbilityEffectRunAction>();
            run.Actions = actions;
            SetNative(spell, "Components", new BlueprintComponent[] { run }.Concat(extra).ToArray());
            return spell;
        }

        private static void ExpectDuration(BlueprintAbility spell, int level, DurationKind kind,
            int rounds = 0, bool extendable = false)
        {
            DurationEstimate duration = KingmakerDurationAdapter.Estimate(spell, level);
            RepeatAssert(duration.Kind == kind && duration.BaseRounds == rounds && duration.Extendable == extendable,
                "duration at CL " + level + ": got " + duration.Kind + "/" + duration.BaseRounds +
                "/" + duration.Extendable + ", expected " + kind + "/" + rounds + "/" + extendable);
        }

        private static void TestNativeDurationForms()
        {
            var ranked = DurationBuff(true);
            var run = DurationList(ranked);
            var spell = DurationAbility(run);
            ExpectDuration(spell, 4, DurationKind.PerCasterLevel, 40, true);
            ExpectDuration(spell, 12, DurationKind.PerCasterLevel, 120, true);
            ExpectDuration(spell, 0, DurationKind.Unknown);
            var rank = NativeObject<ContextRankConfig>();
            SetNative(rank, "m_Progression", ContextRankProgression.Div2);
            SetNative(rank, "m_UseMax", true);
            SetNative(rank, "m_Max", 4);
            spell = DurationAbility(run, rank);
            ExpectDuration(spell, 6, DurationKind.PerCasterLevel, 30, true);
            ExpectDuration(spell, 12, DurationKind.PerCasterLevel, 40, true);
            SetNative(rank, "m_BaseValueType", ContextRankBaseValueType.FeatureRank);
            ExpectDuration(spell, 12, DurationKind.Unknown);
            ExpectDuration(DurationAbility(run, rank, NativeObject<ContextRankConfig>()), 12, DurationKind.Unknown);

            var fixedBuff = DurationBuff(false, 2);
            var fixedSpell = DurationAbility(DurationList(fixedBuff));
            ExpectDuration(fixedSpell, 1, DurationKind.Fixed, 20, true);
            ExpectDuration(fixedSpell, 12, DurationKind.Fixed, 20, true);
            fixedBuff.UseDurationSeconds = true;
            fixedBuff.DurationSeconds = 36f;
            ExpectDuration(fixedSpell, 12, DurationKind.Fixed, 6, false);
            fixedBuff.Permanent = true;
            ExpectDuration(fixedSpell, 12, DurationKind.Permanent);
            var branches = NativeObject<Conditional>();
            branches.IfTrue = run;
            branches.IfFalse = DurationList(DurationBuff(true));
            spell = DurationAbility(DurationList(branches));
            ExpectDuration(spell, 12, DurationKind.PerCasterLevel, 120, true);
            branches.IfFalse = DurationList(fixedBuff);
            ExpectDuration(spell, 12, DurationKind.Unknown);
            spell.AvailableMetamagic = Kingmaker.UnitLogic.Abilities.Metamagic.Extend;
            ExpectDuration(spell, 12, DurationKind.Unknown, 0, true);
            var unknownProvider = new ProviderSnapshot(new ProviderKey("unit-caster", "book",
                new AbilityKey("spell", string.Empty, 0, SourceKind.Spellbook, string.Empty), "slot"),
                "Spell", 2, "pool", 1, null, effectiveCasterLevel: 12,
                duration: KingmakerDurationAdapter.Estimate(spell, 12));
            var extendRod = new CastEnhancementSnapshot("rod", "unit-caster", "rod-id", "Extend", string.Empty,
                CastEnhancementCategory.MetamagicRod, 8, 6, 3, null);
            RepeatAssert(CastEnhancementRelevance.IsRelevantOffer(extendRod, unknownProvider),
                "Native Extend permission must survive an unprovable duration.");
            spell.AvailableMetamagic = 0;
            branches.IfFalse = DurationList();
            ExpectDuration(spell, 12, DurationKind.Unknown);
            ExpectDuration(DurationAbility(DurationList(fixedBuff, ranked)), 12, DurationKind.Unknown);
            ExpectDuration(DurationAbility(DurationList(NativeObject<ContextActionRandomize>())), 12, DurationKind.Unknown);
            var secondary = NativeObject<AbilityExecuteActionOnCast>();
            secondary.Actions = DurationList(fixedBuff);
            ExpectDuration(DurationAbility(run, secondary), 12, DurationKind.Unknown);
            ranked.DurationValue.DiceType = DiceType.D4;
            ranked.DurationValue.DiceCountValue.Value = 1;
            ExpectDuration(DurationAbility(run), 12, DurationKind.Unknown);
        }

        private static void TestDurationPlanParity()
        {
            var spell = DurationAbility(DurationList(DurationBuff(true)));
            spell.LocalizedDuration = null;
            var estimate = KingmakerDurationAdapter.Estimate(spell, 10);
            RepeatAssert(estimate.Kind == DurationKind.PerCasterLevel && estimate.BaseRounds == 100,
                "native duration must survive a missing display-duration field");
            foreach (double remaining in new[] { 49.99, 50.0 })
            {
                bool skip = remaining == 50.0;
                string projected = null;
                foreach (string display in new[] { "1 minute/level", "每等级1分钟", string.Empty })
                {
                    List<ProviderPlanningOption> baseOptions;
                    PartyProviderSnapshot initial = LiveEffectParty(3, out baseOptions);
                    ProviderSnapshot original = initial.Providers.Single();
                    var provider = new ProviderSnapshot(original.Key, original.DisplayName, original.SpellLevel,
                        original.ResourcePoolKey, original.UnitsPerCast, null, effectiveCasterLevel: 10,
                        durationText: display, duration: estimate);
                    var option = new ProviderPlanningOption(provider, baseOptions[0].ReachableTargetIds,
                        baseOptions[0].LegalAnchorIds, 10, provider.ExpectedDurationRounds);
                    var snapshot = new PartyProviderSnapshot(initial.Units, new[] { provider }, initial.ResourcePools);
                    var live = LiveEffects(On("unit-t1", "buff-effect", remaining, 10, 0));
                    var effects = CastingEffectsWithAbilityAlias("source-bulls", "source-communal", CastingBuffAbility);
                    var inputs = new CastingWorkspaceInputs(snapshot, new[] { option }, effects, null, null, live);
                    var document = CastingDocument(DirectCasting("duration-cast", "long", "unit-cleric", "unit-t1",
                        "source-bulls", CastingBuffAbility));
                    var plan = new ExplicitCastingCompiler().Compile(document, snapshot, inputs.ProviderOptions,
                        effects, null, "long", null, false, live);
                    var resolved = plan.CastingById("duration-cast");
                    RepeatAssert(resolved.Readiness == (skip ? ResolvedCastingReadiness.AlreadySatisfied
                        : ResolvedCastingReadiness.Ready), "wrong readiness with display duration '" + display + "'");
                    string expectedNote = skip ? "already-active:unit-t1"
                        : "existing-insufficient:unit-t1:remaining-duration-short:49<50:buff-effect";
                    RepeatAssert(resolved.ExistingEffectNotes.Contains(expectedNote), string.Join("|", resolved.ExistingEffectNotes));
                    RepeatAssert(plan.BudgetLines.Sum(line => line.AllocatedUsage) == (skip ? 0 : 1),
                        "duration changed resource demand");
                    var forecast = CastingQualificationForecast.Project("duration", document, inputs, live);
                    if (skip)
                        RepeatAssert(forecast.Refusal == "nothing-to-cast" && forecast.CastingIds.Count == 0,
                            "forecast projected a satisfied cast");
                    else
                    {
                        RepeatAssert(forecast.Refusal == null && forecast.CastingIds.SequenceEqual(new[] { "duration-cast" })
                            && !string.IsNullOrEmpty(forecast.CanonicalContract), "forecast refusal: " + forecast.Refusal);
                        if (projected == null) projected = forecast.CanonicalContract;
                        else RepeatAssert(projected == forecast.CanonicalContract, "localized display changed the forecast");
                    }
                }
            }
        }

        private static void TestDurationExtend()
        {
            var estimate = KingmakerDurationAdapter.Estimate(DurationAbility(DurationList(DurationBuff(true))), 10);
            const BindingFlags methods = BindingFlags.NonPublic | BindingFlags.Static;
            MethodInfo requirementFor = typeof(ExplicitCastingCompiler).GetMethod("RequirementFor", methods);
            MethodInfo covered = typeof(ExplicitCastingCompiler).GetMethod("CoveredNotePrefix", methods);
            foreach (int mask in new[] { 0, 8 })
                foreach (bool withRod in new[] { false, true })
                {
                    var ability = new AbilityKey("spell", string.Empty, mask, SourceKind.Spellbook, string.Empty);
                    var provider = new ProviderSnapshot(new ProviderKey("unit-caster", "book", ability, "level-2"),
                        "Spell", 2, "pool", 1, null, effectiveCasterLevel: 10, duration: estimate);
                    var rod = new CastEnhancementSnapshot("rod", "unit-caster", "rod-id", "Extend", string.Empty,
                        CastEnhancementCategory.MetamagicRod, 8, 6, 3, null);
                    if (mask == 8 && withRod)
                        RepeatAssert(rod.ApplicabilityFailure(provider) == "metamagic-already-applied",
                            "normal source-plus-rod overlap should stay inapplicable");
                    // Exercise the compiler's strength aggregation even for an overlapping externally supplied mask.
                    var requirement = (ExistingEffectRequirement)requirementFor.Invoke(null, new object[] {
                        provider, withRod ? new[] { rod } : new CastEnhancementSnapshot[0] });
                    double expected = mask == 0 && !withRod ? 100 : 200;
                    RepeatAssert(requirement.FinalExpectedRounds == expected, "Extend applied an incorrect number of times");
                    var expression = Leaf("buff-effect");
                    Func<double, ActiveEffectInstance[]> active = r => new[] {
                        new ActiveEffectInstance(EffectKind.Buff, "buff-effect", r, 10, 8) };
                    foreach (double remaining in new[] { expected / 2.0 - 0.01, expected / 2.0 })
                    {
                        var assessed = ExistingEffectSufficiency.Assess("unit-t1", expression, active(remaining),
                            new HashSet<string>(StringComparer.Ordinal), requirement);
                        RepeatAssert(assessed.Verdict == (remaining < expected / 2 ? ExistingEffectVerdict.Insufficient
                            : ExistingEffectVerdict.Sufficient), "wrong half-duration boundary");
                    }
                    RepeatAssert((string)covered.Invoke(null, new object[] { active(expected), expression, requirement })
                        == "already-covered:", "equality is not longer");
                    RepeatAssert((string)covered.Invoke(null, new object[] { active(expected + 0.01), expression, requirement })
                        == "already-covered-longer:", "longer active instance not reported");
                }
        }
    }
}
