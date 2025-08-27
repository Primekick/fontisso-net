using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using Fontisso.NET.Modules;

namespace Fontisso.NET.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IRecipient<Flux.StoreChangedMessage<Resources.TargetFileState>>,
    IRecipient<Flux.StoreChangedMessage<Fonts.FontStoreState>>
{
    [ObservableProperty] private FileInputViewModel _fileInput;
    [ObservableProperty] private FontPickerViewModel _fontPicker;
    [ObservableProperty] private TextPreviewViewModel _textPreview;

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    private Fonts.FontEntry _selectedFont;
    
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    private Resources.TargetFileData _fileData;

    public MainWindowViewModel(FileInputViewModel fileInput, FontPickerViewModel fontPicker, TextPreviewViewModel textPreview)
    {
        FileInput = fileInput;
        FontPicker = fontPicker;
        TextPreview = textPreview;
        WeakReferenceMessenger.Default.Register<Flux.StoreChangedMessage<Resources.TargetFileState>>(this);
        WeakReferenceMessenger.Default.Register<Flux.StoreChangedMessage<Fonts.FontStoreState>>(this);
    }
    
    private bool CanPatch() => SelectedFont != default && FileData != default;

    [RelayCommand(CanExecute = nameof(CanPatch))]
    private async Task Patch()
    {
        var patchingResult = Patching.PatchExecutable(FileData, SelectedFont.DataRpg2000, SelectedFont.DataRpg2000G);
        await DialogHost.Show(patchingResult);
    }

    public void Receive(Flux.StoreChangedMessage<Resources.TargetFileState> message)
    {
        FileData = message.State.FileData;
    }

    public void Receive(Flux.StoreChangedMessage<Fonts.FontStoreState> message)
    {
        SelectedFont = message.State.SelectedFont;
    }
}