using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FastGrid.FastGrid
{
    internal class FastGridViewRow : Canvas, INotifyPropertyChanged {

        private IReadOnlyList<FastGridViewColumn> _columns;
        private List<FastGridViewCell> _cells = new List<FastGridViewCell>();
        private bool _loaded = false;
        private double _rowHeight = 0;

        internal bool Used = false;
        internal bool Preloaded = false;
        private bool _isSelected = false;
        private Brush _selectedBrush;

        private DataTemplate _rowTemplate;
        private FastGridViewRowContent _content;
        // the only reason for this is to visually show the selection, ON TOP of the _content
        private Canvas _selection;

        private Brush _transparent = new SolidColorBrush(Colors.Transparent);
        private object _rowObject = null;

        private FrameworkElement _rowContentChild;
        private double horizontalOffset_ = 0;

        internal FastGridViewRowContent RowContent => _content;
        internal FrameworkElement RowContentChild {
            get {
                if (_rowContentChild == null) {
                    if (VisualTreeHelper.GetChildrenCount(_content) > 0) {
                        var child = VisualTreeHelper.GetChild(_content, 0) as ContentPresenter;
                        var grandChild = VisualTreeHelper.GetChild(child, 0) as FrameworkElement;
                        _rowContentChild = grandChild;
                    }
                }

                return _rowContentChild;
            }
        }

        public FastGridViewRow(DataTemplate rowTemplate, IReadOnlyList<FastGridViewColumn> columnInfo, double rowHeight) {
            CustomLayout = true;
            _columns = columnInfo;
            RowHeight = rowHeight;
            _rowTemplate = rowTemplate;
            Load();
            _loaded = true;
            BackgroundChanged();
            UpdateUI();

            // to catch the mouse anywhere
            Background = _transparent;
            SizeChanged += FastGridViewRow_SizeChanged;
        }

        private void FastGridViewRow_SizeChanged(object sender, SizeChangedEventArgs e) {
            _content.Width = e.NewSize.Width;
            _content.Height = e.NewSize.Height;
            _selection.Width = e.NewSize.Width;
            _selection.Height = e.NewSize.Height;
        }

        private void Load() {
            Height = _rowHeight;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;

            // content is added first
            _content = new FastGridViewRowContent
            {
                ContentTemplate = _rowTemplate,
                Height = _rowHeight,
            };
            Children.Add(_content);
            _selection = new Canvas();
            Children.Add(_selection);

            // create the cells
            var offset = 0d;
            foreach (var ci in _columns) {
                var cc = new FastGridViewCell {
                    ContentTemplate = ci.CellTemplate,
                    IsCellVisible = ci.IsVisible,
                    CellIndex = ci.ColumnIndex,
                    Width = ci.Width, 
                    MinWidth = ci.MinWidth,
                    MaxWidth = ci.MaxWidth,
                    Height = _rowHeight,
                };
                offset += ci.Width;
                _cells.Add(cc);
                Children.Add(cc);
            }
        }

        // the idea for keeping the object instead of the index: 
        // to easily handle insertions / deletions (in which case, the RowIndex could change for each of the rows)
        public object RowObject
        {
            get => _rowObject;
            set
            {
                // INTENTIONAL - use ReferenceEquals instead of Equals
                if (ReferenceEquals(value, _rowObject)) 
                    return;
                _rowObject = value;
                OnPropertyChanged();
            }
        }

        public bool IsRowVisible { get; set; } = false;

        public double RowHeight {
            get => _rowHeight;
            set {
                if (value.Equals(_rowHeight)) return;
                _rowHeight = value;
                OnPropertyChanged();
            }
        }

        public Brush SelectedBrush
        {
            get => _selectedBrush;
            set
            {
                if (Equals(value, _selectedBrush)) return;
                _selectedBrush = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public double HorizontalOffset {
            get => horizontalOffset_;
            set {
                if (value.Equals(horizontalOffset_)) return;
                horizontalOffset_ = value;
                OnPropertyChanged();
            }
        }

        // make sure _cells are ordered by their index
        private void SortCells() {
            var isSorted = true;
            var prevIdx = -1;
            foreach (var cell in _cells)
                if (cell.CellIndex >= prevIdx)
                    prevIdx = cell.CellIndex;
                else
                    isSorted = false;

            if (!isSorted)
                _cells = _cells.OrderBy(c => c.CellIndex).ToList();
        }

        private void UpdateCellHeight() {
            foreach (var cell in _cells) 
                FastGridUtil.SetHeight(cell, RowHeight);
        }

        internal void UpdateUI() {
            // look at what's visible and what's not, + order by index
            SortCells();

            // IMPORTANT: at this time, I assume all items are NOT complex, that is, showing one cell
            // is always fast (since at this time, I'm always showing ALL cells, but the cells that are 
            // not visible, I'm showing them offscreen)
            var x = -HorizontalOffset;
            foreach (var cell in _cells) {
                var offset = cell.IsCellVisible ? x : -100000;
                FastGridUtil.SetLeft(cell, offset);
                if (cell.IsCellVisible)
                    x += cell.Width;
            }
        }


        internal void SetCellVisible(FastGridViewColumn column, bool isVisible) {
            var idx = FastGridUtil.RefIndex(_columns, column);
            Debug.Assert(idx >= 0);
            _cells[idx].IsCellVisible = isVisible;
        }
        internal void SetCellWidth(FastGridViewColumn column, double width) {
            var idx = FastGridUtil.RefIndex(_columns, column);
            Debug.Assert(idx >= 0);
            FastGridUtil.SetWidth(_cells[idx], width);
        }
        internal void SetCellMinWidth(FastGridViewColumn column, double width) {
            var idx = FastGridUtil.RefIndex(_columns, column);
            Debug.Assert(idx >= 0);
            _cells[idx].MinWidth = width;
        }
        internal void SetCellMaxWidth(FastGridViewColumn column, double width) {
            var idx = FastGridUtil.RefIndex(_columns, column);
            Debug.Assert(idx >= 0);
            _cells[idx].MaxWidth = width;
        }
        internal void SetCellIndex(FastGridViewColumn column, int index) {
            var idx = FastGridUtil.RefIndex(_columns, column);
            Debug.Assert(idx >= 0);
            _cells[idx].CellIndex = index;
        }

        private void BackgroundChanged()
        {
            _selection.Background = IsSelected ? SelectedBrush : _transparent;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs eventArgs) {
            var view = FastGridUtil.TryGetAscendant<FastGridView>(this);
            view?.OnMouseLeftButtonDown(this, eventArgs);
        }




        private void vm_PropertyChanged(string propertyName) {
            switch (propertyName) {
            case "RowHeight":
                UpdateCellHeight();
                UpdateUI();
                break;

            case "SelectedBrush":
            case "IsSelected":
                BackgroundChanged();
                break;

            case "HorizontalOffset":
                UpdateUI();
                break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            if (_loaded)
                vm_PropertyChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
