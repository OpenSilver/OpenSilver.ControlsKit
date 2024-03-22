using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using FastGrid.FastGrid.Data;

namespace FastGrid.FastGrid.Expand
{
    // knows exactly what is expanded, and what is not
    internal partial class FastGridHierarchicalExpandedView : IFlatGridView {
        private FastGridView _self;

        private Item _root;
        // if null -> recompute
        private List<Item> _itemsAsList = null;

        private bool _needsUpdateHorizontalScrollbar = false;

        private static Action<string> Logger => FastGridView.Logger;
        private bool RowEquals(object objA, object objB) => _self.RowEquals(objA, objB);

        public FastGridHierarchicalExpandedView(FastGridView self) {
            _self = self;
            _root = new Item(new RowInfo(null,null, indentLevel: -1)) {
                DataHolder = _self.MainDataHolder,
            };
        }

        public int RowCount() {
            EnsureItemsList();
            return _itemsAsList.Count;
        }

        public int ObjectToRowIndex(object obj, int suggestedFindIndex) {
            EnsureItemsList();

            if (suggestedFindIndex < 0 || suggestedFindIndex >= _itemsAsList.Count)
                suggestedFindIndex = 0;

            // the idea: it's very likely the position hasn't changed. And even if it did, it should be very near by
            const int MAX_NEIGHBORHOOD = 10;
            if (RowEquals(_itemsAsList[suggestedFindIndex].RowObject, obj))
                return suggestedFindIndex; // top row is the same

            for (int i = 1; i < MAX_NEIGHBORHOOD; ++i)
            {
                var beforeIdx = suggestedFindIndex - i;
                var afterIdx = suggestedFindIndex + i;
                if (beforeIdx >= 0 && RowEquals(_itemsAsList[beforeIdx].RowObject, obj))
                    return beforeIdx;
                else if (afterIdx < _itemsAsList.Count && RowEquals(_itemsAsList[afterIdx].RowObject, obj))
                    return afterIdx;
            }

            var idx = _itemsAsList.FindIndex(i => RowEquals(i.RowObject, obj));
            return idx;
        }

        public object RowIndexToObject(int rowIdx) {
            EnsureItemsList();
            if (rowIdx >= 0 && rowIdx < _itemsAsList.Count) {
                return _itemsAsList[rowIdx].RowObject;
            }
            return null;
        }

        public RowInfo RowIndexToInfo(int rowIdx) {
            EnsureItemsList();
            if (rowIdx >= 0 && rowIdx < _itemsAsList.Count) {
                return _itemsAsList[rowIdx].RowInfo;
            }
            return null;
        }



        public int MaxRowIdx() {
            EnsureItemsList();
            // ... note: the last row is not fully visible
            var visibleCount = _self.GuessVisibleRowCount();
            var maxRowIdx = Math.Max(_itemsAsList.Count - visibleCount + 1, 0);

            return maxRowIdx;
        }

        private int ObjectTo_TopRowIndex(object obj, int suggestedFindIndex) {
            var foundIdx = _self.UseTopRowIndex ? suggestedFindIndex : ObjectToRowIndex(obj, suggestedFindIndex);
            var maxRowIdx = MaxRowIdx();
            if (foundIdx > maxRowIdx)
                return maxRowIdx;
            else
                return foundIdx;
        }

        public (object TopRow, int TopRowIndex) ComputeTopRowIndex(object oldTopRow, int oldTopRowIndex) {
            EnsureItemsList();
            if (_itemsAsList.Count < 1) {
                // nothing to draw
                return (null,0); 
            }

            var foundIdx = ObjectTo_TopRowIndex(oldTopRow, oldTopRowIndex);
            if (foundIdx == oldTopRowIndex)
                return (oldTopRow, oldTopRowIndex); // same

            var topRow = oldTopRow;
            var topRowIndex = oldTopRowIndex;
            if (foundIdx >= 0)
                topRowIndex = foundIdx;
            else {
                // if topRow not found -> that means we removed it from the collection. If so, just go to the top
                topRow = _itemsAsList[0].RowObject;
                topRowIndex = 0;
            }
            Logger($"new top row {_self.Name}: {topRowIndex}");
            return (topRow, topRowIndex);
        }

