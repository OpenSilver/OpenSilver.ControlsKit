using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FastGrid.FastGrid;

namespace OpenSilver.ControlsKit.Edit
{
    internal class FastGridViewEditCell : ContentControl, INotifyPropertyChanged {

        private bool IsFullyLoaded() => VisualTreeHelper.GetChildrenCount(this) >= 1;
        private bool _isInitialized = false;

        public FastGridViewColumn Column { get; private set; }
        public bool IsCellVisible => Column.IsVisible;

        public FastGridViewEditCell(FastGridViewColumn column) {
            Column = column;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DataContextChanged += FastGridViewEditCell_DataContextChanged;
            Loaded += FastGridViewEditCell_Loaded; 
        }

        private void FastGridViewEditCell_Loaded(object sender, RoutedEventArgs e) {
            TryInitialize();
        }

        private void FastGridViewEditCell_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(this);
            if (childCount < 1)
                return;
            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            if (cp == null)
                return;

            FastGridUtil.SetCustomLayout(cp, true);
            FastGridUtil.SetHorizontalAlignment(cp, HorizontalAlignment.Stretch);
            FastGridUtil.SetVerticalAlignment(cp, VerticalAlignment.Stretch);
            FastGridUtil.SetDataContext(cp, DataContext);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            TryInitialize();
        }

        private async void TryInitialize()
        {
            if (_isInitialized)
                return; // already initialized
            if (!IsFullyLoaded())
                return;

            _isInitialized = true;
            // we need to wait a bit, since initialization is async (needed after the Full Custom Layout issue)
            await Task.Delay(100);

            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;

            FastGridUtil.SetCustomLayout(cp, true);
            FastGridUtil.SetHorizontalAlignment(cp, HorizontalAlignment.Stretch);
            FastGridUtil.SetVerticalAlignment(cp, VerticalAlignment.Stretch);
            FastGridUtil.SetDataContext(cp, DataContext, out _);
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
