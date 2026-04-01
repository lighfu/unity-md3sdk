using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3CircularProgressStyle
    {
        Standard,
        /// <summary>Wavy/organic arc segments on track ring.</summary>
        Wavy,
        /// <summary>Wavy arc that fills 0→360° without rotation. Wave animates continuously.</summary>
        WavyFill,
    }

    public class MD3CircularProgress : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3CircularProgress, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        MD3Theme _theme;
        readonly float _size;
        readonly MD3CircularProgressStyle _circStyle;
        float _strokeWidth;
        bool _indeterminate;
        double _startTime;

        public float Value
        {
            get => _value;
            set { _value = Mathf.Clamp01(value); MarkDirtyRepaint(); }
        }
        float _value;

        /// <summary>Stroke width override. 0 = auto (size * 0.08).</summary>
        public float StrokeWidth
        {
            get => _strokeWidth;
            set { _strokeWidth = value; MarkDirtyRepaint(); }
        }

        public bool Indeterminate
        {
            get => _indeterminate;
            set
            {
                _indeterminate = value;
                if (_indeterminate)
                    StartAnim();
                else
                    StopAnim();
            }
        }

        public MD3CircularProgress() : this(0f) { }

        public MD3CircularProgress(float value = 0f, float size = 40f, bool indeterminate = false,
            MD3CircularProgressStyle circStyle = MD3CircularProgressStyle.Standard, float strokeWidth = 0f)
        {
            _value = Mathf.Clamp01(value);
            _size = size;
            _circStyle = circStyle;
            _strokeWidth = strokeWidth;
            _indeterminate = indeterminate;

            AddToClassList("md3-circular-progress");
            style.width = size;
            style.height = size;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            MarkDirtyRepaint();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
            if (_indeterminate || _circStyle == MD3CircularProgressStyle.Wavy
                || _circStyle == MD3CircularProgressStyle.WavyFill)
                StartAnim();
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            StopAnim();
        }

        void StartAnim()
        {
            StopAnim();
            _startTime = EditorApplication.timeSinceStartup;
            MD3AnimLoop.Register(this);
        }

        void StopAnim()
        {
            MD3AnimLoop.Unregister(this);
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            if (_circStyle == MD3CircularProgressStyle.WavyFill)
                DrawWavyFill(mgc);
            else if (_circStyle == MD3CircularProgressStyle.Wavy)
                DrawWavy(mgc);
            else
                DrawStandard(mgc);
        }

        void DrawStandard(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float radius = (_size - 4f) * 0.5f;
            float strokeWidth = _strokeWidth > 0f ? _strokeWidth : Mathf.Max(3f, _size * 0.08f);
            float gapDeg = 8f; // gap in degrees between active arc and track

            float fillStart, fillSweep;
            if (_indeterminate)
            {
                fillStart = (float)((EditorApplication.timeSinceStartup - _startTime) * 200.0 % 360.0);
                fillSweep = 270f;
            }
            else
            {
                fillStart = -90f;
                fillSweep = _value * 360f;
            }

            // Track: from end of active arc + gap to start of active arc - gap
            var trackColor = _theme.SurfaceVariant;
            trackColor.a *= 0.5f;
            painter.strokeColor = trackColor;
            painter.lineWidth = strokeWidth;
            painter.lineCap = LineCap.Round;

            float trackStart = fillStart + fillSweep + gapDeg;
            float trackEnd = fillStart + 360f - gapDeg;
            if (fillSweep > 0.5f && trackEnd - trackStart > 1f)
            {
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), radius, trackStart, trackEnd);
                painter.Stroke();
            }
            else if (fillSweep < 0.5f)
            {
                // No fill: draw full track
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), radius, 0f, 360f);
                painter.Stroke();
            }

            // Fill arc
            if (fillSweep > 0.5f)
            {
                painter.strokeColor = _theme.Primary;
                painter.lineWidth = strokeWidth;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), radius, fillStart, fillStart + fillSweep);
                painter.Stroke();
            }
        }

        void DrawWavyArc(Painter2D painter, float cx, float cy, float radius,
            float strokeWidth, float startAngle, float sweepAngle, float elapsed)
        {
            float waveAmp = _size * 0.04f;
            float waveFreq = 6f;
            float waveSpeed = 8f;
            int segments = Mathf.Max(30, (int)(sweepAngle / 3f));

            painter.strokeColor = _theme.Primary;
            painter.lineWidth = strokeWidth;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angleDeg = startAngle + t * sweepAngle;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                // Fade wave at both ends for round cap visibility
                float edgeFade = Mathf.Clamp01(Mathf.Min(t / 0.1f, (1f - t) / 0.1f));
                float waveOffset = Mathf.Sin(angleDeg * Mathf.Deg2Rad * waveFreq - elapsed * waveSpeed) * waveAmp * edgeFade;
                float r = radius + waveOffset;

                float x = cx + Mathf.Cos(angleRad) * r;
                float y = cy + Mathf.Sin(angleRad) * r;

                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();
        }

        void DrawGappedTrack(Painter2D painter, float cx, float cy, float radius,
            float strokeWidth, float fillStart, float fillSweep)
        {
            float gapDeg = 8f;
            var trackColor = _theme.SurfaceVariant;
            trackColor.a *= 0.5f;
            painter.strokeColor = trackColor;
            painter.lineWidth = strokeWidth;
            painter.lineCap = LineCap.Round;

            if (fillSweep < 0.5f)
            {
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), radius, 0f, 360f);
                painter.Stroke();
                return;
            }

            float trackStart = fillStart + fillSweep + gapDeg;
            float trackEnd = fillStart + 360f - gapDeg;
            if (trackEnd - trackStart > 1f)
            {
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), radius, trackStart, trackEnd);
                painter.Stroke();
            }
        }

        void DrawWavy(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float radius = (_size - 6f) * 0.5f;
            float strokeWidth = _strokeWidth > 0f ? _strokeWidth : Mathf.Max(3f, _size * 0.08f);
            float elapsed = (float)(EditorApplication.timeSinceStartup - _startTime);

            float startAngle, sweepAngle;
            if (_indeterminate)
            {
                startAngle = elapsed * 200f % 360f;
                sweepAngle = 240f;
            }
            else
            {
                startAngle = -90f;
                sweepAngle = _value * 360f;
            }

            DrawGappedTrack(painter, cx, cy, radius, strokeWidth, startAngle, sweepAngle);

            if (sweepAngle > 0.5f)
                DrawWavyArc(painter, cx, cy, radius, strokeWidth, startAngle, sweepAngle, elapsed);
        }

        void DrawWavyFill(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float radius = (_size - 6f) * 0.5f;
            float strokeWidth = _strokeWidth > 0f ? _strokeWidth : Mathf.Max(3f, _size * 0.08f);
            float elapsed = (float)(EditorApplication.timeSinceStartup - _startTime);

            float sweepAngle = _value * 360f;

            DrawGappedTrack(painter, cx, cy, radius, strokeWidth, -90f, sweepAngle);

            if (sweepAngle > 0.5f)
                DrawWavyArc(painter, cx, cy, radius, strokeWidth, -90f, sweepAngle, elapsed);
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
