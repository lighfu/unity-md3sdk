using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Spinner : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Spinner, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        MD3Theme _theme;
        double _startTime;
        readonly float _size;

        public MD3Spinner() : this(24f) { }

        public MD3Spinner(float size = 24f)
        {
            _size = size;
            AddToClassList("md3-spinner");

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

            var painter = mgc.painter2D;
            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float radius = (_size - 3f) * 0.5f;
            float strokeWidth = Mathf.Max(2f, _size * 0.1f);

            // Track: full circle, semi-transparent SurfaceVariant
            var trackColor = _theme.SurfaceVariant;
            trackColor.a *= 0.5f;
            painter.strokeColor = trackColor;
            painter.lineWidth = strokeWidth;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), radius, 0f, 360f);
            painter.Stroke();

            // Spinner: 270-degree arc, Primary color
            painter.strokeColor = _theme.Primary;
            painter.lineWidth = strokeWidth;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            float angle = (float)((EditorApplication.timeSinceStartup - _startTime) * 200.0 % 360.0);
            painter.Arc(new Vector2(cx, cy), radius, angle, angle + 270f);
            painter.Stroke();
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
