using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Dropdown : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Dropdown, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int> changed;

        readonly VisualElement _field;
        readonly Label _labelEl;
        readonly Label _valueEl;
        readonly Label _arrow;
        readonly VisualElement _menu;
        readonly string[] _options;
        readonly string _labelText;
        readonly List<MD3MenuItem> _menuItems = new List<MD3MenuItem>();
        MD3Theme _theme;
        bool _open;
        VisualElement _scrim;
        MD3AnimationHandle _menuAnim;
        VisualElement _shadowAmbient, _shadowKey;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                _valueEl.text = _selectedIndex >= 0 && _selectedIndex < _options.Length
                    ? _options[_selectedIndex] : "";
                changed?.Invoke(_selectedIndex);
            }
        }
        int _selectedIndex;

        public MD3Dropdown() : this("Label", new[] { "Option 1", "Option 2" }) { }

        public MD3Dropdown(string label, string[] options, int selectedIndex = 0, Action<int> onChanged = null)
        {
            _options = options;
            _selectedIndex = selectedIndex;
            _labelText = label;
            if (onChanged != null) changed += onChanged;

            AddToClassList("md3-dropdown");

            // Field
            _field = new VisualElement();
            _field.AddToClassList("md3-dropdown__field");

            _labelEl = new Label(label);
            _labelEl.AddToClassList("md3-dropdown__label");
            _labelEl.pickingMode = PickingMode.Ignore;
            _field.Add(_labelEl);

            _valueEl = new Label(selectedIndex >= 0 && selectedIndex < options.Length ? options[selectedIndex] : "");
            _valueEl.AddToClassList("md3-dropdown__value");
            _valueEl.pickingMode = PickingMode.Ignore;
            _field.Add(_valueEl);

            _arrow = new Label(MD3Icon.ArrowDropDown);
            _arrow.AddToClassList("md3-dropdown__arrow");
            _arrow.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_arrow, 14f);
            _field.Add(_arrow);

            _field.RegisterCallback<ClickEvent>(e => Toggle());
            Add(_field);

            // Menu (not added to hierarchy — will be added to themed root on open)
            _menu = new VisualElement();
            _menu.AddToClassList("md3-dropdown__menu");
            _menu.style.position = Position.Absolute;

            for (int i = 0; i < options.Length; i++)
            {
                var item = new MD3MenuItem(options[i]);
                var idx = i;
                item.clicked += () =>
                {
                    _selectedIndex = idx;
                    _valueEl.text = _options[idx];
                    Close();
                    changed?.Invoke(_selectedIndex);
                };
                _menuItems.Add(item);
                _menu.Add(item);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        void Toggle()
        {
            if (_open) Close(); else Open();
        }

        void Open()
        {
            _open = true;
            _arrow.style.rotate = new Rotate(180f);

            // Find themed root
            var root = this as VisualElement;
            VisualElement themedRoot = null;
            while (root != null)
            {
                if (root.ClassListContains("md3-dark") || root.ClassListContains("md3-light"))
                    themedRoot = root;
                root = root.parent;
            }
            if (themedRoot == null) themedRoot = this.parent ?? this;

            // Add transparent scrim
            _scrim = new VisualElement();
            _scrim.AddToClassList("md3-fab-speed-dial__scrim"); // reuse scrim style
            _scrim.style.backgroundColor = Color.clear;
            _scrim.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                Close();
            });
            themedRoot.Add(_scrim);

            // Add menu to themed root
            _menu.style.opacity = 0f;
            _menu.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
            themedRoot.Add(_menu);
            (_shadowAmbient, _shadowKey) = MD3Elevation.AddSiblingShadow(themedRoot, _menu, 4f, 2);

            // Position menu relative to field
            EventCallback<GeometryChangedEvent> positionCb = null;
            var posRoot = themedRoot;
            positionCb = e =>
            {
                _menu.UnregisterCallback(positionCb);
                var fieldWorld = _field.worldBound;
                var rootWorld = posRoot.worldBound;
                _menu.style.left = fieldWorld.x - rootWorld.x;
                _menu.style.top = fieldWorld.yMax - rootWorld.y + 2f;
                _menu.style.width = fieldWorld.width;
            };
            _menu.RegisterCallback(positionCb);

            // Animate in
            _menuAnim?.Cancel();
            _menuAnim = MD3Animate.FadeScale(_menu, 0f, 1f, 0.95f, 1f, 100f, MD3Easing.EaseOut);

            ApplyColors();
        }

        void Close()
        {
            if (!_open) return;
            _open = false;
            _arrow.style.rotate = new Rotate(0f);

            _menuAnim?.Cancel();
            _menuAnim = MD3Animate.FadeScale(_menu, 1f, 0f, 1f, 0.95f, 70f, MD3Easing.EaseIn, () =>
            {
                _menu.RemoveFromHierarchy();
                _shadowAmbient?.RemoveFromHierarchy();
                _shadowKey?.RemoveFromHierarchy();
                _shadowAmbient = null;
                _shadowKey = null;
                _scrim?.RemoveFromHierarchy();
                _scrim = null;
            });

            ApplyColors();
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

            var borderColor = _open ? _theme.Primary : _theme.Outline;
            _field.style.borderTopColor = borderColor;
            _field.style.borderBottomColor = borderColor;
            _field.style.borderLeftColor = borderColor;
            _field.style.borderRightColor = borderColor;
            if (_open)
                _field.style.borderBottomWidth = 2;
            else
                _field.style.borderBottomWidth = 1;

            _field.style.backgroundColor = Color.clear;
            _valueEl.style.color = _theme.OnSurface;
            _arrow.style.color = _theme.OnSurfaceVariant;

            _labelEl.style.color = _open ? _theme.Primary : _theme.OnSurfaceVariant;
            _labelEl.style.backgroundColor = _theme.Surface;

            _menu.style.backgroundColor = _theme.SurfaceContainerHigh;

            MD3Elevation.UpdateShadowColor(_shadowAmbient, _shadowKey, _theme.IsDark, 2);
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
