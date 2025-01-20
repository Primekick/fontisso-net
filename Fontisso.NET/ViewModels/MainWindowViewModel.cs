using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Flux;
using Fontisso.NET.Services;
using OneOf;

namespace Fontisso.NET.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IRecipient<StoreChangedMessage<TargetFileState>>,
    IRecipient<StoreChangedMessage<FontStoreState>>
{
    [ObservableProperty] private FileInputViewModel _fileInput;
    [ObservableProperty] private FontPickerViewModel _fontPicker;
    [ObservableProperty] private SummaryViewModel _summary;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    private FontEntry? _selectedFont;
    
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    private OneOf<TargetFileData, ExtractionError> _fileData;

    private readonly IPatchingService _patchingService;

    public MainWindowViewModel(FileInputViewModel fileInput, FontPickerViewModel fontPicker, SummaryViewModel summary,
        IPatchingService patchingService)
    {
        _patchingService = patchingService;
        FileInput = fileInput;
        FontPicker = fontPicker;
        Summary = summary;
        WeakReferenceMessenger.Default.Register<StoreChangedMessage<TargetFileState>>(this);
        WeakReferenceMessenger.Default.Register<StoreChangedMessage<FontStoreState>>(this);
    }
    
    private bool CanPatch() => SelectedFont is not null && FileData.IsT0 && FileData.AsT0.HasFile;

    [RelayCommand(CanExecute = nameof(CanPatch))]
    private async Task Patch()
    {
        var patchingResult = _patchingService.PatchExecutable(FileData.AsT0, SelectedFont!.Rpg2000Data, SelectedFont!.Rpg2000GData);
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