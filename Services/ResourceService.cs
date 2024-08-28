using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Services;

public interface IResourceService
{
    Task<Bitmap> ExtractIconFromFile(string filePath);
}

public class ResourceService : IResourceService
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
    
    public async Task<Bitmap> ExtractIconFromFile(string filePath) => await Task.Run(() =>
    {
        var iconHandle = ExtractIcon(IntPtr.Zero, filePath, 0);
        
        // null handle means there are no embedded icons
        if (iconHandle == IntPtr.Zero)
        {
            throw new Exception("Failed to extract icon from the specified file.");
        }

        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            using var gdiPlusBitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            
            gdiPlusBitmap.Save(stream, ImageFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);
            return new Bitmap(stream);
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    });
}