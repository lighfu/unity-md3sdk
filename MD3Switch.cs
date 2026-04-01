using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Switch : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Switch, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly VisualElement _thumb;
        readonly Label _checkIcon;
        MD3Theme _theme;
        bool _hovered;
        MD3AnimationHandle _thumbAnim;
        MD3AnimationHandle _sizeAnim;
        MD3AnimationHandle _checkAnim;

        // Thumb geometry
        const float TrackWidth = 52f;
        const float TrackHeight = 32f;
        const float BorderWidth = 2f;
        const float ThumbOffSize = 16f;
        const float ThumbOnSize = 24f;
        const float ThumbOffLeft = 6f;   // (32 - 16) / 2 - 2(border) = 6
        const float ThumbOnLeft = 24f;   // 52 - 24 - 2(border) - 2(padding) = 24
        const float ThumbOffTop = 6f;
        const float ThumbOnTop = 2f;

        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                UpdateVisual(true);
                changed?.Invoke(_value);
            }
        }
        bool _value;

        public MD3Switch() : this(false) { }

        public MD3Switch(bool value)
        {
            _value = value;
            AddToClassList("md3-switch");

            _thumb = new VisualElement();
            _thumb.AddToClassList("md3-switch__thumb");
            _thumb.pickingMode = PickingMode.Ignore;
            Add(_thumb);

            // Check icon inside thumb
            _checkIcon = new Label(MD3Icon.Check);
            _checkIcon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_checkIcon, 16f);
            _checkIcon.style.opacity = 0f;
            _checkIcon.style.scale = new Scale(new Vector3(0.3f, 0.3f, 1f));
            _thumb.Add(_checkIcon);

            // Set initial state without animation
            if (_value)
            {
                _thumb.style.width = ThumbOnSize;
                _thumb.style.height = ThumbOnSize;
                _thumb.style.left = ThumbOnLeft;
                _thumb.style.top = ThumbOnTop;
                _checkIcon.style.opacity = 1f;
                _checkIcon.style.scale = new Scale(Vector3.one);
                AddToClassList("md3-switch--on");
            }
            else
            {
                _thumb.style.width = ThumbOffSize;
                _thumb.style.height = ThumbOffSize;
                _thumb.style.left = ThumbOffLeft;
                _thumb.style.top = ThumbOffTop;
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyColors(); });
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
            _value = !_value;
            UpdateVisual(true);
            changed?.Invoke(_value);
        }

        void UpdateVisual(bool animate)
        {
            if (_value)
                AddToClassList("md3-switch--on");
            else
                RemoveFromClassList("md3-switch--on");

            _thumbAnim?.Cancel();
            _sizeAnim?.Cancel();
            _checkAnim?.Cancel();

            if (!animate)
            {
                SetThumbState(_value);
                ApplyColors();
                return;
            }

            if (_value)
                AnimateOn();
            else
                AnimateOff();

            ApplyColors();
        }

        void SetThumbState(bool on)
        {
            if (on)
            {
                _thumb.style.left = ThumbOnLeft;
                _thumb.style.top = ThumbOnTop;
                _thumb.style.width = ThumbOnSize;
                _thumb.style.height = ThumbOnSize;
                _checkIcon.style.opacity = 1f;
                _checkIcon.style.scale = new Scale(Vector3.one);
                _checkIcon.style.rotate = new Rotate(0f);
            }
            else
            {
                _thumb.style.left = ThumbOffLeft;
                _thumb.style.top = ThumbOffTop;
                _thumb.style.width = ThumbOffSize;
                _thumb.style.height = ThumbOffSize;
                _checkIcon.style.opacity = 0f;
                _checkIcon.style.scale = new Scale(new Vector3(0.3f, 0.3f, 1f));
            }
        }

        void AnimateOn()
        {
            // Thumb position: spring bounce to the right
            _thumbAnim = MD3Animate.Spring(_thumb, ThumbOffLeft, ThumbOnLeft,
                v =>
                {
                    _thumb.style.left = v;
                    // Sync vertical position based on progress
                    float t = Mathf.InverseLerp(ThumbOffLeft, ThumbOnLeft, Mathf.Clamp(v, ThumbOffLeft, ThumbOnLeft));
                    _thumb.style.top = Mathf.Lerp(ThumbOffTop, ThumbOnTop, t);
                },
                stiffness: 500f, damping: 18f);

            // Thumb size: grow
            _sizeAnim = MD3Animate.Spring(_thumb, ThumbOffSize, ThumbOnSize,
                v =>
                {
                    _thumb.style.width = v;
                    _thumb.style.height = v;
                },
                stiffness: 400f, damping: 20f);

            // Check icon: pop in with rotation
            _checkIcon.style.opacity = 0f;
            _checkIcon.style.scale = new Scale(new Vector3(0.2f, 0.2f, 1f));
            _checkIcon.style.rotate = new Rotate(-90f);

            MD3Animate.Delayed(_thumb, 60f, () =>
            {
                _checkAnim = MD3Animate.Keyframes(_checkIcon, 250f, new[]
                {
                    new MD3Keyframe(0f,   0f),
                    new MD3Keyframe(0.3f, 1f, MD3Easing.EaseOutCubic),
                    new MD3Keyframe(1f,   1f),
                }, v => _checkIcon.style.opacity = v);

                MD3Animate.Keyframes(_checkIcon, 300f, new[]
                {
                    new MD3Keyframe(0f,   0.2f),
                    new MD3Keyframe(0.4f, 1.3f, MD3Easing.EaseOutCubic),
                    new MD3Keyframe(0.7f, 0.9f, MD3Easing.EaseInOut),
                    new MD3Keyframe(1f,   1f,   MD3Easing.EaseOut),
                }, v => _checkIcon.style.scale = new Scale(new Vector3(v, v, 1f)));

                MD3Animate.Float(_checkIcon, -90f, 0f, 250f, MD3Easing.EaseOutCubic,
                    v => _checkIcon.style.rotate = new Rotate(v));
            });
        }

        void AnimateOff()
        {
            // Check icon: shrink out first
            _checkAnim = MD3Animate.Keyframes(_checkIcon, 120f, new[]
            {
                new MD3Keyframe(0f,   1f),
                new MD3Keyframe(0.5f, 1.2f, MD3Easing.EaseOut),
                new MD3Keyframe(1f,   0f,   MD3Easing.EaseIn),
            }, v =>
            {
                _checkIcon.style.scale = new Scale(new Vector3(v, v, 1f));
                _checkIcon.style.opacity = v;
            });

            // Thumb position: spring bounce to the left
            _thumbAnim = MD3Animate.Spring(_thumb, ThumbOnLeft, ThumbOffLeft,
                v =>
                {
                    _thumb.style.left = v;
                    float t = Mathf.InverseLerp(ThumbOnLeft, ThumbOffLeft, Mathf.Clamp(v, ThumbOffLeft, ThumbOnLeft));
                    _thumb.style.top = Mathf.Lerp(ThumbOnTop, ThumbOffTop, t);
                },
                stiffness: 500f, damping: 18f);

            // Thumb size: shrink
            _sizeAnim = MD3Animate.Spring(_thumb, ThumbOnSize, ThumbOffSize,
                v =>
                {
                    _thumb.style.width = v;
                    _thumb.style.height = v;
                },
                stiffness: 400f, damping: 20f);
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color trackBg, trackBorder, thumbBg, checkColor;

            if (_value)
            {
                trackBg = _theme.Primary;
                trackBorder = _theme.Primary;
                thumbBg = _theme.OnPrimary;
                checkColor = _theme.Primary;
                if (_hovered)
                    trackBg = _theme.HoverOverlay(trackBg, _theme.OnPrimary);
            }
            else
            {
                trackBg = _theme.SurfaceVariant;
                trackBorder = _theme.Outline;
                thumbBg = _theme.Outline;
                checkColor = Color.clear;
                if (_hovered)
                    trackBg = _theme.HoverOverlay(trackBg, _theme.OnSurface);
            }

            style.backgroundColor = trackBg;
            style.borderTopColor = trackBorder;
            style.borderBottomColor = trackBorder;
            style.borderLeftColor = trackBorder;
            style.borderRightColor = trackBorder;
            _thumb.style.backgroundColor = thumbBg;
            _checkIcon.style.color = checkColor;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
