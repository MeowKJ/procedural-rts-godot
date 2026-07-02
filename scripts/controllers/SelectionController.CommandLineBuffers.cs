using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.Controllers;

public partial class SelectionController
{
    private void CollectLegacyCommandLineUnits(List<UnitModel> result)
    {
        result.Clear();
        foreach (var unit in State.Units)
        {
            if (unit.Owner == ProceduralRts.Core.Owner.Player
                && unit.Selected
                && (unit.CommandVisualTarget is not null || unit.FormationSlot is not null))
            {
                result.Add(unit);
            }
        }
    }

    private void CollectRuntimeCommandLineUnits(List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in UnitBattlefield!.Units)
        {
            if (unit.PlayerSlotId == LocalPlayerSlotId
                && unit.Selected
                && (unit.CommandVisualTarget is not null || unit.FormationSlot is not null))
            {
                result.Add(unit);
            }
        }
    }

    private static (int X, int Y) CommandLineTargetKey(Vector2 visualTarget)
    {
        return (Mathf.RoundToInt(visualTarget.X / 4f), Mathf.RoundToInt(visualTarget.Y / 4f));
    }
}
