using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Helpers;
using OneOf;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Services;

public interface IResourceService
{
    Task<Bitmap> ExtractIconFromFile(string filePath);
    Task WriteResource(string filePath, int resourceId, byte[] data);
    Task<byte[]> ExtractResource(int resourceId);
    Task<OneOf<TargetFileData, ExtractionError>> ExtractTargetFileData(string filePath);
}

public class ResourceService : IResourceService
{
    private static readonly Regex VERSION_REGEX = new(@"1.([01]).(\d{1,2}).\d{1,2}", RegexOptions.Compiled);

    [StructLayout(LayoutKind.Sequential)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;
    private const uint SHGFI_LARGEICON = 0x00000000;
    private const uint SHGFI_SMALLICON = 0x00000001;

    [DllImport("shell32.dll")]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO shfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateResource(IntPtr hUpdate, int lpType, int lpName, ushort wLanguage, byte[]? lpData, uint cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceNames(IntPtr hModule, uint lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EnumResourceLanguages(IntPtr hModule, uint lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    private delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpszType, IntPtr lParam);

    private delegate bool EnumResNameProc(IntPtr hModule, uint lpType, IntPtr lpName, IntPtr lParam);

    private delegate bool EnumResLangProc(IntPtr hModule, uint lpType, IntPtr lpName, ushort wIdLanguage,
        IntPtr lParam);


    private const int RT_RCDATA = 10;
    private const int ERROR_NO_MORE_ITEMS = 259;
    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

    public async Task<Bitmap> ExtractIconFromFile(string filePath) => await Task.Run(() =>
    {
        var iconHandle = ExtractIcon(IntPtr.Zero, filePath, 0);

        // null handle means there are no embedded icons
        if (iconHandle == IntPtr.Zero)
        {
            const uint flags = SHGFI_USEFILEATTRIBUTES | SHGFI_ICON | SHGFI_LARGEICON;

            var shfi = new SHFILEINFO();
            SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);
            iconHandle = shfi.hIcon;
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
        var libHandle = LoadLibraryEx(filePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (libHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadLibraryEx failed.");
        }
        
        var resourcesToRemove = GetResourcesToRemove(libHandle);

        var updateHandle = BeginUpdateResource(filePath, false);
        if (updateHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "BeginUpdateResource failed.");
        }

        foreach (var res in resourcesToRemove)
        {
            if (!UpdateResource(updateHandle, RT_RCDATA, (int)res.Name, res.Language, null, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed.");
            }
        }

        var result = UpdateResource(updateHandle, RT_RCDATA, resourceId, 1033, data, (uint)data.Length);
        if (!result)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed.");
        }

        result = EndUpdateResource(updateHandle, false);
        if (!result)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "EndUpdateResource failed.");
        }
    });

    private ICollection<(IntPtr Name, ushort Language)> GetResourcesToRemove(IntPtr libHandle)
    {
        var resourcesToRemove = new List<(IntPtr Name, ushort Language)>();

        try
        {

            EnumResLangProc enumResLangCallback = (_, _, lpName, wIdLanguage, _) =>
            {
                resourcesToRemove.Add(new(lpName, wIdLanguage));
                return true;
            };

            EnumResNameProc enumResNameCallback = (hModule, _, lpName, _) =>
            {
                if (!EnumResourceLanguages(hModule, RT_RCDATA, lpName, enumResLangCallback, IntPtr.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != ERROR_NO_MORE_ITEMS)
                    {
                        throw new Win32Exception(error, "EnumResourceLanguages failed.");
                    }
                }

                return true;
            };

            EnumResTypeProc enumResTypeCallback = (hModule, lpszType, _) =>
            {
                if (lpszType != RT_RCDATA)
                {
                    return true;
                }

                if (!EnumResourceNames(hModule, RT_RCDATA, enumResNameCallback, IntPtr.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != ERROR_NO_MORE_ITEMS)
                    {
                        throw new Win32Exception(error, "EnumResourceNames failed.");
                    }
                }

                return true;
            };

            if (!EnumResourceTypes(libHandle, enumResTypeCallback, IntPtr.Zero))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_NO_MORE_ITEMS)
                {
                    throw new Win32Exception(error, "EnumResourceTypes failed.");
                }
            }

            return resourcesToRemove;
        }
        finally
        {
            FreeLibrary(libHandle);
        }
    }

    public async Task<byte[]> ExtractResource(int resourceId)
    {
        throw new NotImplementedException();
    }

    private OneOf<EngineType, ExtractionError> ExtractEngineVersion(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var peReader = new PEReader(stream);

            if (peReader is not { IsEntireImageAvailable: true })
            {
                return ExtractionError.NotRm2k3;
            }

            if (peReader.PEHeaders.CoffHeader is not { } fileHeader)
            {
                return ExtractionError.NotRm2k3;
            }

            // old Maniacs has larger CHERRY section than vanilla
            // new Maniacs doesn't have a CHERRY section at all but has the build date as a timestamp
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
            return ExtractionError.NotRm2k3;
        }
    }

    public async Task<OneOf<TargetFileData, ExtractionError>> ExtractTargetFileData(string filePath)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);

        // 2k3 should always have a version info regardless of version
        if (versionInfo.ProductVersion is null)
        {
            return ExtractionError.NotRm2k3;
        }

        // check if it has a valid product version
        if (VERSION_REGEX.Match(versionInfo.ProductVersion) is not { Success: true } match)
        {
            return ExtractionError.NotRm2k3;
        }

        if (match.Groups is not [_, var majorVersion, var minorVersion])
        {
            return ExtractionError.NotRm2k3;
        }

        if (!int.TryParse(majorVersion.Value, out var major) || !int.TryParse(minorVersion.Value, out var minor))
        {
            return ExtractionError.NotRm2k3;
        }

        // only Steam version of 2k3 is patchable
        if (major < 1 || minor < 2)
        {
            return ExtractionError.EngineTooOld;
        }

        var engine = ExtractEngineVersion(filePath);
        if (engine.IsT1)
        {
            return engine.AsT1;
        }

        filePath = engine.AsT0 switch
        {
            EngineType.ModernManiacs => filePath,
            _ => Path.Combine(Path.GetDirectoryName(filePath)!, "ultimate_rt_eb.dll")
        };
        var fileName = Path.GetFileName(filePath);
        var fileIcon = await ExtractIconFromFile(filePath);

        return new TargetFileData(
            TargetFilePath: filePath,
            FileName: fileName,
            FileIcon: fileIcon,
            HasFile: true,
            Engine: engine.AsT0);
    }
}