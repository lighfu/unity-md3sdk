using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3TopAppBar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3TopAppBar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action navigationClicked;

        readonly VisualElement _nav;
        readonly Label _navIcon;
        readonly Label _title;
        readonly VisualElement _actions;
        MD3Theme _theme;
        bool _navHovered;

        public string Title
        {
            get => _title.text;
            set => _title.text = value;
        }

        public VisualElement Actions => _actions;

        public MD3TopAppBar() : this("Title") { }

        public MD3TopAppBar(string title, string navIcon = null)
        {
            AddToClassList("md3-top-app-bar");

            _nav = new VisualElement();
            _nav.AddToClassList("md3-top-app-bar__nav");

            _navIcon = new Label(navIcon ?? MD3Icon.ArrowBack);
            _navIcon.AddToClassList("md3-top-app-bar__nav-icon");
            _navIcon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_navIcon);
            _nav.Add(_navIcon);

            _nav.RegisterCallback<MouseEnterEvent>(e => { _navHovered = true; ApplyColors(); });
            _nav.RegisterCallback<MouseLeaveEvent>(e => { _navHovered = false; ApplyColors(); });
            _nav.RegisterCallback<MouseDownEvent>(e =>
                MD3Ripple.Spawn(_nav, e, _theme != null ? _theme.OnSurface : Color.white));
            _nav.RegisterCallback<ClickEvent>(e => navigationClicked?.Invoke());
            Add(_nav);

            _title = new Label(title);
            _title.AddToClassList("md3-top-app-bar__title");
            _title.pickingMode = PickingMode.Ignore;
            Add(_title);

            _actions = new VisualElement();
            _actions.AddToClassList("md3-top-app-bar__actions");
            Add(_actions);

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
            _title.style.color = _theme.OnSurface;
            _navIcon.style.color = _theme.OnSurface;

            var navBg = Color.clear;
            if (_navHovered) navBg = _theme.HoverOverlay(navBg, _theme.OnSurface);
            _nav.style.backgroundColor = navBg;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
