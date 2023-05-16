using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using FastGrid.FastGrid.Filter;
using OpenSilver.ControlsKit.Annotations;

namespace FastGrid.FastGrid
{
    public class FastGridViewColumn : DependencyObject, INotifyPropertyChanged
    {

        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
                                                        "Width", typeof(double), typeof(FastGridViewColumn), 
                                                        new PropertyMetadata(default(double), (d,_) => OnPropertyChanged(d,"Width")));

        public double Width {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        public static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register(
                                                        "MinWidth", typeof(double), typeof(FastGridViewColumn), new PropertyMetadata(double.NaN, (d,_) => OnPropertyChanged(d,"MinWidth")));

        public double MinWidth {
            get { return (double)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        public static readonly DependencyProperty MaxWidthProperty = DependencyProperty.Register(
                                                        "MaxWidth", typeof(double), typeof(FastGridViewColumn), new PropertyMetadata(double.NaN, (d,_) => OnPropertyChanged(d,"MaxWidth")));

        public double MaxWidth {
            get { return (double)GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
                                                        "HeaderText", typeof(string), typeof(FastGridViewColumn), new PropertyMetadata("", (d,_) => OnPropertyChanged(d,"HeaderText")));
        // simple for now
        public string HeaderText {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
                                                        "IsVisible", typeof(bool), typeof(FastGridViewColumn), new PropertyMetadata(true, (d,_) => OnPropertyChanged(d,"IsVisible")));

        public bool IsVisible {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register(
                                                        "CanResize", typeof(bool), typeof(FastGridViewColumn), new PropertyMetadata(true, (d,_) => OnPropertyChanged(d,"CanResize")));

        public bool CanResize {
            get { return (bool)GetValue(CanResizeProperty); }
            set { SetValue(CanResizeProperty, value); }
        }

        // key in the collection -- allow access via string key
        public static readonly DependencyProperty UniqueNameProperty = DependencyProperty.Register(
                                                        "UniqueName", typeof(string), typeof(FastGridViewColumn), new PropertyMetadata(""));

        public string UniqueName {
            get { return (string)GetValue(UniqueNameProperty); }
            set { SetValue(UniqueNameProperty, value); }
        }

        // future:
        // this is how we're ordering the columns -> at this time, doesn't fully work 
        public static readonly DependencyProperty DisplayIndexProperty = DependencyProperty.Register(
                                                        "DisplayIndex", typeof(int), typeof(FastGridViewColumn), new PropertyMetadata(-1, (d,_) => OnPropertyChanged(d,"DisplayIndex")));

        public int DisplayIndex {
            get { return (int)GetValue(DisplayIndexProperty); }
            set { SetValue(DisplayIndexProperty, value); }
        }

        internal bool IsResizingColumn {
            get => isResizingColumn_;
            set {
                if (value == isResizingColumn_) return;
                isResizingColumn_ = value;
                OnPropertyChanged();
            }
        }

        // true -> ascending, false -> descending, null -> none
        public bool? Sort {
            get => sort_;
            set {
                if (value == sort_) return;
                sort_ = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSortAscending));
                OnPropertyChanged(nameof(IsSortDescending));
                OnPropertyChanged(nameof(IsSortNone));
                OnPropertyChanged(nameof(SortArrowAngle));
            }
        }

        public bool IsEditingFilter {
            get => isEditingFilter_;
            set {
                if (value == isEditingFilter_) return;
                isEditingFilter_ = value;
                OnPropertyChanged();
            }
        }

        public bool IsSortAscending => Sort == true;
        public bool IsSortDescending => Sort == false;
        public bool IsSortNone => Sort == null;
        public double SortArrowAngle => IsSortAscending ? 0 : 180;

        // note: not bindable at this time
        public bool IsFilterable { get; set; } = true;
        // note: not bindable at this time
        public bool IsSortable { get; set; } = true;

        public FastGridViewFilterItem Filter { get; } = new FastGridViewFilterItem();
        // allow setting equivalence for this column
        public PropertyValueCompareEquivalent FilterCompareEquivalent => Filter.CompareEquivalent;

        // the idea: this is the name of the underlying property for this column. You only need to set this
        // if you want sorting and/or filtering
        //
        // note: not bindable at this time
        public string DataBindingPropertyName { get; set; } = "";

        public string FriendlyName() => UniqueName != "" ? UniqueName : DisplayIndex.ToString();

        private static DataTemplate DefaultDataTemplate() {
            var dt = FastGridUtil.CreateDataTemplate(() => new Canvas());
            return dt;
        }

        private static void OnPropertyChanged(DependencyObject d, string propertyName) {
            (d as FastGridViewColumn).OnPropertyChanged(propertyName);
        }
        public static readonly DependencyProperty CellEditTemplateProperty =
            DependencyProperty.Register("CellEditTemplate", typeof(DataTemplate), typeof(FastGridViewColumn), 
                                        new PropertyMetadata(DefaultDataTemplate(), (d, _) => OnPropertyChanged(d, "CellEditTemplate")));
        /// <summary>
        /// Gets or sets the data template for the cell in edit mode.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Please refer to <see cref="GridViewColumn.CellEditTemplate"/> for more information on the property.
        ///     </para>
        /// </remarks>
        public DataTemplate CellEditTemplate
        {
            get
            {
                return (DataTemplate)this.GetValue(CellEditTemplateProperty);
            }
            set
            {
                this.SetValue(CellEditTemplateProperty, value);
            }
        }

        /// <summary>
        /// Identifies the CellTemplate property.
        /// </summary>
        public static readonly DependencyProperty CellTemplateProperty =
            DependencyProperty.Register("CellTemplate", typeof(DataTemplate), typeof(FastGridViewColumn), 
                                        new PropertyMetadata(DefaultDataTemplate(), (d, _) => OnPropertyChanged(d, "CellTemplate")));

        private bool? sort_ = null;
        private bool isResizingColumn_ = false;
        private bool isEditingFilter_ = false;

        /// <summary>
        /// Gets or sets the data template for the cell in view mode.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Please refer to <see cref="GridViewColumn.CellTemplate"/> for more information on the property.
        ///     </para>
        /// </remarks>
        public DataTemplate CellTemplate
        {
            get
            {
                return (DataTemplate)this.GetValue(CellTemplateProperty);
            }
            set
            {
                this.SetValue(CellTemplateProperty, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
