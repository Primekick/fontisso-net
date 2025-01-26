namespace Fontisso.NET.Configuration.Patching;

public sealed record LegacyPatchingConfig(
    string LegacyLoaderDllName,
    string FontsDirectory,
    string FontFileNameA,
    string FontFileNameB,
    string BuiltinFontNameA,
    string BuiltinFontNameB,
    string CustomFontNameA,
    string CustomFontNameB
);