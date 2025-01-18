namespace Fontisso.NET.Data.Models;

public enum FontKind
{
    RPG2000,
    RPG2000G
}

public record FontEntry(FontKind Kind, byte[] Data, string Details);