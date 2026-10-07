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

        public MD3ScrollColumn() : this(0f) { }

        public MD3ScrollColumn(float gap = 0f, float padding = 0f)
            : base(ScrollViewMode.Vertical)
        {
            AddToClassList("md3-scroll-column");
            style.flexGrow = 1;

            contentContainer.style.flexDirection = FlexDirection.Column;

            if (padding > 0)
            {
                contentContainer.Padding(padding);
            }

            MD3FlexGap.Register(contentContainer, gap);
        }
    }
}
