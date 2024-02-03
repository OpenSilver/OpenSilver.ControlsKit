using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FastGrid.FastGrid
{
    internal class FastGridViewRowContent : ContentControl
    {
        public FastGridViewRowContent() {
            CustomLayout = true;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            DataContextChanged += FastGridViewRow_DataContextChanged;
        }

        private void FastGridViewRow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var childCount = VisualTreeHelper.GetChildrenCount(this);
            if (childCount < 1)
                return;
            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            if (cp == null)
                return;
            cp.CustomLayout = true;
            cp.HorizontalAlignment = HorizontalAlignment.Stretch;
            cp.VerticalAlignment = VerticalAlignment.Stretch;
            cp.DataContext = DataContext;
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            var cp = VisualTreeHelper.GetChild(this, 0) as ContentPresenter;
            cp.CustomLayout = true;
            cp.HorizontalAlignment = HorizontalAlignment.Stretch;
            cp.VerticalAlignment = VerticalAlignment.Stretch;
            cp.DataContext = DataContext;
        }
    }
}
