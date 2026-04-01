using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Vertical scrollable column — the most common EditorWindow content pattern.
    /// Combines ScrollView + Column with optional gap and padding.
    /// </summary>
    public class MD3ScrollColumn : ScrollView
    {
        public new class UxmlFactory : UxmlFactory<MD3ScrollColumn, UxmlTraits> { }
        public new class UxmlTraits : ScrollView.UxmlTraits { }

        readonly float _gap;

        public MD3ScrollColumn() : this(0f) { }

        public MD3ScrollColumn(float gap = 0f, float padding = 0f)
            : base(ScrollViewMode.Vertical)
        {
            _gap = gap;
            AddToClassList("md3-scroll-column");
            style.flexGrow = 1;

            contentContainer.style.flexDirection = FlexDirection.Column;

            if (padding > 0)
            {
                contentContainer.style.paddingTop = padding;
                contentContainer.style.paddingBottom = padding;
                contentContainer.style.paddingLeft = padding;
                contentContainer.style.paddingRight = padding;
            }

            if (gap > 0)
            {
                contentContainer.RegisterCallback<GeometryChangedEvent>(_ => ApplyGap());
                contentContainer.RegisterCallback<AttachToPanelEvent>(_ => ApplyGap());
            }
        }

        void ApplyGap()
        {
            var container = contentContainer;
            float half = _gap * 0.5f;
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.ElementAt(i);
                if (child.style.display == DisplayStyle.None) continue;
                child.style.marginTop = i == 0 ? 0 : half;
                child.style.marginBottom = i == container.childCount - 1 ? 0 : half;
            }
        }
    }
}
