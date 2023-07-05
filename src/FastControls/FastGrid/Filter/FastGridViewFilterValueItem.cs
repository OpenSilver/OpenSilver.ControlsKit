using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FastGrid.FastGrid {
    public class FastGridViewFilterValueItem : INotifyPropertyChanged {
        private bool isSelected_ = false;
        private string text_ = "";

        public string Text {
            get => text_;
            set {
                if (value == text_) return;
                text_ = value;
                OnPropertyChanged();
            }
        }

        public object OriginalValue { get; set; }

        public bool IsSelected {
            get => isSelected_;
            set {
                if (value == isSelected_) return;
                isSelected_ = value;
                OnPropertyChanged();
            }
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