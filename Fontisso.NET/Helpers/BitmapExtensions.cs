using System.IO;
using System.Runtime.Versioning;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Fontisso.NET.Helpers;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using GdiBitmap = System.Drawing.Bitmap;

public static class GdiBitmapExtensions
{
    extension(GdiBitmap original)
    {
        [SupportedOSPlatform("windows")]
        public AvaloniaBitmap IntoAvaloniaBitmap()
        {
            using var stream = new MemoryStream();
            original.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);
        
            return new AvaloniaBitmap(stream);
        }
        
        [SupportedOSPlatform("windows")]
        public GdiBitmap Scale(float scaleFactor)
        {
            var newWidth = (int)(original.Width * scaleFactor);
            var newHeight = (int)(original.Height * scaleFactor);

            var newBitmap = new GdiBitmap(newWidth, newHeight, original.PixelFormat);

            using var graphics = Graphics.FromImage(newBitmap);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            graphics.DrawImage(original,
                new Rectangle(0, 0, newWidth, newHeight),
                new Rectangle(0, 0, original.Width, original.Height),
                GraphicsUnit.Pixel);

            return newBitmap;
        }
    }
}

public static class BitmapExtensions
{
    extension(AvaloniaBitmap)
    {
        [SupportedOSPlatform("windows")]
        public static AvaloniaBitmap CreateBlankAvaloniaBitmap(int width, int height, Color color)
        {
            var blankBitmap = new GdiBitmap(width, height, PixelFormat.Format24bppRgb);
            using var graphics = Graphics.FromImage(blankBitmap);
            graphics.Clear(color);

            return blankBitmap.IntoAvaloniaBitmap();
        }   
    }
}