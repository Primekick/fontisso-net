using System;
using System.Drawing;
using Fontisso.NET.Modules.Extensions;
using Fontisso.NET.Modules.Flux;
using Fontisso.NET.Services;

namespace Fontisso.NET.Data.Stores;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

public record struct SetPreviewWidthAction(double PreviewWidth) : IAction;

public record struct GeneratePreviewImageAction(
    string Text,
    ReadOnlyMemory<byte> FontData,
    float FontSize,
    Color TextColor,
    Color BackgroundColor
) : IAction;

public record struct TextPreviewState(AvaloniaBitmap PreviewImage, double PreviewWidth)
{
    public static TextPreviewState Default => new(
        AvaloniaBitmap.CreateBlankAvaloniaBitmap(580, 80, Color.White),
        580
    );
}

public class TextPreviewStore : Store<TextPreviewState>
{
    private readonly IFontService _fontService;

    public TextPreviewStore(IFontService fontService) : base(TextPreviewState.Default)
    {
        _fontService = fontService;
    }

    public override void Dispatch(IAction action)
    {
        switch (action)
        {
            case GeneratePreviewImageAction gpia:
                var previewImage = _fontService.RenderTextToAvaloniaBitmap(gpia.Text, gpia.FontData, gpia.FontSize,
                    gpia.TextColor, gpia.BackgroundColor, (int)State.PreviewWidth);
                SetState(state => state with { PreviewImage = previewImage });
                break;
            case SetPreviewWidthAction spwa:
                SetState(state => state with { PreviewWidth = spwa.PreviewWidth });
                break;
        }
    }
}