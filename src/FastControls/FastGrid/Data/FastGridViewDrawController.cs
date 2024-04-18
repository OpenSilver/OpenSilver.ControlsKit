using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;
using FastGrid.FastGrid.Expand;
using OpenSilver.ControlsKit.Edit;
using OpenSilver.ControlsKit.FastGrid.Row;
using OpenSilver.ControlsKit.FastGrid.Util;

namespace FastGrid.FastGrid.Data
{
    internal class FastGridViewDrawController
    {
        internal const int OUTSIDE_SCREEN = -100000;

        private FastGridView _self;

        private FastGridViewRowProvider RowProvider => _self.RowProvider;
        private FastGridViewExpandController Expander => _self.ExpandController;

        internal FastGridViewEditRow EditRow => _self.EditRow;

        private bool _suspendRender = false;

        // this is the first visible row - based on this, I re-compute the _topRowIndex, on each update of the UI
        // the idea is this: no matter how many insertions/deletions, the top row remains the same -- but the top row index can change
        private object _topRow = null;
        // what's the top row index?
        private int _topRowIndexWhenNotScrolling = 0;

        private int _visibleCount = 0;

        // the idea - i don't want new rows to be created while I'm iterating the collection to update rows' positions
        private bool _isUpdatingUI = false;

        // if >= 0, we're scrolling to this row
        private int _scrollingTopRowIndex = -1;
        private int _successfullyDrawnTopRowIdx = -1;//_topRowIndexWhenNotScrolling

        private int _uiTimerInterval = 100;
        private UiTimer _postponeUiTimer ;

        public bool IsScrollingVertically => _scrollingTopRowIndex >= 0;
        internal int TopRowIndex => _scrollingTopRowIndex >= 0 ? _scrollingTopRowIndex : _topRowIndexWhenNotScrolling;

        private static Action<string> Logger => FastGridView.Logger;

        // just in case we need to wait more before fully showing all rows
        // (this is because the UI updates asynchronously, and the more rows that changed context we have, the more it takes for this to redraw, 
        //  but unfortunately, we don't get notified when the redraw is complete -- so all we can do is guess)
        private int _waitBeforeDrawCount = 0;

        private Dictionary<int, RowInfo> _visibleHeaders = new Dictionary<int, RowInfo>();

        internal int VisibleCount => _visibleCount;

        private Action _postponedUpdateUiAction;

        // for the next UI update to work, even if we're editing
        // useful when we navigate with keys, and one navigation will actually require us to scroll
        private bool _forceUpdateUiWhileEditing;

        public FastGridViewDrawController(FastGridView self) {
            _self = self;
            _postponeUiTimer = new UiTimer(_self, _uiTimerInterval, "PostponeUI");
            _postponeUiTimer.Tick += () => {
                if (TryUpdateUITick())
                    _postponeUiTimer.Stop();
            };
        }

        public int UiTimerInterval {
            get => _uiTimerInterval;
            set {
                _uiTimerInterval = value; 
                _postponeUiTimer.IntervalMillis = value;
            }
        }



        internal void UpdateSelection() {
            if (IsScrollingVertically) {
                _self.PostponeUpdateUI();
                return;
            }
            var rowIdx = _topRowIndexWhenNotScrolling;
            var needsPostponeUI = false;
            for (int i = 0; i < _visibleCount; ++i) {
                var obj = Expander.RowIndexToObject(rowIdx + i);
                var row = RowProvider.TryGetRow(obj);
                if (row != null) {
                    row.IsSelected = _self.IsRowSelected(obj, rowIdx + i);
                } else
                    needsPostponeUI = true;
            }

            if (needsPostponeUI)
                // perhaps user started scrolling or something, since we could not find a row to update
                _self.PostponeUpdateUI();
        }

        internal void PostponeUpdateUI() {
            _successfullyDrawnTopRowIdx = -1;
            if (!_postponeUiTimer.IsEnabled) {
                Logger($"fastgrid: postponed UI update {_self.Name}");
                _postponeUiTimer.Start();
            }
        }

        private int ObjectToRowIndex(object obj, int suggestedFindIndex) => Expander.ObjectToRowIndex(obj, suggestedFindIndex);


