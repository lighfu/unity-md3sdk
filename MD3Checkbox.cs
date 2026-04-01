using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Checkbox : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Checkbox, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly VisualElement _box;
        readonly Label _check;
        readonly Label _label;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;
        MD3AnimationHandle _checkAnim;
        MD3AnimationHandle _boxAnim;

        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                UpdateVisual(animate: true);
                changed?.Invoke(_value);
            }
        }
        bool _value;

        public bool IsDisabled
        {
            get => _disabled;
            set { _disabled = value; SetEnabled(!value); ApplyColors(); }
        }
        bool _disabled;

        public MD3Checkbox() : this(false) { }

        public MD3Checkbox(bool value, string label = null)
        {
            _value = value;
            AddToClassList("md3-checkbox");

            if (label != null)
            {
                style.width = StyleKeyword.Auto;
                style.height = StyleKeyword.Auto;
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
            }

            _box = new VisualElement();
            _box.AddToClassList("md3-checkbox__box");
            _box.pickingMode = PickingMode.Ignore;
            Add(_box);

            _check = new Label(MD3Icon.Check);
            _check.AddToClassList("md3-checkbox__check");
            _check.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_check, 14f);
            _box.Add(_check);

            if (label != null)
            {
                _label = new Label(label);
                _label.AddToClassList("md3-checkbox__label");
                _label.pickingMode = PickingMode.Ignore;
                Add(_label);
            }

            // Set initial state without animation
            if (_value)
            {
                _check.style.opacity = 1f;
                _check.style.scale = new Scale(Vector3.one);
                _check.style.display = DisplayStyle.Flex;
            }
            else
            {
                _check.style.opacity = 0f;
                _check.style.scale = new Scale(new Vector3(0.5f, 0.5f, 1f));
                _check.style.display = DisplayStyle.None;
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(_ => { _pressed = true; ApplyColors(); });
            RegisterCallback<MouseUpEvent>(_ => { _pressed = false; ApplyColors(); });
            RegisterCallback<ClickEvent>(OnClick);
        }

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

        void OnClick(ClickEvent evt)
        {
            if (_disabled) return;
            _value = !_value;
            UpdateVisual(animate: true);
            changed?.Invoke(_value);
        }

        void UpdateVisual(bool animate)
        {
            if (_value)
                AddToClassList("md3-checkbox--checked");
            else
                RemoveFromClassList("md3-checkbox--checked");

            _checkAnim?.Cancel();
            _boxAnim?.Cancel();

            if (animate)
            {
                if (_value)
                {
                    // ── Check ON: box squash → bounce + checkmark overshoot pop + rotate ──

                    // Box: squash down to 0.85 then spring back with overshoot
                    _boxAnim = MD3Animate.Float(this, 0f, 1f, 350f, MD3Easing.Linear, t =>
                    {
                        float scale;
                        if (t < 0.25f)
                        {
                            // Squash: 1.0 → 0.85
                            scale = Mathf.Lerp(1f, 0.85f, t / 0.25f);
                        }
                        else if (t < 0.55f)
                        {
                            // Overshoot: 0.85 → 1.1
                            float p = (t - 0.25f) / 0.3f;
                            scale = Mathf.Lerp(0.85f, 1.1f, p);
                        }
                        else
                        {
                            // Settle: 1.1 → 1.0
                            float p = (t - 0.55f) / 0.45f;
                            scale = Mathf.Lerp(1.1f, 1f, p);
                        }
                        _box.style.scale = new Scale(new Vector3(scale, scale, 1f));
                    });

                    // Checkmark: delayed pop with overshoot + rotation
                    _check.style.display = DisplayStyle.Flex;
                    _check.style.opacity = 0f;
                    _check.style.scale = new Scale(new Vector3(0.2f, 0.2f, 1f));
                    _check.style.rotate = new Rotate(-30f);

                    MD3Animate.Delayed(this, 80f, () =>
                    {
                        _checkAnim = MD3Animate.Float(this, 0f, 1f, 300f, MD3Easing.Linear, t =>
                        {
                            // Opacity: quick fade in
                            float opacity = Mathf.Clamp01(t * 3f);
                            _check.style.opacity = opacity;

                            // Scale: overshoot spring  0.2 → 1.3 → 0.9 → 1.0
                            float scale;
                            if (t < 0.4f)
                            {
                                scale = Mathf.Lerp(0.2f, 1.3f, t / 0.4f);
                            }
                            else if (t < 0.7f)
                            {
                                float p = (t - 0.4f) / 0.3f;
                                scale = Mathf.Lerp(1.3f, 0.9f, p);
                            }
                            else
                            {
                                float p = (t - 0.7f) / 0.3f;
                                scale = Mathf.Lerp(0.9f, 1f, p);
                            }
                            _check.style.scale = new Scale(new Vector3(scale, scale, 1f));

                            // Rotation: -30° → 5° → 0°
                            float rot;
                            if (t < 0.5f)
                                rot = Mathf.Lerp(-30f, 5f, t / 0.5f);
                            else
                                rot = Mathf.Lerp(5f, 0f, (t - 0.5f) / 0.5f);
                            _check.style.rotate = new Rotate(rot);
                        });
                    });
                }
                else
                {
                    // ── Check OFF: shrink + spin out + box pulse ──

                    // Box: subtle pulse out
                    _boxAnim = MD3Animate.Float(this, 0f, 1f, 250f, MD3Easing.Linear, t =>
                    {
                        float scale;
                        if (t < 0.4f)
                            scale = Mathf.Lerp(1f, 1.08f, t / 0.4f);
                        else
                            scale = Mathf.Lerp(1.08f, 1f, (t - 0.4f) / 0.6f);
                        _box.style.scale = new Scale(new Vector3(scale, scale, 1f));
                    });

                    // Checkmark: shrink + rotate out
                    _checkAnim = MD3Animate.Float(this, 0f, 1f, 200f, MD3Easing.EaseIn, t =>
                    {
                        float opacity = 1f - t;
                        float scale = Mathf.Lerp(1f, 0.2f, t);
                        float rot = Mathf.Lerp(0f, 20f, t);
                        _check.style.opacity = opacity;
                        _check.style.scale = new Scale(new Vector3(scale, scale, 1f));
                        _check.style.rotate = new Rotate(rot);
                    }, () =>
                    {
                        _check.style.display = DisplayStyle.None;
                        _check.style.rotate = new Rotate(0f);
                    });
                }
            }
            else
            {
                _box.style.scale = new Scale(Vector3.one);
                if (_value)
                {
                    _check.style.opacity = 1f;
                    _check.style.scale = new Scale(Vector3.one);
                    _check.style.rotate = new Rotate(0f);
                    _check.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _check.style.opacity = 0f;
                    _check.style.scale = new Scale(new Vector3(0.5f, 0.5f, 1f));
                    _check.style.rotate = new Rotate(0f);
                    _check.style.display = DisplayStyle.None;
                }
            }

            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color boxBg, boxBorder, checkColor;

            if (_value)
            {
                boxBg = _theme.Primary;
                boxBorder = _theme.Primary;
                checkColor = _theme.OnPrimary;
            }
            else
            {
                boxBg = Color.clear;
                boxBorder = _theme.OnSurfaceVariant;
                checkColor = Color.clear;
            }

            if (_disabled)
            {
                boxBg = _value ? _theme.Disabled(_theme.OnSurface) : Color.clear;
                boxBorder = _theme.Disabled(_theme.OnSurface);
                checkColor = _value ? _theme.Disabled(_theme.Surface) : Color.clear;
            }
            else if (_pressed)
            {
                boxBg = _value
                    ? _theme.PressOverlay(boxBg, _theme.OnPrimary)
                    : _theme.PressOverlay(Color.clear, _theme.OnSurface);
            }
            else if (_hovered)
            {
                boxBg = _value
                    ? _theme.HoverOverlay(boxBg, _theme.OnPrimary)
                    : _theme.HoverOverlay(Color.clear, _theme.OnSurface);
            }

            _box.style.backgroundColor = boxBg;
            _box.style.borderTopColor = boxBorder;
            _box.style.borderBottomColor = boxBorder;
            _box.style.borderLeftColor = boxBorder;
            _box.style.borderRightColor = boxBorder;
            _check.style.color = checkColor;

            if (_label != null)
                _label.style.color = _disabled ? _theme.Disabled(_theme.OnSurface) : _theme.OnSurface;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
