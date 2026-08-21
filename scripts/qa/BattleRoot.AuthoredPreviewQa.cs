using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    public int DebugCommandFirstPlayerUnit(Vector2 target)
    {
        var unit = _unitBattlefield.Units.First(value => value.PlayerSlotId == PlayerSlotId.One && value.Hp > 0);
        var result = SubmitQaPlayerCommand(
            PlayerSlotId.One,
            PlayerCommandKind.Move,
            PlayerCommandPayload.ForPoint([unit.EntityId], target.X, target.Y));
        return result.AcceptedCount;
    }

    private CommandGatewayResult SubmitQaPlayerCommand(
        PlayerSlotId playerSlotId,
        PlayerCommandKind kind,
        PlayerCommandPayload payload)
    {
        return _unitBattlefield.SubmitLivePlayerCommand(
            new PlayerControllerId($"battle-root-qa-slot-{playerSlotId.Value}"),
            PlayerControllerKind.QaAgent,
            [playerSlotId],
            playerSlotId,
            kind,
            payload);
    }
}
