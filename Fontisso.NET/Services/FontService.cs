using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Fontisso.NET.Helpers;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Fontisso.NET.Data.Models;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using SharpFont;
using Encoding = System.Text.Encoding;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Fontisso.NET.Services;

public interface IFontService
{
    Task<Bitmap> RenderTextToBitmap(string text, byte[] fontData, float fontSize, Color textColor,
        Color backgroundColor, int width);

    Task<ImmutableList<FontEntry>> LoadAvailableFonts();
}

public class FontService : IFontService
{
    private readonly Library _freetype;
    private readonly IEnumerable<Uri> _fontUris;
    private readonly Encoding _systemEncoding;

    public FontService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _freetype = new Library();
        _fontUris = AssetLoader.GetAssets(new Uri("avares://Fontisso.NET/Assets/Fonts"), null);
        _systemEncoding =
            CodePagesEncodingProvider.Instance.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage)
            ?? Encoding.GetEncoding(1250);
    }

    [SupportedOSPlatform("windows")]
    public async Task<Bitmap> RenderTextToBitmap(string text, byte[] fontData, float fontSize, Color textColor,
        Color backgroundColor, int width) =>
        await Task.Run(() =>
        {
            var face = new Face(_freetype, fontData, 0);

            var initWidth = width / 2;
            const int lineHeight = 16;
            const int padding = 4;

            var lines = CalculateTextWrapping(face, text, initWidth - 2 * padding);

            var gdiBitmap = new System.Drawing.Bitmap(initWidth, 40, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(gdiBitmap))
            {
                graphics.Clear(backgroundColor);
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.None;

                var cursorY = 24 - (lineHeight / 2) * (lines.Count - 1);

                foreach (var line in lines)
                {
                    var cursorX = padding;

                    foreach (var rune in line)
                    {
                        face.LoadGlyph(CalculateGlyphIndex(face, rune), LoadFlags.Default, LoadTarget.Normal);
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

                    cursorY += lineHeight;
                }
            }

            return BitmapConverter.FromGdiBitmapToAvaloniaBitmap(ScaleBitmap(gdiBitmap, 2.0f));
        });

    private List<string> CalculateTextWrapping(Face face, string text, int maxWidth)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).AsSpan();
        // preallocate for max expected capacity
        var lines = new List<string>(3);
        var currentLine = new StringBuilder(64);
        var currentWidth = 0;
        var fontSpaceWidth = CalculateSpaceWidth(face);

        foreach (var word in words)
        {
            var wordWidth = CalculateWordWidth(face, word);
            var isLineStarted = currentLine.Length > 0;

            var spaceWidth = isLineStarted ? fontSpaceWidth : 0;
            var totalWidth = currentWidth + spaceWidth + wordWidth;

            if (totalWidth > maxWidth && isLineStarted)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                totalWidth = 0;
            }

            if (isLineStarted)
            {
                currentLine.Append(' ');
            }

            currentLine.Append(word);
            currentWidth = totalWidth;
        }

        var lastLine = currentLine.ToString().Trim();
        if (lastLine.Length > 0)
        {
            lines.Add(lastLine);
        }

        return lines;

        int CalculateWordWidth(Face face, ReadOnlySpan<char> word)
        {
            var width = 0;
            foreach (var rune in word)
            {
                face.LoadGlyph(CalculateGlyphIndex(face, rune), LoadFlags.Default, LoadTarget.Normal);
                width += (int)face.Glyph.Metrics.HorizontalAdvance;
            }

            return width;
        }

        int CalculateSpaceWidth(Face face)
        {
            face.LoadGlyph(CalculateGlyphIndex(face, ' '), LoadFlags.Default, LoadTarget.Normal);
            return (int)face.Glyph.Metrics.HorizontalAdvance;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint CalculateGlyphIndex(Face face, char rune) =>
        face.GetCharIndex(
            Encoding.Convert(
                Encoding.Unicode,
                _systemEncoding,
                Encoding.Unicode.GetBytes([rune])
            )[0]
        );


    [SupportedOSPlatform("windows")]
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

    public async Task<ImmutableList<FontEntry>> LoadAvailableFonts() =>
        await Task.Run(() => _fontUris
            .Select(uri => AssetLoader.Open(uri).ReadToByteArray())
            .Select(data => new FontEntry(
                ExtractModuleName(data),
                ExtractAttribution(data),
                SetFaceNameByFontKind(data, FontKind.RPG2000),
                SetFaceNameByFontKind(data, FontKind.RPG2000G)
            ))
            .ToImmutableList());

    private byte[] SetFaceNameByFontKind(byte[] data, FontKind kind)
    {
        var newName = kind switch
        {
            FontKind.RPG2000 => "RPG2000",
            FontKind.RPG2000G => "RPG2000G",
            _ => throw new UnreachableException()
        };
        var newData = (byte[])data.Clone();
        var dataSpan = newData.AsSpan();

        var fontDirOffset = ExtractOffsetToResourceDirectoryEntry(dataSpan, 0x8007);
        // FONTGROUPHDR size + szFaceName offset (assuming szDeviceName is null)
        var fontDirEntryFaceNameOffset = fontDirOffset + 0x4 + 0x72;
        var targetSpan = dataSpan.Slice(fontDirEntryFaceNameOffset, newName.Length + 1);
        targetSpan.Clear();
        Encoding.ASCII.GetBytes(newName).AsSpan().CopyTo(targetSpan);

        return newData;
    }


    private string ExtractAttribution(ReadOnlySpan<byte> data) =>
        ExtractOffsetToResourceDirectoryEntry(data, 0x8008) switch
        {
            0 => "---",
            // copyright section is a static 60-char array
            var offsetToFont => Encoding.ASCII.GetString(data.Slice(offsetToFont + 0x6, 60)).Trim()
        };

    private int ExtractOffsetToResourceDirectoryEntry(ReadOnlySpan<byte> data, ushort typeId)
    {
        var resourceTableOffset = ExtractOffsetFromNeHeader(data, 0x24);
        // represents the amounts of bits to shift to the left to obtain the real resource offset later
        var shift = BitConverter.ToUInt16(data.Slice(resourceTableOffset));

        var tablePointer = resourceTableOffset + 0x2;
        var resourceTypeId = BitConverter.ToUInt16(data.Slice(tablePointer));
        while (resourceTypeId > 0)
        {
            if (resourceTypeId == typeId)
            {
                // this offset is relative to beginning of file
                return BitConverter.ToUInt16(data.Slice(tablePointer + 0x8)) << shift;
            }

            // ResourceTableEntry size + number of entries * ResourceEntry size
            tablePointer += 0x8 + BitConverter.ToUInt16(data.Slice(tablePointer + 0x2)) * 0xC;
            resourceTypeId = BitConverter.ToUInt16(data.Slice(tablePointer));
        }

        return 0;
    }

    private string ExtractModuleName(ReadOnlySpan<byte> data)
    {
        var residentNameTableOffset = ExtractOffsetFromNeHeader(data, 0x26);
        // first byte = length of the name, first entry in the table = module name, 
        var nameLength = data[residentNameTableOffset];
        return Encoding.ASCII.GetString(data.Slice(residentNameTableOffset + 0x1, nameLength));
    }

    private int ExtractOffsetFromNeHeader(ReadOnlySpan<byte> data, int offset)
    {
        var neHeaderOffset = BitConverter.ToInt32(data.Slice(0x3C));
        return neHeaderOffset + BitConverter.ToUInt16(data.Slice(neHeaderOffset + offset));
    }
}