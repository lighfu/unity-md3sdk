using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Avatar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Avatar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _text;
        MD3Theme _theme;

        public MD3Avatar() : this("A") { }

        public MD3Avatar(string initials, float size = 40f)
        {
            AddToClassList("md3-avatar");
            style.width = size;
            style.height = size;

            _text = new Label(initials);
            _text.AddToClassList("md3-avatar__text");
            _text.pickingMode = PickingMode.Ignore;
            _text.style.fontSize = size * 0.4f;
            Add(_text);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.backgroundColor = _theme.PrimaryContainer;
            _text.style.color = _theme.OnPrimaryContainer;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
