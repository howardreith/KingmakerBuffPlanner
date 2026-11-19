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
        public static string Heading { get { return PlannerText.Get("settings.heading"); } }

        public static string SwitchToCastingFirst { get { return PlannerText.Get("settings.switch"); } }

        // null = the planner has not loaded yet (the page can draw before
        // the first update constructs it).
        public static string Describe(PlannerMode? mode, string hotkey)
        {
            if (mode == null) return PlannerText.Get("settings.loading");
            return mode == PlannerMode.CastingFirst ? PlannerText.Get("settings.current", hotkey) : PlannerText.Get("settings.classic");
        }

        // Whether the one-way switch is offered (only from a stored
        // classic choice).
        public static bool OffersSwitch(PlannerMode? mode)
        {
            return mode == PlannerMode.Classic;
        }

        public static string Switched(string hotkey)
        {
            return PlannerText.Get("settings.switched", hotkey);
        }
    }
}
