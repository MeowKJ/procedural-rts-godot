using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefieldEnemyProductionAi
{
    private void MaintainHarvesterEconomy(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        CollectIdleHarvesters(battlefield, enemyPlayerSlotId, _idleHarvesterBuffer);
        if (_idleHarvesterBuffer.Count == 0)
        {
            return;
        }

        var baseCenter = EnemyBaseCenter(battlefield, enemyPlayerSlotId);
        var resource = battlefield.NearestVisibleResourceNode(OwnerId.FromPlayerSlot(enemyPlayerSlotId), baseCenter);
        if (resource is not { } resourceNode)
        {
            return;
        }

        CollectEntityIds(_idleHarvesterBuffer, _idleHarvesterEntityIds);
        UnitBattlefieldScriptedCommandDriver.Submit(
            battlefield,
            "enemy-economy",
            enemyPlayerSlotId,
            PlayerCommandKind.Harvest,
            PlayerCommandPayload.ForEntityTarget(_idleHarvesterEntityIds, resourceNode.EntityId));
    }

    private void SetEnemyRallyPoints(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        var rally = EnemyBaseCenter(battlefield, enemyPlayerSlotId) + new Vector2(-250, -120);
        battlefield.SetMissingProducerRallyPoints(enemyPlayerSlotId, rally);
    }

    private Vector2 EnemyBaseCenter(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        return battlefield.LiveBuildingCenterOrFallback(enemyPlayerSlotId, EnemyBaseFallback(battlefield));
    }

    private static UnitFactionId FactionFor(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId)
    {
        return battlefield.FirstOwnedBuildingFactionOrDefault(enemyPlayerSlotId, enemyPlayerSlotId == PlayerSlotId.One ? UnitFactionId.Dog : UnitFactionId.Cat);
    }

    private static void CollectIdleHarvesters(UnitBattlefield battlefield, PlayerSlotId enemyPlayerSlotId, List<UnitInstance> result)
    {
        battlefield.CollectIdleEconomyUnits(enemyPlayerSlotId, result);
    }

    private static void CollectEntityIds(IReadOnlyList<UnitInstance> units, List<EntityId> result)
    {
        result.Clear();
        for (var index = 0; index < units.Count; index++)
        {
            result.Add(units[index].EntityId);
        }
    }

    private static void CollectOwnedBuildings(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        List<UnitBattlefieldBuildingSnapshot> result,
        bool liveOnly)
    {
        battlefield.CollectOwnedBuildings(playerSlotId, result, liveOnly);
    }

    private static Vector2 EnemyBaseFallback(UnitBattlefield battlefield)
    {
        return new Vector2(battlefield.WorldSize.X * 0.78f, battlefield.WorldSize.Y * 0.62f);
    }
}
