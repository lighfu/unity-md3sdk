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

            MD3FlexGap.Register(this, gap);
        }
    }
}
