using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Identity;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Unity-free evidence contracts for the workspace runtime scenarios.
    // The runtime host is the only producer; the same types own the final
    // acceptance predicates, so the source-only suite exercises exactly the
    // producer/consumer pair the live scenario uses (review J1/J2).

    internal static class WorkspaceControlOutcome
    {
        internal const string Invoked = "invoked";
        internal const string AlreadyReady = "already-ready";
    }

    // One control-driven Add in the automatic interaction sequence. The
    // State control is valid either as a real click or as a draft that was
    // legitimately already Ready (review I5); correctness is carried by the
    // exact-record booleans, never by the State outcome.
    internal sealed class WorkspaceCastStepEvidence
    {
        internal WorkspaceCastStepEvidence(int ordinal, string casterControl,
            string targetControl, string addControl, string stateOutcome,
            bool grew, bool distinct, bool fieldsExact, bool siblingsUnchanged)
        {
            Ordinal = ordinal;
            CasterControl = casterControl ?? "missing";
            TargetControl = targetControl ?? "missing";
            AddControl = addControl ?? "missing";
            StateOutcome = stateOutcome ?? "missing";
            Grew = grew;
            Distinct = distinct;
            FieldsExact = fieldsExact;
            SiblingsUnchanged = siblingsUnchanged;
        }

        internal int Ordinal { get; private set; }
        internal string CasterControl { get; private set; }
        internal string TargetControl { get; private set; }
        internal string AddControl { get; private set; }
        internal string StateOutcome { get; private set; }
        internal bool Grew { get; private set; }
        internal bool Distinct { get; private set; }
        internal bool FieldsExact { get; private set; }
        internal bool SiblingsUnchanged { get; private set; }

        internal bool Exact
        {
            get { return Grew && Distinct && FieldsExact && SiblingsUnchanged; }
        }

        internal bool ControlsInvoked
        {
            get
            {
                return CasterControl == WorkspaceControlOutcome.Invoked &&
                    TargetControl == WorkspaceControlOutcome.Invoked &&
                    AddControl == WorkspaceControlOutcome.Invoked;
            }
        }

        internal bool StateAccepted
        {
            get
            {
                return StateOutcome == WorkspaceControlOutcome.Invoked ||
                    StateOutcome == WorkspaceControlOutcome.AlreadyReady;
            }
        }

        internal string Describe()
        {
            string n = Ordinal.ToString(CultureInfo.InvariantCulture);
            return "cast" + n + "=controls:" + CasterControl + "/" +
                TargetControl + "/" + AddControl +
                ";state" + n + "=" + StateOutcome +
                ";cast" + n + "Exact=" + Exact;
        }
    }

    // The exact record one control-driven Add must create, resolved from
    // the POST-selection draft (review H4).
    internal sealed class WorkspaceCastExpectation
    {
        internal string CasterUnitId { get; set; }
        internal string DirectTargetUnitId { get; set; }
        internal string SourceId { get; set; }
        internal AbilityKey Ability { get; set; }
        internal string SpellbookGuid { get; set; }
        internal string RoutineId { get; set; }
    }

    // Production evaluation of one Add: one-record growth, a new identity,
    // exact authored fields, and unchanged siblings at full serialized
    // fidelity. The runtime host calls exactly this.
    internal static class WorkspaceCastStepEvaluator
    {
        // Canonical signature of every record EXCEPT the excluded id — the
        // same serialized profile the persistence round trip uses.
        internal static string SiblingSignature(
            IEnumerable<PlannedCasting> castings, string excludeCastingId)
        {
            var parts = new List<string>();
            if (castings != null)
            {
                foreach (PlannedCasting casting in castings)
                {
                    if (casting == null || string.Equals(casting.CastingId,
                            excludeCastingId, StringComparison.Ordinal)) continue;
                    parts.Add(Newtonsoft.Json.JsonConvert.SerializeObject(
                        PlannedCastingProfile.FromDomain(casting),
                        Newtonsoft.Json.Formatting.None));
                }
            }
            return string.Join("||", parts.ToArray());
        }

        internal static bool FieldsExact(PlannedCasting created,
            WorkspaceCastExpectation expected)
        {
            return created != null && expected != null &&
                string.Equals(created.CasterUnitId, expected.CasterUnitId,
                    StringComparison.Ordinal) &&
                string.Equals(created.DirectTargetUnitId,
                    expected.DirectTargetUnitId, StringComparison.Ordinal) &&
                string.Equals(created.SourceId, expected.SourceId,
                    StringComparison.Ordinal) &&
                created.Ability != null && expected.Ability != null &&
                string.Equals(created.Ability.Canonical,
                    expected.Ability.Canonical, StringComparison.Ordinal) &&
                string.Equals(created.SpellbookGuid ?? string.Empty,
                    expected.SpellbookGuid ?? string.Empty,
                    StringComparison.Ordinal) &&
                string.Equals(created.RoutineId, expected.RoutineId,
                    StringComparison.Ordinal) &&
                created.State == CastingAuthoringState.Ready &&
                created.Enhancements.Count == 0;
        }

        internal static WorkspaceCastStepEvidence Evaluate(int ordinal,
            string casterControl, string targetControl, string stateOutcome,
            string addControl, IList<PlannedCasting> castingsAfter,
            int countBefore, IEnumerable<string> priorCastingIds,
            WorkspaceCastExpectation expected, string siblingsBefore,
            out PlannedCasting created)
        {
            int countAfter = castingsAfter == null ? 0 : castingsAfter.Count;
            bool grew = countAfter == countBefore + 1;
            created = grew ? castingsAfter[countAfter - 1] : null;
            PlannedCasting record = created;
            bool distinct = record != null && (priorCastingIds == null ||
                priorCastingIds.All(id => !string.Equals(id, record.CastingId,
                    StringComparison.Ordinal)));
            bool fieldsExact = FieldsExact(record, expected);
            bool siblingsUnchanged = string.Equals(
                SiblingSignature(castingsAfter,
                    record == null ? null : record.CastingId),
                siblingsBefore ?? string.Empty, StringComparison.Ordinal);
            return new WorkspaceCastStepEvidence(ordinal, casterControl,
                targetControl, addControl, stateOutcome, grew, distinct,
                fieldsExact, siblingsUnchanged);
        }
    }

    // The automatic interaction sequence's structured record. Describe() is
    // the human-readable evidence string; Violations() is the ONLY
    // acceptance predicate, evaluated on fields rather than on substrings
    // of the description (review J1: the old substring validator required
    // adjacency the producer never emitted).
    internal sealed class WorkspaceInteractionRecord
    {
        internal const int RequiredCastSteps = 3;

        private readonly List<WorkspaceCastStepEvidence> _castSteps =
            new List<WorkspaceCastStepEvidence>();
        private readonly List<string> _notes = new List<string>();

        internal bool BrowseRecorded { get; private set; }
        internal bool BrowseNoMutation { get; private set; }
        internal string BuffControl { get; private set; }
        internal int CapableCasters { get; private set; }
        internal int Targets { get; private set; }
        internal string RefusedAddControl { get; set; }
        internal bool? RefusedAddClean { get; set; }
        internal string EditControl { get; set; }
        internal bool? EditFocused { get; set; }
        internal string RetargetControl { get; set; }
        internal bool? RetargetApplied { get; set; }
        internal string UndoControl { get; set; }
        internal bool? UndoIntentRestored { get; set; }
        internal string DoneControl { get; set; }
        internal bool? DoneClearedFocus { get; set; }
        internal string SaveControl { get; set; }
        // True only when the Save control ran AND the session reports its
        // authored intent clean afterwards (never a hard-coded claim).
        internal bool? Saved { get; set; }

        internal IList<WorkspaceCastStepEvidence> CastSteps
        {
            get { return _castSteps.AsReadOnly(); }
        }

        internal void RecordBrowse(bool browseNoMutation, string buffControl,
            int capableCasters, int targets)
        {
            BrowseRecorded = true;
            BrowseNoMutation = browseNoMutation;
            BuffControl = buffControl ?? "missing";
            CapableCasters = capableCasters;
            Targets = targets;
        }

        internal WorkspaceCastStepEvidence RecordCastStep(
            WorkspaceCastStepEvidence step)
        {
            if (step == null) throw new ArgumentNullException("step");
            _castSteps.Add(step);
            return step;
        }

        internal void AddNote(string note)
        {
            if (!string.IsNullOrEmpty(note)) _notes.Add(note);
        }

        internal string Describe()
        {
            var parts = new List<string>();
            if (BrowseRecorded)
            {
                parts.Add("browseNoMutation=" + BrowseNoMutation);
                parts.Add("buffControl=" + BuffControl);
                parts.Add("capableCasters=" +
                    CapableCasters.ToString(CultureInfo.InvariantCulture));
                parts.Add("targets=" +
                    Targets.ToString(CultureInfo.InvariantCulture));
            }
            foreach (WorkspaceCastStepEvidence step in _castSteps)
                parts.Add(step.Describe());
            if (RefusedAddControl != null)
                parts.Add("refusedAdd=" + RefusedAddControl +
                    ";refusedAddClean=" + Describe(RefusedAddClean));
            if (EditControl != null)
                parts.Add("editControl=" + EditControl +
                    ";editFocused=" + Describe(EditFocused));
            if (RetargetControl != null)
                parts.Add("retargetControl=" + RetargetControl +
                    ";retargetApplied=" + Describe(RetargetApplied));
            if (UndoControl != null)
                parts.Add("undoControl=" + UndoControl +
                    ";undoIntentRestored=" + Describe(UndoIntentRestored));
            if (DoneControl != null)
                parts.Add("doneControl=" + DoneControl +
                    ";doneClearedFocus=" + Describe(DoneClearedFocus));
            if (SaveControl != null)
                parts.Add("saveControl=" + SaveControl +
                    ";saved=" + Describe(Saved));
            parts.AddRange(_notes);
            return parts.Count == 0 ? "not-run" : string.Join(";", parts.ToArray());
        }

        // Every unmet requirement, named. Empty means the sequence is
        // accepted. A record that never ran (for example, the manual
        // scenario, which performs no scripted authoring) cannot satisfy it.
        internal IList<string> Violations()
        {
            var violations = new List<string>();
            if (!BrowseRecorded) violations.Add("browse:not-run");
            else if (!BrowseNoMutation) violations.Add("browse:mutated");
            if (_castSteps.Count != RequiredCastSteps)
                violations.Add("castSteps:count=" +
                    _castSteps.Count.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < _castSteps.Count; index++)
            {
                WorkspaceCastStepEvidence step = _castSteps[index];
                string prefix = "cast" +
                    (index + 1).ToString(CultureInfo.InvariantCulture) + ":";
                if (step.Ordinal != index + 1)
                    violations.Add(prefix + "ordinal=" +
                        step.Ordinal.ToString(CultureInfo.InvariantCulture));
                if (!step.ControlsInvoked)
                    violations.Add(prefix + "controls=" + step.CasterControl +
                        "/" + step.TargetControl + "/" + step.AddControl);
                if (!step.StateAccepted)
                    violations.Add(prefix + "state=" + step.StateOutcome);
                if (!step.Grew) violations.Add(prefix + "not-added");
                if (!step.Distinct) violations.Add(prefix + "reused-id");
                if (!step.FieldsExact) violations.Add(prefix + "fields-inexact");
                if (!step.SiblingsUnchanged)
                    violations.Add(prefix + "sibling-changed");
            }
            RequireControl(violations, "refusedAdd", RefusedAddControl,
                RefusedAddClean);
            RequireControl(violations, "edit", EditControl, EditFocused);
            RequireControl(violations, "retarget", RetargetControl,
                RetargetApplied);
            RequireControl(violations, "undo", UndoControl, UndoIntentRestored);
            RequireControl(violations, "done", DoneControl, DoneClearedFocus);
            RequireControl(violations, "save", SaveControl, Saved);
            return violations;
        }

        private static void RequireControl(List<string> violations,
            string name, string control, bool? outcome)
        {
            if (control == null)
            {
                violations.Add(name + ":not-run");
                return;
            }
            if (control != WorkspaceControlOutcome.Invoked)
                violations.Add(name + ":control=" + control);
            if (outcome != true)
                violations.Add(name + ":outcome=" + Describe(outcome));
        }

        private static string Describe(bool? value)
        {
            return value.HasValue ? value.Value.ToString() : "missing";
        }
    }

    // ------------------------------------------------------------------
    // Supervised manual session terminal contract (review J2)
    // ------------------------------------------------------------------

    internal enum ManualTerminalRequest
    {
        None,
        Completed,
        Cancelled,
        Deadline
    }

    internal static class ManualTerminalPolicy
    {
        // Markers are honored any time between manual-ready and the
        // deadline; a stop request always wins over a concurrent done
        // request (review I2); once the deadline has elapsed the outcome is
        // the deadline, which is NEVER acceptance.
        internal static ManualTerminalRequest Classify(bool deadlineElapsed,
            bool stopMarkerPresent, bool doneMarkerPresent)
        {
            if (deadlineElapsed) return ManualTerminalRequest.Deadline;
            if (stopMarkerPresent) return ManualTerminalRequest.Cancelled;
            if (doneMarkerPresent) return ManualTerminalRequest.Completed;
            return ManualTerminalRequest.None;
        }

        internal static string OutcomeText(ManualTerminalRequest request)
        {
            switch (request)
            {
                case ManualTerminalRequest.Completed:
                    return "manual-completed;by=done-marker";
                case ManualTerminalRequest.Cancelled:
                    return "manual-cancelled;by=stop-marker";
                case ManualTerminalRequest.Deadline:
                    return "manual-deadline;timeout-is-not-acceptance";
                default:
                    return "manual-pending";
            }
        }
    }

    // Unity-free snapshot of a completed camera-path capture callback.
    internal sealed class ManualCaptureObservation
    {
        internal string FileName { get; set; }
        internal string FailureType { get; set; }
        internal string FailureMessage { get; set; }
        // Null when the capture routine did not report a restoration
        // verdict at all; that is never treated as clean.
        internal bool? RestorationClean { get; set; }
        internal string RestorationVerdict { get; set; }
        internal bool FileExists { get; set; }
        internal string Sha256 { get; set; }
        internal string LumaSummary { get; set; }

        internal bool Failed
        {
            get { return !string.IsNullOrEmpty(FailureType); }
        }
    }

    internal enum ManualFinalCaptureState
    {
        NotStarted,
        Pending,
        Captured,
        CaptureFailed,
        CallbackMissing
    }

    internal enum ManualCameraRestorationState
    {
        NotObserved,
        Clean,
        Unclean
    }

    // Orchestrates the manual scenario's terminal step once a terminal
    // request (done/stop/deadline) is observed. Keeps FIVE facts separate:
    // the operator's request, final evidence capture, native camera
    // restoration, workspace/input-lease cleanup, and — explicitly not
    // inferred here — the operator's usability verdict. The final capture
    // is bounded: a missing callback can never hold the session open.
    internal sealed class ManualTerminalCoordinator
    {
        internal const string FinalCaptureFileName = "manual-final.png";
        internal const long DefaultCaptureBudgetMillis = 20000;

        private readonly string _expectedFileName;
        private readonly long _captureBudgetMillis;
        private long _captureStartedMillis = -1;

        internal ManualTerminalCoordinator()
            : this(FinalCaptureFileName, DefaultCaptureBudgetMillis)
        {
        }

        internal ManualTerminalCoordinator(string expectedFileName,
            long captureBudgetMillis)
        {
            if (string.IsNullOrEmpty(expectedFileName))
                throw new ArgumentException("expectedFileName");
            if (captureBudgetMillis <= 0)
                throw new ArgumentOutOfRangeException("captureBudgetMillis");
            _expectedFileName = expectedFileName;
            _captureBudgetMillis = captureBudgetMillis;
            Request = ManualTerminalRequest.None;
            CaptureState = ManualFinalCaptureState.NotStarted;
            RestorationState = ManualCameraRestorationState.NotObserved;
            CaptureEvidence = "not-started";
            RestorationEvidence = "not-observed";
            CloseEvidence = "not-run";
        }

        internal ManualTerminalRequest Request { get; private set; }
        internal ManualFinalCaptureState CaptureState { get; private set; }
        internal ManualCameraRestorationState RestorationState { get; private set; }
        internal string CaptureEvidence { get; private set; }
        internal string RestorationEvidence { get; private set; }
        internal bool CloseRecorded { get; private set; }
        internal bool WorkspaceClosed { get; private set; }
        internal string CloseEvidence { get; private set; }

        internal string OutcomeText
        {
            get { return ManualTerminalPolicy.OutcomeText(Request); }
        }

        internal bool CaptureResolved
        {
            get
            {
                return CaptureState == ManualFinalCaptureState.Captured ||
                    CaptureState == ManualFinalCaptureState.CaptureFailed ||
                    CaptureState == ManualFinalCaptureState.CallbackMissing;
            }
        }

        // Called once, when the hold observes a terminal request; the host
        // then requests the final capture.
        internal void Begin(ManualTerminalRequest request, long nowMillis)
        {
            if (request == ManualTerminalRequest.None)
                throw new ArgumentException("A terminal request is required.");
            if (Request != ManualTerminalRequest.None)
                throw new InvalidOperationException("Terminal step already begun.");
            Request = request;
            _captureStartedMillis = nowMillis;
            CaptureState = ManualFinalCaptureState.Pending;
            CaptureEvidence = "pending";
        }

        // Returns true once the terminal step may close the workspace: the
        // capture callback resolved (success or failure) or its budget was
        // exhausted. Observations for other files are ignored; anything
        // arriving after resolution cannot change the recorded verdict.
        internal bool Poll(long nowMillis, ManualCaptureObservation observation)
        {
            if (Request == ManualTerminalRequest.None)
                throw new InvalidOperationException("Terminal step not begun.");
            if (CaptureResolved) return true;
            if (observation != null && string.Equals(observation.FileName,
                    _expectedFileName, StringComparison.OrdinalIgnoreCase))
            {
                Resolve(observation);
                return true;
            }
            long waited = nowMillis - _captureStartedMillis;
            if (waited < _captureBudgetMillis) return false;
            CaptureState = ManualFinalCaptureState.CallbackMissing;
            CaptureEvidence = "callback-missing;waitedMillis=" +
                waited.ToString(CultureInfo.InvariantCulture) +
                ";budgetMillis=" +
                _captureBudgetMillis.ToString(CultureInfo.InvariantCulture);
            RestorationState = ManualCameraRestorationState.NotObserved;
            RestorationEvidence = "not-observed;capture-callback-missing";
            return true;
        }

        private void Resolve(ManualCaptureObservation observation)
        {
            if (observation.RestorationClean == true)
            {
                RestorationState = ManualCameraRestorationState.Clean;
                RestorationEvidence = observation.RestorationVerdict ?? "clean";
            }
            else if (observation.RestorationClean == false)
            {
                RestorationState = ManualCameraRestorationState.Unclean;
                RestorationEvidence = "unclean;" +
                    (observation.RestorationVerdict ?? "verdict-missing");
            }
            else
            {
                RestorationState = ManualCameraRestorationState.NotObserved;
                RestorationEvidence = "not-observed;verdict-missing";
            }
            if (observation.Failed)
            {
                CaptureState = ManualFinalCaptureState.CaptureFailed;
                CaptureEvidence = "failed:" + observation.FailureType +
                    (string.IsNullOrEmpty(observation.FailureMessage)
                        ? string.Empty : ":" + observation.FailureMessage);
                return;
            }
            if (!observation.FileExists || string.IsNullOrEmpty(observation.Sha256))
            {
                CaptureState = ManualFinalCaptureState.CaptureFailed;
                CaptureEvidence = "failed:file-missing";
                return;
            }
            CaptureState = ManualFinalCaptureState.Captured;
            CaptureEvidence = "captured;sha256=" + observation.Sha256 +
                ";luma=" + (observation.LumaSummary ?? "missing");
        }

        // The production close ran (or threw); the postcondition is the
        // observed state afterwards — view gone AND input lease released —
        // not the call itself.
        internal void RecordClose(bool workspaceOpenAfterClose,
            bool inputLeaseHeldAfterClose, string closeFailure)
        {
            CloseRecorded = true;
            WorkspaceClosed = !workspaceOpenAfterClose &&
                !inputLeaseHeldAfterClose &&
                string.IsNullOrEmpty(closeFailure);
            CloseEvidence = "workspaceOpenAfterClose=" + workspaceOpenAfterClose +
                ";inputLeaseHeldAfterClose=" + inputLeaseHeldAfterClose +
                (string.IsNullOrEmpty(closeFailure)
                    ? string.Empty : ";closeFailure=" + closeFailure);
        }

        // The final capture could not even be requested; nothing was
        // mutated by it, but restoration is not claimed as observed.
        internal void RecordCaptureStartFailure(string failure)
        {
            if (Request == ManualTerminalRequest.None)
                throw new InvalidOperationException("Terminal step not begun.");
            if (CaptureResolved) return;
            CaptureState = ManualFinalCaptureState.CaptureFailed;
            CaptureEvidence = "failed:start:" + (failure ?? "unknown");
            RestorationState = ManualCameraRestorationState.NotObserved;
            RestorationEvidence = "not-observed;capture-not-started";
        }

        // Writes the manual result contract. The done marker proves only
        // that the operator ended the session; capture, restoration and
        // cleanup are separate assertions, and any failure among them makes
        // the run FAIL with the most safety-relevant stage first.
        internal void AppendAssertions(RuntimeTestResult result,
            string readyEvidence, string manualInteractionEvidence)
        {
            if (result == null) throw new ArgumentNullException("result");
            string ready = readyEvidence ?? "missing";
            bool readyAcknowledged = ready.StartsWith(
                "manual-ready;workspaceOpen=true", StringComparison.Ordinal);
            Add(result, readyAcknowledged, "manual-ready-acknowledged",
                "workspace open;no input requested", ready);
            string interaction = manualInteractionEvidence ?? "missing";
            bool noAuthoring = interaction.StartsWith("manual;outcome=",
                StringComparison.Ordinal);
            Add(result, noAuthoring, "manual-no-automatic-authoring",
                "scripted authoring suspended", interaction);
            bool completed = Request == ManualTerminalRequest.Completed;
            Add(result, completed, "manual-session-outcome", "done-marker",
                Request == ManualTerminalRequest.None ? "missing" : OutcomeText);
            bool captured = CaptureState == ManualFinalCaptureState.Captured;
            Add(result, captured, "manual-final-capture",
                _expectedFileName + " png + sha256", CaptureEvidence);
            bool restored = RestorationState == ManualCameraRestorationState.Clean;
            Add(result, restored, "manual-camera-restoration",
                "targetsRestored=True;activeRestored=True;cleanupFailures=0",
                RestorationEvidence);
            Add(result, CloseRecorded && WorkspaceClosed,
                "manual-workspace-closed",
                "production close;view closed;input lease released", CloseEvidence);

            string stage = null;
            if (RestorationState == ManualCameraRestorationState.Unclean)
                stage = "manual-camera-restoration-unclean";
            else if (!CloseRecorded || !WorkspaceClosed)
                stage = "manual-workspace-close";
            else if (Request == ManualTerminalRequest.Cancelled)
                stage = "manual-cancelled";
            else if (Request == ManualTerminalRequest.Deadline)
                stage = "manual-deadline";
            else if (!readyAcknowledged || !noAuthoring || !completed)
                stage = "manual-lifecycle-validation";
            else if (CaptureState == ManualFinalCaptureState.CallbackMissing)
                stage = "manual-final-capture-missing";
            else if (!captured)
                stage = "manual-final-capture-failed";
            else if (!restored)
                stage = "manual-camera-restoration-unverified";
            if (stage != null)
            {
                result.Status = "FAIL";
                result.Stage = stage;
            }
        }

        private static void Add(RuntimeTestResult result, bool passed,
            string id, string expected, string observed)
        {
            result.Assertions.Add(passed
                ? RuntimeTestAssertion.Pass(id, expected, observed)
                : RuntimeTestAssertion.Fail(id, expected, observed));
        }
    }
}
