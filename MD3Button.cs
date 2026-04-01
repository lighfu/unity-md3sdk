using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3ButtonStyle { Filled, Tonal, Outlined, Text }
    public enum MD3ButtonSize { Small, Medium, Large }

    public class MD3Button : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Button, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;

        readonly Label _label;
        readonly MD3ButtonStyle _style;
        readonly VisualElement _contentWrap;
        Label _icon;
        MD3Spinner _spinner;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;
        bool _isLoading;

        public bool IsDisabled
        {
            get => _disabled;
            set { _disabled = value; SetEnabled(!value); ApplyColors(); }
        }
        bool _disabled;

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public string Icon
        {
            get => _icon?.text;
            set
            {
                if (value == null)
                {
                    if (_icon != null) _icon.style.display = DisplayStyle.None;
                }
                else
                {
                    EnsureIcon();
                    _icon.text = value;
                    _icon.style.display = DisplayStyle.Flex;
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                if (value)
                {
                    _contentWrap.style.display = DisplayStyle.None;
                    EnsureSpinner();
                    _spinner.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _contentWrap.style.display = DisplayStyle.Flex;
                    if (_spinner != null) _spinner.style.display = DisplayStyle.None;
                }
            }
        }

        public MD3Button() : this("Button", MD3ButtonStyle.Filled) { }

        public MD3Button(string text, MD3ButtonStyle style = MD3ButtonStyle.Filled, string icon = null,
            MD3ButtonSize size = MD3ButtonSize.Medium)
        {
            _style = style;
            AddToClassList("md3-button");
            if (style == MD3ButtonStyle.Outlined) AddToClassList("md3-button--outlined");
            if (style == MD3ButtonStyle.Text) AddToClassList("md3-button--text");
            if (size == MD3ButtonSize.Small) AddToClassList("md3-button--small");
            if (size == MD3ButtonSize.Large) AddToClassList("md3-button--large");

            // Content wrapper
            _contentWrap = new VisualElement();
            _contentWrap.AddToClassList("md3-button__content");
            _contentWrap.pickingMode = PickingMode.Ignore;
            Add(_contentWrap);

            // Icon (optional)
            if (icon != null)
            {
                EnsureIcon();
                _icon.text = icon;
            }

            _label = new Label(text);
            _label.AddToClassList("md3-button__label");
            _label.pickingMode = PickingMode.Ignore;
            _contentWrap.Add(_label);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<ClickEvent>(OnClick);
        }

        void EnsureIcon()
        {
            if (_icon != null) return;
            _icon = new Label();
            _icon.AddToClassList("md3-button__icon");
            _icon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_icon, 18f);
            _contentWrap.Insert(0, _icon);
        }

        void EnsureSpinner()
        {
            if (_spinner != null) return;
            _spinner = new MD3Spinner(20f);
            _spinner.style.display = DisplayStyle.None;
            Add(_spinner);
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

        void OnMouseEnter(MouseEnterEvent evt) { _hovered = true; ApplyColors(); }
        void OnMouseLeave(MouseLeaveEvent evt) { _hovered = false; _pressed = false; ApplyColors(); }
        void OnMouseDown(MouseDownEvent evt)
        {
            evt.StopPropagation();
            _pressed = true; ApplyColors();
            if (!_disabled && !_isLoading)
                MD3Ripple.Spawn(this, evt, GetFgColor());
        }
        void OnMouseUp(MouseUpEvent evt) { _pressed = false; ApplyColors(); }

        void OnClick(ClickEvent evt)
        {
            evt.StopPropagation();
            if (_disabled || _isLoading) return;
            clicked?.Invoke();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color bg, fg, border;
            GetBaseColors(out bg, out fg, out border);

            if (_disabled)
            {
                bg = _theme.Disabled(bg);
                fg = _theme.Disabled(fg);
                border = _theme.Disabled(border);
            }
            else if (_pressed)
            {
                bg = _theme.PressOverlay(bg, fg);
            }
            else if (_hovered)
            {
                bg = _theme.HoverOverlay(bg, fg);
            }

            style.backgroundColor = bg;
            _label.style.color = fg;
            if (_icon != null) _icon.style.color = fg;

            if (_style == MD3ButtonStyle.Outlined)
            {
                style.borderTopColor = border;
                style.borderBottomColor = border;
                style.borderLeftColor = border;
                style.borderRightColor = border;
            }
        }

        void GetBaseColors(out Color bg, out Color fg, out Color border)
        {
            var t = _theme;
            border = t.Outline;
            switch (_style)
            {
                case MD3ButtonStyle.Filled:
                    bg = t.Primary; fg = t.OnPrimary; break;
                case MD3ButtonStyle.Tonal:
                    bg = t.SecondaryContainer; fg = t.OnSecondaryContainer; break;
                case MD3ButtonStyle.Outlined:
                    bg = Color.clear; fg = t.Primary; break;
                case MD3ButtonStyle.Text:
                    bg = Color.clear; fg = t.Primary; border = Color.clear; break;
                default:
                    bg = t.Primary; fg = t.OnPrimary; break;
            }
        }

        Color GetFgColor()
        {
            if (_theme == null) return Color.white;
            switch (_style)
            {
                case MD3ButtonStyle.Filled: return _theme.OnPrimary;
                case MD3ButtonStyle.Tonal: return _theme.OnSecondaryContainer;
                default: return _theme.Primary;
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
