using System;
using System.IO;
using System.Linq;
using Fontisso.NET.Configuration.Patching;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Helpers;
using Fontisso.NET.Services.Metadata;
using PeNet;

namespace Fontisso.NET.Services.Patching;

public interface IPatchingStrategy
{
    void Patch(string filePath, ReadOnlyMemory<byte> rpg2000Data, ReadOnlyMemory<byte> rpg2000GData);
}

public sealed class LegacyPatchingStrategy(LegacyPatchingConfig config, IFontMetadataProcessor fontMetadata)
    : IPatchingStrategy
{
    public void Patch(string filePath, ReadOnlyMemory<byte> rpg2000Data, ReadOnlyMemory<byte> rpg2000GData)
    {
        var exeRootDir = Path.GetDirectoryName(filePath)!;
        var currentDir = Directory.GetCurrentDirectory();
        File.Copy(
            Path.Combine(currentDir, config.LegacyLoaderDllName),
            Path.Combine(exeRootDir, config.LegacyLoaderDllName),
            true
        );

        var customFontDataA = fontMetadata.SetFaceName(rpg2000Data, config.CustomFontNameA.Span);
        var customFontDataB = fontMetadata.SetFaceName(rpg2000GData, config.CustomFontNameB.Span);
        Directory.CreateDirectory(Path.Combine(exeRootDir, config.FontsDirectory));
        FileExtensions.OpenAndWrite(
            Path.Combine(exeRootDir, config.FontsDirectory, config.FontFileNameA),
            customFontDataA.Span
        );
        FileExtensions.OpenAndWrite(
            Path.Combine(exeRootDir, config.FontsDirectory, config.FontFileNameB),
            customFontDataB.Span
        );

        var peFile = new PeFile(filePath);
        if (peFile.ImportedFunctions!.All(func => func.DLL != config.LegacyLoaderDllName))
        {
            peFile.AddImport(config.LegacyLoaderDllName, "Dummy");
        }

        var binary = peFile.RawFile.ToArray().AsSpan();
        // sometimes the builtin font names appear more than once in the game binary hence the looped replacement calls - needs more research
        while (binary.TryReplace(config.BuiltinFontNameA.Span, config.CustomFontNameA.Span));
        while (binary.TryReplace(config.BuiltinFontNameB.Span, config.CustomFontNameB.Span));
        FileExtensions.OpenAndWrite(filePath, binary);
    }
}

public sealed class ModernPatchingStrategy(IResourceService resourceService) : IPatchingStrategy
{
    public void Patch(string filePath, ReadOnlyMemory<byte> rpg2000Data, ReadOnlyMemory<byte> rpg2000GData)
    {
        var resources = new[] { (FontKind.RPG2000, rpg2000Data), (FontKind.RPG2000G, rpg2000GData) };
        resourceService.WriteResources(filePath, resources);
    }
}