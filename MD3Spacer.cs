using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Flexible spacer that fills remaining space in a Row or Column.
    /// Optionally specify a fixed size instead.
    /// </summary>
    public class MD3Spacer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Spacer, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        /// <summary>
        /// Flexible spacer (flexGrow = 1). Expands to fill remaining space.
        /// To create a fixed-size spacer, use <see cref="MD3Spacer(float)"/> with a positive value.
        /// Note: <c>new MD3Spacer(0)</c> creates a zero-width fixed spacer, not a flexible one.
        /// </summary>
        public MD3Spacer()
        {
            AddToClassList("md3-spacer");
            style.flexGrow = 1;
        }

        /// <summary>
        /// Fixed-size spacer with the given width and height.
        /// Use <see cref="MD3Spacer()"/> (no arguments) for a flexible spacer that fills remaining space.
        /// </summary>
        /// <param name="size">Fixed size in pixels for both width and height.</param>
        public MD3Spacer(float size)
        {
            AddToClassList("md3-spacer");
            style.flexGrow = 0;
            style.flexShrink = 0;
            style.width = size;
            style.height = size;
        }
    }
}
