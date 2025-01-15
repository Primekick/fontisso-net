using System.IO;

namespace Fontisso.NET.Helpers;

public static class StreamExtensions
{
    public static byte[] ReadToByteArray(this Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        
        return ms.ToArray();
    }
}