using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fontisso.NET.Modules;

namespace Fontisso.NET.ViewModels;

public partial class FontPickerViewModel : ViewModelBase, IRecipient<Flux.StoreChangedMessage<Fonts.FontStoreState>>
{
    private readonly Fonts.FontStore _fontStore;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFonts))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFonts))]
    private ImmutableList<Fonts.FontEntry> _allFonts = ImmutableList<Fonts.FontEntry>.Empty;

    [ObservableProperty]
    private Fonts.FontEntry _selectedFont;

    public FontPickerViewModel(Fonts.FontStore fontStore)
    {
        _fontStore = fontStore;
        WeakReferenceMessenger.Default.Register(this);
        LoadFonts();
    }

    public void Receive(Flux.StoreChangedMessage<Fonts.FontStoreState> message)
    {
        AllFonts = message.State.Fonts;
    }

    public IEnumerable<Fonts.FontEntry> FilteredFonts =>
        AllFonts.Where(f =>
            f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            f.Attribution.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    partial void OnSelectedFontChanged(Fonts.FontEntry value)
    {
        if (value == default)
            return;
        
        _fontStore.Dispatch(new Fonts.SelectFontAction(value));
    }

    [RelayCommand]
    private void LoadFonts()
    {
        _fontStore.Dispatch(new Fonts.SeedFontsAction());
    }
}