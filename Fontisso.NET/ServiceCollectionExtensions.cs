using Fontisso.NET.Data.Stores;
using Fontisso.NET.Services;
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
            .AddSingleton<IPatchingService, PatchingService>();
        return services;
    }

    public static IServiceCollection AddFontissoViewModels(this IServiceCollection services)
    {
        services.AddSingleton<FileInputViewModel>()
            .AddSingleton<FontPickerViewModel>()
            .AddSingleton<SummaryViewModel>()
            .AddSingleton<MainWindowViewModel>();
        return services;
    }
}