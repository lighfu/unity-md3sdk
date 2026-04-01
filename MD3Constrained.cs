using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Container with a max-width constraint, centered horizontally.
    /// Useful for keeping content readable in wide windows.
    /// </summary>
    public class MD3Constrained : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Constrained, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public MD3Constrained() : this(640f) { }

        public MD3Constrained(float maxWidth)
        {
            AddToClassList("md3-constrained");
            style.maxWidth = maxWidth;
            style.flexGrow = 1;
            style.alignSelf = Align.Center;
            style.width = new Length(100, LengthUnit.Percent);
        }
    }
}
