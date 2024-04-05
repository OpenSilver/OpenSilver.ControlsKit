using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using DotNetForHtml5.Core;
using FastGrid.FastGrid.Column;
using Microsoft.Windows;

namespace FastGrid.FastGrid
{
    // data templates
    //
    // you cna also create your own, and use these as inspiration
    public static class FastGridContentTemplate
    {
        public static DataTemplate BottomLineRowTemplate(Color color) {
            var dt = FastGridUtil.CreateDataTemplate(() => {
                var border = new Border {
                    BorderThickness = new Thickness(0,0,0,1), 
                    BorderBrush = new SolidColorBrush(color),
                };
                return border;
            });
            return dt;
        }

        public static DataTemplate BindBackgroundRowTemplate(string backgroundProperty) {
            var dt = FastGridUtil.CreateDataTemplate(() => {
                var border = new Border {
                };
                border.SetBinding(Border.BackgroundProperty, new Binding(backgroundProperty));
                return border;
            });
            return dt;
        }

        public static DataTemplate DefaultRowTemplate() {
            var dt = FastGridUtil.CreateDataTemplate(() => new Canvas());
            return dt;
        }

        public static DataTemplate DefaultExpanderTemplate() {
            return DefaultExpanderTemplate(Color.FromRgb(0x7f, 0x7f, 0x7f), Color.FromRgb(0x9f, 0x9f, 0x9f));
        }

        /*
                <local:FastGridViewColumn HeaderText="" IsFilterable="False" IsSortable="False" Width="20">
                    <local:FastGridViewColumn.CellTemplate>
                        <DataTemplate>
                            <Border Width="9" Height="9" Background="Transparent" BorderBrush="#7f7f7f" BorderThickness="1" VerticalAlignment="Center" HorizontalAlignment="Center">
                                <Grid>
                                    <Path x:Name="plus" Opacity="0" Fill="#9f9f9f" Stretch="Fill" Stroke="{x:Null}" Margin="0" Width="5" Height="5"
                                          Data="M1.937,0 L2.937,0 2.937,2.0209999 5,2.0209999 5,3.0209999 2.937,3.0209999 2.937,5 1.937,5 1.937,3.0209999 0,3.0209999 0,2.0209999 1.937,2.0209999 z" />
                                    <Rectangle x:Name="minus" Fill="#9f9f9f" Stroke="{x:Null}" RadiusX="0" RadiusY="0" Margin="0" VerticalAlignment="Center" Height="1" HorizontalAlignment="Center" Width="5" Opacity="0"/>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </local:FastGridViewColumn.CellTemplate>
                </local:FastGridViewColumn>
         */
        public static DataTemplate DefaultExpanderTemplate(Color borderBgColor, Color lineColor) {
            var dt = FastGridUtil.CreateDataTemplate(() => {
                var border = new Border {
                    Width = 9, Height = 9, 
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderBrush = new SolidColorBrush(borderBgColor),
                    BorderThickness = new Thickness(1),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0,
                    Name = FastGridUtil.EXPANDER_BORDER_NAME,
                };
                var grid = new Grid();

                var geometry = TypeFromStringConverters.ConvertFromInvariantString(typeof(Geometry), "M1.937,0 L2.937,0 2.937,2.0209999 5,2.0209999 5,3.0209999 2.937,3.0209999 2.937,5 1.937,5 1.937,3.0209999 0,3.0209999 0,2.0209999 1.937,2.0209999 z") as Geometry;
                var pathPlus = new Path {
                    Data = geometry,
                    Name = "plus",
                    Opacity = 0,
                    Fill = new SolidColorBrush(lineColor),
                    Stretch = Stretch.Fill,
                    Stroke = null,
                    Margin = new Thickness(0),
                    Width = 5, Height = 5,
                };
                var rectangleMinus = new Rectangle {
                    Name = "minus",
                    Fill = new SolidColorBrush(lineColor),
                    Stroke = null,
                    RadiusX = 0, RadiusY = 0,
                    Margin = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
                    Height = 1, Width = 5,
                    Opacity = 0,
                };
                grid.Children.Add(pathPlus);
                grid.Children.Add(rectangleMinus);
                border.Child = grid;
                return border;
            });
            return dt;
        }

