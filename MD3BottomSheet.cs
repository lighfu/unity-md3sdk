using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3BottomSheet : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3BottomSheet, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action dismissed;

        readonly VisualElement _scrim;
        readonly VisualElement _sheet;
        readonly VisualElement _dragHandle;
        readonly Label _titleLabel;
        readonly ScrollView _contentScroll;
        readonly bool _modal;
        MD3Theme _theme;
        MD3AnimationHandle _scrimAnim;
        MD3AnimationHandle _sheetAnim;
        VisualElement _shadowAmbient, _shadowKey;

        public VisualElement Content => _contentScroll.contentContainer;

        public MD3BottomSheet() : this("Bottom Sheet") { }

        public MD3BottomSheet(string title = null, bool modal = true)
        {
            _modal = modal;

            // Fill parent
            style.position = Position.Absolute;
            style.top = 0;
            style.left = 0;
            style.right = 0;
            style.bottom = 0;

            // Scrim
            _scrim = new VisualElement();
            _scrim.AddToClassList("md3-bottom-sheet__scrim");
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
            _sheet.AddToClassList("md3-bottom-sheet__sheet");

            // Drag handle (decorative)
            _dragHandle = new VisualElement();
            _dragHandle.AddToClassList("md3-bottom-sheet__handle");
            _sheet.Add(_dragHandle);

            // Title (optional)
            if (title != null)
            {
                _titleLabel = new Label(title);
                _titleLabel.AddToClassList("md3-bottom-sheet__title");
                _titleLabel.pickingMode = PickingMode.Ignore;
                _sheet.Add(_titleLabel);
            }

            // Content scroll
            _contentScroll = new ScrollView(ScrollViewMode.Vertical);
            _contentScroll.AddToClassList("md3-bottom-sheet__content");
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

            // Wait for geometry to resolve, then animate sheet up
            _sheet.style.opacity = 0f;
            EventCallback<GeometryChangedEvent> geoCb = null;
            geoCb = e =>
            {
                _sheet.UnregisterCallback(geoCb);

                // Clamp max height to 70% of parent
                float parentHeight = parent.resolvedStyle.height;
                if (parentHeight > 0)
                    _sheet.style.maxHeight = parentHeight * 0.7f;

                float sheetHeight = _sheet.resolvedStyle.height;
                if (sheetHeight <= 0) sheetHeight = 300f;

                _sheet.style.opacity = 1f;
                _sheet.style.translate = new Translate(0, sheetHeight);
                _sheetAnim = MD3Animate.Float(this, sheetHeight, 0f, 200f, MD3Easing.EaseOut, y =>
                {
                    _sheet.style.translate = new Translate(0, y);
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

            // Slide sheet down
            float sheetHeight = _sheet.resolvedStyle.height;
            if (sheetHeight <= 0) sheetHeight = 300f;

            _sheetAnim = MD3Animate.Float(this, 0f, sheetHeight, 150f, MD3Easing.EaseIn, y =>
            {
                _sheet.style.translate = new Translate(0, y);
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

            // Drag handle
            var handleColor = _theme.OnSurfaceVariant;
            handleColor.a = 0.4f;
            _dragHandle.style.backgroundColor = handleColor;

            // Title
            if (_titleLabel != null)
                _titleLabel.style.color = _theme.OnSurface;

            MD3Elevation.UpdateShadowColor(_shadowAmbient, _shadowKey, _theme.IsDark, 3);
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
