using System.Collections.Generic;

namespace FastControls.TestApp.Registry
{
    internal class TestRegistry
    {
        public static readonly IReadOnlyList<TreeItem> Tests;

        static TestRegistry()
        {
            Tests = new []
            {
                new TreeItem("FastCheckBox", "FastCheckBox")
            };
        }
    }
}
