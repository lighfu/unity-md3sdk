using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3LoadingStyle
    {
        Expressive,
        ExpressiveRinged,
        BouncingDots,
        PulseRing,
        WaveBars,
        RotatingDots,
        SineWave,
        DnaHelix,
        Orbit,
        Ripple,
        TypingDots,
    }

    public class MD3Loading : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Loading, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        MD3Theme _theme;
        readonly MD3LoadingStyle _loadingStyle;
        readonly float _size;
        double _startTime;

        public MD3Loading() : this(MD3LoadingStyle.Expressive) { }

        public MD3Loading(MD3LoadingStyle loadingStyle = MD3LoadingStyle.Expressive, float size = 48f)
        {
            _loadingStyle = loadingStyle;
            _size = size;

            AddToClassList("md3-loading");
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
            _startTime = EditorApplication.timeSinceStartup;
            MD3AnimLoop.Register(this);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            MD3AnimLoop.Unregister(this);
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            double elapsed = EditorApplication.timeSinceStartup - _startTime;
            var painter = mgc.painter2D;

            switch (_loadingStyle)
            {
                case MD3LoadingStyle.Expressive:       DrawExpressive(painter, elapsed); break;
                case MD3LoadingStyle.ExpressiveRinged: DrawExpressiveRinged(painter, elapsed); break;
                case MD3LoadingStyle.BouncingDots:   DrawBouncingDots(painter, elapsed); break;
                case MD3LoadingStyle.PulseRing:      DrawPulseRing(painter, elapsed); break;
                case MD3LoadingStyle.WaveBars:       DrawWaveBars(painter, elapsed); break;
                case MD3LoadingStyle.RotatingDots:   DrawRotatingDots(painter, elapsed); break;
                case MD3LoadingStyle.SineWave:       DrawSineWave(painter, elapsed); break;
                case MD3LoadingStyle.DnaHelix:       DrawDnaHelix(painter, elapsed); break;
                case MD3LoadingStyle.Orbit:          DrawOrbit(painter, elapsed); break;
                case MD3LoadingStyle.Ripple:         DrawRipple(painter, elapsed); break;
                case MD3LoadingStyle.TypingDots:     DrawTypingDots(painter, elapsed); break;
            }
        }

        // ─── Expressive: 9-shape morphing loader ───

        const float ShapeCycleMs = 650f;
        const float FullRotationMs = 4666f;
        const int ShapeCount = 8;
        const int SampleCount = 72;

        void DrawExpressive(Painter2D painter, double elapsed)
        {
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float scale = _size * 0.85f;

            float timeMs = (float)(elapsed * 1000.0);
            float cycleT = (timeMs % ShapeCycleMs) / ShapeCycleMs;
            int shapeIdx = (int)(timeMs / ShapeCycleMs) % ShapeCount;
            int nextIdx = (shapeIdx + 1) % ShapeCount;

            float morphT = SpringEase(cycleT);
            float globalAngle = (timeMs % FullRotationMs) / FullRotationMs * 360f;
            float perShapeRot = shapeIdx * 90f;
            float totalRot = (globalAngle + perShapeRot + morphT * 90f) * Mathf.Deg2Rad;

            // Bounce scale: shrink at morph start, overshoot, settle
            float bounceRaw = 1f - Mathf.Exp(-5f * cycleT) * Mathf.Cos(14f * cycleT);
            float bounceScale = 0.72f + 0.28f * bounceRaw;

            painter.fillColor = _theme.Primary;
            painter.BeginPath();

            for (int i = 0; i < SampleCount; i++)
            {
                float angle = (float)i / SampleCount * Mathf.PI * 2f;
                float r1 = ShapeRadius(shapeIdx, angle);
                float r2 = ShapeRadius(nextIdx, angle);
                float r = Mathf.Lerp(r1, r2, morphT) * scale * bounceScale;

                float rotAngle = angle + totalRot;
                float x = cx + Mathf.Cos(rotAngle) * r;
                float y = cy + Mathf.Sin(rotAngle) * r;

                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }
            painter.ClosePath();
            painter.Fill();
        }

        static float SpringEase(float t)
        {
            float zeta = 0.6f;
            float omega0 = 14.1421356f; // sqrt(200)
            float omegaD = omega0 * 0.8f;
            float decay = Mathf.Exp(-zeta * omega0 * t * 0.65f);
            return 1f - decay * (Mathf.Cos(omegaD * t * 0.65f)
                + (zeta * omega0 / omegaD) * Mathf.Sin(omegaD * t * 0.65f));
        }

        static float ShapeRadius(int shape, float theta)
        {
            switch (shape)
            {
                case 0: return 0.40f + 0.06f * Mathf.Cos(8f * theta);         // softBurst
                case 1: return 0.41f + 0.04f * Mathf.Cos(9f * theta);         // cookie9
                case 2: return RoundedPolygonR(5, 0.40f, 0.45f, theta);        // pentagon
                case 3: return 0.42f + 0.04f * Mathf.Cos(6f * theta);         // softFlower6
                case 4: return 0.35f + 0.08f * Mathf.Sqrt(Mathf.Abs(Mathf.Cos(6f * theta)) + 0.001f); // sunny
                case 5: return EllipseR(0.32f, 0.48f, theta);                  // oval
                case 6: return RoundedPolygonR(3, 0.38f, 0.55f, theta);        // triangle
                case 7: return 0.40f + 0.05f * Mathf.Cos(5f * theta);         // soft5fold
                default: return 0.4f;
            }
        }

        static float EllipseR(float a, float b, float theta)
        {
            float ct = Mathf.Cos(theta);
            float st = Mathf.Sin(theta);
            return (a * b) / Mathf.Sqrt(b * b * ct * ct + a * a * st * st);
        }

        static float RoundedPolygonR(int n, float apothem, float rounding, float theta)
        {
            float sector = Mathf.PI * 2f / n;
            float half = sector * 0.5f;
            float wrapped = ((theta % sector) + sector) % sector;
            if (wrapped > half) wrapped = sector - wrapped;
            float polyR = apothem / (Mathf.Cos(wrapped) + 0.001f);
            float vertexR = apothem / (Mathf.Cos(half) + 0.001f);
            // Cap vertex radius; rounding 0..1 (0=sharp, 1=circle)
            float capR = Mathf.Lerp(vertexR, apothem * 1.05f, rounding);
            // Smooth min: flat edges preserved, vertices rounded
            float k = (vertexR - apothem) * rounding * 0.5f;
            if (k < 0.001f) return Mathf.Min(polyR, capR);
            float h = Mathf.Clamp01(0.5f + 0.5f * (capR - polyR) / k);
            return Mathf.Lerp(capR, polyR, h) - k * h * (1f - h);
        }

        // ─── Expressive Ringed: outer circle + smaller morphing shape ───

        void DrawExpressiveRinged(Painter2D painter, double elapsed)
        {
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;

            // Outer ring: PrimaryContainer circle
            float ringR = _size * 0.46f;
            painter.fillColor = _theme.PrimaryContainer;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), ringR, 0f, 360f);
            painter.Fill();

            // Inner morphing shape (smaller, darker)
            float innerScale = _size * 0.55f;

            float timeMs = (float)(elapsed * 1000.0);
            float cycleT = (timeMs % ShapeCycleMs) / ShapeCycleMs;
            int shapeIdx = (int)(timeMs / ShapeCycleMs) % ShapeCount;
            int nextIdx = (shapeIdx + 1) % ShapeCount;

            float morphT = SpringEase(cycleT);
            float globalAngle = (timeMs % FullRotationMs) / FullRotationMs * 360f;
            float perShapeRot = shapeIdx * 90f;
            float totalRot = (globalAngle + perShapeRot + morphT * 90f) * Mathf.Deg2Rad;

            float bounceRaw = 1f - Mathf.Exp(-5f * cycleT) * Mathf.Cos(14f * cycleT);
            float bounceScale = 0.72f + 0.28f * bounceRaw;

            // Use OnPrimaryContainer for the inner shape (darker)
            painter.fillColor = _theme.OnPrimaryContainer;
            painter.BeginPath();

            for (int i = 0; i < SampleCount; i++)
            {
                float angle = (float)i / SampleCount * Mathf.PI * 2f;
                float r1 = ShapeRadius(shapeIdx, angle);
                float r2 = ShapeRadius(nextIdx, angle);
                float r = Mathf.Lerp(r1, r2, morphT) * innerScale * bounceScale;

                float rotAngle = angle + totalRot;
                float x = cx + Mathf.Cos(rotAngle) * r;
                float y = cy + Mathf.Sin(rotAngle) * r;

                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }
            painter.ClosePath();
            painter.Fill();
        }

        // ─── Bouncing Dots ───

        void DrawBouncingDots(Painter2D painter, double elapsed)
        {
            float t = (float)(elapsed % 1.2);
            float dotDia = Mathf.Min(_size * 0.4f, _size / 5f);
            float gap = dotDia * 0.6f;
            float totalW = dotDia * 3f + gap * 2f;
            float startX = (_size - totalW) * 0.5f + dotDia * 0.5f;
            float baseY = _size * 0.6f;
            float bounceH = _size * 0.25f;

            painter.fillColor = _theme.Primary;

            for (int i = 0; i < 3; i++)
            {
                float phase = (t - i / 3f * 1.2f + 1.2f) % 1.2f / 1.2f;
                float offsetY = 0f;
                if (phase < 0.5f / 1.2f * 1.2f)
                {
                    float u = phase / (0.5f / 1.2f * 1.2f);
                    if (u < 1f) offsetY = -Mathf.Sin(Mathf.PI * u) * bounceH;
                }

                float cx = startX + i * (dotDia + gap);
                float cy = baseY + offsetY;
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), dotDia * 0.5f, 0f, 360f);
                painter.Fill();
            }
        }

        // ─── Pulse Ring ───

        void DrawPulseRing(Painter2D painter, double elapsed)
        {
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float maxR = _size * 0.45f;
            float coreR = _size * 0.1f;

            // Core dot
            painter.fillColor = _theme.Primary;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), coreR, 0f, 360f);
            painter.Fill();

            // Two rings
            float period = 1.8f;
            for (int i = 0; i < 2; i++)
            {
                float phase = ((float)(elapsed % period) - i * 0.5f + period) % period / period;
                if (phase > 1f) continue;
                float eased = 1f - (1f - phase) * (1f - phase) * (1f - phase); // EaseOutCubic
                float r = Mathf.Lerp(coreR, maxR, eased);
                float alpha = (1f - eased) * (1f - eased);

                var ringColor = _theme.Primary;
                ringColor.a *= alpha;
                painter.strokeColor = ringColor;
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), r, 0f, 360f);
                painter.Stroke();
            }
        }

        // ─── Wave Bars ───

        void DrawWaveBars(Painter2D painter, double elapsed)
        {
            int barCount = 5;
            float barGap = 2f;
            float barW = (_size - barGap * (barCount - 1)) / barCount;
            float minH = _size * 0.15f;
            float maxH = _size * 0.9f;
            float period = 1f;

            painter.fillColor = _theme.Primary;

            for (int i = 0; i < barCount; i++)
            {
                float phase = ((float)(elapsed % period) - i * 0.18f + period) % period / period;
                float h = Mathf.Lerp(minH, maxH, (Mathf.Sin(phase * Mathf.PI * 2f) + 1f) * 0.5f);
                float x = i * (barW + barGap);
                float y = _size - h;

                float r = barW * 0.35f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x + r, y));
                painter.LineTo(new Vector2(x + barW - r, y));
                painter.Arc(new Vector2(x + barW - r, y + r), r, -90f, 0f);
                painter.LineTo(new Vector2(x + barW, y + h - r));
                painter.Arc(new Vector2(x + barW - r, y + h - r), r, 0f, 90f);
                painter.LineTo(new Vector2(x + r, y + h));
                painter.Arc(new Vector2(x + r, y + h - r), r, 90f, 180f);
                painter.LineTo(new Vector2(x, y + r));
                painter.Arc(new Vector2(x + r, y + r), r, 180f, 270f);
                painter.ClosePath();
                painter.Fill();
            }
        }

        // ─── Rotating Dots ───

        void DrawRotatingDots(Painter2D painter, double elapsed)
        {
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float ringR = _size * 0.35f;
            float dotR = _size * 0.09f;
            int dotCount = 8;

            float t = (float)(elapsed % 1.0);
            int head = (int)(t * dotCount) % dotCount;

            for (int i = 0; i < dotCount; i++)
            {
                float angle = -Mathf.PI * 0.5f + i * Mathf.PI * 2f / dotCount;
                float dx = cx + Mathf.Cos(angle) * ringR;
                float dy = cy + Mathf.Sin(angle) * ringR;

                int dist = ((i - head) % dotCount + dotCount) % dotCount;
                float falloff = (float)dist / (dotCount - 1);
                float alpha = Mathf.Lerp(1f, 0.15f, Mathf.Pow(falloff, 2.5f));

                var dotColor = _theme.Primary;
                dotColor.a *= alpha;
                painter.fillColor = dotColor;
                painter.BeginPath();
                painter.Arc(new Vector2(dx, dy), dotR, 0f, 360f);
                painter.Fill();
            }
        }

        // ─── Sine Wave ───

        void DrawSineWave(Painter2D painter, double elapsed)
        {
            float cy = _size * 0.5f;
            float amplitude = _size * 0.25f;
            float t = (float)(elapsed * 2.5); // speed
            int segments = 40;

            // Main wave
            painter.strokeColor = _theme.Primary;
            painter.lineWidth = Mathf.Max(2f, _size * 0.06f);
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            for (int i = 0; i <= segments; i++)
            {
                float x = (float)i / segments * _size;
                float phase = x / _size * Mathf.PI * 3f - t * Mathf.PI * 2f;
                float y = cy + Mathf.Sin(phase) * amplitude;
                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();

            // Secondary wave (offset, thinner, lower opacity)
            var secColor = _theme.Primary;
            secColor.a *= 0.35f;
            painter.strokeColor = secColor;
            painter.lineWidth = Mathf.Max(1.5f, _size * 0.04f);
            painter.BeginPath();
            for (int i = 0; i <= segments; i++)
            {
                float x = (float)i / segments * _size;
                float phase = x / _size * Mathf.PI * 3f - t * Mathf.PI * 2f + Mathf.PI * 0.6f;
                float y = cy + Mathf.Sin(phase) * amplitude * 0.7f;
                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();

            // Dot riding the main wave
            float dotPhase = -t * Mathf.PI * 2f + Mathf.PI * 0.5f;
            float dotX = _size * 0.5f;
            float dotY = cy + Mathf.Sin(dotPhase + dotX / _size * Mathf.PI * 3f) * amplitude;
            painter.fillColor = _theme.Primary;
            painter.BeginPath();
            painter.Arc(new Vector2(dotX, dotY), _size * 0.06f, 0f, 360f);
            painter.Fill();
        }

        // ─── DNA Helix ───

        void DrawDnaHelix(Painter2D painter, double elapsed)
        {
            float cx = _size * 0.5f;
            float amplitude = _size * 0.3f;
            float t = (float)(elapsed * 1.8);
            int pairCount = 8;

            for (int i = 0; i < pairCount; i++)
            {
                float phase = (float)i / pairCount * Mathf.PI * 2f + t * Mathf.PI * 2f;
                float y = (float)i / (pairCount - 1) * _size * 0.85f + _size * 0.075f;

                float x1 = cx + Mathf.Sin(phase) * amplitude;
                float x2 = cx - Mathf.Sin(phase) * amplitude;

                // Depth-based alpha (front = opaque, back = faint)
                float depth1 = (Mathf.Cos(phase) + 1f) * 0.5f; // 0 = back, 1 = front
                float depth2 = 1f - depth1;

                float dotR = _size * 0.055f;

                // Connecting bar
                var barColor = _theme.OutlineVariant;
                barColor.a *= 0.4f;
                painter.strokeColor = barColor;
                painter.lineWidth = 1.5f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x1, y));
                painter.LineTo(new Vector2(x2, y));
                painter.Stroke();

                // Left dot
                var c1 = _theme.Primary;
                c1.a *= 0.3f + 0.7f * depth1;
                painter.fillColor = c1;
                painter.BeginPath();
                painter.Arc(new Vector2(x1, y), dotR * (0.6f + 0.4f * depth1), 0f, 360f);
                painter.Fill();

                // Right dot
                var c2 = _theme.Tertiary;
                c2.a *= 0.3f + 0.7f * depth2;
                painter.fillColor = c2;
                painter.BeginPath();
                painter.Arc(new Vector2(x2, y), dotR * (0.6f + 0.4f * depth2), 0f, 360f);
                painter.Fill();
            }
        }

        // ─── Orbit ───

        void DrawOrbit(Painter2D painter, double elapsed)
        {
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float orbitR = _size * 0.32f;
            float dotR = _size * 0.07f;

            // Orbit track
            var trackColor = _theme.OutlineVariant;
            trackColor.a *= 0.3f;
            painter.strokeColor = trackColor;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), orbitR, 0f, 360f);
            painter.Stroke();

            // Center dot
            painter.fillColor = _theme.Primary;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), _size * 0.06f, 0f, 360f);
            painter.Fill();

            // 3 orbiting dots with different speeds and sizes
            var dotColors = new[] { _theme.Primary, _theme.Secondary, _theme.Tertiary };
            var speeds = new[] { 1.5f, 2.3f, 3.1f };
            var sizes = new[] { 1f, 0.7f, 0.5f };

            for (int i = 0; i < 3; i++)
            {
                float angle = (float)(elapsed * speeds[i] * Mathf.PI * 2f);
                float dx = cx + Mathf.Cos(angle) * orbitR;
                float dy = cy + Mathf.Sin(angle) * orbitR;

                // Trail
                var trailColor = dotColors[i];
                trailColor.a *= 0.2f;
                painter.strokeColor = trailColor;
                painter.lineWidth = dotR * sizes[i] * 0.8f;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                float trailLen = 0.8f;
                for (int j = 0; j <= 12; j++)
                {
                    float ta = angle - (float)j / 12f * trailLen;
                    float tx = cx + Mathf.Cos(ta) * orbitR;
                    float ty = cy + Mathf.Sin(ta) * orbitR;
                    if (j == 0) painter.MoveTo(new Vector2(tx, ty));
                    else painter.LineTo(new Vector2(tx, ty));
                }
                painter.Stroke();

                painter.fillColor = dotColors[i];
                painter.BeginPath();
                painter.Arc(new Vector2(dx, dy), dotR * sizes[i], 0f, 360f);
                painter.Fill();
            }
        }

        // ─── Ripple ───

        void DrawRipple(Painter2D painter, double elapsed)
        {
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float maxR = _size * 0.45f;
            int ringCount = 3;
            float period = 2.4f;

            for (int i = 0; i < ringCount; i++)
            {
                float phase = ((float)(elapsed % period) - i * (period / ringCount) + period) % period / period;
                float eased = 1f - (1f - phase) * (1f - phase);
                float r = eased * maxR;
                float alpha = (1f - eased);
                alpha *= alpha;

                var ringColor = _theme.Primary;
                ringColor.a *= alpha;
                painter.strokeColor = ringColor;
                painter.lineWidth = Mathf.Lerp(3f, 1f, eased);
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), r, 0f, 360f);
                painter.Stroke();
            }
        }

        // ─── Typing Dots ───

        void DrawTypingDots(Painter2D painter, double elapsed)
        {
            int dotCount = 3;
            float dotDia = Mathf.Min(_size * 0.22f, _size / 4.5f);
            float gap = dotDia * 0.5f;
            float totalW = dotDia * dotCount + gap * (dotCount - 1);
            float startX = (_size - totalW) * 0.5f + dotDia * 0.5f;
            float cy = _size * 0.5f;
            float period = 1.4f;

            for (int i = 0; i < dotCount; i++)
            {
                float phase = ((float)(elapsed % period) - i * 0.15f + period) % period / period;

                // Scale: rest → grow → rest with smooth pulse
                float scale;
                if (phase < 0.3f)
                    scale = 1f;
                else if (phase < 0.5f)
                {
                    float u = (phase - 0.3f) / 0.2f;
                    scale = 1f + 0.5f * Mathf.Sin(u * Mathf.PI);
                }
                else
                    scale = 1f;

                // Alpha: subtle breathing
                float alpha = 0.5f + 0.5f * scale / 1.5f;

                var dotColor = _theme.Primary;
                dotColor.a *= alpha;
                painter.fillColor = dotColor;

                float cx = startX + i * (dotDia + gap);
                float r = dotDia * 0.5f * scale;

                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), r, 0f, 360f);
                painter.Fill();
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