        private bool TryUpdateUITick() {
            try {
                if (_scrollingTopRowIndex >= 0) {
                    _successfullyDrawnTopRowIdx = -1;
                    if (TryScrollToRowIndex(_scrollingTopRowIndex, out var optimizeDrawNow)) {
                        // scroll succeeded
                        if (_scrollingTopRowIndex < Expander.RowCount()) {
                            _topRow = Expander.RowIndexToObject(_scrollingTopRowIndex);
                            _topRowIndexWhenNotScrolling = _scrollingTopRowIndex;
                        } else {
                            _topRow = Expander.RowCount() > 0 ? Expander.RowIndexToObject(0) : null;
                            _topRowIndexWhenNotScrolling = 0;
                        }
                        _scrollingTopRowIndex = -1;
                        Logger($"new top row (scrolled) {_self.Name}: {_topRowIndexWhenNotScrolling}");
                    }
                    // the idea - allow the new bound rows to be shown visually (albeit, off-screen)
                    // this way, on the next tick, I can show them to the user visually -- instantly
                    if (!optimizeDrawNow)
                        return false;
                }

                var uiAlreadyDrawn = _successfullyDrawnTopRowIdx == _topRowIndexWhenNotScrolling && _successfullyDrawnTopRowIdx >= 0;
                if (uiAlreadyDrawn) {
                    PreloadAhead();
                    return true;
                }

                if (TryUpdateUI())
                    // the idea - first, I show the user the updated UI (which can be time consuming anyway)
                    // then, on the next tick, I will preload stuff ahead, so that if user just moves a few rows up/down, everything is already good to go
                    _successfullyDrawnTopRowIdx = _topRowIndexWhenNotScrolling;
                return false;
            }
            catch (Exception e) {
                Logger($"FATAL: exception in UI Tick {_self.Name} {e}");
                return false;
            }
        }

        private bool CanDraw() {
            if (_self.Columns == null)
                return false; // not initialized yet
            if (_self.canvas.Width < 1 || _self.canvas.Height < 1 || _self.Visibility == Visibility.Collapsed)
                return false; // we're hidden
            if (_suspendRender)
                return false;
            if (!Expander.HasSource)
                return false;
            if (_self.RowHeight < 1)
                return false;
            if (_isUpdatingUI)
                return false;

            if (_self.IsOffscreen)
                return false;

            if (_self.IsMouseScrolling)
                return false;

            if (!_self.CanUpdateUI())
                return false;

            if (_self.EditRow.IsEditing && !_forceUpdateUiWhileEditing)
                // the idea: while editing, the UI remains "frozen", for instance, no filtering and sorting, since a single edit of a value can trigger a resort 
                return false;

            return true;
        }

        internal void SetSource(IEnumerable source) {
            // force recompute
            _topRow = null;
        }

