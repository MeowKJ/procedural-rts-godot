using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private static bool TryConstructNextBuilding(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, out string status)
    {
        status = string.Empty;
        var next = NextNeededBuilding(battlefield, enemyPlayerSlotId);
        if (next is null)
        {
            return false;
        }

        var faction = FactionFor(battlefield, enemyPlayerSlotId);
        var baseCenter = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        var facing = baseCenter.X > battlefield.WorldSize.X * 0.5f ? MathF.PI : 0;
        foreach (var position in CandidateBuildPositions(battlefield, enemyPlayerSlotId, next))
        {
            if (!battlefield.ValidateBuildingPlacement(next, enemyPlayerSlotId, position).IsValid)
            {
                continue;
            }

            if (battlefield.ConstructBuilding(enemyPlayerSlotId, faction, next, position, out _, out status, facing))
            {
                status = $"Enemy construction started: {next}";
                return true;
            }
        }

        return false;
    }

    private static string? NextNeededBuilding(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var owned = battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId && building.Hp > 0)
            .ToList();
        var power = battlefield.PowerStatus(enemyPlayerSlotId);

        if (!HasBuilding(owned, BuildingDesignIds.PowerPlant))
        {
            return BuildingDesignIds.PowerPlant;
        }

        if (power.HasProvider && power.Provided < power.Used + 8)
        {
            return BuildingDesignIds.PowerPlant;
        }

        if (!HasBuilding(owned, BuildingDesignIds.Refinery))
        {
            return BuildingDesignIds.Refinery;
        }

        var harvesterCount = battlefield.Units.Count(unit =>
            unit.PlayerSlotId == enemyPlayerSlotId
            && unit.Hp > 0
            && unit.Spec.RoleTags.Contains(UnitRoleTag.Economy));
        var refineryCount = owned.Count(building => building.Kind == BuildingDesignIds.Refinery);
        if (harvesterCount >= 3 && refineryCount < 2)
        {
            return BuildingDesignIds.Refinery;
        }

        if (!HasBuilding(owned, BuildingDesignIds.Barracks))
        {
            return BuildingDesignIds.Barracks;
        }

        if (!HasBuilding(owned, BuildingDesignIds.VehicleFactory))
        {
            return BuildingDesignIds.VehicleFactory;
        }

        if (!HasBuilding(owned, BuildingDesignIds.Airfield)
            && battlefield.Credits(enemyPlayerSlotId) >= BuildSpecCatalog.For(BuildingDesignIds.Airfield).Cost)
        {
            return BuildingDesignIds.Airfield;
        }

        if (!HasBuilding(owned, BuildingDesignIds.GroundTurret) && CombatUnitsNearBase(battlefield, enemyPlayerSlotId) < 4)
        {
            return BuildingDesignIds.GroundTurret;
        }

        if (!HasBuilding(owned, BuildingDesignIds.AntiAirTurret)
            && HasBuilding(owned, BuildingDesignIds.Airfield)
            && battlefield.Credits(enemyPlayerSlotId) >= BuildSpecCatalog.For(BuildingDesignIds.AntiAirTurret).Cost)
        {
            return BuildingDesignIds.AntiAirTurret;
        }

        return null;
    }

    private static bool HasBuilding(IReadOnlyList<UnitBattlefieldBuildingSnapshot> buildings, string kind)
    {
        return buildings.Any(building => building.Kind == kind);
    }

    private static int CombatUnitsNearBase(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var center = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        return battlefield.Units.Count(unit =>
            unit.PlayerSlotId == enemyPlayerSlotId
            && unit.Hp > 0
            && !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
            && unit.Position.DistanceSquaredTo(center) <= 720 * 720);
    }

    private static IEnumerable<Vector2> CandidateBuildPositions(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, string kind)
    {
        var baseCenter = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        var direction = baseCenter.X > battlefield.WorldSize.X * 0.5f ? -1 : 1;
        var offsets = kind switch
        {
            BuildingDesignIds.PowerPlant => new[] { new Vector2(direction * 210, -185), new Vector2(direction * 250, 0), new Vector2(direction * 170, 185) },
            BuildingDesignIds.Refinery => new[] { new Vector2(direction * 325, 210), new Vector2(direction * 385, -170), new Vector2(direction * 480, 40) },
            BuildingDesignIds.Barracks => new[] { new Vector2(direction * 230, -320), new Vector2(direction * 365, -295), new Vector2(direction * 150, -380) },
            BuildingDesignIds.VehicleFactory => new[] { new Vector2(direction * 250, 330), new Vector2(direction * 420, 285), new Vector2(direction * 125, 390) },
            BuildingDesignIds.GroundTurret => new[] { new Vector2(direction * -155, 0), new Vector2(direction * -110, -145), new Vector2(direction * -110, 145) },
            _ => new[] { new Vector2(direction * 260, 0), new Vector2(direction * 0, 260), new Vector2(direction * 0, -260) },
        };

        foreach (var offset in offsets)
        {
            yield return baseCenter + offset;
        }

        for (var radius = 260f; radius <= 700f; radius += 92f)
        {
            for (var step = 0; step < 10; step++)
            {
                var angle = (-0.85f + step * 0.19f) * MathF.PI;
                yield return baseCenter + new Vector2(MathF.Cos(angle) * radius * direction, MathF.Sin(angle) * radius);
            }
        }
    }
}
