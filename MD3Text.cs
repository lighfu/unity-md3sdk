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
        LabelMedium,
        LabelSmall,
        LabelAnnotation,
        [System.Obsolete("Use LabelSmall instead")]
        LabelCaption,
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

            RegisterCallback<AttachToPanelEvent>(OnAttach);
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

            if (_colorOverride.HasValue)
            {
                _label.style.color = _colorOverride.Value;
                return;
            }

            switch (_textStyle)
            {
                case MD3TextStyle.LabelMedium:
                case MD3TextStyle.LabelSmall:
#pragma warning disable CS0618
                case MD3TextStyle.LabelCaption:
#pragma warning restore CS0618
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
                case MD3TextStyle.LabelMedium:      return "md3-text--label-medium";
                case MD3TextStyle.LabelSmall:       return "md3-text--label-small";
#pragma warning disable CS0618
                case MD3TextStyle.LabelCaption:     return "md3-text--label-small";
#pragma warning restore CS0618
                case MD3TextStyle.LabelAnnotation:  return "md3-text--label-annotation";
                default:                            return "md3-text--body";
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
