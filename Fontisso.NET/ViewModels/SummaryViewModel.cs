using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fontisso.NET.Models;

namespace Fontisso.NET.ViewModels;

public partial class SummaryViewModel : ViewModelBase
{
    [ObservableProperty]
    private IAppState _state;

    public SummaryViewModel(IAppState state)
    {
        State = state;
        State.PropertyChanged += OnStatePropertyChanged;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(State.SelectedFont) or nameof(State.SampleText))
        {
            UpdateSampleTextImageCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task UpdateSampleTextImage()
    {
        await State.GeneratePreviewImage();
    }

    public void UpdatePreviewWidth(double width)
    {
        State.PreviewWidth = width;
    }
}