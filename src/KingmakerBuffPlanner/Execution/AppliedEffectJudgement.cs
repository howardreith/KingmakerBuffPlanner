using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Planning;

namespace KingmakerBuffPlanner.Execution
{
    // One instance of an expected effect on a recipient, as read for
    // confirmation: its kind and effect id, a process-local instance
    // identity, its end time (ticks) and whether the game suppresses it.
    public sealed class ObservedEffectInstance
    {
        public ObservedEffectInstance(EffectKind kind, string effectId, string instanceKey,
            long endTimeTicks, bool suppressed, string diagnosticContext = null)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                throw new ArgumentException("Effect ID is required.", "effectId");
            Kind = kind;
            EffectId = effectId;
            InstanceKey = instanceKey ?? string.Empty;
            EndTimeTicks = endTimeTicks;
            Suppressed = suppressed;
            DiagnosticContext = diagnosticContext ?? string.Empty;
        }

        public EffectKind Kind { get; private set; }
        public string EffectId { get; private set; }
        public string InstanceKey { get; private set; }
        public long EndTimeTicks { get; private set; }
        public bool Suppressed { get; private set; }
        // Native context is diagnostic only; never proof of application or equivalence.
        public string DiagnosticContext { get; private set; }

        internal string Identity
        {
            get { return Kind + ":" + EffectId + "#" + InstanceKey; }
        }
    }

    // Each expected recipient's instances of the expected effects, read
    // before the cast was submitted.
    public sealed class EffectBaseline
    {
        private readonly Dictionary<string, IReadOnlyList<ObservedEffectInstance>> _byUnit =
            new Dictionary<string, IReadOnlyList<ObservedEffectInstance>>(StringComparer.Ordinal);
        private readonly List<string> _confirmationPolls = new List<string>();
        private int _confirmationPollCount;

        public void RecordConfirmationPoll(bool confirmed, EffectBaseline after, IEnumerable<string> units)
        {
            string value = (++_confirmationPollCount).ToString(CultureInfo.InvariantCulture) + "@" +
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + ":confirmed=" + confirmed + ":" +
                (after == null ? "unreadable" : after.Describe(units));
            if (_confirmationPolls.Count < 8) _confirmationPolls.Add(value);
            else _confirmationPolls[7] = value;
        }

        public string DescribeConfirmationPolls()
        { return "role-scoped-complete+fresh-primary;poll-count=" + _confirmationPollCount + ";polls={" + string.Join("|", _confirmationPolls.ToArray()) + "}"; }

        public EffectBaseline(IDictionary<string, IEnumerable<ObservedEffectInstance>> byUnit)
        {
            foreach (KeyValuePair<string, IEnumerable<ObservedEffectInstance>> pair in byUnit ??
                new Dictionary<string, IEnumerable<ObservedEffectInstance>>())
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null) continue;
                _byUnit[pair.Key] = new ReadOnlyCollection<ObservedEffectInstance>(
                    pair.Value.Where(value => value != null).ToList());
            }
        }

        public bool TryGet(string unitId, out IReadOnlyList<ObservedEffectInstance> instances)
        {
            instances = null;
            return unitId != null && _byUnit.TryGetValue(unitId, out instances);
        }

        // H1/H2 (0.6.11 review): authoritative per-attempt observations made
        // AFTER submission. Each instance identity this attempt was seen to
        // hold is kept with the read that first saw it and, when the adapter
        // could read them, the native distances at that read. A short-lived
        // application seen while the cast resolved is therefore not judged
        // "never applied" because it expired before a later read; the
        // pre-submission read above remains the only freshness reference.
        private readonly Dictionary<string, Dictionary<string, SeenEffectInstance>> _seen =
            new Dictionary<string, Dictionary<string, SeenEffectInstance>>(StringComparer.Ordinal);
        private readonly Dictionary<int, IDictionary<string, IReadOnlyList<NearbyEligibleUnit>>> _geometryByObservation =
            new Dictionary<int, IDictionary<string, IReadOnlyList<NearbyEligibleUnit>>>();
        private int _observationCount;

        public int ObservationCount { get { return _observationCount; } }

        // geometry: key GeometryKey(center, binding) -> every unit the native
        // wrapper's own filter admitted within radius + tolerance of that
        // centre at THIS read (observable or not), with native distances; null
        // when the reader could not take positions.
        public void RecordObservation(IDictionary<string, IEnumerable<ObservedEffectInstance>> byUnit,
            IDictionary<string, IReadOnlyList<NearbyEligibleUnit>> geometry = null)
        {
            if (byUnit == null) return;
            int observation = ++_observationCount;
            bool firstSighting = false;
            foreach (KeyValuePair<string, IEnumerable<ObservedEffectInstance>> pair in byUnit)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null) continue;
                Dictionary<string, SeenEffectInstance> seen;
                if (!_seen.TryGetValue(pair.Key, out seen)) _seen[pair.Key] = seen = new Dictionary<string, SeenEffectInstance>(StringComparer.Ordinal);
                foreach (ObservedEffectInstance instance in pair.Value.Where(value => value != null && !value.Suppressed))
                {
                    SeenEffectInstance prior;
                    if (seen.TryGetValue(instance.Identity, out prior) && prior.Instance.EndTimeTicks >= instance.EndTimeTicks) continue;
                    seen[instance.Identity] = new SeenEffectInstance(instance, prior == null ? observation : prior.Observation);
                    if (prior == null) firstSighting = true;
                }
            }
            // Positions matter only at a read that first saw an application.
            if (firstSighting && geometry != null && _geometryByObservation.Count < 64)
                _geometryByObservation[observation] = geometry.ToDictionary(pair => pair.Key,
                    pair => (IReadOnlyList<NearbyEligibleUnit>)(pair.Value ?? new NearbyEligibleUnit[0]).ToList(), StringComparer.Ordinal);
        }

        public IEnumerable<SeenEffectInstance> SeenAfterSubmission(string unitId)
        {
            Dictionary<string, SeenEffectInstance> seen;
            return unitId != null && _seen.TryGetValue(unitId, out seen) ? seen.Values.ToList() : new List<SeenEffectInstance>();
        }

        // Null when that read carried no readable geometry for the centre/rule.
        public IReadOnlyList<NearbyEligibleUnit> EligibleAt(int observation, string center, NearbySelectionBinding binding)
        {
            IDictionary<string, IReadOnlyList<NearbyEligibleUnit>> geometry;
            IReadOnlyList<NearbyEligibleUnit> eligible;
            return binding != null && _geometryByObservation.TryGetValue(observation, out geometry) &&
                geometry.TryGetValue(GeometryKey(center, binding), out eligible) ? eligible : null;
        }

        public static string GeometryKey(string center, NearbySelectionBinding binding)
        { return center + "\u001f" + binding.GeometryKey; }

        // Not suppressed, and new or refreshed relative to the read taken
        // before submission (never inferred from an after-read alone).
        public bool IsFresh(string unitId, ObservedEffectInstance instance)
        {
            IReadOnlyList<ObservedEffectInstance> before;
            return instance != null && !instance.Suppressed && TryGet(unitId, out before) &&
                !before.Any(old => old.Identity == instance.Identity && old.EndTimeTicks >= instance.EndTimeTicks);
        }

        public string Describe(IEnumerable<string> recipients)
        {
            var units = (recipients ?? new string[0]).ToList();
            return string.Join("|", units.Take(12).Select(id =>
            {
                IReadOnlyList<ObservedEffectInstance> instances;
                if (!TryGet(id, out instances)) return id + "=unreadable";
                return id + "=[" + string.Join(",", instances.Take(4).Select(value =>
                    value.Identity + "@end=" + value.EndTimeTicks.ToString(CultureInfo.InvariantCulture) +
                    ":suppressed=" + value.Suppressed + ":" + value.DiagnosticContext).ToArray()) +
                    (instances.Count > 4 ? ",truncated" : string.Empty) + "]";
            }).ToArray()) + (units.Count > 12 ? "|recipients-truncated" : string.Empty);
        }
    }

    public sealed class SeenEffectInstance
    {
        internal SeenEffectInstance(ObservedEffectInstance instance, int observation)
        { Instance = instance; Observation = observation; }
        public ObservedEffectInstance Instance { get; private set; }
        // The 1-based post-submission read that first saw this identity.
        public int Observation { get; private set; }
    }

    // Final review A3: an expected effect confirms a cast only when this
    // attempt put it there. Presence alone is no proof: a recast over an
    // insufficient, suppressed or deliberately recast instance would find
    // the old one and confirm a cast that delivered nothing. A recipient is
    // reached only when the expected effects are complete over instances
    // that are not suppressed and at least one of those is new (an identity
    // not read before submission) or refreshed (the same identity with a
    // later end) - the probe's own rule. A recipient without a read before
    // submission, or unreadable after it, is not reached, and an empty
    // recipient set confirms nothing (final review A2).
    public static class AppliedEffectJudgement
    {
        // A natively selected secondary owner's distance from the selection
        // centre may differ from the native read by one frame of movement.
        public const float SecondaryDistanceTolerance = 1.0f;

        public static bool ReachedScoped(EffectExpression expected, string caster, string recipient,
            EffectBaseline baseline, Func<string, IEnumerable<ObservedEffectInstance>> readAfter, bool requireFresh = true)
        {
            if (expected == null || baseline == null || readAfter == null) return false;
            // H1: kept coverage is lasting coverage; a short-lived application
            // is only ever a delivery obligation of the attempt that made it.
            if (!requireFresh && EffectApplicationLifetime.ContainsDurable(expected))
                expected = EffectApplicationLifetime.Durable(expected);
            var scope = new EffectRecipientScope(expected, caster, recipient);
            var before = new Dictionary<string, IReadOnlyList<ObservedEffectInstance>>(StringComparer.Ordinal);
            var after = new Dictionary<string, List<ObservedEffectInstance>>(StringComparer.Ordinal);
            foreach (string owner in scope.UnitIds)
            {
                IReadOnlyList<ObservedEffectInstance> prior;
                if (!baseline.TryGet(owner, out prior)) return false;
                IEnumerable<ObservedEffectInstance> current = readAfter(owner);
                if (current == null) return false;
                before[owner] = prior;
                after[owner] = current.Where(value => value != null && !value.Suppressed).ToList();
            }
            // Every confirmation read is also an observation of this attempt.
            baseline.RecordObservation(after.ToDictionary(pair => pair.Key,
                pair => (IEnumerable<ObservedEffectInstance>)pair.Value, StringComparer.Ordinal));
            bool durableExpected = EffectApplicationLifetime.ContainsDurable(expected);
            bool freshPrimary = false;
            var evaluator = new EffectPresenceEvaluator();
            // The centre's own expected effects: their first fresh sighting
            // is the attempt's application boundary for nearby selection.
            var anchors = new HashSet<string>(StringComparer.Ordinal);
            evaluator.EvaluateScoped(expected, (leaf, role) =>
            {
                if (leaf.SecondarySelection == null && scope.Owner(leaf, role) == recipient)
                    anchors.Add(leaf.Kind + ":" + leaf.EffectId);
                return true;
            }, null);
            EffectPresenceResult complete = evaluator.EvaluateScoped(expected, (leaf, role) =>
            {
                bool transient = EffectApplicationLifetime.IsTransient(leaf);
                if (leaf.SecondarySelection != null)
                    return requireFresh
                        ? SecondaryReached(leaf, recipient, baseline, after, transient, anchors)
                        : scope.PredictedOwners(leaf, role).All(predicted => after.ContainsKey(predicted) &&
                            after[predicted].Any(value => value.Kind == leaf.Kind && value.EffectId == leaf.EffectId));
                string owner = scope.Owner(leaf, role);
                List<ObservedEffectInstance> instances;
                if (owner == null || !after.TryGetValue(owner, out instances)) return false;
                Func<ObservedEffectInstance, bool> matches = value => value.Kind == leaf.Kind && value.EffectId == leaf.EffectId;
                var matching = instances.Where(matches).ToList();
                if (transient)
                {
                    // Delivered by THIS attempt, at this read or an earlier
                    // post-submission read; never old presence.
                    bool delivered = matching.Any(value => baseline.IsFresh(owner, value)) ||
                        baseline.SeenAfterSubmission(owner).Any(seen => matches(seen.Instance) && baseline.IsFresh(owner, seen.Instance));
                    if (delivered && owner == recipient && !durableExpected) freshPrimary = true;
                    return delivered;
                }
                if (owner == recipient && matching.Any(value => baseline.IsFresh(owner, value))) freshPrimary = true;
                return matching.Count != 0;
            }, null);
            if (complete.Kind != EffectPresenceKind.Complete) return false;
            if (requireFresh) return freshPrimary;
            // Kept group coverage must have been complete on the same owners
            // before submission too; it cannot be inferred from an after-read.
            return evaluator.EvaluateScoped(expected, (leaf, role) =>
            {
                if (leaf.SecondarySelection == null && scope.Owner(leaf, role) == null) return false;
                return scope.PredictedOwners(leaf, role).All(owner =>
                {
                    IReadOnlyList<ObservedEffectInstance> prior;
                    return before.TryGetValue(owner, out prior) && prior.Any(value =>
                        !value.Suppressed && value.Kind == leaf.Kind && value.EffectId == leaf.EffectId);
                });
            }, null).Kind == EffectPresenceKind.Complete;
        }

        // H2 (0.6.11 review, corrected after PR #21 review): the native nearby
        // wrapper chooses its owner when its action runs. The prediction is
        // never trusted here. The APPLICATION BOUNDARY is the post-submission
        // read that first saw this attempt's own effect on the selection
        // centre; the native wrapper applies the secondary effect in that same
        // action, so:
        //  - every fresh secondary application on an observed candidate must
        //    have been first seen at exactly that read (an application seen
        //    earlier or later is unrelated, e.g. while a command was pending);
        //  - that read must carry readable geometry: every unit the native
        //    filter admitted within radius + tolerance, observable or not;
        //  - the marked set must be what nearest-first selection could have
        //    chosen there: each marked unit inside reach, at most `maximum`,
        //    no unmarked definitely-eligible unit nearer by more than the
        //    tolerance, and at least min(maximum, definitely-eligible) marked;
        //  - an eligible unit the planner cannot observe that could have been
        //    selected makes the outcome unconfirmable.
        // Empty is accepted only when nothing was definitely eligible there.
        private static bool SecondaryReached(EffectLeafExpression leaf, string center, EffectBaseline baseline,
            IDictionary<string, List<ObservedEffectInstance>> after, bool transient, ISet<string> anchors)
        {
            NearbySelectionBinding selection = leaf.SecondarySelection;
            Func<ObservedEffectInstance, bool> matches = value => value.Kind == leaf.Kind && value.EffectId == leaf.EffectId;
            int application = baseline.SeenAfterSubmission(center)
                .Where(seen => anchors.Contains(seen.Instance.Kind + ":" + seen.Instance.EffectId) &&
                    baseline.IsFresh(center, seen.Instance))
                .Select(seen => seen.Observation).DefaultIfEmpty(0).Min();
            if (application == 0) return false;
            IReadOnlyList<NearbyEligibleUnit> eligible = baseline.EligibleAt(application, center, selection);
            if (eligible == null) return false;
            var distance = eligible.Where(unit => unit.UnitId != center)
                .GroupBy(unit => unit.UnitId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Min(unit => unit.Distance), StringComparer.Ordinal);
            var observed = new HashSet<string>(selection.CandidateUnitIds, StringComparer.Ordinal);
            var marked = new List<string>();
            foreach (string candidate in selection.CandidateUnitIds)
            {
                List<ObservedEffectInstance> current;
                if (!after.TryGetValue(candidate, out current)) return false;
                var sightings = baseline.SeenAfterSubmission(candidate)
                    .Where(seen => matches(seen.Instance) && baseline.IsFresh(candidate, seen.Instance) &&
                        (transient || current.Any(value => value.Identity == seen.Instance.Identity))).ToList();
                if (sightings.Count == 0) continue;
                if (sightings.Any(seen => seen.Observation != application)) return false;
                if (!distance.ContainsKey(candidate)) return false; // outside reach at application
                marked.Add(candidate);
            }
            float tolerance = SecondaryDistanceTolerance;
            if (selection.Maximum > 0 && marked.Count > selection.Maximum) return false;
            var definite = distance.Where(pair => pair.Value <= selection.Radius - tolerance).Select(pair => pair.Key).ToList();
            foreach (string unit in definite.Where(unit => !marked.Contains(unit)))
                if (marked.Any(owner => distance[owner] > distance[unit] + tolerance)) return false;
            int required = selection.Maximum > 0 ? Math.Min(selection.Maximum, definite.Count) : definite.Count;
            if (marked.Count < required) return false;
            // Could the native action have chosen a unit nobody can observe?
            bool room = selection.Maximum <= 0 || marked.Count < selection.Maximum;
            float farthest = marked.Count == 0 ? float.NegativeInfinity : marked.Max(owner => distance[owner]);
            foreach (KeyValuePair<string, float> pair in distance.Where(pair => !observed.Contains(pair.Key)))
                if (room || pair.Value <= farthest + tolerance) return false;
            return true;
        }

        // Adapters call this at every post-submission read, including reads
        // while an animated command is still running.
        public static void RecordProgress(EffectBaseline baseline, IEnumerable<string> unitIds,
            Func<string, IEnumerable<ObservedEffectInstance>> read,
            IDictionary<string, IReadOnlyList<NearbyEligibleUnit>> geometry)
        {
            if (baseline == null || unitIds == null || read == null) return;
            var observed = new Dictionary<string, IEnumerable<ObservedEffectInstance>>(StringComparer.Ordinal);
            foreach (string unitId in unitIds)
            {
                IEnumerable<ObservedEffectInstance> instances = read(unitId);
                if (instances != null) observed[unitId] = instances.ToList();
            }
            baseline.RecordObservation(observed, geometry);
        }

        public static bool Reached(EffectExpression expected,
            IEnumerable<ObservedEffectInstance> before,
            IEnumerable<ObservedEffectInstance> after)
        {
            if (expected == null || before == null || after == null) return false;
            List<ObservedEffectInstance> live = after
                .Where(value => value != null && !value.Suppressed).ToList();
            var markers = new HashSet<ActiveEffectMarker>(live.Select(value =>
                new ActiveEffectMarker(value.Kind, value.EffectId)));
            if (new EffectPresenceEvaluator().EvaluateTyped(expected, markers, null).Kind !=
                    EffectPresenceKind.Complete)
                return false;
            var ends = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (ObservedEffectInstance prior in before.Where(value => value != null))
            {
                long end;
                if (!ends.TryGetValue(prior.Identity, out end) || prior.EndTimeTicks > end)
                    ends[prior.Identity] = prior.EndTimeTicks;
            }
            // Re-review: the new or refreshed instance must be of an expected
            // kind as well as an expected effect (an area instance of the same
            // buff does not show that the buff itself landed).
            var leaves = new HashSet<string>(StringComparer.Ordinal);
            CollectLeafMarkers(expected, leaves);
            return live.Any(value =>
            {
                if (!leaves.Contains(value.Kind + ":" + value.EffectId)) return false;
                long end;
                return !ends.TryGetValue(value.Identity, out end) || value.EndTimeTicks > end;
            });
        }

        private static void CollectLeafMarkers(EffectExpression expression, ISet<string> leaves)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { leaves.Add(leaf.Kind + ":" + leaf.EffectId); return; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children) CollectLeafMarkers(child, leaves);
                return;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                CollectLeafMarkers(conditional.WhenTrue, leaves);
                CollectLeafMarkers(conditional.WhenFalse, leaves);
                return;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) { CollectLeafMarkers(targeted.Child, leaves); return; }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) CollectLeafMarkers(referenced.Child, leaves);
        }

        // Mixed coverage (owner, 2026-09-24): a recipient the plan proved
        // already adequately covered (skip-if-active, from readable detail)
        // may keep its coverage unchanged - a game that leaves a
        // longer-lasting instance in place has not failed the cast - but only
        // while the complete effect was present and unsuppressed before the
        // cast and still is after it. Every other recipient needs a new or
        // refreshed instance, and at least one such recipient must exist.
        public static bool AllReached(IReadOnlyList<string> recipients, EffectExpression expected,
            EffectBaseline baseline, Func<string, IEnumerable<ObservedEffectInstance>> readAfter,
            IEnumerable<string> preCovered = null, string casterUnitId = null)
        {
            if (recipients == null || recipients.Count == 0 || baseline == null || readAfter == null)
                return false;
            var kept = new HashSet<string>(preCovered ?? new string[0], StringComparer.Ordinal);
            bool delivered = false;
            foreach (string unitId in recipients)
            {
                if (casterUnitId != null)
                {
                    bool reached = ReachedScoped(expected, casterUnitId, unitId, baseline, readAfter);
                    if (kept.Contains(unitId))
                    {
                        if (!reached && !ReachedScoped(expected, casterUnitId, unitId, baseline, readAfter, false)) return false;
                    }
                    else { if (!reached) return false; delivered = true; }
                    continue;
                }
                IReadOnlyList<ObservedEffectInstance> before;
                if (!baseline.TryGet(unitId, out before)) return false;
                IEnumerable<ObservedEffectInstance> after = readAfter(unitId);
                if (kept.Contains(unitId))
                {
                    if (!Reached(expected, before, after) && !StillCovered(expected, before, after))
                        return false;
                    continue;
                }
                if (!Reached(expected, before, after)) return false;
                delivered = true;
            }
            return delivered;
        }

        // WP3 (0.4.0): a group cast affects whoever the native ability
        // reaches from its centre and no predicted member is required. The
        // attempt is confirmed when it delivered the expected effect (a new
        // or refreshed, unsuppressed instance) to at least one predicted
        // recipient; a member it did not reach - moved out of the area, or
        // already covered for longer - is not a failure. An empty recipient
        // set confirms nothing.
        public static bool AnyReached(IReadOnlyList<string> recipients, EffectExpression expected,
            EffectBaseline baseline, Func<string, IEnumerable<ObservedEffectInstance>> readAfter, string casterUnitId = null)
        {
            return ReachedRecipients(recipients, expected, baseline, readAfter, casterUnitId).Count != 0;
        }

        public static IReadOnlyList<string> ReachedRecipients(IReadOnlyList<string> recipients,
            EffectExpression expected, EffectBaseline baseline,
            Func<string, IEnumerable<ObservedEffectInstance>> readAfter, string casterUnitId = null)
        {
            var reached = new List<string>();
            if (recipients == null || baseline == null || readAfter == null) return reached;
            foreach (string unitId in recipients)
            {
                if (casterUnitId != null)
                {
                    if (ReachedScoped(expected, casterUnitId, unitId, baseline, readAfter)) reached.Add(unitId);
                    continue;
                }
                IReadOnlyList<ObservedEffectInstance> before;
                if (!baseline.TryGet(unitId, out before)) continue;
                IEnumerable<ObservedEffectInstance> after = readAfter(unitId);
                if (after != null && Reached(expected, before, after)) reached.Add(unitId);
            }
            return reached;
        }

        // The complete expected effect over unsuppressed instances both
        // before and after the cast: a pre-covered recipient's kept coverage.
        public static bool StillCovered(EffectExpression expected,
            IEnumerable<ObservedEffectInstance> before, IEnumerable<ObservedEffectInstance> after)
        {
            return expected != null && before != null && after != null &&
                Complete(expected, before) && Complete(expected, after);
        }

        private static bool Complete(EffectExpression expected, IEnumerable<ObservedEffectInstance> instances)
        {
            var markers = new HashSet<ActiveEffectMarker>(instances
                .Where(value => value != null && !value.Suppressed)
                .Select(value => new ActiveEffectMarker(value.Kind, value.EffectId)));
            return new EffectPresenceEvaluator().EvaluateTyped(expected, markers, null).Kind ==
                EffectPresenceKind.Complete;
        }
    }
}
