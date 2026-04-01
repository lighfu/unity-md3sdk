using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3LinearProgressStyle
    {
        /// <summary>Standard bar fill / sliding bar indeterminate.</summary>
        Standard,
        /// <summary>Sine wave flowing along the track.</summary>
        SineWave,
        /// <summary>Track with active bar + stop dot (MD3 spec style).</summary>
        TrackBar,
        /// <summary>Track with wavy/snake active segment + stop dot.</summary>
        TrackWave,
    }

    public class MD3LinearProgress : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3LinearProgress, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly VisualElement _fill;
        readonly MD3LinearProgressStyle _progressStyle;
        MD3Theme _theme;
        float _customStrokeWidth;
        bool _indeterminate;
        double _animStartTime;

        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                if (!_indeterminate && _progressStyle == MD3LinearProgressStyle.Standard)
                    _fill.style.width = new Length(_value * 100f, LengthUnit.Percent);
                if (_progressStyle != MD3LinearProgressStyle.Standard)
                {
                    // 補間アニメーション開始
                    if (!_lerpRunning && Mathf.Abs(_displayValue - _value) > 0.001f)
                        StartLerpAnim();
                    MarkDirtyRepaint();
                }
            }
        }
        float _value;
        float _displayValue;
        bool _lerpRunning;
        IVisualElementScheduledItem _lerpSchedule;

        void StartLerpAnim()
        {
            if (_lerpRunning) return;
            _lerpRunning = true;
            _lerpSchedule = schedule.Execute(() =>
            {
                float diff = _value - _displayValue;
                if (Mathf.Abs(diff) < 0.002f)
                {
                    _displayValue = _value;
                    _lerpRunning = false;
                    _lerpSchedule?.Pause();
                    _lerpSchedule = null;
                }
                else
                {
                    // 速度: 差分に比例 + 最低速度保証でスムーズに追従
                    float speed = Mathf.Max(Mathf.Abs(diff) * 4f, 0.02f);
                    _displayValue += Mathf.Sign(diff) * Mathf.Min(speed * 0.016f, Mathf.Abs(diff));
                }
                MarkDirtyRepaint();
            }).Every(16); // ~60fps
        }

        /// <summary>Stroke/bar width override for TrackWave/TrackBar. 0 = auto.</summary>
        public float StrokeWidth
        {
            get => _customStrokeWidth;
            set { _customStrokeWidth = value; MarkDirtyRepaint(); }
        }

        public bool Indeterminate
        {
            get => _indeterminate;
            set
            {
                _indeterminate = value;
                if (_indeterminate)
                    StartIndeterminateAnim();
                else
                    StopIndeterminateAnim();
            }
        }

        public MD3LinearProgress() : this(0f) { }

        public MD3LinearProgress(float value = 0f, bool indeterminate = false,
            MD3LinearProgressStyle progressStyle = MD3LinearProgressStyle.Standard)
        {
            _value = Mathf.Clamp01(value);
            _displayValue = _value;
            _indeterminate = indeterminate;
            _progressStyle = progressStyle;

            AddToClassList("md3-linear-progress");

            _fill = new VisualElement();
            _fill.AddToClassList("md3-linear-progress__fill");
            _fill.pickingMode = PickingMode.Ignore;

            if (_progressStyle != MD3LinearProgressStyle.Standard)
            {
                // Non-standard styles draw via generateVisualContent, hide the fill bar
                _fill.style.display = DisplayStyle.None;
            }
            else if (!indeterminate)
            {
                _fill.style.width = new Length(_value * 100f, LengthUnit.Percent);
            }
            Add(_fill);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
            if (_indeterminate || _progressStyle != MD3LinearProgressStyle.Standard)
                StartIndeterminateAnim();
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            StopIndeterminateAnim();
        }

        void StartIndeterminateAnim()
        {
            StopIndeterminateAnim();
            _animStartTime = EditorApplication.timeSinceStartup;
            MD3AnimLoop.Register(this);

            if (_progressStyle == MD3LinearProgressStyle.SineWave)
                generateVisualContent += OnSineWaveRepaint;
            else if (_progressStyle == MD3LinearProgressStyle.TrackBar)
                generateVisualContent += OnTrackBarRepaint;
            else if (_progressStyle == MD3LinearProgressStyle.TrackWave)
                generateVisualContent += OnTrackWaveRepaint;
            else
                generateVisualContent += OnIndeterminateRepaint;
        }

        void StopIndeterminateAnim()
        {
            MD3AnimLoop.Unregister(this);
            generateVisualContent -= OnIndeterminateRepaint;
            generateVisualContent -= OnSineWaveRepaint;
            generateVisualContent -= OnTrackBarRepaint;
            generateVisualContent -= OnTrackWaveRepaint;

            if (_progressStyle == MD3LinearProgressStyle.Standard)
            {
                _fill.style.left = 0;
                _fill.style.width = new Length(_value * 100f, LengthUnit.Percent);
            }
        }

        void OnIndeterminateRepaint(MeshGenerationContext mgc)
        {
            float elapsed = (float)(EditorApplication.timeSinceStartup - _animStartTime);
            float animOffset = (elapsed * 1.5f) % 2f;
            float left = (animOffset - 0.3f) * 100f;
            _fill.style.left = new Length(Mathf.Max(0f, left), LengthUnit.Percent);
            _fill.style.width = new Length(
                Mathf.Clamp(30f, 0f, 100f - Mathf.Max(0f, left)), LengthUnit.Percent);
        }

        void OnSineWaveRepaint(MeshGenerationContext mgc)
        {
            if (_theme == null) return;
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            if (float.IsNaN(w) || w <= 0) return;

            float elapsed = (float)(EditorApplication.timeSinceStartup - _animStartTime);
            float speed = 3f;
            float cy = h * 0.5f;
            float amplitude = h * 0.35f;
            int segments = Mathf.Max(20, (int)(w / 4f));

            var painter = mgc.painter2D;

            // Track background
            painter.fillColor = _theme.SurfaceVariant;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(w, 0));
            painter.LineTo(new Vector2(w, h));
            painter.LineTo(new Vector2(0, h));
            painter.ClosePath();
            painter.Fill();

            // Primary wave — filled from wave to bottom
            painter.fillColor = _theme.Primary;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h));
            for (int i = 0; i <= segments; i++)
            {
                float x = (float)i / segments * w;
                float phase = x / w * Mathf.PI * 2.5f - elapsed * speed;
                float y = cy + Mathf.Sin(phase) * amplitude;
                painter.LineTo(new Vector2(x, y));
            }
            painter.LineTo(new Vector2(w, h));
            painter.ClosePath();
            painter.Fill();

            // Secondary wave — lighter overlay
            var secColor = _theme.PrimaryContainer;
            secColor.a *= 0.5f;
            painter.fillColor = secColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h));
            for (int i = 0; i <= segments; i++)
            {
                float x = (float)i / segments * w;
                float phase = x / w * Mathf.PI * 2f - elapsed * speed * 0.7f + 1.2f;
                float y = cy + Mathf.Sin(phase) * amplitude * 0.6f + h * 0.1f;
                painter.LineTo(new Vector2(x, y));
            }
            painter.LineTo(new Vector2(w, h));
            painter.ClosePath();
            painter.Fill();

            // Determinate mode: progress overlay mask (clear right portion)
            if (!_indeterminate && _value < 1f)
            {
                float cutX = w * _value;
                painter.fillColor = _theme.SurfaceVariant;
                painter.BeginPath();
                painter.MoveTo(new Vector2(cutX, 0));
                painter.LineTo(new Vector2(w, 0));
                painter.LineTo(new Vector2(w, h));
                painter.LineTo(new Vector2(cutX, h));
                painter.ClosePath();
                painter.Fill();
            }
        }

        void OnTrackBarRepaint(MeshGenerationContext mgc)
        {
            if (_theme == null) return;
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            if (float.IsNaN(w) || w <= 0) return;

            float elapsed = (float)(EditorApplication.timeSinceStartup - _animStartTime);
            var painter = mgc.painter2D;
            float cy = h * 0.5f;
            float trackH = Mathf.Max(2f, h * 0.25f);
            float barH = _customStrokeWidth > 0f ? _customStrokeWidth : h;
            float dotR = h * 0.2f;
            float r = barH * 0.5f;

            // Track line
            painter.fillColor = _theme.SurfaceVariant;
            DrawRoundedRect(painter, 0, cy - trackH * 0.5f, w, trackH, trackH * 0.5f);

            if (_indeterminate)
            {
                // Sliding bar
                float period = 2f;
                float t = (elapsed * 1.2f) % period / period;
                float barW = w * 0.25f;
                float eased = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                float barX = eased * (w - barW);

                painter.fillColor = _theme.Primary;
                DrawRoundedRect(painter, barX, cy - barH * 0.5f, barW, barH, r);
            }
            else
            {
                // Determinate fill
                float fillW = w * _value;
                if (fillW > 1f)
                {
                    painter.fillColor = _theme.Primary;
                    DrawRoundedRect(painter, 0, cy - barH * 0.5f, fillW, barH, r);
                }
            }

            // Stop dot at right end
            painter.fillColor = _theme.Primary;
            painter.BeginPath();
            painter.Arc(new Vector2(w - dotR, cy), dotR, 0f, 360f);
            painter.Fill();
        }

        void OnTrackWaveRepaint(MeshGenerationContext mgc)
        {
            if (_theme == null) return;
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            if (float.IsNaN(w) || w <= 0) return;

            float elapsed = (float)(EditorApplication.timeSinceStartup - _animStartTime);
            var painter = mgc.painter2D;
            float strokeW = _customStrokeWidth > 0f ? _customStrokeWidth : Mathf.Max(3f, h * 0.4f);
            float cy = h * 0.5f;
            float trackH = Mathf.Max(2f, h * 0.25f);
            float trackR = trackH * 0.5f;
            float dotR = Mathf.Max(2f, h * 0.12f);
            float gap = h * 0.4f;
            // Keep amplitude within bounds: half height minus half stroke
            float amplitude = (h - strokeW) * 0.4f;
            float speed = 5f;

            // Use interpolated display value for smooth animation
            float val = _indeterminate ? _value : _displayValue;

            // Flatten wave as value approaches 100%
            // Amplitude scales down from 80% onwards, fully flat at 100%
            float flattenT = _indeterminate ? 0f : Mathf.Clamp01((val - 0.8f) / 0.2f);
            amplitude *= (1f - flattenT);

            float waveEnd, waveStart;

            if (_indeterminate)
            {
                float period = 2.5f;
                float t = (elapsed * 1f) % period / period;
                float eased = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                float segW = w * 0.28f;
                waveStart = eased * (w - segW - gap - dotR * 2f);
                waveEnd = waveStart + segW;
            }
            else
            {
                waveStart = 0f;
                // Wave expands to full width as value increases
                float maxWaveW = Mathf.Lerp(w * 0.3f, w, Mathf.Clamp01(val / 0.8f));
                waveEnd = Mathf.Min(maxWaveW, w * val);
            }

            // 1. Remaining track (after gap, rounded pill) — hidden at 100%
            float trackStart = waveEnd + gap;
            float trackEnd = w - dotR * 3f;
            if (trackStart < trackEnd && val < 0.99f)
            {
                painter.fillColor = _theme.SurfaceVariant;
                DrawRoundedRect(painter, trackStart, cy - trackH * 0.5f,
                    trackEnd - trackStart, trackH, trackR);
            }

            // 2. Stop dot at right end — hidden at 100%
            if (val < 0.99f || _indeterminate)
            {
                painter.fillColor = _theme.Primary;
                painter.BeginPath();
                painter.Arc(new Vector2(w - dotR * 1.5f, cy), dotR, 0f, 360f);
                painter.Fill();
            }

            // 3. Wavy active segment (thick, round cap)
            // At 100% and flat, draw as a rounded rect bar instead
            if (flattenT >= 0.99f)
            {
                painter.fillColor = _theme.Primary;
                DrawRoundedRect(painter, 0, cy - strokeW * 0.5f, w, strokeW, strokeW * 0.5f);
                return;
            }

            int segs = Mathf.Max(16, (int)((waveEnd - waveStart) / 2f));
            if (segs < 2) return;

            painter.strokeColor = _theme.Primary;
            painter.lineWidth = strokeW;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();

            float waveLen = 50f; // pixels per full wave cycle (fixed, not dependent on width)
            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                float x = Mathf.Lerp(waveStart, waveEnd, t);
                float phase = x / waveLen * Mathf.PI * 2f - elapsed * speed;
                // Fade only at the start (t=0), not at the end
                float edgeFade = Mathf.Clamp01(t / 0.15f);
                float y = cy + Mathf.Sin(phase) * amplitude * edgeFade;

                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();
        }

        static void DrawRoundedRect(Painter2D painter, float x, float y, float w, float h, float r)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(x + r, y));
            painter.LineTo(new Vector2(x + w - r, y));
            painter.Arc(new Vector2(x + w - r, y + r), r, -90f, 0f);
            painter.LineTo(new Vector2(x + w, y + h - r));
            painter.Arc(new Vector2(x + w - r, y + h - r), r, 0f, 90f);
            painter.LineTo(new Vector2(x + r, y + h));
            painter.Arc(new Vector2(x + r, y + h - r), r, 90f, 180f);
            painter.LineTo(new Vector2(x, y + r));
            painter.Arc(new Vector2(x + r, y + r), r, 180f, 270f);
            painter.ClosePath();
            painter.Fill();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            var trackColor = _theme.SurfaceVariant;
            if (_progressStyle != MD3LinearProgressStyle.Standard)
                style.backgroundColor = Color.clear; // custom styles draw their own background
            else
                style.backgroundColor = trackColor;
            _fill.style.backgroundColor = _theme.Primary;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
