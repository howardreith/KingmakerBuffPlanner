using System;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    internal enum ControllerRoutineCommand
    {
        Up, Down, PreviousPage, NextPage, Confirm, Back
    }

    // Every operation is an edge. Native callbacks can arrive before or after
    // UMM's update; a neutral frame must precede the next accepted callback.
    // This also gates the first input after construction/reconnection.
    internal sealed class ControllerActivationLatch
    {
        private bool _waiting = true;
        private int _readyAfter = int.MaxValue;

        internal void Reset()
        {
            _waiting = true;
            _readyAfter = int.MaxValue;
        }

        internal void Observe(bool neutral, int frame)
        {
            if (!neutral) return;
            if (!_waiting) return;
            _waiting = false;
            _readyAfter = frame;
        }

        internal bool Ready(int frame)
        {
            return !_waiting && frame > _readyAfter;
        }

        internal bool Take(int frame)
        {
            if (!Ready(frame)) return false;
            Reset();
            return true;
        }

        internal bool CanRelease(bool neutral, int frame)
        {
            if (!neutral) { Reset(); return false; }
            Observe(true, frame);
            return Ready(frame);
        }
    }

    // Focus and result paging belong to the selector, never to authoring.
    // Commands delegate to the existing routine entry and host. In particular,
    // this class cannot compile, save, reconcile, accept or spend anything.
    internal sealed class ControllerRoutineModel
    {
        internal static readonly string[] RoutineIds = { "long", "important", "short" };
        private readonly CastingWorkspaceSession _session;
        private readonly Func<string, bool> _run;
        private readonly Action _stop;
        private readonly Func<bool> _running;
        private readonly Func<string> _activeRoutine;
        private readonly string _unavailable;

        internal ControllerRoutineModel(CastingWorkspaceSession session, string unavailable,
            Func<string, bool> run, Action stop, Func<bool> running, Func<string> activeRoutine)
        {
            _session = session;
            _unavailable = unavailable;
            _run = run;
            _stop = stop;
            _running = running;
            _activeRoutine = activeRoutine;
            Message = unavailable ?? "Select a saved routine, then confirm Run. Nothing runs while browsing.";
            if (session != null && session.Document.Castings.Count == 0)
                Message = "This campaign has no configured castings. Create/edit plans with mouse and keyboard, then return here.";
            if (session != null && session.PersistenceBlocked)
                Message = "The saved plan could not be loaded: " + session.LoadWarning +
                    ". Use the mouse/keyboard editor's recovery controls, then return here.";
            if (session != null)
            {
                string saveProblem = WorkspaceFooterText.SaveProblem(session.FooterSaveState);
                if (saveProblem != null)
                    Message = saveProblem + "\n" + (session.PersistenceBlocked ? Message :
                        session.LoadWarning + "\nUse the mouse/keyboard editor's recovery controls, then return here.") +
                        (session.LegacyImportBlocked ? "\n" + session.LegacyImportBlockReason : string.Empty);
                string latest = ControllerRoutineDetails.LatestOutcome(session);
                if (latest != null) Message = latest;
            }
            if (Running) Focus = 3;
        }

        internal int Focus { get; private set; }
        internal string Message { get; private set; }
        internal bool Running { get { return _running(); } }
        internal string ActiveRoutine { get { return _activeRoutine(); } }
        internal string CampaignId { get { return _session == null ? null : _session.CampaignId; } }

        internal string Settings
        {
            get
            {
                if (_session == null) return _unavailable ?? "No saved plan is available.";
                return (_session.ExecutionMode == "instant" ? "Instant" : "Animated") + " | " +
                    (_session.AvailabilityPolicy == ExecutionProfile.CastAvailable
                        ? "Cast available" : "Require full plan") + " | " +
                    WorkspaceFooterText.SaveStatus(_session.FooterSaveState);
            }
        }

        internal string Row(int index)
        {
            if (index == 3) return Running ? "Stop " + (ActiveRoutine ?? "running routine") : "Stop (no active run)";
            string id = RoutineIds[index];
            string name = _session == null ? char.ToUpperInvariant(id[0]) + id.Substring(1)
                : _session.RoutineDisplayName(id);
            int count = _session == null ? 0 : _session.Document.Castings.Count(c => c.RoutineId == id);
            return "Run " + name + (_session == null ? string.Empty : " (" + count + " castings)") +
                (Running && ActiveRoutine == id ? " — RUNNING" : string.Empty);
        }

        internal void Move(int direction)
        {
            Focus = (Focus + (direction < 0 ? 3 : 1)) % 4;
        }

        internal void Confirm()
        {
            if (Focus == 3)
            {
                if (!Running) { Message = "There is no active routine to stop."; return; }
                Message = "Stop requested. The cast in progress may finish; no later cast will start.";
                _stop();
                return;
            }
            if (Running)
            {
                Message = "A routine is running. Select Stop to end it after the cast in progress.";
                return;
            }
            if (_session == null)
            {
                Message = _unavailable ?? "Configure and save a plan with mouse and keyboard first.";
                return;
            }
            if (!_run(RoutineIds[Focus]))
                Message = "The routine entry is unavailable. Close this menu and try again after loading finishes.";
            if (Running) Focus = 3;
        }

        internal void ShowResult(string message)
        {
            Message = message ?? "No result is available.";
        }
    }

    internal static class ControllerRoutineDetails
    {
        private const string RepairNote = "\nPlan repair uses the mouse/keyboard editor. Back returns to the game. " +
            "Use the editor's Retry save/Reload controls when needed.";

        internal static string LatestOutcome(CastingWorkspaceSession session)
        {
            // The session keeps a newer refused attempt alongside the prior
            // completed report, and clears that message on later completion.
            if (!string.IsNullOrEmpty(session.LastAttemptMessage))
                return session.LastAttemptMessage + RepairNote;
            return session.LastRunReport == null ? null : Report(session.LastRunReport, session);
        }

        internal static string Report(CastingRunReport report, CastingWorkspaceSession session)
        {
            Func<string, string> label = session == null ? (Func<string, string>)(id => id) : session.CastingLabel;
            string name = session == null ? report.ScopeRoutineId : session.RoutineDisplayName(report.ScopeRoutineId);
            return CastingRunPresentation.Describe(report, name, label) + "\n\n" +
                string.Join("\n", report.Entries.Select(entry => label(entry.CastingId) + ": " +
                    CastingRunPresentation.DescribeEntry(entry)).ToArray());
        }

        internal static string Refusal(string summary, WorkspaceApplyResult result, CastingWorkspaceSession session)
        {
            return summary + "\n\n" + string.Join("\n", result.BlockingCastings.Select(blocker =>
                session.CastingLabel(blocker.CastingId) + ": " +
                string.Join("; ", blocker.Reasons.Select(WorkspaceReasonText.Describe).ToArray())).ToArray()) +
                RepairNote;
        }
    }
}
