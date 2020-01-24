using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkiaWinForms
{
    class DesktopForm : Form
    {
        readonly Bitmap _bitmap;

        public DesktopForm(Bitmap bitmap)
        {
            _bitmap = bitmap;

            Bounds = Screen.PrimaryScreen.Bounds;
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            // Opacity = 0.50;

            Click += (o, e) => Close();
            KeyPress += (o, e) => Close();
            Paint += (o, e) =>
            {
                e.Graphics.TranslateTransform(_bitmap.Width, bitmap.Height);
                e.Graphics.ScaleTransform(-1, -1);
                e.Graphics.DrawImage(_bitmap, Point.Empty);
            };
        }
    }

    class MainForm : Form
    {
        readonly Bitmap _screenshot;
        readonly Ball _ball;

        MainForm(Bitmap screenshot, Ball ball, int framesPerSecond)
        {
            _screenshot = screenshot;
            _ball = ball;

            Bounds = Screen.PrimaryScreen.Bounds;
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;

            var skcontrol = new SKGLControl() { Dock = DockStyle.Fill };
            skcontrol.PaintSurface += Skcontrol_PaintSurface;
            skcontrol.Click += (o, e) => Close();
            skcontrol.KeyPress += (o, e) => Close();

            var timer = new Timer() { Interval = (int)Math.Round(1000.0 / framesPerSecond) };
            timer.Tick += (o, e) => { _ball.Update(); skcontrol.Invalidate(); };
            timer.Start();

            Controls.Add(skcontrol);
        }

        private void Skcontrol_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.White);

            canvas.DrawCircle(_ball.Position, _ball.Radius, new SKPaint()
            {
                IsAntialias = true,
                Color = SKColors.Green,
                Style = SKPaintStyle.Fill
            });

            canvas.DrawText($"{DateTime.Now}", 100, 100, new SKPaint()
            {
                TextSize = 64.0f,
                IsAntialias = true,
                Color = SKColors.Red,
                Style = SKPaintStyle.Fill
            });

            canvas.Flush();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData.HasFlag(Keys.Escape))
            {
                using (var desktop = new DesktopForm(_screenshot))
                    desktop.ShowDialog();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        static Bitmap CreateScreenshot()
        {
            var rect = Screen.PrimaryScreen.Bounds;
            var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
            }
            return bitmap;
        }

        [STAThread]
        static void Main()
        {
            const int FramesPerSecond = 24;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(CreateScreenshot(),
                            new Ball(Screen.PrimaryScreen.Bounds, FramesPerSecond),
                            FramesPerSecond));
        }
    }

    class Ball
    {
        readonly Rectangle _bounds;
        SKPoint _vector;
        SKPoint _position;

        public SKPoint Position => _position;
        public float Radius => 200.0f;

        public Ball(Rectangle bounds, int framesPerSecond)
        {
            _bounds = bounds;
            _position.X = _bounds.Width / 2;
            _position.Y = _bounds.Height / 2;

            const int PixelsPerSecond = 150;
            var pixelsPerFrame = (float) PixelsPerSecond / framesPerSecond;
            _vector = new SKPoint(pixelsPerFrame, pixelsPerFrame);
        }

        public void Update()
        {
            (_position.X, _vector.X) = CheckBounds(_position.X, _vector.X, _bounds.Left, _bounds.Right);
            (_position.Y, _vector.Y) = CheckBounds(_position.Y, _vector.Y, _bounds.Top, _bounds.Bottom);

            (float newValue, float newVector) CheckBounds(float value, float vector, float min, float max)
            {
                value += vector;
                if (value + Radius > max)
                {
                    value = max - Radius;
                    vector *= -1;
                }
                else if (value - Radius < min)
                {
                    value = min + Radius;
                    vector *= -1;
                }
                return (value, vector);
            }
        }
    }
}
