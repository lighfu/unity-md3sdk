using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>Shared hierarchy and placement operations for themed popups.</summary>
    internal static class MD3Overlay
    {
        // Popups belong to the outermost themed root so they are not clipped by
        // an inner themed container. A theme applied to a single component is not
        // a host. Callers choose their own unthemed fallback.
        internal static VisualElement FindThemedRoot(VisualElement from)
        {
            VisualElement themedRoot = null;
            for (var element = from; element != null; element = element.parent)
            {
                if ((element.ClassListContains("md3-dark") || element.ClassListContains("md3-light")) &&
                    !element.ClassListContains("md3-theme-component"))
                    themedRoot = element;
            }
            return themedRoot;
        }

        // The host may sit outside the anchor's theme scope, so the popup carries
        // the anchor's palette and font itself. Call before adding the popup.
        internal static void InheritScope(VisualElement popup, VisualElement anchor)
        {
            MD3Theme.LinkScope(popup, MD3Theme.Resolve(anchor));
            var font = anchor.resolvedStyle.unityFontDefinition;
            if (font.fontAsset != null || font.font != null)
                popup.style.unityFontDefinition = font;
            else
                popup.style.unityFontDefinition = StyleKeyword.Null;
        }

        internal static VisualElement AddScrim(VisualElement parent, Color color, Action onClick)
        {
            var scrim = new VisualElement();
            scrim.AddToClassList("md3-fab-speed-dial__scrim");
            scrim.style.backgroundColor = color;
            scrim.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                onClick();
            });
            parent.Add(scrim);
            return scrim;
        }

        internal static void OnNextGeometry(VisualElement element, Action callback)
        {
            EventCallback<GeometryChangedEvent> onGeometry = null;
            onGeometry = evt =>
            {
                element.UnregisterCallback(onGeometry);
                callback();
            };
            element.RegisterCallback(onGeometry);
        }

        internal static void PlaceBelow(VisualElement popup, VisualElement anchor, VisualElement root,
            float gap = 2f, bool matchWidth = false)
        {
            var anchorBounds = anchor.worldBound;
            var rootBounds = root.worldBound;
            popup.style.left = anchorBounds.x - rootBounds.x;
            popup.style.top = anchorBounds.yMax - rootBounds.y + gap;
            if (matchWidth)
                popup.style.width = anchorBounds.width;
        }
    }
}
