using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.Execution;
using KingmakerBuffPlanner.Discovery;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.RuntimeTesting;
using KingmakerBuffPlanner.UI;
using Newtonsoft.Json.Linq;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // Simulated native observations, not a reproduction of native Heroism.
        // The session/compiler/gate/converter/executors/coordinator/report are real.
        private sealed class RepeatBuffWorld : IInstantCastRuntimeAdapter, ICastRuntimeAdapter, ICastEffectEvidence
        {
            internal readonly FiniteBuffWorld State = new FiniteBuffWorld();
            internal string Observation = "replace";
            internal long Round;
            internal int EvidenceReads;
            internal bool ThrowEvidence;
            internal string ValidationFailureId;
            internal bool CapturedHeroism;
            internal bool InfectiousCharms;
            internal bool NearbyRecipients;
            internal bool NearbySelectionUnresolved;
            internal string NearbyOwner;
            // 0.6.11 review H1/H2 realism. MarkerSeconds is the discovered
            // native lifetime metadata of the two Infectious Charms markers
            // (installed Call of the Wild: UseDurationSeconds, 3 s, reported
            // as 6 s worst-case); null reproduces an unknown lifetime.
            // MarkerLifetime is the fixture's round-granular end: a 3 s
            // marker is gone by the next round.
            internal float? MarkerSeconds;
            internal long MarkerLifetime = 100;
            // Native selection: the owners the wrapper actually chooses when
            // the action runs, and the native distances at that moment.
            internal bool NativeSelection;
            internal string[] PredictedOwners;
            internal string[] ApplicationOwners;
            internal readonly Dictionary<string, float> ApplicationDistances = new Dictionary<string, float>(StringComparer.Ordinal);
            internal bool NoGeometry;
            internal string UnreadableUnit;
            // An observation during application (the adapters' progress read),
            // then time passes before the executor's final read.
            internal bool RecordAtApplication = true;
            internal bool DelayFinalRead;
            private readonly Dictionary<string, List<ObservedEffectInstance>> _additional =
                new Dictionary<string, List<ObservedEffectInstance>>(StringComparer.Ordinal);
            private string BuffId { get { return CapturedHeroism ? "87ab2fed7feaaff47b62a3320a57ad8d" : "buff-effect"; } }
            private int _instance;
            internal AbilityKey Sentinel = new AbilityKey("sentinel-spell", null, 0,
                SourceKind.Spellbook, null);
            private readonly Dictionary<CastStep, EffectBaseline> _baselines =
                new Dictionary<CastStep, EffectBaseline>();

            public bool IsInCombat { get { return false; } }

            private IEnumerable<ObservedEffectInstance> Read(string target)
            { return Read(target, false); }

            private IEnumerable<ObservedEffectInstance> Read(string target, bool before)
            {
                KeyValuePair<string, long> active;
                if (!before && (Observation == "unreadable" || target == UnreadableUnit ||
                    (Observation == "caster-unreadable" && target == "unit-wizard"))) return null;
                var result = State.Active.TryGetValue(target, out active) && active.Value > Round
                    ? new[] { new ObservedEffectInstance(
                        !before && Observation == "wrong-kind" ? EffectKind.AreaBuff : EffectKind.Buff,
                        target == "unit-t4" ? "sentinel-effect" : BuffId,
                        active.Key, active.Value, !before && Observation == "suppressed") }
                    : new ObservedEffectInstance[0];
                List<ObservedEffectInstance> additional;
                return _additional.TryGetValue(target, out additional)
                    ? result.Concat(additional.Where(value => value.EndTimeTicks > Round)).ToArray() : result;
            }

            public CastRuntimeValidation Validate(CastStep step)
            {
                if (step.AssignmentId == ValidationFailureId)
                    return CastRuntimeValidation.Fail("simulated-late-resource-shortage");
                bool available = step.Reservation.TokenIds.Count != 0
                    ? step.Reservation.TokenIds.All(id => State.Tokens[id])
                    : State.SpontaneousRemaining >= step.Reservation.Units;
                return available ? CastRuntimeValidation.Pass()
                    : CastRuntimeValidation.Fail("simulated-exhausted-source");
            }

            public InstantCastResult Fire(CastStep step)
            {
                _baselines[step] = new EffectBaseline(step.EffectObservationUnitIds.ToDictionary(
                    id => id, id => Read(id, true), StringComparer.Ordinal));
                int before = Available(step);
                State.Fired.Add(step.AssignmentId);
                foreach (string token in step.Reservation.TokenIds) State.Tokens[token] = false;
                if (step.Reservation.TokenIds.Count == 0)
                    State.SpontaneousRemaining -= step.Reservation.Units;
                string target = step.TargetUnitIds[0];
                KeyValuePair<string, long> prior;
                State.Active.TryGetValue(target, out prior);
                if (Observation == "replace" || Observation == "suppressed" || Observation == "wrong-kind" ||
                    Observation.StartsWith("caster-", StringComparison.Ordinal) || Observation.StartsWith("marker-", StringComparison.Ordinal))
                    State.Active[target] = new KeyValuePair<string, long>("instance-" + (++_instance), Round + 100);
                else if (Observation == "refresh")
                    State.Active[target] = new KeyValuePair<string, long>(prior.Key, Round + 100);
                else if (Observation == "wrong-recipient")
                    State.Active["unit-sorcerer"] = new KeyValuePair<string, long>("instance-" + (++_instance), Round + 100);
                else if (Observation == "missing") State.Active.Remove(target);
                if (CapturedHeroism && InfectiousCharms && target != "unit-t4")
                {
                    if (Observation != "caster-missing")
                        PutAdditional(Observation == "caster-wrong-owner" ? target : step.Provider.CasterUnitId,
                            "6d949ebb548bd6c31e8d5bd45f96a7b6");
                    if (Observation == "marker-wrong-owner")
                        PutAdditional(NearbyRecipients ? target : step.Provider.CasterUnitId, "c20c575b6352d1692b3074a25232e699");
                    else if (Observation != "marker-missing")
                        foreach (string owner in NearbyRecipients && ApplicationOwners != null ? ApplicationOwners
                            : new[] { NearbyRecipients ? "unit-t2" : target })
                            PutAdditional(owner, "c20c575b6352d1692b3074a25232e699");
                }
                if (RecordAtApplication)
                    AppliedEffectJudgement.RecordProgress(_baselines[step], step.EffectObservationUnitIds, Read, Distances(step));
                if (DelayFinalRead) Round += 1;
                bool succeeded = Observation != "native-failure";
                return new InstantCastResult(true, succeeded, false, Available(step) < before,
                    true, "simulated-native:" + Observation,
                    AvailableCountJudgement.Violation(step.Reservation, before, Available(step)));
            }

            private void PutAdditional(string owner, string effect)
            {
                List<ObservedEffectInstance> values;
                if (!_additional.TryGetValue(owner, out values)) _additional[owner] = values = new List<ObservedEffectInstance>();
                values.RemoveAll(value => value.EffectId == effect);
                values.Add(new ObservedEffectInstance(EffectKind.Buff, effect, "aux-" + (++_instance), Round + MarkerLifetime, false));
            }

            internal void ReplaceAdditional(string owner, string effect) { PutAdditional(owner, effect); }

            // Native distance from each selection centre at this read.
            private IDictionary<string, float> Distances(CastStep step)
            {
                if (NoGeometry) return null;
                var distances = new Dictionary<string, float>(StringComparer.Ordinal);
                foreach (string center in step.ExpectedRecipientUnitIds)
                    foreach (string unit in step.EffectObservationUnitIds)
                    {
                        float distance;
                        distances[EffectBaseline.DistanceKey(center, unit)] = unit == center ? 0
                            : ApplicationDistances.TryGetValue(unit, out distance) ? distance : 3f;
                    }
                return distances;
            }

            private int Available(CastStep step)
            {
                // Native available casts count primaries, not linked token units.
                return step.Reservation.TokenIds.Count == 0 ? State.SpontaneousRemaining
                    : State.Primaries.Count(id => State.Tokens[id]);
            }

            public bool EffectsObserved(CastStep step)
            {
                EffectBaseline baseline;
                if (_baselines.TryGetValue(step, out baseline))
                    AppliedEffectJudgement.RecordProgress(baseline, step.EffectObservationUnitIds, Read, Distances(step));
                return _baselines.TryGetValue(step, out baseline) &&
                    AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds,
                        step.ExpectedEffects, baseline, Read, step.PreCoveredRecipientUnitIds, step.Provider.CasterUnitId);
            }

            public InstantCastCompletion InspectCompletion(CastStep step)
            { return InstantCastCompletion.Settled("simulated-settled"); }
            public InstantCastCompletion Cleanup(CastStep step)
            { return InstantCastCompletion.Settled("simulated-clean"); }

            public string DescribeEffectEvidence(CastStep step)
            {
                EvidenceReads++;
                if (ThrowEvidence) throw new InvalidOperationException("simulated diagnostic failure");
                EffectBaseline before;
                _baselines.TryGetValue(step, out before);
                var after = new EffectBaseline(step.ExpectedRecipientUnitIds.ToDictionary(
                    id => id, id => Read(id), StringComparer.Ordinal));
                return "before:{" + before.Describe(step.ExpectedRecipientUnitIds) +
                    "};after:{" + after.Describe(step.ExpectedRecipientUnitIds) + "}";
            }

            public IAnimatedCastOperation StartAnimated(CastStep step)
            { return new RepeatOperation(this, step, Fire(step)); }

            private sealed class RepeatOperation : IAnimatedCastOperation, ICastEffectEvidence
            {
                private readonly RepeatBuffWorld _world;
                private readonly CastStep _step;
                private readonly InstantCastResult _result;
                internal RepeatOperation(RepeatBuffWorld world, CastStep step, InstantCastResult result)
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
                public string DescribeEffectEvidence(CastStep step)
                { return _world.DescribeEffectEvidence(step); }
                public void Dispose() { }
            }

            internal CastingWorkspaceInputs Inputs()
            {
                CastingWorkspaceInputs current = State.Inputs();
                var live = LiveEffects(State.Active.Where(pair => pair.Value.Value > Round)
                    .Select(pair => On(pair.Key, pair.Key == "unit-t4" ? "sentinel-effect" : BuffId,
                        pair.Value.Value - Round, 1, 0)).Concat(_additional.SelectMany(pair => pair.Value
                        .Where(value => value.EndTimeTicks > Round).Select(value => On(pair.Key, value.EffectId,
                            value.EndTimeTicks - Round, 1, 0)))).ToArray());
                ProviderSnapshot prepared = current.Snapshot.Providers.Single(value =>
                    value.Key.CasterUnitId == "unit-wizard");
                var wizard = new ProviderSnapshot(prepared.Key, CapturedHeroism ? "Heroism (captured graph)" : "Heroism-like fixture", 2,
                    prepared.ResourcePoolKey, 1, prepared.EligibleTokenIds, null, 1, 100, "", "10 minutes/level");
                var sentinel = new ProviderSnapshot(new ProviderKey("unit-sorcerer", "book-sorcerer",
                    Sentinel, "level-2"), "Sentinel fixture", 2, "pool-unit-sorcerer", 1,
                    null, null, 1, 100, "", "10 minutes/level");
                var providers = new[] { wizard, sentinel };
                EffectExpression heroism = CapturedHeroism ? CapturedHeroismExpression(NearbyRecipients, MarkerSeconds) : Leaf("buff-effect");
                var facts = CapturedHeroism ? new Dictionary<string, bool> { { "infectious-charms-feature", InfectiousCharms } } : null;
                var bound = current.Snapshot.Units.ToDictionary(unit => unit.UnitId, unit => NativeSelection
                    // The production builder's shape: prediction plus every
                    // observable unit the rule's own filter admits.
                    ? EffectRecipientBinding.Bind(EffectConditionBinding.Bind(heroism, facts), rule => new NearbyRecipientSelection(
                        NearbySelectionUnresolved ? null : PredictedOwners ?? new[] { unit.UnitId == "unit-t2" ? "unit-t3" : "unit-t2" },
                        current.Snapshot.Units.Select(other => other.UnitId).Where(id => id != unit.UnitId)))
                    : EffectRecipientBinding.Bind(EffectConditionBinding.Bind(heroism, facts), rule =>
                        NearbySelectionUnresolved ? null : new[] { NearbyOwner ?? (unit.UnitId == "unit-t2" ? "unit-t3" : "unit-t2") }), StringComparer.Ordinal);
                var options = providers.Select(provider => new ProviderPlanningOption(provider,
                    provider == sentinel ? new[] { "unit-t4" } : current.Snapshot.Units.Select(unit => unit.UnitId), new[] { provider.Key.CasterUnitId },
                    10, 100, CastExecutionStrategy.DirectRuleCast, "simulated-direct",
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                    facts, provider == wizard && NearbyRecipients ? bound : null));
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { "source-bulls", heroism },
                    { "source-sentinel", Leaf("sentinel-effect") }
                };
                // Catalogue aliases share the selected ability's expression instance.
                effects[State.Ability.Canonical] = effects["source-bulls"];
                effects[Sentinel.Canonical] = effects["source-sentinel"];
                return new CastingWorkspaceInputs(new PartyProviderSnapshot(current.Snapshot.Units,
                    providers, current.Snapshot.ResourcePools), options, effects, current.Enhancements, null, live);
            }

            internal void Replenish()
            {
                foreach (string id in State.Tokens.Keys.ToArray()) State.Tokens[id] = true;
                State.SpontaneousRemaining = 1;
            }

            internal ProbeObservation Observe(CastStep step, string label)
            {
                string target = step.TargetUnitIds[0];
                var instances = Read(target).Where(value => !value.Suppressed)
                    .Select(value => new ProbeEffectInstance(value.EffectId, value.InstanceKey, value.EndTimeTicks));
                return ProbeObservation.Read(label, ++EvidenceReads, DateTime.UtcNow, target, Available(step),
                    instances, step.Reservation.TokenIds.Count == 0 ? null : step.Reservation.TokenIds
                        .ToDictionary(id => id, id => State.Tokens[id], StringComparer.Ordinal));
            }
        }

        private static void TestHeroismNativeRecipe(string root, string mode)
        {
            var world = new RepeatBuffWorld { CapturedHeroism = true, InfectiousCharms = true, NearbyRecipients = true };
            world.State.Ability = new AbilityKey("5ab0d42fb68c9e34abae4921822b9d63", null, 0, SourceKind.Spellbook, null);
            world.Sentinel = new AbilityKey("3e4ab69ada402d145a5e0ad3ad4b8564", null, 0, SourceKind.Spellbook, null);
            world.State.SpontaneousRemaining = 3;
            var inputs = world.Inputs();
            var selection = CastingQualificationRecipe.Select(CastingQualificationRecipe.HeroismSuccessor, inputs, "fixture-campaign");
            if (!selection.Selected) throw new InvalidOperationException(string.Join("|", selection.Rejections));
            Assert(selection.Castings[0].CasterUnitId != selection.Castings[0].DirectTargetUnitId);
            Assert(selection.Castings[0].Ability.BaseAbilityGuid != selection.Castings[1].Ability.BaseAbilityGuid);
            Assert((bool)selection.EffectContext["casterFacts"]["infectious-charms-feature"]);
            var forecast = CastingQualificationForecast.Forecast(selection, inputs, "fixture-campaign");
            Assert(forecast.Select(value => value.CastingIds.Count).SequenceEqual(new[] { 2, 2, 1 }));
            Assert(forecast.SelectMany(value => value.Projection.Plan.Steps).Sum(value => value.Reservation.Units) == 5);
            CastStep original = forecast[0].Projection.Plan.Steps[0];
            Func<bool, CastStep> ownerSwap = swap => new CastStep(original.SourceId, original.AssignmentId,
                original.Provider, original.AnchorUnitId, original.TargetUnitIds, original.ExpectedRecipientUnitIds,
                original.Reservation, original.MaterialReservation, new SequenceEffectExpression(new[]
                {
                    new EffectLeafExpression(EffectKind.Buff, "first-marker", EffectTarget.CurrentTarget, "fixture", "first", swap ? "unit-t3" : "unit-t2"),
                    new EffectLeafExpression(EffectKind.Buff, "second-marker", EffectTarget.CurrentTarget, "fixture", "second", swap ? "unit-t2" : "unit-t3")
                }), false, original.ExecutionStrategy, original.ExecutionStrategyReason, exactEnhancements: true);
            CastStep firstOwners = ownerSwap(false), swappedOwners = ownerSwap(true);
            Assert(firstOwners.EffectObservationUnitIds.SequenceEqual(swappedOwners.EffectObservationUnitIds));
            Assert(ExplicitCastingStepConverter.Identity(new[] { firstOwners }, ExplicitProjectionScope.Standard) !=
                ExplicitCastingStepConverter.Identity(new[] { swappedOwners }, ExplicitProjectionScope.Standard));
            string refusal;
            var allowance = CastingQualificationAllowance.Parse(QualificationAllowanceJson(o =>
            {
                o["fixtureGameId"] = "fixture-campaign";
                o["recipe"] = CastingQualificationRecipe.HeroismSuccessor;
                o["executionMode"] = mode;
                o["approvedProjectionIds"] = new JArray(forecast.Select(value => value.ProjectionId));
                o["maximumNativeSubmissions"] = 5;
            }), "qual-run-1", out refusal);
            if (allowance == null) throw new InvalidOperationException(refusal);
            long now = 0;
            var host = new CastingExecutionHost(settings => mode == "instant" ?
                (ICastExecutor)new InstantCastExecutor(world, true) : new AnimatedCastExecutor(world, true), () => now);
            var record = new CastingQualificationRecord { CastingScenario = true };
            var driver = new CastingQualificationDriver(record, allowance, "fixture-campaign", world.Inputs,
                boundary => new CastingWorkspaceSession(Path.Combine(root, "heroism-recipe-" + mode), "fixture-campaign", boundary),
                host, world.Observe, () => now, 240000, CastingQualificationRecipe.HeroismSuccessor);
            for (int count = 0; !driver.Completed && count < 2000; count++) { now += 10; driver.Update(); }
            Assert(driver.Completed);
            if (record.Violations().Count != 0) throw new InvalidOperationException(string.Join("|", record.Violations()));
            Assert(world.State.Fired.SequenceEqual(new[] { "qual-cast-1", "qual-cast-2", "qual-cast-1", "qual-cast-2", "qual-cast-2" }));
            Assert(world.State.Primaries.Count(id => world.State.Tokens[id]) == 1 && world.State.SpontaneousRemaining == 0);
            world.State.SpontaneousRemaining = 2;
            Assert(!CastingQualificationRecipe.Select(CastingQualificationRecipe.HeroismSuccessor, world.Inputs(), "fixture-campaign").Selected);
            world.State.Ability = new AbilityKey("lookalike-heroism", null, 0, SourceKind.Spellbook, null);
            world.Replenish(); world.State.SpontaneousRemaining = 3;
            Assert(!CastingQualificationRecipe.Select(CastingQualificationRecipe.HeroismSuccessor, world.Inputs(), "fixture-campaign").Selected);
        }

        private sealed class RepeatSession
        {
            internal readonly RepeatBuffWorld World = new RepeatBuffWorld();
            internal readonly CastingExecutionHost Host;
            internal readonly CastingWorkspaceSession Session;
            internal readonly List<CastStep> Steps = new List<CastStep>();

            internal RepeatSession(string root, string mode, string name, bool party = false, bool capturedHeroism = false,
                bool infectiousCharms = false, bool nearbyRecipients = false, Action<RepeatBuffWorld> configure = null)
            {
                if (configure != null) configure(World);
                World.CapturedHeroism = capturedHeroism;
                World.InfectiousCharms = infectiousCharms;
                World.NearbyRecipients = nearbyRecipients;
                if (capturedHeroism)
                {
                    World.State.Ability = new AbilityKey("5ab0d42fb68c9e34abae4921822b9d63", null, 0, SourceKind.Spellbook, null);
                    World.State.SpontaneousRemaining = 3;
                }
                Host = new CastingExecutionHost(settings => mode == "instant"
                    ? (ICastExecutor)new InstantCastExecutor(World, true)
                    : new AnimatedCastExecutor(World, true), () => World.Round * 6000);
                var boundary = new NativeCastingDispatchBoundary(Host, () => Session.ExecutionSettings);
                Session = new CastingWorkspaceSession(Path.Combine(root, "heroism-repeat-" + mode + "-" + name),
                    "repeat-campaign", boundary);
                Host.RunCompleted = report => Session.RecordRunReport(report);
                Session.SetExecutionMode(mode);
                CastingWorkspaceInputs inputs = World.Inputs();
                Assert(AddDraftCasting(Session, inputs, "unit-wizard", "unit-t1").Applied);
                if (party)
                {
                    Assert(AddDraftCasting(Session, inputs, "unit-wizard", "unit-t2").Applied);
                    Assert(AddDraftCasting(Session, inputs, "unit-wizard", "unit-t3").Applied);
                }
                // Ordinary supported direct sentinel, from a different native pool.
                Session.SelectBuff("source-sentinel");
                Session.BuildView(inputs);
                Session.Draft.SourceId = "source-sentinel";
                Session.Draft.TargetMode = CastingTargetMode.DirectTarget;
                Session.ChooseDraftCaster("unit-sorcerer");
                Session.Draft.DirectTargetUnitId = "unit-t4";
                Session.Draft.State = CastingAuthoringState.Ready;
                Assert(Session.AddCastingFromDraft(inputs).Applied);
            }

            internal WorkspaceApplyResult Apply(bool recast = false, bool useSavedPolicy = false)
            {
                foreach (PlannedCasting casting in Session.Document.Castings.ToArray())
                {
                    Session.FocusCasting(casting.CastingId);
                    Session.SetFocusedRecastPolicy(recast ? ExistingEffectPolicy.Overwrite
                        : ExistingEffectPolicy.SkipAlreadyActive);
                }
                // Rebuild from current buffs, clock, remaining slots and replenishment EACH run.
                WorkspaceApplyResult result = CastingRunPolicy.Apply(Session,
                    useSavedPolicy ? (CastingApplyMode?)null : CastingApplyMode.Ordinary, "long", World.Inputs());
                if (result.Projection != null) Steps.AddRange(result.Projection.Plan.Steps);
                int frames = 0;
                while (Host.IsRunning && frames++ < 100) Host.Pump();
                RepeatAssert(!Host.IsRunning, "fixture run did not settle");
                return result;
            }
        }

        private static void RepeatAssert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        // Faithful node/target/condition structure from the full-user native
        // catalogue at 85e7410 (kbp041-rel-catalog-full), not inferred from the
        // handoff's flat GUID list. The feature identity is symbolic here;
        // the live adapter supplies the actual condition Fact.AssetGuid.
        private static EffectExpression CapturedHeroismExpression(bool nearby = false, float? markerSeconds = null)
        {
            Func<DiscoveryNode[], DiscoveryNode> sequence = children => new DiscoveryNode(DiscoveryNodeKind.Sequence, "ActionList", children);
            Func<string, EffectTarget, DiscoveryNode> buff = (id, target) => new DiscoveryNode(DiscoveryNodeKind.Effect,
                "ContextActionApplyBuff", effectId: id, target: target, sourceContract: "ContextActionApplyBuff",
                // Heroism's own buff is level-dependent: no fixed lifetime.
                maximumDurationSeconds: id == "87ab2fed7feaaff47b62a3320a57ad8d" ? null : markerSeconds);
            return new ActionGraphScanner().Scan(new DiscoveryNode(DiscoveryNodeKind.AbilityReference,
                "5ab0d42fb68c9e34abae4921822b9d63", new[] { sequence(new[] {
                    buff("87ab2fed7feaaff47b62a3320a57ad8d", EffectTarget.CurrentTarget),
                    new DiscoveryNode(DiscoveryNodeKind.Conditional,
                        "Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional",
                        whenTrue: sequence(new[] {
                            buff("6d949ebb548bd6c31e8d5bd45f96a7b6", EffectTarget.Caster),
                            new DiscoveryNode(DiscoveryNodeKind.Sequence, "nearby-wrapper",
                                new[] { sequence(new[] { buff("c20c575b6352d1692b3074a25232e699", EffectTarget.CurrentTarget) }) },
                                nearbyRecipients: nearby ? new NearbyRecipientRule(9.144f, 1, true, true, false) : null) }),
                        whenFalse: sequence(new DiscoveryNode[0]),
                        conditionContract: "And:Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact",
                        casterFactGate: new CasterFactGate(false, new[] { new KeyValuePair<string, bool>("infectious-charms-feature", false) })) }) },
                referencedAbilityId: "5ab0d42fb68c9e34abae4921822b9d63")).Expression;
        }

        private static void TestCapturedHeroism(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "captured-expression", capturedHeroism: true);
            WorkspaceApplyResult accepted = run.Apply();
            RepeatAssert(accepted.Allowed, "initial captured admission: " + accepted.ReviewReason);
            RepeatAssert(run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.NotProcessed == 0,
                "Captured conditional Heroism stopped its distinct sentinel: " + run.Host.LastReport.Entries[0].Detail);
            string first = run.World.State.Active["unit-t1"].Key;
            run.World.Round = 10;
            // Another genuinely remaining prepared slot and a still-affordable
            // distinct sentinel; no pool is refilled between these attempts.
            accepted = run.Apply(true);
            RepeatAssert(accepted.Allowed, "repeat captured admission: " + accepted.ReviewReason);
            RepeatAssert(run.Host.LastReport.Confirmed == 2 && run.World.State.Active["unit-t1"].Key != first, "repeat=" + run.Host.LastReport.Entries[0].Detail);
            Assert(run.World.State.Tokens.Values.Count(v => v) == 1 && run.World.State.Fired.Count == 4);
            run.World.State.Active.Remove("unit-t4");
            accepted = run.Apply();
            RepeatAssert(accepted.Allowed, "skip captured admission: " + accepted.ReviewReason);
            RepeatAssert(run.Host.LastReport.Skipped == 1 && run.Host.LastReport.Confirmed == 1 && run.Host.LastReport.ResourcesSpent == 1,
                "skip: skipped=" + run.Host.LastReport.Skipped + ";confirmed=" + run.Host.LastReport.Confirmed + ";spent=" + run.Host.LastReport.ResourcesSpent);
            foreach (string observation in new[] { "retain", "missing", "suppressed", "unreadable", "wrong-kind", "wrong-recipient" })
            {
                var negative = new RepeatSession(root, mode, "cg-" + observation.Substring(observation.Length - 3), capturedHeroism: true);
                if (observation == "retain") negative.World.State.Active["unit-t1"] = new KeyValuePair<string, long>("old", 1);
                negative.World.Observation = observation;
                accepted = negative.Apply(true);
                RepeatAssert(accepted.Allowed, observation + " admission: " + accepted.ReviewReason);
                RepeatAssert(negative.Host.LastReport.Confirmed == 0 && negative.Host.LastReport.NotProcessed == 1 &&
                    negative.Host.LastReport.ResourcesSpent == 1 && negative.World.State.Fired.Count == 1,
                    observation + ": confirmed=" + negative.Host.LastReport.Confirmed + ";notProcessed=" + negative.Host.LastReport.NotProcessed + ";spent=" + negative.Host.LastReport.ResourcesSpent + ";" + negative.Host.LastReport.Entries[0].Detail);
            }
        }

        private static void TestHeroismFixtureScope(string root)
        {
            Assert(HeroismFixturePreparationPolicy.SaveMustFinishBeforeTermination(true, false, false));
            Assert(HeroismFixturePreparationPolicy.SaveMustFinishBeforeTermination(true, true, true));
            Assert(!HeroismFixturePreparationPolicy.SaveMustFinishBeforeTermination(true, true, false));
            Assert(!HeroismFixturePreparationPolicy.SaveMustFinishBeforeTermination(false, false, true));
            string saveRoot = Path.GetFullPath(Path.Combine(root, "fixture-policy-saves"));
            string file = "Manual_305_KBP_AUTOMATION_WORKING.zks";
            string[] good = { "live-heroism-fixture-prepare", "full-user", "present", "KBP_AUTOMATION_WORKING", file,
                "campaign", "campaign", "campaign", Path.Combine(saveRoot, file), saveRoot };
            Func<string[], string> check = a => HeroismFixturePreparationPolicy.Refusal(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8], a[9]);
            Assert(check(good) == null);
            foreach (var change in new[] {
                Tuple.Create(0, "live-cast-qual"), Tuple.Create(1, "advanced-gunslinger-0136"), Tuple.Create(2, "unknown"),
                Tuple.Create(3, "KBP_AUTOMATION_BASELINE"), Tuple.Create(3, "Real campaign"),
                Tuple.Create(4, "Manual_304_KBP_AUTOMATION_BASELINE.zks"), Tuple.Create(4, "../" + file),
                Tuple.Create(5, "another-campaign"), Tuple.Create(6, "another-campaign"), Tuple.Create(7, ""),
                Tuple.Create(8, Path.Combine(saveRoot, "nested", file)),
                Tuple.Create(8, Path.Combine(saveRoot + "-outside", file)),
                Tuple.Create(8, Path.Combine(saveRoot, "Manual_304_KBP_AUTOMATION_BASELINE.zks")) })
            {
                var bad = (string[])good.Clone(); bad[change.Item1] = change.Item2;
                Assert(check(bad) != null);
            }
            const string scenario = "live-heroism-fixture-prepare";
            Assert(RuntimeTestProtocol.IsWorkspaceScenario(scenario) && RuntimeTestProtocol.IsNoInputWorkspaceScenario(scenario) &&
                !RuntimeTestProtocol.IsCastingQualificationScenario(scenario) && !RuntimeTestProtocol.IsAdvancedFamilyScenario(scenario));
            JObject template = JObject.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tests", "KingmakerBuffPlanner.Tests",
                "Fixtures", "kbp042-removal-01-runtime-request.json")));
            int serial = 0;
            Func<string, Action<JObject>, string> admit = (requestScenario, mutate) =>
            {
                string id = "heroism-preparation-admission-" + (++serial);
                string directory = Path.Combine(_protocolEvidenceRoot, id);
                Directory.CreateDirectory(directory);
                var request = (JObject)template.DeepClone();
                request["runId"] = id; request["scenario"] = requestScenario; request["evidenceDirectory"] = directory;
                request["expectedCommit"] = BuildInfo.Commit; request["expectedModVersion"] = BuildInfo.Version;
                ((JObject)request["parameters"]).Remove("physicalExpectation");
                request["parameters"]["heroismCondition"] = "absent";
                if (mutate != null) mutate(request);
                string path = Path.Combine(directory, "runtime-request.json"); File.WriteAllText(path, request.ToString());
                string rejection;
                return ReadProtocol(new[] { "Kingmaker.exe", RuntimeTestProtocol.ActivationFlag, path }, out rejection) == null ? rejection : "";
            };
            Assert(admit(scenario, null) == "" && admit(scenario, r => r["parameters"]["heroismCondition"] = "present") == "");
            foreach (Action<JObject> mutate in new Action<JObject>[] {
                r => ((JObject)r["parameters"]).Remove("heroismCondition"),
                r => r["parameters"]["heroismCondition"] = true,
                r => r["parameters"]["heroismCondition"] = "unknown",
                r => r["parameters"]["workingSaveName"] = "KBP_AUTOMATION_BASELINE",
                r => r["parameters"]["workingSha256"] = "bad",
                r => r["parameters"]["unexpected"] = "extra",
                r => r["profileId"] = "native-only" }) Assert(admit(scenario, mutate) != "");
            Assert(admit("live-cast-qual-select", null) != "");
        }

        private static void TestUnobservableHeroismAdmission(string root, string mode)
        {
            foreach (bool unresolved in new[] { true, false })
                foreach (bool strict in new[] { false, true })
                {
                    var run = new RepeatSession(root, mode, "owners-" + unresolved + "-" + strict,
                        capturedHeroism: true, infectiousCharms: true, nearbyRecipients: true);
                    run.World.NearbySelectionUnresolved = unresolved;
                    run.World.NearbyOwner = unresolved ? null : "nearby-npc-outside-party";
                    run.Session.SetAvailabilityPolicy(strict ? KingmakerBuffPlanner.Persistence.ExecutionProfile.RequireFullPlan
                        : KingmakerBuffPlanner.Persistence.ExecutionProfile.CastAvailable);
                    var logs = new List<string>();
                    run.Session.RunLog = (lines, warning) => logs.AddRange(lines);
                    string intent = run.Session.DocumentIntentSignature();
                    string heroism = run.Session.Document.Castings[0].CastingId;
                    string successor = run.Session.Document.Castings[1].CastingId;
                    var beforeTokens = run.World.State.Tokens.ToDictionary(pair => pair.Key, pair => pair.Value);
                    WorkspaceApplyResult result = run.Apply(useSavedPolicy: true);
                    string reason = unresolved ? "effect-owner-unresolved" : "effect-owner-not-readable:nearby-npc-outside-party";
                    RepeatAssert(result.GateDecision.Omissions.Any(item => item.CastingId == heroism && item.Reasons.Contains(reason)),
                        "Known unobservable owner was not omitted at preflight: " + reason);
                    if (strict)
                        RepeatAssert(!result.Allowed && run.World.State.Fired.Count == 0 && run.Host.LastReport == null,
                            "Require full plan started before refusing an unobservable owner.");
                    else
                        RepeatAssert(result.Allowed && run.World.State.Fired.SequenceEqual(new[] { successor }) &&
                            run.Host.LastReport.Confirmed == 1 && run.Host.LastReport.Omitted == 1 && run.Host.LastReport.NotProcessed == 0 &&
                            run.World.State.SpontaneousRemaining == 2,
                            "Cast available did not omit unobservable Heroism and execute the distinct affordable successor.");
                    RepeatAssert(beforeTokens.All(pair => run.World.State.Tokens[pair.Key] == pair.Value),
                        "Blocked Heroism consumed a prepared slot.");
                    RepeatAssert(logs.Any(line => line.Contains("effect owner") || line.Contains(reason)) &&
                        run.Session.DocumentIntentSignature() == intent, "Omission explanation or saved intent was lost.");
                }
        }

        private static void TestMixedOwnerHeroism(string root, string mode, bool nearby = false)
        {
            var run = new RepeatSession(root, mode, nearby ? "near" : "mix", capturedHeroism: true, infectiousCharms: true, nearbyRecipients: nearby);
            RepeatAssert(run.Apply().Allowed, "Mixed-owner admission failed.");
            RepeatAssert(run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.NotProcessed == 0,
                "Correct caster-side and recipient-side effects stopped the distinct successor: " + run.Host.LastReport.Entries[0].Detail);
            Assert(run.Steps[0].Provider.CasterUnitId != run.Steps[0].TargetUnitIds[0]);
            if (nearby) Assert(run.Steps[0].EffectObservationUnitIds.Contains("unit-t2"));
            run.World.Round = 10;
            Assert(run.Apply(true).Allowed && run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.ResourcesSpent == 2);
            Assert(run.World.State.Tokens.Values.Count(value => value) == 1);
            run.World.State.Active.Remove("unit-t4");
            Assert(run.Apply().Allowed && run.Host.LastReport.Skipped == 1 && run.Host.LastReport.Confirmed == 1);
            int index = 0;
            foreach (string observation in new[] { "caster-missing", "caster-wrong-owner", "marker-missing", "marker-wrong-owner", "caster-unreadable" })
            {
                var negative = new RepeatSession(root, mode, (nearby ? "nr" : "mx") + (++index), capturedHeroism: true, infectiousCharms: true, nearbyRecipients: nearby);
                negative.World.Observation = observation;
                Assert(negative.Apply(true).Allowed);
                RepeatAssert(negative.Host.LastReport.Confirmed == 0 && negative.Host.LastReport.NotProcessed == 1 &&
                    negative.Host.LastReport.ResourcesSpent == 1 && negative.World.State.Fired.Count == 1, observation + " falsely confirmed.");
                if (observation != "caster-unreadable")
                {
                    negative.World.Observation = "replace";
                    Assert(negative.Apply().Allowed);
                    RepeatAssert(negative.Host.LastReport.Confirmed == 2 && negative.Host.LastReport.Skipped == 0,
                        observation + " incorrectly satisfied already-active assessment.");
                }
            }
        }

        private static void TestNativeNearbyRecipientContract()
        {
            string game = Environment.GetEnvironmentVariable("KBP_TEST_GAME_PATH");
            var optional = System.Reflection.Assembly.LoadFrom(Path.Combine(game, "Mods", "CallOfTheWild", "CallOfTheWild.dll"));
            object wrapper = DetachedViewObject(optional.GetType("CallOfTheWild.AoeMechanics.ApplyActionToTargetsInRange", true));
            SetNative(wrapper, "Radius", 9.144f); SetNative(wrapper, "max_num_units", 1);
            SetNative(wrapper, "CanTargetSelf", true); SetNative(wrapper, "CanTargetFriends", true); SetNative(wrapper, "CanTargetEnemies", false);
            var extract = ViewInputProduct().GetType("KingmakerBuffPlanner.GameAdapters.KingmakerActionGraphAdapter", true)
                .GetMethod("ReadNearbyRecipientRule", ViewInputMembers | System.Reflection.BindingFlags.Static);
            object rule = extract.Invoke(null, new[] { wrapper });
            Assert(rule != null && (int)rule.GetType().GetProperty("Maximum").GetValue(rule, null) == 1);
            Assert((bool)rule.GetType().GetProperty("Friends").GetValue(rule, null) && !(bool)rule.GetType().GetProperty("Enemies").GetValue(rule, null));
            SetNative(wrapper, "Radius", float.NaN);
            Assert(extract.Invoke(null, new[] { wrapper }) == null);
            Assert(extract.Invoke(null, new[] { new object() }) == null);
            Assert(EffectAggregateIdentity.For(CapturedHeroismExpression(), "") ==
                EffectAggregateIdentity.For(CapturedHeroismExpression(true), ""));
            var applicable = EffectConditionBinding.Bind(CapturedHeroismExpression(true),
                new Dictionary<string, bool> { { "infectious-charms-feature", true } });
            Assert(new EffectRecipientScope(EffectRecipientBinding.Bind(applicable, value => null), "caster", "recipient")
                .UnitIds.Contains("unresolved-secondary-recipient"));
            var noSecondary = EffectRecipientBinding.Bind(applicable, value => new string[0]);
            Assert(new EffectRecipientScope(noSecondary, "caster", "recipient").UnitIds.Count == 2);
        }

        private static void TestCapturedHeroismCondition()
        {
            EffectExpression raw = CapturedHeroismExpression();
            var observed = new[] { new ObservedEffectInstance(EffectKind.Buff, "87ab2fed7feaaff47b62a3320a57ad8d", "new", 100, false) };
            Assert(!AppliedEffectJudgement.Reached(raw, new ObservedEffectInstance[0], observed));
            var facts = new Dictionary<string, bool> { { "infectious-charms-feature", false } };
            EffectExpression bound = EffectConditionBinding.Bind(raw, facts);
            Assert(AppliedEffectJudgement.Reached(bound, new ObservedEffectInstance[0], observed));
            Assert(!AppliedEffectJudgement.Reached(bound, new ObservedEffectInstance[0], new ObservedEffectInstance[0]));
            facts["infectious-charms-feature"] = true;
            Assert(!AppliedEffectJudgement.Reached(EffectConditionBinding.Bind(raw, facts), new ObservedEffectInstance[0], observed));
            Assert(!AppliedEffectJudgement.Reached(EffectConditionBinding.Bind(raw, new Dictionary<string, bool>()), new ObservedEffectInstance[0], observed));
            Assert(!AppliedEffectJudgement.Reached(new SequenceEffectExpression(new[] { bound, Leaf("mandatory-enhancement") }),
                new ObservedEffectInstance[0], observed));
            Assert(!AppliedEffectJudgement.Reached(bound, observed, observed.Concat(new[] {
                new ObservedEffectInstance(EffectKind.Buff, "unrelated", "fresh", 200, false) })));
            var optionalOnly = new ConditionalEffectExpression("known false", Leaf("other"), EmptyEffectExpression.NoAction(),
                new CasterFactGate(false, new[] { new KeyValuePair<string, bool>("absent", false) }));
            Assert(!AppliedEffectJudgement.Reached(EffectConditionBinding.Bind(optionalOnly,
                new Dictionary<string, bool> { { "absent", false } }), new ObservedEffectInstance[0], observed));
            // Metadata must not rewrite the persisted catalogue identity.
            var gated = new ConditionalEffectExpression("same-contract", Leaf("a"), Leaf("b"),
                new CasterFactGate(true, new[] { new KeyValuePair<string, bool>("one", false),
                    new KeyValuePair<string, bool>("two", true) }));
            var withoutMetadata = new ConditionalEffectExpression("same-contract", gated.WhenTrue, gated.WhenFalse);
            Assert(EffectAggregateIdentity.For(gated, "") == EffectAggregateIdentity.For(withoutMetadata, ""));
            var state = new Dictionary<string, bool> { { "one", false }, { "two", true } };
            Assert(ReferenceEquals(EffectConditionBinding.Bind(gated, state), gated.WhenFalse));
            state["two"] = false; // Not is applied before Or.
            Assert(ReferenceEquals(EffectConditionBinding.Bind(gated, state), gated.WhenTrue));
        }

        private static void TestNativeCasterFactGate()
        {
            // Exact installed native types and the compiled production adapter;
            // only Unity object construction/liveness is detached. Never invoke
            // Condition.Check or enter a live MechanicsContext.
            var product = ViewInputProduct();
            var native = typeof(Kingmaker.Blueprints.BlueprintScriptableObject).Assembly;
            Type conditionalType = native.GetType("Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional", true);
            object conditional = DetachedViewObject(conditionalType);
            var checkerField = conditionalType.GetField("ConditionsChecker", ViewInputMembers);
            object checker = DetachedViewObject(checkerField.FieldType);
            checkerField.SetValue(conditional, checker);
            var conditionsField = checker.GetType().GetField("Conditions", ViewInputMembers);
            Type conditionType = native.GetType("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionCasterHasFact", true);
            object condition = DetachedViewObject(conditionType);
            object fact = DetachedViewObject(native.GetType("Kingmaker.Blueprints.Classes.BlueprintFeature", true));
            SetNative(fact, "m_AssetGuid", "12345678901234567890123456789012");
            SetNative(condition, "Fact", fact);
            SetNative(condition, "Not", true);
            Array conditions = Array.CreateInstance(conditionsField.FieldType.GetElementType(), 1);
            conditions.SetValue(condition, 0); conditionsField.SetValue(checker, conditions);
            var operation = checker.GetType().GetField("Operation", ViewInputMembers);
            operation.SetValue(checker, Enum.Parse(operation.FieldType, "Or"));
            var extract = product.GetType("KingmakerBuffPlanner.GameAdapters.KingmakerActionGraphAdapter", true)
                .GetMethod("CasterGate", ViewInputMembers | System.Reflection.BindingFlags.Static);
            Func<object> read = () => extract.Invoke(null, new[] { conditional });
            object gate = read();
            Assert(gate != null && (bool)gate.GetType().GetProperty("Any").GetValue(gate, null));
            var facts = (IEnumerable<KeyValuePair<string, bool>>)gate.GetType().GetProperty("Facts").GetValue(gate, null);
            Assert(facts.Single().Key == "12345678901234567890123456789012" && facts.Single().Value);
            conditions.SetValue(DetachedViewObject(native.GetType("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionIsAlly", true)), 0);
            Assert(read() == null);
            conditions.SetValue(condition, 0); SetNative(fact, "m_CachedPtr", IntPtr.Zero);
            Assert(read() == null); // A destroyed native fact is unknown.
        }

        private static void TestHeroismRepeatStateful(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "stateful", true);
            WorkspaceApplyResult first = run.Apply();
            RepeatAssert(first.Allowed, "first: " + first.ReviewReason);
            Assert(run.Host.LastReport.Confirmed == 4 && run.Host.LastReport.ResourcesSpent == 4);
            Assert(run.Steps.Take(3).SelectMany(step => step.Reservation.TokenIds).Distinct().Count() == 3);
            Assert(run.World.State.Tokens.Values.All(value => !value) && run.World.State.SpontaneousRemaining == 0);
            run.World.Round = 10;
            WorkspaceApplyResult covered = run.Apply();
            Assert(!covered.Allowed && covered.ReviewReason.StartsWith("nothing-to-cast:", StringComparison.Ordinal));
            Assert(run.Host.StartedRuns == 1 && run.World.State.Fired.Count == 4);
            // Covered prepared castings must not block a remaining runnable sentinel.
            run.World.State.Active.Remove("unit-t4");
            run.World.State.SpontaneousRemaining = 1;
            WorkspaceApplyResult mixed = run.Apply();
            RepeatAssert(mixed.Allowed, "mixed: " + mixed.ReviewReason);
            Assert(run.Host.LastReport.Skipped == 3 && run.Host.LastReport.Confirmed == 1 &&
                run.Host.LastReport.ResourcesSpent == 1 && run.World.State.Fired.Count == 5);
            // Expiry plus replenishment uses the SAME routine and world.
            run.World.Round = 111; // The sentinel was refreshed at round 10 and expires at 110.
            run.World.Replenish();
            WorkspaceApplyResult expired = run.Apply();
            RepeatAssert(expired.Allowed, "expired: " + expired.ReviewReason);
            Assert(run.Host.LastReport.Confirmed == 4 && run.Host.LastReport.ResourcesSpent == 4);
            Assert(run.World.State.Fired.Count == 9 && run.Host.StartedRuns == 3);
            Assert(run.World.EvidenceReads == 9);
            Console.WriteLine("[HEROISM-REPEAT-FIXTURE] mode=" + mode +
                ";fresh=4-confirmed/4-spent;covered-exhausted=no-active-run/0-spent;" +
                "mixed=3-skipped/1-confirmed/1-spent;expired-replenished=4-confirmed/4-spent");
        }

        private static void TestHeroismRepeatObservation(string root, string mode, string observation)
        {
            var run = new RepeatSession(root, mode, observation);
            Assert(run.Apply().Allowed && run.Host.LastReport.Confirmed == 2);
            run.World.Round = 10;
            run.World.Replenish();
            run.World.Observation = observation;
            Assert(run.Apply(true).Allowed);
            CastingRunReport report = run.Host.LastReport;
            bool confirmed = observation == "replace" || observation == "refresh";
            Assert(report.Confirmed == (confirmed ? 2 : 0));
            Assert(report.ResourcesSpent == (confirmed ? 2 : 1));
            Assert(report.Failed == (confirmed ? 0 : 1) && report.NotProcessed == (confirmed ? 0 : 1));
            Assert(run.World.State.Fired.Count == (confirmed ? 4 : 3));
            Assert(report.Entries[0].State == (confirmed ? CastingOutcomeState.EffectConfirmed : CastingOutcomeState.Failed));
            Assert(run.World.EvidenceReads == (confirmed ? 4 : 3));
            if (confirmed && mode == "animated")
                RepeatAssert(report.Entries.All(entry =>
                    entry.Detail.Contains("expected-effects-observed") &&
                    entry.Detail.Contains("effect-evidence:before:") &&
                    entry.Detail.Contains("ResourceSpent:native-command-spend-completed")),
                    "Animated reports must retain confirmation diagnostics AND actual command spending evidence.");
            // Presence, kind and readability are observations, not inferred from Fire's success.
            string before = "before:{unit-t1=[Buff:buff-effect#instance-1@end=100";
            RepeatAssert(report.Entries[0].Detail.Contains(before), "lost terminal evidence: " + report.Entries[0].Detail);
            if (observation == "retain")
            {
                // Old coverage passes the 50% skip threshold but does not fulfill a 100-round refresh.
                // It must not be called absent, freshly applied, free, or sufficient to continue.
                if (!report.Entries[0].Detail.Contains("expected-application-unconfirmed"))
                    throw new InvalidOperationException("Unchanged old coverage was described as absent: " +
                        report.Entries[0].Detail);
                Assert(!report.Entries[0].FreeCast);
            }
            Console.WriteLine("[HEROISM-REPEAT-FIXTURE] mode=" + mode + ";observation=" + observation +
                ";confirmed=" + report.Confirmed + ";spent=" + report.ResourcesSpent + ";failed=" +
                report.Failed + ";unprocessed=" + report.NotProcessed + ";first=" + report.Entries[0].Detail);
        }

        private static void TestHeroismRepeatManual(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "manual");
            run.World.State.Active["unit-t1"] = new KeyValuePair<string, long>("manual-instance", 100);
            foreach (string token in run.World.State.Tokens.Keys.ToArray()) run.World.State.Tokens[token] = false;
            Assert(run.Apply().Allowed);
            Assert(run.Host.LastReport.Skipped == 1 && run.Host.LastReport.Confirmed == 1 &&
                run.Host.LastReport.ResourcesSpent == 1 && run.World.State.Fired.Count == 1);
            run.World.Round = 10;
            run.World.Replenish();
            Assert(run.Apply(true).Allowed);
            Assert(run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.ResourcesSpent == 2);
            Assert(run.World.State.Fired.Count == 3 && run.World.State.Active["unit-t1"].Key != "manual-instance");
        }

        private static void TestHeroismRepeatDiagnosticFailure(string root, string mode)
        {
            var run = new RepeatSession(root, mode, "diagnostic-failure");
            run.World.ThrowEvidence = true;
            Assert(run.Apply().Allowed);
            Assert(run.Host.LastReport.Confirmed == 2 && run.Host.LastReport.ResourcesSpent == 2 &&
                run.Host.LastReport.NotProcessed == 0 && run.World.EvidenceReads == 2);
            Assert(run.Host.LastReport.Entries.All(entry =>
                entry.Detail.Contains("effect-evidence:unreadable:InvalidOperationException")));
        }
    }
}
