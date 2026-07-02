using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private bool TryConstructNextBuilding(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, out string status)
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
        var direction = baseCenter.X > battlefield.WorldSize.X * 0.5f ? -1 : 1;
        var offsets = CandidateBuildOffsets(next);
        for (var index = 0; index < offsets.Count; index++)
        {
            var offset = offsets[index];
            var position = baseCenter + new Vector2(offset.X * direction, offset.Y);
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

        for (var radius = 260f; radius <= 700f; radius += 92f)
        {
            for (var step = 0; step < 10; step++)
            {
                var angle = (-0.85f + step * 0.19f) * MathF.PI;
                var position = baseCenter + new Vector2(MathF.Cos(angle) * radius * direction, MathF.Sin(angle) * radius);
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
        }

        return false;
    }

    private string? NextNeededBuilding(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        CollectOwnedBuildings(battlefield, enemyPlayerSlotId, _ownedBuildingBuffer, liveOnly: true);
        var power = battlefield.PowerStatus(enemyPlayerSlotId);

        if (!HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.PowerPlant))
        {
            return BuildingDesignIds.PowerPlant;
        }

        if (power.HasProvider && power.Provided < power.Used + 8)
        {
            return BuildingDesignIds.PowerPlant;
        }

        if (!HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.Refinery))
        {
            return BuildingDesignIds.Refinery;
        }

        var harvesterCount = LiveHarvesterCount(battlefield, enemyPlayerSlotId);
        var refineryCount = BuildingCount(_ownedBuildingBuffer, BuildingDesignIds.Refinery);
        if (harvesterCount >= 3 && refineryCount < 2)
        {
            return BuildingDesignIds.Refinery;
        }

        if (!HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.Barracks))
        {
            return BuildingDesignIds.Barracks;
        }

        if (!HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.VehicleFactory))
        {
            return BuildingDesignIds.VehicleFactory;
        }

        if (!HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.Airfield)
            && battlefield.Credits(enemyPlayerSlotId) >= BuildSpecCatalog.For(BuildingDesignIds.Airfield).Cost)
        {
            return BuildingDesignIds.Airfield;
        }

        if (!HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.GroundTurret) && CombatUnitsNearBase(battlefield, enemyPlayerSlotId) < 4)
        {
            return BuildingDesignIds.GroundTurret;
        }

        if (!HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.AntiAirTurret)
            && HasBuilding(_ownedBuildingBuffer, BuildingDesignIds.Airfield)
            && battlefield.Credits(enemyPlayerSlotId) >= BuildSpecCatalog.For(BuildingDesignIds.AntiAirTurret).Cost)
        {
            return BuildingDesignIds.AntiAirTurret;
        }

        return null;
    }

    private static bool HasBuilding(IReadOnlyList<UnitBattlefieldBuildingSnapshot> buildings, string kind)
    {
        for (var index = 0; index < buildings.Count; index++)
        {
            if (buildings[index].Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    private static int BuildingCount(IReadOnlyList<UnitBattlefieldBuildingSnapshot> buildings, string kind)
    {
        var count = 0;
        for (var index = 0; index < buildings.Count; index++)
        {
            if (buildings[index].Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    private int CombatUnitsNearBase(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var center = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        var count = 0;
        foreach (var unit in battlefield.Units)
        {
            if (unit.PlayerSlotId == enemyPlayerSlotId
                && unit.Hp > 0
                && !unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
                && unit.Position.DistanceSquaredTo(center) <= 720 * 720)
            {
                count++;
            }
        }

        return count;
    }
}
