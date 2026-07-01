using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public bool CommandHarvestSelected(PlayerSlotId playerSlotId, ResourceFieldModel field, out string status)
    {
        var harvesters = SelectedUnits(playerSlotId)
            .Where(IsHarvester)
            .ToList();
        if (harvesters.Count == 0)
        {
            status = GameText.T("harvest.selectHarvester");
            return false;
        }

        if (field.Amount <= 0)
        {
            status = GameText.T("harvest.depleted");
            return false;
        }

        var validHarvesters = harvesters
            .OrderBy(unit => unit.Id)
            .Where(harvester => FindBestRefineryIdForHarvester(harvester.PlayerSlotId, field.Position) is int)
            .ToList();
        if (validHarvesters.Count > 0)
        {
            SyncResourceFieldEntity(field);
            SubmitAndApplyInputCommand(new HarvestEntityCommand(
                OwnerId.FromPlayerSlot(playerSlotId),
                validHarvesters.Select(unit => unit.EntityId).ToList(),
                NextInputCommandTick(),
                _resourceFieldEntityIds[field.Id]));
        }

        status = validHarvesters.Count == 0
            ? GameText.T("harvest.needRefinery")
            : GameText.Format("harvest.assigned", validHarvesters.Count, validHarvesters.Count == 1 ? "" : "s", field.Id);
        return validHarvesters.Count > 0;
    }

    public bool CommandHarvestUnits(PlayerSlotId playerSlotId, IEnumerable<int> unitIds, ResourceFieldModel field, out string status)
    {
        var requested = unitIds.ToHashSet();
        var harvesters = Units
            .Where(unit => unit.PlayerSlotId == playerSlotId && requested.Contains(unit.Id))
            .Where(unit => unit.Hp > 0)
            .Where(IsHarvester)
            .OrderBy(unit => unit.Id)
            .ToList();
        if (harvesters.Count == 0)
        {
            status = GameText.T("harvest.selectHarvester");
            return false;
        }

        if (field.Amount <= 0)
        {
            status = GameText.T("harvest.depleted");
            return false;
        }

        var validHarvesters = harvesters
            .Where(harvester => FindBestRefineryIdForHarvester(harvester.PlayerSlotId, field.Position) is int)
            .ToList();
        if (validHarvesters.Count == 0)
        {
            status = GameText.T("harvest.needRefinery");
            return false;
        }

        SyncResourceFieldEntity(field);
        SubmitAndApplyInputCommand(new HarvestEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            validHarvesters.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            _resourceFieldEntityIds[field.Id]));
        status = GameText.Format("harvest.assigned", validHarvesters.Count, validHarvesters.Count == 1 ? "" : "s", field.Id);
        return true;
    }

    public bool CanRepairSelected(PlayerSlotId playerSlotId, UnitInstance target)
    {
        return IsRepairableTarget(playerSlotId, target) && SelectedUnits(playerSlotId).Any(IsRepairer);
    }

    public bool CanRepairSelectedBuilding(PlayerSlotId playerSlotId, int buildingId)
    {
        return IsRepairableBuildingTargetCore(playerSlotId, buildingId) && SelectedUnits(playerSlotId).Any(IsRepairer);
    }

    public bool CommandRepairSelected(PlayerSlotId playerSlotId, UnitInstance target, out string status)
    {
        status = GameText.T("ui.context.repair");
        if (!IsRepairableTarget(playerSlotId, target))
        {
            return false;
        }

        var repairers = SelectedUnits(playerSlotId)
            .Where(IsRepairer)
            .OrderBy(unit => unit.Id)
            .ToList();
        if (repairers.Count == 0)
        {
            return false;
        }

        SyncUnitEntity(target);
        SubmitAndApplyInputCommand(new RepairEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            repairers.Select(unit => unit.EntityId).ToList(),
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

        var repairers = SelectedUnits(playerSlotId)
            .Where(IsRepairer)
            .OrderBy(unit => unit.Id)
            .ToList();
        if (repairers.Count == 0)
        {
            return false;
        }

        SyncBuildingTargetEntity(buildingId);
        SubmitAndApplyInputCommand(new RepairEntityCommand(
            OwnerId.FromPlayerSlot(playerSlotId),
            repairers.Select(unit => unit.EntityId).ToList(),
            NextInputCommandTick(),
            targetEntity.Id));
        return true;
    }

}
