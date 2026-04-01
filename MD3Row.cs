using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Horizontal flex layout container with optional gap.
    /// </summary>
    public class MD3Row : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Row, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public MD3Row() : this(0f) { }

        public MD3Row(float gap = 0f, Align alignItems = Align.Center, Justify justifyContent = Justify.FlexStart, bool wrap = false)
        {
            AddToClassList("md3-row");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = alignItems;
            style.justifyContent = justifyContent;

            if (wrap)
                style.flexWrap = Wrap.Wrap;

            if (gap > 0)
            {
                // USS doesn't support 'gap' in Unity 2022 — use margin on children via RegisterCallback
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
                child.style.marginLeft = i == 0 ? 0 : half;
                child.style.marginRight = i == childCount - 1 ? 0 : half;
            }
        }
    }
}
