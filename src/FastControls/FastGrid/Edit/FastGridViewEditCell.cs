using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FastGrid.FastGrid;
using FastGrid.FastGrid.Filter;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace OpenSilver.ControlsKit.FastGrid.Edit
{
    internal class FastGridViewEditCell : ContentControl, INotifyPropertyChanged {

        private bool IsFullyLoaded() => VisualTreeHelper.GetChildrenCount(this) >= 1;
        private bool _isInitialized = false;

        public FastGridViewColumn Column { get; private set; }
        public bool IsCellVisible => Column.IsVisible;

        private PropertyInfo _propertyType;
        private HandleCellInputBase _handleCellInput;

        public FastGridViewEditCell(FastGridViewColumn column) {
            Column = column;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DataContextChanged += FastGridViewEditCell_DataContextChanged;
            Loaded += FastGridViewEditCell_Loaded; 
        }

        private void FastGridViewEditCell_Loaded(object sender, RoutedEventArgs e) {
            TryInitialize();
            HandleInput();
        }

        private void FastGridViewEditCell_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(this);
            if (childCount < 1)
                return;
            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            if (cp == null)
                return;

            FastGridInternalUtil.SetCustomLayout(cp, true);
            FastGridInternalUtil.SetHorizontalAlignment(cp, HorizontalAlignment.Stretch);
            FastGridInternalUtil.SetVerticalAlignment(cp, VerticalAlignment.Stretch);
            FastGridInternalUtil.SetDataContext(cp, DataContext);

            HandleInput();
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            TryInitialize();
            HandleInput();
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

            FastGridInternalUtil.SetCustomLayout(cp, true);
            FastGridInternalUtil.SetHorizontalAlignment(cp, HorizontalAlignment.Stretch);
            FastGridInternalUtil.SetVerticalAlignment(cp, VerticalAlignment.Stretch);
            FastGridInternalUtil.SetDataContext(cp, DataContext);
        }

        private static Control GetFirstControl(DependencyObject root) {
            return root.GetVisualDescendants().OfType<Control>().FirstOrDefault();
        }

        public Action<KeyNavigateAction, FastGridViewEditCell> OnNavigate;

        public void Navigate(KeyNavigateAction action) {
            OnNavigate?.Invoke(action, this);
        }

        private void HandleInput() {
            if (_handleCellInput != null)
                return;

            if (DataContext == null)
                // basically, I want to find out the property type, so I will know how to handle the input
                return;

            _propertyType = DataContext.GetType().GetProperty(Column.DataBindingPropertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var childCount = VisualTreeHelper.GetChildrenCount(this);
            if (childCount < 1)
                return;
            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            if (cp == null)
                return;

            childCount = VisualTreeHelper.GetChildrenCount(cp);
            if (childCount < 1)
                return; // nothing to present yet

            var root = VisualTreeHelper.GetChild(cp, 0) as FrameworkElement;
            var ctrl = GetFirstControl(root);
            if (FastGridViewFilterUtil.IsBool(_propertyType) && ctrl is CheckBox cb)
                _handleCellInput = new HandleCellInputBool(root, cb, this);
            else if (FastGridViewFilterUtil.IsEnum(_propertyType) && ctrl != null)
                _handleCellInput = new HandleCellInputEnum(root, ctrl, this);
            else if (FastGridViewFilterUtil.IsDateTime(_propertyType) && ctrl != null)
                _handleCellInput = new HandleCellInputDateTime(root, ctrl, this);
            else if ((FastGridViewFilterUtil.IsString(_propertyType) || FastGridViewFilterUtil.IsNumber(_propertyType)) && ctrl != null)
                _handleCellInput = new HandleCellInputText(root, ctrl, this);
            else if (ctrl != null)
                _handleCellInput = new HandleCellInputGeneric(root, ctrl, this);

            _handleCellInput?.Subscribe();
        }

        public void UnhandleInput() {
            _handleCellInput?.Unsubscribe();
            _handleCellInput = null;
        }

        public void OnGotFocus(bool viaClick) {
            _handleCellInput?.GotFocus(viaClick);
        }

        public void OnLostFocus() {
            _handleCellInput?.LostFocus();
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
