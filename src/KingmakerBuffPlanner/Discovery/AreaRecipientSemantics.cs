using System;
using KingmakerBuffPlanner.Domain.Effects;

namespace KingmakerBuffPlanner.Discovery
{
    internal enum AreaSelectionTarget
    {
        Enemy,
        Ally,
        Any,
        Unknown
    }

    // An Any selector is not inherently allied. It is refined only when the surrounding
    // ability contract proves friend targeting and excludes enemy and point targeting.
    internal static class AreaRecipientSemantics
    {
        internal static EffectTarget Resolve(
            AreaSelectionTarget selector,
            bool canTargetFriends,
            bool canTargetEnemies,
            bool canTargetPoint)
        {
            if (selector == AreaSelectionTarget.Ally)
                return EffectTarget.AlliedAreaRecipients;
            if (selector == AreaSelectionTarget.Enemy)
                return EffectTarget.EnemyAreaRecipients;
            if (selector == AreaSelectionTarget.Any && canTargetFriends &&
                !canTargetEnemies && !canTargetPoint)
                return EffectTarget.AlliedAreaRecipients;
            return EffectTarget.AmbiguousAreaRecipients;
        }

        internal static bool IsAllied(
            AreaSelectionTarget selector,
            bool canTargetFriends,
            bool canTargetEnemies,
            bool canTargetPoint)
        {
            return Resolve(selector, canTargetFriends, canTargetEnemies,
                canTargetPoint) == EffectTarget.AlliedAreaRecipients;
        }
    }
}
