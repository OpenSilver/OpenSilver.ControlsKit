using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FastGrid.FastGrid
{
    public class MenuItemSeparator : MenuItem
    {
        /*
                    <MenuItem Background="Gray" Padding="0" >
                        <MenuItem.Header >
                            <Rectangle Fill="Gray" Height="1" />
                        </MenuItem.Header>
                    </MenuItem>
         *
         */
        public MenuItemSeparator()
        {
            Background = new SolidColorBrush(Colors.Gray);
            Padding = new Thickness(0);
            Header = new Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Colors.Transparent),
            };
        }
    }
}

