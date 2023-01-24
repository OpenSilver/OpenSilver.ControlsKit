using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FastControls.TestApp;
using FastGrid.FastGrid;

namespace FastGrid.FastGrid
{
    public partial class TestFastGridView : Page
    {
        private ObservableCollection<Pullout> _pullouts;
        public TestFastGridView()
        {
            this.InitializeComponent();
        }

        private async Task TestSimulateScroll()
        {
            for (int i = 0; i < 200; ++i)
            {
                ctrl.VerticalScrollToRowIndex(i + 1);
                await Task.Delay(50);
            }
            for (int i = 200; i >= 0; --i)
            {
                ctrl.VerticalScrollToRowIndex(i + 1);
                await Task.Delay(50);
            }
        }

        private int RefIndex(Pullout pullout)
        {
            int idx = 0;
            _pullouts.FirstOrDefault(i =>
            {
                if (ReferenceEquals(i, pullout))
                    return true;
                ++idx;
                return false;
            });
            return idx;
        }
        private async Task TestSimulateInsertionDeletions()
        {
            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(500));
            ctrl.ItemsSource = _pullouts;
            await Task.Delay(2000);

            var rowIdx = 100;
            var newInsertions = 300;
            var delay = 50;
            var top = _pullouts[rowIdx];
            ctrl.ScrollToRow(top);
            var sel = _pullouts[rowIdx + 1];
            ctrl.SelectedItem = sel;
            await Task.Delay(1000);

            // compute rowidx + topidx when things are inserted/deleted
            // so the idea is: topidx is recomputed on each redraw

            var newPullouts = new MockViewModel().GetPulloutsByCount(newInsertions, 1000).ToList();
            for (int i = 0; i < newInsertions; ++i)
            {
                // the idea - in this test, the selection is set via an item
                var topIdx = RefIndex(top);
                // this should not affect anything visually
                _pullouts.Insert(topIdx - 10, newPullouts[i]);
                // this should insert a row, visually
                _pullouts.Insert(topIdx + 4, newPullouts[i]);
                // this should delete a row, visually
                _pullouts.RemoveAt(topIdx + 19);
                await Task.Delay(delay);
                Debug.WriteLine($"insert {i}");
            }
        }

        private async Task TestConstantUpdates()
        {
            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(500));
            ctrl.ItemsSource = _pullouts;
            await Task.Delay(2000);

            var rowIdx = 50;
            var maxRows = 25;
            ctrl.ScrollToRow(_pullouts[rowIdx]);
            var colOffset = 0;
            var operationIdx = 0;
            for (int i = 0; i < 400; ++i)
            {
                ctrl.SelectedIndex = rowIdx + (i % maxRows);
                for (int j = 0; j < 20; ++j)
                {
                    var row = _pullouts[rowIdx + ((i + j) % maxRows)];
                    switch (colOffset)
                    {
                        case 0:
                            row.OperatorReportLabel = DateTime.Now.Ticks.ToString();
                            break;
                        case 1:
                            row.OperatorRecordId++;
                            break;
                        case 2:
                            row.Username = $"user {operationIdx}";
                            break;
                        case 3:
                            row.Password = $"pass {operationIdx}";
                            break;
                        case 4:
                            row.Department = $"dep {operationIdx}";
                            break;
                        case 5:
                            row.City = $"city {operationIdx}";
                            break;
                    }

                    colOffset = (colOffset + 1) % 6;
                    ++operationIdx;
                }
                await Task.Delay(50);
            }
        }

        private async Task TestBoundBackground() {
            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(500));
            ctrl.ItemsSource = _pullouts;
            ctrl.RowTemplate = FastGridContentTemplate.BindBackgroundRowTemplate("BgColor");
            await Task.Delay(2000);

            // increment the OperatorID of each one
            for (int i = 0; i < 400; ++i) {
                foreach (var pullout in _pullouts)
                    pullout.OperatorRecordId++;
                await Task.Delay(250);
            }
        }

        private async Task TestAddAndRemoveSorted() {
            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(5, 50));
            ctrl.ItemsSource = _pullouts;

            ctrl.SortDescriptors.Add(new FastGridSortDescriptor { Column = ctrl.Columns["OperatorReportLabel"], SortDirection = SortDirection.Descending});
            await Task.Delay(3000);
            var extraPullouts = new MockViewModel().GetPulloutsByCount(50).ToList();
            for (int i = 0; i < extraPullouts.Count; ++i) {
                _pullouts.Add(extraPullouts[i]);
                await Task.Delay(250);
            }
            for (int i = 0; i < extraPullouts.Count; ++i) {
                _pullouts.Remove(extraPullouts[i]);
                await Task.Delay(250);
            }
        }

        private async Task TestResortingExistingItems() {
            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(5, 50));
            ctrl.ItemsSource = _pullouts;

            ctrl.SortDescriptors.Add(new FastGridSortDescriptor { Column = ctrl.Columns["OperatorReportLabel"], SortDirection = SortDirection.Descending});
            await Task.Delay(3000);

            for (int i = 0; i < 100; ++i) {
                _pullouts[0].OperatorReportLabel = $"Operator {i}";
                _pullouts[1].OperatorReportLabel = $"Operator {i+1}";
                await Task.Delay(250);
            }
        }

        private async Task TestRowBackgroundFunc() {
            var reverseEven = false;
            ctrl.RowBackgroundColorFunc = (o) => {
                var pullout = o as Pullout;
                var isEven = pullout.OperatorRecordId % 2 == 0;
                if (reverseEven)
                    isEven = !isEven;
                var color = isEven ? Colors.Gray : Colors.LightGray;
                return BrushCache.Inst.GetByColor(color);
            };

            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(500));
            ctrl.ItemsSource = _pullouts;

            for (int i = 0; i < 100; ++i) {
                await Task.Delay(2000);
                reverseEven = !reverseEven;
                ctrl.Redraw();
            }
        }

        private void SimpleTest() {
            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(500));
            ctrl.ItemsSource = _pullouts;
            ctrl.AllowSortByMultipleColumns = false;
            ctrl.Columns[1].Sort = true;

            ctrl.AllowMultipleSelection = true;
            ctrl.SelectionChanged += (s, a) => {
                var sel = Enumerable.OfType<Pullout>(ctrl.GetSelection());
                Console.WriteLine($"new selection {string.Join(",", sel.Select(p => p.Username))}");
            };
        }

        private async Task TestOffscreen() {
            _pullouts = new ObservableCollection<Pullout>(new MockViewModel().GetPulloutsByCount(500));
            ctrl.ItemsSource = _pullouts;
            for (int i = 0; i < 50; ++i) {
                await Task.Delay(4000);
                Canvas.SetLeft(ctrl, (i % 2) == 0 ? -10000 : 0);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) {
            SimpleTest();
            //await TestOffscreen();

            //await TestRowBackgroundFunc();

            //await TestAddAndRemoveSorted();
            //await TestResortingExistingItems();
            //await TestBoundBackground();
            //await TestSimulateScroll();
            //await TestSimulateInsertionDeletions();
            //await TestConstantUpdates();
        }

        private void ButtonViewXamlTree_Click(object sender, RoutedEventArgs e) {
            Console.WriteLine("menu 1");
        }

        private void ButtonViewCompilationLog_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("menu 2");
        }

        private void ButtonExecuteJS_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("menu 3");
        }
    }
}
