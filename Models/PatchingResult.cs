namespace Fontisso.NET.Models;

public record PatchingResult(string Title, string Content)
{
    public static PatchingResult OkResult(string content) => new("Info", content);
    public static PatchingResult ErrorResult(string content) => new("Błąd", content);
}