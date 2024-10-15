using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fontisso.NET.Helpers;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Services;

public interface IResourceService
{
    Task<Bitmap> ExtractIconFromFile(string filePath);
    Task WriteResource(string filePath, int resourceId, byte[] data);
    Task<byte[]> ExtractResource(int resourceId);
}

public class ResourceService : IResourceService
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr FindResource(IntPtr hModule, int lpName, int lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateResource(IntPtr hUpdate, int lpType, int lpName, ushort wLanguage, byte[] lpData,
        uint cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    private const int RT_RCDATA = 10;

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
            using var gdiBitmap = icon.ToBitmap();

            return BitmapConverter.FromGdiBitmapToAvaloniaBitmap(gdiBitmap);
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    });

    public async Task WriteResource(string filePath, int resourceId, byte[] data) => await Task.Run(() =>
    {
        var updateHandle = BeginUpdateResource(filePath, false);
        if (updateHandle == IntPtr.Zero)
        {
            throw new Exception("Failed to begin update resource.");
        }

        var result = UpdateResource(updateHandle, RT_RCDATA, resourceId, 0, data, (uint)data.Length);
        if (!result)
        {
            throw new Exception("Failed to update resource.");
        }

        result = EndUpdateResource(updateHandle, false);
        if (!result)
        {
            throw new Exception("Failed to end update resource.");
        }
    });

    public async Task<byte[]> ExtractResource(int resourceId)
    {
        throw new NotImplementedException();
    }
}