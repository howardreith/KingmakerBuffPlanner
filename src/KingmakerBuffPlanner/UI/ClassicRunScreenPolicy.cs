using System;

namespace KingmakerBuffPlanner.UI
{
    // Focused re-review of the Classic run: the rules for the Classic screen
    // around a run, kept apart from the Unity view so they are tested.
    internal static class ClassicRunScreenPolicy
    {
        // Decided once, right after the press: an accepted run (the session
        // is executing) started from the open Classic screen closes it, since
        // it pauses the game. A planner opened later in the run stays open.
        internal static bool CloseAfterPress(bool accepted, bool screenOpen)
        {
            return accepted && screenOpen;
        }

        // A result kept while the screen was closed is shown only for the
        // campaign it belongs to.
        internal static bool ShowStashedResult(string stashedCampaignId, string currentCampaignId)
        {
            return !string.IsNullOrEmpty(stashedCampaignId) &&
                string.Equals(stashedCampaignId, currentCampaignId, StringComparison.Ordinal);
        }
    }
}
