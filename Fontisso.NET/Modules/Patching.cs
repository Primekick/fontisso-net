using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using PeNet;

namespace Fontisso.NET.Modules;

public static class Patching
{
    public record struct OperationResult(string Title, string Content)
    {
        public static OperationResult OkResult(string content) => new(I18n.UI.Dialog_Info, content);
        public static OperationResult ErrorResult(string content) => new(I18n.UI.Dialog_Error, content);
    }
    
    public readonly record struct LegacyPatchConfig(
        string DllName,
        string FontsDir,
        (string FileName, byte[] Face) SlotA,
        (string FileName, byte[] Face) SlotB,
        (byte[] Old, byte[] New)[] Rewrites
    );

    public static OperationResult PatchExecutable(Resources.TargetFileData tfd, ReadOnlySpan<byte> rpg2000Data,
        ReadOnlySpan<byte> rpg2000GData)
    {
        if (!File.Exists(tfd.TargetFilePath))
        {
            return OperationResult.ErrorResult(string.Format(I18n.UI.Error_FileNotFound, tfd.FileName));
        }

        var backupFilePath = $"{tfd.TargetFilePath}.{DateTime.Now:yyyyMMdd_HHmmss}.old";
        try
        {
            File.Copy(tfd.TargetFilePath, backupFilePath);
        }
        catch
        {
            return OperationResult.ErrorResult(string.Format(I18n.UI.Error_CannotCreateBackup, backupFilePath));
        }

        try
        {
            PatchingAction applyPatch = tfd.Engine switch
            {
                Resources.EngineType.Vanilla2k or Resources.EngineType.OldVanilla2k3 => PatchLegacy,
                Resources.EngineType.ModernVanilla2k3 or Resources.EngineType.OldManiacs or Resources.EngineType.ModernManiacs => PatchModern,
                _ => throw new ArgumentOutOfRangeException(nameof(tfd.Engine))
            };
            applyPatch(tfd.TargetFilePath, rpg2000Data, rpg2000GData);
        }
        catch (Exception e)
        {
            File.Replace(backupFilePath, tfd.TargetFilePath, null);
            return e switch
            {
                Win32Exception w32e => OperationResult.ErrorResult(string.Format(I18n.UI.Error_CannotPatchWin32, w32e.NativeErrorCode, w32e.Message)),
                _ => OperationResult.ErrorResult(string.Format(I18n.UI.Error_CannotPatch, e.Message)),
            };
        }

        return OperationResult.OkResult(string.Format(I18n.UI.Success_Patched, Path.GetFileName(backupFilePath)));
    }

    private delegate void PatchingAction(string filePath, ReadOnlySpan<byte> rpg2000Data, ReadOnlySpan<byte> rpg2000GData);
    
    private static void PatchLegacy(string filePath, ReadOnlySpan<byte> rpg2000Data, ReadOnlySpan<byte> rpg2000GData)
    {
        var config = LegacyPatchingConfig.Value;
        
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
        var binary = peFile.RawFile.ToArray();
        foreach (var (oldName, newName) in config.Rewrites)
        {
            while (binary.TryReplace(oldName, newName)) { }
        }
        
        File.WriteAllBytes(filePath, binary);
    }

    private static void PatchModern(string filePath, ReadOnlySpan<byte> rpg2000Data, ReadOnlySpan<byte> rpg2000GData)
    {
        var resources = new (Fonts.FontKind kind, ReadOnlyMemory<byte> data)[] { (Fonts.FontKind.Rpg2000, !rpg2000Data), (Fonts.FontKind.Rpg2000G, !rpg2000GData) };
        Resources.WriteResources(filePath, resources);
    }
    
    private static readonly Lazy<LegacyPatchConfig> LegacyPatchingConfig = new(
        () => new()
        {
            DllName = "Fontisso.NET.LegacyFontLoader.dll",
            FontsDir = "Fonts",
            SlotA = ("RPG2000.fon", !"Cstm01"u8),
            SlotB = ("RPG2000G.fon", !"Cstm02"u8),
            Rewrites =
            [
                (!"MS Mincho"u8, !"Cstm01"u8),
                (!"MS Gothic"u8, !"Cstm02"u8),
                (!"RM2000"u8, !"Cstm01"u8),
                (!"RMG2000"u8, !"Cstm02"u8),
            ]
        },
        isThreadSafe: true
    );
}