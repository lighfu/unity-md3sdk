using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Horizontal toolbar with icon buttons, dividers, and optional labels.
    /// Typically placed at the top or bottom of a panel.
    /// </summary>
    public class MD3Toolbar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Toolbar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        MD3Theme _theme;

        public MD3Toolbar()
        {
            AddToClassList("md3-toolbar");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexShrink = 0;
            style.height = 48;
            style.paddingLeft = MD3Spacing.S;
            style.paddingRight = MD3Spacing.S;

            RegisterCallback<AttachToPanelEvent>(_ => RefreshTheme());
        }

        /// <summary>Adds a vertical divider separator.</summary>
        public MD3Toolbar AddDivider()
        {
            var div = new VisualElement();
            div.AddToClassList("md3-toolbar__divider");
            div.style.width = 1;
            div.style.alignSelf = Align.Stretch;
            div.style.marginTop = MD3Spacing.S;
            div.style.marginBottom = MD3Spacing.S;
            div.style.marginLeft = MD3Spacing.XS;
            div.style.marginRight = MD3Spacing.XS;
            if (_theme != null) div.style.backgroundColor = _theme.OutlineVariant;
            hierarchy.Add(div);
            return this;
        }

        /// <summary>Adds a flexible spacer that pushes subsequent items to the right.</summary>
        public MD3Toolbar AddSpacer()
        {
            var spacer = new MD3Spacer();
            hierarchy.Add(spacer);
            return this;
        }

        /// <summary>Adds a text label to the toolbar.</summary>
        public MD3Toolbar AddLabel(string text, MD3TextStyle textStyle = MD3TextStyle.TitleSmall)
        {
            var label = new MD3Text(text, textStyle);
            label.style.marginLeft = MD3Spacing.S;
            label.style.marginRight = MD3Spacing.S;
            hierarchy.Add(label);
            return this;
        }

        /// <summary>Adds a pre-built element (button, chip, etc.) to the toolbar.</summary>
        public new MD3Toolbar Add(VisualElement element)
        {
            hierarchy.Add(element);
            return this;
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) return;
            style.backgroundColor = _theme.SurfaceContainer;

            // Update divider colors
            for (int i = 0; i < hierarchy.childCount; i++)
            {
                var child = hierarchy[i];
                if (child.ClassListContains("md3-toolbar__divider"))
                    child.style.backgroundColor = _theme.OutlineVariant;
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
