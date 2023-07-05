using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FastGrid.FastGrid {
    public class HierarchicalCollectionInfo : INotifyPropertyChanged {
        private DataTemplate _rowTemplate = FastGridContentTemplate.DefaultRowTemplate();
        private DataTemplate _headerTemplate = FastGridContentTemplate.DefaultHeaderTemplate();
        internal FastGridViewColumnCollectionInternal InternalColumns { get; }
        public FastGridViewColumnCollection Columns { get; private set; }
        public FastGridViewSortDescriptors SortDescriptors { get; private set; }

        public DataTemplate RowTemplate {
            get => _rowTemplate;
            set {
                if (Equals(value, _rowTemplate)) return;
                _rowTemplate = value;
                OnPropertyChanged();
            }
        }

        public DataTemplate HeaderTemplate {
            get => _headerTemplate;
            set {
                if (Equals(value, _headerTemplate)) return;
                _headerTemplate = value;
                OnPropertyChanged();
            }
        }

        // 1 or 2 - for sub-collections, we have this restriction : the header
        // needs to be a multiple of row heights
        public int HeaderRowCount { get; set; } = 1;

        public HierarchicalCollectionInfo() {
            Columns = InternalColumns = new FastGridViewColumnCollectionInternal();
            SortDescriptors = new FastGridViewSortDescriptors();
        }

        internal HierarchicalCollectionInfo(FastGridViewColumnCollectionInternal columns, FastGridViewSortDescriptors sortDescriptors) {
            Columns = InternalColumns = columns;
            SortDescriptors = sortDescriptors;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}