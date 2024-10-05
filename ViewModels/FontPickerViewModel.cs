using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fontisso.NET.Models;
using Fontisso.NET.Services;

namespace Fontisso.NET.ViewModels;

public partial class FontPickerViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFonts))]
    private string searchText = string.Empty;
    
    [ObservableProperty] private AppViewModel _state;
    private readonly IFontService _fontService;
    
    public FontPickerViewModel(AppViewModel state, IFontService fontService)
    {
        _fontService = fontService;
        State = state;
        State.Fonts = _fontService.LoadAvailableFonts();
    }

    public IEnumerable<FontEntry> FilteredFonts =>
        State.Fonts.Where(f => f.Details.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    [RelayCommand]
    private void SelectFont(FontEntry font)
    {
        State.SelectedFont = font;
    }
}