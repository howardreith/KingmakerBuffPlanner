using System;

namespace KingmakerBuffPlanner.UI
{
    // Locates the native spellbook service window with two bounded,
    // logged strategies: the exact proven StaticCanvas path first, then a
    // bounded name-tolerant scan over the ServiceWindow's direct children
    // for a uniquely matching spellbook-named window. Ambiguity refuses
    // instead of guessing; discovery never scans globally or per frame.
    //
    // WP2B: the spellbook sub-window has no close button of its own. The
    // native close affordance is the service window's top-bar Close button
    // (ServiceWindow/Top/Close, a UnityEngine.UI.Button wired to
    // FullScreenTabsWindow.OnButtonClose, proven by the recorded native UI
    // contract), so it is resolved by that exact path relative to the
    // service window and never by a name search inside the spellbook, whose
    // per-spell "Close" containers are slot controls.
    internal static class SpellbookWindowLocator
    {
        internal const string ExactSpellBookPath = "ServiceWindow/SpellBook";
        internal const string NativeClosePath = "Top/Close";
        private const string ServiceWindowName = "ServiceWindow";
        private const int MaximumServiceWindowChildren = 512;

        internal sealed class Result
        {
            internal Result(object window, string locator, object serviceWindow,
                object nativeClose, string closeReason)
            {
                Window = window;
                Locator = locator ?? string.Empty;
                ServiceWindow = serviceWindow;
                NativeClose = nativeClose;
                CloseReason = closeReason ?? string.Empty;
            }
            internal object Window;
            internal string Locator;
            // The FullScreenTabsWindow that owns the spellbook tab.
            internal object ServiceWindow;
            // ServiceWindow/Top/Close, or null with CloseReason explaining why.
            internal object NativeClose;
            internal string CloseReason;
        }

        // Returns null (with a reason for the log) when no usable window
        // exists yet; the caller retries on its bounded cadence.
        internal static Result Find(object canvasRoot, INativeThemeSource source,
            out string reason)
        {
            if (canvasRoot == null) { reason = "canvas-root-missing"; return null; }
            var lookup = new NativeThemeDonorLookup(source);
            object serviceWindow;
            try { serviceWindow = lookup.RequirePath(canvasRoot, ServiceWindowName); }
            catch (Exception serviceFailure)
            {
                reason = "service-window-missing: " + serviceFailure.Message;
                return null;
            }
            object window;
            string locator;
            try
            {
                window = lookup.RequirePath(canvasRoot, ExactSpellBookPath);
                locator = "path '" + ExactSpellBookPath + "'";
            }
            catch (Exception exactFailure)
            {
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
                if (matches != 1)
                {
                    reason = matches == 0
                        ? "spellbook-window-absent;exact=" + exactFailure.Message
                        : "spellbook-window-ambiguous:" + matches;
                    return null;
                }
                window = match;
                locator = "scan ServiceWindow/*SpellBook* '" + matchName + "'";
            }
            object nativeClose = null;
            string closeReason = string.Empty;
            try { nativeClose = lookup.RequirePath(serviceWindow, NativeClosePath); }
            catch (Exception closeFailure)
            {
                closeReason = "native-close-missing: " + closeFailure.Message;
            }
            reason = string.Empty;
            return new Result(window, locator, serviceWindow, nativeClose, closeReason);
        }
    }
}
