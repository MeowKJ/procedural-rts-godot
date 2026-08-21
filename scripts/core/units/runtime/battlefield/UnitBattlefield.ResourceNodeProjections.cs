namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    public UnitBattlefieldResourceNodeProjection? ResourceNodeProjection(EntityId entityId)
    {
        return _entityWorld.TryGet(entityId, out var entity) && TryProjectResourceNode(entity, out var projection)
            ? projection
            : null;
    }

    public IReadOnlyList<UnitBattlefieldResourceNodeProjection> ResourceNodeProjections()
    {
        _resourceNodeProjectionBuffer.Clear();
        foreach (var entity in _entityWorld.OrderedEntities)
        {
            if (TryProjectResourceNode(entity, out var projection))
            {
                _resourceNodeProjectionBuffer.Add(projection);
            }
        }

        return _resourceNodeProjectionBuffer;
    }

    private static bool TryProjectResourceNode(
        EntityInstance entity,
        out UnitBattlefieldResourceNodeProjection projection)
    {
        if (!entity.Components.TryGet<ResourceNodeComponentState>(out var node)
            || !entity.Components.TryGet<CollisionComponentState>(out var collision)
            || !entity.Components.TryGet<ResourcePresentationComponentState>(out var presentation))
        {
            projection = default;
            return false;
        }

        projection = new UnitBattlefieldResourceNodeProjection(
            entity.Id,
            entity.Transform.Position,
            collision.Radius,
            node.MaxAmount,
            node.Amount,
            presentation.Accent);
        return true;
    }
}
