using System.Diagnostics;
using Avalonia.Data.Converters;

namespace Fontisso.NET.Data.Models;

public enum EngineType
{
    Undefined,
    Vanilla,
    OldManiacs,
    ModernManiacs
}

public static class EngineTypeConverter
{
    public static FuncValueConverter<EngineType, string> AsString { get; } =
        new(engineType => engineType switch
        {
            EngineType.Vanilla => I18n.UI.EngineType_Vanilla,
            EngineType.OldManiacs => I18n.UI.EngineType_OldManiacs,
            EngineType.ModernManiacs => I18n.UI.EngineType_ModernManiacs,
            _ => throw new UnreachableException()
        });
}