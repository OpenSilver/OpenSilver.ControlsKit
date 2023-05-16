using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenSilver.ControlsKit.Annotations;

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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}