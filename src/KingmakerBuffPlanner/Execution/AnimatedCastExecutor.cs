using System;
using System.Collections;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    public sealed class AnimatedCastExecutor : ICastExecutor
    {
        private readonly ICastRuntimeAdapter _runtime;
        private readonly bool _outOfCombatOnly;

        public AnimatedCastExecutor(ICastRuntimeAdapter runtime, bool outOfCombatOnly)
        {
            _runtime = runtime ?? throw new ArgumentNullException("runtime");
            _outOfCombatOnly = outOfCombatOnly;
        }

        public IEnumerator Execute(CastPlan plan, ExecutionReport report)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (report == null) throw new ArgumentNullException("report");
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                if (_outOfCombatOnly && _runtime.IsInCombat)
                {
                    report.Add(index, step, CastExecutionStatus.FailedValidation, "combat-policy");
                    continue;
                }
                CastEnhancementPreparation enhancement = Prepare(step);
                if (!enhancement.Valid)
                {
                    report.Add(index, step, CastExecutionStatus.FailedValidation,
                        "enhancement-unavailable:" + enhancement.Reason);
                    continue;
                }
                CastRuntimeValidation validation = _runtime.Validate(step);
                if (!validation.Valid)
                {
                    enhancement.Dispose();
                    report.Add(index, step, CastExecutionStatus.FailedValidation,
                        validation.Reason);
                    continue;
                }
                IAnimatedCastOperation operation;
                try { operation = _runtime.StartAnimated(step); }
                catch (Exception exception)
                {
                    enhancement.Dispose();
                    report.Add(index, step, CastExecutionStatus.FailedSubmission,
                        "start-exception:" + exception.GetType().FullName + ":" + exception.Message);
                    continue;
                }
                if (operation == null)
                {
                    enhancement.Dispose();
                    report.Add(index, step, CastExecutionStatus.FailedSubmission, "operation-null");
                    continue;
                }
                try
                {
                    report.Add(index, step, CastExecutionStatus.Queued, "animated-command-queued");
                    bool startedRecorded = false;
                    while (!operation.IsCompleted)
                    {
                        if (!startedRecorded && operation.IsStarted)
                        {
                            report.Add(index, step, CastExecutionStatus.CastStarted,
                                "animated-command-started");
                            startedRecorded = true;
                        }
                        yield return null;
                    }
                    if (!startedRecorded && operation.IsStarted)
                        report.Add(index, step, CastExecutionStatus.CastStarted,
                            "animated-command-started");
                    if (operation.TimedOut)
                        report.Add(index, step, CastExecutionStatus.TimedOutUnconfirmed, operation.Detail);
                    else if (!operation.Succeeded)
                        report.Add(index, step, CastExecutionStatus.FailedExecution, operation.Detail);
                    else if (operation.EffectsObserved)
                        report.Add(index, step, CastExecutionStatus.EffectConfirmed,
                            "expected-effects-observed;" + operation.Detail);
                    else
                        report.Add(index, step, CastExecutionStatus.TimedOutUnconfirmed,
                            "expected-effects-absent;" + operation.Detail);
                    if (operation.Succeeded && operation.ResourceSpent)
                        report.Add(index, step, CastExecutionStatus.ResourceSpent,
                            "native-command-spend-completed");
                }
                finally
                {
                    enhancement.Dispose();
                }
                yield return null;
            }
        }

        private CastEnhancementPreparation Prepare(CastStep step)
        {
            if (step.EnhancementIds.Count == 0) return CastEnhancementPreparation.Pass(null);
            var runtime = _runtime as ICastEnhancementRuntimeAdapter;
            if (runtime == null) return CastEnhancementPreparation.Fail("runtime-adapter-unsupported");
            try
            {
                return runtime.PrepareEnhancements(step) ??
                    CastEnhancementPreparation.Fail("preparation-result-null");
            }
            catch (Exception exception)
            {
                return CastEnhancementPreparation.Fail("preparation-exception:" +
                    exception.GetType().FullName + ":" + exception.Message);
            }
        }
    }
}
