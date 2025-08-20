using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasFileData))]
    private TargetFileData _fileData;

    public bool HasFileData => FileData != default;

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

        var selectedFiles = await window.StorageProvider.OpenFilePickerAsync(new() { AllowMultiple = false });

        if (selectedFiles is { Count: > 0 })
        {
            _targetFileStore.Dispatch(new ExtractTargetFileDataAction(selectedFiles[0].Path.LocalPath));
        }
    }

    public void HandleDroppedFileAsync(string[] selectedFiles)
    {
        if (selectedFiles is { Length: > 0 })
        {
            _targetFileStore.Dispatch(new ExtractTargetFileDataAction(selectedFiles[0]));
        }
    }

    public void Receive(StoreChangedMessage<TargetFileState> message)
    {
        if (message.State.FileData == default)
        {
            DialogHost.Show(OperationResult.ErrorResult(I18n.UI.Error_Not2k3File));
            return;
        }

        FileData = message.State.FileData;
    }

    private Window? GetActiveWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}