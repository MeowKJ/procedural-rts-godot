using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public static UnitBattlefield AdoptLoadedMap(EntityWorld world, MapSpec map)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(map);
        if (world.MapEnvironment.WorldSize != map.WorldSize
            || MathF.Abs(world.WorldWidth - map.WorldSize.Width) > 0.001f
            || MathF.Abs(world.WorldHeight - map.WorldSize.Height) > 0.001f)
        {
            throw new InvalidOperationException("Loaded map world does not match its MapSpec bounds.");
        }

        var battlefield = new UnitBattlefield(world)
        {
            WorldSize = map.WorldSize.ToVector2(),
        };
        battlefield.AdoptLoadedOwners(map);
        battlefield.AdoptLoadedResources(map);
        battlefield.AdoptLoadedBuildings();
        battlefield.AdoptLoadedUnits();
        return battlefield;
    }

    private void AdoptLoadedOwners(MapSpec map)
    {
        foreach (var first in map.OwnerStarts)
        {
            foreach (var second in map.OwnerStarts)
            {
                if (first.OwnerId == second.OwnerId)
                {
                    continue;
                }

                Relations.Set(
                    first.OwnerId.ToPlayerSlot(),
                    second.OwnerId.ToPlayerSlot(),
                    _entityWorld.Relations.Relation(first.OwnerId, second.OwnerId));
            }
        }
    }

    private void AdoptLoadedResources(MapSpec map)
    {
        for (var index = 0; index < map.Resources.Count; index++)
        {
            var source = map.Resources[index];
            var entity = _entityWorld.OrderedEntities.FirstOrDefault(candidate =>
                candidate.SpecId == $"map.resource.{source.Id}")
                ?? throw new InvalidOperationException($"Loaded map resource '{source.Id}' is missing its EntityWorld entity.");
            var node = entity.Components.Require<ResourceNodeComponentState>();
            var collision = entity.Components.Require<CollisionComponentState>();
            var field = new ResourceFieldModel
            {
                Id = index + 1,
                Position = entity.Transform.Position,
                Radius = collision.Radius,
                Amount = node.Amount,
                MaxAmount = node.MaxAmount,
                Accent = source.Accent.ToColor(),
            };
            _resourceFields.Add(field);
            _resourceFieldEntityIds[field.Id] = entity.Id;
        }
    }

    private void AdoptLoadedBuildings()
    {
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (!entity.Components.TryGet<BuildingIdentityComponentState>(out var identity))
            {
                continue;
            }

            SetBuildingTargetEntityId(identity.BuildingId, entity.Id);
            _nextBuildingTargetId = Math.Max(_nextBuildingTargetId, identity.BuildingId + 1);
            EnsureProductionQueueComponent(identity.BuildingId, entity);
        }
    }

    private void AdoptLoadedUnits()
    {
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (_entityWorld.TryGetSpec(entity.SpecId, out var spec) && spec.Kind == EntityKind.Unit)
            {
                AdoptUnitEntity(entity);
            }
        }
    }
}
