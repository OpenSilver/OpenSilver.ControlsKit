# OpenSilver.ControlsKit
The ControlsKit is a collection of additional libraries, controls, and helpers that developers can use directly in their OpenSilver projects, to complement the built-in controls. 

## AdaptiveColumnsPanel

A responsive that dynamically switches between layouts based on its width:
- when its width meets or exceeds <c>NoColumnsBelowWidth</c>, children are arranged into equal-width columns (one per visible child);
- otherwise, they are stacked vertically in a single column.

Child horizontal and vertical alignments are respected within each allocated slot.

![Screenshot of the AdaptiveColumnsPanel](docs/images/AdaptiveColumnsPanel_Screenshot1.gif)

Example of usage:

```xml
<Page
    x:Class="FastControls.TestApp.Pages.TestAdaptiveColumnsPanel"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controlskit="clr-namespace:OpenSilver.ControlsKit;assembly=OpenSilver.ControlsKit.Controls" xmlns:d="http://schemas.microsoft.com/expression/blend/2008" d:DesignWidth="512" d:DesignHeight="932" xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" mc:Ignorable="d">
    <controlskit:AdaptiveColumnsPanel NoColumnsBelowWidth="400" VerticalAlignment="Top">
        <Border Height="100" CornerRadius="20" Margin="5" Background="#CC505E00">
            <TextBlock Text="Content 1" HorizontalAlignment="Center" Foreground="White" Margin="0,10,0,0"/>
        </Border>
        <Border Height="100" CornerRadius="20" Margin="5" Background="#CC7D0030">
            <TextBlock Text="Content 2" HorizontalAlignment="Center" Foreground="White" Margin="0,10,0,0"/>
        </Border>
        <Border Height="100" CornerRadius="20" Margin="5" Background="#CC00567A">
            <TextBlock Text="Content 3" HorizontalAlignment="Center" Foreground="White" Margin="0,10,0,0"/>
        </Border>
    </controlskit:AdaptiveColumnsPanel>
</Page>
```

## StaggeredPanel

A panel that arranges children in a masonry-like layout:
- It lays out children in columns of equal width but varying heights.
- It tries to balance the total height of each column, minimizing visual gaps.
- It's useful for dynamic content where each item may be a different size.

![Screenshot of the AdaptiveColumnsPanel](docs/images/StaggeredPanel_Screenshot1.png)

Example of usage:

```xml
<Page
    x:Class="FastControls.TestApp.Pages.TestStaggeredPanel"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controlskit="clr-namespace:OpenSilver.ControlsKit;assembly=OpenSilver.ControlsKit.Controls"
    xmlns:local="clr-namespace:FastGrid.FastGrid" 
    xmlns:fg="clr-namespace:FastGrid" 
    xmlns:conv="clr-namespace:System.Windows.Controls;assembly=System.Windows.Controls">
    <controlskit:StaggeredPanel DesiredColumnWidth="100" RowSpacing="20" ColumnSpacing="20">
        <Border Width="100" Height="100" Background="Red"/>
        <Border Width="100" Height="300" Background="Blue"/>
        <Border Width="100" Height="200" Background="Green"/>
        <Border Width="500" Height="300" Background="Yellow"/>
    </controlskit:StaggeredPanel>
</Page>

```