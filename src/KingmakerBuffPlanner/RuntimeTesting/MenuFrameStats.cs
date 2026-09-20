using System;
using System.Globalization;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // Pure luma statistics for a captured game frame. Kept free of Unity
    // types so the black-frame classification is unit-testable offline.
    internal sealed class MenuFrameLumaSummary
    {
        public int SampleCount { get; set; }
        public float Minimum { get; set; }
        public float Maximum { get; set; }
        public float Mean { get; set; }
        public float BlackFraction { get; set; }

        public bool IsNonBlack
        {
            get { return MenuFrameStats.IsNonBlack(this); }
        }

        public string Describe()
        {
            return "samples=" + SampleCount.ToString(CultureInfo.InvariantCulture) +
                ";min=" + Minimum.ToString("F4", CultureInfo.InvariantCulture) +
                ";max=" + Maximum.ToString("F4", CultureInfo.InvariantCulture) +
                ";mean=" + Mean.ToString("F4", CultureInfo.InvariantCulture) +
                ";blackFraction=" + BlackFraction.ToString("F4", CultureInfo.InvariantCulture) +
                ";nonBlack=" + (IsNonBlack ? "True" : "False");
        }
    }

    internal static class MenuFrameStats
    {
        // A rendered Kingmaker main menu always contains bright background art
        // and light text; a fully presented black frame (for example a paused
        // or non-presenting swap chain) has effectively no non-black pixels.
        // The threshold only claims "not entirely black", never menu content.
        internal const float BlackPixelLuma = 0.02f;
        internal const float NonBlackMaximumBlackFraction = 0.98f;

        internal static bool IsNonBlack(MenuFrameLumaSummary summary)
        {
            if (summary == null || summary.SampleCount <= 0) return false;
            return summary.BlackFraction < NonBlackMaximumBlackFraction;
        }

        internal static MenuFrameLumaSummary Summarize(float[] lumaSamples)
        {
            if (lumaSamples == null) throw new ArgumentNullException("lumaSamples");
            if (lumaSamples.Length == 0) throw new ArgumentException("At least one luma sample is required.", "lumaSamples");
            float minimum = lumaSamples[0];
            float maximum = lumaSamples[0];
            double total = 0;
            int black = 0;
            for (int i = 0; i < lumaSamples.Length; i++)
            {
                float value = lumaSamples[i];
                if (value < minimum) minimum = value;
                if (value > maximum) maximum = value;
                total += value;
                if (value <= BlackPixelLuma) black++;
            }
            return new MenuFrameLumaSummary
            {
                SampleCount = lumaSamples.Length,
                Minimum = minimum,
                Maximum = maximum,
                Mean = (float)(total / lumaSamples.Length),
                BlackFraction = black / (float)lumaSamples.Length
            };
        }
    }
}
