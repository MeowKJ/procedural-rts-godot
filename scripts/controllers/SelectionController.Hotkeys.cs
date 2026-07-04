using Godot;
using ProceduralRts.Core;
using ProceduralRts.Ui;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private bool HandleSelectionHotkey(InputEventKey key)
    {
        if (key.Keycode == Key.A && key.CtrlPressed)
        {
            var selected = SelectArmy();
            SelectionChanged?.Invoke(selected);
            StatusChanged?.Invoke(selected == 0
                ? GameText.T("selection.noArmy")
                : GameText.Format("selection.armySelected", selected));
            AudioCueRequested?.Invoke(selected == 0 ? TacticalAudioCue.Invalid : TacticalAudioCue.Selection);
            return true;
        }

        if (key.Keycode == Key.H)
        {
            var selected = SelectNextIdleHarvester();
            StatusChanged?.Invoke(selected
                ? GameText.T("selection.idleHarvesterSelected")
                : GameText.T("selection.noIdleHarvester"));
            AudioCueRequested?.Invoke(selected ? TacticalAudioCue.Selection : TacticalAudioCue.Invalid);
            return true;
        }

        return false;
    }

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

    private int SelectArmy()
    {
        return UseUnitBattlefieldInput()
            ? UnitBattlefield!.SelectArmy(LocalPlayerSlotId)
            : SelectLegacyArmy();
    }

    private bool SelectNextIdleHarvester()
    {
        if (UseUnitBattlefieldInput())
        {
            var harvester = UnitBattlefield!.SelectNextIdleHarvester(LocalPlayerSlotId);
            if (harvester is null)
            {
                return false;
            }

            SelectionChanged?.Invoke(1);
            return true;
        }

        if (SelectNextLegacyIdleHarvester() is null)
        {
            return false;
        }

        SelectionChanged?.Invoke(1);
        return true;
    }

    private int SelectLegacyArmy()
    {
        _selectionHotkeyUnitIdBuffer.Clear();
        foreach (var unit in State.Units)
        {
            if (unit.Owner == ProceduralRts.Core.Owner.Player
                && unit.Hp > 0
                && !IsHarvester(unit))
            {
                _selectionHotkeyUnitIdBuffer.Add(unit.Id);
            }
        }

        return State.SelectUnitsByIds(_selectionHotkeyUnitIdBuffer);
    }

    private UnitModel? SelectNextLegacyIdleHarvester()
    {
        var selectedIdleSeen = false;
        UnitModel? firstIdleHarvester = null;
        UnitModel? nextIdleHarvester = null;
        foreach (var unit in State.Units)
        {
            if (unit.Owner != ProceduralRts.Core.Owner.Player
                || unit.Hp <= 0
                || !IsHarvester(unit)
                || unit.HarvesterMode != HarvesterMode.Idle
                || unit.MoveTarget is not null)
            {
                continue;
            }

            firstIdleHarvester ??= unit;
            if (selectedIdleSeen)
            {
                nextIdleHarvester = unit;
                break;
            }

            if (unit.Selected)
            {
                selectedIdleSeen = true;
            }
        }

        var target = nextIdleHarvester ?? firstIdleHarvester;
        if (target is null)
        {
            return null;
        }

        _selectionHotkeyUnitIdBuffer.Clear();
        _selectionHotkeyUnitIdBuffer.Add(target.Id);
        State.SelectUnitsByIds(_selectionHotkeyUnitIdBuffer);
        return target;
    }
}
