using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Material Design 3 date picker with calendar popup.
    /// </summary>
    public class MD3DatePicker : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3DatePicker, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<DateTime> changed;

        readonly VisualElement _field;
        readonly Label _floatingLabel;
        readonly Label _valueLabel;
        readonly Label _calendarIcon;
        VisualElement _popup;
        VisualElement _scrim;
        MD3Theme _theme;
        bool _focused;
        bool _hovered;
        DateTime _value;
        DateTime _viewMonth;

        public DateTime Value
        {
            get => _value;
            set
            {
                _value = value.Date;
                _viewMonth = new DateTime(_value.Year, _value.Month, 1);
                _valueLabel.text = _value.ToString("yyyy/MM/dd");
                changed?.Invoke(_value);
            }
        }

        public string LabelText
        {
            get => _floatingLabel.text;
            set => _floatingLabel.text = value;
        }

        public MD3DatePicker() : this("Date") { }

        public MD3DatePicker(string label, DateTime? initialValue = null)
        {
            _value = initialValue?.Date ?? DateTime.Today;
            _viewMonth = new DateTime(_value.Year, _value.Month, 1);

            AddToClassList("md3-datepicker");
            style.flexShrink = 0;
            style.overflow = Overflow.Visible;

            // Field (looks like outlined text field)
            _field = new VisualElement();
            _field.style.flexDirection = FlexDirection.Row;
            _field.style.alignItems = Align.Center;
            _field.style.height = 56;
            _field.style.borderTopLeftRadius = MD3Radius.XS;
            _field.style.borderTopRightRadius = MD3Radius.XS;
            _field.style.borderBottomLeftRadius = MD3Radius.XS;
            _field.style.borderBottomRightRadius = MD3Radius.XS;
            _field.style.borderTopWidth = 1;
            _field.style.borderBottomWidth = 1;
            _field.style.borderLeftWidth = 1;
            _field.style.borderRightWidth = 1;
            _field.style.paddingLeft = MD3Spacing.L;
            _field.style.paddingRight = MD3Spacing.S;
            _field.style.cursor = StyleKeyword.None;
            Add(_field);

            // Floating label (always floated)
            _floatingLabel = new Label(label);
            _floatingLabel.style.position = Position.Absolute;
            _floatingLabel.style.fontSize = 12;
            _floatingLabel.style.top = -8;
            _floatingLabel.style.left = 12;
            _floatingLabel.style.paddingLeft = 4;
            _floatingLabel.style.paddingRight = 4;
            _floatingLabel.pickingMode = PickingMode.Ignore;
            _field.Add(_floatingLabel);

            // Value display
            _valueLabel = new Label(_value.ToString("yyyy/MM/dd"));
            _valueLabel.style.fontSize = 16;
            _valueLabel.style.flexGrow = 1;
            _valueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _valueLabel.pickingMode = PickingMode.Ignore;
            _field.Add(_valueLabel);

            // Calendar icon
            _calendarIcon = MD3Icon.Create(MD3Icon.CalendarMonth, 20f);
            _field.Add(_calendarIcon);

            // Events
            _field.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                TogglePopup();
            });
            _field.RegisterCallback<MouseEnterEvent>(_ => { _hovered = true; ApplyFieldColors(); });
            _field.RegisterCallback<MouseLeaveEvent>(_ => { _hovered = false; ApplyFieldColors(); });
            RegisterCallback<AttachToPanelEvent>(_ => RefreshTheme());
        }

        void TogglePopup()
        {
            if (_popup != null)
                ClosePopup();
            else
                OpenPopup();
        }

        void OpenPopup()
        {
            if (_popup != null) return;
            _focused = true;
            ApplyFieldColors();

            var root = GetRootElement();
            if (root == null) return;

            // Scrim
            _scrim = new VisualElement();
            _scrim.style.position = Position.Absolute;
            _scrim.style.top = 0; _scrim.style.left = 0;
            _scrim.style.right = 0; _scrim.style.bottom = 0;
            _scrim.RegisterCallback<ClickEvent>(e => { e.StopPropagation(); ClosePopup(); });
            root.Add(_scrim);

            // Ensure theme is resolved
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) _theme = MD3Theme.Auto();

            // Popup
            _popup = new VisualElement();
            _popup.style.position = Position.Absolute;
            _popup.style.width = 308;
            _popup.style.backgroundColor = _theme.SurfaceContainerHigh;
            _popup.style.color = _theme.OnSurface;
            _popup.Radius(MD3Radius.L);
            _popup.Padding(MD3Spacing.L);
            _popup.style.borderTopWidth = 1;
            _popup.style.borderBottomWidth = 1;
            _popup.style.borderLeftWidth = 1;
            _popup.style.borderRightWidth = 1;
            _popup.style.borderTopColor = _theme.OutlineVariant;
            _popup.style.borderBottomColor = _theme.OutlineVariant;
            _popup.style.borderLeftColor = _theme.OutlineVariant;
            _popup.style.borderRightColor = _theme.OutlineVariant;
            root.Add(_popup);

            BuildCalendar();
            PositionPopup(root);
        }

        void PositionPopup(VisualElement root)
        {
            // Wait one frame for layout to resolve
            schedule.Execute(() =>
            {
                var fieldWorld = _field.worldBound;
                var rootWorld = root.worldBound;
                float left = fieldWorld.x - rootWorld.x;
                float belowY = fieldWorld.yMax - rootWorld.y + 4;
                float popupHeight = _popup.resolvedStyle.height;
                if (float.IsNaN(popupHeight)) popupHeight = 380;

                // If popup would overflow bottom, show above the field
                float maxY = rootWorld.height;
                float top;
                if (belowY + popupHeight > maxY)
                    top = fieldWorld.y - rootWorld.y - popupHeight - 4;
                else
                    top = belowY;

                _popup.style.left = Mathf.Max(0, left);
                _popup.style.top = Mathf.Max(0, top);
            });
        }

        void ClosePopup()
        {
            _focused = false;
            ApplyFieldColors();
            _scrim?.RemoveFromHierarchy();
            _popup?.RemoveFromHierarchy();
            _scrim = null;
            _popup = null;
        }

        void BuildCalendar()
        {
            if (_popup == null) return;
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;
            _popup.Clear();

            // ── Header: < Month Year > ──
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = MD3Spacing.M;

            var prevBtn = new MD3IconButton(MD3Icon.ChevronLeft, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small);
            prevBtn.clicked += () => { _viewMonth = _viewMonth.AddMonths(-1); BuildCalendar(); };
            header.Add(prevBtn);

            var monthLabel = new Label(_viewMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture));
            monthLabel.style.flexGrow = 1;
            monthLabel.style.fontSize = 16;
            monthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            monthLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            monthLabel.style.color = _theme.OnSurface;
            header.Add(monthLabel);

            var nextBtn = new MD3IconButton(MD3Icon.ChevronRight, MD3IconButtonStyle.Standard, MD3IconButtonSize.Small);
            nextBtn.clicked += () => { _viewMonth = _viewMonth.AddMonths(1); BuildCalendar(); };
            header.Add(nextBtn);

            _popup.Add(header);

            // ── Day of week labels ──
            var dowRow = new VisualElement();
            dowRow.style.flexDirection = FlexDirection.Row;
            dowRow.style.marginBottom = MD3Spacing.XS;
            var dowNames = new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
            foreach (var d in dowNames)
            {
                var lbl = new Label(d);
                lbl.style.width = new Length(100f / 7f, LengthUnit.Percent);
                lbl.style.fontSize = 12;
                lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                lbl.style.color = _theme.OnSurfaceVariant;
                dowRow.Add(lbl);
            }
            _popup.Add(dowRow);

            // ── Day grid ──
            int daysInMonth = DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month);
            int firstDow = (int)_viewMonth.DayOfWeek; // 0=Sun
            DateTime today = DateTime.Today;

            VisualElement weekRow = null;
            for (int slot = 0; slot < 42; slot++)
            {
                if (slot % 7 == 0)
                {
                    weekRow = new VisualElement();
                    weekRow.style.flexDirection = FlexDirection.Row;
                    weekRow.style.height = 36;
                    _popup.Add(weekRow);
                }

                int dayNum = slot - firstDow + 1;
                if (dayNum < 1 || dayNum > daysInMonth)
                {
                    // Empty cell
                    var empty = new VisualElement();
                    empty.style.width = new Length(100f / 7f, LengthUnit.Percent);
                    weekRow.Add(empty);
                    continue;
                }

                var date = new DateTime(_viewMonth.Year, _viewMonth.Month, dayNum);
                bool isToday = date == today;
                bool isSelected = date == _value;

                var dayBtn = new VisualElement();
                dayBtn.style.width = new Length(100f / 7f, LengthUnit.Percent);
                dayBtn.style.alignItems = Align.Center;
                dayBtn.style.justifyContent = Justify.Center;
                dayBtn.style.cursor = StyleKeyword.None;

                var circle = new VisualElement();
                circle.style.width = 32;
                circle.style.height = 32;
                circle.style.borderTopLeftRadius = 16;
                circle.style.borderTopRightRadius = 16;
                circle.style.borderBottomLeftRadius = 16;
                circle.style.borderBottomRightRadius = 16;
                circle.style.alignItems = Align.Center;
                circle.style.justifyContent = Justify.Center;

                var dayLabel = new Label(dayNum.ToString());
                dayLabel.style.fontSize = 14;
                dayLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                dayLabel.pickingMode = PickingMode.Ignore;

                if (isSelected)
                {
                    circle.style.backgroundColor = _theme.Primary;
                    dayLabel.style.color = _theme.OnPrimary;
                }
                else if (isToday)
                {
                    circle.style.borderTopWidth = 2;
                    circle.style.borderBottomWidth = 2;
                    circle.style.borderLeftWidth = 2;
                    circle.style.borderRightWidth = 2;
                    circle.style.borderTopColor = _theme.Primary;
                    circle.style.borderBottomColor = _theme.Primary;
                    circle.style.borderLeftColor = _theme.Primary;
                    circle.style.borderRightColor = _theme.Primary;
                    dayLabel.style.color = _theme.Primary;
                }
                else
                {
                    dayLabel.style.color = _theme.OnSurface;
                }

                circle.Add(dayLabel);
                dayBtn.Add(circle);

                // Hover
                var capturedCircle = circle;
                var capturedIsSelected = isSelected;
                dayBtn.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (!capturedIsSelected)
                        capturedCircle.style.backgroundColor = _theme.HoverOverlay(Color.clear, _theme.OnSurface);
                });
                dayBtn.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (!capturedIsSelected)
                        capturedCircle.style.backgroundColor = Color.clear;
                });

                // Click
                var capturedDate = date;
                dayBtn.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    Value = capturedDate;
                    ClosePopup();
                });

                weekRow.Add(dayBtn);

                // Stop after last needed week
                if (dayNum == daysInMonth && slot % 7 == 6)
                    break;
            }

            // ── Footer: Today button ──
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.marginTop = MD3Spacing.S;

            var todayBtn = new MD3Button("Today", MD3ButtonStyle.Text, size: MD3ButtonSize.Small);
            todayBtn.clicked += () =>
            {
                Value = DateTime.Today;
                ClosePopup();
            };
            footer.Add(todayBtn);
            _popup.Add(footer);
        }

        VisualElement GetRootElement()
        {
            // Walk up to find the EditorWindow's rootVisualElement (has md3-dark/md3-light class)
            var el = this.parent;
            while (el != null)
            {
                if (el.ClassListContains("md3-dark") || el.ClassListContains("md3-light"))
                    return el;
                el = el.parent;
            }
            // Fallback to panel root
            return panel?.visualTree;
        }

        // ── Theme ──

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyFieldColors();
        }

        void ApplyFieldColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            Color borderColor, labelColor;

            if (_focused)
            {
                borderColor = _theme.Primary;
                labelColor = _theme.Primary;
                _field.style.borderTopWidth = 2;
                _field.style.borderBottomWidth = 2;
                _field.style.borderLeftWidth = 2;
                _field.style.borderRightWidth = 2;
            }
            else if (_hovered)
            {
                borderColor = _theme.OnSurface;
                labelColor = _theme.OnSurfaceVariant;
                _field.style.borderTopWidth = 1;
                _field.style.borderBottomWidth = 1;
                _field.style.borderLeftWidth = 1;
                _field.style.borderRightWidth = 1;
            }
            else
            {
                borderColor = _theme.Outline;
                labelColor = _theme.OnSurfaceVariant;
                _field.style.borderTopWidth = 1;
                _field.style.borderBottomWidth = 1;
                _field.style.borderLeftWidth = 1;
                _field.style.borderRightWidth = 1;
            }

            _field.style.borderTopColor = borderColor;
            _field.style.borderBottomColor = borderColor;
            _field.style.borderLeftColor = borderColor;
            _field.style.borderRightColor = borderColor;
            _floatingLabel.style.color = labelColor;
            _valueLabel.style.color = _theme.OnSurface;
            _calendarIcon.style.color = _theme.OnSurfaceVariant;

            // Label notch background
            var bg = _theme.Surface;
            var el = this.parent;
            while (el != null)
            {
                var resolved = el.resolvedStyle.backgroundColor;
                if (resolved.a > 0.01f) { bg = resolved; break; }
                el = el.parent;
            }
            _floatingLabel.style.backgroundColor = bg;
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
