using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Card with a top image area, title, and optional description.
    /// Common pattern for asset browsers and content previews.
    /// </summary>
    public class MD3ImageCard : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3ImageCard, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;

        readonly MD3Image _image;
        readonly VisualElement _body;
        readonly Label _title;
        readonly Label _description;
        MD3Theme _theme;
        bool _hovered;

        public Texture2D Texture
        {
            get => _image.Texture;
            set => _image.Texture = value;
        }

        public string Title
        {
            get => _title.text;
            set => _title.text = value;
        }

        public string Description
        {
            get => _description.text;
            set
            {
                _description.text = value;
                _description.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>Content area below the image — add custom elements here.</summary>
        public VisualElement Content => _body;

        public MD3ImageCard() : this("Card") { }

        public MD3ImageCard(string title, string description = null, Texture2D texture = null,
            float imageHeight = 140f)
        {
            AddToClassList("md3-image-card");
            style.flexShrink = 0;
            style.overflow = Overflow.Hidden;
            this.Radius(MD3Radius.M);

            // Image area
            _image = new MD3Image(texture, height: imageHeight, fit: MD3ImageFit.Cover);
            _image.style.width = new Length(100, LengthUnit.Percent);
            _image.pickingMode = PickingMode.Ignore;
            Add(_image);

            // Body
            _body = new VisualElement();
            _body.style.paddingTop = MD3Spacing.M;
            _body.style.paddingBottom = MD3Spacing.M;
            _body.style.paddingLeft = MD3Spacing.L;
            _body.style.paddingRight = MD3Spacing.L;
            Add(_body);

            _title = new Label(title);
            _title.style.fontSize = 16;
            _title.style.unityFontStyleAndWeight = FontStyle.Bold;
            _title.style.marginBottom = MD3Spacing.XS;
            _body.Add(_title);

            _description = new Label(description ?? "");
            _description.style.fontSize = 14;
            _description.style.whiteSpace = WhiteSpace.Normal;
            if (string.IsNullOrEmpty(description))
                _description.style.display = DisplayStyle.None;
            _body.Add(_description);

            // Events
            RegisterCallback<AttachToPanelEvent>(_ => RefreshTheme());
            RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { if (clicked != null) MD3Ripple.Spawn(this, e, _theme?.OnSurface ?? Color.white); });
            RegisterCallback<ClickEvent>(e =>
            {
                if (clicked == null) return;
                clicked.Invoke();
            });
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) return;

            var bg = _theme.SurfaceContainerHigh;
            if (_hovered && clicked != null)
                bg = _theme.HoverOverlay(bg, _theme.OnSurface);

            style.backgroundColor = bg;
            _title.style.color = _theme.OnSurface;
            _description.style.color = _theme.OnSurfaceVariant;

            if (clicked != null)
                style.cursor = StyleKeyword.None;

            // Placeholder for image area
            if (_image.Texture == null)
                _image.style.backgroundColor = _theme.SurfaceContainerHighest;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
