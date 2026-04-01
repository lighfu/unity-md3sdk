using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3EmptyState : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3EmptyState, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _icon;
        readonly Label _title;
        readonly Label _description;
        MD3Theme _theme;

        public MD3EmptyState() : this("Empty", "Nothing here yet") { }

        public MD3EmptyState(string title, string description = null, string icon = null)
        {
            AddToClassList("md3-empty-state");

            if (icon != null)
            {
                _icon = new Label(icon);
                _icon.AddToClassList("md3-empty-state__icon");
                _icon.pickingMode = PickingMode.Ignore;
                MD3Icon.Apply(_icon, 48f);
                Add(_icon);
            }

            _title = new Label(title);
            _title.AddToClassList("md3-empty-state__title");
            _title.pickingMode = PickingMode.Ignore;
            Add(_title);

            if (description != null)
            {
                _description = new Label(description);
                _description.AddToClassList("md3-empty-state__description");
                _description.pickingMode = PickingMode.Ignore;
                Add(_description);
            }

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

            if (_icon != null) _icon.style.color = _theme.OnSurfaceVariant;
            _title.style.color = _theme.OnSurface;
            if (_description != null) _description.style.color = _theme.OnSurfaceVariant;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
