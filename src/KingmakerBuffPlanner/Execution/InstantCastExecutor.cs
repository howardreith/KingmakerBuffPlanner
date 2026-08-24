using System;
using System.Collections;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    public sealed class InstantCastExecutor : ICastExecutor
    {
        private readonly IInstantCastRuntimeAdapter _runtime;
        private readonly bool _outOfCombatOnly;
        private readonly int _batchSize;

        public InstantCastExecutor(IInstantCastRuntimeAdapter runtime, bool outOfCombatOnly, int batchSize = 8)
        {
            _runtime = runtime ?? throw new ArgumentNullException("runtime");
            if (batchSize < 1 || batchSize > 64) throw new ArgumentOutOfRangeException("batchSize");
            _outOfCombatOnly = outOfCombatOnly;
            _batchSize = batchSize;
        }

        public IEnumerator Execute(CastPlan plan, ExecutionReport report)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (report == null) throw new ArgumentNullException("report");
            int sinceYield = 0;
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                if (_outOfCombatOnly && _runtime.IsInCombat)
                    report.Add(index, step, CastExecutionStatus.FailedValidation, "combat-policy");
                else
                {
                    CastRuntimeValidation validation = _runtime.Validate(step);
                    if (!validation.Valid)
                        report.Add(index, step, CastExecutionStatus.FailedValidation, validation.Reason);
                    else
                    {
                        CastEnhancementPreparation enhancement = Prepare(step);
                        if (!enhancement.Valid)
                            report.Add(index, step, CastExecutionStatus.FailedValidation,
                                "enhancement-unavailable:" + enhancement.Reason);
                        else
                        {
                            InstantCastResult result = null;
                            Exception submissionFailure = null;
                            try { result = _runtime.Fire(step); }
                            catch (Exception exception) { submissionFailure = exception; }
                            finally { enhancement.Dispose(); }
                            if (submissionFailure != null)
                            {
                                report.Add(index, step, CastExecutionStatus.FailedSubmission,
                                    "instant-exception:" + submissionFailure.GetType().FullName + ":" +
                                    submissionFailure.Message);
                            }
                            else if (result == null)
                                report.Add(index, step, CastExecutionStatus.FailedSubmission, "instant-result-null");
                            else
                            {
                                if (result.Submitted)
                                {
                                    report.Add(index, step, CastExecutionStatus.Submitted, "rule-cast-submitted");
                                    report.Add(index, step, CastExecutionStatus.CastStarted, "rule-cast-started");
                                }
                                if (result.ResourceSpent)
                                    report.Add(index, step, CastExecutionStatus.ResourceSpent,
                                        "ability-data-spend-completed");
                                if (!result.Submitted)
                                    report.Add(index, step, CastExecutionStatus.FailedSubmission, result.Detail);
                                else if (!result.Succeeded)
                                    report.Add(index, step, CastExecutionStatus.FailedExecution, result.Detail);
                                else
                                {
                                    bool observed = result.EffectsObserved;
                                    for (int confirmationFrame = 0;
                                        !observed && confirmationFrame < 12; confirmationFrame++)
                                    {
                                        yield return null;
                                        observed = _runtime.EffectsObserved(step);
                                    }
                                    report.Add(index, step, observed
                                        ? CastExecutionStatus.EffectConfirmed
                                        : CastExecutionStatus.TimedOutUnconfirmed,
                                        (observed ? "expected-effects-observed;" :
                                            "expected-effects-absent-after-confirmation-window;") + result.Detail);
                                }
                            }
                        }
                    }
                }
                sinceYield++;
                if (sinceYield >= _batchSize)
                {
                    sinceYield = 0;
                    yield return null;
                }
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
