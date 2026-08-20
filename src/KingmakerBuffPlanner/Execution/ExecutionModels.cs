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
        Selected,
        Planned,
        Queued,
        Submitted,
        CastStarted,
        ResourceSpent,
        EffectConfirmed,
        SkippedExisting,
        FailedValidation,
        FailedSubmission,
        FailedExecution,
        TimedOutUnconfirmed
    }

    public sealed class CastExecutionRecord
    {
        internal CastExecutionRecord(int stepIndex, CastStep step, CastExecutionStatus status, string detail)
        {
            StepIndex = stepIndex;
            ProviderKey = step.Provider.Canonical;
            AbilityKey = step.Provider.Ability.Canonical;
            TargetUnitIds = new ReadOnlyCollection<string>(step.TargetUnitIds.ToList());
            ResourcePoolKey = step.Reservation == null ? string.Empty : step.Reservation.PoolKey;
            ResourceTokenIds = new ReadOnlyCollection<string>(step.Reservation == null
                ? new List<string>() : step.Reservation.TokenIds.ToList());
            EnhancementIds = new ReadOnlyCollection<string>(step.EnhancementIds.ToList());
            Status = status;
            Detail = detail ?? string.Empty;
        }

        public int StepIndex { get; private set; }
        public string ProviderKey { get; private set; }
        public string AbilityKey { get; private set; }
        public IReadOnlyList<string> TargetUnitIds { get; private set; }
        public string ResourcePoolKey { get; private set; }
        public IReadOnlyList<string> ResourceTokenIds { get; private set; }
        public IReadOnlyList<string> EnhancementIds { get; private set; }
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
        public int Queued { get { return _records.Count(r => r.Status == CastExecutionStatus.Queued); } }
        public int Submitted { get { return _records.Count(r => r.Status == CastExecutionStatus.Submitted); } }
        public int CastStarted { get { return _records.Count(r => r.Status == CastExecutionStatus.CastStarted); } }
        public int Confirmed { get { return _records.Count(r => r.Status == CastExecutionStatus.EffectConfirmed); } }
        public int SuccessfullyObserved { get { return Confirmed; } }
        public int ResourcesSpent { get { return _records.Count(r => r.Status == CastExecutionStatus.ResourceSpent); } }
        public int Failed { get { return _records.Count(r => r.Status == CastExecutionStatus.FailedValidation ||
            r.Status == CastExecutionStatus.FailedSubmission ||
            r.Status == CastExecutionStatus.FailedExecution ||
            r.Status == CastExecutionStatus.TimedOutUnconfirmed); } }

        internal void Add(int stepIndex, CastStep step, CastExecutionStatus status, string detail)
        {
            _records.Add(new CastExecutionRecord(stepIndex, step, status, detail));
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
        bool IsStarted { get; }
        bool TimedOut { get; }
        bool Succeeded { get; }
        bool EffectsObserved { get; }
        bool ResourceSpent { get; }
        string Detail { get; }
    }

    public interface ICastRuntimeAdapter
    {
        bool IsInCombat { get; }
        CastRuntimeValidation Validate(CastStep step);
        IAnimatedCastOperation StartAnimated(CastStep step);
    }

    public sealed class CastEnhancementPreparation : IDisposable
    {
        private CastEnhancementPreparation(bool valid, string reason, IDisposable lease)
        {
            Valid = valid;
            Reason = reason ?? string.Empty;
            _lease = lease;
        }

        private readonly IDisposable _lease;
        public bool Valid { get; private set; }
        public string Reason { get; private set; }
        public static CastEnhancementPreparation Pass(IDisposable lease)
        {
            return new CastEnhancementPreparation(true, string.Empty, lease);
        }
        public static CastEnhancementPreparation Fail(string reason)
        {
            return new CastEnhancementPreparation(false, reason, null);
        }
        public void Dispose()
        {
            if (_lease != null) _lease.Dispose();
        }
    }

    public interface ICastEnhancementRuntimeAdapter
    {
        CastEnhancementPreparation PrepareEnhancements(CastStep step);
    }

    public interface ICastExecutor
    {
        IEnumerator Execute(CastPlan plan, ExecutionReport report);
    }

    public sealed class InstantCastResult
    {
        public InstantCastResult(bool submitted, bool succeeded, bool effectsObserved, bool resourceSpent, string detail)
        {
            Submitted = submitted;
            Succeeded = succeeded;
            EffectsObserved = effectsObserved;
            ResourceSpent = resourceSpent;
            Detail = detail ?? string.Empty;
        }
        public bool Submitted { get; private set; }
        public bool Succeeded { get; private set; }
        public bool EffectsObserved { get; private set; }
        public bool ResourceSpent { get; private set; }
        public string Detail { get; private set; }
    }

    public interface IInstantCastRuntimeAdapter
    {
        bool IsInCombat { get; }
        CastRuntimeValidation Validate(CastStep step);
        InstantCastResult Fire(CastStep step);
        bool EffectsObserved(CastStep step);
    }
}
