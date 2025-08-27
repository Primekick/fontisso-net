using Fontisso.NET.Modules;

namespace Fontisso.NET.Services.Patching;

public record EnginePatchingMapping(IPatchingStrategy Strategy, Resources.EngineType[] Engines);