using Fontisso.NET.Data.Stores;
using Fontisso.NET.Services;
using Fontisso.NET.Services.Metadata;
using Fontisso.NET.Services.Rendering;
using Fontisso.NET.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Fontisso.NET;

public static class ServiceCollectionExtensions
{
    
    public static IServiceCollection AddDataStores(this IServiceCollection services)
    {
        services.AddSingleton<FontStore>();
        services.AddSingleton<TextPreviewStore>();
        services.AddSingleton<TargetFileStore>();
        return services;
    }
    
    public static IServiceCollection AddFontissoServices(this IServiceCollection services)
    {
        services.AddSingleton<IFontService, FontService>()
            .AddSingleton<IResourceService, ResourceService>()
            .AddSingleton<IPatchingService, PatchingService>()
            .AddSingleton<IFontRenderer, FontRenderer>()
            .AddSingleton<ITextLayoutEngine, TextLayoutEngine>()
            .AddSingleton<IFontMetadataProcessor, FontMetadataProcessor>()
            .AddSingleton<SharpFont.Library>(_ => new SharpFont.Library());
        return services;
    }

    public static IServiceCollection AddFontissoViewModels(this IServiceCollection services)
    {
        services.AddSingleton<FileInputViewModel>()
            .AddSingleton<FontPickerViewModel>()
            .AddSingleton<TextPreviewViewModel>()
            .AddSingleton<MainWindowViewModel>();
        return services;
    }
}