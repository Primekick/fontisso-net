using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Fontisso.NET.Data.Models;

namespace Fontisso.NET.Services.Patching;

public sealed class PatchingStrategyContext
{
    private readonly FrozenDictionary<HashSet<EngineType>, IPatchingStrategy> _strategyMap;

    public PatchingStrategyContext(IEnumerable<EnginePatchingMapping> mappings)
    {
        _strategyMap = mappings.ToFrozenDictionary(
            tuple => tuple.Engines.ToHashSet(),
            tuple => tuple.Strategy,
            HashSet<EngineType>.CreateSetComparer()
        );
    }

    public IPatchingStrategy GetStrategy(EngineType engineType) =>
        _strategyMap.FirstOrDefault(kvp => kvp.Key.Contains(engineType)).Value
        ?? throw new NotSupportedException($"No strategy found for engine type {engineType}");
}