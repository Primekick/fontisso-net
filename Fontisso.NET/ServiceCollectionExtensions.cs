using Fontisso.NET.Data.Stores;
using Fontisso.NET.Models;
using Fontisso.NET.Services;
using Fontisso.NET.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Fontisso.NET;

public static class ServiceCollectionExtensions
{
    
    public static IServiceCollection AddState(this IServiceCollection services)
    {
        services.AddSingleton<IAppState, AppState>();
        services.AddSingleton<AppState>();
        services.AddSingleton<FontStore>();
        services.AddSingleton<TextPreviewStore>();
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