        private static Geometry SortGeometry() {
            var pf = new PathFigure {
                StartPoint = new Point(3,0),
                Segments = new PathSegmentCollection(new PathSegment[] {
                    new LineSegment { Point = new Point(6,4)},
                    new LineSegment { Point = new Point(0,4)},
                })
            };
            var g = new PathGeometry(new PathFigure[] { pf });
            return g;
        }

        public static Path SortPath() {
            var path = new Path {
                Data = SortGeometry(), 
                Fill = new SolidColorBrush(Color.FromArgb(255, 1, 119, 192)),
                RenderTransformOrigin = new Point(0.5, 0.5),
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.Fill, 
                Width = 9, Height = 6,
                Margin = new Thickness(0, 0, 0, 0),
                RenderTransform = new RotateTransform { Angle = 180 },
            };
            return path;
        }

        public static Grid FilterPath() {
            var geometry = TypeFromStringConverters.ConvertFromInvariantString(typeof(Geometry),
                                                                                  "M0.93340254,0 L4.934082,0 L6.934082,0 L10.93358,0 C11.996876,0 12.199773,0.75 11.668063,1.359375 L8.4335356,5.5 C8.100522,5.8975558 7.983531,6.062263 7.9429321,6.2736206 L7.934082,6.3298788 L7.934082,10.332101 C7.934082,10.332101 3.9340818,14.997499 3.9340818,14.997499 L3.9340818,6.3293748 L3.9286206,6.3012671 C3.8825667,6.1045012 3.751812,5.9296875 3.3865709,5.5 L0.24589038,1.40625 C-0.2067349,0.84375 -0.066181421,1.2241071E-16 0.93340254,0 z") as Geometry;
            var path = new Path {
                Data = geometry, 
                Margin = new Thickness(0),
                Stretch = Stretch.Fill, 
                Width = 8, Height = 12,
            };
            path.SetBinding(Path.FillProperty, new Binding("Filter.Color"));
            var grid = new Grid {
                Width = 8, 
                Background = new SolidColorBrush(Colors.Transparent),
                Cursor = Cursors.Hand,
            };
            grid.Children.Add(path);

            return grid;
        }

        public static DataTemplate DefaultHeaderTemplate() {
            return DefaultHeaderTemplate(new Thickness(5, 0, 5, 0));
        }

        public class SimpleHeaderTemplate {
            public readonly Grid Grid;
            public readonly TextBlock Text;
            // the arrow used for sorting
            public readonly Path SortPath;
            // the content control for filtering
            public readonly ContentControl Filter;
            // ... composed of a grid
            public readonly Grid FilterGrid;
            // ... that holds a path
            public readonly Path FilterPath;

            public SimpleHeaderTemplate(Grid grid, TextBlock text, Path sortPath, ContentControl filter, Grid filterGrid, Path filterPath) {
                Grid = grid;
                Text = text;
                SortPath = sortPath;
                Filter = filter;
                FilterPath = filterPath;
                FilterGrid = filterGrid;
            }
        }

