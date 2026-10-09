using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        // WP2B: the spellbook Buff Planner button did not open the planner.
        // These reproduce the two native-contract mechanisms recorded in the
        // native UI contract (StaticCanvas/ServiceWindow at 1920x1080):
        // the only Button named Close is ServiceWindow/Top/Close (143x137,
        // anchored top-right), and SpellBook/BookBackground is full screen.
        private static void RunSpellbookEntryTests()
        {
            Run("spellbook-entry-native-close-is-the-service-window-top-close",
                TestSpellbookNativeCloseResolution);
            Run("spellbook-entry-placement-never-sits-under-the-native-close",
                TestSpellbookEntryPlacement);
            Run("spellbook-entry-handoff-admission-and-recovery", TestSpellbookHandoffAdmission);
        }

        private static ThemeNode NativeServiceWindowFixture(out ThemeNode serviceWindow,
            out ThemeNode spellbook, out ThemeNode topClose)
        {
            ThemeNode canvas = new ThemeNode { Name = "StaticCanvas" };
            serviceWindow = canvas.Add("ServiceWindow");
            serviceWindow.Add("Background");
            spellbook = serviceWindow.Add("SpellBook");
            spellbook.Add("BookBackground");
            // Per-spell slot controls are "Close" containers holding a Button
            // named "Button" (and a "Close" icon below it): the prior lookup
            // searched only inside the spellbook.
            ThemeNode item = spellbook.Add("Container_Book").Add("Book").Add("Image_Book")
                .Add("Container_SpellsLeft").Add("Spells_Container").Add("SpellBookItem")
                .Add("Item").Add("Bottom").Add("Close");
            item.Add("Button").Add("Close");
            ThemeNode top = serviceWindow.Add("Top");
            topClose = top.Add("Close");
            topClose.Add("CrossIcon");
            return canvas;
        }

        private static void TestSpellbookNativeCloseResolution()
        {
            ThemeNode serviceWindow, spellbook, topClose;
            ThemeNode canvas = NativeServiceWindowFixture(out serviceWindow, out spellbook, out topClose);
            string reason;
            SpellbookWindowLocator.Result found = SpellbookWindowLocator.Find(
                canvas, new FixtureThemeSource(), out reason);
            if (found == null) throw new InvalidOperationException("Spellbook not located: " + reason);
            if (!ReferenceEquals(found.Window, spellbook) ||
                !ReferenceEquals(found.ServiceWindow, serviceWindow))
                throw new InvalidOperationException("Window or service window identity was wrong.");
            if (!ReferenceEquals(found.NativeClose, topClose))
                throw new InvalidOperationException(
                    "The native close affordance is not ServiceWindow/Top/Close.");
            // The close affordance is never a descendant of the spellbook.
            for (var node = found.NativeClose as ThemeNode; node != null; node = node.Parent)
                if (ReferenceEquals(node, spellbook))
                    throw new InvalidOperationException("A spellbook slot control was taken as Close.");

            // Missing close: the window is still located, the handoff cannot
            // run, and the reason is explicit (the controller offers no
            // control then rather than a dead button).
            ThemeNode bareService, bareBook, bareClose;
            ThemeNode bare = NativeServiceWindowFixture(out bareService, out bareBook, out bareClose);
            bareClose.Parent.Children.Remove(bareClose);
            SpellbookWindowLocator.Result noClose = SpellbookWindowLocator.Find(
                bare, new FixtureThemeSource(), out reason);
            if (noClose == null || noClose.NativeClose != null ||
                noClose.CloseReason.IndexOf("native-close-missing", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("A missing native close was not reported.");

            // The tolerant spellbook scan still resolves the same exact close.
            ThemeNode renamed = new ThemeNode { Name = "StaticCanvas" };
            ThemeNode renamedService = renamed.Add("ServiceWindow");
            ThemeNode renamedBook = renamedService.Add("SpellBookScreen");
            ThemeNode renamedClose = renamedService.Add("Top").Add("Close");
            SpellbookWindowLocator.Result scanned = SpellbookWindowLocator.Find(
                renamed, new FixtureThemeSource(), out reason);
            if (scanned == null || !ReferenceEquals(scanned.Window, renamedBook) ||
                !ReferenceEquals(scanned.NativeClose, renamedClose))
                throw new InvalidOperationException("The scan path lost the exact native close.");
        }

        private static void TestSpellbookEntryPlacement()
        {
            // Service window local space at 1920x1080, pivot centred (y up).
            var window = new PlacementRect(-960f, -540f, 960f, 540f);
            var close = new PlacementRect(960f - 143f, 540f - 137f, 960f, 540f);
            const float width = 170f, height = 36f;

            // The legacy placement: 20/18 units inside the top-right corner.
            var legacy = new PlacementRect(960f - 20f - width, 540f - 18f - height,
                960f - 20f, 540f - 18f);
            if (!legacy.Overlaps(close, 0f))
                throw new InvalidOperationException("Fixture does not reproduce the legacy overlap.");

            // Tabs centred in the top bar leave room left of the close button.
            var tabs = new List<PlacementRect>();
            for (int index = 0; index < 6; index++)
                tabs.Add(new PlacementRect(-330f + index * 110f, 540f - 68f,
                    -230f + index * 110f, 540f));
            SpellbookEntryPlacementResult chosen = SpellbookEntryPlacement.Choose(
                window, close, tabs, width, height);
            if (!chosen.ConflictFree || chosen.Candidate != "left-of-native-close" ||
                chosen.Rect.Overlaps(close, SpellbookEntryPlacement.ControlMargin) ||
                tabs.Any(tab => chosen.Rect.Overlaps(tab, SpellbookEntryPlacement.ControlMargin)) ||
                !chosen.Rect.Inside(window))
                throw new InvalidOperationException("Top-bar placement was not conflict free: " +
                    chosen.Candidate + " " + chosen.Rect);

            // A native control in that spot moves the button below the close.
            var crowded = new List<PlacementRect>(tabs) {
                new PlacementRect(600f, 470f, 800f, 540f) };
            SpellbookEntryPlacementResult below = SpellbookEntryPlacement.Choose(
                window, close, crowded, width, height);
            if (!below.ConflictFree || below.Candidate != "below-native-close" ||
                below.Rect.YMax > close.YMin)
                throw new InvalidOperationException("Crowded top bar did not fall back below Close.");

            // Every candidate blocked: report the conflict and still never
            // cover the native close.
            var everywhere = new List<PlacementRect> { window };
            SpellbookEntryPlacementResult blocked = SpellbookEntryPlacement.Choose(
                window, close, everywhere, width, height);
            if (blocked.ConflictFree || blocked.Conflict.Length == 0 ||
                blocked.Rect.Overlaps(close, 0f))
                throw new InvalidOperationException("Universal conflict was not reported honestly.");
        }

        private static void TestSpellbookHandoffAdmission()
        {
            if (SpellbookHandoffStateMachine.Admit(false, true, false, true) != null)
                throw new InvalidOperationException("A usable handoff was refused.");
            string[] refusals =
            {
                SpellbookHandoffStateMachine.Admit(true, true, false, true),
                SpellbookHandoffStateMachine.Admit(false, false, false, true),
                SpellbookHandoffStateMachine.Admit(false, true, true, true),
                SpellbookHandoffStateMachine.Admit(false, true, false, false)
            };
            if (!refusals.SequenceEqual(new[] { "handoff-active", "no-game",
                    "planner-already-open", "native-close-affordance-missing" }))
                throw new InvalidOperationException("Refusal reasons drifted: " +
                    string.Join(",", refusals));

            // The opener waits while either the mode or the window is owned.
            var machine = new SpellbookHandoffStateMachine();
            machine.Begin();
            if (machine.ObserveRelease(true) || machine.State != SpellbookHandoffState.WaitingModeRelease)
                throw new InvalidOperationException("Opener armed while the native owner was active.");
            if (!machine.ObserveRelease(false))
                throw new InvalidOperationException("Released native owner did not arm the opener.");

            // Recovery: a window that never closed stays the player's UI; any
            // failure after the closure reopens the native spellbook.
            if (SpellbookHandoffStateMachine.RecoveryFor("mode-release-timeout") !=
                    SpellbookHandoffRecovery.KeepNativeWindow ||
                SpellbookHandoffStateMachine.RecoveryFor("planner-open-refused") !=
                    SpellbookHandoffRecovery.ReopenNativeSpellbook ||
                SpellbookHandoffStateMachine.RecoveryFor("presentation-timeout") !=
                    SpellbookHandoffRecovery.ReopenNativeSpellbook)
                throw new InvalidOperationException("Recovery classification drifted.");
        }
    }
}
