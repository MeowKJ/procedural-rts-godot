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
                CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Attack, unitInstanceEnemy.Position);
                AudioCueRequested?.Invoke(TacticalAudioCue.Attack);
            }
            else if (UnitBattlefield.PickHostileBuildingHoverProjection(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } unitInstanceBuildingEnemy)
            {
                UnitBattlefield.CommandAttackSelected(LocalPlayerSlotId, unitInstanceBuildingEnemy.Id);
                CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Attack, unitInstanceBuildingEnemy.Position);
                AudioCueRequested?.Invoke(TacticalAudioCue.Attack);
            }
            else if (UnitBattlefield.PickAnyUnit(worldPoint, PickPaddingWorld()) is { } repairUnit
                && UnitBattlefield.CanRepairSelected(LocalPlayerSlotId, repairUnit))
            {
                var accepted = UnitBattlefield.CommandRepairSelected(LocalPlayerSlotId, repairUnit, out var status);
                StatusChanged?.Invoke(status);
                CommandAcknowledged?.Invoke(accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid, repairUnit.Position);
                AudioCueRequested?.Invoke(accepted ? TacticalAudioCue.Move : TacticalAudioCue.Invalid);
            }
            else if (UnitBattlefield.PickAnyBuildingHoverProjection(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } repairBuilding
                && UnitBattlefield.CanRepairSelectedBuilding(LocalPlayerSlotId, repairBuilding.Id))
            {
                var accepted = UnitBattlefield.CommandRepairSelectedBuilding(LocalPlayerSlotId, repairBuilding.Id, out var status);
                StatusChanged?.Invoke(status);
                CommandAcknowledged?.Invoke(accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid, repairBuilding.Position);
                AudioCueRequested?.Invoke(accepted ? TacticalAudioCue.Move : TacticalAudioCue.Invalid);
            }
            else if (PickResourceField(worldPoint) is { } resourceField && HasSelectedHarvester())
            {
                var accepted = UnitBattlefield.CommandHarvestSelected(LocalPlayerSlotId, resourceField, out var status);
                StatusChanged?.Invoke(status);
                CommandAcknowledged?.Invoke(accepted ? CommandAcknowledgementKind.Harvest : CommandAcknowledgementKind.Invalid, resourceField.Position);
                AudioCueRequested?.Invoke(accepted ? TacticalAudioCue.Move : TacticalAudioCue.Invalid);
            }
            else
            {
                UnitBattlefield.CommandMoveSelected(LocalPlayerSlotId, worldPoint, State.WorldSize, moveMode);
                StatusChanged?.Invoke(MoveModeStatus(moveMode));
                CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Move, worldPoint);
                AudioCueRequested?.Invoke(moveMode == MoveCommandMode.Attack ? TacticalAudioCue.Attack : TacticalAudioCue.Move);
            }

            ClearDrag();
            return;
        }

        var selectedUnits = State.SelectedUnits().ToList();
        var enemy = State.PickHostileUnit(worldPoint, ProceduralRts.Core.Owner.Player, PickPaddingWorld());
        if (enemy is not null && selectedUnits.Count > 0)
        {
            State.CommandAttackSelected(enemy);
            CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Attack, enemy.Position);
            AudioCueRequested?.Invoke(TacticalAudioCue.Attack);
        }
        else if (State.PickHostileBuilding(worldPoint, ProceduralRts.Core.Owner.Player, PickPaddingWorld()) is { } enemyBuilding && selectedUnits.Count > 0)
        {
            State.CommandAttackSelected(enemyBuilding);
            CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Attack, enemyBuilding.Position);
            AudioCueRequested?.Invoke(TacticalAudioCue.Attack);
        }
        else if (PickResourceField(worldPoint) is { } resourceField && HasSelectedHarvester())
        {
            bool accepted;
            string status;
            if (UseUnitBattlefieldInput() && UnitBattlefield!.SelectedUnits(LocalPlayerSlotId).Any(IsHarvester))
            {
                accepted = UnitBattlefield.CommandHarvestSelected(LocalPlayerSlotId, resourceField, out status);
            }
            else
            {
                accepted = State.CommandHarvestSelected(resourceField, out status);
            }

            StatusChanged?.Invoke(status);
            CommandAcknowledged?.Invoke(accepted ? CommandAcknowledgementKind.Harvest : CommandAcknowledgementKind.Invalid, resourceField.Position);
            AudioCueRequested?.Invoke(accepted ? TacticalAudioCue.Move : TacticalAudioCue.Invalid);
        }
        else
        {
            if (selectedUnits.Count > 0)
            {
                State.CommandMoveSelected(worldPoint, moveMode);
                StatusChanged?.Invoke(MoveModeStatus(moveMode));
                CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Move, worldPoint);
                AudioCueRequested?.Invoke(moveMode == MoveCommandMode.Attack ? TacticalAudioCue.Attack : TacticalAudioCue.Move);
            }
            else
            {
                if (UseUnitBattlefieldInput()
                    && PickResourceField(worldPoint) is { } rallyResource
                    && UnitBattlefield!.HasSelectedBuildings(LocalPlayerSlotId))
                {
                    var accepted = CommandUnitBattlefieldSelectedBuildingRally(rallyResource, out var status);
                    StatusChanged?.Invoke(status);
                    CommandAcknowledged?.Invoke(accepted ? CommandAcknowledgementKind.Rally : CommandAcknowledgementKind.Invalid, rallyResource.Position);
                    AudioCueRequested?.Invoke(accepted ? TacticalAudioCue.Move : TacticalAudioCue.Invalid);
                }
                else
                {
                    var accepted = UseUnitBattlefieldInput()
                        ? CommandUnitBattlefieldSelectedBuildingRally(worldPoint, out var status)
                        : State.CommandSetSelectedBuildingRallyPoint(worldPoint, out status);
                    if (accepted)
                    {
                        StatusChanged?.Invoke(status);
                        CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Rally, worldPoint);
                        AudioCueRequested?.Invoke(TacticalAudioCue.Move);
                    }
                    else
                    {
                        StatusChanged?.Invoke(status);
                        CommandAcknowledged?.Invoke(CommandAcknowledgementKind.Invalid, worldPoint);
                        AudioCueRequested?.Invoke(TacticalAudioCue.Invalid);
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
