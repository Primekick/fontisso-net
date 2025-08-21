using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Fontisso.NET.Modules;
using PeNet;

namespace Fontisso.NET.Services.Patching;

public interface IPatchingStrategy
{
    void Patch(string filePath, ReadOnlySpan<byte> rpg2000Data, ReadOnlySpan<byte> rpg2000GData);
}

public sealed class LegacyPatchingStrategy() : IPatchingStrategy
{
    public void Patch(string filePath, ReadOnlySpan<byte> rpg2000Data, ReadOnlySpan<byte> rpg2000GData)
    {
        var config = Modules.Patching.LegacyPatchingConfig.Value;
        // dump the loader dll into the game's folder
        var exeRootDir = Path.GetDirectoryName(filePath)!;
        var targetDllFileName = Path.Combine(exeRootDir, config.DllName);
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LegacyFontLoader")!)
        using (var fileStream = new FileStream(targetDllFileName, FileMode.Create))
        {
            stream.CopyTo(fileStream);
        }

        // before dumping the fonts, change their face name to a pre-set one to match the face name requested by the game
        Directory.CreateDirectory(Path.Combine(exeRootDir, config.FontsDir));
        var dataSlotA = Fonts.Metadata.ApplyFaceName(rpg2000Data, config.SlotA.Face);
        var dataSlotB = Fonts.Metadata.ApplyFaceName(rpg2000GData, config.SlotB.Face);
        File.WriteAllBytes(
            Path.Combine(exeRootDir, config.FontsDir, config.SlotA.FileName),
            dataSlotA
        );
        File.WriteAllBytes(
            Path.Combine(exeRootDir, config.FontsDir, config.SlotB.FileName),
            dataSlotB
        );
        
        // add dll import with a dummy function target so that the dll gets loaded on game boot
        var peFile = new PeFile(filePath);
        if (peFile.ImportedFunctions!.All(func => func.DLL != config.DllName))
        {
            peFile.AddImport(config.DllName, "Dummy");
        }

        // sometimes the builtin font names appear more than once in the game binary, hence the looped TryReplace calls
        // needs more research
        var binary = peFile.RawFile.ToArray().AsSpan();
        foreach (var (oldName, newName) in config.Rewrites)
        {
            while (binary.TryReplace(oldName, newName)) { }
        }
        
        File.WriteAllBytes(filePath, binary);
    }
}

public sealed class ModernPatchingStrategy(IResourceService resourceService) : IPatchingStrategy
{
    public void Patch(string filePath, ReadOnlySpan<byte> rpg2000Data, ReadOnlySpan<byte> rpg2000GData)
    {
        var resources = new (Fonts.FontKind kind, ReadOnlyMemory<byte> data)[] { (Fonts.FontKind.Rpg2000, rpg2000Data.ToArray()), (Fonts.FontKind.Rpg2000G, rpg2000GData.ToArray()) };
        resourceService.WriteResources(filePath, resources);
    }
}