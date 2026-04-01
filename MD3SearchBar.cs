using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3SearchBar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3SearchBar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<string> searchChanged;
        public event Action<int> suggestionSelected;

        readonly VisualElement _container;
        readonly Label _searchIcon;
        readonly TextField _input;
        readonly Label _clearBtn;
        readonly VisualElement _dropdown;
        readonly List<MD3MenuItem> _menuItems = new List<MD3MenuItem>();
        string[] _suggestions = Array.Empty<string>();
        VisualElement _scrim;
        MD3AnimationHandle _dropdownAnim;
        MD3Theme _theme;
        bool _dropdownOpen;

        public string Value
        {
            get => _input.value;
            set
            {
                _input.value = value;
                UpdateClearButton();
            }
        }

        public MD3SearchBar() : this("Search...") { }

        public MD3SearchBar(string placeholder = "Search...", Action<string> onSearchChanged = null)
        {
            if (onSearchChanged != null) searchChanged += onSearchChanged;

            AddToClassList("md3-search-bar");

            // Container (pill shape)
            _container = new VisualElement();
            _container.AddToClassList("md3-search-bar__container");

            // Search icon
            _searchIcon = new Label(MD3Icon.Search);
            _searchIcon.AddToClassList("md3-search-bar__icon");
            _searchIcon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_searchIcon);
            _container.Add(_searchIcon);

            // Input
            _input = new TextField();
            _input.AddToClassList("md3-search-bar__input");
            var inputField = _input.Q<VisualElement>(className: "unity-text-field__input");
            if (inputField != null)
            {
                inputField.style.borderTopWidth = 0;
                inputField.style.borderBottomWidth = 0;
                inputField.style.borderLeftWidth = 0;
                inputField.style.borderRightWidth = 0;
                inputField.style.backgroundColor = Color.clear;
            }
            _input.RegisterValueChangedCallback(e =>
            {
                UpdateClearButton();
                searchChanged?.Invoke(e.newValue);
                if (_suggestions.Length > 0 && !string.IsNullOrEmpty(e.newValue))
                    OpenDropdown();
                else
                    CloseDropdown();
            });
            _container.Add(_input);

            // Clear button
            _clearBtn = new Label(MD3Icon.Close);
            _clearBtn.AddToClassList("md3-search-bar__clear");
            MD3Icon.Apply(_clearBtn, 18f);
            _clearBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                Clear();
            });
            _clearBtn.style.display = DisplayStyle.None;
            _container.Add(_clearBtn);

            Add(_container);

            // Dropdown (not added to hierarchy — added to themed root on open)
            _dropdown = new VisualElement();
            _dropdown.AddToClassList("md3-search-bar__dropdown");
            _dropdown.style.position = Position.Absolute;

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void SetSuggestions(string[] items)
        {
            _suggestions = items ?? Array.Empty<string>();
            RebuildDropdownItems();

            if (_suggestions.Length > 0 && !string.IsNullOrEmpty(_input.value))
                OpenDropdown();
            else
                CloseDropdown();
        }

        public new void Clear()
        {
            _input.value = "";
            UpdateClearButton();
            CloseDropdown();
            searchChanged?.Invoke("");
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void UpdateClearButton()
        {
            _clearBtn.style.display = string.IsNullOrEmpty(_input.value)
                ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void RebuildDropdownItems()
        {
            _dropdown.Clear();
            _menuItems.Clear();

            for (int i = 0; i < _suggestions.Length; i++)
            {
                var item = new MD3MenuItem(_suggestions[i]);
                var idx = i;
                item.clicked += () =>
                {
                    _input.value = _suggestions[idx];
                    UpdateClearButton();
                    CloseDropdown();
                    suggestionSelected?.Invoke(idx);
                    searchChanged?.Invoke(_input.value);
                };
                _menuItems.Add(item);
                _dropdown.Add(item);
            }
        }

        void OpenDropdown()
        {
            if (_dropdownOpen) return;
            if (_suggestions.Length == 0) return;
            _dropdownOpen = true;

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

            // Scrim (transparent click catcher)
            _scrim = new VisualElement();
            _scrim.AddToClassList("md3-fab-speed-dial__scrim");
            _scrim.style.backgroundColor = Color.clear;
            _scrim.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                CloseDropdown();
            });
            themedRoot.Add(_scrim);

            // Add dropdown
            _dropdown.style.opacity = 0f;
            _dropdown.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
            themedRoot.Add(_dropdown);

            // Position dropdown below search bar
            EventCallback<GeometryChangedEvent> positionCb = null;
            var posRoot = themedRoot;
            positionCb = e =>
            {
                _dropdown.UnregisterCallback(positionCb);
                var barWorld = _container.worldBound;
                var rootWorld = posRoot.worldBound;
                _dropdown.style.left = barWorld.x - rootWorld.x;
                _dropdown.style.top = barWorld.yMax - rootWorld.y + 2f;
                _dropdown.style.width = barWorld.width;
            };
            _dropdown.RegisterCallback(positionCb);

            // Animate in
            _dropdownAnim?.Cancel();
            _dropdownAnim = MD3Animate.FadeScale(_dropdown, 0f, 1f, 0.95f, 1f, 150f, MD3Easing.EaseOut);

            ApplyColors();
        }

        void CloseDropdown()
        {
            if (!_dropdownOpen) return;
            _dropdownOpen = false;

            _dropdownAnim?.Cancel();
            _dropdownAnim = MD3Animate.FadeScale(_dropdown, 1f, 0f, 1f, 0.95f, 100f, MD3Easing.EaseIn, () =>
            {
                _dropdown.RemoveFromHierarchy();
                _scrim?.RemoveFromHierarchy();
                _scrim = null;
            });
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            _container.style.backgroundColor = _theme.SurfaceContainerHigh;
            _searchIcon.style.color = _theme.OnSurfaceVariant;
            _clearBtn.style.color = _theme.OnSurfaceVariant;
            _input.style.color = _theme.OnSurface;
            var textEl = _input.Q<TextElement>();
            if (textEl != null) textEl.style.color = _theme.OnSurface;
            // placeholder カーソル色
            var placeholder = _input.Q(className: "unity-text-field__placeholder");
            if (placeholder != null) placeholder.style.color = _theme.OnSurfaceVariant;
            _dropdown.style.backgroundColor = _theme.SurfaceContainerHigh;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
