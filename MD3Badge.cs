using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Badge : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Badge, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _label;
        MD3Theme _theme;

        /// <summary>Dot badge (no text)</summary>
        public MD3Badge()
        {
            AddToClassList("md3-badge");
            AddToClassList("md3-badge--dot");
            _label = null;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        /// <summary>Numeric badge — shows count (99+ if over 99)</summary>
        public MD3Badge(int count)
        {
            AddToClassList("md3-badge");
            AddToClassList("md3-badge--label");

            string text = count > 99 ? "99+" : count.ToString();
            _label = new Label(text);
            _label.pickingMode = PickingMode.Ignore;
            _label.style.fontSize = 10;
            _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.style.marginLeft = 0;
            _label.style.marginRight = 0;
            _label.style.marginTop = 0;
            _label.style.marginBottom = 0;
            _label.style.paddingLeft = 0;
            _label.style.paddingRight = 0;
            _label.style.paddingTop = 0;
            _label.style.paddingBottom = 0;
            Add(_label);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        /// <summary>Text badge — shows arbitrary text</summary>
        public MD3Badge(string text)
        {
            AddToClassList("md3-badge");
            AddToClassList("md3-badge--label");

            _label = new Label(text);
            _label.pickingMode = PickingMode.Ignore;
            _label.style.fontSize = 10;
            _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.style.marginLeft = 0;
            _label.style.marginRight = 0;
            _label.style.marginTop = 0;
            _label.style.marginBottom = 0;
            _label.style.paddingLeft = 0;
            _label.style.paddingRight = 0;
            _label.style.paddingTop = 0;
            _label.style.paddingBottom = 0;
            Add(_label);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.backgroundColor = _theme.Error;
            if (_label != null)
                _label.style.color = _theme.OnError;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
