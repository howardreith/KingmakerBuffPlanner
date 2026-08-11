using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    public enum CastExecutionStatus
    {
        Fired,
        Succeeded,
        Observed,
        Failed
    }

    public sealed class CastExecutionRecord
    {
        internal CastExecutionRecord(int stepIndex, string providerKey, CastExecutionStatus status, string detail)
        {
            StepIndex = stepIndex;
            ProviderKey = providerKey;
            Status = status;
            Detail = detail ?? string.Empty;
        }

        public int StepIndex { get; private set; }
        public string ProviderKey { get; private set; }
        public CastExecutionStatus Status { get; private set; }
        public string Detail { get; private set; }
    }

    public sealed class ExecutionReport
    {
        private readonly List<CastExecutionRecord> _records = new List<CastExecutionRecord>();

        public ExecutionReport(CastPlan plan)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            Planned = plan.Steps.Count;
            Skipped = plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.SkippedAlreadyActive);
            Unfulfilled = plan.Outcomes.Count(o => o.Kind == TargetOutcomeKind.Unfulfilled);
        }

        public int Planned { get; private set; }
        public int Skipped { get; private set; }
        public int Unfulfilled { get; private set; }
        public IReadOnlyList<CastExecutionRecord> Records
        {
            get { return new ReadOnlyCollection<CastExecutionRecord>(_records); }
        }
        public int Fired { get { return _records.Count(r => r.Status == CastExecutionStatus.Fired); } }
        public int Succeeded { get { return _records.Count(r => r.Status == CastExecutionStatus.Succeeded); } }
        public int SuccessfullyObserved { get { return _records.Count(r => r.Status == CastExecutionStatus.Observed); } }
        public int Failed { get { return _records.Count(r => r.Status == CastExecutionStatus.Failed); } }

        internal void Add(int stepIndex, CastStep step, CastExecutionStatus status, string detail)
        {
            _records.Add(new CastExecutionRecord(stepIndex, step.Provider.Canonical, status, detail));
        }
    }

    public sealed class CastRuntimeValidation
    {
        private CastRuntimeValidation(bool valid, string reason)
        {
            Valid = valid;
            Reason = reason ?? string.Empty;
        }

        public bool Valid { get; private set; }
        public string Reason { get; private set; }
        public static CastRuntimeValidation Pass() { return new CastRuntimeValidation(true, string.Empty); }
        public static CastRuntimeValidation Fail(string reason) { return new CastRuntimeValidation(false, reason); }
    }

    public interface IAnimatedCastOperation
    {
        bool IsCompleted { get; }
        bool Succeeded { get; }
        bool EffectsObserved { get; }
        string Detail { get; }
    }

    public interface ICastRuntimeAdapter
    {
        bool IsInCombat { get; }
        CastRuntimeValidation Validate(CastStep step);
        IAnimatedCastOperation StartAnimated(CastStep step);
    }

    public interface ICastExecutor
    {
        IEnumerator Execute(CastPlan plan, ExecutionReport report);
    }
}
