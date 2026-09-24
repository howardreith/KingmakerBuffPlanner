using System.Globalization;

namespace KingmakerBuffPlanner.Execution
{
    // The game's cast count for a source ability, read around one cast
    // (batch 3 review A7). An unread count is null, never the game's own
    // unlimited (negative). A finite count that fell is a spend. A free
    // (unlimited) reservation must read unlimited and unchanged on both
    // sides; an unread count there is uncertainty, never success.
    public static class AvailableCountJudgement
    {
        public const string UncertainPrefix = "unlimited-source-count-uncertain:";

        public static bool Spent(int? before, int? after)
        {
            return before.HasValue && after.HasValue && before.Value >= 0 && after.Value >= 0 &&
                after.Value < before.Value;
        }

        // Null when the counts prove a free casting spent nothing.
        public static string FreeViolation(int? before, int? after)
        {
            if (!before.HasValue || !after.HasValue)
                return "available-count-unread:" + Format(before) + ">" + Format(after);
            if (before.Value >= 0 || after.Value != before.Value)
                return "available-count-not-unlimited:" + Format(before) + ">" + Format(after);
            return null;
        }

        public static string Format(int? count)
        {
            return count.HasValue ? count.Value.ToString(CultureInfo.InvariantCulture) : "unread";
        }
    }
}
