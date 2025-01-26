using System.Text;

namespace Fontisso.NET.Configuration.Patching;

public sealed class LegacyPatchingConfigBuilder
{
    private string _legacyLoaderDllName;
    private string _fontsDirectory;
    private (string, string) _fontFileNames;
    private (string, string) _builtinFontNames;
    private (string, string) _customFontNames;
    
    public LegacyPatchingConfigBuilder WithLegacyLoaderDllName(string dllName)
    {
        _legacyLoaderDllName = dllName;
        return this;
    }

    public LegacyPatchingConfigBuilder WithFontsDirectory(string directory)
    {
        _fontsDirectory = directory;
        return this;
    }
    
    public LegacyPatchingConfigBuilder WithFontFileNames((string, string) names)
    {
        _fontFileNames = names;
        return this;
    }

    public LegacyPatchingConfigBuilder WithBuiltinFontNames((string, string) names)
    {
        _builtinFontNames = names;
        return this;
    }

    public LegacyPatchingConfigBuilder WithCustomFontNames((string, string) names)
    {
        _customFontNames = names;
        return this;
    }

    public LegacyPatchingConfig Build()
    {
        return new LegacyPatchingConfig(
            LegacyLoaderDllName: _legacyLoaderDllName,
            FontsDirectory: _fontsDirectory,
            FontFileNameA: _fontFileNames.Item1,
            FontFileNameB: _fontFileNames.Item2,
            BuiltinFontNameA: Encoding.ASCII.GetBytes(_builtinFontNames.Item1),
            BuiltinFontNameB: Encoding.ASCII.GetBytes(_builtinFontNames.Item2),
            CustomFontNameA: Encoding.ASCII.GetBytes(_customFontNames.Item1),
            CustomFontNameB: Encoding.ASCII.GetBytes(_customFontNames.Item2)
        );
    }
}