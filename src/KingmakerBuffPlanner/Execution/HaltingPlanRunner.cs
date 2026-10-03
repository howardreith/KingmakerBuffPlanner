using System;
using System.Collections;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    // The Classic routine's run over one executor (mission batch 3, section
    // 6: failures stop later castings and preserve cleanup). Each step runs
    // as its own single-step plan; its work is disposed in a finally, so an
    // owner that stops the run still gets the executor's cleanup (an
    // interrupted command, a held touch removed); its records are copied
    // under the step's own index; and after the first step that did not
    // confirm its effect (or recorded any failure) every later step is
    // recorded as halted and never started.
    public sealed class HaltingPlanRunner
    {
        public const string HaltedDetailPrefix = "halted-after-unconfirmed-step:";
        private readonly ICastExecutor _executor;

        public HaltingPlanRunner(ICastExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException("executor");
        }

        // The index of the step after which the run halted, or null.
        public int? HaltedAfterStep { get; private set; }

        public IEnumerator Run(CastPlan plan, ExecutionReport report)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (report == null) throw new ArgumentNullException("report");
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                if (HaltedAfterStep != null)
                {
                    report.Add(index, step, CastExecutionStatus.FailedValidation,
                        HaltedDetailPrefix + HaltedAfterStep.Value);
                    continue;
                }
                var single = new CastPlan(new[] { step }, new TargetPlanOutcome[0], new string[0]);
                var partial = new ExecutionReport(single);
                IEnumerator work = _executor.Execute(single, partial);
                try
                {
                    while (work.MoveNext()) yield return work.Current;
                }
                finally
                {
                    try
                    {
                        IDisposable disposable = work as IDisposable;
                        if (disposable != null) disposable.Dispose();
                    }
                    finally
                    {
                        // Cleanup can add records; they are kept even when
                        // the run is stopped or throws.
                        foreach (CastExecutionRecord record in partial.Records)
                            report.Add(index, step, record.Status, record.Detail);
                    }
                }
                if (partial.Failed != 0 ||
                    !partial.Records.Any(record => record.Status == CastExecutionStatus.EffectConfirmed))
                    HaltedAfterStep = index;
            }
        }
    }
}
