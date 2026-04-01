using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Radio : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Radio, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly VisualElement _ring;
        readonly VisualElement _dot;
        readonly Label _label;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                UpdateVisual();
                changed?.Invoke(_selected);
            }
        }
        bool _selected;

        public MD3Radio() : this(false) { }

        public MD3Radio(bool selected, string label = null)
        {
            _selected = selected;
            AddToClassList("md3-radio");

            if (label != null)
            {
                style.width = StyleKeyword.Auto;
                style.height = StyleKeyword.Auto;
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
            }

            _ring = new VisualElement();
            _ring.AddToClassList("md3-radio__ring");
            _ring.pickingMode = PickingMode.Ignore;
            Add(_ring);

            _dot = new VisualElement();
            _dot.AddToClassList("md3-radio__dot");
            _dot.pickingMode = PickingMode.Ignore;
            _ring.Add(_dot);

            if (label != null)
            {
                _label = new Label(label);
                _label.AddToClassList("md3-radio__label");
                _label.pickingMode = PickingMode.Ignore;
                Add(_label);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(_ => { _pressed = true; ApplyColors(); });
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
            _theme = ResolveTheme();
            UpdateVisual();
        }

        void OnClick(ClickEvent evt)
        {
            if (_selected) return;
            _selected = true;
            UpdateVisual();
            changed?.Invoke(_selected);
        }

        void UpdateVisual()
        {
            if (_selected)
                AddToClassList("md3-radio--selected");
            else
                RemoveFromClassList("md3-radio--selected");
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color ringBorder, dotBg;

            if (_selected)
            {
                ringBorder = _theme.Primary;
                dotBg = _theme.Primary;
            }
            else
            {
                ringBorder = _theme.OnSurfaceVariant;
                dotBg = Color.clear;
            }

            if (_pressed)
            {
                var overlay = _selected ? _theme.Primary : _theme.OnSurface;
                ringBorder = _theme.PressOverlay(ringBorder, overlay);
            }
            else if (_hovered)
            {
                var overlay = _selected ? _theme.Primary : _theme.OnSurface;
                ringBorder = _theme.HoverOverlay(ringBorder, overlay);
            }

            _ring.style.borderTopColor = ringBorder;
            _ring.style.borderBottomColor = ringBorder;
            _ring.style.borderLeftColor = ringBorder;
            _ring.style.borderRightColor = ringBorder;
            _ring.style.backgroundColor = Color.clear;
            _dot.style.backgroundColor = dotBg;

            if (_label != null)
                _label.style.color = _theme.OnSurface;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
