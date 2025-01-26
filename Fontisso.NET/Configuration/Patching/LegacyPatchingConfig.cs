using System;

namespace Fontisso.NET.Configuration.Patching;

public sealed record LegacyPatchingConfig(
    string LegacyLoaderDllName,
    string FontsDirectory,
    string FontFileNameA,
    string FontFileNameB,
    ReadOnlyMemory<byte> BuiltinFontNameA,
    ReadOnlyMemory<byte> BuiltinFontNameB,
    ReadOnlyMemory<byte> CustomFontNameA,
    ReadOnlyMemory<byte> CustomFontNameB
);