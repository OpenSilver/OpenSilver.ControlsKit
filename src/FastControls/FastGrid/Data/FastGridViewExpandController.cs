using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FastGrid.FastGrid.Expand;

namespace FastGrid.FastGrid.Data
{
    // knows what's expanded and what's not, and understands what is actually visible at a certain row
    internal class FastGridViewExpandController {
        private FastGridView _self;

        private FastGridViewDataHolder Root => _self.MainDataHolder;
        private static Action<string> Logger => FastGridView.Logger;

        public bool IsEmpty => Root.IsEmpty;
        public bool HasSource => Root.HasSource;

        private IFlatGridView _hierarchical, _nonHierarchical;
        private IFlatGridView Impl => _self.IsHierarchical ? _hierarchical : _nonHierarchical;

        public FastGridViewExpandController(FastGridView self) {
            _self = self;
            _hierarchical = new FastGridHierarchicalExpandedView(self);
            _nonHierarchical = new FastGridFlatView(self);
        }

        // returns all the rows the user could scroll to
        public int RowCount() => Impl.RowCount();
        public int MaxRowIdx() => Impl.MaxRowIdx();
        internal int ObjectToRowIndex(object obj, int suggestedFindIndex) => Impl.ObjectToRowIndex(obj, suggestedFindIndex);


        public IEnumerable<object> GetSelection() {
            if (_self.AllowMultipleSelection) {
                if (_self.UseSelectionIndex) {
                    foreach (var idx in _self.SelectedIndexes)
                        if (idx >= 0 && idx < RowCount())
                            yield return RowIndexToObject(idx);
                } else
                    foreach (var obj in _self.SelectedItems)
                        yield return obj;
            } else {
                if (_self.UseSelectionIndex) {
                    if (_self.SelectedIndex >= 0 && _self.SelectedIndex < RowCount())
                        yield return RowIndexToObject(_self.SelectedIndex);
                } else if (_self.SelectedItem != null)
                    yield return _self.SelectedItem;
            }
        }

        public object RowIndexToObject(int idx) => Impl.RowIndexToObject(idx);
        public RowInfo RowIndexToInfo(int idx) => Impl.RowIndexToInfo(idx);

        internal (object TopRow,int TopRowIndex) ComputeTopRowIndex(object oldTopRow, int oldTopRowIndex) => Impl.ComputeTopRowIndex(oldTopRow, oldTopRowIndex);


        public void OnBeforeUpdateUI() => Impl.OnBeforeUpdateUI();

        internal void OnCollectionUpdate(FastGridViewDataHolder dataHolder) => Impl.OnCollectionUpdate(dataHolder);

        public void SetExpanded(object o, bool isExpanded) => Impl.SetExpanded(o, isExpanded);
        public void ToggleExpanded(object o) => Impl.ToggleExpanded(o);

        public void UpdateExpandRow(FastGridViewRow row) => Impl.UpdateExpandRow(row);

        public void FullReFilter() => Impl.FullReFilter();
    }
}
