using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public bool CommandHarvestSelected(PlayerSlotId playerSlotId, ResourceFieldModel field, out string status)
    {
        CollectSelectedCommandUnits(playerSlotId, IsHarvester, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            status = GameText.T("harvest.selectHarvester");
            return false;
        }

        if (field.Amount <= 0)
        {
            status = GameText.T("harvest.depleted");
            return false;
        }

        KeepUnitsWithRefinery(_unitCommandBuffer, field.Position);
        if (_unitCommandBuffer.Count > 0)
        {
            CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
            SubmitAndApplyInputCommand(new HarvestEntityCommand(
                OwnerId.FromPlayerSlot(playerSlotId),
                _unitCommandEntityBuffer,
                NextInputCommandTick(),
                _resourceFieldEntityIds[field.Id]));
        }

        status = _unitCommandBuffer.Count == 0
            ? GameText.T("harvest.needRefinery")
            : GameText.Format("harvest.assigned", _unitCommandBuffer.Count, _unitCommandBuffer.Count == 1 ? "" : "s", field.Id);
        return _unitCommandBuffer.Count > 0;
    }

    public bool CommandHarvestUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, ResourceFieldModel field, out string status)
    {
        CollectRequestedCommandUnits(playerSlotId, unitIds, IsHarvester, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            status = GameText.T("harvest.selectHarvester");
            return false;
        }

        if (field.Amount <= 0)
        {
            status = GameText.T("harvest.depleted");
            return false;
        }

        KeepUnitsWithRefinery(_unitCommandBuffer, field.Position);
        if (_unitCommandBuffer.Count == 0)
        {
            status = GameText.T("harvest.needRefinery");
            return false;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new HarvestEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            _resourceFieldEntityIds[field.Id]));
        status = GameText.Format("harvest.assigned", _unitCommandBuffer.Count, _unitCommandBuffer.Count == 1 ? "" : "s", field.Id);
        return true;
    }

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

    public bool CommandRepairSelected(PlayerSlotId playerSlotId, UnitInstance target, out string status)
    {
        status = GameText.T("ui.context.repair");
        if (!IsRepairableTarget(playerSlotId, target))
        {
            return false;
        }

        CollectSelectedCommandUnits(playerSlotId, IsRepairer, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return false;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new RepairEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            target.EntityId));
        return true;
    }

    public bool CommandRepairSelectedBuilding(PlayerSlotId playerSlotId, int buildingId, out string status)
    {
        status = GameText.T("ui.context.repair");
        var targetEntity = BuildingEntityByTargetId(buildingId);
        if (targetEntity is null)
        {
            return false;
        }

        if (!IsRepairableBuildingTargetCore(playerSlotId, buildingId))
        {
            return false;
        }

        CollectSelectedCommandUnits(playerSlotId, IsRepairer, _unitCommandBuffer);
        if (_unitCommandBuffer.Count == 0)
        {
            return false;
        }

        CollectCommandEntityIds(_unitCommandBuffer, _unitCommandEntityBuffer);
        SubmitAndApplyInputCommand(new RepairEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            _unitCommandEntityBuffer,
            NextInputCommandTick(),
            targetEntity.Id));
        return true;
    }

    private void CollectSelectedCommandUnits(
        PlayerSlotId playerSlotId,
        Predicate<UnitInstance> predicate,
        List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId && unit.Selected && predicate(unit))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
    }

    private void CollectRequestedCommandUnits(
        PlayerSlotId playerSlotId,
        IEnumerable<int> unitIds,
        Predicate<UnitInstance> predicate,
        List<UnitInstance> result)
    {
        _unitCommandIdBuffer.Clear();
        foreach (var unitId in unitIds)
        {
            _unitCommandIdBuffer.Add(unitId);
        }

        result.Clear();
        if (_unitCommandIdBuffer.Count == 0)
        {
            return;
        }

        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId
                && unit.Hp > 0
                && _unitCommandIdBuffer.Contains(unit.Id)
                && predicate(unit))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitInstanceIds);
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

    private void KeepUnitsWithRefinery(List<UnitInstance> units, Vector2 resourcePosition)
    {
        var write = 0;
        for (var read = 0; read < units.Count; read++)
        {
            var unit = units[read];
            if (FindBestRefineryIdForHarvester(unit.PlayerSlotId, resourcePosition) is not int)
            {
                continue;
            }

            units[write++] = unit;
        }

        if (write < units.Count)
        {
            units.RemoveRange(write, units.Count - write);
        }
    }

    private static void CollectCommandEntityIds(IReadOnlyList<UnitInstance> units, List<EntityId> result)
    {
        result.Clear();
        foreach (var unit in units)
        {
            result.Add(unit.EntityId);
        }
    }

    private static int CompareUnitInstanceIds(UnitInstance left, UnitInstance right)
    {
        return left.Id.CompareTo(right.Id);
    }

}
