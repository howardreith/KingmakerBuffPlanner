using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // What one physical-input workspace run did (mission batch 3, section
    // 10): every action is delivered by the operating system's input (the
    // harness acknowledges each request it sent through the game window),
    // and the view's own state is read after it. Programmatic callbacks
    // never count here: the workspace itself must have opened through the
    // physical planner hotkey. Judged Unity-free.
    public sealed class PhysicalWorkspaceRecord
    {
        // Every physical action the run requests, in order.
        public static readonly string[] Actions =
        {
            "ws-click-search", "ws-wheel-grid", "ws-type-query", "ws-click-tile",
            "ws-focus-cycle", "ws-click-search-again", "ws-type-more", "ws-escape"
        };

        // The typed query comes from the label of a tile that was NOT
        // selected (review B5): the first four letters of its first word,
        // then its fifth letter after the focus loss. Clicking that tile must
        // then change the selection.
        public static bool DeriveQuery(string label, out string query, out string suffix)
        {
            query = null;
            suffix = null;
            if (string.IsNullOrEmpty(label)) return false;
            string word = new string(label.ToLowerInvariant().TakeWhile(c => c >= 'a' && c <= 'z').ToArray());
            if (word.Length < 5) return false;
            query = word.Substring(0, 4);
            suffix = word.Substring(4, 1);
            return true;
        }

        public string Query { get; set; }
        public string QuerySuffix { get; set; }
        // The tile the query names; it was not selected before the click.
        public string TargetSource { get; set; }
        // "<width>x<height>" the launcher asked for, or null for the owner's
        // own display settings.
        public string ExpectedScreen { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public bool FullScreen { get; set; }
        // The workspace opened through the physical planner hotkey (not the
        // host's labeled programmatic fallback).
        public bool OpenedPhysically { get; set; }
        public List<string> Acknowledged { get; } = new List<string>();
        public bool SearchFocused { get; set; }
        public string SearchText { get; set; }
        // "<sourceId>|<label>|<selected>" for every tile after the query.
        public List<string> VisibleAfterQuery { get; } = new List<string>();
        // "planner" while the game mode is the one the open workspace set.
        public string ModeAfterTyping { get; set; }
        public bool GridOverflows { get; set; }
        public float? ScrollBefore { get; set; }
        public float? ScrollAfter { get; set; }
        public string SelectedBeforeClick { get; set; }
        public string SelectedAfterClick { get; set; }
        // The launcher's own report of the game window's minimize and
        // restore; the game itself may not update while minimized.
        public string FocusCycle { get; set; }
        public bool FocusRegained { get; set; }
        public bool WorkspaceOpenAfterFocus { get; set; }
        public string SearchTextAfterFocus { get; set; }
        public bool ClosedByEscape { get; set; }
        public bool LeaseReleased { get; set; }
        // The game mode after Escape; the world mode is "Default".
        public string ModeAfterClose { get; set; }
        // The world-input isolation probe over the whole sequence.
        public int PlayerCommands { get; set; }
        public int MovementCommands { get; set; }
        public int AbilityCommands { get; set; }
        public int SelectionEvents { get; set; }
        public int AbilityTargetEvents { get; set; }
        public bool SelectionUnchanged { get; set; }
        public bool CameraUnchanged { get; set; }
        public List<string> Failures { get; } = new List<string>();

        // What the wheel proved: "scrolled", or "not-applicable:no-overflow"
        // when the grid's content fits (the wheel was delivered; there was
        // nothing to scroll), or "no-scroll".
        public string WheelEvidence
        {
            get
            {
                if (ScrollBefore == null || ScrollAfter == null) return "unread";
                bool moved = Math.Abs(ScrollAfter.Value - ScrollBefore.Value) >= 0.001f;
                if (!GridOverflows) return moved ? "moved-without-overflow" : "not-applicable:no-overflow";
                return moved ? "scrolled" : "no-scroll";
            }
        }

        public IList<string> Violations()
        {
            var violations = new List<string>(Failures);
            string screen = ScreenWidth + "x" + ScreenHeight;
            if (!string.IsNullOrEmpty(ExpectedScreen) && ExpectedScreen != screen)
                violations.Add("screen:" + screen + "!=" + ExpectedScreen);
            if (!OpenedPhysically) violations.Add("workspace-opened-programmatically");
            foreach (string action in Actions)
                if (!Acknowledged.Contains(action)) violations.Add("unacknowledged:" + action);
            if (string.IsNullOrEmpty(Query) || Query.Length != 4 || string.IsNullOrEmpty(QuerySuffix))
                violations.Add("query-not-derived");
            if (!SearchFocused) violations.Add("search-not-focused");
            if (Query == null || SearchText != Query) violations.Add("typed:" + (SearchText ?? "none"));
            if (VisibleAfterQuery.Count == 0) violations.Add("no-visible-tile");
            foreach (string tile in VisibleAfterQuery)
            {
                string[] parts = tile.Split('|');
                bool selected = parts.Length == 3 && parts[2] == "True";
                bool matches = parts.Length == 3 && Query != null &&
                    parts[1].IndexOf(Query, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!selected && !matches) violations.Add("unfiltered:" + tile);
            }
            if (TargetSource == null || !VisibleAfterQuery.Any(tile => tile.StartsWith(TargetSource + "|",
                    StringComparison.Ordinal)))
                violations.Add("target-not-shown:" + (TargetSource ?? "none"));
            if (ModeAfterTyping != "planner")
                violations.Add("typing-changed-mode:" + (ModeAfterTyping ?? "none"));
            string wheel = WheelEvidence;
            if (wheel == "unread") violations.Add("scroll-unread");
            else if (wheel == "no-scroll" || wheel == "moved-without-overflow")
                violations.Add("wheel:" + wheel + ":" + ScrollBefore + ">" + ScrollAfter);
            if (TargetSource != null && SelectedBeforeClick == TargetSource)
                violations.Add("target-already-selected:" + TargetSource);
            if (TargetSource == null || SelectedAfterClick != TargetSource)
                violations.Add("tile-not-selected:" + (TargetSource ?? "none") + ">" + (SelectedAfterClick ?? "none"));
            if (FocusCycle == null || FocusCycle.IndexOf("minimized=True", StringComparison.Ordinal) < 0)
                violations.Add("focus-cycle:" + (FocusCycle ?? "none"));
            if (!FocusRegained) violations.Add("focus-not-regained");
            if (!WorkspaceOpenAfterFocus) violations.Add("workspace-lost-on-focus");
            if (Query == null || QuerySuffix == null || SearchTextAfterFocus != Query + QuerySuffix)
                violations.Add("typing-after-focus:" + (SearchTextAfterFocus ?? "none"));
            if (!ClosedByEscape) violations.Add("escape-did-not-close");
            if (!LeaseReleased) violations.Add("lease-held-after-close");
            if (ModeAfterClose != "Default") violations.Add("mode-after-close:" + (ModeAfterClose ?? "none"));
            if (PlayerCommands != 0 || MovementCommands != 0 || AbilityCommands != 0 ||
                SelectionEvents != 0 || AbilityTargetEvents != 0 || !SelectionUnchanged || !CameraUnchanged)
                violations.Add("world-input-leaked:commands=" + PlayerCommands + "/" + MovementCommands + "/" +
                    AbilityCommands + ";events=" + SelectionEvents + "/" + AbilityTargetEvents +
                    ";selectionUnchanged=" + SelectionUnchanged + ";cameraUnchanged=" + CameraUnchanged);
            return violations;
        }
    }
}
