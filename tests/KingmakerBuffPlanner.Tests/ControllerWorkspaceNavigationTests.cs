using System;
using System.Linq;
using KingmakerBuffPlanner.UI;

namespace KingmakerBuffPlanner.Tests
{
    internal static partial class Program
    {
        private static void RunControllerWorkspaceNavigationTests()
        {
            Run("controller-workspace-spatial-header-routines-and-edges", TestSpatialHeaderAndEdges);
            Run("controller-workspace-spatial-footer-follows-visible-order", TestSpatialFooterOrder);
            Run("controller-workspace-spatial-search-filter-and-buff-rows", TestSpatialCatalogue);
            Run("controller-workspace-spatial-body-neighbors-and-empty-column", TestSpatialBodyColumns);
            Run("controller-workspace-spatial-inspector-pairs", TestSpatialInspectorPairs);
            Run("controller-workspace-spatial-vertical-aim-survives-wide-rows-and-refresh", TestSpatialVerticalAim);
            Run("controller-workspace-spatial-long-list-keeps-offscreen-rows", TestSpatialLongList);
            Run("controller-workspace-spatial-fixed-controls-do-not-intercept-scroll", TestSpatialFixedControls);
            Run("controller-workspace-spatial-manual-scroll-keeps-visible-confirm", TestSpatialManualScroll);
            Run("controller-workspace-spatial-empty-viewport-refuses-hidden-action", TestSpatialEmptyViewport);
            Run("controller-workspace-spatial-text-only-inspector-scroll-preserves-reading-focus", TestSpatialTextOnlyInspectorScroll);
            Run("controller-workspace-spatial-rebuild-keeps-identity-and-nearby-row", TestSpatialRebuild);
            Run("controller-workspace-spatial-layout-refresh-and-disabled-inspect", TestSpatialLayoutAndDisabled);
            Run("controller-workspace-spatial-modal-return-and-empty-scope", TestSpatialModalAndEmpty);
        }

        private static ControllerFocusItem SpatialControl(string key, string region,
            float x, float y, float width = 100, float height = 40,
            bool visible = true, string scroll = null, bool enabled = true,
            Action activate = null, Action inspect = null)
        {
            return new ControllerFocusItem(key, region, key, enabled, activate ?? (() => { }), inspect: inspect,
                bounds: new ControllerFocusBounds(x, y, x + width, y + height), visible: visible, scrollKey: scroll);
        }

