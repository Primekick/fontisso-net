using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fontisso.NET.ViewModels;

public partial class SummaryViewModel : ViewModelBase
{
    [ObservableProperty] private AppViewModel _state;

    public SummaryViewModel(AppViewModel state)
    {
        State = state;
    }

    [RelayCommand]
    private async Task Confirm()
    {
    }
}