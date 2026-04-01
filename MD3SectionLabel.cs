using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3SectionLabel : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3SectionLabel, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _label;
        MD3Theme _theme;

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public MD3SectionLabel() : this("Section") { }

        public MD3SectionLabel(string text)
        {
            AddToClassList("md3-section-label");

            _label = new Label(text);
            _label.AddToClassList("md3-section-label__text");
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

            _label.style.color = _theme.Primary;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
