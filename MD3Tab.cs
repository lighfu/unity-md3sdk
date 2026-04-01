using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Tab : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Tab, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

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
                ApplyColors();
                changed?.Invoke(_selected);
            }
        }
        bool _selected;

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public MD3Tab() : this("Tab") { }

        public MD3Tab(string label, bool selected = false)
        {
            _selected = selected;
            AddToClassList("md3-tab");

            _label = new Label(label);
            _label.AddToClassList("md3-tab__label");
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
            if (!_selected)
            {
                _selected = true;
                ApplyColors();
                changed?.Invoke(true);
            }
            // ripple は MouseDown で発火済み
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            var bg = Color.clear;
            Color fg;

            if (_selected)
            {
                fg = _theme.Primary;
                _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                fg = _theme.OnSurfaceVariant;
                _label.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            if (_pressed)
                bg = _theme.PressOverlay(bg, fg);
            else if (_hovered)
                bg = _theme.HoverOverlay(bg, fg);

            style.backgroundColor = bg;
            _label.style.color = fg;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }

    public class MD3TabBar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3TabBar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int> changed;

        readonly List<MD3Tab> _tabs = new List<MD3Tab>();
        readonly VisualElement _indicator;
        MD3Theme _theme;
        MD3AnimationHandle _indicatorLeftAnim;
        MD3AnimationHandle _indicatorWidthAnim;
        bool _hasIndicatorPosition;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                UpdateSelection();
                changed?.Invoke(_selectedIndex);
            }
        }
        int _selectedIndex;

        public MD3TabBar() : this(new[] { "Tab 1", "Tab 2" }, 0) { }

        public MD3TabBar(string[] labels, int selectedIndex = 0, Action<int> onChanged = null)
        {
            _selectedIndex = selectedIndex;
            if (onChanged != null) changed += onChanged;

            AddToClassList("md3-tab-bar");

            for (int i = 0; i < labels.Length; i++)
            {
                var tab = new MD3Tab(labels[i], i == selectedIndex);
                tab.style.flexGrow = 1;
                var idx = i;
                tab.changed += selected =>
                {
                    if (!selected) return;
                    _selectedIndex = idx;
                    UpdateSelection();
                    changed?.Invoke(_selectedIndex);
                };
                _tabs.Add(tab);
                Add(tab);
            }

            // Shared sliding indicator
            _indicator = new VisualElement();
            _indicator.AddToClassList("md3-tab-bar__indicator");
            _indicator.pickingMode = PickingMode.Ignore;
            Add(_indicator);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size == evt.newRect.size) return;
            PositionIndicator(false);
        }

        void UpdateSelection()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (i == _selectedIndex && !_tabs[i].Selected)
                    _tabs[i].Selected = true;
                else if (i != _selectedIndex && _tabs[i].Selected)
                    _tabs[i].Selected = false;
            }
            PositionIndicator(true);
        }

        void PositionIndicator(bool animate)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _tabs.Count) return;

            var tab = _tabs[_selectedIndex];
            var tabBound = tab.worldBound;
            var barBound = this.worldBound;

            // Guard against uninitialized layout
            if (barBound.width <= 0 || tabBound.width <= 0) return;

            float targetLeft = tabBound.x - barBound.x;
            float targetWidth = tabBound.width;

            if (!animate || !_hasIndicatorPosition)
            {
                _indicatorLeftAnim?.Cancel();
                _indicatorWidthAnim?.Cancel();
                _indicator.style.left = targetLeft;
                _indicator.style.width = targetWidth;
                _hasIndicatorPosition = true;
                return;
            }

            float currentLeft = _indicator.resolvedStyle.left;
            float currentWidth = _indicator.resolvedStyle.width;
            if (float.IsNaN(currentLeft)) currentLeft = targetLeft;
            if (float.IsNaN(currentWidth)) currentWidth = targetWidth;

            _indicatorLeftAnim?.Cancel();
            _indicatorWidthAnim?.Cancel();

            _indicatorLeftAnim = MD3Animate.Float(_indicator, currentLeft, targetLeft, 80f, MD3Easing.EaseInOut,
                v => _indicator.style.left = v);
            _indicatorWidthAnim = MD3Animate.Float(_indicator, currentWidth, targetWidth, 80f, MD3Easing.EaseInOut,
                v => _indicator.style.width = v);
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.borderBottomColor = _theme.OutlineVariant;
            _indicator.style.backgroundColor = _theme.Primary;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
