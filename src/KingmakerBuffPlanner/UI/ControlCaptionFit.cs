using System;

namespace KingmakerBuffPlanner.UI
{
    // Pure sizing policy: converts a measured, fully styled caption extent
    // into the room an owned control must reserve so its text never
    // ellipsizes or shrinks below the readable size. Measurement itself is
    // done by the Unity adapter at rebuild boundaries, never per frame.
    internal static class ControlCaptionFit
    {
        internal const float HorizontalSafety = 2f;
        internal const float VerticalSafety = 2f;

        internal static float RequiredWidth(float styledTextWidth, float horizontalInset)
        {
            return styledTextWidth + (2f * horizontalInset) + HorizontalSafety;
        }

        internal static float RequiredHeight(float styledTextHeight, float verticalInset)
        {
            return styledTextHeight + (2f * verticalInset) + VerticalSafety;
        }

        internal static float ResolveExtent(float designMinimum, float requiredExtent)
        {
            if (float.IsNaN(requiredExtent) || requiredExtent < 0f ||
                float.IsInfinity(requiredExtent)) return designMinimum;
            return Math.Max(designMinimum, requiredExtent);
        }
    }
}
