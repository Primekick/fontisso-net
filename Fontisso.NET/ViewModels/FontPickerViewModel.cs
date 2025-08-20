using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Flux;

namespace Fontisso.NET.ViewModels;

public partial class FontPickerViewModel : ViewModelBase, IRecipient<StoreChangedMessage<FontStoreState>>
{
    private readonly FontStore _fontStore;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFonts))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredFonts))]
    private ImmutableList<FontEntry> _fonts = ImmutableList<FontEntry>.Empty;

    [ObservableProperty]
    private FontEntry _selectedFont;

    public FontPickerViewModel(FontStore fontStore)
    {
        _fontStore = fontStore;
        WeakReferenceMessenger.Default.Register(this);
        LoadFonts();
    }

    public void Receive(StoreChangedMessage<FontStoreState> message)
    {
        Fonts = message.State.Fonts;
    }

    public IEnumerable<FontEntry> FilteredFonts =>
        Fonts.Where(f =>
            f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            f.Attribution.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    partial void OnSelectedFontChanged(FontEntry value)
    {
        if (value == default)
            return;
        
        _fontStore.Dispatch(new SelectFontAction(value));
    }

    [RelayCommand]
    private void LoadFonts()
    {
        _fontStore.Dispatch(new SeedFontsAction());
    }
}