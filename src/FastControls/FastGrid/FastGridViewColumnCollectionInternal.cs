using System.Collections.Generic;
using System.Linq;

namespace FastGrid.FastGrid
{
    internal class FastGridViewColumnCollectionInternal : FastGridViewColumnCollection {
        private FastGridView _self;

        private List<FastGridViewColumn> _oldColumns = new List<FastGridViewColumn>();
        // if null -> force recreation
        private List<FastGridViewColumn> _sortedColumns = null;

        public FastGridViewColumnCollectionInternal(FastGridView self) {
            _self = self;
            CollectionChanged += FastGridViewColumnCollectionInternal_CollectionChanged;
            Subscribe();
        }

        private void FastGridViewColumnCollectionInternal_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            Unsubscribe();
            _oldColumns = this.ToList();
            Subscribe();
            _self.OnColumnsCollectionChanged();
        }

        private void BuildSortedColumns() {
            if (_sortedColumns == null)
                _sortedColumns = this.OrderBy(c => c.DisplayIndex).ToList();
        }

        public int GetColumnIndex(FastGridViewColumn column) {
            BuildSortedColumns();
            var idx = FastGridUtil.RefIndex(_sortedColumns, column);
            return idx;
        }

        private void Unsubscribe() {
            foreach (var col in _oldColumns)
                col.PropertyChanged -= Col_PropertyChanged;
        }

        private void Col_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
                case "DisplayIndex": 
                    _sortedColumns = null;
                    break;
            }

            _self.OnColumnPropertyChanged(sender as FastGridViewColumn, e.PropertyName);
        }

        private void Subscribe() {
            foreach (var col in _oldColumns)
                col.PropertyChanged += Col_PropertyChanged;
        }
    }
}
