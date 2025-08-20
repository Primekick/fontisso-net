using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Flux;
using Fontisso.NET.Services;

namespace Fontisso.NET.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IRecipient<StoreChangedMessage<TargetFileState>>,
    IRecipient<StoreChangedMessage<FontStoreState>>
{
    [ObservableProperty] private FileInputViewModel _fileInput;
    [ObservableProperty] private FontPickerViewModel _fontPicker;
    [ObservableProperty] private TextPreviewViewModel _textPreview;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    private FontEntry _selectedFont;
    
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    private TargetFileData _fileData;

    private readonly IPatchingService _patchingService;

    public MainWindowViewModel(FileInputViewModel fileInput, FontPickerViewModel fontPicker, TextPreviewViewModel textPreview,
        IPatchingService patchingService)
    {
        _patchingService = patchingService;
        FileInput = fileInput;
        FontPicker = fontPicker;
        TextPreview = textPreview;
        WeakReferenceMessenger.Default.Register<StoreChangedMessage<TargetFileState>>(this);
        WeakReferenceMessenger.Default.Register<StoreChangedMessage<FontStoreState>>(this);
    }
    
    private bool CanPatch() => SelectedFont != default && FileData != default;

    [RelayCommand(CanExecute = nameof(CanPatch))]
    private async Task Patch()
    {
        var patchingResult = _patchingService.PatchExecutable(FileData, SelectedFont.Rpg2000Data.Span, SelectedFont.Rpg2000GData.Span);
        await DialogHost.Show(patchingResult);
    }

    public void Receive(StoreChangedMessage<TargetFileState> message)
    {
        FileData = message.State.FileData;
    }

    public void Receive(StoreChangedMessage<FontStoreState> message)
    {
        SelectedFont = message.State.SelectedFont;
    }
}