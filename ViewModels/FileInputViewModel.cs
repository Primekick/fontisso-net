using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fontisso.NET.Services;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.ViewModels;

public partial class FileInputViewModel : ViewModelBase
{
    [ObservableProperty] private AppViewModel _state;
    private readonly IResourceService _resourceService;
    private readonly IFontService _fontService;

    public FileInputViewModel(AppViewModel state, IResourceService resourceService, IFontService fontService)
    {
        State = state;
        _resourceService = resourceService;
        _fontService = fontService;
    }
    
    [RelayCommand]
    private async Task SelectFile()
    {
        var fileDialog = new OpenFileDialog();
        var window = GetActiveWindow();
        var selectedFiles = await fileDialog.ShowAsync(window);

        if (selectedFiles is { Length: > 0 })
        {
            await ProcessFileAsync(selectedFiles.First());
        }
    }

    public async Task HandleDroppedFileAsync(string[] selectedFiles)
    {
        if (selectedFiles is { Length: > 0 })
        {
            await ProcessFileAsync(selectedFiles.First());
        }
    }

    private async Task ProcessFileAsync(string filePath)
    {
        State.FileName = Path.GetFileName(filePath);
        State.FileIcon = await _resourceService.ExtractIconFromFile(filePath);
        State.HasFile = true;
    }
    
    private Window GetActiveWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}