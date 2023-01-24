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

        private static Geometry SortPath() {
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
        public static DataTemplate DefaultHeaderTemplate() {
            var dt = FastGridUtil.CreateDataTemplate(() => {
                const double MIN_WIDTH = 20;
                /*
                    <Grid Width="{Binding Width}" Visibility="{Binding IsVisible,Converter={StaticResource BooleanToVisibilityConverter}}">
                        <TextBlock Text="{Binding HeaderText}" VerticalAlignment="Center" HorizontalAlignment="Left" Margin="10 0"/>
                    </Grid>                
                 */
                var grid = new Grid {
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                var tb = new TextBlock {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0),
                    FontSize = 14,
                };
                grid.SetBinding(Grid.WidthProperty, new Binding("Width"));
                grid.SetBinding(Grid.MinWidthProperty, new Binding("MinWidth"));
                grid.SetBinding(Grid.MaxWidthProperty, new Binding("MaxWidth"));
                grid.SetBinding(Grid.VisibilityProperty, new Binding("IsVisible") { Converter = new BooleanToVisibilityConverter() });

                tb.SetBinding(TextBlock.TextProperty, new Binding("HeaderText"));

                var path = new Path {
                    Data = SortPath(), 
                    Fill = new SolidColorBrush(Colors.Gray), 
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    Opacity = 0,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0),
                    Stretch = Stretch.Fill, 
                    Width = 6, Height = 4,
                    RenderTransform = new RotateTransform { Angle = 180 },
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
