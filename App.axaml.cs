using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Fontisso.NET.Services;
using Fontisso.NET.ViewModels;
using Fontisso.NET.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Fontisso.NET;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            
            var services = new ServiceCollection();
            services.AddSingleton<IFontService, FontService>()
                .AddSingleton<IResourceService, ResourceService>()
                .AddSingleton<AppViewModel>()
                .AddSingleton<FileInputViewModel>()
                .AddSingleton<FontPickerViewModel>()
                .AddSingleton<SummaryViewModel>()
                .AddSingleton<MainWindowViewModel>();
            
            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = Ioc.Default.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}