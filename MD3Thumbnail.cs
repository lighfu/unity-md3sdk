using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Small preview image with optional label overlay and click event.
    /// Useful for asset browsers, material lists, texture grids, etc.
    /// </summary>
    public class MD3Thumbnail : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Thumbnail, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;

        readonly MD3Image _image;
        readonly VisualElement _overlay;
        readonly Label _label;
        MD3Theme _theme;
        bool _hovered;

        public Texture2D Texture
        {
            get => _image.Texture;
            set => _image.Texture = value;
        }

        public string LabelText
        {
            get => _label.text;
            set
            {
                _label.text = value;
                _overlay.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public MD3Thumbnail() : this(null, 64f) { }

        public MD3Thumbnail(Texture2D texture, float size = 64f, string label = null, float radius = 0f)
        {
            AddToClassList("md3-thumbnail");
            style.width = size;
            style.height = size;
            style.flexShrink = 0;
            style.overflow = Overflow.Hidden;
            style.cursor = StyleKeyword.None;
            if (radius > 0)
                this.Radius(radius);
            else
                this.Radius(MD3Radius.S);

            // Image
            _image = new MD3Image(texture, fit: MD3ImageFit.Cover);
            _image.style.position = Position.Absolute;
            _image.style.top = 0; _image.style.left = 0;
            _image.style.right = 0; _image.style.bottom = 0;
            _image.pickingMode = PickingMode.Ignore;
            Add(_image);

            // Label overlay (bottom gradient + text)
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.bottom = 0; _overlay.style.left = 0; _overlay.style.right = 0;
            _overlay.style.height = size * 0.4f;
            _overlay.style.justifyContent = Justify.FlexEnd;
            _overlay.style.paddingLeft = MD3Spacing.XS;
            _overlay.style.paddingRight = MD3Spacing.XS;
            _overlay.style.paddingBottom = MD3Spacing.XXS;
            _overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
            _overlay.pickingMode = PickingMode.Ignore;
            if (string.IsNullOrEmpty(label)) _overlay.style.display = DisplayStyle.None;
            Add(_overlay);

            _label = new Label(label ?? "");
            _label.style.fontSize = 10;
            _label.style.color = Color.white;
            _label.style.whiteSpace = WhiteSpace.NoWrap;
            _label.style.overflow = Overflow.Hidden;
            _label.style.unityTextAlign = TextAnchor.LowerLeft;
            _label.pickingMode = PickingMode.Ignore;
            _overlay.Add(_label);

            // Placeholder when no texture
            RegisterCallback<AttachToPanelEvent>(_ => RefreshTheme());
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyHover(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyHover(); });
            RegisterCallback<MouseDownEvent>(e => MD3Ripple.Spawn(this, e, Color.white));
            RegisterCallback<ClickEvent>(e => clicked?.Invoke());
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) return;
            // Placeholder background when no texture
            if (_image.Texture == null)
                style.backgroundColor = _theme.SurfaceContainerHigh;
            else
                style.backgroundColor = Color.clear;
        }

        void ApplyHover()
        {
            if (_hovered)
                style.opacity = 0.85f;
            else
                style.opacity = 1f;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
