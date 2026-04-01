using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Slider : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Slider, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<float> changed;

        readonly VisualElement _track;
        readonly VisualElement _fill;
        readonly VisualElement _thumb;
        readonly VisualElement _popup;
        readonly Label _popupLabel;
        readonly List<VisualElement> _ticks = new List<VisualElement>();
        MD3Theme _theme;
        bool _hovered;
        bool _dragging;
        MD3AnimationHandle _popupAnim;

        readonly float _min;
        readonly float _max;
        readonly float _step;
        readonly string _format;

        public float Value
        {
            get => _value;
            set
            {
                var clamped = SnapToStep(Mathf.Clamp(value, _min, _max));
                if (Mathf.Approximately(_value, clamped)) return;
                _value = clamped;
                UpdateVisual();
                changed?.Invoke(_value);
            }
        }
        float _value;

        public MD3Slider() : this(0f, 0f, 1f) { }

        public MD3Slider(float value, float min = 0f, float max = 1f, float step = 0f)
        {
            _min = min;
            _max = max;
            _step = step;
            _format = DetermineFormat(step);
            _value = SnapToStep(Mathf.Clamp(value, min, max));

            AddToClassList("md3-slider");

            _track = new VisualElement();
            _track.AddToClassList("md3-slider__track");
            _track.pickingMode = PickingMode.Ignore;
            Add(_track);

            _fill = new VisualElement();
            _fill.AddToClassList("md3-slider__fill");
            _fill.pickingMode = PickingMode.Ignore;
            _track.Add(_fill);

            // Tick marks at each step position
            if (_step > 0f && _max > _min)
            {
                int tickCount = Mathf.RoundToInt((_max - _min) / _step) + 1;
                for (int i = 0; i < tickCount; i++)
                {
                    var tick = new VisualElement();
                    tick.AddToClassList("md3-slider__tick");
                    tick.pickingMode = PickingMode.Ignore;
                    _ticks.Add(tick);
                    Add(tick);
                }
            }

            _thumb = new VisualElement();
            _thumb.AddToClassList("md3-slider__thumb");
            _thumb.pickingMode = PickingMode.Ignore;
            Add(_thumb);

            // Value popup
            _popup = new VisualElement();
            _popup.AddToClassList("md3-slider__popup");
            _popup.pickingMode = PickingMode.Ignore;
            _popup.style.opacity = 0f;
            _popup.style.scale = new Scale(new Vector3(0f, 0f, 1f));
            _popup.style.display = DisplayStyle.None;
            Add(_popup);

            _popupLabel = new Label();
            _popupLabel.AddToClassList("md3-slider__popup-label");
            _popupLabel.pickingMode = PickingMode.Ignore;
            _popup.Add(_popupLabel);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (e.oldRect.size != e.newRect.size) UpdateVisual();
            });
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            _theme = ResolveTheme();
            UpdateVisual();
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;
            _dragging = true;
            this.CaptureMouse();
            SetValueFromMouse(evt.localMousePosition.x);
            ShowPopup();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_dragging) return;
            SetValueFromMouse(evt.localMousePosition.x);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!_dragging) return;
            _dragging = false;
            this.ReleaseMouse();
            HidePopup();
        }

        void ShowPopup()
        {
            _popupAnim?.Cancel();
            _popup.style.display = DisplayStyle.Flex;
            _popupAnim = MD3Animate.FadeScale(_popup, 0f, 1f, 0f, 1f, 150f, MD3Easing.EaseOut);
        }

        void HidePopup()
        {
            _popupAnim?.Cancel();
            _popupAnim = MD3Animate.FadeScale(_popup, 1f, 0f, 1f, 0f, 100f, MD3Easing.EaseIn,
                () => _popup.style.display = DisplayStyle.None);
        }

        float SnapToStep(float val)
        {
            if (_step <= 0f) return val;
            return Mathf.Round((val - _min) / _step) * _step + _min;
        }

        void SetValueFromMouse(float localX)
        {
            float thumbHalf = 10f;
            float trackWidth = resolvedStyle.width - thumbHalf * 2;
            if (trackWidth <= 0) return;

            float ratio = Mathf.Clamp01((localX - thumbHalf) / trackWidth);
            float newValue = SnapToStep(Mathf.Lerp(_min, _max, ratio));

            if (!Mathf.Approximately(_value, newValue))
            {
                _value = newValue;
                UpdateVisual();
                changed?.Invoke(_value);
            }
            else
            {
                UpdatePopupPosition();
            }
        }

        void UpdateVisual()
        {
            float ratio = (_max > _min) ? Mathf.Clamp01((_value - _min) / (_max - _min)) : 0f;
            float trackWidth = resolvedStyle.width;
            if (float.IsNaN(trackWidth) || trackWidth <= 0) return;

            float thumbHalf = 10f;
            float usable = trackWidth - thumbHalf * 2;
            float thumbLeft = thumbHalf + usable * ratio - thumbHalf;

            _fill.style.width = Length.Percent(ratio * 100f);
            _thumb.style.left = thumbLeft;

            // Position tick marks
            if (_ticks.Count > 1)
            {
                for (int i = 0; i < _ticks.Count; i++)
                {
                    float tickRatio = (float)i / (_ticks.Count - 1);
                    float tickLeft = thumbHalf + usable * tickRatio - 2f;
                    _ticks[i].style.left = tickLeft;
                }
            }

            UpdatePopupPosition();
            ApplyColors();
        }

        void UpdatePopupPosition()
        {
            float ratio = (_max > _min) ? Mathf.Clamp01((_value - _min) / (_max - _min)) : 0f;
            float trackWidth = resolvedStyle.width;
            if (float.IsNaN(trackWidth) || trackWidth <= 0) return;

            float thumbHalf = 10f;
            float usable = trackWidth - thumbHalf * 2;
            float thumbCenter = thumbHalf + usable * ratio;

            _popup.style.left = thumbCenter - 14f;
            _popupLabel.text = _value.ToString(_format);
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            var trackBg = _theme.SurfaceVariant;
            var fillBg = _theme.Primary;
            var thumbBg = _theme.Primary;

            if (_hovered || _dragging)
                thumbBg = _theme.HoverOverlay(thumbBg, _theme.OnPrimary);

            _track.style.backgroundColor = trackBg;
            _fill.style.backgroundColor = fillBg;
            _thumb.style.backgroundColor = thumbBg;

            _popup.style.backgroundColor = _theme.InverseSurface;
            _popupLabel.style.color = _theme.InverseOnSurface;

            // Tick colors: active side contrasts with fill, inactive side contrasts with track
            if (_ticks.Count > 1)
            {
                float valueRatio = (_max > _min) ? Mathf.Clamp01((_value - _min) / (_max - _min)) : 0f;
                for (int i = 0; i < _ticks.Count; i++)
                {
                    float tickRatio = (float)i / (_ticks.Count - 1);
                    _ticks[i].style.backgroundColor = tickRatio <= valueRatio
                        ? _theme.OnPrimary
                        : _theme.OnSurfaceVariant;
                }
            }
        }

        static string DetermineFormat(float step)
        {
            if (step <= 0f) return "F2";
            // Count decimal places in step
            string s = step.ToString("G");
            int dotIndex = s.IndexOf('.');
            if (dotIndex < 0) return "F0";
            return "F" + (s.Length - dotIndex - 1);
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
