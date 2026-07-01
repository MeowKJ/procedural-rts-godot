namespace ProceduralRts.Core;

/// <summary>
/// Deterministic economy loop for EntityWorld: harvesters gather from resource
/// nodes, reserve a refinery dock, unload cargo into owner Credits, then return.
/// </summary>
public sealed partial class ResourceSystem : ISimSystem
{
    public void Step(SimContext context)
    {
        context.World.Metrics.RecordEconomyTick(context.FixedDelta);
        StepResourceRegeneration(context.World, context.FixedDelta);
        foreach (var harvester in context.World.OrderedEntities)
        {
            if (!harvester.Components.TryGet<HarvesterComponentState>(out var state)
                || !harvester.Components.TryGet<ResourceCargoComponentState>(out var cargo))
            {
                continue;
            }

            RecordHarvesterModeMetrics(context.World, context.FixedDelta, harvester, state);
            StepHarvester(context.World, context.FixedDelta, harvester, state, cargo);
        }
    }

    private static void RecordHarvesterModeMetrics(
        EntityWorld world,
        float dt,
        EntityInstance harvester,
        HarvesterComponentState state)
    {
        if (state.Mode == HarvesterMode.Idle)
        {
            world.Metrics.RecordHarvesterIdle(dt);
            world.Metrics.ClearDockWait(harvester.Id.Value);
            return;
        }

        world.Metrics.RecordHarvesterActiveTrip(dt);
        if (state.Mode != HarvesterMode.ReturningToRefinery)
        {
            world.Metrics.ClearDockWait(harvester.Id.Value);
        }
    }
}
