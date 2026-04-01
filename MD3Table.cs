using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Display-only table with header and rows.
    /// Supports string cells, custom VisualElement cells, and zebra striping.
    /// </summary>
    public class MD3Table : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Table, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        protected readonly List<ColumnDef> _columns = new List<ColumnDef>();
        protected readonly VisualElement _header;
        protected readonly ScrollView _body;
        protected readonly List<VisualElement> _rows = new List<VisualElement>();
        protected MD3Theme _theme;

        public struct ColumnDef
        {
            public string Name;
            public float Width;     // 0 = flex-grow
            public TextAnchor Align;
        }

        public MD3Table()
        {
            AddToClassList("md3-table");
            style.flexShrink = 0;

            _header = new VisualElement();
            _header.AddToClassList("md3-table__header");
            _header.style.flexDirection = FlexDirection.Row;
            _header.style.flexShrink = 0;
            _header.style.minHeight = 40;
            _header.style.alignItems = Align.Center;
            _header.style.paddingLeft = MD3Spacing.L;
            _header.style.paddingRight = MD3Spacing.L;
            Add(_header);

            _body = new ScrollView(ScrollViewMode.Vertical);
            _body.AddToClassList("md3-table__body");
            _body.style.flexGrow = 1;
            Add(_body);

            RegisterCallback<AttachToPanelEvent>(_ => RefreshTheme());
        }

        public MD3Table AddColumn(string name, float width = 0f, TextAnchor align = TextAnchor.MiddleLeft)
        {
            _columns.Add(new ColumnDef { Name = name, Width = width, Align = align });
            RebuildHeader();
            return this;
        }

        public MD3Table AddRow(params string[] cells)
        {
            var elements = new VisualElement[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                var label = new Label(cells[i]);
                label.style.fontSize = 14;
                label.style.unityTextAlign = i < _columns.Count ? _columns[i].Align : TextAnchor.MiddleLeft;
                label.style.overflow = Overflow.Hidden;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                elements[i] = label;
            }
            return AddRow(elements);
        }

        public MD3Table AddRow(params VisualElement[] cells)
        {
            var row = CreateRow(cells, _rows.Count);
            _rows.Add(row);
            _body.Add(row);
            return this;
        }

        public void ClearRows()
        {
            _rows.Clear();
            _body.Clear();
        }

        protected virtual void RebuildHeader()
        {
            _header.Clear();
            foreach (var col in _columns)
            {
                var headerCell = new VisualElement();
                ApplyColumnWidth(headerCell, col);
                headerCell.style.paddingRight = MD3Spacing.M;
                headerCell.style.paddingLeft = MD3Spacing.S;
                if (_columns.IndexOf(col) > 0)
                {
                    headerCell.style.borderLeftWidth = 1;
                    headerCell.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 0.15f);
                }
                var cell = new Label(col.Name);
                cell.style.fontSize = 12;
                cell.style.unityFontStyleAndWeight = FontStyle.Bold;
                cell.style.unityTextAlign = col.Align;
                cell.style.overflow = Overflow.Hidden;
                cell.style.whiteSpace = WhiteSpace.NoWrap;
                headerCell.Add(cell);
                _header.Add(headerCell);
            }
        }

        protected VisualElement CreateRow(VisualElement[] cells, int rowIndex)
        {
            var row = new VisualElement();
            row.AddToClassList("md3-table__row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 48;
            row.style.paddingLeft = MD3Spacing.L;
            row.style.paddingRight = MD3Spacing.L;
            row.style.paddingTop = MD3Spacing.S;
            row.style.paddingBottom = MD3Spacing.S;

            for (int i = 0; i < cells.Length; i++)
            {
                var wrapper = new VisualElement();
                ApplyColumnWidth(wrapper, i < _columns.Count ? _columns[i] : default);
                wrapper.style.paddingRight = MD3Spacing.M;
                wrapper.style.paddingLeft = MD3Spacing.S;
                // Column divider (except first column)
                if (i > 0)
                {
                    wrapper.style.borderLeftWidth = 1;
                    wrapper.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 0.15f);
                }
                wrapper.Add(cells[i]);
                row.Add(wrapper);
            }

            ApplyRowColors(row, rowIndex);
            return row;
        }

        protected void ApplyColumnWidth(VisualElement cell, ColumnDef col)
        {
            if (col.Width > 0)
            {
                cell.style.width = col.Width;
                cell.style.flexShrink = 0;
            }
            else
            {
                cell.style.flexGrow = 1;
                cell.style.flexShrink = 1;
            }
        }

        protected void ApplyRowColors(VisualElement row, int rowIndex)
        {
            if (_theme == null) return;
            // Zebra striping with stronger contrast
            row.style.backgroundColor = rowIndex % 2 == 0
                ? Color.clear
                : _theme.SurfaceContainerHigh;
            // Bottom border for row separation
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = _theme.Outline;
        }

        public virtual void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        protected virtual void ApplyColors()
        {
            if (_theme == null) return;

            _header.style.backgroundColor = _theme.SurfaceContainer;
            _header.style.borderBottomWidth = 1;
            _header.style.borderBottomColor = _theme.OutlineVariant;

            // Header cell colors
            for (int i = 0; i < _header.childCount; i++)
            {
                var cell = _header[i];
                cell.style.color = _theme.OnSurfaceVariant;
            }

            // Row colors
            for (int i = 0; i < _rows.Count; i++)
            {
                ApplyRowColors(_rows[i], i);
                // Cell text colors
                for (int j = 0; j < _rows[i].childCount; j++)
                {
                    var wrapper = _rows[i][j];
                    if (wrapper.childCount > 0 && wrapper[0] is Label lbl)
                        lbl.style.color = _theme.OnSurface;
                }
            }
        }

        protected MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