        // the idea: when you do a pageup/pagedown, without this optimization, it would re-bind all rows (to the newly scrolled data), and that would take time
        // (visually, 250ms or so), so the user would actually see all the rows clear, and then redrawn; and for about 250 ms, the rows would appear clear -- not visually appealing
        //
        // the workaround is to visually load the scrolled rows outside the screen, which will not affect the user in any way. then, when all is created/bound/shown, bring it into the user's view
        private bool TryScrollToRowIndex(int rowIdx, out bool optimizeDrawNow) {
            optimizeDrawNow = false;
            if (!CanDraw())
                return false;

            Logger($"scroll to {rowIdx} - started");
            _isUpdatingUI = true;
            var newlyCreatedRowCount = 0;
            try {
                var maxRowIdx = Math.Min(Expander.RowCount(), rowIdx + _visibleCount );
                while (rowIdx < maxRowIdx) {
                    var ri = Expander.RowIndexToInfo(rowIdx);
                    if (ri.HeaderControl != null) {
                        ++rowIdx;
                        continue;
                    }

                    var obj = Expander.RowIndexToObject(rowIdx);
                    var tryGetRow = RowProvider.TryGetRow(obj);
                    var tryReuseRow = tryGetRow == null ? RowProvider.TryReuseRow(obj) : null;
                    var tryCreateRow = tryReuseRow == null && tryGetRow == null ? RowProvider.CreateRow(obj) : null;

                    var row = tryGetRow ?? tryReuseRow ?? tryCreateRow;
                    if (tryGetRow == null) {
                        // it's a newly created/bound row - create it outside of what the user sees
                        // once it's fully created + bound, then we can show it visually, and it will be instant
                        FastGridInternalUtil.SetLeft(row, OUTSIDE_SCREEN);
                        ++newlyCreatedRowCount;
                    }

                    row.RowObject = obj;
                    row.Used = true;
                    row.IsRowVisible = true;
                    FastGridInternalUtil.SetDataContext(row, obj, out _);
                    row.IsSelected = _self.IsRowSelected(obj, rowIdx);

                    ++rowIdx;
                }
            } finally {
                _isUpdatingUI = false;
            }
            Logger($"scroll COMPLETE");
            const int MAX_NEWLY_CREATED_ROW_COUNT_IS_INSTANT = 4;
            optimizeDrawNow = newlyCreatedRowCount <= MAX_NEWLY_CREATED_ROW_COUNT_IS_INSTANT;
            return true;
        }
        private void UpdateRowColor(FastGridViewRow row) {
            if (_self.RowBackgroundColorFunc != null) {
                var color = _self.RowBackgroundColorFunc(row.RowObject, row.RowIndex);

                if (!FastGridInternalUtil.SameColor(color, FastGridInternalUtil.ControlBackground(row.RowContentChild)))
                    FastGridInternalUtil.SetControlBackground(row.RowContentChild, color);
            }
        }

        private void DrawHeader(ItemsControl headerCtrl, double y, int indentLevel) {
            FastGridInternalUtil.SetLeft(headerCtrl, -_self.HorizontalOffset + indentLevel  * FastGridViewRow.WIDTH_PER_INDENT_LEVEL);
            FastGridInternalUtil.SetTop(headerCtrl, y);
            FastGridInternalUtil.SetHeight(headerCtrl, _self.RowHeight);
        }

        public void OnHorizontalOffsetChange() {
            foreach (var header in _visibleHeaders.Values) {
                var top = Canvas.GetTop(header.HeaderControl);
                DrawHeader(header.HeaderControl, top, header.IndentLevel);
            }
            FastGridInternalUtil.SetLeft(_self.MainDataHolder.HeaderControl(), -_self.HorizontalOffset);
            if (_self.MainDataHolder.NeedsColumnGroup())
                FastGridInternalUtil.SetLeft(_self.MainDataHolder.ColumnGroupControl(), -_self.HorizontalOffset);
        }

        internal void UpdateHorizontalScrollbar() {
            // just so the user can clearly see the last column, and also resize it
            var EXTRA_SIZE = 20;

            _self.horizontalScrollbar.ViewportSize = _self.canvas.Width;
            var columnsWidth = _self.MainDataHolder.Columns.Sum(c => c.IsVisible ? c.Width : 0);
            foreach (var header in _visibleHeaders.Values) {
                if (header.DataHolder.IsDisposed)
                    continue;
                var curWidth = header.DataHolder.Columns.Sum(c => c.IsVisible ? c.Width : 0) + header.IndentLevel * FastGridViewRow.WIDTH_PER_INDENT_LEVEL;
                if (columnsWidth < curWidth)
                    columnsWidth = curWidth;
            }

            foreach (var row in RowProvider.Rows.Where(row => row.IsRowVisible)) {
                var curWidth = row.Columns.Sum(c => c.IsVisible ? c.Width : 0) + row.IndentLevel * FastGridViewRow.WIDTH_PER_INDENT_LEVEL;
                if (columnsWidth < curWidth)
                    columnsWidth = curWidth;
            }


            var maxValue = Math.Max(columnsWidth + EXTRA_SIZE - _self.canvas.Width, 0);
            FastGridInternalUtil.SetMaximum(_self.horizontalScrollbar, maxValue);
            _self.UpdateScrollBarsVisibilityAndSize(showHorizontal: maxValue > 0);
            if (maxValue == 0) {
                _self.HorizontalOffset = 0;
                _self.horizontalScrollbar.Value = 0;
            }
        }



