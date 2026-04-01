using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Extension methods for VisualElement to set spacing tokens without raw px values.
    /// Chainable API: element.Padding(MD3Spacing.L).Margin(MD3Spacing.M)
    /// </summary>
    public static class MD3Layout
    {
        // ── Padding ──

        public static T Padding<T>(this T el, float all) where T : VisualElement
        {
            el.style.paddingTop = all;
            el.style.paddingBottom = all;
            el.style.paddingLeft = all;
            el.style.paddingRight = all;
            return el;
        }

        public static T Padding<T>(this T el, float vertical, float horizontal) where T : VisualElement
        {
            el.style.paddingTop = vertical;
            el.style.paddingBottom = vertical;
            el.style.paddingLeft = horizontal;
            el.style.paddingRight = horizontal;
            return el;
        }

        public static T Padding<T>(this T el, float top, float right, float bottom, float left) where T : VisualElement
        {
            el.style.paddingTop = top;
            el.style.paddingRight = right;
            el.style.paddingBottom = bottom;
            el.style.paddingLeft = left;
            return el;
        }

        // ── Margin ──

        public static T Margin<T>(this T el, float all) where T : VisualElement
        {
            el.style.marginTop = all;
            el.style.marginBottom = all;
            el.style.marginLeft = all;
            el.style.marginRight = all;
            return el;
        }

        public static T Margin<T>(this T el, float vertical, float horizontal) where T : VisualElement
        {
            el.style.marginTop = vertical;
            el.style.marginBottom = vertical;
            el.style.marginLeft = horizontal;
            el.style.marginRight = horizontal;
            return el;
        }

        public static T Margin<T>(this T el, float top, float right, float bottom, float left) where T : VisualElement
        {
            el.style.marginTop = top;
            el.style.marginRight = right;
            el.style.marginBottom = bottom;
            el.style.marginLeft = left;
            return el;
        }

        // ── Size ──

        public static T Size<T>(this T el, float width, float height) where T : VisualElement
        {
            el.style.width = width;
            el.style.height = height;
            return el;
        }

        public static T MinSize<T>(this T el, float minWidth, float minHeight) where T : VisualElement
        {
            el.style.minWidth = minWidth;
            el.style.minHeight = minHeight;
            return el;
        }

        public static T MaxSize<T>(this T el, float maxWidth, float maxHeight) where T : VisualElement
        {
            el.style.maxWidth = maxWidth;
            el.style.maxHeight = maxHeight;
            return el;
        }

        // ── Flex ──

        public static T Grow<T>(this T el, float grow = 1f) where T : VisualElement
        {
            el.style.flexGrow = grow;
            return el;
        }

        public static T Shrink<T>(this T el, float shrink = 1f) where T : VisualElement
        {
            el.style.flexShrink = shrink;
            return el;
        }

        // ── Border radius ──

        public static T Radius<T>(this T el, float radius) where T : VisualElement
        {
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
            return el;
        }

        // ── Alignment ──

        public static T AlignSelf<T>(this T el, Align align) where T : VisualElement
        {
            el.style.alignSelf = align;
            return el;
        }

        // ── Visibility ──

        public static T Opacity<T>(this T el, float opacity) where T : VisualElement
        {
            el.style.opacity = opacity;
            return el;
        }

        public static T Visible<T>(this T el, bool visible) where T : VisualElement
        {
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return el;
        }

        // ── Overflow ──

        public static T ClipContent<T>(this T el) where T : VisualElement
        {
            el.style.overflow = Overflow.Hidden;
            return el;
        }

        // ── Position ──

        public static T Absolute<T>(this T el) where T : VisualElement
        {
            el.style.position = Position.Absolute;
            return el;
        }

        public static T Inset<T>(this T el, float top, float right, float bottom, float left) where T : VisualElement
        {
            el.style.position = Position.Absolute;
            el.style.top = top;
            el.style.right = right;
            el.style.bottom = bottom;
            el.style.left = left;
            return el;
        }

        public static T InsetAll<T>(this T el, float value = 0f) where T : VisualElement
        {
            el.style.position = Position.Absolute;
            el.style.top = value;
            el.style.right = value;
            el.style.bottom = value;
            el.style.left = value;
            return el;
        }
    }
}
