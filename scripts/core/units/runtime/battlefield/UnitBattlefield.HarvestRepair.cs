using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public bool CanRepairSelected(PlayerSlotId playerSlotId, UnitInstance target)
    {
        return IsRepairableTarget(playerSlotId, target) && HasSelectedCommandUnit(playerSlotId, IsRepairer);
    }

    public bool CanRepairSelectedBuilding(PlayerSlotId playerSlotId, int buildingId)
    {
        return IsRepairableBuildingTargetCore(playerSlotId, buildingId) && HasSelectedCommandUnit(playerSlotId, IsRepairer);
    }

    public bool NeedsRepairSupport(PlayerSlotId playerSlotId, UnitInstance target)
    {
        return IsRepairableTarget(playerSlotId, target) && !HasSelectedCommandUnit(playerSlotId, IsRepairer);
    }

    public bool NeedsRepairSupportBuilding(PlayerSlotId playerSlotId, int buildingId)
    {
        return IsRepairableBuildingTargetCore(playerSlotId, buildingId) && !HasSelectedCommandUnit(playerSlotId, IsRepairer);
    }

    public IReadOnlyList<RepairOrderProjection> RepairOrderProjections(PlayerSlotId playerSlotId)
    {
        _repairOrderProjectionBuffer.Clear();
        var owner = OwnerId.FromPlayerSlot(playerSlotId);
        foreach (var repairer in _entityWorld.OrderedEntities)
        {
            if (repairer.OwnerId != owner
                || !repairer.Components.TryGet<RepairOrderComponentState>(out var order)
                || !_entityWorld.TryGet(new EntityId(order.TargetId), out var target))
            {
                continue;
            }

            _repairOrderProjectionBuffer.Add(new RepairOrderProjection(
                repairer.Id,
                target.Id,
                repairer.Transform.Position,
                target.Transform.Position,
                RepairOrderStallReasonFor(owner, order)));
        }

        return _repairOrderProjectionBuffer;
    }

    private bool HasSelectedCommandUnit(PlayerSlotId playerSlotId, Predicate<UnitInstance> predicate)
    {
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected && predicate(unit))
            {
                return true;
            }
        }

        return false;
    }

    private RepairOrderStallReason RepairOrderStallReasonFor(OwnerId owner, RepairOrderComponentState order)
    {
        var costPerHp = MathF.Max(0, order.CreditCostPerHp);
        return costPerHp > 0 && _entityWorld.ResourceInventory(owner).Credits < costPerHp
            ? RepairOrderStallReason.InsufficientCredits
            : RepairOrderStallReason.None;
    }

    private static int CompareUnitInstanceIds(UnitInstance left, UnitInstance right)
    {
        return left.Id.CompareTo(right.Id);
    }

}
