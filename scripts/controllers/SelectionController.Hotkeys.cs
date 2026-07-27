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

        var selectedCount = UnitBattlefield.SelectedCount(LocalPlayerSlotId);
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
        return UnitBattlefield.SelectArmy(LocalPlayerSlotId);
    }

    private bool SelectNextIdleHarvester()
    {
        var harvester = UnitBattlefield.SelectNextIdleHarvester(LocalPlayerSlotId);
        if (harvester is null)
        {
            return false;
        }

        SelectionChanged?.Invoke(1);
        return true;
    }

}
