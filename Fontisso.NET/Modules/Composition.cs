using System;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Services;
using Fontisso.NET.Services.Patching;
using Fontisso.NET.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Fontisso.NET.Modules;

public static class Composition
{
    public static ServiceProvider BuildFontissoApp(this IServiceCollection services)
    {
        services
            .AddSingleton<Fonts.FontStore>()
            .AddSingleton<Fonts.TextPreviewStore>()
            .AddSingleton<TargetFileStore>();

        services
            .AddSingleton<IResourceService, ResourceService>()
            .AddSingleton<IPatchingService, PatchingService>();
        
        services
            .AddKeyedSingleton<IPatchingStrategy, LegacyPatchingStrategy>("legacy")
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
        
        services
            .AddSingleton<FileInputViewModel>()
            .AddSingleton<FontPickerViewModel>()
            .AddSingleton<TextPreviewViewModel>()
            .AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}