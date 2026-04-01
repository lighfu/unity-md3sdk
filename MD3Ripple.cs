using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Ripple effect element added as a child on click.
    /// Expands from click position, then fades out and self-removes.
    /// Parent must have overflow:hidden to clip.
    /// </summary>
    public class MD3Ripple : VisualElement
    {
        const float Duration = 400f; // ms
        const float FadeStart = 200f; // ms — start fading after this

        public MD3Ripple(Vector2 localPos, float maxRadius, Color rippleColor)
        {
            AddToClassList("md3-ripple");

            style.width = 0;
            style.height = 0;
            style.left = localPos.x;
            style.top = localPos.y;
            style.backgroundColor = rippleColor;
            style.opacity = 0.12f;

            double startTime = EditorApplication.timeSinceStartup;

            MD3AnimLoop.Register(this);

            schedule.Execute(() =>
            {
                float elapsed = (float)((EditorApplication.timeSinceStartup - startTime) * 1000.0);
                float t = Mathf.Clamp01(elapsed / Duration);
                // Ease out quad
                float eased = 1f - (1f - t) * (1f - t);
                float size = maxRadius * 2f * eased;
                style.width = size;
                style.height = size;
                style.left = localPos.x - size * 0.5f;
                style.top = localPos.y - size * 0.5f;
                style.borderTopLeftRadius = size * 0.5f;
                style.borderTopRightRadius = size * 0.5f;
                style.borderBottomLeftRadius = size * 0.5f;
                style.borderBottomRightRadius = size * 0.5f;

                if (elapsed > FadeStart)
                {
                    float fadeT = Mathf.Clamp01((elapsed - FadeStart) / (Duration - FadeStart));
                    style.opacity = 0.12f * (1f - fadeT);
                }

                if (elapsed >= Duration)
                {
                    MD3AnimLoop.Unregister(this);
                    RemoveFromHierarchy();
                }
            }).Every(16);
        }

        /// <summary>
        /// Spawns a ripple on the target element using a ClickEvent.
        /// Correctly converts world position to target-local coordinates,
        /// even when a child element receives the click.
        /// </summary>
        public static void Spawn(VisualElement target, ClickEvent evt, Color fgColor)
        {
            var local = target.WorldToLocal(evt.position);
            Spawn(target, local, fgColor);
        }

        /// <summary>
        /// Spawns a ripple on mouse down (pressed instant).
        /// </summary>
        public static void Spawn(VisualElement target, MouseDownEvent evt, Color fgColor)
        {
            var local = target.WorldToLocal(evt.mousePosition);
            Spawn(target, local, fgColor);
        }

        /// <summary>
        /// Spawns a ripple on the target element at the given local mouse position.
        /// </summary>
        public static void Spawn(VisualElement target, Vector2 localMousePos, Color fgColor)
        {
            var rect = target.contentRect;
            // Max radius = distance from click to farthest corner
            float maxRadius = 0f;
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(localMousePos, new Vector2(0, 0)));
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(localMousePos, new Vector2(rect.width, 0)));
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(localMousePos, new Vector2(0, rect.height)));
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(localMousePos, new Vector2(rect.width, rect.height)));

            var ripple = new MD3Ripple(localMousePos, maxRadius, fgColor);
            target.Add(ripple);
        }
    }
}
