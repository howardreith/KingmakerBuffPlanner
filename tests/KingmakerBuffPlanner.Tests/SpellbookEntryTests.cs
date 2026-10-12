using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using KingmakerBuffPlanner.UI;
using KingmakerBuffPlanner.RuntimeTesting;

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
            Run("spellbook-entry-pairs-with-metamagic-in-the-footer",
                TestSpellbookFooterPair);
            Run("spellbook-entry-record-current-and-historical-placement", TestSpellbookEntryPlacementRecord);
            Run("spellbook-entry-physical-paused-state-and-capture", TestSpellbookPausedPhysicalRecord);
            Run("spellbook-entry-handoff-admission-and-recovery", TestSpellbookHandoffAdmission);
        }

        private static void TestSpellbookPausedPhysicalRecord()
        {
            Func<SpellbookPauseObservation> good = () => {
                var value = new SpellbookPauseObservation { PauseAppliedAtWorld = true, PausedBeforeKey = true,
                    PausedWithSpellbook = true, PausedWithPlanner = true, PausedAfterEscape = true, OriginalPauseRestored = true,
                    PlannerClosedAfterSwitch = true, SpellbookShownAfterSwitch = true, LeaseReleasedAfterSwitch = true, PausedAfterSwitch = true };
                foreach (string file in SpellbookPauseObservation.CaptureFiles)
                    value.Captures.Add(new JObject { { "file", file }, { "requestFrame", 10 }, { "completedFrame", 11 } });
                return value;
            };
            Expect(good().Violations().Count == 0, "Complete paused physical cycle must pass.");
            foreach (Action<SpellbookPauseObservation> breakState in new Action<SpellbookPauseObservation>[] {
                v => v.PauseAppliedAtWorld = false, v => v.PausedBeforeKey = false, v => v.PausedWithSpellbook = false,
                v => v.PausedWithPlanner = false, v => v.PausedAfterEscape = false, v => v.OriginalPauseRestored = false,
                v => v.PlannerClosedAfterSwitch = false, v => v.SpellbookShownAfterSwitch = false,
                v => v.LeaseReleasedAfterSwitch = false, v => v.PausedAfterSwitch = false,
                v => v.Captures[0]["completedFrame"] = 10, v => v.Captures.RemoveAt(0), v => v.CaptureFailure = "timeout" })
            {
                var value = good(); breakState(value);
                Expect(value.Violations().Count != 0, "Missing paused state, restoration or held capture must fail.");
            }
            var historical = new SpellbookEntryRecord();
            Expect(!historical.Violations().Contains("pause:observation-missing"), "Schema 1 retains its original interpretation.");
            historical.SchemaVersion = 2;
            Expect(historical.Violations().Contains("pause:observation-missing"), "New schema requires the physical paused cycle.");
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

        // Native UI finish (UI-10), superseding the top-right placement: the
        // planner button pairs with METAMAGIC in the book's footer. From the
        // native place (290.28 x 40 at x 629.34, plate 17 units past each
        // side, footer paper about x 410-885): two native-height buttons of
        // one width, METAMAGIC left, BUFF PLANNER right, a small gap, the
        // shared plate widened over both and inside the paper. Without
        // Metamagic the planner keeps its place and Metamagic is untouched.
        private static void TestSpellbookFooterPair()
        {
            const float nativeCentre = 629.34f, nativeWidth = 290.28f, plate = 17f;
            SpellbookFooterLayout paired = SpellbookFooterLayout.Compute(nativeCentre, nativeWidth, true);
            float metamagicLeft = paired.MetamagicCentre - paired.MetamagicWidth / 2f;
            float metamagicRight = paired.MetamagicCentre + paired.MetamagicWidth / 2f;
            float plannerLeft = paired.PlannerCentre - paired.PlannerWidth / 2f;
            float plannerRight = paired.PlannerCentre + paired.PlannerWidth / 2f;
            float gap = plannerLeft - metamagicRight;
            float plateLeft = metamagicLeft - plate;
            float plateRight = metamagicRight + plate + paired.PlateExtension;
            if (!paired.Paired || paired.MetamagicWidth != paired.PlannerWidth || paired.PlannerWidth < 200f ||
                gap < 0f || gap > 40f || Math.Abs(plateRight - (plannerRight + plate)) > 0.01f ||
                plateLeft < 410f || plateRight > 885f)
                throw new InvalidOperationException("The footer pair is wrong: " + paired.Describe() +
                    ";gap=" + gap + ";plate=" + plateLeft + "-" + plateRight);
            SpellbookFooterLayout alone = SpellbookFooterLayout.Compute(nativeCentre, nativeWidth, false);
            if (alone.Paired || alone.MetamagicCentre != nativeCentre || alone.MetamagicWidth != nativeWidth ||
                alone.PlateExtension != 0f || alone.PlannerCentre != paired.PlannerCentre ||
                alone.PlannerWidth != paired.PlannerWidth)
                throw new InvalidOperationException("Without Metamagic the footer is wrong: " + alone.Describe());
            bool refused = false;
            try { SpellbookFooterLayout.Compute(nativeCentre, 0f, true); }
            catch (ArgumentException) { refused = true; }
            if (!refused) throw new InvalidOperationException("A footer without a native place was laid out.");
            // The production entry is the footer pair built from the native
            // Metamagic button, restored on detach; the retired top-right
            // placement is gone.
            string source = ProductionSource("UI", "BuffPlannerSpellbookEntryController.cs");
            if (!source.Contains("SpellbookFooterLayout.Compute(") || !source.Contains("_nativeFooter.Restore()") ||
                !source.Contains("button.spriteState = footer.Apply.spriteState;") ||
                !source.Contains("label.fontSharedMaterial = native.fontSharedMaterial;") ||
                source.Contains("SpellbookEntryPlacement") || source.Contains("KingmakerUiFactory.CreateButton("))
                throw new InvalidOperationException("The spellbook entry is not the native footer pair.");
        }

        private static void TestSpellbookEntryPlacementRecord()
        {
            string paired = "footer-pair;" + SpellbookFooterLayout.Compute(629.34f, 290.28f, true).Describe();
            string alone = "footer-pair;" + SpellbookFooterLayout.Compute(629.34f, 290.28f, false).Describe();
            foreach (string valid in new[] { paired, alone, "left-of-native-close;conflictFree=True" })
                if (!SpellbookEntryRecord.PlacementIsConflictFree(valid))
                    throw new InvalidOperationException("Safe current/historical placement refused: " + valid);
            foreach (string invalid in new[] {
                null, "", "below-native-close;conflictFree=False",
                "footer-pair;paired=True;metamagic=629.3/290.3;planner=757.3/210.0;plateExtension=220.0",
                paired.Replace("plateExtension=220.0", "plateExtension=0.0"),
                alone.Replace("planner=757.3/210.0", "planner=757.3/0.0"),
                paired.Replace("537.3", "NaN"), paired.Replace("paired=True;", ""),
                "footer-pair;paired=True;conflictFree=True" })
                if (SpellbookEntryRecord.PlacementIsConflictFree(invalid))
                    throw new InvalidOperationException("Unsafe or incomplete placement accepted: " + invalid);
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
