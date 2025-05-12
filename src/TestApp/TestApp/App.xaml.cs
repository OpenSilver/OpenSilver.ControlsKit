using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FastControls.TestApp.Pages;

namespace FastControls.TestApp
{
    public sealed partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();

            // Enter construction logic here...

            Window.Current.Content = new MainPage();
        }
    }
}
