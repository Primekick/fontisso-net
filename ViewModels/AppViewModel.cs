using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Fontisso.NET.Helpers;
using Fontisso.NET.Models;
using Fontisso.NET.Services;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _fileName;

    [ObservableProperty]
    private Bitmap? _fileIcon;

    [ObservableProperty]
    private bool _hasFile;
    
    [ObservableProperty]
    private ObservableCollection<FontEntry> _fonts = [];

    [ObservableProperty]
    private FontEntry? _selectedFont;
    
    [ObservableProperty]
    private string _sampleText = "Zażółć gęślą jaźń";

    [ObservableProperty]
    private Bitmap _previewImage;

    private readonly IFontService _fontService;

    public AppViewModel(IFontService fontService)
    {
        _fontService = fontService;
        PreviewImage = BitmapConverter.CreateBlank(580, 80, Color.White);
    }
    
    partial void OnSampleTextChanged(string value)
    {
        GeneratePreviewImage();
    }
    
    partial void OnSelectedFontChanged(FontEntry value)
    {
        GeneratePreviewImage();
    }

    private void GeneratePreviewImage()
    {
        if (string.IsNullOrEmpty(SampleText) || SelectedFont is null)
            return;

        PreviewImage = _fontService.RenderTextToBitmap(SampleText, SelectedFont.Data, 12.0f, Color.Black, Color.White);
    }
}