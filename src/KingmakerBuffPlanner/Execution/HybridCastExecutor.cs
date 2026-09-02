using System;
using System.Collections;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    public sealed class HybridCastExecutor : ICastExecutor
    {
        private readonly IInstantCastRuntimeAdapter _instantRuntime;
        private readonly ICastRuntimeAdapter _animatedRuntime;
        private readonly Func<CastStep, bool> _legacyRequiresAnimated;
        private readonly Func<CastStep, bool> _requiresNativeCommand;
        private readonly bool _allowAnimatedFallback;
        private readonly bool _outOfCombatOnly;

        public HybridCastExecutor(
            IInstantCastRuntimeAdapter instantRuntime,
            ICastRuntimeAdapter animatedRuntime,
            bool allowAnimatedFallback,
            bool outOfCombatOnly,
            Func<CastStep, bool> requiresNativeCommand = null)
            : this(instantRuntime, animatedRuntime, null,
                allowAnimatedFallback, outOfCombatOnly,
                requiresNativeCommand)
        {
        }

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
            _legacyRequiresAnimated = requiresAnimated;
            _requiresNativeCommand = requiresNativeCommand ?? (step => false);
            _allowAnimatedFallback = allowAnimatedFallback;
            _outOfCombatOnly = outOfCombatOnly;
        }

        public IEnumerator Execute(CastPlan plan, ExecutionReport report)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            if (report == null) throw new ArgumentNullException("report");
            bool priorTransactionUnsettled = false;
            for (int index = 0; index < plan.Steps.Count; index++)
            {
                CastStep step = plan.Steps[index];
                if (priorTransactionUnsettled)
                {
                    report.Add(index, step,
                        CastExecutionStatus.StrategySelected,
                        "configured-mode:instant;selected-strategy:" +
                        step.ExecutionStrategy + ";reason:" +
                        step.ExecutionStrategyReason);
                    report.Add(index, step,
                        CastExecutionStatus.FailedValidation,
                        "prior-hybrid-transaction-unsettled");
                    continue;
                }
                bool mandatoryNativeCommand = _requiresNativeCommand(step) ||
                    step.ExecutionStrategy ==
                        CastExecutionStrategy.NativeCommandRequired;
                bool animatedFallback = step.ExecutionStrategy ==
                        CastExecutionStrategy.AnimatedFallback ||
                    (_legacyRequiresAnimated != null &&
                        _legacyRequiresAnimated(step));
                bool useAnimated = mandatoryNativeCommand || animatedFallback;
                if (animatedFallback && !mandatoryNativeCommand &&
                    !_allowAnimatedFallback)
                {
                    report.Add(index, step,
                        CastExecutionStatus.StrategySelected,
                        "configured-mode:instant;selected-strategy:" +
                        step.ExecutionStrategy + ";reason:" +
                        step.ExecutionStrategyReason);
                    report.Add(index, step, CastExecutionStatus.FailedValidation,
                        "animated-fallback-disabled");
                    continue;
                }
                ICastExecutor executor = useAnimated
                    ? (ICastExecutor)new AnimatedCastExecutor(
                        _animatedRuntime, _outOfCombatOnly, "instant")
                    : new InstantCastExecutor(_instantRuntime, _outOfCombatOnly, 1);
                var singlePlan = new CastPlan(new[] { step }, new TargetPlanOutcome[0], new string[0]);
                var partial = new ExecutionReport(singlePlan);
                IEnumerator work = executor.Execute(singlePlan, partial);
                while (work.MoveNext()) yield return work.Current;
                foreach (CastExecutionRecord record in partial.Records)
                    report.Add(index, step, record.Status, record.Detail);
                priorTransactionUnsettled = partial.Records.Any(record =>
                    record.Status ==
                        CastExecutionStatus.ResidualStateUnsettled);
            }
        }
    }
}