        internal bool TryUpdateUI() {
            if (Expander.IsEmpty) {
                if (_self.ShowHeaderOnNoItems) {
                    _self.RowProvider.HideAllRows();
                    UpdateHorizontalScrollbar();
                    _self.UpdateScrollBarsVisibilityAndSize(showVertical: false);
                } else 
                    FastGridInternalUtil.SetIsVisible(_self.canvas, false);
                return true;
            }

            // update horizontal and vertical scrollbar
            _self.UpdateScrollBarsVisibilityAndSize();

            FastGridInternalUtil.SetIsVisible(_self.canvas, true);
            if (!CanDraw()) {
                _waitBeforeDrawCount = 0;
                return false;
            }
            if (_waitBeforeDrawCount > 0) {
                --_waitBeforeDrawCount;
                return false;
            }

            Dictionary<int, RowInfo> newVisibleHeaders = new Dictionary<int, RowInfo>();

            _isUpdatingUI = true;
            var rowChangeCount = 0;
            try {
                Expander.OnBeforeUpdateUI();

                if (_self.IsHierarchical)
                    (_topRow, _topRowIndexWhenNotScrolling) = Expander.ComputeTopRowIndex(_topRow, _topRowIndexWhenNotScrolling);
                else 
                    _topRow = Expander.RowIndexToObject(_topRowIndexWhenNotScrolling);

                double y = _self.HeaderHeight;
                if (_self.MainDataHolder.Columns.Any(c => c.ColumnGroupName != ""))
                    // the idea : if we have a column group -> we show that too, at the same height as the header
                    // at this time, I only support ColumnGroupName for the main header, if the grid is hierarchical
                    y *= 2;

                var height = _self.canvas.Height;
                var rowIdx = _topRowIndexWhenNotScrolling;

                foreach (var row in RowProvider.Rows)
                    row.IsRowVisible = false;

                var visibleCount = 0;
                while (y < height && rowIdx < Expander.RowCount()) {
                    var ri = Expander.RowIndexToInfo(rowIdx);
                    if (ri.HeaderControl != null) {
                        DrawHeader(ri.HeaderControl, y - ri.HeaderRowIndex * _self.RowHeight, ri.IndentLevel);
                        newVisibleHeaders.Add(ri.HeaderId, ri);
                        rowIdx += ri.HeaderRowCount - ri.HeaderRowIndex;
                        visibleCount += ri.HeaderRowCount - ri.HeaderRowIndex;
                        y += _self.RowHeight * (ri.HeaderRowCount - ri.HeaderRowIndex);
                        continue;
                    }

                    var obj = ri.RowObject;
                    var row = RowProvider.TryGetRow(obj) ?? RowProvider.TryReuseRow(obj) ?? RowProvider.CreateRow(obj);
                    Debug.Assert(row.RowHeight >= 0);
                    row.RowObject = obj;
                    row.Used = true;
                    row.IsRowVisible = true;
                    row.IndentLevel = ri.IndentLevel;
                    row.RowIndex = rowIdx;
                    row.Columns = ri.DataHolder.Columns;
                    var sameContext = ReferenceEquals(row.DataContext, obj);
                    if (!sameContext) {
                        FastGridInternalUtil.SetLeft(row, -100000);
                        rowChangeCount++;
                    }

                    UpdateRowColor(row);
                    Expander.UpdateExpandRow(row);
                    FastGridInternalUtil.SetDataContext(row, obj);
                    FastGridInternalUtil.SetTop(row, y);
                    y += row.RowHeight;
                    row.IsSelected = _self.IsRowSelected(obj, rowIdx);
                    row.HorizontalOffset = _self.HorizontalOffset;
                    row.UpdateUI();

                    ++rowIdx;
                    ++visibleCount;
                }

                // ... update visible count just once, just in case the above is async
                _visibleCount = visibleCount;

                var headersToHide = _visibleHeaders.Keys.Except(newVisibleHeaders.Keys);
                foreach (var id in headersToHide)
                    FastGridInternalUtil.SetLeft(_visibleHeaders[id].HeaderControl, OUTSIDE_SCREEN);
                _visibleHeaders = newVisibleHeaders;

                // we do the full update only when everything is preloaded
                if (rowChangeCount == 0) {
                    RowProvider.HideInvisibleRows();
                    _self.UpdateVerticalScrollbarValue();
                    if (_self.IsHierarchical)
                        // the idea: wherever we scroll, the scrollbar size might change, for instance, at some point we could be seeing only Root rows, fully visible, 
                        // but at some point, we could see expanded rows, which are partially visible, and have horizontal scroll
                        UpdateHorizontalScrollbar();
                    Logger($"fastgrid {_self.Name} - draw {_visibleCount}");
                } else {
                    const int WAIT_BLOCK = 3;
                    const int MAX_WAIT_TICKS = 8;
                    _waitBeforeDrawCount = Math.Min((rowChangeCount + WAIT_BLOCK - 1) / WAIT_BLOCK, MAX_WAIT_TICKS);
                    Logger($"fastgrid {_self.Name} - wait {_waitBeforeDrawCount} ticks");
                }

            }
            catch (Exception e) {
                Logger($"FATAL: exception in TryUpdateUI {_self.Name} {e}");
            } finally {
                _forceUpdateUiWhileEditing = false;
                _postponedUpdateUiAction?.Invoke();
                _postponedUpdateUiAction = null;
                _isUpdatingUI = false;
            }
            return rowChangeCount == 0;
        }

