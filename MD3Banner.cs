using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Banner : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Banner, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _icon;
        readonly Label _message;
        readonly VisualElement _actions;
        MD3Theme _theme;

        public VisualElement Actions => _actions;

        public MD3Banner() : this("Information message.") { }

        public MD3Banner(string message, string icon = null, string actionLabel = null, Action onAction = null)
        {
            AddToClassList("md3-banner");

            _icon = new Label(icon ?? MD3Icon.Warning);
            _icon.AddToClassList("md3-banner__icon");
            _icon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_icon);
            Add(_icon);

            var body = new VisualElement();
            body.AddToClassList("md3-banner__body");
            body.pickingMode = PickingMode.Ignore;

            _message = new Label(message);
            _message.AddToClassList("md3-banner__message");
            _message.pickingMode = PickingMode.Ignore;
            body.Add(_message);

            _actions = new VisualElement();
            _actions.AddToClassList("md3-banner__actions");

            if (actionLabel != null)
            {
                var btn = new MD3Button(actionLabel, MD3ButtonStyle.Text);
                btn.clicked += () => onAction?.Invoke();
                _actions.Add(btn);
            }
            body.Add(_actions);
            Add(body);

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

            style.backgroundColor = _theme.Surface;
            style.borderBottomColor = _theme.OutlineVariant;
            _icon.style.color = _theme.OnSurfaceVariant;
            _message.style.color = _theme.OnSurface;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
