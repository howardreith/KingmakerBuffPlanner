using System;
using System.Collections.Generic;

namespace KingmakerBuffPlanner.UI
{
    internal static class ControllerOwnedItems
    {
        internal static void Ensure<T>(IList<T> items, T owned) where T : class
        {
            foreach (T item in items) if (ReferenceEquals(item, owned)) return;
            items.Add(owned);
        }
        internal static void Remove<T>(IList<T> items, T owned) where T : class
        {
            for (int i = items.Count - 1; i >= 0; i--) if (ReferenceEquals(items[i], owned)) items.RemoveAt(i);
        }
    }
}
