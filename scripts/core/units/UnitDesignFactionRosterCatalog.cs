using Godot;

namespace ProceduralRts.Core;

public sealed record UnitDesignFactionRoster(
    UnitFactionId Faction,
    IReadOnlyList<string> PlayableDesignIds,
    IReadOnlyList<UnitDesignSpawn> StartingUnits)
{
    public IReadOnlyList<string> StartingDesignIds => StartingUnits.Select(unit => unit.DesignId).ToArray();
}

public static class UnitDesignFactionRosterCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<UnitFactionId, UnitDesignFactionRoster>> DiscoveredRosters = new(BuildRosters);

    private static readonly IReadOnlyDictionary<UnitFactionId, IReadOnlyList<UnitDesignSpawn>> StartingUnitsByFaction =
        new Dictionary<UnitFactionId, IReadOnlyList<UnitDesignSpawn>>
        {
            [UnitFactionId.Dog] =
            [
                new("dog.guard_tank", Vector2.Zero),
                new("dog.guard_tank", new Vector2(72, 38), 0.15f),
                new("dog.patrol_vehicle", new Vector2(-72, 44), -0.1f),
                new("dog.infantry", new Vector2(-48, 128)),
                new("dog.infantry", new Vector2(2, 150)),
                new("dog.rocket", new Vector2(54, 132)),
                new("dog.harvester", new Vector2(130, -58), 0.2f),
            ],
            [UnitFactionId.Cat] =
            [
                new("cat.tank", Vector2.Zero),
                new("cat.tank", new Vector2(80, 54), -0.12f),
                new("cat.scout_car", new Vector2(-70, 58), 0.08f),
                new("cat.basic", new Vector2(-44, 136)),
                new("cat.basic", new Vector2(12, 160)),
                new("cat.basic", new Vector2(64, 136)),
                new("cat.harvester", new Vector2(132, -54), 0.18f),
            ],
        };

    public static IReadOnlyDictionary<UnitFactionId, UnitDesignFactionRoster> Rosters => DiscoveredRosters.Value;

    public static UnitDesignFactionRoster For(UnitFactionId faction)
    {
        return Rosters[faction];
    }

    public static IReadOnlyList<UnitDesignSpawn> StartingUnits(UnitFactionId faction)
    {
        return For(faction).StartingUnits;
    }

    public static string? ProductionDesignId(UnitFactionId faction, ProductionSpec production)
    {
        var preferredArchetype = PreferredArchetype(production.Category);
        UnitSpec? best = null;
        var bestPreference = int.MaxValue;
        foreach (var designId in For(faction).PlayableDesignIds)
        {
            var spec = UnitDesignCatalog.Spec(designId);
            if (spec.Production is not { } specProduction
                || specProduction.ProducerKind != production.ProducerKind
                || specProduction.Category != production.Category
                || specProduction.LaneIndex != production.LaneIndex)
            {
                continue;
            }

            var preference = preferredArchetype is not null && spec.Archetype != preferredArchetype ? 1 : 0;
            if (IsBetterProductionOption(spec, best, preference, bestPreference))
            {
                best = spec;
                bestPreference = preference;
            }
        }

        return best?.Id;
    }

    public static string? ProductionDesignId(UnitFactionId faction, ProductionKind productionKind)
    {
        var archetype = PreferredArchetype(productionKind);
        UnitSpec? best = null;
        foreach (var designId in For(faction).PlayableDesignIds)
        {
            var spec = UnitDesignCatalog.Spec(designId);
            if (spec.Archetype == archetype
                && spec.Production is not null
                && IsBetterProductionOption(spec, best, 0, 0))
            {
                best = spec;
            }
        }

        return best?.Id;
    }

    private static IReadOnlyDictionary<UnitFactionId, UnitDesignFactionRoster> BuildRosters()
    {
        return Enum.GetValues<UnitFactionId>()
            .ToDictionary(
                faction => faction,
                faction =>
                {
                    var playableIds = UnitDesignCatalog.Designs.Values
                        .Where(design => design.Faction == faction && design.Production is not null)
                        .OrderBy(design => design.Stats.TechTier)
                        .ThenBy(design => design.Production!.ProducerKind)
                        .ThenBy(design => design.Production!.Category)
                        .ThenBy(design => design.Production!.LaneIndex)
                        .ThenBy(design => design.Id)
                        .Select(design => design.Id)
                        .ToArray();
                    var startingUnits = StartingUnitsByFaction.TryGetValue(faction, out var units) ? units : [];
                    ValidateStartingUnits(faction, startingUnits);
                    return new UnitDesignFactionRoster(faction, playableIds, startingUnits);
                });
    }

    private static void ValidateStartingUnits(UnitFactionId faction, IReadOnlyList<UnitDesignSpawn> startingUnits)
    {
        foreach (var startingUnit in startingUnits)
        {
            var spec = UnitDesignCatalog.Spec(startingUnit.DesignId);
            if (spec.Faction != faction)
            {
                throw new InvalidOperationException($"Starting unit design '{startingUnit.DesignId}' belongs to {spec.Faction}, not {faction}.");
            }
        }
    }

    private static UnitArchetype? PreferredArchetype(ProductionCategory category)
    {
        return category switch
        {
            ProductionCategory.Infantry => UnitArchetype.Infantry,
            ProductionCategory.Vehicle => UnitArchetype.GuardTank,
            ProductionCategory.Economy => UnitArchetype.Harvester,
            _ => null,
        };
    }

    private static UnitArchetype PreferredArchetype(ProductionKind productionKind)
    {
        return productionKind switch
        {
            ProductionKind.InfantrySquad => UnitArchetype.Infantry,
            ProductionKind.LightTank => UnitArchetype.GuardTank,
            ProductionKind.Harvester => UnitArchetype.Harvester,
            _ => throw new ArgumentOutOfRangeException(nameof(productionKind), productionKind, null),
        };
    }

    private static bool IsBetterProductionOption(UnitSpec candidate, UnitSpec? best, int candidatePreference, int bestPreference)
    {
        return best is null
            || candidatePreference < bestPreference
            || (candidatePreference == bestPreference && CompareProductionSortKey(candidate, best) < 0);
    }

    private static int CompareProductionSortKey(UnitSpec left, UnitSpec right)
    {
        var techTier = left.Stats.TechTier.CompareTo(right.Stats.TechTier);
        return techTier != 0 ? techTier : string.CompareOrdinal(left.Id, right.Id);
    }
}
