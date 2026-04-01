using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Foldout : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Foldout, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly VisualElement _header;
        readonly Label _label;
        readonly Label _chevron;
        readonly VisualElement _divider;
        readonly VisualElement _contentWrapper;
        readonly VisualElement _content;
        MD3Theme _theme;
        bool _hovered;
        MD3AnimationHandle _animHandle;
        MD3AnimationHandle _chevronAnim;
        MD3AnimationHandle _headerAnim;

        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (_expanded == value) return;
                _expanded = value;
                UpdateVisual(true);
                changed?.Invoke(_expanded);
            }
        }
        bool _expanded;

        public VisualElement Content => _content;

        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        public MD3Foldout() : this("Foldout", false) { }

        public MD3Foldout(string label, bool expanded = false)
        {
            _expanded = expanded;
            AddToClassList("md3-foldout");

            _header = new VisualElement();
            _header.AddToClassList("md3-foldout__header");
            Add(_header);

            _chevron = new Label(MD3Icon.ExpandMore);
            _chevron.AddToClassList("md3-foldout__chevron");
            _chevron.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_chevron, 20f);
            _header.Add(_chevron);

            _label = new Label(label);
            _label.AddToClassList("md3-foldout__label");
            _label.pickingMode = PickingMode.Ignore;
            _header.Add(_label);

            _divider = new VisualElement();
            _divider.AddToClassList("md3-foldout__divider");
            Add(_divider);

            _contentWrapper = new VisualElement();
            _contentWrapper.AddToClassList("md3-foldout__content-wrapper");

            _content = new VisualElement();
            _content.AddToClassList("md3-foldout__content");
            _contentWrapper.Add(_content);
            Add(_contentWrapper);

            _header.RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            _header.RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyColors(); });
            _header.RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<AttachToPanelEvent>(OnAttach);

            // Set initial state without animation
            if (_expanded)
            {
                _chevron.style.rotate = new Rotate(180f);
            }
            else
            {
                _contentWrapper.style.maxHeight = 0;
                _content.style.opacity = 0;
                _chevron.style.rotate = new Rotate(0f);
            }
            UpdateVisual(false);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            _theme = ResolveTheme();
            UpdateVisual(false);
        }

        void OnClick(ClickEvent evt)
        {
            _expanded = !_expanded;
            UpdateVisual(true);
            changed?.Invoke(_expanded);
        }

        void UpdateVisual(bool animate)
        {
            if (_expanded)
                AddToClassList("md3-foldout--expanded");
            else
                RemoveFromClassList("md3-foldout--expanded");

            _animHandle?.Cancel();
            _chevronAnim?.Cancel();
            _headerAnim?.Cancel();

            if (!animate)
            {
                if (_expanded)
                {
                    _contentWrapper.style.maxHeight = StyleKeyword.None;
                    _content.style.opacity = 1f;
                    _chevron.style.rotate = new Rotate(180f);
                }
                else
                {
                    _contentWrapper.style.maxHeight = 0;
                    _content.style.opacity = 0f;
                    _chevron.style.rotate = new Rotate(0f);
                }
                _chevron.style.scale = new Scale(Vector3.one);
                _header.style.scale = new Scale(Vector3.one);
                ApplyColors();
                return;
            }

            if (_expanded)
                AnimateExpand();
            else
                AnimateCollapse();

            ApplyColors();
        }

        void AnimateExpand()
        {
            // Chevron: snap rotation
            _chevronAnim = MD3Animate.Float(_chevron, 0f, 180f, 200f, MD3Easing.EaseOutCubic,
                v => _chevron.style.rotate = new Rotate(v));

            // Chevron: punch scale
            MD3Animate.Keyframes(_chevron, 250f, new[]
            {
                new MD3Keyframe(0f,   1f),
                new MD3Keyframe(0.2f, 1.5f, MD3Easing.EaseOutCubic),
                new MD3Keyframe(0.5f, 0.85f, MD3Easing.EaseOut),
                new MD3Keyframe(1f,   1f,   MD3Easing.EaseOutBack),
            }, v => _chevron.style.scale = new Scale(new Vector3(v, v, 1f)));

            // Header: subtle press-release
            _headerAnim = MD3Animate.Keyframes(_header, 300f, new[]
            {
                new MD3Keyframe(0f,   1f),
                new MD3Keyframe(0.3f, 0.98f, MD3Easing.EaseOut),
                new MD3Keyframe(1f,   1f,    MD3Easing.EaseOutBack),
            }, v => _header.style.scale = new Scale(new Vector3(v, 1f, 1f)));

            // Content: expand height + fade in
            _contentWrapper.style.maxHeight = StyleKeyword.None;
            _content.style.opacity = 0f;

            EventCallback<GeometryChangedEvent> measureCb = null;
            measureCb = e =>
            {
                _contentWrapper.UnregisterCallback(measureCb);
                float targetHeight = _contentWrapper.resolvedStyle.height;
                if (targetHeight <= 0) targetHeight = _content.resolvedStyle.height;
                _contentWrapper.style.maxHeight = 0;

                _animHandle = MD3Animate.Float(_contentWrapper, 0f, targetHeight, 300f, MD3Easing.EaseOut,
                    v => _contentWrapper.style.maxHeight = v,
                    () => _contentWrapper.style.maxHeight = StyleKeyword.None);

                MD3Animate.Float(_content, 0f, 1f, 300f, MD3Easing.EaseOut,
                    v => _content.style.opacity = v);
            };
            _contentWrapper.RegisterCallback(measureCb);
        }

        void AnimateCollapse()
        {
            // Chevron: snap rotation back
            _chevronAnim = MD3Animate.Float(_chevron, 180f, 0f, 200f, MD3Easing.EaseOutCubic,
                v => _chevron.style.rotate = new Rotate(v));

            // Chevron: squish-pop scale
            MD3Animate.Keyframes(_chevron, 250f, new[]
            {
                new MD3Keyframe(0f,   1f),
                new MD3Keyframe(0.2f, 0.6f, MD3Easing.EaseOutCubic),
                new MD3Keyframe(0.5f, 1.3f, MD3Easing.EaseOut),
                new MD3Keyframe(1f,   1f,   MD3Easing.EaseOutBack),
            }, v => _chevron.style.scale = new Scale(new Vector3(v, v, 1f)));

            // Content: collapse height + fade out
            float currentHeight = _contentWrapper.resolvedStyle.height;
            if (currentHeight <= 0) currentHeight = _content.resolvedStyle.height;

            _contentWrapper.style.maxHeight = currentHeight;

            _animHandle = MD3Animate.Float(_contentWrapper, currentHeight, 0f, 250f, MD3Easing.EaseIn,
                v => _contentWrapper.style.maxHeight = v);

            MD3Animate.Float(_content, 1f, 0f, 200f, MD3Easing.EaseIn,
                v => _content.style.opacity = v);
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            var headerBg = Color.clear;
            if (_hovered)
                headerBg = _theme.HoverOverlay(Color.clear, _theme.OnSurface);

            _header.style.backgroundColor = headerBg;
            _label.style.color = _theme.OnSurface;
            _chevron.style.color = _theme.OnSurfaceVariant;
            _divider.style.backgroundColor = _theme.OutlineVariant;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
