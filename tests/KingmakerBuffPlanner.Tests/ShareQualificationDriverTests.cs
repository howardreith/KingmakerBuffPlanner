using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Compatibility;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    // The shared recipes driven END TO END through the production driver,
    // session, compiler, converter, boundary and both executors (Claude
    // takeover review of 3c1c5d4, B1-B6): no driver test had ever run a
    // shared recipe, and every live chain would have failed - the selection
    // judged Share reads a selection run never takes, shared-powerful was
    // unknown and mis-forecast, and the witness's graph-assigned id differed
    // from the approved projection. The fixture mirrors the live Advanced
    // campaign: a Brown-Fur arcanist whose personal transmutation and witness
    // both spend spontaneous slots, Share and Powerful Change: Strength on
    // one Arcane Reservoir.
    internal static partial class Program
    {
        private static void RunShareQualificationDriverTests(string root)
        {
            Run("shared-recipes-select-cast-and-isolate-end-to-end", () => TestSharedRecipesEndToEnd(root));
            Run("shared-recipes-fail-closed-on-leaks-and-mischarges", () => TestSharedRecipesFailClosed(root));
        }

        private sealed class SharedBuffWorld : IInstantCastRuntimeAdapter, ICastRuntimeAdapter,
            ICastEnhancementRuntimeAdapter
        {
            internal const string Caster = "unit-arcanist";
            internal static readonly AbilityKey Shape =
                new AbilityKey("shape-spell", null, 0, SourceKind.Spellbook, null);
            internal static readonly AbilityKey Witness =
                new AbilityKey("witness-spell", null, 0, SourceKind.Spellbook, null);
            internal const string ShareToggle = "share-toggle";
            internal const string StrengthToggle = "16c06d016437be9e9e6dac6211ff30a5";
            internal static readonly string ShareId = "share-transmutation|" + Caster + "|" + ShareToggle;
            internal static readonly string PowerfulId =
                BrownFurPowerfulChangeProfile.EnhancementId(Caster, StrengthToggle);
            internal static readonly string Reservoir = BrownFurPowerfulChangeProfile.UsagePoolId(Caster);
            internal static readonly string[] Units = { Caster, "unit-ally", "unit-third" };
            // "<unit>|<effect>" -> (instance key, end ticks fixed at landing).
            internal readonly Dictionary<string, Tuple<string, long>> Active =
                new Dictionary<string, Tuple<string, long>>(StringComparer.Ordinal);
            internal readonly SortedDictionary<string, bool> Toggles =
                new SortedDictionary<string, bool>(StringComparer.Ordinal)
                { { ShareToggle, false }, { StrengthToggle, false }, { "unrelated-toggle", true } };
            internal int ShapeSlots = 3;
            internal int WitnessSlots = 3;
            internal int ReservoirPoints = 6;
            internal int Fires;
            // Failure shapes the judges must catch.
            internal bool LeaveShareOn;
            internal bool WitnessSpendsReservoir;
            internal bool DoubleChargePowerful;
            internal long Now;
            private readonly Dictionary<CastStep, EffectBaseline> _baselines =
                new Dictionary<CastStep, EffectBaseline>();
            private int _instances;
            private long _sequence;

            public bool IsInCombat { get { return false; } }
            public CastRuntimeValidation Validate(CastStep step) { return CastRuntimeValidation.Pass(); }
            public CastEnhancementPreparation PrepareEnhancements(CastStep step)
            {
                return CastEnhancementPreparation.Pass(new CallbackDisposable(() => { }));
            }
            public InstantCastResult Fire(CastStep step)
            {
                Land(step);
                return new InstantCastResult(true, true, EffectsObserved(step), true,
                    "simulated-shared;provider-direct:" +
                    (step.ExecutionStrategy == CastExecutionStrategy.ProviderDirectRuleCast) +
                    ";strategy:" + step.ExecutionStrategy + ";");
            }
            public IAnimatedCastOperation StartAnimated(CastStep step)
            {
                return new LandingAnimatedOperation(3, () => Land(step), () => EffectsObserved(step));
            }
            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("simulated-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("simulated-clean"); }

            private static string EffectOf(CastStep step)
            {
                return step.Provider.Ability.Canonical == Shape.Canonical ? "shape-buff" : "witness-buff";
            }

            private List<ObservedEffectInstance> Instances(string unit, string effect)
            {
                var instances = new List<ObservedEffectInstance>();
                Tuple<string, long> instance;
                if (Active.TryGetValue(unit + "|" + effect, out instance))
                    instances.Add(new ObservedEffectInstance(EffectKind.Buff, effect, instance.Item1,
                        instance.Item2, false));
                return instances;
            }

            private void Land(CastStep step)
            {
                Fires++;
                string effect = EffectOf(step);
                _baselines[step] = new EffectBaseline(step.ExpectedRecipientUnitIds.ToDictionary(unit => unit,
                    unit => (IEnumerable<ObservedEffectInstance>)Instances(unit, effect), StringComparer.Ordinal));
                Active[step.TargetUnitIds[0] + "|" + effect] = Tuple.Create("i" + (++_instances),
                    Now * TimeSpan.TicksPerMillisecond + TimeSpan.FromHours(1).Ticks);
                if (effect == "shape-buff")
                {
                    ShapeSlots--;
                    if (step.EnhancementIds.Contains(ShareId)) ReservoirPoints--;
                    if (step.EnhancementIds.Contains(PowerfulId))
                        ReservoirPoints -= DoubleChargePowerful ? 2 : 1;
                    if (LeaveShareOn) Toggles[ShareToggle] = true;
                }
                else
                {
                    WitnessSlots--;
                    if (WitnessSpendsReservoir) ReservoirPoints--;
                }
            }

            public bool EffectsObserved(CastStep step)
            {
                EffectBaseline baseline;
                string effect = EffectOf(step);
                return _baselines.TryGetValue(step, out baseline) &&
                    AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds, step.ExpectedEffects,
                        baseline, unit => Instances(unit, effect));
            }

            internal ProbeObservation Observe(CastStep step, string label)
            {
                string unit = step.TargetUnitIds[0];
                string effect = EffectOf(step);
                var instances = new List<ProbeEffectInstance>();
                Tuple<string, long> instance;
                if (Active.TryGetValue(unit + "|" + effect, out instance))
                    instances.Add(new ProbeEffectInstance(effect, instance.Item1, instance.Item2, new string[0]));
                return ProbeObservation.Read(label, ++_sequence, DateTime.UtcNow, unit,
                    effect == "shape-buff" ? ShapeSlots : WitnessSlots, instances, null,
                    Now * TimeSpan.TicksPerMillisecond);
            }

            internal CasterEnhancementObservation ObserveCaster(string caster, string pool)
            {
                if (caster != Caster || pool != Reservoir)
                    return CasterEnhancementObservation.Failed("wrong-caster-or-pool:" + caster + "|" + pool);
                return CasterEnhancementObservation.Read(ReservoirPoints, Toggles);
            }

            internal CastingWorkspaceInputs Inputs()
            {
                List<UnitSnapshot> units = Units.Select(id => new UnitSnapshot(id, id, false, string.Empty,
                    new TargetValidationSnapshot(true, true, true, true))).ToList();
                string book = BrownFurPowerfulChangeProfile.CastingSpellbookGuid;
                var shape = new ProviderSnapshot(new ProviderKey(Caster, book, Shape, "level-4"),
                    "Shape Spell", 4, "pool-shape", 1, null, null, 9, 90, string.Empty, string.Empty);
                var witness = new ProviderSnapshot(new ProviderKey(Caster, book, Witness, "level-2"),
                    "Witness Spell", 2, "pool-witness", 1, null, null, 9, 90, string.Empty, string.Empty);
                var pools = new[]
                {
                    new ResourcePoolSnapshot("pool-shape", ResourcePoolKind.SpontaneousLevel, 4, ShapeSlots, null),
                    new ResourcePoolSnapshot("pool-witness", ResourcePoolKind.SpontaneousLevel, 4, WitnessSlots, null)
                };
                var options = new List<ProviderPlanningOption>
                {
                    // Personal: the shape reaches only its caster (Share expands it).
                    new ProviderPlanningOption(shape, new[] { Caster }, new string[0], 9, 90,
                        CastExecutionStrategy.DirectRuleCast, "fixture-shape",
                        new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)),
                    new ProviderPlanningOption(witness, Units, new string[0], 9, 90,
                        CastExecutionStrategy.DirectRuleCast, "fixture-witness",
                        new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal))
                };
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { Shape.Canonical, new EffectLeafExpression(EffectKind.Buff, "shape-buff",
                        EffectTarget.CurrentTarget, "fixture", "fixture/shape") },
                    { Witness.Canonical, new EffectLeafExpression(EffectKind.Buff, "witness-buff",
                        EffectTarget.CurrentTarget, "fixture", "fixture/witness") }
                };
                var enhancements = new List<CastEnhancementSnapshot>
                {
                    new CastEnhancementSnapshot(ShareId, Caster, ShareToggle, "Share Transmutation", string.Empty,
                        CastEnhancementCategory.ClassFeature, 0, 0, ReservoirPoints, new[] { "shape-spell" },
                        "Share Transmutation", new[] { book }, Reservoir, false,
                        "brown-fur-share-transmutation", 1, true, "brown-fur-share-transmutation",
                        "Arcane Reservoir", "brown-fur-direct-cast-v1"),
                    new CastEnhancementSnapshot(PowerfulId, Caster, StrengthToggle, "Powerful Change: Strength",
                        string.Empty, CastEnhancementCategory.ClassFeature, 0, 0, ReservoirPoints,
                        new[] { "shape-spell" }, "Powerful Change: Strength", new[] { book }, Reservoir, false,
                        "brown-fur-powerful-change", 1, false, "brown-fur-powerful-change", "Arcane Reservoir",
                        "brown-fur-direct-cast-v1")
                };
                var modifiers = new ICastingTargetingModifier[]
                {
                    new ShareCastingModifier(new[]
                    {
                        new ShareCastingModifier.ShareCapability(Caster, Reservoir, 1, ReservoirPoints,
                            new[] { "shape-spell" }, new[] { book }, new[] { "unit-ally", "unit-third" }, ShareId)
                    })
                };
                var live = Active.Keys.Select(key => On(key.Split('|')[0], key.Split('|')[1], 80, 9, 0)).ToArray();
                return new CastingWorkspaceInputs(new PartyProviderSnapshot(units, new[] { shape, witness }, pools),
                    options, effects, enhancements, modifiers, LiveEffects(live));
            }
        }

        private static CastingQualificationDriver SharedDriver(string dir, SharedBuffWorld world,
            CastingQualificationRecord record, CastingQualificationAllowance allowance, string recipe,
            Func<long> clock, CastingExecutionHost host)
        {
            Directory.CreateDirectory(dir);
            return new CastingQualificationDriver(record, allowance, "fixture-campaign", world.Inputs,
                boundary => new CastingWorkspaceSession(dir, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, recipe, null, false, null, null,
                () => "fixture-lifecycle=1", () => 0L, null, world.ObserveCaster);
        }

        // One selection run, then (from ITS published forecast, as the
        // allowance writer does) one casting run in a FRESH plan directory,
        // as every live run gets a freshly staged mod folder.
        private static CastingQualificationRecord[] RunShared(string root, string name, SharedBuffWorld world,
            string recipe, string mode)
        {
            long now = 0;
            Func<long> clock = () => now;
            var host = new CastingExecutionHost(settings => settings != null && settings.Mode == "animated"
                ? (ICastExecutor)new AnimatedCastExecutor(world, true)
                : new InstantCastExecutor(world, true), clock);
            var select = new CastingQualificationRecord { CastingScenario = false, AllowanceStatus = "not-required" };
            CastingQualificationDriver selectDriver = SharedDriver(Path.Combine(root, name + "-select"), world,
                select, null, recipe, clock, host);
            for (int i = 0; i < 2000 && !selectDriver.Completed; i++) { now += 16; world.Now = now; selectDriver.Update(); }
            if (select.Forecast == null || select.Forecast.Any(step => step.ProjectionId == null))
                return new[] { select, null };
            string refusal;
            CastingQualificationAllowance allowance = CastingQualificationAllowance.Parse(
                QualificationAllowanceJson(o =>
                {
                    o["fixtureGameId"] = "fixture-campaign";
                    o["executionMode"] = mode;
                    o["recipe"] = recipe;
                    o["approvedProjectionIds"] = new JArray(select.Forecast
                        .Select(step => (object)step.ProjectionId).ToArray());
                    o["maximumNativeSubmissions"] = select.Forecast.Sum(step => step.CastingIds.Count);
                }), "qual-run-1", out refusal);
            if (allowance == null) throw new InvalidOperationException("Shared allowance refused: " + refusal);
            var cast = new CastingQualificationRecord { CastingScenario = true, AllowanceStatus = "parsed" };
            CastingQualificationDriver castDriver = SharedDriver(Path.Combine(root, name + "-cast"), world,
                cast, allowance, recipe, clock, host);
            for (int i = 0; i < 6000 && !castDriver.Completed; i++) { now += 16; world.Now = now; castDriver.Update(); }
            return new[] { select, cast };
        }

        private static string Why(CastingQualificationRecord record)
        {
            return record == null ? "no record" : "violations=" + string.Join(" | ", record.Violations().ToArray()) +
                " failures=" + string.Join(" | ", record.Failures.ToArray()) + " terminal=" + record.TerminalReason;
        }

        private static void TestSharedRecipesEndToEnd(string root)
        {
            foreach (string recipe in new[] { CastingQualificationRecipe.SharedPersonal,
                CastingQualificationRecipe.SharedPowerful })
            {
                if (!CastingQualificationRecipe.IsKnown(recipe) ||
                    CastingQualificationRecipe.ForecastSteps(recipe) != 2)
                    throw new InvalidOperationException(recipe + " is unknown or mis-forecast (B4/B5).");
                foreach (string mode in new[] { "instant", "animated" })
                {
                    var world = new SharedBuffWorld();
                    CastingQualificationRecord[] runs = RunShared(root, "share-" + recipe + "-" + mode, world,
                        recipe, mode);
                    CastingQualificationRecord select = runs[0];
                    CastingQualificationRecord cast = runs[1];
                    // B1: the selection (preview) run passes its own judge.
                    if (select.TerminalReason != "completed" || select.Violations().Count != 0 ||
                        select.SharePersisted != true || select.Forecast.Count != 2)
                        throw new InvalidOperationException(recipe + "/" + mode + " selection: " + Why(select));
                    if (!select.Selection.Coverage.Contains("shared-source:SpontaneousLevel") ||
                        !select.Selection.Coverage.Contains("witness-source:SpontaneousLevel"))
                        throw new InvalidOperationException("The finite-slot fixture did not select finite sources.");
                    // B6 and the whole casting contract: every step and rule.
                    if (cast == null || cast.TerminalReason != "completed" || cast.Violations().Count != 0)
                        throw new InvalidOperationException(recipe + "/" + mode + " casting: " + Why(cast));
                    int demand = recipe == CastingQualificationRecipe.SharedPowerful ? 2 : 1;
                    if (cast.ShareExpectedSpend != demand || cast.ShareIndependentDemand != demand ||
                        world.ReservoirPoints != 6 - demand || world.ShapeSlots != 2 || world.WitnessSlots != 2 ||
                        world.Toggles[SharedBuffWorld.ShareToggle] || world.Toggles[SharedBuffWorld.StrengthToggle] ||
                        cast.ShareDraftDisarmKeptCasting != true || cast.SharePersisted != true ||
                        cast.ShareShortageCastings != 3 || world.Fires != 2)
                        throw new InvalidOperationException(recipe + "/" + mode + " observed the wrong costs: " +
                            "reservoir=" + world.ReservoirPoints + " shape=" + world.ShapeSlots + " witness=" +
                            world.WitnessSlots + " fires=" + world.Fires + " shortage=" + cast.ShareShortageCastings);
                    CastingQualificationStepResult shortage = cast.Steps.FirstOrDefault(step =>
                        step.Name == CastingQualificationForecast.Shortage);
                    if (shortage == null || shortage.ApplyAllowed || shortage.Report != null ||
                        !shortage.ApplyReason.StartsWith("apply-refused:blocked-casting:qual-short-",
                            StringComparison.Ordinal))
                        throw new InvalidOperationException("The shortage was not refused whole: " +
                            (shortage == null ? "missing" : shortage.ApplyReason));
                }
            }
        }

        // Each failure shape makes the casting run fail - never a PASS with
        // a leaked toggle, a charge not made exactly once, a witness spend,
        // or an armed baseline the run could not have proven clean.
        private static void TestSharedRecipesFailClosed(string root)
        {
            var cases = new Dictionary<string, Tuple<string, Action<SharedBuffWorld>, string>>
            {
                { "leak", Tuple.Create(CastingQualificationRecipe.SharedPersonal,
                    (Action<SharedBuffWorld>)(world => world.LeaveShareOn = true), "cleanup:activatables-changed") },
                { "double", Tuple.Create(CastingQualificationRecipe.SharedPowerful,
                    (Action<SharedBuffWorld>)(world => world.DoubleChargePowerful = true), "enhancement-resource:") },
                { "witness", Tuple.Create(CastingQualificationRecipe.SharedPersonal,
                    (Action<SharedBuffWorld>)(world => world.WitnessSpendsReservoir = true), "enhancement-resource:") },
                { "armed", Tuple.Create(CastingQualificationRecipe.SharedPersonal,
                    (Action<SharedBuffWorld>)(world => world.Toggles[SharedBuffWorld.StrengthToggle] = true),
                    "share-baseline-armed:") }
            };
            foreach (KeyValuePair<string, Tuple<string, Action<SharedBuffWorld>, string>> item in cases)
            {
                var world = new SharedBuffWorld();
                item.Value.Item2(world);
                CastingQualificationRecord[] runs = RunShared(root, "share-fail-" + item.Key, world,
                    item.Value.Item1, "instant");
                CastingQualificationRecord cast = runs[1];
                string all = Why(cast);
                if (cast == null || cast.Violations().Count == 0 || all.IndexOf(item.Value.Item3,
                        StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Share failure shape '" + item.Key + "' was not caught: " + all);
            }
        }
    }
}
