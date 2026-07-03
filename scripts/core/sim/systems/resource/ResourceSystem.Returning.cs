namespace ProceduralRts.Core;

public sealed partial class ResourceSystem
{
    private static void StepReturningToRefinery(
        EntityWorld world,
        float dt,
        EntityInstance harvester,
        HarvesterComponentState state,
        ResourceCargoComponentState cargo)
    {
        if (state.Retreating && cargo.Cargo <= 0)
        {
            RetreatToNearestRefinery(world, harvester, state);
            return;
        }

        if (cargo.Cargo <= 0)
        {
            ReturnToFieldOrIdle(world, harvester, state);
            return;
        }

        var refinery = ReservedRefinery(world, harvester, state.RefineryId)
            ?? ReserveNearestDock(world, harvester);
        if (refinery is null)
        {
            StopMoving(harvester);
            world.Metrics.RecordDockWait(harvester.Id.Value, dt);
            return;
        }

        world.Metrics.ClearDockWait(harvester.Id.Value);
        SetMoveTarget(harvester, DockApproachPoint(world, harvester, refinery));
        if (harvester.Transform.Position.DistanceTo(refinery.Transform.Position) > DockArrivalDistance(world, refinery))
        {
            if (state.RefineryId != refinery.Id.Value)
            {
                harvester.Components.Set(state with { RefineryId = refinery.Id.Value });
            }

            return;
        }

        StopMoving(harvester);
        var dock = refinery.Components.Require<DockComponentState>();
        refinery.Components.Set(dock with
        {
            ReservedByEntityId = null,
            DockedEntityId = harvester.Id.Value,
        });
        harvester.Components.Set(state with
        {
            Mode = HarvesterMode.Unloading,
            RefineryId = refinery.Id.Value,
        });
    }

    private static void SendToRefinery(EntityWorld world, EntityInstance harvester, HarvesterComponentState state)
    {
        var refinery = ReservedRefinery(world, harvester, state.RefineryId)
            ?? ReserveNearestDock(world, harvester);
        if (refinery is null)
        {
            StopMoving(harvester);
            harvester.Components.Set(state with { Mode = HarvesterMode.ReturningToRefinery, RefineryId = null });
            return;
        }

        SetMoveTarget(harvester, DockApproachPoint(world, harvester, refinery));
        harvester.Components.Set(state with
        {
            Mode = HarvesterMode.ReturningToRefinery,
            RefineryId = refinery.Id.Value,
        });
    }

    private static void RetreatToNearestRefinery(EntityWorld world, EntityInstance harvester, HarvesterComponentState state)
    {
        var refinery = NearestRefinery(world, harvester);
        if (refinery is null)
        {
            ResetHarvester(harvester, state);
            return;
        }

        SetMoveTarget(harvester, DockApproachPoint(world, harvester, refinery));
        if (harvester.Transform.Position.DistanceTo(refinery.Transform.Position) > DockArrivalDistance(world, refinery))
        {
            harvester.Components.Set(state with
            {
                Mode = HarvesterMode.ReturningToRefinery,
                RefineryId = refinery.Id.Value,
                Retreating = true,
            });
            return;
        }

        ResetHarvester(harvester, state);
    }

    private static void ReturnToFieldOrIdle(EntityWorld world, EntityInstance harvester, HarvesterComponentState state)
    {
        if (TryGetResourceNode(world, state.FieldId, out var field, out var node) && node.Amount > 0)
        {
            SetMoveTarget(harvester, field.Transform.Position);
            harvester.Components.Set(state with
            {
                Mode = HarvesterMode.MovingToField,
                RefineryId = null,
            });
            return;
        }

        if (ResourceMiningMath.TryFindNearestAvailableResourceNode(world, harvester.Transform.Position, out var nextField, out _))
        {
            SetMoveTarget(harvester, nextField.Transform.Position);
            harvester.Components.Set(state with
            {
                Mode = HarvesterMode.MovingToField,
                FieldId = nextField.Id.Value,
                RefineryId = null,
            });
            return;
        }

        ResetHarvester(harvester, state);
    }
}
