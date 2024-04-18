using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenSilver.ControlsKit.FastGrid.DataTemplate {
    public static partial class FastGridViewCellTemplate {
        internal class EnumItem : INotifyPropertyChanged {
            private object _value;
            private string _text;

            public object Value {
                get => _value;
                set {
                    if (Equals(value, _value)) {
                        return;
                    }

                    _value = value;
                    OnPropertyChanged();
                }
            }

            public string Text {
                get => _text;
                set {
                    if (value == _text) {
                        return;
                    }

                    _text = value;
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
}