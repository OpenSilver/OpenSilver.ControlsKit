using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FastGrid.FastGrid
{
    internal class FastGridViewSort {
        private FastGridView _self;
        private SortComparer _sort;
        private List<object> _sortedList;

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

        private class SortComparer : IComparer<object> {
            private FastGridViewSort _self;
            private List<(PropertyInfo Property, bool Ascending)> _compareProperties;

            public SortComparer(FastGridViewSort self) {
                _self = self;
            }

            public void RecomputeProperties() {
                _compareProperties = null;
            }

            private void RecomputeProperties(object obj) {
                _compareProperties = new List<(PropertyInfo,bool)>();
                var type = obj.GetType();
                foreach (var col in _self._self.SortDescriptors.Columns) {
                    var pi = type.GetProperty(col.Column.DataBindingPropertyName, BindingFlags.Instance | BindingFlags.Public);
                    if (pi == null)
                        throw new Exception($"Fastgrid: can't find property {col.Column.DataBindingPropertyName}");
                    _compareProperties.Add((pi, col.SortDirection == SortDirection.Ascending));
                }
            }

            public int Compare(object a, object b) {
                Debug.Assert(a != null && b != null);
                if (_compareProperties == null)
                    RecomputeProperties(a);

                foreach (var prop in _compareProperties) {
                    var aValue = prop.Property.GetValue(a);
                    var bValue = prop.Property.GetValue(b);
                    var compare = CompareValue(aValue, bValue);
                    if (compare != 0)
                        return prop.Ascending ? compare : -compare;
                }

                return 0;
            }

            private int CompareValue(object a, object b) {
                if (a is int)
                    return (int)a - (int)b;
                if (a is uint)
                    return (uint)a < (uint)b ? -1 : ( (uint)a > (uint)b ? 1 : 0 );
                if (a is long)
                    return (long)a < (long)b ? -1 : ( (long)a > (long)b ? 1 : 0 );
                if (a is short)
                    return (short)a < (short)b ? -1 : ( (short)a > (short)b ? 1 : 0 );
                if (a is ulong)
                    return (ulong)a < (ulong)b ? -1 : ( (ulong)a > (ulong)b ? 1 : 0 );
                if (a is ushort)
                    return (ushort)a < (ushort)b ? -1 : ( (ushort)a > (ushort)b ? 1 : 0 );

                if (a is byte)
                    return (byte)a < (byte)b ? -1 : ( (byte)a > (byte)b ? 1 : 0 );
                if (a is char)
                    return (char)a < (char)b ? -1 : ( (char)a > (char)b ? 1 : 0 );

                if (a is double)
                    return (double)a < (double)b ? -1 : ( (double)a > (double)b ? 1 : 0 );
                if (a is decimal)
                    return (decimal)a < (decimal)b ? -1 : ( (decimal)a > (decimal)b ? 1 : 0 );
                if (a is float)
                    return (float)a < (float)b ? -1 : ( (float)a > (float)b ? 1 : 0 );
                if (a is string) {
                    var aString = (string)a;
                    var bString = (string)b;
                    return String.Compare(aString, bString, StringComparison.Ordinal);
                }
                if (a is DateTime)
                    return (DateTime)a < (DateTime)b ? -1 : ( (DateTime)a > (DateTime)b ? 1 : 0 );

                Debug.Assert(false);
                throw new Exception($"Type {a.GetType().ToString()} -- don't know how to Compare");
            }
        }

        public FastGridViewSort(FastGridView self) {
            _self = self;
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
            Console.WriteLine($"Fastgrid {_self.Name} - FULL re-sort complete, took {watch.ElapsedMilliseconds} ms");
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
            Console.WriteLine($"Fastgrid {_self.Name} - fast re-sort complete, took {watch.ElapsedMilliseconds} ms");
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
                    npc.PropertyChanged += Item_PropertyChanged;
            }
        }
    }
}
