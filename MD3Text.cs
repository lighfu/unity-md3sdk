using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3TextStyle
    {
        DisplayLarge,
        DisplayMedium,
        DisplaySmall,
        HeadlineLarge,
        HeadlineMedium,
        HeadlineSmall,
        TitleLarge,
        TitleMedium,
        TitleSmall,
        Body,
        BodySmall,
        LabelLarge,
        LabelCaption,
        LabelAnnotation,
    }

    public class MD3Text : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Text, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _label;
        readonly MD3TextStyle _textStyle;
        MD3Theme _theme;
        Color? _colorOverride;

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public Color? ColorOverride
        {
            get => _colorOverride;
            set { _colorOverride = value; ApplyColors(); }
        }

        public MD3Text() : this("Text", MD3TextStyle.Body) { }

        public MD3Text(string text, MD3TextStyle textStyle = MD3TextStyle.Body, Color? color = null)
        {
            _textStyle = textStyle;
            _colorOverride = color;

            AddToClassList("md3-text");
            AddToClassList(StyleClass(textStyle));

            _label = new Label(text);
            _label.AddToClassList("md3-text__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            ApplyTypography(textStyle);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void ApplyTypography(MD3TextStyle s)
        {
            switch (s)
            {
                case MD3TextStyle.DisplayLarge:
                    _label.style.fontSize = 40;
                    break;
                case MD3TextStyle.DisplayMedium:
                    _label.style.fontSize = 32;
                    break;
                case MD3TextStyle.DisplaySmall:
                    _label.style.fontSize = 28;
                    break;
                case MD3TextStyle.HeadlineLarge:
                    _label.style.fontSize = 24;
                    _label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    break;
                case MD3TextStyle.HeadlineMedium:
                    _label.style.fontSize = 20;
                    _label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    break;
                case MD3TextStyle.HeadlineSmall:
                    _label.style.fontSize = 16;
                    _label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    break;
                case MD3TextStyle.TitleLarge:
                    _label.style.fontSize = 18;
                    _label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    break;
                case MD3TextStyle.TitleMedium:
                    _label.style.fontSize = 14;
                    _label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    break;
                case MD3TextStyle.TitleSmall:
                    _label.style.fontSize = 12;
                    _label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    break;
                case MD3TextStyle.Body:
                    _label.style.fontSize = 14;
                    break;
                case MD3TextStyle.BodySmall:
                    _label.style.fontSize = 12;
                    break;
                case MD3TextStyle.LabelLarge:
                    _label.style.fontSize = 13;
                    break;
                case MD3TextStyle.LabelCaption:
                    _label.style.fontSize = 11;
                    break;
                case MD3TextStyle.LabelAnnotation:
                    _label.style.fontSize = 10;
                    break;
            }
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            if (_colorOverride.HasValue)
            {
                _label.style.color = _colorOverride.Value;
                return;
            }

            switch (_textStyle)
            {
                case MD3TextStyle.LabelCaption:
                case MD3TextStyle.LabelAnnotation:
                    _label.style.color = _theme.OnSurfaceVariant;
                    break;
                default:
                    _label.style.color = _theme.OnSurface;
                    break;
            }
        }

        static string StyleClass(MD3TextStyle s)
        {
            switch (s)
            {
                case MD3TextStyle.DisplayLarge:     return "md3-text--display-large";
                case MD3TextStyle.DisplayMedium:    return "md3-text--display-medium";
                case MD3TextStyle.DisplaySmall:     return "md3-text--display-small";
                case MD3TextStyle.HeadlineLarge:    return "md3-text--headline-large";
                case MD3TextStyle.HeadlineMedium:   return "md3-text--headline-medium";
                case MD3TextStyle.HeadlineSmall:    return "md3-text--headline-small";
                case MD3TextStyle.TitleLarge:       return "md3-text--title-large";
                case MD3TextStyle.TitleMedium:      return "md3-text--title-medium";
                case MD3TextStyle.TitleSmall:       return "md3-text--title-small";
                case MD3TextStyle.Body:             return "md3-text--body";
                case MD3TextStyle.BodySmall:        return "md3-text--body-small";
                case MD3TextStyle.LabelLarge:       return "md3-text--label-large";
                case MD3TextStyle.LabelCaption:     return "md3-text--label-caption";
                case MD3TextStyle.LabelAnnotation:  return "md3-text--label-annotation";
                default:                            return "md3-text--body";
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
