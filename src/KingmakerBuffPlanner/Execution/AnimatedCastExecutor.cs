using System;
using System.Collections;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    public sealed class AnimatedCastExecutor : ICastExecutor
    {
        private readonly ICastRuntimeAdapter _runtime;
        private readonly bool _outOfCombatOnly;
        private readonly string _configuredMode;

        public AnimatedCastExecutor(ICastRuntimeAdapter runtime,
            bool outOfCombatOnly, string configuredMode = "animated")
        {
            _runtime = runtime ?? throw new ArgumentNullException("runtime");
            _outOfCombatOnly = outOfCombatOnly;
            _configuredMode = configuredMode ?? "animated";
        }

        public IEnumerator Execute(CastPlan plan, ExecutionReport report)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (report == null) throw new ArgumentNullException("report");
            bool priorTransactionUnsettled = false;
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                report.Add(index, step, CastExecutionStatus.StrategySelected,
                    "configured-mode:" + _configuredMode +
                    ";selected-strategy:native-command;provider-capability:" +
                    step.ExecutionStrategy + ";reason:" +
                    step.ExecutionStrategyReason);
                if (priorTransactionUnsettled)
                {
                    report.Add(index, step, CastExecutionStatus.FailedValidation,
                        "prior-animated-transaction-unsettled");
                    continue;
                }
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
                CastRuntimeValidation validation;
                try { validation = _runtime.Validate(step); }
                catch (Exception exception)
                {
                    validation = CastRuntimeValidation.Fail(
                        "validation-exception:" + exception.GetType().FullName +
                        ":" + exception.Message);
                }
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
                Exception cleanupFailure = null;
                Exception operationFailure = null;
                try
                {
                    report.Add(index, step, CastExecutionStatus.Queued, "animated-command-queued");
                    bool startedRecorded = false;
                    while (true)
                    {
                        bool completed = false;
                        try
                        {
                            completed = operation.IsCompleted;
                            if (!startedRecorded && operation.IsStarted)
                            {
                                report.Add(index, step,
                                    CastExecutionStatus.CastStarted,
                                    "animated-command-started");
                                startedRecorded = true;
                            }
                        }
                        catch (Exception exception)
                        { operationFailure = exception; }
                        if (completed || operationFailure != null) break;
                        yield return null;
                    }
                    if (operationFailure == null)
                    {
                        try
                        {
                            if (!startedRecorded && operation.IsStarted)
                                report.Add(index, step,
                                    CastExecutionStatus.CastStarted,
                                    "animated-command-started");
                            if (operation.TimedOut)
                                report.Add(index, step,
                                    CastExecutionStatus.TimedOutUnconfirmed,
                                    operation.Detail);
                            else if (!operation.Succeeded)
                                report.Add(index, step,
                                    CastExecutionStatus.FailedExecution,
                                    operation.Detail);
                            else if (operation.EffectsObserved)
                                report.Add(index, step,
                                    CastExecutionStatus.EffectConfirmed,
                                    "expected-effects-observed;" +
                                    operation.Detail);
                            else
                                report.Add(index, step,
                                    CastExecutionStatus.TimedOutUnconfirmed,
                                    "expected-effects-absent;" +
                                    operation.Detail);
                            if (operation.Succeeded &&
                                operation.ResourceSpent)
                                report.Add(index, step,
                                    CastExecutionStatus.ResourceSpent,
                                    "native-command-spend-completed");
                        }
                        catch (Exception exception)
                        { operationFailure = exception; }
                    }
                    if (operationFailure != null)
                        report.Add(index, step,
                            CastExecutionStatus.FailedExecution,
                            "animated-operation-exception:" +
                            operationFailure.GetType().FullName + ":" +
                            operationFailure.Message);
                }
                finally
                {
                    try { operation.Dispose(); }
                    catch (Exception exception)
                    {
                        cleanupFailure = exception;
                    }
                    finally { enhancement.Dispose(); }
                }
                try
                {
                    if (cleanupFailure != null ||
                        operation.HasResidualDeliveryState)
                    {
                        priorTransactionUnsettled = true;
                        report.Add(index, step,
                            CastExecutionStatus.ResidualStateUnsettled,
                            cleanupFailure == null
                                ? "animated-delivery-state-remained-after-cleanup;" +
                                    operation.Detail
                                : "animated-cleanup-exception:" +
                                    cleanupFailure.GetType().FullName + ":" +
                                    cleanupFailure.Message);
                    }
                }
                catch (Exception exception)
                {
                    priorTransactionUnsettled = true;
                    report.Add(index, step,
                        CastExecutionStatus.ResidualStateUnsettled,
                        "animated-residual-inspection-exception:" +
                        exception.GetType().FullName + ":" +
                        exception.Message);
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
