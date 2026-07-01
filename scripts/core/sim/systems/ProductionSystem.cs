using Godot;

namespace ProceduralRts.Core;

/// <summary>
/// Deterministic per-producer unit production. Producers own their queue; the
/// UI may aggregate later, but authority is local to each producer entity.
/// </summary>
public sealed partial class ProductionSystem : ISimSystem
{
    private readonly List<EntityInstance> _producerStepBuffer = [];
    private readonly List<SpawnObstacle> _spawnObstacles = [];

    public void Step(SimContext context)
    {
        foreach (var sequenced in context.Commands)
        {
            if (sequenced.Command is ProduceEntityCommand produce)
            {
                ApplyProduce(context.World, produce);
            }
            else if (sequenced.Command is CancelProductionEntityCommand cancel)
            {
                ApplyCancel(context.World, cancel);
            }
            else if (sequenced.Command is SetRepeatProductionEntityCommand repeat)
            {
                ApplyRepeat(context.World, repeat);
            }
            else if (sequenced.Command is SetRallyPointEntityCommand rally)
            {
                ApplyRally(context.World, rally);
            }
        }

        AdvanceQueues(context.World, context.FixedDelta);
    }

    private static void ApplyProduce(EntityWorld world, ProduceEntityCommand command)
    {
        if (!TryGetUnitSpec(command.OutputSpecId, out var unitSpec) || unitSpec.Production is null)
        {
            return;
        }

        foreach (var producerId in command.Subjects)
        {
            if (!world.TryGet(producerId, out var producer)
                || producer.OwnerId.Value != command.Issuer.Value
                || !producer.Components.TryGet<ProductionQueueComponentState>(out var queue)
                || !CanProducerBuild(world, producer, unitSpec))
            {
                continue;
            }

            var inventory = world.ResourceInventory(command.Issuer);
            if (inventory.Credits < unitSpec.Stats.Cost)
            {
                continue;
            }

            inventory.Credits -= unitSpec.Stats.Cost;
            EnqueueUnit(world, producer, queue, unitSpec);
        }
    }

    private static void ApplyCancel(EntityWorld world, CancelProductionEntityCommand command)
    {
        foreach (var producerId in command.Subjects)
        {
            if (!world.TryGet(producerId, out var producer)
                || producer.OwnerId.Value != command.Issuer.Value
                || !producer.Components.TryGet<ProductionQueueComponentState>(out var queue)
                || queue.Items.Count == 0)
            {
                continue;
            }

            var item = queue.Items[0];
            if (TryGetUnitSpec(item.DesignId, out var unitSpec))
            {
                var refund = Mathf.RoundToInt(unitSpec.Stats.Cost * Math.Clamp(command.RefundRatio, 0, 1));
                world.ResourceInventory(command.Issuer).Credits += refund;
            }

            RemoveFirstQueueItem(producer, queue);
        }
    }

    private static void ApplyRepeat(EntityWorld world, SetRepeatProductionEntityCommand command)
    {
        foreach (var producerId in command.Subjects)
        {
            if (!world.TryGet(producerId, out var producer)
                || producer.OwnerId.Value != command.Issuer.Value
                || !producer.Components.TryGet<ProductionQueueComponentState>(out var queue))
            {
                continue;
            }

            if (!command.Enabled)
            {
                producer.Components.Set(queue with { RepeatOutputSpecId = null });
                continue;
            }

            if (!TryGetUnitSpec(command.OutputSpecId, out var unitSpec)
                || unitSpec.Production is null
                || !CanProducerBuild(world, producer, unitSpec))
            {
                continue;
            }

            producer.Components.Set(queue with { RepeatOutputSpecId = unitSpec.Id });
        }
    }

    private static void ApplyRally(EntityWorld world, SetRallyPointEntityCommand command)
    {
        int? targetEntityId = command.TargetEntity.IsValid ? command.TargetEntity.Value : null;
        foreach (var producerId in command.Subjects)
        {
            if (!world.TryGet(producerId, out var producer)
                || producer.OwnerId.Value != command.Issuer.Value
                || !producer.Components.Has<ProductionQueueComponentState>())
            {
                continue;
            }

            producer.Components.Set(new RallyPointComponentState(command.Target, targetEntityId));
        }
    }

