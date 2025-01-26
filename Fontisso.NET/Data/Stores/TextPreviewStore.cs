using System;
using System.Drawing;
using System.Threading.Tasks;
using Fontisso.NET.Flux;
using Fontisso.NET.Helpers;
using Fontisso.NET.Services;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Data.Stores;

public record SetPreviewWidthAction(double PreviewWidth) : IAction;

public record GeneratePreviewImageAction(
    string Text,
    ReadOnlyMemory<byte> FontData,
    float FontSize,
    Color TextColor,
    Color BackgroundColor
) : IAction;

public record TextPreviewState(Bitmap PreviewImage, double PreviewWidth)
{
    public static TextPreviewState Default => new(
        BitmapConverter.CreateBlank(580, 80, Color.White),
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

    public override async Task Dispatch(IAction action)
    {
        switch (action)
        {
            case GeneratePreviewImageAction gpia:
                var previewImage = await _fontService.RenderTextToAvaloniaBitmapAsync(gpia.Text, gpia.FontData, gpia.FontSize,
                    gpia.TextColor, gpia.BackgroundColor, (int)State.PreviewWidth);
                SetState(state => state with { PreviewImage = previewImage });
                break;
            case SetPreviewWidthAction spwa:
                SetState(state => state with { PreviewWidth = spwa.PreviewWidth });
                break;
        }

        await Task.CompletedTask;
    }
}