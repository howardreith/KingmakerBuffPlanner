using System;

namespace KingmakerBuffPlanner.UI
{
    // Locates the native spellbook service window with two bounded,
    // logged strategies: the exact proven StaticCanvas path first, then a
    // bounded name-tolerant scan over the ServiceWindow's direct children
    // for a uniquely matching spellbook-named window. Ambiguity refuses
    // instead of guessing; discovery never scans globally or per frame.
    internal static class SpellbookWindowLocator
    {
        internal const string ExactSpellBookPath = "ServiceWindow/SpellBook";
        private const string ServiceWindowName = "ServiceWindow";
        private const int MaximumServiceWindowChildren = 512;

        internal sealed class Result
        {
            internal Result(object window, string locator)
            {
                Window = window;
                Locator = locator ?? string.Empty;
            }
            internal object Window;
            internal string Locator;
        }

        // Returns null (with a reason for the log) when no usable window
        // exists yet; the caller retries on its bounded cadence.
        internal static Result Find(object canvasRoot, INativeThemeSource source,
            out string reason)
        {
            if (canvasRoot == null) { reason = "canvas-root-missing"; return null; }
            var lookup = new NativeThemeDonorLookup(source);
            try
            {
                object exact = lookup.RequirePath(canvasRoot, ExactSpellBookPath);
                reason = string.Empty;
                return new Result(exact, "path '" + ExactSpellBookPath + "'");
            }
            catch (Exception exactFailure)
            {
                object serviceWindow = null;
                try { serviceWindow = lookup.RequirePath(canvasRoot, ServiceWindowName); }
                catch (Exception serviceFailure)
                {
                    reason = "service-window-missing: " + serviceFailure.Message;
                    return null;
                }
                object match = null;
                int matches = 0;
                string matchName = null;
                if (source.IsAlive(serviceWindow))
                {
                    int count = source.ChildCount(serviceWindow);
                    if (count > MaximumServiceWindowChildren)
                    {
                        reason = "service-window-children-exceed-bound:" + count;
                        return null;
                    }
                    for (int index = 0; index < count; index++)
                    {
                        object child = source.Child(serviceWindow, index);
                        if (!source.IsAlive(child)) continue;
                        string name = source.Name(child) ?? string.Empty;
                        if (name.IndexOf("spellbook", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        match = child;
                        matchName = name;
                        matches++;
                    }
                }
                if (matches == 1)
                {
                    reason = string.Empty;
                    return new Result(match, "scan ServiceWindow/*SpellBook* '" +
                        matchName + "'");
                }
                reason = matches == 0
                    ? "spellbook-window-absent;exact=" + exactFailure.Message
                    : "spellbook-window-ambiguous:" + matches;
                return null;
            }
        }
    }
}
