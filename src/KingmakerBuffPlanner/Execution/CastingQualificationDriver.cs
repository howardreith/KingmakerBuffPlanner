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
        // The enhanced recipe: the caster reads (the enhancement's own
        // resource and every activatable ability of the caster) before and
        // after the step, and per casting the stat modifiers its effect gives
        // its recipient before and after (a null list was not read).
        public CasterEnhancementObservation CasterBefore { get; internal set; }
        public CasterEnhancementObservation CasterAfter { get; internal set; }
        public Dictionary<string, IReadOnlyList<string>> ModifiersBefore { get; } =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        public Dictionary<string, IReadOnlyList<string>> ModifiersAfter { get; } =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        // Per casting, the time left on its recipient's instance right after
        // the step, in seconds (null when unread).
        public Dictionary<string, double?> RemainingSecondsAfter { get; } =
            new Dictionary<string, double?>(StringComparer.Ordinal);

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
        public string RecoveryReload { get; set; }
        // The enhanced recipe: the enhancement options the workspace offered
        // for the focused enhanced casting before the edit and after it ("*"
        // marks a selected one).
        public IReadOnlyList<string> EnhancementOptions { get; set; } = new string[0];
        // The ability-pool recipe: whether the Always recast edit was
        // accepted (recorded; the exhausted plan may be refused either way).
        public bool? ExhaustedAccepted { get; set; }
        // The shared recipes (v1.2 E16-E20). The caster's native state - the
        // Arcane Reservoir and every activatable ability (Share and each
        // Powerful Change toggle), read by the caster observer bound to the
        // selected caster and reservoir pool - is each step's CasterBefore /
        // CasterAfter: before the shared cast, IMMEDIATELY after its cleanup
        // (before any other preparation), before and after the witness, and
        // around the shortage. Unread is a violation, never a pass. The
        // selection (preview) run reads it before anything is authored and
        // after its cleanup: a preview arms and spends nothing.
        public CasterEnhancementObservation PreviewCasterBefore { get; set; }
        public CasterEnhancementObservation PreviewCasterAfter { get; set; }
        // The EXACT reservoir units the shared step's approved projection
        // demands (Share alone, or Share + Powerful Change combined); the
        // OBSERVED delta across the shared step must equal it exactly.
        public int? ShareExpectedSpend { get; set; }
        // The same demand derived independently from the selection's
        // verified snapshots (Share units, plus Powerful Change units on the
        // same reservoir): the projection must charge exactly this, once.
        public int? ShareIndependentDemand { get; set; }
        // The authored shared casting as a FRESH read of the stored plan
        // reports it (what a restart would load), and whether it carries
        // the per-casting Share intent, ally and enhancements as authored.
        public string SharePersistedIntent { get; set; }
        public bool? SharePersisted { get; set; }
        // Disarming Share on the NEXT-casting draft (before the witness)
        // left the authored shared casting's own Share intent unchanged.
        public bool? ShareDraftDisarmKeptCasting { get; set; }
        // The shortage step's extra shared castings (one more than the
        // shared spell's native source could fund).
        public int ShareShortageCastings { get; set; }
        public const string ShortageCastingPrefix = "qual-short-";

        private bool HasDisableStep
        {
            get { return Selection != null && CastingQualificationRecipe.HasDisableStep(Selection.Recipe); }
        }

        private bool IsGroup
        {
            get { return Selection != null && CastingQualificationRecipe.IsGroupRecipe(Selection.Recipe); }
        }

        private bool IsEnhanced
        {
            get { return Selection != null && CastingQualificationRecipe.IsEnhancedRecipe(Selection.Recipe); }
        }

        private bool IsAbilityPool
        {
            get { return Selection != null && Selection.Recipe == CastingQualificationRecipe.AbilityPoolDirect; }
        }

        public bool IsShared
        {
            get
            {
                return Selection != null &&
                    CastingQualificationRecipe.IsSharedRecipe(Selection.Recipe);
            }
        }

        // The judged steps of this record's recipe, in order.
        public IEnumerable<string> JudgedSteps
        {
            get
            {
                if (Selection != null && Selection.Recipe == CastingQualificationRecipe.StopReload) return RecoveryStepNames;
                if (IsGroup) return GroupStepNames;
                if (IsEnhanced) return EnhancedStepNames;
                if (IsAbilityPool) return AbilityPoolStepNames;
                if (IsShared) return SharedStepNames;
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
            if (CastingScenario && Selection != null && Selection.Recipe == CastingQualificationRecipe.StopReload &&
                (RecoveryReload == null || !RecoveryReload.StartsWith("passed=True;", StringComparison.Ordinal)))
                violations.Add("recovery-reload:" + (RecoveryReload ?? "missing"));
            if (Selection == null || !Selection.Selected)
                violations.Add("selection:" + (Selection == null ? "missing" : Selection.Refusal));
            if (Forecast == null || Selection == null ||
                Forecast.Count != CastingQualificationRecipe.ForecastSteps(Selection.Recipe) ||
                Forecast.Any(step => step.ProjectionId == null))
                violations.Add("forecast-incomplete");
            if (IsShared && !CastingScenario)
            {
                // The selection (preview) run (E20 "ordinary preview"):
                // authoring through the graph, forecasting and the cleanup
                // armed nothing and spent nothing, and the authored Share
                // intent was persisted per casting (E16/E17). The casting
                // run's own isolation rules are judged after its steps.
                string preview = CasterDifference("share-preview", PreviewCasterBefore,
                    PreviewCasterAfter, 0);
                if (preview != null) violations.Add(preview);
                if (SharePersisted != true)
                    violations.Add("share-intent-not-persisted:" + (SharePersistedIntent ?? "unread"));
            }
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
            if (IsShared) violations.AddRange(SharedRunFailures());
            // The stop is the player's own: pressed once and taken by the
            // host as the player stop. An animated cast spans many frames,
            // so there the press must land while the first cast is in
            // progress (the stop waits for it to complete). The two-phase
            // recipes have no stop step.
            if (!IsGroup && !IsEnhanced && !IsAbilityPool && !IsShared)
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

        public static readonly string[] RecoveryStepNames = { "stop", "complete" };
        public static readonly string[] StepNames = { "stop", "complete", "repeat", "recast" };
        // The group recipe ends after its mixed step: a repeat is not a
        // no-op there, because the game replaces a covered recipient's
        // longer instance with the group cast's shorter one (seen live,
        // casting-qual-cast-20260924-adv-group-anim-01), so the direct
        // casting is due again. Direct recipes show that a repeat casts
        // nothing.
        public static readonly string[] GroupStepNames = { "prime", "mixed" };
        public static readonly string[] EnhancedStepNames = { "plain", "enhanced" };
        public static readonly string[] AbilityPoolStepNames = { "use", "repeat", "exhausted" };
        public static readonly string[] SharedStepNames = { "shared", "witness", "shortage" };

        // The shared casting run's own isolation and cost rules (v1.2
        // E16-E20), beyond each step's: the projection charges the
        // independently derived demand exactly once; the per-casting Share
        // intent is persisted; a draft disarm leaves the authored casting
        // alone; nothing changes between the shared cast's cleanup and the
        // witness's preparation; and the run starts with Share and every
        // Powerful Change toggle off (so a toggle found on afterwards can
        // only be a leak).
        private List<string> SharedRunFailures()
        {
            var failures = new List<string>();
            if (ShareExpectedSpend == null || ShareIndependentDemand == null)
                failures.Add("share-demand-unread:" + (ShareExpectedSpend == null ? "projection" : "independent"));
            else if (ShareExpectedSpend.Value < 1 || ShareExpectedSpend.Value != ShareIndependentDemand.Value)
                failures.Add("share-demand-not-charged-once:" + ShareExpectedSpend.Value + "!=" +
                    ShareIndependentDemand.Value);
            if (SharePersisted != true)
                failures.Add("share-intent-not-persisted:" + (SharePersistedIntent ?? "unread"));
            if (ShareDraftDisarmKeptCasting != true)
                failures.Add("share-draft-disarm-changed-casting:" +
                    (ShareDraftDisarmKeptCasting == null ? "unread" : "changed"));
            CastingQualificationStepResult shared = Step(CastingQualificationForecast.Shared);
            CastingQualificationStepResult witness = Step(CastingQualificationForecast.Witness);
            if (shared != null && witness != null)
            {
                string between = CasterDifference("share-between-steps", shared.CasterAfter,
                    witness.CasterBefore, 0);
                if (between != null) failures.Add(between);
            }
            string armed = ArmedShareToggles(shared == null ? null : shared.CasterBefore);
            if (armed != null) failures.Add("share-baseline-armed:" + armed);
            return failures;
        }

        // The Share toggle and every Powerful Change toggle that is on (or
        // still running) in a caster read; null when none is, "unread" when
        // the read failed.
        internal string ArmedShareToggles(CasterEnhancementObservation observation)
        {
            if (observation == null || !observation.Succeeded) return "unread";
            var guids = new List<string>();
            string toggle = Selection == null ? null : Selection.Coverage.FirstOrDefault(value =>
                value.StartsWith("toggle:", StringComparison.Ordinal));
            if (toggle != null) guids.Add(toggle.Substring("toggle:".Length));
            guids.AddRange(Compatibility.BrownFurPowerfulChangeProfile.Toggles
                .Select(value => value.ActivatableGuid));
            List<string> armed = observation.Activatables
                .Where(pair => pair.Value && guids.Any(guid =>
                    pair.Key == guid || pair.Key == guid + "@running" ||
                    pair.Key.StartsWith(guid + "#", StringComparison.Ordinal)))
                .Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToList();
            return armed.Count == 0 ? null : string.Join(",", armed.ToArray());
        }

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
            if (name == CastingQualificationForecast.Exhausted)
                return ExhaustedFailure(step, first);
            if (name == CastingQualificationForecast.Shortage)
                return ShortageFailure(step, castings);
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
                else if (step.StateOf(first) != (Selection.Recipe == CastingQualificationRecipe.StopReload
                        ? CastingOutcomeState.Omitted : CastingOutcomeState.Skipped) ||
                    rest.Any(id => step.StateOf(id) != CastingOutcomeState.EffectConfirmed))
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != (Selection.Recipe == CastingQualificationRecipe.StopReload
                        ? "absent" : "unchanged") ||
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
            else if (name == CastingQualificationForecast.Use)
            {
                // The ability alone lands a new instance; its pool's single
                // use is spent (judged with the resources below).
                if (step.Report.TerminalReason != "completed")
                    failure = "report:" + step.Report.TerminalReason;
                else if (step.StateOf(first) != CastingOutcomeState.EffectConfirmed ||
                    step.Report.Entries.Count != 1 || step.Report.Submitted != 1)
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != "new-instance")
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
            }
            else if (name == CastingQualificationForecast.Shared)
            {
                // The shared casting alone lands a new instance on the
                // ally, once, through the route forecast for the armed
                // Share execution identity; the caster's reservoir drops by
                // exactly the projection's demand (Share, or Share + Powerful
                // Change charged once), and every activatable ability -
                // Share and each Powerful Change toggle - is back in its
                // pre-step state at the read taken IMMEDIATELY after the run
                // ended (its cleanup done, nothing else prepared yet).
                if (step.Report.TerminalReason != "completed")
                    failure = "report:" + step.Report.TerminalReason;
                else if (step.StateOf(first) != CastingOutcomeState.EffectConfirmed ||
                    step.Report.Entries.Count != 1 || step.Report.Submitted != 1)
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != "new-instance")
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
                else
                    failure = RouteFailure(step, first) ??
                        CasterFailure(step, ShareExpectedSpend ?? -1);
            }
            else if (name == CastingQualificationForecast.Witness)
            {
                // The witness plain cast by the SAME caster: the shared
                // casting is disabled (kept, not cast; its ally keeps the
                // effect) and the witness lands its own ordinary effect,
                // with no reservoir spend and no Share / Powerful Change
                // state touched.
                if (step.Report.TerminalReason != "completed")
                    failure = "report:" + step.Report.TerminalReason;
                else if (step.StateOf(first) != CastingOutcomeState.Omitted ||
                    rest.Any(id => step.StateOf(id) != CastingOutcomeState.EffectConfirmed) ||
                    step.Report.Submitted != 1)
                    failure = "states:" + States(step);
                else if (step.TransitionOf(first) != "unchanged" ||
                    rest.Any(id => step.TransitionOf(id) != "new-instance"))
                    failure = "effects:" + string.Join(",", step.Transitions.ToArray());
                else
                    failure = CasterFailure(step, 0);
            }
            else if (name == CastingQualificationForecast.Plain)
                failure = PlainFailure(step, castings);
            else if (name == CastingQualificationForecast.Enhanced)
                failure = EnhancedFailure(step, castings);
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

        // The ability-pool recipe's exhausted step: set to Always recast with
        // its pool's single use spent, the casting is refused before
        // anything is submitted, for want of the resource; the pool stays
        // empty and the effect stays exactly as it was.
        private static string ExhaustedFailure(CastingQualificationStepResult step, string casting)
        {
            if (step.ApplyAllowed || step.Report != null) return "not-refused:" + step.ApplyReason;
            string reason = step.ApplyReason ?? string.Empty;
            if (!reason.StartsWith("apply-refused:blocked-casting:" + casting + ":resource-pool-exhausted:", StringComparison.Ordinal))
                return "refused-for-another-reason:" + reason;
            if (!step.Availability.SequenceEqual(new[] { casting + ":0>0" }))
                return "resource:" + string.Join(",", step.Availability.ToArray());
            if (step.TransitionOf(casting) != "unchanged")
                return "effects:" + string.Join(",", step.Transitions.ToArray());
            return null;
        }

        // The enhanced recipe's plain step: the plain casting alone lands a
        // new instance; the enhancement's resource and every activatable
        // ability of the caster are exactly as before; its effect gives the
        // recipient (who had none of it) the enhancement's stat a positive
        // modifier of the enhancement's descriptor.
        private string PlainFailure(CastingQualificationStepResult step, IReadOnlyList<PlannedCasting> castings)
        {
            string plain = castings[0].CastingId;
            CastingQualificationEnhancement enhancement = Selection.Enhancement;
            if (enhancement == null) return "enhancement:none";
            if (step.Report.TerminalReason != "completed") return "report:" + step.Report.TerminalReason;
            if (step.StateOf(plain) != CastingOutcomeState.EffectConfirmed ||
                step.Report.Entries.Count != 1 || step.Report.Submitted != 1)
                return "states:" + States(step);
            if (step.TransitionOf(plain) != "new-instance")
                return "effects:" + string.Join(",", step.Transitions.ToArray());
            string caster = CasterFailure(step, 0);
            if (caster != null) return caster;
            IReadOnlyList<string> before = ModifiersOf(step.ModifiersBefore, plain);
            IReadOnlyList<string> after = ModifiersOf(step.ModifiersAfter, plain);
            if (before == null || after == null) return "modifiers:" + plain + ":unread";
            if (before.Count != 0)
                return "modifiers:" + plain + ":present-before:" + string.Join(";", before.ToArray());
            if (enhancement.Kind == CastingQualificationEnhancement.DurationKind)
            {
                double? remaining;
                step.RemainingSecondsAfter.TryGetValue(plain, out remaining);
                return remaining == null || remaining.Value < CastingQualificationRecipe.MinimumJudgedSeconds
                    ? "duration:" + plain + ":" + (remaining == null ? "unread" : Seconds(remaining.Value) + "<" +
                        Seconds(CastingQualificationRecipe.MinimumJudgedSeconds)) : null;
            }
            int value;
            if (!TryModifierValue(after, enhancement, out value) || value <= 0)
                return "modifiers:" + plain + ":no-" + enhancement.ModifierPrefix + ":" +
                    string.Join(";", after.ToArray());
            return null;
        }

        private static string Seconds(double value)
        {
            return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        // The enhanced step: the plain casting is skipped (its recipient keeps
        // its instance and exactly its modifiers); the enhanced casting lands
        // a new instance whose modifiers are the plain casting's with the
        // enhancement's stat raised by exactly its increase; the
        // enhancement's resource drops by exactly its units per cast; every
        // activatable ability of the caster ends as it began (the one-shot
        // choice consumed, nothing left on or switched off).
        private string EnhancedFailure(CastingQualificationStepResult step, IReadOnlyList<PlannedCasting> castings)
        {
            string plain = castings[0].CastingId;
            string enhanced = castings[1].CastingId;
            CastingQualificationEnhancement enhancement = Selection.Enhancement;
            if (enhancement == null) return "enhancement:none";
            if (step.Report.TerminalReason != "completed") return "report:" + step.Report.TerminalReason;
            if (step.StateOf(plain) != CastingOutcomeState.Skipped ||
                step.StateOf(enhanced) != CastingOutcomeState.EffectConfirmed || step.Report.Submitted != 1)
                return "states:" + States(step);
            if (step.TransitionOf(plain) != "unchanged" || step.TransitionOf(enhanced) != "new-instance")
                return "effects:" + string.Join(",", step.Transitions.ToArray());
            string routeFailure = RouteFailure(step, enhanced);
            if (routeFailure != null) return routeFailure;
            string caster = CasterFailure(step, enhancement.UnitsPerCast);
            if (caster != null) return caster;
            CastingQualificationStepResult plainStep = Step(CastingQualificationForecast.Plain);
            IReadOnlyList<string> reference = plainStep == null ? null
                : ModifiersOf(plainStep.ModifiersAfter, plain);
            if (reference == null) return "modifiers:plain-step-unread";
            IReadOnlyList<string> kept = ModifiersOf(step.ModifiersAfter, plain);
            if (kept == null || !kept.SequenceEqual(reference, StringComparer.Ordinal))
                return "modifiers:" + plain + ":changed:" +
                    (kept == null ? "unread" : string.Join(";", kept.ToArray()));
            IReadOnlyList<string> before = ModifiersOf(step.ModifiersBefore, enhanced);
            IReadOnlyList<string> after = ModifiersOf(step.ModifiersAfter, enhanced);
            if (before == null || after == null) return "modifiers:" + enhanced + ":unread";
            if (before.Count != 0)
                return "modifiers:" + enhanced + ":present-before:" + string.Join(";", before.ToArray());
            if (enhancement.Kind == CastingQualificationEnhancement.DurationKind)
            {
                // The same strength, for the given factor of the plain
                // casting's duration (5% and two rounds of slack for the
                // frames between a cast and its read).
                if (!after.SequenceEqual(reference, StringComparer.Ordinal))
                    return "modifiers:" + enhanced + ":" + string.Join(";", after.ToArray()) + "!=" +
                        string.Join(";", reference.ToArray());
                double? plainRemaining;
                double? enhancedRemaining;
                plainStep.RemainingSecondsAfter.TryGetValue(plain, out plainRemaining);
                step.RemainingSecondsAfter.TryGetValue(enhanced, out enhancedRemaining);
                if (plainRemaining == null || enhancedRemaining == null || plainRemaining.Value <= 0)
                    return "duration:unread";
                double wanted = enhancement.DurationFactor * plainRemaining.Value;
                if (Math.Abs(enhancedRemaining.Value - wanted) > 0.05 * wanted + 12)
                    return "duration:" + enhanced + ":" + Seconds(enhancedRemaining.Value) + "!=" +
                        Seconds(wanted);
                return null;
            }
            List<string> expected = Raised(reference, enhancement);
            if (expected == null || !after.SequenceEqual(expected, StringComparer.Ordinal))
                return "modifiers:" + enhanced + ":" + string.Join(";", after.ToArray()) + "!=" +
                    (expected == null ? "none" : string.Join(";", expected.ToArray()));
            return null;
        }

        // The route that ran is the one forecast (review of the enhanced
        // recipe): in Instant mode a provider-direct enhancement (or Share)
        // through the provider's own transaction; a native-command one, and
        // every cast in Animated mode, through the game's own command. A rod
        // works through the game's rule events: in Instant mode a plain rule
        // cast (which reports its strategy) takes it.
        private string RouteFailure(CastingQualificationStepResult step, string castingId)
        {
            CastStep forecastStep = ReservedStep(step.Name, castingId, false);
            CastingOutcomeEntry entry = step.Report.Entries.FirstOrDefault(value =>
                string.Equals(value.CastingId, castingId, StringComparison.Ordinal));
            string route = ExecutionMode == "instant" && forecastStep != null &&
                forecastStep.ExecutionStrategy == CastExecutionStrategy.ProviderDirectRuleCast
                    ? ";provider-direct:True;"
                    : ExecutionMode == "instant" && forecastStep != null &&
                        forecastStep.ExecutionStrategy == CastExecutionStrategy.DirectRuleCast
                        ? ";strategy:DirectRuleCast;" : "native-command-spend-completed";
            if (entry == null || entry.Detail == null || !entry.Detail.Contains(route))
                return "route:" + route.Trim(';') + ":" + (entry == null ? "none" : entry.Detail);
            return null;
        }

        // The caster reads of one step: both taken; the enhancement's own
        // resource down by exactly the given spend; every activatable ability
        // of the caster in exactly its state before the step.
        private static string CasterFailure(CastingQualificationStepResult step, int spend)
        {
            return CasterDifference(null, step.CasterBefore, step.CasterAfter, spend);
        }

        // Two caster reads (labelled for a cross-step comparison): both
        // taken; the resource down by exactly the spend; every activatable
        // ability in the same state.
        internal static string CasterDifference(string label, CasterEnhancementObservation before,
            CasterEnhancementObservation after, int spend)
        {
            string prefix = label == null ? string.Empty : label + ":";
            if (before == null || after == null || !before.Succeeded || !after.Succeeded)
                return prefix + "caster:unread:" + (before == null ? "none" : before.Describe()) + ">" +
                    (after == null ? "none" : after.Describe());
            if (before.Resource.Value - after.Resource.Value != spend)
                return prefix + "enhancement-resource:" + before.Resource.Value + ">" + after.Resource.Value +
                    ":expected-spend=" + spend;
            List<string> changed = before.Activatables.Keys.Union(after.Activatables.Keys, StringComparer.Ordinal)
                .Where(key =>
                {
                    bool prior;
                    bool next;
                    return !before.Activatables.TryGetValue(key, out prior) ||
                        !after.Activatables.TryGetValue(key, out next) || prior != next;
                })
                .OrderBy(key => key, StringComparer.Ordinal).ToList();
            if (changed.Count != 0)
                return prefix + "cleanup:activatables-changed:" + string.Join(",", changed.ToArray());
            return null;
        }

        // The shortage step (E19): the plan holds one more shared casting
        // than the shared spell's native source can fund, so ordinary Apply
        // refuses the WHOLE routine - only shortage castings blocked, for
        // want of a resource - with nothing submitted, the shared source's
        // native availability, the effects, the reservoir and every Share /
        // Powerful Change toggle exactly as before.
        private static string ShortageFailure(CastingQualificationStepResult step,
            IReadOnlyList<PlannedCasting> castings)
        {
            if (step.ApplyAllowed || step.Report != null) return "not-refused:" + step.ApplyReason;
            string reason = step.ApplyReason ?? string.Empty;
            List<string> blocked = reason.StartsWith("apply-refused:", StringComparison.Ordinal)
                ? reason.Substring("apply-refused:".Length).Split(',').ToList() : new List<string>();
            if (blocked.Count == 0 || blocked.Any(value =>
                    !value.StartsWith("blocked-casting:" + ShortageCastingPrefix, StringComparison.Ordinal) ||
                    (value.IndexOf(":resource-pool-exhausted:", StringComparison.Ordinal) < 0 &&
                     value.IndexOf(":enhancement-pool-exhausted:", StringComparison.Ordinal) < 0)))
                return "refused-for-another-reason:" + reason;
            if (step.Availability.Count == 0) return "resource:unread";
            foreach (string availability in step.Availability)
            {
                string[] parts = availability.Split(new[] { ":" }, 2, StringSplitOptions.None);
                string[] values = parts.Length == 2
                    ? parts[1].Split(new[] { ">" }, StringSplitOptions.None) : new string[0];
                if (values.Length != 2 || values[0] != values[1] || values[0] == "?")
                    return "resource:" + availability;
            }
            if (castings.Any(casting => step.TransitionOf(casting.CastingId) != "unchanged" &&
                    step.TransitionOf(casting.CastingId) != "unobserved"))
                return "effects:" + string.Join(",", step.Transitions.ToArray());
            return CasterFailure(step, 0);
        }

        private static IReadOnlyList<string> ModifiersOf(IDictionary<string, IReadOnlyList<string>> modifiers,
            string castingId)
        {
            IReadOnlyList<string> value;
            return modifiers != null && modifiers.TryGetValue(castingId, out value) ? value : null;
        }

        // The value of the one modifier the enhancement raises.
        private static bool TryModifierValue(IEnumerable<string> modifiers,
            CastingQualificationEnhancement enhancement, out int value)
        {
            value = 0;
            List<string> matching = modifiers.Where(entry =>
                entry.StartsWith(enhancement.ModifierPrefix, StringComparison.Ordinal)).ToList();
            return matching.Count == 1 && int.TryParse(matching[0].Substring(enhancement.ModifierPrefix.Length),
                System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        // The plain casting's modifiers with the enhancement's one raised by
        // its increase (sorted like a read); null without that modifier.
        private static List<string> Raised(IEnumerable<string> reference, CastingQualificationEnhancement enhancement)
        {
            int value;
            List<string> modifiers = reference.ToList();
            if (!TryModifierValue(modifiers, enhancement, out value)) return null;
            List<string> raised = modifiers.Select(entry =>
                    entry.StartsWith(enhancement.ModifierPrefix, StringComparison.Ordinal)
                        ? enhancement.ModifierPrefix + (value + enhancement.Increase).ToString(
                            System.Globalization.CultureInfo.InvariantCulture)
                        : entry)
                .ToList();
            raised.Sort(StringComparer.Ordinal);
            return raised;
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
        // The enhanced recipe's caster read (caster id, the enhancement's
        // usage pool id); null where no recipe needs it.
        private readonly Func<string, string, CasterEnhancementObservation> _observeCaster;
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
        private readonly Func<string> _guardedReload;
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
            Func<CastStep, string, string, ProbeObservation> observeRecipient = null,
            Func<string, string, CasterEnhancementObservation> observeCaster = null,
            Func<string> guardedReload = null)
        {
            _guardedReload = guardedReload;
            _observeRecipient = observeRecipient;
            _observeCaster = observeCaster;
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
                string where = exception.StackTrace == null ? string.Empty
                    : " @at " + exception.StackTrace.Split(Environment.NewLine.ToCharArray())[0];
                Fail("exception:" + exception.GetType().Name + ":" + exception.Message + where);
            }
        }

        private void Advance()
        {
            switch (_phase)
            {
                case "select": Select(); return;
                case "author": Author(); return;
                case "stop": Begin(CastingQualificationForecast.Stop); return;
                case "stop-wait": Wait(true, Recipe == CastingQualificationRecipe.StopReload ? "reload" : "complete"); return;
                case "reload": Reload(); return;
                case "complete": Begin(CastingQualificationForecast.Complete); return;
                case "complete-wait": Wait(false, Recipe == CastingQualificationRecipe.StopReload ? "done" : "repeat"); return;
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
                case "mixed-wait": Wait(false, "done"); return;
                case "use": Begin(CastingQualificationForecast.Use); return;
                case "use-wait": Wait(false, "repeat"); return;
                case "exhausted-edit": ExhaustedEdit(); return;
                case "exhausted": Exhausted(); return;
                case "plain": Begin(CastingQualificationForecast.Plain); return;
                case "plain-wait": Wait(false, "enhance-author"); return;
                case "enhance-author": EnhanceAuthor(); return;
                case "enhanced": Begin(CastingQualificationForecast.Enhanced); return;
                case "enhanced-wait": Wait(false, "done"); return;
                case "shared": Begin(CastingQualificationForecast.Shared); return;
                case "shared-wait": Wait(false, "witness-author"); return;
                case "witness-author": WitnessAuthor(); return;
                case "witness": Begin(CastingQualificationForecast.Witness); return;
                case "witness-wait": Wait(false, "shortage-author"); return;
                case "shortage-author": ShortageAuthor(); return;
                case "shortage": Shortage(); return;
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
            if (CastingQualificationRecipe.IsSharedRecipe(Recipe))
            {
                // The caster's native state before anything is authored: a
                // preview (the selection run) must leave it exactly so.
                Record.PreviewCasterBefore = ObserveShareCaster(null, "preview-before");
                // The shared recipe's forecast must bind the GRAPH-AUTHORED
                // production identities, so authoring happens first; the
                // allowance's projection check follows the authored
                // forecast in AuthorShared. The boundary (and the mode and
                // budget it pins) is created here so the opened session
                // submits through it.
                if (Record.CastingScenario)
                {
                    if (_allowance == null) { Fail("allowance-missing:" + Record.AllowanceStatus); return; }
                    if (!string.Equals(_allowance.FixtureGameId, _campaignId,
                            System.StringComparison.Ordinal))
                    {
                        Record.AllowanceStatus = "fixture-mismatch";
                        Fail("allowance-fixture-mismatch");
                        return;
                    }
                    Record.AllowanceStatus = "valid";
                    Record.ExecutionMode = _allowance.ExecutionMode;
                    Record.MaximumSubmissions = _allowance.MaximumNativeSubmissions;
                    _boundary = new CastingQualificationBoundary(_allowance, _host,
                        () => _session == null ? null : _session.ExecutionSettings);
                }
                _phase = "author";
                return;
            }
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
            if (CastingQualificationRecipe.IsSharedRecipe(Recipe)) { AuthorShared(); return; }
            // The two-phase recipes cast their first casting alone (the
            // group recipe's direct casting, the enhanced recipe's plain
            // one); the rest join after that step.
            bool group = CastingQualificationRecipe.IsGroupRecipe(Recipe);
            bool twoPhase = CastingQualificationRecipe.IsTwoPhase(Recipe);
            foreach (PlannedCasting casting in twoPhase
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
            _phase = group ? "prime" : twoPhase ? "plain"
                : Recipe == CastingQualificationRecipe.AbilityPoolDirect ? "use" : "stop";
        }

        // The shared recipe (v1.2 E18): the SHARED casting is authored
        // through the PRODUCTION graph commands - buff, caster, exact
        // source, Share armed on the next casting, then the ally click -
        // and the forecast is built from the AUTHORED document, so the
        // projections the allowance binds (and the casting run must
        // reproduce) carry the graph-assigned identities. The selection
        // run authors, records and cleans up after itself; the casting
        // run additionally validates the allowance against the authored
        // forecast before anything is submitted.
        private void AuthorShared()
        {
            CastingWorkspaceInputs inputs = _freshInputs();
            // Leftovers of an earlier run (a failed casting run may have
            // persisted its authored plan) are repaired through the
            // session's own removal gesture, never file surgery.
            foreach (PlannedCasting leftover in _session.Document.Castings.ToList())
            {
                _session.FocusCasting(leftover.CastingId);
                if (!_session.RemoveFocusedCasting().Applied)
                { Fail("leftover-removal-refused:" + leftover.CastingId); return; }
            }
            PlannedCasting authored = GraphAuthor(inputs, Record.Selection.Castings[0], true);
            if (authored == null) return;
            Record.Selection = new CastingQualificationSelection(null,
                Record.Selection.SourceId, Record.Selection.Ability,
                new[] { authored, Record.Selection.Castings[1] }.ToList(),
                Record.Selection.CandidatesConsidered,
                Record.Selection.Rejections.ToList(),
                Record.Selection.Recipe, Record.Selection.Coverage,
                Record.Selection.Enhancement);
            inputs = _freshInputs();
            Record.Forecast = CastingQualificationForecast.Forecast(Record.Selection,
                inputs, _campaignId);
            // The exact reservoir demand the approved shared projection
            // carries (the verified Share units, plus Powerful Change's
            // when combined) - what the observed native delta must equal.
            Record.ShareExpectedSpend = Record.Forecast
                .Where(step => step.Name == CastingQualificationForecast.Shared)
                .Select(step => step.Projection == null ? null : (int?)step.Projection.Plan
                    .Steps.SelectMany(value => value.EnhancementUsageByPool)
                    .Where(pair => pair.Key == Record.Selection.Coverage.FirstOrDefault(
                        value2 => value2.StartsWith("reservoir:",
                            System.StringComparison.Ordinal))?.Substring("reservoir:".Length))
                    .Sum(pair => pair.Value))
                .FirstOrDefault();
            Record.ShareIndependentDemand = IndependentShareDemand();
            // E16/E17: the per-casting Share intent is in the STORED plan
            // (a fresh read, as a restart loads it), not only in memory.
            string persistStatus;
            PlannedCasting stored = _session.PersistedCastingForRuntime(authored.CastingId, out persistStatus);
            Record.SharePersisted = stored != null && SameSharedIntent(stored, authored);
            Record.SharePersistedIntent = "load=" + persistStatus + ";" + DescribeSharedIntent(stored);
            if (!Record.CastingScenario)
            {
                // Selection-only run: nothing was submitted; the authored
                // plan is removed so the casting run authors from clean,
                // and the caster is read again (the preview armed and spent
                // nothing).
                _session.FocusCasting(authored.CastingId);
                if (!_session.RemoveFocusedCasting().Applied)
                { Fail("select-cleanup-refused:" + authored.CastingId); return; }
                Record.PreviewCasterAfter = ObserveShareCaster(null, "preview-after");
                Finish("completed");
                return;
            }
            if (!Record.Forecast.Select(step => step.ProjectionId)
                    .SequenceEqual(_allowance.ApprovedProjectionIds, System.StringComparer.Ordinal))
            {
                Record.AllowanceStatus = "projections-differ-from-forecast";
                Fail("allowance-projections-differ-from-forecast");
                return;
            }
            _session.SetExecutionMode(_allowance.ExecutionMode);
            _session.Save();
            inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("accept-refused"); return; }
            _startedMillis = _clock();
            _phase = "shared";
        }

        // The witness phase. First (E17) Share is armed and then disarmed on
        // the NEXT-casting draft: a draft setting changes future castings
        // only, so the authored shared casting must keep its own Share
        // intent exactly. Then the plain witness casting by the SAME caster
        // joins with the exact scripted identity the forecast - and so the
        // allowance - bound (a graph-assigned id would differ from the
        // approved projection and be refused at the boundary).
        private void WitnessAuthor()
        {
            CastingWorkspaceInputs inputs = _freshInputs();
            string sharedId = Record.Selection.Castings[0].CastingId;
            Func<PlannedCasting> sharedNow = () => _session.Document.Castings.FirstOrDefault(value =>
                value != null && string.Equals(value.CastingId, sharedId, StringComparison.Ordinal));
            Func<bool> draftArmed = () => _session.Draft.TargetingModifiers.Any(value =>
                value != null && value.Enabled && string.Equals(value.ModifierId,
                    GameAdapters.ShareCastingModifier.Id, StringComparison.Ordinal));
            PlannedCasting before = sharedNow();
            bool kept = before != null;
            for (int pass = 0; pass < 2 && kept; pass++)
            {
                if (pass == 0 && draftArmed()) continue;
                AuthoringEditResult toggled = _session.ToggleDraftTargetingModifier(
                    GameAdapters.ShareCastingModifier.Id, inputs);
                if (!toggled.Applied) { Fail("share-draft-toggle-refused:" + toggled.Reason); return; }
                PlannedCasting now = sharedNow();
                kept = now != null && SameSharedIntent(now, before);
            }
            Record.ShareDraftDisarmKeptCasting = kept && !draftArmed();
            // The shared casting is disabled through the inspector's own
            // command (kept, visible, not cast) - as the forecast's witness
            // phase models it.
            _session.FocusCasting(sharedId);
            AuthoringEditResult disabled = _session.SetFocusedCastingState(CastingAuthoringState.Disabled);
            if (!disabled.Applied) { Fail("shared-disable-refused:" + disabled.Reason); return; }
            _session.ClearGraphFocus();
            PlannedCasting witness = Record.Selection.Castings[1];
            AuthoringEditResult added = _session.AddCastingForRuntime(witness);
            if (!added.Applied) { Fail("author-refused:" + witness.CastingId + ":" + added.Reason); return; }
            _session.Save();
            inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("witness-accept-refused"); return; }
            _phase = "witness";
        }

        // The most shared castings the shortage step may author (a source
        // with more casts left than this cannot be exhausted here).
        public const int MaximumShortageCastings = 12;

        // The shortage phase (E19): after the witness, add one more shared
        // casting (Always recast, same caster, source, ally, Share and
        // enhancements) than the shared spell's native source can fund now
        // - or, for a free source, than the reservoir can fund - so the
        // complete cost cannot be met.
        private void ShortageAuthor()
        {
            CastingWorkspaceInputs inputs = _freshInputs();
            PlannedCasting shared = Record.Selection.Castings[0];
            ProviderPlanningOption option = inputs.ProviderOptions.FirstOrDefault(value =>
                value != null && value.Provider != null &&
                string.Equals(value.Provider.Key.CasterUnitId, shared.CasterUnitId, StringComparison.Ordinal) &&
                string.Equals(value.Provider.Key.Ability.Canonical, shared.Ability.Canonical, StringComparison.Ordinal) &&
                string.Equals(value.Provider.Key.SpellbookGuid ?? string.Empty, shared.SpellbookGuid ?? string.Empty,
                    StringComparison.Ordinal));
            if (option == null) { Fail("shortage-source-missing"); return; }
            Domain.Providers.ResourcePoolSnapshot pool = inputs.Snapshot.ResourcePools.FirstOrDefault(value =>
                string.Equals(value.PoolKey, option.Provider.ResourcePoolKey, StringComparison.Ordinal));
            int needed;
            if (pool != null && pool.Kind != Domain.Providers.ResourcePoolKind.Unlimited)
                needed = CastingQualificationRecipe.CastsAvailable(inputs, option.Provider,
                    MaximumShortageCastings) + 1;
            else
            {
                CasterEnhancementObservation reservoir = ObserveShareCaster(null, "shortage-plan");
                int perCast = Record.ShareIndependentDemand ?? 0;
                needed = reservoir.Succeeded && perCast > 0
                    ? reservoir.Resource.Value / perCast + 1 : int.MaxValue;
            }
            if (needed > MaximumShortageCastings) { Fail("shortage-unconstructible:" + needed); return; }
            for (int index = 1; index <= needed; index++)
            {
                var casting = new PlannedCasting(
                    CastingQualificationRecord.ShortageCastingPrefix + index, CastingQualificationRecipe.RoutineId,
                    10 + index, shared.SourceId, shared.Ability, shared.CasterUnitId, shared.SpellbookGuid,
                    CastingTargetMode.DirectTarget, shared.DirectTargetUnitId, null, null,
                    shared.TargetingModifiers, shared.Enhancements, ExistingEffectPolicy.Overwrite, null,
                    CastingAuthoringState.Ready, null);
                AuthoringEditResult added = _session.AddCastingForRuntime(casting);
                if (!added.Applied) { Fail("shortage-author-refused:" + casting.CastingId + ":" + added.Reason); return; }
            }
            Record.ShareShortageCastings = needed;
            _session.Save();
            inputs = _freshInputs();
            // Recorded like any edit; the blocked plan is refused either way.
            _session.PresentForReview(inputs);
            _session.AcceptPresentedPlan(inputs);
            _phase = "shortage";
        }

        // The shortage step: its reads, one ordinary Apply that must be
        // refused whole, and the same reads again (nothing may change).
        private void Shortage()
        {
            if (WorldHeld()) return;
            var step = new CastingQualificationStepResult(CastingQualificationForecast.Shortage);
            Record.Steps.Add(step);
            _observeSteps = StepsToObserve(CastingQualificationForecast.Shortage);
            _before = ObserveAll(step, "shortage-before");
            step.CasterBefore = ObserveShareCaster(step, "shortage-before");
            WorkspaceApplyResult result = _session.Apply(CastingApplyMode.Ordinary,
                CastingQualificationRecipe.RoutineId, _freshInputs());
            step.ApplyAllowed = result.Allowed;
            step.ApplyReason = result.Allowed && result.Dispatch != null
                ? result.Dispatch.Reason : result.ReviewReason;
            if (result.Allowed && result.Dispatch != null && result.Dispatch.Submitted)
            {
                _host.Cancel("qualification-unexpected-run");
                Fail("shortage-submitted:" + step.ApplyReason);
                return;
            }
            ObserveTransitions(step, "shortage-after");
            string failure = Record.StepFailure(CastingQualificationForecast.Shortage);
            if (failure != null) { Fail("step:" + failure); return; }
            _phase = "done";
        }

        // The demand the shared step must charge, derived independently of
        // the projection from the selection's verified snapshots: Share's
        // units, plus Powerful Change's when it draws the same reservoir.
        private int? IndependentShareDemand()
        {
            if (Record.Selection == null) return null;
            string units = Record.Selection.Coverage.FirstOrDefault(value =>
                value.StartsWith("units:", StringComparison.Ordinal));
            string reservoir = Record.Selection.Coverage.FirstOrDefault(value =>
                value.StartsWith("reservoir:", StringComparison.Ordinal));
            int share;
            if (units == null || reservoir == null ||
                !int.TryParse(units.Substring("units:".Length), out share))
                return null;
            CastingQualificationEnhancement enhancement = Record.Selection.Enhancement;
            int powerful = enhancement != null && string.Equals(enhancement.UsagePoolId,
                reservoir.Substring("reservoir:".Length), StringComparison.Ordinal)
                ? enhancement.UnitsPerCast : 0;
            return share + powerful;
        }

        // The same authored shared intent: caster, exact source, ally, the
        // one enabled Share selection and the same required enhancements.
        internal static bool SameSharedIntent(PlannedCasting value, PlannedCasting authored)
        {
            if (value == null || authored == null) return false;
            Func<PlannedCasting, string> enhancements = casting => string.Join(",",
                casting.Enhancements.Where(selection => selection != null)
                    .Select(selection => selection.EnhancementId + (selection.Required ? "!" : string.Empty))
                    .OrderBy(id => id, StringComparer.Ordinal).ToArray());
            return string.Equals(value.CastingId, authored.CastingId, StringComparison.Ordinal) &&
                string.Equals(value.CasterUnitId, authored.CasterUnitId, StringComparison.Ordinal) &&
                string.Equals(value.Ability.Canonical, authored.Ability.Canonical, StringComparison.Ordinal) &&
                string.Equals(value.SpellbookGuid ?? string.Empty, authored.SpellbookGuid ?? string.Empty,
                    StringComparison.Ordinal) &&
                string.Equals(value.DirectTargetUnitId, authored.DirectTargetUnitId, StringComparison.Ordinal) &&
                value.TargetingModifiers.Count == 1 && value.TargetingModifiers[0].Enabled &&
                string.Equals(value.TargetingModifiers[0].ModifierId, GameAdapters.ShareCastingModifier.Id,
                    StringComparison.Ordinal) &&
                enhancements(value) == enhancements(authored);
        }

        internal static string DescribeSharedIntent(PlannedCasting value)
        {
            if (value == null) return "casting=absent";
            return "casting=" + value.CastingId + ";caster=" + value.CasterUnitId +
                ";ability=" + value.Ability.Canonical + ";book=" + (value.SpellbookGuid ?? string.Empty) +
                ";target=" + value.DirectTargetUnitId + ";modifiers=" + string.Join(",",
                    value.TargetingModifiers.Select(selection => selection.ModifierId + "=" +
                        (selection.Enabled ? "on" : "off")).ToArray()) +
                ";enhancements=" + string.Join(",", value.Enhancements.Where(selection => selection != null)
                    .Select(selection => selection.EnhancementId).ToArray());
        }

        // The shared recipes' caster read: the selected caster's reservoir
        // (the Share snapshot's usage pool) and every activatable ability.
        private CasterEnhancementObservation ObserveShareCaster(CastingQualificationStepResult step,
            string label)
        {
            string caster = Record.Selection == null || Record.Selection.Castings.Count == 0 ? null
                : Record.Selection.Castings[0].CasterUnitId;
            string reservoir = Record.Selection == null ? null : Record.Selection.Coverage
                .Where(value => value.StartsWith("reservoir:", StringComparison.Ordinal))
                .Select(value => value.Substring("reservoir:".Length)).FirstOrDefault();
            CasterEnhancementObservation observation;
            if (_observeCaster == null || caster == null || reservoir == null)
                observation = CasterEnhancementObservation.Failed("caster-observer-missing");
            else
            {
                try
                {
                    observation = _observeCaster(caster, reservoir) ??
                        CasterEnhancementObservation.Failed("caster-observer-null");
                }
                catch (Exception exception)
                {
                    observation = CasterEnhancementObservation.Failed("observer-exception:" +
                        exception.GetType().Name);
                }
            }
            if (step != null) step.Observations.Add("caster:" + label + ":" + observation.Describe());
            return observation;
        }

        // Authors one casting through the production graph gesture sequence
        // (buff -> caster -> exact source -> [Share on the draft] -> target
        // click) and returns the AUTHORED record after verifying it carries
        // the scripted intent. Null (with the run failed) on any refusal.
        private PlannedCasting GraphAuthor(CastingWorkspaceInputs inputs,
            PlannedCasting script, bool withShare)
        {
            _session.SelectGraphBuff(script.SourceId, inputs);
            _session.SelectGraphCaster(script.CasterUnitId, inputs);
            string canonical = inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null &&
                    string.Equals(value.Provider.Key.CasterUnitId, script.CasterUnitId,
                        System.StringComparison.Ordinal) &&
                    string.Equals(value.Provider.Key.Ability.Canonical,
                        script.Ability.Canonical, System.StringComparison.Ordinal) &&
                    string.Equals(value.Provider.Key.SpellbookGuid ?? string.Empty,
                        script.SpellbookGuid ?? string.Empty, System.StringComparison.Ordinal))
                .OrderBy(value => value.Provider.Key.Canonical, System.StringComparer.Ordinal)
                .First().Provider.Key.Canonical;
            UI.CastingGraphView view = _session.BuildGraph(inputs);
            UI.CastingGraphCasterNode node = view.CasterById(script.CasterUnitId);
            UI.CastingGraphSourceRow row = node == null ? null
                : node.Sources.FirstOrDefault(value =>
                    string.Equals(value.ProviderKey, canonical, System.StringComparison.Ordinal));
            if (row == null) { Fail("graph-source-row-missing:" + canonical); return null; }
            _session.SelectGraphSource(row.ProviderKey, inputs);
            // The combined variant arms Share on the draft exactly as the
            // alone variant; Powerful Change is chosen on the FOCUSED
            // casting through the workspace's own enhancement control
            // after the graph add (below).
            bool shareArmed = _session.Draft.TargetingModifiers.Any(value =>
                value != null && value.Enabled &&
                string.Equals(value.ModifierId, GameAdapters.ShareCastingModifier.Id,
                    System.StringComparison.Ordinal));
            if (withShare && !shareArmed)
            {
                AuthoringEditResult share = _session.ToggleDraftTargetingModifier(
                    GameAdapters.ShareCastingModifier.Id, inputs);
                if (!share.Applied) { Fail("share-arm-refused:" + share.Reason); return null; }
            }
            if (!withShare && shareArmed)
            {
                AuthoringEditResult off = _session.ToggleDraftTargetingModifier(
                    GameAdapters.ShareCastingModifier.Id, inputs);
                if (!off.Applied) { Fail("share-disarm-refused:" + off.Reason); return null; }
            }
            UI.CastingGraphEditResult added = _session.AddGraphCasting(
                script.DirectTargetUnitId, inputs);
            if (!added.Applied)
            {
                Fail("graph-add-refused:" + script.DirectTargetUnitId + ":" +
                    (added.Edit == null ? "null" : added.Edit.Reason));
                return null;
            }
            if (withShare && script.Enhancements.Count != 0)
            {
                // The combined case: the applicable enhancement is chosen
                // through the focused casting's own production control.
                _session.FocusCasting(added.CastingId);
                AuthoringEditResult enhanced = _session.ToggleFocusedEnhancement(
                    script.Enhancements[0].EnhancementId, inputs);
                if (!enhanced.Applied)
                { Fail("combined-enhancement-refused:" + script.Enhancements[0].EnhancementId +
                        ":" + enhanced.Reason); return null; }
            }
            PlannedCasting authored = _session.Document.Castings.FirstOrDefault(value =>
                string.Equals(value.CastingId, added.CastingId, System.StringComparison.Ordinal));
            if (authored == null ||
                !string.Equals(authored.DirectTargetUnitId, script.DirectTargetUnitId,
                    System.StringComparison.Ordinal) ||
                !string.Equals(authored.CasterUnitId, script.CasterUnitId,
                    System.StringComparison.Ordinal) ||
                !string.Equals(authored.Ability.Canonical, script.Ability.Canonical,
                    System.StringComparison.Ordinal) ||
                (withShare
                    ? authored.TargetingModifiers.Count != 1 ||
                        !string.Equals(authored.TargetingModifiers[0].ModifierId,
                            GameAdapters.ShareCastingModifier.Id, System.StringComparison.Ordinal) ||
                        !authored.TargetingModifiers[0].Enabled
                    : authored.TargetingModifiers.Count != 0) ||
                !script.Enhancements.All(selection => authored.Enhancements.Any(value =>
                    string.Equals(value.EnhancementId, selection.EnhancementId,
                        System.StringComparison.Ordinal) && value.Required)))
            {
                Fail("authored-intent-mismatch:" + added.CastingId);
                return null;
            }
            return authored;
        }

        // Ability-pool recipe: the casting is set to Always recast through
        // the session's own recast command (the inspector's control), then
        // reviewed and accepted like any edit (the acceptance is recorded,
        // not required: the plan is blocked for want of the resource).
        private void ExhaustedEdit()
        {
            string castingId = Record.Selection.Castings[0].CastingId;
            _session.FocusCasting(castingId);
            AuthoringEditResult updated = _session.SetFocusedRecastPolicy(ExistingEffectPolicy.Overwrite);
            if (!updated.Applied) { Fail("exhausted-edit-refused:" + updated.Reason); return; }
            _session.Save();
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.PresentForReview(inputs);
            Record.ExhaustedAccepted = _session.AcceptPresentedPlan(inputs);
            _phase = "exhausted";
        }

        // The exhausted step: its reads, one Apply that must be refused, and
        // the same reads again (nothing may have changed).
        private void Exhausted()
        {
            if (WorldHeld()) return;
            var step = new CastingQualificationStepResult(CastingQualificationForecast.Exhausted);
            Record.Steps.Add(step);
            _observeSteps = StepsToObserve(CastingQualificationForecast.Exhausted);
            _before = ObserveAll(step, "exhausted-before");
            WorkspaceApplyResult result = _session.Apply(CastingApplyMode.Ordinary,
                CastingQualificationRecipe.RoutineId, _freshInputs());
            step.ApplyAllowed = result.Allowed;
            step.ApplyReason = result.Allowed && result.Dispatch != null
                ? result.Dispatch.Reason : result.ReviewReason;
            if (result.Allowed && result.Dispatch != null && result.Dispatch.Submitted)
            {
                _host.Cancel("qualification-unexpected-run");
                Fail("exhausted-submitted:" + step.ApplyReason);
                return;
            }
            ObserveTransitions(step, "exhausted-after");
            string failure = Record.StepFailure(CastingQualificationForecast.Exhausted);
            if (failure != null) { Fail("step:" + failure); return; }
            _phase = "done";
        }

        // Enhanced recipe: after the plain step the enhanced casting joins
        // the plan WITHOUT its enhancement; the enhancement is then chosen on
        // it through the workspace's own option for the focused casting (the
        // options the inspector draws, and the session command its
        // enhancement chip calls), and the changed plan is reviewed and
        // accepted like any edit.
        private void EnhanceAuthor()
        {
            PlannedCasting planned = Record.Selection.Castings[1];
            string enhancementId = Record.Selection.Enhancement.EnhancementId;
            AuthoringEditResult added = _session.AddCastingForRuntime(
                planned.WithEnhancementSelections(new AuthoredEnhancementSelection[0]));
            if (!added.Applied) { Fail("author-refused:" + planned.CastingId + ":" + added.Reason); return; }
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.FocusCasting(planned.CastingId);
            List<string> offered = _session.BuildView(inputs).FocusedEnhancements
                .Select(value => value.EnhancementId + (value.Selected ? "*" : string.Empty)).ToList();
            if (!offered.Contains(enhancementId))
            {
                Record.EnhancementOptions = offered.Select(value => "before:" + value).ToList();
                Fail("enhancement-not-offered:" + enhancementId);
                return;
            }
            PlannedCasting focused = _session.Document.Castings.First(value =>
                string.Equals(value.CastingId, planned.CastingId, StringComparison.Ordinal));
            AuthoringEditResult edited = _session.UpdateFocusedCasting(focused.WithEnhancementSelections(
                focused.Enhancements.Where(value => value != null).Concat(new[]
                    { new AuthoredEnhancementSelection(enhancementId, true, null) })));
            List<string> selected = _session.BuildView(inputs).FocusedEnhancements
                .Select(value => value.EnhancementId + (value.Selected ? "*" : string.Empty)).ToList();
            Record.EnhancementOptions = offered.Select(value => "before:" + value)
                .Concat(selected.Select(value => "after:" + value)).ToList();
            if (!edited.Applied) { Fail("enhancement-refused:" + edited.Reason); return; }
            if (!selected.Contains(enhancementId + "*")) { Fail("enhancement-not-selected:" + enhancementId); return; }
            _session.Save();
            inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("enhanced-accept-refused"); return; }
            _phase = "enhanced";
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
            if (beforeFailure == null && CastingQualificationRecipe.IsSharedRecipe(Recipe))
            {
                // The isolation boundaries (E18-E20): the caster's reservoir
                // and every activatable ability, before this step's
                // preparation (for the witness: AFTER the shared cast's
                // cleanup, BEFORE the witness's own preparation).
                step.CasterBefore = ObserveShareCaster(step, name + "-before");
                if (!step.CasterBefore.Succeeded) beforeFailure = "caster:" + step.CasterBefore.Failure;
            }
            if (beforeFailure == null && CastingQualificationRecipe.IsEnhancedRecipe(Recipe))
            {
                // The enhanced recipe also needs the caster read and every
                // effect instance's modifiers before anything is submitted.
                step.CasterBefore = ObserveCaster(step, name + "-before");
                beforeFailure = step.CasterBefore.Succeeded
                    ? _before.Where(pair => ModifiersOf(pair.Value) == null)
                        .Select(pair => pair.Key + ":modifiers-unread").FirstOrDefault()
                    : "caster:" + step.CasterBefore.Failure;
            }
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
        private void Reload()
        {
            if (_guardedReload == null) { Fail("guarded-reload-unavailable"); return; }
            string evidence = _guardedReload();
            if (evidence == null) return;
            Record.RecoveryReload = evidence;
            if (!evidence.StartsWith("passed=True;", StringComparison.Ordinal))
            { Fail("guarded-reload:" + evidence); return; }
            if (!_host.Accepting || _host.IsRunning)
            { Fail("host-after-reload:accepting=" + _host.Accepting + ";shutdown=" + _host.ShutdownReason); return; }
            _session.FocusGraphCasting(Record.Selection.Castings[0].CastingId);
            AuthoringEditResult edit = _session.SetFocusedCastingState(CastingAuthoringState.Disabled);
            if (!edit.Applied) { Fail("second-author-refused:" + edit.Reason); return; }
            CastingWorkspaceInputs inputs = _freshInputs();
            _session.PresentForReview(inputs);
            if (!_session.AcceptPresentedPlan(inputs)) { Fail("second-accept-refused"); return; }
            _phase = "complete";
        }

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
            // For the shared recipes this also takes the caster read: the
            // run is no longer active - its cleanup has run - and nothing
            // else is prepared yet (the authoritative post-shared boundary).
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
            _phase = Recipe == CastingQualificationRecipe.AbilityPoolDirect ? "exhausted-edit" : "recast-edit";
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
            bool shared = CastingQualificationRecipe.IsSharedRecipe(Recipe);
            if (!CastingQualificationRecipe.IsEnhancedRecipe(Recipe) && !shared) return;
            step.CasterAfter = shared ? ObserveShareCaster(step, label) : ObserveCaster(step, label);
            foreach (KeyValuePair<string, ProbeObservation> pair in after)
            {
                ProbeObservation before;
                _before.TryGetValue(pair.Key, out before);
                step.ModifiersBefore[pair.Key] = ModifiersOf(before);
                step.ModifiersAfter[pair.Key] = ModifiersOf(pair.Value);
                step.RemainingSecondsAfter[pair.Key] = RemainingSeconds(pair.Value);
            }
        }

        // The longest time left on the observed instances at the read, in
        // seconds; null without the game clock or any instance.
        internal static double? RemainingSeconds(ProbeObservation observation)
        {
            if (observation == null || !observation.Succeeded || observation.GameTimeTicks == null ||
                observation.EffectInstances == null || observation.EffectInstances.Count == 0)
                return null;
            long end = observation.EffectInstances.Max(instance => instance.EndTimeTicks);
            return (end - observation.GameTimeTicks.Value) / (double)TimeSpan.TicksPerSecond;
        }

        // The stat modifiers of every observed instance together (sorted);
        // null when the read failed or any instance's modifiers were unread.
        internal static IReadOnlyList<string> ModifiersOf(ProbeObservation observation)
        {
            if (observation == null || !observation.Succeeded || observation.EffectInstances == null) return null;
            var modifiers = new List<string>();
            foreach (ProbeEffectInstance instance in observation.EffectInstances)
            {
                if (instance == null || instance.Modifiers == null) return null;
                modifiers.AddRange(instance.Modifiers);
            }
            modifiers.Sort(StringComparer.Ordinal);
            return modifiers;
        }

        private CasterEnhancementObservation ObserveCaster(CastingQualificationStepResult step, string label)
        {
            CastingQualificationEnhancement enhancement = Record.Selection == null ? null
                : Record.Selection.Enhancement;
            CasterEnhancementObservation observation;
            if (_observeCaster == null || enhancement == null)
                observation = CasterEnhancementObservation.Failed("caster-observer-missing");
            else
            {
                try
                {
                    observation = _observeCaster(enhancement.CasterUnitId, enhancement.UsagePoolId) ??
                        CasterEnhancementObservation.Failed("caster-observer-null");
                }
                catch (Exception exception)
                {
                    observation = CasterEnhancementObservation.Failed("observer-exception:" +
                        exception.GetType().Name);
                }
            }
            step.Observations.Add("caster:" + label + ":" + observation.Describe());
            return observation;
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
