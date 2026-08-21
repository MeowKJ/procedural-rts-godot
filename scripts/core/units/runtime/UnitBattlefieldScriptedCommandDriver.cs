namespace ProceduralRts.Core;

internal static class UnitBattlefieldScriptedCommandDriver
{
    public static CommandGatewayResult Submit(
        UnitBattlefield battlefield,
        string controllerScope,
        PlayerSlotId playerSlotId,
        PlayerCommandKind kind,
        PlayerCommandPayload payload)
    {
        return battlefield.SubmitLivePlayerCommand(
            new PlayerControllerId($"{controllerScope}-slot-{playerSlotId.Value}"),
            PlayerControllerKind.ScriptedBot,
            [playerSlotId],
            playerSlotId,
            kind,
            payload);
    }
}
