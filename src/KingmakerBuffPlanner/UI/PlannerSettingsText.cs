using KingmakerBuffPlanner.Persistence;

namespace KingmakerBuffPlanner.UI
{
    // What the mod's Unity Mod Manager settings page says about the planner
    // (everyday-use v1.2 §3). The casting-first graph is the only normal
    // planner: no Classic choice is offered and nothing calls it
    // experimental or asks for an acceptance step. A stored explicit
    // "classic" choice from an earlier version is honored during the
    // bounded retirement, and the page then offers exactly one way
    // forward (never back).
    public static class PlannerSettingsText
    {
        public const string Heading = "Buff Planner";

        public const string SwitchToCastingFirst = "Switch to the casting-first planner";

        // null = the planner has not loaded yet (the page can draw before
        // the first update constructs it).
        public static string Describe(PlannerMode? mode, string hotkey)
        {
            if (mode == null) return "The planner loads with the game.";
            string open = "Open it with " + hotkey + " or the HUD's Setup (gear) button; " +
                "the HUD's moon button runs your Long routine.";
            if (mode == PlannerMode.CastingFirst)
                return "Each saved casting is exactly one cast - its own caster, spell source, " +
                    "target and enhancements. Every edit saves itself. " + open;
            return "This game still uses the Classic planner, chosen in an earlier version. " +
                "The casting-first planner replaces it: your classic plan is imported once and " +
                "kept unchanged on disk.";
        }

        // Whether the one-way switch is offered (only from a stored
        // classic choice).
        public static bool OffersSwitch(PlannerMode? mode)
        {
            return mode == PlannerMode.Classic;
        }

        public static string Switched(string hotkey)
        {
            return "The casting-first planner is now your planner. Open it with " + hotkey +
                " to see the imported plan.";
        }
    }
}
