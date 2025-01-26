using System.Drawing;

namespace Fontisso.NET.Data.Models.Rendering;

public record FontRenderOptions(
    float FontSize, 
    Color TextColor, 
    Color BackgroundColor, 
    int Width
);