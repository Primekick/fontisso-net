using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Flux;

namespace Fontisso.NET.ViewModels;

public partial class FileInputViewModel : ViewModelBase, IRecipient<StoreChangedMessage<TargetFileState>>
{
    private readonly TargetFileStore _targetFileStore;

    [ObservableProperty] private TargetFileData _fileData = TargetFileData.Default;

    public FileInputViewModel(TargetFileStore targetFileStore)
    {
        _targetFileStore = targetFileStore;
        WeakReferenceMessenger.Default.Register(this);
    }

    [RelayCommand]
    private async Task SelectFile()
    {
        if (GetActiveWindow() is not { } window)
        {
            return;
        }

        var selectedFiles = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
        });

        if (selectedFiles is { Count: > 0 })
        {
            await _targetFileStore.Dispatch(new ExtractTargetFileDataAction(selectedFiles[0].Path.LocalPath));
        }
    }

    public async Task HandleDroppedFileAsync(string[] selectedFiles)
    {
        if (selectedFiles is { Length: > 0 })
        {
            await _targetFileStore.Dispatch(new ExtractTargetFileDataAction(selectedFiles[0]));
        }
    }

    public void Receive(StoreChangedMessage<TargetFileState> message)
    {
        if (message.State.FileData.IsT1)
        {
            var errorDescription = message.State.FileData.AsT1 switch
            {
                ExtractionError.NotRm2k3 => I18n.UI.Error_Not2k3File,
                ExtractionError.EngineTooOld => I18n.UI.Error_EngineTooOld,
                _ => throw new UnreachableException()
            };
            DialogHost.Show(OperationResult.ErrorResult(errorDescription));
            return;
        }

        FileData = message.State.FileData.AsT0;
    }

    private Window? GetActiveWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}