using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FastGrid.FastGrid.Data;
using FastGrid.FastGrid.Filter;
using OpenSilver;
using OpenSilver.ControlsKit.Edit;
using OpenSilver.ControlsKit.Edit.Args;
using OpenSilver.ControlsKit.FastGrid.Row;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace FastGrid.FastGrid
{
    [Flags]
    public enum FastGridViewEditTriggers {
        // Denotes that no action will put cell into edit mode.
        None = 1,

        // Denotes that Single click on a cell will put it into edit mode.
        CellClick = 2,

        // Denotes that click on a current cell will put it into edit mode.
        //
        // note: we don't support clicking on a cell (in other words, clicking will always select the full row)
        // in our case, if you click on the selected row, it will go into edit mode on the cell you clicked
        CurrentCellClick = 4,

        // Denotes that F2 key on a cell will put it into edit mode.
        F2 = 8,

        // Denotes that any text input key will put a cell into edit mode.
        // not supported yet
        //TextInput = 16,

        Default = CurrentCellClick | F2 
    }


    /*
     *
     * IMPORTANT:
     * Filtering
     * - at this time, we don't monitor objects that don't match the filter, even if a change could actually make them match the filter.
     *   Example: I can filter by Salary>1000. I add a user with Salary=500 (automatically filtered out). I later set his Salary=1001 (it will still be filtered out)
     *   Note: this limitation exists in Telerik as well.
     *
     *
     * LATER IDEAS:
     * - just in case this is still slow - i can create copies of the original objects and update stuff at a given interval
     *   (note: for me + .net7 -> this works like a boss, so not needed yet)
     *
     *     */
    // all the information needed to properly show and edit the collection
    public partial class FastGridView : UserControl, INotifyPropertyChanged {
        private const double TOLERANCE = 0.0001;

        internal delegate void RowClickHandler(FastGridViewRow row, MouseButtonEventArgs e);
        internal event RowClickHandler RowClick;

        private FastGridViewRowProvider _rowProvider;
        private FastGridViewDrawController _drawController;
        private FastGridViewDataHolder _mainDataHolder;
        private FastGridViewExpandController _expandController;

        




        private UiTimer _checkOffscreenUiTimer;

        private FastGridViewColumnCollectionInternal _columnsRoot;

        public event EventHandler<CellEditBeginArgs> CellEditBegin;
        public event EventHandler<CellEditEndedArgs> CellEditEnded;
        public event EventHandler<CellEditEndingArgs> CellEditEnding; 


        private static bool _firstTime = true;



        public enum RightClickAutoSelectType {
            None, Select, SelectAdd,
        }

        public Func<bool> CheckIsOffscreen;

        public bool IsHierarchical { get; set; } = false;

        public bool AllowPreload { get; set; } = true;

        public static Action<string> Logger ;

        // the idea -- allow user to manually set this
        public bool MonitorOffscreen {
            get => _monitorOffscreen;
            set {
                if (value == _monitorOffscreen) return;
                _monitorOffscreen = value;
                OnPropertyChanged();
            }
        }

        static FastGridView() {
            if (Interop.IsRunningInTheSimulator)
                Logger = (s) => Debug.WriteLine(s);
            else
                Logger = Console.WriteLine;
        }

        public FastGridView() {
            this.InitializeComponent();

            _checkOffscreenUiTimer = new UiTimer(this, 500, "CheckIsOffscreen");

            _rowProvider = new FastGridViewRowProvider(this);
            _drawController = new FastGridViewDrawController(this);

            _columnsRoot = new FastGridViewColumnCollectionInternal();
            _mainDataHolder = new FastGridViewDataHolder(this, _columnsRoot, HeaderTemplate, ColumnGroupTemplate, headerRowCount: 1) ;
            _expandController = new FastGridViewExpandController(this);

            HierarchicalRoot = new HierarchicalCollectionInfo(_columnsRoot, _mainDataHolder.SortDescriptors);
            _editRow = new FastGridViewEditRow(this, HierarchicalRoot);

            CheckIsOffscreen = () => {
                if (VisualTreeHelper.GetParent(this) is Canvas) {
                    var left = Canvas.GetLeft(this);
                    var top = Canvas.GetTop(this);
                    var isOffscreen = left < -9999 || top < -9999;
                    return isOffscreen;
                }

                return false;
            };

            verticalScrollbar.Minimum = 0;
            horizontalScrollbar.Minimum = 0;

            _checkOffscreenUiTimer.Tick += () => {
                IsOffscreen = CheckIsOffscreen();
            };

            if (_firstTime) {
                _firstTime = false;
                Interop.ExecuteJavaScriptVoid("document.addEventListener('contextmenu', event => event.preventDefault());");
            }
        }


        private FastGridViewEditRow _editRow;
        internal FastGridViewEditRow EditRow => _editRow;

        internal FastGridViewRowProvider RowProvider => _rowProvider;
        internal FastGridViewDataHolder MainDataHolder => _mainDataHolder;
        internal FastGridViewDrawController DrawController => _drawController;
        internal FastGridViewExpandController ExpandController => _expandController;

        internal bool RowEquals(object objA, object objB) => RowEqualityComparer?.Equals(objA, objB) ?? ReferenceEquals(objA, objB);

        private int TopRowIndex => _drawController.TopRowIndex;


        internal int GuessVisibleRowCount() => (int)((canvas.Height - HeaderHeight) / RowHeight) ;


        public HierarchicalCollectionInfo HierarchicalRoot { get; }
        public HierarchicalCollectionInfo Hierarchical1 { get; set; }
        public HierarchicalCollectionInfo Hierarchical2 { get; set; }
        public HierarchicalCollectionInfo Hierarchical3 { get; set; }
        public HierarchicalCollectionInfo Hierarchical4 { get; set; }

        public FastGridViewEditTriggers EditTriggers { get; set; } = FastGridViewEditTriggers.Default;

        private bool EditWithF2() => (EditTriggers & FastGridViewEditTriggers.F2) != 0;
        private bool EditWithClick() => (EditTriggers & FastGridViewEditTriggers.CellClick) != 0;
        private bool EditWithCurrentCellClick() => (EditTriggers & FastGridViewEditTriggers.CurrentCellClick) != 0;
        private bool DisableEdit() => (EditTriggers & FastGridViewEditTriggers.None) != 0;

        public FastGridViewColumnCollection Columns => HierarchicalRoot.Columns;
        public FastGridViewSortDescriptors SortDescriptors => HierarchicalRoot.SortDescriptors;
        internal IReadOnlyList<FastGridViewColumn> SortedColumns => HierarchicalRoot.InternalColumns.SortedColumns();

        internal bool CanEdit() => Columns.Any(c => c.CellEditTemplate != null);

        // for an object -> returns the collection that represents the object's details (expanding it)
        // 
        // if returns null -> we can't expand it
        public Func<object, IEnumerable> ExpandFunc;

        public Func<object, HierarchicalCollectionInfo> ObjectTypeFunc;

        public Action<object, bool> OnExpandedFunc;

        // returns true if we're scrolling with the mouse
        public bool IsMouseScrolling { get; private set; } = false;

        // IMPORTANT: this can only be set up once, at loading
        public bool AllowMultipleSelection { get; set; } = false;

        // the idea - when doing pageup/pgdown - we'll need to create more rows to first show them off-screen
        // creation is time consuming, so lets cache ahead -- thus, pageup/pagedown will be faster
        public double CreateExtraRowsAheadPercent { get; set; } = 2.1;

        // these are extra rows that are already bound to the underlying objects, ready to be shown on-screen
        // the idea - load a few extra rows on top + on bottom, just in case the user wants to do a bit of scrolling
        // (like, with the mouse wheel)
        public int ShowAheadExtraRows { get; set; } = 7;

        // if true, I allow several sort columns, if false, one sort column at most
        public bool AllowSortByMultipleColumns { get; set; } = true;

        // if true -> you set selection index -> then i compute the selected object
        // if false -> you set selection object -> then i compute the selection index
        //             (in this case, if you move the selection object, the selection index changes)
        //
        // IMPORTANT:
        // you usually don't need to care about this, this will update, based on what you bind (SelectedIndex(es) or SelectedItem(s))
        // however, if you don't bind anything, then you may want to set this specifically to true or false
        public bool UseSelectionIndex { get; set; } = false;

        // if true -> once you manually scroll to a row (index), we'll remain there, no matter if the item source get new items before or after
        // if false -> the top row is preserved (so for instance, if rows are added below, nothing changes)
        public bool UseTopRowIndex { get; set; } = false;

        // instead of binding the row background, you can also have a function that is called before each row is shown
        // rationale: binding the row background might not be possible, or it may sometimes cause a bit of flicker
        public Func<object, int, Brush> RowBackgroundColorFunc { get; set; } = null;

        // if true -> on column resize + horizontal scrolling, the effect is instant
        // if false -> we dim the cells and then do the resize once the user finishes (much faster)
        public bool InstantColumnResize { get; set; } = false;

        // if true -> we never optimize sorting 
        // if false (default) -> we optimize add/removes based on the sort columns
        public bool AlwaysDoFullSort { get; set; } = false;

        public bool HideAllRowsOnItemsSourceChanged { get; set; } = true;

        // if false (default), we hide everything when no items to show (efficient)
        // if true, we'll show the header even if there are no items to show
        public bool ShowHeaderOnNoItems { get; set; } = false;

        internal FastGridViewFilter Filter => _mainDataHolder.Filter;

        // optimization: you can set this to true when we're offscreen -- in this case, we'll unbind all rows (so that no unnecessary updates take place)
        //
        // by default, if Parent is Canvas, I will check every 0.5 seconds, and if I can determine we're offscreen, I will automatically set this to true
        public bool IsOffscreen {
            get => isOffscreen_;
            set {
                if (value == isOffscreen_) return;
                isOffscreen_ = value;
                OnPropertyChanged();
            }
        }

        internal double HorizontalOffset {
            get => _horizontalOffset;
            set {
                if (value.Equals(_horizontalOffset)) return;
                _horizontalOffset = value;
                OnPropertyChanged();
            }
        }

        private bool IsScrollingHorizontally {
            get => _isScrollingHorizontally;
            set {
                if (value == _isScrollingHorizontally) return;
                _isScrollingHorizontally = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler SelectionChanged;

        public class RowEventArgs : EventArgs {
            public readonly FrameworkElement Row;

            public RowEventArgs(FrameworkElement row) {
                Row = row;
            }
        }

        public event EventHandler<RowEventArgs> RowEnter;
        public event EventHandler<RowEventArgs> RowLeave;

        public int UiTimerInterval {
            get => _drawController.UiTimerInterval;
            set => _drawController.UiTimerInterval = value;
        }


        // note: not bindable at this time
        public bool CanUserReorderColumns { get; set; } = true;

        // note: not bindable at this time
        public bool CanUserResizeRows { get; set; } = true;
        // note: not bindable at this time
        public bool CanUserSortColumns { get; set; } = true;
        // note: not bindable at this time
        public bool IsFilteringAllowed { get; set; } = true;

        internal bool IsFilteringAllowedImpl() {
            if (!IsFilteringAllowed)
                return false;

            if (Columns.Any(c => c.IsFilterable))
                return true;

            return false;
        }

        public FastGridViewStyler FastGridViewStyler { get; set; } = new FastGridViewStyler();

        // you can have your own function that dictates if we can update the UI
        public Func<bool> CanUpdateUI = () => true;

        internal Point EditFilterMousePos { get; set; } = new Point();

        public IEnumerable<object> VisibleRows() => _rowProvider.Rows.Where(r => r.IsRowVisible).Select(r => r.RowObject);

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(FastGridView), 
                                                                                                    new PropertyMetadata(default(IEnumerable), OnItemsSourceChanged));
        public IEnumerable ItemsSource {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = d as FastGridView;
            self?.OnItemsSourceChanged();
        }

        // row height - -1 = auto computed
        // note: at this time (4th Jan 2023) - we only support fixed row heights (no auto)
        public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
                                                        "RowHeight", typeof(double), typeof(FastGridView), new PropertyMetadata(30d, RowHeightChanged));


        public double RowHeight {
            get { return (double)GetValue(RowHeightProperty); }
            set { SetValue(RowHeightProperty, value); }
        }

        public static readonly DependencyProperty HeaderHeightProperty = DependencyProperty.Register(
                                                        "HeaderHeight", typeof(double), typeof(FastGridView), new PropertyMetadata(36d, HeaderHeightChanged));

        public double HeaderHeight {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        public static readonly DependencyProperty HeaderBackgroundProperty = DependencyProperty.Register(
                                                "HeaderBackground", typeof(SolidColorBrush), typeof(FastGridView), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), HeaderBackgroundChanged));

        private static void HeaderBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView)._mainDataHolder.HeaderBackgroundColorChanged((SolidColorBrush)e.NewValue);
        }

        public SolidColorBrush HeaderBackground {
            get { return (SolidColorBrush)GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        public static readonly DependencyProperty GroupHeaderBackgroundProperty = DependencyProperty.Register(
                                        "GroupHeaderBackground", typeof(SolidColorBrush), typeof(FastGridView), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), GroupHeaderBackgroundChanged));

        private static void GroupHeaderBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView)._mainDataHolder.GroupHeaderBackgroundColorChanged((SolidColorBrush)e.NewValue);
        }

        public SolidColorBrush GroupHeaderBackground {
            get { return (SolidColorBrush)GetValue(GroupHeaderBackgroundProperty); }
            set { SetValue(GroupHeaderBackgroundProperty, value); }
        }

        public static readonly DependencyProperty GroupHeaderForegroundProperty = DependencyProperty.Register(
                                "GroupHeaderForeground", typeof(SolidColorBrush), typeof(FastGridView), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), GroupHeaderForegroundChanged));

        private static void GroupHeaderForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView)._mainDataHolder.GroupHeaderForegroundColorChanged((SolidColorBrush)e.NewValue);
        }

        public SolidColorBrush GroupHeaderForeground {
            get { return (SolidColorBrush)GetValue(GroupHeaderForegroundProperty); }
            set { SetValue(GroupHeaderForegroundProperty, value); }
        }

        public static readonly DependencyProperty GroupHeaderLineBackgroundProperty = DependencyProperty.Register(
                                "GroupHeaderLineBackground", typeof(SolidColorBrush), typeof(FastGridView), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), GroupHeaderLineBackgroundChanged));

        private static void GroupHeaderLineBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView)._mainDataHolder.GroupHeaderLineBackgroundChanged((SolidColorBrush)e.NewValue);
        }

        public SolidColorBrush GroupHeaderLineBackground {
            get { return (SolidColorBrush)GetValue(GroupHeaderLineBackgroundProperty); }
            set { SetValue(GroupHeaderLineBackgroundProperty, value); }
        }

        public static readonly DependencyProperty GroupHeaderPaddingProperty = DependencyProperty.Register(
                                "GroupHeaderPadding", typeof(Thickness), typeof(FastGridView), new PropertyMetadata(new Thickness(0), GroupHeaderPaddingChanged));

        private static void GroupHeaderPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView)._mainDataHolder.GroupHeaderPaddingChanged((Thickness)e.NewValue);
        }

        public Thickness GroupHeaderPadding {
            get { return (Thickness)GetValue(GroupHeaderPaddingProperty); }
            set { SetValue(GroupHeaderPaddingProperty, value); }
        }

        private static void RowHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView)._rowProvider.RowHeightChanged();
        }
        private static void HeaderHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).HeaderHeightChanged();
        }


        private void HeaderHeightChanged() {
            _mainDataHolder.HeaderControl().Height = HeaderHeight;
            if (_mainDataHolder.NeedsColumnGroup()) {
                FastGridInternalUtil.SetTop(_mainDataHolder.HeaderControl(), HeaderHeight);
                FastGridInternalUtil.SetTop(_mainDataHolder.HeaderBackground(), HeaderHeight);
                _mainDataHolder.ColumnGroupControl().Height = HeaderHeight;
                _mainDataHolder.ColumnGroupBackground().Height = HeaderHeight;
            }
        }

        // to enumerate selection, regarless of its type: GetSelection()
        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
            "SelectedIndex", typeof(int), typeof(FastGridView), 
            new PropertyMetadata(-1, SingleSelectedIndexChanged));


        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        // to enumerate selection, regarless of its type: GetSelection()
        public static readonly DependencyProperty SelectedIndexesProperty = DependencyProperty.Register(
                                                                                                        "SelectedIndexes", typeof(ObservableCollection<int>), typeof(FastGridView), 
                                                                                                        new PropertyMetadata(new ObservableCollection<int>(), SelectedIndexesChanged));


        public ObservableCollection<int> SelectedIndexes
        {
            get { return (ObservableCollection<int>)GetValue(SelectedIndexesProperty); }
            set { SetValue(SelectedIndexesProperty, value); }
        }

        // to enumerate selection, regarless of its type: GetSelection()
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
                                                                                                     "SelectedItem", typeof(object), typeof(FastGridView), 
                                                                                                     new PropertyMetadata(null, SingleSelectedItemChanged));


        public IEnumerable<HierarchicalCollectionInfo> HierarchicalInfos() {
            foreach (var item in new[] { HierarchicalRoot, Hierarchical1, Hierarchical2, Hierarchical3, Hierarchical4, }.Where(hci => hci != null))
                yield return item;
        }

        public object SelectedItem
        {
            get {
                if (AllowMultipleSelection)
                    throw new FastGridViewException("can't use SelectedItem when using multi-selection");
                if (UseSelectionIndex)
                    return _expandController.RowIndexToObject(SelectedIndex);
                return (object)GetValue(SelectedItemProperty);
            }
            set {
                SetValue(SelectedItemProperty, value);
            }
        }

        // to enumerate selection, regarless of its type: GetSelection()
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
                                                                                                      "SelectedItems", typeof(ObservableCollection<object>), typeof(FastGridView), 
                                                                                                      new PropertyMetadata(new ObservableCollection<object>(), SelectedItemsChanged));
        public ObservableCollection<object> SelectedItems
        {
            get { return (ObservableCollection<object>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }


        public static readonly DependencyProperty SelectionBackgroundProperty = DependencyProperty.Register(
            "SelectionBackground", typeof(Brush), typeof(FastGridView), new PropertyMetadata(new SolidColorBrush(Colors.DarkGray), SelectedBackgroundChanged));
        public Brush SelectionBackground
        {
            get { return (Brush)GetValue(SelectionBackgroundProperty); }
            set { SetValue(SelectionBackgroundProperty, value); }
        }



        public static readonly DependencyProperty RowTemplateProperty = DependencyProperty.Register(
                                                        "RowTemplate", typeof(DataTemplate), typeof(FastGridView), new PropertyMetadata(FastGridContentTemplate. DefaultRowTemplate(), RowTemplateChanged));


        public DataTemplate RowTemplate {
            get { return (DataTemplate)GetValue(RowTemplateProperty); }
            set { SetValue(RowTemplateProperty, value); }
        }

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
                                                        "HeaderTemplate", typeof(DataTemplate), typeof(FastGridView), new PropertyMetadata(FastGridContentTemplate.DefaultHeaderTemplate(), HeaderTemplateChanged));


        public DataTemplate HeaderTemplate {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public static readonly DependencyProperty ColumnGroupTemplateProperty = DependencyProperty.Register(
            nameof(ColumnGroupTemplate), typeof(DataTemplate), typeof(FastGridView), new PropertyMetadata(FastGridContentTemplate.DefaultHeaderColumnGroupTemplate(), ColumnGroupTemplateChanged));


        public DataTemplate ColumnGroupTemplate {
            get { return (DataTemplate)GetValue(ColumnGroupTemplateProperty); }
            set { SetValue(ColumnGroupTemplateProperty, value); }
        }

        public static readonly DependencyProperty RightClickAutoSelectProperty = DependencyProperty.Register(
                                                        "RightClickAutoSelect", typeof(RightClickAutoSelectType), typeof(FastGridView), new PropertyMetadata(RightClickAutoSelectType.None));

        public RightClickAutoSelectType RightClickAutoSelect {
            get { return (RightClickAutoSelectType)GetValue(RightClickAutoSelectProperty); }
            set { SetValue(RightClickAutoSelectProperty, value); }
        }

        public object RightClickSelectedObject {
            get => _rightClickSelectedObject;
            private set {
                if (Equals(value, _rightClickSelectedObject)) return;
                _rightClickSelectedObject = value;
                OnPropertyChanged();
            }
        }


        public double ScrollSize
        {
            get => _scrollSize;
            set {
                if (value.Equals(_scrollSize)) return;
                _scrollSize = value;
                OnPropertyChanged();
            }
        }

        private static void SingleSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FastGridView).SingleSelectedIndexChanged();
        }
        private static void SelectedIndexesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FastGridView).SelectedIndexesChanged();
        }
        private static void SingleSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FastGridView).SingleSelectedItemChanged();
        }
        private static void SelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FastGridView).SelectedItemsChanged();
        }

        private static void SelectedBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FastGridView)._rowProvider.SelectedBackgroundChanged();
        }
        private static void RowTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).RowTemplateChanged();
        }
        private static void HeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).HeaderTemplateChanged();
        }
        private static void ColumnGroupTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).ColumnGroupTemplateChanged();
        }


        private void SingleSelectedIndexChanged()
        {
            if (AllowMultipleSelection)
                throw new FastGridViewException("can't set SelectedIndex on multi-selection");

            UseSelectionIndex = true;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }

        private void SelectedIndexesChanged()
        {
            if (!AllowMultipleSelection)
                throw new FastGridViewException("can't set SelectedIndexes on multi-selection");

            UseSelectionIndex = true;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }

        private void SingleSelectedItemChanged()
        {
            if (AllowMultipleSelection)
                throw new FastGridViewException("can't set SelectedItem on multi-selection");

            UseSelectionIndex = false;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }

        private void SelectedItemsChanged()
        {
            if (!AllowMultipleSelection)
                throw new FastGridViewException("can't set SelectedItems on multi-selection");

            UseSelectionIndex = false;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }


        private void UpdateSelection() {
            _drawController.UpdateSelection();
        }

        private void RowTemplateChanged() {
            HierarchicalRoot.RowTemplate = RowTemplate;
            ClearImpl(clearSelection: false);
        }

        private void HeaderTemplateChanged() {
            _mainDataHolder.HeaderControl().ItemTemplate = HeaderTemplate;
        }
        private void ColumnGroupTemplateChanged() {
            if (_mainDataHolder.NeedsColumnGroup())
                _mainDataHolder.ColumnGroupControl().ItemTemplate = ColumnGroupTemplate;
        }

        internal void OnCollectionUpdate(FastGridViewDataHolder dataHolder) {
            _expandController.OnCollectionUpdate(dataHolder);
        }

        private void ClearCanvas() {
            // the idea -> all our extra controls are kept in a child canvas (like, scroll bars + header)
            foreach (var child in canvas.Children.OfType<FastGridViewRow>().ToList())
                canvas.Children.Remove(child);
        }

        private void CreateDefaultColumnCellTemplates() {
            var asList = ItemsSource as IReadOnlyList<object>;
            if (asList.Count > 0) {
                var anyEditCreation = false;
                foreach (var col in Columns.Where(c => c.DataBindingPropertyName != "")) {
                    var oldEdit = col.CellEditTemplate;
                    col.CreateDefaultDataTemplate(asList[0]);
                    if (oldEdit == null && col.CellEditTemplate != null)
                        anyEditCreation = true;
                }

                if (anyEditCreation)
                    _editRow.RecreateEditCells();
            }
        }

        private void OnItemsSourceChanged() {
            Logger($"fastgrid itemsource set for {Name} {(ItemsSource == null ? "to NULL" : "")}");
            if (ItemsSource == null) {
                if (IsHierarchical)
                    foreach (var item in _mainDataHolder.SortedItems)
                        SetExpanded(item, false);

                _mainDataHolder.SetSource(ItemsSource);
                _drawController.SetSource(ItemsSource);
                ClearImpl();
                return;
            }

            // allow only ObservableCollection<T> - fast to iterate + know when elements are added/removed
            if (!(ItemsSource is INotifyCollectionChanged) || !(ItemsSource is IReadOnlyList<object>))
                throw new FastGridViewException("ItemsSource needs to be ObservableCollection<>");

            if (HideAllRowsOnItemsSourceChanged)
                _rowProvider.HideAllRows();

            ClearSelection();
            CreateDefaultColumnCellTemplates();

            _mainDataHolder.SetSource(ItemsSource);
            _drawController.SetSource(ItemsSource);

            if (_mainDataHolder.HeaderControl().Items.Count < 1) {
                // this can happen when creating columns via code-behind, and we reset the itemssource (in other words, we create the columns once, when we set an itemssource,
                // and then we set another itemssource with completely different data, which means, re-create the header completely)
                _mainDataHolder.CreateFilter();
                _mainDataHolder.CreateHeader();
            }
        }

        public void SuspendRender() => _drawController.SuspendRender();
        public void ResumeRender() => _drawController.ResumeRender();

        internal void PostponeUpdateUI() => _drawController.PostponeUpdateUI();

        public void SetExpanded(object obj, bool isExpanded) {
            // note: internally, this will end up redrawing the UI
            _expandController.SetExpanded(obj, isExpanded);
        }




        private void horiz_bar_scroll(object sender, ScrollEventArgs e) {
            var SMALL = 10;
            var LARGE = 100;
            switch (e.ScrollEventType) {
            case ScrollEventType.SmallDecrement:
                HorizontalScroll(horizontalScrollbar.Value - SMALL);
                break;
            case ScrollEventType.SmallIncrement:
                HorizontalScroll(horizontalScrollbar.Value + SMALL);
                break;
            case ScrollEventType.LargeDecrement:
                HorizontalScroll(horizontalScrollbar.Value - LARGE);
                break;
            case ScrollEventType.LargeIncrement:
                HorizontalScroll(horizontalScrollbar.Value + LARGE);
                break;
            case ScrollEventType.ThumbTrack:
                IsScrollingHorizontally = true;
                HorizontalScroll(e.NewValue, updateScrollbarValue: false);
                if (!InstantColumnResize) {
                    _rowProvider.SetRowOpacity(0.4);
                }

                break;
            case ScrollEventType.EndScroll:
                IsScrollingHorizontally = false;
                HorizontalScroll(e.NewValue, updateScrollbarValue: false);
                if (!InstantColumnResize) {
                    _rowProvider.SetRowOpacity(1);
                }
                break;
            case ScrollEventType.ThumbPosition:
                HorizontalScroll(e.NewValue);
                break;
            case ScrollEventType.First:
                HorizontalScroll(0);
                break;
            case ScrollEventType.Last:
                HorizontalScroll(horizontalScrollbar.Maximum);
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        internal void RefreshHorizontalScroll() {
            HorizontalScroll(horizontalScrollbar.Value);
        }

        internal void HorizontalScroll(double value, bool updateScrollbarValue = true) {
            value = Math.Max(horizontalScrollbar.Minimum, Math.Min(value, horizontalScrollbar.Maximum));
            if (InstantColumnResize || !IsScrollingHorizontally)
                HorizontalOffset = value;

            if (updateScrollbarValue)
                horizontalScrollbar.Value = value;
        }

        public IEqualityComparer RowEqualityComparer { get; set; }


        internal bool IsRowSelected(object row, int rowIdx)
        {
            if (UseSelectionIndex)
            {
                if (AllowMultipleSelection)
                    return SelectedIndexes.Contains(rowIdx);
                else
                    return SelectedIndex == rowIdx;
            }
            else
            {
                if (AllowMultipleSelection)
                    return SelectedItems.Any(i => RowEquals(i, row));
                else
                    return RowEquals(row, SelectedItem);
            }
        }





        internal void Row_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RowClick?.Invoke((FastGridViewRow)sender, e);
        }

        internal void Row_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            RightClickSelectedObject = (sender as FastGridViewRow)?.RowObject;
            OnRightClick(sender as FastGridViewRow);
        }



        private Size _actualSize = Size.Empty;
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            _actualSize = e.NewSize;
            canvas.Width = e.NewSize.Width;
            canvas.Height = e.NewSize.Height;
            UpdateScrollBarsPos();

            //_mainDataHolder.HeaderControl().Width = e.NewSize.Width;
            _mainDataHolder.HeaderControl().Height = HeaderHeight;
            if (_mainDataHolder.NeedsColumnGroup()) {
                FastGridInternalUtil.SetTop(_mainDataHolder.HeaderControl(), HeaderHeight);
                _mainDataHolder.ColumnGroupControl().Width = e.NewSize.Width;
                _mainDataHolder.ColumnGroupControl().Height = HeaderHeight;

                _mainDataHolder.ColumnGroupBackground().Width = e.NewSize.Width;
                _mainDataHolder.ColumnGroupBackground().Height = HeaderHeight;
            }

            _mainDataHolder.HeaderBackground().Width = e.NewSize.Width;
            _mainDataHolder.HeaderBackground().Height = HeaderHeight;

            _rowProvider.SetWidth(e.NewSize.Width);

            DrawController.UpdateHorizontalScrollbar();
            PostponeUpdateUI();
        }

        private void UpdateScrollBarsPos() {
            if (_actualSize == Size.Empty)
                return;

            verticalScrollbar.Width = ScrollSize;
            horizontalScrollbar.Height = ScrollSize;
            scrollGap.Height = ScrollSize;
            scrollGap.Width = ScrollSize;
            FastGridInternalUtil.SetLeft(scrollGap, _actualSize.Width - ScrollSize);
            FastGridInternalUtil.SetTop(scrollGap, _actualSize.Height - ScrollSize);

            FastGridInternalUtil.SetLeft(verticalScrollbar, _actualSize.Width - ScrollSize);
            FastGridInternalUtil.SetTop(horizontalScrollbar, _actualSize.Height - ScrollSize);

            // just size, based on which is visible
            UpdateScrollBarsVisibilityAndSize();
        }

        internal void UpdateScrollBarsVisibilityAndSize(bool? showHorizontal = null, bool? showVertical = null) {
            var isHorizontalVisible = horizontalScrollbar.IsVisible;
            var isVerticalVisible = verticalScrollbar.IsVisible;

            if (showVertical == null) {
                var visibleCount = GuessVisibleRowCount();
                showVertical = (ExpandController.RowCount() > visibleCount);
            }

            if (showHorizontal != null) {
                FastGridInternalUtil.SetIsVisible(horizontalScrollbar, showHorizontal.Value);
                isHorizontalVisible = showHorizontal.Value;
            }

            if (showVertical != null) {
                FastGridInternalUtil.SetIsVisible(verticalScrollbar, showVertical.Value);
                isVerticalVisible = showVertical.Value;
            }
            FastGridInternalUtil.SetIsVisible(scrollGap, isHorizontalVisible && isVerticalVisible);

            var horizontalWidth = canvas.Width - (isVerticalVisible ? ScrollSize : 0);
            var verticalHeight = canvas.Height - (isHorizontalVisible ? ScrollSize : 0);
            FastGridInternalUtil.SetWidth(horizontalScrollbar, Math.Max(horizontalWidth, 0));
            FastGridInternalUtil.SetHeight(verticalScrollbar, Math.Max(verticalHeight, 0));
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //MainDataHolder.CreateHeader();

            HeaderHeightChanged();
            _rowProvider.RowHeightChanged();

            PostponeUpdateUI();
        }

        public void FullReFilter() {
            _expandController.FullReFilter();
        }
        public void Redraw() {
            PostponeUpdateUI();
        }

        public IEnumerable<object> GetSelection() => _expandController.GetSelection();

        private int ObjectToRowIndex(object obj, int suggestedFindIndex) => _expandController.ObjectToRowIndex(obj, suggestedFindIndex);

        private IReadOnlyList<int> GetSelectedIndexesList() {
            Debug.Assert(AllowMultipleSelection);
            if (UseSelectionIndex)
                return SelectedIndexes;
            else {
                List<int> indexes = new List<int>();
                var suggestIdx = TopRowIndex;
                foreach (var obj in SelectedItems)
                {
                    var idx = ObjectToRowIndex(obj, suggestIdx);
                    if (idx >= 0) {
                        indexes.Add(idx);
                        suggestIdx = idx;
                    }
                }

                return indexes;
            }
        }

        private void SetSelectedIndexesList(IReadOnlyList<int> indexes) {
            Debug.Assert(AllowMultipleSelection);
            if (UseSelectionIndex)
                SelectedIndexes = new ObservableCollection<int>(indexes);
            else 
                SelectedItems = new ObservableCollection<object>( indexes.Select(i => _expandController.RowIndexToObject(i)));
        }

        private void ClearSelection() {
            if (AllowMultipleSelection)
                SetSelectedIndexesList(new List<int>());
            else if (UseSelectionIndex)
                SelectedIndex = 0;
            else
                SelectedItem = null;
        }

        private void ClearImpl(bool clearSelection = true) {
            ClearCanvas();
            if (clearSelection)
                ClearSelection();
            _rowProvider.ClearRows();
            PostponeUpdateUI();
        }

        public void Clear() {
            ItemsSource = null;
            Columns.Clear();
            MainDataHolder.CreateFilter();
            MainDataHolder.CreateHeader();
        }

        private void SelectShiftAdd(FastGridViewRow row) {
            // FIXME if i have from several (expanded) fastgrids, only select one

            if (AllowMultipleSelection)
            {
                var selectedIndexes = GetSelectedIndexesList();
                if (selectedIndexes.Count > 0) {
                    var rowIdx = ObjectToRowIndex(row.RowObject, TopRowIndex);
                    var min = Math.Min( selectedIndexes.Min(), rowIdx);
                    var max = Math.Max( selectedIndexes.Max(), rowIdx);
                    List<int> newSel = new List<int>();
                    for (int i = min; i <= max; ++i)
                        newSel.Add(i);
                    SetSelectedIndexesList(newSel);
                } else {
                    if (UseSelectionIndex)
                        SelectedIndexes = new ObservableCollection<int>{ ObjectToRowIndex(row.RowObject, TopRowIndex) };
                    else
                        SelectedItems = new ObservableCollection<object>{ row.RowObject };
                }
            } else {
                if (UseSelectionIndex)
                    SelectedIndex = ObjectToRowIndex(row.RowObject, TopRowIndex);
                else
                    SelectedItem = row.RowObject;
            }
        }

        private void SelectAdd(FastGridViewRow row, bool forceAdd = false) {
            // FIXME if i have from several (expanded) fastgrids, only select one
            if (UseSelectionIndex) {
                var rowIdx = ObjectToRowIndex(row.RowObject, TopRowIndex);
                if (rowIdx < 0)
                    throw new FastGridViewException($"fastgrid {Name}: Can't find row");
                if (AllowMultipleSelection) {
                    var copy = SelectedIndexes.ToList();
                    if (!copy.Contains(rowIdx))
                        copy.Add(rowIdx);
                    else if (!forceAdd)
                        copy.Remove(rowIdx);
                    SelectedIndexes = new ObservableCollection<int>(copy.Distinct());
                } else
                    SelectedIndex = rowIdx;
            } else {
                if (AllowMultipleSelection) {
                    var copy = SelectedItems.ToList();
                    if (!copy.Contains(row.RowObject))
                        copy.Add(row.RowObject);
                    else if (!forceAdd)
                        copy.Remove(row.RowObject);
                    SelectedItems = new ObservableCollection<object>(copy.Distinct());
                }
                else
                    SelectedItem = row.RowObject;
            }
        }
        private void SelectSet(FastGridViewRow row) {
            // FIXME if i have from several (expanded) fastgrids, only select one
            if (UseSelectionIndex) {
                var rowIdx = ObjectToRowIndex(row.RowObject, TopRowIndex);
                if (rowIdx < 0)
                    throw new FastGridViewException($"fastgrid {Name}: Can't find row");
                if (AllowMultipleSelection)
                    SelectedIndexes = new ObservableCollection<int> { rowIdx };
                else
                    SelectedIndex = rowIdx;
            } else {
                if (AllowMultipleSelection) {
                    SelectedItems = new ObservableCollection<object> { row.RowObject };
                } else
                    SelectedItem = row.RowObject;
            }
        }


        private void TryEditCell(FastGridViewRow row, FastGridViewColumn col, bool viaClick) {
            if (col == null || col.IsReadOnly || col.CellEditTemplate == null) {
                // can't edit this cell
                _editRow.AutoCommitEdit();
                return;
            }

            // here, i know the colum is editable
            _editRow.BeginEdit(row, col, viaClick);
        }

        private void TryEditCell(FastGridViewRow row, double left, bool viaClick) {
            var col = _editRow. LeftToColumn(left);
            TryEditCell(row, col, viaClick);
        }

        internal void OnMouseLeftButtonDown(FastGridViewRow row, MouseButtonEventArgs args) {
            if (row.IsSelected && !AllowMultipleSelection)
                // optimization - avoid draw when nothing happened
                return;

            var wasSelected = row.IsSelected;
            row.IsSelected = true;
            var ctrl = (args.KeyModifiers & ModifierKeys.Control) != 0;
            var shift = (args.KeyModifiers & ModifierKeys.Shift) != 0;
            if (shift)
                SelectShiftAdd(row);
            else if (ctrl)
                SelectAdd(row);
            else 
                SelectSet(row);
            PostponeUpdateUI();

            if (CanEdit()) {
                var pos = args.GetPosition(row);
                if (EditWithClick()) {
                    TryEditCell(row, pos.X + HorizontalOffset, viaClick: true);
                } else if (EditWithCurrentCellClick()) {
                    if (wasSelected)
                        TryEditCell(row, pos.X + HorizontalOffset, viaClick: true );
                    else 
                        _editRow.AutoCommitEdit();
                } else 
                    // user was editing , and now clicked elsewhere, but the edit will happen manually
                    _editRow.AutoCommitEdit();
            }
        }

        internal void RaiseCellEditBegin(CellEditBeginArgs args) {
            CellEditBegin?.Invoke(this, args);
        }

        internal void RaiseCellEditEnded(CellEditEndedArgs args) {
            CellEditEnded?.Invoke(this, args);
        }

        internal void RaiseCellEditEnding(CellEditEndingArgs args) {
            CellEditEnding?.Invoke(this, args);
        }

        internal void OnMouseRowEnter(FastGridViewRow row) {
            RowEnter?.Invoke(this, new RowEventArgs(row));
        }

        internal void OnMouseRowLeave(FastGridViewRow row) {
            RowLeave?.Invoke(this, new RowEventArgs(row));
        }

        private void EnsureColumnUniqueNames() {
            int uniqueIndex = 0;
            string prefix = "__unique__";
            foreach (var hci in HierarchicalInfos()) {
                foreach (var col in hci.Columns)
                    if (col.UniqueName == "")
                        col.UniqueName = prefix + ++uniqueIndex;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            EnsureColumnUniqueNames();

            if (MonitorOffscreen)
                SetCheckOffscreenTimer(true);

            MainDataHolder.CreateFilter();
            MainDataHolder.CreateHeader();
            HandleContextMenu();

            HeaderHeightChanged();
            _rowProvider.RowHeightChanged();
            PostponeUpdateUI();

            FastGridInternalUtil.SetLeft(MainDataHolder.HeaderControl(), 0);
            if (MainDataHolder.NeedsColumnGroup()) 
                FastGridInternalUtil.SetLeft(MainDataHolder.ColumnGroupControl(), 0);

            foreach (var hci in HierarchicalInfos())
                hci.PropertyChanged += HierarchicalInfo_PropertyChanged;

            horizontalScrollbar.Minimum = 0;
            horizontalScrollbar.Value = 0;
            DrawController.EditRow.RecreateEditCells();
        }

        private void HierarchicalInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
                case "RowTemplate":
                    ClearImpl(clearSelection: false);
                    break;

                case "HeaderTemplate": 
                    break;
            }
        }

        private void HandleContextMenu() {
            // handle OS bug - when you click on an menu item, by default, it doesn't close
            if (ContextMenu != null) {
                foreach (var mi in ContextMenu.Items.OfType<MenuItem>())
                    mi.Click += (s, a) => ContextMenu.IsOpen = false;
            }
        }

        private void SetCheckOffscreenTimer(bool start) {
            if (start)
                _checkOffscreenUiTimer.Start();
            else
                _checkOffscreenUiTimer.Stop();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) {
            _drawController.OnUnloaded();
            SetCheckOffscreenTimer(false);
        }

        private void canvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
            if (e.Delta == 0)
                return;
            Logger($"mouse wheel {e.Delta}");

            // simple logic -- as soon as the user scrolls, if he was editing, we save it
            _editRow.AutoCommitEdit();

            var goUp = e.Delta > 0;
            var maxRowIdx = _expandController.MaxRowIdx();
            var newIdx = goUp ? TopRowIndex - 1 : TopRowIndex + 1;
            if (newIdx >= 0 && newIdx <= maxRowIdx)
                VerticalScrollToRowIndex(newIdx);
        }


        private double _horizontalOffset = 0;
        private bool _isScrollingHorizontally = false;
        private bool isOffscreen_ = false;
        private object _rightClickSelectedObject = null;
        private bool _monitorOffscreen = true;
        private double _scrollSize = 17.5;


        private void OnHorizontalOffsetChange() {
            _rowProvider.SetHorizontalOffset(HorizontalOffset);
            DrawController.UpdateHorizontalScrollbar();
            DrawController.OnHorizontalOffsetChange();
            EditRow.HorizontalOffset = HorizontalOffset;
        }

        private void OnOffscreenChange() {
            Logger($"fastgrid: {Name} : moved {(isOffscreen_ ? "OFF screen" : "ON screen")}");
            if (IsOffscreen)
                _rowProvider.OnOffscreen();
            else {
                // coming back onscreen - paint ASAP
                _drawController.TryUpdateUI();
                _drawController.PostponeUpdateUI();
            }
        }

        private void OnRightClick(FastGridViewRow row) {
            switch (RightClickAutoSelect) {
            case RightClickAutoSelectType.None:
                break;
            case RightClickAutoSelectType.Select:
                SelectSet(row);
                break;
            case RightClickAutoSelectType.SelectAdd:
                SelectAdd(row, forceAdd:true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private void vm_PropertyChanged(string name) {
            switch (name) {
            case "HorizontalOffset":
                OnHorizontalOffsetChange();
                break;
            case "IsScrollingHorizontally":
                if (!IsScrollingHorizontally)
                    // end of scroll
                    HorizontalOffset = horizontalScrollbar.Value;
                break;
            case "IsOffscreen":
                OnOffscreenChange();
                break;
            case "MonitorOffscreen": 
                SetCheckOffscreenTimer(MonitorOffscreen);
                break;
            }
        }


        internal void vertical_bar_scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)    
        {
            // ... note: the last row is not fully visible
            var visibleCount = Math.Max( _drawController.VisibleCount - 1 , 0);
            var maxRowIdx = _expandController.MaxRowIdx();
            var valueScroll = Math.Min((int)e.NewValue, maxRowIdx);

            // simple logic -- as soon as the user scrolls, if he was editing, we save it
            _editRow.AutoCommitEdit();

            switch (e.ScrollEventType) {
            case ScrollEventType.SmallDecrement:
                if (TopRowIndex > 0)
                    VerticalScrollToRowIndex(TopRowIndex - 1);
                break;
            case ScrollEventType.SmallIncrement:
                if (TopRowIndex < maxRowIdx)
                    VerticalScrollToRowIndex(TopRowIndex + 1);
                break;
            case ScrollEventType.LargeDecrement:
                var pageupIdx = Math.Max(TopRowIndex - visibleCount, 0);
                VerticalScrollToRowIndex(pageupIdx);
                break;
            case ScrollEventType.LargeIncrement:
                var pagedownIdx = Math.Min(TopRowIndex + visibleCount, maxRowIdx);
                VerticalScrollToRowIndex(pagedownIdx);
                break;
            case ScrollEventType.ThumbTrack:
                // this is the user dragging the thumb - I don't want instant update, since very likely that would be very costly
                RowProvider.SetRowOpacity(0.4);
                IsMouseScrolling = true;
                break;
            case ScrollEventType.EndScroll:
                VerticalScrollToRowIndex(valueScroll);
                IsMouseScrolling = false;
                break;
            case ScrollEventType.ThumbPosition:
                VerticalScrollToRowIndex(valueScroll);
                break;
            case ScrollEventType.First:
                VerticalScrollToRowIndex(0);
                break;
            case ScrollEventType.Last:
                VerticalScrollToRowIndex(maxRowIdx);
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        internal void UpdateVerticalScrollbarValue()
        {
            // ... note: the last row is not fully visible
            var visibleCount = (canvas.Height / RowHeight);
            if (Math.Abs((double)(verticalScrollbar.ViewportSize - visibleCount)) > TOLERANCE)
                verticalScrollbar.ViewportSize = visibleCount;
            if (Math.Abs((double)(verticalScrollbar.Value - TopRowIndex)) > TOLERANCE)
                verticalScrollbar.Value = TopRowIndex;

            var maxRowIdx = _expandController.MaxRowIdx();
            if (Math.Abs((double)(verticalScrollbar.Maximum - maxRowIdx)) > TOLERANCE)
                verticalScrollbar.Maximum = maxRowIdx;
        }

        public void VerticalScrollToRowIndex(int rowIdx) => _drawController.VerticalScrollToRowIndex(rowIdx);
        public void ScrollToRow(object obj) => _drawController.ScrollToRow(obj);
        public void EnsureVisible(object obj) => _drawController.EnsureVisible(obj);
        public void EnsureVisible(FastGridViewColumn column) => _drawController.EnsureVisible(column);

        internal void OnExpandToggle(object obj) {
            _expandController.ToggleExpanded(obj);
        }

        private void canvas_KeyDown(object sender, KeyEventArgs e) {
            if (EditWithF2() && e.Key == Key.F2 && GetSelection().Any()) {
                var obj = GetSelection().First();
                var row = RowProvider.TryGetRow(obj);
                var col = _editRow.FirstEditableColumn();
                if (row != null && col != null) {
                    // we're editing the first column from this row
                    EnsureVisible(obj);
                    TryEditCell(row, col, viaClick: false);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            vm_PropertyChanged(propertyName);
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
