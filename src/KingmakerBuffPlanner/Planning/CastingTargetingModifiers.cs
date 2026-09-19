using System;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Planning
{
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
    // between previews or runs.
    public interface ICastingTargetingModifier
    {
        string ModifierId { get; }

        CastingModifierResult Apply(PlannedCasting casting, ProviderPlanningOption option);
    }
}
