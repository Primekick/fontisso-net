using System;
using System.ComponentModel;
using System.IO;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Services.Patching;

namespace Fontisso.NET.Services;

public interface IPatchingService
{
    OperationResult PatchExecutable(TargetFileData tfd, ReadOnlyMemory<byte> rpg2000Data,
        ReadOnlyMemory<byte> rpg2000GData);
}

public class PatchingService(PatchingStrategyContext strategyContext) : IPatchingService
{
    public OperationResult PatchExecutable(TargetFileData tfd, ReadOnlyMemory<byte> rpg2000Data,
        ReadOnlyMemory<byte> rpg2000GData)
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
            var strategy = strategyContext.GetStrategy(tfd.Engine);
            strategy.Patch(tfd.TargetFilePath, rpg2000Data, rpg2000GData);
        }
        catch (Exception e)
        {
            File.Replace(backupFilePath, tfd.TargetFilePath, null);
            return e switch
            {
                Win32Exception w32e => OperationResult.ErrorResult(string.Format(I18n.UI.Error_CannotPatchWin32,
                    w32e.NativeErrorCode, w32e.Message)),
                _ => OperationResult.ErrorResult(string.Format(I18n.UI.Error_CannotPatch, e.Message)),
            };
        }

        return OperationResult.OkResult(string.Format(I18n.UI.Success_Patched, Path.GetFileName(backupFilePath)));
    }
}