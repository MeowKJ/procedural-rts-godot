using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private static bool IsNearOwnedBuilding(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 position, float radius)
    {
        var radiusSquared = radius * radius;
        foreach (var building in battlefield.BuildingSnapshots())
        {
            if (building.PlayerSlotId == playerSlotId
                && building.Hp > 0
                && building.Position.DistanceSquaredTo(position) <= radiusSquared)
            {
                return true;
            }
        }

        return false;
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
        var sum = Vector2.Zero;
        var count = 0;
        foreach (var building in battlefield.BuildingSnapshots())
        {
            if (building.PlayerSlotId != enemyPlayerSlotId || building.Hp <= 0)
            {
                continue;
            }

            sum += building.Position;
            count++;
        }

        if (count > 0)
        {
            return sum / count;
        }

        return EnemyCenter(battlefield, enemyPlayerSlotId);
    }

    private static Vector2 EnemyCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var sum = Vector2.Zero;
        var count = 0;
        foreach (var unit in battlefield.Units)
        {
            if (unit.PlayerSlotId != enemyPlayerSlotId || unit.Hp <= 0)
            {
                continue;
            }

            sum += unit.Position;
            count++;
        }

        if (count == 0)
        {
            return new Vector2(battlefield.WorldSize.X * 0.78f, battlefield.WorldSize.Y * 0.62f);
        }

        return sum / count;
    }
}
