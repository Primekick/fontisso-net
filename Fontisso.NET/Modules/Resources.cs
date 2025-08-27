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
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Fontisso.NET.Modules;

public static partial class Resources
{
    public record struct TargetFileData(
        string FileName,
        AvaloniaBitmap? FileIcon,
        string TargetFilePath,
        EngineType Engine
    );
    
    public enum EngineType
    {
        Undefined,
        Vanilla2k,
        OldVanilla2k3,
        ModernVanilla2k3,
        OldManiacs,
        ModernManiacs
    }
    
    public static class EngineTypeConverter
    {
        public static FuncValueConverter<EngineType, string> AsString { get; } =
            new(engineType => engineType switch
            {
                EngineType.ModernVanilla2k3 => I18n.UI.EngineType_ModernVanilla2k3,
                EngineType.OldVanilla2k3 => I18n.UI.EngineType_OldVanilla2k3,
                EngineType.Vanilla2k => I18n.UI.EngineType_Vanilla2k,
                EngineType.OldManiacs => I18n.UI.EngineType_OldManiacs,
                EngineType.ModernManiacs => I18n.UI.EngineType_ModernManiacs,
                _ => "Unknown"
            });
    }
    
    public record struct PeResource(string Type, string Name, ushort Language);
    
    public record struct ExtractTargetFileDataAction(string FilePath) : Flux.IAction;

    public record struct TargetFileState(TargetFileData FileData)
    {
        public static TargetFileState Default => new(default);
    }

    public class TargetFileStore() : Flux.Store<TargetFileState>(TargetFileState.Default)
    {
        public override void Dispatch(Flux.IAction action)
        {
            switch (action)
            {
                case ExtractTargetFileDataAction etfda:
                    SetState(state => state with { FileData = ExtractTargetFileData(etfda.FilePath) });
                    break;
            }
        }
    }

    public static void WriteResources(string filePath, IEnumerable<(Fonts.FontKind kind, ReadOnlyMemory<byte> data)> resources)
    {
        var libHandle = Win32.LoadLibraryEx(filePath, IntPtr.Zero, Win32.LOAD_LIBRARY_AS_DATAFILE);
        if (libHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "LoadLibraryEx failed.");
        }
        
        var resourcesToRemove = FindResources(libHandle, ["#10"], ["#100", "#101"]);

