using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public Vector2 LiveBuildingCenterOrUnitCenter(PlayerSlotId playerSlotId, Vector2 fallback)
    {
        var sum = Vector2.Zero;
        var count = 0;
        foreach (var building in BuildingSnapshots())
        {
            if (building.PlayerSlotId != playerSlotId || building.Hp <= 0)
            {
                continue;
            }

            sum += building.Position;
            count++;
        }

        return count > 0 ? sum / count : LiveUnitCenterOrFallback(playerSlotId, fallback);
    }

    public Vector2 LiveUnitCenterOrFallback(PlayerSlotId playerSlotId, Vector2 fallback)
    {
        var sum = Vector2.Zero;
        var count = 0;
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId != playerSlotId || unit.Hp <= 0)
            {
                continue;
            }

            sum += unit.Position;
            count++;
        }

        return count > 0 ? sum / count : fallback;
    }
}
