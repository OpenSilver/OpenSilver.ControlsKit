using System.Windows;
using System.Windows.Controls;

namespace FastControls.TestApp.Pages
{
    public partial class FastCheckBoxPage : UserControl
    {
        public FastCheckBoxPage()
        {
            InitializeComponent();
        }

        public void OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Checkbox has been clicked");
        }
    }
}