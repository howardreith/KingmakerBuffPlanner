using System;

namespace KingmakerBuffPlanner.UI
{
    internal static class PlannerHotkeyBinding
    {
        internal const string Default = "Ctrl+Shift+B";
        internal const string Fallback = "Ctrl+Shift+P";

        internal static string Normalize(string value)
        {
            return string.Equals(value, Fallback, StringComparison.Ordinal)
                ? Fallback : Default;
        }

        internal static string PrimaryKey(string binding)
        {
            return Normalize(binding) == Fallback ? "P" : "B";
        }

        internal static bool ShouldSuppress(string binding, string nativeKey,
            string nativeName, bool ctrl, bool shift, bool alt)
        {
            return ctrl && shift && !alt &&
                string.Equals(nativeKey, PrimaryKey(binding), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(nativeName, "KingmakerBuffPlanner.Open", StringComparison.Ordinal);
        }

        // The game's own Escape binding (KeyboardAccess "EscPressed", KeyCode
        // 27, whose EscHotkeyManager opens the Save/Load/Options menu).
        internal const string NativeEscapeBinding = "EscPressed";

        // Everyday-use v1.2 E06: an Escape the planner takes (closing its
        // inspect or the planner itself) never also reaches the game - while
        // a planner window owns the keyboard, and on the frame its Escape
        // closed it (beta-3c1c5d4ar13-phys-sel-01: the closing Escape opened
        // the native menu). Escape with no planner open is the game's.
        internal static bool ShouldSuppressNativeEscape(string nativeName, bool plannerOwnsEscape)
        {
            return plannerOwnsEscape &&
                string.Equals(nativeName, NativeEscapeBinding, StringComparison.Ordinal);
        }
    }
}
