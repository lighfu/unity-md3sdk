using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3SplitButton : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3SplitButton, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;
        public event Action dropdownClicked;

        readonly VisualElement _main;
        readonly Label _mainLabel;
        readonly VisualElement _divider;
        readonly VisualElement _dropdown;
        readonly Label _dropdownIcon;
        MD3Theme _theme;
        bool _mainHovered;
        bool _mainPressed;
        bool _dropHovered;
        bool _dropPressed;

        public string Text
        {
            get => _mainLabel.text;
            set => _mainLabel.text = value;
        }

        public MD3SplitButton() : this("Action") { }

        public MD3SplitButton(string label, Action onClick = null, Action onDropdown = null)
        {
            if (onClick != null) clicked += onClick;
            if (onDropdown != null) dropdownClicked += onDropdown;

            AddToClassList("md3-split-button");

            // Main part
            _main = new VisualElement();
            _main.AddToClassList("md3-split-button__main");

            _mainLabel = new Label(label);
            _mainLabel.AddToClassList("md3-split-button__main-label");
            _mainLabel.pickingMode = PickingMode.Ignore;
            _main.Add(_mainLabel);
            Add(_main);

            // Divider
            _divider = new VisualElement();
            _divider.AddToClassList("md3-split-button__divider");
            _divider.pickingMode = PickingMode.Ignore;
            Add(_divider);

            // Dropdown part
            _dropdown = new VisualElement();
            _dropdown.AddToClassList("md3-split-button__dropdown");

            _dropdownIcon = new Label(MD3Icon.ArrowDropDown);
            _dropdownIcon.AddToClassList("md3-split-button__dropdown-icon");
            _dropdownIcon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_dropdownIcon, 14f);
            _dropdown.Add(_dropdownIcon);
            Add(_dropdown);

            // Main events
            _main.RegisterCallback<MouseEnterEvent>(e => { _mainHovered = true; ApplyColors(); });
            _main.RegisterCallback<MouseLeaveEvent>(e => { _mainHovered = false; _mainPressed = false; ApplyColors(); });
            _main.RegisterCallback<MouseDownEvent>(e => { _mainPressed = true; ApplyColors(); MD3Ripple.Spawn(_main, e, _theme != null ? _theme.OnPrimary : Color.white); });
            _main.RegisterCallback<MouseUpEvent>(e => { _mainPressed = false; ApplyColors(); });
            _main.RegisterCallback<ClickEvent>(e => clicked?.Invoke());

            // Dropdown events
            _dropdown.RegisterCallback<MouseEnterEvent>(e => { _dropHovered = true; ApplyColors(); });
            _dropdown.RegisterCallback<MouseLeaveEvent>(e => { _dropHovered = false; _dropPressed = false; ApplyColors(); });
            _dropdown.RegisterCallback<MouseDownEvent>(e => { _dropPressed = true; ApplyColors(); MD3Ripple.Spawn(_dropdown, e, _theme != null ? _theme.OnPrimary : Color.white); });
            _dropdown.RegisterCallback<MouseUpEvent>(e => { _dropPressed = false; ApplyColors(); });
            _dropdown.RegisterCallback<ClickEvent>(e => dropdownClicked?.Invoke());

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

            var baseBg = _theme.Primary;
            var fg = _theme.OnPrimary;

            // Main part
            var mainBg = baseBg;
            if (_mainPressed)
                mainBg = _theme.PressOverlay(mainBg, fg);
            else if (_mainHovered)
                mainBg = _theme.HoverOverlay(mainBg, fg);
            _main.style.backgroundColor = mainBg;
            _mainLabel.style.color = fg;

            // Dropdown part
            var dropBg = baseBg;
            if (_dropPressed)
                dropBg = _theme.PressOverlay(dropBg, fg);
            else if (_dropHovered)
                dropBg = _theme.HoverOverlay(dropBg, fg);
            _dropdown.style.backgroundColor = dropBg;
            _dropdownIcon.style.color = fg;

            // Divider
            var divColor = fg;
            divColor.a = 0.2f;
            _divider.style.backgroundColor = divColor;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