    private void AdvanceQueues(EntityWorld world, float dt)
    {
        _producerStepBuffer.Clear();
        foreach (var producer in world.OrderedEntities)
        {
            if (producer.Components.Has<ProductionQueueComponentState>())
            {
                _producerStepBuffer.Add(producer);
            }
        }

        foreach (var producer in _producerStepBuffer)
        {
            if (!producer.Components.TryGet<ProductionQueueComponentState>(out var queue))
            {
                continue;
            }

            EnsureRepeatQueued(world, producer, queue);
            queue = producer.Components.Require<ProductionQueueComponentState>();
            if (queue.Items.Count == 0)
            {
                continue;
            }

            var pauseReason = ProductionPauseReasonFor(producer);
            if (pauseReason != ProductionPauseReason.None)
            {
                if (queue.PauseReason != pauseReason)
                {
                    producer.Components.Set(queue with { PauseReason = pauseReason });
                }

                continue;
            }

            if (queue.PauseReason != ProductionPauseReason.None)
            {
                queue = queue with { PauseReason = ProductionPauseReason.None };
                producer.Components.Set(queue);
            }

            var item = queue.Items[0];
            if (!TryGetUnitSpec(item.DesignId, out var unitSpec) || unitSpec.Production is null)
            {
                RemoveFirstQueueItem(producer, queue);
                continue;
            }

            var advance = ProductionMath.Advance(item.Progress, dt, unitSpec.Production.Duration);
            item.Progress = advance.Progress;
            if (!advance.IsComplete)
            {
                continue;
            }

            SpawnProducedUnit(world, producer, unitSpec);
            RemoveFirstQueueItem(producer, queue);
            queue = producer.Components.Require<ProductionQueueComponentState>();
            EnsureRepeatQueued(world, producer, queue);
        }

        _producerStepBuffer.Clear();
    }

    private static void EnsureRepeatQueued(EntityWorld world, EntityInstance producer, ProductionQueueComponentState queue)
    {
        if (queue.Items.Count > 0
            || string.IsNullOrWhiteSpace(queue.RepeatOutputSpecId)
            || !TryGetUnitSpec(queue.RepeatOutputSpecId, out var repeatSpec)
            || repeatSpec.Production is null
            || !CanProducerBuild(world, producer, repeatSpec))
        {
            return;
        }

        var inventory = world.ResourceInventory(producer.OwnerId);
        if (inventory.Credits < repeatSpec.Stats.Cost)
        {
            return;
        }

        inventory.Credits -= repeatSpec.Stats.Cost;
        EnqueueUnit(world, producer, queue, repeatSpec);
    }

    private static bool CanProducerBuild(EntityWorld world, EntityInstance producer, UnitSpec unitSpec)
    {
        if (unitSpec.Production is null)
        {
            return false;
        }

        if (producer.Components.TryGet<HealthComponentState>(out var health) && health.Hp <= 0)
        {
            return false;
        }

        if (producer.Components.TryGet<ConstructionComponentState>(out var construction) && construction.Progress < 1)
        {
            return false;
        }

        if (world.TryGetSpec(producer.SpecId, out var producerSpec)
            && producerSpec.Authoring.BuildingSpecId is { } BuildingSpecId)
        {
            return BuildingSpecId == unitSpec.Production.ProducerKind
                && producerSpec.Authoring.TechTier >= unitSpec.Stats.TechTier;
        }

        return false;
    }

    private static ProductionPauseReason ProductionPauseReasonFor(EntityInstance producer)
    {
        if (producer.Components.TryGet<ConstructionComponentState>(out var construction) && construction.Progress < 1)
        {
            return ProductionPauseReason.UnderConstruction;
        }

        if (producer.Components.TryGet<PowerComponentState>(out var power) && !power.Powered)
        {
            return ProductionPauseReason.Unpowered;
        }

        return ProductionPauseReason.None;
    }

    private static void RemoveFirstQueueItem(EntityInstance producer, ProductionQueueComponentState queue)
    {
        var items = new UnitProductionQueueItem[Math.Max(0, queue.Items.Count - 1)];
        for (var index = 1; index < queue.Items.Count; index++)
        {
            items[index - 1] = queue.Items[index];
        }

        producer.Components.Set(queue with
        {
            Items = items,
            PauseReason = items.Length == 0 ? ProductionPauseReason.None : queue.PauseReason,
        });
    }

    private static void EnqueueUnit(
        EntityWorld world,
        EntityInstance producer,
        ProductionQueueComponentState queue,
        UnitSpec unitSpec)
    {
        var items = new UnitProductionQueueItem[queue.Items.Count + 1];
        for (var index = 0; index < queue.Items.Count; index++)
        {
            items[index] = queue.Items[index];
        }

        items[^1] = new UnitProductionQueueItem
        {
            Id = world.AllocateProductionItemId(),
            Kind = ProductionKindDesignBridge.ProductionKindFor(unitSpec),
            DesignId = unitSpec.Id,
            Faction = unitSpec.Faction,
            Progress = 0,
        };
        producer.Components.Set(queue with { Items = items });
    }

    private static bool TryGetUnitSpec(string designId, out UnitSpec unitSpec)
    {
        try
        {
            unitSpec = UnitDesignCatalog.Spec(designId);
            return true;
        }
        catch (InvalidOperationException)
        {
            unitSpec = null!;
            return false;
        }
    }

}
