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
        var count = FinishUnitBattlefieldSelection(worldPoint, startWorld, distance, additive, doubleClick);

        SelectionChanged?.Invoke(count);
        ClearDrag();
    }

    private int FinishUnitBattlefieldSelection(Vector2 worldPoint, Vector2 startWorld, float distance, bool additive, bool doubleClick)
    {
        var isDrag = SelectionGestureMath.IsLeftSelectionDrag(distance);
        if (!isDrag && UnitBattlefield.PickUnit(worldPoint, LocalPlayerSlotId, PickPaddingWorld()) is null)
        {
            return UnitBattlefield.SelectBuildingTargetAt(LocalPlayerSlotId, worldPoint, additive, PickPaddingWorld());
        }

        UnitBattlefield.ClearSelection(LocalPlayerSlotId);
        return !SelectionGestureMath.IsLeftSelectionDrag(distance)
            ? doubleClick
                ? UnitBattlefield.SelectSameUnitsAt(LocalPlayerSlotId, worldPoint, VisibleWorldRect(), additive, PickPaddingWorld())
                : UnitBattlefield.SelectSingleAt(LocalPlayerSlotId, worldPoint, additive, PickPaddingWorld())
            : UnitBattlefield.SelectRect(LocalPlayerSlotId, RectFromPoints(startWorld, worldPoint), additive);
    }

    private void FinishRightClickCommand(Vector2 screenPoint, MoveCommandMode moveMode)
    {
        var worldPoint = ScreenToWorld(screenPoint);
        if (UnitBattlefield.SelectedCount(LocalPlayerSlotId) > 0)
        {
            _lastGatewayRejection = CommandGatewayValidationError.None;
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
            else if (FinishRuntimeRepairCommand(worldPoint))
            {
            }
            else if (PickResourceNode(worldPoint) is { } resourceNode && HasSelectedHarvester())
            {
                var subjects = SelectedRuntimeUnitSubjects();
                var accepted = SubmitRuntimeCommand(
                    PlayerCommandKind.Harvest,
                    PlayerCommandPayload.ForEntityTarget(subjects, resourceNode.EntityId));
                StatusChanged?.Invoke(accepted
                    ? GameText.Format("harvest.assigned", subjects.Count, subjects.Count == 1 ? "" : "s", resourceNode.EntityId.Value)
                    : GatewayRejectedStatus(GameText.T("harvest.selectHarvester")));
                AcknowledgeCommand(
                    accepted ? CommandAcknowledgementKind.Harvest : CommandAcknowledgementKind.Invalid,
                    resourceNode.Position,
                    accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
            }
            else
            {
                var subjects = SelectedRuntimeUnitSubjects();
                var commandKind = moveMode == MoveCommandMode.Attack ? PlayerCommandKind.AttackMove : PlayerCommandKind.Move;
                var accepted = SubmitRuntimeCommand(commandKind, PlayerCommandPayload.ForPoint(subjects, worldPoint.X, worldPoint.Y, moveMode));
                StatusChanged?.Invoke(accepted
                    ? CommandRibbonContextResolver.MoveModeLabel(moveMode)
                    : GatewayRejectedStatus(CommandRibbonContextResolver.MoveModeLabel(moveMode)));
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

        else if (PickResourceNode(worldPoint) is { } rallyResource
            && UnitBattlefield.HasSelectedBuildings(LocalPlayerSlotId))
        {
            FinishSelectedBuildingRallyCommand(rallyResource);
        }
        else
        {
            FinishSelectedBuildingRallyCommand(worldPoint);
        }

        ClearDrag();
    }

    public void SetMoveCommandMode(MoveCommandMode mode)
    {
        CurrentMoveMode = mode;
        StatusChanged?.Invoke(CommandRibbonContextResolver.MoveModeLabel(mode));
    }

    private void AcknowledgeCommand(CommandAcknowledgementKind kind, Vector2 position, CommandAcknowledgementAudioCue audioCue)
    {
        CommandAcknowledged?.Invoke(kind, position, audioCue);
    }

    private bool CommandUnitBattlefieldSelectedBuildingRally(Vector2 worldPoint, out string status)
    {
        var subjects = UnitBattlefield.SelectedBuildingEntityIds(LocalPlayerSlotId);
        var accepted = SubmitRuntimeCommand(PlayerCommandKind.Rally, PlayerCommandPayload.ForPoint(subjects, worldPoint.X, worldPoint.Y));
        status = accepted ? GameText.T("rally.set") : GatewayRejectedStatus(GameText.T("rally.selectProducer"));
        return accepted;
    }

    private bool CommandUnitBattlefieldSelectedBuildingRally(UnitBattlefieldResourceNodeProjection resource, out string status)
    {
        var subjects = UnitBattlefield.SelectedBuildingEntityIds(LocalPlayerSlotId);
        var accepted = SubmitRuntimeCommand(
            PlayerCommandKind.Rally,
            PlayerCommandPayload.ForPoint(subjects, resource.Position.X, resource.Position.Y) with { TargetEntity = resource.EntityId });
        status = accepted ? GameText.T("rally.set") : GatewayRejectedStatus(GameText.T("rally.selectProducer"));
        return accepted;
    }

    private bool CommandUnitBattlefieldSelectedBuildingRally(UnitInstance unit, out string status)
    {
        var subjects = UnitBattlefield.SelectedBuildingEntityIds(LocalPlayerSlotId);
        var accepted = SubmitRuntimeCommand(
            PlayerCommandKind.Rally,
            PlayerCommandPayload.ForPoint(subjects, unit.Position.X, unit.Position.Y) with { TargetEntity = unit.EntityId });
        status = accepted ? GameText.T("rally.set") : GatewayRejectedStatus(GameText.T("rally.selectProducer"));
        return accepted;
    }

    private void FinishSelectedBuildingRallyCommand(UnitBattlefieldResourceNodeProjection resource)
    {
        var accepted = CommandUnitBattlefieldSelectedBuildingRally(resource, out var status);
        StatusChanged?.Invoke(status);
        AcknowledgeCommand(
            accepted ? CommandAcknowledgementKind.Rally : CommandAcknowledgementKind.Invalid,
            resource.Position,
            accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
    }

    private void FinishSelectedBuildingRallyCommand(UnitInstance unit)
    {
        var accepted = CommandUnitBattlefieldSelectedBuildingRally(unit, out var status);
        StatusChanged?.Invoke(status);
        AcknowledgeCommand(
            accepted ? CommandAcknowledgementKind.Rally : CommandAcknowledgementKind.Invalid,
            unit.Position,
            accepted ? CommandAcknowledgementAudioCue.Move : CommandAcknowledgementAudioCue.Invalid);
    }

    private void FinishSelectedBuildingRallyCommand(Vector2 worldPoint)
    {
        var accepted = CommandUnitBattlefieldSelectedBuildingRally(worldPoint, out var status);
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


    private IReadOnlyList<EntityId> SelectedRuntimeUnitSubjects()
    {
        return UnitBattlefield.SelectedUnitEntityIds(LocalPlayerSlotId);
    }

    private bool SubmitRuntimeCommand(PlayerCommandKind kind, PlayerCommandPayload payload)
    {
        var result = UnitBattlefield.SubmitLiveLocalPlayerCommand(LocalPlayerSlotId, kind, payload);
        _lastGatewayRejection = CommandGatewayFeedback.FirstRejection(result);
        return result.AcceptedCount > 0;
    }

    private string GatewayRejectedStatus(string fallback)
    {
        return _lastGatewayRejection == CommandGatewayValidationError.None
            ? fallback
            : CommandGatewayFeedback.RejectionStatus(_lastGatewayRejection);
    }
}
