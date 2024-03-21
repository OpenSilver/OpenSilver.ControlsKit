using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using FastGrid.FastGrid.Data;

namespace FastGrid.FastGrid.Expand
{
    internal class FastGridFlatView : IFlatGridView
    {
        private FastGridView _self;

        public FastGridFlatView(FastGridView self) {
            _self = self;
        }

        private FastGridViewDataHolder Root => _self.MainDataHolder;
        private static Action<string> Logger => FastGridView.Logger;

        public void ToggleExpanded(object o) {
            // nothing to do
        }

        public int MaxRowIdx() {
            // ... note: the last row is not fully visible
            var visibleCount = _self.GuessRowCount();
            var maxRowIdx = Math.Max(Root.SortedItems.Count - visibleCount + 1, 0);
            return maxRowIdx;
        }

        public void UpdateExpandRow(FastGridViewRow row) {
            // nothing to do, no Expand allowed
        }

        public void FullReFilter() {
            Root.FullReFilter();
        }


        // extra optimization - if this is towards the end, and we could actually see more object, return an index that will show as many objects as possbible
        //
        // example: we set the top row index to zero, then reverse the sorting order -- in this case, our Top object would become the last
        //          without this optimization, we'd end up seeing only one object
        private int ObjectTo_TopRowIndex(object obj, int suggestedFindIndex) {
            var foundIdx = _self.UseTopRowIndex ? suggestedFindIndex : ObjectToRowIndex(obj, suggestedFindIndex);
            var maxRowIdx = MaxRowIdx();
            if (foundIdx > maxRowIdx)
                return maxRowIdx;
            else
                return foundIdx;
        }

        public int RowCount() {
            return Root.SortedItems?.Count ?? 0;
            
        }

        // returns -1 if not found
        public int ObjectToRowIndex(object obj, int suggestedFindIndex) {
            return Root.ObjectToSubRowIndex(obj, suggestedFindIndex);
        }

        // returns null if not found
        public object RowIndexToObject(int idx) {
            if (Root.SortedItems == null)
                return null;

            if (idx >= 0 && idx < Root.SortedItems.Count)
                return Root.SortedItems[idx];
            else
                return null;
        }

        public RowInfo RowIndexToInfo(int idx) {
            return new RowInfo(Root, RowIndexToObject(idx), indentLevel: 0) ;
        }


        public (object TopRow, int TopRowIndex) ComputeTopRowIndex(object oldTopRow, int oldTopRowIndex) {
            if (Root.SortedItems == null || Root.SortedItems.Count < 1) {
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
                topRow = Root.SortedItems[0];
                topRowIndex = 0;
            }
            Logger($"new top row {_self.Name}: {topRowIndex}");
            return (topRow, topRowIndex);
        }


        public void OnBeforeUpdateUI() {
            Root.OnBeforeUpdateUI();
        }

        public void OnCollectionUpdate(FastGridViewDataHolder dataHolder) {
            // nothing to do
        }

        public void SetExpanded(object o, bool isExpanded) {
            // nothing to do
        }
    }
}
