namespace Fontisso.NET.Data.Models;

public enum FontKind
{
    RPG2000 = 100,
    RPG2000G = 101
}

public record FontEntry(string Name, string Attribution, byte[] Rpg2000Data, byte[] Rpg2000GData);