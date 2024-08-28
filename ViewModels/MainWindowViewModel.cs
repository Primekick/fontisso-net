using CommunityToolkit.Mvvm.ComponentModel;

namespace Fontisso.NET.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private FileInputViewModel _fileInput;
    [ObservableProperty] private FontPickerViewModel _fontPicker;
    [ObservableProperty] private SummaryViewModel _summary;
    [ObservableProperty] private AppViewModel _state;

    public MainWindowViewModel(FileInputViewModel fileInput, FontPickerViewModel fontPicker, SummaryViewModel summary, AppViewModel state)
    {
        FileInput = fileInput;
        FontPicker = fontPicker;
        Summary = summary;
        State = state;
    }
}
