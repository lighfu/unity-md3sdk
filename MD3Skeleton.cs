using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Skeleton : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Skeleton, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        static Texture2D _shimmerTex;
        static Texture2D GetShimmerTexture()
        {
            if (_shimmerTex != null) return _shimmerTex;
            const int w = 64;
            _shimmerTex = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w];
            for (int i = 0; i < w; i++)
            {
                float t = (float)i / (w - 1);
                float a = Mathf.Exp(-8f * (t - 0.5f) * (t - 0.5f));
                pixels[i] = new Color(1, 1, 1, a);
            }
            _shimmerTex.SetPixels(pixels);
            _shimmerTex.Apply();
            return _shimmerTex;
        }

        readonly VisualElement _shimmer;
        MD3Theme _theme;
        double _startTime;

        public MD3Skeleton() : this(200f, 16f) { }

        public MD3Skeleton(float width, float height, bool circle = false)
        {
            AddToClassList("md3-skeleton");
            if (circle) AddToClassList("md3-skeleton--circle");

            style.width = width;
            style.height = height;

            _shimmer = new VisualElement();
            _shimmer.AddToClassList("md3-skeleton__shimmer");
            _shimmer.pickingMode = PickingMode.Ignore;
            Add(_shimmer);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
            _startTime = EditorApplication.timeSinceStartup;
            MD3AnimLoop.Register(this);
            generateVisualContent += OnShimmerRepaint;
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            MD3AnimLoop.Unregister(this);
            generateVisualContent -= OnShimmerRepaint;
        }

        void OnShimmerRepaint(MeshGenerationContext mgc)
        {
            double elapsed = EditorApplication.timeSinceStartup - _startTime;
            float offset = (float)(elapsed * 0.8 % 1.8) - 0.4f;
            _shimmer.style.left = new Length(offset * 100f, LengthUnit.Percent);
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.backgroundColor = _theme.SurfaceVariant;
            var shimmerColor = _theme.Surface;
            shimmerColor.a = 0.6f;
            _shimmer.style.backgroundColor = Color.clear;
            _shimmer.style.backgroundImage = new StyleBackground(GetShimmerTexture());
            _shimmer.style.unityBackgroundImageTintColor = shimmerColor;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
