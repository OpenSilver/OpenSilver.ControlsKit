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
using OpenSilver;

namespace FastGrid.FastGrid
{
    /* 

     * filtering
     * - basically monitor for changes (based on sort, etc.)
     *
     * + text wrapping
     *
     *
     *
     * 
     *
     *
     * IMPORTANT:
     * Filtering
     * - at this time, we don't monitor objects that don't match the filter, even if a change could actually make them match the filter.
     *   Example: I can filter by Salary>1000. I add a user with Salary=500 (automatically filtered out). I later set his Salary=1001 (it will still be filtered out)
     *   Note: this limitation exists in Telerik as well.
     *
        FIXME
     * move to ControlKit
     *
     * FIXME CellEditTemplate
     *
     * LATER IDEAS:
     * - just in case this is still slow - i can create copies of the original objects and update stuff at a given interval
     *   (note: for me + .net7 -> this works like a boss, so not needed yet)
     *
     *     */

    public partial class FastGridView : UserControl, INotifyPropertyChanged {
        private const double TOLERANCE = 0.0001;

        public const double SCROLLBAR_WIDTH = 12;
        public const double SCROLLBAR_HEIGHT = 12;

        private const int OUTSIDE_SCREEN = -100000;

        // these are the rows - not all of them need to be visible, since we'll always make sure we have enough rows to accomodate the whole height of the control
        private List<FastGridViewRow> _rows = new List<FastGridViewRow>();
        private IReadOnlyList<object> _items;
        private bool _suspendRender = false;

        // this is the first visible row - based on this, I re-compute the _topRowIndex, on each update of the UI
        // the idea is this: no matter how many insertions/deletions, the top row remains the same -- but the top row index can change
        private object _topRow = null;
        // what's the top row index?
        private int _topRowIndexWhenNotScrolling = 0;

        // if >= 0, we're scrolling to this row
        private int _scrollingTopRowIndex = -1;

        // the idea - i don't want new rows to be created while I'm iterating the collection to update rows' positions
        private bool _isUpdatingUI = false;

        private int _visibleCount = 0;

