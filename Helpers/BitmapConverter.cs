using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Helpers;

public static class BitmapConverter
{
    [SuppressMessage("Interoperability", "CA1416:Walidacja zgodności z platformą")]
    public static Bitmap FromGdiBitmapToAvaloniaBitmap(System.Drawing.Bitmap gdiBitmap)
    {
        using var stream = new MemoryStream();
        gdiBitmap.Save(stream, ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);
        
        return new Bitmap(stream);
    }
    
    [SuppressMessage("Interoperability", "CA1416:Walidacja zgodności z platformą")]
    public static Bitmap CreateBlank(int width, int height, Color color)
    {
        var blankBitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(blankBitmap);
        graphics.Clear(color);

        return FromGdiBitmapToAvaloniaBitmap(blankBitmap);
    }
}