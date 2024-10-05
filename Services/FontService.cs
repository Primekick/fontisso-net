using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using Fontisso.NET.Helpers;
using System.Text;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using SharpFont;
using Encoding = System.Text.Encoding;

namespace Fontisso.NET.Services;

public interface IFontService
{
    Bitmap RenderTextToBitmap(string text, byte[] fontData, float fontSize, Color textColor, Color backgroundColor);
}

public class FontService : IFontService
{
    private readonly Library _freetype;

    public FontService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _freetype = new Library();
    }

    [SuppressMessage("Interoperability", "CA1416:Walidacja zgodności z platformą")]
    public Bitmap RenderTextToBitmap(string text, byte[] fontData, float fontSize, Color textColor, Color backgroundColor)
    {
        var face = new Face(_freetype, fontData, 0);
        var stringBytes = Encoding.Unicode.GetBytes(text);

        var width = 0;
        var height = (int)(fontSize * 1.5);

        // calculate the total width of the text beforehand
        foreach (var strByte in stringBytes)
        {
            face.LoadChar(strByte, LoadFlags.Default, LoadTarget.Normal);
            width += (int)face.Glyph.Metrics.HorizontalAdvance;
        }

        var gdiBitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
        var cursorX = 0;
        var cursorY = (int)(fontSize * 0.8);
        using (var graphics = Graphics.FromImage(gdiBitmap))
        {
            graphics.Clear(backgroundColor);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            foreach (var rune in text)
            {
                // TODO: support other ANSI encodings
                var convertedRune = Encoding.Convert(
                    Encoding.Unicode,
                    Encoding.GetEncoding(1250),
                    Encoding.Unicode.GetBytes(new[]
                    {
                        rune
                    }));
                var glyphIndex = face.GetCharIndex(convertedRune[0]);
                face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
                face.Glyph.RenderGlyph(RenderMode.Normal);

                var ftBitmap = face.Glyph.Bitmap;
                var bitmapRect = new Rectangle(0, 0, ftBitmap.Width, ftBitmap.Rows);
                using (var glyphBitmap = new System.Drawing.Bitmap(ftBitmap.Width, ftBitmap.Rows, PixelFormat.Format1bppIndexed))
                {
                    var locked = glyphBitmap.LockBits(
                        bitmapRect,
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format1bppIndexed);

                    for (var row = 0; row < ftBitmap.Rows; row++) unsafe
                    {
                        Buffer.MemoryCopy(
                            (ftBitmap.Buffer + row * ftBitmap.Pitch).ToPointer(),
                            (locked.Scan0 + row * locked.Stride).ToPointer(),
                            locked.Stride,
                            locked.Stride);
                    }

                    glyphBitmap.UnlockBits(locked);
                    
                    // .fon fonts work only with mono palettes
                    var palette = glyphBitmap.Palette;
                    palette.Entries[0] = Color.FromArgb(0, textColor);
                    palette.Entries[1] = Color.FromArgb(255, textColor);
                    glyphBitmap.Palette = palette;

                    // DrawImage can result in blurry image so we're using DrawImageUnscaled
                    var drawX = cursorX + face.Glyph.BitmapLeft;
                    var drawY = cursorY - face.Glyph.BitmapTop;
                    graphics.DrawImageUnscaled(glyphBitmap, drawX, drawY);
                }

                cursorX += (int)face.Glyph.Metrics.HorizontalAdvance;
            }
        }

        return BitmapConverter.FromGdiBitmapToAvaloniaBitmap(gdiBitmap);
    }
}