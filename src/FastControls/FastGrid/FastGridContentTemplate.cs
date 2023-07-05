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
                Fill = new SolidColorBrush(Colors.Gray), 
                RenderTransformOrigin = new Point(0.5, 0.5),
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 6, 0, 0),
                Stretch = Stretch.Fill, 
                Width = 6, Height = 4,
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
        public static DataTemplate DefaultHeaderTemplate(Thickness headerMargin) {
            var dt = FastGridUtil.CreateDataTemplate(() => {
                const double MIN_WIDTH = 20;
                /*
                    <Grid Width="{Binding Width}" Visibility="{Binding IsVisible,Converter={StaticResource BooleanToVisibilityConverter}}">
                        <TextBlock Text="{Binding HeaderText}" VerticalAlignment="Center" HorizontalAlignment="Left" Margin="10 0"/>
                    </Grid>                
                 */
                var grid = new Grid {
                    Background = new SolidColorBrush(Colors.Transparent),
                };
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = headerMargin,
                    FontSize = 14,
                };
                grid.SetBinding(Grid.WidthProperty, new Binding("Width"));
                grid.SetBinding(Grid.MinWidthProperty, new Binding("MinWidth"));
                grid.SetBinding(Grid.MaxWidthProperty, new Binding("MaxWidth"));
                grid.SetBinding(Grid.VisibilityProperty, new Binding("IsVisible") { Converter = new BooleanToVisibilityConverter() });

                tb.SetBinding(TextBlock.TextProperty, new Binding("HeaderText"));

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
                    Fill = new SolidColorBrush(Colors.Gray),
                    Width = 1, 
                };
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
                    if ((s as FrameworkElement).DataContext is FastGridViewColumn column) {
                        if (column.DataBindingPropertyName == "" || !column.IsSortable)
                            return; // we can't sort by this column

                        if (column.IsSortNone) {
                            // none to ascending
                            column.Sort = true; 
                        } else if (column.IsSortAscending) {
                            //ascending to descending
                            column.Sort = false;
                        } else {
                            // descending to none
                            column.Sort = null; 
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
                    if (column.DataBindingPropertyName == "" || !column.IsFilterable)
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
    }
}
