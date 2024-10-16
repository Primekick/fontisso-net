﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Fontisso.NET.Helpers;
using System.Text;
using Avalonia.Platform;
using Fontisso.NET.Models;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using SharpFont;
using Encoding = System.Text.Encoding;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Fontisso.NET.Services;

public interface IFontService
{
    Bitmap RenderTextToBitmap(string text, byte[] fontData, float fontSize, Color textColor, Color backgroundColor);
    ObservableCollection<FontEntry> LoadAvailableFonts();
}

public class FontService : IFontService
{
    private readonly Library _freetype;
    private readonly IEnumerable<Uri> _fontUris;

    public FontService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _freetype = new Library();
        _fontUris = AssetLoader.GetAssets(new Uri("avares://Fontisso.NET/Assets/Fonts"), null);
    }

    [SuppressMessage("Interoperability", "CA1416:Walidacja zgodności z platformą")]
    public Bitmap RenderTextToBitmap(string text, byte[] fontData, float fontSize, Color textColor, Color backgroundColor)
    {
        var face = new Face(_freetype, fontData, 0);

        var width = 290;
        var height = 40;

        var gdiBitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
        var cursorX = 4;
        var cursorY = 24;
        using (var graphics = Graphics.FromImage(gdiBitmap))
        {
            graphics.Clear(backgroundColor);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.SmoothingMode = SmoothingMode.None;

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

        return BitmapConverter.FromGdiBitmapToAvaloniaBitmap(ScaleBitmap(gdiBitmap, 2.0f));
    }
    
    
    [SuppressMessage("Interoperability", "CA1416:Walidacja zgodności z platformą")]
    private static System.Drawing.Bitmap ScaleBitmap(System.Drawing.Bitmap sourceBitmap, float scaleFactor)
    {
        var newWidth = (int)(sourceBitmap.Width * scaleFactor);
        var newHeight = (int)(sourceBitmap.Height * scaleFactor);

        var newBitmap = new System.Drawing.Bitmap(newWidth, newHeight, sourceBitmap.PixelFormat);

        using var graphics = Graphics.FromImage(newBitmap);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        graphics.DrawImage(sourceBitmap, 
            new Rectangle(0, 0, newWidth, newHeight),
            new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
            GraphicsUnit.Pixel);

        return newBitmap;
    }


    public ObservableCollection<FontEntry> LoadAvailableFonts()
    {
        var fontEntries = new ObservableCollection<FontEntry>();
        foreach (var uri in _fontUris)
        {
            var data = AssetLoader.Open(uri).ReadToByteArray();
            var parts = uri.Segments.TakeLast(2).ToArray();
            if (parts is not [var kind, var name]) 
                continue;

            var fontKind = kind.Replace("/", "") switch
            {
                "RPG2000" => FontKind.RPG2000,
                "RPG2000G" => FontKind.RPG2000G,
                _ => throw new UnreachableException()
            };
            fontEntries.Add(new FontEntry(
                fontKind,
                data,
                name
            ));
        }

        return fontEntries;
    }
}