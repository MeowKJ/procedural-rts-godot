namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private bool CollectSelectedBuildingRallyProducerIds(PlayerSlotId playerSlotId, List<int> result)
    {
        result.Clear();
        var hasSelected = false;
        CollectBuildingTargetIds(_buildingTargetIdBuffer);
        foreach (var buildingId in _buildingTargetIdBuffer)
        {
            if (BuildingIdentity(buildingId)?.PlayerSlotId != playerSlotId
                || BuildingProjection(buildingId)?.Selected != true)
            {
                continue;
            }

            hasSelected = true;
            if (HasAnyProductionForCore(buildingId))
            {
                result.Add(buildingId);
            }
        }

        result.Sort(CompareBuildingIds);
        return hasSelected;
    }
}
