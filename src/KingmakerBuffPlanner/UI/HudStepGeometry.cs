using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    // A rectangle in the native HUD panel's own space (Menu_Buttons48px:
    // origin bottom-left, y up, panel units).
    public struct HudRect
    {
        public HudRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float Right { get { return X + Width; } }
        public float Top { get { return Y + Height; } }

        public bool Overlaps(HudRect other)
        {
            return X < other.Right - 0.01f && other.X < Right - 0.01f &&
                Y < other.Top - 0.01f && other.Y < Top - 0.01f;
        }

        public override string ToString()
        {
            return Format(X) + "," + Format(Y) + "," + Format(Width) + "x" + Format(Height);
        }

        private static string Format(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    // One piece of the native frame texture drawn at a place in the panel:
    // source in texture pixels (row 0 at the bottom), target in panel units.
    public sealed class HudFramePiece
    {
        public HudFramePiece(string name, int sourceX, int sourceY, int sourceWidth, int sourceHeight,
            HudRect target)
        {
            Name = name;
            SourceX = sourceX;
            SourceY = sourceY;
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;
            Target = target;
        }

        public string Name { get; private set; }
        public int SourceX { get; private set; }
        public int SourceY { get; private set; }
        public int SourceWidth { get; private set; }
        public int SourceHeight { get; private set; }
        public HudRect Target { get; private set; }
    }

    // Native UI finish (UI-09), the owner's stepped enclosure (R12): four
    // cells form a third row directly above the first four native columns;
    // the panel's own frame rises over them and steps down after the fourth
    // into the native frame. Pure geometry from the live native measurements
    // and the native frame texture's layout (TBM_backButton, kbpnui-facts-03:
    // the top border's rows hold the rail and its left fitting, the right
    // 120 x 120 px hold the ornate corner whose right 34 px are the frame's
    // vertical rail, rows 6-34 from the top the horizontal rail).
    //
    // The raised frame IS the native top border, lifted one row: a strip of
    // the texture's top rows from its left edge (fitting, rail, the stone
    // below) across the four columns, then the texture's own corner, its
    // vertical rail standing just after the fourth column. The native rail
    // the corner's foot crosses is drawn again over it, from the same texture
    // pixels the native frame shows there, so the step meets the native rail
    // instead of cutting through it. The native rail under the new row is
    // covered - the row has no divider through it.
    public sealed class HudStepLayout
    {
        public const int CornerSizePx = 120;
        public const int CornerRailPx = 34;
        public const int RailTopRowPx = 6;
        public const int RailBottomRowPx = 34;

        private HudStepLayout()
        {
        }

        public HudRect[] Cells { get; private set; }
        public HudRect Strip { get; private set; }
        public HudRect Corner { get; private set; }
        public HudRect Junction { get; private set; }
        public float RaisedTop { get; private set; }
        public float UnitsPerPixel { get; private set; }
        public int StripRowsPx { get; private set; }
        public float FrameTop { get; private set; }

        // columnLefts: the native columns' left edges (at least five), the
        // native cell size, the native top row's bottom, the row pitch, the
        // native frame's top edge and the frame texture's units per pixel.
        public static HudStepLayout Compute(float[] columnLefts, float cellWidth, float cellHeight,
            float topRowBottom, float rowPitch, float frameTop, float unitsPerPixel)
        {
            if (columnLefts == null || columnLefts.Length < 5)
                throw new ArgumentException("Five native columns are required.", "columnLefts");
            if (cellWidth <= 0f || cellHeight <= 0f || rowPitch <= 0f || unitsPerPixel <= 0f)
                throw new ArgumentException("Native metrics must be positive.");
            float[] lefts = columnLefts.OrderBy(value => value).ToArray();
            float nativeTop = topRowBottom + cellHeight;
            // The lifted border reaches from the native top row's top edge to
            // the native frame's top raised by one row pitch, in whole rows.
            int stripRows = (int)Math.Round((frameTop + rowPitch - nativeTop) / unitsPerPixel);
            float raisedTop = nativeTop + stripRows * unitsPerPixel;
            float corner = CornerSizePx * unitsPerPixel;
            float cornerRail = CornerRailPx * unitsPerPixel;
            float cornerRight = lefts[4] + cornerRail;
            var layout = new HudStepLayout
            {
                UnitsPerPixel = unitsPerPixel,
                StripRowsPx = stripRows,
                RaisedTop = raisedTop,
                FrameTop = frameTop,
                Cells = Enumerable.Range(0, 4)
                    .Select(index => new HudRect(lefts[index], topRowBottom + rowPitch, cellWidth, cellHeight))
                    .ToArray(),
                Strip = new HudRect(0f, nativeTop, cornerRight - corner, raisedTop - nativeTop),
                Corner = new HudRect(cornerRight - corner, nativeTop, corner, raisedTop - nativeTop),
                Junction = new HudRect(lefts[4], frameTop - RailBottomRowPx * unitsPerPixel,
                    cornerRail * 1.5f, (RailBottomRowPx - RailTopRowPx) * unitsPerPixel)
            };
            return layout;
        }

        // The frame pieces in drawing order (the cells are drawn after them).
        // nativeFrameWidth and the texture's left and right slice borders
        // place the junction on the exact pixels the native tiled frame
        // shows there.
        public IList<HudFramePiece> FramePieces(int textureWidth, int textureHeight, float nativeFrameWidth,
            int borderLeftPx, int borderRightPx)
        {
            if (textureWidth < CornerSizePx * 2 || textureHeight < StripRowsPx || StripRowsPx < RailBottomRowPx)
                throw new InvalidOperationException("The native frame texture is smaller than its border.");
            var pieces = new List<HudFramePiece>();
            int stripPx = (int)Math.Round(Strip.Width / UnitsPerPixel);
            if (stripPx < 1 || stripPx > textureWidth - CornerSizePx)
                throw new InvalidOperationException("The raised strip does not fit the native frame texture.");
            int rowsBottom = textureHeight - StripRowsPx;
            pieces.Add(new HudFramePiece("Strip", 0, rowsBottom, stripPx, StripRowsPx, Strip));
            pieces.Add(new HudFramePiece("Corner", textureWidth - CornerSizePx, rowsBottom, CornerSizePx,
                StripRowsPx, Corner));
            int railBottom = textureHeight - RailBottomRowPx;
            int railRows = RailBottomRowPx - RailTopRowPx;
            foreach (Tuple<float, float, int> segment in NativeColumns(Junction.X, Junction.Right,
                textureWidth, nativeFrameWidth, borderLeftPx, borderRightPx))
            {
                int width = (int)Math.Round((segment.Item2 - segment.Item1) / UnitsPerPixel);
                if (width < 1) continue;
                pieces.Add(new HudFramePiece("Junction", segment.Item3, railBottom, width, railRows,
                    new HudRect(segment.Item1, Junction.Y, width * UnitsPerPixel, Junction.Height)));
            }
            return pieces;
        }

        // The native frame image is a tiled 9-slice from the panel's left
        // edge: the left border at its size, the centre repeated from the
        // left border on, the right border at the right. Returns the panel
        // spans of [from, to) with the texture column each starts at.
        private List<Tuple<float, float, int>> NativeColumns(float from, float to, int textureWidth,
            float frameWidth, int borderLeftPx, int borderRightPx)
        {
            var result = new List<Tuple<float, float, int>>();
            float left = borderLeftPx * UnitsPerPixel;
            float rightStart = frameWidth - borderRightPx * UnitsPerPixel;
            float tile = (textureWidth - borderLeftPx - borderRightPx) * UnitsPerPixel;
            float position = from;
            while (position < to - 0.01f)
            {
                float end;
                int column;
                if (position < left)
                {
                    end = Math.Min(to, left);
                    column = (int)Math.Round(position / UnitsPerPixel);
                }
                else if (position >= rightStart)
                {
                    end = to;
                    column = textureWidth - borderRightPx + (int)Math.Round((position - rightStart) / UnitsPerPixel);
                }
                else
                {
                    float offset = (position - left) % tile;
                    end = Math.Min(Math.Min(to, rightStart), position + (tile - offset));
                    column = borderLeftPx + (int)Math.Round(offset / UnitsPerPixel);
                }
                result.Add(Tuple.Create(position, end, column));
                position = end;
            }
            return result;
        }

        // The geometry the owner specified: one cell per native column, the
        // cells clear of every native cell, the raised frame over the new row,
        // the step just after the fourth column, nothing over the native cells
        // and nothing of the frame over the fifth column's cells.
        public string Violation(float[] columnLefts, float cellWidth, float topRowBottom)
        {
            float[] lefts = columnLefts.OrderBy(value => value).ToArray();
            for (int index = 0; index < 4; index++)
                if (Math.Abs(Cells[index].X - lefts[index]) > 0.05f) return "cell-column:" + index;
            var nativeTop = new HudRect(lefts[0], topRowBottom, lefts[lefts.Length - 1] + cellWidth - lefts[0],
                Cells[0].Height);
            if (Cells.Any(cell => cell.Overlaps(nativeTop))) return "cell-over-native-row";
            if (RaisedTop < Cells[0].Top + 1f) return "frame-not-over-row";
            float stepRail = Corner.Right - CornerRailPx * UnitsPerPixel;
            if (stepRail < Cells[3].Right - 0.05f || stepRail > lefts[4] + 0.05f) return "step-not-after-fourth";
            if (Strip.Y > Cells[0].Y || Strip.X > Cells[0].X || Corner.X > Strip.Right + 0.05f)
                return "row-not-enclosed";
            if (new[] { Strip, Corner, Junction }.Any(piece => piece.Overlaps(nativeTop)))
                return "frame-over-native-row";
            return null;
        }
    }
}
