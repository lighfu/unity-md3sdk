using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3SplitDirection { Horizontal, Vertical }

    /// <summary>
    /// Resizable split pane with a draggable divider.
    /// Contains two panels whose sizes can be adjusted by dragging.
    /// </summary>
    public class MD3SplitPane : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3SplitPane, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<float> ratioChanged;

        readonly VisualElement _first;
        readonly VisualElement _second;
        readonly VisualElement _handle;
        readonly VisualElement _handleBar;
        readonly MD3SplitDirection _direction;
        MD3Theme _theme;
        bool _dragging;
        float _dragStartPos;
        float _dragStartSize;
        float _minFirst;
        float _minSecond;

        /// <summary>The first (left or top) panel. Add your content here.</summary>
        public VisualElement First => _first;

        /// <summary>The second (right or bottom) panel. Add your content here.</summary>
        public VisualElement Second => _second;

        /// <summary>Current ratio (0..1) of first panel size to total size.</summary>
        public float Ratio
        {
            get
            {
                float total = _direction == MD3SplitDirection.Horizontal
                    ? resolvedStyle.width : resolvedStyle.height;
                if (float.IsNaN(total) || total <= 0) return 0.5f;
                float firstSize = _direction == MD3SplitDirection.Horizontal
                    ? _first.resolvedStyle.width : _first.resolvedStyle.height;
                if (float.IsNaN(firstSize)) return 0.5f;
                return Mathf.Clamp01(firstSize / total);
            }
        }

        public MD3SplitPane() : this(MD3SplitDirection.Horizontal) { }

        /// <param name="direction">Horizontal = left|right, Vertical = top|bottom</param>
        /// <param name="initialRatio">Initial size ratio of the first panel (0..1)</param>
        /// <param name="minFirst">Minimum size in px of the first panel</param>
        /// <param name="minSecond">Minimum size in px of the second panel</param>
        public MD3SplitPane(MD3SplitDirection direction = MD3SplitDirection.Horizontal,
            float initialRatio = 0.5f, float minFirst = 100f, float minSecond = 100f)
        {
            _direction = direction;
            _minFirst = minFirst;
            _minSecond = minSecond;

            AddToClassList("md3-split-pane");
            style.flexGrow = 1;
            style.flexDirection = direction == MD3SplitDirection.Horizontal
                ? FlexDirection.Row : FlexDirection.Column;

            // First panel
            _first = new VisualElement();
            _first.AddToClassList("md3-split-pane__first");
            _first.style.flexGrow = 0;
            _first.style.flexShrink = 0;
            _first.style.overflow = Overflow.Hidden;
            Add(_first);

            // Handle
            _handle = new VisualElement();
            _handle.AddToClassList("md3-split-pane__handle");
            _handle.style.flexShrink = 0;
            _handle.style.alignItems = Align.Center;
            _handle.style.justifyContent = Justify.Center;

            if (direction == MD3SplitDirection.Horizontal)
            {
                _handle.style.width = 12;
                _handle.style.cursor = StyleKeyword.None;
            }
            else
            {
                _handle.style.height = 12;
                _handle.style.cursor = StyleKeyword.None;
            }
            Add(_handle);

            // Handle visual bar
            _handleBar = new VisualElement();
            _handleBar.AddToClassList("md3-split-pane__handle-bar");
            _handleBar.pickingMode = PickingMode.Ignore;
            if (direction == MD3SplitDirection.Horizontal)
            {
                _handleBar.style.width = 4;
                _handleBar.style.height = 32;
                _handleBar.Radius(2);
            }
            else
            {
                _handleBar.style.width = 32;
                _handleBar.style.height = 4;
                _handleBar.Radius(2);
            }
            _handle.Add(_handleBar);

            // Second panel
            _second = new VisualElement();
            _second.AddToClassList("md3-split-pane__second");
            _second.style.flexGrow = 1;
            _second.style.flexShrink = 1;
            _second.style.overflow = Overflow.Hidden;
            Add(_second);

            // Apply initial ratio on first layout only
            EventCallback<GeometryChangedEvent> initLayout = null;
            initLayout = e =>
            {
                if (_dragging) return;
                float total = direction == MD3SplitDirection.Horizontal ? e.newRect.width : e.newRect.height;
                if (total <= 0) return;
                float handleSize = 12f;
                float firstSize = (total - handleSize) * initialRatio;
                firstSize = Mathf.Clamp(firstSize, _minFirst, total - handleSize - _minSecond);
                if (direction == MD3SplitDirection.Horizontal)
                    _first.style.width = firstSize;
                else
                    _first.style.height = firstSize;
                UnregisterCallback(initLayout);
            };
            RegisterCallback(initLayout);

            // Drag events
            _handle.RegisterCallback<MouseDownEvent>(OnDragStart);
            _handle.RegisterCallback<MouseMoveEvent>(OnDragMove);
            _handle.RegisterCallback<MouseUpEvent>(OnDragEnd);
            _handle.RegisterCallback<MouseEnterEvent>(_ => _handleBar.style.opacity = 1f);
            _handle.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!_dragging) _handleBar.style.opacity = 0.5f;
            });

            RegisterCallback<AttachToPanelEvent>(_ => RefreshTheme());
        }

        void OnDragStart(MouseDownEvent e)
        {
            if (e.button != 0) return;
            _dragging = true;
            _handle.CaptureMouse();
            _handleBar.style.opacity = 1f;

            if (_direction == MD3SplitDirection.Horizontal)
            {
                _dragStartPos = e.mousePosition.x;
                _dragStartSize = _first.resolvedStyle.width;
            }
            else
            {
                _dragStartPos = e.mousePosition.y;
                _dragStartSize = _first.resolvedStyle.height;
            }
            e.StopPropagation();
        }

        void OnDragMove(MouseMoveEvent e)
        {
            if (!_dragging) return;

            float total = _direction == MD3SplitDirection.Horizontal
                ? resolvedStyle.width : resolvedStyle.height;
            if (float.IsNaN(total) || total <= 0) return;

            float delta = _direction == MD3SplitDirection.Horizontal
                ? e.mousePosition.x - _dragStartPos
                : e.mousePosition.y - _dragStartPos;

            float handleSize = 12f;
            float newSize = _dragStartSize + delta;
            newSize = Mathf.Clamp(newSize, _minFirst, total - handleSize - _minSecond);

            if (_direction == MD3SplitDirection.Horizontal)
                _first.style.width = newSize;
            else
                _first.style.height = newSize;

            ratioChanged?.Invoke(Ratio);
        }

        void OnDragEnd(MouseUpEvent e)
        {
            if (!_dragging) return;
            _dragging = false;
            _handle.ReleaseMouse();
            _handleBar.style.opacity = 0.5f;
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) return;
            _handleBar.style.backgroundColor = _theme.OutlineVariant;
            _handleBar.style.opacity = 0.5f;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
