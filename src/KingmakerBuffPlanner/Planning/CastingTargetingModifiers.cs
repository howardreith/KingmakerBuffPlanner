using System;
using System.Collections.Generic;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Planning
{
    // The verified native cost of one applied targeting modifier: a usage
    // pool (for example a class-resource reservoir) and the units it
    // charges. Modifier demands enter the same atomic cost vector as
    // spell/slot/enhancement/material demands, so two features spending one
    // pool are validated as combined demand. An unvalidated modifier blocks
    // its casting; unknown modifier costs are never treated as free.
    public sealed class ModifierUsageDemand
    {
        public ModifierUsageDemand(string usagePoolId, int units)
        {
            if (string.IsNullOrWhiteSpace(usagePoolId))
                throw new ArgumentException("Usage pool ID is required.", "usagePoolId");
            if (units < 1)
                throw new ArgumentOutOfRangeException("units");
            UsagePoolId = usagePoolId;
            Units = units;
        }

        public string UsagePoolId { get; private set; }
        public int Units { get; private set; }
    }

    // The outcome of one registered targeting modifier against one casting's
    // resolved provider option. A modifier may only narrow or expand the
    // proven option's targeting (reachability, anchors, coverage); it never
    // changes the invocation count or substitutes the caster.
    public sealed class CastingModifierResult
    {
        private CastingModifierResult(
            bool applied, ProviderPlanningOption option, string unavailableReason)
        {
            IsApplied = applied;
            Option = option;
            UnavailableReason = unavailableReason ?? string.Empty;
        }

        public bool IsApplied { get; private set; }
        public ProviderPlanningOption Option { get; private set; }
        public string UnavailableReason { get; private set; }

        public static CastingModifierResult Applied(ProviderPlanningOption option)
        {
            if (option == null)
                throw new ArgumentNullException("option");
            return new CastingModifierResult(true, option, string.Empty);
        }

        public static CastingModifierResult Unavailable(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason is required.", "reason");
            return new CastingModifierResult(false, null, reason);
        }
    }

    // A targeting modifier (for example Share Transmutation) selected on a
    // casting. Implementations must be pure: the same casting and option
    // always produce the same result, so recompilation never leaks state
    // between previews or runs. A modifier that consumes resources or
    // changes temporary native state declares those demands so the shared
    // budget validates them atomically; its native setup and restoration
    // belong to the execution phase.
    public interface ICastingTargetingModifier
    {
        string ModifierId { get; }

        CastingModifierResult Apply(PlannedCasting casting, ProviderPlanningOption option);

        IReadOnlyList<ModifierUsageDemand> UsageDemands(
            PlannedCasting casting, ProviderPlanningOption option);
    }
}
