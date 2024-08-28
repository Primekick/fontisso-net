using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fontisso.NET.ViewModels;

public partial class FileInputViewModel : ViewModelBase
{
    [ObservableProperty] private AppViewModel _state;

    public FileInputViewModel(AppViewModel state)
    {
        State = state;
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
        if (selectedFiles.Length > 0)
        {
            await ProcessFileAsync(selectedFiles.First());
        }
    }

    private async Task ProcessFileAsync(string filePath)
    {
        State.FileName = Path.GetFileName(filePath);
        State.FileIcon = await GetFileIconAsync(filePath);
        State.HasFile = true;
    }
    
    private async Task<Bitmap> GetFileIconAsync(string filePath)
    {
        // TODO: extract icon from file
        return new Bitmap(AssetLoader.Open(new Uri("avares://Fontisso.NET/Assets/avalonia-logo.ico")));
    }
    
    private Window GetActiveWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}