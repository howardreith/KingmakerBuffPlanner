using System;
using System.Collections;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    public sealed class HybridCastExecutor : ICastExecutor
    {
        private readonly IInstantCastRuntimeAdapter _instantRuntime;
        private readonly ICastRuntimeAdapter _animatedRuntime;
        private readonly Func<CastStep, bool> _requiresAnimated;
        private readonly Func<CastStep, bool> _requiresNativeCommand;
        private readonly bool _allowAnimatedFallback;
        private readonly bool _outOfCombatOnly;

        public HybridCastExecutor(
            IInstantCastRuntimeAdapter instantRuntime,
            ICastRuntimeAdapter animatedRuntime,
            Func<CastStep, bool> requiresAnimated,
            bool allowAnimatedFallback,
            bool outOfCombatOnly,
            Func<CastStep, bool> requiresNativeCommand = null)
        {
            _instantRuntime = instantRuntime ?? throw new ArgumentNullException("instantRuntime");
            _animatedRuntime = animatedRuntime ?? throw new ArgumentNullException("animatedRuntime");
            _requiresAnimated = requiresAnimated ?? throw new ArgumentNullException("requiresAnimated");
            _requiresNativeCommand = requiresNativeCommand ?? (step => false);
            _allowAnimatedFallback = allowAnimatedFallback;
            _outOfCombatOnly = outOfCombatOnly;
        }

        public IEnumerator Execute(CastPlan plan, ExecutionReport report)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (report == null) throw new ArgumentNullException("report");
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                bool mandatoryNativeCommand = _requiresNativeCommand(step);
                bool fallback = mandatoryNativeCommand || _requiresAnimated(step);
                if (fallback && !mandatoryNativeCommand &&
                    !_allowAnimatedFallback)
                {
                    report.Add(index, step, CastExecutionStatus.FailedValidation,
                        "animated-fallback-disabled");
                    continue;
                }
                ICastExecutor executor = fallback
                    ? (ICastExecutor)new AnimatedCastExecutor(_animatedRuntime, _outOfCombatOnly)
                    : new InstantCastExecutor(_instantRuntime, _outOfCombatOnly, 1);
                var singlePlan = new CastPlan(new[] { step }, new TargetPlanOutcome[0], new string[0]);
                var partial = new ExecutionReport(singlePlan);
                IEnumerator work = executor.Execute(singlePlan, partial);
                while (work.MoveNext()) yield return work.Current;
                foreach (CastExecutionRecord record in partial.Records)
                    report.Add(index, step, record.Status, record.Detail);
            }
        }
    }
}
