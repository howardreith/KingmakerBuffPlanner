using System;
using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    // Bounded donor lookup shared by the Unity adapter and deterministic
    // hierarchy fixtures. Every step is guarded so a pathological native
    // hierarchy cannot stall the planner: depth, child count, and ambiguity
    // limits reject loudly instead of scanning wide or deep.
    internal sealed class NativeThemeDonorLookup
    {
        internal const int MaximumChildren = 512;
        internal const int MaximumDepth = 32;
        internal const int MaximumScanNodes = 2048;

        private readonly INativeThemeSource _source;

        internal NativeThemeDonorLookup(INativeThemeSource source)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;
        }

        internal object RequirePath(object owner, string path)
        {
            if (string.IsNullOrEmpty(path)) throw Failure(owner, "path", path, "empty path");
            string[] parts = path.Split('/');
            if (parts.Length > MaximumDepth)
                throw Failure(owner, "path", path, "path depth exceeds limit");
            object current = owner;
            foreach (string part in parts)
            {
                if (part.Length == 0)
                    throw Failure(current, "path", path, "empty path segment");
                current = FindDirect(current, part, "path", path);
            }
            return current;
        }

        internal object RequireLiteralChild(object owner, string name)
        {
            if (string.IsNullOrEmpty(name)) throw Failure(owner, "literal child", name, "empty name");
            return FindDirect(owner, name, "literal child", name);
        }

        internal object RequireComponent(object node, NativeThemeComponent component,
            string kind, string locator)
        {
            if (!_source.IsAlive(node))
                throw Failure(node, kind, locator, "owner missing/destroyed");
            object[] values = _source.Components(node, component);
            if (values == null || values.Length == 0)
                throw Failure(node, kind, locator, "expected component missing: " + component);
            if (values.Length != 1)
                throw Failure(node, kind, locator,
                    "ambiguous expected components: " + component + " (" + values.Length + ")");
            if (!_source.IsAlive(values[0]))
                throw Failure(node, kind, locator, "expected component destroyed");
            return values[0];
        }

        // Breadth-first bounded scan for the first node under root carrying the
        // requested component. Used only where no verified literal path exists
        // yet; the visited-node bound keeps worst cases linear and small.
        internal object ScanForComponent(object root, NativeThemeComponent component,
            string locator)
        {
            if (!_source.IsAlive(root))
                throw Failure(root, "scan", locator, "scan root missing/destroyed");
            var queue = new Queue<object>();
            queue.Enqueue(root);
            int visited = 0;
            while (queue.Count > 0)
            {
                object node = queue.Dequeue();
                if (visited++ > MaximumScanNodes)
                    throw Failure(root, "scan", locator,
                        "scan exceeded " + MaximumScanNodes + " nodes");
                if (!_source.IsAlive(node)) continue;
                object[] values = _source.Components(node, component);
                if (values != null && values.Length > 0 && _source.IsAlive(values[0]))
                    return node;
                int count = _source.ChildCount(node);
                if (count > MaximumChildren)
                    throw Failure(node, "scan", locator,
                        "direct-child count exceeds " + MaximumChildren);
                for (int index = 0; index < count; index++)
                {
                    object child = _source.Child(node, index);
                    if (_source.IsAlive(child)) queue.Enqueue(child);
                }
            }
            throw Failure(root, "scan", locator, "no node carries " + component);
        }

        internal bool IsUnderOwner(object node, object owner)
        {
            if (!_source.IsAlive(owner)) return false;
            for (int depth = 0; depth < MaximumDepth && _source.IsAlive(node);
                depth++, node = _source.Parent(node))
                if (_source.SameNode(node, owner)) return true;
            return false;
        }

        internal string Location(object node)
        {
            var names = new List<string>();
            int depth = 0;
            while (_source.IsAlive(node) && depth++ < 12)
            {
                names.Add(Quote(_source.Name(node)));
                node = _source.Parent(node);
            }
            if (_source.IsAlive(node)) names.Add("...");
            names.Reverse();
            return names.Count == 0 ? "<missing/destroyed>" : string.Join(" > ", names.ToArray());
        }

        private object FindDirect(object owner, string name, string kind, string locator)
        {
            if (!_source.IsAlive(owner))
                throw Failure(owner, kind, locator, "owner missing/destroyed");
            int count = _source.ChildCount(owner);
            if (count > MaximumChildren)
                throw Failure(owner, kind, locator,
                    "direct-child count exceeds " + MaximumChildren);
            object match = null;
            int matches = 0;
            for (int index = 0; index < count; index++)
            {
                object child = _source.Child(owner, index);
                // Deliberately no activeSelf/activeInHierarchy filter: inactive
                // native screens still carry valid donor artwork.
                if (_source.IsAlive(child) &&
                    string.Equals(_source.Name(child), name, StringComparison.Ordinal))
                {
                    match = child;
                    matches++;
                }
            }
            if (matches == 0)
                throw Failure(owner, kind, locator, "child not found: " + Quote(name));
            if (matches != 1)
                throw Failure(owner, kind, locator,
                    "ambiguous direct children: " + Quote(name) + " (" + matches + ")");
            return match;
        }

        private InvalidOperationException Failure(object owner, string kind,
            string locator, string reason)
        {
            var children = new List<string>();
            if (_source.IsAlive(owner))
            {
                int count = _source.ChildCount(owner);
                for (int index = 0; index < Math.Min(count, 6); index++)
                {
                    object child = _source.Child(owner, index);
                    children.Add(_source.IsAlive(child)
                        ? Quote(_source.Name(child)) : "<destroyed>");
                }
                if (count > 6) children.Add("... (" + count + " children)");
            }
            return new InvalidOperationException(reason + "; locator=" + kind + " " +
                Quote(locator) + "; owner=" + Location(owner) + "; direct children=[" +
                string.Join(", ", children.ToArray()) + "]");
        }

        private static string Quote(string value)
        {
            value = (value ?? "<null>").Replace("\r", " ").Replace("\n", " ").Replace("'", "''");
            return "'" + (value.Length <= 160 ? value : value.Substring(0, 157) + "...") + "'";
        }
    }
}
