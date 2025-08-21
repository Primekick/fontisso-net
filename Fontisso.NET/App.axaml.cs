using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Fontisso.NET.Modules.Composition;
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
        I18n.UI.Culture = CultureInfo.CurrentCulture;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ViewLocator.Register<MainWindowViewModel, MainWindow>();
            ViewLocator.Register<FileInputViewModel, FileInputView>();
            ViewLocator.Register<FontPickerViewModel, FontPickerView>();
            ViewLocator.Register<TextPreviewViewModel, TextPreviewView>();
            
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            
            Ioc.Default.ConfigureServices(new ServiceCollection().BuildFontissoApp());
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = Ioc.Default.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}