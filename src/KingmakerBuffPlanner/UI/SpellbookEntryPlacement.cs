using System;
using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    // Axis-aligned rectangle in the service window's local space (y up).
    internal struct PlacementRect
    {
        internal PlacementRect(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        internal readonly float XMin;
        internal readonly float YMin;
        internal readonly float XMax;
        internal readonly float YMax;
        internal float Width { get { return XMax - XMin; } }
        internal float Height { get { return YMax - YMin; } }

        internal bool Overlaps(PlacementRect other, float margin)
        {
            return XMin < other.XMax + margin && other.XMin - margin < XMax &&
                YMin < other.YMax + margin && other.YMin - margin < YMax;
        }

        internal bool Inside(PlacementRect bounds)
        {
            return XMin >= bounds.XMin && XMax <= bounds.XMax &&
                YMin >= bounds.YMin && YMax <= bounds.YMax;
        }

        public override string ToString()
        {
            return XMin.ToString("F0") + "," + YMin.ToString("F0") + "-" +
                XMax.ToString("F0") + "," + YMax.ToString("F0");
        }
    }

    internal sealed class SpellbookEntryPlacementResult
    {
        internal SpellbookEntryPlacementResult(string candidate, PlacementRect rect,
            bool conflictFree, string conflict)
        {
            Candidate = candidate;
            Rect = rect;
            ConflictFree = conflictFree;
            Conflict = conflict ?? string.Empty;
        }

        internal string Candidate { get; private set; }
        internal PlacementRect Rect { get; private set; }
        internal bool ConflictFree { get; private set; }
        internal string Conflict { get; private set; }
    }

    // WP2B: where the owned spellbook button may sit. The native service
    // window's top-bar Close button (143x137 at the top-right corner) used to
    // cover the planner button, which was pinned 20/18 units inside that same
    // corner; a click then reached the native Close instead. Placement is now
    // a bounded, ordered candidate list checked against the live native
    // controls, so the button never covers (or is covered by) a native
    // control. Pure: the Unity side supplies rectangles in one local space.
    internal static class SpellbookEntryPlacement
    {
        internal const float ControlMargin = 4f;
        private const float CloseGap = 12f;
        private const float EdgeInset = 20f;
        private const float TopInset = 16f;

        internal static SpellbookEntryPlacementResult Choose(PlacementRect window,
            PlacementRect nativeClose, IEnumerable<PlacementRect> nativeControls,
            float width, float height)
        {
            if (width <= 0f || height <= 0f)
                throw new ArgumentException("Button size must be positive.");
            var controls = new List<PlacementRect>(nativeControls ?? new PlacementRect[0]);
            var candidates = new[]
            {
                new KeyValuePair<string, PlacementRect>("left-of-native-close",
                    new PlacementRect(nativeClose.XMin - CloseGap - width,
                        nativeClose.YMax - TopInset - height,
                        nativeClose.XMin - CloseGap, nativeClose.YMax - TopInset)),
                new KeyValuePair<string, PlacementRect>("below-native-close",
                    new PlacementRect(window.XMax - EdgeInset - width,
                        nativeClose.YMin - CloseGap - height,
                        window.XMax - EdgeInset, nativeClose.YMin - CloseGap)),
                new KeyValuePair<string, PlacementRect>("bottom-right",
                    new PlacementRect(window.XMax - EdgeInset - width,
                        window.YMin + EdgeInset,
                        window.XMax - EdgeInset, window.YMin + EdgeInset + height)),
                new KeyValuePair<string, PlacementRect>("bottom-left",
                    new PlacementRect(window.XMin + EdgeInset, window.YMin + EdgeInset,
                        window.XMin + EdgeInset + width, window.YMin + EdgeInset + height))
            };
            string firstConflict = null;
            foreach (KeyValuePair<string, PlacementRect> candidate in candidates)
            {
                string conflict = ConflictOf(candidate.Value, window, nativeClose, controls);
                if (conflict == null)
                    return new SpellbookEntryPlacementResult(candidate.Key,
                        candidate.Value, true, string.Empty);
                if (firstConflict == null) firstConflict = candidate.Key + ":" + conflict;
            }
            // Every candidate collides: keep the one below the close button
            // (it never covers the close affordance) and report the conflict.
            return new SpellbookEntryPlacementResult(candidates[1].Key,
                candidates[1].Value, false, firstConflict);
        }

        private static string ConflictOf(PlacementRect candidate, PlacementRect window,
            PlacementRect nativeClose, List<PlacementRect> controls)
        {
            if (!candidate.Inside(window)) return "outside-window";
            if (candidate.Overlaps(nativeClose, ControlMargin)) return "native-close";
            for (int index = 0; index < controls.Count; index++)
                if (candidate.Overlaps(controls[index], ControlMargin))
                    return "native-control:" + controls[index];
            return null;
        }
    }
}
