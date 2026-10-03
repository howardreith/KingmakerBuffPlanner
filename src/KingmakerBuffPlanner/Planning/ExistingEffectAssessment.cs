using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Planning
{
    public enum ExistingEffectVerdict
    {
        // The recipient does not currently have the complete effect.
        NotPresent,
        // The complete effect is present and provably at least as strong as
        // the planned casting, with enough duration left.
        Sufficient,
        // The complete effect is present but weaker, differently enhanced,
        // unprovable, or nearly expired: it does not satisfy the casting.
        Insufficient
    }

    public sealed class ExistingEffectRecipientAssessment
    {
        internal ExistingEffectRecipientAssessment(string unitId,
            ExistingEffectVerdict verdict, IEnumerable<string> reasons)
        {
            UnitId = unitId ?? string.Empty;
            Verdict = verdict;
            Reasons = new ReadOnlyCollection<string>((reasons ?? new string[0])
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToList());
        }

        public string UnitId { get; private set; }
        public ExistingEffectVerdict Verdict { get; private set; }
        public IReadOnlyList<string> Reasons { get; private set; }
    }

    // What the planned casting would produce, as far as it is known.
    public sealed class ExistingEffectRequirement
    {
        public ExistingEffectRequirement(int plannedCasterLevel,
            int plannedMetamagicMask, int plannedExpectedRounds,
            bool durationComparable, string strengthUnprovableReason = null)
        {
            if (plannedCasterLevel < 0) throw new ArgumentOutOfRangeException("plannedCasterLevel");
            if (plannedMetamagicMask < 0) throw new ArgumentOutOfRangeException("plannedMetamagicMask");
            if (plannedExpectedRounds < 0) throw new ArgumentOutOfRangeException("plannedExpectedRounds");
            PlannedCasterLevel = plannedCasterLevel;
            PlannedMetamagicMask = plannedMetamagicMask;
            PlannedExpectedRounds = plannedExpectedRounds;
            DurationComparable = durationComparable;
            StrengthUnprovableReason = string.IsNullOrEmpty(strengthUnprovableReason)
                ? null : strengthUnprovableReason;
        }

        // 0 = unknown: an existing instance is then never provably as
        // strong (review of rc4).
        public int PlannedCasterLevel { get; private set; }
        // Native metamagic flags of the planned casting (spell variant plus
        // applied metamagic enhancements).
        public int PlannedMetamagicMask { get; private set; }
        // 0 = unknown.
        public int PlannedExpectedRounds { get; private set; }
        // The expected duration is a trustworthy per-level estimate.
        public bool DurationComparable { get; private set; }
        // Non-null when the casting's strength cannot be compared to an
        // existing instance at all (for example an applied class-feature
        // enhancement whose effect the planner does not model).
        public string StrengthUnprovableReason { get; private set; }
    }

    // The casting-first existing-effect policy (legacy parity plus the
    // distinctions the legacy presence check could not make). An existing
    // effect satisfies a SkipAlreadyActive casting only when the COMPLETE
    // effect is present from instances that are
    //   - readably not suppressed;
    //   - not from a lower caster level than the planned caster, with both
    //     caster levels read (review of rc4: an unreadable one proves
    //     nothing);
    //   - carrying every strength-affecting metamagic flag the planned
    //     casting applies (Empower, Maximize, Extend, Heighten);
    //   - not left with less than half of the planned casting's expected
    //     duration (compared only when that duration is a trustworthy
    //     per-level estimate; an instance with no expiry always has enough).
    // A different effect never matches (exact effect identities), and a
    // casting whose strength the planner cannot model is never satisfied
    // by presence alone: it casts, and the reason is disclosed.
    public static class ExistingEffectSufficiency
    {
        public const double MinimumRemainingFraction = 0.5;

        // Whatever the duration comparability: an instance with less than
        // two rounds (12 seconds) left is about to expire and never counts
        // as satisfying a casting (fixed durations and non-English duration
        // texts are otherwise only checked for presence).
        public const double MinimumRemainingRounds = 2;

        // Kingmaker's [Flags] Metamagic values that change an effect's
        // strength or duration (Empower=1, Maximize=2, Extend=8,
        // Heighten=16). Quicken (4) and Reach (32) change only casting time
        // or range, so an instance without them is not weaker.
        public const int StrengthMetamagicMask = 1 | 2 | 8 | 16;
        public const int ExtendMetamagicFlag = 8;

        public static ExistingEffectRecipientAssessment Assess(
            string unitId,
            EffectExpression expression,
            IEnumerable<ActiveEffectInstance> instances,
            ISet<string> ignoredEffectIds,
            ExistingEffectRequirement requirement)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (requirement == null) throw new ArgumentNullException("requirement");
            List<ActiveEffectInstance> live = (instances ?? new ActiveEffectInstance[0])
                .Where(value => value != null && !value.Suppressed).ToList();
            var evaluator = new EffectPresenceEvaluator();
            EffectPresenceResult all = evaluator.EvaluateTyped(expression,
                new HashSet<ActiveEffectMarker>(live.Select(value => value.Marker)),
                ignoredEffectIds);
            if (all.Kind != EffectPresenceKind.Complete)
                return new ExistingEffectRecipientAssessment(unitId,
                    ExistingEffectVerdict.NotPresent,
                    all.Kind == EffectPresenceKind.Partial
                        ? new[] { "partial:" + string.Join(",", all.PresentMarkers.ToArray()) }
                        : new string[0]);
            var reasons = new List<string>();
            var relevant = new HashSet<string>(all.PresentMarkers, StringComparer.Ordinal);
            var sufficient = new HashSet<ActiveEffectMarker>();
            foreach (ActiveEffectInstance instance in live)
            {
                string failure = Failure(instance, requirement);
                if (failure == null) sufficient.Add(instance.Marker);
                else if (relevant.Contains(instance.EffectId))
                    reasons.Add(failure + ":" + instance.EffectId);
            }
            EffectPresenceResult strong = evaluator.EvaluateTyped(expression,
                sufficient, ignoredEffectIds);
            if (strong.Kind == EffectPresenceKind.Complete)
                return new ExistingEffectRecipientAssessment(unitId,
                    ExistingEffectVerdict.Sufficient,
                    requirement.DurationComparable ? new string[0]
                        : new[] { "remaining-duration-not-compared" });
            if (reasons.Count == 0) reasons.Add("present-effect-not-sufficient");
            return new ExistingEffectRecipientAssessment(unitId,
                ExistingEffectVerdict.Insufficient, reasons);
        }

        // Null when the instance is at least as strong as the planned
        // casting and has enough duration left; otherwise the reason.
        internal static string Failure(ActiveEffectInstance instance,
            ExistingEffectRequirement requirement)
        {
            if (requirement.StrengthUnprovableReason != null)
                return "equivalence-unproven:" + requirement.StrengthUnprovableReason;
            // Review of rc4: what cannot be read never establishes that an
            // existing effect is sufficient; the casting keeps its step.
            if (!instance.SuppressionReadable) return "suppression-unreadable";
            if (requirement.PlannedCasterLevel <= 0) return "caster-level-unverified:planned";
            if (instance.CasterLevel == null) return "caster-level-unverified";
            if (instance.CasterLevel.Value < requirement.PlannedCasterLevel)
                return "weaker-caster-level:" + instance.CasterLevel.Value + "<" +
                    requirement.PlannedCasterLevel;
            int plannedStrength = requirement.PlannedMetamagicMask & StrengthMetamagicMask;
            if (plannedStrength != 0)
            {
                if (instance.MetamagicMask == null)
                    return "metamagic-unverified:" + plannedStrength;
                int missing = plannedStrength & ~instance.MetamagicMask.Value;
                if (missing != 0) return "missing-metamagic:" + missing;
            }
            if (instance.RemainingRounds != null &&
                instance.RemainingRounds.Value < MinimumRemainingRounds)
                return "expiring:" + Math.Floor(instance.RemainingRounds.Value).ToString(
                    CultureInfo.InvariantCulture) + "<" + MinimumRemainingRounds.ToString(
                    CultureInfo.InvariantCulture);
            if (instance.RemainingRounds != null && requirement.DurationComparable &&
                requirement.PlannedExpectedRounds > 0)
            {
                double expected = requirement.PlannedExpectedRounds *
                    ((requirement.PlannedMetamagicMask & ExtendMetamagicFlag) != 0 ? 2.0 : 1.0);
                double minimum = expected * MinimumRemainingFraction;
                if (instance.RemainingRounds.Value < minimum)
                    return "remaining-duration-short:" +
                        Math.Floor(instance.RemainingRounds.Value).ToString(
                            CultureInfo.InvariantCulture) + "<" +
                        Math.Ceiling(minimum).ToString(CultureInfo.InvariantCulture);
            }
            return null;
        }

        // A per-level duration text ("1 hour/level", "10 minutes per level")
        // makes the provider's expected-duration estimate trustworthy; any
        // other text (fixed durations, other languages) is not compared.
        public static bool IsPerLevelDuration(string durationText)
        {
            if (string.IsNullOrWhiteSpace(durationText)) return false;
            string compact = new string(durationText.ToLowerInvariant()
                .Where(value => !char.IsWhiteSpace(value) && value != '.').ToArray());
            return compact.Contains("/level") || compact.Contains("perlevel");
        }
    }
}
