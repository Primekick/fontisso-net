using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using GdiBitmap = System.Drawing.Bitmap;

namespace Fontisso.NET.Modules;

public static class FontKindExtensions
{
    public static ReadOnlySpan<byte> AsByteSpan(this Fonts.FontKind fontKind) => fontKind switch
    {
        Fonts.FontKind.Rpg2000 => "RPG2000"u8,
        Fonts.FontKind.Rpg2000G => "RPG2000G"u8,
        _ => throw new InvalidOperationException()
    };
}

public static class Fonts
{
    static Fonts()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public enum FontKind
    {
        Rpg2000 = 100,
        Rpg2000G = 101
    }
    
    public readonly record struct FontEntry(
        string Name,
        string Attribution,
        byte[] DataRpg2000,
        byte[] DataRpg2000G
    );
    
    public record struct SetPreviewWidthAction(double PreviewWidth) : Flux.IAction;

    public record struct GeneratePreviewImageAction(
        string Text,
        ReadOnlyMemory<byte> FontData,
        Color TextColor,
        Color BackgroundColor
    ) : Flux.IAction;

    public record struct TextPreviewState(AvaloniaBitmap PreviewImage, double PreviewWidth)
    {
        public static TextPreviewState Default => new(
            AvaloniaBitmap.CreateBlankAvaloniaBitmap(580, 80, Color.White),
            580
        );
    }
    
    public record struct SeedFontsAction : Flux.IAction;

    public record struct SelectFontAction(FontEntry Font) : Flux.IAction;

    public record struct FontStoreState(ImmutableList<FontEntry> Fonts, FontEntry SelectedFont)
    {
        public static FontStoreState Default => new(ImmutableList<FontEntry>.Empty, default);
    }
    
    public class TextPreviewStore() : Flux.Store<TextPreviewState>(TextPreviewState.Default)
    {
        public override void Dispatch(Flux.IAction action)
        {
            switch (action)
            {
                case GeneratePreviewImageAction gpia:
                    var options = new RenderOptions
                    {
                        TextColor = gpia.TextColor,
                        BackgroundColor = gpia.BackgroundColor,
                        Width = (int)State.PreviewWidth,
                    };
                    SetState(state => state with { PreviewImage = RenderPreview(gpia.Text, gpia.FontData, options) });
                    break;
                case SetPreviewWidthAction spwa:
                    SetState(state => state with { PreviewWidth = spwa.PreviewWidth });
                    break;
            }
        }
    }

    public class FontStore() : Flux.Store<FontStoreState>(FontStoreState.Default)
    {
        public override void Dispatch(Flux.IAction action)
        {
            switch (action)
            {
                case SeedFontsAction:
                    SetState(state => state with { Fonts = LoadAvailableFonts() });
                    break;
                case SelectFontAction select:
                    SetState(state => state with { SelectedFont = select.Font });
                    break;
            }
        }
    }
    
    public static class Metadata
    {
        public static string ExtractModuleName(ReadOnlySpan<byte> data)
        {
            var residentNameTableOffset = ExtractOffsetFromNeHeader(data, 0x26);
            // first byte = length of the name, first entry in the table = module name
            var nameLength = data[residentNameTableOffset];
            return Encoding.ASCII.GetString(data.Slice(residentNameTableOffset + 0x1, nameLength));
        }

        public static string ExtractAttribution(ReadOnlySpan<byte> data) =>
            ExtractOffsetToResourceDirectoryEntry(data, 0x8008) switch
            {
                0 => "---",
                // copyright section is a static 60-char array
                var offset => Encoding.ASCII.GetString(data.Slice(offset + 0x6, 60)).Trim()
            };

        public static ReadOnlySpan<byte> ApplyFaceName(ReadOnlySpan<byte> data, ReadOnlySpan<byte> newName)
        {
            var newData = data.ToArray();
            var dataSpan = newData.AsSpan();
            var fontDirOffset = ExtractOffsetToResourceDirectoryEntry(dataSpan, 0x8007);
            // FONTGROUPHDR size + szFaceName offset (assuming szDeviceName is null)
            var faceNameOffset = fontDirOffset + 0x4 + 0x72;
            var targetSpan = dataSpan.Slice(faceNameOffset, newName.Length + 1);

            targetSpan.Clear();
            newName.CopyTo(targetSpan);
            return newData;
        }

