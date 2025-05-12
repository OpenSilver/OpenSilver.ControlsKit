using System.Collections.Generic;
using System.Linq;

namespace FastControls.TestApp.Registry
{
    internal class TreeItem
    {
        public string Name { get; }

        public string FileName { get; }

        public IReadOnlyList<TreeItem> Children { get; }

        public bool IsLeaf
        {
            get => Children == null || Children.Count == 0;
        }

        public TreeItem(string name, string fileName)
        {
            Name = name;
            FileName = fileName;
        }

        public TreeItem(string name, IEnumerable<TreeItem> children)
        {
            Name = name;
            Children = children.ToList().AsReadOnly();
        }
    }
}
