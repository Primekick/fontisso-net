namespace Fontisso.NET.Data.Models;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

public enum ExtractionError
{
    NotRm2kX,
    EngineTooOld,
}

public record TargetFileData(
    string FileName,
    AvaloniaBitmap? FileIcon,
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