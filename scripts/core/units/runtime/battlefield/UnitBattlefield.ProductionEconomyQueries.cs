using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public int SetMissingProducerRallyPoints(PlayerSlotId playerSlotId, Vector2 rally)
    {
        var count = 0;
        var buildings = BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            var building = buildings[index];
            if (building.PlayerSlotId == playerSlotId && BuildingRallyPoint(building.Id) is null && SetRallyPoint(building.Id, rally, out _))
            {
                count++;
            }
        }

        return count;
    }

    public Vector2 LiveBuildingCenterOrFallback(PlayerSlotId playerSlotId, Vector2 fallback)
    {
        var sum = Vector2.Zero;
        var count = 0;
        var buildings = BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            var building = buildings[index];
            if (building.PlayerSlotId != playerSlotId || building.Hp <= 0)
            {
                continue;
            }

            sum += building.Position;
            count++;
        }

        return count == 0 ? fallback : sum / count;
    }

    public UnitFactionId FirstOwnedBuildingFactionOrDefault(PlayerSlotId playerSlotId, UnitFactionId fallback)
    {
        var buildings = BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            if (buildings[index].PlayerSlotId == playerSlotId)
            {
                return buildings[index].Faction;
            }
        }

        return fallback;
    }

    public void CollectIdleEconomyUnits(PlayerSlotId playerSlotId, List<UnitInstance> result)
    {
        result.Clear();
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId == playerSlotId
                && unit.Hp > 0
                && unit.Spec.RoleTags.Contains(UnitRoleTag.Economy)
                && (unit.HarvesterMode == HarvesterMode.Idle || unit.HarvestResourceEntityId is null))
            {
                result.Add(unit);
            }
        }

        result.Sort(CompareUnitIds);
    }

    public UnitBattlefieldResourceNodeProjection? NearestVisibleResourceNode(OwnerId owner, Vector2 origin)
    {
        UnitBattlefieldResourceNodeProjection? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var resource in ResourceNodeProjections())
        {
            if (resource.Amount <= 0
                || !EntityWorld.Visibility.IsVisible(owner, resource.EntityId))
            {
                continue;
            }

            var distance = resource.Position.DistanceSquaredTo(origin);
            if (distance < bestDistance)
            {
                best = resource;
                bestDistance = distance;
            }
        }

        return best;
    }

    public void CollectOwnedBuildings(PlayerSlotId playerSlotId, List<UnitBattlefieldBuildingSnapshot> result, bool liveOnly)
    {
        result.Clear();
        var buildings = BuildingSnapshots();
        for (var index = 0; index < buildings.Count; index++)
        {
            var building = buildings[index];
            if (building.PlayerSlotId != playerSlotId || (liveOnly && building.Hp <= 0))
            {
                continue;
            }

            result.Add(building);
        }
    }

    private static int CompareUnitIds(UnitInstance left, UnitInstance right)
    {
        return left.Id.CompareTo(right.Id);
    }
}
