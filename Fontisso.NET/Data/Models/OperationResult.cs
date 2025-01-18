namespace Fontisso.NET.Data.Models;

public record OperationResult(string Title, string Content)
{
    public static OperationResult OkResult(string content) => new(I18n.UI.Dialog_Info, content);
    public static OperationResult ErrorResult(string content) => new(I18n.UI.Dialog_Error, content);
}