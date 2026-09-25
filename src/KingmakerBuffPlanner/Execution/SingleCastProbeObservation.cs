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
        public ProbeEffectInstance(string effectId, string instanceKey, long endTimeTicks,
            IEnumerable<string> modifiers = null)
        {
            EffectId = effectId ?? string.Empty;
            InstanceKey = instanceKey ?? string.Empty;
            EndTimeTicks = endTimeTicks;
            Modifiers = modifiers == null ? null : new ReadOnlyCollection<string>(modifiers
                .Where(value => !string.IsNullOrEmpty(value))
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public string EffectId { get; private set; }
        public string InstanceKey { get; private set; }
        public long EndTimeTicks { get; private set; }
        // The stat modifiers this instance gives its unit, each
        // "<stat>/<descriptor>/<value>", sorted; null when they were not read.
        public IReadOnlyList<string> Modifiers { get; private set; }
        public override string ToString()
        {
            return EffectId + "#" + InstanceKey + "@" + EndTimeTicks + (Modifiers == null ? string.Empty
                : "{" + string.Join(";", Modifiers.ToArray()) + "}");
        }
    }

    // The caster-side reads of the enhanced qualification (mission batch 3,
    // section 8): the amount of the enhancement's own resource (for example
    // the Arcane Reservoir) and every activatable ability of the caster with
    // its on/off state, read from the game, never from the planner's
    // discovery, so the paid resource and the cleanup are both observed.
    public sealed class CasterEnhancementObservation
    {
        private CasterEnhancementObservation(string failure, int? resource,
            IDictionary<string, bool> activatables)
        {
            Failure = failure;
            Resource = resource;
            Activatables = new ReadOnlyDictionary<string, bool>(new SortedDictionary<string, bool>(
                activatables ?? new Dictionary<string, bool>(), StringComparer.Ordinal));
        }

        public static CasterEnhancementObservation Failed(string failure)
        {
            return new CasterEnhancementObservation(string.IsNullOrEmpty(failure) ? "unknown" : failure,
                null, null);
        }

        public static CasterEnhancementObservation Read(int resource, IDictionary<string, bool> activatables)
        {
            return new CasterEnhancementObservation(null, resource, activatables);
        }

        public bool Succeeded { get { return Failure == null; } }
        public string Failure { get; private set; }
        public int? Resource { get; private set; }
        // Blueprint id ("#<n>" added for a repeated one) -> switched on.
        public IReadOnlyDictionary<string, bool> Activatables { get; private set; }

        public string Describe()
        {
            if (!Succeeded) return "failed:" + Failure;
            return "resource=" + Resource.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ";activatables=" + Activatables.Count + ";on=" + string.Join(",", Activatables
                    .Where(pair => pair.Value).Select(pair => pair.Key).ToArray());
        }
    }

    // Review N3: one memorized slot as the observer sees it. Observation
    // is separate from spendability: a consumed slot is still THE source.
    public sealed class ProbeSlotView
    {
        public ProbeSlotView(string tokenId, bool available, bool isMainSlot, bool matchesAbility)
        {
            TokenId = tokenId ?? string.Empty;
            Available = available;
            IsMainSlot = isMainSlot;
            MatchesAbility = matchesAbility;
        }

        public string TokenId { get; private set; }
        public bool Available { get; private set; }
        public bool IsMainSlot { get; private set; }
        public bool MatchesAbility { get; private set; }
    }

    public static class ProbeSourceSlots
    {
        // Exactly the reserved slots, whether or not they are still
        // available; never another slot of the same spell. A missing or
        // non-matching reserved slot is a failed read, not a substitution.
        public static IDictionary<string, bool> ReservedExactly(IEnumerable<ProbeSlotView> slots,
            IEnumerable<string> reservedTokenIds, out string failure)
        {
            failure = null;
            var byToken = new Dictionary<string, ProbeSlotView>(StringComparer.Ordinal);
            foreach (ProbeSlotView slot in slots ?? new ProbeSlotView[0])
                if (slot != null && !byToken.ContainsKey(slot.TokenId)) byToken[slot.TokenId] = slot;
            var result = new SortedDictionary<string, bool>(StringComparer.Ordinal);
            foreach (string token in reservedTokenIds ?? new string[0])
            {
                ProbeSlotView slot;
                if (!byToken.TryGetValue(token, out slot))
                {
                    failure = "reserved-slot-missing:" + token;
                    return null;
                }
                if (!slot.MatchesAbility && slot.IsMainSlot)
                {
                    failure = "reserved-slot-holds-other-spell:" + token;
                    return null;
                }
                result[token] = slot.Available;
            }
            if (result.Count == 0) failure = "no-reserved-slots";
            return result.Count == 0 ? null : result;
        }
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
        // Review N3: availability of EXACTLY the reserved prepared tokens;
        // null for non-prepared reservations or when not read.
        public IReadOnlyDictionary<string, bool> ReservedTokenAvailability { get; private set; }
        // The game clock at the read, in ticks (the same scale as an
        // instance's end time); null when not read.
        public long? GameTimeTicks { get; private set; }
        // Why the clock read failed; null when it was read or not asked for.
        public string ClockFailure { get; private set; }

        public static ProbeObservation Read(string phase, long sequence, DateTime capturedAtUtc,
            string targetUnitId, int? availableForCast, IEnumerable<ProbeEffectInstance> effectInstances,
            IDictionary<string, bool> reservedTokenAvailability = null, long? gameTimeTicks = null,
            string clockFailure = null)
        {
            return new ProbeObservation
            {
                GameTimeTicks = gameTimeTicks,
                ClockFailure = gameTimeTicks == null ? clockFailure : null,
                Phase = phase, Sequence = sequence, CapturedAtUtc = capturedAtUtc, Succeeded = true,
                Failure = string.Empty, TargetUnitId = targetUnitId, AvailableForCast = availableForCast,
                EffectInstances = new ReadOnlyCollection<ProbeEffectInstance>(
                    (effectInstances ?? new ProbeEffectInstance[0]).Where(value => value != null).ToList()),
                ReservedTokenAvailability = reservedTokenAvailability == null ? null
                    : new ReadOnlyDictionary<string, bool>(new SortedDictionary<string, bool>(
                        reservedTokenAvailability, StringComparer.Ordinal))
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
                ? ";available=" + AvailableForCast +
                    (GameTimeTicks != null
                        ? ";clock=" + GameTimeTicks.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : ClockFailure != null ? ";clock=failed:" + ClockFailure : string.Empty) +
                    (ReservedTokenAvailability == null ? string.Empty : ";slots=" + string.Join(",",
                        ReservedTokenAvailability.Select(pair => pair.Key + ":" + pair.Value).ToArray())) +
                    ";effects=[" +
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
                _step.Reservation == null || !_step.Reservation.CostKnown)
            {
                outcome = "unknown";
                return false;
            }
            // Review N3: a prepared reservation is judged by EXACTLY its
            // reserved tokens: all available before, all consumed after.
            if (_step.Reservation.TokenIds.Count != 0)
            {
                IReadOnlyDictionary<string, bool> before = Before.ReservedTokenAvailability;
                IReadOnlyDictionary<string, bool> after = After.ReservedTokenAvailability;
                if (before == null || after == null ||
                    _step.Reservation.TokenIds.Any(token => !before.ContainsKey(token) || !after.ContainsKey(token)))
                {
                    outcome = "unknown";
                    return false;
                }
                if (_step.Reservation.TokenIds.All(token => before[token] && !after[token]))
                {
                    outcome = "reserved-slots-consumed";
                    return true;
                }
                outcome = "prepared-slot-mismatch:" + string.Join(",", _step.Reservation.TokenIds
                    .Select(token => token + "=" + before[token] + ">" + after[token]).ToArray());
                return false;
            }
            if (Before.AvailableForCast == null || After.AvailableForCast == null)
            {
                outcome = "unknown";
                return false;
            }
            int delta = Before.AvailableForCast.Value - After.AvailableForCast.Value;
            // Re-review: a free step is proven free by the same rule as the
            // executors and the Classic judgement (unlimited and unchanged).
            if (_step.Reservation.Unlimited)
            {
                string violation = AvailableCountJudgement.FreeViolation(Before.AvailableForCast,
                    After.AvailableForCast);
                if (violation == null)
                {
                    outcome = "free-unchanged";
                    return true;
                }
                outcome = Before.AvailableForCast.Value >= 0 && After.AvailableForCast.Value >= 0 && delta > 0
                    ? "unexpected-paid-resource-loss:" + delta
                    : "free-not-proven:" + violation;
                return false;
            }
            int expected = 1;
            if (delta == expected)
            {
                outcome = "decreased-by-one";
                return true;
            }
            outcome = "delta-mismatch:expected=" + expected + ";observed=" + delta;
            return false;
        }
    }
}