        var updateHandle = Win32.BeginUpdateResource(filePath, false);
        if (updateHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "BeginUpdateResource failed.");
        }

        foreach (var res in resourcesToRemove)
        {
            if (!Win32.UpdateResource(updateHandle, Win32.RT_RCDATA, Win32.StringToResource(res.Name), res.Language, null, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed.");
            }
        }

        bool result;
        foreach (var resource in resources)
        {
            result = Win32.UpdateResource(updateHandle, Win32.RT_RCDATA, (int)resource.kind, 1033, resource.data.Span, (uint)resource.data.Length);
            if (!result)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateResource failed.");
            }
        }

        result = Win32.EndUpdateResource(updateHandle, false);
        if (!result)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "EndUpdateResource failed.");
        }
    }

    private static List<PeResource> FindResources(
        IntPtr libHandle,
        string[]? resourceTypes = null,
        string[]? resourceNames = null,
        ushort[]? resourceLanguages = null)
    {
        resourceTypes ??= [];
        resourceNames ??= [];
        resourceLanguages ??= [];
        var resourcesFound = new List<PeResource>();

        try
        {
            Win32.EnumResTypeProc enumResTypeCallback = (hModule, lpszType, _) =>
            {
                var type = Win32.ResourceToString(lpszType);
                if (!Win32.MatchesFilter(type, resourceTypes))
                {
                    return true;
                }
                
                Win32.EnumResNameProc enumResNameCallback = (hModuleName, _, lpName, _) =>
                {
                    var name = Win32.ResourceToString(lpName);
                    if (!Win32.MatchesFilter(name, resourceNames))
                    {
                        return true;
                    }
                
                    Win32.EnumResLangProc enumResLangCallback = (_, _, _, wIdLanguage, _) =>
                    {
                        if (!Win32.MatchesLanguage(wIdLanguage, resourceLanguages))
                        {
                            return true;
                        }
                
                        resourcesFound.Add(new PeResource(type, name, wIdLanguage));
                        return true;
                    };
                
                    if (!Win32.EnumResourceLanguages(hModuleName, lpszType, lpName, enumResLangCallback, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error != Win32.ERROR_NO_MORE_ITEMS)
                        {
                            throw new Win32Exception(error, "EnumResourceLanguages failed.");
                        }
                    }

                    return true;
                };

                if (!Win32.EnumResourceNames(hModule, lpszType, enumResNameCallback, IntPtr.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != Win32.ERROR_NO_MORE_ITEMS)
                    {
                        throw new Win32Exception(error, "EnumResourceNames failed.");
                    }
                }

                return true;
            };

            if (!Win32.EnumResourceTypes(libHandle, enumResTypeCallback, IntPtr.Zero))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != Win32.ERROR_NO_MORE_ITEMS)
                {
                    throw new Win32Exception(error, "EnumResourceTypes failed.");
                }
            }

            return resourcesFound;
        }
        finally
        {
            Win32.FreeLibrary(libHandle);
        }
    }
    
    private static AvaloniaBitmap ExtractIconFromFile(string filePath)
    {
        var iconHandle = Win32.ExtractIcon(IntPtr.Zero, filePath, 0);

        // null handle means there are no embedded icons
        if (iconHandle == IntPtr.Zero)
        {
            var shfi = new Win32.ShFileInfo();
            Win32.SHGetFileInfo(
                filePath,
                0,
                ref shfi,
                (uint)Marshal.SizeOf(typeof(Win32.ShFileInfo)),
                Win32.SHGFI_USEFILEATTRIBUTES | Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON
            );
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
            Win32.DestroyIcon(iconHandle);
        }
    }

    private static TargetFileData ExtractTargetFileData(string filePath)
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

    private static EngineType ExtractModern2k3Engine(string filePath)
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

    private static EngineType ExtractEngineFromVersionInfo(string filePath, FileVersionInfo versionInfo)
    {
        // check if it has a valid product version
        if (VersionRegex().Match(versionInfo.ProductVersion ?? string.Empty) is not { Success: true } match)
        {
            return default;
        }
        
        // groups are 1.XX.YY.ZZ but we only really need to check the major version 
        if (match.Groups is not [_, var majorVersion, _])
        {
            return default;
        }

        if (!int.TryParse(majorVersion.Value, out var major))
        {
            return default;
        }
            
        // versions 1.1.1.0 are Steam releases of 2k3 and considered modern
        return (major < 1) switch
        {
            true => EngineType.OldVanilla2k3,
            false => ExtractModern2k3Engine(filePath)
        };
    }
    
    private static EngineType ExtractEngineWithoutVersionInfo(string filePath)
    {
        var libHandle = Win32.LoadLibraryEx(filePath, IntPtr.Zero, Win32.LOAD_LIBRARY_AS_DATAFILE);
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

    private static partial class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x00000010;
        public const uint SHGFI_LARGEICON = 0x00000000;
        public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        public const int RT_RCDATA = 10;
        public const int ERROR_NO_MORE_ITEMS = 259;
        
        [StructLayout(LayoutKind.Sequential)]
        public struct ShFileInfo
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref ShFileInfo shfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

        [LibraryImport("kernel32.dll", EntryPoint = "UpdateResourceW", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, ReadOnlySpan<byte> lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        public delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpszType, IntPtr lParam);
        public delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);
        public delegate bool EnumResLangProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wIdLanguage, IntPtr lParam);

        // resource IDs are 16bit integers stored in pointer
        public static string ResourceToString(IntPtr resource) => (resource.ToInt64() >> 16) switch
        {
            0 => "#" + resource.ToInt32(),
            _ => Marshal.PtrToStringUni(resource)!
        };

        public static IntPtr StringToResource(string resource) => ushort.Parse(resource[1..], NumberStyles.None, CultureInfo.InvariantCulture);

        public static bool MatchesFilter(string value, string[] filters) => filters is [] || filters.Contains(value, StringComparer.OrdinalIgnoreCase);

        public static bool MatchesLanguage(ushort lang, ushort[] filters) => filters is [] || filters.Contains(lang);
    }
    

    [GeneratedRegex(@"1.([01]).(\d{1,2}).\d{1,2}", RegexOptions.Compiled)]
    private static partial Regex VersionRegex();
}