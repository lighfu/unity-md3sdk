using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3SelectionMode { None, Single, Multi }

    /// <summary>
    /// Interactive data table extending MD3Table with sorting and row selection.
    /// </summary>
    public class MD3DataTable : MD3Table
    {
        public new class UxmlFactory : UxmlFactory<MD3DataTable, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int, bool> sortChanged;
        public event Action<List<int>> selectionChanged;

        const string ArrowUp = "\ue5d8";
        const string ArrowDown = "\ue5db";
        const string SortIcon = "\ue164";

        readonly List<bool> _sortable = new List<bool>();
        readonly List<Label> _sortIcons = new List<Label>();
        readonly List<string[]> _rowData = new List<string[]>();
        readonly List<int> _sortedOrder = new List<int>();
        readonly HashSet<int> _selectedDataIndices = new HashSet<int>();
        int _sortColumn = -1;
        bool _sortAscending = true;

        public MD3SelectionMode SelectionMode { get; set; } = MD3SelectionMode.None;

        public IReadOnlyCollection<int> SelectedIndices => _selectedDataIndices;

        public MD3DataTable() : base()
        {
            AddToClassList("md3-data-table");
        }

        public MD3DataTable AddColumn(string name, float width = 0f, TextAnchor align = TextAnchor.MiddleLeft,
            bool sortable = false)
        {
            base.AddColumn(name, width, align);
            _sortable.Add(sortable);
            return this;
        }

        public new MD3DataTable AddRow(params string[] cells)
        {
            int dataIndex = _rowData.Count;
            _rowData.Add(cells);
            _sortedOrder.Add(dataIndex);
            RebuildBody();
            return this;
        }

        public new void ClearRows()
        {
            _rowData.Clear();
            _sortedOrder.Clear();
            _selectedDataIndices.Clear();
            _rows.Clear();
            _body.Clear();
        }

        protected override void RebuildHeader()
        {
            _header.Clear();
            _sortIcons.Clear();

            for (int i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                bool sortable = i < _sortable.Count && _sortable[i];

                var cell = new VisualElement();
                cell.style.flexDirection = FlexDirection.Row;
                cell.style.alignItems = Align.Center;
                cell.style.paddingRight = MD3Spacing.M;
                cell.style.paddingLeft = MD3Spacing.S;
                if (i > 0)
                {
                    cell.style.borderLeftWidth = 1;
                    cell.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f, 0.15f);
                }
                ApplyColumnWidth(cell, col);

                var label = new Label(col.Name);
                label.style.fontSize = 12;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.unityTextAlign = col.Align;
                label.style.overflow = Overflow.Hidden;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.flexGrow = 1;
                cell.Add(label);

                Label sortIcon = null;
                if (sortable)
                {
                    sortIcon = new Label(SortIcon);
                    MD3Icon.Apply(sortIcon, 14f);
                    sortIcon.style.opacity = 0.4f;
                    sortIcon.style.marginLeft = 2;
                    cell.Add(sortIcon);

                    cell.style.cursor = StyleKeyword.None;
                    var idx = i;
                    cell.RegisterCallback<ClickEvent>(_ => OnHeaderClick(idx));
                    cell.RegisterCallback<MouseEnterEvent>(_ =>
                        cell.style.backgroundColor = _theme?.HoverOverlay(Color.clear, _theme.OnSurface) ?? Color.clear);
                    cell.RegisterCallback<MouseLeaveEvent>(_ =>
                        cell.style.backgroundColor = Color.clear);
                }
                _sortIcons.Add(sortIcon);

                _header.Add(cell);
            }
        }

        void OnHeaderClick(int columnIndex)
        {
            if (_sortColumn == columnIndex)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = columnIndex;
                _sortAscending = true;
            }

            UpdateSortIcons();
            SortRows();
            sortChanged?.Invoke(_sortColumn, _sortAscending);
        }

        void SortRows()
        {
            if (_sortColumn < 0) return;

            int col = _sortColumn;
            _sortedOrder.Sort((a, b) =>
            {
                string va = col < _rowData[a].Length ? _rowData[a][col] : "";
                string vb = col < _rowData[b].Length ? _rowData[b][col] : "";

                // Try numeric comparison first
                bool aNum = float.TryParse(va, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out float fa);
                bool bNum = float.TryParse(vb, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out float fb);

                int cmp;
                if (aNum && bNum)
                    cmp = fa.CompareTo(fb);
                else
                    cmp = string.Compare(va, vb, StringComparison.OrdinalIgnoreCase);

                return _sortAscending ? cmp : -cmp;
            });

            RebuildBody();
        }

        void RebuildBody()
        {
            _rows.Clear();
            _body.Clear();

            for (int displayIdx = 0; displayIdx < _sortedOrder.Count; displayIdx++)
            {
                int dataIdx = _sortedOrder[displayIdx];
                string[] cells = _rowData[dataIdx];

                var elements = new VisualElement[cells.Length];
                for (int i = 0; i < cells.Length; i++)
                {
                    var label = new Label(cells[i]);
                    label.style.fontSize = 14;
                    label.style.unityTextAlign = i < _columns.Count ? _columns[i].Align : TextAnchor.MiddleLeft;
                    label.style.overflow = Overflow.Hidden;
                    label.style.whiteSpace = WhiteSpace.NoWrap;
                    if (_theme != null) label.style.color = _theme.OnSurface;
                    elements[i] = label;
                }

                var row = CreateRow(elements, displayIdx);
                SetupRowInteraction(row, dataIdx);

                if (_selectedDataIndices.Contains(dataIdx))
                    ApplySelectedColor(row);

                _rows.Add(row);
                _body.Add(row);
            }
        }

        void UpdateSortIcons()
        {
            for (int i = 0; i < _sortIcons.Count; i++)
            {
                var icon = _sortIcons[i];
                if (icon == null) continue;

                if (i == _sortColumn)
                {
                    icon.text = _sortAscending ? ArrowUp : ArrowDown;
                    icon.style.opacity = 1f;
                }
                else
                {
                    icon.text = SortIcon;
                    icon.style.opacity = 0.4f;
                }
            }
        }

        void SetupRowInteraction(VisualElement row, int dataIndex)
        {
            row.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (SelectionMode == MD3SelectionMode.None) return;
                if (!_selectedDataIndices.Contains(dataIndex) && _theme != null)
                {
                    int displayIdx = _sortedOrder.IndexOf(dataIndex);
                    row.style.backgroundColor = _theme.HoverOverlay(
                        displayIdx % 2 == 0 ? Color.clear : _theme.SurfaceContainerLowest,
                        _theme.OnSurface);
                }
            });

            row.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!_selectedDataIndices.Contains(dataIndex))
                {
                    int displayIdx = _sortedOrder.IndexOf(dataIndex);
                    ApplyRowColors(row, displayIdx);
                }
            });

            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (SelectionMode == MD3SelectionMode.None) return;
                ToggleSelection(dataIndex);
            });
        }

        void ToggleSelection(int dataIndex)
        {
            if (SelectionMode == MD3SelectionMode.Single)
            {
                bool wasSelected = _selectedDataIndices.Contains(dataIndex);
                _selectedDataIndices.Clear();

                if (!wasSelected)
                    _selectedDataIndices.Add(dataIndex);
            }
            else if (SelectionMode == MD3SelectionMode.Multi)
            {
                if (_selectedDataIndices.Contains(dataIndex))
                    _selectedDataIndices.Remove(dataIndex);
                else
                    _selectedDataIndices.Add(dataIndex);
            }

            // Refresh all row visuals
            for (int i = 0; i < _rows.Count; i++)
            {
                int di = _sortedOrder[i];
                if (_selectedDataIndices.Contains(di))
                    ApplySelectedColor(_rows[i]);
                else
                    ApplyRowColors(_rows[i], i);
            }

            selectionChanged?.Invoke(new List<int>(_selectedDataIndices));
        }

        void ApplySelectedColor(VisualElement row)
        {
            if (_theme != null)
                row.style.backgroundColor = _theme.PressOverlay(
                    _theme.SurfaceContainerLowest, _theme.Primary);
        }

        public void ClearSelection()
        {
            _selectedDataIndices.Clear();
            for (int i = 0; i < _rows.Count; i++)
                ApplyRowColors(_rows[i], i);
            selectionChanged?.Invoke(new List<int>());
        }

        protected override void ApplyColors()
        {
            base.ApplyColors();

            // Sort icon colors
            for (int i = 0; i < _sortIcons.Count; i++)
            {
                var icon = _sortIcons[i];
                if (icon != null && _theme != null)
                    icon.style.color = _theme.OnSurfaceVariant;
            }

            // Header label colors
            for (int i = 0; i < _header.childCount; i++)
            {
                var cell = _header[i];
                var label = cell.Q<Label>();
                if (label != null) label.style.color = _theme?.OnSurfaceVariant ?? Color.white;
            }

            // Re-apply selection
            for (int i = 0; i < _rows.Count; i++)
            {
                int di = _sortedOrder[i];
                if (_selectedDataIndices.Contains(di))
                    ApplySelectedColor(_rows[i]);
            }
        }
    }
}
