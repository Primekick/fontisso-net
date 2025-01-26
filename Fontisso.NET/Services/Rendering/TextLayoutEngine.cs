using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Encoding = System.Text.Encoding;

namespace Fontisso.NET.Services.Rendering;

public interface ITextLayoutEngine
{
    List<string> CalculateTextLayout(SharpFont.Face face, string text, int maxWidth);
    uint CalculateGlyphIndex(SharpFont.Face face, char rune);
}

public sealed class TextLayoutEngine : ITextLayoutEngine
{
    private readonly Encoding _systemEncoding;

    public TextLayoutEngine()
    {
        _systemEncoding = CodePagesEncodingProvider.Instance.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage)
                          ?? Encoding.GetEncoding(1250);
    }

    public List<string> CalculateTextLayout(SharpFont.Face face, string text, int maxWidth)
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
    public uint CalculateGlyphIndex(SharpFont.Face face, char rune) =>
        face.GetCharIndex(
            Encoding.Convert(
                Encoding.Unicode,
                _systemEncoding,
                Encoding.Unicode.GetBytes([rune])
            )[0]
        );

    private int CalculateWordWidth(SharpFont.Face face, ReadOnlySpan<char> word)
    {
        var width = 0;
        foreach (var rune in word)
        {
            face.LoadGlyph(CalculateGlyphIndex(face, rune), SharpFont.LoadFlags.Default, SharpFont.LoadTarget.Normal);
            width += (int)face.Glyph.Metrics.HorizontalAdvance;
        }

        return width;
    }

    private int CalculateSpaceWidth(SharpFont.Face face)
    {
        face.LoadGlyph(CalculateGlyphIndex(face, ' '), SharpFont.LoadFlags.Default, SharpFont.LoadTarget.Normal);
        return (int)face.Glyph.Metrics.HorizontalAdvance;
    }
}