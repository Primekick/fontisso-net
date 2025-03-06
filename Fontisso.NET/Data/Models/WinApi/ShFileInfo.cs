using System;
using System.Runtime.InteropServices;

namespace Fontisso.NET.Data.Models.Metadata;

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