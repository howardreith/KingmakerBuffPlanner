using System;

namespace KingmakerBuffPlanner.UI
{
    // An sRGB colour with components in 0..1 (Unity's Color without Unity).
    public struct UiRgb
    {
        public UiRgb(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public float R;
        public float G;
        public float B;

        public static UiRgb FromBytes(int r, int g, int b)
        {
            return new UiRgb(r / 255f, g / 255f, b / 255f);
        }

        // A Unity colour tint multiplies the sprite's colour per channel.
        public UiRgb Times(UiRgb tint)
        {
            return new UiRgb(R * tint.R, G * tint.G, B * tint.B);
        }

        public override string ToString()
        {
            return "(" + R.ToString("0.000") + "," + G.ToString("0.000") + "," + B.ToString("0.000") + ")";
        }
    }

    // WCAG 2.x relative luminance and contrast ratio.
    public static class UiContrast
    {
        public static double RelativeLuminance(UiRgb colour)
        {
            return 0.2126 * Linear(colour.R) + 0.7152 * Linear(colour.G) + 0.0722 * Linear(colour.B);
        }

        public static double Ratio(UiRgb first, UiRgb second)
        {
            double a = RelativeLuminance(first);
            double b = RelativeLuminance(second);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static double Linear(float channel)
        {
            double c = Math.Max(0.0, Math.Min(1.0, channel));
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }

    public enum PlannerButtonState
    {
        Normal,
        Hover,
        Pressed,
        Selected,
        Disabled
    }

    // The planner's button contract (addendum 6.2): the native gray stone
    // stays, darkened by a per-state tint so a warm-ivory caption reads on it
    // in every state. The stone colours were MEASURED from the rendered
    // native button sprite (rc6 frame casting-ws-reload-20260924-rc6-01/
    // ws-interact-authored.png, 1920x1200, white-tinted chips and tabs):
    // the median stone behind a caption and its lighter, 90th-percentile
    // bevels, where the old light-cream caption fell to 3.0:1.
    public static class PlannerButtonPalette
    {
        public static readonly UiRgb StoneMedian = new UiRgb(0.427f, 0.408f, 0.365f);
        public static readonly UiRgb StoneLight = new UiRgb(0.518f, 0.502f, 0.478f);

        // The caption before this correction (for the recorded measurement).
        public static readonly UiRgb PreviousCaption = new UiRgb(0.965f, 0.894f, 0.710f);

        public static readonly UiRgb Caption = new UiRgb(0.980f, 0.955f, 0.870f);
        public static readonly UiRgb DisabledCaption = new UiRgb(0.690f, 0.665f, 0.615f);

        public static readonly UiRgb NormalTint = new UiRgb(0.560f, 0.520f, 0.470f);
        public static readonly UiRgb HoverTint = new UiRgb(0.720f, 0.660f, 0.580f);
        public static readonly UiRgb PressedTint = new UiRgb(0.440f, 0.400f, 0.360f);
        // Selected (a chosen tab or toggle): burgundy on the stone.
        public static readonly UiRgb SelectedTint = new UiRgb(0.800f, 0.300f, 0.220f);
        public static readonly UiRgb SelectedHoverTint = new UiRgb(0.920f, 0.380f, 0.280f);
        public static readonly UiRgb DisabledTint = new UiRgb(0.500f, 0.500f, 0.500f);

        public const double NormalTextMinimum = 4.5;
        public const double DisabledTextMinimum = 3.0;

        public static UiRgb TintFor(PlannerButtonState state)
        {
            switch (state)
            {
                case PlannerButtonState.Hover: return HoverTint;
                case PlannerButtonState.Pressed: return PressedTint;
                case PlannerButtonState.Selected: return SelectedTint;
                case PlannerButtonState.Disabled: return DisabledTint;
                default: return NormalTint;
            }
        }

        public static UiRgb CaptionFor(PlannerButtonState state)
        {
            return state == PlannerButtonState.Disabled ? DisabledCaption : Caption;
        }

        // The caption against the LIGHTER measured stone of a state: the
        // worst case behind a glyph.
        public static double WorstCaseRatio(PlannerButtonState state)
        {
            return UiContrast.Ratio(CaptionFor(state), StoneLight.Times(TintFor(state)));
        }

        public static double MedianRatio(PlannerButtonState state)
        {
            return UiContrast.Ratio(CaptionFor(state), StoneMedian.Times(TintFor(state)));
        }
    }
}
