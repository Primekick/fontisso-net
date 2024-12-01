using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fontisso.NET.Helpers;
using Fontisso.NET.Models;
using OneOf;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Services;

public interface IResourceService
{
    Task<Bitmap> ExtractIconFromFile(string filePath);
    Task WriteResource(string filePath, int resourceId, byte[] data);
    Task<byte[]> ExtractResource(int resourceId);
    Task<OneOf<TargetFileData, FileError>> ExtractTargetFileData(string filePath);
}

public class ResourceService : IResourceService
{
    private static readonly Regex VERSION_REGEX = new (@"1.([01]).(\d{1,2}).\d{1,2}", RegexOptions.Compiled);
    
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

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
    
    private OneOf<EngineType, FileError> ExtractEngineVersion(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var peReader = new PEReader(stream);

            if (peReader is not { IsEntireImageAvailable: true })
            {
                return FileError.NotRm2k3;
            }

            if (peReader.PEHeaders.CoffHeader is not { } fileHeader)
            {
                return FileError.NotRm2k3;
            }

            // Old Maniacs has larger CHERRY section than vanilla
            // New Maniacs doesn't have a CHERRY section at all but has the build date as a timestamp
            var cherrySection = peReader.PEHeaders.SectionHeaders.FirstOrDefault(section => section.Name == "CHERRY");
            var hasLargeCherrySection = cherrySection is { VirtualSize: > 0x10000 };

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(fileHeader.TimeDateStamp).DateTime.ToLocalTime();
            var hasModernTimestamp = timestamp is { Year: >= 2021 };

            return (hasModernTimestamp, hasLargeCherrySection) switch
            {
                (true, _) => EngineType.ModernManiacs,
                (false, true) => EngineType.OldManiacs,
                (false, false) => EngineType.Vanilla
            };
        }
        catch (Exception ex)
        {
            return FileError.NotRm2k3;
        }
    }
    
    public async Task<OneOf<TargetFileData, FileError>> ExtractTargetFileData(string filePath)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
        
        // 2k3 should always have a version info regardless of version
        if (versionInfo.ProductVersion is null)
        {
            return FileError.NotRm2k3;
        }
        
        // check if it has a valid product version
        if (VERSION_REGEX.Match(versionInfo.ProductVersion) is not { Success: true } match)
        {
            return FileError.NotRm2k3;
        }
        
        if (match.Groups is not [_, var majorVersion, var minorVersion])
        {
            return FileError.NotRm2k3;
        }

        if (!int.TryParse(majorVersion.Value, out var major) || !int.TryParse(minorVersion.Value, out var minor))
        {
            return FileError.NotRm2k3;
        }

        // only Steam version of 2k3 is patchable
        if (major < 1 || minor < 2)
        {
            return FileError.EngineTooOld;
        }

        var engine = ExtractEngineVersion(filePath);
        if (engine.IsT1)
        {
            return engine.AsT1;
        }
        
        var fileName = Path.GetFileName(filePath);
        var fileIcon = await ExtractIconFromFile(filePath);
        
        filePath = engine.AsT0 switch
        {
            EngineType.ModernManiacs => filePath,
            _ => Path.Combine(Path.GetDirectoryName(filePath)!, "ultimate_rt_eb.dll")
        };
        
        return new TargetFileData(
            TargetFilePath: filePath,
            FileName: fileName,
            FileIcon: fileIcon,
            HasFile: true,
            Engine: engine.AsT0);
    }
}