using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fontisso.NET.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private FileInputViewModel _fileInput;
    [ObservableProperty] private FontPickerViewModel _fontPicker;
    [ObservableProperty] private SummaryViewModel _summary;

    public MainWindowViewModel(FileInputViewModel fileInput, FontPickerViewModel fontPicker, SummaryViewModel summary)
    {
        FileInput = fileInput;
        FontPicker = fontPicker;
        Summary = summary;
    }
    
    [RelayCommand]
    private async Task Confirm()
    {
    }
}
