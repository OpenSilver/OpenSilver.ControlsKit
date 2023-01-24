using System.Collections.Generic;
using System.Linq;

namespace FastGrid.FastGrid
{
    internal class FastGridViewColumnCollectionInternal : FastGridViewColumnCollection {
        private FastGridView _self;

        private List<FastGridViewColumn> _oldColumns = new List<FastGridViewColumn>();

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

        private void Unsubscribe() {
            foreach (var col in _oldColumns)
                col.PropertyChanged -= Col_PropertyChanged;
        }

        private void Col_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _self.OnColumnPropertyChanged(sender as FastGridViewColumn, e.PropertyName);
        }

        private void Subscribe() {
            foreach (var col in _oldColumns)
                col.PropertyChanged += Col_PropertyChanged;
        }
    }
}
