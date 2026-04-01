using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Divider : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Divider, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        MD3Theme _theme;

        public MD3Divider() : this(0f) { }

        public MD3Divider(float inset)
        {
            AddToClassList("md3-divider");
            style.marginLeft = inset;

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.backgroundColor = _theme.OutlineVariant;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
