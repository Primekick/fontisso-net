using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fontisso.NET.Models;

namespace Fontisso.NET.ViewModels;

public partial class FontPickerViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFonts))]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private IAppState _state;
    
    public FontPickerViewModel(IAppState state)
    {
        State = state;
        State.PropertyChanged += OnStatePropertyChanged;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(State.Fonts))
        {
            OnPropertyChanged(nameof(FilteredFonts));
        }
    }

    public IEnumerable<FontEntry> FilteredFonts =>
        State.Fonts.Where(f => f.Details.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
    
    [RelayCommand]
    private async Task LoadFonts()
    {
        await State.LoadFonts();
    }
}