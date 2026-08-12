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
    }
}
