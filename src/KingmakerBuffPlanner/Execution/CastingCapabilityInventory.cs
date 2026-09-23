using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.Domain.Effects;
using KingmakerBuffPlanner.Domain.Planning;
using KingmakerBuffPlanner.Domain.Providers;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Execution
{
    // The party's casting capabilities as the planner's own discovery sees
    // them, as read-only evidence for choosing honest qualification cases:
    // units and pets, every resource pool, every provider option (source,
    // caster, spellbook, level, pool and remaining count, reserved tokens,
    // caster level, duration, execution strategy, reachable targets, legal
    // anchors and anchor coverage, effect shape) and every enhancement.
    // Nothing is cast, spent or changed.
    public static class CastingCapabilityInventory
    {
        public static IList<string> Describe(CastingWorkspaceInputs inputs)
        {
            var lines = new List<string>();
            if (inputs == null || inputs.Snapshot == null)
            {
                lines.Add("inputs-unavailable");
                return lines;
            }
            foreach (UnitSnapshot unit in inputs.Snapshot.Units)
                lines.Add("unit=" + unit.UnitId + ";name=" + unit.DisplayName + ";pet=" + unit.IsPet +
                    ";master=" + (string.IsNullOrEmpty(unit.MasterUnitId) ? "none" : unit.MasterUnitId) +
                    ";targetable=" + (unit.TargetValidation != null && unit.TargetValidation.Alive &&
                        unit.TargetValidation.Conscious && unit.TargetValidation.Friendly &&
                        unit.TargetValidation.Targetable));
            var pools = new Dictionary<string, ResourcePoolSnapshot>(StringComparer.Ordinal);
            foreach (ResourcePoolSnapshot pool in inputs.Snapshot.ResourcePools
                .OrderBy(value => value.PoolKey, StringComparer.Ordinal))
            {
                if (!pools.ContainsKey(pool.PoolKey)) pools.Add(pool.PoolKey, pool);
                lines.Add("pool=" + pool.PoolKey + ";kind=" + pool.Kind + ";remaining=" + pool.Remaining +
                    "/" + pool.Capacity + ";tokens=" + pool.Tokens.Count + ";availableTokens=" +
                    pool.Tokens.Count(token => token.Available) + ";linkedTokens=" +
                    pool.Tokens.Count(token => token.LinkedTokenIds.Count != 0) + ";tokenLevels=" +
                    string.Join(",", pool.Tokens.Select(token => token.SpellLevel.ToString())
                        .Distinct().ToArray()));
            }
            foreach (ProviderPlanningOption option in inputs.ProviderOptions
                .Where(value => value != null && value.Provider != null)
                .OrderBy(value => value.Provider.Key.Canonical, StringComparer.Ordinal))
            {
                ProviderSnapshot provider = option.Provider;
                ResourcePoolSnapshot pool;
                pools.TryGetValue(provider.ResourcePoolKey ?? string.Empty, out pool);
                EffectExpression effect;
                inputs.EffectsBySource.TryGetValue(provider.Key.Ability.Canonical, out effect);
                int widest = option.RecipientIdsByAnchor.Count == 0 ? 0
                    : option.RecipientIdsByAnchor.Values.Max(value => value.Count);
                lines.Add("provider=" + provider.Key.Ability.Canonical + ";name=" + provider.DisplayName +
                    ";source=" + provider.SourceDisplayName + ";caster=" + provider.Key.CasterUnitId +
                    ";book=" + (string.IsNullOrEmpty(provider.Key.SpellbookGuid) ? "none" : provider.Key.SpellbookGuid) +
                    ";kind=" + provider.Key.Ability.SourceKind + ";instance=" + provider.Key.SourceInstanceId +
                    ";level=" + provider.SpellLevel + ";variant=" + provider.IsConcreteVariant +
                    ";metamagic=" + provider.Key.Ability.MetamagicMask +
                    ";pool=" + (pool == null ? "missing:" + provider.ResourcePoolKey
                        : pool.Kind + ":" + pool.Remaining + "/" + pool.Capacity) +
                    ";unitsPerCast=" + provider.UnitsPerCast + ";tokens=" + provider.EligibleTokenIds.Count +
                    ";material=" + (provider.MaterialComponent == null ? "none"
                        : provider.MaterialComponent.AvailableCount + "/" + provider.MaterialComponent.RequiredCount) +
                    ";cl=" + provider.EffectiveCasterLevel + ";rounds=" + provider.ExpectedDurationRounds +
                    ";strategy=" + option.ExecutionStrategy + ";targets=" + option.ReachableTargetIds.Count +
                    ";anchors=" + option.LegalAnchorIds.Count + ";widestAnchorCoverage=" + widest +
                    ";shape=" + Shape(effect));
            }
            foreach (CastEnhancementSnapshot enhancement in inputs.Enhancements ?? new CastEnhancementSnapshot[0])
                lines.Add("enhancement=" + enhancement.EnhancementId + ";name=" + enhancement.DisplayName +
                    ";caster=" + enhancement.CasterUnitId + ";category=" + enhancement.Category +
                    ";metamagic=" + enhancement.MetamagicMask + ";maxLevel=" + enhancement.MaximumSpellLevel +
                    ";uses=" + (enhancement.RemainingUses.HasValue ? enhancement.RemainingUses.Value.ToString() : "unlimited"));
            return lines;
        }

        // The recipients an effect reaches, from its leaf and targeted
        // nodes: "direct" (the cast's target), "self", "pet", "party",
        // "allied-area", "enemy-area", "ambiguous-area", combined with "+".
        public static string Shape(EffectExpression effect)
        {
            var targets = new SortedSet<string>(StringComparer.Ordinal);
            Collect(effect, targets);
            return targets.Count == 0 ? "none" : string.Join("+", targets.ToArray());
        }

        private static void Collect(EffectExpression expression, SortedSet<string> targets)
        {
            var leaf = expression as EffectLeafExpression;
            if (leaf != null) { targets.Add(Name(leaf.Target)); return; }
            var sequence = expression as SequenceEffectExpression;
            if (sequence != null)
            {
                foreach (EffectExpression child in sequence.Children) Collect(child, targets);
                return;
            }
            var conditional = expression as ConditionalEffectExpression;
            if (conditional != null)
            {
                Collect(conditional.WhenTrue, targets);
                Collect(conditional.WhenFalse, targets);
                return;
            }
            var targeted = expression as TargetedEffectExpression;
            if (targeted != null)
            {
                var inner = new SortedSet<string>(StringComparer.Ordinal);
                Collect(targeted.Child, inner);
                targets.Add(Name(targeted.Target) + (inner.Count == 0 ? string.Empty
                    : "(" + string.Join("+", inner.ToArray()) + ")"));
                return;
            }
            var referenced = expression as ReferencedAbilityExpression;
            if (referenced != null) Collect(referenced.Child, targets);
        }

        private static string Name(EffectTarget target)
        {
            switch (target)
            {
                case EffectTarget.CurrentTarget: return "direct";
                case EffectTarget.Caster: return "self";
                case EffectTarget.Pet: return "pet";
                case EffectTarget.Party: return "party";
                case EffectTarget.AlliedAreaRecipients: return "allied-area";
                case EffectTarget.EnemyAreaRecipients: return "enemy-area";
                default: return "ambiguous-area";
            }
        }
    }
}
