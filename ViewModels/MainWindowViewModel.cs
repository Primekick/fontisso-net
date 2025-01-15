using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using Fontisso.NET.Models;
using Fontisso.NET.Services;

namespace Fontisso.NET.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private IAppState _state;
    [ObservableProperty] private FileInputViewModel _fileInput;
    [ObservableProperty] private FontPickerViewModel _fontPicker;
    [ObservableProperty] private SummaryViewModel _summary;

    private readonly IPatchingService _patchingService;

    public MainWindowViewModel(IAppState state, FileInputViewModel fileInput, FontPickerViewModel fontPicker, SummaryViewModel summary,
        IPatchingService patchingService)
    {
        State = state;
        FileInput = fileInput;
        FontPicker = fontPicker;
        Summary = summary;
        _patchingService = patchingService;
    }

    [RelayCommand]
    private async Task Confirm()
    {
        var patchingResult = await _patchingService.PatchExecutable(State.FileData, State.SelectedFont.Data);
        await DialogHost.Show(patchingResult);
    }
}