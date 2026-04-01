using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3NavBarItem : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3NavBarItem, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<bool> changed;

        readonly VisualElement _pill;
        readonly Label _icon;
        readonly Label _label;
        MD3Theme _theme;
        bool _hovered;
        bool _pressed;
        MD3AnimationHandle _iconAnim;
        MD3AnimationHandle _iconScaleAnim;
        MD3AnimationHandle _labelAnim;

        internal VisualElement Pill => _pill;
        internal bool _useSharedHighlight;

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                bool wasSelected = _selected;
                _selected = value;
                ApplyColors();
                if (_selected) AnimateSelect();
                else if (wasSelected) AnimateDeselect();
                changed?.Invoke(_selected);
            }
        }
        bool _selected;

        public MD3NavBarItem() : this(MD3Icon.Home, "Home") { }

        public MD3NavBarItem(string icon, string label, bool selected = false)
        {
            _selected = selected;
            AddToClassList("md3-nav-bar-item");

            _pill = new VisualElement();
            _pill.AddToClassList("md3-nav-bar-item__pill");
            _pill.pickingMode = PickingMode.Ignore;

            _icon = new Label(icon);
            _icon.AddToClassList("md3-nav-bar-item__icon");
            _icon.pickingMode = PickingMode.Ignore;
            MD3Icon.Apply(_icon);
            _pill.Add(_icon);
            Add(_pill);

            _label = new Label(label);
            _label.AddToClassList("md3-nav-bar-item__label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<MouseEnterEvent>(e => { _hovered = true; ApplyColors(); });
            RegisterCallback<MouseLeaveEvent>(e => { _hovered = false; _pressed = false; ApplyColors(); });
            RegisterCallback<MouseDownEvent>(e => { _pressed = true; ApplyColors(); MD3Ripple.Spawn(_pill, e, _theme != null ? _theme.OnSecondaryContainer : Color.white); });
            RegisterCallback<MouseUpEvent>(e => { _pressed = false; ApplyColors(); });
            RegisterCallback<ClickEvent>(OnClick);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void OnClick(ClickEvent evt)
        {
            if (!_selected)
            {
                _selected = true;
                ApplyColors();
                AnimateSelect();
                changed?.Invoke(true);
            }
            // ripple は MouseDown で発火済み
        }

        void AnimateSelect()
        {
            // アイコンバウンス: 上に大きく跳ねて 2 回バウンド
            _iconAnim?.Cancel();
            _iconAnim = MD3Animate.Float(_icon, 0f, 1f, 400f, MD3Easing.Linear, t =>
            {
                float y;
                if (t < 0.3f)
                    y = -7f * (t / 0.3f); // 上に 7px
                else if (t < 0.55f)
                    y = -7f * (1f - (t - 0.3f) / 0.25f); // 戻り
                else if (t < 0.75f)
                    y = -3f * ((t - 0.55f) / 0.2f); // 2 回目 3px
                else if (t < 0.9f)
                    y = -3f * (1f - (t - 0.75f) / 0.15f); // 戻り
                else
                    y = -1f * Mathf.Sin((t - 0.9f) / 0.1f * Mathf.PI); // 微振動
                _icon.style.translate = new Translate(0, y);
            }, () => _icon.style.translate = new Translate(0, 0));

            // アイコンスケールポップ: 1.0 → 1.25 → 1.0
            _iconScaleAnim?.Cancel();
            _iconScaleAnim = MD3Animate.Float(_icon, 0f, 1f, 350f, MD3Easing.EaseOut, t =>
            {
                float s;
                if (t < 0.35f)
                    s = 1f + 0.25f * (t / 0.35f); // 拡大
                else
                    s = 1.25f - 0.25f * ((t - 0.35f) / 0.65f); // 縮小
                _icon.style.scale = new Scale(new Vector2(s, s));
            }, () => _icon.style.scale = new Scale(Vector2.one));

            // ラベルフェードイン + スライドアップ (少し遅延)
            _labelAnim?.Cancel();
            _label.style.opacity = 0;
            _label.style.translate = new Translate(0, 6);
            _labelAnim = MD3Animate.Float(_label, 0f, 1f, 300f, MD3Easing.EaseOut, t =>
            {
                // 少し遅延開始 (t の前半 20% は待機)
                float lt = Mathf.Clamp01((t - 0.2f) / 0.8f);
                _label.style.opacity = lt;
                _label.style.translate = new Translate(0, 6f * (1f - lt));
            }, () =>
            {
                _label.style.opacity = 1;
                _label.style.translate = new Translate(0, 0);
            });
        }

        void AnimateDeselect()
        {
            // アイコンスケールを戻す
            _iconScaleAnim?.Cancel();
            _icon.style.scale = new Scale(Vector2.one);

            // ラベルフェードアウト + スライドダウン
            _labelAnim?.Cancel();
            _labelAnim = MD3Animate.Float(_label, 0f, 1f, 150f, MD3Easing.EaseIn, t =>
            {
                _label.style.opacity = 1f - t;
                _label.style.translate = new Translate(0, 3f * t);
            }, () =>
            {
                _label.style.opacity = 1;
                _label.style.translate = new Translate(0, 0);
            });
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color pillBg, fg;
            if (_selected)
            {
                pillBg = _useSharedHighlight ? Color.clear : _theme.SecondaryContainer;
                fg = _theme.OnSecondaryContainer;
                _label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                pillBg = Color.clear;
                fg = _theme.OnSurfaceVariant;
                _label.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            if (_pressed)
                pillBg = _theme.PressOverlay(pillBg, fg);
            else if (_hovered)
                pillBg = _theme.HoverOverlay(pillBg, fg);

            _pill.style.backgroundColor = pillBg;
            _icon.style.color = fg;
            _label.style.color = fg;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }

    public class MD3NavBar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3NavBar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int> changed;

        readonly List<MD3NavBarItem> _items = new List<MD3NavBarItem>();
        readonly VisualElement _highlight;
        MD3Theme _theme;
        MD3AnimationHandle _highlightAnim;
        MD3AnimationHandle _highlightWidthAnim;
        float _currentLeft = float.NaN;
        const float HighlightBaseWidth = 64f;
        bool _isAnimating;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                UpdateSelection();
                changed?.Invoke(_selectedIndex);
            }
        }
        int _selectedIndex;

        public MD3NavBar() : this(new[] { (MD3Icon.Home, "Home"), (MD3Icon.Star, "Starred") }) { }

        public MD3NavBar((string icon, string label)[] entries, int selectedIndex = 0, Action<int> onChanged = null)
        {
            _selectedIndex = selectedIndex;
            if (onChanged != null) changed += onChanged;

            AddToClassList("md3-nav-bar");
            style.flexDirection = FlexDirection.Row;
            style.height = 64;
            style.flexShrink = 0;

            // Shared sliding highlight (added first so it renders behind items)
            _highlight = new VisualElement();
            _highlight.AddToClassList("md3-nav-bar__highlight");
            _highlight.pickingMode = PickingMode.Ignore;
            _highlight.style.position = Position.Absolute;
            _highlight.style.width = 64;
            _highlight.style.height = 32;
            _highlight.style.borderTopLeftRadius = 16;
            _highlight.style.borderTopRightRadius = 16;
            _highlight.style.borderBottomLeftRadius = 16;
            _highlight.style.borderBottomRightRadius = 16;
            Add(_highlight);

            for (int i = 0; i < entries.Length; i++)
            {
                var item = new MD3NavBarItem(entries[i].icon, entries[i].label, i == selectedIndex);
                item._useSharedHighlight = true;
                var idx = i;
                item.changed += selected =>
                {
                    if (!selected) return;
                    _selectedIndex = idx;
                    UpdateSelection();
                    changed?.Invoke(_selectedIndex);
                };
                _items.Add(item);
                Add(item);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (e.oldRect.size == e.newRect.size) return;
                if (!_isAnimating)
                    PositionHighlight(false);
            });
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
            // Defer initial positioning to ensure children are laid out
            schedule.Execute(() => PositionHighlight(false));
        }

        void UpdateSelection()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (i == _selectedIndex && !_items[i].Selected)
                    _items[i].Selected = true;
                else if (i != _selectedIndex && _items[i].Selected)
                    _items[i].Selected = false;
            }
            PositionHighlight(true);
        }

        void PositionHighlight(bool animate)
        {
            if (_selectedIndex < 0 || _selectedIndex >= _items.Count) return;

            var pill = _items[_selectedIndex].Pill;
            var pillBound = pill.worldBound;
            var barBound = this.worldBound;

            if (barBound.width <= 0 || pillBound.width <= 0) return;

            float targetLeft = pillBound.x - barBound.x;
            float targetTop = pillBound.y - barBound.y;

            _highlight.style.top = targetTop;

            if (!animate || float.IsNaN(_currentLeft))
            {
                _highlightAnim?.Cancel();
                _isAnimating = false;
                _highlight.style.left = targetLeft;
                _currentLeft = targetLeft;
                return;
            }

            _highlightAnim?.Cancel();
            _highlightWidthAnim?.Cancel();
            float fromLeft = _currentLeft;
            float distance = Mathf.Abs(targetLeft - fromLeft);
            _currentLeft = targetLeft;
            _isAnimating = true;

            // pill スライド移動 (EaseOutBack で反動付き)
            _highlightAnim = MD3Animate.Float(_highlight, fromLeft, targetLeft, 350f, MD3Easing.EaseOutBack,
                v => _highlight.style.left = v,
                () => _isAnimating = false);

            // pill ストレッチ: 移動中に横幅が伸びて到着時に縮む
            float stretchExtra = Mathf.Min(distance * 0.3f, 24f);
            _highlightWidthAnim = MD3Animate.Float(_highlight, 0f, 1f, 350f, MD3Easing.Linear, t =>
            {
                // 0→0.4: 伸びる, 0.4→1.0: 縮む (反動に合わせて非対称)
                float stretch = t < 0.4f
                    ? stretchExtra * (t / 0.4f)
                    : stretchExtra * (1f - (t - 0.4f) / 0.6f);
                _highlight.style.width = HighlightBaseWidth + stretch;
            }, () => _highlight.style.width = HighlightBaseWidth);
        }

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            style.backgroundColor = _theme.Surface;
            style.borderTopWidth = 1;
            style.borderTopColor = _theme.OutlineVariant;
            _highlight.style.backgroundColor = _theme.SecondaryContainer;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
