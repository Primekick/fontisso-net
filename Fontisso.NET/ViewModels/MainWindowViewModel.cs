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
    [ObservableProperty]
    public partial FileInputViewModel FileInput { get; set; }

    [ObservableProperty]
    public partial FontPickerViewModel FontPicker { get; set; }

    [ObservableProperty]
    public partial TextPreviewViewModel TextPreview { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    public partial Fonts.FontEntry SelectedFont { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PatchCommand))]
    public partial Resources.TargetFileData FileData { get; set; }

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