using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Execution
{
    // One step of a qualification run as observed: the Apply decision, the
    // run report, and per target the effect transition and the source
    // availability before and after (from fresh native reads).
    public sealed class CastingQualificationStepResult
    {
        internal CastingQualificationStepResult(string name)
        {
            Name = name ?? string.Empty;
        }

        public string Name { get; private set; }
        public bool ApplyAllowed { get; internal set; }
        public string ApplyReason { get; internal set; }
        public string ProjectionId { get; internal set; }
        public CastingRunReport Report { get; internal set; }
        // "<castingId>:<transition>" per observed casting target.
        public List<string> Transitions { get; } = new List<string>();
        // "<castingId>:<before>><after>" source availability per casting.
        public List<string> Availability { get; } = new List<string>();
        public List<string> Observations { get; } = new List<string>();

        internal string TransitionOf(string castingId)
        {
            string prefix = castingId + ":";
            string entry = Transitions.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
            return entry == null ? "unobserved" : entry.Substring(prefix.Length);
        }

        internal CastingOutcomeState? StateOf(string castingId)
        {
            if (Report == null) return null;
            CastingOutcomeEntry entry = Report.Entries.FirstOrDefault(value =>
                string.Equals(value.CastingId, castingId, StringComparison.Ordinal));
            return entry == null ? (CastingOutcomeState?)null : entry.State;
        }
    }

    // What one guarded qualification run did, judged by Unity-free rules.
    public sealed class CastingQualificationRecord
    {
        public bool CastingScenario { get; set; }
        public string AllowanceStatus { get; set; } = "not-read";
        public string TerminalReason { get; set; }
        public CastingQualificationSelection Selection { get; set; }
        public IReadOnlyList<CastingQualificationStepForecast> Forecast { get; set; }
        public List<CastingQualificationStepResult> Steps { get; } = new List<CastingQualificationStepResult>();
        public List<string> Failures { get; } = new List<string>();
        public IReadOnlyList<string> Submissions { get; set; } = new string[0];
        public int PlannedSubmissions { get; set; }
        public int MaximumSubmissions { get; set; }

        internal CastingQualificationStepResult Step(string name)
        {
            return Steps.FirstOrDefault(value => value.Name == name);
        }

        // Every expectation of the zero-cost-mixed sequence. A selection-only
        // run needs one selected recipe with three projecting steps; a
        // casting run must also show each step exactly as forecast, with
        // effects and (unchanged, free) resources observed separately.
        public IList<string> Violations()
        {
            var violations = new List<string>(Failures);
            if (Selection == null || !Selection.Selected)
                violations.Add("selection:" + (Selection == null ? "missing" : Selection.Refusal));
            if (Forecast == null || Forecast.Count != 3 || Forecast.Any(step => step.ProjectionId == null))
                violations.Add("forecast-incomplete");
            if (!CastingScenario || violations.Count != 0) return violations;
            if (AllowanceStatus != "valid") violations.Add("allowance:" + AllowanceStatus);
            if (TerminalReason != "completed") violations.Add("terminal:" + (TerminalReason ?? "none"));
            if (PlannedSubmissions > MaximumSubmissions)
                violations.Add("submission-cap:" + PlannedSubmissions + ">" + MaximumSubmissions);
            if (Submissions.Any(value => value.StartsWith("refused:", StringComparison.Ordinal)))
                violations.Add("refused-submission:" + string.Join("|", Submissions.ToArray()));
            foreach (string name in StepNames)
            {
                string failure = StepFailure(name);
                if (failure != null) violations.Add(name + ":" + failure);
            }
            return violations;
        }

        public static readonly string[] StepNames = { "stop", "complete", "repeat", "recast" };

        // The rule for ONE step, applied by the driver the moment the step
        // ends (the run stops at the first mismatch, before anything else
        // is submitted) and again by Violations over the whole record.
        // Null when the step is exactly as expected.
        public string StepFailure(string name)
        {
            CastingQualificationStepResult step = Step(name);
            if (step == null) return "missing";
            if (Selection == null || !Selection.Selected) return "no-selection";
            IReadOnlyList<PlannedCasting> castings = Selection.Castings;
            string first = castings[0].CastingId;
            List<string> rest = castings.Skip(1).Select(value => value.CastingId).ToList();
            if (name == "repeat")
                return step.ApplyAllowed || step.Report != null ||
                    step.ApplyReason != "nothing-to-cast:" + castings.Count
                        ? "not-a-no-op:" + step.ApplyReason : null;
            if (step.Report == null) return "report:none";
            if (step.Report.CleanupFailures.Count != 0)
                return "cleanup:" + string.Join("|", step.Report.CleanupFailures.ToArray());
            string failure = null;
            if (name == "stop")
            {
                if (!step.Report.Cancelled ||
                    step.Report.TerminalReason != "cancelled:" + CastingQualificationDriver.StopReason)
                    failure = "report:" + step.Report.TerminalReason;
                else if (step.StateOf(first) != CastingOutcomeState.EffectConfirmed ||
                    rest.Any(id => step.StateOf(id) != CastingOutcomeState.NotProcessed) ||
                    step.Report.Submitted != 1)
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != "new-instance" ||
                    rest.Any(id => step.TransitionOf(id) != "absent"))
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
            }
            else if (name == "complete")
            {
                if (step.Report.TerminalReason != "completed")
                    failure = "report:" + step.Report.TerminalReason;
                else if (step.StateOf(first) != CastingOutcomeState.Skipped ||
                    rest.Any(id => step.StateOf(id) != CastingOutcomeState.EffectConfirmed))
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != "unchanged" ||
                    rest.Any(id => step.TransitionOf(id) != "new-instance"))
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
            }
            else if (name == "recast")
            {
                string transition = step.TransitionOf(first);
                if (step.Report.TerminalReason != "completed")
                    failure = "report:" + step.Report.TerminalReason;
                else if (step.StateOf(first) != CastingOutcomeState.EffectConfirmed ||
                    rest.Any(id => step.StateOf(id) != CastingOutcomeState.Skipped))
                    failure = "states:" + States(step);
                else if ((transition != "new-instance" && transition != "refreshed") ||
                    rest.Any(id => step.TransitionOf(id) != "unchanged"))
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
            }
            else return "unknown-step";
            if (failure != null) return failure;
            foreach (string availability in step.Availability)
            {
                string[] parts = availability.Split(new[] { ":" }, 2, StringSplitOptions.None);
                string[] values = parts.Length == 2
                    ? parts[1].Split(new[] { ">" }, StringSplitOptions.None) : new string[0];
                if (values.Length != 2 || values[0] != values[1] || values[0] == "unknown")
                    return "resource:" + availability;
            }
            return null;
        }

        private static string States(CastingQualificationStepResult step)
        {
            return step.Report == null ? "none" : string.Join(",", step.Report.Entries
                .Select(entry => entry.CastingId + "=" + entry.State).ToArray());
        }
    }

    // The zero-cost-mixed qualification as a per-frame state machine over
    // the PRODUCTION session, gate, converter, boundary rules and execution
    // host. Every Apply goes through CastingWorkspaceSession.Apply with
    // fresh inputs; only the qualification boundary (approved projections
    // in order, submission budget) stands between it and the host. Steps:
    // select -> author/accept -> stop (the run is stopped after its first
    // casting) -> complete (the rest; the first is skipped as active) ->
    // repeat (nothing to cast) -> recast (Always recast on the first
    // casting, accepted, then the session is REOPENED from disk and the
    // restored acceptance authorizes the run).
    public sealed class CastingQualificationDriver
    {
        public const string StopReason = "qualification-stop";

        private readonly CastingQualificationAllowance _allowance;
        private readonly string _campaignId;
        private readonly Func<CastingWorkspaceInputs> _freshInputs;
        private readonly Func<ICastingDispatchBoundary, CastingWorkspaceSession> _openSession;
        private readonly CastingExecutionHost _host;
        private readonly Func<CastStep, string, ProbeObservation> _observe;
        private readonly Func<long> _clock;
        private readonly long _deadlineMillis;
        private CastingQualificationBoundary _boundary;
        private CastingWorkspaceSession _session;
        private CastingQualificationStepResult _running;
        private Dictionary<string, ProbeObservation> _before;
        private long _startedMillis = -1;
        private string _phase = "select";

        public CastingQualificationDriver(CastingQualificationRecord record,
            CastingQualificationAllowance allowance, string campaignId,
            Func<CastingWorkspaceInputs> freshInputs,
            Func<ICastingDispatchBoundary, CastingWorkspaceSession> openSession,
            CastingExecutionHost host, Func<CastStep, string, ProbeObservation> observe,
            Func<long> clock, long deadlineMillis)
        {
            Record = record ?? throw new ArgumentNullException("record");
            _allowance = allowance;
            _campaignId = campaignId;
            _freshInputs = freshInputs ?? throw new ArgumentNullException("freshInputs");
            _openSession = openSession ?? throw new ArgumentNullException("openSession");
            _host = host ?? throw new ArgumentNullException("host");
            _observe = observe;
            _clock = clock ?? throw new ArgumentNullException("clock");
            _deadlineMillis = deadlineMillis;
        }

        public CastingQualificationRecord Record { get; private set; }
        public bool Completed { get; private set; }
        public string Phase { get { return _phase; } }

        public void Terminate(string reason)
        {
            if (Completed) return;
            if (_host.IsRunning) _host.Cancel(reason);
            RecordInterruptedStep();
            Finish(string.IsNullOrEmpty(reason) ? "terminated" : reason);
        }

        // A step cancelled in flight (deadline, shutdown, exception) keeps
        // its run report and the post-run reads in the record.
        private void RecordInterruptedStep()
        {
            CastingQualificationStepResult running = _running;
            _running = null;
            if (running == null) return;
            running.Report = _host.LastReport;
            try { ObserveTransitions(running, running.Name + "-interrupted"); }
            catch (Exception exception)
            {
                running.Observations.Add("interrupted-reads-failed:" + exception.GetType().Name);
            }
        }

        private void Finish(string reason)
        {
            if (Completed) return;
            Completed = true;
            Record.TerminalReason = reason;
            if (_boundary != null)
            {
                Record.Submissions = _boundary.Submissions.ToList();
                Record.PlannedSubmissions = _boundary.PlannedSubmissions;
            }
        }

        private void Fail(string failure)
        {
            Record.Failures.Add(_phase + ":" + failure);
            Finish("failed:" + _phase);
        }

        public void Update()
        {
            if (Completed) return;
            if (_startedMillis >= 0 && _clock() - _startedMillis > _deadlineMillis)
            {
                Terminate("qualification-deadline");
                return;
            }
            try { Advance(); }
            catch (Exception exception)
            {
                if (_host.IsRunning) _host.Cancel("qualification-exception");
                RecordInterruptedStep();
                Fail("exception:" + exception.GetType().Name + ":" + exception.Message);
            }
        }

        private void Advance()
        {
            switch (_phase)
            {
                case "select": Select(); return;
                case "author": Author(); return;
                case "stop": Begin(CastingQualificationForecast.Stop); return;
                case "stop-wait": Wait(true, "complete"); return;
                case "complete": Begin(CastingQualificationForecast.Complete); return;
                case "complete-wait": Wait(false, "repeat"); return;
                case "repeat": Repeat(); return;
                case "recast-edit": RecastEdit(); return;
                case "recast": Begin(CastingQualificationForecast.Recast); return;
                case "recast-wait": Wait(false, "done"); return;
                default: Finish("completed"); return;
            }
        }

        private void Select()
        {
            CastingWorkspaceInputs inputs = _freshInputs();
            Record.Selection = CastingQualificationRecipe.SelectZeroCostMixed(inputs, _campaignId);
            if (!Record.Selection.Selected) { Fail("selection-refused:" + Record.Selection.Refusal); return; }
            Record.Forecast = CastingQualificationForecast.Forecast(Record.Selection, inputs, _campaignId);
            if (!Record.CastingScenario) { Finish("completed"); return; }
            if (_allowance == null) { Fail("allowance-missing:" + Record.AllowanceStatus); return; }
            if (!string.Equals(_allowance.FixtureGameId, _campaignId, StringComparison.Ordinal))
            {
                Record.AllowanceStatus = "fixture-mismatch";
                Fail("allowance-fixture-mismatch");
                return;
            }
            if (!Record.Forecast.Select(step => step.ProjectionId)
                    .SequenceEqual(_allowance.ApprovedProjectionIds, StringComparer.Ordinal))
            {
                Record.AllowanceStatus = "projections-differ-from-forecast";
                Fail("allowance-projections-differ-from-forecast");
                return;
            }
            Record.AllowanceStatus = "valid";
            Record.MaximumSubmissions = _allowance.MaximumNativeSubmissions;
            _boundary = new CastingQualificationBoundary(_allowance, _host,
                () => _session == null ? null : _session.ExecutionSettings);
            _phase = "author";
        }

        private void Author()
        {
            _session = _openSession(_boundary);
            foreach (PlannedCasting casting in Record.Selection.Castings)
            {
                AuthoringEditResult added = _session.AddCastingForRuntime(casting);
                if (!added.Applied) { Fail("author-refused:" + casting.CastingId + ":" + added.Reason); return; }
            }
            _session.SetExecutionMode("instant");
            _session.Save();
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("accept-refused"); return; }
            _startedMillis = _clock();
            _phase = "stop";
        }

        private void Begin(string name)
        {
            var step = new CastingQualificationStepResult(name);
            Record.Steps.Add(step);
            _before = ObserveAll(step, name + "-before");
            WorkspaceApplyResult result = _session.Apply(CastingApplyMode.Ordinary,
                CastingQualificationRecipe.RoutineId, _freshInputs());
            step.ApplyAllowed = result.Allowed;
            step.ApplyReason = result.Allowed && result.Dispatch != null
                ? result.Dispatch.Reason : result.ReviewReason;
            step.ProjectionId = result.Projection == null ? null : result.Projection.ProjectionId;
            if (!result.Allowed || result.Dispatch == null || !result.Dispatch.Submitted)
            {
                Fail("apply-refused:" + step.ApplyReason);
                return;
            }
            _running = step;
            _phase = name + "-wait";
        }

        // Pumps the run; the stop step cancels as soon as the first casting
        // has finished (the coordinator is then between castings).
        private void Wait(bool stopAfterFirst, string next)
        {
            if (_host.IsRunning && stopAfterFirst && _host.ActiveFinishedCastings >= 1)
                _host.Cancel(StopReason);
            if (_host.IsRunning)
            {
                _host.Pump();
                return;
            }
            CastingQualificationStepResult finished = _running;
            _running = null;
            finished.Report = _host.LastReport;
            ObserveTransitions(finished, finished.Name + "-after");
            // Review P0: the step is judged NOW. A failed, uncertain,
            // cancelled or otherwise unexpected step ends the run before
            // anything else is submitted.
            string failure = Record.StepFailure(finished.Name);
            if (failure != null) { Fail("step:" + failure); return; }
            _phase = next;
        }

        private void Repeat()
        {
            var step = new CastingQualificationStepResult("repeat");
            Record.Steps.Add(step);
            WorkspaceApplyResult result = _session.Apply(CastingApplyMode.Ordinary,
                CastingQualificationRecipe.RoutineId, _freshInputs());
            step.ApplyAllowed = result.Allowed;
            step.ApplyReason = result.Allowed && result.Dispatch != null
                ? result.Dispatch.Reason : result.ReviewReason;
            if (result.Allowed && result.Dispatch != null && result.Dispatch.Submitted)
            {
                // Never expected (the boundary only approves the recast id
                // next); stop it and record the failure.
                _host.Cancel("qualification-unexpected-run");
                Fail("repeat-submitted:" + step.ApplyReason);
                return;
            }
            // Only the exact no-op (every casting already active) continues.
            string failure = Record.StepFailure("repeat");
            if (failure != null) { Fail("step:" + failure); return; }
            _phase = "recast-edit";
        }

        private void RecastEdit()
        {
            PlannedCasting first = _session.Document.Castings.First(value =>
                value.CastingId == Record.Selection.Castings[0].CastingId);
            _session.FocusCasting(first.CastingId);
            AuthoringEditResult updated = _session.UpdateFocusedCasting(new PlannedCasting(
                first.CastingId, first.RoutineId, first.Order, first.SourceId, first.Ability,
                first.CasterUnitId, first.SpellbookGuid, first.TargetMode, first.DirectTargetUnitId,
                first.Origin, first.RequiredCoverageUnitIds, first.TargetingModifiers,
                first.Enhancements, ExistingEffectPolicy.Overwrite, first.IgnoredPresenceMarkers,
                first.State, first.Provenance));
            if (!updated.Applied) { Fail("recast-edit-refused:" + updated.Reason); return; }
            _session.Save();
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("recast-accept-refused"); return; }
            string accepted = _session.AcceptedDigestFor(CastingQualificationRecipe.RoutineId);
            // Close and reopen: a NEW session from disk must restore exactly
            // the acceptance just given (review P3-5: not merely any stored
            // one) and let the unchanged plan run without ceremony.
            _session = _openSession(_boundary);
            string restored = _session.AcceptedDigestFor(CastingQualificationRecipe.RoutineId);
            if (accepted == null || restored != accepted)
            {
                Fail("reopen-acceptance-not-restored:" + (restored ?? "none"));
                return;
            }
            _phase = "recast";
        }

        // Fresh native reads of every recipe casting target and source.
        private Dictionary<string, ProbeObservation> ObserveAll(CastingQualificationStepResult step,
            string label)
        {
            var observations = new Dictionary<string, ProbeObservation>(StringComparer.Ordinal);
            ExplicitStepConversion stop = Record.Forecast[0].Projection;
            if (_observe == null || stop == null) return observations;
            for (int index = 0; index < stop.Plan.Steps.Count; index++)
            {
                string castingId = stop.CastingIds[index];
                ProbeObservation observation;
                try { observation = _observe(stop.Plan.Steps[index], label + ":" + castingId); }
                catch (Exception exception)
                {
                    observation = ProbeObservation.Failed(label, 0, DateTime.UtcNow,
                        "observer-exception:" + exception.GetType().Name);
                }
                observations[castingId] = observation;
                step.Observations.Add(castingId + ":" + (observation == null ? "null" : observation.Describe()));
            }
            return observations;
        }

        private void ObserveTransitions(CastingQualificationStepResult step, string label)
        {
            Dictionary<string, ProbeObservation> after = ObserveAll(step, label);
            foreach (KeyValuePair<string, ProbeObservation> pair in after)
            {
                ProbeObservation before;
                _before.TryGetValue(pair.Key, out before);
                step.Transitions.Add(pair.Key + ":" + Transition(before, pair.Value));
                step.Availability.Add(pair.Key + ":" + Available(before) + ">" + Available(pair.Value));
            }
        }

        internal static string Transition(ProbeObservation before, ProbeObservation after)
        {
            if (before == null || after == null || !before.Succeeded || !after.Succeeded ||
                before.EffectInstances == null || after.EffectInstances == null)
                return "unknown";
            var prior = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (ProbeEffectInstance instance in before.EffectInstances)
                prior[instance.EffectId + "#" + instance.InstanceKey] = instance.EndTimeTicks;
            if (after.EffectInstances.Any(instance =>
                    !prior.ContainsKey(instance.EffectId + "#" + instance.InstanceKey)))
                return "new-instance";
            if (after.EffectInstances.Any(instance =>
                    instance.EndTimeTicks > prior[instance.EffectId + "#" + instance.InstanceKey]))
                return "refreshed";
            if (after.EffectInstances.Count == 0)
                return prior.Count == 0 ? "absent" : "removed";
            return "unchanged";
        }

        private static string Available(ProbeObservation observation)
        {
            return observation == null || !observation.Succeeded ||
                observation.AvailableForCast == null
                    ? "unknown" : observation.AvailableForCast.Value.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
