using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyAttackWaveAi
{
    private static UnitBattlefieldBuildingSnapshot? VisibleAttackableHeadquarters(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        Vector2 origin,
        float aggressionRadius)
    {
        return battlefield.VisibleAttackableHeadquarters(playerSlotId, origin, aggressionRadius);
    }

    private static UnitBattlefieldBuildingSnapshot? NearestVisibleAttackableBuilding(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        Vector2 origin,
        float aggressionRadius)
    {
        return battlefield.NearestVisibleAttackableBuilding(playerSlotId, origin, aggressionRadius);
    }

    private static UnitInstance? NearestVisibleAttackableUnit(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 origin, float aggressionRadius)
    {
        return battlefield.NearestVisibleAttackableUnit(playerSlotId, origin, aggressionRadius);
    }

    private UnitInstance? NearestVisibleDefenseThreatUnit(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 baseCenter)
    {
        return battlefield.NearestVisibleDefenseThreatUnit(playerSlotId, baseCenter, DefenseRadius);
    }

    private UnitBattlefieldBuildingSnapshot? NearestVisibleDefenseThreatBuilding(UnitBattlefield battlefield, PlayerSlotId playerSlotId, Vector2 baseCenter)
    {
        return battlefield.NearestVisibleDefenseThreatBuilding(playerSlotId, baseCenter, DefenseRadius);
    }
}
