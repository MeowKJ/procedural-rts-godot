using Godot;

namespace ProceduralRts.Core;

public sealed partial class ProductionSystem
{
    private void SpawnProducedUnit(EntityWorld world, EntityInstance producer, UnitSpec unitSpec)
    {
        var spawn = SpawnPointFor(world, producer, unitSpec);
        var unit = world.SpawnUnit(unitSpec, producer.OwnerId, new Vector2(spawn.X, spawn.Y), producer.Transform.Facing);
        if (!producer.Components.TryGet<RallyPointComponentState>(out var rally))
        {
            return;
        }

        if (TryApplyResourceRally(world, unit, rally))
        {
            return;
        }

        if (TryApplyEntityRally(world, unit, rally))
        {
            return;
        }

        if (rally.Target is { } target)
        {
            ApplyPointRally(unit, target);
        }
    }

    private static bool TryApplyResourceRally(EntityWorld world, EntityInstance unit, RallyPointComponentState rally)
    {
        if (rally.TargetEntityId is not int targetId
            || !unit.Components.Has<HarvesterComponentState>()
            || !unit.Components.Has<ResourceCargoComponentState>()
            || !world.TryGet(new EntityId(targetId), out var resource)
            || !resource.Components.TryGet<ResourceNodeComponentState>(out var node)
            || node.Amount <= 0)
        {
            return false;
        }

        unit.Components.Set(new HarvesterComponentState(
            HarvesterMode.MovingToField,
            FieldId: resource.Id.Value));
        ApplyPointRally(unit, resource.Transform.Position);
        return true;
    }

    private static bool TryApplyEntityRally(EntityWorld world, EntityInstance unit, RallyPointComponentState rally)
    {
        if (rally.TargetEntityId is not int targetId
            || !world.TryGet(new EntityId(targetId), out var target)
            || target.Components.Has<ResourceNodeComponentState>()
            || world.Relations.Relation(unit.OwnerId, target.OwnerId) is not (PlayerRelation.Self or PlayerRelation.Allied))
        {
            return false;
        }

        ApplyPointRally(unit, target.Transform.Position);
        return true;
    }

    private static void ApplyPointRally(EntityInstance unit, Vector2 target)
    {
        var movement = unit.Components.TryGet<MovementComponentState>(out var existingMovement)
            ? existingMovement
            : new MovementComponentState(Vector2.Zero);
        unit.Components.Set(movement with { MoveTarget = target, FormationSlot = target });

        var commandable = unit.Components.TryGet<CommandableComponentState>(out var existingCommandable)
            ? existingCommandable
            : new CommandableComponentState();
        unit.Components.Set(commandable with
        {
            PlayerIntentTarget = target,
            CommandVisualTarget = target,
            MoveMode = MoveCommandMode.Direct,
        });
    }

    private SpawnPoint SpawnPointFor(EntityWorld world, EntityInstance producer, UnitSpec unitSpec)
    {
        var producerSize = producer.Components.TryGet<FootprintComponentState>(out var footprint)
            ? footprint.Size
            : new Vector2(96, 96);
        var unitRadius = unitSpec.Collision.Radius;
        _spawnObstacles.Clear();
        foreach (var entity in world.OrderedEntities)
        {
            if (entity.Id.Value == producer.Id.Value
                || !entity.Components.TryGet<CollisionComponentState>(out var collision)
                || !collision.BlocksMovement)
            {
                continue;
            }

            _spawnObstacles.Add(new SpawnObstacle(
                entity.Transform.Position.X,
                entity.Transform.Position.Y,
                collision.Radius));
        }

        return ProductionSpawnMath.FindSpawnPoint(
            producer.Transform.Position.X,
            producer.Transform.Position.Y,
            producer.Transform.Facing,
            producerSize.X,
            producerSize.Y,
            unitRadius,
            world.WorldWidth,
            world.WorldHeight,
            _spawnObstacles);
    }
}
