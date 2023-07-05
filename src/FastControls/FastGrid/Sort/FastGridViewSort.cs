using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using FastGrid.FastGrid.Data;

namespace FastGrid.FastGrid
{
    internal partial class FastGridViewSort : IDisposable {
        private FastGridViewDataHolder _self;
        private SortComparer _sort;
        private List<object> _sortedList;
        private string _gridName;

        // only used for optimization, while the user is editing the filter
        // the idea: in this case, we sort once -- when the user starts editing the filter
        // when the user toggles items in the filter, we'll only re-filter this list -- since the sorting itself doesn't change
        private List<object> _filteredSortedListWhileEditingFilter;
        
        private HashSet<string> _propertyNames = new HashSet<string>();

        public IReadOnlyList<object> SortedItems {
            get {
                if (_self.IsEditingFilter) {
                    Debug.Assert(_filteredSortedListWhileEditingFilter != null);
                    return _filteredSortedListWhileEditingFilter;
                } else
                    return _sortedList ?? _self.FilteredItems;
            }
        }

        public FastGridViewSort(FastGridViewDataHolder self, string gridName) {
            _self = self;
            _gridName = gridName;
            _sort = new SortComparer(this);
        }

        // does a complete resort, ignoring anything we previously cached
        public void FullResort() {
            var watch = Stopwatch.StartNew();
            if (_sortedList != null)
                foreach (var item in _sortedList.OfType<INotifyPropertyChanged>())
                    item.PropertyChanged -= Item_PropertyChanged;

            var needsSort = _self.SortDescriptors.Count > 0;
            if (!needsSort) {
                // optimization - we don't have any sort, and no filtering
                _sortedList = null;
                if (_self.IsEditingFilter) {
                    _sortedList = _self.FilteredItems.ToList();
                    FastResort(); // compute current edited filter
                }
                return;
            }
            _sortedList = _self.FilteredItems.ToList();
            _propertyNames = new HashSet<string>(_self.SortDescriptors.Columns.Select(c => c.Column.DataBindingPropertyName));
            _sort.RecomputeProperties();

            foreach (var item in _sortedList.OfType<INotifyPropertyChanged>())
                item.PropertyChanged += Item_PropertyChanged;

            _sortedList.Sort(_sort);
            if (_self.IsEditingFilter) 
                FastResort(); // compute current edited filter
            FastGridView.Logger($"Fastgrid {_gridName} - FULL re-sort complete, took {watch.ElapsedMilliseconds} ms");
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (_self.IsEditingFilter)
                // optimization: while editing the filter, don't resort 
                return;

            if (_propertyNames.Contains(e.PropertyName))
                _self.NeedsResort();
        }

        // the difference between full resort and resort - on the re-sort, the filtered items haven't changed, however,
        // at least an object's Sort property has changed
        //
        // Example: I'm sorting by username, and for an object, I've updated the username
        public void FastResort() {
            var watch = Stopwatch.StartNew();
            if (!_self.IsEditingFilter) {
                _sortedList?.Sort(_sort);
                _filteredSortedListWhileEditingFilter = null;
            }
            else {
                // we're editing the filter -- we already have everything sorted
                var editedFilter = _self.EditFilterItem;
                _filteredSortedListWhileEditingFilter = _sortedList.Where(i => editedFilter.Matches(i)).ToList();
            }
            FastGridView.Logger($"Fastgrid {_gridName} - fast re-sort complete, took {watch.ElapsedMilliseconds} ms");
        }

        public void SortedAdd(object obj) {
            if (_sortedList == null)
                return;

            var index = _sortedList.BinarySearch(obj, _sort);
            if (index < 0) {
                _sortedList.Insert(~index, obj);
                if (obj is INotifyPropertyChanged npc)
                    npc.PropertyChanged += Item_PropertyChanged;
            }
            else
                _sortedList[index] = obj;

        }

        private int IndexOf(object obj) {
            var index = _sortedList.BinarySearch(obj, _sort);
            if (index >= 0) {
                // note: several object may be equivalent
                while (index < _sortedList.Count && !ReferenceEquals(obj, _sortedList[index])) {
                    ++index;
                    if (index < _sortedList.Count)
                        if (_sort.Compare(obj, _sortedList[index]) != 0)
                            // object not found
                            break;
                }
            }
            return -1;
        }

        public void Remove(object obj) {
            if (_sortedList == null)
                return;

            var index = IndexOf(obj);
            if (index >= 0) {
                _sortedList.RemoveAt(index);
                if (obj is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= Item_PropertyChanged;
            }
        }

        public void Dispose() {
            _sort.Dispose();
            _sortedList?.Clear();
            _filteredSortedListWhileEditingFilter?.Clear();
            _sortedList = null;
            _filteredSortedListWhileEditingFilter = null;
        }
    }
}
