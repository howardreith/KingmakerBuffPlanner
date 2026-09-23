using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Execution
{
    // Review M2: authoritative probe observations. Every observation is a
    // FRESH native read stamped from one monotonic sequence at the moment of
    // the read, so a cached or reused reading cannot pass as a post-cast
    // observation. A failed read is recorded as failed (never replaced by an
    // older success); missing values are never zero.

    public sealed class ProbeSequenceClock
    {
        private long _value;
        public long Next() { return ++_value; }
    }

    // One instance of an expected effect on the target: the effect id, a
    // process-local instance identity, and its end time (ticks), so a new
    // application and a refresh of an existing one are both observable.
    public sealed class ProbeEffectInstance
    {
        public ProbeEffectInstance(string effectId, string instanceKey, long endTimeTicks)
        {
            EffectId = effectId ?? string.Empty;
            InstanceKey = instanceKey ?? string.Empty;
            EndTimeTicks = endTimeTicks;
        }

        public string EffectId { get; private set; }
        public string InstanceKey { get; private set; }
        public long EndTimeTicks { get; private set; }
        public override string ToString() { return EffectId + "#" + InstanceKey + "@" + EndTimeTicks; }
    }

    public sealed class ProbeObservation
    {
        private ProbeObservation() { }

        public string Phase { get; private set; }
        public long Sequence { get; private set; }
        public DateTime CapturedAtUtc { get; private set; }
        public bool Succeeded { get; private set; }
        public string Failure { get; private set; }
        public string TargetUnitId { get; private set; }
        // AbilityData.GetAvailableForCastCount at read time; null = not read.
        public int? AvailableForCast { get; private set; }
        // Expected-effect instances on the target; null = not read.
        public IReadOnlyList<ProbeEffectInstance> EffectInstances { get; private set; }

        public static ProbeObservation Read(string phase, long sequence, DateTime capturedAtUtc,
            string targetUnitId, int availableForCast, IEnumerable<ProbeEffectInstance> effectInstances)
        {
            return new ProbeObservation
            {
                Phase = phase, Sequence = sequence, CapturedAtUtc = capturedAtUtc, Succeeded = true,
                Failure = string.Empty, TargetUnitId = targetUnitId, AvailableForCast = availableForCast,
                EffectInstances = new ReadOnlyCollection<ProbeEffectInstance>(
                    (effectInstances ?? new ProbeEffectInstance[0]).Where(value => value != null).ToList())
            };
        }

        public static ProbeObservation Failed(string phase, long sequence, DateTime capturedAtUtc,
            string failure)
        {
            return new ProbeObservation
            {
                Phase = phase, Sequence = sequence, CapturedAtUtc = capturedAtUtc, Succeeded = false,
                Failure = string.IsNullOrEmpty(failure) ? "unknown" : failure
            };
        }

        public string Describe()
        {
            return Phase + "#" + Sequence + (Succeeded
                ? ";available=" + AvailableForCast + ";effects=[" +
                    string.Join(",", EffectInstances.Select(value => value.ToString()).ToArray()) + "]"
                : ";failed=" + Failure);
        }
    }

    // The native producer. The Unity adapter reads the caster's ability
    // availability and the target's buffs directly (never the UI's cached
    // discovery) and must call clock.Next() at the moment of its read.
    public interface IProbeObserver
    {
        ProbeObservation Observe(CastStep step, string phase, ProbeSequenceClock clock);
    }

    // Before -> submission -> after, with each outcome kept distinct:
    // invocation (the run outcome), effect result, resource delta, native
    // cleanup and transaction restoration are separate evidence.
    public sealed class SingleCastProbeObservationSession
    {
        private readonly IProbeObserver _observer;
        private readonly ProbeSequenceClock _clock;
        private readonly CastStep _step;

        public SingleCastProbeObservationSession(IProbeObserver observer, ProbeSequenceClock clock,
            CastStep step)
        {
            _observer = observer ?? throw new ArgumentNullException("observer");
            _clock = clock ?? throw new ArgumentNullException("clock");
            _step = step ?? throw new ArgumentNullException("step");
        }

        public ProbeObservation Before { get; private set; }
        public long? SubmissionSequence { get; private set; }
        public ProbeObservation After { get; private set; }

        public ProbeObservation ObserveBefore()
        {
            Before = Observe("before");
            return Before;
        }

        public void MarkSubmission()
        {
            SubmissionSequence = _clock.Next();
        }

        public ProbeObservation ObserveAfter()
        {
            After = Observe("after");
            return After;
        }

        private ProbeObservation Observe(string phase)
        {
            try
            {
                ProbeObservation observation = _observer.Observe(_step, phase, _clock);
                return observation ?? ProbeObservation.Failed(phase, _clock.Next(), DateTime.UtcNow,
                    "observer-returned-null");
            }
            catch (Exception exception)
            {
                return ProbeObservation.Failed(phase, _clock.Next(), DateTime.UtcNow,
                    "observer-exception:" + exception.GetType().Name + ":" + exception.Message);
            }
        }

        public string EffectOutcome
        {
            get { string outcome; EffectVerdict(out outcome); return outcome; }
        }

        public string ResourceOutcome
        {
            get { string outcome; ResourceVerdict(out outcome); return outcome; }
        }

        public IList<string> Violations()
        {
            var violations = new List<string>();
            if (Before == null || !Before.Succeeded)
                violations.Add("before-observation-unavailable:" + (Before == null ? "not-taken" : Before.Failure));
            if (After == null || !After.Succeeded)
                violations.Add("after-observation-unavailable:" + (After == null ? "not-taken" : After.Failure));
            if (violations.Count != 0) return violations;
            if (SubmissionSequence == null || !(Before.Sequence < SubmissionSequence.Value &&
                    SubmissionSequence.Value < After.Sequence))
                violations.Add("observation-order-invalid:before=" + Before.Sequence + ";submission=" +
                    SubmissionSequence + ";after=" + After.Sequence);
            string outcome;
            if (!EffectVerdict(out outcome)) violations.Add("effect:" + outcome);
            if (!ResourceVerdict(out outcome)) violations.Add("resource:" + outcome);
            return violations;
        }

        private bool EffectVerdict(out string outcome)
        {
            if (Before == null || After == null || !Before.Succeeded || !After.Succeeded ||
                Before.EffectInstances == null || After.EffectInstances == null)
            {
                outcome = "unknown";
                return false;
            }
            var before = Before.EffectInstances.ToDictionary(value => value.EffectId + "#" + value.InstanceKey,
                value => value.EndTimeTicks, StringComparer.Ordinal);
            bool created = After.EffectInstances.Any(value =>
                !before.ContainsKey(value.EffectId + "#" + value.InstanceKey));
            bool refreshed = After.EffectInstances.Any(value =>
                before.ContainsKey(value.EffectId + "#" + value.InstanceKey) &&
                value.EndTimeTicks > before[value.EffectId + "#" + value.InstanceKey]);
            if (created) { outcome = "new-instance"; return true; }
            if (refreshed) { outcome = "refreshed"; return true; }
            outcome = before.Count == 0
                ? (After.EffectInstances.Count == 0 ? "absent-after" : "unexplained")
                : "transition-unverified";
            return false;
        }

        // One cast consumes one available cast of a finite source and none
        // of a verified Unlimited source.
        private bool ResourceVerdict(out string outcome)
        {
            if (Before == null || After == null || !Before.Succeeded || !After.Succeeded ||
                Before.AvailableForCast == null || After.AvailableForCast == null ||
                _step.Reservation == null || !_step.Reservation.CostKnown)
            {
                outcome = "unknown";
                return false;
            }
            int delta = Before.AvailableForCast.Value - After.AvailableForCast.Value;
            int expected = _step.Reservation.Unlimited ? 0 : 1;
            if (delta == expected)
            {
                outcome = _step.Reservation.Unlimited ? "free-unchanged" : "decreased-by-one";
                return true;
            }
            outcome = _step.Reservation.Unlimited && delta > 0
                ? "unexpected-paid-resource-loss:" + delta
                : "delta-mismatch:expected=" + expected + ";observed=" + delta;
            return false;
        }
    }
}
