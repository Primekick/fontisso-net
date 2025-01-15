using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fontisso.NET.Models;

namespace Fontisso.NET.ViewModels;

public partial class FileInputViewModel : ViewModelBase
{
    [ObservableProperty]
    private IAppState _state;
    
    public FileInputViewModel(IAppState state)
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
            await State.ProcessFileAsync(selectedFiles.First());
        }
    }

    public async Task HandleDroppedFileAsync(string[] selectedFiles)
    {
        if (selectedFiles is { Length: > 0 })
        {
            await State.ProcessFileAsync(selectedFiles.First());
        }
    }
    
    private Window? GetActiveWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}