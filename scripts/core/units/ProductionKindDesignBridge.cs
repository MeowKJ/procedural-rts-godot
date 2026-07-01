namespace ProceduralRts.Core;

public static class ProductionKindDesignBridge
{
    public static UnitFactionId UnitFactionFor(FactionId factionId)
    {
        return factionId switch
        {
            FactionId.Dog => UnitFactionId.Dog,
            FactionId.Cat => UnitFactionId.Cat,
            FactionId.Corruption => UnitFactionId.Corruption,
            _ => UnitFactionId.Dog,
        };
    }

    public static ProductionKind ProductionKindFor(UnitSpec spec)
    {
        return spec.Archetype switch
        {
            UnitArchetype.Harvester => ProductionKind.Harvester,
            UnitArchetype.PatrolVehicle
                or UnitArchetype.GuardTank
                or UnitArchetype.RepairSupport
                or UnitArchetype.ShieldVehicle
                or UnitArchetype.SiegeArtillery
                or UnitArchetype.AssaultTank
                or UnitArchetype.ScoutAircraft => ProductionKind.LightTank,
            _ => ProductionKind.InfantrySquad,
        };
    }

    public static UnitSpec SpecFor(FactionId factionId, ProductionKind kind)
    {
        return SpecFor(UnitFactionFor(factionId), kind);
    }

    public static UnitSpec SpecFor(UnitFactionId faction, ProductionKind kind)
    {
        if (TrySpecFor(faction, kind, out var spec))
        {
            return spec;
        }

        throw new KeyNotFoundException($"No UnitDesign is available for {faction} / {kind}.");
    }

    public static bool TrySpecFor(FactionId factionId, ProductionKind kind, out UnitSpec spec)
    {
        return TrySpecFor(UnitFactionFor(factionId), kind, out spec);
    }

    public static bool TrySpecFor(UnitFactionId faction, ProductionKind kind, out UnitSpec spec)
    {
        var designId = UnitDesignRuntimeLoadouts.ProductionDesignId(faction, kind);
        if (designId is null)
        {
            spec = null!;
            return false;
        }

        spec = UnitDesignCatalog.Spec(designId);
        return true;
    }

    public static IEnumerable<UnitSpec> PlayableProductionSpecs(params UnitFactionId[] factions)
    {
        foreach (var faction in factions)
        {
            foreach (var designId in UnitDesignFactionRosterCatalog.For(faction).PlayableDesignIds)
            {
                var spec = UnitDesignCatalog.Spec(designId);
                if (spec.Production is not null)
                {
                    yield return spec;
                }
            }
        }
    }

    public static float DurationLimitFor(FactionId factionId, ProductionKind kind)
    {
        return DurationLimitFor(UnitFactionFor(factionId), kind);
    }

    public static float DurationLimitFor(UnitFactionId faction, ProductionKind kind)
    {
        var durations = PlayableProductionSpecs(faction)
            .Where(spec => ProductionKindFor(spec) == kind)
            .Select(spec => spec.Production!.Duration)
            .ToArray();

        if (durations.Length == 0)
        {
            throw new KeyNotFoundException($"No UnitDesign production duration is available for {faction} / {kind}.");
        }

        return durations.Max();
    }
}
