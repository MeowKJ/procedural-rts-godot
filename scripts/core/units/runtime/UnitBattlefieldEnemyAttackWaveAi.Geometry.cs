using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
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
        return battlefield.LiveBuildingCenterOrUnitCenter(enemyPlayerSlotId, EnemyCenterFallback(battlefield));
    }

    private static Vector2 EnemyCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        return battlefield.LiveUnitCenterOrFallback(enemyPlayerSlotId, EnemyCenterFallback(battlefield));
    }

    private static Vector2 EnemyCenterFallback(UnitBattlefield battlefield)
    {
        return new Vector2(battlefield.WorldSize.X * 0.78f, battlefield.WorldSize.Y * 0.62f);
    }
}
