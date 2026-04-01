using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Dialog : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Dialog, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action dismissed;

        readonly VisualElement _scrim;
        readonly VisualElement _card;
        readonly Label _title;
        readonly Label _message;
        readonly VisualElement _content;
        readonly VisualElement _actions;
        readonly MD3Button _confirmBtn;
        readonly MD3Button _dismissBtn;
        readonly Action _onConfirm;
        readonly Action _onDismiss;
        MD3Theme _theme;
        MD3AnimationHandle _animHandle;
        VisualElement _shadowAmbient, _shadowKey;

        public VisualElement Content => _content;

        public MD3Dialog() : this("Dialog", "Message") { }

        public MD3Dialog(string title, string message, string confirmLabel = "OK", string dismissLabel = null,
            Action onConfirm = null, Action onDismiss = null)
        {
            _onConfirm = onConfirm;
            _onDismiss = onDismiss;

            // Scrim overlay
            _scrim = new VisualElement();
            _scrim.AddToClassList("md3-dialog__scrim");
            _scrim.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.target == _scrim)
                    Dismiss();
            });
            Add(_scrim);

            // Card
            _card = new VisualElement();
            _card.AddToClassList("md3-dialog__card");

            _title = new Label(title);
            _title.AddToClassList("md3-dialog__title");
            _title.pickingMode = PickingMode.Ignore;
            _card.Add(_title);

            _message = new Label(message);
            _message.AddToClassList("md3-dialog__message");
            _message.pickingMode = PickingMode.Ignore;
            _card.Add(_message);

            // Custom content slot
            _content = new VisualElement();
            _content.AddToClassList("md3-dialog__content");
            _card.Add(_content);

            // Action buttons
            _actions = new VisualElement();
            _actions.AddToClassList("md3-dialog__actions");

            if (dismissLabel != null)
            {
                _dismissBtn = new MD3Button(dismissLabel, MD3ButtonStyle.Text);
                _dismissBtn.clicked += () =>
                {
                    _onDismiss?.Invoke();
                    Dismiss();
                };
                _dismissBtn.style.marginRight = 8;
                _actions.Add(_dismissBtn);
            }

            _confirmBtn = new MD3Button(confirmLabel, MD3ButtonStyle.Text);
            _confirmBtn.clicked += () =>
            {
                _onConfirm?.Invoke();
                Dismiss();
            };
            _actions.Add(_confirmBtn);

            _card.Add(_actions);
            _scrim.Add(_card);
            (_shadowAmbient, _shadowKey) = MD3Elevation.AddSiblingShadow(_scrim, _card, 28f, 3);

            // Make this element fill the parent
            style.position = Position.Absolute;
            style.top = 0;
            style.left = 0;
            style.right = 0;
            style.bottom = 0;

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

            // Animate scrim
            _scrim.style.opacity = 0f;
            MD3Animate.Float(this, 0f, 1f, 120f, MD3Easing.EaseOut, t =>
            {
                _scrim.style.opacity = t;
            });

            // Animate card
            _card.style.opacity = 0f;
            _card.style.scale = new Scale(new Vector3(0.9f, 0.9f, 1f));
            _animHandle = MD3Animate.FadeScale(_card, 0f, 1f, 0.9f, 1f, 120f, MD3Easing.EaseOut);
        }

        public void Dismiss()
        {
            _animHandle?.Cancel();

            MD3Animate.Float(this, 1f, 0f, 100f, MD3Easing.EaseIn, t =>
            {
                _scrim.style.opacity = t;
            });

            _animHandle = MD3Animate.FadeScale(_card, 1f, 0f, 1f, 0.9f, 100f, MD3Easing.EaseIn,
                () =>
                {
                    RemoveFromHierarchy();
                    dismissed?.Invoke();
                });
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            // Scrim
            var scrimColor = _theme.OnSurface;
            scrimColor.a = 0.32f;
            _scrim.style.backgroundColor = scrimColor;

            // Card
            _card.style.backgroundColor = _theme.Surface;

            // Center the card in the scrim
            _scrim.style.alignItems = Align.Center;
            _scrim.style.justifyContent = Justify.Center;

            _title.style.color = _theme.OnSurface;
            _message.style.color = _theme.OnSurfaceVariant;

            MD3Elevation.UpdateShadowColor(_shadowAmbient, _shadowKey, _theme.IsDark, 3);
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
