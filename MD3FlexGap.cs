using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    // Unity 2022 USS has no gap property. Keep margin-based spacing consistent
    // for rows, columns and ScrollView content containers.
    internal static class MD3FlexGap
    {
        internal static void Register(VisualElement container, float gap, bool horizontal = false)
        {
            if (!(gap > 0f)) return;
            container.RegisterCallback<GeometryChangedEvent>(_ => Apply(container, gap, horizontal));
            container.RegisterCallback<AttachToPanelEvent>(_ => Apply(container, gap, horizontal));
        }

        static void Apply(VisualElement container, float gap, bool horizontal)
        {
            float half = gap * 0.5f;
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.ElementAt(i);
                if (child.style.display == DisplayStyle.None) continue;
                float before = i == 0 ? 0f : half;
                float after = i == container.childCount - 1 ? 0f : half;
                if (horizontal)
                {
                    child.style.marginLeft = before;
                    child.style.marginRight = after;
                }
                else
                {
                    child.style.marginTop = before;
                    child.style.marginBottom = after;
                }
            }
        }
    }
}
