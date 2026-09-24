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
    // One exact prepared-slot token as read before and after a step (review
    // RC1): the token id is opaque (native ids contain "|") and the states
    // are typed, so validation never re-parses presentation text. A null
    // state means the token was absent from that read.
    public sealed class CastingQualificationTokenReading
    {
        internal CastingQualificationTokenReading(string castingId, string tokenId,
            bool? before, bool? after)
        {
            CastingId = castingId ?? string.Empty;
            TokenId = tokenId ?? string.Empty;
            Before = before;
            After = after;
        }

        public string CastingId { get; private set; }
        public string TokenId { get; private set; }
        public bool? Before { get; private set; }
        public bool? After { get; private set; }

        // Presentation only.
        public override string ToString()
        {
            return CastingId + " " + TokenId + " " + State(Before) + ">" + State(After);
        }

        internal static string State(bool? value)
        {
            return value == null ? "?" : value.Value ? "T" : "F";
        }
    }

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
        // The exact prepared tokens this step observes per casting (none for
        // non-prepared reservations), and the castings whose token read
        // exists on only one side.
        public List<CastingQualificationTokenReading> TokenReadings { get; } =
            new List<CastingQualificationTokenReading>();
        public List<string> UnreadTokenCastings { get; } = new List<string>();
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
        // The party snapshot the selection saw (units and pets), as evidence.
        public IReadOnlyList<Domain.Providers.UnitSnapshot> Roster { get; set; } =
            new Domain.Providers.UnitSnapshot[0];
        public IReadOnlyList<CastingQualificationStepForecast> Forecast { get; set; }
        public List<CastingQualificationStepResult> Steps { get; } = new List<CastingQualificationStepResult>();
        public List<string> Failures { get; } = new List<string>();
        public IReadOnlyList<string> Submissions { get; set; } = new string[0];
        public int PlannedSubmissions { get; set; }
        public int MaximumSubmissions { get; set; }
        // The casting mode the allowance approved (every run uses it).
        public string ExecutionMode { get; set; }
        // The player's stop in the stop step: how it was delivered, whether
        // the host took it as the player stop, and whether the first
        // casting was still in progress when it arrived.
        public string StopPress { get; set; }
        public bool StopPressHandled { get; set; }
        public bool? StopPressedInFlight { get; set; }
        // The disable step: how the planner was disabled, at which point of
        // the run ("in-flight", "before-start", ...), the whole updates it
        // stayed disabled (nothing may run or be accepted then), and whether
        // the host accepted runs again once the planner was enabled.
        public string Disable { get; set; }
        public string DisabledAt { get; set; }
        public int DisableHeldUpdates { get; set; }
        public bool AcceptingWhileDisabled { get; set; }
        public bool RunningWhileDisabled { get; set; }
        // Observed across the hold (re-review): how often the planner's
        // owner ticked (null when unread; it must not tick while disabled)
        // and how many runs the host started (none may start).
        public long? OwnerTicksDuringHold { get; set; }
        public int RunsStartedDuringHold { get; set; }
        public bool AcceptingAfterEnable { get; set; }
        // The recover step's lifecycle evidence: the owner's probe (live
        // subscriptions, HUD roots, planner roots, game mode) read just
        // before the disable and again after the recover run, and the
        // host's run counts then: nothing may be duplicated by the disable
        // and enable, and every started run is reported exactly once.
        public string LifecycleBefore { get; set; }
        public string LifecycleAfter { get; set; }
        public int RunsStarted { get; set; }
        public int RunsReported { get; set; }
        public string CallbackFailure { get; set; }

        private bool HasDisableStep
        {
            get { return Selection != null && CastingQualificationRecipe.HasDisableStep(Selection.Recipe); }
        }

        private bool IsGroup
        {
            get { return Selection != null && CastingQualificationRecipe.IsGroupRecipe(Selection.Recipe); }
        }

        // The judged steps of this record's recipe, in order.
        public IEnumerable<string> JudgedSteps
        {
            get
            {
                if (IsGroup) return GroupStepNames;
                return HasDisableStep
                    ? StepNames.Concat(new[] { CastingQualificationForecast.Disable, CastingQualificationForecast.Recover })
                    : StepNames;
            }
        }

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
            if (Forecast == null || Selection == null ||
                Forecast.Count != CastingQualificationRecipe.ForecastSteps(Selection.Recipe) ||
                Forecast.Any(step => step.ProjectionId == null))
                violations.Add("forecast-incomplete");
            if (!CastingScenario || violations.Count != 0) return violations;
            if (AllowanceStatus != "valid") violations.Add("allowance:" + AllowanceStatus);
            if (TerminalReason != "completed") violations.Add("terminal:" + (TerminalReason ?? "none"));
            if (PlannedSubmissions > MaximumSubmissions)
                violations.Add("submission-cap:" + PlannedSubmissions + ">" + MaximumSubmissions);
            if (Submissions.Any(value => value.StartsWith("refused:", StringComparison.Ordinal)))
                violations.Add("refused-submission:" + string.Join("|", Submissions.ToArray()));
            foreach (string name in JudgedSteps)
            {
                string failure = StepFailure(name);
                if (failure != null) violations.Add(name + ":" + failure);
            }
            // The stop is the player's own: pressed once and taken by the
            // host as the player stop. An animated cast spans many frames,
            // so there the press must land while the first cast is in
            // progress (the stop waits for it to complete). The group
            // recipe has no stop step.
            if (!IsGroup)
            {
                if (StopPress == null) violations.Add("stop-press:none");
                else if (!StopPressHandled) violations.Add("stop-press:not-handled:" + StopPress);
                else if (ExecutionMode == "animated" && StopPressedInFlight != true)
                    violations.Add("stop-press:not-in-flight:" + StopPress);
            }
            // The disable lands while the animated cast is in progress, or,
            // for instant, before the run's first step (a disable-before-
            // start case, never claimed as an in-flight instant
            // interruption); it is held over whole updates with nothing
            // running or accepted, and the host accepts runs again once the
            // planner is enabled.
            if (HasDisableStep)
            {
                string disableFailure = DisableFailure();
                if (disableFailure != null) violations.Add(disableFailure);
                // After the recover run: the same lifecycle state as before
                // the disable, read by a real probe (review B7), and exactly
                // one report per started run.
                int submitted = Submissions.Count(value => value.StartsWith("submitted:", StringComparison.Ordinal));
                if (!ProbeRead(LifecycleBefore) || LifecycleBefore != LifecycleAfter)
                    violations.Add("lifecycle:" + (LifecycleBefore ?? "none") + ">" + (LifecycleAfter ?? "none"));
                if (RunsStarted != submitted || RunsReported != submitted)
                    violations.Add("runs:started=" + RunsStarted + ";reported=" + RunsReported +
                        ";submitted=" + submitted);
                if (CallbackFailure != null) violations.Add("callback:" + CallbackFailure);
            }
            return violations;
        }

        public static readonly string[] StepNames = { "stop", "complete", "repeat", "recast" };
        public static readonly string[] GroupStepNames = { "prime", "mixed", "repeat" };

        // The disable rules, applied the moment the disable step ends (so a
        // failed rule stops the run before the recover run is submitted)
        // and again by Violations. Null when the disable is as required.
        public string DisableFailure()
        {
            string expectedAt = ExecutionMode == "animated" ? "in-flight" : "before-start";
            if (Disable == null) return "disable:none";
            if (DisabledAt != expectedAt) return "disable:not-" + expectedAt + ":" + Disable;
            if (DisableHeldUpdates < CastingQualificationDriver.DisableHoldUpdates) return "disable:not-held:" + Disable;
            if (AcceptingWhileDisabled || RunningWhileDisabled) return "disable:active-while-disabled:" + Disable;
            if (OwnerTicksDuringHold == null || OwnerTicksDuringHold.Value != 0)
                return "disable:owner-ticked-while-disabled:" + Disable;
            if (RunsStartedDuringHold != 0) return "disable:run-started-while-disabled:" + Disable;
            if (!AcceptingAfterEnable) return "disable:not-resumed:" + Disable;
            return null;
        }

        // A lifecycle line from the owner's probe, not a stand-in for one.
        public static bool ProbeRead(string lifecycle)
        {
            return !string.IsNullOrEmpty(lifecycle) && lifecycle != CastingQualificationDriver.Unprobed &&
                lifecycle != "null" && !lifecycle.StartsWith("probe-failed:", StringComparison.Ordinal);
        }

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
                    step.Report.TerminalReason != "cancelled:" + CastingExecutionHost.PlayerStopReason)
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
            else if (name == CastingQualificationForecast.Disable)
            {
                // The disable ends the run: the animated cast in progress is
                // interrupted and cleaned up (submitted, never confirmed),
                // an instant run ends before its first step (nothing
                // submitted); nothing lands and the rest stay skipped.
                bool animated = ExecutionMode == "animated";
                CastingOutcomeEntry entry = step.Report.Entries.FirstOrDefault(value =>
                    string.Equals(value.CastingId, first, StringComparison.Ordinal));
                if (!step.Report.Cancelled ||
                    step.Report.TerminalReason != "cancelled:" + CastingQualificationDriver.DisableReason)
                    failure = "report:" + step.Report.TerminalReason;
                else if (entry == null || entry.Submitted != animated ||
                    entry.State != (animated ? CastingOutcomeState.Cancelled : CastingOutcomeState.NotProcessed) ||
                    !entry.Detail.StartsWith(animated ? "Cancelled:cancelled-in-flight" : "stopped-before-start",
                        StringComparison.Ordinal) ||
                    rest.Any(id => step.StateOf(id) != CastingOutcomeState.Skipped))
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != "unchanged" ||
                    rest.Any(id => step.TransitionOf(id) != "unchanged"))
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
            }
            else if (name == CastingQualificationForecast.Prime)
            {
                // The direct casting alone reaches its recipient.
                if (step.Report.TerminalReason != "completed")
                    failure = "report:" + step.Report.TerminalReason;
                else if (step.StateOf(first) != CastingOutcomeState.EffectConfirmed ||
                    step.Report.Entries.Count != 1 || step.Report.Submitted != 1)
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != "new-instance")
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
            }
            else if (name == CastingQualificationForecast.Mixed)
                failure = MixedFailure(step, castings);
            else if (name == "recast" || name == CastingQualificationForecast.Recover)
            {
                // recast, and recover (the same Always recast plan as a new
                // run after the planner is enabled again).
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
            return ResourceFailure(step, castings);
        }

        // The mixed step (group recipe): the direct casting is skipped (its
        // recipient holds the effect); each group casting runs once (one
        // invocation, one unit of cost whatever the number of beneficiaries)
        // and is confirmed; the pre-covered recipient keeps, refreshes or
        // receives the effect, and every other expected recipient receives
        // a new instance. The direct casting's recipient is the group
        // casting's only pre-covered recipient.
        private string MixedFailure(CastingQualificationStepResult step, IReadOnlyList<PlannedCasting> castings)
        {
            string first = castings[0].CastingId;
            List<string> groups = castings.Skip(1).Select(value => value.CastingId).ToList();
            if (step.Report.TerminalReason != "completed") return "report:" + step.Report.TerminalReason;
            if (step.StateOf(first) != CastingOutcomeState.Skipped ||
                groups.Any(id => step.StateOf(id) != CastingOutcomeState.EffectConfirmed) ||
                step.Report.Submitted != groups.Count)
                return "states:" + States(step);
            string kept = step.TransitionOf(first);
            if (kept != "unchanged" && kept != "refreshed" && kept != "new-instance")
                return "effects:" + first + ":" + kept;
            foreach (string id in groups)
            {
                CastStep cast = ReservedStep(step.Name, id, false);
                if (cast == null || !cast.MassCast) return "group-step-missing:" + id;
                foreach (string recipient in cast.ExpectedRecipientUnitIds)
                {
                    string transition = step.TransitionOf(id + "@" + recipient);
                    bool ok = cast.PreCoveredRecipientUnitIds.Contains(recipient)
                        ? transition == "unchanged" || transition == "refreshed" || transition == "new-instance"
                        : transition == "new-instance";
                    if (!ok) return "effects:" + id + "@" + recipient + ":" + transition;
                }
            }
            CastStep grouped = ReservedStep(step.Name, groups[0], false);
            if (!grouped.PreCoveredRecipientUnitIds.SequenceEqual(new[] { castings[0].DirectTargetUnitId }))
                return "pre-covered:" + string.Join(",", grouped.PreCoveredRecipientUnitIds.ToArray());
            return null;
        }

        // Resources of one step: each casting's native availability drops by
        // exactly the confirmed casts from its finite pool (for prepared
        // slots, of the same spell) and never for verified-free sources; a
        // confirmed prepared casting spends exactly the tokens its step
        // reserved, and nothing else changes any token.
        private string ResourceFailure(CastingQualificationStepResult step,
            IReadOnlyList<PlannedCasting> castings)
        {
            List<string> confirmed = step.Report.Entries
                .Where(entry => entry.State == CastingOutcomeState.EffectConfirmed)
                .Select(entry => entry.CastingId).ToList();
            foreach (string availability in step.Availability)
            {
                string[] parts = availability.Split(new[] { ":" }, 2, StringSplitOptions.None);
                string[] values = parts.Length == 2
                    ? parts[1].Split(new[] { ">" }, StringSplitOptions.None) : new string[0];
                int before;
                int after;
                if (values.Length != 2 || !int.TryParse(values[0], out before) ||
                    !int.TryParse(values[1], out after))
                    return "resource:" + availability;
                int expectedSpend = ExpectedSpend(step.Name, parts[0], confirmed);
                if (before - after != expectedSpend)
                    return "resource:" + availability + ":expected-spend=" + expectedSpend;
            }
            foreach (string castingId in castings.Select(value => value.CastingId))
            {
                CastStep observed = ReservedStep(step.Name, castingId, true);
                bool prepared = observed != null && observed.Reservation != null &&
                    observed.Reservation.TokenIds.Count != 0;
                if (step.UnreadTokenCastings.Contains(castingId))
                    return "tokens:" + castingId + ":unread";
                List<CastingQualificationTokenReading> readings = step.TokenReadings
                    .Where(reading => reading.CastingId == castingId).ToList();
                if (readings.Count == 0)
                {
                    if (prepared && step.Availability.Count != 0)
                        return "tokens:" + castingId + ":unobserved";
                    continue;
                }
                bool spends = prepared && confirmed.Contains(castingId) &&
                    ReservedStep(step.Name, castingId, false) != null;
                foreach (CastingQualificationTokenReading reading in readings)
                {
                    bool ok = spends
                        ? reading.Before == true && reading.After == false
                        : reading.Before.HasValue && reading.Before == reading.After;
                    if (!ok)
                        return "tokens:" + castingId + ":" + reading.TokenId + "=" +
                            CastingQualificationTokenReading.State(reading.Before) + ">" +
                            CastingQualificationTokenReading.State(reading.After);
                }
                if (prepared && !readings.Select(reading => reading.TokenId)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .SequenceEqual(observed.Reservation.TokenIds, StringComparer.Ordinal))
                    return "tokens:" + castingId + ":read-other-tokens";
            }
            return null;
        }

        // The CastStep through which a step observes a casting: its own
        // forecast projection when the step executes it, else (with
        // fallBack) the stop projection's.
        private CastStep ReservedStep(string stepName, string castingId, bool fallBack)
        {
            if (Forecast == null) return null;
            foreach (CastingQualificationStepForecast forecast in new[]
                {
                    Forecast.FirstOrDefault(value => value.Name == stepName),
                    fallBack && Forecast.Count != 0 ? Forecast[0] : null
                })
            {
                ExplicitStepConversion projection = forecast == null ? null : forecast.Projection;
                if (projection == null) continue;
                int index = projection.CastingIds.ToList().IndexOf(castingId);
                if (index >= 0) return projection.Plan.Steps[index];
            }
            return null;
        }

        private int ExpectedSpend(string stepName, string castingId, IList<string> confirmed)
        {
            CastStep observed = ReservedStep(stepName, castingId, true);
            if (observed == null || observed.Reservation == null || observed.Reservation.Unlimited)
                return 0;
            bool prepared = observed.Reservation.TokenIds.Count != 0;
            int spend = 0;
            foreach (string other in confirmed)
            {
                CastStep cast = ReservedStep(stepName, other, true);
                if (cast == null || cast.Reservation == null || cast.Reservation.Unlimited ||
                    cast.Reservation.PoolKey != observed.Reservation.PoolKey)
                    continue;
                if (prepared && cast.Provider.Ability.Canonical != observed.Provider.Ability.Canonical)
                    continue;
                spend++;
            }
            return spend;
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
    // select -> author/accept -> stop (the player's stop, pressed while the
    // first casting is in progress: it completes, nothing after it starts)
    // -> complete (the rest; the first is skipped as active) ->
    // repeat (nothing to cast) -> recast (Always recast on the first
    // casting, accepted, then the session is REOPENED from disk and the
    // restored acceptance authorizes the run).
    public sealed class CastingQualificationDriver
    {
        // The reason the planner's own disable ends a run with (the root's
        // SetEnabled(false), which unchecking the mod calls).
        public const string DisableReason = "mod-disabled";

        private readonly CastingQualificationAllowance _allowance;
        private readonly string _campaignId;
        private readonly Func<CastingWorkspaceInputs> _freshInputs;
        private readonly Func<ICastingDispatchBoundary, CastingWorkspaceSession> _openSession;
        private readonly CastingExecutionHost _host;
        private readonly Func<CastStep, string, ProbeObservation> _observe;
        // A read of one expected recipient of a group casting (its source
        // and that recipient's effects); null where no recipe needs it.
        private readonly Func<CastStep, string, string, ProbeObservation> _observeRecipient;
        private readonly Func<long> _clock;
        private readonly long _deadlineMillis;
        private readonly string _requestedRecipe;
        // Whether a cast can execute in the world now (Default mode, not
        // paused, no full-screen window); null means always.
        private readonly Func<bool> _worldRunning;
        // The host's owner pumps it (the planner's root, once per frame
        // while the world runs); the driver then only waits on it.
        private readonly bool _ownerPumpsHost;
        // The player's routine press as the HUD delivers it; without it the
        // stop goes to the host's own player stop.
        private readonly Func<string, bool> _pressRoutine;
        // The planner's own disable and enable (the root's SetEnabled);
        // without it the disable step shuts the host down and resumes it.
        private readonly Action<bool> _setPlannerEnabled;
        // The owner's lifecycle state (subscriptions, roots, mode) as one
        // comparable line; null means unprobed.
        private readonly Func<string> _lifecycleProbe;
        // How often the planner's owner has ticked it (null: unobservable).
        private readonly Func<long> _ownerTicks;
        private long? _ownerTicksAtDisable;
        private int _runsAtDisable;
        private bool _stopPressed;
        private bool _disabled;
        private bool _enabledAgain;
        private int _disabledUpdates;
        private CastingQualificationBoundary _boundary;
        private Dictionary<string, CastStep> _observeSteps;
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
            Func<long> clock, long deadlineMillis, string recipe = null,
            Func<bool> worldRunning = null, bool ownerPumpsHost = false,
            Func<string, bool> pressRoutine = null, Action<bool> setPlannerEnabled = null,
            Func<string> lifecycleProbe = null, Func<long> ownerTicks = null,
            Func<CastStep, string, string, ProbeObservation> observeRecipient = null)
        {
            _observeRecipient = observeRecipient;
            _lifecycleProbe = lifecycleProbe;
            _ownerTicks = ownerTicks;
            _requestedRecipe = recipe;
            _worldRunning = worldRunning;
            _ownerPumpsHost = ownerPumpsHost;
            _pressRoutine = pressRoutine;
            _setPlannerEnabled = setPlannerEnabled;
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
        // Updates on which a step waited because the world was held.
        public int HeldUpdates { get; private set; }

        private bool WorldHeld()
        {
            if (_worldRunning == null || _worldRunning()) return false;
            HeldUpdates++;
            return true;
        }
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
            // A run that ends during the held disable (deadline, exception,
            // shutdown) never leaves the planner disabled behind it.
            if (_disabled && !_enabledAgain)
            {
                try
                {
                    EnablePlanner();
                    Record.Disable += ";enabled-at-finish";
                }
                catch (Exception exception)
                {
                    Record.Failures.Add("enable-at-finish:" + exception.GetType().Name + ":" + exception.Message);
                }
            }
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
                case "recast-wait":
                    Wait(false, CastingQualificationRecipe.HasDisableStep(Recipe) ? "disable" : "done");
                    return;
                case "disable": Begin(CastingQualificationForecast.Disable); return;
                case "disable-wait": WaitDisable(); return;
                case "recover": Begin(CastingQualificationForecast.Recover); return;
                case "recover-wait": Wait(false, "done"); return;
                case "prime": Begin(CastingQualificationForecast.Prime); return;
                case "prime-wait": Wait(false, "group-author"); return;
                case "group-author": GroupAuthor(); return;
                case "mixed": Begin(CastingQualificationForecast.Mixed); return;
                case "mixed-wait": Wait(false, "repeat"); return;
                default: Finish("completed"); return;
            }
        }

        // The allowance names the recipe of a casting run; a selection-only
        // run uses the requested one (zero-cost-mixed by default). A request
        // that names a different recipe than its allowance is refused.
        private string Recipe
        {
            get
            {
                if (_allowance != null) return _allowance.Recipe;
                return string.IsNullOrEmpty(_requestedRecipe)
                    ? CastingQualificationRecipe.ZeroCostMixed : _requestedRecipe;
            }
        }

        private void Select()
        {
            if (_allowance != null && !string.IsNullOrEmpty(_requestedRecipe) &&
                _requestedRecipe != _allowance.Recipe)
            {
                Record.AllowanceStatus = "recipe-differs-from-request";
                Fail("allowance-recipe-differs:" + _allowance.Recipe + "/" + _requestedRecipe);
                return;
            }
            CastingWorkspaceInputs inputs = _freshInputs();
            Record.Roster = inputs.Snapshot.Units.ToList();
            Record.Selection = CastingQualificationRecipe.Select(Recipe, inputs, _campaignId);
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
            Record.ExecutionMode = _allowance.ExecutionMode;
            Record.MaximumSubmissions = _allowance.MaximumNativeSubmissions;
            _boundary = new CastingQualificationBoundary(_allowance, _host,
                () => _session == null ? null : _session.ExecutionSettings);
            _phase = "author";
        }

        private void Author()
        {
            _session = _openSession(_boundary);
            // The group recipe primes with its direct casting alone; the
            // group castings join after the prime step.
            bool group = CastingQualificationRecipe.IsGroupRecipe(Recipe);
            foreach (PlannedCasting casting in group
                ? Record.Selection.Castings.Take(1) : Record.Selection.Castings)
            {
                AuthoringEditResult added = _session.AddCastingForRuntime(casting);
                if (!added.Applied) { Fail("author-refused:" + casting.CastingId + ":" + added.Reason); return; }
            }
            _session.SetExecutionMode(_allowance.ExecutionMode);
            _session.Save();
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("accept-refused"); return; }
            _startedMillis = _clock();
            _phase = group ? "prime" : "stop";
        }

        // Group recipe: after the prime step the group castings join the
        // plan (the direct casting stays, now covering its recipient), and
        // the changed plan is reviewed and accepted like any edit.
        private void GroupAuthor()
        {
            foreach (PlannedCasting casting in Record.Selection.Castings.Skip(1))
            {
                AuthoringEditResult added = _session.AddCastingForRuntime(casting);
                if (!added.Applied) { Fail("author-refused:" + casting.CastingId + ":" + added.Reason); return; }
            }
            _session.Save();
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("group-accept-refused"); return; }
            _phase = "mixed";
        }

        private void Begin(string name)
        {
            // A step starts (and its before-reads are taken) only while the
            // world runs; a held world waits under the run deadline.
            if (WorldHeld()) return;
            var step = new CastingQualificationStepResult(name);
            Record.Steps.Add(step);
            _observeSteps = StepsToObserve(name);
            _before = ObserveAll(step, name + "-before");
            string beforeFailure = BeforeReadFailure(_observeSteps, _before);
            if (beforeFailure != null)
            {
                // Nothing was applied: zero native submissions for this step.
                Fail("before-read:" + beforeFailure);
                return;
            }
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
            // The instant run is disabled before its first step: each mod
            // update ticks the planner root before this driver, so no pump
            // has run and nothing was submitted. This is the instant
            // disable-before-start case, not an in-flight interruption: an
            // instant cast submits within one pump but confirms and cleans
            // up over later frames, a window this step does not exercise.
            if (name == CastingQualificationForecast.Disable && _allowance.ExecutionMode != "animated")
                DisablePlanner();
        }

        // The number of whole updates the planner stays disabled before it
        // is enabled again (review B7).
        public const int DisableHoldUpdates = 5;
        public const string Unprobed = "unprobed";

        // The animated run is disabled as soon as its cast is in progress;
        // either run then stays disabled over whole updates, with nothing
        // running or accepted, before the planner is enabled again.
        private void WaitDisable()
        {
            if (!_disabled)
            {
                if (_host.IsRunning && _host.ActiveCastingInFlight)
                {
                    DisablePlanner();
                    return;
                }
                Wait(false, "recover");
                return;
            }
            if (!_enabledAgain)
            {
                if (_host.Accepting) Record.AcceptingWhileDisabled = true;
                if (_host.IsRunning) Record.RunningWhileDisabled = true;
                Record.DisableHeldUpdates = ++_disabledUpdates;
                if (_disabledUpdates < DisableHoldUpdates) return;
                // Observed across the whole hold, before the enable.
                long? ticks = ReadOwnerTicks();
                Record.OwnerTicksDuringHold = ticks == null || _ownerTicksAtDisable == null
                    ? (long?)null : ticks.Value - _ownerTicksAtDisable.Value;
                Record.RunsStartedDuringHold = _host.StartedRuns - _runsAtDisable;
                EnablePlanner();
            }
            Wait(false, "recover");
        }

        private long? ReadOwnerTicks()
        {
            if (_ownerTicks == null) return null;
            try { return _ownerTicks(); }
            catch (Exception) { return null; }
        }

        // The planner's own disable, as the mod toggle drives it: the host
        // ends the run through its owned terminal (the cast in progress is
        // interrupted and cleaned up) and refuses runs while disabled.
        private void DisablePlanner()
        {
            Record.LifecycleBefore = Probe();
            _disabled = true;
            string at = !_host.IsRunning ? "after-run"
                : _host.ActiveCastingInFlight ? "in-flight"
                : _host.ActiveFinishedCastings == 0 ? "before-start" : "between-castings";
            if (_setPlannerEnabled != null) _setPlannerEnabled(false);
            else _host.Shutdown(DisableReason);
            _ownerTicksAtDisable = ReadOwnerTicks();
            _runsAtDisable = _host.StartedRuns;
            Record.DisabledAt = at;
            Record.Disable = (_setPlannerEnabled != null ? "planner-disable" : "host-shutdown") +
                ";at=" + at + ";ended=" + !_host.IsRunning;
        }

        // The enable after the held disable: the host accepts runs again.
        private void EnablePlanner()
        {
            _enabledAgain = true;
            if (_setPlannerEnabled != null) _setPlannerEnabled(true);
            else _host.Resume();
            Record.AcceptingAfterEnable = _host.Accepting;
            Record.Disable += ";held=" + _disabledUpdates + ";acceptingWhileDisabled=" +
                Record.AcceptingWhileDisabled + ";runningWhileDisabled=" + Record.RunningWhileDisabled +
                ";ownerTicks=" + (Record.OwnerTicksDuringHold.HasValue
                    ? Record.OwnerTicksDuringHold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "unread") +
                ";runsDuringHold=" + Record.RunsStartedDuringHold + ";accepting=" + _host.Accepting;
        }

        // Waits on the run (pumping it unless its owner does); the stop step
        // presses the player's stop once the first casting is in progress
        // or has finished.
        private void Wait(bool stopAfterFirst, string next)
        {
            if (_host.IsRunning && stopAfterFirst && !_stopPressed &&
                (_host.ActiveCastingInFlight || _host.ActiveFinishedCastings >= 1))
                PressStop();
            if (_host.IsRunning)
            {
                // A run advances only while the world runs.
                if (!WorldHeld() && !_ownerPumpsHost) _host.Pump();
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
            // Re-review: the disable rules (and a real probe line before it)
            // are judged here, so a failed rule stops the run before the
            // recover run is submitted.
            if (finished.Name == CastingQualificationForecast.Disable)
            {
                string disableFailure = Record.DisableFailure() ??
                    (CastingQualificationRecord.ProbeRead(Record.LifecycleBefore) ? null
                        : "lifecycle-unprobed:" + (Record.LifecycleBefore ?? "none"));
                if (disableFailure != null) { Fail("step:" + disableFailure); return; }
            }
            if (finished.Name == CastingQualificationForecast.Recover)
            {
                Record.LifecycleAfter = Probe();
                Record.RunsStarted = _host.StartedRuns;
                Record.RunsReported = _host.ReportedRuns;
                Record.CallbackFailure = _host.LastCallbackFailure;
            }
            _phase = next;
        }

        private string Probe()
        {
            if (_lifecycleProbe == null) return Unprobed;
            try { return _lifecycleProbe() ?? "null"; }
            catch (Exception exception) { return "probe-failed:" + exception.GetType().Name; }
        }

        // The player's stop, delivered once: a routine press exactly as the
        // HUD sends it (the host's own player stop when no press route is
        // given). The cast in progress completes; nothing after it starts.
        private void PressStop()
        {
            _stopPressed = true;
            bool inFlight = _host.ActiveFinishedCastings == 0;
            bool handled = _pressRoutine != null
                ? _pressRoutine(CastingQualificationRecipe.RoutineId)
                : _host.RequestStop(CastingExecutionHost.PlayerStopReason);
            string pending = _host.ActiveStopRequested;
            Record.StopPressedInFlight = inFlight;
            Record.StopPressHandled = handled && (_host.IsRunning
                ? pending == CastingExecutionHost.PlayerStopReason
                : _host.LastReport != null && _host.LastReport.TerminalReason ==
                    "cancelled:" + CastingExecutionHost.PlayerStopReason);
            Record.StopPress = (_pressRoutine != null ? "routine-press" : "host-request") +
                ";handled=" + handled + ";inFlight=" + inFlight + ";pending=" +
                (pending ?? (_host.IsRunning ? "none" : "run-ended"));
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
            _phase = CastingQualificationRecipe.IsGroupRecipe(Recipe) ? "done" : "recast-edit";
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

        // Review RC2: the step casts only on a complete before-state. Every
        // casting to observe needs its own read, succeeded, of the step's
        // own target, with the effect and availability reads, and for a
        // prepared reservation every reserved token. Null when complete.
        internal static string BeforeReadFailure(IReadOnlyDictionary<string, CastStep> steps,
            IReadOnlyDictionary<string, ProbeObservation> observations)
        {
            if (steps == null || steps.Count == 0) return "nothing-to-observe";
            foreach (KeyValuePair<string, CastStep> pair in steps
                .OrderBy(value => value.Key, StringComparer.Ordinal))
                foreach (KeyValuePair<string, string> read in ReadsOf(pair.Key, pair.Value))
                {
                    ProbeObservation observation;
                    if (observations == null || !observations.TryGetValue(read.Key, out observation) ||
                        observation == null)
                        return read.Key + ":missing";
                    if (!observation.Succeeded) return read.Key + ":failed:" + observation.Failure;
                    string target = read.Value ?? pair.Value.TargetUnitIds.FirstOrDefault();
                    if (!string.Equals(observation.TargetUnitId, target, StringComparison.Ordinal))
                        return read.Key + ":wrong-target:" + observation.TargetUnitId;
                    if (observation.EffectInstances == null) return read.Key + ":effects-unread";
                    if (observation.AvailableForCast == null) return read.Key + ":availability-unread";
                    ResourceReservation reservation = pair.Value.Reservation;
                    if (reservation != null && reservation.TokenIds.Count != 0 &&
                        (observation.ReservedTokenAvailability == null ||
                         reservation.TokenIds.Any(id => !observation.ReservedTokenAvailability.ContainsKey(id))))
                        return read.Key + ":tokens-unread";
                }
            return null;
        }

        // Every recipe casting, observed through THIS step's forecast
        // projection when the step executes it (so exactly the tokens it
        // reserves are read) and through the stop projection otherwise.
        private Dictionary<string, CastStep> StepsToObserve(string name)
        {
            var steps = new Dictionary<string, CastStep>(StringComparer.Ordinal);
            foreach (CastingQualificationStepForecast forecast in new[]
                {
                    Record.Forecast[0],
                    Record.Forecast.FirstOrDefault(value => value.Name == name)
                })
            {
                ExplicitStepConversion projection = forecast == null ? null : forecast.Projection;
                if (projection == null) continue;
                for (int index = 0; index < projection.Plan.Steps.Count; index++)
                    steps[projection.CastingIds[index]] = projection.Plan.Steps[index];
            }
            return steps;
        }

        // The reads one casting takes: its own target, or for a group
        // casting every expected recipient (key "<castingId>@<unit>", value
        // the unit); each read carries the casting's source availability.
        internal static IEnumerable<KeyValuePair<string, string>> ReadsOf(string castingId, CastStep step)
        {
            if (!step.MassCast)
                return new[] { new KeyValuePair<string, string>(castingId, null) };
            return step.ExpectedRecipientUnitIds.Select(unit =>
                new KeyValuePair<string, string>(castingId + "@" + unit, unit)).ToList();
        }

        // The casting a read belongs to.
        internal static string CastingOfRead(string readKey)
        {
            int at = readKey.IndexOf('@');
            return at < 0 ? readKey : readKey.Substring(0, at);
        }

        // Fresh native reads of every recipe casting target and source.
        private Dictionary<string, ProbeObservation> ObserveAll(CastingQualificationStepResult step,
            string label)
        {
            var observations = new Dictionary<string, ProbeObservation>(StringComparer.Ordinal);
            if (_observe == null || _observeSteps == null) return observations;
            foreach (KeyValuePair<string, CastStep> pair in _observeSteps
                .OrderBy(value => value.Key, StringComparer.Ordinal))
                foreach (KeyValuePair<string, string> read in ReadsOf(pair.Key, pair.Value))
                {
                    ProbeObservation observation;
                    try
                    {
                        observation = read.Value == null
                            ? _observe(pair.Value, label + ":" + read.Key)
                            : _observeRecipient == null
                                ? ProbeObservation.Failed(label, 0, DateTime.UtcNow, "recipient-observer-missing")
                                : _observeRecipient(pair.Value, read.Value, label + ":" + read.Key);
                    }
                    catch (Exception exception)
                    {
                        observation = ProbeObservation.Failed(label, 0, DateTime.UtcNow,
                            "observer-exception:" + exception.GetType().Name);
                    }
                    observations[read.Key] = observation;
                    step.Observations.Add(read.Key + ":" + (observation == null ? "null" : observation.Describe()));
                }
            return observations;
        }

        private void ObserveTransitions(CastingQualificationStepResult step, string label)
        {
            Dictionary<string, ProbeObservation> after = ObserveAll(step, label);
            var sources = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, ProbeObservation> pair in after
                .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                ProbeObservation before;
                _before.TryGetValue(pair.Key, out before);
                step.Transitions.Add(pair.Key + ":" + Transition(before, pair.Value));
                // One availability and token record per casting: a group
                // casting's reads all carry the same source.
                string castingId = CastingOfRead(pair.Key);
                if (!sources.Add(castingId)) continue;
                step.Availability.Add(castingId + ":" + Available(before) + ">" + Available(pair.Value));
                RecordTokens(step, castingId, before, pair.Value);
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

        // The exact reserved tokens either read named, as typed readings;
        // a casting whose token read exists on only one side is unread.
        internal static void RecordTokens(CastingQualificationStepResult step, string castingId,
            ProbeObservation before, ProbeObservation after)
        {
            IReadOnlyDictionary<string, bool> prior = before == null ? null : before.ReservedTokenAvailability;
            IReadOnlyDictionary<string, bool> next = after == null ? null : after.ReservedTokenAvailability;
            if (prior == null && next == null) return;
            if (prior == null || next == null)
            {
                step.UnreadTokenCastings.Add(castingId);
                return;
            }
            foreach (string token in prior.Keys.Union(next.Keys, StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal))
                step.TokenReadings.Add(new CastingQualificationTokenReading(castingId, token,
                    Lookup(prior, token), Lookup(next, token)));
        }

        private static bool? Lookup(IReadOnlyDictionary<string, bool> tokens, string key)
        {
            bool available;
            return tokens.TryGetValue(key, out available) ? available : (bool?)null;
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
