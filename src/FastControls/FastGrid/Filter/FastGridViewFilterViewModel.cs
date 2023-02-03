using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using OpenSilver.ControlsKit.Annotations;

namespace FastGrid.FastGrid
{
    internal class FastGridViewFilterViewModel : INotifyPropertyChanged
    {
        private IReadOnlyList<FastGridViewFilterValueItem> filterValueItems_ = new List<FastGridViewFilterValueItem>();

        private FastGridViewFilterItem filterItem_;
        private FastGridViewColumn editColumn_;

        public IReadOnlyList<FastGridViewFilterValueItem> FilterValueItems {
            get => filterValueItems_;
            set {
                if (Equals(value, filterValueItems_)) return;
                filterValueItems_ = value;
                OnPropertyChanged();
            }
        }


        public FastGridViewFilterItem FilterItem {
            get => filterItem_;
            set {
                if (Equals(value, filterItem_)) return;
                filterItem_ = value;
                OnPropertyChanged();
            }
        }

        public FastGridViewColumn EditColumn {
            get => editColumn_;
            set {
                if (Equals(value, editColumn_)) return;
                editColumn_ = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
