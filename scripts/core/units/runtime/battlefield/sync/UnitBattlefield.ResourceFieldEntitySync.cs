using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void SyncResourceFieldEntities()
    {
        foreach (var field in ResourceFields)
        {
            SyncResourceFieldEntity(field);
        }
    }

    private void SyncResourceFieldEntity(ResourceFieldModel field)
    {
        var spec = ResourceFieldEntitySpec(field);
        var components = new EntityComponentState[]
        {
            new ResourceNodeComponentState(field.Amount, field.MaxAmount),
            new CollisionComponentState(field.Radius, 10, 0, BlocksMovement: false),
        };

        if (_resourceFieldEntityIds.TryGetValue(field.Id, out var entityId) && _entityWorld.TryGet(entityId, out var existing))
        {
            existing.Transform = EntityTransform.At(field.Position);
            existing.Components.Clear();
            foreach (var component in components)
            {
                existing.Components.Set(component);
            }

            return;
        }

        var entity = _entityWorld.Spawn(spec, OwnerId.None, EntityTransform.At(field.Position), components);
        _resourceFieldEntityIds[field.Id] = entity.Id;
    }

    private void SyncResourceFieldFromEntity(ResourceFieldModel field)
    {
        if (!_resourceFieldEntityIds.TryGetValue(field.Id, out var entityId)
            || !_entityWorld.TryGet(entityId, out var entity)
            || !entity.Components.TryGet<ResourceNodeComponentState>(out var node))
        {
            return;
        }

        if (node.Amount != field.Amount)
        {
            field.Pulse = 1;
        }

        field.Amount = node.Amount;
    }

    private void SyncResourceFieldsFromEntities()
    {
        foreach (var field in ResourceFields)
        {
            SyncResourceFieldFromEntity(field);
        }
    }

    private static EntitySpec ResourceFieldEntitySpec(ResourceFieldModel field)
    {
        return new EntitySpec
        {
            Id = $"resource.field.{field.Id}",
            Kind = EntityKind.Resource,
            Display = new EntityDisplaySpec(
                $"Resource Field {field.Id}",
                "resource.field.name",
                "resource.field.role",
                $"R{field.Id}",
                IconGlyph.Harvester),
            Tags = new HashSet<string> { "Resource", "Credit" },
            Collision = new CollisionSpec(field.Radius, 10, 0, BlocksMovement: false),
        };
    }
}
