using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    internal enum ControllerWorkspaceCommand { Up, Down, Left, Right, PreviousRoutine, NextRoutine, Confirm, Inspect, ScrollUp, ScrollDown }

    internal sealed class ControllerFocusItem
    {
        internal readonly string Key, Region, Caption;
        internal readonly bool Enabled;
        internal readonly Action Activate, Reveal, Inspect;
        internal ControllerFocusItem(string key, string region, string caption, bool enabled,
            Action activate, Action reveal = null, Action inspect = null)
        { Key = key; Region = region; Caption = caption; Enabled = enabled; Activate = activate; Reveal = reveal; Inspect = inspect; }
    }

    // A presentation-only focus cursor. Rebuild replaces live actions; only
    // semantic keys and the prior region/ordinal survive, never Unity objects.
    internal sealed class ControllerWorkspaceFocus
    {
        private List<ControllerFocusItem> _items = new List<ControllerFocusItem>();
        private readonly Dictionary<string, string> _remembered = new Dictionary<string, string>();
        internal string Key { get; private set; }
        internal ControllerFocusItem Current { get { return _items.FirstOrDefault(i => i.Key == Key); } }
        internal IReadOnlyList<ControllerFocusItem> Items { get { return _items; } }

        internal void Replace(IEnumerable<ControllerFocusItem> items)
        {
            ControllerFocusItem prior = Current;
            int ordinal = prior == null ? 0 : _items.Where(i => i.Region == prior.Region).ToList().IndexOf(prior);
            _items = items.ToList();
            if (_items.Select(i => i.Key).Distinct(StringComparer.Ordinal).Count() != _items.Count)
                throw new InvalidOperationException("Duplicate semantic controller focus key.");
            if (_items.Any(i => i.Key == Key)) return;
            var neighbors = prior == null ? _items : _items.Where(i => i.Region == prior.Region).ToList();
            ControllerFocusItem fallback = neighbors.Count == 0 ? _items.FirstOrDefault()
                : neighbors[Math.Min(Math.Max(0, ordinal), neighbors.Count - 1)];
            Key = fallback == null ? null : fallback.Key;
        }

        internal void Restore(string key)
        { if (_items.Any(i => i.Key == key)) Key = key; }

        internal void Move(int direction, bool region)
        {
            ControllerFocusItem current = Current;
            if (current == null) return;
            _remembered[current.Region] = Key;
            if (region)
            {
                string[] regions = _items.Select(i => i.Region).Distinct().ToArray();
                string next = regions[(Array.IndexOf(regions, current.Region) + (direction < 0 ? regions.Length - 1 : 1)) % regions.Length];
                string remembered;
                Key = _remembered.TryGetValue(next, out remembered) && _items.Any(i => i.Key == remembered)
                    ? remembered : _items.First(i => i.Region == next).Key;
            }
            else
            {
                var regionItems = _items.Where(i => i.Region == current.Region).ToList();
                Key = regionItems[(regionItems.IndexOf(current) + (direction < 0 ? regionItems.Count - 1 : 1)) % regionItems.Count].Key;
            }
        }

        internal string Confirm()
        {
            ControllerFocusItem item = Current;
            if (item == null) return "No controls in this view.";
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
