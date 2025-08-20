using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Fontisso.NET.Data.Models.Rendering;
using Fontisso.NET.Helpers;

namespace Fontisso.NET.Services.Rendering;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using GdiBitmap = System.Drawing.Bitmap;

public interface IFontRenderer
{
    AvaloniaBitmap RenderTextToAvaloniaBitmap(
        string text,
        ReadOnlyMemory<byte> fontData,
        FontRenderOptions options
    );
}

public class FontRenderer(SharpFont.Library freetype, ITextLayoutEngine layout) : IFontRenderer
{
    [SupportedOSPlatform("windows")]
    public AvaloniaBitmap RenderTextToAvaloniaBitmap(string text, ReadOnlyMemory<byte> fontData,
        FontRenderOptions options)
    {
        using var face = CreateFace(fontData);

        var initWidth = options.Width / 2;
        const int lineHeight = 16;
        const int padding = 4;

        var lines = layout.CalculateTextLayout(face, text, initWidth - 2 * padding);

        var gdiBitmap = new GdiBitmap(initWidth, 40, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(gdiBitmap))
        {
            graphics.Clear(options.BackgroundColor);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.SmoothingMode = SmoothingMode.None;

            var cursorY = 24 - (lineHeight / 2) * (lines.Count - 1);

            foreach (var line in lines)
            {
                var cursorX = padding;

                foreach (var rune in line)
                {
                    face.LoadGlyph(layout.CalculateGlyphIndex(face, rune), SharpFont.LoadFlags.Default, SharpFont.LoadTarget.Normal);
                    face.Glyph.RenderGlyph(SharpFont.RenderMode.Normal);

                    var ftBitmap = face.Glyph.Bitmap;
                    var bitmapRect = new Rectangle(0, 0, ftBitmap.Width, ftBitmap.Rows);
                    using (var glyphBitmap = new GdiBitmap(ftBitmap.Width, ftBitmap.Rows, PixelFormat.Format1bppIndexed))
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
                        palette.Entries[0] = Color.FromArgb(0, options.TextColor);
                        palette.Entries[1] = Color.FromArgb(255, options.TextColor);
                        glyphBitmap.Palette = palette;

                        // DrawImage can result in blurry image so we're using DrawImageUnscaled
                        var drawX = cursorX + face.Glyph.BitmapLeft;
                        var drawY = cursorY - face.Glyph.BitmapTop;
                        graphics.DrawImageUnscaled(glyphBitmap, drawX, drawY);
                    }

                    cursorX += (int)face.Glyph.Metrics.HorizontalAdvance;
                }

                cursorY += lineHeight;
            }
        }

        return gdiBitmap.Scale(2.0f).IntoAvaloniaBitmap();
    }

    private unsafe SharpFont.Face CreateFace(ReadOnlyMemory<byte> fontData) =>
        new(freetype, (IntPtr)fontData.Pin().Pointer, fontData.Length, 0);
}