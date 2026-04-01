using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3TextFieldStyle
    {
        Outlined,
        Filled,
        /// <summary>テキストのみ。ボーダー・背景・ラベルなし。</summary>
        Plain,
    }

    public class MD3TextField : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3TextField, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<string> changed;
        public event Action trailingIconClicked;

        readonly VisualElement _container;
        readonly Label _floatingLabel;
        readonly TextField _input;
        readonly MD3TextFieldStyle _style;
        readonly VisualElement _indicator; // filled style bottom indicator
        Label _leadingIcon;
        Label _trailingIcon;
        Label _helperLabel;
        Label _counterLabel;
        MD3Theme _theme;
        bool _focused;
        bool _hovered;
        bool _hasError;
        string _errorText;
        string _helperText;
        int _maxLength;

        // Custom style overrides
        float? _customBorderWidth;
        float? _customBorderRadius;
        Color? _customBorderColor;
        Color? _customBackgroundColor;
        Color? _customFocusBorderColor;
        string _placeholder;

        public string Value
        {
            get => _input.value;
            set
            {
                _input.value = value;
                UpdateLabelFloat();
                UpdateCounter();
            }
        }

        public string LabelText
        {
            get => _floatingLabel.text;
            set => _floatingLabel.text = value;
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                ApplyColors();
                UpdateHelperText();
            }
        }

        public string ErrorText
        {
            get => _errorText;
            set
            {
                _errorText = value;
                if (_hasError) UpdateHelperText();
            }
        }

        public string LeadingIcon
        {
            get => _leadingIcon?.text;
            set
            {
                if (value == null)
                {
                    if (_leadingIcon != null) _leadingIcon.style.display = DisplayStyle.None;
                }
                else
                {
                    EnsureLeadingIcon();
                    _leadingIcon.text = value;
                    _leadingIcon.style.display = DisplayStyle.Flex;
                }
            }
        }

        public string TrailingIcon
        {
            get => _trailingIcon?.text;
            set
            {
                if (value == null)
                {
                    if (_trailingIcon != null) _trailingIcon.style.display = DisplayStyle.None;
                }
                else
                {
                    EnsureTrailingIcon();
                    _trailingIcon.text = value;
                    _trailingIcon.style.display = DisplayStyle.Flex;
                }
            }
        }

        /// <summary>ボーダー幅のオーバーライド。null でテーマデフォルト。</summary>
        public float? BorderWidth
        {
            get => _customBorderWidth;
            set { _customBorderWidth = value; ApplyColors(); }
        }

        /// <summary>ボーダー角丸のオーバーライド。null でテーマデフォルト。</summary>
        public float? BorderRadius
        {
            get => _customBorderRadius;
            set
            {
                _customBorderRadius = value;
                if (value.HasValue)
                    _container.Radius(value.Value);
                else
                    _container.Radius(_style == MD3TextFieldStyle.Filled ? 4f : 4f);
            }
        }

        /// <summary>ボーダー色のオーバーライド。null でテーマデフォルト。</summary>
        public Color? CustomBorderColor
        {
            get => _customBorderColor;
            set { _customBorderColor = value; ApplyColors(); }
        }

        /// <summary>背景色のオーバーライド。null でテーマデフォルト。</summary>
        public Color? CustomBackgroundColor
        {
            get => _customBackgroundColor;
            set { _customBackgroundColor = value; ApplyColors(); }
        }

        /// <summary>フォーカス時のボーダー色オーバーライド。null でテーマデフォルト。</summary>
        public Color? CustomFocusBorderColor
        {
            get => _customFocusBorderColor;
            set { _customFocusBorderColor = value; ApplyColors(); }
        }

        /// <summary>プレースホルダーテキスト（Plain スタイルで使用）。</summary>
        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value;
                var inner = _input.Q(className: "unity-text-field__input");
                // Unity 2022.3 doesn't have native placeholder, so we rely on the label or a workaround
                UpdatePlaceholder();
            }
        }

        public MD3TextField() : this("Label", MD3TextFieldStyle.Outlined) { }

        public MD3TextField(string label, MD3TextFieldStyle style = MD3TextFieldStyle.Outlined,
            string leadingIcon = null, string trailingIcon = null,
            string helperText = null, int maxLength = 0, bool multiline = false,
            string placeholder = null)
        {
            _style = style;
            _helperText = helperText;
            _maxLength = maxLength;
            _placeholder = placeholder;
            AddToClassList("md3-textfield");
            if (multiline) AddToClassList("md3-textfield--multiline");

            // Container
            _container = new VisualElement();
            _container.AddToClassList("md3-textfield__container");
            if (style == MD3TextFieldStyle.Plain)
            {
                _container.AddToClassList("md3-textfield__container--plain");
                // Plain: no border, no background by default
                _container.style.borderTopWidth = 0;
                _container.style.borderBottomWidth = 0;
                _container.style.borderLeftWidth = 0;
                _container.style.borderRightWidth = 0;
                _container.style.backgroundColor = Color.clear;
            }
            else
            {
                _container.AddToClassList(style == MD3TextFieldStyle.Outlined
                    ? "md3-textfield__container--outlined"
                    : "md3-textfield__container--filled");
            }
            Add(_container);

            // Leading icon
            if (leadingIcon != null)
            {
                EnsureLeadingIcon();
                _leadingIcon.text = leadingIcon;
            }

            // Floating label (hidden for Plain style)
            _floatingLabel = new Label(label);
            _floatingLabel.AddToClassList("md3-textfield__label");
            _floatingLabel.pickingMode = PickingMode.Ignore;
            if (style == MD3TextFieldStyle.Plain)
                _floatingLabel.style.display = DisplayStyle.None;
            _container.Add(_floatingLabel);

            // Text input
            _input = new TextField();
            _input.AddToClassList("md3-textfield__input");
            _input.multiline = multiline;
            _container.Add(_input);

            // Trailing icon
            if (trailingIcon != null)
            {
                EnsureTrailingIcon();
                _trailingIcon.text = trailingIcon;
            }

            // Filled style: bottom indicator
            if (style == MD3TextFieldStyle.Filled)
            {
                _indicator = new VisualElement();
                _indicator.style.position = Position.Absolute;
                _indicator.style.bottom = 0;
                _indicator.style.left = 0;
                _indicator.style.right = 0;
                _indicator.style.height = 1;
                _container.Add(_indicator);
            }

            // Helper text
            if (helperText != null)
            {
                EnsureHelperLabel();
                _helperLabel.text = helperText;
            }

            // Counter
            if (maxLength > 0)
            {
                EnsureCounterLabel();
                UpdateCounter();
            }

            // Placeholder label (for Plain style or explicit placeholder)
            if (placeholder != null)
                EnsurePlaceholderLabel();

            // Click anywhere in container → focus the input
            _container.RegisterCallback<PointerDownEvent>(_ =>
            {
                var inner = _input.Q(className: "unity-text-field__input");
                if (inner != null)
                    inner.Focus();
                else
                    _input.Focus();
            });

            // Events
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            _container.RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            _container.RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyColors(); });

            _input.RegisterCallback<FocusInEvent>(_ =>
            {
                _focused = true;
                UpdateLabelFloat();
                UpdatePlaceholder();
                ApplyColors();
            });

            _input.RegisterCallback<FocusOutEvent>(_ =>
            {
                _focused = false;
                UpdateLabelFloat();
                UpdatePlaceholder();
                ApplyColors();
            });

            _input.RegisterValueChangedCallback(evt =>
            {
                UpdateLabelFloat();
                UpdateCounter();
                UpdatePlaceholder();
                changed?.Invoke(evt.newValue);
            });
        }

        void EnsureLeadingIcon()
        {
            if (_leadingIcon != null) return;
            _leadingIcon = new Label();
            _leadingIcon.AddToClassList("md3-textfield__leading-icon");
            _leadingIcon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_leadingIcon);
            // Insert at beginning of container
            _container.Insert(0, _leadingIcon);
        }

        void EnsureTrailingIcon()
        {
            if (_trailingIcon != null) return;
            _trailingIcon = new Label();
            _trailingIcon.AddToClassList("md3-textfield__trailing-icon");
            MD3Icon.Apply(_trailingIcon);
            _trailingIcon.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                trailingIconClicked?.Invoke();
            });
            _container.Add(_trailingIcon);
        }

        void EnsureHelperLabel()
        {
            if (_helperLabel != null) return;
            _helperLabel = new Label();
            _helperLabel.AddToClassList("md3-textfield__helper");
            Add(_helperLabel);
        }

        void EnsureCounterLabel()
        {
            if (_counterLabel != null) return;
            _counterLabel = new Label();
            _counterLabel.AddToClassList("md3-textfield__counter");
            Add(_counterLabel);
        }

        Label _placeholderLabel;

        void EnsurePlaceholderLabel()
        {
            if (_placeholderLabel != null) return;
            _placeholderLabel = new Label(_placeholder ?? "");
            _placeholderLabel.pickingMode = PickingMode.Ignore;
            _placeholderLabel.style.position = Position.Absolute;
            _placeholderLabel.style.left = 16;
            _placeholderLabel.style.top = 0;
            _placeholderLabel.style.bottom = 0;
            _placeholderLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _placeholderLabel.style.fontSize = 14;
            _container.Add(_placeholderLabel);
        }

        void UpdatePlaceholder()
        {
            if (_placeholderLabel == null && string.IsNullOrEmpty(_placeholder)) return;
            if (!string.IsNullOrEmpty(_placeholder))
                EnsurePlaceholderLabel();
            if (_placeholderLabel == null) return;

            _placeholderLabel.text = _placeholder;
            bool show = string.IsNullOrEmpty(_input.value) && !_focused;
            _placeholderLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            _placeholderLabel.style.color = _theme?.OnSurfaceVariant ?? new Color(0.6f, 0.6f, 0.6f);
            _placeholderLabel.style.opacity = 0.6f;
        }

        void UpdateHelperText()
        {
            if (_helperLabel == null && !_hasError) return;
            if (_hasError && !string.IsNullOrEmpty(_errorText))
            {
                EnsureHelperLabel();
                _helperLabel.text = _errorText;
                _helperLabel.style.display = DisplayStyle.Flex;
            }
            else if (!_hasError && !string.IsNullOrEmpty(_helperText))
            {
                _helperLabel.text = _helperText;
                _helperLabel.style.display = DisplayStyle.Flex;
            }
            else if (_helperLabel != null && string.IsNullOrEmpty(_helperText))
            {
                _helperLabel.style.display = _hasError && !string.IsNullOrEmpty(_errorText)
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }
            ApplyColors();
        }

        void UpdateCounter()
        {
            if (_maxLength <= 0 || _counterLabel == null) return;
            int len = _input.value?.Length ?? 0;
            _counterLabel.text = $"{len}/{_maxLength}";
            ApplyColors();
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            _theme = ResolveTheme();
            UpdateLabelFloat();
            UpdatePlaceholder();
            ApplyColors();
        }

        void UpdateLabelFloat()
        {
            bool shouldFloat = _focused || !string.IsNullOrEmpty(_input.value);
            if (shouldFloat)
                _floatingLabel.AddToClassList("md3-textfield__label--float");
            else
                _floatingLabel.RemoveFromClassList("md3-textfield__label--float");

            // Offset label when leading icon is present to avoid overlap
            bool hasLeading = _leadingIcon != null && _leadingIcon.style.display != DisplayStyle.None;
            if (hasLeading && !shouldFloat)
                _floatingLabel.style.left = 48f;
            else
                _floatingLabel.style.left = StyleKeyword.Null; // let USS handle it

            // Notch effect: give floating label a background to mask the border
            UpdateLabelBackground(shouldFloat);
        }

        void UpdateLabelBackground(bool floating)
        {
            if (_style == MD3TextFieldStyle.Outlined && floating)
            {
                // Resolve the surface color behind the text field to mask the border
                var bg = _theme?.Surface ?? Color.clear;
                // Walk up to find the nearest opaque background
                var el = this.parent;
                while (el != null)
                {
                    var resolved = el.resolvedStyle.backgroundColor;
                    if (resolved.a > 0.01f) { bg = resolved; break; }
                    el = el.parent;
                }
                _floatingLabel.style.backgroundColor = bg;
            }
            else
            {
                _floatingLabel.style.backgroundColor = Color.clear;
            }
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color borderColor, labelColor, inputColor, bgColor;

            if (_style == MD3TextFieldStyle.Plain)
            {
                bgColor = _customBackgroundColor ?? Color.clear;
                inputColor = _theme.OnSurface;
                labelColor = _theme.OnSurfaceVariant;

                float bw = _customBorderWidth ?? 0f;
                borderColor = _customBorderColor ?? Color.clear;

                if (_focused)
                    borderColor = _customFocusBorderColor ?? _customBorderColor ?? _theme.Primary;

                _container.style.borderTopWidth = bw;
                _container.style.borderBottomWidth = bw;
                _container.style.borderLeftWidth = bw;
                _container.style.borderRightWidth = bw;
                _container.style.borderTopColor = borderColor;
                _container.style.borderBottomColor = borderColor;
                _container.style.borderLeftColor = borderColor;
                _container.style.borderRightColor = borderColor;
            }
            else if (_style == MD3TextFieldStyle.Outlined)
            {
                bgColor = _customBackgroundColor ?? Color.clear;
                inputColor = _theme.OnSurface;

                if (_hasError)
                {
                    borderColor = _theme.Error;
                    labelColor = _theme.Error;
                    SetBorderWidth(_customBorderWidth ?? 2f);
                }
                else if (_focused)
                {
                    borderColor = _customFocusBorderColor ?? _theme.Primary;
                    labelColor = _theme.Primary;
                    SetBorderWidth(_customBorderWidth ?? 2f);
                }
                else if (_hovered)
                {
                    borderColor = _customBorderColor ?? _theme.OnSurface;
                    labelColor = _theme.OnSurfaceVariant;
                    SetBorderWidth(_customBorderWidth ?? 1f);
                }
                else
                {
                    borderColor = _customBorderColor ?? _theme.Outline;
                    labelColor = _theme.OnSurfaceVariant;
                    SetBorderWidth(_customBorderWidth ?? 1f);
                }

                _container.style.borderTopColor = borderColor;
                _container.style.borderBottomColor = borderColor;
                _container.style.borderLeftColor = borderColor;
                _container.style.borderRightColor = borderColor;
            }
            else // Filled
            {
                bgColor = _customBackgroundColor ?? _theme.SurfaceVariant;
                inputColor = _theme.OnSurface;

                if (_hasError)
                {
                    labelColor = _theme.Error;
                    if (_indicator != null)
                    {
                        _indicator.style.height = 2;
                        _indicator.style.backgroundColor = _theme.Error;
                    }
                }
                else if (_focused)
                {
                    labelColor = _theme.Primary;
                    if (_indicator != null)
                    {
                        _indicator.style.height = 2;
                        _indicator.style.backgroundColor = _theme.Primary;
                    }
                }
                else
                {
                    labelColor = _theme.OnSurfaceVariant;
                    if (_indicator != null)
                    {
                        _indicator.style.height = 1;
                        _indicator.style.backgroundColor = _theme.OnSurfaceVariant;
                    }
                }

                if (_hovered && !_focused)
                    bgColor = _theme.HoverOverlay(bgColor, _theme.OnSurface);
            }

            _container.style.backgroundColor = bgColor;
            _floatingLabel.style.color = labelColor;
            _input.style.color = inputColor;
            // Unity TextField renders text in a nested .unity-text-field__input element
            var innerInput = _input.Q(className: "unity-text-field__input");
            if (innerInput != null)
                innerInput.style.color = inputColor;

            // Icons
            if (_leadingIcon != null)
                _leadingIcon.style.color = _hasError ? _theme.Error : _theme.OnSurfaceVariant;
            if (_trailingIcon != null)
                _trailingIcon.style.color = _hasError ? _theme.Error : _theme.OnSurfaceVariant;

            // Helper label
            if (_helperLabel != null)
                _helperLabel.style.color = _hasError ? _theme.Error : _theme.OnSurfaceVariant;

            // Counter
            if (_counterLabel != null)
            {
                int len = _input.value?.Length ?? 0;
                _counterLabel.style.color = (_maxLength > 0 && len > _maxLength) ? _theme.Error : _theme.OnSurfaceVariant;
            }

            // Refresh notch background on theme change
            bool floating = _focused || !string.IsNullOrEmpty(_input.value);
            UpdateLabelBackground(floating);
        }

        void SetBorderWidth(float w)
        {
            _container.style.borderTopWidth = w;
            _container.style.borderBottomWidth = w;
            _container.style.borderLeftWidth = w;
            _container.style.borderRightWidth = w;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
