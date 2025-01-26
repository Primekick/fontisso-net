using System;
using Fontisso.NET.Configuration.Patching;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Stores;
using Fontisso.NET.Services;
using Fontisso.NET.Services.Metadata;
using Fontisso.NET.Services.Patching;
using Fontisso.NET.Services.Rendering;
using Fontisso.NET.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Fontisso.NET;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFontissoApp(this IServiceCollection services) =>
        services.AddFontissoServices()
            .AddDataStores()
            .AddFontissoViewModels();

    private static IServiceCollection AddFontissoServices(this IServiceCollection services) =>
        services.AddLegacyPatchingConfig(
                legacyPatchingConfig => legacyPatchingConfig
                    .WithLegacyLoaderDllName("Fontisso.NET.LegacyFontLoader.dll")
                    .WithFontsDirectory("Fonts")
                    .WithFontFileNames(("RPG2000.fon", "RPG2000G.fon"))
                    .WithBuiltinFontNames(("MS Mincho", "MS Gothic"))
                    .WithCustomFontNames(("CstFnt1", "CstFnt2"))
            )
            .AddSingleton<IFontService, FontService>()
            .AddSingleton<IResourceService, ResourceService>()
            .AddSingleton<IFontRenderer, FontRenderer>()
            .AddSingleton<ITextLayoutEngine, TextLayoutEngine>()
            .AddSingleton<IFontMetadataProcessor, FontMetadataProcessor>()
            .AddPatchingStrategies()
            .AddSingleton<IPatchingService, PatchingService>()
            .AddSingleton<SharpFont.Library>(_ => new SharpFont.Library());


    private static IServiceCollection AddLegacyPatchingConfig(this IServiceCollection services,
        Action<LegacyPatchingConfigBuilder> configure)
    {
        var configBuilder = new LegacyPatchingConfigBuilder();
        configure.Invoke(configBuilder);
        var legacyPatchingConfig = configBuilder.Build();
        services.AddSingleton(legacyPatchingConfig);
        return services;
    }

    private static IServiceCollection AddPatchingStrategies(this IServiceCollection services) =>
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

    private static IServiceCollection AddDataStores(this IServiceCollection services) =>
        services.AddSingleton<FontStore>()
            .AddSingleton<TextPreviewStore>()
            .AddSingleton<TargetFileStore>();

    private static IServiceCollection AddFontissoViewModels(this IServiceCollection services) =>
        services.AddSingleton<FileInputViewModel>()
            .AddSingleton<FontPickerViewModel>()
            .AddSingleton<TextPreviewViewModel>()
            .AddSingleton<MainWindowViewModel>();
}