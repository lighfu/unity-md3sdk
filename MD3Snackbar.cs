using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Snackbar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Snackbar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly Label _message;
        readonly Label _action;
        readonly Action _onAction;
        MD3Theme _theme;
        IVisualElementScheduledItem _autoDismiss;
        MD3AnimationHandle _animHandle;
        bool _actionHovered;
        VisualElement _shadowAmbient, _shadowKey;

        public MD3Snackbar() : this("Message") { }

        public MD3Snackbar(string message, string actionLabel = null, Action onAction = null)
        {
            _onAction = onAction;
            AddToClassList("md3-snackbar");

            _message = new Label(message);
            _message.AddToClassList("md3-snackbar__message");
            _message.pickingMode = PickingMode.Ignore;
            Add(_message);

            if (actionLabel != null)
            {
                _action = new Label(actionLabel);
                _action.AddToClassList("md3-snackbar__action");
                _action.RegisterCallback<ClickEvent>(e =>
                {
                    _onAction?.Invoke();
                    Dismiss();
                });
                _action.RegisterCallback<MouseEnterEvent>(e => { _actionHovered = true; ApplyColors(); });
                _action.RegisterCallback<MouseLeaveEvent>(e => { _actionHovered = false; ApplyColors(); });
                Add(_action);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        public void Show(VisualElement parent)
        {
            parent.Add(this);
            (_shadowAmbient, _shadowKey) = MD3Elevation.AddSiblingShadow(parent, this, 4f, 2);
            ApplyColors();

            // Fade in
            style.opacity = 0f;
            style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
            _animHandle = MD3Animate.FadeScale(this, 0f, 1f, 0.95f, 1f, 120f, MD3Easing.EaseOut);

            // Auto dismiss after 3 seconds
            _autoDismiss = schedule.Execute(Dismiss);
            _autoDismiss.ExecuteLater(3000);
        }

        public void Dismiss()
        {
            _autoDismiss?.Pause();
            _autoDismiss = null;
            _animHandle?.Cancel();
            _animHandle = MD3Animate.FadeScale(this, 1f, 0f, 1f, 0.95f, 100f, MD3Easing.EaseIn,
                () =>
                {
                    _shadowAmbient?.RemoveFromHierarchy();
                    _shadowKey?.RemoveFromHierarchy();
                    RemoveFromHierarchy();
                });
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.backgroundColor = _theme.InverseSurface;
            _message.style.color = _theme.InverseOnSurface;
            MD3Elevation.UpdateShadowColor(_shadowAmbient, _shadowKey, _theme.IsDark, 2);

            if (_action != null)
            {
                _action.style.color = _theme.InversePrimary;
                var bg = Color.clear;
                if (_actionHovered)
                    bg = _theme.HoverOverlay(bg, _theme.InversePrimary);
                _action.style.backgroundColor = bg;
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
