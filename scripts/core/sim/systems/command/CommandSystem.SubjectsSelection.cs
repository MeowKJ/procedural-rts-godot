namespace ProceduralRts.Core;

public sealed partial class CommandSystem
{
    private static IEnumerable<EntityInstance> OwnedSubjects(
        EntityWorld world,
        OwnerId issuer,
        IReadOnlyList<EntityId> subjects)
    {
        foreach (var entityId in subjects)
        {
            if (world.TryGet(entityId, out var entity) && entity.OwnerId.Value == issuer.Value)
            {
                yield return entity;
            }
        }
    }

    private static void ApplySelection(EntityWorld world, SetSelectionEntityCommand command)
    {
        var selected = command.Subjects
            .Select(id => id.Value)
            .ToHashSet();

        foreach (var entity in world.OrderedEntities)
        {
            if (entity.OwnerId.Value != command.Issuer.Value
                || !entity.Components.TryGet<SelectableComponentState>(out var selectable))
            {
                continue;
            }

            entity.Components.Set(selectable with { Selected = selected.Contains(entity.Id.Value) });
        }
    }
}
