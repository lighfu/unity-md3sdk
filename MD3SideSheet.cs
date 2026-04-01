using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3SideSheet : VisualElement, IMD3Themeable
    {
        public event Action dismissed;

        readonly VisualElement _scrim;
        readonly VisualElement _sheet;
        readonly Label _titleLabel;
        readonly VisualElement _closeBtn;
        readonly ScrollView _contentScroll;
        readonly bool _modal;
        MD3Theme _theme;
        MD3AnimationHandle _scrimAnim;
        MD3AnimationHandle _sheetAnim;
        VisualElement _shadowAmbient, _shadowKey;

        public VisualElement Content => _contentScroll.contentContainer;

        public MD3SideSheet() : this("Side Sheet") { }

        public MD3SideSheet(string title = null, bool modal = true)
        {
            _modal = modal;

            style.position = Position.Absolute;
            style.top = 0;
            style.left = 0;
            style.right = 0;
            style.bottom = 0;

            // Scrim
            _scrim = new VisualElement();
            _scrim.AddToClassList("md3-side-sheet__scrim");
            if (_modal)
            {
                _scrim.RegisterCallback<ClickEvent>(e =>
                {
                    if (e.target == _scrim)
                        Dismiss();
                });
            }
            Add(_scrim);

            // Sheet
            _sheet = new VisualElement();
            _sheet.AddToClassList("md3-side-sheet__sheet");

            // Header
            var header = new VisualElement();
            header.AddToClassList("md3-side-sheet__header");

            if (title != null)
            {
                _titleLabel = new Label(title);
                _titleLabel.AddToClassList("md3-side-sheet__title");
                _titleLabel.pickingMode = PickingMode.Ignore;
                header.Add(_titleLabel);
            }
            else
            {
                var spacer = new VisualElement();
                spacer.style.flexGrow = 1;
                header.Add(spacer);
            }

            _closeBtn = new VisualElement();
            _closeBtn.AddToClassList("md3-side-sheet__close");
            var closeLabel = MD3Icon.Create(MD3Icon.Close);
            _closeBtn.Add(closeLabel);
            _closeBtn.RegisterCallback<ClickEvent>(e => Dismiss());
            header.Add(_closeBtn);

            _sheet.Add(header);

            // Content scroll
            _contentScroll = new ScrollView(ScrollViewMode.Vertical);
            _contentScroll.AddToClassList("md3-side-sheet__content");
            _sheet.Add(_contentScroll);

            Add(_sheet);
            (_shadowAmbient, _shadowKey) = MD3Elevation.AddSiblingShadow(this, _sheet, 28f, 3);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void Show(VisualElement parent)
        {
            parent.Add(this);

            // Animate scrim
            _scrim.style.opacity = 0f;
            _scrimAnim = MD3Animate.Float(this, 0f, 1f, 120f, MD3Easing.EaseOut, t =>
            {
                _scrim.style.opacity = t;
            });

            // Wait for geometry, then slide sheet in from right
            _sheet.style.opacity = 0f;
            EventCallback<GeometryChangedEvent> geoCb = null;
            geoCb = e =>
            {
                _sheet.UnregisterCallback(geoCb);

                // Clamp max width to 60% of parent
                float parentWidth = parent.resolvedStyle.width;
                if (parentWidth > 0)
                    _sheet.style.maxWidth = parentWidth * 0.6f;

                float sheetWidth = _sheet.resolvedStyle.width;
                if (sheetWidth <= 0) sheetWidth = 360f;

                _sheet.style.opacity = 1f;
                _sheet.style.translate = new Translate(sheetWidth, 0);
                _sheetAnim = MD3Animate.Float(this, sheetWidth, 0f, 200f, MD3Easing.EaseOut, x =>
                {
                    _sheet.style.translate = new Translate(x, 0);
                });
            };
            _sheet.RegisterCallback(geoCb);
        }

        public void Dismiss()
        {
            _scrimAnim?.Cancel();
            _sheetAnim?.Cancel();

            // Fade scrim
            MD3Animate.Float(this, _scrim.resolvedStyle.opacity, 0f, 120f, MD3Easing.EaseIn, t =>
            {
                _scrim.style.opacity = t;
            });

            // Slide sheet out to right
            float sheetWidth = _sheet.resolvedStyle.width;
            if (sheetWidth <= 0) sheetWidth = 360f;

            _sheetAnim = MD3Animate.Float(this, 0f, sheetWidth, 150f, MD3Easing.EaseIn, x =>
            {
                _sheet.style.translate = new Translate(x, 0);
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

            // Scrim
            if (_modal)
            {
                var scrimColor = _theme.OnSurface;
                scrimColor.a = 0.32f;
                _scrim.style.backgroundColor = scrimColor;
            }
            else
            {
                _scrim.style.backgroundColor = Color.clear;
            }

            // Sheet
            _sheet.style.backgroundColor = _theme.SurfaceContainerHigh;

            // Title
            if (_titleLabel != null)
                _titleLabel.style.color = _theme.OnSurface;

            // Close button
            _closeBtn.style.color = _theme.OnSurfaceVariant;

            MD3Elevation.UpdateShadowColor(_shadowAmbient, _shadowKey, _theme.IsDark, 3);
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
