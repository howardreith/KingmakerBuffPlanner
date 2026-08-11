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
                    report.Add(index, step, CastExecutionStatus.Failed, "combat-policy");
                    continue;
                }
                CastRuntimeValidation validation = _runtime.Validate(step);
                if (!validation.Valid)
                {
                    report.Add(index, step, CastExecutionStatus.Failed, validation.Reason);
                    continue;
                }
                IAnimatedCastOperation operation;
                try { operation = _runtime.StartAnimated(step); }
                catch (Exception exception)
                {
                    report.Add(index, step, CastExecutionStatus.Failed,
                        "start-exception:" + exception.GetType().FullName + ":" + exception.Message);
                    continue;
                }
                if (operation == null)
                {
                    report.Add(index, step, CastExecutionStatus.Failed, "operation-null");
                    continue;
                }
                report.Add(index, step, CastExecutionStatus.Fired, "animated-command-queued");
                while (!operation.IsCompleted) yield return null;
                report.Add(index, step,
                    operation.Succeeded ? CastExecutionStatus.Succeeded : CastExecutionStatus.Failed,
                    operation.Detail);
                if (operation.Succeeded && operation.EffectsObserved)
                    report.Add(index, step, CastExecutionStatus.Observed, "expected-effects-observed");
                yield return null;
            }
        }
    }
}
