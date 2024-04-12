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
using TestRadComboBox;

namespace FastControls.TestApp.Pages
{
    public partial class TestFastGridEdit : Page
    {
        private ObservableCollection<DummyData> _dummy;

        public TestFastGridEdit()
        {
            this.InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void SimpleTest() {
            //_dummy = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(500));
            _dummy = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(25));
            ctrl.ItemsSource = _dummy;
            ctrl.AllowSortByMultipleColumns = false;
            ctrl.Columns[1].Sort = true;
            ctrl.AllowMultipleSelection = true;

            ctrl.AllowMultipleSelection = true;
            ctrl.SelectionChanged += (s, a) => {
                var sel = ctrl.GetSelection().OfType<DummyData>();
                Console.WriteLine($"new selection {string.Join(",", sel.Select(p => p.Username))}");
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            SimpleTest();
        }
    }
}
