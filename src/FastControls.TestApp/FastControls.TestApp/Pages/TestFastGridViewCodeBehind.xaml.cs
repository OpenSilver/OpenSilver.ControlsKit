using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using FastGrid.FastGrid;
using OpenSilver.ControlsKit.FastGrid.DataTemplate;
using OpenSilver.ControlsKit.FastGrid.Util;
using TestRadComboBox;

namespace FastControls.TestApp.Pages
{
    public partial class TestFastGridViewCodeBehind : Page
    {
        public TestFastGridViewCodeBehind()
        {
            this.InitializeComponent();
        }
        
        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private bool _isFirst = true;

        private void ToggleItemsSource() {
            if (_isFirst)
                FirstItemsSource();
            else
                SecondItemsSource();
            _isFirst = !_isFirst;
        }

        private void FirstItemsSource() {
            // IMPORTANT: first, clear anything we used to have, so that any temp/cached controls are removed from the UI tree
            ctrl.Clear();

            var list = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(500));

            ctrl.HeaderTemplate = FastGridContentTemplate.DefaultHeaderTemplate(new Thickness(5, 0, 5, 0), (h) => {
                h.SortPath.Fill = new SolidColorBrush(Colors.Black);
                h.SortPath.HorizontalAlignment = HorizontalAlignment.Right;
                h.SortPath.VerticalAlignment = VerticalAlignment.Center;
                h.SortPath.Margin = new Thickness(0, 0, 5, 0);
            });

            ctrl.Columns.Add(new FastGridViewColumn {
                    HeaderText = "City",
                    Width = 70, MinWidth = 50, IsFilterable = false, IsSortable = false,
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "City"),
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 200, MinWidth = 50,
                    HeaderText = "Operator",
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "OperatorReportLabel"),
                    // for filtering/sorting
                    DataBindingPropertyName = "OperatorReportLabel",

                    ToolTipPropertyName = "OperatorReportLabel",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 100, MinWidth = 50,
                    HeaderText = "User",
                    CellTemplate = FastGridViewCellTemplate.DefaultEdit(list[0], "Username"),
                    DataBindingPropertyName = "Username",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 100, MinWidth = 60,
                    HeaderText = "Pass",
                    CellTemplate = FastGridViewCellTemplate.DefaultEdit(list[0], "Password"),
                    DataBindingPropertyName = "Password",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 70, MinWidth = 50,
                    HeaderText = "Active",
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "IsActive"),
                    DataBindingPropertyName = "IsActive",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 70, MinWidth = 50,
                    HeaderText = "Editable BB",
                    CellTemplate = FastGridViewCellTemplate.DefaultEdit(list[0], "EditableBool"),
                    IsFilterable = false, IsSortable = false,
                }
            );
            foreach (var col in ctrl.Columns)
                col.HeaderForeground = BrushCache.Inst.GetByColor(Colors.Gray);

            ctrl.ItemsSource = list;
            ctrl.AllowSortByMultipleColumns = false;
            ctrl.Columns[1].Sort = true;
            ctrl.AllowMultipleSelection = false;
        }

        private void SecondItemsSource() {
            // IMPORTANT: first, clear anything we used to have, so that any temp/cached controls are removed from the UI tree
            ctrl.Clear();

            var list = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(100));

            ctrl.Columns.Add(new FastGridViewColumn {
                    HeaderText = "City",
                    Width = 70, MinWidth = 50, IsFilterable = false, IsSortable = false,
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "City"),
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 120, MinWidth = 50,
                    HeaderText = "Operator",
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "OperatorReportLabel"),
                    // for filtering/sorting
                    DataBindingPropertyName = "OperatorReportLabel",

                    ToolTipPropertyName = "OperatorReportLabel",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 70, MinWidth = 60,
                    HeaderText = "Pass",
                    CellTemplate = FastGridViewCellTemplate.DefaultEdit(list[0], "Password"),
                    DataBindingPropertyName = "Password",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 100, MinWidth = 50,
                    HeaderText = "User",
                    CellTemplate = FastGridViewCellTemplate.DefaultEdit(list[0], "Username"),
                    DataBindingPropertyName = "Username",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 70, MinWidth = 50,
                    HeaderText = "Active",
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "IsActive"),
                    DataBindingPropertyName = "IsActive",
                }
            );

            foreach (var col in ctrl.Columns)
                col.HeaderForeground = BrushCache.Inst.GetByColor(Colors.Gray);

            ctrl.ItemsSource = list;
            ctrl.AllowSortByMultipleColumns = false;
            ctrl.Columns[1].Sort = true;
            ctrl.AllowMultipleSelection = false;
        }

        private void EmptyItemsSource() {
            // IMPORTANT: first, clear anything we used to have, so that any temp/cached controls are removed from the UI tree
            ctrl.Clear();

            var list = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(1));

            ctrl.Columns.Add(new FastGridViewColumn {
                    HeaderText = "City",
                    Width = 70, MinWidth = 50, IsFilterable = false, IsSortable = false,
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "City"),
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 120, MinWidth = 50,
                    HeaderText = "Operator",
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "OperatorReportLabel"),
                    // for filtering/sorting
                    DataBindingPropertyName = "OperatorReportLabel",

                    ToolTipPropertyName = "OperatorReportLabel",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 70, MinWidth = 60,
                    HeaderText = "Pass",
                    CellTemplate = FastGridViewCellTemplate.DefaultEdit(list[0], "Password"),
                    DataBindingPropertyName = "Password",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 100, MinWidth = 50,
                    HeaderText = "User",
                    CellTemplate = FastGridViewCellTemplate.DefaultEdit(list[0], "Username"),
                    DataBindingPropertyName = "Username",
                }
            );
            ctrl.Columns.Add(new FastGridViewColumn {
                    Width = 70, MinWidth = 50,
                    HeaderText = "Active",
                    CellTemplate = FastGridViewCellTemplate.Default(list[0], "IsActive"),
                    DataBindingPropertyName = "IsActive",
                }
            );

            foreach (var col in ctrl.Columns)
                col.HeaderForeground = BrushCache.Inst.GetByColor(Colors.Gray);

            list.Clear();
            ctrl.ShowHeaderOnNoItems = true;
            ctrl.ItemsSource = list;
            ctrl.AllowSortByMultipleColumns = false;
            ctrl.Columns[1].Sort = true;
            ctrl.AllowMultipleSelection = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            ToggleItemsSource();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            ToggleItemsSource();
        }
    }
}