        private static void TestSpatialHeaderAndEdges()
        {
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("close", "Header", 850, 900),
                SpatialControl("routine-a", "Routines", 100, 830),
                SpatialControl("routine-b", "Routines", 250, 830),
                SpatialControl("search", "Catalogue", 100, 750, 200),
                SpatialControl("buff", "Catalogue", 100, 650, 200),
                SpatialControl("inspector", "Inspector", 850, 750),
                SpatialControl("resources", "Footer", 100, 50) });
            Assert(focus.Key == "search");
            focus.Restore("routine-a");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "routine-a");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "routine-b");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "routine-b");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "search");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "routine-b");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "close");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "close");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "close");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "inspector");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "close");
            focus.Restore("resources");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "resources");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "buff");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "resources");
        }

        private static void TestSpatialFooterOrder()
        {
            int actions = 0;
            string[] order = { "resources", "policy", "mode", "undo", "run" };
            var focus = new ControllerWorkspaceFocus();
            // Matches the real footer's reverse construction order.
            focus.Replace(order.Select((key, index) => SpatialControl(key, "Footer", index * 120, 50,
                activate: () => actions++)).Reverse());
            focus.Restore(order[0]);
            foreach (string key in order.Skip(1))
            { focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == key); }
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "run");
            foreach (string key in order.Reverse().Skip(1))
            { focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == key); }
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "resources" && actions == 0);
        }

        private static void TestSpatialCatalogue()
        {
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("all", "Catalogue", 0, 700, 90),
                SpatialControl("spells", "Catalogue", 100, 700, 90),
                SpatialControl("abilities", "Catalogue", 200, 700, 90),
                SpatialControl("other", "Catalogue", 300, 700, 90),
                SpatialControl("buff-a", "Catalogue", 0, 630, 400, scroll: "catalogue"),
                SpatialControl("buff-b", "Catalogue", 0, 570, 400, scroll: "catalogue"),
                SpatialControl("search", "Catalogue", 0, 760, 400) });
            Assert(focus.Key == "search");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "all");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "spells");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "abilities");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "spells");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "all");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "all");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-a");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-b");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-b");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "buff-a");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "all");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "search");
        }

        private static void TestSpatialBodyColumns()
        {
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("buff", "Catalogue", 0, 450),
                SpatialControl("caster-top", "Casters", 200, 650),
                SpatialControl("caster-hidden", "Casters", 200, 450, visible: false),
                SpatialControl("caster-near", "Casters", 200, 440),
                SpatialControl("target", "Recipients", 600, 450),
                SpatialControl("option", "Inspector", 800, 500),
                SpatialControl("run", "Footer", 800, 50),
                SpatialControl("close", "Header", 800, 900) });
            focus.Restore("buff");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "buff");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "caster-near");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "target");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "option");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "option");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "target");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "caster-near");
        }

        private static void TestSpatialInspectorPairs()
        {
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("target", "Recipients", 400, 600),
                SpatialControl("change-recipient", "Inspector", 600, 600),
                SpatialControl("change-caster", "Inspector", 720, 600),
                SpatialControl("recast", "Inspector", 600, 530, 220),
                SpatialControl("earlier", "Inspector", 600, 450),
                SpatialControl("later", "Inspector", 720, 450) });
            focus.Restore("change-recipient");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "change-caster");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "recast");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "later");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "recast");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "change-caster");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "recast");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "later");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "later");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "earlier");
            focus.Move(ControllerWorkspaceCommand.Left); Assert(focus.Key == "target");
        }

        private static void TestSpatialVerticalAim()
        {
            var focus = new ControllerWorkspaceFocus();
            ControllerFocusItem[] controls = {
                SpatialControl("all", "Catalogue", 0, 700, 90),
                SpatialControl("spells", "Catalogue", 100, 700, 90),
                SpatialControl("abilities", "Catalogue", 200, 700, 90),
                SpatialControl("buff-a", "Catalogue", 0, 630, 300, scroll: "catalogue"),
                SpatialControl("buff-b", "Catalogue", 0, 570, 300, scroll: "catalogue") };
            focus.Replace(controls);
            foreach (string key in new[] { "all", "spells", "abilities" })
            {
                focus.Restore(key);
                focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-a");
                focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == key);
            }
            focus.Restore("all");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "spells");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-a");
            // Stable-key refresh changes enumeration order, not the column
            // remembered while crossing the full-width buff row.
            focus.Replace(controls.Reverse());
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "spells");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-a");
            focus.UpdateLayout("buff-a", new ControllerFocusBounds(0, 850, 300, 890), false, "catalogue");
            Assert(focus.KeepVisible("catalogue") && focus.Key == "buff-b");
            focus.UpdateLayout("buff-a", new ControllerFocusBounds(0, 630, 300, 670), true, "catalogue");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "buff-a");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "spells");
        }

        private static void TestSpatialLongList()
        {
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(Enumerable.Range(0, 200).Select(index => SpatialControl("buff-" + index, "Catalogue",
                0, 1000 - index * 60, visible: index < 6, scroll: "catalogue")));
            for (int index = 1; index < 200; index++)
            { focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-" + index); }
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-199");
            for (int index = 198; index >= 0; index--)
            { focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "buff-" + index); }
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "buff-0");
        }

        private static void TestSpatialFixedControls()
        {
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("search", "Catalogue", 0, 850),
                SpatialControl("filter", "Catalogue", 0, 760),
                SpatialControl("buff-98", "Catalogue", 0, 900, visible: false, scroll: "catalogue"),
                SpatialControl("buff-99", "Catalogue", 0, 810, visible: false, scroll: "catalogue"),
                SpatialControl("buff-100", "Catalogue", 0, 700, scroll: "catalogue"),
                SpatialControl("buff-101", "Catalogue", 0, 640, scroll: "catalogue") });
            focus.Restore("buff-100");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "buff-99");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "buff-98");
            focus.Restore("search");
            focus.Move(ControllerWorkspaceCommand.Up); Assert(focus.Key == "search");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "filter");
            focus.Move(ControllerWorkspaceCommand.Down); Assert(focus.Key == "buff-100");
            // A partially clipped row still has full geometry behind the
            // fixed filter bar. Right must leave the list for the caster,
            // not mistake an overlapping filter for a button in that row.
            focus.Replace(new[] {
                SpatialControl("filter-right", "Catalogue", 300, 760, 90),
                SpatialControl("partly-clipped", "Catalogue", 0, 710, 400, 100, scroll: "catalogue"),
                SpatialControl("caster", "Casters", 500, 710, 100, 100, scroll: "graph") });
            focus.Restore("partly-clipped");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "caster");
        }

        private static void TestSpatialManualScroll()
        {
            int hiddenActions = 0, visibleActions = 0;
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("old-row", "Catalogue", 0, 600, scroll: "catalogue", activate: () => hiddenActions++),
                SpatialControl("new-row", "Catalogue", 0, 500, scroll: "catalogue", activate: () => visibleActions++),
                SpatialControl("other-pane", "Casters", 200, 600, scroll: "graph") });
            focus.Restore("old-row");
            focus.UpdateLayout("old-row", new ControllerFocusBounds(0, 900, 100, 940), false, "catalogue");
            focus.UpdateLayout("new-row", new ControllerFocusBounds(0, 600, 100, 640), true, "catalogue");
            Assert(focus.KeepVisible("catalogue") && focus.Key == "new-row");
            Assert(hiddenActions == 0 && visibleActions == 0);
            Assert(focus.Confirm() == null && hiddenActions == 0 && visibleActions == 1);
        }

        private static void TestSpatialEmptyViewport()
        {
            int actions = 0;
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] { SpatialControl("hidden", "Catalogue", 0, 900, visible: false,
                scroll: "catalogue", activate: () => actions++) });
            Assert(!focus.KeepVisible("catalogue"));
            Assert(focus.Confirm() != null && actions == 0);
        }

        private static void TestSpatialTextOnlyInspectorScroll()
        {
            int acknowledgements = 0;
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] { SpatialControl("acknowledge-imported-constraints", "Inspector",
                0, 1000, 200, 32, scroll: "inspector", activate: () => acknowledgements++) });
            focus.Restore("acknowledge-imported-constraints");
            // A 600-pixel viewport initially ends at the button. Scrolling
            // up through long wrapped notices leaves only text at offset 0.
            focus.UpdateLayout(focus.Key, new ControllerFocusBounds(0, 1000, 200, 1032), false, "inspector");
            Assert(!focus.KeepVisible("inspector") && focus.ReadingScrollKey == "inspector");
            Assert(focus.Key == "acknowledge-imported-constraints");
            // The following frame must keep pane reading focus, not request
            // a reveal that would restore the old 432-pixel offset.
            Assert(!focus.KeepVisible("inspector") && focus.ReadingScrollKey == "inspector");
            Assert(focus.Confirm().Contains("No visible") && acknowledgements == 0);
            focus.Replace(new[] { SpatialControl("acknowledge-imported-constraints", "Inspector",
                0, 1000, 200, 32, visible: false, scroll: "inspector",
                activate: () => acknowledgements++) });
            Assert(!focus.KeepVisible("inspector") && focus.ReadingScrollKey == "inspector");
            Assert(focus.Confirm() != null && acknowledgements == 0);
            focus.UpdateLayout(focus.Key, new ControllerFocusBounds(0, 1000, 200, 1032), true, "inspector");
            Assert(focus.KeepVisible("inspector") && focus.ReadingScrollKey == null);
            Assert(focus.Confirm() == null && acknowledgements == 1);
        }

        private static void TestSpatialRebuild()
        {
            int oldActions = 0, newActions = 0;
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("a", "Catalogue", 0, 500),
                SpatialControl("b", "Catalogue", 0, 440, activate: () => oldActions++),
                SpatialControl("c", "Catalogue", 0, 340) });
            focus.Restore("b");
            focus.Replace(new[] {
                SpatialControl("c", "Catalogue", 0, 340),
                SpatialControl("b", "Catalogue", 0, 440, activate: () => newActions++),
                SpatialControl("a", "Catalogue", 0, 500) });
            Assert(focus.Key == "b" && focus.Confirm() == null && newActions == 1 && oldActions == 0);
            focus.Replace(focus.Items.Where(item => item.Key != "b").ToArray());
            Assert(focus.Key == "a");
        }

        private static void TestSpatialLayoutAndDisabled()
        {
            int actions = 0, inspections = 0;
            var focus = new ControllerWorkspaceFocus();
            focus.Replace(new[] {
                SpatialControl("buff", "Catalogue", 0, 500),
                SpatialControl("top", "Casters", 200, 500),
                SpatialControl("blocked", "Casters", 200, 300, enabled: false,
                    activate: () => actions++, inspect: () => inspections++) });
            focus.Restore("buff");
            focus.UpdateLayout("buff", new ControllerFocusBounds(0, 300, 100, 340), true, "catalogue");
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "blocked");
            Assert(focus.Confirm().Contains("Unavailable") && actions == 0);
            focus.Current.Inspect(); Assert(inspections == 1);
        }

        private static void TestSpatialModalAndEmpty()
        {
            var focus = new ControllerWorkspaceFocus();
            ControllerFocusItem[] workspace = {
                SpatialControl("search", "Catalogue", 0, 600),
                SpatialControl("resource-origin", "Footer", 0, 50) };
            focus.Replace(workspace); focus.Restore("resource-origin");
            string origin = focus.Key;
            focus.Replace(new[] { SpatialControl("resource-close", "Resources", 400, 700) });
            focus.Move(ControllerWorkspaceCommand.Right); Assert(focus.Key == "resource-close");
            focus.Replace(workspace); focus.Restore(origin); Assert(focus.Key == "resource-origin");
            focus.Replace(new ControllerFocusItem[0]);
            focus.Move(ControllerWorkspaceCommand.Down);
            Assert(focus.Key == null && !focus.KeepVisible("catalogue") && focus.Confirm().Contains("No controls"));
        }
    }
}
