using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScreenReflector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Matrix _transformToDevice;
        private bool _shown;
        private DispatcherTimer _timer;
        private int _timerTickCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_shown)
                return;
            _shown = true;
            var compositionTarget = PresentationSource.FromVisual(this)?.CompositionTarget ?? throw new Exception("Failed to get real window details");
            _transformToDevice = compositionTarget.TransformToDevice;
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, OnTimerTick, Dispatcher.CurrentDispatcher);
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private static BitmapSource ToBitmapSource(Bitmap bitmap, int pixelSizeWidth, int pixelSizeHeight)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                //var bitmapSizeOptions = BitmapSizeOptions.FromEmptyOptions();
                var bitmapSizeOptions = BitmapSizeOptions.FromWidthAndHeight(pixelSizeWidth, pixelSizeHeight);
                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, bitmapSizeOptions);
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (_timerTickCount++ % 50 != 0 && !IsActive)
                return;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var pixelPosition = (System.Windows.Point)_transformToDevice.Transform((Vector)new System.Windows.Point(Left, screenHeight - Top - Height));
            var pixelPositionX = (int)pixelPosition.X;
            var pixelPositionY = (int)pixelPosition.Y;
            var pixelSize = (System.Windows.Size)_transformToDevice.Transform((Vector)new System.Windows.Size(Width, Height));
            var pixelSizeWidth = (int)pixelSize.Width;
            var pixelSizeHeight = (int)pixelSize.Height;
            using var bitmap = new Bitmap(pixelSizeWidth, pixelSizeHeight);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(pixelPositionX, pixelPositionY, 0, 0, new System.Drawing.Size(pixelSizeWidth, pixelSizeWidth));
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            Image.Source = ToBitmapSource(bitmap, pixelSizeWidth, pixelSizeHeight);
            Trace.WriteLine($"Stopwatch {sw.ElapsedMilliseconds} {Thread.CurrentThread.ManagedThreadId}");
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
