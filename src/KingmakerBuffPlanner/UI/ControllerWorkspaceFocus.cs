using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    internal enum ControllerWorkspaceCommand { Up, Down, Left, Right, PreviousRoutine, NextRoutine, Confirm, Inspect, ScrollUp, ScrollDown }

    // Root-local coordinates, with positive Y pointing up. Keeping geometry
    // here makes directional selection testable without Unity or input state.
    internal struct ControllerFocusBounds
    {
        internal readonly float MinX, MinY, MaxX, MaxY;
        internal ControllerFocusBounds(float minX, float minY, float maxX, float maxY)
        { MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; }
        internal float CenterX { get { return (MinX + MaxX) * .5f; } }
        internal float CenterY { get { return (MinY + MaxY) * .5f; } }
        internal bool Usable
        {
            get
            {
                return MaxX > MinX && MaxY > MinY &&
                    !float.IsNaN(CenterX) && !float.IsNaN(CenterY) &&
                    !float.IsInfinity(CenterX) && !float.IsInfinity(CenterY);
            }
        }
    }

    internal sealed class ControllerFocusItem
    {
        internal readonly string Key, Region, Caption;
        internal readonly bool Enabled;
        internal readonly Action Activate, Reveal, Inspect;
        internal ControllerFocusBounds? Bounds { get; private set; }
        internal bool Visible { get; private set; }
        internal string ScrollKey { get; private set; }
        internal bool HasBounds { get { return Bounds.HasValue && Bounds.Value.Usable; } }
        internal ControllerFocusItem(string key, string region, string caption, bool enabled,
            Action activate, Action reveal = null, Action inspect = null,
            ControllerFocusBounds? bounds = null, bool visible = true, string scrollKey = null)
        {
            Key = key; Region = region; Caption = caption; Enabled = enabled;
            Activate = activate; Reveal = reveal; Inspect = inspect;
            Bounds = bounds; Visible = visible; ScrollKey = scrollKey;
        }
        internal void UpdateLayout(ControllerFocusBounds bounds, bool visible, string scrollKey)
        { Bounds = bounds; Visible = visible; ScrollKey = scrollKey; }
    }

    // A presentation-only focus cursor. Rebuild replaces live actions; only
    // semantic keys and the prior position survive, never Unity objects.
    internal sealed class ControllerWorkspaceFocus
    {
        private List<ControllerFocusItem> _items = new List<ControllerFocusItem>();
        private Dictionary<string, ControllerFocusItem> _byKey = new Dictionary<string, ControllerFocusItem>(StringComparer.Ordinal);
        private float? _horizontalAim;
        private static readonly string[] BodyRegions = { "Catalogue", "Casters", "Castings", "Recipients", "Inspector" };
        private const float Tolerance = .5f;
        internal string Key { get; private set; }
        // When a scroll pane shows only text, keep the pane selected without
        // treating the retained offscreen button as an actionable control.
        internal string ReadingScrollKey { get; private set; }
        internal ControllerFocusItem Current
        {
            get
            {
                ControllerFocusItem current;
                return Key != null && _byKey.TryGetValue(Key, out current) ? current : null;
            }
        }
        internal IReadOnlyList<ControllerFocusItem> Items { get { return _items; } }

        internal void Replace(IEnumerable<ControllerFocusItem> items)
        {
            ControllerFocusItem prior = Current;
            int ordinal = prior == null ? 0 : _items.Where(i => i.Region == prior.Region).ToList().IndexOf(prior);
            _items = items.ToList();
            if (_items.Select(i => i.Key).Distinct(StringComparer.Ordinal).Count() != _items.Count)
                throw new InvalidOperationException("Duplicate semantic controller focus key.");
            _byKey = _items.ToDictionary(i => i.Key, StringComparer.Ordinal);
            if (Key != null && _byKey.ContainsKey(Key)) return;
            var neighbors = prior == null ? new List<ControllerFocusItem>() : _items.Where(i => i.Region == prior.Region).ToList();
            ControllerFocusItem fallback = null;
            if (prior != null && prior.HasBounds)
                fallback = Nearest(neighbors.Where(i => i.HasBounds), prior);
            if (fallback == null && neighbors.Count != 0)
                fallback = neighbors[Math.Min(Math.Max(0, ordinal), neighbors.Count - 1)];
            if (fallback == null) fallback = InitialItem();
            Key = fallback == null ? null : fallback.Key;
            if (prior == null || fallback == null || prior.Region != fallback.Region) _horizontalAim = null;
        }

        internal void Restore(string key)
        {
            if (key == null || !_byKey.ContainsKey(key)) return;
            if (Key != key) _horizontalAim = null;
            Key = key;
            ReadingScrollKey = null;
        }

        internal void ClearReading() { ReadingScrollKey = null; }

        internal void UpdateLayout(string key, ControllerFocusBounds bounds, bool visible, string scrollKey)
        {
            ControllerFocusItem item;
            if (key != null && _byKey.TryGetValue(key, out item)) item.UpdateLayout(bounds, visible, scrollKey);
        }

        internal void Move(ControllerWorkspaceCommand command)
        {
            ReadingScrollKey = null;
            ControllerFocusItem current = Current;
            if (current == null || !current.HasBounds) return;
            ControllerFocusItem next = null;
            bool horizontal = command == ControllerWorkspaceCommand.Left || command == ControllerWorkspaceCommand.Right;
            float aim = ClampAim(_horizontalAim ?? current.Bounds.Value.MinX, current.Bounds.Value);
            if (horizontal)
                next = Horizontal(current, command == ControllerWorkspaceCommand.Left ? -1 : 1);
            else if (command == ControllerWorkspaceCommand.Up || command == ControllerWorkspaceCommand.Down)
                next = Vertical(current, command == ControllerWorkspaceCommand.Up ? 1 : -1, aim);
            if (next != null)
            {
                Key = next.Key;
                // A full-width row must not erase the column the player came
                // from: Down from a filter and Up again returns to that filter.
                _horizontalAim = horizontal ? next.Bounds.Value.CenterX : ClampAim(aim, next.Bounds.Value);
            }
        }

        // Manual scrolling must not leave Confirm aimed at an invisible row.
        // When no control is visible, retain a pane-level reading focus until
        // scrolling or navigation brings an actionable control into view.
        internal bool KeepVisible(string scrollKey)
        {
            if (string.IsNullOrEmpty(scrollKey)) return false;
            ControllerFocusItem current = Current;
            if (current != null && current.ScrollKey == scrollKey && current.HasBounds && current.Visible)
            { ReadingScrollKey = null; return true; }
            var candidates = _items.Where(i => i.HasBounds && i.Visible && i.ScrollKey == scrollKey).ToList();
            var sameRegion = current == null ? candidates : candidates.Where(i => i.Region == current.Region).ToList();
            ControllerFocusItem next = Nearest(sameRegion.Count == 0 ? candidates : sameRegion, current);
            if (next == null) { ReadingScrollKey = scrollKey; return false; }
            Key = next.Key;
            ReadingScrollKey = null;
            return true;
        }

        private ControllerFocusItem InitialItem()
        {
            var visible = _items.Where(i => i.Visible && i.HasBounds).ToList();
            var catalogue = visible.Where(i => i.Region == "Catalogue").ToList();
            return (catalogue.Count == 0 ? visible : catalogue)
                .OrderByDescending(i => i.Bounds.Value.MaxY).ThenBy(i => i.Bounds.Value.MinX)
                .ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault() ?? _items.FirstOrDefault();
        }

        private ControllerFocusItem Horizontal(ControllerFocusItem current, int direction)
        {
            ControllerFocusBounds origin = current.Bounds.Value;
            // Filters, routine tabs, footer actions and inspector pairs all
            // follow their visible row before crossing into a body column.
            ControllerFocusItem neighbor = _items.Where(i => i != current && i.Region == current.Region &&
                i.ScrollKey == current.ScrollKey && i.HasBounds && i.Visible)
                .Where(i => direction * (i.Bounds.Value.CenterX - origin.CenterX) > Tolerance &&
                    Overlap(origin.MinY, origin.MaxY, i.Bounds.Value.MinY, i.Bounds.Value.MaxY) > Tolerance)
                .OrderBy(i => Math.Abs(i.Bounds.Value.CenterX - origin.CenterX))
                .ThenBy(i => Math.Abs(i.Bounds.Value.CenterY - origin.CenterY))
                .ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault();
            if (neighbor != null) return neighbor;
            int region = Array.IndexOf(BodyRegions, current.Region);
            if (region < 0) return null;
            for (int index = region + direction; index >= 0 && index < BodyRegions.Length; index += direction)
            {
                var candidates = _items.Where(i => i.Region == BodyRegions[index] && i.HasBounds).ToList();
                if (candidates.Count == 0) continue;
                var visible = candidates.Where(i => i.Visible).ToList();
                return (visible.Count == 0 ? candidates : visible)
                    .OrderBy(i => Math.Abs(i.Bounds.Value.CenterY - origin.CenterY))
                    .ThenBy(i => Math.Abs(i.Bounds.Value.CenterX - origin.CenterX))
                    .ThenBy(i => i.Key, StringComparer.Ordinal).First();
            }
            return null;
        }

        private ControllerFocusItem Vertical(ControllerFocusItem current, int direction, float aim)
        {
            // Offscreen rows in the same list remain eligible. Fixed search
            // and filter controls cannot intercept a long list midway merely
            // because scrolling has carried earlier rows behind them.
            var ownRegion = _items.Where(i => i != current && i.Region == current.Region && i.HasBounds).ToList();
            ControllerFocusItem next = null;
            if (!string.IsNullOrEmpty(current.ScrollKey))
                next = VerticalNeighbor(ownRegion.Where(i => i.ScrollKey == current.ScrollKey), current, direction, false, aim);
            if (next == null)
                next = VerticalNeighbor(ownRegion.Where(i => string.IsNullOrEmpty(i.ScrollKey) ||
                    (string.IsNullOrEmpty(current.ScrollKey) && i.Visible)), current, direction, false, aim);
            if (next != null) return next;

            IEnumerable<ControllerFocusItem> boundary;
            if (Array.IndexOf(BodyRegions, current.Region) >= 0)
            {
                boundary = direction > 0 ? _items.Where(i => i.Region == "Routines" || i.Region == "Header")
                    : _items.Where(i => i.Region == "Footer");
            }
            else if (current.Region == "Header" && direction < 0)
                boundary = _items.Where(i => i.Region == "Routines" || Array.IndexOf(BodyRegions, i.Region) >= 0);
            else if (current.Region == "Routines")
                boundary = direction > 0 ? _items.Where(i => i.Region == "Header")
                    : _items.Where(i => Array.IndexOf(BodyRegions, i.Region) >= 0);
            else if (current.Region == "Footer" && direction > 0)
                boundary = _items.Where(i => Array.IndexOf(BodyRegions, i.Region) >= 0);
            else return null;
            return VerticalNeighbor(boundary.Where(i => i.Visible && i.HasBounds), current, direction, true, aim);
        }

        private static ControllerFocusItem VerticalNeighbor(IEnumerable<ControllerFocusItem> candidates,
            ControllerFocusItem current, int direction, bool alignColumn, float aim)
        {
            ControllerFocusBounds origin = current.Bounds.Value;
            var eligible = candidates.Where(i => direction > 0
                ? i.Bounds.Value.MinY >= origin.MaxY - Tolerance
                : i.Bounds.Value.MaxY <= origin.MinY + Tolerance);
            // At a section boundary choose the column under/above the cursor;
            // within a section choose the next row, then align within that row.
            return eligible.OrderBy(i => alignColumn
                    ? IntervalDistance(aim, i.Bounds.Value.MinX, i.Bounds.Value.MaxX)
                    : VerticalGap(origin, i.Bounds.Value, direction))
                .ThenBy(i => alignColumn ? VerticalGap(origin, i.Bounds.Value, direction)
                    : IntervalDistance(aim, i.Bounds.Value.MinX, i.Bounds.Value.MaxX))
                .ThenBy(i => Math.Abs(i.Bounds.Value.CenterX - aim))
                .ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault();
        }

        private static ControllerFocusItem Nearest(IEnumerable<ControllerFocusItem> candidates, ControllerFocusItem current)
        {
            if (current == null || !current.HasBounds)
                return candidates.OrderByDescending(i => i.Bounds.Value.MaxY).ThenBy(i => i.Bounds.Value.MinX)
                    .ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault();
            ControllerFocusBounds origin = current.Bounds.Value;
            return candidates.OrderBy(i => DistanceSquared(origin, i.Bounds.Value))
                .ThenBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault();
        }

        private static float DistanceSquared(ControllerFocusBounds first, ControllerFocusBounds second)
        {
            float x = first.CenterX - second.CenterX, y = first.CenterY - second.CenterY;
            return x * x + y * y;
        }

        private static float VerticalGap(ControllerFocusBounds first, ControllerFocusBounds second, int direction)
        { return direction > 0 ? second.MinY - first.MaxY : first.MinY - second.MaxY; }
        private static float IntervalDistance(float value, float min, float max)
        { return value < min ? min - value : value > max ? value - max : 0; }
        private static float ClampAim(float value, ControllerFocusBounds bounds)
        { return Math.Max(bounds.MinX, Math.Min(value, bounds.MaxX)); }
        private static float Overlap(float firstMin, float firstMax, float secondMin, float secondMax)
        { return Math.Min(firstMax, secondMax) - Math.Max(firstMin, secondMin); }

        internal string Confirm()
        {
            ControllerFocusItem item = Current;
            if (item == null) return "No controls in this view.";
            if (ReadingScrollKey != null) return "No visible control in this scroll position.";
            if (!item.Visible) return "The focused control is outside the visible area.";
            if (!item.Enabled) return "Unavailable: " + item.Caption;
            item.Activate();
            return null;
        }
    }

    internal sealed class ControllerNavigationRepeat
    {
        private ControllerWorkspaceCommand? _held;
        private float _next;
        internal bool Take(ControllerWorkspaceCommand? command, float time)
        {
            if (!command.HasValue) { _held = null; return false; }
            if (_held != command) { _held = command; _next = time + .38f; return true; }
            if (time < _next) return false;
            _next = time + .10f;
            return true;
        }
        internal void Reset() { _held = null; }
    }

    internal sealed class ControllerSearchKeyboard
    {
        internal static readonly string[] Keys = "abcdefghijklmnopqrstuvwxyz0123456789-'".Select(c => c.ToString())
            .Concat(new[] { "Space", "Delete", "Clear", "Apply", "Cancel" }).ToArray();
        internal const int Columns = 8;
        internal string Draft { get; private set; }
        internal int Focus { get; private set; }
        internal bool Finished { get; private set; }
        internal bool Accepted { get; private set; }
        internal ControllerSearchKeyboard(string original) { Draft = original ?? string.Empty; }
        internal void Move(int x, int y)
        {
            int row = Focus / Columns, col = Focus % Columns;
            int rows = (Keys.Length + Columns - 1) / Columns;
            row = (row + y + rows) % rows;
            int width = Math.Min(Columns, Keys.Length - row * Columns);
            col = x == 0 ? Math.Min(col, width - 1) : (col + x + width) % width;
            Focus = row * Columns + col;
        }
        internal void Confirm()
        {
            if (Finished) return;
            string key = Keys[Focus];
            if (key == "Cancel") { Cancel(); return; }
            if (key == "Apply") { Accepted = Finished = true; return; }
            if (key == "Clear") Draft = string.Empty;
            else if (key == "Delete") Draft = Draft.Length == 0 ? Draft : Draft.Substring(0, Draft.Length - 1);
            else if (Draft.Length < 128) Draft += key == "Space" ? " " : key;
        }
        internal void Cancel() { Finished = true; Accepted = false; }
    }
}
