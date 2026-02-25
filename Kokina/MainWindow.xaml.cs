using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KokinaWidget
{
    public partial class MainWindow : Window
    {
        // Leaf list for animation tracking
        private List<Grid> leaves = new List<Grid>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustPosition();

            // Left Branches
            AddLeaf(30, 180, -70, 0.7);
            AddLeaf(30, 130, -45, 0.9);
            AddLeaf(76, 130, -45, 0.9);
            AddLeaf(71, 160, 40, 0.8);
            AddLeaf(100, 80, 30, 0.8);
            AddLeaf(108, 110, 30, 0.8);

            // Right Branches
            AddLeaf(225, 140, -30, 0.9);
            AddLeaf(247, 100, -10, 0.9);
            AddLeaf(265, 120, 45, 0.8);
            AddLeaf(295, 190, 70, 0.7);
            AddLeaf(310, 160, 60, 0.8);

            // Middle Branches
            AddLeaf(170, 35, 0, 1.0);
            AddLeaf(180, 55, 20, 0.8);
            AddLeaf(158, 75, -30, 0.9);
            AddLeaf(154, 110, -35, 0.9);
            AddLeaf(192, 110, 45, 1.0);
            AddLeaf(143, 130, -65, 1.0);
            AddLeaf(192, 150, 60, 1.0);

            // Lower Sections
            AddLeaf(105, 220, -50, 0.7);
            AddLeaf(235, 220, 50, 0.7);
            AddLeaf(192, 220, 50, 0.3);

            PlayAnimation();
        }

        private void AddLeaf(double x, double y, double angle, double scale)
        {
            // 1. Create Group
            Grid leafGroup = new Grid();
            leafGroup.Width = 40;
            leafGroup.Height = 60;
            leafGroup.RenderTransformOrigin = new Point(0.5, 0.5);

            // 2. Leaf Body
            Path body = new Path();
            body.Stroke = new SolidColorBrush(Color.FromRgb(0, 77, 0));
            body.StrokeThickness = 1;
            
            // Using the resource defined in XAML
            if (TryFindResource("LeafLive") is Brush leafBrush)
                body.Fill = leafBrush;
            else
                body.Fill = Brushes.Green; // Fallback to green if resource not found

            // Normalized leaf path data
            body.Data = Geometry.Parse("M 20,60 C 0,40 10,20 20,0 C 30,20 40,40 20,60 Z");

            // 3. Vein
            Path vein = new Path();
            if (TryFindResource("LeafVeinBrush") is Brush veinBrush)
                vein.Stroke = veinBrush;
            else
                vein.Stroke = Brushes.LightGreen;

            vein.StrokeThickness = 1;
            vein.Data = Geometry.Parse("M 20,58 L 20,5");
            vein.Opacity = 0.5;
            
            leafGroup.Children.Add(body);
            leafGroup.Children.Add(vein);

            // 4. Transform Settings
            TransformGroup tg = new TransformGroup();
            // Initial scale set to 'scale', animation will handle the 0 to 1 transition
            tg.Children.Add(new ScaleTransform(scale, scale));
            tg.Children.Add(new RotateTransform(angle));
            leafGroup.RenderTransform = tg;

            // 5. Position
            Canvas.SetLeft(leafGroup, x);
            Canvas.SetTop(leafGroup, y);

            // 6. Add to Canvas
            // Ensure <Canvas x:Name="LeafLayer"/> exists in XAML
            LeafLayer.Children.Add(leafGroup);

            // 7. Add to list (Critical for animation)
            leaves.Add(leafGroup);
        }

        private void AdjustPosition()
        {
            // Get desktop working area (excluding taskbar)
            var desktopWorkingArea = SystemParameters.WorkArea;

            // Widget dimensions (Defined as 380x450 in XAML)
            double widgetWidth = this.Width;
            double widgetHeight = this.Height;

            // POSITIONING ABOVE SYSTEM CLOCK:
            // Right: Right edge of screen - widget width
            this.Left = desktopWorkingArea.Right - widgetWidth + 20; // +20 slightly offsets to the right for a natural look

            // Bottom: Bottom edge of screen - widget height
            // Offset downward by 40px so the root appears to grow from outside the screen
            this.Top = desktopWorkingArea.Bottom - widgetHeight + 40;
        }

        private void PlayAnimation()
        {
            StartBloom_NoStoryboard();
        }

        private void StartBloom_NoStoryboard()
        {
            var branches = FindName("Branches") as System.Windows.Shapes.Path;
            if (branches != null)
            {
                // ---------- 1) BRANCHES (Clip reveal) ----------
                var pen = new Pen(branches.Stroke, branches.StrokeThickness);
                var b = branches.Data.GetRenderBounds(pen);

                var clip = branches.Clip as RectangleGeometry;
                if (clip == null)
                {
                    clip = new RectangleGeometry();
                    branches.Clip = clip;
                }

                // Reset and stop previous animation
                clip.BeginAnimation(RectangleGeometry.RectProperty, null);
                clip.Rect = new Rect(b.Left, b.Bottom - 1, b.Width, 1);

                var branchesAnim = new RectAnimation
                {
                    From = new Rect(b.Left, b.Bottom - 1, b.Width, 1),
                    To = new Rect(b.Left, b.Top, b.Width, b.Height),
                    Duration = TimeSpan.FromMilliseconds(700),
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                clip.BeginAnimation(RectangleGeometry.RectProperty, branchesAnim);
            }

            // ---------- 2) LEAVES (Scale 0->1 sequentially) ----------
            ScaleTransform EnsureLeafScale(UIElement leaf)
            {
                leaf.RenderTransformOrigin = new Point(0.5, 0.5);

                if (leaf.RenderTransform is ScaleTransform stOnly)
                {
                    stOnly.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    stOnly.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }

                if (leaf.RenderTransform is TransformGroup tg)
                {
                    var s = tg.Children.OfType<ScaleTransform>().FirstOrDefault();
                    if (s == null)
                    {
                        s = new ScaleTransform(1, 1);
                        tg.Children.Insert(0, s);
                    }
                    s.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    s.BeginAnimation(ScaleTransform.ScaleYProperty, null);

                    // Reset before animation
                    s.ScaleX = 0; s.ScaleY = 0;
                    return s;
                }

                if (leaf.RenderTransform is RotateTransform rt)
                {
                    var tg2 = new TransformGroup();
                    var s = new ScaleTransform(0, 0);
                    tg2.Children.Add(s);
                    tg2.Children.Add(rt);
                    leaf.RenderTransform = tg2;
                    return s;
                }

                var sOnly2 = new ScaleTransform(0, 0);
                leaf.RenderTransform = sOnly2;
                return sOnly2;
            }

            // ANIMATION FOR EACH LEAF IN LIST
            for (int i = 0; i < leaves.Count; i++)
            {
                var s = EnsureLeafScale(leaves[i]);

                // Timing logic
                var t0 = 700 + i * 120;

                var ax = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
                {
                    BeginTime = TimeSpan.FromMilliseconds(t0),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.30 }
                };
                var ay = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
                {
                    BeginTime = TimeSpan.FromMilliseconds(t0),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.30 }
                };

                s.BeginAnimation(ScaleTransform.ScaleXProperty, ax);
                s.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
            }


            // ---------- 3) BERRIES (Dark + 3D) ----------
            var berry3d = (Brush)FindResource("BerryBrush3D");
            var darkBerry = (Brush)FindResource("DarkBerryBrush");

            var berries = FindVisualChildren<Ellipse>(MainGrid)
                .Where(e => ReferenceEquals(e.Fill, berry3d) || ReferenceEquals(e.Fill, darkBerry))
                .ToList();

            foreach (var e in berries)
            {
                // Stop previous animation
                e.BeginAnimation(UIElement.OpacityProperty, null);

                e.RenderTransformOrigin = new Point(0.5, 0.5);
                e.RenderTransform = new ScaleTransform(0, 0);
                ((ScaleTransform)e.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, null);
                ((ScaleTransform)e.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, null);

                e.Opacity = 0;
            }

            double start = 1200;
            double step = 80;
            int idx = 0;

            void Pop(Ellipse e, double ms, double amp)
            {
                var s = (ScaleTransform)e.RenderTransform;

                var op = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120))
                { BeginTime = TimeSpan.FromMilliseconds(ms) };

                var bx = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                {
                    BeginTime = TimeSpan.FromMilliseconds(ms),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = amp }
                };
                var by = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                {
                    BeginTime = TimeSpan.FromMilliseconds(ms),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = amp }
                };

                e.BeginAnimation(UIElement.OpacityProperty, op);
                s.BeginAnimation(ScaleTransform.ScaleXProperty, bx);
                s.BeginAnimation(ScaleTransform.ScaleYProperty, by);
            }

            foreach (var e in berries.Where(x => ReferenceEquals(x.Fill, darkBerry)))
                Pop(e, start + (idx++) * step, 0.20);

            foreach (var e in berries.Where(x => ReferenceEquals(x.Fill, berry3d)))
                Pop(e, start + (idx++) * step, 0.35);
        }

        // Visual tree helper
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PlayAnimation();
            // this.DragMove();
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