        private static int ExtractOffsetToResourceDirectoryEntry(ReadOnlySpan<byte> data, ushort typeId)
        {
            var resourceTableOffset = ExtractOffsetFromNeHeader(data, 0x24);
            // represents the amounts of bits to shift to the left to obtain the real resource offset later
            var shift = BitConverter.ToUInt16(data.Slice(resourceTableOffset));
            var tablePointer = resourceTableOffset + 0x2;

            while (true)
            {
                var resourceTypeId = BitConverter.ToUInt16(data.Slice(tablePointer));
                if (resourceTypeId == 0)
                {
                    break;
                }
                
                if (resourceTypeId == typeId)
                {
                    // this offset is relative to beginning of file
                    return BitConverter.ToUInt16(data.Slice(tablePointer + 0x8)) << shift;
                }
                
                // ResourceTableEntry size + number of entries * ResourceEntry size
                tablePointer += 0x8 + BitConverter.ToUInt16(data.Slice(tablePointer + 0x2)) * 0xC;
            }

            return 0;
        }

        private static int ExtractOffsetFromNeHeader(ReadOnlySpan<byte> data, int offset)
        {
            var neHeaderOffset = BitConverter.ToInt32(data.Slice(0x3C));
            return neHeaderOffset + BitConverter.ToUInt16(data.Slice(neHeaderOffset + offset));
        }
    }
    
    private readonly record struct RenderOptions(
        int Width,
        Color TextColor,
        Color BackgroundColor
    );

    private static readonly Lazy<SharpFont.Library> Freetype =
        new(() => new SharpFont.Library(), isThreadSafe: true);
    
    private static ImmutableList<FontEntry> LoadAvailableFonts() =>
        Avalonia.Platform.AssetLoader
            .GetAssets(new Uri("avares://Fontisso.NET/Assets/Fonts"), null)
            .Select(uri => Avalonia.Platform.AssetLoader.Open(uri).ReadToByteArray())
            .Select(data => new FontEntry(
                Metadata.ExtractModuleName(data),
                Metadata.ExtractAttribution(data),
                !Metadata.ApplyFaceName(data, FontKind.Rpg2000.AsByteSpan()),
                !Metadata.ApplyFaceName(data, FontKind.Rpg2000G.AsByteSpan())
            ))
            .ToImmutableList();
    
    private static AvaloniaBitmap RenderPreview(string text, ReadOnlyMemory<byte> fontData, RenderOptions options)
    {
        using var face = CreateFace(fontData);
        
        const int lineHeight = 16;
        const int padding = 4;
        
        var initWidth = options.Width / 2;
        var gdiBitmap = new GdiBitmap(initWidth, 40, PixelFormat.Format32bppArgb);
        var lines = Layout.CalculateTextLayout(face, text, initWidth - 2 * padding);

        using (var g = Graphics.FromImage(gdiBitmap))
        {
            g.Clear(options.BackgroundColor);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.SmoothingMode = SmoothingMode.None;

            var cursorY = 24 - (lineHeight / 2) * (lines.Count - 1);
            foreach (var line in lines)
            {
                var cursorX = padding;
                foreach (var rune in line)
                {
                    face.LoadGlyph(
                        Layout.CalculateGlyphIndex(face, rune), 
                        SharpFont.LoadFlags.Default,
                        SharpFont.LoadTarget.Normal
                    );
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
                        g.DrawImageUnscaled(glyphBitmap, drawX, drawY);
                    }

                    cursorX += (int)face.Glyph.Metrics.HorizontalAdvance;
                }

                cursorY += lineHeight;
            }
        }

        return gdiBitmap.Scale(2.0f).IntoAvaloniaBitmap();

        // needs inline function without wrapping the whole method in a giant unsafe scope
        unsafe SharpFont.Face CreateFace(ReadOnlyMemory<byte> fd) => new(Freetype.Value, (IntPtr)fd.Pin().Pointer, fd.Length, 0);
    }

    private static class Layout
    {
        private static Lazy<Encoding> SystemEncoding =>
            new(() => CodePagesEncodingProvider.Instance.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage) ?? Encoding.GetEncoding(1250), isThreadSafe: true);
        
        public static List<string> CalculateTextLayout(SharpFont.Face face, string text, int maxWidth)
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
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateGlyphIndex(SharpFont.Face face, char rune) =>
            face.GetCharIndex(
                Encoding.Convert(
                    Encoding.Unicode,
                    SystemEncoding.Value,
                    Encoding.Unicode.GetBytes([rune])
                )[0]
            );
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateWordWidth(SharpFont.Face face, ReadOnlySpan<char> word)
        {
            var width = 0;
            foreach (var rune in word)
            {
                face.LoadGlyph(CalculateGlyphIndex(face, rune), SharpFont.LoadFlags.Default, SharpFont.LoadTarget.Normal);
                width += (int)face.Glyph.Metrics.HorizontalAdvance;
            }

            return width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateSpaceWidth(SharpFont.Face face)
        {
            face.LoadGlyph(CalculateGlyphIndex(face, ' '), SharpFont.LoadFlags.Default, SharpFont.LoadTarget.Normal);
            return (int)face.Glyph.Metrics.HorizontalAdvance;
        }
    }
}
