using FastControls.TestApp.Registry;
using System;
using System.Windows.Controls;

namespace FastControls.TestApp
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            // Enter construction logic here...
            foreach (var i in TestRegistry.Tests)
            {
                CreateTreeItem(i, MenuContainer.Items);
            }
        }

        private void CreateTreeItem(TreeItem treeItem, ItemCollection parent)
        {
            if (treeItem.IsLeaf)
            {
                TreeViewItem treeViewItem = new TreeViewItem
                {
                    Header = treeItem.Name
                };
                treeViewItem.Selected += (sender, e) => {
                    NavigateToPage(treeItem.FileName);
                };

                parent.Add(treeViewItem);
            }
            else
            {
                TreeViewItem treeViewItem = new TreeViewItem
                {
                    Header = treeItem.Name
                };

                parent.Add(treeViewItem);

                foreach (var child in treeItem.Children)
                {
                    CreateTreeItem(child, treeViewItem.Items);
                }
            }
        }

        private void NavigateToPage(string pageName)
        {
            // Navigate to the target page:
            Uri uri = new Uri(string.Format("/{0}Page", pageName), UriKind.Relative);
            ContentContainer.Source = uri;

            // Scroll to top:
            ScrollViewer1.ScrollToVerticalOffset(0d);
        }
    }
}
