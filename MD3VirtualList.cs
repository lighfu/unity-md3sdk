using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// High-performance virtual scrolling list. Only renders visible items,
    /// recycling off-screen elements via an object pool.
    /// Requires fixed item height for O(1) index calculation.
    /// </summary>
    public class MD3VirtualList<T> : VisualElement, IMD3Themeable
    {
        public event Action<int, T> itemClicked;

        readonly float _itemHeight;
        readonly int _overscan;
        readonly ScrollView _scrollView;
        readonly VisualElement _topSpacer;
        readonly VisualElement _bottomSpacer;
        readonly VisualElement _container;
        readonly Queue<VisualElement> _pool = new Queue<VisualElement>();
        readonly List<VisualElement> _activeItems = new List<VisualElement>();

        IReadOnlyList<T> _data;
        Action<T, VisualElement, int> _bindCallback;
        Func<VisualElement> _makeItem;

        int _firstVisible = -1;
        int _lastVisible = -1;
        float _lastScrollY = -1f;
        MD3Theme _theme;

        public MD3VirtualList(float itemHeight = 48f, int overscan = 2)
        {
            _itemHeight = itemHeight;
            _overscan = overscan;
            AddToClassList("md3-virtual-list");
            style.flexGrow = 1;
            style.overflow = Overflow.Hidden;

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1;
            hierarchy.Add(_scrollView);

            _topSpacer = new VisualElement();
            _topSpacer.AddToClassList("md3-virtual-list__spacer");
            _topSpacer.style.flexShrink = 0;
            _scrollView.contentContainer.Add(_topSpacer);

            _container = new VisualElement();
            _container.AddToClassList("md3-virtual-list__container");
            _container.style.flexDirection = FlexDirection.Column;
            _container.style.flexShrink = 0;
            _scrollView.contentContainer.Add(_container);

            _bottomSpacer = new VisualElement();
            _bottomSpacer.AddToClassList("md3-virtual-list__spacer");
            _bottomSpacer.style.flexShrink = 0;
            _scrollView.contentContainer.Add(_bottomSpacer);

            _scrollView.verticalScroller.valueChanged += _ => OnScroll();
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<GeometryChangedEvent>(_ => OnScroll());
        }

        /// <summary>
        /// Set or replace the data source and binding functions.
        /// </summary>
        /// <param name="data">Data items.</param>
        /// <param name="bind">Called to bind data to a VisualElement. (item, element, index)</param>
        /// <param name="makeItem">Factory for new item elements. Defaults to MD3ListItem.</param>
        public void SetData(IReadOnlyList<T> data, Action<T, VisualElement, int> bind,
            Func<VisualElement> makeItem = null)
        {
            _data = data;
            _bindCallback = bind;
            _makeItem = makeItem ?? (() => new MD3ListItem());

            // Reset state
            RecycleAll();
            _firstVisible = -1;
            _lastVisible = -1;
            _lastScrollY = -1f;

            OnScroll();
        }

        /// <summary>Refresh a single item's binding.</summary>
        public void RefreshItem(int index)
        {
            if (index < _firstVisible || index > _lastVisible) return;
            int slot = index - _firstVisible;
            if (slot >= 0 && slot < _activeItems.Count)
                _bindCallback?.Invoke(_data[index], _activeItems[slot], index);
        }

        /// <summary>Refresh all visible items.</summary>
        public void RefreshAll()
        {
            _firstVisible = -1; // Force full rebuild
            OnScroll();
        }

        /// <summary>Scroll to bring the specified index into view.</summary>
        public void ScrollToIndex(int index)
        {
            if (_data == null || index < 0 || index >= _data.Count) return;
            _scrollView.scrollOffset = new Vector2(0, index * _itemHeight);
        }

        public void RefreshTheme()
        {
            _theme = MD3Theme.Resolve(this);
            // Refresh all active items
            foreach (var item in _activeItems)
            {
                if (item is IMD3Themeable t)
                    t.RefreshTheme();
            }
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            _theme = MD3Theme.Resolve(this);
            schedule.Execute(OnScroll);
        }

        void OnScroll()
        {
            if (_data == null || _data.Count == 0)
            {
                RecycleAll();
                _topSpacer.style.height = 0;
                _bottomSpacer.style.height = 0;
                return;
            }

            float scrollY = _scrollView.scrollOffset.y;
            float viewportH = _scrollView.contentViewport.resolvedStyle.height;
            if (float.IsNaN(viewportH) || viewportH <= 0) return;

            // Skip if scroll position hasn't changed enough
            if (Mathf.Abs(scrollY - _lastScrollY) < _itemHeight * 0.25f &&
                _firstVisible >= 0)
                return;
            _lastScrollY = scrollY;

            int totalCount = _data.Count;
            int newFirst = Mathf.Max(0, Mathf.FloorToInt(scrollY / _itemHeight) - _overscan);
            int newLast = Mathf.Min(totalCount - 1,
                Mathf.CeilToInt((scrollY + viewportH) / _itemHeight) + _overscan);

            if (newFirst == _firstVisible && newLast == _lastVisible) return;

            UpdateVisibleRange(newFirst, newLast, totalCount);
        }

        void UpdateVisibleRange(int newFirst, int newLast, int totalCount)
        {
            // Recycle items outside new range
            if (_firstVisible >= 0)
            {
                for (int i = _activeItems.Count - 1; i >= 0; i--)
                {
                    int dataIdx = _firstVisible + i;
                    if (dataIdx < newFirst || dataIdx > newLast)
                    {
                        RecycleItem(i);
                    }
                }
            }

            // Determine which indices need new items
            int oldFirst = _firstVisible;
            int oldLast = _lastVisible;
            _firstVisible = newFirst;
            _lastVisible = newLast;

            // Rebuild active items list in correct order
            var newActive = new List<VisualElement>(newLast - newFirst + 1);
            for (int idx = newFirst; idx <= newLast; idx++)
            {
                VisualElement item = null;

                // Check if already active
                if (oldFirst >= 0)
                {
                    int oldSlot = idx - oldFirst;
                    if (oldSlot >= 0 && oldSlot < _activeItems.Count)
                    {
                        item = _activeItems[oldSlot];
                        _activeItems[oldSlot] = null; // mark as consumed
                    }
                }

                if (item == null)
                {
                    item = GetOrCreateItem();
                    BindItem(item, idx);
                }

                newActive.Add(item);
            }

            // Recycle any remaining unconsumed items
            for (int i = 0; i < _activeItems.Count; i++)
            {
                if (_activeItems[i] != null)
                    ReturnToPool(_activeItems[i]);
            }

            _activeItems.Clear();
            _activeItems.AddRange(newActive);

            // Rebuild container
            _container.Clear();
            foreach (var item in _activeItems)
                _container.Add(item);

            // Update spacers
            _topSpacer.style.height = newFirst * _itemHeight;
            _bottomSpacer.style.height = Mathf.Max(0, (totalCount - newLast - 1) * _itemHeight);
        }

        void BindItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _data.Count) return;

            element.style.height = _itemHeight;

            _bindCallback?.Invoke(_data[index], element, index);

            // Wire click
            element.userData = index;
            element.UnregisterCallback<ClickEvent>(OnItemClick);
            element.RegisterCallback<ClickEvent>(OnItemClick);

            // Refresh theme if needed
            if (element is IMD3Themeable t)
                t.RefreshTheme();
        }

        void OnItemClick(ClickEvent evt)
        {
            var el = evt.currentTarget as VisualElement;
            if (el?.userData is int idx && idx >= 0 && idx < _data.Count)
                itemClicked?.Invoke(idx, _data[idx]);
        }

        VisualElement GetOrCreateItem()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();
            return _makeItem();
        }

        void ReturnToPool(VisualElement item)
        {
            item.RemoveFromHierarchy();
            int maxPool = (_lastVisible - _firstVisible + 1) * 2;
            if (_pool.Count < maxPool)
                _pool.Enqueue(item);
        }

        void RecycleItem(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex >= _activeItems.Count) return;
            var item = _activeItems[activeIndex];
            if (item != null)
            {
                ReturnToPool(item);
                _activeItems[activeIndex] = null;
            }
        }

        void RecycleAll()
        {
            foreach (var item in _activeItems)
            {
                if (item != null)
                    ReturnToPool(item);
            }
            _activeItems.Clear();
            _container.Clear();
            _firstVisible = -1;
            _lastVisible = -1;
        }
    }
}
