using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using FastGrid.FastGrid;
using TestRadComboBox;

namespace FastGrid
{
    public partial class TestFastGridHierarchical : UserControl
    {
        public TestFastGridHierarchical()
        {
            this.InitializeComponent();
        }

        private HierarchicalCollectionInfo ObjectInfo(object o) {
            var dummy = o as DummyData;
            if (dummy.City.Contains(" Level1 "))
                return ctrl.Hierarchical1;
            if (dummy.City.Contains(" Level2 "))
                return ctrl.Hierarchical2;
            if (dummy.City.Contains(" Level3 "))
                return ctrl.Hierarchical3;
            return ctrl.HierarchicalRoot;
        }

        private ObservableCollection<DummyData> _items;
        private void SetItemsSource() {
            var data = _items = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(20));

            var p1 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level1"));
            var p2 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(5, "Level1"));
            var p3 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(20, "Level1"));
            var p4 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(2, "Level1"));
            var p5 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(4, "Level1"));

            var p11 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));
            var p12 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));
            var p13 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));

            var p21 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));
            var p22 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));
            var p23 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));
            var p24 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));

            var p31 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));
            var p32 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level2"));

            var p231 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level3"));
            var p232 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level3"));

            var p121 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level3"));
            var p122 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level3"));
            var p123 = new ObservableCollection<DummyData>(new MockViewModel().GetDummyByCount(10, "Level3"));

            data[0].Children = p1;
            data[1].Children = p2;
            data[2].Children = p3;
            data[3].Children = p4;
            data[4].Children = p5;

            p1[0].Children = p11;
            p1[1].Children = p12;
            p1[2].Children = p13;

            p2[0].Children = p21;
            p2[1].Children = p22;
            p2[2].Children = p23;
            p2[3].Children = p24;

            p3[0].Children = p31;
            p3[1].Children = p32;

            p23[0].Children = p231;
            p23[1].Children = p232;

            p12[0].Children = p121;
            p12[1].Children = p122;
            p12[2].Children = p123;

            ctrl.ExpandFunc = (o) => {
                var item = o as DummyData;
                return item.Children.Count > 0 ? item.Children : null;
            };
            ctrl.ObjectTypeFunc = ObjectInfo;
            ctrl.ItemsSource = data;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e) {
            var delay = 1000;
            SetItemsSource();
            return;
            await Task.Delay(delay);
            ctrl.SetExpanded(_items[1], true);
            await Task.Delay(delay);
            ctrl.SetExpanded(_items[3], true);
            await Task.Delay(delay);
            ctrl.SetExpanded(_items[1].Children[0], true);

            await Task.Delay(delay);
            ctrl.SetExpanded(_items[1], false);
        }
    }
}
