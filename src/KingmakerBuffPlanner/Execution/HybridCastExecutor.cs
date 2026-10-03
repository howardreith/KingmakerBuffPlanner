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
        private readonly Action<int, CastStep, bool, string> _routeSelected;

        public HybridCastExecutor(
            IInstantCastRuntimeAdapter instantRuntime,
            ICastRuntimeAdapter animatedRuntime,
            bool allowAnimatedFallback,
            bool outOfCombatOnly,
            Func<CastStep, bool> requiresNativeCommand = null,
            Action<int, CastStep, bool, string> routeSelected = null)
            : this(instantRuntime, animatedRuntime, null,
                allowAnimatedFallback, outOfCombatOnly,
                requiresNativeCommand, routeSelected)
        {
        }

        public HybridCastExecutor(
            IInstantCastRuntimeAdapter instantRuntime,
            ICastRuntimeAdapter animatedRuntime,
            Func<CastStep, bool> requiresAnimated,
            bool allowAnimatedFallback,
            bool outOfCombatOnly,
            Func<CastStep, bool> requiresNativeCommand = null,
            Action<int, CastStep, bool, string> routeSelected = null)
        {
            _instantRuntime = instantRuntime ?? throw new ArgumentNullException("instantRuntime");
            _animatedRuntime = animatedRuntime ?? throw new ArgumentNullException("animatedRuntime");
            _legacyRequiresAnimated = requiresAnimated;
            _requiresNativeCommand = requiresNativeCommand ?? (step => false);
            _allowAnimatedFallback = allowAnimatedFallback;
            _outOfCombatOnly = outOfCombatOnly;
            _routeSelected = routeSelected;
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
                bool nativeCallback = _requiresNativeCommand(step);
                bool nativeStrategy = step.ExecutionStrategy ==
                    CastExecutionStrategy.NativeCommandRequired;
                bool fallbackStrategy = step.ExecutionStrategy ==
                    CastExecutionStrategy.AnimatedFallback;
                bool legacyCallback = !fallbackStrategy &&
                    _legacyRequiresAnimated != null && _legacyRequiresAnimated(step);
                bool mandatoryNativeCommand = nativeCallback || nativeStrategy;
                bool animatedFallback = fallbackStrategy || legacyCallback;
                bool useAnimated = mandatoryNativeCommand || animatedFallback;
                bool refused = animatedFallback && !mandatoryNativeCommand &&
                    !_allowAnimatedFallback;
                string route = "configured-mode:instant;planned-strategy:" +
                    step.ExecutionStrategy + ";actual-executor:" +
                    (refused ? "Refused" : useAnimated ? "Animated" : "Instant") +
                    ";native-callback:" + nativeCallback +
                    ";native-strategy:" + nativeStrategy +
                    ";legacy-callback:" + legacyCallback +
                    ";fallback-strategy:" + fallbackStrategy +
                    ";allow-animated-fallback:" + _allowAnimatedFallback +
                    ";reason:" + step.ExecutionStrategyReason;
                report.Add(index, step, CastExecutionStatus.ExecutorSelected, route);
                if (_routeSelected != null)
                    _routeSelected(index, step, useAnimated && !refused, route);
                if (refused)
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
                        // Disposal can add cleanup failures. Copy them even
                        // when the outer iterator is cancelled or throws.
                        foreach (CastExecutionRecord record in partial.Records)
                            report.Add(index, step, record.Status, record.Detail);
                    }
                }
                priorTransactionUnsettled = partial.Records.Any(record =>
                    record.Status ==
                        CastExecutionStatus.ResidualStateUnsettled);
            }
        }
    }
}
