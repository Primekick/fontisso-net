using System;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Services;
using Fontisso.NET.Services.Metadata;
using Fontisso.NET.Services.Patching;
using Fontisso.NET.Services.Rendering;
using Fontisso.NET.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Fontisso.NET.Modules.Composition;

public static class Composition
{
    public static ServiceProvider BuildFontissoApp(this IServiceCollection services)
    {
        services.AddSingleton<FontStore>()
            .AddSingleton<TextPreviewStore>()
            .AddSingleton<TargetFileStore>();
        
        services
            .AddSingleton<IFontService, FontService>()
            .AddSingleton<IResourceService, ResourceService>()
            .AddSingleton<IFontRenderer, FontRenderer>()
            .AddSingleton<ITextLayoutEngine, TextLayoutEngine>()
            .AddSingleton<IFontMetadataProcessor, FontMetadataProcessor>()
            .AddSingleton<IPatchingService, PatchingService>()
            .AddSingleton<SharpFont.Library>(_ => new SharpFont.Library());
        
        services.AddKeyedSingleton<IPatchingStrategy, LegacyPatchingStrategy>("legacy")
            .AddKeyedSingleton<IPatchingStrategy, ModernPatchingStrategy>("modern")
            .AddSingleton(sp => new PatchingStrategyContext([
                new EnginePatchingMapping(
                    Strategy: sp.GetRequiredKeyedService<IPatchingStrategy>("legacy"),
                    Engines: [EngineType.Vanilla2k, EngineType.OldVanilla2k3]
                ),
                new EnginePatchingMapping(
                    Strategy: sp.GetRequiredKeyedService<IPatchingStrategy>("modern"),
                    Engines: [EngineType.ModernVanilla2k3, EngineType.OldManiacs, EngineType.ModernManiacs]
                )
            ]));
        
        services.AddSingleton<FileInputViewModel>()
            .AddSingleton<FontPickerViewModel>()
            .AddSingleton<TextPreviewViewModel>()
            .AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}