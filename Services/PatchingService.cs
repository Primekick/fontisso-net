using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Fontisso.NET.Models;

namespace Fontisso.NET.Services;

public interface IPatchingService
{
    Task<PatchingResult> PatchExecutable(TargetFileData tfd, byte[] fontData);
}

public class PatchingService : IPatchingService
{
    private readonly IResourceService _resourceService;

    public PatchingService(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<PatchingResult> PatchExecutable(TargetFileData tfd, byte[] fontData)
    {
        if (!File.Exists(tfd.TargetFilePath))
        {
            return PatchingResult.ErrorResult($"Plik {tfd.FileName} nie istnieje.");
        }

        var backupFilePath = $"{tfd.TargetFilePath}_{DateTime.Now:yyyyMMdd_HHmmss}.old";
        try
        {
            File.Copy(tfd.TargetFilePath, backupFilePath);
        }
        catch
        {
            return PatchingResult.ErrorResult($"Nie można stworzyć kopii zapasowej: {backupFilePath}");
        }

        try
        {
            await _resourceService.WriteResource(tfd.TargetFilePath, 100, fontData);
            await _resourceService.WriteResource(tfd.TargetFilePath, 101, fontData);
        }
        catch (Exception e)
        {
            File.Replace(backupFilePath, tfd.TargetFilePath, null);
            return e switch
            {
                Win32Exception w32e => PatchingResult.ErrorResult($"Nie można spatchować pliku (kod {w32e.NativeErrorCode}): {w32e.Message}"),
                _ => PatchingResult.ErrorResult($"Nie można spatchować pliku: {e.Message}"),
            };
        }
        
        return PatchingResult.OkResult($"Spatchowano pomyślnie! Utworzono backup: {Path.GetFileName(backupFilePath)}");
    }
}