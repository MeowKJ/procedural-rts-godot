using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Tools.Qa;

public static class QaPlayerCommandDriver
{
    public static CommandGatewayResult MoveSelection(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        Vector2 target,
        MoveCommandMode mode = MoveCommandMode.Direct)
    {
        var kind = mode == MoveCommandMode.Attack
            ? PlayerCommandKind.AttackMove
            : PlayerCommandKind.Move;
        return Submit(
            battlefield,
            playerSlotId,
            kind,
            PlayerCommandPayload.ForPoint(
                battlefield.SelectedUnitEntityIds(playerSlotId),
                target.X,
                target.Y,
                mode));
    }

    public static CommandGatewayResult AttackSelection(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        UnitInstance target)
    {
        return Submit(
            battlefield,
            playerSlotId,
            PlayerCommandKind.Attack,
            PlayerCommandPayload.ForEntityTarget(
                battlefield.SelectedUnitEntityIds(playerSlotId),
                target.EntityId));
    }

    public static CommandGatewayResult AttackBuildingSelection(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        int buildingId)
    {
        return Submit(
            battlefield,
            playerSlotId,
            PlayerCommandKind.Attack,
            PlayerCommandPayload.ForEntityTarget(
                battlefield.SelectedUnitEntityIds(playerSlotId),
                battlefield.BuildingEntityIdByTargetId(buildingId) ?? default,
                CombatTargetKind.Building));
    }

    public static CommandGatewayResult StopSelection(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId)
    {
        return Submit(
            battlefield,
            playerSlotId,
            PlayerCommandKind.Stop,
            PlayerCommandPayload.ForSubjects(battlefield.SelectedUnitEntityIds(playerSlotId)));
    }

    public static CommandGatewayResult SetSelectionStance(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        UnitStance stance)
    {
        return Submit(
            battlefield,
            playerSlotId,
            PlayerCommandKind.SetStance,
            PlayerCommandPayload.ForSubjects(battlefield.SelectedUnitEntityIds(playerSlotId)) with
            {
                Stance = stance,
            });
    }

    public static CommandGatewayResult HarvestSelection(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        ResourceFieldModel field)
    {
        battlefield.TryGetResourceEntityId(field, out var target);
        return Submit(
            battlefield,
            playerSlotId,
            PlayerCommandKind.Harvest,
            PlayerCommandPayload.ForEntityTarget(
                battlefield.SelectedUnitEntityIds(playerSlotId),
                target));
    }

    private static CommandGatewayResult Submit(
        UnitBattlefield battlefield,
        PlayerSlotId playerSlotId,
        PlayerCommandKind kind,
        PlayerCommandPayload payload)
    {
        return battlefield.SubmitLivePlayerCommand(
            new PlayerControllerId($"qa-slot-{playerSlotId.Value}"),
            PlayerControllerKind.QaAgent,
            [playerSlotId],
            playerSlotId,
            kind,
            payload);
    }
}
