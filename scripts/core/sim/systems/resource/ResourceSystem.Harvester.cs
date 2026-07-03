using Godot;

namespace ProceduralRts.Core;

public sealed partial class ResourceSystem
{
    private static void StepHarvester(
        EntityWorld world,
        float dt,
        EntityInstance harvester,
        HarvesterComponentState state,
        ResourceCargoComponentState cargo)
    {
        if (state.Retreating && state.Mode is HarvesterMode.MovingToField or HarvesterMode.Gathering)
        {
            SendToRefinery(world, harvester, state);
            return;
        }

        switch (state.Mode)
        {
            case HarvesterMode.MovingToField:
                StepMovingToField(world, harvester, state, cargo);
                break;
            case HarvesterMode.Gathering:
                StepGathering(world, dt, harvester, state, cargo);
                break;
            case HarvesterMode.ReturningToRefinery:
                StepReturningToRefinery(world, dt, harvester, state, cargo);
                break;
            case HarvesterMode.Unloading:
                StepUnloading(world, dt, harvester, state, cargo);
                break;
        }
    }

    private static void StepMovingToField(
        EntityWorld world,
        EntityInstance harvester,
        HarvesterComponentState state,
        ResourceCargoComponentState cargo)
    {
        if (!TryGetResourceNode(world, state.FieldId, out var field, out var node))
        {
            ReturnToFieldOrIdle(world, harvester, state);
            return;
        }

        if (node.Amount <= 0 && cargo.Cargo <= 0)
        {
            ReturnToFieldOrIdle(world, harvester, state);
            return;
        }

        SetMoveTarget(harvester, field.Transform.Position);
        if (harvester.Transform.Position.DistanceTo(field.Transform.Position) > world.EconomyTuning.GatherDistance)
        {
            return;
        }

        StopMoving(harvester);
        harvester.Components.Set(state with { Mode = HarvesterMode.Gathering });
    }

    private static void StepGathering(
        EntityWorld world,
        float dt,
        EntityInstance harvester,
        HarvesterComponentState state,
        ResourceCargoComponentState cargo)
    {
        if (!TryGetResourceNode(world, state.FieldId, out var field, out var node))
        {
            if (cargo.Cargo > 0)
            {
                SendToRefinery(world, harvester, state);
            }
            else
            {
                ResetHarvester(harvester, state);
            }

            return;
        }

        if (cargo.Cargo >= cargo.Capacity || node.Amount <= 0)
        {
            SendToRefinery(world, harvester, state);
            return;
        }

        var amount = Math.Min(
            Math.Min(Mathf.CeilToInt(world.EconomyTuning.GatherRateFor(node) * dt), cargo.Capacity - cargo.Cargo),
            node.Amount);
        if (amount <= 0)
        {
            return;
        }

        field.Components.Set(node with { Amount = node.Amount - amount });
        harvester.Components.Set(cargo with { Cargo = cargo.Cargo + amount });
        harvester.Components.Set(state with { HarvestPulse = state.HarvestPulse + amount });
    }

    private static void StepUnloading(
        EntityWorld world,
        float dt,
        EntityInstance harvester,
        HarvesterComponentState state,
        ResourceCargoComponentState cargo)
    {
        if (cargo.Cargo <= 0)
        {
            ReleaseDock(world, harvester.Id.Value, state.RefineryId);
            world.Metrics.ClearDockWait(harvester.Id.Value);
            world.Metrics.RecordResourceTripCompleted();
            if (state.Retreating)
            {
                ResetHarvester(harvester, state);
                return;
            }

            ReturnToFieldOrIdle(world, harvester, state);
            return;
        }

        var amount = Math.Min(Mathf.CeilToInt(world.EconomyTuning.SafeUnloadRate * dt), cargo.Cargo);
        if (amount <= 0)
        {
            return;
        }

        harvester.Components.Set(cargo with { Cargo = cargo.Cargo - amount });
        world.ResourceInventory(harvester.OwnerId).Credits += amount;
        world.Metrics.RecordCreditsBanked(amount);
    }
}
