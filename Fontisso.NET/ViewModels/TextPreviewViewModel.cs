using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fontisso.NET.Modules;

namespace Fontisso.NET.ViewModels;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

public partial class TextPreviewViewModel : ViewModelBase, IRecipient<Flux.StoreChangedMessage<Fonts.FontStoreState>>,
    IRecipient<Flux.StoreChangedMessage<Fonts.TextPreviewState>>
{
    private readonly Fonts.TextPreviewStore _textPreviewStore;

    private Fonts.FontEntry SelectedFont { get; set; }

    [ObservableProperty] private string _previewText = I18n.UI.Summary_SampleText;

    [ObservableProperty] private AvaloniaBitmap _previewImage = AvaloniaBitmap.CreateBlankAvaloniaBitmap(580, 80, Color.White);

    public TextPreviewViewModel(Fonts.TextPreviewStore textPreviewStore)
    {
        _textPreviewStore = textPreviewStore;
        WeakReferenceMessenger.Default.Register<Flux.StoreChangedMessage<Fonts.FontStoreState>>(this);
        WeakReferenceMessenger.Default.Register<Flux.StoreChangedMessage<Fonts.TextPreviewState>>(this);
    }

    [RelayCommand]
    private void UpdateSampleTextImage()
    {
        if (SelectedFont == default)
            return;
        
        _textPreviewStore.Dispatch(new Fonts.GeneratePreviewImageAction(
            Text: PreviewText,
            FontData: SelectedFont.DataRpg2000,
            TextColor: Color.Black,
            BackgroundColor: Color.White
        ));
    }

    public void UpdatePreviewWidth(double width)
    {
        _textPreviewStore.Dispatch(new Fonts.SetPreviewWidthAction(width));
    }

    public void Receive(Flux.StoreChangedMessage<Fonts.FontStoreState> message)
    {
        SelectedFont = message.State.SelectedFont;
        UpdateSampleTextImageCommand.Execute(null);
    }

    public void Receive(Flux.StoreChangedMessage<Fonts.TextPreviewState> message)
    {
        PreviewImage = message.State.PreviewImage;
    }
}