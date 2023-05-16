using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OpenSilver.ControlsKit.Annotations;

namespace FastGrid.FastGrid
{
    internal class FastGridViewCell : ContentControl, INotifyPropertyChanged
    {
        private bool isCellVisible_ = true;

        // the reason for this - much easier to resort, when the column's display index changes
        private FastGridViewColumn column_;

        public bool IsCellVisible => column_.IsVisible;
        public int CellIndex => column_.DisplayIndex;

        public FastGridViewCell(FastGridViewColumn column) {
            column_ = column;
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



        public void UpdateWidth() {
            FastGridUtil.SetWidth(this, column_.Width);
            // note: I don't really care about Min/MaxWidth -- the column (header) itself deals with that
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
