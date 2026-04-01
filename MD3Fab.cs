using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3FabSize { Standard, Small, Large }

    public class MD3Fab : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Fab, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;

        readonly Label _icon;
        readonly Label _label;
        readonly MD3FabSize _size;
        readonly string _originalIcon;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        // Speed dial
        List<(string icon, string label, Action action)> _speedDialItems;
        VisualElement _speedDialContainer;
        VisualElement _speedDialScrim;
        bool _speedDialOpen;
        readonly List<MD3AnimationHandle> _speedDialAnims = new List<MD3AnimationHandle>();
        MD3AnimationHandle _fabRotateAnim;

        public MD3Fab() : this(MD3Icon.Add) { }

        /// <summary>Standard or Extended FAB. Pass label for Extended variant.</summary>
        public MD3Fab(string icon, string label = null, MD3FabSize size = MD3FabSize.Standard)
        {
            _size = size;
            _originalIcon = icon;
            AddToClassList("md3-fab");
            if (size == MD3FabSize.Small) AddToClassList("md3-fab--small");
            if (size == MD3FabSize.Large) AddToClassList("md3-fab--large");

            _icon = new Label(icon);
            _icon.AddToClassList("md3-fab__icon");
            _icon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_icon);
            Add(_icon);

            if (label != null)
            {
                _label = new Label(label);
                _label.AddToClassList("md3-fab__label");
                _label.pickingMode = PickingMode.Ignore;
                Add(_label);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(e => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(e => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { _pressed = true; ApplyColors(); MD3Ripple.Spawn(this, e, _theme != null ? _theme.OnPrimaryContainer : Color.white); });
            RegisterCallback<MouseUpEvent>(e => { _pressed = false; ApplyColors(); });
            RegisterCallback<ClickEvent>(OnClick);
        }

        public MD3Fab MakeFloating(float bottom = 16f, float right = 16f)
        {
            style.position = Position.Absolute;
            style.bottom = bottom;
            style.right = right;
            AddToClassList("md3-fab--floating");
            return this;
        }

        public void SetSpeedDial(params (string icon, string label, Action action)[] items)
        {
            _speedDialItems = new List<(string, string, Action)>(items);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void OnClick(ClickEvent evt)
        {
            if (_speedDialItems != null && _speedDialItems.Count > 0)
                ToggleSpeedDial();
            else
                clicked?.Invoke();
        }

        void ToggleSpeedDial()
        {
            if (_speedDialOpen)
                CloseSpeedDial();
            else
                OpenSpeedDial();
        }

        void OpenSpeedDial()
        {
            _speedDialOpen = true;
            if (_theme == null) _theme = ResolveTheme();

            // Find themed root
            var root = this as VisualElement;
            VisualElement themedRoot = null;
            while (root != null)
            {
                if (root.ClassListContains("md3-dark") || root.ClassListContains("md3-light"))
                    themedRoot = root;
                root = root.parent;
            }
            if (themedRoot == null) themedRoot = this.parent ?? this;

            // Add scrim
            _speedDialScrim = new VisualElement();
            _speedDialScrim.AddToClassList("md3-fab-speed-dial__scrim");
            _speedDialScrim.style.backgroundColor = new Color(0, 0, 0, 0.32f);
            _speedDialScrim.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                CloseSpeedDial();
            });
            themedRoot.Add(_speedDialScrim);

            // Create speed dial container
            _speedDialContainer = new VisualElement();
            _speedDialContainer.AddToClassList("md3-fab-speed-dial");
            themedRoot.Add(_speedDialContainer);

            // Position relative to FAB
            EventCallback<GeometryChangedEvent> positionCb = null;
            var posRoot = themedRoot;
            positionCb = e =>
            {
                _speedDialContainer.UnregisterCallback(positionCb);
                var fabWorld = this.worldBound;
                var rootWorld = posRoot.worldBound;
                _speedDialContainer.style.right = rootWorld.xMax - fabWorld.xMax;
                _speedDialContainer.style.bottom = rootWorld.yMax - fabWorld.y + 8f;
            };
            _speedDialContainer.RegisterCallback(positionCb);

            // Create speed dial items with staggered animation
            for (int i = 0; i < _speedDialItems.Count; i++)
            {
                var data = _speedDialItems[i];
                var itemRow = new VisualElement();
                itemRow.AddToClassList("md3-fab-speed-dial__item");

                var itemLabel = new Label(data.label);
                itemLabel.AddToClassList("md3-fab-speed-dial__label");
                itemLabel.style.backgroundColor = _theme.SurfaceContainerHigh;
                itemLabel.style.color = _theme.OnSurface;
                itemRow.Add(itemLabel);

                var miniFab = new MD3Fab(data.icon, null, MD3FabSize.Small);
                var action = data.action;
                miniFab.clicked += () =>
                {
                    CloseSpeedDial();
                    action?.Invoke();
                };
                itemRow.Add(miniFab);

                itemRow.style.opacity = 0f;
                itemRow.style.scale = new Scale(new Vector3(0.5f, 0.5f, 1f));
                _speedDialContainer.Add(itemRow);

                // Staggered animation
                var anim = MD3Animate.Delayed(itemRow, i * 50f, () =>
                {
                    MD3Animate.FadeScale(itemRow, 0f, 1f, 0.5f, 1f, 200f, MD3Easing.EaseOut);
                });
                _speedDialAnims.Add(anim);
            }

            // Rotate FAB icon
            _fabRotateAnim?.Cancel();
            _fabRotateAnim = MD3Animate.Float(this, 0f, 45f, 200f, MD3Easing.EaseOut,
                v => _icon.style.rotate = new Rotate(v));
        }

        void CloseSpeedDial()
        {
            _speedDialOpen = false;

            // Cancel pending animations
            foreach (var anim in _speedDialAnims)
                anim?.Cancel();
            _speedDialAnims.Clear();

            // Animate items out in reverse
            if (_speedDialContainer != null)
            {
                int childCount = _speedDialContainer.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    var child = _speedDialContainer[i];
                    int reverseIdx = childCount - 1 - i;
                    MD3Animate.Delayed(child, reverseIdx * 30f, () =>
                    {
                        MD3Animate.FadeScale(child, 1f, 0f, 1f, 0.5f, 150f, MD3Easing.EaseIn);
                    });
                }

                // Remove after all animations complete
                float totalCloseMs = (childCount - 1) * 30f + 150f + 50f;
                MD3Animate.Delayed(_speedDialContainer, totalCloseMs, () =>
                {
                    _speedDialContainer?.RemoveFromHierarchy();
                    _speedDialContainer = null;
                    _speedDialScrim?.RemoveFromHierarchy();
                    _speedDialScrim = null;
                });
            }

            // Rotate FAB icon back
            _fabRotateAnim?.Cancel();
            _fabRotateAnim = MD3Animate.Float(this, 45f, 0f, 200f, MD3Easing.EaseIn,
                v => _icon.style.rotate = new Rotate(v));
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            var bg = _theme.PrimaryContainer;
            var fg = _theme.OnPrimaryContainer;

            if (_pressed)
                bg = _theme.PressOverlay(bg, fg);
            else if (_hovered)
                bg = _theme.HoverOverlay(bg, fg);

            style.backgroundColor = bg;
            _icon.style.color = fg;
            if (_label != null) _label.style.color = fg;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
