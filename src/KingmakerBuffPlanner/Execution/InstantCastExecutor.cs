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
            bool priorTransactionUnsettled = false;
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                report.Add(index, step, CastExecutionStatus.StrategySelected,
                    "configured-mode:instant;selected-strategy:" +
                    step.ExecutionStrategy + ";reason:" +
                    step.ExecutionStrategyReason);
                if (priorTransactionUnsettled)
                    report.Add(index, step, CastExecutionStatus.FailedValidation,
                        "prior-instant-transaction-unsettled");
                else if (_outOfCombatOnly && _runtime.IsInCombat)
                    report.Add(index, step, CastExecutionStatus.FailedValidation, "combat-policy");
                else
                {
                    CastEnhancementPreparation enhancement = Prepare(step);
                    if (!enhancement.Valid)
                        report.Add(index, step, CastExecutionStatus.FailedValidation,
                            "enhancement-unavailable:" + enhancement.Reason);
                    else
                    {
                        try
                        {
                            CastRuntimeValidation validation;
                            try { validation = _runtime.Validate(step); }
                            catch (Exception exception)
                            {
                                validation = CastRuntimeValidation.Fail(
                                    "validation-exception:" +
                                    exception.GetType().FullName + ":" +
                                    exception.Message);
                            }
                            if (!validation.Valid)
                                report.Add(index, step,
                                    CastExecutionStatus.FailedValidation,
                                    validation.Reason);
                            else
                            {
                                InstantCastResult result = null;
                                Exception submissionFailure = null;
                                try { result = _runtime.Fire(step); }
                                catch (Exception exception)
                                {
                                    submissionFailure = exception;
                                }
                                if (submissionFailure != null)
                                {
                                    InstantCastCompletion failedCleanup =
                                        Cleanup(step);
                                    report.Add(index, step,
                                        CastExecutionStatus.FailedSubmission,
                                        "instant-exception:" +
                                        submissionFailure.GetType().FullName + ":" +
                                        submissionFailure.Message +
                                        ";cleanup-complete:" +
                                        failedCleanup.Complete +
                                        ";cleanup-state:" +
                                        failedCleanup.Detail);
                                    if (!failedCleanup.Complete &&
                                        failedCleanup.ResidualDeliveryState)
                                    {
                                        priorTransactionUnsettled = true;
                                        report.Add(index, step,
                                            CastExecutionStatus
                                                .ResidualStateUnsettled,
                                            failedCleanup.Detail);
                                    }
                                }
                                else if (result == null)
                                {
                                    InstantCastCompletion nullCleanup =
                                        Cleanup(step);
                                    report.Add(index, step,
                                        CastExecutionStatus.FailedSubmission,
                                        "instant-result-null;cleanup-complete:" +
                                        nullCleanup.Complete +
                                        ";cleanup-state:" +
                                        nullCleanup.Detail);
                                    if (!nullCleanup.Complete &&
                                        nullCleanup.ResidualDeliveryState)
                                    {
                                        priorTransactionUnsettled = true;
                                        report.Add(index, step,
                                            CastExecutionStatus
                                                .ResidualStateUnsettled,
                                            nullCleanup.Detail);
                                    }
                                }
                                else
                                {
                                    if (result.Submitted)
                                    {
                                        report.Add(index, step,
                                            CastExecutionStatus.Submitted,
                                            "rule-cast-submitted");
                                        report.Add(index, step,
                                            CastExecutionStatus.CastStarted,
                                            "rule-cast-started");
                                    }
                                    if (result.SpendInvoked)
                                        report.Add(index, step,
                                            CastExecutionStatus.SpendInvoked,
                                            "ability-data-spend-invoked");
                                    if (result.ResourceSpent)
                                        report.Add(index, step,
                                            CastExecutionStatus.ResourceSpent,
                                            "native-resource-delta-observed");

                                    bool observed = result.EffectsObserved;
                                    InstantCastCompletion completion = result.Submitted
                                        ? InspectCompletion(step)
                                        : InstantCastCompletion.Settled(
                                            "rule-cast-not-submitted");
                                    for (int confirmationFrame = 0;
                                        result.Submitted &&
                                        (!completion.Complete ||
                                         (result.Succeeded && !observed)) &&
                                        confirmationFrame < 12;
                                        confirmationFrame++)
                                    {
                                        yield return null;
                                        if (result.Succeeded && !observed)
                                            observed = _runtime.EffectsObserved(step);
                                        completion = InspectCompletion(step);
                                    }
                                    InstantCastCompletion cleanup = completion;
                                    if (!completion.Complete)
                                        cleanup = Cleanup(step);
                                    string terminalDetail = result.Detail +
                                        ";transaction-complete:" +
                                        completion.Complete +
                                        ";transaction-state:" +
                                        completion.Detail +
                                        ";cleanup-complete:" + cleanup.Complete +
                                        ";cleanup-state:" + cleanup.Detail;
                                    if (!result.Submitted)
                                        report.Add(index, step,
                                            CastExecutionStatus.FailedSubmission,
                                            terminalDetail);
                                    else if (!result.Succeeded)
                                        report.Add(index, step,
                                            CastExecutionStatus.FailedExecution,
                                            terminalDetail);
                                    else
                                        report.Add(index, step,
                                            observed && completion.Complete
                                                ? CastExecutionStatus.EffectConfirmed
                                                : CastExecutionStatus.TimedOutUnconfirmed,
                                            (observed
                                                ? "expected-effects-observed;"
                                                : "expected-effects-absent-after-confirmation-window;") +
                                            terminalDetail);
                                    if (!cleanup.Complete &&
                                        cleanup.ResidualDeliveryState)
                                    {
                                        priorTransactionUnsettled = true;
                                        report.Add(index, step,
                                            CastExecutionStatus
                                                .ResidualStateUnsettled,
                                            cleanup.Detail);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            enhancement.Dispose();
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

        private InstantCastCompletion InspectCompletion(CastStep step)
        {
            try
            {
                return _runtime.InspectCompletion(step) ??
                    InstantCastCompletion.Pending(
                        "completion-inspection-returned-null");
            }
            catch (Exception exception)
            {
                return InstantCastCompletion.Pending(
                    "completion-inspection-exception:" +
                    exception.GetType().FullName + ":" + exception.Message);
            }
        }

        private InstantCastCompletion Cleanup(CastStep step)
        {
            try
            {
                return _runtime.Cleanup(step) ??
                    InstantCastCompletion.Pending(
                        "completion-cleanup-returned-null");
            }
            catch (Exception exception)
            {
                return InstantCastCompletion.Pending(
                    "completion-cleanup-exception:" +
                    exception.GetType().FullName + ":" + exception.Message);
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
