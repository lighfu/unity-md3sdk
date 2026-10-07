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

            MD3FlexGap.Register(this, gap, horizontal: true);
        }
    }
}
