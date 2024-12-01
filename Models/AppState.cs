using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Fontisso.NET.Helpers;
using Fontisso.NET.Services;
using OneOf;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Models;

public interface IAppState
{
    TargetFileData FileData { get; }
    
    IReadOnlyList<FontEntry> Fonts { get; }
    FontEntry? SelectedFont { get; set; }
    
    string SampleText { get; set; }
    double PreviewWidth { get; set; }
    Bitmap PreviewImage { get; }

    Task ProcessFileAsync(string filePath);
    Task LoadFonts();
    Task GeneratePreviewImage();
    
    event PropertyChangingEventHandler? PropertyChanging;
    event PropertyChangedEventHandler? PropertyChanged;
}

public partial class AppState : ObservableObject, IAppState
{
    [ObservableProperty] private TargetFileData _fileData = TargetFileData.Default; 
    [ObservableProperty] private IReadOnlyList<FontEntry> _fonts = Array.Empty<FontEntry>();
    [ObservableProperty] private FontEntry? _selectedFont;
    [ObservableProperty] private string _sampleText = "Zażółć gęślą jaźń";
    [ObservableProperty] private double _previewWidth = 580;
    [ObservableProperty] private Bitmap _previewImage;
    
    private readonly IFontService _fontService;
    private readonly IResourceService _resourceService;
    
    public AppState(IFontService fontService, IResourceService resourceService)
    {
        _fontService = fontService;
        _resourceService = resourceService;
        PreviewImage = BitmapConverter.CreateBlank((int)PreviewWidth, 80, Color.White);
    }
    
    public async Task ProcessFileAsync(string filePath)
    {
        try
        {
            var targetFileData = await _resourceService.ExtractTargetFileData(filePath);
            if (targetFileData.IsT0)
            {
                FileData = targetFileData.AsT0;
            }
        }
        catch (Exception ex)
        {
            // TODO: error handling
        }
    }

    public async Task LoadFonts()
    {
        try
        {
            Fonts = await _fontService.LoadAvailableFonts();
        }
        catch (Exception ex)
        {
            // TODO: error handling
        }
    }

    public async Task GeneratePreviewImage()
    {
        if (string.IsNullOrEmpty(SampleText) || SelectedFont is null)
            return;

        try
        {
            PreviewImage = await _fontService.RenderTextToBitmap(SampleText,
                SelectedFont.Data,
                12.0f,
                Color.Black,
                Color.White,
                (int)PreviewWidth);
        }
        catch (Exception ex)
        {
            // TODO: error handling
        }
    }
}