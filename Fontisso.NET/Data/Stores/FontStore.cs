using System.Collections.Immutable;
using System.Threading.Tasks;
using Fontisso.NET.Models;
using Fontisso.NET.Services;

namespace Fontisso.NET.Data.Stores;

public record SeedFontsAction() : IAction;

public record SelectFontAction(FontEntry Font) : IAction;

public record FontStoreState(ImmutableList<FontEntry> Fonts, FontEntry? SelectedFont)
{
    public static FontStoreState Default => new(ImmutableList<FontEntry>.Empty, null);
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