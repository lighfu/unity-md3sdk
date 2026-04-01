using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Centers all children both horizontally and vertically.
    /// </summary>
    public class MD3Center : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Center, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public MD3Center()
        {
            AddToClassList("md3-center");
            style.flexGrow = 1;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
        }
    }
}
