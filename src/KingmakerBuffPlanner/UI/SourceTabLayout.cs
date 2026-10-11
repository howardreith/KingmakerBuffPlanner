using System;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    // Native UI finish (UI-03, UI-06): the catalogue's four source tabs share
    // one row of the catalogue lane. The planner's canvas has no scaler, so
    // its text keeps its pixel size while the lane narrows with the screen
    // (about 166 px at 1280x720). Four equal quarters left "Abilities" no
    // room, and each caption shrank on its own until it touched or overran
    // the button's frame (kbpnui-final-qual-07, kbpnui-final-qual-720-05).
    //
    // One caption size for all four - the largest, up to the role's own, at
    // which every caption fits between margins clear of the button's gold
    // frame - and each tab as wide as its caption then needs, the rest of
    // the row shared evenly. Units are the canvas's (screen pixels).
    public sealed class SourceTabLayout
    {
        // Each side of a caption, inside the button art's frame.
        public const float CaptionMargin = 6f;
        public const float TabGap = 2f;

        private SourceTabLayout()
        {
        }

        public float CaptionSize { get; private set; }
        public float[] Lefts { get; private set; }
        public float[] Widths { get; private set; }
        // False when even the minimum size leaves the row too narrow.
        public bool Fits { get; private set; }

        // captionWidths: each caption's width at maxSize, in the same units.
        public static SourceTabLayout Compute(float rowWidth, float[] captionWidths, float maxSize, float minSize)
        {
            if (rowWidth <= 0f) throw new ArgumentException("The tab row has no width.");
            if (captionWidths == null || captionWidths.Length == 0 || captionWidths.Any(width => width <= 0f))
                throw new ArgumentException("Every tab needs a measured caption.");
            if (minSize <= 0f || maxSize < minSize) throw new ArgumentException("The caption size range is empty.");
            int count = captionWidths.Length;
            float fixedSpace = count * 2f * CaptionMargin + (count - 1) * TabGap;
            float text = captionWidths.Sum();
            float size = maxSize;
            if (fixedSpace + text > rowWidth) size = (rowWidth - fixedSpace) / text * maxSize;
            // Half-point steps, never above what fits.
            size = (float)Math.Floor(size * 2f) / 2f;
            if (size > maxSize) size = maxSize;
            if (size < minSize) size = minSize;
            float[] needs = captionWidths.Select(width => width * size / maxSize + 2f * CaptionMargin).ToArray();
            float used = needs.Sum() + (count - 1) * TabGap;
            float share = Math.Max(0f, rowWidth - used) / count;
            var widths = new float[count];
            var lefts = new float[count];
            float left = 0f;
            for (int index = 0; index < count; index++)
            {
                widths[index] = needs[index] + share;
                lefts[index] = left;
                left += widths[index] + TabGap;
            }
            return new SourceTabLayout
            {
                CaptionSize = size,
                Lefts = lefts,
                Widths = widths,
                Fits = used <= rowWidth + 0.01f
            };
        }
    }
}
