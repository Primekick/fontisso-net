using System;
using System.Collections.Generic;

namespace Fontisso.NET.Configuration.Patching;

public sealed record LegacyPatchingConfig(
    string LegacyLoaderDllName,
    string FontsDirectory,
    string FontFileNameA,
    string FontFileNameB,
    IEnumerable<ReadOnlyMemory<byte>> BuiltinFontNamesA,
    IEnumerable<ReadOnlyMemory<byte>> BuiltinFontNamesB,
    ReadOnlyMemory<byte> CustomFontNameA,
    ReadOnlyMemory<byte> CustomFontNameB
);