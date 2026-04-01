using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Tooltip : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Tooltip, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly bool _isRich;
        Label _text;
        Label _title;
        Label _description;
        Label _actionLabel;
        MD3Theme _theme;
        MD3AnimationHandle _animHandle;

        public MD3Tooltip() : this(false) { }

        MD3Tooltip(bool isRich)
        {
            _isRich = isRich;
            AddToClassList("md3-tooltip");
            if (isRich) AddToClassList("md3-tooltip--rich");

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public static MD3Tooltip Plain(string text)
        {
            var tip = new MD3Tooltip(false);

            tip._text = new Label(text);
            tip._text.AddToClassList("md3-tooltip__text");
            tip._text.pickingMode = PickingMode.Ignore;
            tip.Add(tip._text);

            return tip;
        }

        public static MD3Tooltip Rich(string title, string description, string actionLabel = null, Action onAction = null)
        {
            var tip = new MD3Tooltip(true);

            tip._title = new Label(title);
            tip._title.AddToClassList("md3-tooltip__title");
            tip._title.pickingMode = PickingMode.Ignore;
            tip.Add(tip._title);

            tip._description = new Label(description);
            tip._description.AddToClassList("md3-tooltip__description");
            tip._description.pickingMode = PickingMode.Ignore;
            tip.Add(tip._description);

            if (actionLabel != null)
            {
                tip._actionLabel = new Label(actionLabel);
                tip._actionLabel.AddToClassList("md3-tooltip__action-label");
                tip._actionLabel.RegisterCallback<ClickEvent>(e =>
                {
                    onAction?.Invoke();
                    tip.Hide();
                });
                tip.Add(tip._actionLabel);
            }

            return tip;
        }

        public void ShowAt(VisualElement anchor)
        {
            // Find the themed root (element with md3-dark/md3-light class)
            var root = anchor;
            VisualElement themedRoot = null;
            while (root != null)
            {
                if (root.ClassListContains("md3-dark") || root.ClassListContains("md3-light"))
                    themedRoot = root;
                root = root.parent;
            }
            if (themedRoot == null) themedRoot = anchor.parent ?? anchor;
            themedRoot.Add(this);

            // Position below the anchor (one-shot geometry callback)
            EventCallback<GeometryChangedEvent> positionCb = null;
            var posRoot = themedRoot;
            positionCb = e =>
            {
                UnregisterCallback(positionCb);
                var anchorWorld = anchor.worldBound;
                var rootWorld = posRoot.worldBound;
                float left = anchorWorld.x - rootWorld.x;
                float top = anchorWorld.yMax - rootWorld.y + 4f;
                style.left = left;
                style.top = top;
            };
            RegisterCallback(positionCb);

            // Animate in
            style.opacity = 0f;
            style.scale = new Scale(new Vector3(0.9f, 0.9f, 1f));
            _animHandle = MD3Animate.FadeScale(this, 0f, 1f, 0.9f, 1f, 150f, MD3Easing.EaseOut);
        }

        public void Hide()
        {
            _animHandle?.Cancel();
            _animHandle = MD3Animate.FadeScale(this, 1f, 0f, 1f, 0.9f, 100f, MD3Easing.EaseIn,
                () => RemoveFromHierarchy());
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

            if (_isRich)
            {
                style.backgroundColor = _theme.SurfaceContainerHigh;
                if (_title != null) _title.style.color = _theme.OnSurface;
                if (_description != null) _description.style.color = _theme.OnSurfaceVariant;
                if (_actionLabel != null) _actionLabel.style.color = _theme.Primary;
            }
            else
            {
                style.backgroundColor = _theme.InverseSurface;
                if (_text != null) _text.style.color = _theme.InverseOnSurface;
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
