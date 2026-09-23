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
        internal ExplicitCastingRunEntry(string castingId, bool attempted,
            bool confirmed, string finalStatus, string detail)
        {
            CastingId = castingId;
            Attempted = attempted;
            Confirmed = confirmed;
            FinalStatus = finalStatus ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string CastingId { get; private set; }
        // False when the run halted before this casting was submitted.
        public bool Attempted { get; private set; }
        public bool Confirmed { get; private set; }
        public string FinalStatus { get; private set; }
        public string Detail { get; private set; }
    }

    public sealed class ExplicitCastingRunOutcome
    {
        internal ExplicitCastingRunOutcome(string projectionId,
            IList<ExplicitCastingRunEntry> entries, string haltedAfterCastingId,
            string haltReason)
        {
            ProjectionId = projectionId ?? string.Empty;
            Entries = new ReadOnlyCollection<ExplicitCastingRunEntry>(entries);
            HaltedAfterCastingId = haltedAfterCastingId;
            HaltReason = haltReason ?? string.Empty;
        }

        public string ProjectionId { get; private set; }
        public IReadOnlyList<ExplicitCastingRunEntry> Entries { get; private set; }
        public string HaltedAfterCastingId { get; private set; }
        public string HaltReason { get; private set; }
        public bool Halted { get { return HaltedAfterCastingId != null || HaltReason.Length != 0; } }
        public bool AllConfirmed
        {
            get { return !Halted && Entries.All(entry => entry.Confirmed); }
        }
    }

    // Review K5: runs an approved explicit projection through an EXISTING
    // executor (instant or animated, unchanged) one casting at a time and
    // stops all FUTURE submissions as soon as a casting is not positively
    // confirmed — validation/preparation/submission failure, a
    // required-enhancement failure, an unconfirmed or timed-out result, or
    // unsettled residual state. The executor's own cleanup still runs for
    // the in-flight casting; nothing is retried. Already-applied effects
    // and spent resources are not (and cannot be) rolled back — the
    // outcome reports the incomplete plan truthfully instead.
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
            if (projection == null) throw new ArgumentNullException("projection");
            if (completed == null) throw new ArgumentNullException("completed");
            var entries = new List<ExplicitCastingRunEntry>();
            if (!projection.Converted)
            {
                completed(new ExplicitCastingRunOutcome(projection.ProjectionId,
                    entries, null, "projection-not-converted:" + projection.Refusal));
                yield break;
            }
            string haltedAfter = null;
            string haltReason = null;
            for (int index = 0; index < projection.Plan.Steps.Count; index++)
            {
                string castingId = projection.CastingIds[index];
                if (haltReason != null)
                {
                    entries.Add(new ExplicitCastingRunEntry(castingId, false, false,
                        "NotAttempted", "halted:" + haltReason));
                    continue;
                }
                if (index >= _maximumSubmissions)
                {
                    haltReason = "submission-limit:" + _maximumSubmissions;
                    entries.Add(new ExplicitCastingRunEntry(castingId, false, false,
                        "NotAttempted", "halted:" + haltReason));
                    continue;
                }
                var single = new CastPlan(new[] { projection.Plan.Steps[index] },
                    new TargetPlanOutcome[0],
                    new[] { "explicit-run;projection=" + projection.ProjectionId +
                        ";casting=" + castingId });
                var report = new ExecutionReport(single);
                IEnumerator run = _executor.Execute(single, report);
                while (true)
                {
                    bool moved;
                    try { moved = run.MoveNext(); }
                    catch (Exception exception)
                    {
                        report.Add(0, projection.Plan.Steps[index],
                            CastExecutionStatus.FailedExecution,
                            "executor-exception:" + exception.GetType().Name + ":" +
                            exception.Message);
                        break;
                    }
                    if (!moved) break;
                    yield return run.Current;
                }
                CastExecutionRecord stop = report.Records.FirstOrDefault(
                    record => Stopping.Contains(record.Status));
                bool confirmed = stop == null && report.Records.Any(
                    record => record.Status == CastExecutionStatus.EffectConfirmed);
                CastExecutionRecord last = report.Records.LastOrDefault();
                entries.Add(new ExplicitCastingRunEntry(castingId, true, confirmed,
                    stop != null ? stop.Status.ToString()
                        : last == null ? "NoRecord" : last.Status.ToString(),
                    stop != null ? stop.Detail : last == null ? "no-executor-record"
                        : last.Detail));
                if (!confirmed)
                {
                    haltedAfter = castingId;
                    haltReason = stop != null
                        ? stop.Status + ":" + stop.Detail
                        : "not-confirmed:" + (last == null ? "no-record" : last.Status.ToString());
                }
            }
            completed(new ExplicitCastingRunOutcome(projection.ProjectionId, entries,
                haltedAfter, haltReason));
        }
    }
}
