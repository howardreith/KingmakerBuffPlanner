using System;

namespace KingmakerBuffPlanner.Execution
{
    internal static class MaterialComponentAvailability
    {
        internal static bool IsSatisfied(bool required, Func<bool> hasEnough)
        {
            if (!required) return true;
            if (hasEnough == null) throw new ArgumentNullException("hasEnough");
            return hasEnough();
        }
    }
}