        private DispatcherTimer _postponeUiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100)};
        private DispatcherTimer _checkOffscreenUiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500)};

        private FastGridViewColumnCollectionInternal _columns;

        private FastGridViewSortDescriptors _sortDescriptors;
        private FastGridViewSort _sort;

        private int uiTimerInterval_ = 100;

        private static bool _firstTime = true;

        private int _successfullyDrawnTopRowIdx = -1;//_topRowIndexWhenNotScrolling

        private bool _needsRefilter = false, _needsFullReSort = false, _needsReSort = false;

        public FastGridView() {
            this.InitializeComponent();
            _columns = new FastGridViewColumnCollectionInternal(this);
            _sort = new FastGridViewSort(this);
            _sortDescriptors = new FastGridViewSortDescriptors();
            _sortDescriptors.OnResort += () => {
                _needsFullReSort = true;
                PostponeUpdateUI();
            };

            _postponeUiTimer.Tick += (s, a) => {
                if (_scrollingTopRowIndex >= 0) {
                    _successfullyDrawnTopRowIdx = -1;
                    if (TryScrollToRowIndex(_scrollingTopRowIndex, out var optimizeDrawNow)) {
                        // scroll succeeded
                        if (_scrollingTopRowIndex < SortedItems.Count) {
                            _topRow = SortedItems[_scrollingTopRowIndex];
                            _topRowIndexWhenNotScrolling = _scrollingTopRowIndex;
                        } else {
                            _topRow = SortedItems.Count > 0 ? SortedItems[0] : null;
                            _topRowIndexWhenNotScrolling = 0;
                        }
                        _scrollingTopRowIndex = -1;
                        Console.WriteLine($"new top row (scrolled) {Name}: {_topRowIndexWhenNotScrolling}");
                    }
                    // the idea - allow the new bound rows to be shown visually (albeit, off-screen)
                    // this way, on the next tick, I can show them to the user visually -- instantly
                    if (!optimizeDrawNow)
                        return;
                }

                var uiAlreadyDrawn = _successfullyDrawnTopRowIdx == _topRowIndexWhenNotScrolling && _successfullyDrawnTopRowIdx >= 0;
                if (uiAlreadyDrawn) {
                    PreloadAhead();
                    _postponeUiTimer.Stop();
                    return;
                }

                if (TryUpdateUI())
                    // the idea - first, I show the user the updated UI (which can be time consuming anyway)
                    // then, on the next tick, I will preload stuff ahead, so that if user just moves a few rows up/down, everything is already good to go
                    _successfullyDrawnTopRowIdx = _topRowIndexWhenNotScrolling;
            };
            verticalScrollbar.Minimum = 0;
            horizontalScrollbar.Minimum = 0;

            _checkOffscreenUiTimer.Tick += (s, a) => {
                var left = Canvas.GetLeft(this);
                var top = Canvas.GetTop(this);
                var isOffscreen = left < -9999 || top < -9999;
                IsOffscreen = isOffscreen;
            };

            if (_firstTime) {
                _firstTime = false;
                Interop.ExecuteJavaScript("document.addEventListener('contextmenu', event => event.preventDefault());");
            }
        }

        private int TopRowIndex => _scrollingTopRowIndex >= 0 ? _scrollingTopRowIndex : _topRowIndexWhenNotScrolling;
        private bool IsEmpty => SortedItems == null || SortedItems.Count < 1;
        internal IReadOnlyList<object> FilteredItems => _items;
        internal IReadOnlyList<object> SortedItems => _sort.SortedItems;

        public FastGridViewColumnCollection Columns => _columns;
        public FastGridViewSortDescriptors SortDescriptors => _sortDescriptors;
        public bool IsScrollingVertically => _scrollingTopRowIndex >= 0;

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

        // instead of binding the row background, you can also have a function that is called before each row is shown
        // rationale: binding the row background might not be possible, or it may sometimes cause a bit of flicker
        public Func<object, Brush> RowBackgroundColorFunc { get; set; } = null;

        // if true -> on column resize + horizontal scrolling, the effect is instant
        // if false -> we dim the cells and then do the resize once the user finishes (much faster)
        public bool InstantColumnResize { get; set; } = false;

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

        private double HorizontalOffset {
            get => horizontalOffset_;
            set {
                if (value.Equals(horizontalOffset_)) return;
                horizontalOffset_ = value;
                OnPropertyChanged();
            }
        }

        private bool IsScrollingHorizontally {
            get => isScrollingHorizontally_;
            set {
                if (value == isScrollingHorizontally_) return;
                isScrollingHorizontally_ = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler SelectionChanged;

        public int UiTimerInterval {
            get => uiTimerInterval_;
            set {
                uiTimerInterval_ = value; 
                _postponeUiTimer.Interval = TimeSpan.FromMilliseconds(value);
            }
        }

        // FIXME not implemented yet
        // note: not bindable at this time
        public bool CanUserReorderColumns { get; set; } = false;

        // note: not bindable at this time
        public bool CanUserResizeRows { get; set; } = true;
        // note: not bindable at this time
        public bool CanUserSortColumns { get; set; } = true;
        // note: not bindable at this time
        public bool IsFilteringAllowed { get; set; } = false;

        public IEnumerable<object> VisibleRows() => _rows.Where(r => r.IsRowVisible).Select(r => r.RowObject);

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
                                                        "HeaderHeight", typeof(double), typeof(FastGridView), new PropertyMetadata(30d, HeaderHeightChanged));

        public double HeaderHeight {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        private static void RowHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).RowHeightChanged();
        }
        private static void HeaderHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).HeaderHeightChanged();
        }

        private void RowHeightChanged() {
            foreach (var row in _rows)
                row.RowHeight = RowHeight;
        }

        private void HeaderHeightChanged() {
            headerCtrl.Height = HeaderHeight;
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


        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
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
            (d as FastGridView).SelectedBackgroundChanged();
        }
        private static void RowTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).RowTemplateChanged();
        }
        private static void HeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as FastGridView).HeaderTemplateChanged();
        }

        private void SelectedBackgroundChanged()
        {
            foreach (var row in _rows)
                row.SelectedBrush = SelectionBackground;
        }

        private void SingleSelectedIndexChanged()
        {
            if (AllowMultipleSelection)
                throw new Exception("can't set SelectedIndex on multi-selection");

            UseSelectionIndex = true;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }

        private void SelectedIndexesChanged()
        {
            if (!AllowMultipleSelection)
                throw new Exception("can't set SelectedIndexes on multi-selection");

            UseSelectionIndex = true;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }

        private void SingleSelectedItemChanged()
        {
            if (AllowMultipleSelection)
                throw new Exception("can't set SelectedItem on multi-selection");

            UseSelectionIndex = false;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }

        private void SelectedItemsChanged()
        {
            if (!AllowMultipleSelection)
                throw new Exception("can't set SelectedItems on multi-selection");

            UseSelectionIndex = false;
            UpdateSelection();
            SelectionChanged?.Invoke(this,EventArgs.Empty);
        }

        public IEnumerable<object> GetSelection() {
            if (AllowMultipleSelection) {
                if (UseSelectionIndex) {
                    foreach (var idx in SelectedIndexes)
                        if (idx >= 0 && idx < SortedItems.Count)
                            yield return SortedItems[idx];
                } else
                    foreach (var obj in SelectedItems)
                        yield return obj;
            } else {
                if (UseSelectionIndex) {
                    if (SelectedIndex >= 0 && SelectedIndex < SortedItems.Count)
                        yield return SortedItems[SelectedIndex];
                } else if (SelectedItem != null)
                    yield return SelectedItem;
            }
        }

        private void UpdateSelection() {
            if (IsScrollingVertically) {
                PostponeUpdateUI();
                return;
            }
            var rowIdx = _topRowIndexWhenNotScrolling;
            var needsPostponeUI = false;
            for (int i = 0; i < _visibleCount; ++i) {
                var row = TryGetRow(rowIdx + i);
                if (row != null) {
                    var obj = SortedItems[rowIdx + i];
                    row.IsSelected = IsRowSelected(obj, rowIdx + i);
                } else
                    needsPostponeUI = true;
            }

            if (needsPostponeUI)
                // perhaps user started scrolling or something, since we could not find a row to update
                PostponeUpdateUI();
        }

        private void RowTemplateChanged() {
            ClearCanvas();
            PostponeUpdateUI();
        }

        private void HeaderTemplateChanged() {
            headerCtrl.ItemTemplate = HeaderTemplate;
        }

        private void ClearCanvas() {
            // the idea -> all our extra controls are kept in a child canvas (like, scroll bars + header)
            _rows.Clear();
            foreach (var child in canvas.Children.ToList())
                if (child is FastGridViewRow)
                    canvas.Children.Remove(child);
        }

        private void OnItemsSourceChanged() {
            Console.WriteLine($"fastgrid itemsource set for {Name}");
            if (ItemsSource == null) {
                ClearCanvas();
                PostponeUpdateUI();
                return;
            }

            // allow only ObservableCollection<T> - fast to iterate + know when elements are added/removed
            if (!(ItemsSource is INotifyCollectionChanged) || !(ItemsSource is IReadOnlyList<object>))
                throw new Exception("ItemsSource needs to be ObservableCollection<>");

            if (_items is INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= FastGridView_CollectionChanged;
            _items = (IReadOnlyList<object>)ItemsSource ;
            (ItemsSource as INotifyCollectionChanged).CollectionChanged += FastGridView_CollectionChanged;

            // force recompute
            _topRow = null;

            ClearCanvas();
            _needsRefilter = true;
            _needsFullReSort = true;
            PostponeUpdateUI();
        }

        public void SuspendRender() {
            _suspendRender = true;
        }

        public void ResumeRender() {
            if (!_suspendRender)
                return;
            _suspendRender = false;
            PostponeUpdateUI();
        }


        private void PostponeUpdateUI() {
            _successfullyDrawnTopRowIdx = -1;
            if (!_postponeUiTimer.IsEnabled) {
                Console.WriteLine($"fastgrid: postponed UI update {Name}");
                _postponeUiTimer.Start();
            }
        }

        // the idea:
        // a property we're sorting by has changed -- thus, we need to resort
        internal void NeedsResort() {
            // resorting only happens when we need to redraw, no point in doing it faster,
            // since several changes can happen at the same time
            _needsReSort = true;
            PostponeUpdateUI();
        }

        // if object not found, returns -1
        private int ObjectToRowIndex(object obj, int suggestedFindIndex)
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
            if ( ReferenceEquals(SortedItems[suggestedFindIndex], obj))
                return suggestedFindIndex; // top row is the same

            for (int i = 1; i < MAX_NEIGHBORHOOD; ++i)
            {
                var beforeIdx = suggestedFindIndex - i;
                var afterIdx = suggestedFindIndex + i;
                if (beforeIdx >= 0 && ReferenceEquals(SortedItems[beforeIdx], obj)) 
                    return beforeIdx;
                else if (afterIdx < SortedItems.Count && ReferenceEquals(SortedItems[afterIdx], obj)) 
                    return afterIdx;
            }

            // in this case, top row is not in the neighborhood
            for (int i = 0; i < SortedItems.Count; ++i)
                if (ReferenceEquals(SortedItems[i], obj)) 
                    return i;

            return -1;
        }

        private void ComputeTopRowIndex()
        {
            if (SortedItems == null || SortedItems.Count < 1) {
                // nothing to draw
                _topRow = null;
                _topRowIndexWhenNotScrolling = 0;
                return; 
            }

            var foundIdx = ObjectToRowIndex(_topRow, _topRowIndexWhenNotScrolling);
            if (foundIdx == _topRowIndexWhenNotScrolling)
                return; // same

            if (foundIdx >= 0)
                _topRowIndexWhenNotScrolling = foundIdx;
            else {
                // if topRow not found -> that means we removed it from the collection. If so, just go to the top
                _topRow = SortedItems[0];
                _topRowIndexWhenNotScrolling = 0;
            }
            Debug.WriteLine($"new top row {Name}: {_topRowIndexWhenNotScrolling}");
        }

        private bool CanDraw() {
            if (_columns == null)
                return false; // not initialized yet
            if (canvas.Width < 1 || canvas.Height < 1 || Visibility == Visibility.Collapsed)
                return false; // we're hidden
            if (_suspendRender)
                return false;
            if (SortedItems == null)
                return false;
            if (RowHeight < 1)
                return false;
            if (_isUpdatingUI)
                return false;

            return true;
        }

        // the idea: when you do a pageup/pagedown, without this optimization, it would re-bind all rows (to the newly scrolled data), and that would take time
        // (visually, 250ms or so), so the user would actually see all the rows clear, and then redrawn; and for about 250 ms, the rows would appear clear -- not visually appealing
        //
        // the workaround is to visually load the scrolled rows outside the screen, which will not affect the user in any way. then, when all is created/bound/shown, bring it into the user's view
        private bool TryScrollToRowIndex(int rowIdx, out bool optimizeDrawNow) {
            optimizeDrawNow = false;
            if (!CanDraw())
                return false;

            Console.WriteLine($"scroll to {rowIdx} - started");
            _isUpdatingUI = true;
            var newlyCreatedRowCount = 0;
            try {
                var maxRowIdx = Math.Min(SortedItems.Count, rowIdx + _visibleCount );
                while (rowIdx < maxRowIdx) {
                    var tryGetRow = TryGetRow(rowIdx);
                    var tryReuseRow = tryGetRow == null ? TryReuseRow() : null;
                    var tryCreateRow = tryReuseRow == null && tryGetRow == null ? CreateRow() : null;

                    var row = tryGetRow ?? tryReuseRow ?? tryCreateRow;
                    if (tryGetRow == null) {
                        // it's a newly created/bound row - create it outside of what the user sees
                        // once it's fully created + bound, then we can show it visually, and it will be instant
                        FastGridUtil.SetLeft(row, OUTSIDE_SCREEN);
                        ++newlyCreatedRowCount;
                    }

                    var obj = SortedItems[rowIdx];
                    row.RowObject = obj;
                    row.Used = true;
                    row.IsRowVisible = true;
                    FastGridUtil.SetDataContext(row, SortedItems[rowIdx]);
                    row.IsSelected = IsRowSelected(obj, rowIdx);

                    ++rowIdx;
                }
            } finally {
                _isUpdatingUI = false;
            }
            Console.WriteLine($"scroll COMPLETE");
            const int MAX_NEWLY_CREATED_ROW_COUNT_IS_INSTANT = 4;
            optimizeDrawNow = newlyCreatedRowCount <= MAX_NEWLY_CREATED_ROW_COUNT_IS_INSTANT;
            return true;
        }

        private bool TryUpdateUI() {
            if (IsEmpty) {
                FastGridUtil.SetIsVisible(canvas, false);
                return false;
            }
            FastGridUtil.SetIsVisible(canvas, true);
            if (!CanDraw())
                return false;

            _isUpdatingUI = true;
            try {
                if (_needsRefilter)
                    FullReFilter();
                if (_needsFullReSort) {
                    _needsFullReSort = false;
                    _needsReSort = false;
                    _sort.FullResort();
                }
                if (_needsReSort) {
                    _needsReSort = false;
                    _sort.Resort();
                }

                ComputeTopRowIndex();

                double y = HeaderHeight;
                var height = canvas.Height;
                var rowIdx = _topRowIndexWhenNotScrolling;

                foreach (var row in _rows)
                    row.IsRowVisible = false;

                var visibleCount = 0;
                while (y < height && rowIdx < SortedItems.Count) {
                    var row = TryGetRow(rowIdx) ?? TryReuseRow() ?? CreateRow();
                    FastGridUtil.SetTop(row, y);
                    Debug.Assert(row.RowHeight >= 0);
                    var obj = SortedItems[rowIdx];
                    row.RowObject = obj;
                    row.Used = true;
                    row.IsRowVisible = true;
                    y += row.RowHeight;
                    FastGridUtil.SetDataContext(row, SortedItems[rowIdx]);
                    UpdateRowColor(row);
                    row.IsSelected = IsRowSelected(obj, rowIdx);
                    row.HorizontalOffset = HorizontalOffset;
                    row.UpdateUI();

                    ++rowIdx;
                    ++visibleCount;
                }
                // ... update visible count just once, just in case the above is async
                _visibleCount = visibleCount;
                Console.WriteLine($"fastgrid {Name} - draw {_visibleCount}");

                HideInvisibleRows();
                UpdateVerticalScrollbar();

            } finally {
                _isUpdatingUI = false;
            }
            return true;
        }

        private void PreloadAhead() {
            _isUpdatingUI = true;
            List<int> extra = new List<int>();
            for (int i = 1; i <= ShowAheadExtraRows; ++i) {
                if (_topRowIndexWhenNotScrolling - i >= 0)
                    extra.Add(_topRowIndexWhenNotScrolling - i);
                if (_topRowIndexWhenNotScrolling + _visibleCount + i < SortedItems.Count)
                    extra.Add(_topRowIndexWhenNotScrolling + _visibleCount + i);
            }
            // note: dump only those that haven't already been loaded
            Console.WriteLine($"fastgrid {Name} - preloading ahead [{string.Join(",",extra.Where(i => TryGetRow(i) == null))}]");
            var cacheAhead =(int)Math.Round(_visibleCount * CreateExtraRowsAheadPercent + ShowAheadExtraRows * 2) ;
            while (_rows.Count < cacheAhead) {
                var row = CreateRow();
                row.Used = false;
                FastGridUtil.SetLeft(row, OUTSIDE_SCREEN);
            }
            try {

                foreach (var row in _rows)
                    row.Preloaded = false;

                foreach (var rowIdx in extra) {
                    var row = TryGetRow(rowIdx) ?? TryReuseRow() ?? CreateRow();
                    FastGridUtil.SetLeft(row, OUTSIDE_SCREEN);
                    var obj = SortedItems[rowIdx];
                    row.RowObject = obj;
                    row.Used = true;
                    row.Preloaded = true;
                    row.IsRowVisible = false;
                    FastGridUtil.SetDataContext(row, SortedItems[rowIdx]);
                    UpdateRowColor(row);
                    row.IsSelected = IsRowSelected(obj, rowIdx);
                    row.HorizontalOffset = HorizontalOffset;
                    row.UpdateUI();
                }
                HideInvisibleRows();
            } finally {
                _isUpdatingUI = false;
            }
        }


        private void UpdateRowColor(FastGridViewRow row) {
            if (RowBackgroundColorFunc != null) {
                var color = RowBackgroundColorFunc(row.RowObject);

                if (!FastGridUtil.SameColor(color, FastGridUtil.ControlBackground(row.RowContentChild)))
                    FastGridUtil.SetControlBackground(row.RowContentChild, color);
            }
        }

        private int MaxRowIdx() {
            // ... note: the last row is not fully visible
            var visibleCount = GuessRowCount();
            var maxRowIdx = Math.Max(SortedItems.Count - visibleCount + 1, 0);

            return maxRowIdx;
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
                if (!InstantColumnResize)
                    SetRowOpacity(0.4);
                break;
            case ScrollEventType.EndScroll:
                IsScrollingHorizontally = false;
                HorizontalScroll(e.NewValue, updateScrollbarValue: false);
                if (!InstantColumnResize)
                    SetRowOpacity(1);
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

        private void HorizontalScroll(double value, bool updateScrollbarValue = true) {
            value = Math.Max(horizontalScrollbar.Minimum, Math.Min(value, horizontalScrollbar.Maximum));
            if (InstantColumnResize || !IsScrollingHorizontally)
                HorizontalOffset = value;

            if (updateScrollbarValue)
                horizontalScrollbar.Value = value;
        }

        private void SetRowOpacity(double value) {
            foreach (var row in _rows.Where(r => r.IsRowVisible))
                FastGridUtil.SetOpacity(row, value);
        }

        private void vertical_bar_scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)    
        {
            // ... note: the last row is not fully visible
            var visibleCount = Math.Max( _visibleCount - 1 , 0);
            var maxRowIdx = MaxRowIdx();
            var valueScroll = Math.Min((int)e.NewValue, maxRowIdx);


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
                SetRowOpacity(0.4);
                break;
            case ScrollEventType.EndScroll:
                VerticalScrollToRowIndex(valueScroll);
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

        private void UpdateHorizontalScrollbar() {
            // just so the user can clearly see the last column, and also resize it
            var EXTRA_SIZE = 20;

            horizontalScrollbar.ViewportSize = canvas.Width;
            var columnsWidth = Columns.Sum(c => c.IsVisible ? c.Width : 0);
            horizontalScrollbar.Maximum = Math.Max(columnsWidth + EXTRA_SIZE - canvas.Width, 0) ;
        }

        private void UpdateVerticalScrollbar()
        {
            // ... note: the last row is not fully visible
            var visibleCount = GuessRowCount();
            if (Math.Abs((double)(verticalScrollbar.ViewportSize - visibleCount)) > TOLERANCE)
                verticalScrollbar.ViewportSize = visibleCount;
            if (Math.Abs((double)(verticalScrollbar.Value - TopRowIndex)) > TOLERANCE)
                verticalScrollbar.Value = TopRowIndex;

            var maxRowIdx = MaxRowIdx();
            if (Math.Abs((double)(verticalScrollbar.Maximum - maxRowIdx)) > TOLERANCE)
                verticalScrollbar.Maximum = maxRowIdx;
        }

        private bool IsRowSelected(object row, int rowIdx)
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
                    return SelectedItems.Any(i => ReferenceEquals(i, row));
                else
                    return ReferenceEquals(row, SelectedItem);
            }
        }

        public void VerticalScrollToRowIndex(int rowIdx) {
            if (rowIdx < 0 || rowIdx >= SortedItems.Count)
                return; // invalid index

            if (_scrollingTopRowIndex >= 0) {
                // we're in the process of scrolling already
                // (note: while scrolling, the postponeUiTimer is already running)
                _scrollingTopRowIndex = rowIdx;
                Console.WriteLine($"scroll to row {rowIdx}");
                return;
            }

            // here, we're not scrolling
            if (!ReferenceEquals(_topRow, SortedItems[rowIdx])) {
                _scrollingTopRowIndex = rowIdx;
                Console.WriteLine($"scroll to row {rowIdx}");
                PostponeUpdateUI();
            }
        }

        public void ScrollToRow(object obj) {
            if (SortedItems.Count < 1)
                return; // nothing to scroll to

            if (_scrollingTopRowIndex >= 0) {
                // we're in the process of scrolling already
                // (note: while scrolling, the postponeUiTimer is already running)
                _scrollingTopRowIndex = ObjectToRowIndex(obj, _scrollingTopRowIndex);
                return;
            }

            // here, we're not scrolling
            if (ReferenceEquals(_topRow, obj))
                return;

            var rowIdx = ObjectToRowIndex(obj, -1);
            if (rowIdx < 0)
                rowIdx = 0;
            _scrollingTopRowIndex = rowIdx;
            Console.WriteLine($"scroll to row {rowIdx}");
            PostponeUpdateUI();
        }


        private void HideInvisibleRows() {
            foreach (var row in _rows) {
                var visible = row.IsRowVisible;
                var left = visible ? 0 : -100000;
                FastGridUtil.SetLeft(row, left);

                if (visible) 
                    FastGridUtil.SetOpacity(row, 1);
                else {
                    if (!row.Preloaded) {
                        row.DataContext = null;
                        row.Used = false;
                    }
                }
            }
        }

        private FastGridViewRow TryGetRow(int rowIdx)
        {
            var obj = SortedItems[rowIdx];
            foreach (var row in _rows)
                if (ReferenceEquals(row.RowObject,obj) && row.Used) 
                    return row;
            return null;
        }

        private FastGridViewRow TryReuseRow() {
            foreach (var row in _rows)
                if (row.DataContext == null && !row.Used) {
                    row.Used = true;
                    return row;
                }

            return null;
        }

        private FastGridViewRow CreateRow() {
            var row = new FastGridViewRow(RowTemplate, _columns, RowHeight) {
                Width = canvas.Width,
                Used = true,
                SelectedBrush = SelectionBackground,
            };
            _rows.Add(row);
            canvas.Children.Add(row);
            Console.WriteLine($"row created({Name}), rows={_rows.Count}");
            return row;
        }

        private void FastGridView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (CanUserSortColumns || IsFilteringAllowed) {
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
                    if (IsFilteringAllowed)
                        _needsRefilter = true;
                    if (CanUserSortColumns)
                        _needsFullReSort = true;
                    break;
                }
            }

            PostponeUpdateUI();
        }

        private bool MatchesFilter(object item) {
            if (!IsFilteringAllowed)
                return true; // no filtering

            // FIXME
            return true;
        }

        private void OnAddedItem(object item) {
            if (!MatchesFilter(item))
                return; // doesn't matter
            _sort.SortedAdd(item);
        }

        private void OnRemovedItem(object item) {
            if (!MatchesFilter(item))
                return; // doesn't matter
            _sort.Remove(item);
        }

        private int GuessRowCount() => (int)( canvas.Height / RowHeight);

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            canvas.Width = e.NewSize.Width;
            canvas.Height = e.NewSize.Height;
            verticalScrollbar.Width = SCROLLBAR_WIDTH;
            verticalScrollbar.Height = Math.Max(e.NewSize.Height - SCROLLBAR_HEIGHT, 0) ;
            horizontalScrollbar.Width = Math.Max(e.NewSize.Width - SCROLLBAR_WIDTH, 0) ;
            horizontalScrollbar.Height = SCROLLBAR_HEIGHT;
            FastGridUtil.SetLeft(verticalScrollbar, e.NewSize.Width - SCROLLBAR_WIDTH);
            FastGridUtil.SetTop(horizontalScrollbar, e.NewSize.Height - SCROLLBAR_HEIGHT);

            headerCtrl.Width = e.NewSize.Width;
            headerCtrl.Height = HeaderHeight;

            foreach (var row in _rows)
                row.Width = e.NewSize.Width;

            UpdateHorizontalScrollbar();
            PostponeUpdateUI();
        }

        private void CreateHeader() {
            headerCtrl.ItemTemplate = HeaderTemplate;
            headerCtrl.ItemsSource = _columns;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            CreateHeader();

            HeaderHeightChanged();
            RowHeightChanged();

            PostponeUpdateUI();
        }

        public void Redraw() {
            PostponeUpdateUI();
        }

        private void SelectAdd(FastGridViewRow row) {
            if (UseSelectionIndex) {
                var rowIdx = ObjectToRowIndex(row.RowObject, TopRowIndex);
                if (rowIdx < 0)
                    throw new Exception($"fastgrid {Name}: Can't find row");
                if (AllowMultipleSelection) {
                    var copy = SelectedIndexes.ToList();
                    copy.Add(rowIdx);
                    SelectedIndexes = new ObservableCollection<int>(copy);
                } else
                    SelectedIndex = rowIdx;
            } else {
                if (AllowMultipleSelection) {
                    var copy = SelectedItems.ToList();
                    copy.Add(row.RowObject);
                    SelectedItems = new ObservableCollection<object>(copy);
                }
                else
                    SelectedItem = row.RowObject;
            }
        }
        private void SelectSet(FastGridViewRow row) {
            if (UseSelectionIndex) {
                var rowIdx = ObjectToRowIndex(row.RowObject, TopRowIndex);
                if (rowIdx < 0)
                    throw new Exception($"fastgrid {Name}: Can't find row");
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

        internal void OnMouseLeftButtonDown(FastGridViewRow row, MouseButtonEventArgs eventArgs) {
            row.IsSelected = true;
            var ctrl = (eventArgs.KeyModifiers & ModifierKeys.Control) != 0;
            if (ctrl)
                SelectAdd(row);
            else 
                SelectSet(row);
            PostponeUpdateUI();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            var parent = VisualTreeHelper.GetParent(this);
            if (parent is Canvas)
                _checkOffscreenUiTimer.Start();

            HandleFilterSortColumns();
            CreateHeader();
            HandleContextMenu();

            HeaderHeightChanged();
            RowHeightChanged();
            PostponeUpdateUI();
        }

        private void HandleFilterSortColumns() {
            if (!CanUserSortColumns && !IsFilteringAllowed)
                return;
            
            // here, each sortable/filterable column needs to have a databinding property name
            foreach (var col in Columns)
                if ((col.IsFilterable || col.IsSortable) && col.DataBindingPropertyName == "")
                    throw new Exception($"Fastgrid: if filter and/or sort, you need to set DataBindingPropertyName for all columns ({col.FriendlyName()})");


        }

        // recomputes the filter completely, ignoring any previous cache
        // after this, you need to re-sort
        private void FullReFilter() {
            _needsRefilter = false;
            // FIXME to implement
            // also - if no filter, reuse _items
            _needsFullReSort = true;
        }

        private void HandleContextMenu() {
            // handle OS bug - when you click on an menu item, by default, it doesn't close
            if (ContextMenu != null) {
                foreach (var mi in ContextMenu.Items.OfType<MenuItem>())
                    mi.Click += (s, a) => ContextMenu.IsOpen = false;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) {
            _postponeUiTimer.Stop();
            _checkOffscreenUiTimer.Stop();
        }

        private void canvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
            if (e.Delta == 0)
                return;
            Console.WriteLine($"mouse wheel {e.Delta}");

            var goUp = e.Delta > 0;
            var maxRowIdx = MaxRowIdx();
            var newIdx = goUp ? TopRowIndex - 1 : TopRowIndex + 1;
            if (newIdx >= 0 && newIdx <= maxRowIdx)
                VerticalScrollToRowIndex(newIdx);
        }

        internal void OnColumnsCollectionChanged() {

        }

        private bool _ignoreSort = false;
        private double horizontalOffset_ = 0;
        private bool isScrollingHorizontally_ = false;
        private bool isOffscreen_ = false;

        internal void OnColumnPropertyChanged(FastGridViewColumn col, string propertyName) {
            switch (propertyName) {
            case "IsVisible": 
                foreach (var row in _rows)
                    row.SetCellVisible(col, col.IsVisible);
                PostponeUpdateUI();
                break;

            case "IsResizingColumn":
                if (col.IsResizingColumn) {
                    if (!InstantColumnResize)
                        SetRowOpacity(0.4);
                } else {
                    if (!InstantColumnResize) {
                        SetRowOpacity(1);
                        foreach (var row in _rows)
                            row.SetCellWidth(col, col.Width);
                        PostponeUpdateUI();
                    }

                    UpdateHorizontalScrollbar();
                    // the idea - the old value might have become invalid
                    HorizontalScroll(horizontalScrollbar.Value);
                }
                break;

            case "Width":
                var updateCellWidthNow = InstantColumnResize || !col.IsResizingColumn;
                if (updateCellWidthNow) {
                    foreach (var row in _rows)
                        row.SetCellWidth(col, col.Width);
                    PostponeUpdateUI();
                }
                break;
            
            case "MinWidth": 
                foreach (var row in _rows)
                    row.SetCellMinWidth(col, col.MinWidth);
                PostponeUpdateUI();
                break;
            
            case "MaxWidth": 
                foreach (var row in _rows)
                    row.SetCellMaxWidth(col, col.MaxWidth);
                PostponeUpdateUI();
                break;
            
            case "ColumnIndex": 
                foreach (var row in _rows)
                    row.SetCellIndex(col, col.ColumnIndex);
                PostponeUpdateUI();
                break;

            case "Sort":
                if (!_ignoreSort) {
                    _ignoreSort = true;
                    if (!AllowSortByMultipleColumns && !col.IsSortNone) {
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
                    _ignoreSort = false;
                }
                break;

            case "HeaderText": break;
            case "CanResize": break;
            case "CellTemplate": break;
            case "CellEditTemplate": break;
            }
        }

        private void OnHorizontalOffsetChange() {
            foreach (var row in _rows.Where(r => r.IsRowVisible))
                row.HorizontalOffset = HorizontalOffset;
            UpdateHorizontalScrollbar();
            FastGridUtil.SetLeft(headerCtrl, -HorizontalOffset);
        }

        private void OnOffscreenChange() {
            Console.WriteLine($"fastgrid: {Name} : moved {(isOffscreen_ ? "OFF screen" : "ON screen")}");
            if (IsOffscreen)
                foreach (var row in _rows) {
                    row.DataContext = null;
                    row.Used = false;
                    row.RowObject = null;
                }
            else {
                // coming back onscreen - paint ASAP
                TryUpdateUI();
                PostponeUpdateUI();
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
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            vm_PropertyChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
