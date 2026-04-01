using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3GridMode
    {
        /// <summary>Fixed column count — cells stretch to fill container width.</summary>
        FixedColumns,
        /// <summary>Fixed cell width — columns auto-calculated, cells stay at exact width.</summary>
        FixedCellWidth,
        /// <summary>Dynamic cell width — columns auto-calculated from min width, cells stretch to fill remaining space.</summary>
        DynamicCellWidth,
    }

    /// <summary>
    /// Grid layout that wraps children into columns with optional gap.
    /// Three modes:
    /// - FixedColumns: MD3Grid(columns, gap) — cell width = container / columns
    /// - FixedCellWidth: MD3Grid(FixedCellWidth, cellWidth, gap) — cells stay fixed,余白 remains
    /// - DynamicCellWidth: MD3Grid(DynamicCellWidth, minCellWidth, gap) — cells stretch to fill width
    /// Implemented via flex-wrap since Unity 2022 UI Toolkit lacks CSS Grid.
    /// </summary>
    public class MD3Grid : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MD3Grid, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        readonly int _requestedColumns;
        int _columns;
        readonly float _gap;
        readonly float _cellWidth;
        readonly MD3GridMode _mode;

        /// <summary>FixedColumns モードで1カラムに折り畳む最小セル幅 (px)。0 で無効。</summary>
        public float MinCellWidth { get; set; } = 250f;

        public MD3Grid() : this(2) { }

        /// <summary>Fixed column count mode — cells stretch to fill container width.</summary>
        public MD3Grid(int columns, float gap = MD3Spacing.S)
        {
            _mode = MD3GridMode.FixedColumns;
            _requestedColumns = Mathf.Max(1, columns);
            _columns = _requestedColumns;
            _gap = gap;
            _cellWidth = 0;
            Init();
        }

        /// <summary>
        /// Auto-column mode with fixed or dynamic cell width.
        /// FixedCellWidth: cells stay at exact cellWidth, remaining space is unused.
        /// DynamicCellWidth: cellWidth is minimum, cells stretch to fill container.
        /// </summary>
        public MD3Grid(MD3GridMode mode, float cellWidth, float gap = MD3Spacing.S)
        {
            _mode = mode == MD3GridMode.FixedColumns ? MD3GridMode.DynamicCellWidth : mode;
            _cellWidth = Mathf.Max(1f, cellWidth);
            _gap = gap;
            _requestedColumns = 1;
            _columns = 1;
            Init();
        }

        void Init()
        {
            AddToClassList("md3-grid");
            style.flexDirection = FlexDirection.Row;
            style.flexWrap = Wrap.Wrap;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        float _lastWidth;

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // evt.newRect.width を使って最新の幅を取得
            float containerWidth = evt.newRect.width;
            if (float.IsNaN(containerWidth) || containerWidth <= 0) return;

            // padding を差し引いた実際の利用可能幅
            float pl = resolvedStyle.paddingLeft;
            float pr = resolvedStyle.paddingRight;
            if (!float.IsNaN(pl)) containerWidth -= pl;
            if (!float.IsNaN(pr)) containerWidth -= pr;
            if (containerWidth <= 0) return;

            // 幅が変わっていなければスキップ (子マージン変更の連鎖を防止)
            if (Mathf.Abs(containerWidth - _lastWidth) < 0.5f) return;
            _lastWidth = containerWidth;

            if (_mode == MD3GridMode.FixedColumns)
            {
                // レスポンシブ: セル幅が MinCellWidth を下回る場合カラム数を減らす
                if (MinCellWidth > 0)
                {
                    int maxCols = Mathf.Max(1, Mathf.FloorToInt((containerWidth + _gap) / (MinCellWidth + _gap)));
                    _columns = Mathf.Min(_requestedColumns, maxCols);
                }
                else
                {
                    _columns = _requestedColumns;
                }
            }
            else
            {
                _columns = Mathf.Max(1, Mathf.FloorToInt((containerWidth + _gap) / (_cellWidth + _gap)));
            }

            float totalGapWidth = _gap * (_columns - 1);
            float cellWidth;
            switch (_mode)
            {
                case MD3GridMode.FixedCellWidth:
                    cellWidth = _cellWidth;
                    break;
                case MD3GridMode.DynamicCellWidth:
                case MD3GridMode.FixedColumns:
                default:
                    // Floor で切り捨て — 合計がコンテナ幅を超えないことを保証
                    cellWidth = Mathf.Floor((containerWidth - totalGapWidth) / _columns);
                    break;
            }

            for (int i = 0; i < childCount; i++)
            {
                var child = ElementAt(i);
                int col = i % _columns;

                // 最終列のセルは余った端数ピクセルを吸収
                float w = (col == _columns - 1)
                    ? containerWidth - cellWidth * (_columns - 1) - totalGapWidth
                    : cellWidth;

                child.style.width = w;
                child.style.marginBottom = _gap;
                child.style.marginRight = (col < _columns - 1) ? _gap : 0;
                child.style.marginLeft = 0;
            }
        }
    }
}
