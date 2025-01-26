using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Fontisso.NET.Helpers;

public static class FileExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OpenAndWrite(string path, ReadOnlySpan<byte> buffer)
    {
        using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
        writer.Write(buffer);
    }
}