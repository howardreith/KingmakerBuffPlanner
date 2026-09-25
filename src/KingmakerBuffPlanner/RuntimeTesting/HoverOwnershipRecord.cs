using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Unity-free evidence and acceptance predicate for the planner's pointer
    // highlight (casting-graph addendum v1.1: exactly one highlight, on the
    // control under the pointer, gone when the pointer leaves). The runtime
    // host fills it from Unity's own per-control states (PlannerHoverProbe);
    // the source-only suite exercises the same predicate.
    internal static class HoverBehaviour
    {
        // The shipped build: planner controls never take the EventSystem
        // selection.
        internal const string Fixed = "fixed";
        // The guarded reproduction on the same binary: the rc6 button
        // behaviour (PlannerUiReproduction), judged only as evidence that the
        // owner-reported ghost highlight is reproduced.
        internal const string Rc6 = "rc6";
    }

    // One reading of which controls DRAW a highlight now (Unity's own
    // Highlighted state on a control with a visible transition), which report
    // the pointer inside, and which control holds the EventSystem selection.
    internal sealed class HoverSample
    {
        internal HoverSample(string label, string surface, string behaviour, bool physical,
            string aimed, string clicked, string expectedOwner, string topControl,
            IEnumerable<string> highlighted, IEnumerable<string> pointerInside,
            string selection, bool selectionIsPlannerControl, bool? cursorInsideOwner,
            string detail)
        {
            if (string.IsNullOrEmpty(label)) throw new ArgumentException("A sample needs a label.", "label");
            Label = label;
            Surface = surface ?? string.Empty;
            Behaviour = behaviour ?? HoverBehaviour.Fixed;
            Physical = physical;
            Aimed = NullIfEmpty(aimed);
            Clicked = NullIfEmpty(clicked);
            ExpectedOwner = NullIfEmpty(expectedOwner);
            TopControl = NullIfEmpty(topControl);
            Highlighted = (highlighted ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            PointerInside = (pointerInside ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
            Selection = NullIfEmpty(selection);
            SelectionIsPlannerControl = selectionIsPlannerControl;
            CursorInsideOwner = cursorInsideOwner;
            Detail = detail ?? string.Empty;
        }

        internal string Label { get; private set; }
        // "graph" (casting-first workspace) or "classic".
        internal string Surface { get; private set; }
        internal string Behaviour { get; private set; }
        // True: the real OS cursor was moved there; false: Unity's pointer
        // handlers were driven directly (ExecuteEvents).
        internal bool Physical { get; private set; }
        // The control the pointer was aimed at (null: a neutral point).
        internal string Aimed { get; private set; }
        // The control clicked just before this reading, if any.
        internal string Clicked { get; private set; }
        // The one control that should draw a highlight (null: none).
        internal string ExpectedOwner { get; private set; }
        // Physical readings: the topmost control Unity's raycast finds at
        // the cursor.
        internal string TopControl { get; private set; }
        internal IReadOnlyList<string> Highlighted { get; private set; }
        internal IReadOnlyList<string> PointerInside { get; private set; }
        internal string Selection { get; private set; }
        internal bool SelectionIsPlannerControl { get; private set; }
        // Physical readings: whether Unity's cursor position is inside the
        // highlighted control's screen rectangle.
        internal bool? CursorInsideOwner { get; private set; }
        internal string Detail { get; private set; }

        internal IEnumerable<string> Judge()
        {
            if (!string.Equals(Behaviour, HoverBehaviour.Fixed, StringComparison.Ordinal)) yield break;
            if (Highlighted.Count > 1)
                yield return "more-than-one-hover-owner:" + Label + ":" + Join(Highlighted);
            else if (ExpectedOwner == null && Highlighted.Count == 1)
                yield return "highlight-without-pointer:" + Label + ":" + Highlighted[0];
            else if (ExpectedOwner != null && (Highlighted.Count == 0 ||
                     !string.Equals(Highlighted[0], ExpectedOwner, StringComparison.Ordinal)))
                yield return "hover-owner-misaligned:" + Label + ":expected=" + ExpectedOwner +
                    ";highlighted=" + Join(Highlighted);
            if (SelectionIsPlannerControl)
                yield return "planner-control-holds-selection:" + Label + ":" + Selection;
            if (Physical && Aimed != null && !string.Equals(Aimed, TopControl, StringComparison.Ordinal))
                yield return "aimed-control-not-topmost:" + Label + ":aimed=" + Aimed + ";top=" +
                    (TopControl ?? "none");
            if (Physical && CursorInsideOwner == false)
                yield return "highlight-not-under-cursor:" + Label;
        }

        // The owner-reported ghost: after a click on one control and a hover
        // on another, BOTH draw a highlight.
        internal bool ShowsGhost
        {
            get
            {
                return Clicked != null && ExpectedOwner != null &&
                    !string.Equals(Clicked, ExpectedOwner, StringComparison.Ordinal) &&
                    Highlighted.Contains(Clicked) && Highlighted.Contains(ExpectedOwner);
            }
        }

        internal string Describe()
        {
            return Label + "[" + Surface + "," + Behaviour + "," + (Physical ? "physical" : "synthetic") +
                "]aimed=" + (Aimed ?? "neutral") + (Clicked == null ? string.Empty : ";clicked=" + Clicked) +
                ";expected=" + (ExpectedOwner ?? "none") +
                (Physical ? ";top=" + (TopControl ?? "none") : string.Empty) +
                ";highlighted=" + Join(Highlighted) + ";inside=" + Join(PointerInside) +
                ";selection=" + (Selection ?? "none") +
                (CursorInsideOwner == null ? string.Empty : ";cursorInsideOwner=" + CursorInsideOwner) +
                (Detail.Length == 0 ? string.Empty : ";" + Detail);
        }

        internal static string Join(IEnumerable<string> names)
        {
            string[] values = (names ?? Enumerable.Empty<string>()).ToArray();
            return values.Length == 0 ? "none" : string.Join("|", values);
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    // One stone button shown in one required visual state for the in-game
    // contrast capture (normal, hover, pressed, selected, disabled).
    internal sealed class ButtonStateObservation
    {
        internal ButtonStateObservation(string state, string control, string unityState,
            bool selectedPalette, string tint, string screenRect)
        {
            State = state ?? string.Empty;
            Control = control ?? "missing";
            UnityState = unityState ?? "missing";
            SelectedPalette = selectedPalette;
            Tint = tint ?? string.Empty;
            ScreenRect = screenRect ?? string.Empty;
        }

        internal string State { get; private set; }
        internal string Control { get; private set; }
        internal string UnityState { get; private set; }
        internal bool SelectedPalette { get; private set; }
        internal string Tint { get; private set; }
        // "x,y,width,height" in Unity screen pixels (origin bottom left).
        internal string ScreenRect { get; private set; }

        internal bool Shown
        {
            get
            {
                switch (State)
                {
                    case "normal": return UnityState == "Normal" && !SelectedPalette;
                    case "hover": return UnityState == "Highlighted" && !SelectedPalette;
                    case "pressed": return UnityState == "Pressed";
                    case "selected": return UnityState == "Normal" && SelectedPalette;
                    case "disabled": return UnityState == "Disabled";
                    default: return false;
                }
            }
        }

        internal string Describe()
        {
            return State + "=" + Control + ":" + UnityState + (SelectedPalette ? "+selected" : string.Empty) +
                (Tint.Length == 0 ? string.Empty : ";tint=" + Tint) + ";rect=" + ScreenRect;
        }
    }

    internal sealed class HoverOwnershipRecord
    {
        internal static readonly string[] RequiredButtonStates =
            { "normal", "hover", "pressed", "selected", "disabled" };
        // The live pointer sweep aims at this many planner controls.
        internal const int MinimumPhysicalAims = 5;

        private readonly List<HoverSample> _samples = new List<HoverSample>();
        private readonly List<ButtonStateObservation> _buttons = new List<ButtonStateObservation>();
        private readonly List<string> _notes = new List<string>();
        private readonly List<string> _failures = new List<string>();

        internal bool ProbeAvailable { get; set; }
        internal string Screen { get; set; }
        internal int PlannerRootsAfterReopen { get; set; } = -1;
        internal int GraphSelectablesBeforeClose { get; set; } = -1;
        internal int GraphSelectablesAfterReopen { get; set; } = -1;
        internal bool Completed { get; set; }

        internal IList<HoverSample> Samples { get { return _samples.AsReadOnly(); } }
        internal IList<ButtonStateObservation> Buttons { get { return _buttons.AsReadOnly(); } }
        internal IList<string> Notes { get { return _notes.AsReadOnly(); } }
        internal IList<string> Failures { get { return _failures.AsReadOnly(); } }

        internal HoverSample Add(HoverSample sample)
        {
            if (sample == null) throw new ArgumentNullException("sample");
            _samples.Add(sample);
            return sample;
        }

        internal void AddButton(ButtonStateObservation observation)
        {
            if (observation == null) throw new ArgumentNullException("observation");
            _buttons.Add(observation);
        }

        internal void AddNote(string note)
        {
            if (!string.IsNullOrEmpty(note)) _notes.Add(note);
        }

        internal void AddFailure(string failure)
        {
            if (!string.IsNullOrEmpty(failure)) _failures.Add(failure);
        }

        // The rc6 reproduction showed the owner-reported ghost (two
        // highlights after a click then a hover) on the Classic screen.
        internal bool GhostReproduced
        {
            get
            {
                return _samples.Any(sample => sample.Behaviour == HoverBehaviour.Rc6 &&
                    sample.Surface == "classic" && sample.ShowsGhost);
            }
        }

        internal IList<string> Violations()
        {
            var violations = new List<string>();
            if (!ProbeAvailable) violations.Add("hover-probe-unavailable");
            violations.AddRange(_failures.Select(failure => "failure:" + failure));
            foreach (HoverSample sample in _samples) violations.AddRange(sample.Judge());
            // Coverage: a truncated run never passes.
            List<HoverSample> judged = _samples.Where(sample => sample.Behaviour == HoverBehaviour.Fixed).ToList();
            RequireCoverage(violations, judged, "graph", false, "graph-synthetic");
            RequireCoverage(violations, judged, "classic", false, "classic-synthetic");
            RequireCoverage(violations, judged, "reopen", false, "reopen-synthetic");
            if (!judged.Any(sample => sample.Surface == "graph" && !sample.Physical && sample.Clicked != null))
                violations.Add("missing-sample:graph-click-then-hover");
            if (!judged.Any(sample => sample.Surface == "classic" && !sample.Physical && sample.Clicked != null))
                violations.Add("missing-sample:classic-click-then-hover");
            int aims = judged.Count(sample => sample.Physical && sample.Aimed != null);
            if (aims < MinimumPhysicalAims)
                violations.Add("missing-sample:physical-aims=" + aims + "<" + MinimumPhysicalAims);
            if (!judged.Any(sample => sample.Physical && sample.Aimed == null))
                violations.Add("missing-sample:physical-neutral");
            if (!GhostReproduced) violations.Add("rc6-ghost-not-reproduced");
            foreach (string state in RequiredButtonStates)
            {
                ButtonStateObservation observation = _buttons.FirstOrDefault(value => value.State == state);
                if (observation == null) violations.Add("button-state-missing:" + state);
                else if (!observation.Shown)
                    violations.Add("button-state-not-shown:" + observation.Describe());
            }
            // Close/reopen: exactly one planner root, and the reopened
            // workspace is still single-owner (judged by its samples above).
            if (PlannerRootsAfterReopen != 1)
                violations.Add("planner-roots-after-reopen=" + PlannerRootsAfterReopen);
            if (!Completed) violations.Add("hover-sequence-incomplete");
            return violations;
        }

        private static void RequireCoverage(List<string> violations, List<HoverSample> judged,
            string surface, bool physical, string name)
        {
            if (!judged.Any(sample => sample.Surface == surface && sample.Physical == physical &&
                    sample.ExpectedOwner != null))
                violations.Add("missing-sample:" + name + "-owner");
            if (!judged.Any(sample => sample.Surface == surface && sample.Physical == physical &&
                    sample.ExpectedOwner == null))
                violations.Add("missing-sample:" + name + "-exit");
        }

        internal string Describe()
        {
            return "probe=" + ProbeAvailable + ";screen=" + (Screen ?? "unknown") +
                ";samples=" + _samples.Count + ";ghostReproduced=" + GhostReproduced +
                ";buttons=" + string.Join(",", _buttons.Select(value => value.Describe()).ToArray()) +
                ";rootsAfterReopen=" + PlannerRootsAfterReopen +
                ";graphSelectables=" + GraphSelectablesBeforeClose + "->" + GraphSelectablesAfterReopen +
                ";completed=" + Completed +
                (_failures.Count == 0 ? string.Empty : ";failures=" + string.Join(",", _failures.ToArray())) +
                (_notes.Count == 0 ? string.Empty : ";notes=" + string.Join(",", _notes.ToArray()));
        }
    }
}
