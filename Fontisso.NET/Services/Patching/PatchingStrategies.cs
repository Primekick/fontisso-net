using System;
using Fontisso.NET.Configuration.Patching;

namespace Fontisso.NET.Services.Patching;

public interface IPatchingStrategy
{
    void Patch(string filePath, ReadOnlyMemory<byte> rpg2000Data, ReadOnlyMemory<byte> rpg2000GData);
}

public sealed class LegacyPatchingStrategy(LegacyPatchingConfig config) : IPatchingStrategy
{
    public void Patch(string filePath, ReadOnlyMemory<byte> rpg2000Data, ReadOnlyMemory<byte> rpg2000GData)
    {
        throw new NotImplementedException();
    }
}

public sealed class ModernPatchingStrategy(IResourceService resourceService) : IPatchingStrategy
{
    public void Patch(string filePath, ReadOnlyMemory<byte> rpg2000Data, ReadOnlyMemory<byte> rpg2000GData)
    {
        throw new NotImplementedException();
    }
}