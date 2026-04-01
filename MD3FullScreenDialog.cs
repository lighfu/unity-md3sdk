using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3FullScreenDialog : VisualElement, IMD3Themeable
    {
        public event Action dismissed;

        readonly VisualElement _container;
        readonly VisualElement _topBar;
        readonly VisualElement _closeBtn;
        readonly Label _titleLabel;
        readonly MD3Button _actionBtn;
        readonly ScrollView _contentScroll;
        MD3Theme _theme;
        MD3AnimationHandle _anim;

        public VisualElement Content => _contentScroll.contentContainer;

        public MD3FullScreenDialog() : this("Full Screen Dialog") { }

        public MD3FullScreenDialog(string title, string actionLabel = null, Action onAction = null)
        {
            AddToClassList("md3-fullscreen-dialog");

            // Container fills parent
            _container = new VisualElement();
            _container.style.flexGrow = 1;
            _container.style.flexDirection = FlexDirection.Column;

            // Top bar
            _topBar = new VisualElement();
            _topBar.AddToClassList("md3-fullscreen-dialog__top-bar");

            _closeBtn = new VisualElement();
            _closeBtn.AddToClassList("md3-fullscreen-dialog__close");
            var closeLabel = MD3Icon.Create(MD3Icon.Close);
            _closeBtn.Add(closeLabel);
            _closeBtn.RegisterCallback<ClickEvent>(e => Dismiss());
            _topBar.Add(_closeBtn);

            _titleLabel = new Label(title);
            _titleLabel.AddToClassList("md3-fullscreen-dialog__title");
            _titleLabel.pickingMode = PickingMode.Ignore;
            _topBar.Add(_titleLabel);

            if (actionLabel != null)
            {
                _actionBtn = new MD3Button(actionLabel, MD3ButtonStyle.Filled);
                if (onAction != null)
                    _actionBtn.clicked += onAction;
                _topBar.Add(_actionBtn);
            }

            _container.Add(_topBar);

            // Content scroll
            _contentScroll = new ScrollView(ScrollViewMode.Vertical);
            _contentScroll.AddToClassList("md3-fullscreen-dialog__content");
            _container.Add(_contentScroll);

            Add(_container);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void Show(VisualElement parent)
        {
            parent.Add(this);

            // Slide up from bottom
            _container.style.opacity = 0f;
            EventCallback<GeometryChangedEvent> geoCb = null;
            geoCb = e =>
            {
                _container.UnregisterCallback(geoCb);

                float parentHeight = parent.resolvedStyle.height;
                if (parentHeight <= 0) parentHeight = 600f;

                _container.style.opacity = 1f;
                _container.style.translate = new Translate(0, parentHeight);
                _anim = MD3Animate.Float(this, parentHeight, 0f, 200f, MD3Easing.EaseOut, y =>
                {
                    _container.style.translate = new Translate(0, y);
                });
            };
            _container.RegisterCallback(geoCb);
        }

        public void Dismiss()
        {
            _anim?.Cancel();

            float parentHeight = parent?.resolvedStyle.height ?? 600f;
            if (parentHeight <= 0) parentHeight = 600f;

            _anim = MD3Animate.Float(this, 0f, parentHeight, 150f, MD3Easing.EaseIn, y =>
            {
                _container.style.translate = new Translate(0, y);
            }, () =>
            {
                RemoveFromHierarchy();
                dismissed?.Invoke();
            });
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            _container.style.backgroundColor = _theme.Surface;
            _topBar.style.backgroundColor = _theme.Surface;
            _topBar.style.borderBottomColor = _theme.OutlineVariant;
            _closeBtn.style.color = _theme.OnSurface;
            _titleLabel.style.color = _theme.OnSurface;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
