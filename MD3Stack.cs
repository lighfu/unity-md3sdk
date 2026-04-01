using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Stacks children on top of each other (ZStack).
    /// All children are positioned absolutely within the container.
    /// Useful for overlays, badges on images, loading states, etc.
    /// </summary>
    public class MD3Stack : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Stack, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public MD3Stack()
        {
            AddToClassList("md3-stack");
        }

        public override VisualElement contentContainer => this;

        public new void Add(VisualElement child)
        {
            // First child defines the size (position: relative), rest are overlaid
            if (childCount > 0)
            {
                child.style.position = Position.Absolute;
                child.style.top = 0;
                child.style.left = 0;
                child.style.right = 0;
                child.style.bottom = 0;
            }
            base.Add(child);
        }

        /// <summary>
        /// Adds an overlay child positioned at a specific corner or edge.
        /// </summary>
        public void AddOverlay(VisualElement child, MD3StackAlignment alignment = MD3StackAlignment.Fill)
        {
            child.style.position = Position.Absolute;

            switch (alignment)
            {
                case MD3StackAlignment.Fill:
                    child.style.top = 0; child.style.left = 0;
                    child.style.right = 0; child.style.bottom = 0;
                    break;
                case MD3StackAlignment.TopLeft:
                    child.style.top = 0; child.style.left = 0;
                    break;
                case MD3StackAlignment.TopRight:
                    child.style.top = 0; child.style.right = 0;
                    break;
                case MD3StackAlignment.BottomLeft:
                    child.style.bottom = 0; child.style.left = 0;
                    break;
                case MD3StackAlignment.BottomRight:
                    child.style.bottom = 0; child.style.right = 0;
                    break;
                case MD3StackAlignment.Center:
                    child.style.top = 0; child.style.left = 0;
                    child.style.right = 0; child.style.bottom = 0;
                    child.style.alignSelf = Align.Center;
                    child.style.justifyContent = Justify.Center;
                    child.style.alignItems = Align.Center;
                    break;
            }
            base.Add(child);
        }
    }

    public enum MD3StackAlignment
    {
        Fill,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center,
    }
}
