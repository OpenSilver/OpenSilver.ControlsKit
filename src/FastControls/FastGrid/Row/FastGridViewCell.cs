using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace FastGrid.FastGrid.Row
{
    internal class FastGridViewCell : ContentControl, INotifyPropertyChanged
    {

        // the reason for this - much easier to resort, when the column's display index changes
        private FastGridViewColumn _column;

        public bool IsCellVisible => _column.IsVisible;
        public int CellIndex => _column.DisplayIndex;

        private bool IsFullyLoaded() => VisualTreeHelper.GetChildrenCount(this) >= 1;
        private bool _isInitialized = false;

        private bool _isHovered = false;

        public FastGridViewColumn Column
        {
            get => _column;
            set => _column = value;
        }

        public FastGridViewCell(FastGridViewColumn column)
        {
            _column = column;
            CustomLayout = true;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DataContextChanged += FastGridViewCell_DataContextChanged;
            Loaded += FastGridViewCell_Loaded;
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
            if (VisualTreeHelper.GetChildrenCount(cp) > 0)
            {
                var border = VisualTreeHelper.GetChild(cp, 0) as Border;
                if (border?.Name == FastGridUtil.EXPANDER_BORDER_NAME)
                {
                    // it's the Expander column
                    border.MouseLeftButtonDown += Border_MouseLeftButtonDown;
                }
            }

            FastGridUtil.SetCustomLayout(cp, true);
            FastGridUtil.SetHorizontalAlignment(cp, HorizontalAlignment.Stretch);
            FastGridUtil.SetVerticalAlignment(cp, VerticalAlignment.Stretch);
            FastGridUtil.SetDataContext(cp, DataContext, out _);

            SubscribeToTooltip(null);
        }

        private void SubscribeToTooltip(object oldValue)
        {
            if (_column.ToolTipPropertyName != "" && oldValue is INotifyPropertyChanged oldNotifyPropertyChanged)
            {
                oldNotifyPropertyChanged.PropertyChanged -= NotifyPropertyChanged_PropertyChanged;
            }
            if (_column.ToolTipPropertyName != "" && DataContext is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += NotifyPropertyChanged_PropertyChanged;
            }
            UpdateToolTip();
        }

        private void NotifyPropertyChanged_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _column.ToolTipPropertyName)
                UpdateToolTip();
        }

        // IMPORTANT: initially, I would set the tooltip on OnMouseEnter -- but that is too late, and due to how it's handled in OpenSilver, it just wouldn't work
        public void UpdateToolTip()
        {
            if (_column.ToolTipPropertyName != "" && DataContext != null)
            {
                var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
                var property = DataContext.GetType().GetProperty(_column.ToolTipPropertyName);
                var value = property.GetValue(DataContext);
                ToolTipService.SetToolTip(cp, value?.ToString());
            }
        }

        private void FastGridViewCell_Loaded(object sender, RoutedEventArgs e)
        {
            TryInitialize();
        }

        private void FastGridViewCell_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
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
            FastGridUtil.SetDataContext(cp, DataContext, out _);

            SubscribeToTooltip(e.OldValue);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            TryInitialize();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FastGridUtil.TryGetAscendant<FastGridViewRow>(this);
            var view = FastGridUtil.TryGetAscendant<FastGridView>(this);
            view.OnExpandToggle(row.RowObject);
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs eventArgs)
        {
            // later: needed for CellEditTemplate
            base.OnMouseLeftButtonDown(eventArgs);
        }

        internal void UpdateExpandCell(bool canExpand, bool isExpanded)
        {
            if (!_isInitialized)
                return;


            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            if (VisualTreeHelper.GetChildrenCount(cp) < 1)
                return;

            var border = VisualTreeHelper.GetChild(cp, 0) as Border;
            var grid = VisualTreeHelper.GetChild(border, 0) as Grid;
            var plus = grid.Children[0] as Path;
            var minus = grid.Children[1] as Rectangle;

            FastGridUtil.SetOpacity(border, canExpand ? 1 : 0);
            FastGridUtil.SetOpacity(plus, canExpand && !isExpanded ? 1 : 0);
            FastGridUtil.SetOpacity(minus, canExpand && isExpanded ? 1 : 0);
        }


        public void UpdateWidth()
        {
            FastGridUtil.SetWidth(this, _column.Width);
            // note: I don't really care about Min/MaxWidth -- the column (header) itself deals with that
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
