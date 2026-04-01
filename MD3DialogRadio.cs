using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3DialogRadio : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3DialogRadio, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly MD3Radio _radio;
        readonly Label _label;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        public bool Selected
        {
            get => _radio.Selected;
            set
            {
                _radio.Selected = value;
                ApplyColors();
            }
        }

        public MD3DialogRadio() : this("Option") { }

        public MD3DialogRadio(string label, bool selected = false)
        {
            AddToClassList("md3-dialog-radio");

            _radio = new MD3Radio(selected);
            _radio.pickingMode = PickingMode.Ignore;
            _radio.changed += v =>
            {
                ApplyColors();
                changed?.Invoke(v);
            };
            Add(_radio);

            _label = new Label(label);
            _label.AddToClassList("md3-dialog-radio__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(e => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(e => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { _pressed = true; ApplyColors(); MD3Ripple.Spawn(this, e, _theme != null ? _theme.Primary : Color.white); });
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
            if (!_radio.Selected)
            {
                _radio.Selected = true;
                changed?.Invoke(true);
            }
            // ripple は MouseDown で発火済み
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
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
