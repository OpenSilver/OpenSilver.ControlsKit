using FastGrid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FastControls.TestApp
{
    public sealed partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();

            // Enter construction logic here...

            //Window.Current.Content = new MainPage();

            //Window.Current.Content = new FastGrid.FastGrid.TestFastGridView();
            Window.Current.Content = new TestFastGridHierarchical();
        }
    }
}
