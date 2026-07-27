using Godot;

namespace ProceduralRts.Core;

public sealed partial class ConstructionSystem
{
    private static EntitySpec QueuedConstructionSpec(BuildSpec spec)
    {
        return new EntitySpec
        {
            Id = $"construction.queue.{spec.Kind}",
            Kind = EntityKind.Objective,
            Display = new EntityDisplaySpec(
                $"{spec.Label} Queue",
                spec.NameKey,
                spec.RoleKey,
                spec.ShortCode,
                spec.Icon),
            Authoring = new EntityAuthoringMetadata(BuildingSpecId: spec.Kind),
        };
    }

    private Vector2 QueueTicketPosition(EntityWorld world, QueueConstructionEntityCommand command)
    {
        CollectOrderedSubjects(command.Subjects, _constructionSubjectOrder);
        foreach (var subject in _constructionSubjectOrder)
        {
            if (world.TryGet(subject, out var entity))
            {
                return entity.Transform.Position;
            }
        }

        return Vector2.Zero;
    }

    private static IEnumerable<EntityComponentState> QueuedConstructionComponents(BuildSpec spec)
    {
        yield return new ConstructionIdentityComponentState(spec.Kind);
        yield return new ConstructionComponentState(
            Progress: spec.BuildTime <= 0 ? 1 : 0,
            BuildTime: spec.BuildTime,
            Cost: spec.Cost,
            RefundRatio: spec.RefundRatio,
            Phase: spec.BuildTime <= 0 ? ConstructionPhase.ReadyToPlace : ConstructionPhase.Queued);
    }

    private static IEnumerable<EntityComponentState> InitialConstructionComponents(
        BuildSpec spec,
        float facing,
        float? initialProgress = null)
    {
        var progress = initialProgress ?? (spec.BuildTime <= 0 ? 1 : 0);
        var logicalFootprint = spec.LogicalFootprint(facing);
        yield return new ConstructionIdentityComponentState(spec.Kind);
        yield return new HealthComponentState(spec.MaxHp, spec.MaxHp);
        yield return new SelectableComponentState();
        yield return new VisionComponentState(spec.SightRange);
        yield return new CollisionComponentState(
            Mathf.Max(logicalFootprint.X, logicalFootprint.Y) * 0.5f,
            8,
            100,
            BlocksMovement: true);
        yield return new FootprintComponentState(logicalFootprint, spec.PlacementDomain);
        yield return new ConstructionComponentState(
            Progress: Mathf.Clamp(progress, 0, 1),
            BuildTime: spec.BuildTime,
            Cost: spec.Cost,
            RefundRatio: spec.RefundRatio);
        yield return new PowerComponentState(spec.PowerProvided, spec.PowerUsed, Powered: false);
        yield return new RallyPointComponentState();
        yield return new PresentationPulseComponentState();

        if (spec.WeaponId is { } weaponId)
        {
            yield return new WeaponUserComponentState(
            [
                new WeaponMountRuntimeState("main", weaponId, facing, 0),
            ]);
        }

        if (spec.Kind == BuildingDesignIds.Refinery)
        {
            yield return new DockComponentState();
        }

        if (ProducesUnits(spec.Kind))
        {
            yield return new ProductionQueueComponentState(Array.Empty<UnitProductionQueueItem>());
        }

        if (spec.BuildRadius > 0)
        {
            yield return new BuildRadiusComponentState(spec.BuildRadius);
        }
    }

    private static bool ProducesUnits(string kind)
    {
        return kind is BuildingDesignIds.Barracks
            or BuildingDesignIds.VehicleFactory
            or BuildingDesignIds.Airfield;
    }
}
