using Godot;

namespace ProceduralRts.Core;

public sealed partial class ResourceSystem
{
    private static EntityInstance? ReservedRefinery(EntityWorld world, EntityInstance harvester, int? refineryId)
    {
        if (refineryId is not int id || !world.TryGet(new EntityId(id), out var refinery))
        {
            return null;
        }

        if (refinery.OwnerId.Value != harvester.OwnerId.Value
            || !refinery.Components.TryGet<DockComponentState>(out var dock))
        {
            return null;
        }

        return dock.ReservedByEntityId == harvester.Id.Value || dock.DockedEntityId == harvester.Id.Value
            ? refinery
            : null;
    }

    private static EntityInstance? ReserveNearestDock(EntityWorld world, EntityInstance harvester)
    {
        EntityInstance? best = null;
        var bestDistanceSq = float.MaxValue;
        foreach (var candidate in world.OrderedEntities)
        {
            if (candidate.OwnerId.Value != harvester.OwnerId.Value
                || !candidate.Components.TryGet<DockComponentState>(out var dock)
                || dock.ReservedByEntityId is not null
                || dock.DockedEntityId is not null)
            {
                continue;
            }

            var distanceSq = harvester.Transform.Position.DistanceSquaredTo(RefineryDockPoint(world, candidate));
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                best = candidate;
            }
        }

        if (best is not null)
        {
            var dock = best.Components.Require<DockComponentState>();
            best.Components.Set(dock with { ReservedByEntityId = harvester.Id.Value });
        }

        return best;
    }

    private static EntityInstance? NearestRefinery(EntityWorld world, EntityInstance harvester)
    {
        EntityInstance? best = null;
        var bestDistanceSq = float.MaxValue;
        foreach (var candidate in world.OrderedEntities)
        {
            if (candidate.OwnerId.Value != harvester.OwnerId.Value
                || !candidate.Components.Has<DockComponentState>())
            {
                continue;
            }

            var distanceSq = harvester.Transform.Position.DistanceSquaredTo(RefineryDockPoint(world, candidate));
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                best = candidate;
            }
        }

        return best;
    }

    private static Vector2 RefineryDockPoint(EntityWorld world, EntityInstance refinery)
    {
        if (world.TryGetSpec(refinery.SpecId, out var entitySpec)
            && entitySpec.Authoring.BuildingSpecId is { } buildingSpecId
            && BuildSpecCatalog.Definitions.TryGetValue(buildingSpecId, out var buildSpec)
            && PlacementReservationMath.TryCenter(
                buildSpec,
                PlacementReservationKind.RefineryDock,
                refinery.Transform.Position,
                refinery.Transform.Facing,
                out var dockPoint))
        {
            return dockPoint;
        }

        return refinery.Transform.Position;
    }

    private static float DockArrivalDistance(EntityWorld world)
    {
        return world.EconomyTuning.DockDistance;
    }

    private static void ReleaseDock(EntityWorld world, int harvesterId, int? refineryId)
    {
        if (refineryId is not int id
            || !world.TryGet(new EntityId(id), out var refinery)
            || !refinery.Components.TryGet<DockComponentState>(out var dock))
        {
            return;
        }

        if (dock.ReservedByEntityId == harvesterId || dock.DockedEntityId == harvesterId)
        {
            refinery.Components.Set(dock with
            {
                ReservedByEntityId = dock.ReservedByEntityId == harvesterId ? null : dock.ReservedByEntityId,
                DockedEntityId = dock.DockedEntityId == harvesterId ? null : dock.DockedEntityId,
            });
        }
    }
}
