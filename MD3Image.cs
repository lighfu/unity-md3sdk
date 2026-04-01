using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// How the image fits within the container.
    /// </summary>
    public enum MD3ImageFit
    {
        /// <summary>Scale to cover the entire area, cropping if needed.</summary>
        Cover,
        /// <summary>Scale to fit inside the area, preserving aspect ratio (may letterbox).</summary>
        Contain,
        /// <summary>Stretch to fill the area exactly.</summary>
        Fill,
    }

    /// <summary>
    /// Displays a Texture2D or Sprite with rounded corners and fit modes.
    /// </summary>
    public class MD3Image : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Image, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly VisualElement _imageElement;
        Texture2D _texture;
        MD3ImageFit _fit;

        public Texture2D Texture
        {
            get => _texture;
            set { _texture = value; ApplyImage(); }
        }

        public MD3ImageFit Fit
        {
            get => _fit;
            set { _fit = value; ApplyImage(); }
        }

        public MD3Image() : this(null) { }

        public MD3Image(Texture2D texture, float width = 0f, float height = 0f,
            MD3ImageFit fit = MD3ImageFit.Cover, float radius = 0f)
        {
            _texture = texture;
            _fit = fit;

            AddToClassList("md3-image");
            style.overflow = Overflow.Hidden;
            style.flexShrink = 0;

            if (width > 0) style.width = width;
            if (height > 0) style.height = height;
            if (radius > 0) this.Radius(radius);

            _imageElement = new VisualElement();
            _imageElement.AddToClassList("md3-image__inner");
            _imageElement.pickingMode = PickingMode.Ignore;
            _imageElement.style.position = Position.Absolute;
            _imageElement.style.top = 0;
            _imageElement.style.left = 0;
            _imageElement.style.right = 0;
            _imageElement.style.bottom = 0;
            Add(_imageElement);

            ApplyImage();
        }

        /// <summary>Sets the image from a Sprite asset.</summary>
        public void SetSprite(Sprite sprite)
        {
            if (sprite != null && sprite.texture != null)
                Texture = sprite.texture;
        }

        void ApplyImage()
        {
            if (_texture != null)
            {
                _imageElement.style.backgroundImage = new StyleBackground(_texture);
                _imageElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                _imageElement.style.backgroundImage = StyleKeyword.None;
                _imageElement.style.display = DisplayStyle.None;
                return;
            }

            switch (_fit)
            {
                case MD3ImageFit.Cover:
                    _imageElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    break;
                case MD3ImageFit.Contain:
                    _imageElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    break;
                case MD3ImageFit.Fill:
                    _imageElement.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
                    break;
            }
        }
    }
}
