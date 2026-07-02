using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private static bool IsNearOwnedBuilding(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 position, float radius)
    {
        return battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == playerSlotId && building.Hp > 0)
            .Any(building => building.Position.DistanceSquaredTo(position) <= radius * radius);
    }

    private static Vector2 ScoutPoint(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var center = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        return new Vector2(
            Mathf.Clamp(battlefield.WorldSize.X - center.X, 180, battlefield.WorldSize.X - 180),
            Mathf.Clamp(battlefield.WorldSize.Y - center.Y, 180, battlefield.WorldSize.Y - 180));
    }

    private static bool IsInsideAggressionRadius(Vector2 targetPosition, Vector2 enemyCenter, float aggressionRadius)
    {
        if (float.IsPositiveInfinity(aggressionRadius))
        {
            return true;
        }

        return targetPosition.DistanceSquaredTo(enemyCenter) <= aggressionRadius * aggressionRadius;
    }

    private static Vector2 EnemyBaseCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var buildings = battlefield.BuildingSnapshots()
            .Where(building => building.PlayerSlotId == enemyPlayerSlotId && building.Hp > 0)
            .Select(building => building.Position)
            .ToList();
        if (buildings.Count > 0)
        {
            return buildings.Aggregate(Vector2.Zero, (sum, position) => sum + position) / buildings.Count;
        }

        return EnemyCenter(battlefield, enemyPlayerSlotId);
    }

    private static Vector2 EnemyCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var units = battlefield.Units
            .Where(unit => unit.PlayerSlotId == enemyPlayerSlotId && unit.Hp > 0)
            .Select(unit => unit.Position)
            .ToList();
        if (units.Count == 0)
        {
            return new Vector2(battlefield.WorldSize.X * 0.78f, battlefield.WorldSize.Y * 0.62f);
        }

        return units.Aggregate(Vector2.Zero, (sum, position) => sum + position) / units.Count;
    }
}
