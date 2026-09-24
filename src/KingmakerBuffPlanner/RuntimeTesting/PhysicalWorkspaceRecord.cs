using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // What one physical-input workspace run did (mission batch 3, section
    // 10): every action is delivered by the operating system's input (the
    // harness acknowledges each request it sent through the game window),
    // and the view's own state is read after it. Programmatic callbacks
    // never count here. Judged Unity-free.
    public sealed class PhysicalWorkspaceRecord
    {
        // The search typed physically, then one more letter after the game
        // window lost and regained focus.
        public const string Query = "resis";
        public const string QuerySuffix = "t";

        // Every physical action the run requests, in order.
        public static readonly string[] Actions =
        {
            "ws-click-search", "ws-type-query", "ws-wheel-grid", "ws-click-tile",
            "ws-focus-cycle", "ws-click-search-again", "ws-type-more", "ws-escape"
        };

        // "<width>x<height>" the launcher asked for, or null for the owner's
        // own display settings.
        public string ExpectedScreen { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public bool FullScreen { get; set; }
        public List<string> Acknowledged { get; } = new List<string>();
        public bool SearchFocused { get; set; }
        public string SearchText { get; set; }
        // "<sourceId>|<label>|<selected>" for every tile after the query.
        public List<string> VisibleAfterQuery { get; } = new List<string>();
        public string ModeAfterTyping { get; set; }
        public bool GridOverflows { get; set; }
        public float? ScrollBefore { get; set; }
        public float? ScrollAfter { get; set; }
        public string ClickedSource { get; set; }
        public string SelectedAfterClick { get; set; }
        // The launcher's report of the game window's own minimize and
        // restore, and what the game saw.
        public string FocusCycle { get; set; }
        public bool FocusLostObserved { get; set; }
        public bool FocusRegained { get; set; }
        public bool WorkspaceOpenAfterFocus { get; set; }
        public string SearchTextAfterFocus { get; set; }
        public bool ClosedByEscape { get; set; }
        public bool LeaseReleased { get; set; }
        public string ModeAfterClose { get; set; }
        // The world-input isolation probe over the whole sequence.
        public int PlayerCommands { get; set; }
        public int MovementCommands { get; set; }
        public int AbilityCommands { get; set; }
        public bool SelectionUnchanged { get; set; }
        public bool CameraUnchanged { get; set; }
        public List<string> Failures { get; } = new List<string>();

        public IList<string> Violations()
        {
            var violations = new List<string>(Failures);
            string screen = ScreenWidth + "x" + ScreenHeight;
            if (!string.IsNullOrEmpty(ExpectedScreen) && ExpectedScreen != screen)
                violations.Add("screen:" + screen + "!=" + ExpectedScreen);
            foreach (string action in Actions)
                if (!Acknowledged.Contains(action)) violations.Add("unacknowledged:" + action);
            if (!SearchFocused) violations.Add("search-not-focused");
            if (SearchText != Query) violations.Add("typed:" + (SearchText ?? "none"));
            if (VisibleAfterQuery.Count == 0) violations.Add("no-visible-tile");
            foreach (string tile in VisibleAfterQuery)
            {
                string[] parts = tile.Split('|');
                bool selected = parts.Length == 3 && parts[2] == "True";
                bool matches = parts.Length == 3 &&
                    parts[1].IndexOf(Query, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!selected && !matches) violations.Add("unfiltered:" + tile);
            }
            if (!VisibleAfterQuery.Any(tile => tile.Split('|').Length == 3 &&
                    tile.Split('|')[1].IndexOf(Query, StringComparison.OrdinalIgnoreCase) >= 0))
                violations.Add("no-match-shown");
            if (ModeAfterTyping != null && ModeAfterTyping != "planner")
                violations.Add("typing-changed-mode:" + ModeAfterTyping);
            if (ScrollBefore == null || ScrollAfter == null) violations.Add("scroll-unread");
            else if (GridOverflows ? Math.Abs(ScrollAfter.Value - ScrollBefore.Value) < 0.001f
                    : Math.Abs(ScrollAfter.Value - ScrollBefore.Value) >= 0.001f)
                violations.Add("wheel:" + (GridOverflows ? "no-scroll" : "moved-without-overflow") + ":" +
                    ScrollBefore + ">" + ScrollAfter);
            if (string.IsNullOrEmpty(ClickedSource) || ClickedSource != SelectedAfterClick)
                violations.Add("tile-not-selected:" + (ClickedSource ?? "none") + ">" + (SelectedAfterClick ?? "none"));
            if (FocusCycle == null || FocusCycle.IndexOf("minimized=True", StringComparison.Ordinal) < 0)
                violations.Add("focus-cycle:" + (FocusCycle ?? "none"));
            if (!FocusRegained) violations.Add("focus-not-regained");
            if (!WorkspaceOpenAfterFocus) violations.Add("workspace-lost-on-focus");
            if (SearchTextAfterFocus != Query + QuerySuffix)
                violations.Add("typing-after-focus:" + (SearchTextAfterFocus ?? "none"));
            if (!ClosedByEscape) violations.Add("escape-did-not-close");
            if (!LeaseReleased) violations.Add("lease-held-after-close");
            if (PlayerCommands != 0 || MovementCommands != 0 || AbilityCommands != 0 ||
                !SelectionUnchanged || !CameraUnchanged)
                violations.Add("world-input-leaked:commands=" + PlayerCommands + "/" + MovementCommands + "/" +
                    AbilityCommands + ";selectionUnchanged=" + SelectionUnchanged + ";cameraUnchanged=" +
                    CameraUnchanged);
            return violations;
        }
    }
}
