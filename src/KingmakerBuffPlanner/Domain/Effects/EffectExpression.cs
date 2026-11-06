using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Domain.Effects
{
    public enum EffectKind
    {
        Buff,
        AreaBuff,
        WornItemEnchantment
    }

    public enum EffectTarget
    {
        CurrentTarget,
        Caster,
        Pet,
        Party,
        AlliedAreaRecipients,
        EnemyAreaRecipients,
        AmbiguousAreaRecipients
    }

    public abstract class EffectExpression
    {
        protected EffectExpression(string expressionType)
        {
            ExpressionType = expressionType;
        }

        [JsonProperty("expressionType", Order = 1)]
        public string ExpressionType { get; private set; }
    }

    public sealed class EmptyEffectExpression : EffectExpression
    {
        // An empty whose cause was not recorded: never taken as doing nothing.
        public EmptyEffectExpression() : this("unrecorded") { }

        // unmodeledReason null: a node that does nothing (a null action or an
        // empty action list). Otherwise what the planner does not model there,
        // such as "restorative-action:<type>", "offensive-action:<type>",
        // "unknown-node:<type>", "maximum-depth" or "cycle" (independent
        // review of the next iteration: a damaging or removing action is not
        // a no-op). Used only to judge a plain buff; not part of any
        // serialized form or identity.
        public EmptyEffectExpression(string unmodeledReason) : base("empty")
        {
            UnmodeledReason = unmodeledReason;
        }

        public static EmptyEffectExpression NoAction() { return new EmptyEffectExpression(null); }

        [JsonIgnore] public string UnmodeledReason { get; private set; }

        [JsonIgnore] public bool IsNoAction { get { return UnmodeledReason == null; } }
    }

    public sealed class EffectLeafExpression : EffectExpression
    {
        public EffectLeafExpression(
            EffectKind kind,
            string effectId,
            EffectTarget target,
            string sourceContract,
            string actionPath,
            string recipientUnitId = null,
            float? maximumDurationSeconds = null,
            NearbySelectionBinding secondarySelection = null)
            : base("leaf")
        {
            if (string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException("Effect ID is required.", "effectId");
            Kind = kind;
            EffectId = effectId;
            Target = target;
            SourceContract = sourceContract ?? string.Empty;
            ActionPath = actionPath ?? string.Empty;
            RecipientUnitId = recipientUnitId;
            MaximumDurationSeconds = maximumDurationSeconds.HasValue &&
                !float.IsNaN(maximumDurationSeconds.Value) && !float.IsInfinity(maximumDurationSeconds.Value) &&
                maximumDurationSeconds.Value > 0 ? maximumDurationSeconds : null;
            SecondarySelection = secondarySelection;
        }

        // Copy with a bound owner; lifetime metadata is retained.
        internal EffectLeafExpression WithOwner(string recipientUnitId, NearbySelectionBinding secondarySelection)
        {
            return new EffectLeafExpression(Kind, EffectId, Target, SourceContract, ActionPath,
                recipientUnitId, MaximumDurationSeconds, secondarySelection ?? SecondarySelection);
        }

        [JsonProperty("kind", Order = 2)] public EffectKind Kind { get; private set; }
        [JsonProperty("effectId", Order = 3)] public string EffectId { get; private set; }
        [JsonProperty("target", Order = 4)] public EffectTarget Target { get; private set; }
        [JsonProperty("sourceContract", Order = 5)] public string SourceContract { get; private set; }
        [JsonProperty("actionPath", Order = 6)] public string ActionPath { get; private set; }
        [JsonProperty("recipientUnitId", Order = 7, NullValueHandling = NullValueHandling.Ignore)]
        public string RecipientUnitId { get; private set; }
        // The longest native lifetime this application can have (fixed
        // seconds or a constant duration value, worst-case extended); null
        // when permanent, level/dice-dependent or unread. Inspection
        // metadata, not catalogue identity (EffectAggregateIdentity).
        [JsonProperty("maximumDurationSeconds", Order = 8, NullValueHandling = NullValueHandling.Ignore)]
        public float? MaximumDurationSeconds { get; private set; }
        // Present only on a leaf bound under a verified nearby-recipient
        // wrapper: the native action, not the planner, chooses the owner
        // when it runs (see EffectRecipientBinding).
        [JsonProperty("secondarySelection", Order = 9, NullValueHandling = NullValueHandling.Ignore)]
        public NearbySelectionBinding SecondarySelection { get; private set; }
    }

    // H1 (0.6.11 review): an application whose native lifetime is
    // structurally shorter than the minimum remaining coverage the planner
    // ever accepts (ExistingEffectSufficiency.MinimumRemainingRounds) can
    // never represent lasting buff coverage. It is an immediate
    // application-confirmation obligation only: confirmation still requires
    // it, freshly applied by the attempt, but already-active assessment does
    // not. Leaves whose lifetime is unknown or level-dependent are durable.
    public static class EffectApplicationLifetime
    {
        // Two six-second rounds.
        public const double MinimumCoverageSeconds = 12;

        public static bool IsTransient(EffectLeafExpression leaf)
        {
            return leaf != null && leaf.MaximumDurationSeconds.HasValue &&
                leaf.MaximumDurationSeconds.Value < MinimumCoverageSeconds;
        }

        public static bool ContainsTransient(EffectExpression expression)
        {
            return Leaves(expression).Any(IsTransient);
        }

        public static bool ContainsDurable(EffectExpression expression)
        {
            return Leaves(expression).Any(leaf => !IsTransient(leaf));
        }

        // The lasting-coverage obligation: transient leaves become no-ops;
        // every other node, branch and owner is preserved.
        public static EffectExpression Durable(EffectExpression expression)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) return IsTransient(leaf) ? (EffectExpression)EmptyEffectExpression.NoAction() : leaf;
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                var children = sequence.Children.Select(Durable).ToList();
                return children.SequenceEqual(sequence.Children) ? expression
                    : new SequenceEffectExpression(children, sequence.NearbyRecipients);
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                EffectExpression yes = Durable(conditional.WhenTrue), no = Durable(conditional.WhenFalse);
                return ReferenceEquals(yes, conditional.WhenTrue) && ReferenceEquals(no, conditional.WhenFalse) ? expression
                    : new ConditionalEffectExpression(conditional.ConditionContract, yes, no, conditional.CasterFactGate);
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                EffectExpression child = Durable(targeted.Child);
                return ReferenceEquals(child, targeted.Child) ? expression : new TargetedEffectExpression(targeted.Target, child);
            }
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null)
            {
                EffectExpression child = Durable(reference.Child);
                return ReferenceEquals(child, reference.Child) ? expression : new ReferencedAbilityExpression(reference.AbilityId, child);
            }
            return expression;
        }

        public static IEnumerable<EffectLeafExpression> Leaves(EffectExpression expression)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) return new[] { leaf };
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.SelectMany(Leaves);
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null) return Leaves(conditional.WhenTrue).Concat(Leaves(conditional.WhenFalse));
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return Leaves(targeted.Child);
            var reference = expression as ReferencedAbilityExpression;
            return reference == null ? new EffectLeafExpression[0] : Leaves(reference.Child);
        }
    }

    public sealed class SequenceEffectExpression : EffectExpression
    {
        public SequenceEffectExpression(IEnumerable<EffectExpression> children, NearbyRecipientRule nearbyRecipients = null)
            : base("sequence")
        {
            Children = new ReadOnlyCollection<EffectExpression>(
                new List<EffectExpression>(children ?? throw new ArgumentNullException("children")));
            NearbyRecipients = nearbyRecipients;
        }

        [JsonProperty("children", Order = 2)]
        public IReadOnlyList<EffectExpression> Children { get; private set; }
        [JsonProperty("nearbyRecipients", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
        public NearbyRecipientRule NearbyRecipients { get; private set; }
    }

    public sealed class ConditionalEffectExpression : EffectExpression
    {
        public ConditionalEffectExpression(
            string conditionContract,
            EffectExpression whenTrue,
            EffectExpression whenFalse,
            CasterFactGate casterFactGate = null)
            : base("conditional")
        {
            ConditionContract = conditionContract ?? string.Empty;
            WhenTrue = whenTrue ?? throw new ArgumentNullException("whenTrue");
            WhenFalse = whenFalse ?? throw new ArgumentNullException("whenFalse");
            CasterFactGate = casterFactGate;
        }

        [JsonProperty("conditionContract", Order = 2)] public string ConditionContract { get; private set; }
        [JsonProperty("whenTrue", Order = 3)] public EffectExpression WhenTrue { get; private set; }
        [JsonProperty("whenFalse", Order = 4)] public EffectExpression WhenFalse { get; private set; }
        // Inspection metadata, not part of catalogue/saved identity.
        [JsonProperty("casterFactGate", Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public CasterFactGate CasterFactGate { get; private set; }
    }

    public sealed class CasterFactGate
    {
        public CasterFactGate(bool any, IEnumerable<KeyValuePair<string, bool>> facts)
        {
            Any = any;
            Facts = new ReadOnlyCollection<KeyValuePair<string, bool>>(facts.ToList());
        }
        [JsonProperty("any")] public bool Any { get; private set; }
        // Value is the native condition's Not flag.
        [JsonProperty("facts")] public IReadOnlyList<KeyValuePair<string, bool>> Facts { get; private set; }
    }

    public static class EffectConditionBinding
    {
        public static EffectExpression Bind(EffectExpression expression,
            IReadOnlyDictionary<string, bool> casterFacts)
        {
            if (expression == null || casterFacts == null || casterFacts.Count == 0) return expression;
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                CasterFactGate gate = conditional.CasterFactGate;
                if (gate != null && gate.Facts.Count != 0 && gate.Facts.All(f => casterFacts.ContainsKey(f.Key)))
                {
                    bool result = gate.Any ? gate.Facts.Any(f => casterFacts[f.Key] != f.Value)
                        : gate.Facts.All(f => casterFacts[f.Key] != f.Value);
                    EffectExpression selected = Bind(result ? conditional.WhenTrue : conditional.WhenFalse, casterFacts);
                    var emptySequence = selected as SequenceEffectExpression;
                    return emptySequence != null && emptySequence.Children.Count == 0
                        ? EmptyEffectExpression.NoAction() : selected;
                }
                EffectExpression whenTrue = Bind(conditional.WhenTrue, casterFacts);
                EffectExpression whenFalse = Bind(conditional.WhenFalse, casterFacts);
                return ReferenceEquals(whenTrue, conditional.WhenTrue) && ReferenceEquals(whenFalse, conditional.WhenFalse)
                    ? expression : new ConditionalEffectExpression(conditional.ConditionContract, whenTrue, whenFalse, gate);
            }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                var children = sequence.Children.Select(c => Bind(c, casterFacts)).ToList();
                return children.SequenceEqual(sequence.Children) ? expression : new SequenceEffectExpression(children, sequence.NearbyRecipients);
            }
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null)
            {
                EffectExpression child = Bind(reference.Child, casterFacts);
                return ReferenceEquals(child, reference.Child) ? expression : new ReferencedAbilityExpression(reference.AbilityId, child);
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                EffectExpression child = Bind(targeted.Child, casterFacts);
                return ReferenceEquals(child, targeted.Child) ? expression : new TargetedEffectExpression(targeted.Target, child);
            }
            return expression;
        }

        public static IEnumerable<string> FactIds(EffectExpression expression)
        {
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return (conditional.CasterFactGate == null ? new string[0] : conditional.CasterFactGate.Facts.Select(f => f.Key))
                    .Concat(FactIds(conditional.WhenTrue)).Concat(FactIds(conditional.WhenFalse)).Distinct(StringComparer.Ordinal);
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.SelectMany(FactIds).Distinct(StringComparer.Ordinal);
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) return FactIds(reference.Child);
            var targeted = expression as TargetedEffectExpression;
            return targeted == null ? new string[0] : FactIds(targeted.Child);
        }
    }

    public sealed class TargetedEffectExpression : EffectExpression
    {
        public TargetedEffectExpression(EffectTarget target, EffectExpression child)
            : base("targeted")
        {
            Target = target;
            Child = child ?? throw new ArgumentNullException("child");
        }

        [JsonProperty("target", Order = 2)] public EffectTarget Target { get; private set; }
        [JsonProperty("child", Order = 3)] public EffectExpression Child { get; private set; }
    }

    public sealed class ReferencedAbilityExpression : EffectExpression
    {
        public ReferencedAbilityExpression(string abilityId, EffectExpression child)
            : base("ability-reference")
        {
            AbilityId = abilityId ?? string.Empty;
            Child = child ?? throw new ArgumentNullException("child");
        }

        [JsonProperty("abilityId", Order = 2)] public string AbilityId { get; private set; }
        [JsonProperty("child", Order = 3)] public EffectExpression Child { get; private set; }
    }

    public static class EffectExpressionAnalysis
    {
        public static bool ContainsLeaf(EffectExpression expression)
        {
            if (expression is EffectLeafExpression) return true;
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.Any(ContainsLeaf);
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return ContainsLeaf(conditional.WhenTrue) || ContainsLeaf(conditional.WhenFalse);
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return ContainsLeaf(targeted.Child);
            var referenced = expression as ReferencedAbilityExpression;
            return referenced != null && ContainsLeaf(referenced.Child);
        }
    }

    // A primary recipient is a spell target or one predicted group member.
    // Mixed payloads can additionally require effects on the caster. A wholly
    // personal payload follows its effective recipient (native infusion/Share).
    public sealed class EffectRecipientScope
    {
        private readonly string _caster, _recipient;
        private readonly bool _personal;
        public EffectRecipientScope(EffectExpression expression, string caster, string recipient)
        {
            _caster = caster; _recipient = recipient;
            var roles = Roles(expression, null).Distinct().ToList();
            _personal = roles.Count != 0 && roles.All(role => role == EffectTarget.Caster);
            UnitIds = new ReadOnlyCollection<string>(roles.Select(Owner).Concat(ExplicitOwners(expression)).Concat(new[] { recipient })
                .Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal).ToList());
        }
        public IReadOnlyList<string> UnitIds { get; private set; }
        public string Owner(EffectTarget role)
        { return role == EffectTarget.Caster && !_personal ? _caster : _recipient; }
        // A natively selected secondary obligation has no single owner: null
        // fails closed for any consumer that does not handle it explicitly.
        public string Owner(EffectLeafExpression leaf, EffectTarget role)
        { return leaf.SecondarySelection != null ? null : leaf.RecipientUnitId ?? Owner(role); }

        // Owners for lasting-coverage and forecast purposes: the PREDICTED
        // owners of a natively selected obligation (possibly none), otherwise
        // the single owner. Confirmation never uses this for a selection.
        public IReadOnlyList<string> PredictedOwners(EffectLeafExpression leaf, EffectTarget role)
        {
            if (leaf.SecondarySelection != null) return leaf.SecondarySelection.PredictedUnitIds;
            string owner = Owner(leaf, role);
            return owner == null ? new string[0] : new[] { owner };
        }

        public static bool ContainsSelection(EffectExpression expression)
        {
            return EffectApplicationLifetime.Leaves(expression).Any(leaf => leaf.SecondarySelection != null);
        }

        private static IEnumerable<string> ExplicitOwners(EffectExpression expression)
        {
            var leaf = expression as EffectLeafExpression;
            // A natively selected secondary owner may be any authorized
            // candidate: every candidate is observed, from before submission.
            if (leaf != null) return (leaf.RecipientUnitId == null ? new string[0] : new[] { leaf.RecipientUnitId })
                .Concat(leaf.SecondarySelection == null ? new string[0] : leaf.SecondarySelection.CandidateUnitIds);
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.SelectMany(ExplicitOwners);
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null) return ExplicitOwners(conditional.WhenTrue).Concat(ExplicitOwners(conditional.WhenFalse));
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return ExplicitOwners(targeted.Child);
            var reference = expression as ReferencedAbilityExpression;
            return reference == null ? new string[0] : ExplicitOwners(reference.Child);
        }

        public static EffectTarget Resolve(EffectTarget role, EffectTarget? outer)
        { return role == EffectTarget.CurrentTarget && outer.HasValue ? outer.Value : role; }

        private static IEnumerable<EffectTarget> Roles(EffectExpression expression, EffectTarget? outer)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) return new[] { Resolve(leaf.Target, outer) };
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.SelectMany(child => Roles(child, outer));
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null) return Roles(conditional.WhenTrue, outer).Concat(Roles(conditional.WhenFalse, outer));
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return Roles(targeted.Child, Resolve(targeted.Target, outer));
            var reference = expression as ReferencedAbilityExpression;
            return reference == null ? new EffectTarget[0] : Roles(reference.Child, outer);
        }
    }
}
