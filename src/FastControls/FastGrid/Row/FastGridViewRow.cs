using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FastGrid.FastGrid.Row;

namespace FastGrid.FastGrid
{
    internal class FastGridViewRow : Canvas, INotifyPropertyChanged {
        public const int WIDTH_PER_INDENT_LEVEL = 20;

        private List<FastGridViewCell> _cells = new List<FastGridViewCell>();
        private bool _loaded = false;
        private double _rowHeight = 0;

        internal bool Used = false;
        internal bool Preloaded = false;
        private bool _isSelected = false;
        private Brush _selectedBrush;

        private DataTemplate RowTemplate => HierchicalInfo.RowTemplate;
        private FastGridViewRowContent _content;
        // the only reason for this is to visually show the selection, ON TOP of the _content
        private Canvas _selection;

        private Brush _transparent = new SolidColorBrush(Colors.Transparent);
        private object _rowObject = null;

        private FrameworkElement _rowContentChild;
        private double horizontalOffset_ = 0;
        private FastGridViewColumnCollection _columns;

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

        internal HierarchicalCollectionInfo HierchicalInfo { get; private set; }

        // the idea: we're reusing rows, since they are time consuming to create, and are far from lightweight
        // so, we may need to update the columns collection, because, for instance,
        // we may create a row for an item that has a parent. Then, we collapse the parent
        // (which will destroy its underlying data, except for the rows, which are reused)
        // when we re-expand the parent, a new Header control will be created (with a new set of columns)
        // thus, we need to update them, so for instance, on resize of a column header, we'll properly resize its corresponding cells
        public FastGridViewColumnCollection Columns {
            get => _columns;
            set {
                // IMPORTANT: reference comparison
                if (ReferenceEquals(value, _columns)) 
                    return;
                _columns = value;
                OnPropertyChanged();
            }
        }

        public FastGridViewRow(HierarchicalCollectionInfo hci, double rowHeight) {
            HierchicalInfo = hci;
            Columns = hci.Columns;
            CustomLayout = true;
            RowHeight = rowHeight;
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
                ContentTemplate = RowTemplate,
                Height = _rowHeight,
            };
            Children.Add(_content);
            _selection = new Canvas();
            Children.Add(_selection);

            // create the cells
            var offset = 0d;
            foreach (var ci in HierchicalInfo.Columns) {
                var cc = new FastGridViewCell(ci) {
                    ContentTemplate = ci.CellTemplate,
                    Width = ci.Width, 
                    MinWidth = double.IsNaN(ci.MinWidth) ? 0 : ci.MinWidth,
                    MaxWidth = double.IsNaN(ci.MaxWidth) ? double.MaxValue : ci.MaxWidth,
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

        // useful at this point for alternate row colors
        // if -1 -> we don't know yet (for instance, it's preloaded)
        public int RowIndex { get; set; } = -1;

        public int IndentLevel { get; set; } = 0;

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
            if (!IsRowVisible)
                return; // useless while we're not visible

            // look at what's visible and what's not, + order by index
            SortCells();

            // IMPORTANT: at this time, I assume all items are NOT complex, that is, showing one cell
            // is always fast (since at this time, I'm always showing ALL cells, but the cells that are 
            // not visible, I'm showing them offscreen)
            var x = -HorizontalOffset + IndentLevel * WIDTH_PER_INDENT_LEVEL;
            foreach (var cell in _cells) {
                var offset = cell.IsCellVisible ? x : -100000;
                cell.UpdateWidth();
                FastGridUtil.SetLeft(cell, offset);
                if (cell.IsCellVisible)
                    x += cell.Width;
            }
        }

        internal void UpdateExpandCell(bool canExpand, bool isExpanded) {
            _cells[0].UpdateExpandCell(canExpand, isExpanded);
        }


        private void BackgroundChanged()
        {
            _selection.Background = IsSelected ? SelectedBrush : _transparent;
        }

        private void OnColumnsChanged() {
            if (_cells.Count != Columns.Count)
                throw new Exception($"invalid set of columns for row, expected {_cells.Count} but got {Columns.Count}");

            foreach (var cell in _cells) {
                var column = Columns.FirstOrDefault(c => c.UniqueName == cell.Column.UniqueName);
                if (column == null)
                    throw new Exception($"Can't find cell column: {cell.Column.FriendlyName()}");
                cell.Column = column;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs eventArgs) {
            var view = FastGridUtil.TryGetAscendant<FastGridView>(this);
            view?.OnMouseLeftButtonDown(this, eventArgs);
        }

        protected override void OnMouseEnter(MouseEventArgs eventArgs) {
            base.OnMouseEnter(eventArgs);
            var view = FastGridUtil.TryGetAscendant<FastGridView>(this);
            view?.OnMouseRowEnter(this);
        }

        protected override void OnMouseLeave(MouseEventArgs eventArgs) {
            base.OnMouseLeave(eventArgs);
            var view = FastGridUtil.TryGetAscendant<FastGridView>(this);
            view?.OnMouseRowLeave(this);
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
            case "Columns":
                OnColumnsChanged();
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

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
