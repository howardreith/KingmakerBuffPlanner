using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        AreaRecipients
    }

    public abstract class EffectExpression
    {
        protected EffectExpression(string expressionType)
        {
            ExpressionType = expressionType;
        }

        public string ExpressionType { get; private set; }
    }

    public sealed class EmptyEffectExpression : EffectExpression
    {
        public EmptyEffectExpression() : base("empty") { }
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

        public EffectKind Kind { get; private set; }
        public string EffectId { get; private set; }
        public EffectTarget Target { get; private set; }
        public string SourceContract { get; private set; }
        public string ActionPath { get; private set; }
    }

    public sealed class SequenceEffectExpression : EffectExpression
    {
        public SequenceEffectExpression(IEnumerable<EffectExpression> children)
            : base("sequence")
        {
            Children = new ReadOnlyCollection<EffectExpression>(
                new List<EffectExpression>(children ?? throw new ArgumentNullException("children")));
        }

        public IReadOnlyList<EffectExpression> Children { get; private set; }
    }

    public sealed class ConditionalEffectExpression : EffectExpression
    {
        public ConditionalEffectExpression(
            string conditionContract,
            EffectExpression whenTrue,
            EffectExpression whenFalse)
            : base("conditional")
        {
            ConditionContract = conditionContract ?? string.Empty;
            WhenTrue = whenTrue ?? throw new ArgumentNullException("whenTrue");
            WhenFalse = whenFalse ?? throw new ArgumentNullException("whenFalse");
        }

        public string ConditionContract { get; private set; }
        public EffectExpression WhenTrue { get; private set; }
        public EffectExpression WhenFalse { get; private set; }
    }

    public sealed class TargetedEffectExpression : EffectExpression
    {
        public TargetedEffectExpression(EffectTarget target, EffectExpression child)
            : base("targeted")
        {
            Target = target;
            Child = child ?? throw new ArgumentNullException("child");
        }

        public EffectTarget Target { get; private set; }
        public EffectExpression Child { get; private set; }
    }

    public sealed class ReferencedAbilityExpression : EffectExpression
    {
        public ReferencedAbilityExpression(string abilityId, EffectExpression child)
            : base("ability-reference")
        {
            AbilityId = abilityId ?? string.Empty;
            Child = child ?? throw new ArgumentNullException("child");
        }

        public string AbilityId { get; private set; }
        public EffectExpression Child { get; private set; }
    }
}
