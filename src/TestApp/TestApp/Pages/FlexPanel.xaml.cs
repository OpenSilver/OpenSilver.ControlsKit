using OpenSilver.ControlsKit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace TestApp.Pages
{

    public partial class FlexPanel : Page
    {
        public FlexPanel()
        {
            this.InitializeComponent ();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        public void OnDirectionSelectionItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flexPanel == null)
                return;
            var item = ((ComboBox)sender).SelectedItem as ComboBoxItem;
            flexPanel.Orientation = (Orientation)Enum.Parse (typeof (Orientation), (string)item.Content);
        }

        public void OnSpacingTextChanged(object sender, TextChangedEventArgs e)
        {
            if (flexPanel == null)
                return;

            var tb = ((TextBox)sender);
            double value = 0.0;
            if (Double.TryParse (tb.Text, out value) == false)
                return;
            flexPanel.Spacing = value;
        }

        public void OnJustifySelectionItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flexPanel == null)
                return;
            var item = ((ComboBox)sender).SelectedItem as ComboBoxItem;
            flexPanel.JustifyContent = (JustifyContent)Enum.Parse (typeof (JustifyContent), (string)item.Content);
        }

        public void OnAlignItemsSelectionItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flexPanel == null)
                return;
            var item = ((ComboBox)sender).SelectedItem as ComboBoxItem;
            var data = (AlignItems)Enum.Parse (typeof (AlignItems), (string)item.Content);
            flexPanel.AlignItems = data;

            foreach (var children in flexPanel.Children)
            {
                if (data == AlignItems.Stretch)
                {
                    children.SetValue (WidthProperty, double.NaN);
                    children.SetValue (HeightProperty, double.NaN);
                }
                else
                {
                    children.SetValue (WidthProperty, 100.0);
                    children.SetValue (HeightProperty, 100.0);
                }
            }
        }

        public void OnAlignContentSelectionItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flexPanel == null)
                return;
            var item = ((ComboBox)sender).SelectedItem as ComboBoxItem;
            flexPanel.AlignContent = (AlignContent)Enum.Parse (typeof (AlignContent), (string)item.Content);
        }

        public void OnGrowTextChanged(object sender, TextChangedEventArgs e)
        {
            if (flexPanel == null)
                return;

            if (flexPanel.AlignItems != AlignItems.Start && flexPanel.JustifyContent != JustifyContent.Start)
                return;
            foreach (var children in flexPanel.Children)
            {
                children.SetValue (WidthProperty, double.NaN);
                children.SetValue (HeightProperty, double.NaN);
            }
            flexPanel.InvalidateMeasure ();
            var tb = ((TextBox)sender);

            string[] strings = new string[6] { "0", "0", "0", "0", "0", "0" };
            var grow = tb.Text.Split (',');
            int length = grow.Length > 6?  6: grow.Length;
            Array.Copy (grow, 0, strings, 0, length);

            int i = 0;
            foreach (var children in flexPanel.Children)
            {
                double value = 0.0;
                if (Double.TryParse (strings[i++], out value) == false)
                {

                    OpenSilver.ControlsKit.FlexPanel.SetGrow (children, 0.0);
                    continue;
                }

                OpenSilver.ControlsKit.FlexPanel.SetGrow(children, value);
            }
        }
    }
}
