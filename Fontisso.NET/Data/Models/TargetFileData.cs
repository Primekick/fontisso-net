using Avalonia.Media.Imaging;

namespace Fontisso.NET.Data.Models;

public enum ExtractionError
{
    NotRm2k3,
    EngineTooOld,
}

public record TargetFileData(
    string FileName,
    Bitmap? FileIcon,
    bool HasFile,
    string TargetFilePath,
    EngineType Engine
)
{
    public static TargetFileData Default => new(
        string.Empty,
        null,
        false,
        string.Empty,
        EngineType.Undefined
    );
}