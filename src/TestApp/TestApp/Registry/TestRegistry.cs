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
                new TreeItem("TestStaggeredPanel", "TestStaggeredPanel"),
                new TreeItem("FastCheckBox", "FastCheckBox"),
                new TreeItem("AdaptiveColumnsPanel", "AdaptiveColumnsPanel"),
                new TreeItem("TestAnimatedSegmentedControl", "TestAnimatedSegmentedControl"),
                new TreeItem("FlexPanel", "FlexPanel"),
                new TreeItem("ExtendedButton", "TestExtendedButtonControl"),
                new TreeItem("ExtendedSwitch", "TestExtendedSwitch"),
                new TreeItem("ExtendedSlider", "TestExtendedSlider"),
                new TreeItem("ExtendedCheckBox", "TestExtendedCheckBox"),
            };
        }
    }
}
