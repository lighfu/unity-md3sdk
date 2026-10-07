using System;
using System.Collections.Generic;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>Synchronizes item states without assigning unchanged selections.</summary>
    internal static class MD3Selection
    {
        internal static bool Update<T>(IReadOnlyList<T> items, int selectedIndex,
            Func<T, bool> isSelected, Action<T, bool> setSelected, Func<bool> isCurrent)
        {
            for (int i = 0; i < items.Count; i++)
            {
                // A child notification can select another index while this pass runs.
                // The nested pass owns the final state once this request is superseded.
                if (!isCurrent()) return false;
                var item = items[i];
                bool selected = i == selectedIndex;
                if (isSelected(item) != selected)
                    setSelected(item, selected);
            }
            return isCurrent();
        }
    }
}