        public static DataTemplate DefaultHeaderTemplate(Thickness headerMargin, Action<SimpleHeaderTemplate> updateHeader = null) {
            var dt = FastGridUtil.CreateDataTemplate(() => {
                const double MIN_WIDTH = 20;
                /*
                    <Grid Width="{Binding Width}" Visibility="{Binding IsVisible,Converter={StaticResource BooleanToVisibilityConverter}}">
                        <TextBlock Text="{Binding HeaderText}" VerticalAlignment="Center" HorizontalAlignment="Left" Margin="10 0"/>
                    </Grid>                
                 */
                var grid = new Grid {
                    Background = new SolidColorBrush(Colors.Transparent),
                    RenderTransform = null,
                };
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = headerMargin,                   
                };
                grid.SetBinding(Grid.WidthProperty, new Binding("Width"));
                grid.SetBinding(Grid.MinWidthProperty, new Binding("MinWidth"));
                grid.SetBinding(Grid.MaxWidthProperty, new Binding("MaxWidth"));
                grid.SetBinding(Grid.VisibilityProperty, new Binding("IsVisible") { Converter = new BooleanToVisibilityConverter() });
                // CanDrag -> means we can both drag and drop
                //grid.SetBinding(Grid.AllowDropProperty, new Binding("CanDrag"));
                grid.AllowDrop = true;

                tb.SetBinding(TextBlock.TextProperty, new Binding("HeaderText"));
                tb.SetBinding(TextBlock.FontSizeProperty, new Binding("HeaderFontSize"));
                tb.SetBinding(TextBlock.ForegroundProperty, new Binding("HeaderForeground"));
                tb.SetBinding(TextBlock.FontWeightProperty, new Binding("HeaderFontWeight"));
                tb.SetBinding(TextBlock.FontFamilyProperty, new Binding("HeaderFontFamily"));

                var path = SortPath();
                var filterButton = new ContentControl {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0),
                    Padding = new Thickness(2),
                    Content = FilterPath(),
                    Cursor = Cursors.Hand,
                };

                var canvas = new Canvas {
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                var rect = new Rectangle {
                    Width = 1, 
                    Fill = new SolidColorBrush(Color.FromRgb(71, 71, 71)),
                };
                Canvas.SetZIndex(rect, 1000);
                const int TRANSPARENT_WIDTH = 10;
                var transparentRect = new Rectangle {
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Width = TRANSPARENT_WIDTH, 
                    Cursor = Cursors.SizeWE,
                };
                canvas.Children.Add(rect);
                canvas.Children.Add(transparentRect);
                grid.Children.Add(tb);
                grid.Children.Add(path);
                grid.Children.Add(filterButton);
                grid.Children.Add(canvas);
                grid.DataContextChanged += (s, a) => {
                    var column = (s as FrameworkElement).DataContext as FastGridViewColumn;
                    column.PropertyChanged += (ss,aa) => Column_PropertyChanged(grid);
                    Column_PropertyChanged(grid);
                };
                grid.MouseLeftButtonUp += (s, a) => {
                    if (s is FrameworkElement fe && fe.DataContext is FastGridViewColumn sourceColumn) {
                        sourceColumn.MouseLeftDown = FastGridViewColumn.InvalidMousePos;
                        if (sourceColumn.IsDragging) {
                            fe.ReleaseMouseCapture();
                            fe.RenderTransform = null;
                            sourceColumn.IsDragging = false;
                            var destColumn = FastGridUtil. FindColumnAtPos(a.GetPosition(null));
                            if (destColumn != null && !ReferenceEquals(destColumn, sourceColumn)) {
                                FastGridView.Logger($"dragging column {sourceColumn.FriendlyName()} complete: over {destColumn.FriendlyName()}");
                                var sourceDislayIndex = sourceColumn.DisplayIndex;
                                var destDisplayIndex = destColumn.DisplayIndex;
                                int minIndex = Math.Min(sourceDislayIndex, destDisplayIndex), maxIndex = Math.Max(sourceDislayIndex, destDisplayIndex);
                                var fastGrid = FastGridUtil.ColumnToView(fe);
                                var colsToUpdate = fastGrid.Columns.Where(c => c.DisplayIndex >= minIndex && c.DisplayIndex <= maxIndex && !ReferenceEquals(c, sourceColumn)).ToList();
                                foreach (var c in colsToUpdate)
                                    c.DisplayIndex = sourceDislayIndex > destDisplayIndex ? c.DisplayIndex + 1 : c.DisplayIndex - 1;
                                sourceColumn.DisplayIndex = destDisplayIndex;
                            }
                            return;
                        }

                        var view = FastGridUtil.ColumnToView(fe);
                        if (sourceColumn.DataBindingPropertyName == "" || !sourceColumn.IsSortable || !view.CanUserSortColumns)
                            return; // we can't sort by this column

                        if (sourceColumn.IsSortNone) {
                            // none to ascending
                            sourceColumn.Sort = true; 
                        } else if (sourceColumn.IsSortAscending) {
                            //ascending to descending
                            sourceColumn.Sort = false;
                        } else {
                            // descending to none
                            sourceColumn.Sort = null; 
                        }
                    }
                };
                grid.MouseLeftButtonDown += (s, a) => {
                    if ((s as FrameworkElement).DataContext is FastGridViewColumn column) {
                        var header = VisualTreeHelper.GetParent(s as DependencyObject) as UIElement;
                        column.MouseLeftDown = a.GetPosition(header);
                    }
                };
                grid.MouseMove += (s, a) => {
                    if (s is FrameworkElement fe && fe.DataContext is FastGridViewColumn column && column.CanDrag && column.MouseLeftDown != FastGridViewColumn.InvalidMousePos) {
                        var header = VisualTreeHelper.GetParent(fe) as UIElement;
                        var curPos = a.GetPosition(header);
                        var diffX = Math.Abs(curPos.X - column.MouseLeftDown.X);
                        var diffY = Math.Abs(curPos.Y - column.MouseLeftDown.Y);
                        const int MIN_DRAG_PX = 10;
                        if ((diffX >= MIN_DRAG_PX || diffY >= MIN_DRAG_PX) && !column.IsDragging) {
                            column.IsDragging = true;
                            fe.CaptureMouse();
                            FastGridView.Logger($"dragging column {column.FriendlyName()}");
                        }

                        if (column.IsDragging) {
                            if (!(fe.RenderTransform is TranslateTransform))
                                fe.RenderTransform = new TranslateTransform();
                            if (fe.RenderTransform is TranslateTransform _translate) {
                                _translate.X = curPos.X - column.MouseLeftDown.X;
                                _translate.Y = curPos.Y - column.MouseLeftDown.Y;
                            }
                        }
                    }
                };

                grid.SizeChanged += (s, a) => {
                    rect.Height = a.NewSize.Height;
                    Canvas.SetLeft(rect, a.NewSize.Width);
                    transparentRect.Height = a.NewSize.Height;
                    Canvas.SetLeft(transparentRect, a.NewSize.Width - TRANSPARENT_WIDTH / 2);
                };

                grid.Loaded += (s, a) => {
                    var column = ((s as FrameworkElement).DataContext as FastGridViewColumn);
                    var view = FastGridUtil.ColumnToView(s as FrameworkElement);
                    if (column.DataBindingPropertyName == "" || !column.IsFilterable || !view.IsFilteringAllowed)
                        filterButton.Visibility = Visibility.Collapsed;
                    if (!column.IsSortable)
                        path.Visibility = Visibility.Collapsed;
                };

                bool mouseDown = false;
                Point initialPos = new Point(0,0);
                double initialWidth = 0;
                transparentRect.MouseLeftButtonDown += (s, a) => {
                    mouseDown = true;
                    initialPos = a.GetPosition(canvas);
                    initialWidth = ((s as FrameworkElement).DataContext as FastGridViewColumn).Width;
                    transparentRect.CaptureMouse();
                    a.Handled = true;
                    ((s as FrameworkElement).DataContext as FastGridViewColumn).IsResizingColumn = true;
                };
                transparentRect.MouseLeftButtonUp += (s, a) => {
                    mouseDown = false;
                    transparentRect.ReleaseMouseCapture();
                    a.Handled = true;
                    ((s as FrameworkElement).DataContext as FastGridViewColumn).IsResizingColumn = false;
                };
                transparentRect.MouseMove += (s, a) => {
                    if (mouseDown && (s as FrameworkElement).DataContext is FastGridViewColumn column) {
                        var curPos = a.GetPosition(canvas);
                        var newWidth = initialWidth + (curPos.X - initialPos.X);
                        if (!double.IsNaN(column.MinWidth))
                            newWidth = Math.Max(newWidth, column.MinWidth);
                        if (!double.IsNaN(column.MaxWidth))
                            newWidth = Math.Min(newWidth, column.MaxWidth);
                        newWidth = Math.Max(newWidth, MIN_WIDTH);
                        column.Width = newWidth;
                    }
                    a.Handled = true;
                };

                filterButton.MouseLeftButtonDown += (s, a) => {
                    // so that i don't trigger a "drag" of the column
                    a.Handled = true;
                };

                filterButton.MouseLeftButtonUp += (s, a) => {
                    var self = s as FrameworkElement;
                    var view = FastGridUtil.TryGetAscendant<FastGridView>(self);
                    if (view != null) {
                        var column = ((s as FrameworkElement).DataContext as FastGridViewColumn);
                        view.EditFilterMousePos = a.GetPosition(view);
                        column.IsEditingFilter = !column.IsEditingFilter;
                    }
                    a.Handled = true;
                };

                var filterGrid = filterButton.Content as Grid;
                var filterPath = filterGrid.Children.First() as Path;
                updateHeader?.Invoke(new SimpleHeaderTemplate(grid, tb, path, filterButton, filterGrid, filterPath));
                return grid;
            });
            return dt;
        }