        private void EnsureItemsList() {
            if (_itemsAsList == null) {
                _itemsAsList = new List<Item>();
                WalkChildren(_root, (i) => _itemsAsList.Add(i));
            }
        }


        private void WalkChildren(Item item, Action<Item> action) {
            foreach (var sub in item.Children)
                Walk(sub, action);
        }

        private void Walk(Item item, Action<Item> action) {
            action(item);
            foreach (var sub in item.Children)
                Walk(sub, action);
        }
        private void Walk(Item item, Action<Item> action, Func<Item,bool> walkChildrenFunc) {
            action(item);
            if (walkChildrenFunc(item))
                foreach (var sub in item.Children)
                    Walk(sub, action);
        }

        private Item FirstOrDefault(Item item, Func<Item, bool> action) {
            if (action(item))
                return item;

            foreach (var sub in item.Children) {
                var first = FirstOrDefault(sub, action);
                if (first != null)
                    return first;
            }

            return null;
        }

        public void OnBeforeUpdateUI() {
            Walk(_root, (i) => {
                if (i.DataHolder == null) 
                    return;
                if (i.DataHolder.NeedsCollectionUpdate)
                    _itemsAsList = null;
                i.DataHolder.OnBeforeUpdateUI();
            });

            if (_needsUpdateHorizontalScrollbar) {
                _needsUpdateHorizontalScrollbar = false;
                _self.UpdateHorizontalScrollbar();
            }
        }


        private void Initialize() {
            Debug.Assert(_self.ExpandFunc != null);
            foreach (var child in _root.Children)
                child.DisposeTree();
            _root.Children.Clear();

            foreach (var si in _self.MainDataHolder.SortedItems) {
                var coll = _self.ExpandFunc(si);
                _root.Children.Add(new Item(new RowInfo(_self.MainDataHolder, si, indentLevel: 0)) {
                    Collection = coll,
                });
            }

            _self.PostponeUpdateUI();
        }

        public void SetExpanded(object o, bool isExpanded) {
            var item = FirstOrDefault(_root, (i) => RowEquals(i.RowObject, o));
            if (item != null && item.IsExpanded != isExpanded) {
                _itemsAsList = null;
                if (isExpanded)
                    item.Expand(_self);
                else 
                    item.Collapse(_self);
                // the idea - the new rows (indented) might need to extend how much we can scroll
                // or vice versa, in case of a collapse
                _needsUpdateHorizontalScrollbar = true;
            }
        }

        public void ToggleExpanded(object o) {
            var item = FirstOrDefault(_root, (i) => RowEquals(i.RowObject, o));
            if (item != null) {
                _itemsAsList = null;
                if (!item.IsExpanded)
                    item.Expand(_self);
                else 
                    item.Collapse(_self);
                // the idea - the new rows (indented) might need to extend how much we can scroll
                // or vice versa, in case of a collapse
                _needsUpdateHorizontalScrollbar = true;
            }
        }


        private void SubRefresh(FastGridViewDataHolder dataHolder) {
            _itemsAsList = null;
            var item = FirstOrDefault(_root, (i) => ReferenceEquals(i.DataHolder, dataHolder));
            item.RefreshChildren(_self);
        }

        public void OnCollectionUpdate(FastGridViewDataHolder dataHolder) {
            _itemsAsList = null;
            if (ReferenceEquals(dataHolder, _self.MainDataHolder)) {
                if (_root.Children.Count == 0)
                    Initialize();
                else 
                    SubRefresh(dataHolder);
            }
            else 
                SubRefresh(dataHolder);
        }

        public void UpdateExpandRow(FastGridViewRow row) {
            var obj = row.RowObject;
            var item = FirstOrDefault(_root, (i) => RowEquals(i.RowObject, obj));
            row.UpdateExpandCell(item.CanBeExpanded, item.IsExpanded);
        }

        public void FullReFilter() {
            Walk(_root, (i) => { i.DataHolder.FullReFilter(); }, i => i.IsExpanded);
        }
    }
}
