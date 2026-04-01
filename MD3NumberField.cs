using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Numeric input field with +/- buttons and drag-to-adjust.
    /// Supports int and float modes with configurable min/max/step.
    /// </summary>
    public class MD3NumberField : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3NumberField, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<float> changed;

        readonly VisualElement _container;
        readonly Label _floatingLabel;
        readonly VisualElement _decrementBtn;
        readonly TextField _input;
        readonly VisualElement _incrementBtn;
        readonly float _min;
        readonly float _max;
        readonly float _step;
        readonly bool _intMode;
        readonly string _format;
        MD3Theme _theme;
        bool _focused;
        bool _hovered;
        bool _dragging;
        float _dragStartValue;
        float _dragStartY;

        public float Value
        {
            get => _value;
            set
            {
                float clamped = Clamp(value);
                if (Mathf.Approximately(_value, clamped)) return;
                _value = clamped;
                SyncInputText();
                changed?.Invoke(_value);
            }
        }
        float _value;

        public string LabelText
        {
            get => _floatingLabel.text;
            set => _floatingLabel.text = value;
        }

        public MD3NumberField() : this("Value") { }

        public MD3NumberField(string label, float value = 0f, float min = float.MinValue,
            float max = float.MaxValue, float step = 1f, bool intMode = false)
        {
            _min = min;
            _max = max;
            _step = Mathf.Max(step, 0.001f);
            _intMode = intMode;
            _format = intMode ? "0" : DetermineFormat(step);
            _value = Clamp(value);

            AddToClassList("md3-numberfield");

            // Container
            _container = new VisualElement();
            _container.AddToClassList("md3-numberfield__container");
            Add(_container);

            // Decrement button
            _decrementBtn = CreateStepButton(MD3Icon.NavigateBefore, -1);
            _container.Add(_decrementBtn);

            // Floating label
            _floatingLabel = new Label(label);
            _floatingLabel.AddToClassList("md3-numberfield__label");
            _floatingLabel.pickingMode = PickingMode.Ignore;
            _container.Add(_floatingLabel);

            // Text input
            _input = new TextField();
            _input.AddToClassList("md3-numberfield__input");
            _input.value = FormatValue(_value);
            _container.Add(_input);

            // Increment button
            _incrementBtn = CreateStepButton(MD3Icon.NavigateNext, 1);
            _container.Add(_incrementBtn);

            // Events
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            _container.RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            _container.RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyColors(); });

            _input.RegisterCallback<FocusInEvent>(_ =>
            {
                _focused = true;
                ApplyColors();
            });

            _input.RegisterCallback<FocusOutEvent>(_ =>
            {
                _focused = false;
                CommitInput();
                ApplyColors();
            });

            _input.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    CommitInput();
                    e.StopPropagation();
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    Value += _step;
                    e.StopPropagation();
                }
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    Value -= _step;
                    e.StopPropagation();
                }
            });

            // Drag to adjust on label
            _floatingLabel.RegisterCallback<MouseDownEvent>(OnDragStart);
            _floatingLabel.RegisterCallback<MouseMoveEvent>(OnDragMove);
            _floatingLabel.RegisterCallback<MouseUpEvent>(OnDragEnd);
            _floatingLabel.style.cursor = StyleKeyword.None; // drag cursor
        }

        VisualElement CreateStepButton(string icon, int direction)
        {
            var btn = new VisualElement();
            btn.AddToClassList("md3-numberfield__step-btn");
            var iconLabel = MD3Icon.Create(icon, 18f);
            btn.Add(iconLabel);

            btn.RegisterCallback<ClickEvent>(e =>
            {
                Value += _step * direction;
                e.StopPropagation();
            });
            btn.RegisterCallback<MouseEnterEvent>(_ => btn.AddToClassList("md3-numberfield__step-btn--hover"));
            btn.RegisterCallback<MouseLeaveEvent>(_ => btn.RemoveFromClassList("md3-numberfield__step-btn--hover"));

            return btn;
        }

        void OnDragStart(MouseDownEvent e)
        {
            if (e.button != 0) return;
            _dragging = true;
            _dragStartValue = _value;
            _dragStartY = e.mousePosition.y;
            _floatingLabel.CaptureMouse();
            e.StopPropagation();
        }

        void OnDragMove(MouseMoveEvent e)
        {
            if (!_dragging) return;
            float delta = _dragStartY - e.mousePosition.y;
            float sensitivity = _step;
            Value = _dragStartValue + delta * sensitivity;
        }

        void OnDragEnd(MouseUpEvent e)
        {
            if (!_dragging) return;
            _dragging = false;
            _floatingLabel.ReleaseMouse();
        }

        void CommitInput()
        {
            if (float.TryParse(_input.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                Value = parsed;
            else
                SyncInputText();
        }

        void SyncInputText()
        {
            _input.SetValueWithoutNotify(FormatValue(_value));
        }

        string FormatValue(float v)
        {
            return _intMode ? Mathf.RoundToInt(v).ToString() : v.ToString(_format, CultureInfo.InvariantCulture);
        }

        float Clamp(float v)
        {
            v = Mathf.Clamp(v, _min, _max);
            if (_step > 0 && _step < (_max - _min))
            {
                float offset = v - _min;
                v = _min + Mathf.Round(offset / _step) * _step;
                v = Mathf.Clamp(v, _min, _max);
            }
            if (_intMode) v = Mathf.Round(v);
            return v;
        }

        static string DetermineFormat(float step)
        {
            if (step >= 1f) return "0";
            if (step >= 0.1f) return "0.0";
            if (step >= 0.01f) return "0.00";
            return "0.000";
        }

        // ── Theme ──

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color borderColor, labelColor;

            if (_focused)
            {
                borderColor = _theme.Primary;
                labelColor = _theme.Primary;
                _container.style.borderTopWidth = 2;
                _container.style.borderBottomWidth = 2;
                _container.style.borderLeftWidth = 2;
                _container.style.borderRightWidth = 2;
            }
            else if (_hovered)
            {
                borderColor = _theme.OnSurface;
                labelColor = _theme.OnSurfaceVariant;
                _container.style.borderTopWidth = 1;
                _container.style.borderBottomWidth = 1;
                _container.style.borderLeftWidth = 1;
                _container.style.borderRightWidth = 1;
            }
            else
            {
                borderColor = _theme.Outline;
                labelColor = _theme.OnSurfaceVariant;
                _container.style.borderTopWidth = 1;
                _container.style.borderBottomWidth = 1;
                _container.style.borderLeftWidth = 1;
                _container.style.borderRightWidth = 1;
            }

            _container.style.borderTopColor = borderColor;
            _container.style.borderBottomColor = borderColor;
            _container.style.borderLeftColor = borderColor;
            _container.style.borderRightColor = borderColor;
            _container.style.backgroundColor = Color.clear;
            _floatingLabel.style.color = labelColor;
            _input.style.color = _theme.OnSurface;

            var innerInput = _input.Q(className: "unity-text-field__input");
            if (innerInput != null) innerInput.style.color = _theme.OnSurface;

            // Floating label notch background
            var bg = _theme.Surface;
            var el = this.parent;
            while (el != null)
            {
                var resolved = el.resolvedStyle.backgroundColor;
                if (resolved.a > 0.01f) { bg = resolved; break; }
                el = el.parent;
            }
            _floatingLabel.style.backgroundColor = bg;

            // Step button icon colors
            SetStepButtonColor(_decrementBtn);
            SetStepButtonColor(_incrementBtn);
        }

        void SetStepButtonColor(VisualElement btn)
        {
            var icon = btn.Q<Label>();
            if (icon != null) icon.style.color = _theme.OnSurfaceVariant;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
