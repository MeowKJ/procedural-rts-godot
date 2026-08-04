using Godot;

namespace ProceduralRts.Core;

public sealed partial class UnitBattlefield
{
    private void RefreshResourceFieldProjection(ResourceFieldModel field)
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

    private void RefreshResourceFieldProjections()
    {
        foreach (var field in ResourceFields)
        {
            RefreshResourceFieldProjection(field);
        }
    }

}
