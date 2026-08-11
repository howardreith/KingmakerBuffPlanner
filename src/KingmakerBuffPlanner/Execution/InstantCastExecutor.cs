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
                    report.Add(index, step, CastExecutionStatus.Failed, "combat-policy");
                else
                {
                    CastRuntimeValidation validation = _runtime.Validate(step);
                    if (!validation.Valid)
                        report.Add(index, step, CastExecutionStatus.Failed, validation.Reason);
                    else
                    {
                        try
                        {
                            InstantCastResult result = _runtime.Fire(step);
                            if (result == null)
                                report.Add(index, step, CastExecutionStatus.Failed, "instant-result-null");
                            else
                            {
                                if (result.Fired)
                                    report.Add(index, step, CastExecutionStatus.Fired, "rule-cast-triggered");
                                report.Add(index, step,
                                    result.Succeeded ? CastExecutionStatus.Succeeded : CastExecutionStatus.Failed,
                                    result.Detail);
                                if (result.Succeeded && result.EffectsObserved)
                                    report.Add(index, step, CastExecutionStatus.Observed, "expected-effects-observed");
                                if (result.ResourceSpent)
                                    report.Add(index, step, CastExecutionStatus.ResourceSpent, "ability-data-spend-completed");
                            }
                        }
                        catch (Exception exception)
                        {
                            report.Add(index, step, CastExecutionStatus.Failed,
                                "instant-exception:" + exception.GetType().FullName + ":" + exception.Message);
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
    }
}
