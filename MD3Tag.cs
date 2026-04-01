using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Tag : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Tag, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _label;
        MD3Theme _theme;

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public MD3Tag() : this("Tag") { }

        public MD3Tag(string label)
        {
            AddToClassList("md3-tag");

            _label = new Label(label);
            _label.AddToClassList("md3-tag__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

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

            style.backgroundColor = _theme.SecondaryContainer;
            _label.style.color = _theme.OnSecondaryContainer;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