        private static void Column_PropertyChanged(Grid grid) {
            var column = grid?.DataContext as FastGridViewColumn;
            if (grid == null || column == null)
                return;

            var path = grid.Children.OfType<Path>().FirstOrDefault();
            if (column.IsSortAscending) {
                (path.RenderTransform as RotateTransform).Angle = 0;
                path.Opacity = 1;
            } else if (column.IsSortDescending) {
                (path.RenderTransform as RotateTransform).Angle = 180;
                path.Opacity = 1;
            } else {
                path.Opacity = 0;
            }
        }

        public static DataTemplate DefaultHeaderColumnGroupTemplate() {
            return DefaultHeaderColumnGroupTemplate(new Thickness(5, 0, 5, 0));
        }
        public static DataTemplate DefaultHeaderColumnGroupTemplate(Thickness headerMargin) {
            var dt = FastGridUtil.CreateDataTemplate(() => {
                /*
                    <Grid Width="{Binding Width}" Visibility="{Binding IsVisible,Converter={StaticResource BooleanToVisibilityConverter}}">
                        <TextBlock Text="{Binding HeaderText}" VerticalAlignment="Center" HorizontalAlignment="Left" Margin="10 0"/>
                    </Grid>                
                 */
                var grid = new Grid {
                    Background = new SolidColorBrush(Colors.Transparent),
                };
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = headerMargin,
                    FontSize = 14,
                };
                grid.SetBinding(Grid.WidthProperty, new Binding("Width"));
                grid.SetBinding(Grid.BackgroundProperty, new Binding("NonEmptyGroupHeaderBackground"));
                grid.SetBinding(Grid.MarginProperty, new Binding("GroupHeaderPadding"));
                tb.SetBinding(TextBlock.TextProperty, new Binding("ColumnGroupName"));
                tb.SetBinding(TextBlock.ForegroundProperty, new Binding("GroupHeaderForeground"));

                // note: right now, I don't care about visibility, we don't need it at this time

                var canvas = new Canvas {
                    VerticalAlignment = VerticalAlignment.Stretch,
                };

                grid.Children.Add(tb);
                grid.Children.Add(canvas);

                return grid;
            });
            return dt;
        }

    }
}