        private void PreloadAhead() {
            if (_self.IsOffscreen)
                return;
            if (!_self.AllowPreload)
                return;
            if (_self.IsHierarchical)
                return; // no preload for hierarchical grids

            _isUpdatingUI = true;
            List<int> extra = new List<int>();
            for (int i = 1; i <= _self.ShowAheadExtraRows; ++i) {
                if (_topRowIndexWhenNotScrolling - i >= 0)
                    extra.Add(_topRowIndexWhenNotScrolling - i);
                if (_topRowIndexWhenNotScrolling + _visibleCount + i < Expander.RowCount())
                    extra.Add(_topRowIndexWhenNotScrolling + _visibleCount + i);
            }
            // note: dump only those that haven't already been loaded
            Logger($"fastgrid {_self.Name} - preloading ahead {extra.Count}");
            var cacheAhead =(int)Math.Round(_visibleCount * _self.CreateExtraRowsAheadPercent + _self.ShowAheadExtraRows * 2) ;
            while (RowProvider.Rows.Count < cacheAhead) {
                var row = RowProvider.CreateRow(null);
                row.Used = false;
                FastGridInternalUtil.SetLeft(row, OUTSIDE_SCREEN);
            }
            try {

                foreach (var row in RowProvider.Rows)
                    row.Preloaded = false;

                foreach (var rowIdx in extra) {
                    var obj = Expander.RowIndexToObject(rowIdx);
                    var row = RowProvider.TryGetRow(obj) ?? RowProvider.TryReuseRow(obj) ?? RowProvider.CreateRow(obj);
                    FastGridInternalUtil.SetLeft(row, OUTSIDE_SCREEN);
                    row.RowObject = obj;
                    row.RowIndex = -1; // unknown yet
                    row.Used = true;
                    row.Preloaded = true;
                    row.IsRowVisible = false;
                    FastGridInternalUtil.SetDataContext(row, obj, out _);
                    UpdateRowColor(row);
                    row.IsSelected = _self.IsRowSelected(obj, rowIdx);
                    row.HorizontalOffset = _self.HorizontalOffset;
                    row.UpdateUI();
                }
                RowProvider.HideInvisibleRows();
            } finally {
                _isUpdatingUI = false;
            }
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

        public void OnUnloaded() {
            _postponeUiTimer.Stop(); 
        }

        public void VerticalScrollToRowIndex(int rowIdx) {
            if (rowIdx < 0 || rowIdx >= Expander.RowCount())
                return; // invalid index

            if (_scrollingTopRowIndex >= 0) {
                // we're in the process of scrolling already
                // (note: while scrolling, the postponeUiTimer is already running)
                _scrollingTopRowIndex = rowIdx;
                Logger($"scroll to row {rowIdx}");
                return;
            }

            if (!_self.RowEquals(_topRow, Expander.RowIndexToObject(rowIdx)) ) {
                _scrollingTopRowIndex = rowIdx;
                Logger($"scroll to row {rowIdx}");
                PostponeUpdateUI();
            } 
        }
        public void ScrollToRow(object obj) {
            if (Expander.RowCount() < 1)
                return; // nothing to scroll to

            if (_scrollingTopRowIndex >= 0) {
                // we're in the process of scrolling already
                // (note: while scrolling, the postponeUiTimer is already running)
                _scrollingTopRowIndex = ObjectToRowIndex(obj, _scrollingTopRowIndex);
                return;
            }

            // here, we're not scrolling
            if (_self.RowEquals(_topRow, obj))
                return;

            var rowIdx = ObjectToRowIndex(obj, -1);
            if (rowIdx < 0)
                rowIdx = 0;
            _scrollingTopRowIndex = rowIdx;
            Logger($"scroll to row {rowIdx}");
            PostponeUpdateUI();
        }

        public void EnsureVisible(object obj) {
            var rowIdx = ObjectToRowIndex(obj, -1);
            if (rowIdx >= _topRowIndexWhenNotScrolling && rowIdx < _topRowIndexWhenNotScrolling + _visibleCount)
                return; // already visible
            ScrollToRow(obj);
        }

        // called ONCE on the first successful UI update
        public void PostponeUpdateUiAction(Action a) {
            _postponedUpdateUiAction = a;
        }

        internal void TryEditScroll(KeyNavigateAction action) {
            var maxRowIdx = _self.ExpandController.MaxRowIdx();
            var visibleCount = _self.GuessVisibleRowCount();
            var rowCount = _self.ExpandController.RowCount();

            switch (action) {
                case KeyNavigateAction.Up:
                    if (_self.EditRow.EditRowIdx > 0) {
                        if (_self.EditRow.EditRowIdx > _topRowIndexWhenNotScrolling) 
                            OnPostEditScroll(action);
                        else {
                            _forceUpdateUiWhileEditing = true;
                            PostponeUpdateUiAction(() => OnPostEditScroll(action));
                            _scrollingTopRowIndex = _self.EditRow.EditRowIdx - 1;
                            PostponeUpdateUI();
                        }
                    }
                    break;
                case KeyNavigateAction.Down:
                    if (_self.EditRow.EditRowIdx < rowCount - 1) {
                        if (_self.EditRow.EditRowIdx < _topRowIndexWhenNotScrolling + visibleCount - 1)
                            OnPostEditScroll(action);
                        else {
                            _forceUpdateUiWhileEditing = true;
                            PostponeUpdateUiAction(() => OnPostEditScroll(action));
                            _scrollingTopRowIndex = Math.Max(_self.EditRow.EditRowIdx - visibleCount + 2, 0) ;
                            PostponeUpdateUI();
                        }
                    }
                    break;
                case KeyNavigateAction.Prev:
                    if (_self.EditRow.EditColumnIdx > _self.EditRow.MinEditableColumnIdx()) {
                        // note: ensuring a column is visible is instant, it doesn't need any extra UpdateUI
                        var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx));
                        var col = _self.EditRow.PrevEditableColumn(_self.EditRow.EditColumn)  ;
                        EnsureVisible(col);
                        if (row != null )
                            _self.EditRow.BeginEdit(row, col, viaClick: false);
                    }
                    break;
                case KeyNavigateAction.Next:
                    if (_self.EditRow.EditColumnIdx < _self.EditRow.MaxEditableColumnIdx()) {
                        // note: ensuring a column is visible is instant, it doesn't need any extra UpdateUI
                        var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx));
                        var col = _self.EditRow.NextEditableColumn(_self.EditRow.EditColumn);
                        EnsureVisible(col);
                        if (row != null )
                            _self.EditRow.BeginEdit(row, col, viaClick: false);
                    }
                    break;
                case KeyNavigateAction.PrevOrUp:
                    // figure out -> Prev or Up
                    if (_self.EditRow.EditColumnIdx > _self.EditRow.MinEditableColumnIdx()) {
                        // note: ensuring a column is visible is instant, it doesn't need any extra UpdateUI
                        var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx));
                        var col = _self.EditRow.PrevEditableColumn(_self.EditRow.EditColumn)  ;
                        EnsureVisible(col);
                        if (row != null )
                            _self.EditRow.BeginEdit(row, col, viaClick: false);
                    } else {
                        // this is Up + last editable column
                        if (_self.EditRow.EditRowIdx > 0) {
                            EnsureVisible(_self.EditRow.MaxEditableColumn());
                            if (_self.EditRow.EditRowIdx > _topRowIndexWhenNotScrolling)
                                OnPostEditScroll(action);
                            else {
                                _forceUpdateUiWhileEditing = true;
                                PostponeUpdateUiAction(() => OnPostEditScroll(action));
                                _scrollingTopRowIndex = _self.EditRow.EditRowIdx - 1;
                                PostponeUpdateUI();
                            }
                        }
                    }
                    break;
                case KeyNavigateAction.NextOrDown:
                    // figure out -> Next or Down
                    if (_self.EditRow.EditColumnIdx < _self.EditRow.MaxEditableColumnIdx()) {
                        // note: ensuring a column is visible is instant, it doesn't need any extra UpdateUI
                        var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx));
                        var col = _self.EditRow.NextEditableColumn(_self.EditRow.EditColumn);
                        EnsureVisible(col);
                        if (row != null )
                            _self.EditRow.BeginEdit(row, col, viaClick: false);
                    } else {
                        // down + first editable column
                        if (_self.EditRow.EditRowIdx < rowCount - 1) {
                            EnsureVisible(_self.EditRow.MinEditableColumn());
                            if (_self.EditRow.EditRowIdx < _topRowIndexWhenNotScrolling + visibleCount - 1)
                                OnPostEditScroll(action);
                            else {
                                _forceUpdateUiWhileEditing = true;
                                PostponeUpdateUiAction(() => OnPostEditScroll(action));
                                _scrollingTopRowIndex = Math.Max(_self.EditRow.EditRowIdx - visibleCount + 2, 0) ;
                                PostponeUpdateUI();
                            }
                        }
                    }
                    break;

                case KeyNavigateAction.None:
                case KeyNavigateAction.Escape:
                case KeyNavigateAction.Toggle:
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        internal void EnsureVisible(FastGridViewColumn col) {
            var colLeft = _self.EditRow.ColumnLeft(col) - _self.HorizontalOffset;
            var maxWidth = _self.canvas.Width - (_self.verticalScrollbar.IsVisible ? _self.ScrollSize : 0);
            var isOk = colLeft >= 0 && colLeft + col.Width < maxWidth;
            if (!isOk) {
                if (colLeft < 0) 
                    _self.HorizontalScroll(_self.HorizontalOffset + colLeft);
                else
                    _self.HorizontalScroll(_self.HorizontalOffset + colLeft + col.Width - maxWidth);
            }
        }

        private void OnPostEditScroll(KeyNavigateAction action) {
            switch (action) {
                case KeyNavigateAction.Up: {
                    var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx - 1));
                    var col = _self.EditRow.EditColumn;
                    if (row != null )
                        _self.EditRow.BeginEdit(row, col, viaClick: false);
                    break;
                }
                case KeyNavigateAction.Down: {
                    var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx + 1));
                    var col = _self.EditRow.EditColumn;
                    if (row != null )
                        _self.EditRow.BeginEdit(row, col, viaClick: false);
                }
                    break;

                case KeyNavigateAction.PrevOrUp: {
                    var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx - 1));
                    var col = _self.EditRow.MaxEditableColumn();
                    if (row != null )
                        _self.EditRow.BeginEdit(row, col, viaClick: false);
                    break;
                }
                case KeyNavigateAction.NextOrDown: {
                    var row = RowProvider.TryGetRow(Expander.RowIndexToObject(_self.EditRow.EditRowIdx + 1));
                    var col = _self.EditRow.MinEditableColumn();
                    if (row != null )
                        _self.EditRow.BeginEdit(row, col, viaClick: false);
                    break;
                }

                case KeyNavigateAction.Prev: 
                case KeyNavigateAction.Next: 
                case KeyNavigateAction.None:
                case KeyNavigateAction.Escape:
                case KeyNavigateAction.Toggle:
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

    }
}
