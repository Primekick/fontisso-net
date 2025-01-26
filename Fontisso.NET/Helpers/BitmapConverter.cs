using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace Fontisso.NET.Helpers;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using GdiBitmap = System.Drawing.Bitmap;

public static class BitmapConverter
{
    [SupportedOSPlatform("windows")]
    public static AvaloniaBitmap FromGdiBitmapToAvaloniaBitmap(GdiBitmap gdiBitmap)
    {
        using var stream = new MemoryStream();
        gdiBitmap.Save(stream, ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);
        
        return new AvaloniaBitmap(stream);
    }
    
    [SupportedOSPlatform("windows")]
    public static AvaloniaBitmap CreateBlankAvaloniaBitmap(int width, int height, Color color)
    {
        var blankBitmap = new GdiBitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(blankBitmap);
        graphics.Clear(color);

        return FromGdiBitmapToAvaloniaBitmap(blankBitmap);
    }
}