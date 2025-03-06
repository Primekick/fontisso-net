using System.Collections.Immutable;
using System.Threading.Tasks;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Flux;
using Fontisso.NET.Services;

namespace Fontisso.NET.Data.Stores;

public record struct SeedFontsAction() : IAction;

public record struct SelectFontAction(FontEntry Font) : IAction;

public record struct FontStoreState(ImmutableList<FontEntry> Fonts, FontEntry SelectedFont)
{
    public static FontStoreState Default => new(ImmutableList<FontEntry>.Empty, default);
}

public class FontStore : Store<FontStoreState>
{
    private readonly IFontService _fontService;
    
    public FontStore(IFontService fontService) : base(FontStoreState.Default)
    {
        _fontService = fontService;
    }
    
    public override async Task Dispatch(IAction action)
    {
        switch (action)
        {
            case SeedFontsAction seed:
                var fonts = await _fontService.LoadAvailableFonts();
                SetState(state => state with { Fonts = fonts });
                break;
            case SelectFontAction select:
                SetState(state => state with { SelectedFont = select.Font });
                break;
        }

        await Task.CompletedTask;
    }
}