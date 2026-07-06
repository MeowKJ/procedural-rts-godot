using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public int SellSelectedBuildings(PlayerSlotId playerSlotId, out string status)
    {
        CollectSelectedBuildingEntityIds(playerSlotId, _selectedBuildingEntityIdBuffer);
        if (_selectedBuildingEntityIdBuffer.Count == 0)
        {
            status = GameText.T("build.sell.none");
            return 0;
        }

        _buildingDeathBuffer.Clear();
        _removedBuildingIdBuffer.Clear();
        var totalRefund = 0;
        var soldLabel = "";
        foreach (var entityId in _selectedBuildingEntityIdBuffer)
        {
            if (!_buildingTargetIdsByEntityId.TryGetValue(entityId, out var buildingId)
                || BuildingIdentity(buildingId) is not { PlayerSlotId: var ownerSlot } identity
                || ownerSlot != playerSlotId
                || BuildingSnapshot(buildingId) is not { Hp: > 0 } snapshot)
            {
                continue;
            }

            var spec = BuildSpecCatalog.For(identity.Kind);
            var refund = Mathf.RoundToInt(spec.Cost * Math.Clamp(spec.RefundRatio, 0, 1));
            totalRefund += refund;
            soldLabel = spec.Label;
            _buildingDeathBuffer.Add(new UnitBattlefieldBuildingDeathInfo(
                snapshot.Id,
                snapshot.Kind,
                snapshot.PlayerSlotId,
                snapshot.Faction,
                snapshot.Position,
                snapshot.Footprint,
                UnitBattlefieldBuildingRemovalCause.Sold));
            _removedBuildingIdBuffer.Add(snapshot.Id);
        }

        if (_buildingDeathBuffer.Count == 0)
        {
            status = GameText.T("build.sell.none");
            return 0;
        }

        var inventory = ResourceInventory(playerSlotId);
        inventory.Credits += totalRefund;
        _entityWorld.ResourceInventory(OwnerId.FromPlayerSlot(playerSlotId)).Credits = inventory.Credits;

        foreach (var removedId in _removedBuildingIdBuffer)
        {
            RemoveBuildingEntity(removedId);
        }

        foreach (var unit in Units)
        {
            if (unit.AttackTargetKind == CombatTargetKind.Building
                && unit.AttackTargetId is not null
                && _removedBuildingIdBuffer.Contains(unit.AttackTargetId.Value))
            {
                ClearAttackTarget(unit);
            }
        }

        ResourceInventoryChanged?.Invoke(playerSlotId, inventory);
        BuildingsRemoved?.Invoke(_buildingDeathBuffer);
        status = _buildingDeathBuffer.Count == 1
            ? GameText.Format("build.sold", soldLabel, totalRefund)
            : GameText.Format("build.soldMany", _buildingDeathBuffer.Count, totalRefund);
        return _buildingDeathBuffer.Count;
    }
}
