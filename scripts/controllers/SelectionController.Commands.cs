using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private void FinishSelection(Vector2 endScreen, bool additive, bool doubleClick)
    {
        var startScreen = _dragStartScreen!.Value;
        var startWorld = _dragStartWorld!.Value;
        var distance = startScreen.DistanceTo(endScreen);
        var worldPoint = ScreenToWorld(endScreen);
        var count = UseUnitBattlefieldInput()
            ? FinishUnitBattlefieldSelection(worldPoint, startWorld, distance, additive, doubleClick)
            : !SelectionGestureMath.IsLeftSelectionDrag(distance)
                ? doubleClick
                    ? State.SelectSameUnitsAt(worldPoint, VisibleWorldRect(), additive, PickPaddingWorld())
                    : State.SelectSingleAt(worldPoint, additive, PickPaddingWorld())
                : State.SelectRect(RectFromPoints(startWorld, worldPoint), additive);

        SelectionChanged?.Invoke(count);
        ClearDrag();
    }

    private int FinishUnitBattlefieldSelection(Vector2 worldPoint, Vector2 startWorld, float distance, bool additive, bool doubleClick)
    {
        var isDrag = SelectionGestureMath.IsLeftSelectionDrag(distance);
        if (!isDrag && UnitBattlefield!.PickUnit(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is null)
        {
            var count = UnitBattlefield.SelectBuildingTargetAt(LocalPlayerSlotId, worldPoint, additive, PickPaddingWorld());
            SyncBuildingSelectionFromUnitBattlefieldToState();
            return count;
        }

        State.ClearSelection();
        return !SelectionGestureMath.IsLeftSelectionDrag(distance)
            ? doubleClick
                ? UnitBattlefield!.SelectSameUnitsAt(LocalPlayerSlotId, worldPoint, VisibleWorldRect(), additive, PickPaddingWorld())
                : UnitBattlefield!.SelectSingleAt(LocalPlayerSlotId, worldPoint, additive, PickPaddingWorld())
            : UnitBattlefield!.SelectRect(LocalPlayerSlotId, RectFromPoints(startWorld, worldPoint), additive);
    }

    private void FinishRightClickCommand(Vector2 screenPoint, MoveCommandMode moveMode)
    {
        var worldPoint = ScreenToWorld(screenPoint);
        if (UseUnitBattlefieldInput() && UnitBattlefield!.SelectedCount(LocalPlayerSlotId) > 0)
        {
            if (UnitBattlefield.PickHostileUnit(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } unitInstanceEnemy)
            {
                UnitBattlefield.CommandAttackSelected(LocalPlayerSlotId, unitInstanceEnemy);
                AcknowledgeCommand(CommandAcknowledgementKind.Attack, unitInstanceEnemy.Position, CommandAcknowledgementAudioCue.Attack);
            }
            else if (UnitBattlefield.PickHostileBuildingHoverProjection(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } unitInstanceBuildingEnemy)
            {
                UnitBattlefield.CommandAttackSelected(LocalPlayerSlotId, unitInstanceBuildingEnemy.Id);
                AcknowledgeCommand(CommandAcknowledgementKind.Attack, unitInstanceBuildingEnemy.Position, CommandAcknowledgementAudioCue.Attack);
            }
            else if (UnitBattlefield.PickAnyUnit(worldPoint, PickPaddingWorld()) is { } repairUnit
                && UnitBattlefield.CanRepairSelected(LocalPlayerSlotId, repairUnit))
            {
                var accepted = UnitBattlefield.CommandRepairSelected(LocalPlayerSlotId, repairUnit, out var status);
                StatusChanged?.Invoke(status);
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid,
                    repairUnit.Position,
                    accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            }
            else if (UnitBattlefield.PickAnyBuildingHoverProjection(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } repairBuilding
                && UnitBattlefield.CanRepairSelectedBuilding(LocalPlayerSlotId, repairBuilding.Id))
            {
                var accepted = UnitBattlefield.CommandRepairSelectedBuilding(LocalPlayerSlotId, repairBuilding.Id, out var status);
                StatusChanged?.Invoke(status);
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid,
                    repairBuilding.Position,
                    accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            }
            else if (PickResourceField(worldPoint) is { } resourceField && HasSelectedHarvester())
            {
                var accepted = UnitBattlefield.CommandHarvestSelected(LocalPlayerSlotId, resourceField, out var status);
                StatusChanged?.Invoke(status);
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Harvest : CommandAcknowledgementKind.Invalid,
                    resourceField.Position,
                    accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            }
            else
            {
                UnitBattlefield.CommandMoveSelected(LocalPlayerSlotId, worldPoint, State.WorldSize, moveMode);
                StatusChanged?.Invoke(MoveModeStatus(moveMode));
                AcknowledgeCommand(
                    CommandAcknowledgementKind.Move,
                    worldPoint,
                    moveMode == MoveCommandMode.Attack ? CommandAcknowledgementAudioCue.Attack : CommandAcknowledgementAudioCue.Move);
            }

            ClearDrag();
            return;
        }

        CollectSelectedLegacyUnits(_legacySelectedUnitCommandBuffer);
        var hasSelectedUnits = _legacySelectedUnitCommandBuffer.Count > 0;
        var enemy = State.PickHostileUnit(worldPoint, ProceduralRts.Core.Owner.Player, PickPaddingWorld());
        if (enemy is not null && hasSelectedUnits)
        {
            State.CommandAttackSelected(enemy);
            AcknowledgeCommand(CommandAcknowledgementKind.Attack, enemy.Position, CommandAcknowledgementAudioCue.Attack);
        }
        else if (State.PickHostileBuilding(worldPoint, ProceduralRts.Core.Owner.Player, PickPaddingWorld()) is { } enemyBuilding && hasSelectedUnits)
        {
            State.CommandAttackSelected(enemyBuilding);
            AcknowledgeCommand(CommandAcknowledgementKind.Attack, enemyBuilding.Position, CommandAcknowledgementAudioCue.Attack);
        }
        else if (PickResourceField(worldPoint) is { } resourceField && HasSelectedHarvester())
        {
            bool accepted;
            string status;
            if (UseUnitBattlefieldInput() && HasSelectedRuntimeHarvester())
            {
                accepted = UnitBattlefield!.CommandHarvestSelected(LocalPlayerSlotId, resourceField, out status);
            }
            else
            {
                accepted = State.CommandHarvestSelected(resourceField, out status);
            }

            StatusChanged?.Invoke(status);
            AcknowledgeCommand(
                accepted ? CommandAcknowledgementKind.Harvest : CommandAcknowledgementKind.Invalid,
                resourceField.Position,
                accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
        }
        else
        {
            if (hasSelectedUnits)
            {
                State.CommandMoveSelected(worldPoint, moveMode);
                StatusChanged?.Invoke(MoveModeStatus(moveMode));
                AcknowledgeCommand(
                    CommandAcknowledgementKind.Move,
                    worldPoint,
                    moveMode == MoveCommandMode.Attack ? CommandAcknowledgementAudioCue.Attack : CommandAcknowledgementAudioCue.Move);
            }
            else
            {
                if (UseUnitBattlefieldInput()
                    && PickResourceField(worldPoint) is { } rallyResource
                    && UnitBattlefield!.HasSelectedBuildings(LocalPlayerSlotId))
                {
                    var accepted = CommandUnitBattlefieldSelectedBuildingRally(rallyResource, out var status);
                    StatusChanged?.Invoke(status);
                    AcknowledgeCommand(
                        accepted ? CommandAcknowledgementKind.Rally : CommandAcknowledgementKind.Invalid,
                        rallyResource.Position,
                        accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
                }
                else
                {
                    var accepted = UseUnitBattlefieldInput()
                        ? CommandUnitBattlefieldSelectedBuildingRally(worldPoint, out var status)
                        : State.CommandSetSelectedBuildingRallyPoint(worldPoint, out status);
                    if (accepted)
                    {
                        StatusChanged?.Invoke(status);
                        AcknowledgeCommand(CommandAcknowledgementKind.Rally, worldPoint, CommandAcknowledgementAudioCue.Move);
                    }
                    else
                    {
                        StatusChanged?.Invoke(status);
                        AcknowledgeCommand(CommandAcknowledgementKind.Invalid, worldPoint, CommandAcknowledgementAudioCue.Invalid);
                    }
                }
            }
        }

        ClearDrag();
    }

    public void SetMoveCommandMode(MoveCommandMode mode)
    {
        CurrentMoveMode = mode;
        StatusChanged?.Invoke(MoveModeStatus(mode));
    }

    private void AcknowledgeCommand(CommandAcknowledgementKind kind, Vector2 position, CommandAcknowledgementAudioCue audioCue)
    {
        CommandAcknowledged?.Invoke(kind, position, audioCue);
    }

    private bool CommandUnitBattlefieldSelectedBuildingRally(Vector2 worldPoint, out string status)
    {
        return UnitBattlefield!.SetSelectedBuildingRallyPoints(LocalPlayerSlotId, worldPoint, out status);
    }

    private bool CommandUnitBattlefieldSelectedBuildingRally(ResourceFieldModel field, out string status)
    {
        return UnitBattlefield!.SetSelectedBuildingRallyPoints(LocalPlayerSlotId, field, out status);
    }

    private MoveCommandMode MoveModeFromModifiers(InputEventMouseButton mouse)
    {
        if (mouse.CtrlPressed)
        {
            return MoveCommandMode.Ignore;
        }

        return mouse.AltPressed ? MoveCommandMode.Attack : CurrentMoveMode;
    }

    private static string MoveModeStatus(MoveCommandMode mode)
    {
        return mode switch
        {
            MoveCommandMode.Attack => GameText.T("move.attack"),
            MoveCommandMode.Ignore => GameText.T("move.ignore"),
            _ => GameText.T("move.direct"),
        };
    }
}
