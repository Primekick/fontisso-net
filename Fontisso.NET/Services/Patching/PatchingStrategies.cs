using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
        // dump the loader dll into the game's folder
        var exeRootDir = Path.GetDirectoryName(filePath)!;
        var targetDllFileName = Path.Combine(exeRootDir, config.LegacyLoaderDllName);
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LegacyFontLoader")!)
        using (var fileStream = new FileStream(targetDllFileName, FileMode.Create))
        {
            stream.CopyTo(fileStream);
        }

        // before dumping the fonts, change their face name to a pre-set one to match the face name requested by the game
        Directory.CreateDirectory(Path.Combine(exeRootDir, config.FontsDirectory));
        var customFontDataA = fontMetadata.SetFaceName(rpg2000Data, config.CustomFontNameA.Span);
        var customFontDataB = fontMetadata.SetFaceName(rpg2000GData, config.CustomFontNameB.Span);
        FileExtensions.OpenAndWrite(
            Path.Combine(exeRootDir, config.FontsDirectory, config.FontFileNameA),
            customFontDataA.Span
        );
        FileExtensions.OpenAndWrite(
            Path.Combine(exeRootDir, config.FontsDirectory, config.FontFileNameB),
            customFontDataB.Span
        );
        
        // add dll import with a dummy function target so that the dll gets loaded on game boot
        var peFile = new PeFile(filePath);
        if (peFile.ImportedFunctions!.All(func => func.DLL != config.LegacyLoaderDllName))
        {
            peFile.AddImport(config.LegacyLoaderDllName, "Dummy");
        }

        // sometimes the builtin font names appear more than once in the game binary, hence the looped TryReplace calls
        // needs more research
        var binary = peFile.RawFile.ToArray().AsSpan();
        foreach (var fontName in config.BuiltinFontNamesA)
        {
            while (binary.TryReplace(fontName.Span, config.CustomFontNameA.Span));
        }
        foreach (var fontName in config.BuiltinFontNamesB)
        {
            while (binary.TryReplace(fontName.Span, config.CustomFontNameB.Span));
        }
        
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