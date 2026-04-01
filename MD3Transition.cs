using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Animated show/hide transitions for VisualElements.
    /// Call MD3Transition.FadeIn/SlideIn etc. to animate an element into view,
    /// and FadeOut/SlideOut to animate it out (optionally removing from hierarchy).
    /// </summary>
    public static class MD3Transition
    {
        const float DefaultDurationMs = 200f;

        // ═══════════════════════════════════════════
        //  Fade
        // ═══════════════════════════════════════════

        public static MD3AnimationHandle FadeIn(VisualElement el, float durationMs = DefaultDurationMs,
            Action onComplete = null)
        {
            el.style.display = DisplayStyle.Flex;
            el.style.opacity = 0f;
            return MD3Animate.Float(el, 0f, 1f, durationMs, MD3Easing.EaseOut, t =>
            {
                el.style.opacity = t;
            }, onComplete);
        }

        public static MD3AnimationHandle FadeOut(VisualElement el, float durationMs = DefaultDurationMs,
            bool remove = false, Action onComplete = null)
        {
            return MD3Animate.Float(el, 1f, 0f, durationMs, MD3Easing.EaseIn, t =>
            {
                el.style.opacity = t;
            }, () =>
            {
                el.style.display = DisplayStyle.None;
                if (remove) el.RemoveFromHierarchy();
                onComplete?.Invoke();
            });
        }

        // ═══════════════════════════════════════════
        //  Slide (vertical)
        // ═══════════════════════════════════════════

        public static MD3AnimationHandle SlideDown(VisualElement el, float durationMs = 250f,
            Action onComplete = null)
        {
            el.style.display = DisplayStyle.Flex;
            el.style.opacity = 0f;
            el.style.translate = new Translate(0, -12);
            return MD3Animate.Float(el, 0f, 1f, durationMs, MD3Easing.EaseOut, t =>
            {
                el.style.opacity = t;
                el.style.translate = new Translate(0, Mathf.Lerp(-12f, 0f, t));
            }, () =>
            {
                el.style.translate = new Translate(0, 0);
                onComplete?.Invoke();
            });
        }

        public static MD3AnimationHandle SlideUp(VisualElement el, float durationMs = 250f,
            bool remove = false, Action onComplete = null)
        {
            return MD3Animate.Float(el, 0f, 1f, durationMs, MD3Easing.EaseIn, t =>
            {
                el.style.opacity = 1f - t;
                el.style.translate = new Translate(0, Mathf.Lerp(0f, -12f, t));
            }, () =>
            {
                el.style.display = DisplayStyle.None;
                el.style.translate = new Translate(0, 0);
                if (remove) el.RemoveFromHierarchy();
                onComplete?.Invoke();
            });
        }

        // ═══════════════════════════════════════════
        //  Scale (pop in/out)
        // ═══════════════════════════════════════════

        public static MD3AnimationHandle ScaleIn(VisualElement el, float durationMs = 200f,
            Action onComplete = null)
        {
            el.style.display = DisplayStyle.Flex;
            el.style.opacity = 0f;
            el.style.scale = new Scale(new Vector3(0.8f, 0.8f, 1f));
            return MD3Animate.Float(el, 0f, 1f, durationMs, MD3Easing.EaseOut, t =>
            {
                el.style.opacity = t;
                float s = Mathf.Lerp(0.8f, 1f, t);
                el.style.scale = new Scale(new Vector3(s, s, 1f));
            }, () =>
            {
                el.style.scale = new Scale(Vector3.one);
                onComplete?.Invoke();
            });
        }

        public static MD3AnimationHandle ScaleOut(VisualElement el, float durationMs = 200f,
            bool remove = false, Action onComplete = null)
        {
            return MD3Animate.Float(el, 0f, 1f, durationMs, MD3Easing.EaseIn, t =>
            {
                el.style.opacity = 1f - t;
                float s = Mathf.Lerp(1f, 0.8f, t);
                el.style.scale = new Scale(new Vector3(s, s, 1f));
            }, () =>
            {
                el.style.display = DisplayStyle.None;
                el.style.scale = new Scale(Vector3.one);
                if (remove) el.RemoveFromHierarchy();
                onComplete?.Invoke();
            });
        }

        // ═══════════════════════════════════════════
        //  Expand / Collapse (height animation)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Expands element from 0 height to its natural height.
        /// The element should be inside a wrapper with overflow:hidden.
        /// </summary>
        public static MD3AnimationHandle Expand(VisualElement el, float targetHeight,
            float durationMs = 250f, Action onComplete = null)
        {
            el.style.display = DisplayStyle.Flex;
            el.style.height = 0;
            el.style.overflow = Overflow.Hidden;
            return MD3Animate.Float(el, 0f, targetHeight, durationMs, MD3Easing.EaseOut, h =>
            {
                el.style.height = h;
            }, () =>
            {
                el.style.height = StyleKeyword.Auto;
                el.style.overflow = StyleKeyword.Null;
                onComplete?.Invoke();
            });
        }

        public static MD3AnimationHandle Collapse(VisualElement el, float durationMs = 250f,
            Action onComplete = null)
        {
            float currentHeight = el.resolvedStyle.height;
            if (float.IsNaN(currentHeight)) currentHeight = 0f;
            el.style.overflow = Overflow.Hidden;
            return MD3Animate.Float(el, currentHeight, 0f, durationMs, MD3Easing.EaseIn, h =>
            {
                el.style.height = Mathf.Max(0f, h);
            }, () =>
            {
                el.style.display = DisplayStyle.None;
                el.style.height = StyleKeyword.Auto;
                el.style.overflow = StyleKeyword.Null;
                onComplete?.Invoke();
            });
        }

        // ═══════════════════════════════════════════
        //  Crossfade (swap two elements)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Fades out 'from' and fades in 'to' simultaneously.
        /// </summary>
        public static void Crossfade(VisualElement from, VisualElement to,
            float durationMs = DefaultDurationMs, Action onComplete = null)
        {
            FadeOut(from, durationMs);
            FadeIn(to, durationMs, onComplete);
        }
    }
}
