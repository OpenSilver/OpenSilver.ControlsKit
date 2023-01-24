using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FastGrid.FastGrid
{
    internal class FastGridViewCell : ContentControl, INotifyPropertyChanged
    {
        private int cellIndex_ = 0;
        private bool isCellVisible_ = true;

        // just move it offscreen
        public bool IsCellVisible {
            get => isCellVisible_;
            set {
                if (value == isCellVisible_) return;
                isCellVisible_ = value;
                OnPropertyChanged();
            }
        }

        public int CellIndex {
            get => cellIndex_;
            set {
                if (value == cellIndex_) return;
                cellIndex_ = value;
                OnPropertyChanged();
            }
        }

        public FastGridViewCell() {
            CustomLayout = true;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DataContextChanged += FastGridViewCell_DataContextChanged;
        }

        private void FastGridViewCell_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var childCount = VisualTreeHelper.GetChildrenCount(this);
            if (childCount < 1)
                return;
            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            if (cp == null)
                return;
            cp.CustomLayout = true;
            cp.HorizontalAlignment = HorizontalAlignment.Stretch;
            cp.VerticalAlignment = VerticalAlignment.Stretch;
            cp.DataContext = DataContext;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            cp.CustomLayout = true;
            cp.HorizontalAlignment = HorizontalAlignment.Stretch;
            cp.VerticalAlignment = VerticalAlignment.Stretch;
            cp.DataContext = DataContext;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs eventArgs) {
            // later: needed for CellEditTemplate
            base.OnMouseLeftButtonDown(eventArgs);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
