using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3NavDrawerItem : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3NavDrawerItem, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly Label _icon;
        readonly Label _label;
        readonly Label _badge;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

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

        public MD3NavDrawerItem() : this(MD3Icon.Home, "Home") { }

        public MD3NavDrawerItem(string icon, string label, int badge = 0, bool selected = false)
        {
            _selected = selected;
            AddToClassList("md3-nav-drawer-item");

            _icon = new Label(icon);
            _icon.AddToClassList("md3-nav-drawer-item__icon");
            _icon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_icon);
            Add(_icon);

            _label = new Label(label);
            _label.AddToClassList("md3-nav-drawer-item__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            if (badge > 0)
            {
                _badge = new Label(badge.ToString());
                _badge.AddToClassList("md3-nav-drawer-item__badge");
                _badge.pickingMode = PickingMode.Ignore;
                Add(_badge);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(e => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(e => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e =>
            {
                _pressed = true;
                ApplyColors();
                var local = this.WorldToLocal(e.mousePosition);
                MD3Ripple.Spawn(this, new Vector2(local.x, local.y),
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

            Color bg, fg;
            if (_selected)
            {
                bg = _useSharedHighlight ? Color.clear : _theme.SecondaryContainer;
                fg = _theme.OnSecondaryContainer;
                _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                bg = Color.clear;
                fg = _theme.OnSurfaceVariant;
                _label.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            if (_pressed)
                bg = _theme.PressOverlay(bg, fg);
            else if (_hovered)
                bg = _theme.HoverOverlay(bg, fg);

            style.backgroundColor = bg;
            _icon.style.color = fg;
            _label.style.color = fg;
            if (_badge != null) _badge.style.color = fg;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }

    public class MD3NavDrawer : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3NavDrawer, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int> changed;

        readonly List<MD3NavDrawerItem> _items = new List<MD3NavDrawerItem>();
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

        public MD3NavDrawer() : this(new[] { (MD3Icon.Home, "Home", 0), (MD3Icon.Settings, "Settings", 0) }) { }

        public MD3NavDrawer((string icon, string label, int badge)[] entries, int selectedIndex = 0, Action<int> onChanged = null)
        {
            _selectedIndex = selectedIndex;
            if (onChanged != null) changed += onChanged;

            AddToClassList("md3-nav-drawer");
            style.flexShrink = 0;

            // Shared sliding highlight
            _highlight = new VisualElement();
            _highlight.AddToClassList("md3-nav-drawer__highlight");
            _highlight.pickingMode = PickingMode.Ignore;
            _highlight.style.position = Position.Absolute;
            _highlight.style.left = 0;
            _highlight.style.right = 0;
            _highlight.style.height = 56;
            _highlight.style.borderTopLeftRadius = 28;
            _highlight.style.borderTopRightRadius = 28;
            _highlight.style.borderBottomLeftRadius = 28;
            _highlight.style.borderBottomRightRadius = 28;
            Add(_highlight);

            for (int i = 0; i < entries.Length; i++)
            {
                var item = new MD3NavDrawerItem(entries[i].icon, entries[i].label, entries[i].badge, i == selectedIndex);
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

            var item = _items[_selectedIndex];
            var itemBound = item.worldBound;
            var drawerBound = this.worldBound;

            if (drawerBound.height <= 0 || itemBound.height <= 0) return;

            float targetTop = itemBound.y - drawerBound.y;

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

            _highlight.style.backgroundColor = _theme.SecondaryContainer;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
