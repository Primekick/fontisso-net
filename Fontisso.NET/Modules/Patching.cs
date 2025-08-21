using System;

namespace Fontisso.NET.Modules;

public static class Patching
{
    public readonly record struct LegacyPatchConfig(
        string DllName,
        string FontsDir,
        (string FileName, byte[] Face) SlotA,
        (string FileName, byte[] Face) SlotB,
        (byte[] Old, byte[] New)[] Rewrites
    );

    public static readonly Lazy<LegacyPatchConfig> LegacyPatchingConfig = new(
        () => new()
        {
            DllName = "Fontisso.NET.LegacyFontLoader.dll",
            FontsDir = "Fonts",
            SlotA = ("RPG2000.fon", "Cstm01"u8.ToArray()),
            SlotB = ("RPG2000G.fon", "Cstm02"u8.ToArray()),
            Rewrites =
            [
                ("MS Mincho"u8.ToArray(), "Cstm01"u8.ToArray()),
                ("MS Gothic"u8.ToArray(), "Cstm02"u8.ToArray()),
                ("RM2000"u8.ToArray(), "Cstm01"u8.ToArray()),
                ("RMG2000"u8.ToArray(), "Cstm02"u8.ToArray()),
            ]
        },
        isThreadSafe: true
    );
}