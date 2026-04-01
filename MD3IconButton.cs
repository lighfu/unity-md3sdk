using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3IconButtonStyle { Standard, Filled, Tonal, Outlined }
    public enum MD3IconButtonSize { Small, Medium, Large }

    public class MD3IconButton : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3IconButton, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;
        public event Action<bool> toggled;

        readonly Label _icon;
        readonly MD3IconButtonStyle _style;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;
        bool _isToggle;
        string _unselectedIcon;
        string _selectedIcon;

        /// <summary>アイコン色のオーバーライド。null でデフォルトに戻る。</summary>
        public Color? IconColorOverride
        {
            get => _iconColorOverride;
            set { _iconColorOverride = value; ApplyColors(); }
        }
        Color? _iconColorOverride;

        public bool IsDisabled
        {
            get => _disabled;
            set { _disabled = value; SetEnabled(!value); ApplyColors(); }
        }
        bool _disabled;

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                if (_isToggle)
                    _icon.text = _selected ? _selectedIcon : _unselectedIcon;
                ApplyColors();
                toggled?.Invoke(_selected);
            }
        }
        bool _selected;

        public string Icon
        {
            get => _icon.text;
            set
            {
                _icon.text = value;
                if (_isToggle)
                {
                    if (_selected) _selectedIcon = value;
                    else _unselectedIcon = value;
                }
            }
        }

        public MD3IconButton() : this(MD3Icon.Star, MD3IconButtonStyle.Standard) { }

        public MD3IconButton(string icon, MD3IconButtonStyle style = MD3IconButtonStyle.Standard,
            MD3IconButtonSize size = MD3IconButtonSize.Medium)
        {
            _style = style;
            AddToClassList("md3-icon-button");
            if (style == MD3IconButtonStyle.Outlined) AddToClassList("md3-icon-button--outlined");
            if (size == MD3IconButtonSize.Small) AddToClassList("md3-icon-button--small");
            if (size == MD3IconButtonSize.Large) AddToClassList("md3-icon-button--large");

            _icon = new Label(icon);
            _icon.AddToClassList("md3-icon-button__icon");
            _icon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_icon);
            Add(_icon);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { _pressed = true; ApplyColors(); if (!_disabled) MD3Ripple.Spawn(this, e, GetFgColor()); });
            RegisterCallback<MouseUpEvent>(_ => { _pressed = false; ApplyColors(); });
            RegisterCallback<ClickEvent>(OnClick);
        }

        /// <summary>
        /// Enables toggle mode with separate icons for unselected/selected states.
        /// </summary>
        public MD3IconButton MakeToggle(string unselectedIcon, string selectedIcon, bool selected = false)
        {
            _isToggle = true;
            _unselectedIcon = unselectedIcon;
            _selectedIcon = selectedIcon;
            _selected = selected;
            _icon.text = _selected ? _selectedIcon : _unselectedIcon;
            ApplyColors();
            return this;
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void OnClick(ClickEvent evt)
        {
            if (_disabled) return;

            if (_isToggle)
                Selected = !_selected;

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
            _icon.style.color = _iconColorOverride ?? fg;

            if (_style == MD3IconButtonStyle.Outlined)
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

            if (_isToggle && _selected)
            {
                // Selected toggle uses inverse/filled colors per MD3 spec
                switch (_style)
                {
                    case MD3IconButtonStyle.Standard:
                        bg = Color.clear; fg = t.Primary; break;
                    case MD3IconButtonStyle.Filled:
                        bg = t.Primary; fg = t.OnPrimary; break;
                    case MD3IconButtonStyle.Tonal:
                        bg = t.SecondaryContainer; fg = t.OnSecondaryContainer; break;
                    case MD3IconButtonStyle.Outlined:
                        bg = t.InverseSurface; fg = t.InverseOnSurface; border = Color.clear; break;
                    default:
                        bg = Color.clear; fg = t.Primary; break;
                }
            }
            else if (_isToggle && !_selected)
            {
                // Unselected toggle
                switch (_style)
                {
                    case MD3IconButtonStyle.Standard:
                        bg = Color.clear; fg = t.OnSurfaceVariant; break;
                    case MD3IconButtonStyle.Filled:
                        bg = t.SurfaceContainerHighest; fg = t.Primary; break;
                    case MD3IconButtonStyle.Tonal:
                        bg = t.SurfaceContainerHighest; fg = t.OnSurfaceVariant; break;
                    case MD3IconButtonStyle.Outlined:
                        bg = Color.clear; fg = t.OnSurfaceVariant; break;
                    default:
                        bg = Color.clear; fg = t.OnSurfaceVariant; break;
                }
            }
            else
            {
                // Non-toggle (original behavior)
                switch (_style)
                {
                    case MD3IconButtonStyle.Standard:
                        bg = Color.clear; fg = t.OnSurfaceVariant; break;
                    case MD3IconButtonStyle.Filled:
                        bg = t.Primary; fg = t.OnPrimary; break;
                    case MD3IconButtonStyle.Tonal:
                        bg = t.SecondaryContainer; fg = t.OnSecondaryContainer; break;
                    case MD3IconButtonStyle.Outlined:
                        bg = Color.clear; fg = t.OnSurfaceVariant; break;
                    default:
                        bg = Color.clear; fg = t.OnSurfaceVariant; break;
                }
            }
        }

        Color GetFgColor()
        {
            if (_theme == null) return Color.white;
            if (_isToggle && _selected)
            {
                switch (_style)
                {
                    case MD3IconButtonStyle.Filled: return _theme.OnPrimary;
                    case MD3IconButtonStyle.Tonal: return _theme.OnSecondaryContainer;
                    case MD3IconButtonStyle.Outlined: return _theme.InverseOnSurface;
                    default: return _theme.Primary;
                }
            }
            switch (_style)
            {
                case MD3IconButtonStyle.Filled: return _theme.OnPrimary;
                case MD3IconButtonStyle.Tonal: return _theme.OnSecondaryContainer;
                default: return _theme.OnSurfaceVariant;
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
