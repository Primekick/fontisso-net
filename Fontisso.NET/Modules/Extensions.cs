using System;
using System.IO;
using System.Runtime.Versioning;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;

namespace Fontisso.NET.Modules.Extensions;

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

public static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReplace(this Span<byte> target, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        var index = target.IndexOf(oldValue);
        if (index >= 0)
        {
            var replacementSlice = target.Slice(index, oldValue.Length);
            newValue.CopyTo(replacementSlice);
            
            if (newValue.Length < oldValue.Length)
            {
                replacementSlice.Slice(newValue.Length).Clear();
            }

            return true;
        }

        return false;
    }
}

public static class StreamExtensions
{
    public static byte[] ReadToByteArray(this Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        
        return ms.ToArray();
    }
}