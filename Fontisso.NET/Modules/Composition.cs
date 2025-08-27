using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Fontisso.NET.Services;
using Fontisso.NET.Services.Patching;
using Fontisso.NET.ViewModels;
using Fontisso.NET.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Fontisso.NET.Modules;

public class ViewLocator : IDataTemplate
{
    private static Dictionary<Type, Func<Control>> Registration = new();

    public static void Register<TViewModel, TView>() where TView : Control, new()
    {
        Registration.Add(typeof(TViewModel), () => new TView());
    }

    public static void Register<TViewModel, TView>(Func<TView> factory) where TView : Control
    {
        Registration.Add(typeof(TViewModel), factory);
    }

    public Control Build(object? data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var type = data.GetType();

        if (Registration.TryGetValue(type, out var factory))
        {
            return factory();
        }

        return new TextBlock { Text = "Not Found: " + type };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}

public static class Composition
{
    public static ServiceProvider BuildFontissoApp(this IServiceCollection services)
    {
        ViewLocator.Register<MainWindowViewModel, MainWindow>();
        ViewLocator.Register<FileInputViewModel, FileInputView>();
        ViewLocator.Register<FontPickerViewModel, FontPickerView>();
        ViewLocator.Register<TextPreviewViewModel, TextPreviewView>();
        
        services
            .AddSingleton<Fonts.FontStore>()
            .AddSingleton<Fonts.TextPreviewStore>()
            .AddSingleton<Resources.TargetFileStore>();

        services
            .AddSingleton<IPatchingService, PatchingService>();
        
        services
            .AddKeyedSingleton<IPatchingStrategy, LegacyPatchingStrategy>("legacy")
            .AddKeyedSingleton<IPatchingStrategy, ModernPatchingStrategy>("modern")
            .AddSingleton(sp => new PatchingStrategyContext([
                new EnginePatchingMapping(
                    Strategy: sp.GetRequiredKeyedService<IPatchingStrategy>("legacy"),
                    Engines: [Resources.EngineType.Vanilla2k, Resources.EngineType.OldVanilla2k3]
                ),
                new EnginePatchingMapping(
                    Strategy: sp.GetRequiredKeyedService<IPatchingStrategy>("modern"),
                    Engines: [Resources.EngineType.ModernVanilla2k3, Resources.EngineType.OldManiacs, Resources.EngineType.ModernManiacs]
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