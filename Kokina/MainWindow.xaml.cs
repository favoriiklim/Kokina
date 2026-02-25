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
        //this list for leaves DO NOT DELETE AMK
        private List<Grid> leaves = new List<Grid>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PozisyonuAyarla();

            // Sol Dallar İçin
            YaprakEkle(30, 180, -70, 0.7);
            YaprakEkle(30, 130, -45, 0.9);
            YaprakEkle(76, 130, -45, 0.9);
            YaprakEkle(71, 160, 40, 0.8);
            YaprakEkle(100, 80, 30, 0.8);
            YaprakEkle(108, 110, 30, 0.8);

            // Sağ Dallar İçin
            YaprakEkle(225, 140, -30, 0.9);
            YaprakEkle(247, 100, -10, 0.9);
            YaprakEkle(265, 120, 45, 0.8);
            YaprakEkle(295, 190, 70, 0.7);
            YaprakEkle(310, 160, 60, 0.8);

            // Orta Dallar İçin
            YaprakEkle(170, 35, 0, 1.0);
            YaprakEkle(180, 55, 20, 0.8);
            YaprakEkle(158, 75, -30, 0.9);
            YaprakEkle(154, 110, -35, 0.9);
            YaprakEkle(192, 110, 45, 1.0);
            YaprakEkle(143, 130, -65, 1.0);
            YaprakEkle(192, 150, 60, 1.0);

            // Alt kısımlar
            YaprakEkle(105, 220, -50, 0.7);
            YaprakEkle(235, 220, 50, 0.7);
            YaprakEkle(192, 220, 50, 0.3);
            AnimasyonuOynat();
        }
        private void YaprakEkle(double x, double y, double aci, double boyut)
        {
            // 1. Grup Oluştur
            Grid leafGroup = new Grid();
            leafGroup.Width = 40;
            leafGroup.Height = 60;
            leafGroup.RenderTransformOrigin = new Point(0.5, 0.5);

            // 2. Yaprak Gövdesi
            Path govde = new Path();
            govde.Stroke = new SolidColorBrush(Color.FromRgb(0, 77, 0));
            govde.StrokeThickness = 1;
            // XAML'daki kaynağı kullanıyoruz
            if (TryFindResource("LeafLive") is Brush leafBrush)
                govde.Fill = leafBrush;
            else
                govde.Fill = Brushes.Green; // Kaynak bulunamazsa yeşil yap

            // Normalize edilmiş (merkeze yakın) yaprak verisi
            govde.Data = Geometry.Parse("M 20,60 C 0,40 10,20 20,0 C 30,20 40,40 20,60 Z");

            // 3. Damar
            Path damar = new Path();
            if (TryFindResource("LeafVeinBrush") is Brush veinBrush)
                damar.Stroke = veinBrush;
            else
                damar.Stroke = Brushes.LightGreen;

            damar.StrokeThickness = 1;
            damar.Data = Geometry.Parse("M 20,58 L 20,5");
            damar.Opacity = 0.5;
            leafGroup.Children.Add(govde);
            leafGroup.Children.Add(damar);

            // 4. Transform Ayarları
            TransformGroup tg = new TransformGroup();
            // Animasyon Scale'i kontrol edeceği için burada 1 veriyoruz, animasyon 0 yapacak
            tg.Children.Add(new ScaleTransform(boyut, boyut));
            tg.Children.Add(new RotateTransform(aci));
            leafGroup.RenderTransform = tg;

            // 5. Konumlandır
            Canvas.SetLeft(leafGroup, x);
            Canvas.SetTop(leafGroup, y);

            // 6. Ekrana (Canvas'a) Ekle
            // XAML'da <Canvas x:Name="LeafLayer"/> olduğundan emin ol
            LeafLayer.Children.Add(leafGroup);

            // 7. LİSTEYE EKLE (Animasyon için kritik)
            leaves.Add(leafGroup);
        }

        private void PozisyonuAyarla()
        {
            // Windows çalışma alanını al (Görev çubuğu hariç)
            var desktopWorkingArea = SystemParameters.WorkArea;

            // Widget boyutlarımız (XAML'da 380x450 belirledik)
            double widgetWidth = this.Width;
            double widgetHeight = this.Height;

            // SAATİN ÜSTÜNE KONUMLANDIRMA:
            // Right: Ekranın en sağı - widget genişliği
            this.Left = desktopWorkingArea.Right - widgetWidth + 20; // +20 sağa biraz taşırıp daha doğal durmasını sağlar

            // Bottom: Ekranın en altı - widget yüksekliği
            // +40 px aşağıya itiyorum ki kök kısmı ekranın dışından geliyormuş gibi dursun
            this.Top = desktopWorkingArea.Bottom - widgetHeight + 40;
        }
       

        private void AnimasyonuOynat()
        {
            StartBloom_NoStoryboard();
        }


        private void StartBloom_NoStoryboard()
        {
            var branches = FindName("Branches") as System.Windows.Shapes.Path;
            if (branches != null)
            {
                // ---------- 1) DALLAR (Clip reveal) ----------
                var pen = new Pen(branches.Stroke, branches.StrokeThickness);
                var b = branches.Data.GetRenderBounds(pen);

                var clip = branches.Clip as RectangleGeometry;
                if (clip == null)
                {
                    clip = new RectangleGeometry();
                    branches.Clip = clip;
                }

                // reset + stop previous anim
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

            // ---------- 2) YAPRAKLAR (Scale 0->1 sırayla) ----------
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

                    // Animasyon öncesi sıfırla
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

            // LİSTEDEKİ HER YAPRAK İÇİN ANİMASYON
            for (int i = 0; i < leaves.Count; i++)
            {
                var s = EnsureLeafScale(leaves[i]);

                // Senin orijinal zamanlama formülün
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


            // ---------- 3) MEYVELER (Dark + 3D hepsi) ----------
            var berry3d = (Brush)FindResource("BerryBrush3D");
            var darkBerry = (Brush)FindResource("DarkBerryBrush");

            var berries = FindVisualChildren<Ellipse>(MainGrid)
                .Where(e => ReferenceEquals(e.Fill, berry3d) || ReferenceEquals(e.Fill, darkBerry))
                .ToList();

            foreach (var e in berries)
            {
                // stop previous anim
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
            AnimasyonuOynat();
            // this.DragMove();
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}