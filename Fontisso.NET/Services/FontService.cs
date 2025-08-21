using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using Fontisso.NET.Modules.Extensions;
using System.Text;
using Avalonia.Platform;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Models.Rendering;
using Fontisso.NET.Services.Metadata;
using Fontisso.NET.Services.Rendering;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using Encoding = System.Text.Encoding;

namespace Fontisso.NET.Services;

public interface IFontService
{
    AvaloniaBitmap RenderTextToAvaloniaBitmap(string text, ReadOnlyMemory<byte> fontData, float fontSize, Color textColor,
        Color backgroundColor, int width);

    ImmutableList<FontEntry> LoadAvailableFonts();
}

public sealed class FontService : IFontService
{
    private readonly IEnumerable<Uri> _fontUris;
    private readonly IFontRenderer _renderer;
    private readonly IFontMetadataProcessor _fontMetadata;

    public FontService(IFontRenderer renderer, IFontMetadataProcessor fontMetadata)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _fontUris = AssetLoader.GetAssets(new Uri("avares://Fontisso.NET/Assets/Fonts"), null);
        _renderer = renderer;
        _fontMetadata = fontMetadata;
    }

    public AvaloniaBitmap RenderTextToAvaloniaBitmap(string text,
        ReadOnlyMemory<byte> fontData,
        float fontSize,
        Color textColor,
        Color backgroundColor,
        int width) =>
        _renderer.RenderTextToAvaloniaBitmap(
            text,
            fontData,
            new FontRenderOptions(
                fontSize,
                textColor,
                backgroundColor,
                width
            )
        );

    public ImmutableList<FontEntry> LoadAvailableFonts() =>
        _fontUris
            .Select(uri => AssetLoader.Open(uri).ReadToByteArray())
            .Select(data => new FontEntry(
                _fontMetadata.ExtractModuleName(data),
                _fontMetadata.ExtractAttribution(data),
                _fontMetadata.SetFaceName(data, FontKind.RPG2000.AsByteSpan()).ToArray(),
                _fontMetadata.SetFaceName(data, FontKind.RPG2000G.AsByteSpan()).ToArray()
            ))
            .ToImmutableList();
}