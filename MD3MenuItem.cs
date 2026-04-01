using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3MenuItem : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3MenuItem, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;

        readonly Label _label;
        readonly Label _shortcut;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public MD3MenuItem() : this("Menu Item") { }

        public MD3MenuItem(string label, string shortcut = null)
        {
            AddToClassList("md3-menu-item");

            _label = new Label(label);
            _label.AddToClassList("md3-menu-item__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            if (shortcut != null)
            {
                _shortcut = new Label(shortcut);
                _shortcut.AddToClassList("md3-menu-item__shortcut");
                _shortcut.pickingMode = PickingMode.Ignore;
                Add(_shortcut);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(e => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(e => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { _pressed = true; ApplyColors(); MD3Ripple.Spawn(this, e, _theme != null ? _theme.OnSurface : Color.white); });
            RegisterCallback<MouseUpEvent>(e => { _pressed = false; ApplyColors(); });
            RegisterCallback<ClickEvent>(OnClick);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void OnClick(ClickEvent evt)
        {
            clicked?.Invoke();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            var bg = Color.clear;
            if (_pressed)
                bg = _theme.PressOverlay(bg, _theme.OnSurface);
            else if (_hovered)
                bg = _theme.HoverOverlay(bg, _theme.OnSurface);

            style.backgroundColor = bg;
            _label.style.color = _theme.OnSurface;
            if (_shortcut != null) _shortcut.style.color = _theme.OnSurfaceVariant;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
