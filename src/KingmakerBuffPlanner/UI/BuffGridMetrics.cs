using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    internal static class CompleteNameLayout
    {
        internal const float TextLeft = 82f;
        internal const float TextRight = 12f;
        internal const float TextTop = 8f;
        internal const float MinimumNameHeight = 22f;
        internal const float NameToAvailabilityGap = 3f;
        internal const float AvailabilityHeight = 20f;
        internal const float ConfigurationHeight = 18f;
        internal const float BottomInset = 7f;
        internal const float RoutineChipWidth = 54f;
        internal const float RoutineChipSize = 16f;
        internal const float RoutineChipSpacing = 2f;
        internal const float BadgeWidth = RoutineChipWidth;

        internal static float NameWidth(float cellWidth)
        {
            if (cellWidth <= 0) throw new ArgumentOutOfRangeException("cellWidth");
            return Math.Max(72f, cellWidth - TextLeft - TextRight);
        }

        internal static float RequiredCardHeight(
            float baselineHeight, float preferredNameHeight)
        {
            if (baselineHeight <= 0) throw new ArgumentOutOfRangeException("baselineHeight");
            if (preferredNameHeight < 0)
                throw new ArgumentOutOfRangeException("preferredNameHeight");
            float nameHeight = Math.Max(MinimumNameHeight, preferredNameHeight);
            float required = TextTop + nameHeight + NameToAvailabilityGap +
                AvailabilityHeight + ConfigurationHeight + BottomInset;
            return Math.Max(baselineHeight, (float)Math.Ceiling(required));
        }
    }

    internal sealed class BuffGridLayout
    {
        private readonly IReadOnlyList<float> _rowHeights;
        private readonly IReadOnlyList<float> _rowOffsets;

        private BuffGridLayout(
            IEnumerable<float> rowHeights,
            IEnumerable<float> rowOffsets,
            float spacing,
            float contentHeight)
        {
            _rowHeights = new ReadOnlyCollection<float>(rowHeights.ToList());
            _rowOffsets = new ReadOnlyCollection<float>(rowOffsets.ToList());
            Spacing = spacing;
            ContentHeight = contentHeight;
        }

        internal int RowCount { get { return _rowHeights.Count; } }
        internal float Spacing { get; private set; }
        internal float ContentHeight { get; private set; }

        internal float RowHeight(int row)
        {
            if (row < 0 || row >= RowCount) throw new ArgumentOutOfRangeException("row");
            return _rowHeights[row];
        }

        internal float RowOffset(int row)
        {
            if (row < 0 || row >= RowCount) throw new ArgumentOutOfRangeException("row");
            return _rowOffsets[row];
        }

        internal float ScrollOffsetForItem(int itemIndex)
        {
            if (itemIndex < 0) throw new ArgumentOutOfRangeException("itemIndex");
            int row = itemIndex / BuffGridMetrics.ColumnCount;
            return RowOffset(row);
        }

        internal int FirstVisibleRow(float scrollOffset)
        {
            if (RowCount == 0) return 0;
            float offset = Math.Max(0, scrollOffset);
            int low = 0;
            int high = RowCount - 1;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                float bottom = _rowOffsets[middle] + _rowHeights[middle] + Spacing;
                if (bottom <= offset) low = middle + 1;
                else high = middle;
            }
            return Math.Max(0, low - 1);
        }

        internal static BuffGridLayout Calculate(
            IEnumerable<float> itemHeights,
            float minimumRowHeight,
            float spacing)
        {
            if (minimumRowHeight <= 0)
                throw new ArgumentOutOfRangeException("minimumRowHeight");
            if (spacing < 0) throw new ArgumentOutOfRangeException("spacing");
            List<float> items = (itemHeights ?? throw new ArgumentNullException("itemHeights"))
                .ToList();
            if (items.Any(value => value < 0))
                throw new ArgumentOutOfRangeException("itemHeights");
            var heights = new List<float>();
            var offsets = new List<float>();
            float offset = 0;
            for (int index = 0; index < items.Count; index += BuffGridMetrics.ColumnCount)
            {
                offsets.Add(offset);
                float height = Math.Max(minimumRowHeight,
                    items.Skip(index).Take(BuffGridMetrics.ColumnCount).DefaultIfEmpty(0).Max());
                heights.Add(height);
                offset += height + spacing;
            }
            return new BuffGridLayout(heights, offsets, spacing,
                heights.Count == 0 ? 0 : offset - spacing);
        }
    }

    internal sealed class BuffGridMetrics
    {
        internal const int ColumnCount = 4;
        internal const int PoolCapacity = 32;

        internal static int RowCount(int itemCount)
        {
            if (itemCount < 0) throw new ArgumentOutOfRangeException("itemCount");
            return (itemCount + ColumnCount - 1) / ColumnCount;
        }

        internal static int ModelIndex(int firstRow, int pooledCardIndex)
        {
            if (firstRow < 0) throw new ArgumentOutOfRangeException("firstRow");
            if (pooledCardIndex < 0 || pooledCardIndex >= PoolCapacity)
                throw new ArgumentOutOfRangeException("pooledCardIndex");
            return checked(firstRow * ColumnCount + pooledCardIndex);
        }

        internal static BuffGridMetrics Calculate(float viewportWidth, float viewportHeight)
        {
            if (viewportWidth <= 0) throw new ArgumentOutOfRangeException("viewportWidth");
            if (viewportHeight <= 0) throw new ArgumentOutOfRangeException("viewportHeight");
            const float spacing = 14f;
            const float sideInset = 20f;
            float cellWidth = (viewportWidth - spacing * (ColumnCount - 1) - sideInset * 2f) / ColumnCount;
            return new BuffGridMetrics
            {
                Columns = ColumnCount,
                CellWidth = Math.Max(220f, cellWidth),
                CellHeight = viewportHeight < 500f ? 92f : 104f,
                HorizontalSpacing = spacing,
                VerticalSpacing = 10f,
                SideInset = sideInset,
                HorizontalScrolling = false
            };
        }

        internal int Columns { get; private set; }
        internal float CellWidth { get; private set; }
        internal float CellHeight { get; private set; }
        internal float HorizontalSpacing { get; private set; }
        internal float VerticalSpacing { get; private set; }
        internal float SideInset { get; private set; }
        internal bool HorizontalScrolling { get; private set; }
    }
}
