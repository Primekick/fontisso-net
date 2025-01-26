using System;
using System.Text;
using Fontisso.NET.Data.Models;

namespace Fontisso.NET.Services.Metadata;

public interface IFontMetadataProcessor
{
    string ExtractModuleName(ReadOnlySpan<byte> data);
    string ExtractAttribution(ReadOnlySpan<byte> data);
    byte[] SetFaceNameByFontKind(byte[] data, FontKind kind);
}

public sealed class FontMetadataProcessor : IFontMetadataProcessor
{
    public string ExtractModuleName(ReadOnlySpan<byte> data)
    {
        var residentNameTableOffset = ExtractOffsetFromNeHeader(data, 0x26);
        // first byte = length of the name, first entry in the table = module name
        var nameLength = data[residentNameTableOffset];
        return Encoding.ASCII.GetString(data.Slice(residentNameTableOffset + 0x1, nameLength));
    }

    public string ExtractAttribution(ReadOnlySpan<byte> data) =>
        ExtractOffsetToResourceDirectoryEntry(data, 0x8008) switch
        {
            0 => "---",
            // copyright section is a static 60-char array
            var offset => Encoding.ASCII.GetString(data.Slice(offset + 0x6, 60)).Trim()
        };

    public byte[] SetFaceNameByFontKind(byte[] data, FontKind kind)
    {
        var newName = kind switch
        {
            FontKind.RPG2000 => "CstmFnt01",
            FontKind.RPG2000G => "CstmFnt02",
            _ => throw new InvalidOperationException()
        };

        var newData = (byte[])data.Clone();
        var dataSpan = newData.AsSpan();
        var fontDirOffset = ExtractOffsetToResourceDirectoryEntry(dataSpan, 0x8007);
        // FONTGROUPHDR size + szFaceName offset (assuming szDeviceName is null)
        var faceNameOffset = fontDirOffset + 0x4 + 0x72;
        var targetSpan = dataSpan.Slice(faceNameOffset, newName.Length + 1);

        targetSpan.Clear();
        Encoding.ASCII.GetBytes(newName).AsSpan().CopyTo(targetSpan);
        return newData;
    }

    private int ExtractOffsetToResourceDirectoryEntry(ReadOnlySpan<byte> data, ushort typeId)
    {
        var resourceTableOffset = ExtractOffsetFromNeHeader(data, 0x24);
        // represents the amounts of bits to shift to the left to obtain the real resource offset later
        var shift = BitConverter.ToUInt16(data.Slice(resourceTableOffset));
        var tablePointer = resourceTableOffset + 0x2;

        while (true)
        {
            var resourceTypeId = BitConverter.ToUInt16(data.Slice(tablePointer));
            if (resourceTypeId == 0)
            {
                break;
            }
            
            if (resourceTypeId == typeId)
            {
                // this offset is relative to beginning of file
                return BitConverter.ToUInt16(data.Slice(tablePointer + 0x8)) << shift;
            }
            
            // ResourceTableEntry size + number of entries * ResourceEntry size
            tablePointer += 0x8 + BitConverter.ToUInt16(data.Slice(tablePointer + 0x2)) * 0xC;
        }

        return 0;
    }

    private int ExtractOffsetFromNeHeader(ReadOnlySpan<byte> data, int offset)
    {
        var neHeaderOffset = BitConverter.ToInt32(data.Slice(0x3C));
        return neHeaderOffset + BitConverter.ToUInt16(data.Slice(neHeaderOffset + offset));
    }
}