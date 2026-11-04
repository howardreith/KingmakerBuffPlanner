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
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

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
            private int _instance;
            private static readonly AbilityKey Sentinel = new AbilityKey("sentinel-spell", null, 0,
                SourceKind.Spellbook, null);
            private readonly Dictionary<CastStep, EffectBaseline> _baselines =
                new Dictionary<CastStep, EffectBaseline>();

            public bool IsInCombat { get { return false; } }

            private IEnumerable<ObservedEffectInstance> Read(string target)
            {
                KeyValuePair<string, long> active;
                if (Observation == "unreadable") return null;
                return State.Active.TryGetValue(target, out active) && active.Value > Round
                    ? new[] { new ObservedEffectInstance(
                        Observation == "wrong-kind" ? EffectKind.AreaBuff : EffectKind.Buff,
                        target == "unit-t4" ? "sentinel-effect" : "buff-effect",
                        active.Key, active.Value, Observation == "suppressed") }
                    : new ObservedEffectInstance[0];
            }

            public CastRuntimeValidation Validate(CastStep step)
            {
                bool available = step.Reservation.TokenIds.Count != 0
                    ? step.Reservation.TokenIds.All(id => State.Tokens[id])
                    : State.SpontaneousRemaining >= step.Reservation.Units;
                return available ? CastRuntimeValidation.Pass()
                    : CastRuntimeValidation.Fail("simulated-exhausted-source");
            }

            public InstantCastResult Fire(CastStep step)
            {
                _baselines[step] = new EffectBaseline(step.ExpectedRecipientUnitIds.ToDictionary(
                    id => id, id => Read(id), StringComparer.Ordinal));
                int before = Available(step);
                State.Fired.Add(step.AssignmentId);
                foreach (string token in step.Reservation.TokenIds) State.Tokens[token] = false;
                if (step.Reservation.TokenIds.Count == 0)
                    State.SpontaneousRemaining -= step.Reservation.Units;
                string target = step.TargetUnitIds[0];
                KeyValuePair<string, long> prior;
                State.Active.TryGetValue(target, out prior);
                if (Observation == "replace" || Observation == "suppressed" || Observation == "wrong-kind")
                    State.Active[target] = new KeyValuePair<string, long>("instance-" + (++_instance), Round + 100);
                else if (Observation == "refresh")
                    State.Active[target] = new KeyValuePair<string, long>(prior.Key, Round + 100);
                else if (Observation == "wrong-recipient")
                    State.Active["unit-sorcerer"] = new KeyValuePair<string, long>("instance-" + (++_instance), Round + 100);
                else if (Observation == "missing") State.Active.Remove(target);
                bool succeeded = Observation != "native-failure";
                return new InstantCastResult(true, succeeded, false, Available(step) < before,
                    true, "simulated-native:" + Observation,
                    AvailableCountJudgement.Violation(step.Reservation, before, Available(step)));
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
                return _baselines.TryGetValue(step, out baseline) &&
                    AppliedEffectJudgement.AllReached(step.ExpectedRecipientUnitIds,
                        step.ExpectedEffects, baseline, Read, step.PreCoveredRecipientUnitIds);
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
                    .Select(pair => On(pair.Key, pair.Key == "unit-t4" ? "sentinel-effect" : "buff-effect",
                        pair.Value.Value - Round, 1, 0)).ToArray());
                ProviderSnapshot prepared = current.Snapshot.Providers.Single(value =>
                    value.Key.CasterUnitId == "unit-wizard");
                var wizard = new ProviderSnapshot(prepared.Key, "Heroism-like fixture", 2,
                    prepared.ResourcePoolKey, 1, prepared.EligibleTokenIds, null, 1, 100, "", "10 minutes/level");
                var sentinel = new ProviderSnapshot(new ProviderKey("unit-sorcerer", "book-sorcerer",
                    Sentinel, "level-2"), "Sentinel fixture", 2, "pool-unit-sorcerer", 1,
                    null, null, 1, 100, "", "10 minutes/level");
                var providers = new[] { wizard, sentinel };
                var options = providers.Select(provider => new ProviderPlanningOption(provider,
                    current.Snapshot.Units.Select(unit => unit.UnitId), new[] { provider.Key.CasterUnitId },
                    10, 100, CastExecutionStrategy.DirectRuleCast, "simulated-direct",
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)));
                var effects = new Dictionary<string, EffectExpression>(StringComparer.Ordinal)
                {
                    { "source-bulls", Leaf("buff-effect") },
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
        }

        private sealed class RepeatSession
        {
            internal readonly RepeatBuffWorld World = new RepeatBuffWorld();
            internal readonly CastingExecutionHost Host;
            internal readonly CastingWorkspaceSession Session;
            internal readonly List<CastStep> Steps = new List<CastStep>();

            internal RepeatSession(string root, string mode, string name, bool party = false)
            {
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

            internal WorkspaceApplyResult Apply(bool recast = false)
            {
                foreach (PlannedCasting casting in Session.Document.Castings.ToArray())
                {
                    Session.FocusCasting(casting.CastingId);
                    Session.SetFocusedRecastPolicy(recast ? ExistingEffectPolicy.Overwrite
                        : ExistingEffectPolicy.SkipAlreadyActive);
                }
                // Rebuild from current buffs, clock, remaining slots and replenishment EACH run.
                WorkspaceApplyResult result = Session.Apply(CastingApplyMode.Ordinary, "long", World.Inputs());
                if (result.Projection != null) Steps.AddRange(result.Projection.Plan.Steps);
                int frames = 0;
                while (Host.IsRunning && frames++ < 100) Host.Pump();
                Assert(!Host.IsRunning);
                return result;
            }
        }

        private static void RepeatAssert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
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
            // Presence, kind and readability are observations, not inferred from Fire's success.
            string before = observation == "unreadable" ? "before:{unit-t1=unreadable" :
                "before:{unit-t1=[" + (observation == "wrong-kind" ? "AreaBuff" : "Buff") +
                ":buff-effect#instance-1@end=100";
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
