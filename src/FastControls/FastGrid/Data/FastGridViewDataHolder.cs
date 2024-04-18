using FastGrid.FastGrid.Column;
using FastGrid.FastGrid.Filter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace FastGrid.FastGrid.Data
{
    // holds one expanded grid, or the main grid
    internal class FastGridViewDataHolder : IDisposable {
        private FastGridView _self;
        private FastGridViewColumnCollectionInternal _columns;

        private IReadOnlyList<object> _items;
        private IReadOnlyList<object> _filteredItems;
        // this is non-null ONLY when editing a filter
        // the idea: when I'm editing a filter -> I will compute how many unique values I have available, based on the OTHER filters
        private IReadOnlyList<object> _temporaryFilteredItems = null;

        private FastGridViewSortDescriptors _sortDescriptors;
        private FastGridViewSort _sort;

        private bool _ignoreUpdate = false;
        private bool _ignoreSort = false;

        private bool _needsRefilter = false;
        internal bool _needsFullReSort = false;
        private bool _needsReSort = false;
        private bool _needsRebuildHeader = false;
        private ItemsControl _headerControl;
        private Grid _headerBackground;
        private Grid _columnGroupBackground;
        private ItemsControl _columnGroupControl;
        private bool _columnsGroupAddedToCanvas = false;
        private DataTemplate _columnGroupTemplate;

        public bool HasSource => _items != null;
        public bool IsEmpty => _items == null || _items.Count < 1;

        public FastGridViewColumnCollection Columns => _columns;
        internal bool IsEditingFilter => Columns.Any(col => col.IsEditingFilter);

        internal FastGridViewFilter Filter { get; } = new FastGridViewFilter();

        private static Action<string> Logger => FastGridView.Logger;

        public bool NeedsColumnGroup() => Columns.Any(c => c.ColumnGroupName != "");

        public ItemsControl HeaderControl() {
            if (_headerControl == null) {
                _headerControl = FastGridInternalUtil.NewHeaderControl();
                _headerControl.SizeChanged += headerControl_SizeChanged;
            }

            return _headerControl;
        }
        public Grid HeaderBackground() {
            if (_headerBackground == null) {
                _headerBackground = new Grid();
            }

            return _headerBackground;
        }
        // ... just in case we have a column group
        public ItemsControl ColumnGroupControl() {
            Debug.Assert(NeedsColumnGroup());

            if (_columnGroupControl == null) {
                _columnGroupControl = FastGridInternalUtil.NewHeaderControl();
            }

            return _columnGroupControl;
        }
        public Grid ColumnGroupBackground() {
            if (_columnGroupBackground == null) {
                _columnGroupBackground = new Grid();
            }

            return _columnGroupBackground;
        }


        public int HeaderRowCount { get; }


        internal IReadOnlyList<object> FilteredItems {
            get {
                if (IsEditingFilter)
                    return _temporaryFilteredItems ?? _items;
                else
                    return _filteredItems ?? _items;
            }
        }

        public bool IsDisposed => _columns == null;

        public FastGridViewDataHolder(FastGridView self, FastGridViewColumnCollectionInternal columns, DataTemplate headerTemplate, DataTemplate columnGroupTemplate, int headerRowCount) {
            Debug.Assert(columns != null);
            _self = self;
            _columns = columns;
            _columns.DataHolder = this;
            _columnGroupTemplate = columnGroupTemplate;
            HeaderRowCount = headerRowCount;

            _sort = new FastGridViewSort(this, self.Name);
            _sortDescriptors = new FastGridViewSortDescriptors();
            _sortDescriptors.OnResort += () => {
                _needsFullReSort = true;
                _self.PostponeUpdateUI();
            };

            HeaderControl().ItemTemplate = headerTemplate;
            if (self.IsHierarchical)
                columns.EnsureExpandColumn();
            CreateHeader();
            _self.canvas.Children.Add(HeaderBackground());
            _self.canvas.Children.Add(HeaderControl());
            FastGridInternalUtil.SetLeft(HeaderControl(), FastGridViewDrawController.OUTSIDE_SCREEN);
        }


        internal IReadOnlyList<object> SortedItems => _sort.SortedItems;
        public FastGridViewSortDescriptors SortDescriptors => _sortDescriptors;

        internal FastGridViewSort Sort => _sort;

        internal FastGridViewFilterItem EditFilterItem {
            get {
                var column = _self.editFilterCtrl.ViewModel.EditColumn;
                Debug.Assert(column != null);
                var filterItem = Filter.GetOrAddFilterForProperty(column);
                return filterItem;
            }
        }

        private void headerControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            var width = e.NewSize.Width;
            OnNewHeaderWidth(width);
            HeaderBackground().Height = HeaderControl().Height;
        }

        private void OnNewHeaderWidth(double width) {
            var auto = Columns.FirstOrDefault(c => c.AutoWidth > 0);
            if (auto == null)
                return;
            var otherWidth = Columns.Sum(c => c.AutoWidth > 0 ? 0 : c.Width);
            var autoWidth = width - otherWidth;
            if (autoWidth >= auto.MinWidth)
                auto.Width = autoWidth;
        }

        internal void CreateFilter() {
            if (!_self.CanUserSortColumns && !_self.IsFilteringAllowedImpl())
                return;

            Filter.Clear();

            // rationale: allow the user to customize the filter for a column
            //
            // usually, I want to allow customizing the Filter equivalence, for instance, on a Date/time column, I can specify the date/time format
            // By default, that's "yyyy/MM/dd HH:mm:ss", but I may want to specify "HH:mm" (in this case, when user would filter by that column,
            // he'd see the unique values formatted as HH:mm)
            foreach (var col in Columns)
                if (col.IsFilterable) {
                    col.Filter.PropertyName = col.DataBindingPropertyName;
                    Filter.AddFilter(col.Filter);
                }

            HandleFilterSortColumns();
        }

        private void HandleFilterSortColumns() {
            if (!_self.CanUserSortColumns && !_self.IsFilteringAllowedImpl())
                return;
            
            // here, each sortable/filterable column needs to have a databinding property name
            foreach (var col in Columns)
                if ((col.IsFilterable || col.IsSortable) && col.DataBindingPropertyName == "")
                    throw new FastGridViewException($"Fastgrid: if filter and/or sort for a column, you need to set DataBindingPropertyName for it ({col.FriendlyName()})");
        }

        private void OnColumnSortChanged(FastGridViewColumn col) {
            if (!_self.AllowSortByMultipleColumns && !col.IsSortNone) {
                var isSameSort = _sortDescriptors.Count == 1 && ReferenceEquals(_sortDescriptors.Columns[0].Column, col);
                if (!isSameSort)
                    _sortDescriptors.Clear();
            }

            if (!col.IsSortNone)
                _sortDescriptors.Add(new FastGridSortDescriptor {
                    Column = col, 
                    SortDirection = col.IsSortAscending ? SortDirection.Ascending : SortDirection.Descending
                });
            else 
                _sortDescriptors.Remove(new FastGridSortDescriptor { Column = col});
        }

        // the idea:
        // a property we're sorting by has changed -- thus, we need to resort
        //
        // note: will postpone a redraw as well
        internal void NeedsResort() {
            // resorting only happens when we need to redraw, no point in doing it faster,
            // since several changes can happen at the same time
            if (!_self.AlwaysDoFullSort)
                _needsReSort = true;
            else
                _needsFullReSort = true;
            _self.PostponeUpdateUI();
        }

        // note: will postpone a redraw as well
        internal void NeedsRefilter() {
            _needsRefilter = true;
            NeedsResort();
        }

        // recomputes the filter completely, ignoring any previous cache
        // after this, you need to re-sort
        internal void FullReFilter() {
            _needsRefilter = false;
            if (!Filter.IsEmpty) {
                var watch = Stopwatch.StartNew();
                _filteredItems = _items.Where(i => Filter.Matches(i)).ToList();
                Logger($"Fastgrid {_self.Name} - refilter complete, took {watch.ElapsedMilliseconds} ms");
            } else
                // no filter
                _filteredItems = null;
            _needsFullReSort = true;
        }

        internal void SetSource(IEnumerable source) {
            if (_items is INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= FastGridView_CollectionChanged;
            _items = (IReadOnlyList<object>)source ;
            if (_items != null && source is INotifyPropertyChanged)
                (source as INotifyCollectionChanged).CollectionChanged += FastGridView_CollectionChanged;

            NeedsRefilter();
        }

        internal void CreateHeader() {
            RebuildHeaderCollection();
        }


        internal void OnColumnsCollectionChanged() {
            _self.DrawController.UpdateHorizontalScrollbar();
            _self.EditRow.RecreateEditCells();
        }


        internal void OnColumnPropertyChanged(FastGridViewColumn col, string propertyName) {
            switch (propertyName) {
            case "IsVisible": 
                _self.PostponeUpdateUI();
                if (NeedsColumnGroup())
                    UpdateColumnGroupWidths();
                break;

            case "IsResizingColumn":
                if (col.IsResizingColumn) {
                    if (!_self.InstantColumnResize)
                        _self.RowProvider.SetRowOpacity(0.4);
                } else {
                    if (!_self.InstantColumnResize) {
                        // first, make sure all cells have the correct size, after row resize
                        _self.RowProvider.UpdateUI();

                        _self.RowProvider.SetRowOpacity(1);
                        _self.PostponeUpdateUI();
                    }

                    _self.DrawController.UpdateHorizontalScrollbar();
                    // the idea - the old value might have become invalid
                    _self.RefreshHorizontalScroll();
                }
                break;

            case "Width":
                var updateCellWidthNow = _self.InstantColumnResize || !col.IsResizingColumn;
                if (updateCellWidthNow) {
                    _self.RowProvider.UpdateUI();
                    _self.PostponeUpdateUI();
                }
                if (NeedsColumnGroup())
                    UpdateColumnGroupWidths();
                break;
            
            case "MinWidth": 
                _self.PostponeUpdateUI();
                break;
            
            case "MaxWidth": 
                _self.PostponeUpdateUI();
                break;
            
            case "DisplayIndex":
                if (!_ignoreUpdate) {
                    _needsRebuildHeader = true;
                    _self.PostponeUpdateUI();
                }
                break;

            case "Sort":
                if (!_ignoreSort) {
                    _ignoreSort = true;
                    OnColumnSortChanged(col);
                    _ignoreSort = false;
                }
                break;

            case "IsEditingFilter":
                if (col.IsEditingFilter) {
                    foreach (var other in _columns.Where(c => !ReferenceEquals(c, col)))
                        other.IsEditingFilter = false;
                    OpenEditFilter(col, _self.EditFilterMousePos);
                } else {
                    if (_columns.All(c => !c.IsEditingFilter)) {
                        CloseEditFilter(col);
                        _needsRefilter = true;
                        _self.PostponeUpdateUI();
                    }
                }

                break;

            case "HeaderText": break;
            case "CanResize": break;
            case "CellTemplate": break;
            case "CellEditTemplate": break;
                case "HeaderForeground": break;
                case "HeaderFontSize": break;
            }
        }

        private IReadOnlyList<FastGridViewColumnGroup> CreateColumnGroups() {
            if (_columns.Count < 1)
                return new List<FastGridViewColumnGroup>();

            var sortedColumns = HeaderControl().ItemsSource as IReadOnlyList<FastGridViewColumn>;
            var names = sortedColumns.Where(c =>c.ColumnGroupName != "").Select(c => c.ColumnGroupName).Distinct().ToList();
            if (sortedColumns[0].ColumnGroupName == "")
                // the idea: in this case, the first area is without text
                names.Insert(0, "");

            return names.Select(n => new FastGridViewColumnGroup {
                ColumnGroupName = n, Width = 0,
            }).ToList();
        }

        private void UpdateColumnGroupWidths() {
            if (_columns.Count < 1)
                return;
            if (_needsRebuildHeader)
                return; // we'll be called as part of rebuilding the header
            var sortedColumns = HeaderControl().ItemsSource as IReadOnlyList<FastGridViewColumn>;
            if (sortedColumns == null || sortedColumns.Count != _columns.Count)
                return; // we'll be called as part of rebuilding the header

            var columnGroups = ColumnGroupControl().ItemsSource as IReadOnlyList<FastGridViewColumnGroup>;
            var isEmptyTextColumn = sortedColumns[0].ColumnGroupName == "";
            var padding = _self.GroupHeaderPadding.Left + _self.GroupHeaderPadding.Right;
            foreach (var cg in columnGroups) {
                if (isEmptyTextColumn) {
                    isEmptyTextColumn = false;
                    var emptyWidth = 0d;
                    foreach (var column in _columns) {
                        if (column.ColumnGroupName != "")
                            break;
                        emptyWidth += column.IsVisible ? column.Width : 0;
                    }
                    cg.Width = emptyWidth - padding;
                } else
                    cg.Width = _columns.Sum(c => c.ColumnGroupName == cg.ColumnGroupName && cg.IsVisible ? c.Width : 0) - padding;
            }
        }

        private void RebuildHeaderCollection() {
            _ignoreUpdate = true;
            if (_self.CanUserReorderColumns)
                foreach (var col in _columns)
                    col.CanDrag = true;
            for (var index = 0; index < _columns.Count; index++) {
                var column = _columns[index];
                if (column.DisplayIndex < 0)
                    column.DisplayIndex = index;
            }
            _ignoreUpdate = false;

            // once column order changes -> make sure we reflect that instantly
            _self.RowProvider.UpdateUI();

            if (_self.IsHierarchical)
                _columns.EnsureExpandColumn();
            HeaderControl().ItemsSource = _columns.OrderBy(c => c.DisplayIndex).ToList();
            foreach(var col in _columns)
                _self.FastGridViewStyler.StyleColumn(col);
            if (NeedsColumnGroup()) {
                if (ColumnGroupControl().ItemTemplate == null)
                    ColumnGroupControl().ItemTemplate = _columnGroupTemplate;
                ColumnGroupControl().ItemsSource = CreateColumnGroups();
                UpdateColumnGroupWidths();
                if (!_columnsGroupAddedToCanvas) {
                    _columnsGroupAddedToCanvas = true;
                    _self.canvas.Children.Add(ColumnGroupBackground());
                    _self.canvas.Children.Add(ColumnGroupControl());

                    GroupHeaderBackgroundColorChanged(_self.GroupHeaderBackground);
                    GroupHeaderForegroundColorChanged(_self.GroupHeaderForeground);
                    GroupHeaderPaddingChanged(_self.GroupHeaderPadding);
                }
            }
        }

        public bool NeedsCollectionUpdate => _needsRefilter || _needsFullReSort || _needsReSort;

        public void OnBeforeUpdateUI() {
            if (_needsRebuildHeader) {
                _needsRebuildHeader = false;
                RebuildHeaderCollection();
            }

            var collectionUpdate = NeedsCollectionUpdate;
            if (_needsRefilter ) {
                _needsRefilter = false;
                FullReFilter();
            }

            if (_needsFullReSort ) {
                _needsFullReSort = false;
                _needsReSort = false;
                Sort.FullResort();
            }
            if (_needsReSort) {
                _needsReSort = false;
                Sort.FastResort();
            }
            if (collectionUpdate)
                _self.OnCollectionUpdate(this);

            if (!_self.IsHierarchical)
                Logger($"Fastgrid: updating UI {_self.Name}, all={_items.Count}, filtered={FilteredItems.Count}, sorted={SortedItems.Count}");
        }

        private bool RowEquals(object objA, object objB) => _self.RowEquals(objA, objB);

        // if object not found, returns -1
        internal int ObjectToSubRowIndex(object obj, int suggestedFindIndex)
        {
            if (SortedItems == null || SortedItems.Count < 1)
                return -1; // nothing to draw

            if (obj == null)
            {
                // set the top row now
                obj = SortedItems[0];
                suggestedFindIndex = 0;
            }

            if (suggestedFindIndex < 0 || suggestedFindIndex >= SortedItems.Count)
                suggestedFindIndex = 0;

            // the idea: it's very likely the position hasn't changed. And even if it did, it should be very near by
            const int MAX_NEIGHBORHOOD = 10;
            if (RowEquals(SortedItems[suggestedFindIndex], obj))
                return suggestedFindIndex; // top row is the same

            for (int i = 1; i < MAX_NEIGHBORHOOD; ++i)
            {
                var beforeIdx = suggestedFindIndex - i;
                var afterIdx = suggestedFindIndex + i;
                if (beforeIdx >= 0 && RowEquals(SortedItems[beforeIdx], obj))
                    return beforeIdx;
                else if (afterIdx < SortedItems.Count && RowEquals(SortedItems[afterIdx], obj))
                    return afterIdx;
            }

            // in this case, top row is not in the neighborhood
            for (int i = 0; i < SortedItems.Count; ++i)
                if (RowEquals(SortedItems[i], obj))
                    return i;

            return -1;
        }

        internal void SetSelection(IReadOnlyList<object> selectedItems, int topRowIndex) {
            if (selectedItems.Count > 1 && _self.IsHierarchical) {
                // the idea - we don't allow objects from several heterogeneous collections (in case of hierarchical grid)
                var type = selectedItems[0].GetType();
                if (selectedItems.Any(o => o.GetType() != type))
                    selectedItems = new List<object> { selectedItems[0] };
            }

            var indexes = _self.UseSelectionIndex ? selectedItems.Select(o => ObjectToSubRowIndex(o, topRowIndex)).ToList() : null;
            if (_self.AllowMultipleSelection) {
                if (_self.UseSelectionIndex) 
                    _self.SelectedIndexes = new ObservableCollection<int>(indexes);
                else 
                    _self.SelectedItems = new ObservableCollection<object>(selectedItems);
            } else {
                if (_self.UseSelectionIndex)
                    _self.SelectedIndex = indexes.Count > 0 ? indexes[0] : -1;
                else 
                    _self.SelectedItem = selectedItems.Count > 0 ? selectedItems[0] : null;
            }
        }


        private void FastGridView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            try {
                // search for outdated references in selected items
                if (e.NewItems != null)
                {
                    // IMPORTANT:
                    // I'm not touching old items. This is because if I have a selected item that is removed, that won't be visible to the user anyway.
                    //
                    // But more importantly, when something is replaced, it's done via a Remove + Insert.
                    // So, on any update, we could end up with that item being removed from the selection
                    var selectedItems = _self.GetSelection().ToList() ;
                    var anyChange = false;
                    for (int i = 0; i < selectedItems.Count; i++)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (RowEquals(selectedItems[i], item))
                            {
                                selectedItems[i] = item;
                                anyChange = true;
                                continue;
                            }
                        }
                    }
                    if (anyChange)
                        SetSelection(selectedItems, _self.IsHierarchical ? -1 : _self.DrawController.TopRowIndex);
                }

                if (IsEditingFilter)
                    // the idea -- once the user stops filtering, we'll do a full refilter + resort anyway
                    return;

                if (_self.CanUserSortColumns || _self.IsFilteringAllowedImpl()) {
                    switch (e.Action) {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                        if (e.NewItems != null)
                            foreach (var item in e.NewItems)
                                OnAddedItem(item);
                        if (e.OldItems != null)
                            foreach(var item in e.OldItems)
                                OnRemovedItem(item);
                        break;

                        // consider this is a complete collection reset
                    default:
                        if (_self.IsFilteringAllowedImpl())
                            _needsRefilter = true;
                        if (_self.CanUserSortColumns)
                            _needsFullReSort = true;
                        //Logger($"Fastgrid {Name} - needs refilter/resort");
                        _self.OnCollectionUpdate(this);
                        break;
                    }
                }

                _self.PostponeUpdateUI();
            }
            catch (Exception exception) {
                Logger($"FATAL: exception in collectionchanged {_self.Name} :{e}");
            }
        }

        private bool MatchesFilter(object item) {
            if (!_self.IsFilteringAllowedImpl())
                return true; // no filtering
            return Filter.Matches(item);
        }

        private void OnAddedItem(object item) {
            if (!MatchesFilter(item))
                return; // doesn't matter
            Sort.SortedAdd(item);
            if (_self.AlwaysDoFullSort)
                _needsFullReSort = true;
        }

        private void OnRemovedItem(object item) {
            if (!MatchesFilter(item))
                return; // doesn't matter
            Sort.Remove(item);
            if (_self.AlwaysDoFullSort)
                _needsFullReSort = true;
        }

        internal void OpenEditFilter(FastGridViewColumn column, Point mouse) {
            var editFilterCtrl = _self.editFilterCtrl;

            if (_self.editFilterCtrl.ViewModel.FilterItem != null)
                editFilterCtrl.ViewModel.FilterItem.PropertyChanged -= FilterItem_PropertyChanged;

            ComputeTemporaryFilterItems(column);
            editFilterCtrl.ViewModel.EditColumn = column;
            var filterItem = Filter.GetOrAddFilterForProperty(column);
            var uniqueValues = FastGridViewFilterUtil.ToUniqueValues(FilteredItems, column.DataBindingPropertyName, column.FilterPropertyName(), filterItem.CompareEquivalent);
            editFilterCtrl.ViewModel.FilterItem = filterItem;
            var selectedValues = new HashSet<string>(filterItem.PropertyValues.Select(v => v.AsString));
            var list = uniqueValues.Select(v => new FastGridViewFilterValueItem {
                Text = v.AsString, 
                OriginalValue = v.OriginalValue, 
                IsSelected = selectedValues.Contains(v.AsString)
            }).ToList();
            editFilterCtrl.ViewModel.FilterValueItems = list;
            foreach (var item in list)
                item.PropertyChanged += filter_ValueItem_PropertyChanged;
            const double PAD = 20;
            var x = mouse.X + PAD + editFilterCtrl.Width > _self.canvas.Width ? _self.canvas.Width - editFilterCtrl.Width : mouse.X + PAD;
            var y = mouse.Y + PAD;

            if (y + editFilterCtrl.Height > _self.canvas.Height) {
                // in this case, if we're shown at the bottom, it's possible that the filter is not fully visible
                y = mouse.Y;
                var point = _self.canvas.TransformToVisual(null).TransformPoint(new Point(0, 0));
                var maxUp = point.Y;
                var goUp = -(_self.canvas.Height - editFilterCtrl.Height - PAD) + PAD;
                if (goUp > maxUp)
                    goUp = maxUp; // go only as much as possible
                y -= goUp;
            }

            editFilterCtrl.grid.Background = _self.Background;
            _self.editFilterPopup.HorizontalOffset = x;
            _self.editFilterPopup.VerticalOffset = y;
            _self.editFilterPopup.IsOpen = true;
            editFilterCtrl.Visibility = Visibility.Visible;

            // monitor for manual filter changes
            editFilterCtrl.ViewModel.FilterItem.PropertyChanged += FilterItem_PropertyChanged;
        }

        internal void CloseEditFilter(FastGridViewColumn column) {
            _self.editFilterCtrl.ViewModel.EditColumn = null;
            _self.editFilterPopup.IsOpen = false;
            if (_self.editFilterCtrl.ViewModel.FilterItem != null) {
                column.ForceUpdateColor();
                _self.editFilterCtrl.ViewModel.FilterItem.PropertyChanged -= FilterItem_PropertyChanged;
            }
            _self.editFilterCtrl.ViewModel.FilterItem = null;

            _temporaryFilteredItems = null;
        }

        private void FilterItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
                case "ForceRefreshFilter":
                    _needsRefilter = true;
                    _self.PostponeUpdateUI();
                    break;
            }
        }
        private void filter_ValueItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateEditFilterValues();
            if (!_self.AlwaysDoFullSort)
                _needsReSort = true;
            else
                _needsFullReSort = true;
            _self.PostponeUpdateUI();
        }

        private void ComputeTemporaryFilterItems(FastGridViewColumn column) {
            Debug.Assert(IsEditingFilter);

            var TempFilter = Filter.Copy();
            TempFilter.RemoveFilter(column.DataBindingPropertyName);

            if (!TempFilter.IsEmpty) 
                _temporaryFilteredItems = _items.Where(i => TempFilter.Matches(i)).ToList();
            else
                // optimization - in this case, there's a single filter
                _temporaryFilteredItems = null;

            // resort, based on temporary filtered items
            _needsFullReSort = true;
            _self.PostponeUpdateUI();
        }
        private void UpdateEditFilterValues() {
            var column = _self.editFilterCtrl.ViewModel.EditColumn;
            var filterItem = Filter.GetOrAddFilterForProperty(column);
            var filterValueItems = _self.editFilterCtrl.ViewModel.FilterValueItems;
            // if everything is selected -> no filter
            var selectedItems =  filterValueItems.All(vi => vi.IsSelected) ? new List<(string AsString, object OriginalValue)>() : filterValueItems.Where(vi => vi.IsSelected).Select(vi => (vi.Text,vi.OriginalValue)).ToList();
            filterItem.PropertyValues = selectedItems;
        }

        public void Dispose() {
            SetSource(null);
            _filteredItems = null;
            _temporaryFilteredItems = null;

            if (_sortDescriptors != null)
                _sortDescriptors.OnResort = null;
            _sortDescriptors = null;

            _columns?.Dispose();
            _columns = null;

            _sort?.Dispose();
            _sort = null;

            Filter.Dispose();

            HeaderControl().ItemsSource = null;
            _self.canvas.Children.Remove(HeaderControl());
            _self.canvas.Children.Remove(HeaderBackground());

            if (_headerControl != null)
                _headerControl.SizeChanged -= headerControl_SizeChanged;

            if (_columnGroupControl != null) {
                _columnGroupControl.ItemsSource = null;
                _self.canvas.Children.Remove(_columnGroupControl);
                _self.canvas.Children.Remove(_columnGroupBackground);
            }
        }

        internal void HeaderBackgroundColorChanged(SolidColorBrush color) {
            FastGridInternalUtil.SetControlBackground(HeaderBackground(), color);
        }

        internal void GroupHeaderForegroundColorChanged(SolidColorBrush newValue) {
            if (!NeedsColumnGroup())
                return;

            var groupColumns = ColumnGroupControl().ItemsSource as List<FastGridViewColumnGroup>;
            groupColumns?.ForEach(x => x.GroupHeaderForeground = newValue ?? new SolidColorBrush(Colors.White));
        }

        internal void GroupHeaderBackgroundColorChanged(SolidColorBrush newValue) {
            if (!NeedsColumnGroup())
                return;
            var groupColumns = ColumnGroupControl().ItemsSource as List<FastGridViewColumnGroup>;
            groupColumns?.ForEach(x => x.GroupHeaderBackground = newValue ?? new SolidColorBrush(Colors.Transparent));
        }

        internal void GroupHeaderLineBackgroundChanged(SolidColorBrush newValue) {
            if (!NeedsColumnGroup())
                return;
            FastGridInternalUtil.SetControlBackground(ColumnGroupBackground(), newValue);
        }

        internal void GroupHeaderPaddingChanged(Thickness newValue) {
            if (!NeedsColumnGroup())
                return;
            var groupColumns = ColumnGroupControl().ItemsSource as List<FastGridViewColumnGroup>;
            groupColumns?.ForEach(x => x.GroupHeaderPadding = newValue);
        }
    }
}
