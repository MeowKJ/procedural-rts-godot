using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private bool HandleMoveModeHotkey(InputEventKey key)
    {
        MoveCommandMode? mode = key.Keycode switch
        {
            Key.F1 => MoveCommandMode.Direct,
            Key.F2 => MoveCommandMode.Attack,
            Key.F3 => MoveCommandMode.Ignore,
            _ => null,
        };

        if (mode is null)
        {
            return false;
        }

        SetMoveCommandMode(mode.Value);
        MoveModeRequested?.Invoke(mode.Value);
        return true;
    }

    private bool HandleStanceHotkey(InputEventKey key)
    {
        UnitStance? stance = key.Keycode switch
        {
            Key.Z => UnitStance.Hold,
            Key.X => UnitStance.Aggressive,
            Key.C => UnitStance.ReturnGuard,
            Key.V => UnitStance.PassiveRetaliate,
            Key.B => UnitStance.Ignore,
            _ => null,
        };

        if (stance is null)
        {
            return false;
        }

        var selectedCount = UseUnitBattlefieldInput()
            ? UnitBattlefield!.SelectedCount(LocalPlayerSlotId)
            : State.SelectedUnitCount();
        if (selectedCount == 0)
        {
            StatusChanged?.Invoke(GameText.T("stance.selectRequired"));
            return true;
        }

        UnitStanceRequested?.Invoke(stance.Value);
        return true;
    }

    private static string StanceLabel(UnitStance stance)
    {
        return stance switch
        {
            UnitStance.Hold => GameText.T("stance.hold"),
            UnitStance.Aggressive => GameText.T("stance.aggressive"),
            UnitStance.ReturnGuard => GameText.T("stance.returnGuard"),
            UnitStance.PassiveRetaliate => GameText.T("stance.passive"),
            UnitStance.Ignore => GameText.T("stance.ignore"),
            _ => stance.ToString(),
        };
    }
}
