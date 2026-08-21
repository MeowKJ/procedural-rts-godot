using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private UnitInstance? NearestOwnedUnit(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding)
    {
        UnitInstance? best = null;
        var bestDistanceSquared = float.PositiveInfinity;
        foreach (var unit in Units)
        {
            if (unit.PlayerSlotId != playerSlotId)
            {
                continue;
            }

            var distanceSquared = unit.Position.DistanceSquaredTo(worldPoint);
            if (IsCloserUnitPick(unit, distanceSquared, bestDistanceSquared, pickPadding))
            {
                best = unit;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    private UnitInstance? NearestAnyUnit(Vector2 worldPoint, float pickPadding)
    {
        UnitInstance? best = null;
        var bestDistanceSquared = float.PositiveInfinity;
        foreach (var unit in Units)
        {
            var distanceSquared = unit.Position.DistanceSquaredTo(worldPoint);
            if (IsCloserUnitPick(unit, distanceSquared, bestDistanceSquared, pickPadding))
            {
                best = unit;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    private UnitInstance? NearestHostileUnit(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding)
    {
        UnitInstance? best = null;
        var bestDistanceSquared = float.PositiveInfinity;
        foreach (var unit in Units)
        {
            if (!Relations.CanAttack(attackerPlayerSlotId, unit.PlayerSlotId))
            {
                continue;
            }

            var distanceSquared = unit.Position.DistanceSquaredTo(worldPoint);
            if (IsCloserUnitPick(unit, distanceSquared, bestDistanceSquared, pickPadding))
            {
                best = unit;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    private int? NearestOwnedBuildingTargetId(Vector2 worldPoint, PlayerSlotId playerSlotId, float pickPadding)
    {
        return NearestBuildingTargetId(worldPoint, pickPadding, playerSlotId, hostileTo: null);
    }

    private int? NearestHostileBuildingTargetId(Vector2 worldPoint, PlayerSlotId attackerPlayerSlotId, float pickPadding)
    {
        return NearestBuildingTargetId(worldPoint, pickPadding, ownerFilter: null, hostileTo: attackerPlayerSlotId);
    }

    private int? NearestAnyBuildingTargetId(Vector2 worldPoint, float pickPadding)
    {
        return NearestBuildingTargetId(worldPoint, pickPadding, ownerFilter: null, hostileTo: null);
    }

    private int? NearestBuildingTargetId(
        Vector2 worldPoint,
        float pickPadding,
        PlayerSlotId? ownerFilter,
        PlayerSlotId? hostileTo)
    {
        int? bestId = null;
        var bestDistanceSquared = float.PositiveInfinity;
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity))
            {
                continue;
            }

            if (ownerFilter is not null && identity.PlayerSlotId != ownerFilter.Value)
            {
                continue;
            }

            if (hostileTo is not null && !Relations.CanAttack(hostileTo.Value, identity.PlayerSlotId))
            {
                continue;
            }

            if (!entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp <= 0)
            {
                continue;
            }

            var distanceSquared = entity.Transform.Position.DistanceSquaredTo(worldPoint);
            var radius = BuildingTargetRadiusCore(identity.BuildingId, identity.Kind) + pickPadding;
            if (distanceSquared > radius * radius)
            {
                continue;
            }

            if (distanceSquared < bestDistanceSquared
                || (distanceSquared == bestDistanceSquared && (bestId is null || identity.BuildingId < bestId.Value)))
            {
                bestId = identity.BuildingId;
                bestDistanceSquared = distanceSquared;
            }
        }

        return bestId;
    }

    private UnitBattlefieldResourceNodeProjection? NearestResourceNode(Vector2 worldPoint, float pickPadding)
    {
        UnitBattlefieldResourceNodeProjection? best = null;
        var bestDistanceSquared = float.PositiveInfinity;
        foreach (var field in ResourceNodeProjections())
        {
            if (field.Amount <= 0)
            {
                continue;
            }

            var distanceSquared = field.Position.DistanceSquaredTo(worldPoint);
            var radius = field.Radius + pickPadding;
            if (distanceSquared > radius * radius || distanceSquared >= bestDistanceSquared)
            {
                continue;
            }

            best = field;
            bestDistanceSquared = distanceSquared;
        }

        return best;
    }

    private static bool IsCloserUnitPick(UnitInstance unit, float distanceSquared, float bestDistanceSquared, float pickPadding)
    {
        var radius = unit.Spec.Collision.Radius + pickPadding;
        return distanceSquared <= radius * radius && distanceSquared < bestDistanceSquared;
    }
}
