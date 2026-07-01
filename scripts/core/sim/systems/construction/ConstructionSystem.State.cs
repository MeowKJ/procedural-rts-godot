using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static void AdvanceConstruction(EntityWorld world, int tick, float dt)
    {
        foreach (var entity in world.OrderedEntities)
        {
            if (!entity.Components.TryGet<ConstructionComponentState>(out var construction))
            {
                continue;
            }

            if (TryDestroyDeadConstruction(world, tick, entity, construction))
            {
                continue;
            }

            if (construction.Progress >= 1)
            {
                if (construction.ReadyToPlace && construction.PauseReason != ConstructionPauseReason.None)
                {
                    entity.Components.Set(construction with { PauseReason = ConstructionPauseReason.None });
                }

                continue;
            }

            if (construction.Phase is ConstructionPhase.ReadyToPlace or ConstructionPhase.RestartCapture)
            {
                continue;
            }

            if (construction.Phase == ConstructionPhase.Queued)
            {
                var nextQueueProgress = construction.BuildTime <= 0
                    ? 1
                    : Mathf.Clamp(construction.Progress + (dt / construction.BuildTime), 0, 1);
                var nextPhase = nextQueueProgress >= 1
                    ? ConstructionPhase.ReadyToPlace
                    : ConstructionPhase.Queued;
                entity.Components.Set(construction with
                {
                    Progress = nextQueueProgress,
                    PauseReason = ConstructionPauseReason.None,
                    Phase = nextPhase,
                });
                continue;
            }

            var pauseReason = ConstructionPauseReasonFor(entity, construction);
            if (pauseReason != ConstructionPauseReason.None)
            {
                if (construction.PauseReason != pauseReason)
                {
                    entity.Components.Set(construction with { PauseReason = pauseReason });
                }

                continue;
            }

            if (construction.PauseReason != ConstructionPauseReason.None)
            {
                construction = construction with { PauseReason = ConstructionPauseReason.None };
                entity.Components.Set(construction);
            }

            var nextProgress = construction.BuildTime <= 0
                ? 1
                : Mathf.Clamp(construction.Progress + (dt / construction.BuildTime), 0, 1);
            if (nextProgress == construction.Progress)
            {
                continue;
            }

            entity.Components.Set(construction with { Progress = nextProgress, PauseReason = ConstructionPauseReason.None });
        }
    }

    private static bool TryDestroyDeadConstruction(
        EntityWorld world,
        int tick,
        EntityInstance entity,
        ConstructionComponentState construction)
    {
        if (!entity.Components.TryGet<HealthComponentState>(out var health) || health.Hp > 0)
        {
            return false;
        }

        var buildingSpecId = BuildingSpecIdFor(world, entity) ?? entity.SpecId;
        world.Events.Raise(new ConstructionDestroyedEvent(
            tick,
            entity.Id,
            entity.OwnerId,
            buildingSpecId,
            entity.Transform.Position,
            construction.Progress,
            construction.Phase));
        world.Events.Raise(new EntityDestroyedEvent(
            tick,
            entity.Id,
            entity.OwnerId,
            entity.Transform.Position));
        world.QueueRemoval(entity.Id);
        return true;
    }

    private static ConstructionPauseReason ConstructionPauseReasonFor(EntityInstance entity, ConstructionComponentState construction)
    {
        if (ShouldPauseForUnpoweredConstruction(entity, construction))
        {
            return ConstructionPauseReason.Unpowered;
        }

        return ConstructionPauseReason.None;
    }

    private static bool ShouldPauseForUnpoweredConstruction(EntityInstance entity, ConstructionComponentState construction)
    {
        return construction.Progress > 0
            && entity.Components.TryGet<PowerComponentState>(out var power)
            && IsPowerGatedConstructionConsumer(power)
            && !power.Powered;
    }

    private static bool IsPowerGatedConstructionConsumer(PowerComponentState power)
    {
        return power.Used > 0;
    }
}
