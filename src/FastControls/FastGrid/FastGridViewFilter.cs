using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastGrid.FastGrid
{
    public class FastGridViewFilter {
        private List<FastGridViewFilterItem> _filterItems = new List<FastGridViewFilterItem>();

        public IReadOnlyList<FastGridViewFilterItem> FilterItems => _filterItems;

        public void AddFilter(FastGridViewFilterItem filterItem) {
            _filterItems.Add(filterItem);
        }

        public IReadOnlyList<string> GetFilterList(IReadOnlyList<object> items, FastGridViewColumn col)
        {
            return null;
        }

        public bool Matches(object obj) {
            if (FilterItems.Count == 0)
                return true;

            return FilterItems.All(f => f.Matches(obj));
        }
    }
}
