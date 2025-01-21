using System.Drawing;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Flux;
using Fontisso.NET.Helpers;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.ViewModels;

public partial class TextPreviewViewModel : ViewModelBase, IRecipient<StoreChangedMessage<FontStoreState>>,
    IRecipient<StoreChangedMessage<TextPreviewState>>
{
    private readonly TextPreviewStore _textPreviewStore;

    private FontEntry? SelectedFont { get; set; }

    [ObservableProperty] private string _previewText = I18n.UI.Summary_SampleText;

    [ObservableProperty] private Bitmap _previewImage = BitmapConverter.CreateBlank(580, 80, Color.White);

    public TextPreviewViewModel(TextPreviewStore textPreviewStore)
    {
        _textPreviewStore = textPreviewStore;
        WeakReferenceMessenger.Default.Register<StoreChangedMessage<FontStoreState>>(this);
        WeakReferenceMessenger.Default.Register<StoreChangedMessage<TextPreviewState>>(this);
    }

    [RelayCommand]
    private async Task UpdateSampleTextImage()
    {
        if (SelectedFont is not null)
        {
            await _textPreviewStore.Dispatch(new GeneratePreviewImageAction(
                PreviewText,
                SelectedFont.Rpg2000Data,
                12.0f,
                Color.Black,
                Color.White
            ));
        }
    }

    public async Task UpdatePreviewWidth(double width)
    {
        await _textPreviewStore.Dispatch(new SetPreviewWidthAction(width));
    }

    public void Receive(StoreChangedMessage<FontStoreState> message)
    {
        SelectedFont = message.State.SelectedFont;
        UpdateSampleTextImageCommand.Execute(null);
    }

    public void Receive(StoreChangedMessage<TextPreviewState> message)
    {
        PreviewImage = message.State.PreviewImage;
    }
}