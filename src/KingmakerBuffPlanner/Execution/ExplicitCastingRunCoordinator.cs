using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.Execution
{
    // The per-casting result of a coordinated explicit run.
    public sealed class ExplicitCastingRunEntry
    {
        internal ExplicitCastingRunEntry(string castingId, bool processed,
            bool nativeSubmissionReported, bool confirmed, string finalStatus,
            string detail, bool resourceSpentReported = false,
            bool spendInvoked = false)
        {
            CastingId = castingId;
            Processed = processed;
            NativeSubmissionReported = nativeSubmissionReported;
            Confirmed = confirmed;
            FinalStatus = finalStatus ?? string.Empty;
            Detail = detail ?? string.Empty;
            ResourceSpentReported = resourceSpentReported;
            SpendInvoked = spendInvoked;
        }

        // Resource spending is reported separately from the effect: the
        // executor observed the native resource being spent (ResourceSpent)
        // or invoked the native spend (SpendInvoked). Neither says anything
        // about whether the effect landed, and neither is ever reversed.
        public bool ResourceSpentReported { get; private set; }
        public bool SpendInvoked { get; private set; }

        public string CastingId { get; private set; }
        // Review L2: the executor was invoked for this casting (validation
        // may still have refused it before any native call). False when the
        // run halted, was cancelled or hit its limit before this casting.
        public bool Processed { get; private set; }
        // The executor reported a native submission/command for this
        // casting (queued, submitted, started, spend or effect records). A
        // validation refusal is Processed but never a submission.
        public bool NativeSubmissionReported { get; private set; }
        public bool Confirmed { get; private set; }
        public string FinalStatus { get; private set; }
        public string Detail { get; private set; }
    }

    public sealed class ExplicitCastingRunOutcome
    {
        internal ExplicitCastingRunOutcome(string projectionId,
            IList<ExplicitCastingRunEntry> entries, string haltedAfterCastingId,
            string haltReason, bool cancelled)
        {
            ProjectionId = projectionId ?? string.Empty;
            Entries = new ReadOnlyCollection<ExplicitCastingRunEntry>(entries);
            HaltedAfterCastingId = haltedAfterCastingId;
            HaltReason = haltReason ?? string.Empty;
            Cancelled = cancelled;
        }

        public string ProjectionId { get; private set; }
        public IReadOnlyList<ExplicitCastingRunEntry> Entries { get; private set; }
        public string HaltedAfterCastingId { get; private set; }
        public string HaltReason { get; private set; }
        // The run was disposed/abandoned by its owner before finishing. The
        // in-flight casting's executor was disposed (its own cleanup ran);
        // effects already applied and resources already spent are NOT
        // reversed — the entry reports what the executor recorded.
        public bool Cancelled { get; private set; }
        public bool Halted { get { return HaltedAfterCastingId != null || HaltReason.Length != 0; } }
        public bool AllConfirmed
        {
            get { return !Halted && Entries.All(entry => entry.Confirmed); }
        }
    }

    // Review K5/L2: runs an approved explicit projection through an
    // EXISTING executor (instant or animated, unchanged) one casting at a
    // time and stops all FUTURE submissions as soon as a casting is not
    // positively confirmed — validation/preparation/submission failure, a
    // required-enhancement failure, an unconfirmed or timed-out result,
    // unsettled residual state, or uncertain executor cleanup.
    //
    // The coordinator OWNS each nested executor iterator: it disposes it
    // exactly once on normal exhaustion, on failure, and when the OUTER
    // iterator is disposed while suspended (cancellation, deadline,
    // teardown), so the executor's own finally-block cleanup always runs.
    // Nothing is retried. Already-applied effects and spent resources are
    // not (and cannot be) rolled back; the outcome reports them truthfully.
    // The completion callback is invoked exactly once, including from the
    // cancellation path.
    public sealed class ExplicitCastingRunCoordinator
    {
        private static readonly CastExecutionStatus[] Stopping =
        {
            CastExecutionStatus.FailedValidation,
            CastExecutionStatus.FailedSubmission,
            CastExecutionStatus.FailedExecution,
            CastExecutionStatus.TimedOutUnconfirmed,
            CastExecutionStatus.ResidualStateUnsettled
        };

        private static readonly CastExecutionStatus[] SubmissionEvidence =
        {
            CastExecutionStatus.Queued,
            CastExecutionStatus.Submitted,
            CastExecutionStatus.CastStarted,
            CastExecutionStatus.SpendInvoked,
            CastExecutionStatus.ResourceSpent,
            CastExecutionStatus.EffectConfirmed
        };

        private readonly ICastExecutor _executor;
        private readonly int _maximumSubmissions;

        public ExplicitCastingRunCoordinator(ICastExecutor executor,
            int maximumSubmissions = int.MaxValue)
        {
            _executor = executor ?? throw new ArgumentNullException("executor");
            if (maximumSubmissions < 1)
                throw new ArgumentOutOfRangeException("maximumSubmissions");
            _maximumSubmissions = maximumSubmissions;
        }

        public IEnumerator Run(ExplicitStepConversion projection,
            Action<ExplicitCastingRunOutcome> completed)
        {
            return Run(projection, completed, null);
        }

        // castingFinished observes each casting as soon as its executor has
        // finished and been disposed (progress only: an observer exception
        // is swallowed and never changes the run).
        public IEnumerator Run(ExplicitStepConversion projection,
            Action<ExplicitCastingRunOutcome> completed,
            Action<ExplicitCastingRunEntry> castingFinished)
        {
            if (projection == null) throw new ArgumentNullException("projection");
            if (completed == null) throw new ArgumentNullException("completed");
            return RunCore(projection, completed, castingFinished);
        }

        private IEnumerator RunCore(ExplicitStepConversion projection,
            Action<ExplicitCastingRunOutcome> completed,
            Action<ExplicitCastingRunEntry> castingFinished)
        {
            var entries = new List<ExplicitCastingRunEntry>();
            if (!projection.Converted)
            {
                completed(new ExplicitCastingRunOutcome(projection.ProjectionId,
                    entries, null, "projection-not-converted:" + projection.Refusal, false));
                yield break;
            }
            bool reported = false;
            IEnumerator active = null;
            ExecutionReport activeReport = null;
            int activeIndex = -1;
            string haltedAfter = null;
            string haltReason = null;
            try
            {
                for (int index = 0; index < projection.Plan.Steps.Count; index++)
                {
                    string castingId = projection.CastingIds[index];
                    if (haltReason != null)
                    {
                        entries.Add(NotProcessed(castingId, haltReason));
                        continue;
                    }
                    if (index >= _maximumSubmissions)
                    {
                        haltReason = "submission-limit:" + _maximumSubmissions;
                        entries.Add(NotProcessed(castingId, haltReason));
                        continue;
                    }
                    CastStep step = projection.Plan.Steps[index];
                    var single = new CastPlan(new[] { step },
                        new TargetPlanOutcome[0],
                        new[] { "explicit-run;projection=" + projection.ProjectionId +
                            ";casting=" + castingId });
                    var report = new ExecutionReport(single);
                    activeReport = report;
                    activeIndex = index;
                    try { active = _executor.Execute(single, report); }
                    catch (Exception exception)
                    {
                        active = null;
                        report.Add(0, step, CastExecutionStatus.FailedExecution,
                            "executor-acquire-exception:" + Describe(exception));
                    }
                    if (active == null && report.Records.Count == 0)
                        report.Add(0, step, CastExecutionStatus.FailedExecution,
                            "executor-returned-no-iterator");
                    while (active != null)
                    {
                        bool moved = false;
                        object current = null;
                        try
                        {
                            moved = active.MoveNext();
                            if (moved) current = active.Current;
                        }
                        catch (Exception exception)
                        {
                            moved = false;
                            report.Add(0, step, CastExecutionStatus.FailedExecution,
                                "executor-exception:" + Describe(exception));
                        }
                        if (!moved) break;
                        yield return current;
                    }
                    // Exhausted or failed: dispose exactly once. A disposal
                    // failure is uncertain cleanup, recorded beside (never
                    // instead of) any earlier failure.
                    string disposeFailure = DisposeOnce(ref active);
                    if (disposeFailure != null)
                        report.Add(0, step, CastExecutionStatus.ResidualStateUnsettled,
                            "executor-dispose-exception:" + disposeFailure);
                    activeReport = null;
                    activeIndex = -1;
                    ExplicitCastingRunEntry entry = Evaluate(castingId, report);
                    entries.Add(entry);
                    if (castingFinished != null)
                    {
                        try { castingFinished(entry); }
                        catch (Exception)
                        {
                            // Progress observation never alters the run.
                        }
                    }
                    if (!entry.Confirmed)
                    {
                        haltedAfter = castingId;
                        haltReason = entry.FinalStatus + ":" + entry.Detail;
                    }
                    // One frame between castings: an owner that stops the run
                    // here stops it BETWEEN castings (the next one never
                    // starts), not in the middle of the next submission.
                    else if (index + 1 < projection.Plan.Steps.Count)
                        yield return null;
                }
                reported = true;
                completed(new ExplicitCastingRunOutcome(projection.ProjectionId, entries,
                    haltedAfter, haltReason, false));
            }
            finally
            {
                if (!reported)
                {
                    // The OUTER iterator was disposed (or an unexpected
                    // failure escaped) while a casting was in flight:
                    // propagate disposal so the executor's own cleanup runs.
                    string disposeFailure = DisposeOnce(ref active);
                    if (activeIndex >= 0 && activeReport != null)
                    {
                        CastStep step = projection.Plan.Steps[activeIndex];
                        if (disposeFailure != null)
                            activeReport.Add(0, step, CastExecutionStatus.ResidualStateUnsettled,
                                "executor-dispose-exception:" + disposeFailure);
                        ExplicitCastingRunEntry inFlight = Evaluate(
                            projection.CastingIds[activeIndex], activeReport);
                        entries.Add(new ExplicitCastingRunEntry(inFlight.CastingId, true,
                            inFlight.NativeSubmissionReported, false,
                            "Cancelled", "cancelled-in-flight;last:" + inFlight.FinalStatus +
                            ":" + inFlight.Detail, inFlight.ResourceSpentReported,
                            inFlight.SpendInvoked));
                        haltedAfter = inFlight.CastingId;
                    }
                    string cancelReason = "cancelled" +
                        (disposeFailure != null ? ";cleanup-uncertain:" + disposeFailure : string.Empty);
                    for (int index = entries.Count; index < projection.Plan.Steps.Count; index++)
                        entries.Add(NotProcessed(projection.CastingIds[index], cancelReason));
                    reported = true;
                    completed(new ExplicitCastingRunOutcome(projection.ProjectionId, entries,
                        haltedAfter, haltReason ?? cancelReason, true));
                }
            }
        }

        private static ExplicitCastingRunEntry NotProcessed(string castingId, string reason)
        {
            return new ExplicitCastingRunEntry(castingId, false, false, false,
                "NotProcessed", "halted:" + reason);
        }

        private static ExplicitCastingRunEntry Evaluate(string castingId, ExecutionReport report)
        {
            CastExecutionRecord stop = report.Records.FirstOrDefault(
                record => Stopping.Contains(record.Status));
            bool confirmed = stop == null && report.Records.Any(
                record => record.Status == CastExecutionStatus.EffectConfirmed);
            bool submitted = report.Records.Any(
                record => SubmissionEvidence.Contains(record.Status));
            CastExecutionRecord last = report.Records.LastOrDefault();
            string status = stop != null ? stop.Status.ToString()
                : last == null ? "NoRecord"
                : confirmed ? last.Status.ToString() : "NotConfirmed";
            string detail = stop != null ? stop.Detail
                : last == null ? "no-executor-record" : last.Status + ":" + last.Detail;
            // Every later stopping record (e.g. uncertain cleanup after a
            // failure) is kept in the detail, never replacing the first.
            string later = string.Join(";", report.Records
                .Where(record => Stopping.Contains(record.Status) && !ReferenceEquals(record, stop))
                .Select(record => record.Status + ":" + record.Detail).ToArray());
            if (later.Length != 0) detail += "|also:" + later;
            return new ExplicitCastingRunEntry(castingId, true, submitted, confirmed,
                status, detail,
                report.Records.Any(record => record.Status == CastExecutionStatus.ResourceSpent),
                report.Records.Any(record => record.Status == CastExecutionStatus.SpendInvoked));
        }

        private static string DisposeOnce(ref IEnumerator active)
        {
            IEnumerator owned = active;
            active = null;
            var disposable = owned as IDisposable;
            if (disposable == null) return null;
            try
            {
                disposable.Dispose();
                return null;
            }
            catch (Exception exception)
            {
                return Describe(exception);
            }
        }

        private static string Describe(Exception exception)
        {
            return exception.GetType().Name + ":" + exception.Message;
        }
    }
}
