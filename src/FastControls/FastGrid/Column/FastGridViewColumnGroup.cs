using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace FastGrid.FastGrid.Column
{
    internal sealed class FastGridViewColumnGroup : INotifyPropertyChanged {
        private double _width = 0;
        private string _columnGroupName = "";
        private Brush _groupHeaderForeground = new SolidColorBrush(Colors.White);
        private Brush _groupHeaderTextBackground = new SolidColorBrush(Colors.Transparent);
        private Thickness _groupHeaderPadding = new Thickness(0);

        public double Width {
            get => _width;
            set {
                if (value.Equals(_width)) return;
                _width = value;
                OnPropertyChanged();
            }
        }

        public string ColumnGroupName {
            get => _columnGroupName;
            set {
                if (value == _columnGroupName) return;
                _columnGroupName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NonEmptyGroupHeaderBackground));
            }
        }

        public Brush GroupHeaderForeground {
            get => _groupHeaderForeground;
            set {
                if (value == _groupHeaderForeground) return;
                _groupHeaderForeground = value;
                OnPropertyChanged();
            }
        }

        public Brush GroupHeaderBackground {
            get => _groupHeaderTextBackground;
            set {
                if (value == _groupHeaderTextBackground) return;
                _groupHeaderTextBackground = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NonEmptyGroupHeaderBackground));
            }
        }

        public Thickness GroupHeaderPadding {
            get => _groupHeaderPadding;
            set {
                if (value == _groupHeaderPadding) return;
                _groupHeaderPadding = value;
                OnPropertyChanged();
            }
        }

        public Brush NonEmptyGroupHeaderBackground {
            get => !string.IsNullOrEmpty(ColumnGroupName) ? _groupHeaderTextBackground : new SolidColorBrush(Colors.Transparent);
        }

        // note: right now, I don't care about visibility, we don't need it at this time
        public bool IsVisible { get; set; } = true;


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
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
