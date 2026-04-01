using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3ListItem : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3ListItem, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action clicked;

        readonly VisualElement _avatar;
        readonly Label _avatarText;
        readonly Label _headline;
        readonly Label _supporting;
        readonly Label _trailing;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;

        public MD3ListItem() : this("Item") { }

        public MD3ListItem(string headline, string supporting = null, string avatar = null, string trailing = null)
        {
            AddToClassList("md3-list-item");

            // Avatar
            if (avatar != null)
            {
                _avatar = new VisualElement();
                _avatar.AddToClassList("md3-list-item__avatar");
                _avatar.pickingMode = PickingMode.Ignore;

                _avatarText = new Label(avatar);
                _avatarText.AddToClassList("md3-list-item__avatar-text");
                _avatarText.pickingMode = PickingMode.Ignore;
                _avatar.Add(_avatarText);
                Add(_avatar);
            }

            // Body column
            var body = new VisualElement();
            body.AddToClassList("md3-list-item__body");
            body.pickingMode = PickingMode.Ignore;

            _headline = new Label(headline);
            _headline.AddToClassList("md3-list-item__headline");
            _headline.pickingMode = PickingMode.Ignore;
            body.Add(_headline);

            if (supporting != null)
            {
                _supporting = new Label(supporting);
                _supporting.AddToClassList("md3-list-item__supporting");
                _supporting.pickingMode = PickingMode.Ignore;
                body.Add(_supporting);
            }
            Add(body);

            // Trailing
            if (trailing != null)
            {
                _trailing = new Label(trailing);
                _trailing.AddToClassList("md3-list-item__trailing");
                _trailing.pickingMode = PickingMode.Ignore;
                Add(_trailing);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(e => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(e => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { _pressed = true; ApplyColors(); MD3Ripple.Spawn(this, e, _theme != null ? _theme.OnSurface : Color.white); });
            RegisterCallback<MouseUpEvent>(e => { _pressed = false; ApplyColors(); });
            RegisterCallback<ClickEvent>(OnClick);
        }

        /// <summary>アバター領域にサムネイル画像を設定。</summary>
        public void SetAvatarImage(Texture2D tex)
        {
            if (_avatar == null) return;
            _avatar.style.backgroundImage = new StyleBackground(tex);
            _avatar.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            if (_avatarText != null)
                _avatarText.style.display = DisplayStyle.None;
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void OnClick(ClickEvent evt)
        {
            clicked?.Invoke();
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            var bg = Color.clear;
            if (_pressed)
                bg = _theme.PressOverlay(bg, _theme.OnSurface);
            else if (_hovered)
                bg = _theme.HoverOverlay(bg, _theme.OnSurface);

            style.backgroundColor = bg;

            _headline.style.color = _theme.OnSurface;
            if (_supporting != null) _supporting.style.color = _theme.OnSurfaceVariant;
            if (_trailing != null) _trailing.style.color = _theme.OnSurfaceVariant;

            if (_avatar != null)
            {
                _avatar.style.backgroundColor = _theme.PrimaryContainer;
                _avatarText.style.color = _theme.OnPrimaryContainer;
            }
        }

        /// <summary>Update headline text (for virtual list rebinding).</summary>
        public void SetHeadline(string text) => _headline.text = text;

        /// <summary>Update supporting text (for virtual list rebinding).</summary>
        public void SetSupporting(string text)
        {
            if (_supporting != null) _supporting.text = text;
        }

        /// <summary>Update trailing text (for virtual list rebinding).</summary>
        public void SetTrailing(string text)
        {
            if (_trailing != null) _trailing.text = text;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
