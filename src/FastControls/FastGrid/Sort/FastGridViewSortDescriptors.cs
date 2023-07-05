using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastGrid.FastGrid
{
    public enum SortDirection {
        Ascending, Descending, 
    }

    public class FastGridSortDescriptor {
        public FastGridViewColumn Column { get; set; } = null;
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    }

    public class FastGridViewSortDescriptors {
        private List<FastGridSortDescriptor> _sortDescriptors = new List<FastGridSortDescriptor>();
        public Action OnResort;

        public IReadOnlyList<FastGridSortDescriptor> Columns => _sortDescriptors;
        public int Count => Columns.Count;

        public void Add(FastGridSortDescriptor sortDescriptor) {
            var existingIdx = _sortDescriptors.FindIndex(sd => ReferenceEquals(sd.Column, sortDescriptor.Column));
            if (existingIdx >= 0)
                _sortDescriptors[existingIdx].SortDirection = sortDescriptor.SortDirection;
            else
                _sortDescriptors.Add(sortDescriptor);
            OnResort?.Invoke();
        }

        public void Remove(FastGridSortDescriptor sortDescriptor) {
            var existingIdx = _sortDescriptors.FindIndex(sd => ReferenceEquals(sd.Column, sortDescriptor.Column));
            if (existingIdx >= 0)
                _sortDescriptors.RemoveAt(existingIdx);
            OnResort?.Invoke();
        }

        public void Clear() {
            foreach (var sort in _sortDescriptors)
                sort.Column.Sort = null;
            _sortDescriptors.Clear();
            OnResort?.Invoke();
        }

        public FastGridViewSortDescriptors Clone() {
            return new FastGridViewSortDescriptors {
                _sortDescriptors = _sortDescriptors.ToList(),
                OnResort = OnResort,
            };
        }
    }
}
