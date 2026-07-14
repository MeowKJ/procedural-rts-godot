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

    private bool HandleStanceHotkey(InputEventKey key)
    {
        if (!UnitStancePresentationCatalog.TryDefinitionForHotkey((char)key.Keycode, out var presentation))
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

        UnitStanceRequested?.Invoke(presentation.Stance);
        return true;
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
