using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3SegmentedButton : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3SegmentedButton, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int> changed;

        readonly List<VisualElement> _segments = new List<VisualElement>();
        readonly List<Label> _checkLabels = new List<Label>();
        readonly List<Label> _textLabels = new List<Label>();
        readonly List<VisualElement> _dividers = new List<VisualElement>();
        readonly string[] _labels;
        MD3Theme _theme;
        int _hoveredIndex = -1;
        int _pressedIndex = -1;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                ApplyColors();
                changed?.Invoke(_selectedIndex);
            }
        }
        int _selectedIndex;

        public MD3SegmentedButton() : this(new[] { "A", "B", "C" }) { }

        public MD3SegmentedButton(string[] labels, int selectedIndex = 0, Action<int> onChanged = null)
        {
            _labels = labels;
            _selectedIndex = selectedIndex;
            if (onChanged != null) changed += onChanged;

            AddToClassList("md3-segmented");

            for (int i = 0; i < labels.Length; i++)
            {
                // Divider between segments
                if (i > 0)
                {
                    var div = new VisualElement();
                    div.AddToClassList("md3-segmented__divider");
                    div.pickingMode = PickingMode.Ignore;
                    _dividers.Add(div);
                    Add(div);
                }

                var seg = new VisualElement();
                seg.AddToClassList("md3-segmented__item");

                var check = new Label(MD3Icon.Check);
                check.AddToClassList("md3-segmented__check");
                check.pickingMode = PickingMode.Ignore;
                MD3Icon.Apply(check, 14f);
                seg.Add(check);
                _checkLabels.Add(check);

                var lbl = new Label(labels[i]);
                lbl.AddToClassList("md3-segmented__label");
                lbl.pickingMode = PickingMode.Ignore;
                if (labels[i].Length > 0 && labels[i][0] >= '\uE000')
                    MD3Icon.Apply(lbl, 18f);
                seg.Add(lbl);
                _textLabels.Add(lbl);

                var idx = i;
                seg.RegisterCallback<MouseEnterEvent>(e => { _hoveredIndex = idx; ApplyColors(); });
                seg.RegisterCallback<MouseLeaveEvent>(e => { _hoveredIndex = -1; _pressedIndex = -1; ApplyColors(); });
                seg.RegisterCallback<MouseDownEvent>(e => { _pressedIndex = idx; ApplyColors(); MD3Ripple.Spawn(seg, e, _theme != null ? _theme.OnSecondaryContainer : Color.white); });
                seg.RegisterCallback<MouseUpEvent>(e => { _pressedIndex = -1; ApplyColors(); });
                seg.RegisterCallback<ClickEvent>(e =>
                {
                    if (_selectedIndex == idx) return;
                    _selectedIndex = idx;
                    ApplyColors();
                    changed?.Invoke(_selectedIndex);
                });

                _segments.Add(seg);
                Add(seg);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
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

            // Outer border
            style.borderTopColor = _theme.Outline;
            style.borderBottomColor = _theme.Outline;
            style.borderLeftColor = _theme.Outline;
            style.borderRightColor = _theme.Outline;

            for (int i = 0; i < _segments.Count; i++)
            {
                bool selected = i == _selectedIndex;
                var seg = _segments[i];

                Color bg, fg;
                if (selected)
                {
                    bg = _theme.SecondaryContainer;
                    fg = _theme.OnSecondaryContainer;
                }
                else
                {
                    bg = Color.clear;
                    fg = _theme.OnSurface;
                }

                if (i == _pressedIndex)
                    bg = _theme.PressOverlay(bg, fg);
                else if (i == _hoveredIndex)
                    bg = _theme.HoverOverlay(bg, fg);

                seg.style.backgroundColor = bg;
                _textLabels[i].style.color = fg;
                _checkLabels[i].style.color = fg;
                _checkLabels[i].style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Divider colors
            foreach (var div in _dividers)
                div.style.backgroundColor = _theme.Outline;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
