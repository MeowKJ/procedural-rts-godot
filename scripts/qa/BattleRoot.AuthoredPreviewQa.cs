using Godot;
using ProceduralRts.Core;

namespace ProceduralRts;

public partial class BattleRoot
{
    public int DebugCommandFirstPlayerUnit(Vector2 target)
    {
        var unit = _unitBattlefield.Units.First(value => value.PlayerSlotId == PlayerSlotId.One && value.Hp > 0);
        return _unitBattlefield.CommandMoveUnits(
            PlayerSlotId.One, [unit.Id], target, _state.WorldSize, MoveCommandMode.Direct);
    }
}
