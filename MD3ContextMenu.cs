using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3ContextMenu : VisualElement, IMD3Themeable
    {
        public event Action<int> selected;

        readonly VisualElement _menu;
        readonly (string label, Action action)[] _items;
        MD3Theme _theme;
        VisualElement _scrim;
        MD3AnimationHandle _menuAnim;
        VisualElement _shadowAmbient, _shadowKey;

        public MD3ContextMenu((string label, Action action)[] items)
        {
            _items = items;

            _menu = new VisualElement();
            _menu.AddToClassList("md3-context-menu");
            _menu.style.position = Position.Absolute;

            for (int i = 0; i < items.Length; i++)
            {
                var item = new MD3MenuItem(items[i].label);
                var idx = i;
                var action = items[i].action;
                item.clicked += () =>
                {
                    action?.Invoke();
                    selected?.Invoke(idx);
                    Close();
                };
                _menu.Add(item);
            }
        }

        public void ShowAt(VisualElement anchor)
        {
            var themedRoot = FindThemedRoot(anchor);
            if (themedRoot == null) themedRoot = anchor.parent ?? anchor;

            AddOverlay(themedRoot);

            _menu.style.opacity = 0f;
            _menu.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
            themedRoot.Add(_menu);
            (_shadowAmbient, _shadowKey) = MD3Elevation.AddSiblingShadow(themedRoot, _menu, 4f, 2);

            EventCallback<GeometryChangedEvent> posCb = null;
            var root = themedRoot;
            posCb = e =>
            {
                _menu.UnregisterCallback(posCb);
                var anchorWorld = anchor.worldBound;
                var rootWorld = root.worldBound;
                float x = anchorWorld.xMin - rootWorld.x;
                float y = anchorWorld.yMax - rootWorld.y + 2f;
                ClampPosition(root, x, y);
            };
            _menu.RegisterCallback(posCb);

            AnimateOpen();
        }

        public void ShowAtPosition(VisualElement parent, float x, float y)
        {
            var themedRoot = FindThemedRoot(parent);
            if (themedRoot == null) themedRoot = parent;

            AddOverlay(themedRoot);

            _menu.style.opacity = 0f;
            _menu.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
            themedRoot.Add(_menu);
            (_shadowAmbient, _shadowKey) = MD3Elevation.AddSiblingShadow(themedRoot, _menu, 4f, 2);

            EventCallback<GeometryChangedEvent> posCb = null;
            var root = themedRoot;
            posCb = e =>
            {
                _menu.UnregisterCallback(posCb);
                ClampPosition(root, x, y);
            };
            _menu.RegisterCallback(posCb);

            AnimateOpen();
        }

        public void Close()
        {
            _menuAnim?.Cancel();
            _menuAnim = MD3Animate.FadeScale(_menu, 1f, 0f, 1f, 0.95f, 70f, MD3Easing.EaseIn, () =>
            {
                _menu.RemoveFromHierarchy();
                _shadowAmbient?.RemoveFromHierarchy();
                _shadowKey?.RemoveFromHierarchy();
                _shadowAmbient = null;
                _shadowKey = null;
                _scrim?.RemoveFromHierarchy();
                _scrim = null;
            });
        }

        void AddOverlay(VisualElement themedRoot)
        {
            _scrim = new VisualElement();
            _scrim.AddToClassList("md3-fab-speed-dial__scrim");
            _scrim.style.backgroundColor = Color.clear;
            _scrim.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                Close();
            });
            themedRoot.Add(_scrim);
        }

        void ClampPosition(VisualElement root, float x, float y)
        {
            var rootWorld = root.worldBound;
            float menuW = _menu.resolvedStyle.width;
            float menuH = _menu.resolvedStyle.height;
            float maxX = rootWorld.width - menuW;
            float maxY = rootWorld.height - menuH;
            if (maxX < 0) maxX = 0;
            if (maxY < 0) maxY = 0;
            _menu.style.left = Mathf.Clamp(x, 0, maxX);
            _menu.style.top = Mathf.Clamp(y, 0, maxY);
        }

        void AnimateOpen()
        {
            _menuAnim?.Cancel();
            _menuAnim = MD3Animate.FadeScale(_menu, 0f, 1f, 0.95f, 1f, 100f, MD3Easing.EaseOut);
            ApplyColors();
        }

        static VisualElement FindThemedRoot(VisualElement from)
        {
            var el = from;
            VisualElement themedRoot = null;
            while (el != null)
            {
                if (el.ClassListContains("md3-dark") || el.ClassListContains("md3-light"))
                    themedRoot = el;
                el = el.parent;
            }
            return themedRoot;
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            _menu.style.backgroundColor = _theme.SurfaceContainerHigh;
            _menu.style.borderBottomWidth = 1;
            _menu.style.borderBottomColor = _theme.Outline;

            MD3Elevation.UpdateShadowColor(_shadowAmbient, _shadowKey, _theme.IsDark, 2);
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(_menu);
    }
}
