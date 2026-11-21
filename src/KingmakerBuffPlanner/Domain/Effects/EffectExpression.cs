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
        public EmptyEffectExpression(string unmodeledReason) : this(unmodeledReason, null) { }

        // removal: the exact native ContextActionRemoveBuff this node stands
        // for, when known (missing-beneficial-buffs mission §3.B). Like the
        // unmodeled reason it is planning metadata only: never serialized
        // and never part of a catalogue or saved identity
        // (EffectAggregateIdentity describes every empty node as "empty").
        public EmptyEffectExpression(string unmodeledReason, EffectRemoval removal) : base("empty")
        {
            UnmodeledReason = unmodeledReason;
            Removal = removal;
        }

        public static EmptyEffectExpression NoAction() { return new EmptyEffectExpression(null); }

        [JsonIgnore] public string UnmodeledReason { get; private set; }

        [JsonIgnore] public EffectRemoval Removal { get; private set; }

        [JsonIgnore] public bool IsNoAction { get { return UnmodeledReason == null; } }
    }

    // One buff a casting's own native actions remove, and whose (the
    // removal's resolved recipient role, as for an effect leaf).
    public sealed class EffectRemoval
    {
        public EffectRemoval(string effectId, EffectTarget target)
        {
            if (string.IsNullOrWhiteSpace(effectId)) throw new ArgumentException("Effect ID is required.", "effectId");
            EffectId = effectId;
            Target = target;
        }

        public string EffectId { get; private set; }
        public EffectTarget Target { get; private set; }
    }

    // Missing-beneficial-buffs mission §3.B: the buffs a casting removes
    // (on any branch: a conditional removal can replace the application),
    // each with its recipient role resolved through the enclosing target
    // transforms. Mutually exclusive selections (the wizard's Abjuration
    // resistances: each applies its own buff and removes the other four)
    // are recognized from these native removals, never from names.
    public static class EffectRemovals
    {
        public static IReadOnlyList<EffectRemoval> Collect(EffectExpression expression)
        {
            var removals = new List<EffectRemoval>();
            Collect(expression, null, removals);
            return removals.AsReadOnly();
        }

        private static void Collect(EffectExpression expression, EffectTarget? outer, List<EffectRemoval> removals)
        {
            var empty = expression as EmptyEffectExpression;
            if (empty != null)
            {
                if (empty.Removal != null)
                    removals.Add(new EffectRemoval(empty.Removal.EffectId,
                        EffectRecipientScope.Resolve(empty.Removal.Target, outer)));
                return;
            }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children) Collect(child, outer, removals);
                return;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                Collect(conditional.WhenTrue, outer, removals);
                Collect(conditional.WhenFalse, outer, removals);
                return;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                Collect(targeted.Child, EffectRecipientScope.Resolve(targeted.Target, outer), removals);
                return;
            }
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) Collect(reference.Child, outer, removals);
        }
    }

    // What one casting's BOUND expression (caster facts and nearby
    // recipients resolved as for execution) does to one primary recipient's
    // scope, by owner unit: the effects it may apply, and the effects it may
    // remove that are not guaranteed to be applied again later on the same
    // path (review R1-F3). Branch semantics and action order are kept:
    // - a removal counts on any branch the binding left possible (a
    //   known-false branch is gone after binding; an unknown one may run);
    // - a later application cancels an earlier removal only when it is
    //   certain on that path (in a sequence after the removal, or on BOTH
    //   alternatives of a later conditional); an application on an
    //   alternative branch never proves the removed effect is restored.
    public sealed class EffectChanges
    {
        internal EffectChanges(IEnumerable<KeyValuePair<string, string>> applied,
            IEnumerable<KeyValuePair<string, string>> removed,
            IEnumerable<KeyValuePair<string, string>> guaranteed = null)
        {
            Applied = applied.Distinct().ToList().AsReadOnly();
            Removed = removed.Distinct().ToList().AsReadOnly();
            Guaranteed = (guaranteed ?? new KeyValuePair<string, string>[0]).Distinct().ToList().AsReadOnly();
        }

        // owner unit id -> effect id the casting may apply there.
        public IReadOnlyList<KeyValuePair<string, string>> Applied { get; private set; }
        // owner unit id -> effect id the casting may remove there and does
        // not certainly re-apply afterwards.
        public IReadOnlyList<KeyValuePair<string, string>> Removed { get; private set; }
        // owner unit id -> effect id the casting certainly leaves applied at
        // its end on every path (review R1-F3/R2-F1: only these restore an
        // earlier removal or justify a structural "already granted").
        public IReadOnlyList<KeyValuePair<string, string>> Guaranteed { get; private set; }

        public static EffectChanges Merge(IEnumerable<EffectChanges> parts)
        {
            List<EffectChanges> list = (parts ?? new EffectChanges[0]).Where(p => p != null).ToList();
            return new EffectChanges(list.SelectMany(p => p.Applied), list.SelectMany(p => p.Removed),
                list.SelectMany(p => p.Guaranteed));
        }
    }

    public static class EffectChangeAnalysis
    {
        private sealed class State
        {
            internal readonly HashSet<KeyValuePair<string, string>> Certain = new HashSet<KeyValuePair<string, string>>();
            internal readonly HashSet<KeyValuePair<string, string>> Possible = new HashSet<KeyValuePair<string, string>>();
            internal readonly HashSet<KeyValuePair<string, string>> Removed = new HashSet<KeyValuePair<string, string>>();
        }

        public static EffectChanges Analyze(EffectExpression boundExpression, string casterUnitId,
            string recipientUnitId)
        {
            if (boundExpression == null) return new EffectChanges(new KeyValuePair<string, string>[0],
                new KeyValuePair<string, string>[0]);
            var scope = new EffectRecipientScope(boundExpression, casterUnitId, recipientUnitId);
            State state = Visit(boundExpression, null, scope);
            return new EffectChanges(state.Possible, state.Removed, state.Certain);
        }

        private static State Visit(EffectExpression expression, EffectTarget? outer, EffectRecipientScope scope)
        {
            var state = new State();
            var leaf = expression as EffectLeafExpression;
            if (leaf != null)
            {
                EffectTarget role = EffectRecipientScope.Resolve(leaf.Target, outer);
                foreach (string owner in scope.PredictedOwners(leaf, role))
                {
                    var key = new KeyValuePair<string, string>(owner, leaf.EffectId);
                    state.Possible.Add(key);
                    // A natively selected owner is a prediction, never certain.
                    if (leaf.SecondarySelection == null) state.Certain.Add(key);
                }
                return state;
            }
            var empty = expression as EmptyEffectExpression;
            if (empty != null)
            {
                if (empty.Removal != null)
                {
                    string owner = scope.Owner(EffectRecipientScope.Resolve(empty.Removal.Target, outer));
                    if (!string.IsNullOrEmpty(owner))
                        state.Removed.Add(new KeyValuePair<string, string>(owner, empty.Removal.EffectId));
                }
                return state;
            }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children)
                {
                    State next = Visit(child, outer, scope);
                    // A later certain application restores an earlier
                    // removal; a later removal undoes an earlier certainty.
                    state.Removed.ExceptWith(next.Certain);
                    state.Certain.ExceptWith(next.Removed);
                    state.Removed.UnionWith(next.Removed);
                    state.Certain.UnionWith(next.Certain);
                    state.Possible.UnionWith(next.Possible);
                }
                return state;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                State yes = Visit(conditional.WhenTrue, outer, scope);
                State no = Visit(conditional.WhenFalse, outer, scope);
                state.Certain.UnionWith(yes.Certain);
                state.Certain.IntersectWith(no.Certain);
                state.Possible.UnionWith(yes.Possible);
                state.Possible.UnionWith(no.Possible);
                state.Removed.UnionWith(yes.Removed);
                state.Removed.UnionWith(no.Removed);
                return state;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
                return Visit(targeted.Child, EffectRecipientScope.Resolve(targeted.Target, outer), scope);
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) return Visit(reference.Child, outer, scope);
            return state;
        }
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
                    : new ConditionalEffectExpression(conditional.ConditionContract, yes, no, conditional.CasterFactGate,
                        conditional.RecipientFactGate);
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
            CasterFactGate casterFactGate = null,
            RecipientFactGate recipientFactGate = null)
            : base("conditional")
        {
            ConditionContract = conditionContract ?? string.Empty;
            WhenTrue = whenTrue ?? throw new ArgumentNullException("whenTrue");
            WhenFalse = whenFalse ?? throw new ArgumentNullException("whenFalse");
            CasterFactGate = casterFactGate;
            RecipientFactGate = recipientFactGate;
        }

        // A condition on the recipient's own facts (review R2-F5). Planning
        // metadata only: never serialized, never part of an identity.
        [JsonIgnore] public RecipientFactGate RecipientFactGate { get; private set; }

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

    // A conditional on facts of the unit the action runs on (native
    // ContextConditionHasFact): Value is the condition's Not flag, as for
    // CasterFactGate.
    public sealed class RecipientFactGate
    {
        public RecipientFactGate(bool any, IEnumerable<KeyValuePair<string, bool>> facts)
        {
            Any = any;
            Facts = new ReadOnlyCollection<KeyValuePair<string, bool>>(facts.ToList());
        }
        public bool Any { get; private set; }
        public IReadOnlyList<KeyValuePair<string, bool>> Facts { get; private set; }
    }

    // Review R2-F5: resolves recipient-fact gates for one recipient. A gate
    // whose facts are all read selects its branch; a gate with an unread
    // fact stays and is reported (unresolved), so a consumer can withhold a
    // definite coverage claim instead of guessing.
    public static class EffectRecipientFactBinding
    {
        public static bool HasGates(EffectExpression expression)
        {
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return conditional.RecipientFactGate != null || HasGates(conditional.WhenTrue) ||
                    HasGates(conditional.WhenFalse);
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.Any(HasGates);
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) return HasGates(reference.Child);
            var targeted = expression as TargetedEffectExpression;
            return targeted != null && HasGates(targeted.Child);
        }

        public static EffectExpression Bind(EffectExpression expression, Func<string, bool?> recipientHasFact,
            out bool unresolved)
        {
            bool open = false;
            EffectExpression bound = Resolve(expression, recipientHasFact ?? (id => null), ref open);
            unresolved = open;
            return bound;
        }

        private static EffectExpression Resolve(EffectExpression expression, Func<string, bool?> hasFact, ref bool open)
        {
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                RecipientFactGate gate = conditional.RecipientFactGate;
                if (gate != null && gate.Facts.Count != 0)
                {
                    var values = gate.Facts.Select(f => new { f.Value, Has = hasFact(f.Key) }).ToList();
                    if (values.All(v => v.Has.HasValue))
                    {
                        bool result = gate.Any ? values.Any(v => v.Has.Value != v.Value)
                            : values.All(v => v.Has.Value != v.Value);
                        return Resolve(result ? conditional.WhenTrue : conditional.WhenFalse, hasFact, ref open);
                    }
                    open = true;
                }
                EffectExpression yes = Resolve(conditional.WhenTrue, hasFact, ref open);
                EffectExpression no = Resolve(conditional.WhenFalse, hasFact, ref open);
                return ReferenceEquals(yes, conditional.WhenTrue) && ReferenceEquals(no, conditional.WhenFalse)
                    ? expression : new ConditionalEffectExpression(conditional.ConditionContract, yes, no,
                        conditional.CasterFactGate, gate);
            }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                var children = new List<EffectExpression>();
                foreach (EffectExpression child in sequence.Children) children.Add(Resolve(child, hasFact, ref open));
                return children.SequenceEqual(sequence.Children) ? expression
                    : new SequenceEffectExpression(children, sequence.NearbyRecipients);
            }
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null)
            {
                EffectExpression child = Resolve(reference.Child, hasFact, ref open);
                return ReferenceEquals(child, reference.Child) ? expression : new ReferencedAbilityExpression(reference.AbilityId, child);
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                EffectExpression child = Resolve(targeted.Child, hasFact, ref open);
                return ReferenceEquals(child, targeted.Child) ? expression : new TargetedEffectExpression(targeted.Target, child);
            }
            return expression;
        }

        public static IEnumerable<string> FactIds(EffectExpression expression)
        {
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return (conditional.RecipientFactGate == null ? new string[0]
                        : conditional.RecipientFactGate.Facts.Select(f => f.Key))
                    .Concat(FactIds(conditional.WhenTrue)).Concat(FactIds(conditional.WhenFalse))
                    .Distinct(StringComparer.Ordinal);
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.SelectMany(FactIds).Distinct(StringComparer.Ordinal);
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) return FactIds(reference.Child);
            var targeted = expression as TargetedEffectExpression;
            return targeted == null ? new string[0] : FactIds(targeted.Child);
        }
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
                    ? expression : new ConditionalEffectExpression(conditional.ConditionContract, whenTrue, whenFalse, gate,
                        conditional.RecipientFactGate);
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

        // Missing-beneficial-buffs review R5-F3: a leaf reached without
        // passing an UNMODELLED conditional - one whose native checker has
        // conditions but was expressed as neither a caster- nor a
        // recipient-fact gate (a single checker mixing both predicate types,
        // or any other contract). Such a leaf depends on a predicate the
        // planner cannot evaluate, so it gives no definite coverage. A
        // conditional with a gate is descended (its unresolved gates are the
        // caller's to judge); one with no conditions always runs as before.
        public static bool ContainsDefiniteLeaf(EffectExpression expression)
        {
            if (expression is EffectLeafExpression) return true;
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.Any(ContainsDefiniteLeaf);
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return !IsUnmodelledConditional(conditional) &&
                    (ContainsDefiniteLeaf(conditional.WhenTrue) || ContainsDefiniteLeaf(conditional.WhenFalse));
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null) return ContainsDefiniteLeaf(targeted.Child);
            var referenced = expression as ReferencedAbilityExpression;
            return referenced != null && ContainsDefiniteLeaf(referenced.Child);
        }

        public static bool IsUnmodelledConditional(ConditionalEffectExpression conditional)
        {
            if (conditional == null || conditional.CasterFactGate != null || conditional.RecipientFactGate != null)
                return false;
            string contract = conditional.ConditionContract ?? string.Empty;
            return contract.Length != 0 && !contract.EndsWith(":", StringComparison.Ordinal);
        }

        // A caster-fact gate left anywhere in the expression (unread caster fact).
        public static bool HasCasterGate(EffectExpression expression)
        {
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
                return conditional.CasterFactGate != null || HasCasterGate(conditional.WhenTrue) ||
                    HasCasterGate(conditional.WhenFalse);
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null) return sequence.Children.Any(HasCasterGate);
            var reference = expression as ReferencedAbilityExpression;
            if (reference != null) return HasCasterGate(reference.Child);
            var targeted = expression as TargetedEffectExpression;
            return targeted != null && HasCasterGate(targeted.Child);
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
