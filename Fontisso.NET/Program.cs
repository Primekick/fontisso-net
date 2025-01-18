using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace Fontisso.NET;

sealed class Program
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int MessageBox(
        IntPtr hWnd, 
        string text, 
        string caption, 
        uint type
    );
    
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            _ = MessageBox(IntPtr.Zero, string.Format(I18n.UI.Error_Unhandled, ex.Message), I18n.UI.Dialog_Error, 0x10);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
