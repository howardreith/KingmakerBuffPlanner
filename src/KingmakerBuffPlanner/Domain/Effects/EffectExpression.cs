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
            string actionPath)
            : base("leaf")
        {
            if (string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException("Effect ID is required.", "effectId");
            Kind = kind;
            EffectId = effectId;
            Target = target;
            SourceContract = sourceContract ?? string.Empty;
            ActionPath = actionPath ?? string.Empty;
        }

        [JsonProperty("kind", Order = 2)] public EffectKind Kind { get; private set; }
        [JsonProperty("effectId", Order = 3)] public string EffectId { get; private set; }
        [JsonProperty("target", Order = 4)] public EffectTarget Target { get; private set; }
        [JsonProperty("sourceContract", Order = 5)] public string SourceContract { get; private set; }
        [JsonProperty("actionPath", Order = 6)] public string ActionPath { get; private set; }
    }

    public sealed class SequenceEffectExpression : EffectExpression
    {
        public SequenceEffectExpression(IEnumerable<EffectExpression> children)
            : base("sequence")
        {
            Children = new ReadOnlyCollection<EffectExpression>(
                new List<EffectExpression>(children ?? throw new ArgumentNullException("children")));
        }

        [JsonProperty("children", Order = 2)]
        public IReadOnlyList<EffectExpression> Children { get; private set; }
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
                return children.SequenceEqual(sequence.Children) ? expression : new SequenceEffectExpression(children);
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
}
