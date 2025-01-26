using Fontisso.NET.Data.Models;

namespace Fontisso.NET.Services.Patching;

public record EnginePatchingMapping(IPatchingStrategy Strategy, EngineType[] Engines);