using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace FastGrid.FastGrid
{
    public partial class FastGridViewFilterCtrl : UserControl
    {
        internal FastGridViewFilterViewModel ViewModel => DataContext as FastGridViewFilterViewModel;
        private IReadOnlyList<FastGridViewFilterValueItem> _oldFilterValueItems = new List<FastGridViewFilterValueItem>();
        private bool _ignoreSelectAll;
        public FastGridViewFilterCtrl()
        {
            this.InitializeComponent();
            var vm = new FastGridViewFilterViewModel ();
            vm.PropertyChanged += Vm_PropertyChanged;
            DataContext = vm;
        }

        private void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
            case "FilterValueItems": 
                foreach (var item in _oldFilterValueItems)
                    item.PropertyChanged -= Item_PropertyChanged;
                foreach (var item in ViewModel.FilterValueItems)
                        item.PropertyChanged += Item_PropertyChanged;
                _oldFilterValueItems = ViewModel.FilterValueItems;
                RebuildSelectAll();
                break;
            }
        }

        private void RebuildSelectAll() {
            _ignoreSelectAll = true;
            var isAll = ViewModel.FilterValueItems.All(i => i.IsSelected);
            var isNone = ViewModel.FilterValueItems.All(i => !i.IsSelected);
            if (isAll)
                selectAll.IsChecked = true;
            else if (isNone)
                selectAll.IsChecked = false;
            else
                selectAll.IsChecked = null;
            _ignoreSelectAll = false;
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "IsSelected": 
                    if (!_ignoreSelectAll)
                        RebuildSelectAll();
                    break;
            }
        }

        private void close_Click(object sender, RoutedEventArgs e) {
            ViewModel.EditColumn.IsEditingFilter = false;
        }

        private void filter_click(object sender, RoutedEventArgs e) {
            ViewModel.EditColumn.Filter.RefreshFilter();
        }

        private void clear_filter_click(object sender, RoutedEventArgs e) {
            foreach (var item in ViewModel.FilterValueItems)
                item.IsSelected = false;
            ViewModel.EditColumn.Filter.CompareToValue2 = "";
            ViewModel.EditColumn.Filter.CompareToValue = "";
            ViewModel.EditColumn.Filter.RefreshFilter();
        }

        private void selectAll_click(object sender, RoutedEventArgs e) {
            _ignoreSelectAll = true;
            if (!selectAll.IsChecked.HasValue)
                // when the user clicks -> either select all, or unselect all
                selectAll.IsChecked = false;

            foreach (var item in ViewModel.FilterValueItems)
                item.IsSelected = selectAll.IsChecked.Value;
            _ignoreSelectAll = false;

        }
    }
}
