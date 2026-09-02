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
        StrategySelected,
        Queued,
        Submitted,
        CastStarted,
        SpendInvoked,
        ResourceSpent,
        EffectConfirmed,
        SkippedExisting,
        FailedValidation,
        FailedSubmission,
        FailedExecution,
        TimedOutUnconfirmed,
        ResidualStateUnsettled
    }

    public sealed class CastExecutionRecord
    {
        internal CastExecutionRecord(int stepIndex, CastStep step, CastExecutionStatus status, string detail)
        {
            StepIndex = stepIndex;
            SourceId = step.SourceId;
            ProviderKey = step.Provider.Canonical;
            AbilityKey = step.Provider.Ability.Canonical;
            CasterUnitId = step.Provider.CasterUnitId;
            TargetUnitIds = new ReadOnlyCollection<string>(step.TargetUnitIds.ToList());
            ExpectedRecipientUnitIds = new ReadOnlyCollection<string>(
                step.ExpectedRecipientUnitIds.ToList());
            ResourcePoolKey = step.Reservation == null ? string.Empty : step.Reservation.PoolKey;
            ResourceTokenIds = new ReadOnlyCollection<string>(step.Reservation == null
                ? new List<string>() : step.Reservation.TokenIds.ToList());
            EnhancementIds = new ReadOnlyCollection<string>(step.EnhancementIds.ToList());
            ExecutionStrategy = step.ExecutionStrategy;
            ExecutionStrategyReason = step.ExecutionStrategyReason;
            Status = status;
            Detail = detail ?? string.Empty;
        }

        public int StepIndex { get; private set; }
        public string SourceId { get; private set; }
        public string ProviderKey { get; private set; }
        public string AbilityKey { get; private set; }
        public string CasterUnitId { get; private set; }
        public IReadOnlyList<string> TargetUnitIds { get; private set; }
        public IReadOnlyList<string> ExpectedRecipientUnitIds { get; private set; }
        public string ResourcePoolKey { get; private set; }
        public IReadOnlyList<string> ResourceTokenIds { get; private set; }
        public IReadOnlyList<string> EnhancementIds { get; private set; }
        public CastExecutionStrategy ExecutionStrategy { get; private set; }
        public string ExecutionStrategyReason { get; private set; }
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
        public int SpendInvocations { get { return _records.Count(r => r.Status == CastExecutionStatus.SpendInvoked); } }
        public int Confirmed { get { return _records.Count(r => r.Status == CastExecutionStatus.EffectConfirmed); } }
        public int SuccessfullyObserved { get { return Confirmed; } }
        public int ResourcesSpent { get { return _records.Count(r => r.Status == CastExecutionStatus.ResourceSpent); } }
        public int Failed
        {
            get
            {
                return _records.Where(r =>
                        r.Status == CastExecutionStatus.FailedValidation ||
                        r.Status == CastExecutionStatus.FailedSubmission ||
                        r.Status == CastExecutionStatus.FailedExecution ||
                        r.Status == CastExecutionStatus.TimedOutUnconfirmed ||
                        r.Status == CastExecutionStatus.ResidualStateUnsettled)
                    .Select(r => r.StepIndex).Distinct().Count();
            }
        }

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

    public interface IAnimatedCastOperation : IDisposable
    {
        bool IsCompleted { get; }
        bool IsStarted { get; }
        bool TimedOut { get; }
        bool Succeeded { get; }
        bool EffectsObserved { get; }
        bool ResourceSpent { get; }
        bool HasResidualDeliveryState { get; }
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
            : this(submitted, succeeded, effectsObserved, resourceSpent,
                resourceSpent, detail)
        {
        }

        public InstantCastResult(bool submitted, bool succeeded,
            bool effectsObserved, bool resourceSpent, bool spendInvoked,
            string detail)
        {
            Submitted = submitted;
            Succeeded = succeeded;
            EffectsObserved = effectsObserved;
            ResourceSpent = resourceSpent;
            SpendInvoked = spendInvoked;
            Detail = detail ?? string.Empty;
        }
        public bool Submitted { get; private set; }
        public bool Succeeded { get; private set; }
        public bool EffectsObserved { get; private set; }
        public bool ResourceSpent { get; private set; }
        public bool SpendInvoked { get; private set; }
        public string Detail { get; private set; }
    }

    public sealed class InstantCastCompletion
    {
        private InstantCastCompletion(bool complete,
            bool residualDeliveryState, string detail)
        {
            Complete = complete;
            ResidualDeliveryState = residualDeliveryState;
            Detail = detail ?? string.Empty;
        }

        public bool Complete { get; private set; }
        public bool ResidualDeliveryState { get; private set; }
        public string Detail { get; private set; }

        public static InstantCastCompletion Settled(string detail)
        {
            return new InstantCastCompletion(true, false, detail);
        }

        public static InstantCastCompletion Pending(string detail)
        {
            return new InstantCastCompletion(false, true, detail);
        }
    }

    public static class RuleCastSpendPolicy
    {
        public static bool ShouldInvokeSpend(bool submitted, bool umdFailed)
        {
            return submitted && !umdFailed;
        }
    }

    public sealed class AnimatedStickyTouchLifecycleSnapshot
    {
        public AnimatedStickyTouchLifecycleSnapshot(
            bool carrierFinished,
            bool carrierSucceeded,
            bool deliveryExpected,
            bool deliveryIdentified,
            bool deliveryFinished,
            bool deliverySucceeded,
            bool heldTouch,
            bool effectsObserved)
        {
            CarrierFinished = carrierFinished;
            CarrierSucceeded = carrierSucceeded;
            DeliveryExpected = deliveryExpected;
            DeliveryIdentified = deliveryIdentified;
            DeliveryFinished = deliveryFinished;
            DeliverySucceeded = deliverySucceeded;
            HeldTouch = heldTouch;
            EffectsObserved = effectsObserved;
        }

        public bool CarrierFinished { get; private set; }
        public bool CarrierSucceeded { get; private set; }
        public bool DeliveryExpected { get; private set; }
        public bool DeliveryIdentified { get; private set; }
        public bool DeliveryFinished { get; private set; }
        public bool DeliverySucceeded { get; private set; }
        public bool HeldTouch { get; private set; }
        public bool EffectsObserved { get; private set; }
    }

    public sealed class AnimatedStickyTouchLifecycleDecision
    {
        internal AnimatedStickyTouchLifecycleDecision(bool complete,
            bool succeeded, bool timedOut, string detail)
        {
            Complete = complete;
            Succeeded = succeeded;
            TimedOut = timedOut;
            Detail = detail ?? string.Empty;
        }

        public bool Complete { get; private set; }
        public bool Succeeded { get; private set; }
        public bool TimedOut { get; private set; }
        public string Detail { get; private set; }
    }

    public sealed class AnimatedStickyTouchLifecycle
    {
        private readonly int _maximumFrames;
        private readonly int _confirmationFrames;
        private int _frames;
        private int _settlingFrames;

        public AnimatedStickyTouchLifecycle(int maximumFrames)
            : this(maximumFrames, 12)
        {
        }

        public AnimatedStickyTouchLifecycle(int maximumFrames,
            int confirmationFrames)
        {
            if (maximumFrames < 1) throw new ArgumentOutOfRangeException("maximumFrames");
            if (confirmationFrames < 1)
                throw new ArgumentOutOfRangeException("confirmationFrames");
            _maximumFrames = maximumFrames;
            _confirmationFrames = confirmationFrames;
        }

        public AnimatedStickyTouchLifecycleDecision Observe(
            AnimatedStickyTouchLifecycleSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            _frames++;
            if (snapshot.CarrierFinished && !snapshot.CarrierSucceeded)
                return Failed("carrier-command-failed");
            if (snapshot.CarrierFinished && snapshot.DeliveryExpected &&
                snapshot.DeliveryIdentified && snapshot.DeliveryFinished &&
                !snapshot.DeliverySucceeded)
                return Failed("delivery-command-failed");

            string pending;
            if (!snapshot.CarrierFinished)
                pending = "carrier-command-running";
            else if (snapshot.DeliveryExpected &&
                !snapshot.DeliveryIdentified)
                pending = "delivery-command-not-yet-identified";
            else if (snapshot.DeliveryExpected &&
                snapshot.DeliveryIdentified && !snapshot.DeliveryFinished)
                pending = "delivery-command-running";
            else if (snapshot.HeldTouch)
                pending = "held-touch-not-released";
            else if (!snapshot.EffectsObserved)
                pending = "expected-effects-not-observed";
            else
                return new AnimatedStickyTouchLifecycleDecision(true, true,
                    false, "sticky-touch-lifecycle-complete");

            bool settlementOnly = snapshot.CarrierFinished &&
                snapshot.CarrierSucceeded &&
                (!snapshot.DeliveryExpected ||
                    (snapshot.DeliveryIdentified &&
                        snapshot.DeliveryFinished &&
                        snapshot.DeliverySucceeded)) &&
                !snapshot.HeldTouch && !snapshot.EffectsObserved;
            if (settlementOnly) _settlingFrames++;
            else _settlingFrames = 0;
            if (_frames >= _maximumFrames ||
                _settlingFrames >= _confirmationFrames)
                return new AnimatedStickyTouchLifecycleDecision(true, false,
                    true, "sticky-touch-lifecycle-timeout:" + pending);
            return new AnimatedStickyTouchLifecycleDecision(false, false,
                false, pending);
        }

        private static AnimatedStickyTouchLifecycleDecision Failed(string detail)
        {
            return new AnimatedStickyTouchLifecycleDecision(true, false,
                false, detail);
        }
    }

    public interface IInstantCastRuntimeAdapter
    {
        bool IsInCombat { get; }
        CastRuntimeValidation Validate(CastStep step);
        InstantCastResult Fire(CastStep step);
        bool EffectsObserved(CastStep step);
        InstantCastCompletion InspectCompletion(CastStep step);
        InstantCastCompletion Cleanup(CastStep step);
    }
}
