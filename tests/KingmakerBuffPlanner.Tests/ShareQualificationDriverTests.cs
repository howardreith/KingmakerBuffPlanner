using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Compatibility;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.GameAdapters;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.Persistence;
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
            Run("probe-catalogue-distinct-spells-keep-own-expression", TestProbeDistinctSpellExpressions);
            Run("shared-recipes-distinct-blueprints-keep-own-expression",
                () => TestSharedDistinctSpellExpressions(root));
            Run("shared-recipes-catalogue-nonrepresentative-spell-selects-through-driver",
                () => TestSharedCatalogueSourceSelection(root));
            Run("probe-source-id-preserves-legacy-alias-and-raw-fallback",
                TestProbeSourceIdLegacyFallback);
            Run("shared-recipes-select-cast-and-isolate-end-to-end", () => TestSharedRecipesEndToEnd(root));
            Run("shared-recipes-fail-closed-on-leaks-and-mischarges", () => TestSharedRecipesFailClosed(root));
            Run("policy-route-refuses-strict-then-casts-affordable-authored-subset",
                () => TestPolicyRouteEndToEnd(root));
            Run("policy-route-driver-and-judge-fail-closed",
                () => TestPolicyRouteFailClosed(root));
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
            string recipe, string mode,
            Func<IReadOnlyList<CastingQualificationStepForecast>, IEnumerable<string>> approved = null)
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
                    IEnumerable<string> projectionIds = approved == null
                        ? select.Forecast.Select(step => step.ProjectionId)
                        : approved(select.Forecast);
                    o["approvedProjectionIds"] = new JArray(projectionIds
                        .Select(value => (object)value).ToArray());
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

        private static EffectExpression ScannedSpellExpression(AbilityKey ability, string buff)
        {
            return new ActionGraphScanner().Scan(new DiscoveryNode(DiscoveryNodeKind.AbilityReference,
                ability.BaseAbilityGuid, new[] { EffectNode(buff) },
                referencedAbilityId: ability.BaseAbilityGuid, sourceContract: "BlueprintAbility")).Expression;
        }

        private static CastingWorkspaceInputs DistinctSpellInputs(SharedBuffWorld world)
        {
            CastingWorkspaceInputs original = world.Inputs();
            var ability = new AbilityKey("a-representative-spell", null, 0, SourceKind.Spellbook, null);
            var provider = new ProviderSnapshot(new ProviderKey("unit-ally",
                BrownFurPowerfulChangeProfile.CastingSpellbookGuid, ability, "level-4"),
                "A unavailable representative", 4, "pool-unavailable", 1, null, null, 9, 90);
            var options = original.ProviderOptions.Concat(new[]
            {
                new ProviderPlanningOption(provider, SharedBuffWorld.Units, new string[0], 9, 90)
            }).ToList();
            var snapshot = new PartyProviderSnapshot(original.Snapshot.Units,
                original.Snapshot.Providers.Concat(new[] { provider }),
                original.Snapshot.ResourcePools.Concat(new[]
                {
                    new ResourcePoolSnapshot("pool-unavailable", ResourcePoolKind.SpontaneousLevel, 3, 0, null)
                }));
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            {
                { ability.Canonical, ScannedSpellExpression(ability, "shape-buff") },
                { SharedBuffWorld.Shape.Canonical, ScannedSpellExpression(SharedBuffWorld.Shape, "shape-buff") },
                { SharedBuffWorld.Witness.Canonical, ScannedSpellExpression(SharedBuffWorld.Witness, "witness-buff") }
            };
            var catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("fixture-campaign"),
                snapshot, new ActiveEffectSnapshot(null), effects, options, ignored => { });
            SetupSourceRow source = catalogue.Sources.Single(value =>
                value.Abilities.Any(key => key.Equals(SharedBuffWorld.Shape)));
            Expect(source.Ability.Equals(ability) && source.Abilities.Count == 2 &&
                ReferenceEquals(catalogue.EffectsBySource[source.SourceId], effects[ability.Canonical]) &&
                !ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                    catalogue.EffectsBySource[source.SourceId], SharedBuffWorld.Shape) &&
                ExplicitCastingStepConverter.IsPlainCurrentTargetBuff(
                    effects[SharedBuffWorld.Shape.Canonical], SharedBuffWorld.Shape),
                "Distinct scanner wrappers must share a catalogue entry without sharing ability identity.");
            return new CastingWorkspaceInputs(snapshot, options, catalogue.EffectsBySource,
                original.Enhancements, original.TargetingModifiers, original.LiveEffects);
        }

        private static void TestProbeDistinctSpellExpressions()
        {
            var world = new SharedBuffWorld { ShapeSlots = 1, WitnessSlots = 0 };
            CastingWorkspaceInputs inputs = DistinctSpellInputs(world);
            string source = SingleCastProbeSelector.SourceIdFor(inputs.EffectsBySource, SharedBuffWorld.Shape);
            var casting = new PlannedCasting("probe-own-expression", "long", 0, source,
                SharedBuffWorld.Shape, SharedBuffWorld.Caster, BrownFurPowerfulChangeProfile.CastingSpellbookGuid,
                CastingTargetMode.DirectTarget, "unit-ally", null, null, null, null,
                ExistingEffectPolicy.SkipAlreadyActive, null, CastingAuthoringState.Ready, null);
            // Probe the same discovered source with an explicitly reachable ally;
            // the shared recipe below separately exercises Share targeting.
            var options = inputs.ProviderOptions.Select(option => option.Provider.Key.Ability.Equals(SharedBuffWorld.Shape)
                ? new ProviderPlanningOption(option.Provider, SharedBuffWorld.Units, new string[0], 9, 90)
                : option).ToList();
            var document = new CastingPlanDocument("fixture-campaign",
                new[] { new RoutineDefinition("long", "Long") }, new[] { casting });
            ExplicitCastingPlan plan = new ExplicitCastingCompiler().Compile(document, inputs.Snapshot,
                options, inputs.EffectsBySource, inputs.Enhancements, "long");
            CastingApplyDecision decision = new CastingExecutionGate().Evaluate(plan, CastingApplyMode.Ordinary, "long");
            Expect(plan.CastingById(casting.CastingId).IsExecutable && decision.Allowed,
                "The available nonrepresentative spell must compile Ready and pass the execution gate.");
            ExplicitStepConversion projection = ExplicitCastingStepConverter.Convert(plan, decision, options,
                inputs.EffectsBySource, ExplicitProjectionScope.SingleCastProbe);
            Expect(projection.Converted, "The selected spell's probe projection failed: " + projection.Refusal);
            var probeInputs = new CastingWorkspaceInputs(inputs.Snapshot, options, inputs.EffectsBySource,
                inputs.Enhancements);
            SingleCastProbeSelection selected = SingleCastProbeSelector.Select(probeInputs, "fixture-campaign",
                (target, expression) =>
                {
                    Expect(ReferenceEquals(expression, inputs.EffectsBySource[SharedBuffWorld.Shape.Canonical]),
                        "Probe presence validation borrowed the representative's expression.");
                    return false;
                });
            Expect(selected.Selected && selected.Provider.Ability.Equals(SharedBuffWorld.Shape) &&
                selected.SourceId == source && ReferenceEquals(projection.Plan.Steps[0].ExpectedEffects,
                    inputs.EffectsBySource[SharedBuffWorld.Shape.Canonical]),
                "The available spell must retain its catalogue ID and exact projected expression.");
            var legacy = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            { { source, inputs.EffectsBySource[SharedBuffWorld.Shape.Canonical] } };
            Expect(ExplicitCastingStepConverter.Convert(plan, decision, options, legacy,
                ExplicitProjectionScope.SingleCastProbe).Converted, "Legacy source-only expressions lost support.");
            legacy[SharedBuffWorld.Shape.Canonical] = inputs.EffectsBySource[source];
            Expect(!ExplicitCastingStepConverter.Convert(plan, decision, options, legacy,
                ExplicitProjectionScope.SingleCastProbe).Converted,
                "A discovered reference to another spell must not be hidden by a valid legacy alias.");
        }

        private static void TestSharedDistinctSpellExpressions(string root)
        {
            var world = new SharedBuffWorld();
            Func<CastingWorkspaceInputs> discover = () => DistinctSpellInputs(world);
            CastingQualificationSelection selection = CastingQualificationRecipe.SelectSharedPersonal(
                discover(), "fixture-campaign");
            Expect(selection.Selected, "Shared qualification rejected the available spell's own wrapper: " + selection.Refusal);
            long now = 0;
            Func<long> clock = () => now;
            var host = new CastingExecutionHost(settings => new InstantCastExecutor(world, true), clock);
            var record = new CastingQualificationRecord { CastingScenario = false, AllowanceStatus = "not-required" };
            string directory = Path.Combine(root, "shared-distinct-blueprints-select");
            Directory.CreateDirectory(directory);
            var driver = new CastingQualificationDriver(record, null, "fixture-campaign", discover,
                boundary => new CastingWorkspaceSession(directory, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, CastingQualificationRecipe.SharedPersonal,
                null, false, null, null, () => "fixture-lifecycle=1", () => 0L, null, world.ObserveCaster);
            for (int index = 0; index < 2000 && !driver.Completed; index++)
            { now += 16; world.Now = now; driver.Update(); }
            Expect(driver.Completed && record.TerminalReason == "completed" && record.Failures.Count == 0 &&
                record.Violations().Count == 0 && record.SharePersisted == true && world.Fires == 0,
                "Distinct-blueprint catalogue -> shared selector -> driver failed: " + Why(record));
        }

        private static void TestSharedCatalogueSourceSelection(string root)
        {
            var world = new SharedBuffWorld();
            Func<CastingWorkspaceInputs> discover = () =>
            {
                CastingWorkspaceInputs original = world.Inputs();
                var extended = new AbilityKey(SharedBuffWorld.Shape.BaseAbilityGuid, null,
                    ExistingEffectSufficiency.ExtendMetamagicFlag, SourceKind.Spellbook, null);
                var provider = new ProviderSnapshot(new ProviderKey("unit-ally",
                    BrownFurPowerfulChangeProfile.CastingSpellbookGuid, extended, "level-5"),
                    "A representative extended shape", 5, "pool-extended", 1, null, null, 9, 180,
                    string.Empty, string.Empty);
                var options = original.ProviderOptions.Concat(new[]
                {
                    new ProviderPlanningOption(provider, new[] { "unit-ally" }, new string[0], 9, 180)
                }).ToList();
                var snapshot = new PartyProviderSnapshot(original.Snapshot.Units,
                    original.Snapshot.Providers.Concat(new[] { provider }),
                    original.Snapshot.ResourcePools.Concat(new[]
                    {
                        new ResourcePoolSnapshot("pool-extended", ResourcePoolKind.SpontaneousLevel, 3, 3, null)
                    }));
                var effects = original.EffectsBySource.ToDictionary(pair => pair.Key,
                    pair => pair.Value, StringComparer.Ordinal);
                effects[extended.Canonical] = new EffectLeafExpression(EffectKind.Buff, "shape-buff",
                    EffectTarget.CurrentTarget, "fixture", "fixture/extended-shape");
                var catalogue = new PlannerSetupModel(BuffPlannerProfile.CreateDefault("fixture-campaign"),
                    snapshot, new ActiveEffectSnapshot(null), effects, options, ignored => { });
                SetupSourceRow source = catalogue.Sources.Single(value =>
                    value.Abilities.Any(ability => ability.Equals(SharedBuffWorld.Shape)));
                Expect(source.Ability.Equals(extended) && source.Providers.Count == 2 &&
                    !ReferenceEquals(catalogue.EffectsBySource[source.SourceId],
                        effects[SharedBuffWorld.Shape.Canonical]),
                    "The plain spell must be a nonrepresentative catalogue member with its own expression.");
                return new CastingWorkspaceInputs(snapshot, options, catalogue.EffectsBySource,
                    original.Enhancements, original.TargetingModifiers, original.LiveEffects);
            };
            CastingWorkspaceInputs inputs = discover();
            CastingQualificationSelection selected = CastingQualificationRecipe.SelectSharedPersonal(
                inputs, "fixture-campaign");
            Expect(selected.Selected, "The shared-spell selector did not select the eligible plain spell.");
            long now = 0;
            Func<long> clock = () => now;
            var host = new CastingExecutionHost(settings => new InstantCastExecutor(world, true), clock);
            var record = new CastingQualificationRecord { CastingScenario = false, AllowanceStatus = "not-required" };
            string directory = Path.Combine(root, "shared-catalogue-nonrepresentative-select");
            Directory.CreateDirectory(directory);
            var driver = new CastingQualificationDriver(record, null, "fixture-campaign", discover,
                boundary => new CastingWorkspaceSession(directory, "fixture-campaign", boundary),
                host, world.Observe, clock, 240000, CastingQualificationRecipe.SharedPersonal,
                null, false, null, null, () => "fixture-lifecycle=1", () => 0L, null, world.ObserveCaster);
            for (int index = 0; index < 2000 && !driver.Completed; index++)
            { now += 16; world.Now = now; driver.Update(); }
            Expect(driver.Completed && record.TerminalReason == "completed" &&
                record.Failures.Count == 0 && record.Violations().Count == 0,
                "Catalogue -> shared-spell selector -> qualification authoring failed: " + Why(record));
            string canonical = CatalogSourceIdentity.For(SharedBuffWorld.Shape,
                inputs.EffectsBySource[SharedBuffWorld.Shape.Canonical]);
            Expect(selected.Castings.Where(casting => casting.Ability.Equals(SharedBuffWorld.Shape))
                    .All(casting => casting.SourceId == canonical) &&
                record.SharePersisted == true && record.Forecast.Count == 2 &&
                world.Fires == 0 && world.ShapeSlots == 3 && world.ReservoirPoints == 6,
                "Selection must preserve canonical source identity and persisted intent without spending.");
        }

        private static void TestProbeSourceIdLegacyFallback()
        {
            AbilityKey ability = SharedBuffWorld.Shape;
            var expression = new EffectLeafExpression(EffectKind.Buff, "shape-buff",
                EffectTarget.CurrentTarget, "fixture", "fixture/shape");
            var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
            { { ability.Canonical, expression } };
            Expect(SingleCastProbeSelector.SourceIdFor(effects, ability) == ability.Canonical,
                "An ability without a published catalogue entry lost its raw-key fallback.");
            effects["legacy-source"] = expression;
            Expect(SingleCastProbeSelector.SourceIdFor(effects, ability) == "legacy-source",
                "An explicit legacy expression alias was not preserved.");
            effects[CatalogSourceIdentity.For(ability, expression)] = new EffectLeafExpression(
                EffectKind.Buff, "shape-buff", EffectTarget.CurrentTarget, "fixture", "fixture/other");
            Expect(SingleCastProbeSelector.SourceIdFor(effects, ability) == CatalogSourceIdentity.For(ability, expression),
                "A published canonical entry must take priority over legacy aliases.");
            effects.Remove(ability.Canonical);
            Expect(SingleCastProbeSelector.SourceIdFor(effects, ability) == null,
                "A missing discovered expression must remain unresolved.");
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

        // One guarded route proves both halves of the player policy against
        // the exact same authored plan.  The strict attempt spends nothing;
        // after the policy survives a session reopen, ReadyCastsOnly executes
        // the first two affordable shortage castings in authored order and
        // reports the third with its concrete pool shortage.
        private static void TestPolicyRouteEndToEnd(string root)
        {
            var world = new SharedBuffWorld();
            CastingQualificationRecord[] runs = RunShared(root, "policy-shared-powerful", world,
                CastingQualificationRecipe.PolicySharedPowerful, "instant");
            CastingQualificationRecord select = runs[0];
            CastingQualificationRecord cast = runs[1];
            if (!CastingQualificationRecipe.IsKnown(CastingQualificationRecipe.PolicySharedPowerful) ||
                CastingQualificationRecipe.ForecastSteps(CastingQualificationRecipe.PolicySharedPowerful) != 3 ||
                select.TerminalReason != "completed" || select.Violations().Count != 0 ||
                select.Forecast.Count != 3)
                throw new InvalidOperationException("Policy selection did not produce its bounded forecast: " +
                    Why(select));
            if (cast == null || cast.TerminalReason != "completed" || cast.Violations().Count != 0)
                throw new InvalidOperationException("Policy casting route was not clean: " + Why(cast));

            CastingQualificationStepResult strict = cast.Step(CastingQualificationForecast.Shortage);
            CastingQualificationStepResult available = cast.Step(CastingQualificationForecast.Available);
            string[] expected = { "qual-short-1", "qual-short-2" };
            if (strict == null || strict.ApplyAllowed || strict.Report != null ||
                strict.Omissions.Count == 0 || available == null || !available.ApplyAllowed ||
                available.Report == null || available.Report.Mode != CastingApplyMode.ReadyCastsOnly ||
                !cast.PolicyExecutableCastingIds.SequenceEqual(expected, StringComparer.Ordinal) ||
                !available.Report.Entries.Where(value => value.State == CastingOutcomeState.EffectConfirmed)
                    .Select(value => value.CastingId).SequenceEqual(expected, StringComparer.Ordinal) ||
                !cast.PolicyAvailableOmissions.Any(value => value.StartsWith("qual-short-3|",
                    StringComparison.Ordinal)) ||
                cast.PolicyBefore != KingmakerBuffPlanner.Persistence.ExecutionProfile.RequireFullPlan ||
                cast.PolicyAfter != KingmakerBuffPlanner.Persistence.ExecutionProfile.CastAvailable ||
                cast.PolicyReloaded != KingmakerBuffPlanner.Persistence.ExecutionProfile.CastAvailable ||
                cast.PolicyStrictMode != CastingApplyMode.Ordinary.ToString() ||
                cast.PolicyPlannerMode != CastingApplyMode.ReadyCastsOnly.ToString() ||
                cast.PolicyHudMode != CastingApplyMode.ReadyCastsOnly.ToString() ||
                cast.PolicyAuthoredIntentPreserved != true || cast.PolicyPostchecksSpentNothing != true ||
                cast.PolicyGlobalRefusalReason != CastingExecutionPolicy.CombatActive ||
                string.IsNullOrEmpty(cast.PolicyNoOpReason) ||
                !cast.PolicyNoOpReason.StartsWith("nothing-to-cast:", StringComparison.Ordinal) ||
                world.ShapeSlots != 0 || world.WitnessSlots != 2 || world.ReservoirPoints != 0 ||
                world.Fires != 4)
                throw new InvalidOperationException("Policy evidence was incomplete or charged wrongly: " +
                    Why(cast) + " ids=" + string.Join(",", cast.PolicyExecutableCastingIds.ToArray()) +
                    " omissions=" + string.Join(",", cast.PolicyAvailableOmissions.ToArray()) +
                    " resources=" + world.ShapeSlots + "/" + world.WitnessSlots + "/" +
                    world.ReservoirPoints + " fires=" + world.Fires);
        }

        private static void TestPolicyRouteFailClosed(string root)
        {
            var world = new SharedBuffWorld();
            CastingQualificationRecord[] runs = RunShared(root, "policy-judge", world,
                CastingQualificationRecipe.PolicySharedPowerful, "instant");
            CastingQualificationRecord cast = runs[1];
            if (cast == null || cast.Violations().Count != 0)
                throw new InvalidOperationException("Policy baseline was not clean: " + Why(cast));

            IReadOnlyList<string> savedOmissions = cast.PolicyAvailableOmissions;
            cast.PolicyAvailableOmissions = new[] { "qual-short-3|unexplained" };
            if (!cast.Violations().Contains("policy-omission-reasons-missing"))
                throw new InvalidOperationException("The policy judge accepted an omission without a reason.");
            cast.PolicyAvailableOmissions = savedOmissions;

            string savedPolicy = cast.PolicyReloaded;
            cast.PolicyReloaded = KingmakerBuffPlanner.Persistence.ExecutionProfile.RequireFullPlan;
            if (!cast.Violations().Any(value => value.StartsWith("policy-not-persisted:",
                    StringComparison.Ordinal)))
                throw new InvalidOperationException("The policy judge accepted a setting that did not persist.");
            cast.PolicyReloaded = savedPolicy;

            bool? savedPostchecks = cast.PolicyPostchecksSpentNothing;
            cast.PolicyPostchecksSpentNothing = false;
            if (!cast.Violations().Contains("policy-postchecks-spent-resources"))
                throw new InvalidOperationException("The policy judge accepted a spending postcheck.");
            cast.PolicyPostchecksSpentNothing = savedPostchecks;
            if (cast.Violations().Count != 0)
                throw new InvalidOperationException("Restoring the policy evidence did not restore PASS: " +
                    Why(cast));

            var refusedWorld = new SharedBuffWorld();
            CastingQualificationRecord[] refused = RunShared(root, "policy-allowance-missing-projection",
                refusedWorld, CastingQualificationRecipe.PolicySharedPowerful, "instant",
                forecast => forecast.Take(forecast.Count - 1).Select(value => value.ProjectionId));
            CastingQualificationRecord refusedCast = refused[1];
            if (refusedCast == null || !refusedCast.TerminalReason.StartsWith("failed:",
                    StringComparison.Ordinal) ||
                refusedCast.AllowanceStatus != "projections-differ-from-forecast" ||
                !refusedCast.Failures.Any(value => value.EndsWith(
                    "allowance-projections-differ-from-forecast", StringComparison.Ordinal)) ||
                refusedWorld.Fires != 0 || refusedWorld.ShapeSlots != 3 ||
                refusedWorld.WitnessSlots != 3 || refusedWorld.ReservoirPoints != 6)
                throw new InvalidOperationException("A policy allowance missing the affordable projection was not " +
                    "refused before spend: " + Why(refusedCast));
        }
    }
}
