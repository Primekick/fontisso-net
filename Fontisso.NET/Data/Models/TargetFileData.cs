namespace Fontisso.NET.Data.Models;

using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

public record struct TargetFileData(
    string FileName,
    AvaloniaBitmap? FileIcon,
    string TargetFilePath,
    EngineType Engine
);