using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Data.Models.Metadata;
using Fontisso.NET.Data.Models.WinApi;
using Fontisso.NET.Modules.Extensions;

namespace Fontisso.NET.Services;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

public interface IResourceService
{
    AvaloniaBitmap ExtractIconFromFile(string filePath);
    void WriteResources(string filePath, IEnumerable<(FontKind kind, ReadOnlyMemory<byte> data)> resources);
    TargetFileData ExtractTargetFileData(string filePath);
}

public partial class ResourceService : IResourceService
{
    private static readonly Regex VERSION_REGEX = new(@"1.([01]).(\d{1,2}).\d{1,2}", RegexOptions.Compiled);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;
    private const uint SHGFI_LARGEICON = 0x00000000;
    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    private const int RT_RCDATA = 10;
    private const int ERROR_NO_MORE_ITEMS = 259;

    [DllImport("shell32.dll")]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref ShFileInfo shfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

    [LibraryImport("kernel32.dll", EntryPoint = "UpdateResourceW", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, ReadOnlySpan<byte> lpData, uint cbData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    private delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpszType, IntPtr lParam);

    private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);

    private delegate bool EnumResLangProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wIdLanguage,
        IntPtr lParam);

    [SupportedOSPlatform("windows")]
    public AvaloniaBitmap ExtractIconFromFile(string filePath)
    {
        var iconHandle = ExtractIcon(IntPtr.Zero, filePath, 0);

        // null handle means there are no embedded icons
        if (iconHandle == IntPtr.Zero)
        {
            const uint flags = SHGFI_USEFILEATTRIBUTES | SHGFI_ICON | SHGFI_LARGEICON;

            var shfi = new ShFileInfo();
            SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(typeof(ShFileInfo)), flags);
            iconHandle = shfi.hIcon;
        }

        try
        {
            using var icon = Icon.FromHandle(iconHandle);
            using var gdiBitmap = icon.ToBitmap();

            return gdiBitmap.IntoAvaloniaBitmap();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    public void WriteResources(string filePath, IEnumerable<(FontKind kind, ReadOnlyMemory<byte> data)> resources)
    {
        var libHandle = LoadLibraryEx(filePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (libHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadLibraryEx failed.");
        }
        
        var resourcesToRemove = FindResources(libHandle, ["#10"], ["#100", "#101"]);

        var updateHandle = BeginUpdateResource(filePath, false);
        if (updateHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "BeginUpdateResource failed.");
        }

        foreach (var res in resourcesToRemove)
        {
            if (!UpdateResource(updateHandle, RT_RCDATA, StringToResource(res.Name), res.Language, null, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed.");
            }
        }

        bool result;
        foreach (var resource in resources)
        {
            result = UpdateResource(updateHandle, RT_RCDATA, (int)resource.kind, 1033, resource.data.Span, (uint)resource.data.Length);
            if (!result)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed.");
            }
        }

        result = EndUpdateResource(updateHandle, false);
        if (!result)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "EndUpdateResource failed.");
        }
    }

    private List<PEResource> FindResources(
        IntPtr libHandle,
        string[]? resourceTypes = null,
        string[]? resourceNames = null,
        ushort[]? resourceLanguages = null)
    {
        resourceTypes ??= [];
        resourceNames ??= [];
        resourceLanguages ??= [];
        var resourcesFound = new List<PEResource>();

        try
        {
            EnumResTypeProc enumResTypeCallback = (hModule, lpszType, _) =>
            {
                var type = ResourceToString(lpszType);
                if (!MatchesFilter(type, resourceTypes))
                {
                    return true;
                }
                
                EnumResNameProc enumResNameCallback = (hModuleName, _, lpName, _) =>
                {
                    var name = ResourceToString(lpName);
                    if (!MatchesFilter(name, resourceNames))
                    {
                        return true;
                    }
                
                    EnumResLangProc enumResLangCallback = (_, _, _, wIdLanguage, _) =>
                    {
                        if (!MatchesLanguage(wIdLanguage, resourceLanguages))
                        {
                            return true;
                        }
                
                        resourcesFound.Add(new PEResource(type, name, wIdLanguage));
                        return true;
                    };
                
                    if (!EnumResourceLanguages(hModuleName, lpszType, lpName, enumResLangCallback, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error != ERROR_NO_MORE_ITEMS)
                        {
                            throw new Win32Exception(error, "EnumResourceLanguages failed.");
                        }
                    }

                    return true;
                };

                if (!EnumResourceNames(hModule, lpszType, enumResNameCallback, IntPtr.Zero))
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

            return resourcesFound;
        }
        finally
        {
            FreeLibrary(libHandle);
        }
    }

    private string ResourceToString(IntPtr resource) => IsIntResource(resource)
        ? "#" + resource.ToInt32()
        : Marshal.PtrToStringUni(resource)!;

    private IntPtr StringToResource(string resource) =>
        ushort.Parse(resource[1..], NumberStyles.None, CultureInfo.InvariantCulture);

    // resource IDs are 16bit integers stored in pointer
    private bool IsIntResource(IntPtr resource) => (resource.ToInt64() >> 16) == 0;

    private bool MatchesFilter(string value, string[] filters) =>
        filters is [] || filters.Contains(value, StringComparer.OrdinalIgnoreCase);

    private bool MatchesLanguage(ushort lang, ushort[] filters) => filters is [] || filters.Contains(lang);

    private EngineType ExtractModern2k3Engine(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var peReader = new PEReader(stream);

            if (peReader is not { IsEntireImageAvailable: true })
            {
                return default;
            }

            if (peReader.PEHeaders.CoffHeader is not { } fileHeader)
            {
                return default;
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
                (false, false) => EngineType.ModernVanilla2k3
            };
        }
        catch (Exception)
        {
            return default;
        }
    }

    private EngineType ExtractEngineFromVersionInfo(string filePath, FileVersionInfo versionInfo)
    {
        // check if it has a valid product version
        if (VERSION_REGEX.Match(versionInfo.ProductVersion ?? string.Empty) is not { Success: true } match)
        {
            return default;
        }

        if (match.Groups is not [_, var majorVersion, var minorVersion])
        {
            return default;
        }

        if (!int.TryParse(majorVersion.Value, out var major) || !int.TryParse(minorVersion.Value, out var minor))
        {
            return default;
        }
            
        return (major < 1 || minor < 2) switch
        {
            true => EngineType.OldVanilla2k3,
            false => ExtractModern2k3Engine(filePath)
        };
    }
    
    private EngineType ExtractEngineWithoutVersionInfo(string filePath)
    {
        var libHandle = LoadLibraryEx(filePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (libHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadLibraryEx failed.");
        }

        var logosFound = FindResources(libHandle, ["XYZ"]);
        return logosFound.Count switch
        {
            // only 2k has 3 logos
            3 => EngineType.Vanilla2k,
            // a single logo is most probably 2k3 (not always though)
            1 => EngineType.OldVanilla2k3,
            // no idea
            _ => default
        };
    }

    [SupportedOSPlatform("windows")]
    public TargetFileData ExtractTargetFileData(string filePath)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
        
        var engine = versionInfo.ProductVersion switch
        {
            // turns out 2k and some earlier 2k3 versions don't have VersionInfo embedded 
            null => ExtractEngineWithoutVersionInfo(filePath),
            _ => ExtractEngineFromVersionInfo(filePath, versionInfo)
        };
        
        if (engine == default)
        {
            return default;
        }
        
        filePath = engine switch
        {
            EngineType.ModernManiacs or EngineType.OldVanilla2k3 or EngineType.Vanilla2k => filePath,
            _ => Path.Combine(Path.GetDirectoryName(filePath)!, "ultimate_rt_eb.dll")
        };
        var fileName = Path.GetFileName(filePath);
        var fileIcon = ExtractIconFromFile(filePath);

        return new TargetFileData(
            TargetFilePath: filePath,
            FileName: fileName,
            FileIcon: fileIcon,
            Engine: engine);
    }
}