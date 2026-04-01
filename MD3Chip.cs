using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Chip : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Chip, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> toggled;
        public event Action clicked;
        public event Action closed;

        readonly Label _label;
        readonly Label _closeBtn;
        readonly bool _closeable;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        public bool Selected
        {
            get => _selected;
            set { _selected = value; ApplyColors(); }
        }
        bool _selected;

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public MD3Chip() : this("Chip") { }

        public MD3Chip(string text, bool selected = false, bool closeable = false)
        {
            _selected = selected;
            _closeable = closeable;
            AddToClassList("md3-chip");

            _label = new Label(text);
            _label.AddToClassList("md3-chip__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            if (closeable)
            {
                _closeBtn = new Label(MD3Icon.Close);
                _closeBtn.AddToClassList("md3-chip__close");
                MD3Icon.Apply(_closeBtn, 14f);
                _closeBtn.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    closed?.Invoke();
                });
                Add(_closeBtn);
            }
            else
            {
                _closeBtn = null;
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { _pressed = true; ApplyColors(); MD3Ripple.Spawn(this, e, GetFgColor()); });
            RegisterCallback<MouseUpEvent>(_ => { _pressed = false; ApplyColors(); });
            RegisterCallback<ClickEvent>(OnClick);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
        }

        void OnClick(ClickEvent evt)
        {
            if (toggled != null)
            {
                _selected = !_selected;
                ApplyColors();
                toggled.Invoke(_selected);
            }
            clicked?.Invoke();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color bg, fg, border;
            if (_selected)
            {
                bg = _theme.SecondaryContainer;
                fg = _theme.OnSecondaryContainer;
                border = Color.clear;
            }
            else
            {
                bg = Color.clear;
                fg = _theme.OnSurfaceVariant;
                border = _theme.Outline;
            }

            if (_pressed) bg = _theme.PressOverlay(bg == Color.clear ? _theme.Surface : bg, fg);
            else if (_hovered) bg = _theme.HoverOverlay(bg == Color.clear ? _theme.Surface : bg, fg);

            style.backgroundColor = bg;
            style.borderTopColor = border;
            style.borderBottomColor = border;
            style.borderLeftColor = border;
            style.borderRightColor = border;
            _label.style.color = fg;

            if (_closeBtn != null)
                _closeBtn.style.color = fg;
        }

        Color GetFgColor()
        {
            if (_theme == null) return Color.white;
            return _selected ? _theme.OnSecondaryContainer : _theme.OnSurfaceVariant;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
