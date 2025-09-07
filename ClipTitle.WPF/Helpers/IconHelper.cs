using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClipTitle.Helpers
{
    public static class IconHelper
    {
        public static Icon? CreateIconFromResource()
        {
            try
            {
                // Try to load the ICO file if it exists
                var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ClipTitle.ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath);
                }

                // Fallback: Create icon programmatically
                return CreateDefaultIcon();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icon: {ex.Message}");
                try
                {
                    return CreateDefaultIcon();
                }
                catch
                {
                    return null;
                }
            }
        }

        private static Icon CreateDefaultIcon()
        {
            // Create a simple default icon programmatically
            using (var bitmap = new Bitmap(32, 32))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    // Draw background circle
                    g.Clear(System.Drawing.Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(37, 99, 235)))
                    {
                        g.FillEllipse(brush, 2, 2, 28, 28);
                    }
                    
                    // Draw "C" for ClipTitle
                    using (var font = new Font("Arial", 16, System.Drawing.FontStyle.Bold))
                    using (var brush = new SolidBrush(System.Drawing.Color.White))
                    {
                        var text = "C";
                        var textSize = g.MeasureString(text, font);
                        var x = (32 - textSize.Width) / 2;
                        var y = (32 - textSize.Height) / 2;
                        g.DrawString(text, font, brush, x, y);
                    }
                }

                // Convert bitmap to icon
                IntPtr hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        public static ImageSource GetImageSource()
        {
            try
            {
                // Try to load SVG or PNG as ImageSource for WPF usage
                var pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ClipTitle.png");
                if (File.Exists(pngPath))
                {
                    return new BitmapImage(new Uri(pngPath));
                }

                // Create a default image source
                return CreateDefaultImageSource();
            }
            catch
            {
                return CreateDefaultImageSource();
            }
        }

        private static ImageSource CreateDefaultImageSource()
        {
            var drawingGroup = new DrawingGroup();
            
            // Background circle
            var backgroundDrawing = new GeometryDrawing(
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)),
                new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175)), 2),
                new EllipseGeometry(new System.Windows.Point(16, 16), 14, 14));
            drawingGroup.Children.Add(backgroundDrawing);
            
            // Text
            var text = new FormattedText(
                "C",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                new SolidColorBrush(Colors.White),
                1.0);
            
            var textDrawing = new GeometryDrawing(
                new SolidColorBrush(Colors.White),
                null,
                text.BuildGeometry(new System.Windows.Point(10, 8)));
            drawingGroup.Children.Add(textDrawing);
            
            var drawingImage = new DrawingImage(drawingGroup);
            drawingImage.Freeze();
            
            return drawingImage;
        }
    }
}