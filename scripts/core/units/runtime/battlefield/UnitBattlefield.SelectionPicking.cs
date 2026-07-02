using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public IEnumerable<UnitInstance> SelectedUnits(PlayerSlotId playerSlotId)
    {
        return Units.Where(unit => unit.PlayerSlotId == playerSlotId && unit.Selected);
    }

    public int SelectedCount(PlayerSlotId playerSlotId)
    {
        return SelectedUnits(playerSlotId).Count();
    }

    public void ClearSelection(PlayerSlotId playerSlotId)
    {
        SubmitSelectionCommand(playerSlotId, []);
    }

    public UnitInstance? PickUnit(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding = 8)
    {
        return NearestOwnedUnit(worldPoint, playerSlotId, pickPadding);
    }

    public UnitInstance? PickAnyUnit(Vector2 worldPoint, float pickPadding = 8)
    {
        return NearestAnyUnit(worldPoint, pickPadding);
    }

    public UnitInstance? PickHostileUnit(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding = 8)
    {
        return NearestHostileUnit(worldPoint, attackerPlayerSlotId, pickPadding);
    }

    private int? PickHostileBuildingIdCore(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding = 8)
    {
        return NearestHostileBuildingTargetId(worldPoint, attackerPlayerSlotId, pickPadding);
    }

    public int? PickHostileBuildingId(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding = 8)
    {
        return PickHostileBuildingIdCore(worldPoint, attackerPlayerSlotId, pickPadding);
    }

    public BuildingHoverProjection? PickHostileBuildingHoverProjection(Vector2 worldPoint, PlayerSlotId viewer, float pickPadding = 8)
    {
        var buildingId = PickHostileBuildingId(worldPoint, viewer, pickPadding);
        return buildingId is null ? null : BuildingHoverProjection(buildingId.Value, viewer);
    }

    private int? PickBuildingTargetIdCore(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding = 8)
    {
        return NearestOwnedBuildingTargetId(worldPoint, playerSlotId, pickPadding);
    }

    public int? PickBuildingTargetId(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding = 8)
    {
        return PickBuildingTargetIdCore(worldPoint, playerSlotId, pickPadding);
    }

    private int? PickAnyBuildingTargetIdCore(Vector2 worldPoint, float pickPadding = 8)
    {
        return NearestAnyBuildingTargetId(worldPoint, pickPadding);
    }

    public int? PickAnyBuildingTargetId(Vector2 worldPoint, float pickPadding = 8)
    {
        return PickAnyBuildingTargetIdCore(worldPoint, pickPadding);
    }

    public BuildingHoverProjection? PickAnyBuildingHoverProjection(Vector2 worldPoint, PlayerSlotId viewer, float pickPadding = 8)
    {
        var buildingId = PickAnyBuildingTargetId(worldPoint, pickPadding);
        return buildingId is null ? null : BuildingHoverProjection(buildingId.Value, viewer);
    }
}
