using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Vertical flex layout container with optional gap.
    /// </summary>
    public class MD3Column : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Column, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public MD3Column() : this(0f) { }

        public MD3Column(float gap = 0f, Align alignItems = Align.Stretch, Justify justifyContent = Justify.FlexStart)
        {
            AddToClassList("md3-column");
            style.flexDirection = FlexDirection.Column;
            style.alignItems = alignItems;
            style.justifyContent = justifyContent;

            if (gap > 0)
            {
                RegisterCallback<GeometryChangedEvent>(_ => ApplyGap(gap));
                RegisterCallback<AttachToPanelEvent>(_ => ApplyGap(gap));
            }
        }

        void ApplyGap(float gap)
        {
            float half = gap * 0.5f;
            for (int i = 0; i < childCount; i++)
            {
                var child = ElementAt(i);
                if (child.style.display == DisplayStyle.None) continue;
                child.style.marginTop = i == 0 ? 0 : half;
                child.style.marginBottom = i == childCount - 1 ? 0 : half;
            }
        }
    }
}
