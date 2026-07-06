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
            _lastGatewayRejectedStatus = string.Empty;
            if (UnitBattlefield.PickHostileUnit(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } unitInstanceEnemy)
            {
                var accepted = SubmitRuntimeCommand(
                    PlayerCommandKind.Attack,
                    PlayerCommandPayload.ForEntityTarget(SelectedRuntimeUnitSubjects(), unitInstanceEnemy.EntityId));
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Attack : CommandAcknowledgementKind.Invalid,
                    unitInstanceEnemy.Position,
                    accepted ? CommandAcknowledgementAudioCue.Attack : CommandAcknowledgementAudioCue.Invalid);
            }
            else if (UnitBattlefield.PickHostileBuildingHoverProjection(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } unitInstanceBuildingEnemy)
            {
                var accepted = UnitBattlefield.BuildingEntityIdByTargetId(unitInstanceBuildingEnemy.Id) is { } targetEntity
                    && SubmitRuntimeCommand(
                        PlayerCommandKind.Attack,
                        PlayerCommandPayload.ForEntityTarget(SelectedRuntimeUnitSubjects(), targetEntity, CombatTargetKind.Building));
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Attack : CommandAcknowledgementKind.Invalid,
                    unitInstanceBuildingEnemy.Position,
                    accepted ? CommandAcknowledgementAudioCue.Attack : CommandAcknowledgementAudioCue.Invalid);
            }
            else if (UnitBattlefield.PickAnyUnit(worldPoint, PickPaddingWorld()) is { } repairUnit
                && UnitBattlefield.CanRepairSelected(LocalPlayerSlotId, repairUnit))
            {
                var accepted = SubmitRuntimeCommand(
                    PlayerCommandKind.Repair,
                    PlayerCommandPayload.ForEntityTarget(SelectedRuntimeUnitSubjects(), repairUnit.EntityId));
                StatusChanged?.Invoke(GameText.T("ui.context.repair"));
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid,
                    repairUnit.Position,
                    accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            }
            else if (UnitBattlefield.PickAnyBuildingHoverProjection(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is { } repairBuilding
                && UnitBattlefield.CanRepairSelectedBuilding(LocalPlayerSlotId, repairBuilding.Id))
            {
                var accepted = UnitBattlefield.BuildingEntityIdByTargetId(repairBuilding.Id) is { } targetEntity
                    && SubmitRuntimeCommand(
                        PlayerCommandKind.Repair,
                        PlayerCommandPayload.ForEntityTarget(SelectedRuntimeUnitSubjects(), targetEntity, CombatTargetKind.Building));
                StatusChanged?.Invoke(GameText.T("ui.context.repair"));
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Repair : CommandAcknowledgementKind.Invalid,
                    repairBuilding.Position,
                    accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            }
            else if (PickResourceField(worldPoint) is { } resourceField && HasSelectedHarvester())
            {
                var subjects = SelectedRuntimeUnitSubjects();
                var accepted = UnitBattlefield.TryGetResourceEntityId(resourceField, out var resourceEntity)
                    && SubmitRuntimeCommand(
                        PlayerCommandKind.Harvest,
                        PlayerCommandPayload.ForEntityTarget(subjects, resourceEntity));
                StatusChanged?.Invoke(accepted
                    ? GameText.Format("harvest.assigned", subjects.Count, subjects.Count == 1 ? "" : "s", resourceField.Id)
                    : GatewayRejectedStatus(GameText.T("harvest.selectHarvester")));
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Harvest : CommandAcknowledgementKind.Invalid,
                    resourceField.Position,
                    accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            }
            else
            {
                var subjects = SelectedRuntimeUnitSubjects();
                var commandKind = moveMode == MoveCommandMode.Attack ? PlayerCommandKind.AttackMove : PlayerCommandKind.Move;
                var accepted = SubmitRuntimeCommand(commandKind, PlayerCommandPayload.ForPoint(subjects, worldPoint.X, worldPoint.Y, moveMode));
                StatusChanged?.Invoke(accepted ? MoveModeStatus(moveMode) : GatewayRejectedStatus(MoveModeStatus(moveMode)));
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Move : CommandAcknowledgementKind.Invalid,
                    worldPoint,
                    accepted
                        ? moveMode == MoveCommandMode.Attack ? CommandAcknowledgementAudioCue.Attack : CommandAcknowledgementAudioCue.Move
                        : CommandAcknowledgementAudioCue.Invalid);
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
                    FinishSelectedBuildingRallyCommand(rallyResource);
                }
                else
                {
                    FinishSelectedBuildingRallyCommand(worldPoint);
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
        var subjects = UnitBattlefield!.SelectedBuildingEntityIds(LocalPlayerSlotId);
        var accepted = SubmitRuntimeCommand(PlayerCommandKind.Rally, PlayerCommandPayload.ForPoint(subjects, worldPoint.X, worldPoint.Y));
        status = accepted ? GameText.T("rally.set") : GatewayRejectedStatus(GameText.T("rally.selectProducer"));
        return accepted;
    }

    private bool CommandUnitBattlefieldSelectedBuildingRally(ResourceFieldModel field, out string status)
    {
        var subjects = UnitBattlefield!.SelectedBuildingEntityIds(LocalPlayerSlotId);
        var accepted = UnitBattlefield.TryGetResourceEntityId(field, out var resourceEntity)
            && SubmitRuntimeCommand(
                PlayerCommandKind.Rally,
                PlayerCommandPayload.ForPoint(subjects, field.Position.X, field.Position.Y) with { TargetEntity = resourceEntity });
        status = accepted ? GameText.T("rally.set") : GatewayRejectedStatus(GameText.T("rally.selectProducer"));
        return accepted;
    }

    private void FinishSelectedBuildingRallyCommand(ResourceFieldModel resourceField)
    {
        var accepted = CommandUnitBattlefieldSelectedBuildingRally(resourceField, out var status);
        StatusChanged?.Invoke(status);
        AcknowledgeCommand(
            accepted ? CommandAcknowledgementKind.Rally : CommandAcknowledgementKind.Invalid,
            resourceField.Position,
            accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
    }

    private void FinishSelectedBuildingRallyCommand(Vector2 worldPoint)
    {
        var accepted = UseUnitBattlefieldInput()
            ? CommandUnitBattlefieldSelectedBuildingRally(worldPoint, out var status)
            : State.CommandSetSelectedBuildingRallyPoint(worldPoint, out status);
        StatusChanged?.Invoke(status);
        AcknowledgeCommand(
            accepted ? CommandAcknowledgementKind.Rally : CommandAcknowledgementKind.Invalid,
            worldPoint,
            accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
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

    private IReadOnlyList<EntityId> SelectedRuntimeUnitSubjects()
    {
        return UnitBattlefield!.SelectedUnitEntityIds(LocalPlayerSlotId);
    }

    private bool SubmitRuntimeCommand(PlayerCommandKind kind, PlayerCommandPayload payload)
    {
        var result = UnitBattlefield!.SubmitLiveLocalPlayerCommand(LocalPlayerSlotId, kind, payload);
        _lastGatewayRejectedStatus = FirstGatewayRejection(result);
        return result.AcceptedCount > 0;
    }

    private string GatewayRejectedStatus(string fallback)
    {
        return string.IsNullOrWhiteSpace(_lastGatewayRejectedStatus) ? fallback : _lastGatewayRejectedStatus;
    }

    private static string FirstGatewayRejection(CommandGatewayResult result)
    {
        foreach (var command in result.Commands)
        {
            if (!command.Accepted)
            {
                return command.Message;
            }
        }

        return string.Empty;
    }
}
