using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3CardStyle { Elevated, Filled, Outlined }

    public class MD3Card : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Card, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;

        readonly Label _titleLabel;
        readonly Label _bodyLabel;
        readonly MD3CardStyle _style;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        public string Title
        {
            get => _titleLabel.text;
            set
            {
                _titleLabel.text = value;
                _titleLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public string Body
        {
            get => _bodyLabel.text;
            set
            {
                _bodyLabel.text = value;
                _bodyLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        bool IsClickable => clicked != null;

        public MD3Card() : this("Card", "", MD3CardStyle.Elevated) { }

        public MD3Card(string title, string body = "", MD3CardStyle style = MD3CardStyle.Elevated)
        {
            _style = style;
            AddToClassList("md3-card");

            switch (style)
            {
                case MD3CardStyle.Elevated: AddToClassList("md3-card--elevated"); break;
                case MD3CardStyle.Filled: AddToClassList("md3-card--filled"); break;
                case MD3CardStyle.Outlined: AddToClassList("md3-card--outlined"); break;
            }

            _titleLabel = new Label(title);
            _titleLabel.AddToClassList("md3-card__title");
            if (string.IsNullOrEmpty(title))
                _titleLabel.style.display = DisplayStyle.None;
            Add(_titleLabel);

            _bodyLabel = new Label(body);
            _bodyLabel.AddToClassList("md3-card__body");
            if (string.IsNullOrEmpty(body))
                _bodyLabel.style.display = DisplayStyle.None;
            Add(_bodyLabel);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
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

        void OnMouseEnter(MouseEnterEvent evt)
        {
            if (!IsClickable) return;
            _hovered = true;
            AddToClassList("md3-card--clickable");
            ApplyColors();
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (!IsClickable) return;
            _hovered = false;
            _pressed = false;
            RemoveFromClassList("md3-card--clickable");
            ApplyColors();
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (!IsClickable) return;
            _pressed = true;
            ApplyColors();
            MD3Ripple.Spawn(this, evt, _theme?.OnSurface ?? Color.white);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!IsClickable) return;
            _pressed = false;
            ApplyColors();
        }

        void OnClick(ClickEvent evt)
        {
            if (!IsClickable) return;
            clicked?.Invoke();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color bg, fg, border;
            switch (_style)
            {
                case MD3CardStyle.Elevated:
                    bg = _theme.SurfaceContainerHigh;
                    fg = _theme.OnSurface;
                    border = Color.clear;
                    // Simulate elevation with subtle bottom/right border
                    style.borderBottomColor = new Color(0, 0, 0, 0.15f);
                    style.borderRightColor = new Color(0, 0, 0, 0.1f);
                    style.borderBottomWidth = 1;
                    style.borderRightWidth = 1;
                    break;
                case MD3CardStyle.Filled:
                    bg = _theme.SurfaceVariant;
                    fg = _theme.OnSurface;
                    border = Color.clear;
                    break;
                case MD3CardStyle.Outlined:
                default:
                    bg = _theme.Surface;
                    fg = _theme.OnSurface;
                    border = _theme.OutlineVariant;
                    break;
            }

            // Apply hover/press overlays for clickable cards
            if (IsClickable)
            {
                if (_pressed)
                    bg = _theme.PressOverlay(bg, fg);
                else if (_hovered)
                    bg = _theme.HoverOverlay(bg, fg);
            }

            style.backgroundColor = bg;
            _titleLabel.style.color = fg;
            _bodyLabel.style.color = fg;

            if (_style == MD3CardStyle.Outlined)
            {
                style.borderTopColor = border;
                style.borderBottomColor = border;
                style.borderLeftColor = border;
                style.borderRightColor = border;
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
