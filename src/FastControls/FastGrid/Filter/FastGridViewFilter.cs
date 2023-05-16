using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FastGrid.FastGrid
{
    internal class FastGridViewFilter {
        private List<FastGridViewFilterItem> _filterItems = new List<FastGridViewFilterItem>();

        public IReadOnlyList<FastGridViewFilterItem> FilterItems => _filterItems;

        public void AddFilter(FastGridViewFilterItem filterItem) {
            Debug.Assert(filterItem.PropertyName != "");
            var foundIdx = _filterItems.FindIndex(fi => fi.PropertyName == filterItem.PropertyName);
            if (foundIdx >= 0)
                _filterItems[foundIdx] = filterItem;
            else 
                _filterItems.Add(filterItem);
        }

        public void RemoveFilter(FastGridViewFilterItem filterItem) {
            RemoveFilter(filterItem.PropertyName);
        }
        public void RemoveFilter(string propertyName) {
            _filterItems.RemoveAll(fi => fi.PropertyName == propertyName);
        }

        public FastGridViewFilter Copy() {
            return new FastGridViewFilter {
                _filterItems = _filterItems.ToList(),
            };
        }

        public FastGridViewFilterItem GetOrAddFilterForProperty(FastGridViewColumn col) => GetOrAddFilterForProperty(col.DataBindingPropertyName);
        public FastGridViewFilterItem GetOrAddFilterForProperty(string name) {
            var found = _filterItems.FirstOrDefault(fi => fi.PropertyName == name);
            if (found != null)
                return found;
            else {
                var added = new FastGridViewFilterItem {
                    PropertyName = name,
                };
                AddFilter(added);
                return added;
            }
        }

        public FastGridViewFilterItem TryFilterForProperty(FastGridViewColumn col) => TryFilterForProperty(col.DataBindingPropertyName);
        public FastGridViewFilterItem TryFilterForProperty(string name) {
            return _filterItems.FirstOrDefault(fi => fi.PropertyName == name);
        }

        public IReadOnlyList<string> GetFilterList(IReadOnlyList<object> items, FastGridViewColumn col)
        {
            return null;
        }

        public bool IsEmpty => FilterItems.Count == 0 || FilterItems.Count(f => !f.IsEmpty) == 0;

        public bool Matches(object obj) {
            if (FilterItems.Count == 0)
                return true;

            return FilterItems.All(f => f.Matches(obj));
        }
    }
}
