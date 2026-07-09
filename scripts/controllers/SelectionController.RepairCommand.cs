using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private bool _repairCommandArmed;

    public void ArmRepairCommand()
    {
        _rallyCommandArmed = false;
        _armedAbility = null;
        if (!UseUnitBattlefieldInput() || !HasSelectedRuntimeRepairer())
        {
            _repairCommandArmed = false;
            StatusChanged?.Invoke(GameText.Format("ui.ability.unavailable", GameText.T("ui.context.repair")));
            AudioCueRequested?.Invoke(TacticalAudioCue.Invalid);
            return;
        }

        _repairCommandArmed = true;
        StatusChanged?.Invoke(GameText.T("ui.context.repair"));
        AudioCueRequested?.Invoke(TacticalAudioCue.Move);
        QueueRedraw();
    }

    private bool HandleArmedRepairMouse(InputEventMouseButton mouse)
    {
        if (!_repairCommandArmed)
        {
            return false;
        }

        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (!mouse.Pressed)
            {
                FinishArmedRepairCommand(mouse.Position);
            }

            GetViewport().SetInputAsHandled();
            return true;
        }

        if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            CancelArmedRepairCommand();
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    private bool HandleArmedRepairKey(InputEventKey key)
    {
        if (!_repairCommandArmed || key.Keycode != Key.Escape)
        {
            return false;
        }

        CancelArmedRepairCommand();
        return true;
    }

    private CommandPreviewState ArmedRepairPreview(Vector2 screenPosition, Vector2 worldPosition)
    {
        if (_hoveredUnitInstance is { } unit && UnitBattlefield!.CanRepairSelected(LocalPlayerSlotId, unit))
        {
            return new CommandPreviewState(CommandPreviewKind.Repair, RepairUnitPreviewLabel(), screenPosition, unit.Position, true);
        }

        if (_hoveredBuildingProjection is { } building && UnitBattlefield!.CanRepairSelectedBuilding(LocalPlayerSlotId, building.Id))
        {
            return new CommandPreviewState(CommandPreviewKind.Repair, RepairStructurePreviewLabel(), screenPosition, building.Position, true);
        }

        return new CommandPreviewState(CommandPreviewKind.Repair, RepairInvalidPreviewLabel(), screenPosition, worldPosition, false);
    }

    private static string RepairUnitPreviewLabel()
    {
        return GameText.T("preview.repair.unit");
    }

    private static string RepairStructurePreviewLabel()
    {
        return GameText.T("preview.repair.structure");
    }

    private static string RepairInvalidPreviewLabel()
    {
        return GameText.T("preview.repair.invalid");
    }

    private static string RepairNeedsSupportPreviewLabel()
    {
        return GameText.T("preview.repair.needSupport");
    }

    private void FinishArmedRepairCommand(Vector2 screenPoint)
    {
        FinishRuntimeRepairCommand(ScreenToWorld(screenPoint), acknowledgeInvalidAtTarget: true);
        _repairCommandArmed = false;
        ClearDrag();
    }

    private bool FinishRuntimeRepairCommand(Vector2 worldPoint, bool acknowledgeInvalidAtTarget = false)
    {
        if (UnitBattlefield!.PickAnyUnit(worldPoint, PickPaddingWorld()) is { } hoveredUnit
            && UnitBattlefield.CanRepairSelected(LocalPlayerSlotId, hoveredUnit))
        {
            var accepted = SubmitRuntimeCommand(
                PlayerCommandKind.Repair,
                PlayerCommandPayload.ForEntityTarget(SelectedRuntimeUnitSubjects(), hoveredUnit.EntityId));
            StatusChanged?.Invoke(accepted ? RepairCommandStatusText(hoveredUnit.EntityId) : GameText.T("ui.context.repair"));
            AcknowledgeCommand(
                accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid,
                hoveredUnit.Position,
                accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            return accepted;
        }

        if (UnitBattlefield.PickAnyBuildingHoverProjection(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } hoveredBuilding
            && UnitBattlefield.CanRepairSelectedBuilding(LocalPlayerSlotId, hoveredBuilding.Id))
        {
            var targetEntity = UnitBattlefield.BuildingEntityIdByTargetId(hoveredBuilding.Id);
            var accepted = false;
            if (targetEntity is { } repairTarget)
            {
                accepted = SubmitRuntimeCommand(
                    PlayerCommandKind.Repair,
                    PlayerCommandPayload.ForEntityTarget(SelectedRuntimeUnitSubjects(), repairTarget, CombatTargetKind.Building));
            }

            StatusChanged?.Invoke(accepted && targetEntity is { } acceptedTarget
                ? RepairCommandStatusText(acceptedTarget)
                : GameText.T("ui.context.repair"));
            AcknowledgeCommand(
                accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid,
                hoveredBuilding.Position,
                accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            return accepted;
        }

        if (acknowledgeInvalidAtTarget)
        {
            StatusChanged?.Invoke(GameText.T("ui.context.repair"));
            AcknowledgeCommand(CommandAcknowledgementKind.Invalid, worldPoint, CommandAcknowledgementAudioCue.Invalid);
        }

        return false;
    }

    private void CancelArmedRepairCommand()
    {
        _repairCommandArmed = false;
        ClearDrag();
        StatusChanged?.Invoke(GameText.T("ui.status.ready"));
        QueueRedraw();
    }

    private string RepairCommandStatusText(EntityId targetEntity)
    {
        foreach (var projection in UnitBattlefield!.RepairOrderProjections(LocalPlayerSlotId))
        {
            if (projection.Target == targetEntity
                && projection.StallReason == RepairOrderStallReason.InsufficientCredits)
            {
                return GameText.T("repair.stalled.noCredits");
            }
        }

        return GameText.T("ui.context.repair");
    }

    private bool HasSelectedRuntimeRepairer()
    {
        if (!UseUnitBattlefieldInput())
        {
            return false;
        }

        foreach (var unit in UnitBattlefield!.Units)
        {
            if (unit.PlayerSlotId == LocalPlayerSlotId
                && unit.Selected
                && unit.Hp > 0
                && unit.Spec.HasAbility(AbilityKind.RepairField))
            {
                return true;
            }
        }

        return false;
    }
}
