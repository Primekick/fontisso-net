using System.Diagnostics;
using Avalonia.Data.Converters;

namespace Fontisso.NET.Data.Models;

public enum EngineType
{
    Undefined,
    Vanilla2k,
    OldVanilla2k3,
    ModernVanilla2k3,
    OldManiacs,
    ModernManiacs
}

public static class EngineTypeConverter
{
    public static FuncValueConverter<EngineType, string> AsString { get; } =
        new(engineType => engineType switch
        {
            EngineType.ModernVanilla2k3 => I18n.UI.EngineType_ModernVanilla2k3,
            EngineType.OldVanilla2k3 => I18n.UI.EngineType_OldVanilla2k3,
            EngineType.Vanilla2k => I18n.UI.EngineType_Vanilla2k,
            EngineType.OldManiacs => I18n.UI.EngineType_OldManiacs,
            EngineType.ModernManiacs => I18n.UI.EngineType_ModernManiacs,
            _ => throw new UnreachableException()
        });
}