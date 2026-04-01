using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3NavRailItem : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3NavRailItem, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly VisualElement _pill;
        readonly Label _icon;
        readonly Label _label;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        internal VisualElement Pill => _pill;
        internal bool _useSharedHighlight;

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

        public MD3NavRailItem() : this(MD3Icon.Home, "Home") { }

        public MD3NavRailItem(string icon, string label, bool selected = false)
        {
            _selected = selected;
            AddToClassList("md3-nav-rail-item");

            _pill = new VisualElement();
            _pill.AddToClassList("md3-nav-rail-item__pill");
            _pill.pickingMode = PickingMode.Ignore;

            _icon = new Label(icon);
            _icon.AddToClassList("md3-nav-rail-item__icon");
            _icon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_icon, 18f);
            _pill.Add(_icon);
            Add(_pill);

            _label = new Label(label);
            _label.AddToClassList("md3-nav-rail-item__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(e => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(e => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e =>
            {
                _pressed = true;
                ApplyColors();
                var local = _pill.WorldToLocal(e.mousePosition);
                MD3Ripple.Spawn(_pill, new Vector2(local.x, local.y),
                    _theme != null ? _theme.OnSecondaryContainer : Color.white);
            });
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
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color pillBg, fg;
            if (_selected)
            {
                pillBg = _useSharedHighlight ? Color.clear : _theme.SecondaryContainer;
                fg = _theme.OnSecondaryContainer;
                _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                pillBg = Color.clear;
                fg = _theme.OnSurfaceVariant;
                _label.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            if (_pressed)
                pillBg = _theme.PressOverlay(pillBg, fg);
            else if (_hovered)
                pillBg = _theme.HoverOverlay(pillBg, fg);

            _pill.style.backgroundColor = pillBg;
            _icon.style.color = fg;
            _label.style.color = fg;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }

    public class MD3NavRail : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3NavRail, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int> changed;

        readonly List<MD3NavRailItem> _items = new List<MD3NavRailItem>();
        readonly VisualElement _highlight;
        MD3Theme _theme;
        MD3AnimationHandle _highlightAnim;
        float _currentTop = float.NaN;
        bool _isAnimating;

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

        public MD3NavRail() : this(new[] { (MD3Icon.Home, "Home"), (MD3Icon.Settings, "Settings") }) { }

        public MD3NavRail((string icon, string label)[] entries, int selectedIndex = 0, Action<int> onChanged = null)
        {
            _selectedIndex = selectedIndex;
            if (onChanged != null) changed += onChanged;

            AddToClassList("md3-nav-rail");
            style.flexDirection = FlexDirection.Column;
            style.width = 80;
            style.paddingTop = 8;
            style.flexShrink = 0;

            // Shared sliding highlight
            _highlight = new VisualElement();
            _highlight.AddToClassList("md3-nav-rail__highlight");
            _highlight.pickingMode = PickingMode.Ignore;
            _highlight.style.position = Position.Absolute;
            _highlight.style.width = 56;
            _highlight.style.height = 32;
            _highlight.style.borderTopLeftRadius = 16;
            _highlight.style.borderTopRightRadius = 16;
            _highlight.style.borderBottomLeftRadius = 16;
            _highlight.style.borderBottomRightRadius = 16;
            Add(_highlight);

            for (int i = 0; i < entries.Length; i++)
            {
                var item = new MD3NavRailItem(entries[i].icon, entries[i].label, i == selectedIndex);
                item._useSharedHighlight = true;
                var idx = i;
                item.changed += selected =>
                {
                    if (!selected) return;
                    _selectedIndex = idx;
                    UpdateSelection();
                    changed?.Invoke(_selectedIndex);
                };
                _items.Add(item);
                Add(item);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (e.oldRect.size == e.newRect.size) return;
                if (!_isAnimating)
                    PositionHighlight(false);
            });
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
            schedule.Execute(() => PositionHighlight(false));
        }

        void UpdateSelection()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (i == _selectedIndex && !_items[i].Selected)
                    _items[i].Selected = true;
                else if (i != _selectedIndex && _items[i].Selected)
                    _items[i].Selected = false;
            }
            PositionHighlight(true);
        }

        void PositionHighlight(bool animate)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _items.Count) return;

            var pill = _items[_selectedIndex].Pill;
            var pillBound = pill.worldBound;
            var railBound = this.worldBound;

            if (railBound.height <= 0 || pillBound.height <= 0) return;

            float targetLeft = pillBound.x - railBound.x;
            float targetTop = pillBound.y - railBound.y;

            _highlight.style.left = targetLeft;

            if (!animate || float.IsNaN(_currentTop))
            {
                _highlightAnim?.Cancel();
                _isAnimating = false;
                _highlight.style.top = targetTop;
                _currentTop = targetTop;
                return;
            }

            _highlightAnim?.Cancel();
            float fromTop = _currentTop;
            _currentTop = targetTop;
            _isAnimating = true;
            _highlightAnim = MD3Animate.Float(_highlight, fromTop, targetTop, 80f, MD3Easing.EaseInOut,
                v => _highlight.style.top = v,
                () => _isAnimating = false);
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.backgroundColor = _theme.Surface;
            style.borderRightWidth = 1;
            style.borderRightColor = _theme.OutlineVariant;
            _highlight.style.backgroundColor = _theme.SecondaryContainer;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
