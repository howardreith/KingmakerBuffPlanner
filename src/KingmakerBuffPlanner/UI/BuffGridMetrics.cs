using System;

namespace KingmakerBuffPlanner.UI
{
    internal sealed class BuffGridMetrics
    {
        internal const int ColumnCount = 4;

        internal static BuffGridMetrics Calculate(float viewportWidth, float viewportHeight)
        {
            if (viewportWidth <= 0) throw new ArgumentOutOfRangeException("viewportWidth");
            if (viewportHeight <= 0) throw new ArgumentOutOfRangeException("viewportHeight");
            const float spacing = 10f;
            float cellWidth = (viewportWidth - spacing * (ColumnCount - 1) - 12f) / ColumnCount;
            return new BuffGridMetrics
            {
                Columns = ColumnCount,
                CellWidth = Math.Max(220f, cellWidth),
                CellHeight = viewportHeight < 500f ? 92f : 104f,
                HorizontalScrolling = false
            };
        }

        internal int Columns { get; private set; }
        internal float CellWidth { get; private set; }
        internal float CellHeight { get; private set; }
        internal bool HorizontalScrolling { get; private set; }
    }
}
