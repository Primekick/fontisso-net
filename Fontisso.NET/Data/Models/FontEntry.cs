using System;

namespace Fontisso.NET.Data.Models;

public static class FontKindExtensions
{
    public static ReadOnlySpan<byte> AsByteSpan(this FontKind fontKind) => fontKind switch
    {
        FontKind.RPG2000 => "RPG2000"u8,
        FontKind.RPG2000G => "RPG2000G"u8,
        _ => throw new InvalidOperationException()
    };
}

public enum FontKind
{
    RPG2000 = 100,
    RPG2000G = 101
}

public record FontEntry(string Name, string Attribution, ReadOnlyMemory<byte> Rpg2000Data, ReadOnlyMemory<byte> Rpg2000GData);