using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private CommandPreviewState CreatePreviewState(Vector2 screenPosition, Vector2 worldPosition)
    {
        if (_dragStartScreen is not null)
        {
            return DragPreviewState(screenPosition, worldPosition);
        }

        if (_rallyCommandArmed)
        {
            return ArmedRallyPreview(screenPosition, worldPosition);
        }

        if (_repairCommandArmed)
        {
            return ArmedRepairPreview(screenPosition, worldPosition);
        }

        if (_armedAbility is not null)
        {
            return ArmedAbilityPreview(screenPosition, worldPosition);
        }

        if (_hoveredUnitInstance is { } hoveredUnitInstance)
        {
            var relation = UnitBattlefield!.Relations.Relation(LocalPlayerSlotId, hoveredUnitInstance.PlayerSlotId);
            if (relation == PlayerRelation.Hostile && UnitBattlefield.SelectedCount(LocalPlayerSlotId) > 0)
            {
                return new CommandPreviewState(CommandPreviewKind.Attack, RuntimeUnitAttackPreviewLabel(hoveredUnitInstance.Spec), screenPosition, hoveredUnitInstance.Position, true);
            }

            if (UnitBattlefield.CanRepairSelected(LocalPlayerSlotId, hoveredUnitInstance))
            {
                return new CommandPreviewState(CommandPreviewKind.Repair, RepairUnitPreviewLabel(), screenPosition, hoveredUnitInstance.Position, true);
            }

            if (UnitBattlefield.NeedsRepairSupport(LocalPlayerSlotId, hoveredUnitInstance))
            {
                return new CommandPreviewState(CommandPreviewKind.Repair, RepairNeedsSupportPreviewLabel(), screenPosition, hoveredUnitInstance.Position, false);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, relation == PlayerRelation.Hostile ? GameText.T("preview.enemy") : GameText.T("preview.unit"), screenPosition, hoveredUnitInstance.Position, true);
        }

        if (_hoveredBuildingProjection is { } buildingProjection)
        {
            var isEnemy = buildingProjection.Relation == PlayerRelation.Hostile;
            if (isEnemy && UnitBattlefield!.SelectedCount(LocalPlayerSlotId) > 0)
            {
                return new CommandPreviewState(CommandPreviewKind.Attack, RuntimeBuildingAttackPreviewLabel(buildingProjection), screenPosition, buildingProjection.Position, true);
            }

            if (UnitBattlefield!.CanRepairSelectedBuilding(LocalPlayerSlotId, buildingProjection.Id))
            {
                return new CommandPreviewState(CommandPreviewKind.Repair, RepairStructurePreviewLabel(), screenPosition, buildingProjection.Position, true);
            }

            if (UnitBattlefield.NeedsRepairSupportBuilding(LocalPlayerSlotId, buildingProjection.Id))
            {
                return new CommandPreviewState(CommandPreviewKind.Repair, RepairNeedsSupportPreviewLabel(), screenPosition, buildingProjection.Position, false);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, isEnemy ? GameText.T("preview.enemyStructure") : GameText.T("preview.structure"), screenPosition, buildingProjection.Position, true);
        }

        if (_hoveredResourceNode is { } resourceNode)
        {
            var hasHarvester = HasSelectedHarvester();
            if (hasHarvester)
            {
                return new CommandPreviewState(CommandPreviewKind.Harvest, GameText.T("preview.harvest"), screenPosition, resourceNode.Position, true);
            }

            if (HasSelectedBuildingForPreview())
            {
                return new CommandPreviewState(CommandPreviewKind.Rally, GameText.T("preview.setRally"), screenPosition, resourceNode.Position, true);
            }

            return new CommandPreviewState(CommandPreviewKind.TargetHover, GameText.T("preview.resource"), screenPosition, resourceNode.Position, true);
        }

        if (UnitBattlefield.SelectedCount(LocalPlayerSlotId) > 0)
        {
            return MovePreviewState(screenPosition, worldPosition);
        }

        if (HasSelectedBuildingForPreview())
        {
            return new CommandPreviewState(CommandPreviewKind.Rally, GameText.T("preview.setRally"), screenPosition, worldPosition, true);
        }

        return new CommandPreviewState(CommandPreviewKind.Select, GameText.T("preview.select"), screenPosition, worldPosition, true);
    }

    private CommandPreviewState MovePreviewState(Vector2 screenPosition, Vector2 worldPosition)
    {
        return new CommandPreviewState(
            CommandPreviewKind.Move,
            CommandRibbonContextResolver.MoveModeLabel(PreviewMoveModeFromModifiers()),
            screenPosition,
            worldPosition,
            true);
    }

    private CommandPreviewState DragPreviewState(Vector2 screenPosition, Vector2 worldPosition)
    {
        var kind = IsActiveSelectionDrag(screenPosition)
            ? CommandPreviewKind.DragSelect
            : CommandPreviewKind.Select;
        return new CommandPreviewState(kind, GameText.T("preview.select"), screenPosition, worldPosition, true);
    }

    private bool IsActiveSelectionDrag(Vector2 screenPosition)
    {
        if (_dragStartScreen is null)
        {
            return false;
        }

        var distance = _dragStartScreen.Value.DistanceTo(screenPosition);
        if (_dragButton == MouseButton.Right)
        {
            var elapsed = Time.GetTicksMsec() / 1000.0 - _dragStartSeconds;
            return SelectionGestureMath.IsRightSelectionDrag(distance, elapsed);
        }

        return SelectionGestureMath.IsLeftSelectionDrag(distance);
    }

    private MoveCommandMode PreviewMoveModeFromModifiers()
    {
        if (Input.IsKeyPressed(Key.Ctrl))
        {
            return MoveCommandMode.Ignore;
        }

        return Input.IsKeyPressed(Key.Alt) ? MoveCommandMode.Attack : CurrentMoveMode;
    }
}
